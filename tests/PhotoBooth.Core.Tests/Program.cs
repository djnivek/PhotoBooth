using PhotoBooth.Core;

var tests = new List<(string Name, Action Test)>();
void Test(string name, Action action) => tests.Add((name, action));
void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
Session Captured(int count = 4, PhotoMode mode = PhotoMode.Original)
{
    var s = new Session(new SessionOptions(count, mode));
    for (var i = 0; i < count; i++)
    {
        s.StartCountdown();
        for (var tick = 0; tick < s.Options.CountdownSeconds; tick++) s.Tick();
        s.RecordCapture(new CapturedPhoto(Guid.NewGuid(), $"original-{i}"));
    }
    return s;
}
Session Ready()
{
    var s = Captured();
    s.CompleteProcessing(s.Captures.Select(c => new FinalPhoto(c.Id, c.LocalAssetKey)), "sheet");
    return s;
}

foreach (var count in new[] { 1, 4 }) foreach (var mode in new[] { PhotoMode.Original, PhotoMode.Surprise })
    Test($"{count} / {mode}: live view, complete countdown, full-resolution capture, explicit print", () =>
    {
        var s = new Session(new SessionOptions(count, mode));
        for (var i = 0; i < count; i++)
        {
            Check(s.WantsLiveView);
            s.StartCountdown();
            for (var t = s.Options.CountdownSeconds; t >= 1; t--) { Check(s.SecondsRemaining == t && s.WantsLiveView); s.Tick(); }
            Check(s.Stage == SessionStage.Capturing && !s.WantsLiveView);
            s.RecordCapture(new CapturedPhoto(Guid.NewGuid(), $"capture-{i}"));
        }
        Check(s.Stage == SessionStage.Processing);
        if (mode == PhotoMode.Surprise) s.AssignStyle("test_style");
        s.CompleteProcessing(s.Captures.Reverse().Select(c => new FinalPhoto(c.Id,
            mode == PhotoMode.Original ? c.LocalAssetKey : $"edited-{c.Id}")), "sheet");
        Check(s.Results.Select(x => x.CaptureId).SequenceEqual(s.Captures.Select(x => x.Id)));
        Check(s.PrintStage == PrintStage.NotRequested);
        var request = s.RequestPrint();
        Check(request.SessionId == s.Id && request.SheetAssetKey == "sheet");
    });
Test("reject unsupported shot counts, modes and countdowns", () =>
{
    foreach (var c in new[] { -1, 0, 2, 3, 5 }) Throws<ArgumentOutOfRangeException>(() => new SessionOptions(c, PhotoMode.Original));
    Throws<ArgumentOutOfRangeException>(() => new SessionOptions(1, (PhotoMode)9));
    Throws<ArgumentOutOfRangeException>(() => new SessionOptions(1, PhotoMode.Original, 0));
    Throws<ArgumentOutOfRangeException>(() => new SessionOptions(1, PhotoMode.Original, 11));
});
Test("cannot capture before countdown ends", () =>
{
    var s = new Session(new SessionOptions(1, PhotoMode.Original));
    Throws<InvalidOperationException>(() => s.RecordCapture(new(Guid.NewGuid(), "original")));
    s.StartCountdown(); s.Tick();
    Throws<InvalidOperationException>(() => s.RecordCapture(new(Guid.NewGuid(), "original")));
});
Test("cannot double-start or over-tick countdown", () =>
{
    var s = new Session(new SessionOptions(1, PhotoMode.Original)); s.StartCountdown();
    Throws<InvalidOperationException>(s.StartCountdown);
    for (var i = 0; i < s.Options.CountdownSeconds; i++) s.Tick();
    Throws<InvalidOperationException>(s.Tick);
});
Test("four-shot session requires four distinct captures", () =>
{
    var s = new Session(new SessionOptions(4, PhotoMode.Original));
    var photo = new CapturedPhoto(Guid.NewGuid(), "original");
    for (var i = 0; i < 2; i++)
    {
        s.StartCountdown(); for (var t = 0; t < s.Options.CountdownSeconds; t++) s.Tick();
        if (i == 0) s.RecordCapture(photo);
        else Throws<ArgumentException>(() => s.RecordCapture(photo));
    }
    Check(s.Captures.Count == 1);
});
Test("partial AI completion cannot reach review", () =>
{
    var s = Captured(4, PhotoMode.Surprise); s.AssignStyle("space");
    Throws<ArgumentException>(() => s.CompleteProcessing(s.Captures.Take(3).Select(c => new FinalPhoto(c.Id, $"edited-{c.Id}")), "sheet"));
    Check(s.Stage == SessionStage.Processing);
});
Test("result ownership and duplicates are checked", () =>
{
    var s = Captured();
    Throws<ArgumentException>(() => s.CompleteProcessing(Enumerable.Range(0, 4).Select(i => new FinalPhoto(Guid.NewGuid(), $"asset-{i}")), "sheet"));
    Throws<ArgumentException>(() => s.CompleteProcessing(Enumerable.Range(0, 4).Select(i => new FinalPhoto(s.Captures[0].Id, $"asset-{i}")), "sheet"));
});
Test("original mode preserves the original references", () =>
{
    var s = Captured(1);
    Throws<ArgumentException>(() => s.CompleteProcessing([new(s.Captures[0].Id, "filtered")], "sheet"));
    Throws<InvalidOperationException>(() => s.AssignStyle("unexpected"));
});
Test("AI results cannot silently fall back to originals", () =>
{
    var s = Captured(1, PhotoMode.Surprise); s.AssignStyle("space");
    Throws<ArgumentException>(() => s.CompleteProcessing([new(s.Captures[0].Id, s.Captures[0].LocalAssetKey)], "sheet"));
});
Test("AI style is required and stable across retry", () =>
{
    var s = Captured(1, PhotoMode.Surprise);
    Throws<InvalidOperationException>(() => s.CompleteProcessing([new(s.Captures[0].Id, "edited")], "sheet"));
    s.AssignStyle("space"); s.AssignStyle("space");
    Throws<InvalidOperationException>(() => s.AssignStyle("watercolor"));
});
Test("cannot print before review", () => Throws<InvalidOperationException>(() => Captured().RequestPrint()));
Test("double click cannot duplicate a print request", () =>
{
    var s = Ready(); var r = s.RequestPrint();
    Throws<InvalidOperationException>(() => s.RequestPrint());
    s.MarkPrintSubmitted(r.Id, "windows-job-42");
    Check(s.PrintStage == PrintStage.Submitted);
    Throws<InvalidOperationException>(() => s.RequestPrint());
});
Test("ambiguous print requires operator, no automatic retry", () =>
{
    var s = Ready(); var r = s.RequestPrint(); s.MarkPrintUncertain(r.Id);
    Check(s.PrintStage == PrintStage.NeedsOperator);
    Throws<InvalidOperationException>(() => s.RequestPrint());
});
Test("spool acknowledgment must match the request", () =>
{
    var s = Ready(); s.RequestPrint();
    Throws<InvalidOperationException>(() => s.MarkPrintSubmitted(Guid.NewGuid(), "job"));
});
Test("failure retains captured originals and stops live view", () =>
{
    var s = Captured(); s.Fail("network_unavailable");
    Check(s.Captures.Count == 4 && !s.WantsLiveView && s.Stage == SessionStage.Failed);
    Throws<InvalidOperationException>(s.StartCountdown);
});
Test("cancel countdown prevents subsequent capture", () =>
{
    var s = new Session(new SessionOptions(4, PhotoMode.Original)); s.StartCountdown(); s.Cancel();
    Check(!s.WantsLiveView && s.SecondsRemaining == 0);
    Throws<InvalidOperationException>(s.Tick);
});
Test("4x6 layout contains nonoverlapping frames inside safe margins", () =>
{
    foreach (var n in new[] { 1, 4 })
    {
        var l = PrintLayout.FourBySix(n);
        Check(l.Width == 1800 && l.Height == 1200 && l.Dpi == 300 && l.Photos.Count == n);
        foreach (var a in l.Photos)
        {
            Check(a.X >= 60 && a.Y >= 60 && a.X + a.Width <= 1740 && a.Y + a.Height <= 1140);
            foreach (var b in l.Photos.Where(b => b != a))
                Check(a.X + a.Width <= b.X || b.X + b.Width <= a.X || a.Y + a.Height <= b.Y || b.Y + b.Height <= a.Y);
        }
    }
});
Test("layout rejects unsupported dimensions", () =>
{
    Throws<ArgumentOutOfRangeException>(() => PrintLayout.FourBySix(2));
    Throws<ArgumentOutOfRangeException>(() => PrintLayout.FourBySix(4, -1));
    Throws<ArgumentOutOfRangeException>(() => PrintLayout.FourBySix(4, 60, 9999));
});
Test("Operator code is exactly four printable characters", () =>
{
    Throws<ArgumentException>(() => KioskExitGate.Create("123"));
    Throws<ArgumentException>(() => KioskExitGate.Create("a bc"));
});
Test("exit PIN rejects wrong code and enforces cooldown", () =>
{
    var clock = new ManualTime();
    var gate = KioskExitGate.Create("E429", clock); // Test fixture only; never an application default.
    Check(gate.TryAuthorizeExit("E429"));
    for (var i = 0; i < 5; i++) Check(!gate.TryAuthorizeExit("wrong"));
    Check(!gate.TryAuthorizeExit("E429"));
    clock.Advance(TimeSpan.FromSeconds(30));
    Check(!gate.TryAuthorizeExit("0000"));
    Check(gate.TryAuthorizeExit("E429"));
});

Test("catalog accepts new identifiers without prompts and rejects malformed metadata", () =>
{
    var json = System.Text.Json.JsonSerializer.Serialize(StyleCatalog.BuiltIn);
    var loaded = StyleCatalog.Parse(json);
    Check(loaded.Styles.Length == 9 && loaded.Find("not_bundled").Id == StyleCatalog.Chef);
    Check(new SessionOptions(4, PhotoMode.Surprise, styleChoice: "ocean").StyleChoice == "ocean");
    Throws<InvalidDataException>(() => StyleCatalog.Parse("{\"version\":\"x\",\"styles\":[null]}"));
    Throws<ArgumentException>(() => new SessionOptions(1, PhotoMode.Surprise, styleChoice: "../path"));
});
Test("corner hold requires a complete continuous three seconds and triggers once", () =>
{
    var clock = new HoldTime(); var hold = new CornerHold(clock);
    hold.Begin(); clock.Advance(2999); Check(!hold.Poll()); clock.Advance(1); Check(hold.Poll()); Check(!hold.Poll());
    hold.Begin(); clock.Advance(2000); hold.Cancel(); clock.Advance(2000); Check(!hold.Poll());
    hold.Begin(); clock.Advance(3000); Check(hold.Poll());
});
Test("four-character operator code roundtrip and legacy credential compatibility", () =>
{
    var gate = KioskExitGate.Create("E62!"); Check(gate.TryAuthorizeExit("E62!"));
    Check(KioskExitGate.Restore(gate.Export()).TryAuthorizeExit("E62!"));
    var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
    var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("381629",salt,600000,System.Security.Cryptography.HashAlgorithmName.SHA256,32);
    var old = new KioskExitGate.Credential(Convert.ToBase64String(salt),Convert.ToBase64String(hash),0,default);
    Check(KioskExitGate.Restore(old).TryAuthorizeExit("381629"));
});

Test("catalog categories are ordered, backward compatible and bounded", () =>
{
    Check(StyleCatalog.Categories.Select(c => c.Id).SequenceEqual(new[] {"estelle", "essentials", "other"}));
    var oldJson = System.Text.Json.JsonSerializer.Serialize(StyleCatalog.BuiltIn).Replace(",\"Category\":\"other\"", "");
    Check(StyleCatalog.Parse(oldJson).Styles.All(style => style.Category == "other"));
    var categorized = StyleCatalog.BuiltIn with { Styles = [StyleCatalog.BuiltIn.Styles[0] with { Category = "estelle" }] };
    Check(StyleCatalog.Parse(System.Text.Json.JsonSerializer.Serialize(categorized)).Styles[0].Category == "estelle");
    Throws<InvalidDataException>(() => StyleCatalog.Parse(System.Text.Json.JsonSerializer.Serialize(categorized with {
        Styles = [categorized.Styles[0] with { Category = "untrusted" }] })));
});

Test("theme selection is bounded, immutable and independent of click order", () =>
{
    string[] chosen = ["space", "halloween", "fantasy"];
    var options = new SessionOptions(4, PhotoMode.Surprise, styleChoices: chosen);
    chosen[0] = "disco";
    Check(options.StyleChoices.SequenceEqual(new[] {"fantasy", "halloween", "space"}) && options.StyleChoice == StyleCatalog.Mixed);
    foreach (var invalid in new string[][] { [], ["chef", "space"], ["space", "space"], ["mix"], ["../scene"], ["a","b","c","d","e"] })
        Throws<ArgumentException>(() => new SessionOptions(4, PhotoMode.Surprise, styleChoices: invalid));
    Throws<ArgumentException>(() => new SessionOptions(1, PhotoMode.Surprise, styleChoices: ["space", "fantasy"]));
});
Test("four poses use every selected theme with balanced diagonal repetition", () =>
{
    string[] all = ["halloween", "space", "fantasy", "disco"];
    for (var count = 1; count <= 4; count++) for (var trial = 0; trial < 64; trial++)
    {
        var choices = all.Take(count).ToArray();
        var plan = StyleSelection.RehearsalPlan(choices, 4);
        Check(StyleSelection.MatchesPlan(choices, plan, 4));
        Check(plan.Distinct().Count() == count);
        if(count>1)Check(plan[0]!=plan[1]&&plan[0]!=plan[2]&&plan[1]!=plan[3]&&plan[2]!=plan[3]);
        Check(plan.GroupBy(id => id).Max(g => g.Count()) == (int)Math.Ceiling(4d/count));
    }
    var chef = StyleSelection.RehearsalPlan([StyleCatalog.Chef], 4);
    Check(chef.Count == 4 && chef.Distinct().Count() == 4);
});
Test("per-pose assignment is frozen and copied; original fallback clears it", () =>
{
    var session = Captured();
    Throws<InvalidOperationException>(() => session.AssignStyles(["space", "halloween", "fantasy", "disco"]));
    session = Captured(4, PhotoMode.Surprise);
    string[] plan = ["space", "halloween", "space", "fantasy"];
    session.AssignStyles(plan); plan[0] = "disco";
    Check(session.StyleId == StyleCatalog.Mixed && session.ShotStyles[0] == "space");
    Throws<InvalidOperationException>(() => session.AssignStyles(plan));
    session.Fail("generation_failed"); session.PrepareOriginalFallback();
    Check(session.ShotStyles.Count == 0 && session.StyleId is null);
});

var failed = 0;
foreach (var (name, action) in tests)
{
    try { action(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failed++; Console.Error.WriteLine($"FAIL {name}: {error.Message}"); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
return failed == 0 ? 0 : 1;

sealed class ManualTime : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UnixEpoch;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan elapsed) => now += elapsed;
}

sealed class HoldTime : TimeProvider
{
    private long timestamp;
    public override long TimestampFrequency => 1000;
    public override long GetTimestamp() => timestamp;
    public void Advance(long milliseconds) => timestamp += milliseconds;
}

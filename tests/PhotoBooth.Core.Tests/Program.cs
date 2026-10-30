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
        for (var tick = 0; tick < 5; tick++) s.Tick();
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
            for (var t = 5; t >= 1; t--) { Check(s.SecondsRemaining == t && s.WantsLiveView); s.Tick(); }
            Check(s.Stage == SessionStage.Capturing && !s.WantsLiveView);
            s.RecordCapture(new CapturedPhoto(Guid.NewGuid(), $"capture-{i}"));
        }
        Check(s.Stage == SessionStage.Processing);
        if (mode == PhotoMode.Surprise) s.AssignStyle("test-style");
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
    for (var i = 0; i < 5; i++) s.Tick();
    Throws<InvalidOperationException>(s.Tick);
});
Test("four-shot session requires four distinct captures", () =>
{
    var s = new Session(new SessionOptions(4, PhotoMode.Original));
    var photo = new CapturedPhoto(Guid.NewGuid(), "original");
    for (var i = 0; i < 2; i++)
    {
        s.StartCountdown(); for (var t = 0; t < 5; t++) s.Tick();
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
Test("PIN has no short or nonnumeric default", () =>
{
    Throws<ArgumentException>(() => KioskExitGate.Create("1234"));
    Throws<ArgumentException>(() => KioskExitGate.Create("abcdef"));
});
Test("exit PIN rejects wrong code and enforces cooldown", () =>
{
    var clock = new ManualTime();
    var gate = KioskExitGate.Create("864209", clock); // Test fixture only; never an application default.
    Check(gate.TryAuthorizeExit("864209"));
    for (var i = 0; i < 5; i++) Check(!gate.TryAuthorizeExit("wrong"));
    Check(!gate.TryAuthorizeExit("864209"));
    clock.Advance(TimeSpan.FromSeconds(30));
    Check(!gate.TryAuthorizeExit("000000"));
    Check(gate.TryAuthorizeExit("864209"));
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

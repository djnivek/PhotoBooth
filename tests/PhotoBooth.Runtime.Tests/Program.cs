using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;

var root = Path.Combine(Path.GetTempPath(), "photobooth-runtime-" + Guid.NewGuid().ToString("N"));
var passed = 0;
async Task WaitFor(Func<bool> ready) { using var limit=new CancellationTokenSource(5000); while(!ready())await Task.Delay(10,limit.Token); }
void Check(bool ok, string description) { if (!ok) throw new Exception(description); passed++; }
try
{
    foreach (var count in new[] { 1, 4 })
    foreach (var mode in new[] { PhotoMode.Original, PhotoMode.Surprise })
    {
        var store = new SessionStore(root);
        var ticks = 0;
        await using var controller = new BoothController(store, () => new FixtureCamera(store), new RehearsalTransform(),
            (_, token) => { token.ThrowIfCancellationRequested(); ticks++; return Task.CompletedTask; });
        await controller.BeginAsync(new SessionOptions(count, mode));
        await controller.TakeAsync();
        Check(controller.Session!.Stage == SessionStage.Ready, $"{count}/{mode} reaches preview");
        Check(controller.FinalAssets.Count == count, "All poses persisted");
        Check(controller.Session.PrintStage == PrintStage.NotRequested, "No automatic printing");
        Check(ticks == count * 3 + count - 1, "Countdown per pose plus repositioning pauses");
        var sent = await controller.GetPrintBytesAsync();
        Check(SessionStore.Hash(sent) == controller.Sheet!.Sha256, "Print is exact reviewed master");
        var finalBytes = new List<byte[]>();
        foreach (var asset in controller.FinalAssets) finalBytes.Add(await store.ReadVerifiedAsync(asset));
        var expectedSheet = mode == PhotoMode.Surprise ? MetamorphosisSheet.Compose(finalBytes, controller.Session.StyleId) : PhotoProcessor.Compose(finalBytes);
        Check(sent.SequenceEqual(expectedSheet), "Only Metamorphose receives the themed square composition");
        Check(File.Exists(Path.Combine(root, controller.Session.Id.ToString("N"), "session.json")), "Durable session manifest");
    }
    {
        var store = new SessionStore(root);
        var shutter = new SpyCamera(store);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var controller = new BoothController(store, () => shutter, new RehearsalTransform(),
            async (_, token) => await wait.Task.WaitAsync(token));
        await controller.BeginAsync(new SessionOptions(4, PhotoMode.Original));
        var taking = controller.TakeAsync();
        await controller.TakeAsync();
        await controller.CancelAsync();
        await taking;
        Check(controller.Session!.Stage == SessionStage.Cancelled, "Cancel remains terminal");
        Check(shutter.Captures == 0, "Cancel during countdown prevents shutter");
        Check(!controller.IsTaking, "Controls recover after cancellation");
    }
    foreach (var failsAfterCancel in new[] { false, true })
    {
        var store=new SessionStore(root);var first=new DelayedConnectionCamera(failsAfterCancel);var factories=0;
        await using var controller=new BoothController(store,()=>++factories==1?first:new FixtureCamera(store),new RehearsalTransform());
        var connecting=controller.BeginAsync(new(1,PhotoMode.Original));
        await first.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var cancelled=controller.Session!;await controller.CancelAsync();
        var rejected=false;
        try {await controller.BeginAsync(new(1,PhotoMode.Original));}catch(InvalidOperationException){rejected=true;}
        first.Resume.SetResult();await connecting;
        Check(rejected,"A new session cannot overlap an unresolved camera connection");
        Check(cancelled.Stage==SessionStage.Cancelled,"Late connection completion cannot revive a cancelled session");
        Check(first.Disposed,"Cancelled connection is closed after it settles");
        Check(!first.DisposedWhileConnecting,"Camera is never disposed while its connection is in flight");
        Check(!controller.IsBusy,"Cancelled connection clears busy state even after a driver error");
        await controller.BeginAsync(new(1,PhotoMode.Original));
        Check(controller.Session!.Id!=cancelled.Id&&controller.Session.Stage==SessionStage.Positioning,"Next guest starts after connection cleanup");
        Check(factories==2,"No camera was opened for the rejected overlapping session");
    }
    {
        var store = new SessionStore(root); var controlled = new PreparationCamera(store);
        await using var controller = new BoothController(store, () => controlled, new RehearsalTransform(), (_, _) => Task.CompletedTask);
        await controller.BeginAsync(new(4, PhotoMode.Original)); var run = controller.TakeAsync();
        await WaitFor(() => controlled.LiveStarts == 1);
        Check(controller.Session!.Stage == SessionStage.Positioning && controlled.Shots == 0, "No countdown before the first live frame");
        controlled.Frames[0].SetResult(); await controlled.FocusEntered.Task;
        Check(controller.Session.Stage == SessionStage.Positioning && controlled.Shots == 0, "Focus preparation completes before countdown and shutter");
        controlled.FocusReady.SetResult();
        for (var next = 1; next < 4; next++)
        {
            await WaitFor(() => controlled.LiveStarts == next + 1);
            Check(controlled.Shots == next && controller.Session.Stage == SessionStage.Positioning, "Each next pose waits for a newly started preview");
            controlled.Frames[next].SetResult();
        }
        await run;
        Check(controller.Session.Stage == SessionStage.Ready && controlled.Shots == 4 && controlled.Preparations == 4, "All four poses require their own camera preparation");
    }
    {
        var store = new SessionStore(root); var controlled = new PreparationCamera(store); controlled.Frames[0].SetResult();
        await using var controller = new BoothController(store, () => controlled, new RehearsalTransform(), (_, _) => Task.CompletedTask);
        await controller.BeginAsync(new(1, PhotoMode.Original)); var run = controller.TakeAsync(); await controlled.FocusEntered.Task;
        await controller.CancelAsync(); controlled.FocusReady.TrySetResult(); await run;
        Check(controlled.Shots == 0 && controller.Session!.Stage == SessionStage.Cancelled, "Cancellation during autofocus never fires the shutter");
    }
    foreach(var (failStop,failPrepare) in new[]{(false,false),(true,false),(true,true)})
    {
        var store=new SessionStore(root);var faulty=new CleanupFailureCamera(store,failStop,failPrepare);var created=0;
        await using var controller=new BoothController(store,()=>created++==0?faulty:new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
        await controller.BeginAsync(new(1,PhotoMode.Original));await controller.TakeAsync();
        Check(controller.Session!.Stage==SessionStage.Failed,"Cleanup failure cannot escape the failure handler");
        Check(controller.Message.Contains(failPrepare?"Primary focus failure":failStop?"visée":"Primary capture failure"),"Cleanup preserves the original guest-facing failure");
        Check(!failStop || faulty.Shots==0,"A failed preview stop prevents the controller from asking for a shutter");
        var path=Path.Combine(root,controller.Session.Id.ToString("N"),"session.json");var before=await File.ReadAllTextAsync(path);
        using(var manifest=System.Text.Json.JsonDocument.Parse(before))
            Check(manifest.RootElement.GetProperty("state").GetString()=="failed" && manifest.RootElement.GetProperty("cameraCleanupError").GetString()=="Camera close failure","Failure manifest retains the secondary cleanup diagnostic");
        await controller.BeginAsync(new(1,PhotoMode.Original));
        Check(controller.Session.Stage==SessionStage.Positioning && faulty.Disposals==1,"Next session starts after a failed close without disposing the old link twice");
        Check(await File.ReadAllTextAsync(path)==before,"Starting again never rewrites the prior failure manifest");
    }
    {
        var gate = KioskExitGate.Create("E629");
        for (var i = 0; i < 5; i++) gate.TryAuthorizeExit("0000");
        var restored = KioskExitGate.Restore(gate.Export());
        Check(!restored.TryAuthorizeExit("E629"), "PIN cooldown survives restart");
        var valid = KioskExitGate.Restore(KioskExitGate.Create("E629").Export());
        Check(valid.TryAuthorizeExit("E629"), "Salted credential survives restart");
    }
}
finally { Directory.Delete(root, recursive: true); }
Console.WriteLine($"PASS {passed} runtime checks; full rehearsal flow, no paid API.");

sealed class SpyCamera(SessionStore store) : IPhotoCamera
{
    private readonly FixtureCamera inner = new(store);
    public int Captures { get; private set; }
    public Task<CameraStatus> ConnectAsync(CancellationToken token) => inner.ConnectAsync(token);
    public IAsyncEnumerable<LiveViewFrame> LiveViewAsync(CancellationToken token) => inner.LiveViewAsync(token);
    public Task<CapturedPhoto> CaptureAsync(Guid session, int shot, CancellationToken token)
    { Captures++; return inner.CaptureAsync(session, shot, token); }
    public ValueTask DisposeAsync() => inner.DisposeAsync();
}

sealed class DelayedConnectionCamera(bool failsAfterCancel):IPhotoCamera
{
    public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Resume {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Disposed {get;private set;}
    private bool connecting;
    public bool DisposedWhileConnecting {get;private set;}
    public async Task<CameraStatus> ConnectAsync(CancellationToken token)
    {
        connecting=true;Entered.SetResult();
        try {await Resume.Task;}
        finally {connecting=false;}
        if(failsAfterCancel)throw new System.Runtime.InteropServices.COMException("Driver finished with an error after cancel");
        return new(true,"Delayed USB",true,true);
    }
    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken token)
    {await Task.Delay(Timeout.Infinite,token);yield break;}
    public Task<CapturedPhoto> CaptureAsync(Guid session,int shot,CancellationToken token)=>throw new InvalidOperationException("Unexpected shutter");
    public ValueTask DisposeAsync(){DisposedWhileConnecting|=connecting;Disposed=true;return ValueTask.CompletedTask;}
}

sealed class CleanupFailureCamera(SessionStore store,bool failStop,bool failPrepare):IPhotoCamera
{
    private readonly FixtureCamera inner=new(store);
    public int Shots,Disposals;
    public Task<CameraStatus> ConnectAsync(CancellationToken token)=>inner.ConnectAsync(token);
    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken token)
    {
        try {await foreach(var frame in inner.LiveViewAsync(token))yield return frame;}
        finally {if(failStop)throw new IOException("Preview stop failure");}
    }
    public Task<CapturedPhoto> CaptureAsync(Guid id,int shot,CancellationToken token)
    {Shots++;throw new IOException("Primary capture failure");}
    public Task PrepareCaptureAsync(CancellationToken token)=>failPrepare?Task.FromException(new IOException("Primary focus failure")):Task.CompletedTask;
    public ValueTask DisposeAsync(){Disposals++;return ValueTask.FromException(new IOException("Camera close failure"));}
}

sealed class PreparationCamera(SessionStore store) : IPhotoCamera
{
    private readonly FixtureCamera inner = new(store);
    public TaskCompletionSource[] Frames = Enumerable.Range(0,4).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
    public TaskCompletionSource FocusEntered = new(TaskCreationOptions.RunContinuationsAsynchronously), FocusReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int LiveStarts, Shots, Preparations;
    public Task<CameraStatus> ConnectAsync(CancellationToken token) => inner.ConnectAsync(token);
    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken token)
    {
        var index = Interlocked.Increment(ref LiveStarts)-1; await Frames[index].Task.WaitAsync(token);
        yield return new(FixtureCamera.CreateFixture(index),DateTimeOffset.UtcNow); await Task.Delay(Timeout.Infinite,token);
    }
    public async Task PrepareCaptureAsync(CancellationToken token)
    {Preparations++;FocusEntered.TrySetResult();await FocusReady.Task.WaitAsync(token);}
    public Task<CapturedPhoto> CaptureAsync(Guid session,int shot,CancellationToken token)
    {Shots++;return inner.CaptureAsync(session,shot,token);}
    public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
}

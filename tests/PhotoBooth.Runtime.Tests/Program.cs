using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;

var root = Path.Combine(Path.GetTempPath(), "photobooth-runtime-" + Guid.NewGuid().ToString("N"));
var passed = 0;
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
        Check(ticks == count * 5 + count - 1, "Countdown per pose plus repositioning pauses");
        var sent = await controller.GetPrintBytesAsync();
        Check(SessionStore.Hash(sent) == controller.Sheet!.Sha256, "Print is exact reviewed master");
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
    {
        var store=new SessionStore(root);var first=new DelayedConnectionCamera();var factories=0;
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
        await controller.BeginAsync(new(1,PhotoMode.Original));
        Check(controller.Session!.Id!=cancelled.Id&&controller.Session.Stage==SessionStage.Positioning,"Next guest starts after connection cleanup");
        Check(factories==2,"No camera was opened for the rejected overlapping session");
    }
    {
        var gate = KioskExitGate.Create("381629");
        for (var i = 0; i < 5; i++) gate.TryAuthorizeExit("000000");
        var restored = KioskExitGate.Restore(gate.Export());
        Check(!restored.TryAuthorizeExit("381629"), "PIN cooldown survives restart");
        var valid = KioskExitGate.Restore(KioskExitGate.Create("381629").Export());
        Check(valid.TryAuthorizeExit("381629"), "Salted credential survives restart");
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

sealed class DelayedConnectionCamera:IPhotoCamera
{
    public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Resume {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Disposed {get;private set;}
    public async Task<CameraStatus> ConnectAsync(CancellationToken token)
    {Entered.SetResult();await Resume.Task;return new(true,"Delayed USB",true,true);}
    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken token)
    {await Task.Delay(Timeout.Infinite,token);yield break;}
    public Task<CapturedPhoto> CaptureAsync(Guid session,int shot,CancellationToken token)=>throw new InvalidOperationException("Unexpected shutter");
    public ValueTask DisposeAsync(){Disposed=true;return ValueTask.CompletedTask;}
}

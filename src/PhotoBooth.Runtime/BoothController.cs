using PhotoBooth.Core;
using PhotoBooth.Imaging;
using SkiaSharp;

namespace PhotoBooth.Runtime;

public sealed record TransformResult(string Style, IReadOnlyList<byte[]> Photos, bool Normalized = false, IReadOnlyList<string>? ShotStyles = null);
public interface IPhotoTransform
{
    Task<TransformResult> TransformAsync(Guid sessionId, IReadOnlyList<byte[]> originals,
        IProgress<string> progress, CancellationToken token, string styleChoice = StyleCatalog.Chef, IReadOnlyList<string>? styleChoices = null);
}

public sealed class RehearsalTransform : IPhotoTransform
{
    public Task<TransformResult> TransformAsync(Guid sessionId, IReadOnlyList<byte[]> originals,
        IProgress<string> progress, CancellationToken token, string styleChoice = StyleCatalog.Chef, IReadOnlyList<string>? styleChoices = null)
    {
        var photos = originals.Select(bytes =>
        {
            token.ThrowIfCancellationRequested();
            using var bitmap = PhotoProcessor.DecodeUpright(bytes);
            using var canvas = new SKCanvas(bitmap);
            using var paint = new SKPaint { Color = new SKColor(90, 38, 130, 45), BlendMode = SKBlendMode.SrcOver };
            canvas.DrawRect(0, 0, bitmap.Width, bitmap.Height, paint);
            return PhotoProcessor.Jpeg(bitmap);
        }).ToArray();
        var plan = StyleSelection.RehearsalPlan(styleChoices ?? [styleChoice], originals.Count);
        return Task.FromResult(new TransformResult(plan.Distinct().Count() == 1 ? plan[0] : StyleCatalog.Mixed, photos, ShotStyles: plan));
    }
}

public sealed class BoothController : IAsyncDisposable
{
    private readonly SessionStore store;
    private readonly BoothTelemetry? telemetry;
    private readonly Func<IPhotoCamera> cameraFactory;
    private readonly IPhotoTransform transform;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private IPhotoCamera? camera;
    private CancellationTokenSource? sessionCancellation;
    private CancellationTokenSource? liveCancellation;
    private Task? liveTask;
    private TaskCompletionSource? firstLiveFrame;
    private long lastLiveTicks;
    private Exception? liveFailure;
    private Exception? cameraCleanupFailure;
    private bool taking;
    private bool connecting;
    public Session? Session { get; private set; }
    public StoredAsset? Sheet { get; private set; }
    public IReadOnlyList<StoredAsset> FinalAssets { get; private set; } = [];
    public string Message { get; private set; } = "";
    public bool IsTaking => taking;
    public bool IsBusy => taking || connecting;
    public event Action? Changed;
    public event Action<byte[]>? Frame;
    public SessionStore Store => store;

    public BoothController(SessionStore store, Func<IPhotoCamera> cameraFactory, IPhotoTransform transform,
        Func<TimeSpan, CancellationToken, Task>? delay = null, BoothTelemetry? telemetry = null)
    {
        this.store = store; this.cameraFactory = cameraFactory; this.transform = transform; this.telemetry = telemetry;
        this.delay = delay ?? ((duration, token) => Task.Delay(duration, token));
    }

    public async Task BeginAsync(SessionOptions options)
    {
        if (IsBusy) throw new InvalidOperationException("Une séance est en cours.");
        connecting = true;
        try
        {
            await CloseCameraAsync();
            sessionCancellation?.Dispose(); sessionCancellation = new();
            Session = new Session(options); Sheet = null; FinalAssets = [];
            cameraCleanupFailure = null;
            await ObserveAsync("session_started");
            Message = "Connexion à la caméra…"; Notify();
            camera = cameraFactory();
            var status = await camera.ConnectAsync(sessionCancellation.Token);
            // Some USB drivers finish an in-flight connection even after cancellation.
            // Do not let that late result start preview or replace the next guest's camera.
            sessionCancellation.Token.ThrowIfCancellationRequested();
            if (!status.Connected || !status.CanCapture || !status.CanLiveView)
                throw new InvalidOperationException("La caméra ne propose pas la prise de vue et le retour vidéo.");
            Message = "Prenez place dans le cadre."; StartLiveView();
        }
        catch (OperationCanceledException) when (sessionCancellation?.IsCancellationRequested == true)
        { await CloseCameraAsync(); Message = "Séance annulée."; }
        catch (Exception exception) { await FailAsync(exception); }
        finally { connecting = false; Notify(); }
    }

    public async Task TakeAsync()
    {
        if (IsBusy || Session is not { Stage: SessionStage.Positioning } || camera is null) return;
        taking = true;
        var token = sessionCancellation!.Token;
        try
        {
            while (Session.Captures.Count < Session.Options.PhotoCount)
            {
                Message = "L’appareil prépare la photo…"; Notify();
                await firstLiveFrame!.Task.WaitAsync(TimeSpan.FromSeconds(10), token);
                await camera.PrepareCaptureAsync(token);
                token.ThrowIfCancellationRequested();
                EnsureLiveViewFresh();
                Session.StartCountdown(); Message = "Gardez le sourire !"; Notify();
                while (Session.Stage == SessionStage.Countdown)
                {
                    await delay(TimeSpan.FromSeconds(1), token);
                    token.ThrowIfCancellationRequested();
                    Session.Tick(); Notify();
                }
                EnsureLiveViewFresh();
                // Release the preview stream before issuing the shutter command.
                await StopLiveViewAsync();
                if (liveFailure is not null)
                    throw new IOException("L’arrêt de la visée a échoué. Vérifiez le Canon avant de recommencer.", liveFailure);
                Message = "Gardez la pose…"; Notify();
                var capture = await camera.CaptureAsync(Session.Id, Session.Captures.Count, token);
                token.ThrowIfCancellationRequested();
                Session.RecordCapture(capture);
                await SaveManifestAsync("captured", token);
                await ObserveAsync("capture_saved", Session.Captures.Count-1);
                if (Session.Stage == SessionStage.Positioning)
                {
                    Message = "Une autre pose ?"; StartLiveView(); Notify();
                    await delay(TimeSpan.FromMilliseconds(1500), token);
                }
            }
            await CloseCameraAsync();
            Message = Session.Options.Mode == PhotoMode.Surprise ? "Vos photos changent de décor…" : "On prépare vos souvenirs…";
            Notify();
            var originals = new List<byte[]>();
            foreach (var shot in Session.Captures)
                originals.Add(await File.ReadAllBytesAsync(store.Resolve(shot.LocalAssetKey), token));
            List<StoredAsset> final = [];
            if (Session.Options.Mode == PhotoMode.Surprise)
            {
                var result = await transform.TransformAsync(Session.Id, originals,
                    new Progress<string>(message => { if (Session.Stage == SessionStage.Processing) { Message = message; Notify(); } }), token, Session.Options.StyleChoice, Session.Options.StyleChoices);
                if (result.Photos.Count != originals.Count) throw new InvalidDataException("Il manque une photo transformée.");
                Session.AssignStyles(result.ShotStyles ?? Enumerable.Repeat(result.Style, originals.Count).ToArray());
                foreach (var bytes in result.Photos)
                    final.Add(await store.SavePhotoAsync(Session.Id, result.Normalized ? bytes : await Task.Run(() => PhotoProcessor.Normalize(bytes), token), token));
            }
            else
                final.AddRange(Session.Captures.Select((photo, i) => new StoredAsset(photo.LocalAssetKey,
                    SessionStore.Hash(originals[i]), originals[i].LongLength)));
            var finalBytes = new List<byte[]>();
            foreach (var asset in final) finalBytes.Add(await store.ReadVerifiedAsync(asset, token));
            Sheet = await store.SavePhotoAsync(Session.Id, await Task.Run(() =>
                Session.Options.Mode == PhotoMode.Surprise
                    ? MetamorphosisSheet.Compose(finalBytes, Session.StyleId)
                    : PhotoProcessor.Compose(finalBytes), token), token);
            FinalAssets = final;
            Session.CompleteProcessing(Session.Captures.Select((shot, i) => new FinalPhoto(shot.Id, final[i].Key)), Sheet.Key);
            Message = "Vos souvenirs sont prêts.";
            await SaveManifestAsync("ready", token);
            await ObserveAsync("session_ready");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        { Message = "Séance annulée. Les photos déjà prises sont conservées."; }
        catch (Exception exception) { await FailAsync(exception); }
        finally { taking = false; Notify(); }
    }

    public Task<byte[]> GetPrintBytesAsync(CancellationToken token = default)
    {
        var session=Session;var sheet=Sheet;
        if (session?.Stage != SessionStage.Ready || sheet is null) throw new InvalidOperationException("La planche n’est pas prête.");
        return ReadPrintBytesAsync(session, sheet, token);
    }

    private async Task<byte[]> ReadPrintBytesAsync(Session session, StoredAsset sheet, CancellationToken token)
    {
        if (session.Stage != SessionStage.Ready || session.SheetAssetKey != sheet.Key)
            throw new InvalidOperationException("La planche ne correspond pas à la séance prête.");
        var bytes = await store.ReadVerifiedAsync(sheet, token);
        using var raster = PhotoProcessor.DecodeUpright(bytes);
        if (raster.Width != 1800 || raster.Height != 1200) throw new InvalidDataException("Format d’impression incorrect.");
        return bytes;
    }

    public async Task<PrintSubmission> PrintAsync(IPhotoPrinter printer, CancellationToken token = default)
    {
        // A guest can start the next session while Windows is accepting this spool job.
        // Keep every verification and journal write tied to the original reviewed sheet.
        var session = Session ?? throw new InvalidOperationException("La séance n’est pas prête.");
        var sheet = Sheet ?? throw new InvalidOperationException("La planche n’est pas prête.");
        await ReadPrintBytesAsync(session, sheet, token);
        var journalPath = Path.Combine(store.Root, session.Id.ToString("N"), "print.json");
        if (File.Exists(journalPath))
        {
            var previous = System.Text.Json.JsonSerializer.Deserialize<PrintJournal>(await File.ReadAllBytesAsync(journalPath, token));
            if (previous is not {State:"not_submitted"} || previous.SheetKey != sheet.Key || previous.Sha256 != sheet.Sha256)
                throw new InvalidOperationException("Une impression a déjà été demandée. Vérifiez la file Windows avant toute nouvelle tentative.");
        }
        var request = session.RequestPrint(); // guards concurrent clicks before the first await
        await store.WritePrintJournalAsync(session.Id, new(request.Id, sheet.Key, sheet.Sha256, "requested", DateTimeOffset.UtcNow), token);
        try
        {
            var submission = await printer.SubmitAsync(request, token);
            await store.WritePrintJournalAsync(session.Id, new(request.Id, sheet.Key, sheet.Sha256, "submitted", DateTimeOffset.UtcNow, submission.SpoolJobId));
            session.MarkPrintSubmitted(request.Id, submission.SpoolJobId); Notify();
            return submission;
        }
        catch (PrintNotSubmittedException)
        {
            // Persist proof of no submission before permitting a new explicit click.
            await store.WritePrintJournalAsync(session.Id, new(request.Id, sheet.Key, sheet.Sha256, "not_submitted", DateTimeOffset.UtcNow));
            session.MarkPrintNotSubmitted(request.Id); Notify(); throw;
        }
        catch
        {
            session.MarkPrintUncertain(request.Id);
            await store.WritePrintJournalAsync(session.Id, new(request.Id, sheet.Key, sheet.Sha256, "needs_operator", DateTimeOffset.UtcNow));
            Notify(); throw;
        }
    }

    public async Task UseOriginalsAsync()
    {
        if (IsBusy || Session is null) return;
        taking = true;
        try
        {
            Session.PrepareOriginalFallback(); Message = "On prépare vos originaux…"; Notify();
            var bytes = new List<byte[]>(); var final = new List<StoredAsset>();
            foreach (var shot in Session.Captures)
            {
                var image = await File.ReadAllBytesAsync(store.Resolve(shot.LocalAssetKey)); bytes.Add(image);
                final.Add(new(shot.LocalAssetKey, SessionStore.Hash(image), image.Length));
            }
            Sheet = await store.SavePhotoAsync(Session.Id, PhotoProcessor.Compose(bytes)); FinalAssets = final;
            Session.CompleteProcessing(Session.Captures.Select(c => new FinalPhoto(c.Id,c.LocalAssetKey)),Sheet.Key);
            Message = "Vos originaux sont prêts."; await SaveManifestAsync("ready_original_fallback",CancellationToken.None);
            await ObserveAsync("original_fallback");
        }
        catch (Exception exception) { await FailAsync(exception); }
        finally { taking = false; Notify(); }
    }

    public async Task CancelAsync()
    {
        sessionCancellation?.Cancel();
        if (Session is { Stage: not (SessionStage.Ready or SessionStage.Failed or SessionStage.Cancelled) }) { Session.Cancel(); await ObserveAsync("session_cancelled"); }
        await StopLiveViewAsync();
        Message = "Séance annulée."; Notify();
    }

    private void StartLiveView()
    {
        firstLiveFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = firstLiveFrame; liveFailure = null; Interlocked.Exchange(ref lastLiveTicks, 0);
        liveCancellation = CancellationTokenSource.CreateLinkedTokenSource(sessionCancellation!.Token);
        var token = liveCancellation.Token;
        var currentCamera = camera!;
        liveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var frame in currentCamera.LiveViewAsync(token))
                    if (Session?.WantsLiveView == true)
                    {
                        if (frame.Jpeg.IsEmpty) continue;
                        Interlocked.Exchange(ref lastLiveTicks, DateTimeOffset.UtcNow.UtcTicks);
                        ready.TrySetResult(); Frame?.Invoke(frame.Jpeg.ToArray());
                    }
                if (!token.IsCancellationRequested) throw new IOException("Le retour vidéo s’est arrêté.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception) { liveFailure = exception; ready.TrySetException(exception); Message = "Retour vidéo interrompu. Vérifiez la caméra avant de continuer."; Notify(); }
        }, token);
    }

    private void EnsureLiveViewFresh()
    {
        var age = DateTimeOffset.UtcNow.UtcTicks - Interlocked.Read(ref lastLiveTicks);
        if (liveFailure is not null || age > TimeSpan.FromSeconds(3).Ticks)
            throw new IOException("Le retour vidéo n’est plus disponible. Vérifiez la caméra avant de recommencer.", liveFailure);
    }
    private async Task StopLiveViewAsync()
    {
        liveCancellation?.Cancel();
        if (liveTask is not null) { try { await liveTask; } catch (OperationCanceledException) { } }
        liveTask = null; liveCancellation?.Dispose(); liveCancellation = null;
    }
    private async Task CloseCameraAsync()
    {
        await StopLiveViewAsync();
        var closing = camera; camera = null;
        if (closing is null) return;
        try { await closing.DisposeAsync(); }
        catch (Exception exception) { cameraCleanupFailure = exception; }
    }
    private async Task FailAsync(Exception exception)
    {
        if (Session is { Stage: not (SessionStage.Ready or SessionStage.Failed or SessionStage.Cancelled) }) Session.Fail(exception.GetType().Name);
        await CloseCameraAsync(); Message = exception.Message;
        await SaveManifestAsync("failed", CancellationToken.None); await ObserveAsync("session_failed"); Notify();
    }
    private Task ObserveAsync(string kind,int shot=-1) => telemetry is null || Session is null ? Task.CompletedTask :
        telemetry.RecordAsync(telemetry.Metric(Session.Id,Session.Options,kind,shot,style:Session.StyleId));
    private Task SaveManifestAsync(string state, CancellationToken token) => Session is null ? Task.CompletedTask :
        store.WriteManifestAsync(Session.Id, new { schema = 1, state, Session.Id, Session.Options,
            Session.StyleId, Session.ShotStyles, Session.UsedOriginalFallback, captures = Session.Captures, final = FinalAssets, sheet = Sheet,
            cameraCleanupError = cameraCleanupFailure?.Message, updatedAt = DateTimeOffset.UtcNow }, token);
    private void Notify() => Changed?.Invoke();
    public async ValueTask DisposeAsync() { await CancelAsync(); await CloseCameraAsync(); sessionCancellation?.Dispose(); if (transform is IDisposable disposable) disposable.Dispose(); }
}

using System.Collections.ObjectModel;

namespace PhotoBooth.Core;

public enum PhotoMode { Original, Surprise }
public enum SessionStage { Positioning, Countdown, Capturing, Processing, Ready, Failed, Cancelled }
public enum PrintStage { NotRequested, Requested, Submitted, NeedsOperator }

public sealed record SessionOptions
{
    public int PhotoCount { get; }
    public PhotoMode Mode { get; }
    public int CountdownSeconds { get; }

    public SessionOptions(int photoCount, PhotoMode mode, int countdownSeconds = 5)
    {
        if (photoCount is not (1 or 4)) throw new ArgumentOutOfRangeException(nameof(photoCount));
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (countdownSeconds is < 3 or > 10) throw new ArgumentOutOfRangeException(nameof(countdownSeconds));
        PhotoCount = photoCount;
        Mode = mode;
        CountdownSeconds = countdownSeconds;
    }
}

// Opaque references, never visitor-controlled file paths or download URLs.
public sealed record CapturedPhoto(Guid Id, string LocalAssetKey);
public sealed record FinalPhoto(Guid CaptureId, string AssetKey);
public sealed record PrintRequest(Guid Id, Guid SessionId, string SheetAssetKey);

/// <summary>
/// Portable domain state machine. The host must serialize calls and persist transitions.
/// This class does not perform I/O, call OpenAI, or claim that a spool job was printed.
/// </summary>
public sealed class Session
{
    private readonly List<CapturedPhoto> captures = [];
    private readonly List<FinalPhoto> results = [];
    public Guid Id { get; } = Guid.NewGuid();
    public SessionOptions Options { get; }
    public SessionStage Stage { get; private set; } = SessionStage.Positioning;
    public PrintStage PrintStage { get; private set; }
    public int SecondsRemaining { get; private set; }
    public string? StyleId { get; private set; }
    public string? SheetAssetKey { get; private set; }
    public string? FailureCode { get; private set; }
    public PrintRequest? PrintRequest { get; private set; }
    public string? SpoolJobId { get; private set; }
    public ReadOnlyCollection<CapturedPhoto> Captures => captures.AsReadOnly();
    public ReadOnlyCollection<FinalPhoto> Results => results.AsReadOnly();

    // Keep the camera preview running while the countdown is displayed.
    public bool WantsLiveView => Stage is SessionStage.Positioning or SessionStage.Countdown;

    public Session(SessionOptions options) => Options = options ?? throw new ArgumentNullException(nameof(options));

    public void StartCountdown()
    {
        Require(SessionStage.Positioning);
        SecondsRemaining = Options.CountdownSeconds;
        Stage = SessionStage.Countdown;
    }

    // The host supplies one tick per elapsed second, not one tick per UI render.
    public void Tick()
    {
        Require(SessionStage.Countdown);
        if (--SecondsRemaining == 0) Stage = SessionStage.Capturing;
    }

    public void RecordCapture(CapturedPhoto photo)
    {
        Require(SessionStage.Capturing);
        ArgumentNullException.ThrowIfNull(photo);
        if (photo.Id == Guid.Empty || string.IsNullOrWhiteSpace(photo.LocalAssetKey))
            throw new ArgumentException("Capture must have an ID and a durable local asset reference.", nameof(photo));
        if (captures.Any(x => x.Id == photo.Id || x.LocalAssetKey == photo.LocalAssetKey))
            throw new ArgumentException("A capture cannot be reused for another shot.", nameof(photo));
        captures.Add(photo);
        Stage = captures.Count == Options.PhotoCount ? SessionStage.Processing : SessionStage.Positioning;
    }

    // The server picks one allowed style per session, then persists it before processing.
    public void AssignStyle(string styleId)
    {
        Require(SessionStage.Processing);
        if (Options.Mode != PhotoMode.Surprise) throw new InvalidOperationException("Original sessions have no style.");
        ArgumentException.ThrowIfNullOrWhiteSpace(styleId);
        if (StyleId is not null && StyleId != styleId) throw new InvalidOperationException("A retry must retain the session style.");
        StyleId = styleId;
    }

    public void CompleteProcessing(IEnumerable<FinalPhoto> photos, string sheetAssetKey)
    {
        Require(SessionStage.Processing);
        ArgumentNullException.ThrowIfNull(photos);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetAssetKey);
        var incoming = photos.ToArray();
        if (incoming.Length != Options.PhotoCount || incoming.Any(x => x is null || string.IsNullOrWhiteSpace(x.AssetKey)))
            throw new ArgumentException("All shots must have a result before review.", nameof(photos));
        if (incoming.Select(x => x.CaptureId).Distinct().Count() != captures.Count ||
            incoming.Any(x => !captures.Any(c => c.Id == x.CaptureId)))
            throw new ArgumentException("Each capture must have exactly one corresponding result.", nameof(photos));
        if (incoming.Select(x => x.AssetKey).Distinct().Count() != incoming.Length)
            throw new ArgumentException("Each result must reference a distinct image.", nameof(photos));
        if (Options.Mode == PhotoMode.Surprise && StyleId is null)
            throw new InvalidOperationException("Select and persist a style before completing the AI session.");
        if (Options.Mode == PhotoMode.Original && incoming.Any(x => captures.Single(c => c.Id == x.CaptureId).LocalAssetKey != x.AssetKey))
            throw new ArgumentException("Original mode must preserve the captured images.", nameof(photos));
        if (Options.Mode == PhotoMode.Surprise && incoming.Any(x => captures.Any(c => c.LocalAssetKey == x.AssetKey)))
            throw new ArgumentException("Unmodified originals cannot be reported as generated results.", nameof(photos));
        results.AddRange(captures.Select(c => incoming.Single(x => x.CaptureId == c.Id)));
        SheetAssetKey = sheetAssetKey;
        Stage = SessionStage.Ready;
    }

    // Called only for an explicit visitor action. The host must journal before spooling.
    public PrintRequest RequestPrint()
    {
        Require(SessionStage.Ready);
        if (PrintStage != PrintStage.NotRequested) throw new InvalidOperationException("Print already requested; operator reconciliation is required.");
        PrintRequest = new PrintRequest(Guid.NewGuid(), Id, SheetAssetKey!);
        PrintStage = PrintStage.Requested;
        return PrintRequest;
    }

    public void MarkPrintSubmitted(Guid requestId, string spoolJobId)
    {
        RequirePrintRequest(requestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spoolJobId);
        SpoolJobId = spoolJobId;
        PrintStage = PrintStage.Submitted;
    }

    public void MarkPrintUncertain(Guid requestId)
    {
        RequirePrintRequest(requestId);
        PrintStage = PrintStage.NeedsOperator;
    }

    public void Fail(string code)
    {
        if (Stage is SessionStage.Ready or SessionStage.Cancelled or SessionStage.Failed)
            throw new InvalidOperationException("Session is already terminal.");
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        FailureCode = code;
        SecondsRemaining = 0;
        Stage = SessionStage.Failed;
    }

    public void Cancel()
    {
        if (Stage is SessionStage.Ready or SessionStage.Cancelled or SessionStage.Failed)
            throw new InvalidOperationException("Session is already terminal.");
        SecondsRemaining = 0;
        Stage = SessionStage.Cancelled;
    }

    private void Require(SessionStage expected)
    {
        if (Stage != expected) throw new InvalidOperationException($"Expected {expected}, got {Stage}.");
    }

    private void RequirePrintRequest(Guid id)
    {
        if (PrintStage != PrintStage.Requested || PrintRequest?.Id != id)
            throw new InvalidOperationException("No matching pending print request.");
    }
}

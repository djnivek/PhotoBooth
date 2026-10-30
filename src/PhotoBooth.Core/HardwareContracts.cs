namespace PhotoBooth.Core;

public sealed record LiveViewFrame(ReadOnlyMemory<byte> Jpeg, DateTimeOffset CapturedAt);
public sealed record CameraStatus(bool Connected, string Model, bool CanLiveView, bool CanCapture);

/// <summary>
/// Windows camera boundary (WPD/PTP or licensed EDSDK). Serialize camera operations.
/// Live view frames are ephemeral; CaptureAsync returns only after the full-resolution
/// JPEG has been atomically saved locally. A live-view frame is never a final photo.
/// </summary>
public interface IPhotoCamera : IAsyncDisposable
{
    Task<CameraStatus> ConnectAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<LiveViewFrame> LiveViewAsync(CancellationToken cancellationToken);
    Task<CapturedPhoto> CaptureAsync(Guid sessionId, int shotIndex, CancellationToken cancellationToken);
}

public sealed record PrintSubmission(string SpoolJobId);

/// <summary>
/// Windows adapter. Resolve a trusted asset reference, one page, named printer, 4x6 media.
/// A successful submission means accepted by the spooler, not a physical print.
/// Never automatically retry after an ambiguous timeout.
/// </summary>
public interface IPhotoPrinter
{
    Task<PrintSubmission> SubmitAsync(PrintRequest request, CancellationToken cancellationToken);
}

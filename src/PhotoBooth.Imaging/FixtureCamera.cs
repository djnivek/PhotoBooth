using System.Runtime.CompilerServices;
using PhotoBooth.Core;
using SkiaSharp;

namespace PhotoBooth.Imaging;

// Never contacts a network or opens a physical device. Imported files stay local.
public sealed class FixtureCamera(SessionStore store, IReadOnlyList<string>? files = null) : IPhotoCamera
{
    private int next;
    public Task<CameraStatus> ConnectAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new CameraStatus(true, "Images de répétition", true, true));

    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new LiveViewFrame(await GetImageAsync(next, cancellationToken), DateTimeOffset.UtcNow);
            await Task.Delay(250, cancellationToken);
        }
    }

    public async Task<CapturedPhoto> CaptureAsync(Guid sessionId, int shotIndex, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        var jpeg = await GetImageAsync(shotIndex, cancellationToken);
        var asset = await store.SavePhotoAsync(sessionId, jpeg, cancellationToken);
        next = shotIndex + 1;
        return new CapturedPhoto(Guid.NewGuid(), asset.Key);
    }

    private async Task<byte[]> GetImageAsync(int index, CancellationToken token)
    {
        if (files is { Count: > 0 })
            return PhotoProcessor.Normalize(await File.ReadAllBytesAsync(files[index % files.Count], token));
        return CreateFixture(index % 4);
    }

    public static byte[] CreateFixture(int index)
    {
        var colors = new[] { SKColor.Parse("#D6A6E8"), SKColor.Parse("#EEC66A"), SKColor.Parse("#97CBBB"), SKColor.Parse("#DB8D80") };
        using var bitmap = new SKBitmap(1200, 800);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(colors[index % 4]);
        using var paint = new SKPaint { Color = SKColor.Parse("#302342"), IsAntialias = true };
        // Calibration geometry, deliberately not a fabricated portrait or an AI output.
        canvas.DrawRect(40, 40, 80, 80, paint);
        canvas.DrawCircle(1100, 100, 40, paint);
        for (var i = 0; i <= index % 4; i++) canvas.DrawCircle(450 + i * 100, 400, 30, paint);
        canvas.DrawRect(40, 660, 200, 100, paint);
        return PhotoProcessor.Jpeg(bitmap);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PhotoBooth.Core;
using PhotoBooth.Runtime;
using QRCoder;

namespace PhotoBooth.App;

public sealed partial class MainWindow
{
    private readonly Dictionary<Guid, PublishedSession> galleries = [];
    private readonly Dictionary<Guid, Task<PublishedSession>> publications = [];
    private readonly HashSet<BoothController> detachedControllers = [];
    private readonly HashSet<BoothController> finishingControllers = [];

    private static void ShowQr(Image image, PublishedSession link)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(link.Url, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(data);
        (image.Source as Bitmap)?.Dispose();
        image.Source = Bitmap(code.GetGraphic(4));
        image.Width = image.Height = 220;
        image.IsVisible = true;
    }

    private Task<PublishedSession> PublishOnceAsync(BoothController source, BoothCloudClient client)
    {
        var id = source.Session!.Id;
        if (publications.TryGetValue(id, out var existing)) return existing;
        var task = client.PublishAsync(source.Session, source.FinalAssets, source.Sheet!, lifetime.Token);
        publications[id] = task;
        return task;
    }

    private Task StartNextSessionAsync()
    {
        var previous = controller;
        if (previous.IsBusy && (previous.Session?.Stage != SessionStage.Processing || !galleries.ContainsKey(previous.Session.Id)))
            return Task.CompletedTask;
        var previousCloud = cloud;
        detachedControllers.Add(previous);
        controller = CreateController();
        RenderHome();
        if (!previous.IsBusy) _ = CompleteDetachedAsync(previous, previousCloud);
        return Task.CompletedTask;
    }

    private async Task CompleteDetachedAsync(BoothController source, BoothCloudClient? client)
    {
        if (!finishingControllers.Add(source)) return;
        var id = source.Session?.Id;
        try
        {
            if (source.Session?.Stage == SessionStage.Ready && client is not null)
                await PublishOnceAsync(source, client);
        }
        catch (Exception exception)
        {
            // Retain a local diagnostic; never restart an ambiguous paid generation.
            if (id is not null && !lifetime.IsCancellationRequested)
                await File.WriteAllTextAsync(Path.Combine(store.Root, id.Value.ToString("N"), "sharing-error.txt"), exception.Message);
        }
        finally
        {
            detachedControllers.Remove(source);
            await source.DisposeAsync();
            client?.Dispose();
            if (id is not null) { publications.Remove(id.Value); galleries.Remove(id.Value); }
        }
    }
}

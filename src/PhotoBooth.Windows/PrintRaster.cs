using PhotoBooth.Imaging;
using SkiaSharp;

namespace PhotoBooth.Windows;

public sealed record PrinterGeometry(int PrintableWidth, int PrintableHeight, int DpiX, int DpiY);
public sealed record RasterPlacement(int X, int Y, int Width, int Height);

public static class PrintRaster
{
    public static RasterPlacement Fit(PrinterGeometry device)
    {
        if (device.DpiX < 72 || device.DpiY < 72 || device.PrintableWidth <= 0 || device.PrintableHeight <= 0)
            throw new InvalidDataException("Le pilote annonce une surface d’impression invalide.");
        // Preserve physical aspect for any non-square device DPI. DNP documents
        // 600x600 to applications, with 300x600 conversion inside its driver.
        var inchesWide = (double)device.PrintableWidth / device.DpiX;
        var inchesHigh = (double)device.PrintableHeight / device.DpiY;
        var scale = Math.Min(inchesWide / 6, inchesHigh / 4);
        var w = (int)Math.Round(6 * scale * device.DpiX);
        var h = (int)Math.Round(4 * scale * device.DpiY);
        return new((device.PrintableWidth - w) / 2, (device.PrintableHeight - h) / 2, w, h);
    }

    public static byte[] ToBgra(byte[] jpeg)
    {
        using var decoded = PhotoProcessor.DecodeUpright(jpeg);
        if (decoded.Width != 1800 || decoded.Height != 1200) throw new InvalidDataException("La planche doit mesurer 1800×1200.");
        using var bitmap = new SKBitmap(new SKImageInfo(1800, 1200, SKColorType.Bgra8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.White); canvas.DrawBitmap(decoded, 0, 0);
        return bitmap.Bytes;
    }
}

using PhotoBooth.Core;
using SkiaSharp;

namespace PhotoBooth.Imaging;

// Artwork is the existing, approved Colors Party gouache set. Composition is
// local and deterministic; printing never asks the image provider to draw a logo.
public static class MetamorphosisSheet
{
    public static byte[] Compose(IReadOnlyList<byte[]> photos, string? styleId)
    {
        var layout = PrintLayout.Metamorphosis(photos.Count);
        var mixed = styleId == StyleCatalog.Mixed;
        var unknown = styleId is not null && styleId != StyleCatalog.Chef && !StyleCatalog.BuiltIn.Styles.Any(s => s.Id == styleId);
        var style = StyleCatalog.BuiltIn.Find(mixed || unknown ? "colors_party" : styleId ?? StyleCatalog.Chef);
        using var bitmap = new SKBitmap(layout.Width, layout.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColor.Parse("#FFF8EC"));
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(style.Color) };
        canvas.DrawRect(1188, 0, 612, 1200, paint);

        for (var i = 0; i < photos.Count; i++)
        {
            var cell = layout.Photos[i];
            paint.Color = SKColors.White;
            canvas.DrawRect(cell.X - 8, cell.Y - 8, cell.Width + 16, cell.Height + 16, paint);
            using var photo = PhotoProcessor.DecodeUpright(photos[i]);
            using var image = SKImage.FromBitmap(photo);
            DrawContained(canvas, image, new SKRect(cell.X, cell.Y, cell.X + cell.Width, cell.Y + cell.Height));
        }

        using var logo = LoadImage("wordmark");
        DrawContained(canvas, logo, new SKRect(1240, 78, 1748, 424));
        using var fontStream = Resource("Display.ttf");
        using var typeface = SKTypeface.FromStream(fontStream) ?? throw new InvalidDataException("Police d’impression absente.");
        using var font = new SKFont(typeface, 39);
        paint.Color = SKColor.Parse("#512831");
        canvas.DrawText("by Estelle", 1494, 478, SKTextAlign.Center, font, paint);

        using var emblem = LoadImage(style.Asset);
        DrawContained(canvas, emblem, new SKRect(1256, 540, 1732, 966));
        font.Size = 38;
        // Built-in labels plus safe fallback only; a remote catalog cannot inject
        // asset paths or unbounded copy into the print master.
        var label = mixed ? "Plusieurs univers" : unknown ? "Votre métamorphose" : style.Id == StyleCatalog.Chef ? "La Surprise du Chef" : style.Label;
        while (font.MeasureText(label, paint) > 492 && font.Size > 28) font.Size -= 1;
        canvas.DrawText(label, 1494, 1036, SKTextAlign.Center, font, paint);
        font.Size = 29;
        canvas.DrawText("31 octobre 2026", 1494, 1100, SKTextAlign.Center, font, paint);
        return PhotoProcessor.Jpeg(bitmap);
    }

    private static Stream Resource(string name) => typeof(MetamorphosisSheet).Assembly
        .GetManifestResourceStream("PhotoBooth.Print." + name) ?? throw new InvalidDataException("Décor d’impression absent.");
    private static SKImage LoadImage(string name)
    {
        using var stream = Resource(name + ".png");
        using var data = SKData.Create(stream);
        return SKImage.FromEncodedData(data) ?? throw new InvalidDataException("Décor d’impression illisible.");
    }
    private static void DrawContained(SKCanvas canvas, SKImage image, SKRect box)
    {
        var scale = Math.Min(box.Width / image.Width, box.Height / image.Height);
        var w = image.Width * scale; var h = image.Height * scale;
        var x = box.Left + (box.Width - w) / 2; var y = box.Top + (box.Height - h) / 2;
        canvas.DrawImage(image, new SKRect(x, y, x + w, y + h), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
    }
}

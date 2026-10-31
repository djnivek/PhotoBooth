using PhotoBooth.Core;
using SkiaSharp;

namespace PhotoBooth.Imaging;

public static class PhotoProcessor
{
    public const int MaxEncodedBytes = 30 * 1024 * 1024;
    public const long MaxPixels = 32_000_000;

    // Decode before allocating the full raster; EXIF is applied once then stripped.
    public static SKBitmap DecodeUpright(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length is 0 or > MaxEncodedBytes) throw new InvalidDataException("Photo vide ou trop volumineuse.");
        using var data = SKData.CreateCopy(encoded);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("Image illisible.");
        var info = codec.Info;
        if (info.Width < 2 || info.Height < 2 || (long)info.Width * info.Height > MaxPixels)
            throw new InvalidDataException("Dimensions de photo non prises en charge.");
        using var raw = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        if (codec.GetPixels(raw.Info, raw.GetPixels()) != SKCodecResult.Success)
            throw new InvalidDataException("Photo incomplète ou corrompue.");
        return Orient(raw, codec.EncodedOrigin);
    }

    public static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        var swap = (int)origin >= 5;
        var result = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height,
            SKColorType.Rgba8888, SKAlphaType.Premul);
        var w = source.Width; var h = source.Height;
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity
        };
        using var canvas = new SKCanvas(result);
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    public static byte[] Normalize(ReadOnlySpan<byte> encoded, int longestSide = 2560, int jpegQuality = 96)
    {
        if (longestSide is < 600 or > 6000) throw new ArgumentOutOfRangeException(nameof(longestSide));
        if (jpegQuality is < 75 or > 100) throw new ArgumentOutOfRangeException(nameof(jpegQuality));
        using var upright = DecodeUpright(encoded);
        var ratio = Math.Min(1d, (double)longestSide / Math.Max(upright.Width, upright.Height));
        using var target = new SKBitmap(Math.Max(1, (int)Math.Round(upright.Width * ratio)),
            Math.Max(1, (int)Math.Round(upright.Height * ratio)));
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.White);
        using var image = SKImage.FromBitmap(upright);
        canvas.DrawImage(image, new SKRect(0, 0, target.Width, target.Height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        return Jpeg(target, jpegQuality);
    }

    // Separate working copy for generation. Preserve framing and every edge;
    // never upscale. The archived original keeps its existing resolution/bytes.
    public static byte[] PrepareGenerationInput(ReadOnlySpan<byte> encoded) => Normalize(encoded, 1536, 90);

    // Every input is one distinct final photograph, in capture order. No stretching.
    // Contain (rather than crop) protects faces at the edges of a group photograph.
    public static byte[] Compose(IReadOnlyList<byte[]> photos)
    {
        var layout = PrintLayout.FourBySix(photos.Count);
        using var bitmap = new SKBitmap(layout.Width, layout.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        for (var i = 0; i < photos.Count; i++)
        {
            using var photo = DecodeUpright(photos[i]);
            var box = layout.Photos[i];
            var scale = Math.Min((float)box.Width / photo.Width, (float)box.Height / photo.Height);
            var width = photo.Width * scale; var height = photo.Height * scale;
            var x = box.X + (box.Width - width) / 2;
            var y = box.Y + (box.Height - height) / 2;
            using var image = SKImage.FromBitmap(photo);
            canvas.DrawImage(image, new SKRect(x, y, x + width, y + height),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
        return Jpeg(bitmap);
    }

    public static byte[] Jpeg(SKBitmap bitmap, int quality = 96)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return WithDpi(encoded.ToArray(), 300);
    }

    // Set JFIF units/density. Physical size is also explicitly supplied to the driver.
    private static byte[] WithDpi(byte[] jpeg, ushort dpi)
    {
        for (var p = 2; p + 16 < jpeg.Length && jpeg[p] == 0xff;)
        {
            var marker = jpeg[p + 1];
            if (marker is 0xda or 0xd9) break;
            var length = jpeg[p + 2] * 256 + jpeg[p + 3];
            if (length < 2 || p + length + 2 > jpeg.Length) break;
            if (marker == 0xe0 && jpeg.AsSpan(p + 4, 5).SequenceEqual("JFIF\0"u8))
            {
                jpeg[p + 11] = 1;
                jpeg[p + 12] = jpeg[p + 14] = (byte)(dpi >> 8);
                jpeg[p + 13] = jpeg[p + 15] = (byte)dpi;
                return jpeg;
            }
            p += length + 2;
        }
        throw new InvalidDataException("JPEG encoder did not emit the expected JFIF header.");
    }
}

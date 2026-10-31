using PhotoBooth.Core;
using PhotoBooth.Imaging;
using SkiaSharp;

var assertions = 0;
void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
void Reject(Action action) { try { action(); } catch (ArgumentException) { assertions++; return; } catch (InvalidDataException) { assertions++; return; } throw new Exception("Expected rejection."); }

// Eight EXIF orientations, non-square input with unique pixel IDs. Also exercise
// JPEG metadata parsing, not merely the transform helper called by implementation.
using var source = new SKBitmap(3, 2);
var palette = new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Yellow, SKColors.Magenta, SKColors.Cyan };
for (var i = 0; i < 6; i++) source.SetPixel(i % 3, i / 3, palette[i]);
int[][] expected = [[0,1,2,3,4,5],[2,1,0,5,4,3],[5,4,3,2,1,0],[3,4,5,0,1,2],
    [0,3,1,4,2,5],[3,0,4,1,5,2],[5,2,4,1,3,0],[2,5,1,4,0,3]];
for (var orientation = 1; orientation <= 8; orientation++)
{
    using var actual = PhotoProcessor.Orient(source, (SKEncodedOrigin)orientation);
    Check(actual.Width == (orientation >= 5 ? 2 : 3), $"EXIF {orientation} width");
    for (var i = 0; i < 6; i++) Check(actual.GetPixel(i % actual.Width, i / actual.Width) == palette[expected[orientation-1][i]], $"EXIF {orientation} pixel {i}");
    var jpeg = WithExif(FixtureCamera.CreateFixture(0), (byte)orientation);
    using var decoded = PhotoProcessor.DecodeUpright(jpeg);
    Check(decoded.Width == (orientation >= 5 ? 800 : 1200), $"EXIF header {orientation}");
    using var normalized = PhotoProcessor.DecodeUpright(PhotoProcessor.Normalize(jpeg));
    Check(normalized.Width == decoded.Width && normalized.Height == decoded.Height, "No double rotation");
}

var fixtures = Enumerable.Range(0, 4).Select(FixtureCamera.CreateFixture).ToArray();
var sheet = PhotoProcessor.Compose(fixtures);
using (var decoded = PhotoProcessor.DecodeUpright(sheet))
{
    Check(decoded.Width == 1800 && decoded.Height == 1200, "6x4 300 dpi dimensions");
    var layout = PrintLayout.FourBySix(4);
    for (var i = 0; i < 4; i++)
    {
        using var photo = PhotoProcessor.DecodeUpright(fixtures[i]);
        var cell = layout.Photos[i];
        var actual = decoded.GetPixel(cell.X + cell.Width / 2, cell.Y + 60);
        var wanted = photo.GetPixel(600, 100);
        Check(Distance(actual, wanted) < 12, $"Pose {i + 1} in correct quadrant");
    }
    Check(decoded.GetPixel(10, 10).Red > 245, "Safe margin white");
    Check(decoded.GetPixel(900, 600).Red > 245, "Gutter white");
}
Check(sheet[13] == 1 && sheet[14] == 1 && sheet[15] == 44 && sheet[16] == 1 && sheet[17] == 44, "JFIF 300x300 dpi");
using (var single = PhotoProcessor.DecodeUpright(PhotoProcessor.Compose([fixtures[0]]))) Check(single.Width == 1800, "Single same print format");
Reject(() => PhotoProcessor.Compose([fixtures[0], fixtures[1]]));
Reject(() => PhotoProcessor.DecodeUpright([1,2,3]));
Reject(() => PhotoProcessor.DecodeUpright(fixtures[0][..100]));
using (var large = new SKBitmap(2560, 1706))
{
    using var canvas = new SKCanvas(large); canvas.Clear(SKColors.White);
    using var paint = new SKPaint { Color = SKColors.DarkBlue };
    canvas.DrawRect(0, 0, 200, 1706, paint); paint.Color = SKColors.DarkRed; canvas.DrawRect(2360, 0, 200, 1706, paint);
    var original = PhotoProcessor.Jpeg(large); var before = SessionStore.Hash(original);
    var optimized = PhotoProcessor.PrepareGenerationInput(original);
    using var small = PhotoProcessor.DecodeUpright(optimized);
    Check(small.Width == 1536 && small.Height == 1024, "Generation input keeps landscape proportion without crop");
    Check(small.GetPixel(25,500).Blue > small.GetPixel(25,500).Red, "Left edge preserved in generation input");
    Check(small.GetPixel(1510,500).Red > small.GetPixel(1510,500).Blue, "Right edge preserved in generation input");
    Check(optimized.Length < original.Length && SessionStore.Hash(original) == before, "Input transfer reduced without modifying archived original bytes");
}
using (var small = PhotoProcessor.DecodeUpright(PhotoProcessor.PrepareGenerationInput(fixtures[0])))
    Check(small.Width == 1200 && small.Height == 800, "Generation preparation never upscales smaller inputs");

// Print-ready square output fixtures: asymmetric corner marks prove no crop,
// rotation or overlay, and four distinct fills prove pose order.
var printEvidence = Path.Combine(Environment.CurrentDirectory, "artifacts", "direct-square-0.6.0", "prints");
Directory.CreateDirectory(printEvidence);
var squares = Enumerable.Range(0, 4).Select(i => {
    using var bitmap = new SKBitmap(1024, 1024); using var canvas = new SKCanvas(bitmap);
    canvas.Clear(new[] { SKColor.Parse("#CBA0D6"), SKColor.Parse("#EEC66A"), SKColor.Parse("#97CBBB"), SKColor.Parse("#DB8D80") }[i]);
    using var paint = new SKPaint { Color = SKColor.Parse("#302342"), IsAntialias = true };
    canvas.DrawRect(0, 0, 80, 80, paint); canvas.DrawRect(944, 944, 80, 80, paint);
    for (var mark = 0; mark <= i; mark++) canvas.DrawCircle(365 + mark * 100, 512, 30, paint);
    return PhotoProcessor.Jpeg(bitmap);
}).ToArray();
var hashes = squares.Select(bytes => SessionStore.Hash(bytes)).ToArray();
foreach (var style in StyleCatalog.BuiltIn.Styles.Append(StyleCatalog.Surprise).Append(new PhotoStyle(StyleCatalog.Mixed, "Plusieurs univers", "", "colors_party", "#E0E7D8", ["Les couleurs se préparent."])))
foreach (var count in new[] { 1, 4 })
{
    var printed = MetamorphosisSheet.Compose(squares.Take(count).ToArray(), style.Id);
    using var decoded = PhotoProcessor.DecodeUpright(printed);
    Check(decoded.Width == 1800 && decoded.Height == 1200, "Square photos keep physical 6x4 master");
    Check(printed[13] == 1 && printed[14] == 1 && printed[15] == 44 && printed[16] == 1 && printed[17] == 44, "Themed sheet remains 300dpi");
    var layout = PrintLayout.Metamorphosis(count);
    foreach (var (cell, i) in layout.Photos.Select((cell, i) => (cell, i)))
    {
        Check(cell.Width == cell.Height && cell.X >= 60 && cell.Y >= 60 && cell.X + cell.Width <= 1140 && cell.Y + cell.Height <= 1140, "Square cells stay inside safe print area");
        using var input = PhotoProcessor.DecodeUpright(squares[i]);
        Check(Distance(decoded.GetPixel(cell.X + cell.Width / 2, cell.Y + cell.Height / 4), input.GetPixel(512, 256)) < 15, "Pose order and color preserved");
        Check(decoded.GetPixel(cell.X + 10, cell.Y + 10).Red < 80 && decoded.GetPixel(cell.X + cell.Width - 10, cell.Y + cell.Height - 10).Red < 80, "Opposite image edges intact, no decorative overlay");
    }
    await File.WriteAllBytesAsync(Path.Combine(printEvidence, $"{style.Id}-{count}.jpg"), printed);
}
Check(hashes.SequenceEqual(squares.Select(bytes => SessionStore.Hash(bytes))), "Print composition never changes source photos");
Check(MetamorphosisSheet.Compose([squares[0]], "unknown_style").SequenceEqual(MetamorphosisSheet.Compose([squares[0]], "another_remote_style")), "Unknown themes use one deterministic neutral print fallback");
Check(!MetamorphosisSheet.Compose([squares[0]], "unknown_style").SequenceEqual(MetamorphosisSheet.Compose([squares[0]], StyleCatalog.Chef)), "Unknown chosen theme is not mislabeled as a Chef choice");
Check(PhotoProcessor.Compose(fixtures).SequenceEqual(sheet), "Au naturel master remains byte-for-byte unchanged");
Reject(() => MetamorphosisSheet.Compose([squares[0], squares[1]], "halloween"));

var root = Path.Combine(Path.GetTempPath(), "photobooth-imaging-tests-" + Guid.NewGuid().ToString("N"));
try
{
    var store = new SessionStore(root); var session = Guid.NewGuid();
    var asset = await store.SavePhotoAsync(session, sheet);
    Check((await store.ReadVerifiedAsync(asset)).SequenceEqual(sheet), "Preview/print use exact persisted bytes");
    Reject(() => store.Resolve("../../outside.jpg"));
    await File.WriteAllBytesAsync(store.Resolve(asset.Key), fixtures[0]);
    try { await store.ReadVerifiedAsync(asset); throw new Exception("Tamper accepted"); } catch (InvalidDataException) { assertions++; }
    await using var camera = new FixtureCamera(store);
    var captures = new List<CapturedPhoto>();
    for (var i = 0; i < 4; i++) captures.Add(await camera.CaptureAsync(session, i, default));
    Check(captures.Select(c => c.LocalAssetKey).Distinct().Count() == 4, "Four independent files");
    Check(captures.Select(c => SessionStore.Hash(File.ReadAllBytes(store.Resolve(c.LocalAssetKey)))).Distinct().Count() == 4, "Four distinct images");
    var artifact = Path.Combine(Environment.CurrentDirectory, "artifacts", "validation");
    Directory.CreateDirectory(artifact);
    await File.WriteAllBytesAsync(Path.Combine(artifact, "four-poses-4x6.jpg"), sheet);
    await File.WriteAllBytesAsync(Path.Combine(artifact, "single-pose-4x6.jpg"), PhotoProcessor.Compose([fixtures[0]]));
}
finally { Directory.Delete(root, recursive: true); }
Console.WriteLine($"PASS {assertions} imaging assertions; no network, no physical print.");

static int Distance(SKColor a, SKColor b) => Math.Abs(a.Red-b.Red)+Math.Abs(a.Green-b.Green)+Math.Abs(a.Blue-b.Blue);
static byte[] WithExif(byte[] jpeg, byte orientation)
{
    // APP1, Exif, little-endian TIFF, one SHORT orientation entry.
    byte[] exif = [0xff,0xe1,0,34,69,120,105,102,0,0,73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,orientation,0,0,0,0,0,0,0];
    return [..jpeg[..2], ..exif, ..jpeg[2..]];
}

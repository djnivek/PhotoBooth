using System.Diagnostics;
using System.Text.Json;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;
using SkiaSharp;

internal static class TransferBenchmark
{
    internal static async Task RunAsync()
    {
        var samples = Enumerable.Range(0, 4).Select(DetailedFixture).ToArray();
        var sets = new List<(string Name, byte[][] Photos)> { ("synthetic detailed images, not portraits or OpenAI output", samples) };
        var local = new List<byte[]>(); var seen = new HashSet<string>();
        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EstellArt", "sessions");
        if (Directory.Exists(dataRoot))
        {
            var store = new SessionStore(dataRoot);
            foreach (var manifest in Directory.EnumerateFiles(dataRoot, "session.json", SearchOption.AllDirectories))
            {
                try
                {
                    using var json = JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
                    foreach (var capture in json.RootElement.GetProperty("captures").EnumerateArray())
                    {
                        var bytes = await File.ReadAllBytesAsync(store.Resolve(capture.GetProperty("LocalAssetKey").GetString()!));
                        using var image = PhotoProcessor.DecodeUpright(bytes);
                        if (Math.Max(image.Width, image.Height) < 1800 || !seen.Add(SessionStore.Hash(bytes))) continue;
                        local.Add(bytes); if (local.Count == 4) break;
                    }
                }
                catch (Exception) { /* Skip incomplete journals; never alter session files. */ }
                if (local.Count == 4) break;
            }
            if (local.Count > 0) sets.Add(("local booth originals, measured locally without upload", Enumerable.Range(0, 4).Select(i => local[i % local.Count]).ToArray()));
        }
        var reports = new List<object>();
        foreach (var set in sets)
        {
            var timer = Stopwatch.StartNew();
            var inputs = set.Photos.Select(bytes => PhotoProcessor.PrepareGenerationInput(bytes)).ToArray(); timer.Stop();
            var encodeMs = timer.Elapsed.TotalMilliseconds;
            // Transfer estimate uses a local JPEG surrogate for provider output.
            // This does not measure generation latency or identity preservation.
            var provider = set.Photos.Select(bytes => PhotoProcessor.Normalize(bytes, 1536, 95)).ToArray();
            var oldFinals = provider.Select(bytes => PhotoProcessor.Normalize(bytes)).ToArray();
            var oldSheet = PhotoProcessor.Compose(oldFinals); var newSheet = PhotoProcessor.Compose(provider);
            var oldArchive = BoothCloudClient.BuildArchive(oldFinals, oldSheet, set.Photos);
            long oldUpload = set.Photos.Sum(b => (long)b.Length) + oldFinals.Sum(b => (long)b.Length) * 2 + oldSheet.Length + oldArchive.Length;
            long newUpload = set.Photos.Sum(b => (long)b.Length) + inputs.Sum(b => (long)b.Length) + newSheet.Length;
            reports.Add(new {
                sample = set.Name, originals_bytes = set.Photos.Sum(b => (long)b.Length), optimized_inputs_bytes = inputs.Sum(b => (long)b.Length),
                input_reduction_percent = Math.Round(100d * (1 - (double)inputs.Sum(b => (long)b.Length) / set.Photos.Sum(b => (long)b.Length)), 1),
                old_mobile_upload_bytes = oldUpload, new_mobile_upload_bytes = newUpload,
                upload_reduction_percent = Math.Round(100d * (1 - (double)newUpload / oldUpload), 1),
                input_preparation_ms = Math.Round(encodeMs), input_psnr_db = set.Photos.Select((bytes, i) => Math.Round(Psnr(bytes, inputs[i]), 2)).ToArray(),
                uplink_transfer_lower_bounds_seconds = new[] { 2, 5, 10 }.Select(mbps => new { mbps, before = Math.Round(oldUpload * 8d / (mbps * 1_000_000), 1), after = Math.Round(newUpload * 8d / (mbps * 1_000_000), 1) }),
                warning = "Transfer-only lower bounds, excluding RTT, provider generation and contention; JPEG surrogate is not generated content. PSNR is not a face-identity evaluation."
            });
        }
        var output = Path.Combine(Environment.CurrentDirectory, "artifacts", "optimization-0.5.0"); Directory.CreateDirectory(output);
        var report = new { generated_at = DateTimeOffset.UtcNow, paid_calls = 0, local_unique_large_originals = local.Count, reports };
        await File.WriteAllTextAsync(Path.Combine(output, "transfer-benchmark.json"), JsonSerializer.Serialize(report, SessionStore.JsonOptions));
        Console.WriteLine(JsonSerializer.Serialize(report, SessionStore.JsonOptions));
    }

    private static byte[] DetailedFixture(int seed)
    {
        using var bitmap = new SKBitmap(2560, 1706); var random = new Random(618 + seed);
        for (var y = 0; y < bitmap.Height; y++) for (var x = 0; x < bitmap.Width; x++)
        {
            var texture = random.Next(-12, 13); var tone = 128 + 48 * Math.Sin(x * .013) + 35 * Math.Cos(y * .017);
            bitmap.SetPixel(x, y, new SKColor((byte)Math.Clamp(tone + texture + 15, 0, 255), (byte)Math.Clamp(tone + texture, 0, 255), (byte)Math.Clamp(tone + texture - 15, 0, 255)));
        }
        return PhotoProcessor.Jpeg(bitmap);
    }
    private static double Psnr(byte[] original, byte[] encoded)
    {
        using var decoded = PhotoProcessor.DecodeUpright(encoded);
        using var source = PhotoProcessor.DecodeUpright(original);
        using var reference = new SKBitmap(decoded.Width, decoded.Height); using var canvas = new SKCanvas(reference);
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, new SKRect(0, 0, decoded.Width, decoded.Height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        double squared = 0;
        for (var y = 0; y < decoded.Height; y++) for (var x = 0; x < decoded.Width; x++)
        {
            var a = reference.GetPixel(x, y); var b = decoded.GetPixel(x, y);
            squared += Math.Pow(a.Red - b.Red, 2) + Math.Pow(a.Green - b.Green, 2) + Math.Pow(a.Blue - b.Blue, 2);
        }
        var mse = squared / (decoded.Width * (double)decoded.Height * 3);
        return mse == 0 ? 100 : 10 * Math.Log10(255d * 255 / mse);
    }
}

using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;
using PhotoBooth.Windows;

var passed = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); passed++; }
var normal = PrintRaster.Fit(new(1800, 1200, 300, 300));
Check(normal == new RasterPlacement(0, 0, 1800, 1200), "Standard 300 dpi target");
var high = PrintRaster.Fit(new(1800, 2400, 300, 600));
Check(high == new RasterPlacement(0, 0, 1800, 2400), "Non-square DNP device pixels preserve physical aspect");
var inset = PrintRaster.Fit(new(1750, 1170, 300, 300));
Check(inset.Width <= 1750 && inset.Height <= 1170 && inset.X >= 0 && inset.Y >= 0, "No clipping outside printable area");
var sheet = PhotoProcessor.Compose(Enumerable.Range(0, 4).Select(FixtureCamera.CreateFixture).ToArray());
var dib = PrintRaster.ToBgra(sheet);
Check(dib.Length == 1800 * 1200 * 4, "One uncompressed 32-bit page");
using (var decoded = PhotoProcessor.DecodeUpright(sheet))
{
    foreach (var (x, y) in new[] { (200, 100), (1100, 100), (200, 750), (1100, 750) })
    {
        var color = decoded.GetPixel(x, y); var offset = (y * 1800 + x) * 4;
        Check(dib[offset] == color.Blue && dib[offset+1] == color.Green && dib[offset+2] == color.Red,
            "BGR channel order and top-down scanlines preserve each reviewed pose");
    }
}
var root = Path.Combine(Path.GetTempPath(), "estellart-print-" + Guid.NewGuid().ToString("N"));
try
{
    foreach (var fail in new[] { false, true })
    {
        var store = new SessionStore(root);
        await using var controller = new BoothController(store, () => new FixtureCamera(store), new RehearsalTransform(), (_, _) => Task.CompletedTask);
        await controller.BeginAsync(new SessionOptions(1, PhotoMode.Original)); await controller.TakeAsync();
        var printer = new FakePrinter(store, fail);
        try { await controller.PrintAsync(printer); } catch (IOException) when (fail) { }
        Check(printer.Calls == 1, "Exactly one spool attempt");
        Check(controller.Session!.PrintStage == (fail ? PrintStage.NeedsOperator : PrintStage.Submitted), "Ambiguous vs submitted state");
        try { await controller.PrintAsync(printer); throw new Exception("Duplicate accepted"); } catch (InvalidOperationException) { passed++; }
        Check(printer.Calls == 1, "Duplicate visitor click never resubmits");
        var journal = await File.ReadAllTextAsync(Path.Combine(root, controller.Session.Id.ToString("N"), "print.json"));
        Check(journal.Contains(fail ? "needs_operator" : "submitted"), "Durable result journal");
    }
}
finally { Directory.Delete(root, true); }
Console.WriteLine($"PASS {passed} print preparation checks; native spooler and Canon remain hardware gates.");

sealed class FakePrinter(SessionStore store, bool fail) : IPhotoPrinter
{
    public int Calls { get; private set; }
    public Task<PrintSubmission> SubmitAsync(PrintRequest request, CancellationToken token)
    {
        Calls++;
        var journal = File.ReadAllText(Path.Combine(store.Root, request.SessionId.ToString("N"), "print.json"));
        if (!journal.Contains("requested")) throw new Exception("No durable intent before external side effect");
        if (fail) throw new IOException("Ambiguous spool timeout");
        return Task.FromResult(new PrintSubmission("test-job-42"));
    }
}

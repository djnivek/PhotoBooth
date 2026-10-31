using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;
using PhotoBooth.Windows;
using System.Text.Json;

var passed = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); passed++; }
foreach (var key in new uint[] { 0x5B, 0x5C, 0x7A })
    Check(WindowsKioskGuard.Blocks(key, false, false), "Windows keys and F11 cannot leave guest mode");
foreach (var key in new uint[] { 0x09, 0x1B, 0x20, 0x73 })
    Check(WindowsKioskGuard.Blocks(key, true, false), "Alt desktop shortcuts are suppressed");
Check(WindowsKioskGuard.Blocks(0x1B, false, true), "Ctrl Escape and Ctrl Shift Escape are suppressed");
foreach (var key in new uint[] { 0x09, 0x20, 0x41, 0x31, 0x0D, 0x08 })
    Check(!WindowsKioskGuard.Blocks(key, false, false), "Normal navigation and operator-code input stay usable");
if (OperatingSystem.IsWindows()) { using var guard = new WindowsKioskGuard(); guard.Dispose(); }
var normal = PrintRaster.Fit(new(1800, 1200, 300, 300));
Check(normal == new RasterPlacement(0, 0, 1800, 1200), "Standard 300 dpi target");
var high = PrintRaster.Fit(new(1800, 2400, 300, 600));
Check(high == new RasterPlacement(0, 0, 1800, 2400), "Non-square DNP device pixels preserve physical aspect");
var inset = PrintRaster.Fit(new(1750, 1170, 300, 300));
Check(inset.Width <= 1750 && inset.Height <= 1170 && inset.X >= 0 && inset.Y >= 0, "No clipping outside printable area");
Check(PrintRaster.Fit(new(1844,1240,300,300))==new RasterPlacement(0,5,1844,1229),"DNP documented high-speed bleed raster preserves the complete sheet");
Check(PrintRaster.Fit(new(3688,2480,600,600))==new RasterPlacement(0,10,3688,2459),"DNP documented high-quality application raster preserves the complete sheet");
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
    {
        var store=new SessionStore(root);
        await using var controller=new BoothController(store,()=>new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
        await controller.BeginAsync(new(1,PhotoMode.Original));await controller.TakeAsync();
        var printer=new NotSubmittedOncePrinter();
        try {await controller.PrintAsync(printer);throw new Exception("Preflight failure accepted");}
        catch(PrintNotSubmittedException) {passed++;}
        Check(printer.Calls==1 && controller.Session!.PrintStage==PrintStage.NotRequested,"Proven preflight rejection does not retry itself and permits a later explicit action");
        var path=Path.Combine(root,controller.Session!.Id.ToString("N"),"print.json");
        var rejected=JsonSerializer.Deserialize<PrintJournal>(await File.ReadAllTextAsync(path))!;
        Check(rejected.State=="not_submitted","Durable preflight refusal before reopening the print action");
        await controller.PrintAsync(printer);
        Check(printer.Calls==2 && controller.Session.PrintStage==PrintStage.Submitted,"Explicit retry after correcting preflight can submit once");
        var accepted=JsonSerializer.Deserialize<PrintJournal>(await File.ReadAllTextAsync(path))!;
        Check(accepted.RequestId!=rejected.RequestId && accepted.Sha256==rejected.Sha256,"Explicit retry creates a fresh intent for exactly the reviewed sheet");
        try {await controller.PrintAsync(printer);throw new Exception("Third print accepted");}
        catch(InvalidOperationException) {passed++;}
        Check(printer.Calls==2,"Successful explicit retry cannot print twice");
        var missing=new WindowsPhotoPrinter(store,controller.Sheet!,"missing-"+Guid.NewGuid().ToString("N"));
        try {await missing.SubmitAsync(new(Guid.NewGuid(),controller.Session.Id,controller.Sheet!.Key),default);throw new Exception("Missing queue accepted");}
        catch(PrintNotSubmittedException) {passed++;}
    }
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
    foreach (var fail in new[] { false, true })
    {
        var store=new SessionStore(root);
        await using var controller=new BoothController(store,()=>new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
        await controller.BeginAsync(new(1,PhotoMode.Original));await controller.TakeAsync();
        var printedSession=controller.Session!;var printedSheet=controller.Sheet!;
        var printer=new PausedPrinter(fail);var printing=controller.PrintAsync(printer);
        await printer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await controller.BeginAsync(new(1,PhotoMode.Original));
        var next=controller.Session!;
        printer.Resume.SetResult();
        try {await printing;}catch(IOException) when(fail) { }
        Check(printedSession.PrintStage==(fail?PrintStage.NeedsOperator:PrintStage.Submitted),"Print completion stays attached to the original session");
        Check(next.PrintStage==PrintStage.NotRequested,"Next guest never inherits another guest's print state");
        var journal=JsonSerializer.Deserialize<PrintJournal>(await File.ReadAllTextAsync(Path.Combine(root,printedSession.Id.ToString("N"),"print.json")))!;
        Check(journal.SheetKey==printedSheet.Key&&journal.Sha256==printedSheet.Sha256,"In-flight print journal retains the reviewed sheet after session switch");
        Check(!File.Exists(Path.Combine(root,next.Id.ToString("N"),"print.json")),"No print journal is written for the next guest");
    }
}
finally { Directory.Delete(root, true); }
Console.WriteLine($"PASS {passed} print preparation checks; native spooler and Canon remain hardware gates.");
await CanonChecks.RunAsync();

sealed class NotSubmittedOncePrinter:IPhotoPrinter
{
    public int Calls;
    public Task<PrintSubmission> SubmitAsync(PrintRequest request,CancellationToken token)
    {
        if(++Calls==1)throw new PrintNotSubmittedException("Test queue unavailable.");
        return Task.FromResult(new PrintSubmission("explicit-retry-job"));
    }
}

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

sealed class PausedPrinter(bool fail):IPhotoPrinter
{
    public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Resume {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task<PrintSubmission> SubmitAsync(PrintRequest request,CancellationToken token)
    {
        Entered.SetResult();await Resume.Task;
        if(fail)throw new IOException("Uncertain physical spool result");
        return new("paused-job");
    }
}

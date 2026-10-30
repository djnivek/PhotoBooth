using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;
using PhotoBooth.Windows;
using QRCoder;

namespace PhotoBooth.App;

internal static class PackageSelfTest
{
    // Always explicit fake dependencies. This executable test cannot connect to a camera,
    // printer or OpenAI, regardless of the operator's saved application settings.
    internal static async Task RunAsync(string output)
    {
        Directory.CreateDirectory(output);var receipts=new List<object>();
        var store=new SessionStore(Path.Combine(output,"sessions"));
        foreach(var count in new[]{1,4})foreach(var mode in new[]{PhotoMode.Original,PhotoMode.Surprise})
        {
            await using var controller=new BoothController(store,()=>new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
            await controller.BeginAsync(new(count,mode));await controller.TakeAsync();
            if(controller.Session?.Stage!=SessionStage.Ready)throw new InvalidOperationException(controller.Message);
            var bytes=await controller.GetPrintBytesAsync();var raster=PrintRaster.ToBgra(bytes);
            if(raster.Length!=1800*1200*4)throw new InvalidDataException("Packaged native imaging output invalid.");
            var printer=new RehearsalPrinter(store);await controller.PrintAsync(printer);
            if(printer.Submissions!=1)throw new InvalidOperationException("Expected one simulated spool submission.");
            var photos=new List<byte[]>();foreach(var photo in controller.FinalAssets)photos.Add(await store.ReadVerifiedAsync(photo));
            var originals=new List<byte[]>();
            if(mode==PhotoMode.Surprise)foreach(var capture in controller.Session.Captures)
                originals.Add(await File.ReadAllBytesAsync(store.Resolve(capture.LocalAssetKey)));
            using var zip=new ZipArchive(new MemoryStream(BoothCloudClient.BuildArchive(photos,bytes,originals)));
            if(zip.Entries.Count!=count+originals.Count+1)throw new InvalidDataException("Incomplete packaged archive.");
            receipts.Add(new { count,mode=mode.ToString(),sheet_sha256=controller.Sheet!.Sha256,archive_entries=zip.Entries.Count,simulated_print_jobs=printer.Submissions });
        }
        using(var generator=new QRCodeGenerator())using(var data=generator.CreateQrCode("https://example.invalid/rehearsal",QRCodeGenerator.ECCLevel.M))
        using(var png=new PngByteQRCode(data))
        {using var bitmap=PhotoProcessor.DecodeUpright(png.GetGraphic(3));if(bitmap.Width<50)throw new InvalidDataException("Packaged QR rendering failed.");}
        var pin=KioskExitGate.Create("C817");if(!pin.TryAuthorizeExit("C817"))throw new InvalidOperationException("PIN check failed.");
        var credential=Path.Combine(output,"test-credential");await CredentialStore.SaveAsync(credential,"synthetic-credential");
        if(CredentialStore.Load(credential)!="synthetic-credential")throw new InvalidOperationException("Packaged credential protection failed.");File.Delete(credential);
        await File.WriteAllTextAsync(Path.Combine(output,"self-test.json"),JsonSerializer.Serialize(new
        {
            version=typeof(Program).Assembly.GetName().Version?.ToString(),utc=DateTimeOffset.UtcNow,
            os=RuntimeInformation.OSDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),
            camera_sdk=typeof(FC.SDK.CanonCamera).Assembly.GetName().Version?.ToString(),
            cases=receipts,paid_api_calls=0,hardware_access=false,success=true
        },SessionStore.JsonOptions));
    }
    private sealed class RehearsalPrinter(SessionStore store):IPhotoPrinter
    {
        public int Submissions;
        public Task<PrintSubmission> SubmitAsync(PrintRequest request,CancellationToken token)
        {
            var intent=File.ReadAllText(Path.Combine(store.Root,request.SessionId.ToString("N"),"print.json"));
            if(!intent.Contains("requested"))throw new InvalidOperationException("No persisted print intent.");
            Submissions++;return Task.FromResult(new PrintSubmission("self-test-only"));
        }
    }
}

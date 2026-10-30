using System.IO.Compression;
using System.Text.Json;
using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;

if(args.Length!=1||args[0] is not ("--share-synthetic-originals" or "--share-synthetic-edits"))
    throw new ArgumentException("Explicit --share-synthetic-originals or --share-synthetic-edits is required. This tool never calls a generation endpoint.");
var edited=args[0]=="--share-synthetic-edits";
var provision=JsonDocument.Parse(await File.ReadAllTextAsync(".local/booth-provisioning.json"));
var api=provision.RootElement.GetProperty("api_base_url").GetString()!;
var key=provision.RootElement.GetProperty("access_key").GetString()!;
var root=Path.Combine(Environment.CurrentDirectory,"artifacts","cloud-smoke");
var store=new SessionStore(root);
await using var controller=new BoothController(store,()=>new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
await controller.BeginAsync(new SessionOptions(4,edited?PhotoMode.Surprise:PhotoMode.Original));await controller.TakeAsync();
if(controller.Session?.Stage!=SessionStage.Ready)throw new Exception(controller.Message);
using var client=new BoothCloudClient(api,key,store);
var generationConfigured=await client.CheckAsync();
var published=await client.PublishAsync(controller.Session,controller.FinalAssets,controller.Sheet!);
using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false});
var downloaded=await http.GetByteArrayAsync(published.DownloadUrl ?? published.Url);
using var archive=new ZipArchive(new MemoryStream(downloaded));
if(archive.Entries.Count!=(edited?9:5))throw new Exception("Archive does not contain every required photograph.");
if(edited)for(var i=0;i<4;i++){
 await using var entry=archive.GetEntry($"originaux/photo-{i+1}.jpg")!.Open();using var output=new MemoryStream();await entry.CopyToAsync(output);
 var original=await File.ReadAllBytesAsync(store.Resolve(controller.Session.Captures[i].LocalAssetKey));
 if(!output.ToArray().SequenceEqual(original))throw new Exception("Unedited original was not preserved.");
}
for(var i=0;i<4;i++)
{
    await using var entry=archive.GetEntry($"photo-{i+1}.jpg")!.Open();using var output=new MemoryStream();await entry.CopyToAsync(output);
    if(SessionStore.Hash(output.ToArray())!=controller.FinalAssets[i].Sha256)throw new Exception("Downloaded photograph differs from local preview input.");
}
await using(var entry=archive.GetEntry("planche-4x6.jpg")!.Open())
{using var output=new MemoryStream();await entry.CopyToAsync(output);if(SessionStore.Hash(output.ToArray())!=controller.Sheet!.Sha256)throw new Exception("Downloaded sheet differs from print master.");}
var retry=await client.PublishAsync(controller.Session,controller.FinalAssets,controller.Sheet!);
var receipt=new { session_id=controller.Session.Id, utc=DateTimeOffset.UtcNow, mode=edited?"synthetic-local-effect-NOT-OPENAI":"synthetic-originals", generation_calls=0,
    generation_configured=generationConfigured, archive_entries=archive.Entries.Count, archive_sha256=SessionStore.Hash(downloaded),
    sheet_sha256=controller.Sheet!.Sha256, published.ExpiresAt, publish_retry_succeeded=retry.Url.Length>0 };
await File.WriteAllTextAsync(Path.Combine(root,edited?"receipt-with-originals.json":"receipt.json"),JsonSerializer.Serialize(receipt,SessionStore.JsonOptions));
Console.WriteLine(JsonSerializer.Serialize(receipt,SessionStore.JsonOptions));

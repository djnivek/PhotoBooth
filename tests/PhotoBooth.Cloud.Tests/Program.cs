using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;

var root=Path.Combine(Path.GetTempPath(),"estellart-cloud-tests-"+Guid.NewGuid().ToString("N"));
var passed=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);passed++;}
try
{
    var originals=Enumerable.Range(0,4).Select(FixtureCamera.CreateFixture).ToArray();
    var sheet=PhotoProcessor.Compose(originals);
    var zip=BoothCloudClient.BuildArchive(originals,sheet);
    Check(zip.SequenceEqual(BoothCloudClient.BuildArchive(originals,sheet)),"Archive bytes stable across upload retries");
    using(var archive=new ZipArchive(new MemoryStream(zip)))
    {
        Check(archive.Entries.Count==5,"Four independent JPEGs plus one sheet");
        for(var i=0;i<4;i++)
        {using var entry=archive.GetEntry($"photo-{i+1}.jpg")!.Open();using var memory=new MemoryStream();entry.CopyTo(memory);Check(memory.ToArray().SequenceEqual(originals[i]),"Downloaded JPEG preserves exact original bytes");}
    }
    using(var archive=new ZipArchive(new MemoryStream(BoothCloudClient.BuildArchive(originals,sheet,originals))))
    {
        Check(archive.Entries.Count==9,"Four edited photos, four originals and one sheet");
        for(var i=0;i<4;i++)
        {using var entry=archive.GetEntry($"originaux/photo-{i+1}.jpg")!.Open();using var memory=new MemoryStream();entry.CopyTo(memory);Check(memory.ToArray().SequenceEqual(originals[i]),"Unedited pose preserved in the download archive");}
    }
    foreach(var networkDrop in new[]{false,true})
    {
        var handler=new FakeHttp(originals[0]){DropStartResponse=networkDrop};
        using var cloud=new BoothCloudClient("https://test.supabase.co/functions/v1/photobooth-api","fake-booth",new(root),handler:handler,delay:(_,_)=>Task.CompletedTask);
        var transformed=await cloud.TransformAsync(Guid.NewGuid(),[originals[0]],new Progress<string>(),default);
        Check(transformed.Photos.Count==1&&transformed.Style=="cinema","One result for one pose");
        Check(handler.Starts==1,"Ambiguous start response never causes another POST");
        Check(!handler.CredentialSentToStorage,"Booth secret excluded from image download requests");
    }
    {
        var handler=new FakeHttp(originals[0]){Fixtures=originals};
        using var cloud=new BoothCloudClient("https://test.supabase.co/functions/v1/photobooth-api","fake-booth",new(root),handler:handler,delay:(_,_)=>Task.CompletedTask);
        var id=Guid.NewGuid();
        var result=await cloud.TransformAsync(id,originals,new Progress<string>(),default);
        Check(result.Photos.Count==4&&result.Style=="cinema","Four edited poses use one stable session style");
        for(var i=0;i<4;i++) Check(result.Photos[i].SequenceEqual(originals[i]),"Parallel results preserve pose order");
        Check(handler.Starts==4,"Four distinct poses start exactly four edits");
        await cloud.TransformAsync(id,originals,new Progress<string>(),default);
        Check(handler.Starts==4,"Resuming completed edits never starts another billable call");
        Check(handler.UploadCalls==4,"Each original uploaded only once across generation retries");
    }
    {
        var handler=new FakeHttp(originals[0]);var store=new SessionStore(root);
        using var cloud=new BoothCloudClient("https://test.supabase.co/functions/v1/photobooth-api","fake-booth",store,handler:handler);
        await using var controller=new BoothController(store,()=>new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
        await controller.BeginAsync(new SessionOptions(4,PhotoMode.Original));await controller.TakeAsync();
        var first=await cloud.PublishAsync(controller.Session!,controller.FinalAssets,controller.Sheet!);
        var second=await cloud.PublishAsync(controller.Session!,controller.FinalAssets,controller.Sheet!);
        Check(first.Url==second.Url,"Share retry preserves download URL");
        Check(handler.ShareTokens.Distinct().Count()==1,"Share capability persists across retries");
        Check(handler.Uploads.Count==6,"Four photos, sheet and archive uploaded");
        Check(handler.Starts==0,"Original sharing cannot invoke generation");
    }
    {
        var store=new SessionStore(root);
        await using var controller=new BoothController(store,()=>new FixtureCamera(store),new FailingTransform(),(_,_)=>Task.CompletedTask);
        await controller.BeginAsync(new SessionOptions(4,PhotoMode.Surprise));await controller.TakeAsync();
        Check(controller.Session!.Stage==SessionStage.Failed,"Failed AI never silently returns originals");
        await controller.UseOriginalsAsync();
        Check(controller.Session.Stage==SessionStage.Ready&&controller.Session.UsedOriginalFallback,"Explicit original recovery reaches preview");
        Check(controller.Session.Results.Select(x=>x.AssetKey).SequenceEqual(controller.Session.Captures.Select(x=>x.LocalAssetKey)),"Fallback keeps all original poses in order");
    }
    {
        var secret=Path.Combine(root,"credential");await CredentialStore.SaveAsync(secret,"test-only-credential");
        Check(CredentialStore.Load(secret)=="test-only-credential","Credential roundtrip (DPAPI on Windows)");
        Check(!File.ReadAllText(secret).Contains("test-only-credential"),"Credential not stored as literal text");
    }
}
finally{Directory.Delete(root,true);}
Console.WriteLine($"PASS {passed} cloud client checks; HTTP fully replaced by a fake handler, no paid API or external network.");

sealed class FailingTransform:IPhotoTransform
{
    public Task<TransformResult> TransformAsync(Guid id,IReadOnlyList<byte[]> photos,IProgress<string> progress,CancellationToken token)=>throw new IOException("Simulated API failure");
}
sealed class FakeHttp(byte[] fixture):HttpMessageHandler
{
    public int Starts;public int UploadCalls;public bool DropStartResponse;public bool CredentialSentToStorage;
    public byte[][]? Fixtures;
    private readonly HashSet<string> started=[];
    public Dictionary<string,string> Uploads=new();public List<string> ShareTokens=[];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        var path=request.RequestUri!.AbsolutePath;
        HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=JsonContent.Create(value)};
        if(path.Contains("/storage/"))
        {CredentialSentToStorage|=request.Headers.Authorization is not null;
            var index=int.Parse(Path.GetFileNameWithoutExtension(path));
            return new(HttpStatusCode.OK){Content=new ByteArrayContent(Fixtures?[index]??fixture)};}
        if(request.Headers.Authorization?.Parameter!="fake-booth")throw new Exception("Missing booth auth");
        if(path.EndsWith("/sessions"))return Json(new{style="cinema"});
        if(path.Contains("/photos/"))
        {
            if(request.Method==HttpMethod.Post){Starts++;started.Add(path);if(DropStartResponse)throw new HttpRequestException("Response lost after accept");return Json(new{state="running"});}
            return !started.Contains(path)?Json(new{state="queued"}):Json(new{state="ready",url=$"https://test.supabase.co/storage/{path.Split('/').Last()}.jpg"});
        }
        if(path.Contains("/assets/"))
        {
            UploadCalls++;
            var bytes=await request.Content!.ReadAsByteArrayAsync(token);var digest=SessionStore.Hash(bytes);
            if(Uploads.TryGetValue(path,out var existing)&&existing!=digest)throw new Exception("Non-idempotent asset upload");
            Uploads[path]=digest;return Json(new{sha256=digest});
        }
        if(path.EndsWith("/share"))
        {
            using var input=JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));ShareTokens.Add(input.RootElement.GetProperty("token").GetString()!);
            return Json(new{download_url="https://test.supabase.co/storage/archive.zip",expires_at=DateTimeOffset.UtcNow.AddDays(7)});
        }
        throw new Exception("Unexpected request "+path);
    }
}

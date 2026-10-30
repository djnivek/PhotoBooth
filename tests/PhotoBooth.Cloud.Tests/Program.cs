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
    var catalogJson=JsonSerializer.Serialize(StyleCatalog.BuiltIn);
    using(var handler=new CatalogHttp(catalogJson))
    {
        var catalog=await StyleCatalogClient.FetchAsync("https://test.supabase.co/functions/v1/photobooth-api",default,handler);
        Check(catalog.Styles.Length==6&&handler.Calls==1,"Public catalog uses exactly one unauthenticated request");
    }
    foreach(var invalid in new[]{"{}",new string('x',256*1024+1),catalogJson.Replace("Halloween","Halloween AI"),catalogJson.Replace("Halloween","Halloween-party")})
    {
        try {await StyleCatalogClient.FetchAsync("https://test.supabase.co",default,new CatalogHttp(invalid));throw new Exception("Invalid catalog accepted");}
        catch(InvalidDataException){Check(true,"Malformed, oversized or forbidden guest copy rejected");}
    }
    using(var cancellation=new CancellationTokenSource(30))
    {
        try {await StyleCatalogClient.FetchAsync("https://test.supabase.co",cancellation.Token,new CatalogHttp(null));throw new Exception("Stalled catalog body ignored cancellation");}
        catch(OperationCanceledException){Check(true,"Catalog body reads honor cancellation after headers");}
    }
    try {await StyleCatalogClient.FetchAsync("http://test.invalid",default,new CatalogHttp(catalogJson));throw new Exception("Insecure catalog accepted");}
    catch(ArgumentException){Check(true,"Catalog rejects HTTP before sending");}
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
        var handler=new FakeHttp(originals[0]);
        using var cloud=new BoothCloudClient("https://test.supabase.co/functions/v1/photobooth-api","fake-booth",new(root),handler:handler,delay:(_,_)=>Task.CompletedTask);
        var results=await Task.WhenAll(Enumerable.Range(0,32).Select(_=>Task.Run(()=>
            cloud.TransformAsync(Guid.NewGuid(),[originals[0]],new Progress<string>(),default))));
        Check(results.All(r=>r.Photos.Count==1&&r.Photos[0].SequenceEqual(originals[0])),"Concurrent fake requests preserve every accepted job and result");
        Check(handler.Starts==32,"Concurrent start accounting is exact");
        Check(handler.UploadCalls==32,"Concurrent upload accounting is exact");
    }
    {
        var handler=new FakeHttp(originals[0]);var store=new SessionStore(root);
        using var cloud=new BoothCloudClient("https://test.supabase.co/functions/v1/photobooth-api","fake-booth",store,handler:handler);
        await using var controller=new BoothController(store,()=>new FixtureCamera(store),new RehearsalTransform(),(_,_)=>Task.CompletedTask);
        await controller.BeginAsync(new SessionOptions(4,PhotoMode.Original));await controller.TakeAsync();
        var first=await cloud.PublishAsync(controller.Session!,controller.FinalAssets,controller.Sheet!);
        var second=await cloud.PublishAsync(controller.Session!,controller.FinalAssets,controller.Sheet!);
        Check(first.Url==second.Url,"Share retry preserves download URL");
        Check(first.Url.StartsWith("https://gallery.test/#") && first.DownloadUrl!.EndsWith("archive.zip"),"Server gallery is the default QR target; ZIP remains separately available");
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
        var printBytes=await controller.GetPrintBytesAsync();
        Check(SessionStore.Hash(printBytes)==controller.Sheet!.Sha256,"Original fallback passes the verified print-sheet guard");
        await controller.PrintAsync(new FallbackPrinter());
        Check(controller.Session.PrintStage==PrintStage.Submitted,"Original fallback remains explicitly printable after AI failure");
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
    public Task<TransformResult> TransformAsync(Guid id,IReadOnlyList<byte[]> photos,IProgress<string> progress,CancellationToken token,string styleChoice=StyleCatalog.Chef)=>throw new IOException("Simulated API failure");
}
sealed class FallbackPrinter:IPhotoPrinter
{
    public Task<PrintSubmission> SubmitAsync(PrintRequest request,CancellationToken token)=>Task.FromResult(new PrintSubmission("fake-fallback-print"));
}
sealed class FakeHttp(byte[] fixture):HttpMessageHandler
{
    public int Starts;public int UploadCalls;public bool DropStartResponse;public bool CredentialSentToStorage;
    public byte[][]? Fixtures;
    private readonly object gate=new();
    private readonly HashSet<string> started=[];
    public Dictionary<string,string> Uploads=new();public List<string> ShareTokens=[];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        var path=request.RequestUri!.AbsolutePath;
        HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=JsonContent.Create(value)};
        if(path.Contains("/storage/"))
        {lock(gate)CredentialSentToStorage|=request.Headers.Authorization is not null;
            var index=int.Parse(Path.GetFileNameWithoutExtension(path));
            return new(HttpStatusCode.OK){Content=new ByteArrayContent(Fixtures?[index]??fixture)};}
        if(request.Headers.Authorization?.Parameter!="fake-booth")throw new Exception("Missing booth auth");
        if(path.EndsWith("/sessions"))return Json(new{style="cinema"});
        if(path.Contains("/photos/"))
        {
            lock(gate)
            {
                if(request.Method==HttpMethod.Post){Starts++;started.Add(path);if(DropStartResponse)throw new HttpRequestException("Response lost after accept");return Json(new{state="running"});}
                return !started.Contains(path)?Json(new{state="queued"}):Json(new{state="ready",url=$"https://test.supabase.co/storage/{path.Split('/').Last()}.jpg"});
            }
        }
        if(path.Contains("/assets/"))
        {
            var bytes=await request.Content!.ReadAsByteArrayAsync(token);var digest=SessionStore.Hash(bytes);
            lock(gate)
            {
                UploadCalls++;
                if(Uploads.TryGetValue(path,out var existing)&&existing!=digest)throw new Exception("Non-idempotent asset upload");
                Uploads[path]=digest;
            }
            return Json(new{sha256=digest});
        }
        if(path.EndsWith("/share"))
        {
            using var input=JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            lock(gate)ShareTokens.Add(input.RootElement.GetProperty("token").GetString()!);
            return Json(new{gallery_url="https://gallery.test/#"+input.RootElement.GetProperty("token").GetString(),download_url="https://test.supabase.co/storage/archive.zip",expires_at=DateTimeOffset.UtcNow.AddDays(7)});
        }
        throw new Exception("Unexpected request "+path);
    }
}

sealed class CatalogHttp(string? json):HttpMessageHandler
{
    public int Calls;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        Calls++;
        if(request.Headers.Authorization is not null||request.Method!=HttpMethod.Get||!request.RequestUri!.AbsolutePath.EndsWith("/styles"))throw new Exception("Catalog request contract violated");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=json is null?new StreamContent(new StalledRead()):new StringContent(json)});
    }
}
sealed class StalledRead:Stream
{
    public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
    public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){await Task.Delay(Timeout.Infinite,token);return 0;}
    public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    public override void Flush()=>throw new NotSupportedException();public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
    public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
}

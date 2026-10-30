using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhotoBooth.Core;
using PhotoBooth.Imaging;

namespace PhotoBooth.Runtime;

public sealed record PublishedSession(string Url, DateTimeOffset ExpiresAt);

public sealed class BoothCloudClient : IPhotoTransform, IDisposable
{
    private readonly HttpClient http;
    private readonly Uri api;
    private readonly string accessKey;
    private readonly SessionStore store;
    private readonly string galleryBase;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;

    public BoothCloudClient(string baseUrl, string accessKey, SessionStore store, string galleryBase = "",
        HttpMessageHandler? handler = null, Func<TimeSpan,CancellationToken,Task>? delay = null)
    {
        api = new Uri(baseUrl.TrimEnd('/') + "/");
        if (api.Scheme != "https") throw new ArgumentException("L’API doit utiliser HTTPS.");
        if (string.IsNullOrWhiteSpace(accessKey)) throw new ArgumentException("Le code de connexion de la borne est manquant.");
        this.accessKey = accessKey; this.store = store; this.galleryBase = galleryBase;
        this.delay = delay ?? ((time, token) => Task.Delay(time, token));
        http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(75) };
    }

    public async Task<bool> CheckAsync(CancellationToken token = default)
    {
        using var result = await SendJsonAsync(HttpMethod.Get, "booth/check", null, token);
        if (!result.RootElement.GetProperty("authenticated").GetBoolean()) throw new InvalidOperationException("Connexion refusée.");
        return result.RootElement.GetProperty("image_generation_enabled").GetBoolean();
    }

    private async Task<string> EnsureSession(Guid id, int count, PhotoMode mode, CancellationToken token, string styleChoice = StyleCatalog.Chef)
    {
        using var result = await SendJsonAsync(HttpMethod.Post, "sessions", new { id = id.ToString(), photo_count = count,
            mode = mode == PhotoMode.Surprise ? "surprise" : "original", style = styleChoice }, token);
        return result.RootElement.GetProperty("style").GetString()!;
    }

    public async Task<TransformResult> TransformAsync(Guid sessionId, IReadOnlyList<byte[]> originals,
        IProgress<string> progress, CancellationToken token, string styleChoice = StyleCatalog.Chef)
    {
        var style = await EnsureSession(sessionId, originals.Count, PhotoMode.Surprise, token, styleChoice);
        using var concurrency = new SemaphoreSlim(2);
        var tasks = originals.Select(async (bytes, index) =>
        {
            await concurrency.WaitAsync(token);
            try
            {
                await UploadOriginalAsync(sessionId,index,bytes,token);
                progress.Report($"Transformation de la pose {index + 1} sur {originals.Count}…");
                var route = $"sessions/{sessionId}/photos/{index}";
                using var initial = await SendJsonAsync(HttpMethod.Get, route, null, token);
                var state = initial.RootElement.GetProperty("state").GetString();
                var journal = Path.Combine(store.Root, sessionId.ToString("N"), $"cloud-{index}.json");
                if (state == "queued")
                {
                    if (File.Exists(journal)) throw new IOException("Une génération a déjà été tentée. Vérifiez son état avant de la relancer.");
                    await SessionStore.WriteAtomicAsync(journal, JsonSerializer.SerializeToUtf8Bytes(new { sessionId, index, requestedAt = DateTimeOffset.UtcNow }), token);
                    try
                    {
                        using var content = JsonContent.Create(new { });
                        using var start = await SendAsync(HttpMethod.Post, route, content, token);
                        await EnsureSuccess(start);
                    }
                    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested)
                    {
                        // The server may have accepted it. Poll the same job; never repeat POST.
                        progress.Report("Connexion interrompue. Vérification de la génération déjà demandée…");
                    }
                }
                for (var attempt = 0; attempt < 240; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    using var status = await SendJsonAsync(HttpMethod.Get, route, null, token);
                    state = status.RootElement.GetProperty("state").GetString();
                    if (state == "ready") return await DownloadAsync(status.RootElement.GetProperty("url").GetString()!, token);
                    if (state is "failed" or "uncertain" || (state == "queued" && attempt > 4))
                        throw new IOException("La transformation n’a pas abouti. Vos originaux sont conservés ; aucune nouvelle génération automatique.");
                    await delay(TimeSpan.FromSeconds(2), token);
                }
                throw new IOException("La génération prend trop de temps. Les originaux et l’identifiant de suivi sont conservés.");
            }
            finally { concurrency.Release(); }
        }).ToArray();
        return new TransformResult(style, await Task.WhenAll(tasks));
    }

    public async Task<PublishedSession> PublishAsync(Session session, IReadOnlyList<StoredAsset> photos, StoredAsset sheet, CancellationToken token = default)
    {
        if (session.Stage != SessionStage.Ready || photos.Count != session.Options.PhotoCount ||
            !session.Results.Select(r => r.AssetKey).SequenceEqual(photos.Select(p => p.Key)) || session.SheetAssetKey != sheet.Key)
            throw new InvalidOperationException("Le partage doit correspondre exactement à l’aperçu validé.");
        await EnsureSession(session.Id, photos.Count, session.Options.Mode, token, session.Options.StyleChoice);
        var images = new List<byte[]>();
        foreach (var photo in photos) images.Add(await store.ReadVerifiedAsync(photo, token));
        var sheetBytes = await store.ReadVerifiedAsync(sheet, token);
        var originals = new List<byte[]>();
        if(session.Options.Mode==PhotoMode.Surprise)
            foreach(var capture in session.Captures)
            {
                var bytes=await File.ReadAllBytesAsync(store.Resolve(capture.LocalAssetKey),token);
                await UploadOriginalAsync(session.Id,originals.Count,bytes,token); originals.Add(bytes);
            }
        var archive = BuildArchive(images, sheetBytes, originals);
        if(archive.Length>50*1024*1024)throw new IOException("Les photos dépassent la taille maximale du téléchargement (50 Mo). Elles restent disponibles sur cet ordinateur.");
        for (var i = 0; i < images.Count; i++) await UploadAsync(session.Id, $"photo-{i+1}.jpg", images[i], token);
        await UploadAsync(session.Id, "sheet.jpg", sheetBytes, token);
        await UploadAsync(session.Id, "archive.zip", archive, token);
        var tokenPath = Path.Combine(store.Root, session.Id.ToString("N"), "share-token.txt");
        var shareToken = File.Exists(tokenPath) ? (await File.ReadAllTextAsync(tokenPath, token)).Trim() : Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        await SessionStore.WriteAtomicAsync(tokenPath, Encoding.ASCII.GetBytes(shareToken), token);
        using var shared = await SendJsonAsync(HttpMethod.Post, $"sessions/{session.Id}/share", new { token = shareToken, used_original_fallback = session.UsedOriginalFallback }, token);
        var url = galleryBase.Length > 0 ? galleryBase.TrimEnd('/') + "/#" + shareToken : shared.RootElement.GetProperty("download_url").GetString()!;
        var result = new PublishedSession(url, shared.RootElement.GetProperty("expires_at").GetDateTimeOffset());
        await SessionStore.WriteAtomicAsync(Path.Combine(store.Root, session.Id.ToString("N"), "share.json"), JsonSerializer.SerializeToUtf8Bytes(result), token);
        return result;
    }

    public static byte[] BuildArchive(IReadOnlyList<byte[]> photos, byte[] sheet,IReadOnlyList<byte[]>? originals=null)
    {
        if (photos.Count is not (1 or 4)) throw new ArgumentException("One or four photos required.");
        if(originals is {Count:>0}&&originals.Count!=photos.Count)throw new ArgumentException("Every edited pose needs its original.");
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen:true))
        {
            for (var i=0;i<photos.Count;i++) Write($"photo-{i+1}.jpg", photos[i]);
            if(originals is not null)for(var i=0;i<originals.Count;i++)Write($"originaux/photo-{i+1}.jpg",originals[i]);
            Write("planche-4x6.jpg",sheet);
            void Write(string name, byte[] bytes) { var entry=archive.CreateEntry(name,CompressionLevel.NoCompression);
                entry.LastWriteTime=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero);
                using var stream=entry.Open();stream.Write(bytes); }
        }
        return memory.ToArray();
    }
    private async Task UploadOriginalAsync(Guid id,int index,byte[] bytes,CancellationToken token)
    {
        var marker=Path.Combine(store.Root,id.ToString("N"),$"original-upload-{index}.sha256");
        var digest=SessionStore.Hash(bytes);
        if(File.Exists(marker)&&await File.ReadAllTextAsync(marker,token)==digest)return;
        await UploadAsync(id,$"original-{index+1}.jpg",bytes,token);
        await SessionStore.WriteAtomicAsync(marker,Encoding.ASCII.GetBytes(digest),token);
    }
    private async Task UploadAsync(Guid id, string name, byte[] bytes, CancellationToken token)
    {
        using var content = new ByteArrayContent(bytes); content.Headers.ContentType = new(name.EndsWith(".zip") ? "application/zip" : "image/jpeg");
        using var response = await SendAsync(HttpMethod.Put, $"sessions/{id}/assets/{name}", content, token);
        await EnsureSuccess(response);
        using var result = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        if (result.RootElement.GetProperty("sha256").GetString() != SessionStore.Hash(bytes))
            throw new IOException("La vérification du fichier envoyé a échoué.");
    }
    private async Task<byte[]> DownloadAsync(string address, CancellationToken token)
    {
        var uri = new Uri(address);
        if (uri.Scheme != "https" || uri.Host != api.Host || uri.Port != api.Port) throw new IOException("Adresse de résultat non reconnue.");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri); // No booth credential on signed storage requests.
        using var response = await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > PhotoProcessor.MaxEncodedBytes) throw new IOException("Image distante trop volumineuse.");
        await using var stream=await response.Content.ReadAsStreamAsync(token);using var memory=new MemoryStream();
        var buffer=new byte[65536];int read;
        while((read=await stream.ReadAsync(buffer,token))>0)
        { if(memory.Length+read>PhotoProcessor.MaxEncodedBytes)throw new IOException("Image distante trop volumineuse.");await memory.WriteAsync(buffer.AsMemory(0,read),token); }
        var bytes=memory.ToArray();using var decoded=PhotoProcessor.DecodeUpright(bytes);return bytes;
    }
    private async Task<JsonDocument> SendJsonAsync(HttpMethod method,string path,object? body,CancellationToken token)
    {
        using var content=body is null?null:JsonContent.Create(body);
        using var response=await SendAsync(method,path,content,token);await EnsureSuccess(response);
        return JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
    }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method,string path,HttpContent? content,CancellationToken token)
    {
        using var request=new HttpRequestMessage(method,new Uri(api,path)){Content=content};
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",accessKey);
        return await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
    }
    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if(response.IsSuccessStatusCode)return;
        string? code=null;
        try { using var body=JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());code=body.RootElement.GetProperty("code").GetString(); } catch(JsonException) { } catch(KeyNotFoundException) { }
        throw new IOException(code switch
        {
            "openai_not_configured"=>"La clé OpenAI doit être configurée dans Supabase avant le premier essai de métamorphose.",
            "unauthorized"=>"Le code de connexion de la borne est incorrect. Vérifiez les réglages.",
            "generation_uncertain_no_auto_retry"=>"La génération est incertaine. Aucun nouvel appel payant n’a été relancé automatiquement.",
            _=>$"Service momentanément indisponible ({(int)response.StatusCode}, {code??"réseau"}). Les photos restent enregistrées localement."
        });
    }
    public void Dispose()=>http.Dispose();
}

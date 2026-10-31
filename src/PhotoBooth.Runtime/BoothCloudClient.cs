using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhotoBooth.Core;
using PhotoBooth.Imaging;

namespace PhotoBooth.Runtime;

public sealed record PublishedSession(string Url, DateTimeOffset ExpiresAt, string? DownloadUrl = null);

public sealed class BoothCloudClient : IPhotoTransform, IDisposable
{
    private static readonly SemaphoreSlim GenerationSlots = new(8);
    private static readonly SemaphoreSlim UploadSlots = new(2);
    private static readonly SemaphoreSlim DownloadSlots = new(2);
    public BoothTelemetry? Telemetry { get; set; }
    private readonly ConcurrentDictionary<Guid,SessionOptions> metricOptions = new();
    private readonly HttpClient http;
    private readonly Uri api;
    private readonly string accessKey;
    private readonly SessionStore store;
    private readonly string galleryBase;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    public event Action<Guid, PublishedSession>? GalleryAvailable;

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

    private async Task<IReadOnlyList<string>> EnsureSession(Guid id, int count, PhotoMode mode, CancellationToken token,
        IReadOnlyList<string> choices)
    {
        var selected = StyleSelection.Normalize(choices, count);
        var modeName = mode == PhotoMode.Surprise ? "surprise" : "original";
        object request = selected.Count == 1
            ? new { id = id.ToString(), photo_count = count, mode = modeName, style = selected[0] }
            : new { id = id.ToString(), photo_count = count, mode = modeName, styles = selected };
        using var result = await SendJsonAsync(HttpMethod.Post, "sessions", request, token);
        if (mode == PhotoMode.Original) return Array.Empty<string>();
        string[] plan;
        if (result.RootElement.TryGetProperty("shot_styles", out var shots) && shots.ValueKind == JsonValueKind.Array)
            plan = shots.EnumerateArray().Select(s => s.ValueKind == JsonValueKind.String ? s.GetString()! : "").ToArray();
        else if (selected.Count == 1)
            plan = Enumerable.Repeat(result.RootElement.GetProperty("style").GetString()!, count).ToArray();
        else throw new IOException("Le serveur doit être actualisé pour préparer plusieurs univers. Vos originaux sont conservés.");
        if (!StyleSelection.MatchesPlan(selected, plan, count))
            throw new IOException("La répartition des univers reçue est incomplète. Vos originaux sont conservés.");
        var path = Path.Combine(store.Root, id.ToString("N"), "style-plan.json");
        if (File.Exists(path))
        {
            using var previous = JsonDocument.Parse(await File.ReadAllBytesAsync(path, token));
            if (!previous.RootElement.GetProperty("shots").EnumerateArray().Select(s => s.GetString()).SequenceEqual(plan))
                throw new IOException("La répartition enregistrée a changé. Aucune génération supplémentaire n’a été lancée.");
        }
        else await SessionStore.WriteAtomicAsync(path, JsonSerializer.SerializeToUtf8Bytes(new { selected, shots = plan }), token);
        return Array.AsReadOnly(plan);
    }

    public async Task<TransformResult> TransformAsync(Guid sessionId, IReadOnlyList<byte[]> originals,
        IProgress<string> progress, CancellationToken token, string styleChoice = StyleCatalog.Chef, IReadOnlyList<string>? styleChoices = null)
    {
        var options = new SessionOptions(originals.Count, PhotoMode.Surprise, styleChoice: styleChoice, styleChoices: styleChoices);
        metricOptions[sessionId] = options;
        var plan = await EnsureSession(sessionId, originals.Count, PhotoMode.Surprise, token, options.StyleChoices);
        var shareToken = await ShareTokenAsync(sessionId, token);
        using (var gallery = await SendJsonAsync(HttpMethod.Post, $"sessions/{sessionId}/gallery", new { token = shareToken }, token))
            GalleryAvailable?.Invoke(sessionId, GalleryLink(gallery.RootElement, shareToken));
        var inputs = await Task.WhenAll(originals.Select(bytes => Task.Run(() => PhotoProcessor.PrepareGenerationInput(bytes), token)));
        var inputsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uploadedInputs = 0;
        async Task ArchiveOriginalsAsync()
        {
            await inputsReady.Task.WaitAsync(token);
            await Task.WhenAll(originals.Select((bytes, index) => UploadOriginalAsync(sessionId, index, bytes, token)));
        }
        var archivalUpload = ArchiveOriginalsAsync();
        var tasks = originals.Select(async (bytes, index) =>
        {
            var acquired = false;
            try
            {
                await GenerationSlots.WaitAsync(token); acquired = true;
                await UploadAsync(sessionId, $"input-{index + 1}.jpg", inputs[index], token);
                if (Interlocked.Increment(ref uploadedInputs) == originals.Count) inputsReady.TrySetResult();
                progress.Report($"Transformation de la pose {index + 1} sur {originals.Count}…");
                var route = $"sessions/{sessionId}/photos/{index}";
                using var initial = await SendJsonAsync(HttpMethod.Get, route + "?transport=direct", null, token);
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
                    using var status = await SendJsonAsync(HttpMethod.Get, route + "?transport=direct", null, token);
                    state = status.RootElement.GetProperty("state").GetString();
                    if (state == "ready")
                    {
                        // The server's generation slot is already free. Let the
                        // next guest start while these bytes download for print.
                        GenerationSlots.Release(); acquired = false;
                        var image = await DownloadAsync(status.RootElement.GetProperty("url").GetString()!, token, sessionId, index);
                        if (status.RootElement.TryGetProperty("asset_stored", out var stored) && stored.GetBoolean())
                        {
                            if (!status.RootElement.TryGetProperty("sha256", out var digest) || digest.GetString() != SessionStore.Hash(image))
                                throw new IOException("La vérification du résultat téléchargé a échoué. Les originaux sont conservés.");
                            await RememberUploadAsync(sessionId, $"photo-{index + 1}.jpg", image, token);
                            return image; // exact provider JPEG, already stored beside the gallery
                        }
                        var normalized = await Task.Run(() => PhotoProcessor.Normalize(image), token);
                        await UploadAsync(sessionId, $"photo-{index + 1}.jpg", normalized, token);
                        return normalized;
                    }
                    if (state is "failed" or "uncertain" || (state == "queued" && attempt > 4))
                        throw new IOException("La transformation n’a pas abouti. Vos originaux sont conservés ; aucune nouvelle génération automatique.");
                    await delay(TimeSpan.FromSeconds(2), token);
                }
                throw new IOException("La génération prend trop de temps. Les originaux et l’identifiant de suivi sont conservés.");
            }
            catch (Exception exception) { inputsReady.TrySetException(exception); throw; }
            finally { if (acquired) GenerationSlots.Release(); }
        }).ToArray();
        await Task.WhenAll(tasks.Cast<Task>().Append(archivalUpload));
        return new TransformResult(plan.Distinct().Count() == 1 ? plan[0] : StyleCatalog.Mixed, tasks.Select(task => task.Result).ToArray(), Normalized: true, ShotStyles: plan);
    }

    public async Task<PublishedSession> PublishAsync(Session session, IReadOnlyList<StoredAsset> photos, StoredAsset sheet, CancellationToken token = default)
    {
        if (session.Stage != SessionStage.Ready || photos.Count != session.Options.PhotoCount ||
            !session.Results.Select(r => r.AssetKey).SequenceEqual(photos.Select(p => p.Key)) || session.SheetAssetKey != sheet.Key)
            throw new InvalidOperationException("Le partage doit correspondre exactement à l’aperçu validé.");
        metricOptions[session.Id]=session.Options;
        await EnsureSession(session.Id, photos.Count, session.Options.Mode, token, session.Options.StyleChoices);
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
        if (!session.UsedOriginalFallback)
            for (var i = 0; i < images.Count; i++) await UploadAsync(session.Id, $"photo-{i+1}.jpg", images[i], token);
        await UploadAsync(session.Id, "sheet.jpg", sheetBytes, token);
        // The server assembles the archive from verified Storage assets. Sending
        // another full copy of originals/results over mobile data is unnecessary.
        var shareToken = await ShareTokenAsync(session.Id, token);
        using var shared = await SendJsonAsync(HttpMethod.Post, $"sessions/{session.Id}/share", new { token = shareToken, used_original_fallback = session.UsedOriginalFallback }, token);
        var downloadUrl = shared.RootElement.GetProperty("download_url").GetString()!;
        var result = GalleryLink(shared.RootElement, shareToken) with { DownloadUrl = downloadUrl };
        await SessionStore.WriteAtomicAsync(Path.Combine(store.Root, session.Id.ToString("N"), "share.json"), JsonSerializer.SerializeToUtf8Bytes(result), token);
        return result;
    }

    private async Task<string> ShareTokenAsync(Guid id, CancellationToken token)
    {
        var path = Path.Combine(store.Root, id.ToString("N"), "share-token.txt");
        if (File.Exists(path)) return (await File.ReadAllTextAsync(path, token)).Trim();
        var value = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        await SessionStore.WriteAtomicAsync(path, Encoding.ASCII.GetBytes(value), token);
        return value;
    }

    private PublishedSession GalleryLink(JsonElement response, string token)
    {
        var url = galleryBase.Length > 0 ? galleryBase.TrimEnd('/') + "/#" + token : response.GetProperty("gallery_url").GetString()!;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) || address.Scheme != "https" || address.UserInfo.Length > 0)
            throw new IOException("Adresse de partage non reconnue.");
        return new PublishedSession(url, response.GetProperty("expires_at").GetDateTimeOffset());
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
        var marker = UploadMarker(id, name); var digest = SessionStore.Hash(bytes);
        if (File.Exists(marker) && await File.ReadAllTextAsync(marker, token) == digest) return;
        await UploadSlots.WaitAsync(token);
        var started=Stopwatch.GetTimestamp(); long? elapsed=null; var outcome="failed";
        try
        {
        using var content = new ByteArrayContent(bytes); content.Headers.ContentType = new(name.EndsWith(".zip") ? "application/zip" : "image/jpeg");
        using var response = await SendAsync(HttpMethod.Put, $"sessions/{id}/assets/{name}", content, token);
        await EnsureSuccess(response);
        var replyBytes=await response.Content.ReadAsByteArrayAsync(token); elapsed=(long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        using var result = JsonDocument.Parse(replyBytes);
        if (result.RootElement.GetProperty("sha256").GetString() != digest)
            throw new IOException("La vérification du fichier envoyé a échoué.");
        await RememberUploadAsync(id, name, bytes, token); outcome="success";
        }
        finally { UploadSlots.Release(); await ObserveTransferAsync(id,"upload",name,elapsed ?? (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,bytes.LongLength,outcome); }
    }
    private string UploadMarker(Guid id, string name) => Path.Combine(store.Root, id.ToString("N"), $"asset-upload-{name}.sha256");
    private Task RememberUploadAsync(Guid id, string name, byte[] bytes, CancellationToken token) =>
        SessionStore.WriteAtomicAsync(UploadMarker(id, name), Encoding.ASCII.GetBytes(SessionStore.Hash(bytes)), token);
    private async Task<byte[]> DownloadAsync(string address, CancellationToken token, Guid sessionId, int index)
    {
        var uri = new Uri(address);
        if (uri.Scheme != "https" || uri.Host != api.Host || uri.Port != api.Port) throw new IOException("Adresse de résultat non reconnue.");
        await DownloadSlots.WaitAsync(token);
        var started=Stopwatch.GetTimestamp(); long? elapsed=null; long transferred=0; var outcome="failed";
        try
        {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri); // No booth credential on signed storage requests.
        using var response = await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > PhotoProcessor.MaxEncodedBytes) throw new IOException("Image distante trop volumineuse.");
        await using var stream=await response.Content.ReadAsStreamAsync(token);using var memory=new MemoryStream();
        var buffer=new byte[65536];int read;
        while((read=await stream.ReadAsync(buffer,token))>0)
        { if(memory.Length+read>PhotoProcessor.MaxEncodedBytes)throw new IOException("Image distante trop volumineuse.");await memory.WriteAsync(buffer.AsMemory(0,read),token); transferred+=read; }
        elapsed=(long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var bytes=memory.ToArray();using var decoded=PhotoProcessor.DecodeUpright(bytes);outcome="success";return bytes;
        }
        finally { DownloadSlots.Release(); await ObserveTransferAsync(sessionId,"download",$"photo-{index+1}.jpg",elapsed ?? (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,transferred,outcome); }
    }
    private Task ObserveTransferAsync(Guid id,string kind,string asset,long milliseconds,long bytes,string outcome) =>
        Telemetry is null || !metricOptions.TryGetValue(id,out var options) ? Task.CompletedTask :
        Telemetry.RecordAsync(Telemetry.Metric(id,options,kind,milliseconds:milliseconds,bytes:bytes,asset:asset,outcome:outcome));
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
            "generation_disabled"=>"Les métamorphoses sont en pause. Les photos au naturel restent disponibles.",
            "event_quota_reached"=>"La limite de métamorphoses de la soirée est atteinte. Les photos au naturel restent disponibles.",
            "generation_rate_limited"=>"Les métamorphoses vont un peu vite. Attendez une minute avant une nouvelle séance. Vos originaux sont conservés.",
            "generation_busy"=>"Des métamorphoses sont encore en cours. Vos originaux sont conservés. Prévenez la personne qui s’occupe de la borne.",
            "provider_rate_limited"=>"OpenAI reçoit trop de demandes pour le moment. Vos originaux sont conservés. Attendez une minute avant une nouvelle séance.",
            "generation_uncertain_no_auto_retry"=>"La génération est incertaine. Aucun nouvel appel payant n’a été relancé automatiquement.",
            "archive_too_large"=>"Le téléchargement complet dépasse 50 Mo. Vos photos restent disponibles individuellement et sur cet ordinateur.",
            _=>$"Service momentanément indisponible ({(int)response.StatusCode}, {code??"réseau"}). Les photos restent enregistrées localement."
        });
    }
    public void Dispose()=>http.Dispose();
}

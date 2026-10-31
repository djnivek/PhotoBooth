using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;

internal static class ProgressiveSmoke
{
    private const string Api = "https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api";

    internal static async Task RunAsync()
    {
        string key;
        if (File.Exists(".local/booth-provisioning.json"))
        {
            using var provision = JsonDocument.Parse(await File.ReadAllTextAsync(".local/booth-provisioning.json"));
            if (provision.RootElement.GetProperty("api_base_url").GetString()?.TrimEnd('/') != Api)
                throw new InvalidOperationException("Provisioning does not target PhotoBooth.");
            key = provision.RootElement.GetProperty("access_key").GetString()!;
        }
        else
        {
            var appRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EstellArt");
            if (BoothSettings.Load(Path.Combine(appRoot, "booth-settings.json")).ApiBaseUrl.TrimEnd('/') != Api)
                throw new InvalidOperationException("Windows settings do not target PhotoBooth.");
            key = CredentialStore.Load(Path.Combine(appRoot, "cloud-credential"));
        }
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("No provisioned booth credential.");

        var output = Path.Combine(Environment.CurrentDirectory, "artifacts", "direct-square-0.6.0", "live-smoke");
        var store = new SessionStore(output);
        await using var controller = new BoothController(store, () => new FixtureCamera(store), new RehearsalTransform(), (_, _) => Task.CompletedTask);
        await controller.BeginAsync(new SessionOptions(4, PhotoMode.Surprise, styleChoice: "halloween"));
        await controller.TakeAsync();
        if (controller.Session?.Stage != SessionStage.Ready) throw new InvalidOperationException(controller.Message);
        var id = controller.Session.Id;
        using var http = new HttpClient(new NeverGenerateHandler()) { Timeout = TimeSpan.FromSeconds(45) };
        using var client = new BoothCloudClient(Api, key, store, handler: new NeverGenerateHandler());
        var checks = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidDataException(name);
            checks.Add(name);
        }

        async Task<JsonDocument> Send(HttpMethod method, string path, object? body = null, byte[]? bytes = null, string? bearer = null, bool publicRequest = false)
        {
            using var request = new HttpRequestMessage(method, Api + path);
            if (!publicRequest) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer ?? key);
            if (bytes is not null)
            {
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new("image/jpeg");
            }
            else if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
        }

        using (var health = await Send(HttpMethod.Get, "/health", publicRequest: true))
            Check(health.RootElement.GetProperty("api_version").GetString() == "0.6.0", "deployed API is 0.6.0");
        Check(await client.CheckAsync(), "booth authentication and OpenAI key presence");
        using (var session = await Send(HttpMethod.Post, "/sessions", new { id, photo_count = 4, mode = "surprise", style = "halloween" }))
            Check(session.RootElement.GetProperty("id").GetGuid() == id, "synthetic session created without generation");
        var capability = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        await SessionStore.WriteAtomicAsync(Path.Combine(store.Root, id.ToString("N"), "share-token.txt"), Encoding.ASCII.GetBytes(capability));
        string earlyUrl;
        using (var gallery = await Send(HttpMethod.Post, $"/sessions/{id}/gallery", new { token = capability }))
        {
            earlyUrl = gallery.RootElement.GetProperty("gallery_url").GetString()!;
            Check(earlyUrl == "https://colors-party-estelle.vercel.app/#" + capability, "early QR points to the canonical gallery");
        }
        Task<JsonDocument> Resolve() => Send(HttpMethod.Post, "/downloads/resolve", new { token = capability }, publicRequest: true);
        using (var empty = await Resolve())
            Check(empty.RootElement.GetProperty("state").GetString() == "processing" && empty.RootElement.GetProperty("assets").GetArrayLength() == 0,
                "gallery resolves while empty and processing");

        for (var i = 0; i < 4; i++)
        {
            var original = await File.ReadAllBytesAsync(store.Resolve(controller.Session.Captures[i].LocalAssetKey));
            using var input = await Send(HttpMethod.Put, $"/sessions/{id}/assets/input-{i + 1}.jpg", bytes: PhotoProcessor.PrepareGenerationInput(original));
        }
        using (var privateInputs = await Resolve())
            Check(privateInputs.RootElement.GetProperty("assets").GetArrayLength() == 0, "generation inputs are private and absent from guest gallery");

        var expected = new Dictionary<string, byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var name = $"original-{i + 1}.jpg";
            var bytes = await File.ReadAllBytesAsync(store.Resolve(controller.Session.Captures[i].LocalAssetKey));
            expected[name] = bytes;
            using var uploaded = await Send(HttpMethod.Put, $"/sessions/{id}/assets/{name}", bytes: bytes);
            Check(uploaded.RootElement.GetProperty("sha256").GetString() == SessionStore.Hash(bytes), $"original {i + 1} upload verified");
        }
        using (var originals = await Resolve())
            Check(originals.RootElement.GetProperty("assets").GetArrayLength() == 4 && originals.RootElement.GetProperty("completed_count").GetInt32() == 0,
                "originals are available before transformations");

        for (var i = 0; i < 4; i++) expected[$"photo-{i + 1}.jpg"] = await store.ReadVerifiedAsync(controller.FinalAssets[i]);
        using (var first = await Send(HttpMethod.Put, $"/sessions/{id}/assets/photo-1.jpg", bytes: expected["photo-1.jpg"]))
            Check(first.RootElement.GetProperty("sha256").GetString() == controller.FinalAssets[0].Sha256, "first local-effect result upload verified");
        using (var partial = await Resolve())
            Check(partial.RootElement.GetProperty("state").GetString() == "processing" && partial.RootElement.GetProperty("completed_count").GetInt32() == 1 && partial.RootElement.GetProperty("assets").GetArrayLength() == 5,
                "first result arrives while the session is still processing");

        using (var guestRequest = new HttpRequestMessage(HttpMethod.Get, Api + "/booth/check"))
        {
            guestRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", capability);
            using var rejected = await http.SendAsync(guestRequest);
            Check(rejected.StatusCode == HttpStatusCode.Unauthorized, "guest capability cannot authenticate as a booth");
        }

        var published = await client.PublishAsync(controller.Session, controller.FinalAssets, controller.Sheet!);
        Check(published.Url == earlyUrl, "same QR before and after final publication");
        expected["sheet.jpg"] = await store.ReadVerifiedAsync(controller.Sheet!);
        expected["archive.zip"] = BoothCloudClient.BuildArchive(Enumerable.Range(1, 4).Select(i => expected[$"photo-{i}.jpg"]).ToArray(), expected["sheet.jpg"], Enumerable.Range(1, 4).Select(i => expected[$"original-{i}.jpg"]).ToArray());
        using (var complete = await Resolve())
        {
            Check(complete.RootElement.GetProperty("state").GetString() == "ready" && complete.RootElement.GetProperty("assets").GetArrayLength() == 10, "completed gallery contains all ten assets");
            foreach (var asset in complete.RootElement.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString()!;
                var uri = new Uri(asset.GetProperty("url").GetString()!);
                Check(uri.Scheme == "https" && uri.Host == "frkckrvsdsiwivdfyjau.supabase.co" && uri.AbsolutePath.Contains($"/booth-private/{id}/"), $"signed address for {name}");
                var bytes = await http.GetByteArrayAsync(uri);
                if (name == "archive.zip")
                {
                    using var zip = new ZipArchive(new MemoryStream(bytes));
                    Check(zip.Entries.Count == 9, "server archive contains every original, result and sheet");
                    foreach (var entry in zip.Entries)
                    {
                        var source = entry.FullName.StartsWith("originaux/") ? entry.FullName.Replace("originaux/photo-", "original-") : entry.FullName == "planche-4x6.jpg" ? "sheet.jpg" : entry.FullName;
                        using var stream = entry.Open(); using var memory = new MemoryStream(); await stream.CopyToAsync(memory);
                        Check(memory.ToArray().SequenceEqual(expected[source]), $"exact archived bytes for {entry.FullName}");
                    }
                }
                else Check(expected.TryGetValue(name, out var original) && bytes.SequenceEqual(original), $"exact downloaded bytes for {name}");
            }
        }
        using var archive = new ZipArchive(new MemoryStream(expected["archive.zip"]));
        Check(archive.Entries.Count == 9, "archive has four results, four originals and sheet");
        var replay = await client.PublishAsync(controller.Session, controller.FinalAssets, controller.Sheet!);
        Check(replay.Url == earlyUrl, "publication replay retains the early QR");
        var receipt = new { utc = DateTimeOffset.UtcNow, session_id = id, source = "synthetic calibration and local effect; NOT OpenAI output", generation_calls = 0,
            early_gallery = true, progressive_result = true, unchanged_qr = true, exact_assets_verified = 10, checks, success = true };
        await File.WriteAllTextAsync(Path.Combine(output, "receipt.json"), JsonSerializer.Serialize(receipt, SessionStore.JsonOptions));
        Console.WriteLine(JsonSerializer.Serialize(new { success = true, checks = checks.Count, generation_calls = 0, exact_assets_verified = 10, early_gallery = true, unchanged_qr = true }));
    }

    private sealed class NeverGenerateHandler : DelegatingHandler
    {
        internal NeverGenerateHandler() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.Host != "frkckrvsdsiwivdfyjau.supabase.co" || request.RequestUri.AbsolutePath.Contains("/photos/"))
                throw new InvalidOperationException("A non-PhotoBooth or generation request was blocked by the synthetic test.");
            return base.SendAsync(request, token);
        }
    }
}

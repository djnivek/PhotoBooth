using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhotoBooth.Imaging;

namespace PhotoBooth.Runtime;

// Durable release intents contain session IDs, never photos or a bearer credential.
public sealed class BoothProjection : IAsyncDisposable
{
    private readonly string directory, credential;
    private readonly HttpClient http;
    private readonly SemaphoreSlim gate = new(1);
    private readonly CancellationTokenSource stop = new();
    private readonly Task worker;
    private bool enabled, includeTests;
    private long revision;
    private long synced = -1;
    private string viewerToken;
    public string ViewerUrl => viewerToken.Length == 64 ? "https://colors-party-estelle.vercel.app/viewer/#" + viewerToken : "";
    public int PendingCount => Directory.Exists(directory) ? Directory.EnumerateFiles(directory,"*.json").Count() : 0;
    public BoothProjection(string root,string api,string key,bool enabled,bool includeTests,HttpMessageHandler? handler=null,bool automatic=true)
    {
        var address=new Uri(api.TrimEnd('/')+"/");
        if(address.Scheme!="https" || string.IsNullOrWhiteSpace(key))throw new ArgumentException("Connexion HTTPS de la borne requise.");
        directory=Path.Combine(root,"projection",SessionStore.Hash(Encoding.UTF8.GetBytes(address.AbsoluteUri+key))[..16]);
        credential=Path.Combine(directory,"viewer-credential");
        viewerToken=CredentialStore.Load(credential);
        this.enabled=enabled;this.includeTests=includeTests;
        http=new(handler??new HttpClientHandler {AllowAutoRedirect=false}) {BaseAddress=address,Timeout=TimeSpan.FromSeconds(8)};
        http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",key);
        worker=automatic?Task.Run(PumpAsync):Task.CompletedTask;
    }
    public async Task ConfigureAsync(bool value,bool tests)
    {
        await gate.WaitAsync();
        try {enabled=value;includeTests=tests;Interlocked.Increment(ref revision);await EnsureTokenAsync();}
        finally {gate.Release();}
    }
    private async Task EnsureTokenAsync()
    {
        if(!enabled || viewerToken.Length>0)return;
        var value=Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        await CredentialStore.SaveAsync(credential,value);viewerToken=value;
    }
    public async Task ReleaseAsync(Guid id,string source)
    {
        if(!enabled || !new[]{"camera","fixture"}.Contains(source))return;
        try
        {
            Directory.CreateDirectory(directory);
            var file=Path.Combine(directory,id.ToString("N")+".json");
            if(!File.Exists(file) && PendingCount<2000)
                await SessionStore.WriteAtomicAsync(file,JsonSerializer.SerializeToUtf8Bytes(new Release(id,source,DateTimeOffset.UtcNow)));
        }
        catch(Exception) { /* A projection failure must not interrupt the booth. */ }
    }
    public async Task FlushAsync(CancellationToken token=default)
    {
        if(!await gate.WaitAsync(0,token))return;
        try
        {
            using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,stop.Token);
            await EnsureTokenAsync();
            if(viewerToken.Length!=64)return;
            var current=Interlocked.Read(ref revision);
            if(synced!=current)
            {
                using var config=await http.PostAsJsonAsync("viewer/settings",new {token=viewerToken,enabled,include_tests=includeTests},linked.Token);
                if(!await Acknowledged(config,linked.Token))return;
                synced=current;
            }
            if(!enabled)return;
            var files=Directory.Exists(directory)?Directory.EnumerateFiles(directory,"*.json").Order().Take(20).ToArray():[];
            foreach(var file in files)
            {
                try
                {
                    var item=JsonSerializer.Deserialize<Release>(await File.ReadAllBytesAsync(file,linked.Token));
                    if(item is null)continue;
                    if(item.At<DateTimeOffset.UtcNow.AddDays(-7)){File.Delete(file);continue;}
                    using var response=await http.PostAsJsonAsync($"sessions/{item.Id}/viewer",new {source=item.Source},linked.Token);
                    if(await Acknowledged(response,linked.Token))File.Delete(file);
                }
                catch(JsonException) { File.Move(file,Path.ChangeExtension(file,"invalid")); }
            }
        }
        catch(Exception) { /* Retry the same idempotent release, never photo effects. */ }
        finally {gate.Release();}
    }
    private static async Task<bool> Acknowledged(HttpResponseMessage response,CancellationToken token)
    {
        if(!response.IsSuccessStatusCode)return false;
        using var json=JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        return json.RootElement.TryGetProperty("accepted",out var accepted)&&accepted.ValueKind==JsonValueKind.True;
    }
    private async Task PumpAsync()
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(5));
        try {await FlushAsync();while(await timer.WaitForNextTickAsync(stop.Token))await FlushAsync();}
        catch(OperationCanceledException) { }
    }
    public async ValueTask DisposeAsync(){stop.Cancel();http.CancelPendingRequests();await worker;http.Dispose();stop.Dispose();}
    private sealed record Release(Guid Id,string Source,DateTimeOffset At);
}

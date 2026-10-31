using PhotoBooth.Imaging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PhotoBooth.Core;

namespace PhotoBooth.Runtime;

public sealed record BoothMetric(Guid Id, Guid SessionId, string Kind, DateTimeOffset OccurredAt,
    string Mode, int PhotoCount, string RequestedStyle, string Source, string? Style = null,
    int ShotIndex = -1, long? DurationMs = null, long? Bytes = null, string? Asset = null, string? Outcome = null);

// A small durable outbox. Failures here must never interrupt capture, sharing or printing.
public sealed class BoothTelemetry : IAsyncDisposable
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private readonly string directory;
    private readonly HttpClient http;
    private readonly string key;
    private readonly SemaphoreSlim gate = new(1);
    private readonly CancellationTokenSource stop = new();
    private readonly Task worker;
    public string Source { get; }
    public int PendingCount => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json").Count() : 0;
    public BoothTelemetry(string root, string api, string key, string source, HttpMessageHandler? handler = null, bool automatic = true)
    {
        if (!Uri.TryCreate(api.TrimEnd('/')+"/",UriKind.Absolute,out var address) || address.Scheme!="https") throw new ArgumentException("HTTPS API required");
        directory=Path.Combine(root,"monitoring",SessionStore.Hash(System.Text.Encoding.UTF8.GetBytes(address.AbsoluteUri))[..16]);
        this.key=key; Source=source;
        http=new(handler ?? new HttpClientHandler { AllowAutoRedirect=false }) { BaseAddress=address,Timeout=TimeSpan.FromSeconds(8) };
        http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",key);
        worker=automatic ? Task.Run(PumpAsync) : Task.CompletedTask;
    }
    public BoothMetric Metric(Guid id, SessionOptions options, string kind, int shot=-1, long? milliseconds=null, long? bytes=null, string? asset=null, string? outcome=null, string? style=null) =>
        new(Guid.NewGuid(),id,kind,DateTimeOffset.UtcNow,options.Mode==PhotoMode.Surprise?"surprise":"original",options.PhotoCount,
            options.Mode==PhotoMode.Surprise?options.StyleChoice:"original",Source,style,shot,milliseconds,bytes,asset,outcome);
    public async Task RecordAsync(BoothMetric metric)
    {
        try
        {
            Directory.CreateDirectory(directory);
            // Bounded disk use even if this PC never reconnects. Nothing is silently purged.
            if (PendingCount>=20000) { await File.WriteAllTextAsync(Path.Combine(directory,"overflow.flag"),"Monitoring outbox full: new measurements could not be recorded. Reconnect and inspect the queue."); return; }
            await SessionStore.WriteAtomicAsync(Path.Combine(directory,metric.Id.ToString("N")+".json"),JsonSerializer.SerializeToUtf8Bytes(metric,Json));
        }
        catch (Exception) { /* Photography has priority; no secret/error payload is logged. */ }
    }
    public async Task FlushAsync(CancellationToken token=default)
    {
        if(key.Length==0 || stop.IsCancellationRequested || !await gate.WaitAsync(0,token))return;
        try
        {
            using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,stop.Token);
            for(var batch=0;batch<10;batch++)
            {
                var files=Directory.Exists(directory)?Directory.EnumerateFiles(directory,"*.json").Order().Take(10).ToArray():[];
                if(files.Length==0)return;
                var metrics=new List<BoothMetric>();
                foreach(var file in files)
                {
                    try { metrics.Add(JsonSerializer.Deserialize<BoothMetric>(await File.ReadAllBytesAsync(file,linked.Token),Json)!); }
                    catch(JsonException) { File.Move(file,Path.ChangeExtension(file,"invalid")); }
                }
                if(metrics.Count!=files.Length)continue;
                using var response=await http.PostAsJsonAsync("monitoring/booth",new { events=metrics },Json,linked.Token);
                if(!response.IsSuccessStatusCode)return;
                // Check the acknowledgement before removing local measurements.
                using var ack=JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(linked.Token));
                if(!ack.RootElement.TryGetProperty("accepted",out var accepted)||accepted.GetInt32()!=metrics.Count)return;
                foreach(var file in files)File.Delete(file);
            }
        }
        catch(Exception) { /* Retain all unacknowledged events for a later retry. */ }
        finally {gate.Release();}
    }
    private async Task PumpAsync()
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(15));
        try { await FlushAsync(); while(await timer.WaitForNextTickAsync(stop.Token))await FlushAsync(); }
        catch(OperationCanceledException) { }
    }
    public async ValueTask DisposeAsync() { stop.Cancel(); http.CancelPendingRequests(); await worker; http.Dispose(); stop.Dispose(); }
}

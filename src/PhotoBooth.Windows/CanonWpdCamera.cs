using System.Runtime.CompilerServices;
using System.Threading.Channels;
using PhotoBooth.Core;
using PhotoBooth.Imaging;

namespace PhotoBooth.Windows;

/// <summary>Injectable boundary for testing sequencing without touching Windows or USB.</summary>
public interface ICanonConnection : IAsyncDisposable
{
    event Action<uint>? ObjectReady;
    Task<string> OpenAsync(CancellationToken token);
    Task StartPreviewAsync(CancellationToken token);
    Task StopPreviewAsync(CancellationToken token);
    Task<byte[]?> ReadPreviewAsync(CancellationToken token);
    Task PrepareFocusAsync(bool manualFocusConfirmed, CancellationToken token);
    Task TriggerAsync(CancellationToken token);
    Task<string?> FileNameAsync(uint handle,CancellationToken token);
    Task<byte[]> DownloadAsync(uint handle,CancellationToken token);
    Task CompleteAsync(uint handle,CancellationToken token);
    Task CancelTransferAsync(uint handle,CancellationToken token);
}

/// <summary>Windows' existing WPD/MTP driver; no proprietary Canon DLL and no driver replacement.</summary>
public sealed class CanonWpdCamera(SessionStore store,ICanonConnection? connection=null,TimeSpan? captureTimeout=null, bool manualFocusConfirmed=false) : IPhotoCamera
{
    private readonly ICanonConnection link=connection??new FcCanonConnection();
    private readonly Channel<uint> objects=Channel.CreateUnbounded<uint>(new UnboundedChannelOptions {SingleReader=true});
    private bool connected,preview,prepared;
    private readonly SemaphoreSlim previewCommands = new(1, 1);
    private int capturing;
    private bool disposed;

    public async Task<CameraStatus> ConnectAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(connected)throw new InvalidOperationException("Le Canon est déjà connecté.");
        link.ObjectReady+=OnObject;
        var model=await link.OpenAsync(cancellationToken); connected=true;
        return new(true,model,true,true);
    }
    private void OnObject(uint handle)=>objects.Writer.TryWrite(handle);

    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        if(!connected||preview||Volatile.Read(ref capturing)!=0)throw new InvalidOperationException("La caméra n’est pas disponible pour la visée.");
        preview=true; prepared=false;
        try
        {
            await link.StartPreviewAsync(cancellationToken);
            var lastFrame=DateTimeOffset.UtcNow;
            while(!cancellationToken.IsCancellationRequested)
            {
                byte[]? jpeg;
                await previewCommands.WaitAsync(cancellationToken);
                try { jpeg=await link.ReadPreviewAsync(cancellationToken); }
                finally { previewCommands.Release(); }
                if(jpeg is {Length:>0}) {lastFrame=DateTimeOffset.UtcNow;yield return new(jpeg,DateTimeOffset.UtcNow);}
                else if(DateTimeOffset.UtcNow-lastFrame>TimeSpan.FromSeconds(8))throw new IOException("Le Canon ne fournit plus de visée. Vérifiez son écran et la connexion USB.");
                await Task.Delay(80,cancellationToken);
            }
        }
        finally
        {
            using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try {await link.StopPreviewAsync(stop.Token);} finally {preview=false;}
        }
    }

    public async Task PrepareCaptureAsync(CancellationToken token)
    {
        if (!connected || !preview) throw new InvalidOperationException("La visée est nécessaire pour préparer la photo.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(12));
        await previewCommands.WaitAsync(deadline.Token);
        try { await link.PrepareFocusAsync(manualFocusConfirmed, deadline.Token); prepared=true; }
        finally { previewCommands.Release(); }
    }

    public async Task<CapturedPhoto> CaptureAsync(Guid sessionId,int shotIndex,CancellationToken cancellationToken)
    {
        if(!connected||preview||!prepared)throw new InvalidOperationException("La préparation doit réussir et la visée doit être arrêtée avant la photo.");
        if(Interlocked.CompareExchange(ref capturing,1,0)!=0)throw new InvalidOperationException("Une capture est déjà en cours.");
        prepared=false;
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(captureTimeout??TimeSpan.FromSeconds(35));
        try
        {
            while(objects.Reader.TryRead(out var stale))await link.CancelTransferAsync(stale,deadline.Token);
            await link.TriggerAsync(deadline.Token); // Exactly one shutter request; never retry an ambiguous command.
            var rawSeen=false;
            while(true)
            {
                var handle=await objects.Reader.ReadAsync(deadline.Token);
                var name=await link.FileNameAsync(handle,deadline.Token);
                if(name is not null&&!name.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)&&!name.EndsWith(".jpeg",StringComparison.OrdinalIgnoreCase))
                {rawSeen=true;await link.CancelTransferAsync(handle,deadline.Token);continue;}
                try
                {
                    var jpeg=await link.DownloadAsync(handle,deadline.Token);
                    var bytes=PhotoProcessor.Normalize(jpeg);
                    var asset=await store.SavePhotoAsync(sessionId,bytes,deadline.Token);
                    await link.CompleteAsync(handle,deadline.Token);
                    return new(Guid.NewGuid(),asset.Key);
                }
                catch(InvalidDataException) when(rawSeen)
                {throw new IOException("Réglez le Canon sur JPEG haute qualité. Le fichier reçu n’est pas un JPEG exploitable.");}
            }
        }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested)
        {throw new TimeoutException("Aucun JPEG reçu du Canon en 35 secondes. Vérifiez JPEG, mise au point et USB. Aucun deuxième déclenchement n’a été envoyé.");}
        finally {Interlocked.Exchange(ref capturing,0);}
    }
    public async ValueTask DisposeAsync()
    {
        if(disposed)return;disposed=true;connected=false;
        link.ObjectReady-=OnObject;objects.Writer.TryComplete();await link.DisposeAsync();
    }
}

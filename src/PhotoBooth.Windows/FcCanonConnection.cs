using FC.SDK.Canon;
using PhotoBooth.Imaging;
using FcCamera=FC.SDK.CanonCamera;

namespace PhotoBooth.Windows;

internal sealed class FcCanonConnection : ICanonConnection
{
    private FcCamera? camera;
    public event Action<uint>? ObjectReady;
    private FcCamera Camera=>camera??throw new InvalidOperationException("Le Canon n’est pas connecté.");
    public async Task<string> OpenAsync(CancellationToken token)
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("La connexion Canon USB demande Windows.");
        var devices=await Task.Run(()=>
        {
            if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException();
            return FcCamera.EnumerateWpdCameras().ToArray();
        },token);
        if(devices.Length!=1)throw new IOException(devices.Length==0?
            "Aucun Canon USB détecté. Allumez-le, branchez-le et fermez DSLR Booth/EOS Utility.":
            "Plusieurs appareils Canon détectés. Gardez uniquement celui du photomaton branché.");
        camera=await FcCamera.ConnectWpdAutoAsync(devices[0].DeviceId,token);
        camera.ObjectAdded+=OnObject;
        Check(await camera.OpenSessionAsync(token),"ouvrir le Canon");
        camera.StartEventPolling();
        Check(await camera.SetSaveToAsync(EdsSaveTo.Host,token),"enregistrer les photos sur ce PC");
        return camera.Model??devices[0].FriendlyName;
    }
    private void OnObject(object? sender,FC.SDK.CanonObjectAddedEventArgs args)=>ObjectReady?.Invoke(args.ObjectHandle);
    public async Task StartPreviewAsync(CancellationToken token)=>Check(await Camera.StartLiveViewAsync(token),"démarrer la visée");
    public async Task StopPreviewAsync(CancellationToken token)=>Check(await Camera.StopLiveViewAsync(token),"arrêter la visée avant la capture");
    public async Task<byte[]?> ReadPreviewAsync(CancellationToken token)
    {
        var (error,jpeg)=await Camera.GetLiveViewFrameAsync(token);
        if(error is EdsError.DeviceBusy or EdsError.ObjectNotReady)return null;
        Check(error,"recevoir la visée");return jpeg;
    }
    public async Task TriggerAsync(CancellationToken token)=>Check(await Camera.TakePictureAsync(token),"déclencher la photo");
    public async Task<string?> FileNameAsync(uint handle,CancellationToken token)
    {var (error,name)=await Camera.GetObjectFileNameAsync(handle,token);Check(error,"identifier la photo");return name;}
    public async Task<byte[]> DownloadAsync(uint handle,CancellationToken token)
    {
        using var destination=new LimitedPhotoStream();
        Check(await Camera.DownloadAsync(handle,destination,token),"transférer la photo complète");
        return destination.ToArray();
    }
    public async Task CompleteAsync(uint handle,CancellationToken token)=>Check(await Camera.TransferCompleteAsync(handle,token),"confirmer le transfert");
    public async Task CancelTransferAsync(uint handle,CancellationToken token)=>Check(await Camera.CancelTransferAsync(handle,token),"libérer le transfert précédent");
    public async ValueTask DisposeAsync()
    {
        if(camera is null)return;
        var current=camera;camera=null;current.ObjectAdded-=OnObject;
        try {await current.StopEventPollingAsync();using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await current.CloseSessionAsync(stop.Token);}
        finally {await current.DisposeAsync();}
    }
    private static void Check(EdsError error,string action)
    {if(error!=EdsError.OK)throw new IOException($"Impossible de {action} ({error}). Vérifiez le Canon et fermez les autres logiciels caméra.");}
    private sealed class LimitedPhotoStream:MemoryStream
    {
        private void Guard(int count){if(Position+count>PhotoProcessor.MaxEncodedBytes)throw new InvalidDataException("Le fichier Canon dépasse la limite autorisée. Utilisez le mode JPEG.");}
        public override void Write(byte[] buffer,int offset,int count){Guard(count);base.Write(buffer,offset,count);}
        public override void Write(ReadOnlySpan<byte> buffer){Guard(buffer.Length);base.Write(buffer);}
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,CancellationToken cancellationToken=default){Guard(buffer.Length);return base.WriteAsync(buffer,cancellationToken);}
        public override Task WriteAsync(byte[] buffer,int offset,int count,CancellationToken cancellationToken){Guard(count);return base.WriteAsync(buffer,offset,count,cancellationToken);}
    }
}

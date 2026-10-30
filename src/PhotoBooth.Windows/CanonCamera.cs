using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PhotoBooth.Core;
using PhotoBooth.Imaging;

namespace PhotoBooth.Windows;

/// <summary>EDSDK 13.x x64 adapter. Requires the operator's licensed Canon binaries.
/// All SDK calls and callbacks run on one STA thread with a Windows event pump.</summary>
public sealed class CanonCamera : IPhotoCamera
{
    private readonly SessionStore store;
    private readonly BlockingCollection<Action> commands = new();
    private readonly Thread thread;
    private readonly Native.ObjectHandler objectHandler;
    private IntPtr camera;
    private bool initialized, sessionOpen, live;
    private uint previousOutput;
    private TaskCompletionSource<byte[]>? pendingCapture;
    private bool disposed;

    public CanonCamera(SessionStore store)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Le Canon nécessite Windows et son SDK.");
        this.store = store; objectHandler = OnObject;
        thread = new Thread(Run) { IsBackground = true, Name = "Canon EDSDK STA" };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
    }

    public Task<CameraStatus> ConnectAsync(CancellationToken token) => Invoke(() =>
    {
        if (sessionOpen) return new CameraStatus(true, "Canon EOS", true, true);
        var library = Path.Combine(AppContext.BaseDirectory, "canon", "EDSDK.dll");
        if (!File.Exists(library)) throw new FileNotFoundException("SDK Canon absent. Placez les DLL Windows x64 licenciées dans le dossier canon à côté d’EstellArt.exe. Fermez DSLR Booth et EOS Utility.");
        Native.EnsureResolver(library);
        Check(Native.EdsInitializeSDK(), "Initialiser Canon"); initialized = true;
        Check(Native.EdsGetCameraList(out var list), "Lister les appareils");
        try
        {
            Check(Native.EdsGetChildCount(list, out var count), "Compter les appareils");
            if (count == 0) throw new InvalidOperationException("Canon introuvable. Allumez-le, branchez l’USB et fermez DSLR Booth/EOS Utility.");
            if (count != 1) throw new InvalidOperationException("Branchez un seul Canon pour cette borne.");
            Check(Native.EdsGetChildAtIndex(list, 0, out camera), "Choisir le Canon");
        }
        finally { Native.EdsRelease(list); }
        Check(Native.EdsOpenSession(camera), "Ouvrir le Canon"); sessionOpen = true;
        Check(Native.EdsSetObjectEventHandler(camera, 0x200, objectHandler, IntPtr.Zero), "Écouter les photos");
        SetProperty(0x0b, 2); // SaveTo_Host; disk space advertised below.
        Check(Native.EdsSetCapacity(camera, new Native.Capacity { FreeClusters = int.MaxValue, BytesPerSector = 4096, Reset = 1 }), "Annoncer le stockage");
        return new CameraStatus(true, "Canon EOS · USB", true, true);
    }, token);

    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([EnumeratorCancellation] CancellationToken token)
    {
        await Invoke(() =>
        {
            Check(Native.EdsGetPropertyData(camera, 0x500, 0, 4, out previousOutput), "Lire le retour vidéo");
            SetProperty(0x501, 1); SetProperty(0x500, previousOutput | 2); live = true; return true;
        }, token);
        try
        {
            while (!token.IsCancellationRequested)
            {
                var bytes = await Invoke(ReadLiveFrame, token);
                if (bytes is not null) yield return new LiveViewFrame(bytes, DateTimeOffset.UtcNow);
                await Task.Delay(80, token);
            }
        }
        finally
        {
            if (!disposed) await Invoke(() => { if (live) { SetProperty(0x500, previousOutput); live = false; } return true; }, CancellationToken.None);
        }
    }

    public async Task<CapturedPhoto> CaptureAsync(Guid sessionId, int shotIndex, CancellationToken token)
    {
        var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Invoke(() =>
        {
            if (pendingCapture is not null) throw new InvalidOperationException("Une capture Canon est déjà en attente.");
            pendingCapture = completion;
            try { Check(Native.EdsSendCommand(camera, 0, 0), "Déclencher la photo"); }
            catch { pendingCapture = null; throw; }
            return true;
        }, token);
        byte[] jpeg;
        try { jpeg = await completion.Task.WaitAsync(TimeSpan.FromSeconds(35), token); }
        catch (TimeoutException) { throw new IOException("Photo non reçue après 35 secondes. Vérifiez mise au point, batterie, câble et mode JPEG. Aucun second déclenchement automatique."); }
        finally { await Invoke(() => { if (pendingCapture == completion) pendingCapture = null; return true; }, CancellationToken.None); }
        var asset = await store.SavePhotoAsync(sessionId, PhotoProcessor.Normalize(jpeg), token);
        return new CapturedPhoto(Guid.NewGuid(), asset.Key);
    }

    private byte[]? ReadLiveFrame()
    {
        Check(Native.EdsCreateMemoryStream(0, out var stream), "Créer le flux vidéo"); IntPtr image = IntPtr.Zero;
        try
        {
            Check(Native.EdsCreateEvfImageRef(stream, out image), "Créer le retour vidéo");
            var error = Native.EdsDownloadEvfImage(camera, image);
            if (error is 0xa102 or 0x81) return null; // not ready / busy: preview only, never retry a shutter.
            Check(error, "Lire le retour vidéo"); return ReadStream(stream);
        }
        finally { if (image != IntPtr.Zero) Native.EdsRelease(image); Native.EdsRelease(stream); }
    }

    private uint OnObject(uint kind, IntPtr item, IntPtr context)
    {
        try
        {
            if (kind != 0x208) return 0;
            if (pendingCapture is null) { Native.EdsDownloadCancel(item); return 0; }
            Check(Native.EdsGetDirectoryItemInfo(item, out var info), "Lire la photo Canon");
            if (info.IsFolder != 0 || info.Size is 0 or > PhotoProcessor.MaxEncodedBytes ||
                !info.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                Native.EdsDownloadCancel(item);
                throw new InvalidDataException("Réglez le Canon sur JPEG haute qualité (pas RAW seul), puis recommencez.");
            }
            Check(Native.EdsCreateMemoryStream(info.Size, out var stream), "Recevoir la photo");
            try
            {
                Check(Native.EdsDownload(item, info.Size, stream), "Transférer la photo");
                Check(Native.EdsDownloadComplete(item), "Terminer le transfert");
                pendingCapture.TrySetResult(ReadStream(stream));
            }
            finally { Native.EdsRelease(stream); }
        }
        catch (Exception exception) { pendingCapture?.TrySetException(exception); }
        finally { if (item != IntPtr.Zero) Native.EdsRelease(item); }
        return 0; // Never let managed exceptions cross the native callback boundary.
    }

    private static byte[] ReadStream(IntPtr stream)
    {
        Check(Native.EdsGetLength(stream, out var length), "Lire la taille du flux");
        if (length is 0 or > PhotoProcessor.MaxEncodedBytes) throw new InvalidDataException("Flux Canon trop volumineux ou vide.");
        Check(Native.EdsGetPointer(stream, out var pointer), "Lire les pixels Canon");
        var bytes = new byte[(int)length]; Marshal.Copy(pointer, bytes, 0, bytes.Length); return bytes;
    }
    private void SetProperty(uint property, uint value) => Check(Native.EdsSetPropertyData(camera, property, 0, 4, ref value), "Configurer le Canon");
    private static void Check(uint result, string operation)
    { if (result != 0) throw new IOException($"{operation} : erreur Canon 0x{result:X8}. Vérifiez que DSLR Booth/EOS Utility est fermé."); }

    private Task<T> Invoke<T>(Func<T> action, CancellationToken token)
    {
        if (disposed) throw new ObjectDisposedException(nameof(CanonCamera));
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        commands.Add(() => { try { token.ThrowIfCancellationRequested(); completion.SetResult(action()); } catch (Exception ex) { completion.SetException(ex); } }, token);
        return completion.Task;
    }
    private void Run()
    {
        Native.CoInitializeEx(IntPtr.Zero, 2);
        try
        {
            while (!commands.IsCompleted)
            {
                if (commands.TryTake(out var action, 15)) action();
                if (initialized) Native.EdsGetEvent();
                while (Native.PeekMessage(out var message, IntPtr.Zero, 0, 0, 1))
                { Native.TranslateMessage(ref message); Native.DispatchMessage(ref message); }
            }
        }
        finally { Native.CoUninitialize(); }
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await Invoke(() =>
        {
            pendingCapture?.TrySetCanceled(); pendingCapture = null;
            if (sessionOpen) { if (live) Native.EdsSetPropertyData(camera, 0x500, 0, 4, ref previousOutput); Native.EdsCloseSession(camera); }
            if (camera != IntPtr.Zero) Native.EdsRelease(camera);
            if (initialized) Native.EdsTerminateSDK();
            initialized = sessionOpen = live = false; camera = IntPtr.Zero; return true;
        }, CancellationToken.None);
        disposed = true; commands.CompleteAdding(); await Task.Run(() => thread.Join(3000));
        GC.KeepAlive(objectHandler);
    }

    private static class Native
    {
        private static bool resolverSet;
        private static readonly object resolverLock = new();
        internal static void EnsureResolver(string file)
        {
            lock (resolverLock)
            {
                if (resolverSet) return;
                NativeLibrary.SetDllImportResolver(typeof(CanonCamera).Assembly, (name, _, _) =>
                {
                    if (name != "EDSDK") return IntPtr.Zero;
                    var library = LoadLibraryEx(file, IntPtr.Zero, 0x1100);
                    if (library == IntPtr.Zero) throw new DllNotFoundException("Impossible de charger le SDK Canon x64 et ses dépendances (EDSDK.dll, EdsImage.dll, runtime Visual C++ du SDK).");
                    return library;
                }); resolverSet = true;
            }
        }
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate uint ObjectHandler(uint kind, IntPtr item, IntPtr context);
        [StructLayout(LayoutKind.Sequential)] internal struct Capacity { public int FreeClusters, BytesPerSector, Reset; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] internal struct ItemInfo
        { public ulong Size; public int IsFolder; public uint GroupId, Option; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string FileName; public uint Format, DateTime; }
        [StructLayout(LayoutKind.Sequential)] internal struct Message
        { public IntPtr Window; public uint Id; public UIntPtr WParam; public IntPtr LParam; public uint Time; public int X, Y; public uint Private; }
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern IntPtr LoadLibraryEx(string file, IntPtr handle, uint flags);
        [DllImport("ole32.dll")] internal static extern int CoInitializeEx(IntPtr reserved, uint model);
        [DllImport("ole32.dll")] internal static extern void CoUninitialize();
        [DllImport("user32.dll", EntryPoint = "PeekMessageW")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool TranslateMessage(ref Message message);
        [DllImport("user32.dll", EntryPoint = "DispatchMessageW")] internal static extern IntPtr DispatchMessage(ref Message message);
        [DllImport("EDSDK")] internal static extern uint EdsInitializeSDK();
        [DllImport("EDSDK")] internal static extern uint EdsTerminateSDK();
        [DllImport("EDSDK")] internal static extern uint EdsGetEvent();
        [DllImport("EDSDK")] internal static extern uint EdsGetCameraList(out IntPtr list);
        [DllImport("EDSDK")] internal static extern uint EdsGetChildCount(IntPtr item, out int count);
        [DllImport("EDSDK")] internal static extern uint EdsGetChildAtIndex(IntPtr item, int index, out IntPtr child);
        [DllImport("EDSDK")] internal static extern uint EdsOpenSession(IntPtr camera);
        [DllImport("EDSDK")] internal static extern uint EdsCloseSession(IntPtr camera);
        [DllImport("EDSDK")] internal static extern uint EdsRelease(IntPtr item);
        [DllImport("EDSDK")] internal static extern uint EdsSetObjectEventHandler(IntPtr camera, uint kind, ObjectHandler handler, IntPtr context);
        [DllImport("EDSDK")] internal static extern uint EdsSetCapacity(IntPtr camera, Capacity capacity);
        [DllImport("EDSDK")] internal static extern uint EdsGetPropertyData(IntPtr camera, uint property, int parameter, uint size, out uint value);
        [DllImport("EDSDK")] internal static extern uint EdsSetPropertyData(IntPtr camera, uint property, int parameter, uint size, ref uint value);
        [DllImport("EDSDK")] internal static extern uint EdsCreateMemoryStream(ulong size, out IntPtr stream);
        [DllImport("EDSDK")] internal static extern uint EdsCreateEvfImageRef(IntPtr stream, out IntPtr image);
        [DllImport("EDSDK")] internal static extern uint EdsDownloadEvfImage(IntPtr camera, IntPtr image);
        [DllImport("EDSDK")] internal static extern uint EdsSendCommand(IntPtr camera, uint command, int parameter);
        [DllImport("EDSDK")] internal static extern uint EdsGetDirectoryItemInfo(IntPtr item, out ItemInfo info);
        [DllImport("EDSDK")] internal static extern uint EdsDownload(IntPtr item, ulong length, IntPtr stream);
        [DllImport("EDSDK")] internal static extern uint EdsDownloadComplete(IntPtr item);
        [DllImport("EDSDK")] internal static extern uint EdsDownloadCancel(IntPtr item);
        [DllImport("EDSDK")] internal static extern uint EdsGetLength(IntPtr stream, out ulong length);
        [DllImport("EDSDK")] internal static extern uint EdsGetPointer(IntPtr stream, out IntPtr pointer);
    }
}

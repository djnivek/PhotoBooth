using System.ComponentModel;
using System.Runtime.InteropServices;
using PhotoBooth.Core;
using PhotoBooth.Imaging;

namespace PhotoBooth.Windows;

// Native GDI, one raster, one page, one explicit spool submission. No shell print verbs.
public sealed class WindowsPhotoPrinter(SessionStore store, StoredAsset sheet, string printerName) : IPhotoPrinter
{
    public async Task<PrintSubmission> SubmitAsync(PrintRequest request, CancellationToken token)
    {
        byte[] pixels;
        try
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("L’impression réelle demande Windows.");
            if (sheet.Key != request.SheetAssetKey) throw new InvalidDataException("Planche différente de l’aperçu.");
            var jpeg = await store.ReadVerifiedAsync(sheet, token);
            pixels = await Task.Run(() => PrintRaster.ToBgra(jpeg), token);
            token.ThrowIfCancellationRequested();
        }
        catch (Exception exception) { throw new PrintNotSubmittedException(exception.Message, exception); }
        // Once GDI starts, cancellation cannot establish whether paper was emitted.
        return await Task.Run(() => Submit(request, pixels), CancellationToken.None);
    }

    public static IReadOnlyList<string> ListPrinters()
    {
        if (!OperatingSystem.IsWindows()) return [];
        Native.EnumPrinters(6, null, 4, IntPtr.Zero, 0, out var needed, out _);
        if (needed == 0) return [];
        var buffer = Marshal.AllocHGlobal(checked((int)needed));
        try
        {
            if (!Native.EnumPrinters(6, null, 4, buffer, needed, out _, out var count)) throw new Win32Exception();
            var result = new List<string>(); var size = Marshal.SizeOf<Native.PrinterInfo4>();
            for (var i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<Native.PrinterInfo4>(buffer + i * size);
                if (Marshal.PtrToStringUni(info.Name) is { Length: > 0 } name) result.Add(name);
            }
            return result;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private PrintSubmission Submit(PrintRequest request, byte[] pixels)
    {
        IntPtr printer = IntPtr.Zero, devmode = IntPtr.Zero, dc = IntPtr.Zero; var started = false; var submissionAttempted = false;
        try
        {
            if (string.IsNullOrWhiteSpace(printerName) || !ListPrinters().Contains(printerName))
                throw new InvalidOperationException("Sélectionnez une imprimante installée dans les réglages.");
            if (!Native.OpenPrinter(printerName, out printer, IntPtr.Zero)) throw new Win32Exception();
            var size = Native.DocumentProperties(IntPtr.Zero, printer, printerName, IntPtr.Zero, IntPtr.Zero, 0);
            if (size < Marshal.SizeOf<Native.DevMode>()) throw new InvalidDataException("DEVMODE invalide.");
            devmode = Marshal.AllocHGlobal(size);
            if (Native.DocumentProperties(IntPtr.Zero, printer, printerName, devmode, IntPtr.Zero, 2) < 0) throw new Win32Exception();
            var mode = Marshal.PtrToStructure<Native.DevMode>(devmode);
            var paper = FindFourBySix(devmode);
            mode.PaperSize = paper.Id;
            mode.Orientation = paper.Wide ? (short)1 : (short)2; mode.Copies = 1;
            mode.Fields = (mode.Fields | 0x00000103u) & ~0x0000000cu; // orientation, paper size, copies; no custom width/length
            Marshal.StructureToPtr(mode, devmode, false);
            if (Native.DocumentProperties(IntPtr.Zero, printer, printerName, devmode, devmode, 10) < 0) throw new Win32Exception();
            var accepted = Marshal.PtrToStructure<Native.DevMode>(devmode);
            if (accepted.PaperSize != mode.PaperSize || accepted.Copies != 1 || accepted.Orientation != mode.Orientation)
                throw new InvalidDataException("Le pilote n’a pas accepté 4×6 paysage, une copie.");
            dc = Native.CreateDC("WINSPOOL", printerName, null, devmode);
            if (dc == IntPtr.Zero) throw new Win32Exception();
            var geometry = new PrinterGeometry(Native.GetDeviceCaps(dc, 8), Native.GetDeviceCaps(dc, 10),
                Native.GetDeviceCaps(dc, 88), Native.GetDeviceCaps(dc, 90));
            var physicalWidth = (double)Native.GetDeviceCaps(dc, 110) / geometry.DpiX;
            var physicalHeight = (double)Native.GetDeviceCaps(dc, 111) / geometry.DpiY;
            if (Math.Abs(physicalWidth - 6) > .25 || Math.Abs(physicalHeight - 4) > .25)
                throw new InvalidDataException($"Le pilote annonce {physicalWidth:F2}×{physicalHeight:F2} pouces. Vérifiez le papier 4×6.");
            var place = PrintRaster.Fit(geometry);
            var info = new Native.DocInfo { Size = Marshal.SizeOf<Native.DocInfo>(), Name = $"EstellArt {request.Id:N}" };
            submissionAttempted = true; // From this boundary onward, never classify failure as safe to resubmit.
            var job = Native.StartDoc(dc, ref info);
            if (job <= 0) throw new Win32Exception(); started = true;
            if (Native.StartPage(dc) <= 0) throw new Win32Exception();
            var bitmap = new Native.BitmapInfoHeader { Size = 40, Width = 1800, Height = -1200, Planes = 1, BitCount = 32,
                SizeImage = (uint)pixels.Length };
            Native.SetStretchBltMode(dc, 4); Native.SetBrushOrgEx(dc, 0, 0, IntPtr.Zero);
            var lines = Native.StretchDIBits(dc, place.X, place.Y, place.Width, place.Height,
                0, 0, 1800, 1200, pixels, ref bitmap, 0, 0x00cc0020);
            if (lines <= 0 || Native.EndPage(dc) <= 0 || Native.EndDoc(dc) <= 0) throw new Win32Exception();
            started = false;
            return new PrintSubmission(job.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (!submissionAttempted)
        { throw new PrintNotSubmittedException(exception.Message, exception); }
        finally
        {
            if (started && dc != IntPtr.Zero) Native.AbortDoc(dc);
            if (dc != IntPtr.Zero) Native.DeleteDC(dc);
            if (devmode != IntPtr.Zero) Marshal.FreeHGlobal(devmode);
            if (printer != IntPtr.Zero) Native.ClosePrinter(printer);
        }
    }

    private (short Id, bool Wide) FindFourBySix(IntPtr devmode)
    {
        var count = Native.DeviceCapabilities(printerName, null, 2, IntPtr.Zero, devmode);
        if (count <= 0 || count > 1000) throw new InvalidDataException("Formats papier indisponibles.");
        var ids = Marshal.AllocHGlobal(count * 2); var sizes = Marshal.AllocHGlobal(count * 8);
        try
        {
            if (Native.DeviceCapabilities(printerName, null, 2, ids, devmode) != count ||
                Native.DeviceCapabilities(printerName, null, 3, sizes, devmode) != count) throw new Win32Exception();
            for (var i = 0; i < count; i++)
            {
                var w = Marshal.ReadInt32(sizes, i * 8); var h = Marshal.ReadInt32(sizes, i * 8 + 4);
                if (Math.Abs(Math.Min(w, h) - 1016) <= 15 && Math.Abs(Math.Max(w, h) - 1524) <= 15)
                    return (Marshal.ReadInt16(ids, i * 2), w > h);
            }
            throw new InvalidDataException("Le pilote ne propose pas de papier 4×6 (102×152 mm). Installez le pilote DNP DS620.");
        }
        finally { Marshal.FreeHGlobal(ids); Marshal.FreeHGlobal(sizes); }
    }

    private static class Native
    {
        [StructLayout(LayoutKind.Sequential)] internal struct PrinterInfo4 { public IntPtr Name, Server; public uint Attributes; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DevMode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            public ushort SpecVersion, DriverVersion, Size, DriverExtra; public uint Fields;
            public short Orientation, PaperSize, PaperLength, PaperWidth, Scale, Copies, DefaultSource, PrintQuality;
            public short Color, Duplex, YResolution, TTOption, Collate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FormName;
            public ushort LogPixels; public uint BitsPerPel, PelsWidth, PelsHeight, DisplayFlags, DisplayFrequency;
            public uint IcmMethod, IcmIntent, MediaType, DitherType, Reserved1, Reserved2, PanningWidth, PanningHeight;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] internal struct DocInfo
        { public int Size; [MarshalAs(UnmanagedType.LPWStr)] public string Name; public IntPtr Output, DataType; public uint Type; }
        [StructLayout(LayoutKind.Sequential)] internal struct BitmapInfoHeader
        { public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, SizeImage; public int XPelsPerMeter, YPelsPerMeter; public uint ColorsUsed, ColorsImportant; }
        [DllImport("winspool.drv", EntryPoint = "EnumPrintersW", SetLastError = true, CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool EnumPrinters(uint flags, string? name, uint level, IntPtr buffer, uint size, out uint needed, out uint returned);
        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool OpenPrinter(string name, out IntPtr printer, IntPtr defaults);
        [DllImport("winspool.drv", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool ClosePrinter(IntPtr printer);
        [DllImport("winspool.drv", EntryPoint = "DocumentPropertiesW", SetLastError = true, CharSet = CharSet.Unicode)] internal static extern int DocumentProperties(IntPtr window, IntPtr printer, string name, IntPtr output, IntPtr input, uint mode);
        [DllImport("winspool.drv", EntryPoint = "DeviceCapabilitiesW", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern int DeviceCapabilities(string device, string? port, ushort capability, IntPtr output, IntPtr mode);
        [DllImport("gdi32.dll", EntryPoint = "CreateDCW", SetLastError = true, CharSet = CharSet.Unicode)] internal static extern IntPtr CreateDC(string driver, string device, string? output, IntPtr mode);
        [DllImport("gdi32.dll")] internal static extern int GetDeviceCaps(IntPtr dc, int index);
        [DllImport("gdi32.dll", EntryPoint = "StartDocW", SetLastError = true, CharSet = CharSet.Unicode)] internal static extern int StartDoc(IntPtr dc, ref DocInfo info);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern int StartPage(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern int EndPage(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern int EndDoc(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern int AbortDoc(IntPtr dc);
        [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern int SetStretchBltMode(IntPtr dc, int mode);
        [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetBrushOrgEx(IntPtr dc, int x, int y, IntPtr old);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern int StretchDIBits(IntPtr dc, int x, int y, int width, int height, int sx, int sy, int sw, int sh, byte[] pixels, ref BitmapInfoHeader bitmap, uint usage, uint rop);
    }
}

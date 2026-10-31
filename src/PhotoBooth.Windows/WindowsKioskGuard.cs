using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PhotoBooth.Windows;

/// <summary>Suppresses common desktop shortcuts while this process owns foreground focus.
/// This is an application guard, not Windows Assigned Access or a secure desktop boundary.</summary>
public sealed class WindowsKioskGuard : IDisposable
{
    private readonly Thread thread;
    private readonly ManualResetEventSlim started = new();
    private readonly HashSet<uint> suppressed = [];
    private HookProc? callback;
    private Exception? failure;
    private uint threadId;
    private int disposed;

    public WindowsKioskGuard()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        thread = new Thread(Run) { IsBackground = true, Name = "Booth shortcut guard" };
        thread.Start();
        started.Wait();
        if (failure is not null) { Dispose(); throw new InvalidOperationException("Le verrouillage du clavier n’a pas pu être activé.", failure); }
    }

    // Virtual keys, independent of the keyboard layout. Never record text or other input.
    public static bool Blocks(uint key, bool alt, bool control) =>
        key is 0x5B or 0x5C or 0x7A ||
        (alt && key is 0x09 or 0x1B or 0x20 or 0x73) ||
        (control && key == 0x1B);

    private void Run()
    {
        nint hook = 0;
        try
        {
            threadId = GetCurrentThreadId();
            PeekMessage(out _, 0, 0, 0, 0); // Message queue must exist before Dispose.
            callback = OnKey;
            hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
            if (hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            started.Set();
            while (GetMessage(out var message, 0, 0, 0) > 0)
            { TranslateMessage(ref message); DispatchMessage(ref message); }
        }
        catch (Exception error) { failure = error; started.Set(); }
        finally { if (hook != 0) UnhookWindowsHookEx(hook); GC.KeepAlive(callback); }
    }

    private nint OnKey(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            var key = Marshal.PtrToStructure<KeyboardInput>(data);
            var down = message is 0x100 or 0x104;
            var up = message is 0x101 or 0x105;
            if (up && suppressed.Remove(key.Key)) return 1;
            GetWindowThreadProcessId(GetForegroundWindow(), out var owner);
            if (down && owner == (uint)Environment.ProcessId &&
                Blocks(key.Key, (key.Flags & 0x20) != 0, (GetAsyncKeyState(0x11) & 0x8000) != 0))
            { suppressed.Add(key.Key); return 1; }
        }
        return CallNextHookEx(0, code, message, data);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        if (thread.IsAlive) { PostThreadMessage(threadId, 0x12, 0, 0); thread.Join(); }
        started.Dispose();
    }

    private delegate nint HookProc(int code, nint message, nint data);
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    { public uint Key, ScanCode, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct Message
    { public nint Window; public uint Id; public nuint WParam; public nint LParam; public uint Time; public int X, Y; public uint Private; }
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetWindowsHookEx(int id, HookProc callback, nint module, uint thread);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? module);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern int GetMessage(out Message message, nint window, uint min, uint max);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PeekMessage(out Message message, nint window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern nint DispatchMessage(ref Message message);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostThreadMessage(uint thread, uint message, nuint wParam, nint lParam);
}

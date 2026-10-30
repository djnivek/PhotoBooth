using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using PhotoBooth.Imaging;

namespace PhotoBooth.Runtime;

public static class CredentialStore
{
    public static string Load(string path)
    {
        if (!File.Exists(path)) return "";
        var value = File.ReadAllText(path);
        if (value.StartsWith("dpapi:"))
        {
            if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Ce code de connexion appartient au profil Windows d’origine.");
            return Encoding.UTF8.GetString(Protect(Convert.FromBase64String(value[6..]), false));
        }
        if (value.StartsWith("local:" ) && !OperatingSystem.IsWindows()) return Encoding.UTF8.GetString(Convert.FromBase64String(value[6..]));
        throw new InvalidDataException("Reconfigurez le code de connexion sur cet ordinateur.");
    }
    public static async Task SaveAsync(string path, string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var value = OperatingSystem.IsWindows() ? "dpapi:" + Convert.ToBase64String(Protect(bytes,true)) : "local:" + Convert.ToBase64String(bytes);
        await SessionStore.WriteAtomicAsync(path, Encoding.ASCII.GetBytes(value));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    private static byte[] Protect(byte[] bytes,bool encrypt)
    {
        var input=new Blob { Size=bytes.Length, Data=Marshal.AllocHGlobal(bytes.Length) }; Blob output=default;
        try
        {
            Marshal.Copy(bytes,0,input.Data,bytes.Length);
            var success=encrypt ? CryptProtectData(ref input,null,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output)
                : CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);
            if (!success) throw new Win32Exception();
            var result=new byte[output.Size];Marshal.Copy(output.Data,result,0,result.Length);return result;
        }
        finally { Marshal.FreeHGlobal(input.Data);if(output.Data!=IntPtr.Zero)LocalFree(output.Data); }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Blob { public int Size;public IntPtr Data; }
    [DllImport("crypt32.dll",SetLastError=true,CharSet=CharSet.Unicode)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool CryptProtectData(ref Blob input,string? description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
    [DllImport("crypt32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
}

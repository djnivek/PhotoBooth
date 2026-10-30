param([Parameter(Mandatory)][System.Diagnostics.Process]$ApplicationProcess,[Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class PhotoBoothWindowCapture {
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
  [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd,out Rect rect);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hwnd,ref Point point);
  [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);
}
'@
$watch=[Diagnostics.Stopwatch]::StartNew()
do {
    $ApplicationProcess.Refresh()
    if($ApplicationProcess.HasExited){throw 'Native UI exited before desktop capture'}
    $handle=$ApplicationProcess.MainWindowHandle
    if($handle -eq [IntPtr]::Zero){Start-Sleep -Milliseconds 50}
} while($handle -eq [IntPtr]::Zero -and $watch.Elapsed.TotalSeconds -lt 15)
if($handle -eq [IntPtr]::Zero){throw 'Native window handle not found'}
Start-Sleep -Milliseconds 1000
$previous=[PhotoBoothWindowCapture]::SetThreadDpiAwarenessContext([IntPtr](-4))
try {
    $rect=New-Object PhotoBoothWindowCapture+Rect
    $point=New-Object PhotoBoothWindowCapture+Point
    if(-not [PhotoBoothWindowCapture]::GetClientRect($handle,[ref]$rect)){throw 'Client bounds unavailable'}
    if(-not [PhotoBoothWindowCapture]::ClientToScreen($handle,[ref]$point)){throw 'Client position unavailable'}
    $width=$rect.Right-$rect.Left;$height=$rect.Bottom-$rect.Top
    $bitmap=[Drawing.Bitmap]::new($width,$height)
    $graphics=[Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($point.X,$point.Y,0,0,$bitmap.Size)
        $bitmap.Save((Join-Path $OutputDirectory 'desktop-client.png'),[Drawing.Imaging.ImageFormat]::Png)
        @{left=$point.X;top=$point.Y;width=$width;height=$height;source='Windows desktop client pixels'} |
            ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'desktop-client.json') -Encoding utf8
    }
    finally {$graphics.Dispose();$bitmap.Dispose()}
}
finally {[void][PhotoBoothWindowCapture]::SetThreadDpiAwarenessContext($previous)}

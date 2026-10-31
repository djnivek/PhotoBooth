using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Text.Json;
using PhotoBooth.Imaging;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace PhotoBooth.App;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(refreshCatalogOnOpen: Program.SmokeOutput is null);
            if(Program.SmokeOutput is { } directory)
                desktop.MainWindow.Opened+=(_,_)=>DispatcherTimer.RunOnce(async ()=>
                {
                    try
                    {
                        var window=desktop.MainWindow;
                        CaptureNativeWindow(window,directory,"native-home.png","native-ui.json");
                        window.Width=960;window.Height=700;
                        await Task.Delay(500);
                        CaptureNativeWindow(window,directory,"native-minimum.png","native-minimum-ui.json");
                        if(Environment.GetEnvironmentVariable("PHOTOBOOTH_SMOKE_DESKTOP_CAPTURE")=="1")
                        {
                            // The external capture helper may compile its Win32 bridge slowly
                            // on a fresh CI runner. Keep the real window alive until its receipt.
                            using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(25));
                            while(!File.Exists(Path.Combine(directory,"desktop-client.json")))
                                await Task.Delay(100,limit.Token);
                        }
                        desktop.Shutdown(0);
                    }
                    catch(Exception exception)
                    {File.WriteAllText(Path.Combine(directory,"native-ui-error.txt"),exception.ToString());desktop.Shutdown(1);}
                },TimeSpan.FromSeconds(5));
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void CaptureNativeWindow(Window window,string directory,string imageName,string receiptName)
    {
        window.UpdateLayout();
        var logical=window.ClientSize;var scaling=window.RenderScaling;
        var size=new PixelSize((int)Math.Ceiling(logical.Width*scaling),(int)Math.Ceiling(logical.Height*scaling));
        if(logical.Width<900||logical.Height<650)throw new InvalidOperationException("Native window did not receive usable bounds.");
        var controls=window.GetVisualDescendants().OfType<Control>().Where(b=>b.Name is "Settings" or "HomeSettings" or "StartSession" or "HomeSheet").Select(b=>
        {var position=b.TranslatePoint(new Point(0,0),window)??throw new InvalidOperationException("Missing control transform.");
            var viewport=b.GetVisualAncestors().OfType<Control>().First(c=>c.Name is "SessionBody" or "Header");
            var origin=viewport.TranslatePoint(new Point(0,0),window)!.Value;
            var insideViewport=position.X>=origin.X-1&&position.Y>=origin.Y-1&&position.X+b.Bounds.Width<=origin.X+viewport.Bounds.Width+1&&position.Y+b.Bounds.Height<=origin.Y+viewport.Bounds.Height+1;
            return new {b.Name,position.X,position.Y,b.Bounds.Width,b.Bounds.Height,right=position.X+b.Bounds.Width,bottom=position.Y+b.Bounds.Height,inside_viewport=insideViewport};}).ToArray();
        var inside=controls.Length==4&&controls.All(c=>c.inside_viewport&&c.X>=0&&c.Y>=0&&c.right<=logical.Width+1&&c.bottom<=logical.Height+1);
        using var screenshot=new RenderTargetBitmap(size,new Vector(96*scaling,96*scaling));screenshot.Render(window);
        var path=Path.Combine(directory,imageName);screenshot.Save(path,PngBitmapEncoderOptions.Default);
        File.WriteAllText(Path.Combine(directory,receiptName),JsonSerializer.Serialize(new
        {
            native_window_opened=true,size.Width,size.Height,scaling,logical_size=logical.ToString(),
            bounds=window.Bounds.ToString(),content_bounds=((Control)window.Content!).Bounds.ToString(),controls,controls_inside_client=inside,
            layout=window.GetVisualDescendants().OfType<Control>().Where(c=>c.Name is "AppLayout" or "Header" or "SessionBody" or "HomeLayout")
                .Select(c=>new {c.Name,bounds=c.Bounds.ToString(),desired=c.DesiredSize.ToString()}).ToArray(),
            os=System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            image_sha256=SessionStore.Hash(File.ReadAllBytes(path)),
            paid_api_calls=0,hardware_access=false
        },SessionStore.JsonOptions));
        if(!inside)throw new InvalidOperationException("Primary controls extend outside the native client area.");
    }
}

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
            desktop.MainWindow = new MainWindow();
            if(Program.SmokeOutput is { } directory)
                desktop.MainWindow.Opened+=(_,_)=>DispatcherTimer.RunOnce(()=>
                {
                    try
                    {
                        var window=desktop.MainWindow;
                        window.UpdateLayout();
                        var logical=window.ClientSize;var scaling=window.RenderScaling;
                        var size=new PixelSize((int)Math.Ceiling(logical.Width*scaling),(int)Math.Ceiling(logical.Height*scaling));
                        if(logical.Width<900||logical.Height<650)throw new InvalidOperationException("Native window did not receive usable bounds.");
                        var controls=window.GetVisualDescendants().OfType<Button>().Where(b=>b.Name is "Settings" or "StartSession").Select(b=>
                        {var position=b.TranslatePoint(new Point(0,0),window)??throw new InvalidOperationException("Missing control transform.");
                            return new {b.Name,position.X,position.Y,b.Bounds.Width,b.Bounds.Height,right=position.X+b.Bounds.Width,bottom=position.Y+b.Bounds.Height};}).ToArray();
                        var inside=controls.Length==2&&controls.All(c=>c.X>=0&&c.Y>=0&&c.right<=logical.Width+1&&c.bottom<=logical.Height+1);
                        using var screenshot=new RenderTargetBitmap(size,new Vector(96*scaling,96*scaling));screenshot.Render(window);
                        var path=Path.Combine(directory,"native-home.png");screenshot.Save(path,PngBitmapEncoderOptions.Default);
                        File.WriteAllText(Path.Combine(directory,"native-ui.json"),JsonSerializer.Serialize(new
                        {
                            native_window_opened=true,size.Width,size.Height,scaling,logical_size=logical.ToString(),
                            bounds=window.Bounds.ToString(),content_bounds=((Control)window.Content!).Bounds.ToString(),controls,controls_inside_client=inside,
                            os=System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                            image_sha256=SessionStore.Hash(File.ReadAllBytes(path)),
                            paid_api_calls=0,hardware_access=false
                        },SessionStore.JsonOptions));
                        if(!inside)throw new InvalidOperationException("Primary controls extend outside the native client area.");
                        desktop.Shutdown(0);
                    }
                    catch(Exception exception)
                    {File.WriteAllText(Path.Combine(directory,"native-ui-error.txt"),exception.ToString());desktop.Shutdown(1);}
                },TimeSpan.FromSeconds(5));
        }
        base.OnFrameworkInitializationCompleted();
    }
}

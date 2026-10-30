using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Text.Json;
using PhotoBooth.Imaging;

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
                        var size=new PixelSize((int)window.Bounds.Width,(int)window.Bounds.Height);
                        if(size.Width<900||size.Height<650)throw new InvalidOperationException("Native window did not receive usable bounds.");
                        using var screenshot=new RenderTargetBitmap(size,new Vector(96,96));screenshot.Render(window);
                        var path=Path.Combine(directory,"native-home.png");screenshot.Save(path,PngBitmapEncoderOptions.Default);
                        File.WriteAllText(Path.Combine(directory,"native-ui.json"),JsonSerializer.Serialize(new
                        {
                            native_window_opened=true,size.Width,size.Height,
                            os=System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                            image_sha256=SessionStore.Hash(File.ReadAllBytes(path)),
                            paid_api_calls=0,hardware_access=false
                        },SessionStore.JsonOptions));
                        desktop.Shutdown(0);
                    }
                    catch(Exception exception)
                    {File.WriteAllText(Path.Combine(directory,"native-ui-error.txt"),exception.ToString());desktop.Shutdown(1);}
                },TimeSpan.FromSeconds(2));
        }
        base.OnFrameworkInitializationCompleted();
    }
}

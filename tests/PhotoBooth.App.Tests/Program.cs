using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PhotoBooth.App;

var root = Path.Combine(Path.GetTempPath(), "estellart-ui-" + Guid.NewGuid().ToString("N"));
Environment.SetEnvironmentVariable("PHOTOBOOTH_DATA_DIR", root);
AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
var screenshots = Path.Combine(Environment.CurrentDirectory, ".impeccable", "review");
Directory.CreateDirectory(screenshots);
var window = new MainWindow((_, token) => Task.Delay(90, token)); window.Show();
Pump(150);
Capture("desktop-1280x800.png");
window.Width = 1024; window.Height = 768; Pump(80); Capture("desktop-1024x768.png");
window.Width = 1280; window.Height = 800; Pump(80);
Click("StartSession"); Until(() => Find("TakePhotos") is not null, 3000);
Pump(300); Capture("positioning.png");
Click("TakePhotos"); Pump(45); Capture("countdown.png");
Until(() => window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Clic !"), 3000);
Capture("capture-flash.png");
Until(() => Find("PrintPreview") is not null, 10000);
Capture("result.png"); Click("PrintPreview");
Until(() => Directory.GetFiles(root, "print-preview.jpg", SearchOption.AllDirectories).Length == 1, 3000);
var file = Directory.GetFiles(root, "print-preview.jpg", SearchOption.AllDirectories).Single();
if (new FileInfo(file).Length < 10000) throw new Exception("Print preview is empty.");
window.Close(); Pump(100);
Console.WriteLine("PASS UI: home at two sizes, start, live fixture, animated countdown, four captures, result, exact print export. No paid API or printer.");
Directory.Delete(root, true);

Button? Find(string name) => window.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == name);
void Click(string name)
{
    var button = Find(name) ?? throw new Exception("Missing button " + name);
    if (!button.IsEnabled || !button.IsVisible) throw new Exception("Unavailable button " + name);
    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
void Pump(int milliseconds)
{
    var end = DateTime.UtcNow.AddMilliseconds(milliseconds);
    do { Dispatcher.UIThread.RunJobs(); Thread.Sleep(5); } while (DateTime.UtcNow < end);
}
void Until(Func<bool> predicate, int timeout)
{
    var end = DateTime.UtcNow.AddMilliseconds(timeout);
    while (!predicate()) { if (DateTime.UtcNow > end) throw new Exception("UI condition timed out."); Pump(10); }
    Pump(40);
}
void Capture(string name)
{
    using var frame = window.CaptureRenderedFrame() ?? throw new Exception("No rendered pixels.");
    frame.Save(Path.Combine(screenshots, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
}

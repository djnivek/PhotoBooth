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
var openedFolders = new List<string>();
var window = new MainWindow((_, token) => Task.Delay(90, token), openedFolders.Add); window.Show();
Pump(150);
Capture("desktop-1280x800.png");
window.Width = 1024; window.Height = 768; Pump(80); Capture("desktop-1024x768.png");
AssertHomeFits();
window.Width = 960; window.Height = 700; Pump(80); AssertHomeFits(); Capture("desktop-960x700.png");
window.Width = 1280; window.Height = 800; Pump(80);
AssertHomeFits();
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
window.Width=1024;window.Height=768;Pump(80);Capture("result-1024x768.png");
window.Width=1280;window.Height=800;Pump(80);
Click("Settings"); Until(() => window.OwnedWindows.Count > 0, 2000);
var settings = window.OwnedWindows.Single(); Pump(50);
using (var settingsFrame = settings.CaptureRenderedFrame()!) settingsFrame.Save(Path.Combine(screenshots, "settings.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
((ScrollViewer)settings.Content!).Offset=new Vector(0,10000);Pump(80);
using (var settingsFrame = settings.CaptureRenderedFrame()!) settingsFrame.Save(Path.Combine(screenshots, "settings-bottom.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
settings.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "ExitPin").Text = "381629";
settings.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Activer le plein écran"))
    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Until(() => window.WindowState == WindowState.FullScreen, 2000);
Click("OpenFiles"); Pump(100);
if(openedFolders.Count!=0)throw new Exception("Locked kiosk opened the filesystem without the PIN.");
Until(() => window.OwnedWindows.Count > 0, 2000);
var unlock = window.OwnedWindows.Single(); Pump(50);
using (var unlockFrame = unlock.CaptureRenderedFrame()!) unlockFrame.Save(Path.Combine(screenshots, "unlock.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
unlock.GetVisualDescendants().OfType<TextBox>().Single(t => t.PasswordChar == '●').Text = "000000";
unlock.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Déverrouiller"))
    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Until(()=>unlock.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.StartsWith("Code incorrect")==true),2000);
using (var unlockFrame = unlock.CaptureRenderedFrame()!) unlockFrame.Save(Path.Combine(screenshots, "unlock-error.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
if(openedFolders.Count!=0||window.WindowState!=WindowState.FullScreen)throw new Exception("Wrong PIN opened the filesystem.");
unlock.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Retour à la fête"))
    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Until(()=>window.OwnedWindows.Count==0,2000);
if(openedFolders.Count!=0||window.WindowState!=WindowState.FullScreen)throw new Exception("Cancelled PIN opened the filesystem.");
window.Close();Until(()=>window.OwnedWindows.Count>0,2000);
window.OwnedWindows.Single().GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Retour à la fête"))
    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Until(()=>window.OwnedWindows.Count==0,2000);
Click("OpenFiles");Until(()=>window.OwnedWindows.Count>0,2000);
unlock=window.OwnedWindows.Single();
unlock.GetVisualDescendants().OfType<TextBox>().Single(t => t.PasswordChar == '●').Text = "381629";
unlock.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Déverrouiller"))
    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Until(() => window.WindowState == WindowState.Normal, 3000);
Until(()=>openedFolders.Count==1,2000);
if(openedFolders[0]!=Path.GetDirectoryName(file))throw new Exception("Unlocked folder does not match the reviewed session.");
if (File.ReadAllText(Path.Combine(root, "kiosk.json")).Contains("381629")) throw new Exception("Plain PIN stored");
window.Close(); Pump(100);
Console.WriteLine("PASS UI: home at three sizes, capture flow, print export, PIN fullscreen, protected close and file access (wrong/cancelled/correct PIN). No paid API or printer.");
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
void AssertHomeFits()
{
    foreach(var name in new[]{"Settings","StartSession","HomeSheet"})
    {
        var control=window.GetVisualDescendants().OfType<Control>().Single(c=>c.Name==name);
        var point=control.TranslatePoint(new Point(0,0),window)??throw new Exception("Detached control "+name);
        if(point.X<0||point.Y<0||point.X+control.Bounds.Width>window.ClientSize.Width+1||point.Y+control.Bounds.Height>window.ClientSize.Height+1)
            throw new Exception($"{name} exceeds {window.ClientSize}: {point}, {control.Bounds.Size}");
        var viewport=control.GetVisualAncestors().OfType<Control>().First(c=>c.Name is "SessionBody" or "Header");
        var origin=viewport.TranslatePoint(new Point(0,0),window)!.Value;
        if(point.X<origin.X-1||point.Y<origin.Y-1||point.X+control.Bounds.Width>origin.X+viewport.Bounds.Width+1||point.Y+control.Bounds.Height>origin.Y+viewport.Bounds.Height+1)
            throw new Exception($"{name} is clipped by {viewport.Name}: {point}, {control.Bounds.Size}");
    }
}

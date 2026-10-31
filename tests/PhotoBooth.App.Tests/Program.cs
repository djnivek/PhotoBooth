using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PhotoBooth.App;
using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;

var root = Path.Combine(Path.GetTempPath(), "colors-party-ui-" + Guid.NewGuid().ToString("N"));
Environment.SetEnvironmentVariable("PHOTOBOOTH_DATA_DIR", root);
AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
var screenshots = Path.Combine(Environment.CurrentDirectory, ".impeccable", "review");
Directory.CreateDirectory(screenshots);
var openedFolders = new List<string>();
var catalogCalls = 0; var failCatalog = false;
var serverCatalog = StyleCatalog.BuiltIn;
Task<StyleCatalog> LoadCatalog(CancellationToken token)
{ catalogCalls++; if(failCatalog)throw new IOException("Offline fixture");return Task.FromResult(serverCatalog); }
var transform = new DelayedTransform();
var window = new MainWindow((_, token) => Task.Delay(140, token), openedFolders.Add,
    catalogLoader:LoadCatalog, transformFactory:()=>transform); window.Show();
Pump(200);
if(catalogCalls!=1)throw new Exception("Expected exactly one startup catalog request.");
Capture("desktop-1280x800.png"); AssertHomeFits();
window.Width = 1024; window.Height = 768; Pump(80); Capture("desktop-1024x768.png"); AssertHomeFits();
window.Width = 960; window.Height = 700; Pump(80); Capture("desktop-960x700.png"); AssertHomeFits();
window.Width = 1280; window.Height = 800; Pump(80);
Click("StartSession"); Until(() => Find("TakePhotos") is {IsEnabled:true,IsVisible:true}, 3000);
Capture("positioning.png"); Click("TakePhotos");
Until(()=>window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="3"),3000);Capture("countdown.png");
Until(()=>window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Gardez la pose"),3000);Capture("capture-flash.png");
Until(() => Find("PrintPreview") is not null, 10000);
Capture("result.png"); Click("PrintPreview");
Until(() => Directory.GetFiles(root, "print-preview.jpg", SearchOption.AllDirectories).Length == 1, 3000);
var file = Directory.GetFiles(root, "print-preview.jpg", SearchOption.AllDirectories).Single();
if(new FileInfo(file).Length<10000)throw new Exception("Print preview is empty.");
window.Width=1024;window.Height=768;Pump(80);Capture("result-1024x768.png");
window.Width=1280;window.Height=800;Pump(80);
Click("NewSession"); ClickLabel("Métamorphose"); Pump(80); Capture("surprise-choice.png");
AssertStylesFit();
window.Width=960;window.Height=700;Pump(80);AssertStylesFit();Capture("styles-960x700.png");
window.Width=1280;window.Height=800;Pump(80);
Click("BackFromStyles");ClickLabel("Métamorphose");
if(catalogCalls!=1)throw new Exception("Opening styles must never fetch the catalog.");
Click("Style_halloween");Capture("halloween-choice.png");Click("StartStyledSession");
Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);Click("TakePhotos");
Until(()=>transform.Entered.Task.IsCompleted,10000);Pump(100);Capture("processing-halloween.png");
if(!window.ProcessingAnimationRunning)throw new Exception("Processing animation did not start.");
transform.Resume.SetResult();Until(()=>Find("PrintPreview") is not null,10000);Capture("surprise-result.png");
if(window.ProcessingAnimationRunning)throw new Exception("Processing timer leaked after result.");
if(transform.Choice!="halloween")throw new Exception("Selected style was lost before image processing.");
Click("Settings");Until(()=>window.OwnedWindows.Count>0,2000);
var settings=window.OwnedWindows.Single();Pump(80);CaptureDialog(settings,"settings.png");
((ScrollViewer)settings.Content!).Offset=new Vector(0,10000);Pump(80);CaptureDialog(settings,"settings-bottom.png");
settings.GetVisualDescendants().OfType<CheckBox>().Single(t=>t.Name=="ProtectSettings").IsChecked=true;
settings.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="ExitPin").Text="E629";
DialogClick(settings,"Activer le plein écran");Until(()=>window.WindowState==WindowState.FullScreen,2000);
if(!settings.IsVisible)throw new Exception("Entering fullscreen must leave settings open.");
if(Find("Settings")!.IsVisible)throw new Exception("Settings button leaked into fullscreen guest UI.");
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);
// Short press, moving away, and release must not authorize or open the protected panel.
HoldCorner(350);Pump(100);if(window.OwnedWindows.Count!=0)throw new Exception("Short corner tap opened settings.");
var corner=new Point(20,window.ClientSize.Height-20);
window.MouseDown(corner,MouseButton.Left);Pump(100);window.MouseMove(new Point(100,window.ClientSize.Height-20));Pump(3050);window.MouseUp(corner,MouseButton.Left);
if(window.OwnedWindows.Count!=0)throw new Exception("Leaving the corner failed to cancel its hold.");
HoldCorner(3150);Until(()=>window.OwnedWindows.Count==1,2000);
var unlock=window.OwnedWindows.Single();CaptureDialog(unlock,"unlock.png");
unlock.GetVisualDescendants().OfType<TextBox>().Single().Text="0000";DialogClick(unlock,"Déverrouiller");
Until(()=>unlock.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.StartsWith("Code incorrect")==true),2000);CaptureDialog(unlock,"unlock-error.png");
if(window.WindowState!=WindowState.FullScreen)throw new Exception("Wrong code exited fullscreen.");
unlock.GetVisualDescendants().OfType<TextBox>().Single().Text="E629";DialogClick(unlock,"Déverrouiller");
Until(()=>window.OwnedWindows.Any(w=>w.Title=="Réglages de la borne"),3000);settings=window.OwnedWindows.Single();
if(window.WindowState!=WindowState.FullScreen)throw new Exception("Corner unlock should preserve fullscreen.");
if(File.ReadAllText(Path.Combine(root,"kiosk.json")).Contains("E629"))throw new Exception("Plain code stored.");
// Refresh supports newly added and removed universes; unknown artwork uses local fallback.
var added=new PhotoStyle("ocean","Sous l’océan","Une fête sous les vagues.","new_unbundled_asset","#512831",["Les poissons préparent la fête."]);
serverCatalog=new("test.2",StyleCatalog.BuiltIn.Styles.Where(s=>s.Id!="halloween").Append(added).ToArray());
DialogClick(settings,"Actualiser les univers");Until(()=>catalogCalls==2,2000);
failCatalog=true;DialogClick(settings,"Actualiser les univers");Until(()=>catalogCalls==3,2000);
if(window.CatalogCount!=6)throw new Exception("Network failure replaced the in-memory catalog.");
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);
// Preserve the existing desktop escape guard after the operator-access refactor.
Click("OpenFiles");Until(()=>window.OwnedWindows.Count==1,2000);unlock=window.OwnedWindows.Single();
DialogClick(unlock,"Retour à la fête");Until(()=>window.OwnedWindows.Count==0,2000);
if(openedFolders.Count!=0||window.WindowState!=WindowState.FullScreen)throw new Exception("Cancelling file access escaped the protected booth.");
Click("OpenFiles");Until(()=>window.OwnedWindows.Count==1,2000);unlock=window.OwnedWindows.Single();
unlock.GetVisualDescendants().OfType<TextBox>().Single().Text="E629";DialogClick(unlock,"Déverrouiller");
Until(()=>openedFolders.Count==1&&window.WindowState==WindowState.Normal,2000);
Click("Settings");Until(()=>window.OwnedWindows.Count==1,2000);settings=window.OwnedWindows.Single();
DialogClick(settings,"Activer le plein écran");Until(()=>window.WindowState==WindowState.FullScreen,2000);
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);
Click("NewSession");ClickLabel("Métamorphose");Pump(80);
if(Find("Style_ocean") is null||Find("Style_halloween") is not null)throw new Exception("Refreshed catalog not applied.");
if((Find("Style_ocean")!.Background as Avalonia.Media.ISolidColorBrush)?.Color!=Avalonia.Media.Color.Parse("#E0E7D8"))throw new Exception("Inaccessible server colour did not use the readable sage fallback.");
Capture("dynamic-styles.png");AssertStylesFit();
var backButton=Find("BackFromStyles")!;
var backPoint=backButton.TranslatePoint(new Point(10,backButton.Bounds.Height/2),window)!.Value;
if((window.InputHitTest(backPoint) as Control)?.Name=="AdminCorner")throw new Exception("Hidden corner intercepts the guest Retour button.");
if(catalogCalls!=3)throw new Exception("Style picker fetched again.");
// With no code configured, the same hold opens settings directly.
HoldCorner(3150);Until(()=>window.OwnedWindows.Count==1,2000);unlock=window.OwnedWindows.Single();
unlock.GetVisualDescendants().OfType<TextBox>().Single().Text="E629";DialogClick(unlock,"Déverrouiller");
Until(()=>window.OwnedWindows.Any(w=>w.Title=="Réglages de la borne"),3000);settings=window.OwnedWindows.Single();
settings.GetVisualDescendants().OfType<CheckBox>().Single(t=>t.Name=="ProtectSettings").IsChecked=false;
DialogClick(settings,"Enregistrer le code");Until(()=>!File.Exists(Path.Combine(root,"kiosk.json")),2000);
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);
var touchPoint=new Point(20,window.ClientSize.Height-20);
using(var touch=window.TouchBegin(touchPoint)) {Pump(3150);window.TouchEnd(touch,touchPoint);Pump(50);}
Until(()=>window.OwnedWindows.Any(w=>w.Title=="Réglages de la borne"),2000);settings=window.OwnedWindows.Single();
DialogClick(settings,"Quitter le plein écran");Until(()=>window.WindowState==WindowState.Normal,2000);
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);window.Close();Pump(100);
// A slow startup catalog must not replace the picker underneath a guest.
var deferredCatalog=new TaskCompletionSource<StyleCatalog>();
window=new MainWindow(catalogLoader:_=>deferredCatalog.Task);window.Show();Pump(80);
ClickLabel("Métamorphose");
deferredCatalog.SetResult(serverCatalog);Pump(80);
if(Find("Style_halloween") is null||Find("Style_ocean") is not null)throw new Exception("Background catalog changed an active picker.");
Click("BackFromStyles");ClickLabel("Métamorphose");
if(Find("Style_ocean") is null||Find("Style_halloween") is not null)throw new Exception("Pending catalog was not applied on home.");
window.Close();Pump(80);
foreach(var driverError in new[]{false,true})
{
    var delayed=new DelayedUiCamera(driverError);var cameraOpens=0;
    window=new MainWindow(refreshCatalogOnOpen:false,cameraFactory:store=>++cameraOpens==1?delayed:new FixtureCamera(store));window.Show();Pump(80);
    var start=Find("StartSession")!;Click("StartSession");
    if(start.IsEnabled)throw new Exception("Start accepts another tap while connecting.");
    Until(()=>delayed.Entered.Task.IsCompleted&&Find("CancelSession") is not null,2000);
    if(Find("TakePhotos")!.IsVisible)throw new Exception("Shutter is visible during connection.");
    Click("CancelSession");
    Until(()=>window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Annulation en cours…"),2000);
    if(Find("StartSession") is not null||cameraOpens!=1)throw new Exception("Home is available before camera cleanup.");
    if(!driverError)Capture("cancel-connection.png");
    delayed.Resume.SetResult();Until(()=>Find("StartSession") is not null,2000);
    if(window.OwnedWindows.Count!=0)throw new Exception("Cancelled driver error opened a blocking dialog.");
    Click("StartSession");Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);
    if(cameraOpens!=2)throw new Exception("Next guest did not get one fresh camera.");
    Click("CancelSession");Until(()=>Find("StartSession") is not null,2000);
    window.Close();Pump(80);
}
// Standard capture and its exact print master remain usable with cloud enabled and no network.
var cloudSettingsTask = new BoothSettings { CloudEnabled=true }.SaveAsync(Path.Combine(root,"booth-settings.json"));
Until(()=>cloudSettingsTask.IsCompleted,3000);cloudSettingsTask.GetAwaiter().GetResult();
var credentialTask = CredentialStore.SaveAsync(Path.Combine(root,"cloud-credential"),"offline-test-only");
Until(()=>credentialTask.IsCompleted,3000);credentialTask.GetAwaiter().GetResult();
foreach(var count in new[]{1,4})
{
    var offline = new OfflineHttp();
    window = new MainWindow((_,_)=>Task.CompletedTask, refreshCatalogOnOpen:false,
        cloudFactory:store=>new BoothCloudClient("https://offline.invalid/api","offline-test-only",store,handler:offline));
    window.Show();Pump(80);
    if(count==1)ClickLabel("Une photo");
    var previous = Directory.GetFiles(root,"print-preview.jpg",SearchOption.AllDirectories).ToHashSet();
    Click("StartSession");Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);Click("TakePhotos");
    Until(()=>Find("PrintPreview") is not null,10000);
    Until(()=>window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Name=="ShareStatus" && t.Text!.Contains("attend la connexion")),3000);
    if(offline.GenerationCalls!=0 || offline.Calls!=1)throw new Exception("Original mode attempted a transformation or retried offline sharing.");
    if(window.GetVisualDescendants().OfType<Image>().Single(t=>t.Name=="ShareQr").IsVisible)throw new Exception("QR exposed before publication.");
    Click("PrintPreview");
    Until(()=>Directory.GetFiles(root,"print-preview.jpg",SearchOption.AllDirectories).Except(previous).Any(),3000);
    var printed=Directory.GetFiles(root,"print-preview.jpg",SearchOption.AllDirectories).Except(previous).Single();
    using var manifest=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(printed)!,"session.json")));
    var master=Path.Combine(root,"sessions",manifest.RootElement.GetProperty("sheet").GetProperty("Key").GetString()!);
    if(!File.ReadAllBytes(printed).SequenceEqual(File.ReadAllBytes(master)))throw new Exception("Offline print differs from the reviewed sheet.");
    Capture($"offline-original-{count}.png");window.Close();Pump(80);
}
// Leaving with an early QR must keep the old session running and never steal the next guest's screen.
var progressiveHttp = new ProgressiveUiHttp();
window = new MainWindow((_,_)=>Task.CompletedTask, refreshCatalogOnOpen:false,
    cloudFactory:store=>new BoothCloudClient("https://progress.invalid/api","offline-test-only",store,handler:progressiveHttp,delay:(_,token)=>Task.Delay(20,token)));
window.Show();Pump(80);ClickLabel("Une photo");ClickLabel("Métamorphose");Click("Style_halloween");Click("StartStyledSession");
Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);Click("TakePhotos");
Until(()=>Find("ContinueOnPhone") is {IsEnabled:true} && progressiveHttp.GenerationWaiting,8000);
if(!window.GetVisualDescendants().OfType<Image>().Single(i=>i.Name=="ProcessingQr").IsVisible)throw new Exception("Early QR missing during generation.");
Capture("processing-qr-1280x800.png");window.Width=960;window.Height=700;Pump(100);Capture("processing-qr-960x700.png");
var next=Find("ContinueOnPhone")!;var nextPoint=next.TranslatePoint(new Point(),window)!.Value;
if(nextPoint.Y+next.Bounds.Height>window.ClientSize.Height||nextPoint.X+next.Bounds.Width>window.ClientSize.Width)throw new Exception("Continue-on-phone action clipped.");
Click("ContinueOnPhone");Until(()=>Find("StartSession") is not null,3000);
if(window.ProcessingAnimationRunning)throw new Exception("Processing animation leaked after leaving with QR.");
ClickLabel("Au naturel");Click("StartSession");Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);
progressiveHttp.Resume.TrySetResult();Until(()=>progressiveHttp.Shares==1,10000);
if(Find("TakePhotos") is not {IsVisible:true}||Find("PrintPreview") is not null)throw new Exception("Background result replaced next guest's capture screen.");
Click("TakePhotos");Until(()=>Find("PrintPreview") is not null&&progressiveHttp.Shares==2,10000);Pump(100);
if(Find("ShareRetry") is {IsVisible:true})throw new Exception("Retry share visible after successful publication.");
if(Find("NewSession") is not {IsVisible:true} newSession||!newSession.Classes.Contains("primary"))throw new Exception("New session is not the prominent primary action.");
Capture("shared-result-960x700.png");window.Close();Pump(100);

// Projection enabled: fixed button footprint, pause for settings, automatic release, disable restores manual flow.
var viewerSettings=new BoothSettings {CloudEnabled=true,ViewerEnabled=true,ViewerIncludeTests=true};
var saveViewer=viewerSettings.SaveAsync(Path.Combine(root,"booth-settings.json"));Until(()=>saveViewer.IsCompleted,3000);saveViewer.GetAwaiter().GetResult();
var viewerClock=DateTimeOffset.UtcNow;var shareGate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var viewerHttp=new ProgressiveUiHttp {ShareGate=shareGate.Task};
window=new MainWindow((_,_)=>Task.CompletedTask,refreshCatalogOnOpen:false,clock:()=>viewerClock,
    cloudFactory:store=>new BoothCloudClient("https://progress.invalid/api","offline-test-only",store,handler:viewerHttp));
window.Show();Pump(80);ClickLabel("Une photo");Click("StartSession");Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);Click("TakePhotos");Until(()=>Find("NewSession") is not null,10000);Pump(300);
if(!window.ResultReturnRunning||window.ResultReturnSeconds!=60)throw new Exception("60 second result timer missing");
viewerClock=viewerClock.AddSeconds(70);Pump(300);if(window.ResultReturnSeconds!=60||Find("NewSession") is null)throw new Exception("Unfinished sharing must pause auto-return");
shareGate.SetResult();Until(()=>viewerHttp.Shares==1,3000);Pump(100);
if(!window.GetVisualDescendants().Any(c=>c.Name=="ProjectionGuestMessage"))throw new Exception("Room projection message missing");
var sixtyBounds=Find("NewSession")!.Bounds.Size;Capture("viewer-result-1280x800.png");
viewerClock=viewerClock.AddSeconds(51);Pump(300);if(window.ResultReturnSeconds!=9||Find("NewSession")!.Bounds.Size!=sixtyBounds)throw new Exception("Button moved at single digits");
Click("Settings");Until(()=>window.OwnedWindows.Count==1,2000);settings=window.OwnedWindows.Single();viewerClock=viewerClock.AddSeconds(90);Pump(300);
if(window.ResultReturnSeconds!=9)throw new Exception("Settings did not pause auto-return");
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);
window.Width=960;window.Height=700;Pump(80);viewerClock=viewerClock.AddSeconds(8);Pump(300);Capture("viewer-result-960x700.png");
var oneBounds=Find("NewSession")!.Bounds.Size;viewerClock=viewerClock.AddMilliseconds(100);Pump(250);if(Find("NewSession")!.Bounds.Size!=oneBounds)throw new Exception("Countdown changed button dimensions");
viewerClock=viewerClock.AddSeconds(1);Until(()=>Find("StartSession") is not null,3000);Pump(300);
if(window.ResultReturnRunning||Directory.GetFiles(Path.Combine(root,"projection"),"*.json",SearchOption.AllDirectories).Length!=1)throw new Exception("Auto-return must enqueue exactly one release and stop its timer");
// Disable from the actual settings UI; repeated tick cannot return another result.
Click("Settings");Until(()=>window.OwnedWindows.Count==1,2000);settings=window.OwnedWindows.Single();
settings.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Name=="ViewerEnabled").IsChecked=false;
DialogClick(settings,"Enregistrer la projection");Until(()=>!BoothSettings.Load(Path.Combine(root,"booth-settings.json")).ViewerEnabled,3000);
DialogClick(settings,"Fermer les réglages");Until(()=>window.OwnedWindows.Count==0,2000);
Click("StartSession");Until(()=>Find("TakePhotos") is {IsEnabled:true,IsVisible:true},3000);Click("TakePhotos");Until(()=>Find("NewSession") is not null,10000);
viewerClock=viewerClock.AddMinutes(2);Pump(300);if(window.ResultReturnRunning||Find("NewSession") is null||window.GetVisualDescendants().Any(c=>c.Name=="NewSessionCountdown"))throw new Exception("Disabled mode regressed manual result flow");
Click("PrintPreview");Click("NewSession");Until(()=>Find("StartSession") is not null,3000);window.Close();Pump(100);
Console.WriteLine("PASS UI: native flows, cloud offline, early QR, visible loading and wait copy, background completion during next guest, no share retry after success, prominent new-session action. No network, paid API or hardware.");
Directory.Delete(root,true);
void HoldCorner(int milliseconds)
{var p=new Point(20,window.ClientSize.Height-20);window.MouseMove(p);window.MouseDown(p,MouseButton.Left);Pump(milliseconds);window.MouseUp(p,MouseButton.Left);Pump(50);}
void DialogClick(Window dialog,string label)=>dialog.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
void ClickLabel(string label){Pump(50);window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(50);}
void CaptureDialog(Window dialog,string name)
{dialog.InvalidateVisual();Pump(100);using var image=dialog.CaptureRenderedFrame()!;image.Save(Path.Combine(screenshots,name),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}
void AssertStylesFit()
{foreach(var b in window.GetVisualDescendants().OfType<Button>().Where(b=>b.Name?.StartsWith("Style_")==true||b.Name=="StartStyledSession"))
 {var p=b.TranslatePoint(new Point(),window)!.Value;if(p.X<0||p.Y<0||p.X+b.Bounds.Width>window.ClientSize.Width+1||p.Y+b.Bounds.Height>window.ClientSize.Height+1)throw new Exception("Style button clipped: "+b.Name);}}
Button? Find(string name) => window.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == name);
void Click(string name)
{
    var button = Find(name) ?? throw new Exception("Missing button " + name);
    if (!button.IsEnabled || !button.IsVisible) throw new Exception("Unavailable button " + name);
    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(30);
}
void Pump(int milliseconds)
{
    // Run the platform loop too: RunJobs alone excludes timer events and would
    // never exercise the real hold gesture or processing animation.
    using var cancellation = new CancellationTokenSource(milliseconds);
    Dispatcher.UIThread.MainLoop(cancellation.Token);
}
void Until(Func<bool> predicate, int timeout)
{
    var end = DateTime.UtcNow.AddMilliseconds(timeout);
    while (!predicate()) { if (DateTime.UtcNow > end) throw new Exception("UI condition timed out."); Pump(10); }
    Pump(40);
}
void Capture(string name)
{
    window.InvalidateVisual(); Pump(100);
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

sealed class DelayedUiCamera(bool driverError):IPhotoCamera
{
    public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Resume {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task<CameraStatus> ConnectAsync(CancellationToken token)
    {Entered.SetResult();await Resume.Task;if(driverError)throw new System.Runtime.InteropServices.COMException("Late driver failure");return new(true,"Delayed fixture",true,true);}
    public async IAsyncEnumerable<LiveViewFrame> LiveViewAsync([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken token)
    {await Task.Delay(Timeout.Infinite,token);yield break;}
    public Task<CapturedPhoto> CaptureAsync(Guid session,int shot,CancellationToken token)=>throw new InvalidOperationException("Unexpected capture");
    public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
}

sealed class DelayedTransform:IPhotoTransform
{
    public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Resume {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string? Choice;
    public async Task<TransformResult> TransformAsync(Guid id,IReadOnlyList<byte[]> photos,IProgress<string> progress,CancellationToken token,string styleChoice=StyleCatalog.Chef)
    {Choice=styleChoice;Entered.TrySetResult();await Resume.Task.WaitAsync(token);return await new RehearsalTransform().TransformAsync(id,photos,progress,token,styleChoice);}
}

sealed class OfflineHttp:HttpMessageHandler
{
    public int Calls; public int GenerationCalls;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    { Calls++;if(request.RequestUri!.AbsolutePath.Contains("/photos/"))GenerationCalls++;throw new HttpRequestException("Network disconnected"); }
}

sealed class ProgressiveUiHttp:HttpMessageHandler
{
    public TaskCompletionSource Resume {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool GenerationWaiting;public int Shares;public Task? ShareGate;
    private readonly HashSet<string> started=[];
    private readonly Dictionary<string,string> galleryTokens=[];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        var path=request.RequestUri!.AbsolutePath;
        HttpResponseMessage Json(object value)=>new(System.Net.HttpStatusCode.OK){Content=System.Net.Http.Json.JsonContent.Create(value)};
        if(path.Contains("/storage/"))return new(System.Net.HttpStatusCode.OK){Content=new ByteArrayContent(FixtureCamera.CreateFixture(0))};
        if(path.EndsWith("/sessions"))return Json(new{style="halloween"});
        if(path.EndsWith("/gallery")||path.EndsWith("/share"))
        {
            using var input=System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var shareToken=input.RootElement.GetProperty("token").GetString()!;var id=path.Split('/')[^2];
            if(galleryTokens.TryGetValue(id,out var previous)&&previous!=shareToken)throw new Exception("Gallery token changed before final publication.");
            galleryTokens[id]=shareToken;
            if(path.EndsWith("/share")){if(ShareGate is not null)await ShareGate.WaitAsync(token);Shares++;}
            return Json(new{gallery_url="https://gallery.invalid/#"+shareToken,expires_at=DateTimeOffset.UtcNow.AddDays(7),download_url="https://progress.invalid/storage/archive.zip"});
        }
        if(path.Contains("/assets/"))return Json(new{sha256=SessionStore.Hash(await request.Content!.ReadAsByteArrayAsync(token))});
        if(path.Contains("/photos/"))
        {
            if(request.Method==HttpMethod.Post){started.Add(path);return Json(new{state="running"});}
            if(!started.Contains(path))return Json(new{state="queued"});
            GenerationWaiting=true;await Resume.Task.WaitAsync(token);
            return Json(new{state="ready",url="https://progress.invalid/storage/photo.jpg"});
        }
        throw new Exception("Unexpected fake request "+path);
    }
}

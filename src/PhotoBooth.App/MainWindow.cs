using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PhotoBooth.Core;
using PhotoBooth.Imaging;
using PhotoBooth.Runtime;
using PhotoBooth.Windows;
using QRCoder;

namespace PhotoBooth.App;

public sealed partial class MainWindow : Window
{
    private static readonly IBrush Ink = Brush.Parse("#512831"), Muted = Brush.Parse("#705952"), Lemon = Brush.Parse("#EFD789");
    private readonly ContentControl body = new()
    { Name = "SessionBody", HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private TextBlock status = Text("", 22);
    private readonly SessionStore store;
    private BoothController controller;
    private int photoCount = 4;
    private PhotoMode mode = PhotoMode.Original;
    private string[] fixtureFiles = [];
    private string screen = "home";
    private bool compactHome;
    private Image? liveImage;
    private TextBlock? countdown;
    private Border? countdownBadge;
    private Border? flashVeil;
    private readonly DispatcherTimer flashTimer = new() { Interval = TimeSpan.FromMilliseconds(170) };
    private int displayedCaptures;
    private bool liveViewReady;
    private byte[]? pendingFrame;
    private int frameQueued;
    private TextBlock? pose;
    private Button? capture;
    private Bitmap? currentBitmap;
    private KioskExitGate? exitGate;
    private bool kioskLocked;
    private readonly string settingsPath;
    private readonly string boothSettingsPath;
    private BoothSettings boothSettings = new();
    private readonly string credentialPath;
    private string cloudKey = "";
    private BoothCloudClient? cloud;
    private readonly Dictionary<string,BoothTelemetry> telemetryClients = [];
    private CancellationTokenSource? sharingCancellation;
    private readonly DispatcherTimer pulse = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private DateTimeOffset pulseStarted;
    private readonly Func<TimeSpan, CancellationToken, Task>? rehearsalDelay;
    private readonly Action<string> openFolder;
    private readonly Func<SessionStore, IPhotoCamera>? cameraFactoryOverride;
    private readonly Func<IPhotoTransform>? transformFactoryOverride;
    private readonly Func<SessionStore, BoothCloudClient>? cloudFactoryOverride;

    public MainWindow(Func<TimeSpan, CancellationToken, Task>? rehearsalDelay = null, Action<string>? openFolder = null,
        Func<SessionStore, IPhotoCamera>? cameraFactory = null,
        Func<CancellationToken, Task<StyleCatalog>>? catalogLoader = null, bool refreshCatalogOnOpen = true, Func<IPhotoTransform>? transformFactory = null,
        Func<SessionStore, BoothCloudClient>? cloudFactory = null, Func<DateTimeOffset>? clock = null)
    {
        this.rehearsalDelay = rehearsalDelay;
        this.clock=clock??(()=>DateTimeOffset.UtcNow);
        resultReturn.Tick+=ReturnTick;
        this.openFolder = openFolder ?? OpenFolder;
        cameraFactoryOverride = cameraFactory; transformFactoryOverride = transformFactory;
        cloudFactoryOverride = cloudFactory;
        this.catalogLoader = catalogLoader ?? (token => StyleCatalogClient.FetchAsync(boothSettings.ApiBaseUrl, token));
        Title = "Colors Party by Estelle";
        Width = 1280; Height = 800; MinWidth = 960; MinHeight = 700;
        if (Screens.Primary is { } display)
        {
            Width = Math.Max(MinWidth, Math.Min(Width, display.WorkingArea.Width / display.Scaling - 32));
            Height = Math.Max(MinHeight, Math.Min(Height, display.WorkingArea.Height / display.Scaling - 48));
        }
        compactHome = Width < 1100 || Height < 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = Environment.GetEnvironmentVariable("PHOTOBOOTH_DATA_DIR") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EstellArt");
        store = new SessionStore(Path.Combine(root, "sessions"));
        settingsPath = Path.Combine(root, "kiosk.json");
        boothSettingsPath = Path.Combine(root, "booth-settings.json");
        credentialPath = Path.Combine(root, "cloud-credential");
        try { boothSettings = BoothSettings.Load(boothSettingsPath); } catch (Exception) { /* Keep the real-hardware defaults if a profile is unreadable. */ }
        try { boothSettings = boothSettings with { PrinterName = WindowsPhotoPrinter.SelectPrinter(boothSettings.PrinterName, WindowsPhotoPrinter.ListPrinters()) }; }
        catch (Exception) { /* A missing spooler must not block operator access. */ }
        try { cloudKey = CredentialStore.Load(credentialPath); } catch (Exception) { boothSettings = boothSettings with { CloudEnabled = false }; }
        if (cloudKey.Length == 0) boothSettings = boothSettings with { CloudEnabled = false };
        if (File.Exists(settingsPath))
        {
            try { exitGate = KioskExitGate.Restore(JsonSerializer.Deserialize<KioskExitGate.Credential>(File.ReadAllText(settingsPath))!); }
            catch (Exception) { /* A new PIN is required before entering fullscreen. */ }
        }
        controller = CreateController();
        var header = new Grid { Name = "Header", ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 24) };
        header.Children.Add(Text("Colors Party by Estelle", 26, FontWeight.SemiBold));
        adminButton = Button("Réglages", RequestOperatorAsync); var admin = adminButton; admin.Name = "Settings";
        Grid.SetColumn(admin, 1); header.Children.Add(admin);
        var layout = new Grid { Name = "AppLayout", RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(36, 24) };
        layout.Children.Add(header); Grid.SetRow(body, 1); layout.Children.Add(body);
        var host = new Grid(); host.Children.Add(layout);
        InstallAdminCorner(host); Content = host;
        // Windows can clamp the requested startup size after the first layout pass.
        // Keep the grid's measured minimum tied to the actual viewport, including later resizes.
        void ConstrainLayout()
        {
            layout.MaxWidth = Math.Max(0, ClientSize.Width - layout.Margin.Left - layout.Margin.Right);
            layout.MaxHeight = Math.Max(0, ClientSize.Height - layout.Margin.Top - layout.Margin.Bottom);
            var compact = ClientSize.Width < 1100 || ClientSize.Height < 760;
            if (compactHome != compact)
            {
                compactHome = compact;
                if (screen == "home") RenderHome();
                else if (screen == "styles") RenderStyles();
            }
        }
        Opened += async (_, _) =>
        {
            ConstrainLayout();
            if (boothSettings.FullscreenEnabled || Program.ForceKiosk) EnterFullscreen();
            // Persist the one-time hardware migration without changing the code or cloud credential.
            try { await boothSettings.SaveAsync(boothSettingsPath); }
            catch (IOException) { /* Settings remain usable; an explicit save will report any write failure. */ }
            catch (UnauthorizedAccessException) { /* A read-only profile must not prevent the home screen. */ }
            if (refreshCatalogOnOpen) _ = RefreshCatalogAsync();
        };
        Resized += (_, _) => ConstrainLayout();
        pulse.Tick += (_, _) =>
        {
            if (countdown is null || boothSettings.ReducedMotion) { pulse.Stop(); return; }
            var age = (DateTimeOffset.UtcNow - pulseStarted).TotalMilliseconds;
            var scale = 1 + .12 * Math.Exp(-age / 110);
            countdown.RenderTransform = new ScaleTransform(scale, scale);
            if (age > 550) pulse.Stop();
        };
        flashTimer.Tick += (_, _) => { if (flashVeil is not null) flashVeil.Opacity = 0; flashTimer.Stop(); };
        Closing += (_, e) =>
        {
            if (kioskLocked) e.Cancel = true;
        };
        Closed += async (_, _) => { resultReturn.Stop(); pulse.Stop(); flashTimer.Stop(); StopProcessing(); cornerTimer.Stop(); lifetime.Cancel(); sharingCancellation?.Cancel(); await controller.DisposeAsync(); foreach (var pending in detachedControllers.ToArray()) await pending.DisposeAsync(); foreach(var metrics in telemetryClients.Values) await metrics.DisposeAsync(); foreach(var viewer in projectionClients.Values) await viewer.DisposeAsync(); currentBitmap?.Dispose(); resultBitmap?.Dispose(); foreach (var bitmap in artwork.Values) bitmap.Dispose(); lifetime.Dispose(); };
        Closed += (_, _) => shortcutGuard?.Dispose();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F11 || (e.Key == Key.Escape && kioskLocked))
                e.Handled = true;
        };
        RenderHome();
    }

    private BoothController CreateController()
    {
        PrepareProjection();
        cloud = boothSettings.CloudEnabled ? cloudFactoryOverride?.Invoke(store) ?? new BoothCloudClient(boothSettings.ApiBaseUrl, cloudKey, store, boothSettings.GalleryBaseUrl) : null;
        BoothTelemetry? metrics=null;
        try
        {
            var metricKey = SessionStore.Hash(System.Text.Encoding.UTF8.GetBytes(boothSettings.ApiBaseUrl+cloudKey+boothSettings.CanonCamera));
            if(!telemetryClients.TryGetValue(metricKey,out metrics))
            {
                metrics = new BoothTelemetry(Path.GetDirectoryName(store.Root)!,boothSettings.ApiBaseUrl,cloudKey,boothSettings.CanonCamera ? "camera" : "fixture",automatic:cloudFactoryOverride is null && cameraFactoryOverride is null);
                telemetryClients[metricKey]=metrics;
            }
        }
        catch(Exception) { /* Monitoring configuration never blocks the local booth. */ }
        if(cloud is not null) cloud.Telemetry=metrics;
        var result = new BoothController(store, () => cameraFactoryOverride is not null ? cameraFactoryOverride(store) : boothSettings.CanonCamera ?
            (boothSettings.CanonUseOfficialSdk ? new CanonCamera(store, boothSettings.CanonManualFocusConfirmed) : new CanonWpdCamera(store, manualFocusConfirmed: boothSettings.CanonManualFocusConfirmed)) : new FixtureCamera(store, fixtureFiles),
            transformFactoryOverride?.Invoke() ?? (IPhotoTransform?)cloud ?? new RehearsalTransform(), rehearsalDelay, metrics);
        var client = cloud;
        result.Changed += () => Dispatcher.UIThread.Post(() =>
        {
            if (controller == result) UpdateSession();
            else if (detachedControllers.Contains(result) && !result.IsBusy) _ = CompleteDetachedAsync(result, client);
        });
        if (client is not null) client.GalleryAvailable += (id, link) => Dispatcher.UIThread.Post(() =>
        {
            galleries[id] = link;
            if (controller == result && controller.Session?.Id == id && screen == "processing") RenderProcessing();
        });
        result.Frame += bytes =>
        {
            Interlocked.Exchange(ref pendingFrame, bytes);
            if (Interlocked.Exchange(ref frameQueued, 1) != 0) return;
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var latest = Interlocked.Exchange(ref pendingFrame, null);
                    if (latest is null || controller != result || screen != "capture" || liveImage is null || controller.Session?.WantsLiveView != true) return;
                    ReplaceImage(liveImage, latest); liveViewReady = true;
                    if (capture is not null && controller.Session?.Stage == SessionStage.Positioning && !controller.IsBusy) capture.IsEnabled = true;
                }
                finally { Interlocked.Exchange(ref frameQueued, 0); }
            });
        };
        return result;
    }

    private void RenderCapture()
    {
        StopProcessing(); screen = "capture";
        liveViewReady=false; displayedCaptures = 0;
        // Detached screens still own their controls; a new session needs a new status view.
        status = Text("", 22);
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        pose = Text("Prenez place dans le cadre.", 30, FontWeight.SemiBold); layout.Children.Add(pose);
        var stage = new Grid { Background = Brush.Parse("#150F1C"), Margin = new Thickness(0, 20) };
        liveImage = new Image { Stretch = Stretch.Uniform };
        stage.Children.Add(liveImage);
        flashVeil = new Border { Background = Brushes.White, Opacity = 0, IsHitTestVisible = false };
        stage.Children.Add(flashVeil);
        countdown = Text("", 152, FontWeight.Bold); countdown.Foreground = Brushes.White;
        countdown.HorizontalAlignment = HorizontalAlignment.Center; countdown.VerticalAlignment = VerticalAlignment.Center;
        countdown.RenderTransformOrigin = RelativePoint.Center;
        countdownBadge = new Border { Child = countdown, Background = Ink, Padding = new Thickness(26, 0),
            CornerRadius = new CornerRadius(16), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
        stage.Children.Add(countdownBadge);
        Grid.SetRow(stage, 1); layout.Children.Add(stage);
        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var back = Button("Annuler", async () => { await controller.CancelAsync(); if (!controller.IsBusy) RenderHome(); });
        back.Name = "CancelSession"; actions.Children.Add(back);
        status.HorizontalAlignment = HorizontalAlignment.Center; status.VerticalAlignment = VerticalAlignment.Center;
        status.Margin = new Thickness(18, 0); Grid.SetColumn(status, 1); actions.Children.Add(status);
        capture = Button("On est prêts !", () => controller.TakeAsync(), primary: true); capture.Name = "TakePhotos";
        capture.IsEnabled=false;
        Grid.SetColumn(capture, 2); actions.Children.Add(capture);
        Grid.SetRow(actions, 2); layout.Children.Add(actions); body.Content = layout;
    }

    private void UpdateSession()
    {
        var session = controller.Session; if (session is null) return;
        if (session.Stage == SessionStage.Ready) { if (screen != "result") RenderResult(); return; }
        if (session.Stage == SessionStage.Cancelled)
        {
            if (!controller.IsBusy) RenderHome();
            else { status.Text = "Annulation en cours…"; if (capture is not null) capture.IsVisible = false; }
            return;
        }
        if (session.Stage == SessionStage.Failed) { RenderError(controller.Message); return; }
        if (session.Stage == SessionStage.Processing) { if (screen != "processing") RenderProcessing(); processingProgress!.Text = controller.Message; return; }
        if (screen != "capture") RenderCapture();
        status.Text = controller.Message;
        var received = session.Captures.Count > displayedCaptures;
        displayedCaptures = session.Captures.Count;
        if (received && !boothSettings.ReducedMotion && flashVeil is not null)
        { flashVeil.Opacity = .18; flashTimer.Start(); }
        if (pose is not null) pose.Text = session.Stage == SessionStage.Processing ? "On prépare vos souvenirs…" :
            $"Pose {Math.Min(session.Captures.Count + 1, session.Options.PhotoCount)} sur {session.Options.PhotoCount}";
        if (capture is not null) { capture.IsVisible = session.Stage == SessionStage.Positioning && !controller.IsBusy; capture.IsEnabled=liveViewReady; }
        if (countdown is not null)
        {
            var next = session.Stage == SessionStage.Countdown ? session.SecondsRemaining.ToString() : session.Stage == SessionStage.Capturing ? "Gardez la pose" : "";
            if (countdownBadge is not null) countdownBadge.IsVisible = next.Length > 0;
            if (next != countdown.Text) { countdown.Text = next; countdown.FontSize = session.Stage == SessionStage.Countdown ? 152 : 40; pulseStarted = DateTimeOffset.UtcNow; if (!boothSettings.ReducedMotion) pulse.Start(); }
        }
    }

    private void RenderResult()
    {
        StopProcessing(); screen = "result"; pulse.Stop();
        resultShareReady=cloud is null;
        var resultRoot = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), Margin = new Thickness(0,18,0,0) };
        Grid.SetRow(footer,1); resultRoot.Children.Add(footer);
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("7*,4*") };
        resultRoot.Children.Add(layout);
        var left = new StackPanel { Spacing = 22, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 30, 0) };
        left.Children.Add(Text("Ça, c’est un souvenir.", 38, FontWeight.SemiBold));
        resultBitmap?.Dispose(); resultBitmap = new Bitmap(store.Resolve(controller.Sheet!.Key));
        var image = new Image { Source = resultBitmap, Stretch = Stretch.Uniform, MaxHeight = 430 };
        left.Children.Add(image); layout.Children.Add(left);
        var actions = new StackPanel { Spacing = 20, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(Text(controller.Session!.EffectiveMode == PhotoMode.Surprise ?
            "Votre métamorphose est prête." : "Tout simplement vous.", 30, FontWeight.SemiBold));
        var note = Text(controller.Session.UsedOriginalFallback ? "Vous avez choisi de garder les photos originales." :
            "Votre planche est enregistrée sur cet ordinateur.", 19);
        note.Foreground = Muted; left.Children.Add(note);
        var printState = Text("", 18);
        var print = Button("Imprimer", async () =>
        {
            Interlocked.Increment(ref printingResults);
            try {
            if (boothSettings.PhysicalPrinting)
            {
                var printer = WindowsPhotoPrinter.SelectPrinter(boothSettings.PrinterName, WindowsPhotoPrinter.ListPrinters());
                var submission = await controller.PrintAsync(new WindowsPhotoPrinter(store, controller.Sheet!, printer));
                printState.Text = $"Envoyé à l’imprimante (travail {submission.SpoolJobId}). Récupérez votre feuille après sa sortie.";
                return;
            }
            var sessionId = controller.Session.Id;
            var bytes = await controller.GetPrintBytesAsync();
            var target = Path.Combine(store.Root, sessionId.ToString("N"), "print-preview.jpg");
            await SessionStore.WriteAtomicAsync(target, bytes);
            printState.Text = "Votre planche est enregistrée.";
            } finally {Interlocked.Decrement(ref printingResults);}
        }); print.Name = "PrintPreview"; footer.Children.Add(print); left.Children.Add(printState);
        var folder = Path.GetDirectoryName(store.Resolve(controller.Sheet.Key))!;
        var files = Button("Voir les fichiers", () =>
        {
            if (!kioskLocked) openFolder(folder);
            return Task.CompletedTask;
        });
        resultFiles = files; files.IsVisible = !kioskLocked;
        files.Name = "OpenFiles"; files.Margin = new Thickness(12,0,12,0); Grid.SetColumn(files,1); footer.Children.Add(files);
        var shareStatus = Text(cloud is null ? "Partage en ligne désactivé dans les réglages." : "Préparation de votre lien…", 17); shareStatus.Name = "ShareStatus";
        var qr = new Image { Name = "ShareQr", Width = 230, Height = 230, IsVisible = false, HorizontalAlignment = HorizontalAlignment.Left };
        Button? retry = null;
        retry = Button("Réessayer le partage", () => ShareAsync(qr, shareStatus, retry!)); retry.Name = "ShareRetry"; retry.IsVisible = false;
        actions.Children.Add(qr); actions.Children.Add(shareStatus); actions.Children.Add(retry);
        AddProjectionNote(actions);
        var again = Button("Une nouvelle séance", StartNextSessionAsync, primary: true);
        again.Name = "NewSession"; again.HorizontalAlignment = HorizontalAlignment.Stretch;
        PrepareReturnButton(again);
        Grid.SetColumn(again, 2); footer.Children.Add(again);
        var scrollingActions = new ScrollViewer { Content = actions };
        Grid.SetColumn(scrollingActions, 1); layout.Children.Add(scrollingActions); body.Content = resultRoot;
        if (cloud is not null) _ = ShareAsync(qr, shareStatus, retry);
    }

    private void RenderError(string message)
    {
        StopProcessing(); screen = "error";
        var panel = new StackPanel { Spacing = 24, MaxWidth = 720, VerticalAlignment = VerticalAlignment.Center,
            Children = { Text("Une petite pause.", 48, FontWeight.SemiBold), Text(message, 23),
                Text("Les photos déjà enregistrées restent sur cet ordinateur.", 19),
                Button("Revenir à l’accueil", () => { RenderHome(); return Task.CompletedTask; }, true) } };
        if (controller.Session is { Stage: SessionStage.Failed, Options.Mode: PhotoMode.Surprise } session &&
            session.Captures.Count == session.Options.PhotoCount)
            panel.Children.Insert(3,Button("Garder les originaux",()=>controller.UseOriginalsAsync(),true));
        body.Content = panel;
    }

    private async Task ShowSettingsAsync()
    {
        if (!operatorAccess) { await RequestOperatorAsync(); return; }
        if (controller.IsBusy) return;
        var settings = Dialog("Réglages de la borne", 690, 680);
        var pin = new TextBox { PasswordChar = '●', MaxLength = 4 };
        var info = Text(boothSettings.CloudEnabled ? "Connexion en ligne active. Le mode Métamorphose lance des appels OpenAI payants." :
            "Connexion en ligne désactivée. Aucun appel OpenAI ne sera facturé.", 19);
        var content = new StackPanel { Spacing = 18, Margin = new Thickness(30) };
        content.Children.Add(Text("Prêts pour la fête ?", 34, FontWeight.SemiBold));
        content.Children.Add(info);
        var canon = new CheckBox { Name = "CanonCamera", Content = "Utiliser le Canon USB (Windows)", IsChecked = boothSettings.CanonCamera };
        var canonSdk = new CheckBox { Content = "Connexion avancée : SDK Canon officiel installé", IsChecked = boothSettings.CanonUseOfficialSdk };
        var printing = new CheckBox { Name = "PhysicalPrinting", Content = "Envoyer à l’imprimante", IsChecked = boothSettings.PhysicalPrinting };
        var installedPrinters = WindowsPhotoPrinter.ListPrinters();
        var printers = new ComboBox { Name = "PhotoPrinter", ItemsSource = installedPrinters, SelectedItem = WindowsPhotoPrinter.SelectPrinter(boothSettings.PrinterName, installedPrinters),
            PlaceholderText = "Choisir la DNP DS620", HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Children.Add(canon); content.Children.Add(canonSdk);
        content.Children.Add(Text("Par défaut, connexion par le pilote Windows existant, sans SDK à installer. Compatibilité à vérifier sur le boîtier.",15));
        var manualFocus = new CheckBox { Content = "Mise au point manuelle vérifiée sur le Canon", IsChecked = boothSettings.CanonManualFocusConfirmed };
        content.Children.Add(manualFocus);
        content.Children.Add(Text("À activer uniquement après avoir réglé l’objectif sur MF et vérifié la netteté à la distance de pose.", 15));
        content.Children.Add(printing); content.Children.Add(printers);
        content.Children.Add(Button("Enregistrer le matériel", async () =>
        {
            if ((canon.IsChecked == true || printing.IsChecked == true) && !OperatingSystem.IsWindows())
                throw new InvalidOperationException("Ces deux connexions nécessitent Windows.");
            var name = printers.SelectedItem as string ?? "";
            if (printing.IsChecked == true && name.Length == 0) throw new InvalidOperationException("Choisissez l’imprimante avant d’activer l’envoi.");
            sharingCancellation?.Cancel(); await controller.DisposeAsync();
            boothSettings = boothSettings with { CanonCamera = canon.IsChecked == true, CanonUseOfficialSdk = canonSdk.IsChecked == true, CanonManualFocusConfirmed = manualFocus.IsChecked == true, PhysicalPrinting = printing.IsChecked == true, PrinterName = name };
            await boothSettings.SaveAsync(boothSettingsPath); controller = CreateController(); RenderHome();
            info.Text = "Réglages enregistrés. Lancez une séance pour vérifier la connexion.";
        }));
        content.Children.Add(Button("Choisir des photos locales", async () =>
        {
            var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            { Title = "Photos locales", AllowMultiple = true, FileTypeFilter = [FilePickerFileTypes.ImageAll] });
            var paths = picked.Select(p => p.TryGetLocalPath()).Where(p => p is not null).Cast<string>().ToArray();
            if (paths.Length == 0) return;
            foreach (var path in paths)
            {
                if (new FileInfo(path).Length > PhotoProcessor.MaxEncodedBytes) throw new InvalidDataException("Une photo est trop volumineuse.");
                using var decoded = PhotoProcessor.DecodeUpright(await File.ReadAllBytesAsync(path));
            }
            fixtureFiles = paths; info.Text = $"{paths.Length} photo(s) locale(s) sélectionnée(s). Elles seront utilisées dans l’ordre.";
        }));
        var connection = new TextBox { PasswordChar = '●', Name = "CloudAccessKey" };
        content.Children.Add(Text("Code de connexion de la borne · pas la clé OpenAI",20,FontWeight.SemiBold));
        content.Children.Add(connection);
        content.Children.Add(Button("Activer OpenAI et le partage", async () =>
        {
            var candidate = string.IsNullOrWhiteSpace(connection.Text) ? cloudKey : connection.Text.Trim();
            using var check = new BoothCloudClient(boothSettings.ApiBaseUrl,candidate,store);
            var imageReady = await check.CheckAsync();
            await CredentialStore.SaveAsync(credentialPath,candidate); cloudKey=candidate;
            sharingCancellation?.Cancel(); await controller.DisposeAsync();
            boothSettings=boothSettings with {CloudEnabled=true}; await boothSettings.SaveAsync(boothSettingsPath);
            controller=CreateController(); RenderHome(); connection.Text="";
            info.Text=imageReady ? "Connexion vérifiée. Une séance Métamorphose utilisera OpenAI en qualité medium et sera facturée." :
                "Partage connecté. Ajoutez la clé OpenAI dans Supabase avant d’utiliser Métamorphose.";
        }));
        content.Children.Add(Button("Désactiver la connexion en ligne",async()=>
        {
            sharingCancellation?.Cancel(); await controller.DisposeAsync();
            if(projection is not null)await projection.ConfigureAsync(false,boothSettings.ViewerIncludeTests);
            boothSettings=boothSettings with {CloudEnabled=false,ViewerEnabled=false};
            await boothSettings.SaveAsync(boothSettingsPath);controller=CreateController();RenderHome();info.Text="Connexion en ligne désactivée. Aucun appel OpenAI payant.";
        }));
        content.Children.Add(Text("Le mode en ligne envoie les photos vers Supabase, et vers OpenAI uniquement en mode Métamorphose. Les liens expirent après 7 jours.",15));
        var catalogInfo = Text(catalogStatus, 17); content.Children.Add(catalogInfo);
        var refresh = Button("Actualiser les univers", () => RefreshCatalogAsync(catalogInfo)); refresh.Name = "RefreshStyles"; content.Children.Add(refresh);
        AddViewerSettings(content,info);
        var reduce = new CheckBox { Content = "Réduire les animations", IsChecked = boothSettings.ReducedMotion };
        reduce.IsCheckedChanged += async (_, _) => { boothSettings = boothSettings with { ReducedMotion = reduce.IsChecked == true }; await boothSettings.SaveAsync(boothSettingsPath); };
        content.Children.Add(reduce);
        var protect = new CheckBox { Name = "ProtectSettings", Content = "Protéger les réglages avec un code", IsChecked = exitGate is not null };
        content.Children.Add(protect);
        pin.Name = "ExitPin";
        content.Children.Add(Text("Code des réglages · quatre caractères", 20, FontWeight.SemiBold)); content.Children.Add(pin);
        content.Children.Add(Text("Laissez le champ vide pour garder le code existant.", 15));
        async Task SaveProtection()
        {
            if (protect.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(pin.Text)) exitGate = KioskExitGate.Create(pin.Text);
                if (exitGate is null) throw new InvalidOperationException("Choisissez un code de quatre caractères.");
                await SaveKioskAsync();
            }
            else { exitGate = null; if (File.Exists(settingsPath)) File.Delete(settingsPath); }
            pin.Text = "";
        }
        var saveCode = Button("Enregistrer le code", async () => { await SaveProtection(); info.Text = exitGate is null ? "Les réglages sont accessibles sans code." : "Le code est enregistré."; });
        saveCode.Name = "SaveOperatorCode"; content.Children.Add(saveCode);
        var fullscreen = Button("Activer le plein écran", async () =>
        {
            await SaveProtection(); EnterFullscreen();
            boothSettings = boothSettings with { FullscreenEnabled = true };
            await boothSettings.SaveAsync(boothSettingsPath);
            info.Text = "Plein écran activé. Fermez les réglages pour reprendre la fête.";
        }, true); fullscreen.Name = "EnterFullscreen"; content.Children.Add(fullscreen);
        var leave = Button("Quitter le plein écran", LeaveFullscreenAsync);
        leave.Name = "LeaveFullscreen"; content.Children.Add(leave);
        content.Children.Add(Text("Sur l’accueil, touchez la roue crantée en bas à gauche pour ouvrir les réglages.", 16));
        content.Children.Add(Text("Le plein écran sera conservé au prochain lancement. Pour protéger aussi l’accès à Windows, configurez un compte kiosque dédié sur le PC de la borne.", 15));
        content.Children.Add(Button("Fermer les réglages", () => { settings.Close(); return Task.CompletedTask; }));
        settings.Content = new ScrollViewer { Content = content }; await settings.ShowDialog(this);
    }

    private Task SaveKioskAsync() => SessionStore.WriteAtomicAsync(settingsPath, JsonSerializer.SerializeToUtf8Bytes(exitGate!.Export()));

    private readonly HashSet<Guid> sharingSessions = [];
    private async Task ShareAsync(Image qr, TextBlock label, Button retry)
    {
        if (cloud is null || controller.Session is null || controller.Sheet is null) return;
        var source = controller; var session = source.Session; var client = cloud;
        if (!sharingSessions.Add(session.Id)) return;
        sharingCancellation?.Dispose(); sharingCancellation=new();
        var token=sharingCancellation.Token; retry.IsVisible=false;
        if (galleries.TryGetValue(session.Id, out var existing)) ShowQr(qr, existing);
        try
        {
            label.Text="Envoi de vos souvenirs…";
            var result=await PublishOnceAsync(source, client);
            if (token.IsCancellationRequested || screen!="result" || controller.Session?.Id!=session.Id) return;
            ShowQr(qr, result); retry.IsVisible=false; resultShareReady=true;
            label.Text=$"Scannez pour retrouver vos photos. Lien valable jusqu’au {result.ExpiresAt.LocalDateTime:dd/MM}.";
        }
        catch (OperationCanceledException) when(token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (publications.TryGetValue(session.Id, out var attempt) && attempt.IsFaulted) publications.Remove(session.Id);
            if (screen == "result" && controller.Session?.Id == session.Id && !token.IsCancellationRequested)
            {
                retry.IsVisible=true;
                label.Text=exception is HttpRequestException or OperationCanceledException
                    ? "Le partage attend la connexion. Vous pouvez imprimer vos photos et réessayer le partage ici."
                    : exception.Message;
            }
        }
        finally { sharingSessions.Remove(session.Id); }
    }

    private Window Dialog(string title, double width, double height) => new()
    { Title = title, Width = width, Height = height, CanResize = false, Topmost = kioskLocked, WindowStartupLocation = WindowStartupLocation.CenterOwner };

    private static Grid ChoiceRow(params (string Label, bool Selected, Action Action)[] choices)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        for (var i = 0; i < choices.Length; i++)
        {
            var choice = choices[i]; var button = Button(choice.Label, () => { choice.Action(); return Task.CompletedTask; });
            if (choice.Selected) button.Classes.Add("selected");
            button.Margin = new Thickness(i == 0 ? 0 : 6, 0, i == 0 ? 6 : 0, 0);
            button.HorizontalAlignment = HorizontalAlignment.Stretch; Grid.SetColumn(button, i); row.Children.Add(button);
        }
        return row;
    }

    private static TextBlock Text(string content, double size, FontWeight? weight = null) => new()
    { Text = content, FontSize = size, FontWeight = weight ?? FontWeight.Normal, TextWrapping = TextWrapping.Wrap,
        FontFamily = size >= 28 ? new FontFamily("avares://ColorsParty/Assets/BricolageGrotesque.ttf#Bricolage Grotesque") : new FontFamily("Segoe UI, Arial") };
    private static Button Button(string text, Func<Task> action, bool primary = false)
    {
        var button = new Button { Content = text };
        if (primary) button.Classes.Add("primary");
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await action(); }
            catch (Exception ex)
            {
                var window = TopLevel.GetTopLevel(button) as Window;
                if (window is not null)
                {
                    var error = new Window { Title = "Action interrompue", Width = 520, Height = 270, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                    error.Content = new StackPanel { Spacing = 20, Margin = new Thickness(28), Children =
                    { Text(ex.Message, 20), Button("Fermer", () => { error.Close(); return Task.CompletedTask; }) } };
                    await error.ShowDialog(window);
                }
            }
            finally { button.IsEnabled = true; }
        };
        return button;
    }
    private static Bitmap Bitmap(byte[] bytes) => new(new MemoryStream(bytes, writable: false));
    private void ReplaceImage(Image target, byte[] bytes)
    {
        var bitmap = Bitmap(bytes); var previous = currentBitmap; currentBitmap = bitmap; target.Source = bitmap; previous?.Dispose();
    }
    private static void OpenFolder(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}

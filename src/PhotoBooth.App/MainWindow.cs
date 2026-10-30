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

public sealed class MainWindow : Window
{
    private static readonly IBrush Ink = Brush.Parse("#241931"), Muted = Brush.Parse("#C7B6CD"), Lemon = Brush.Parse("#F5E764");
    private readonly ContentControl body = new()
    { Name = "SessionBody", HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly TextBlock footer = Text("Répétition · aucune caméra, aucune impression, aucun appel IA", 15);
    private readonly TextBlock status = Text("", 22);
    private readonly SessionStore store;
    private BoothController controller;
    private int photoCount = 4;
    private PhotoMode mode = PhotoMode.Original;
    private string[] fixtureFiles = [];
    private string screen = "home";
    private Image? liveImage;
    private TextBlock? countdown;
    private Border? countdownBadge;
    private Border? flashVeil;
    private readonly DispatcherTimer flashTimer = new() { Interval = TimeSpan.FromMilliseconds(170) };
    private bool showingCapture;
    private bool liveViewReady;
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
    private CancellationTokenSource? sharingCancellation;
    private readonly DispatcherTimer pulse = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private DateTimeOffset pulseStarted;
    private readonly Func<TimeSpan, CancellationToken, Task>? rehearsalDelay;

    public MainWindow(Func<TimeSpan, CancellationToken, Task>? rehearsalDelay = null)
    {
        this.rehearsalDelay = rehearsalDelay;
        Title = "Estell’Art — l’anniversaire d’Estelle";
        Width = 1280; Height = 800; MinWidth = 960; MinHeight = 700;
        if (Screens.Primary is { } display)
        {
            Width = Math.Max(MinWidth, Math.Min(Width, display.WorkingArea.Width / display.Scaling - 32));
            Height = Math.Max(MinHeight, Math.Min(Height, display.WorkingArea.Height / display.Scaling - 48));
        }
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = Environment.GetEnvironmentVariable("PHOTOBOOTH_DATA_DIR") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EstellArt");
        store = new SessionStore(Path.Combine(root, "sessions"));
        settingsPath = Path.Combine(root, "kiosk.json");
        boothSettingsPath = Path.Combine(root, "booth-settings.json");
        credentialPath = Path.Combine(root, "cloud-credential");
        try { boothSettings = BoothSettings.Load(boothSettingsPath); } catch (Exception) { /* Safe local rehearsal defaults. */ }
        try { cloudKey = CredentialStore.Load(credentialPath); } catch (Exception) { boothSettings = boothSettings with { CloudEnabled = false }; }
        if (cloudKey.Length == 0) boothSettings = boothSettings with { CloudEnabled = false };
        if (File.Exists(settingsPath))
        {
            try { exitGate = KioskExitGate.Restore(JsonSerializer.Deserialize<KioskExitGate.Credential>(File.ReadAllText(settingsPath))!); }
            catch (Exception) { footer.Text = "Code de sortie illisible : configurez un nouveau code avant le plein écran."; }
        }
        controller = CreateController();
        UpdateFooter();
        var header = new Grid { Name = "Header", ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 24) };
        header.Children.Add(Text("Estell’Art.", 32, FontWeight.Bold));
        var admin = Button("Réglages", ShowSettingsAsync); admin.Name = "Settings";
        Grid.SetColumn(admin, 1); header.Children.Add(admin);
        var layout = new Grid { Name = "AppLayout", RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(36, 24) };
        layout.Children.Add(header); Grid.SetRow(body, 1); layout.Children.Add(body);
        footer.Foreground = Muted; footer.TextWrapping = TextWrapping.Wrap; footer.Margin = new Thickness(0, 20, 0, 0);
        Grid.SetRow(footer, 2); layout.Children.Add(footer); Content = layout;
        // Windows can clamp the requested startup size after the first layout pass.
        // Keep the grid's measured minimum tied to the actual viewport, including later resizes.
        void ConstrainLayout()
        {
            layout.MaxWidth = Math.Max(0, ClientSize.Width - layout.Margin.Left - layout.Margin.Right);
            layout.MaxHeight = Math.Max(0, ClientSize.Height - layout.Margin.Top - layout.Margin.Bottom);
        }
        Opened += (_, _) => ConstrainLayout();
        Resized += (_, _) => ConstrainLayout();
        pulse.Tick += (_, _) =>
        {
            if (countdown is null) return;
            var age = (DateTimeOffset.UtcNow - pulseStarted).TotalMilliseconds;
            var scale = 1 + .12 * Math.Exp(-age / 110);
            countdown.RenderTransform = new ScaleTransform(scale, scale);
            if (age > 550) pulse.Stop();
        };
        flashTimer.Tick += (_, _) => { if (flashVeil is not null) flashVeil.Opacity = 0; flashTimer.Stop(); };
        Closing += (_, e) =>
        {
            if (kioskLocked) { e.Cancel = true; _ = ShowUnlockAsync(); }
        };
        Closed += async (_, _) => { pulse.Stop(); flashTimer.Stop(); sharingCancellation?.Cancel(); await controller.DisposeAsync(); currentBitmap?.Dispose(); };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F11 || (e.Key == Key.Escape && kioskLocked))
            { e.Handled = true; _ = ShowUnlockAsync(); }
        };
        RenderHome();
    }

    private BoothController CreateController()
    {
        cloud = boothSettings.CloudEnabled ? new BoothCloudClient(boothSettings.ApiBaseUrl, cloudKey, store, boothSettings.GalleryBaseUrl) : null;
        var result = new BoothController(store, () => boothSettings.CanonCamera ?
            (boothSettings.CanonUseOfficialSdk ? new CanonCamera(store) : new CanonWpdCamera(store)) : new FixtureCamera(store, fixtureFiles),
            (IPhotoTransform?)cloud ?? new RehearsalTransform(), rehearsalDelay);
        result.Changed += () => Dispatcher.UIThread.Post(UpdateSession);
        result.Frame += bytes => Dispatcher.UIThread.Post(() =>
        {
            if (screen != "capture" || liveImage is null || controller.Session?.WantsLiveView != true) return;
            ReplaceImage(liveImage, bytes);
            liveViewReady=true;
            if(capture is not null&&controller.Session?.Stage==SessionStage.Positioning&&!controller.IsTaking)capture.IsEnabled=true;
        });
        return result;
    }

    private void RenderHome()
    {
        sharingCancellation?.Cancel(); sharingCancellation?.Dispose(); sharingCancellation = null;
        screen = "home"; liveImage = null; countdown = null; pulse.Stop();
        var grid = new Grid { Name = "HomeLayout", ColumnDefinitions = new ColumnDefinitions("6*,5*") };
        var choices = new StackPanel { Spacing = 19, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 38, 0) };
        choices.Children.Add(Text("Des souvenirs\nplein la fête.", 57, FontWeight.SemiBold));
        var subtitle = Text("L’anniversaire d’Estelle, avec votre plus beau sourire.", 22); subtitle.Foreground = Muted;
        choices.Children.Add(subtitle);
        choices.Children.Add(Text("Combien de poses ?", 20, FontWeight.SemiBold));
        choices.Children.Add(ChoiceRow(("Une photo", photoCount == 1, () => { photoCount = 1; RenderHome(); }),
            ("Quatre photos", photoCount == 4, () => { photoCount = 4; RenderHome(); })));
        choices.Children.Add(Text("À votre façon", 20, FontWeight.SemiBold));
        choices.Children.Add(ChoiceRow(("Au naturel", mode == PhotoMode.Original, () => { mode = PhotoMode.Original; RenderHome(); }),
            ("Surprise IA", mode == PhotoMode.Surprise, () => { mode = PhotoMode.Surprise; RenderHome(); })));
        var detail = Text(mode == PhotoMode.Original ? "Vos photos, telles quelles." : "Un style tiré au sort pour toute votre série.", 17);
        detail.Foreground = Muted; choices.Children.Add(detail);
        var start = Button("Démarrer", async () =>
        {
            startBusy(true);
            await controller.BeginAsync(new SessionOptions(photoCount, mode));
            startBusy(false);
        }, primary: true);
        void startBusy(bool busy) { foreach (var child in choices.Children.OfType<Button>()) child.IsEnabled = !busy; }
        start.Name = "StartSession"; start.HorizontalAlignment = HorizontalAlignment.Stretch;
        choices.Children.Add(start); grid.Children.Add(choices);
        var preview = new StackPanel { Spacing = 18, VerticalAlignment = VerticalAlignment.Center };
        var image = new Image { Source = Bitmap(PhotoProcessor.Compose(Enumerable.Range(0, photoCount).Select(FixtureCamera.CreateFixture).ToArray())), Stretch = Stretch.Uniform, MaxHeight = 380 };
        preview.Children.Add(new Border { Name = "HomeSheet", Background = Brushes.White, Padding = new Thickness(12), Child = image, CornerRadius = new CornerRadius(4) });
        preview.Children.Add(Text("Une feuille. Tous les sourires.", 27, FontWeight.SemiBold));
        var note = Text("1 ou 4 poses · à télécharger ou à imprimer", 18); note.Foreground = Muted; preview.Children.Add(note);
        Grid.SetColumn(preview, 1); grid.Children.Add(preview); body.Content = grid;
    }

    private void RenderCapture()
    {
        screen = "capture";
        liveViewReady=false;
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        pose = Text("Placez-vous dans le cadre.", 30, FontWeight.SemiBold); layout.Children.Add(pose);
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
        var back = Button("Annuler", async () => { await controller.CancelAsync(); if (!controller.IsTaking) RenderHome(); }); actions.Children.Add(back);
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
        if (session.Stage == SessionStage.Cancelled) { if (!controller.IsTaking) RenderHome(); return; }
        if (session.Stage == SessionStage.Failed) { RenderError(controller.Message); return; }
        if (screen != "capture") RenderCapture();
        status.Text = controller.Message;
        var isCapture = session.Stage == SessionStage.Capturing;
        if (isCapture && !showingCapture && flashVeil is not null)
        { flashVeil.Opacity = .22; flashTimer.Start(); }
        showingCapture = isCapture;
        if (pose is not null) pose.Text = session.Stage == SessionStage.Processing ? "On prépare vos souvenirs…" :
            $"Pose {Math.Min(session.Captures.Count + 1, session.Options.PhotoCount)} sur {session.Options.PhotoCount}";
        if (capture is not null) { capture.IsVisible = session.Stage == SessionStage.Positioning && !controller.IsTaking; capture.IsEnabled=liveViewReady; }
        if (countdown is not null)
        {
            var next = session.Stage == SessionStage.Countdown ? session.SecondsRemaining.ToString() : session.Stage == SessionStage.Capturing ? "Clic !" : "";
            if (countdownBadge is not null) countdownBadge.IsVisible = next.Length > 0;
            if (next != countdown.Text) { countdown.Text = next; pulseStarted = DateTimeOffset.UtcNow; pulse.Start(); }
        }
    }

    private void RenderResult()
    {
        screen = "result"; pulse.Stop();
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("7*,4*") };
        var left = new StackPanel { Spacing = 22, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 30, 0) };
        left.Children.Add(Text("Ça, c’est un souvenir.", 38, FontWeight.SemiBold));
        var image = new Image { Source = new Bitmap(store.Resolve(controller.Sheet!.Key)), Stretch = Stretch.Uniform, MaxHeight = 430 };
        left.Children.Add(image); layout.Children.Add(left);
        var actions = new StackPanel { Spacing = 20, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(Text(controller.Session!.EffectiveMode == PhotoMode.Surprise ?
            (cloud is null ? "Essai de transformation" : "Votre surprise est prête.") : "Tout simplement vous.", 30, FontWeight.SemiBold));
        var note = Text(controller.Session.UsedOriginalFallback ? "Vous avez choisi de garder les photos originales." :
            controller.Session.EffectiveMode == PhotoMode.Surprise && cloud is null ?
            "En répétition, un filtre local simule le traitement. Aucune image n’a été envoyée à l’IA." : "Votre planche est enregistrée sur cet ordinateur.", 19);
        note.Foreground = Muted; actions.Children.Add(note);
        var printState = Text("", 18);
        var print = Button(boothSettings.PhysicalPrinting ? "Imprimer" : "Tester l’impression", async () =>
        {
            if (boothSettings.PhysicalPrinting)
            {
                var submission = await controller.PrintAsync(new WindowsPhotoPrinter(store, controller.Sheet!, boothSettings.PrinterName));
                printState.Text = $"Envoyé à l’imprimante (travail {submission.SpoolJobId}). Récupérez votre feuille après sa sortie.";
                return;
            }
            var bytes = await controller.GetPrintBytesAsync();
            var target = Path.Combine(store.Root, controller.Session.Id.ToString("N"), "print-preview.jpg");
            await SessionStore.WriteAtomicAsync(target, bytes);
            printState.Text = "Planche vérifiée et enregistrée. Aucune feuille envoyée : mode répétition.";
        }, primary: true); print.Name = "PrintPreview"; actions.Children.Add(print); actions.Children.Add(printState);
        actions.Children.Add(Button("Voir les fichiers", () => { OpenFolder(Path.GetDirectoryName(store.Resolve(controller.Sheet.Key))!); return Task.CompletedTask; }));
        var shareStatus = Text(cloud is null ? "Partage en ligne désactivé dans les réglages." : "Préparation de votre lien…", 17);
        var qr = new Image { Width = 230, Height = 230, IsVisible = false, HorizontalAlignment = HorizontalAlignment.Left };
        var retry = Button("Réessayer le partage", () => ShareAsync(qr, shareStatus)); retry.IsVisible = cloud is not null;
        actions.Children.Add(qr); actions.Children.Add(shareStatus); actions.Children.Add(retry);
        actions.Children.Add(Button("Une nouvelle séance", () => { RenderHome(); return Task.CompletedTask; }));
        var scrollingActions = new ScrollViewer { Content = actions };
        Grid.SetColumn(scrollingActions, 1); layout.Children.Add(scrollingActions); body.Content = layout;
        if (cloud is not null) _ = ShareAsync(qr, shareStatus);
    }

    private void RenderError(string message)
    {
        screen = "error";
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
        if (kioskLocked) { await ShowUnlockAsync(); return; }
        if (controller.IsTaking) return;
        var settings = Dialog("Réglages de la borne", 690, 680);
        var pin = new TextBox { PasswordChar = '●', MaxLength = 12 };
        var info = Text(boothSettings.CloudEnabled ? "Connexion en ligne active. Le mode Surprise lance des appels OpenAI payants." :
            "Connexion en ligne désactivée. Aucun appel OpenAI ne sera facturé.", 19);
        var content = new StackPanel { Spacing = 18, Margin = new Thickness(30) };
        content.Children.Add(Text("Prêts pour la fête ?", 34, FontWeight.SemiBold));
        content.Children.Add(info);
        var canon = new CheckBox { Content = "Utiliser le Canon USB (Windows)", IsChecked = boothSettings.CanonCamera };
        var canonSdk = new CheckBox { Content = "Connexion avancée : SDK Canon officiel installé", IsChecked = boothSettings.CanonUseOfficialSdk };
        var printing = new CheckBox { Content = "Envoyer réellement à l’imprimante", IsChecked = boothSettings.PhysicalPrinting };
        var printers = new ComboBox { ItemsSource = WindowsPhotoPrinter.ListPrinters(), SelectedItem = boothSettings.PrinterName,
            PlaceholderText = "Choisir la DNP DS620", HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Children.Add(canon); content.Children.Add(canonSdk);
        content.Children.Add(Text("Par défaut, connexion par le pilote Windows existant, sans SDK à installer. Compatibilité à vérifier sur le boîtier.",15));
        content.Children.Add(printing); content.Children.Add(printers);
        content.Children.Add(Button("Enregistrer le matériel", async () =>
        {
            if ((canon.IsChecked == true || printing.IsChecked == true) && !OperatingSystem.IsWindows())
                throw new InvalidOperationException("Ces deux connexions demandent le PC Windows. La répétition fonctionne sur ce Mac.");
            var name = printers.SelectedItem as string ?? "";
            if (printing.IsChecked == true && name.Length == 0) throw new InvalidOperationException("Choisissez l’imprimante avant d’activer l’envoi réel.");
            sharingCancellation?.Cancel(); await controller.DisposeAsync();
            boothSettings = boothSettings with { CanonCamera = canon.IsChecked == true, CanonUseOfficialSdk = canonSdk.IsChecked == true, PhysicalPrinting = printing.IsChecked == true, PrinterName = name };
            await boothSettings.SaveAsync(boothSettingsPath); controller = CreateController(); UpdateFooter(); RenderHome();
            info.Text = "Réglages enregistrés. Lancez une séance pour vérifier la connexion.";
        }));
        content.Children.Add(Button("Choisir des photos de répétition", async () =>
        {
            var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            { Title = "Photos de répétition", AllowMultiple = true, FileTypeFilter = [FilePickerFileTypes.ImageAll] });
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
            controller=CreateController(); UpdateFooter(); RenderHome(); connection.Text="";
            info.Text=imageReady ? "Connexion vérifiée. Une séance Surprise utilisera OpenAI en qualité xhigh et sera facturée." :
                "Partage connecté. Ajoutez la clé OpenAI dans Supabase avant d’utiliser Surprise IA.";
        }));
        content.Children.Add(Button("Désactiver la connexion en ligne",async()=>
        {
            sharingCancellation?.Cancel(); await controller.DisposeAsync(); boothSettings=boothSettings with {CloudEnabled=false};
            await boothSettings.SaveAsync(boothSettingsPath);controller=CreateController();UpdateFooter();RenderHome();info.Text="Connexion en ligne désactivée. Aucun appel OpenAI payant.";
        }));
        content.Children.Add(Text("Le mode en ligne envoie les photos vers Supabase, et vers OpenAI uniquement en mode Surprise. Les liens expirent après 7 jours.",15));
        pin.Name = "ExitPin";
        content.Children.Add(Text("Code de sortie du plein écran · 6 à 12 chiffres",20,FontWeight.SemiBold));
        content.Children.Add(pin);
        content.Children.Add(Button("Activer le plein écran", async () =>
        {
            if (!string.IsNullOrEmpty(pin.Text)) exitGate = KioskExitGate.Create(pin.Text);
            if (exitGate is null) { info.Text = "Choisissez d’abord un code de 6 à 12 chiffres."; return; }
            await SaveKioskAsync(); kioskLocked = true; WindowState = WindowState.FullScreen; settings.Close();
        }, true));
        content.Children.Add(Text("Le code protège les commandes de l’application. Les commandes système Windows restent accessibles.", 15));
        content.Children.Add(Button("Fermer les réglages", () => { settings.Close(); return Task.CompletedTask; }));
        settings.Content = new ScrollViewer { Content = content }; await settings.ShowDialog(this);
    }

    private bool unlockOpen;
    private async Task ShowUnlockAsync()
    {
        if (!kioskLocked || exitGate is null || unlockOpen) return;
        unlockOpen = true;
        try
        {
            var dialog = Dialog("Code de sortie", 480, 430);
            var pin = new TextBox { PasswordChar = '●', MaxLength = 12 };
            var error = Text("", 17);
            dialog.Content = new StackPanel { Spacing = 20, Margin = new Thickness(30), Children =
            {
                Text("Retour aux réglages", 28, FontWeight.SemiBold), Text("Code de sortie",18,FontWeight.SemiBold), pin, error,
                Button("Déverrouiller", async () =>
                {
                    var authorized = exitGate.TryAuthorizeExit(pin.Text ?? ""); await SaveKioskAsync();
                    if (!authorized) { pin.Text = ""; error.Text = "Code incorrect ou verrouillage de 30 secondes."; return; }
                    kioskLocked = false; WindowState = WindowState.Normal; dialog.Close();
                }, true),
                Button("Retour à la fête", () => { dialog.Close(); return Task.CompletedTask; })
            } };
            await dialog.ShowDialog(this);
        }
        finally { unlockOpen = false; }
    }
    private Task SaveKioskAsync() => SessionStore.WriteAtomicAsync(settingsPath, JsonSerializer.SerializeToUtf8Bytes(exitGate!.Export()));
    private void UpdateFooter() => footer.Text = $"{(boothSettings.CanonCamera ? "Canon USB" : "Caméra simulée")} · {(boothSettings.PhysicalPrinting ? "Impression réelle" : "Impression simulée")} · {(boothSettings.CloudEnabled ? "Partage en ligne · Surprise IA payante" : "IA simulée, aucun appel payant")}";

    private bool sharing;
    private async Task ShareAsync(Image qr, TextBlock label)
    {
        if (sharing || cloud is null || controller.Session is null || controller.Sheet is null) return;
        sharing=true; sharingCancellation?.Dispose(); sharingCancellation=new();
        var token=sharingCancellation.Token;var session=controller.Session;var client=cloud;
        try
        {
            label.Text="Envoi de vos souvenirs…";
            var result=await client.PublishAsync(session,controller.FinalAssets,controller.Sheet,token);
            if (token.IsCancellationRequested || screen!="result" || controller.Session?.Id!=session.Id) return;
            using var generator=new QRCodeGenerator();using var data=generator.CreateQrCode(result.Url,QRCodeGenerator.ECCLevel.M);
            using var code=new PngByteQRCode(data);qr.Source=Bitmap(code.GetGraphic(3));
            qr.Width=qr.Height=data.ModuleMatrix.Count*3;qr.IsVisible=true;
            label.Text=$"Scannez pour télécharger vos photos{(boothSettings.GalleryBaseUrl.Length==0?" (ZIP)":"")}. Lien valable jusqu’au {result.ExpiresAt.LocalDateTime:dd/MM}.";
        }
        catch (OperationCanceledException) when(token.IsCancellationRequested) { }
        catch (Exception exception) { label.Text=exception.Message; }
        finally { sharing=false; }
    }

    private Window Dialog(string title, double width, double height) => new()
    { Title = title, Width = width, Height = height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };

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
        FontFamily = size >= 28 ? new FontFamily("avares://EstellArt/Assets/BricolageGrotesque.ttf#Bricolage Grotesque") : new FontFamily("Segoe UI, Arial") };
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

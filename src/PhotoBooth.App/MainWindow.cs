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

namespace PhotoBooth.App;

public sealed class MainWindow : Window
{
    private static readonly IBrush Ink = Brush.Parse("#241931"), Muted = Brush.Parse("#C7B6CD"), Lemon = Brush.Parse("#F5E764");
    private readonly ContentControl body = new();
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
    private TextBlock? pose;
    private Button? capture;
    private Bitmap? currentBitmap;
    private KioskExitGate? exitGate;
    private bool kioskLocked;
    private readonly string settingsPath;
    private readonly string boothSettingsPath;
    private BoothSettings boothSettings = new();
    private readonly DispatcherTimer pulse = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private DateTimeOffset pulseStarted;
    private readonly Func<TimeSpan, CancellationToken, Task>? rehearsalDelay;

    public MainWindow(Func<TimeSpan, CancellationToken, Task>? rehearsalDelay = null)
    {
        this.rehearsalDelay = rehearsalDelay;
        Title = "Estell’Art — l’anniversaire d’Estelle";
        Width = 1280; Height = 800; MinWidth = 960; MinHeight = 700;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = Environment.GetEnvironmentVariable("PHOTOBOOTH_DATA_DIR") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EstellArt");
        store = new SessionStore(Path.Combine(root, "sessions"));
        settingsPath = Path.Combine(root, "kiosk.json");
        boothSettingsPath = Path.Combine(root, "booth-settings.json");
        try { boothSettings = BoothSettings.Load(boothSettingsPath); } catch (Exception) { /* Safe local rehearsal defaults. */ }
        if (File.Exists(settingsPath))
        {
            try { exitGate = KioskExitGate.Restore(JsonSerializer.Deserialize<KioskExitGate.Credential>(File.ReadAllText(settingsPath))!); }
            catch (Exception) { footer.Text = "Code de sortie illisible : configurez un nouveau code avant le plein écran."; }
        }
        controller = CreateController();
        UpdateFooter();
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 24) };
        header.Children.Add(Text("Estell’Art.", 32, FontWeight.Bold));
        var admin = Button("Réglages", ShowSettingsAsync); admin.Name = "Settings";
        Grid.SetColumn(admin, 1); header.Children.Add(admin);
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(36, 24) };
        layout.Children.Add(header); Grid.SetRow(body, 1); layout.Children.Add(body);
        footer.Foreground = Muted; footer.TextWrapping = TextWrapping.Wrap; footer.Margin = new Thickness(0, 20, 0, 0);
        Grid.SetRow(footer, 2); layout.Children.Add(footer); Content = layout;
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
        Closed += async (_, _) => { pulse.Stop(); flashTimer.Stop(); await controller.DisposeAsync(); currentBitmap?.Dispose(); };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F11 || (e.Key == Key.Escape && kioskLocked))
            { e.Handled = true; _ = ShowUnlockAsync(); }
        };
        RenderHome();
    }

    private BoothController CreateController()
    {
        var result = new BoothController(store, () => boothSettings.CanonCamera ? new CanonCamera(store) : new FixtureCamera(store, fixtureFiles), new RehearsalTransform(), rehearsalDelay);
        result.Changed += () => Dispatcher.UIThread.Post(UpdateSession);
        result.Frame += bytes => Dispatcher.UIThread.Post(() =>
        {
            if (screen != "capture" || liveImage is null || controller.Session?.WantsLiveView != true) return;
            ReplaceImage(liveImage, bytes);
        });
        return result;
    }

    private void RenderHome()
    {
        screen = "home"; liveImage = null; countdown = null; pulse.Stop();
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("6*,5*") };
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
        preview.Children.Add(new Border { Background = Brushes.White, Padding = new Thickness(12), Child = image, CornerRadius = new CornerRadius(4) });
        preview.Children.Add(Text("Une feuille. Tous les sourires.", 27, FontWeight.SemiBold));
        var note = Text("1 ou 4 poses · à télécharger ou à imprimer", 18); note.Foreground = Muted; preview.Children.Add(note);
        Grid.SetColumn(preview, 1); grid.Children.Add(preview); body.Content = grid;
    }

    private void RenderCapture()
    {
        screen = "capture";
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
        if (capture is not null) { capture.IsVisible = session.Stage == SessionStage.Positioning && !controller.IsTaking; }
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
        actions.Children.Add(Text(controller.Session!.Options.Mode == PhotoMode.Surprise ? "Essai de transformation" : "Tout simplement vous.", 30, FontWeight.SemiBold));
        var note = Text(controller.Session.Options.Mode == PhotoMode.Surprise ?
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
        actions.Children.Add(Text("Le QR apparaîtra ici après activation du partage en ligne.", 17));
        actions.Children.Add(Button("Une nouvelle séance", () => { RenderHome(); return Task.CompletedTask; }));
        Grid.SetColumn(actions, 1); layout.Children.Add(actions); body.Content = layout;
    }

    private void RenderError(string message)
    {
        screen = "error";
        body.Content = new StackPanel { Spacing = 24, MaxWidth = 720, VerticalAlignment = VerticalAlignment.Center,
            Children = { Text("Une petite pause.", 48, FontWeight.SemiBold), Text(message, 23),
                Text("Les photos déjà enregistrées restent sur cet ordinateur.", 19),
                Button("Revenir à l’accueil", () => { RenderHome(); return Task.CompletedTask; }, true) } };
    }

    private async Task ShowSettingsAsync()
    {
        if (kioskLocked) { await ShowUnlockAsync(); return; }
        if (controller.IsTaking) return;
        var settings = Dialog("Réglages de la borne", 690, 680);
        var pin = new TextBox { PlaceholderText = "Nouveau code de sortie (6 à 12 chiffres)", PasswordChar = '●', MaxLength = 12 };
        var info = Text("Mode répétition actif. Les images restent locales et aucune génération n’est facturée.", 19);
        var content = new StackPanel { Spacing = 18, Margin = new Thickness(30) };
        content.Children.Add(Text("Prêts pour la fête ?", 34, FontWeight.SemiBold));
        content.Children.Add(info);
        var canon = new CheckBox { Content = "Utiliser le Canon USB (Windows + SDK requis)", IsChecked = boothSettings.CanonCamera };
        var printing = new CheckBox { Content = "Envoyer réellement à l’imprimante", IsChecked = boothSettings.PhysicalPrinting };
        var printers = new ComboBox { ItemsSource = WindowsPhotoPrinter.ListPrinters(), SelectedItem = boothSettings.PrinterName,
            PlaceholderText = "Choisir la DNP DS620", HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Children.Add(canon); content.Children.Add(printing); content.Children.Add(printers);
        content.Children.Add(Button("Enregistrer le matériel", async () =>
        {
            if ((canon.IsChecked == true || printing.IsChecked == true) && !OperatingSystem.IsWindows())
                throw new InvalidOperationException("Ces deux connexions demandent le PC Windows. La répétition fonctionne sur ce Mac.");
            var name = printers.SelectedItem as string ?? "";
            if (printing.IsChecked == true && name.Length == 0) throw new InvalidOperationException("Choisissez l’imprimante avant d’activer l’envoi réel.");
            await controller.DisposeAsync();
            boothSettings = boothSettings with { CanonCamera = canon.IsChecked == true, PhysicalPrinting = printing.IsChecked == true, PrinterName = name };
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
            var dialog = Dialog("Code de sortie", 480, 360);
            var pin = new TextBox { PasswordChar = '●', MaxLength = 12, PlaceholderText = "Votre code" };
            var error = Text("", 17);
            dialog.Content = new StackPanel { Spacing = 20, Margin = new Thickness(30), Children =
            {
                Text("Retour aux réglages", 28, FontWeight.SemiBold), pin, error,
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
    private void UpdateFooter() => footer.Text = $"{(boothSettings.CanonCamera ? "Canon USB" : "Caméra simulée")} · {(boothSettings.PhysicalPrinting ? "Impression réelle" : "Impression simulée")} · IA simulée, aucun appel payant";

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

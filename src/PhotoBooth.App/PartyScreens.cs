using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PhotoBooth.Core;

namespace PhotoBooth.App;

public sealed partial class MainWindow
{
    private StyleCatalog catalog = StyleCatalog.BuiltIn;
    private StyleCatalog? pendingCatalog;
    private string styleChoice = StyleCatalog.Chef;
    private PhotoStyle? sessionStyle;
    private readonly Func<CancellationToken, Task<StyleCatalog>> catalogLoader;
    private readonly CancellationTokenSource lifetime = new();
    private bool refreshingCatalog;
    private string catalogStatus = "Les six univers de départ sont disponibles.";
    private readonly Dictionary<string, Bitmap> artwork = [];
    private Bitmap? resultBitmap;
    private readonly DispatcherTimer cooking = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private Image? cookingImage;
    private TextBlock? cookingPhrase, processingProgress;
    private readonly TranslateTransform cookingMove = new();
    private readonly Stopwatch cookingClock = new();
    private int phraseIndex = -1;
    public bool ProcessingAnimationRunning => cooking.IsEnabled;
    public int CatalogCount => catalog.Styles.Length;

    private Image Art(string name, double width, double height)
    {
        string[] known = ["wordmark", "colors_party", "halloween", "space", "fantasy", "elves", "disco", "chef"];
        if (!known.Contains(name))
        {
            string[] defaults = ["colors_party", "disco", "chef"];
            name = defaults[(int)((uint)name.Aggregate(0, (hash, c) => unchecked(hash * 31 + c)) % 3)];
        }
        if (!artwork.TryGetValue(name, out var bitmap))
        {
            using var stream = AssetLoader.Open(new Uri($"avares://ColorsParty/Assets/Party/{name}.png"));
            bitmap = Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, name == "wordmark" ? 800 : 288);
            artwork[name] = bitmap;
        }
        return new Image { Source = bitmap, Width = width, Height = height, Stretch = Stretch.Uniform };
    }

    private void RenderHome()
    {
        resultReturn.Stop();
        if (pendingCatalog is not null) { ApplyCatalog(pendingCatalog); pendingCatalog = null; }
        StopProcessing(); sharingCancellation?.Cancel(); sharingCancellation?.Dispose(); sharingCancellation = null;
        screen = "home"; liveImage = null; countdown = null; pulse.Stop();
        resultBitmap?.Dispose(); resultBitmap = null; currentBitmap?.Dispose(); currentBitmap = null;
        var grid = new Grid { Name = "HomeLayout", ColumnDefinitions = new ColumnDefinitions("6*,5*") };
        var brand = new StackPanel { Spacing = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 38, 0) };
        var mark = Art("wordmark", compactHome ? 370 : 470, compactHome ? 278 : 350);
        mark.MaxWidth = compactHome ? 370 : 470; mark.HorizontalAlignment = HorizontalAlignment.Center;
        AutomationProperties.SetName(mark, "Colors Party");
        brand.Children.Add(new Border { Name = "HomeSheet", Child = mark });
        var by = Text("by Estelle", 30, FontWeight.SemiBold); by.HorizontalAlignment = HorizontalAlignment.Center; brand.Children.Add(by);
        var invitation = Text("La fête vous va si bien.", compactHome ? 26 : 30); invitation.HorizontalAlignment = HorizontalAlignment.Center;
        brand.Children.Add(invitation); grid.Children.Add(brand);
        var choices = new StackPanel { Spacing = compactHome ? 14 : 20, VerticalAlignment = VerticalAlignment.Center };
        choices.Children.Add(Text("À vous de jouer.", compactHome ? 38 : 44, FontWeight.SemiBold));
        choices.Children.Add(Text("Combien de poses ?", 20, FontWeight.SemiBold));
        choices.Children.Add(ChoiceRow(("Une photo", photoCount == 1, () => { photoCount = 1; RenderHome(); }),
            ("Quatre photos", photoCount == 4, () => { photoCount = 4; RenderHome(); })));
        choices.Children.Add(Text("À votre façon", 20, FontWeight.SemiBold));
        choices.Children.Add(ChoiceRow(("Au naturel", mode == PhotoMode.Original, () => { mode = PhotoMode.Original; RenderHome(); }),
            ("Métamorphose", mode == PhotoMode.Surprise, () => { mode = PhotoMode.Surprise; RenderStyles(); })));
        var detail = Text(mode == PhotoMode.Original ? "Vos photos, telles quelles." : catalog.Find(styleChoice).Label, 18);
        detail.Foreground = Muted; choices.Children.Add(detail);
        var start = Button("Démarrer", BeginChosenSessionAsync, primary: true); start.Name = "StartSession";
        start.IsEnabled = mode == PhotoMode.Original || catalog.Styles.Length > 0;
        start.HorizontalAlignment = HorizontalAlignment.Stretch; choices.Children.Add(start);
        var note = Text("À télécharger, à imprimer, à garder.", 17); note.Foreground = Muted; choices.Children.Add(note);
        Grid.SetColumn(choices, 1); grid.Children.Add(choices); body.Content = grid;
    }

    private async Task BeginChosenSessionAsync()
    {
        sessionStyle = catalog.Find(styleChoice);
        await controller.BeginAsync(new SessionOptions(photoCount, mode, styleChoice: styleChoice));
    }

    private void RenderStyles()
    {
        StopProcessing(); screen = "styles";
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        var title = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 18) };
        title.Children.Add(Text("Et si on changeait d’univers ?", compactHome ? 32 : 40, FontWeight.SemiBold));
        var help = Text("Choisissez le décor de votre prochaine aventure.", 19); help.Foreground = Muted; title.Children.Add(help);
        layout.Children.Add(title);
        var tiles = new UniformGrid { Columns = 3, Rows = Math.Max(1, (catalog.Styles.Length + 2) / 3) };
        foreach (var style in catalog.Styles)
        {
            var button = Button(style.Label, () => { styleChoice = style.Id; RenderStyles(); return Task.CompletedTask; });
            button.Name = "Style_" + style.Id; button.Classes.Add("style-choice");
            button.Background = ReadableTileBackground(style.Color); button.Margin = new Thickness(0, 0, 12, 12);
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.VerticalAlignment = VerticalAlignment.Stretch;
            button.MinHeight = compactHome ? 148 : 170; button.Padding = new Thickness(14, 10);
            var content = new StackPanel { Spacing = 2 };
            content.Children.Add(Art(style.Asset.Length > 0 ? style.Asset : style.Id, compactHome ? 66 : 78, compactHome ? 66 : 78));
            var label = Text(style.Label, 22, FontWeight.SemiBold); label.TextAlignment = TextAlignment.Center; content.Children.Add(label);
            var description = Text(style.Description, 16); description.TextAlignment = TextAlignment.Center; content.Children.Add(description);
            button.Content = content; AutomationProperties.SetName(button, style.Label);
            if (styleChoice == style.Id) { button.BorderBrush = Ink; button.BorderThickness = new Thickness(3); }
            tiles.Children.Add(button);
        }
        var scroll = new ScrollViewer { Content = tiles, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 1); layout.Children.Add(scroll);
        var bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 12, 0, 0) };
        var back = Button("Retour", () => { RenderHome(); return Task.CompletedTask; }); back.Name = "BackFromStyles"; bottom.Children.Add(back);
        var chef = Button("La Surprise du Chef", () => { styleChoice = StyleCatalog.Chef; RenderStyles(); return Task.CompletedTask; });
        chef.Name = "Style_chef"; chef.Background = Brush.Parse("#F1E4BC"); chef.Margin = new Thickness(14, 0);
        chef.HorizontalAlignment = HorizontalAlignment.Stretch;
        var chefContent = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        chefContent.Children.Add(Art("chef", 52, 52));
        var chefWords = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        chefWords.Children.Add(Text("La Surprise du Chef", 21, FontWeight.SemiBold)); chefWords.Children.Add(Text("Un univers choisi pour vous.", 15)); chefContent.Children.Add(chefWords);
        chef.Content = chefContent; AutomationProperties.SetName(chef, "La Surprise du Chef");
        if (styleChoice == StyleCatalog.Chef) { chef.BorderBrush = Ink; chef.BorderThickness = new Thickness(3); }
        chef.IsEnabled = catalog.Styles.Length > 0; Grid.SetColumn(chef, 1); bottom.Children.Add(chef);
        var start = Button("C’est parti !", BeginChosenSessionAsync, true); start.Name = "StartStyledSession";
        start.IsEnabled = catalog.Styles.Length > 0; Grid.SetColumn(start, 2); bottom.Children.Add(start);
        Grid.SetRow(bottom, 2); layout.Children.Add(bottom); body.Content = layout;
    }

    private static IBrush ReadableTileBackground(string value)
    {
        static double Luminance(Color color)
        {
            static double Linear(byte channel) { var s = channel / 255d; return s <= .04045 ? s / 12.92 : Math.Pow((s + .055) / 1.055, 2.4); }
            return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        }
        var background = Color.Parse(value);
        var paper = Luminance(background); var ink = Luminance(((ISolidColorBrush)Ink).Color);
        var contrast = (Math.Max(paper, ink) + .05) / (Math.Min(paper, ink) + .05);
        // The catalog may introduce any valid hex colour. Keep guest copy readable
        // with the approved sage fallback instead of trusting remote art direction.
        return new SolidColorBrush(contrast >= 4.5 ? background : Color.Parse("#E0E7D8"));
    }

    private async Task RefreshCatalogAsync(TextBlock? feedback = null)
    {
        if (refreshingCatalog || controller.IsBusy) return;
        refreshingCatalog = true;
        if (feedback is not null) feedback.Text = "Récupération des univers…";
        try
        {
            var incoming = await catalogLoader(lifetime.Token);
            if (lifetime.IsCancellationRequested) return;
            catalogStatus = $"{incoming.Styles.Length} univers disponibles. Liste actualisée.";
            // A background request may finish after someone has opened the picker.
            // Freeze that guest's choices until the next home screen. An explicit
            // operator refresh may apply immediately while the settings own focus.
            if (controller.IsBusy || (feedback is null && screen != "home")) pendingCatalog = incoming;
            else
            {
                ApplyCatalog(incoming); pendingCatalog = null;
                if (screen == "styles") RenderStyles();
                else if (screen == "home") RenderHome();
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception) { catalogStatus = "Connexion impossible. Les univers déjà chargés restent disponibles."; }
        finally { refreshingCatalog = false; if (feedback is not null) feedback.Text = catalogStatus; }
    }

    private void ApplyCatalog(StyleCatalog incoming)
    {
        catalog = incoming;
        if (!catalog.Styles.Any(s => s.Id == styleChoice)) styleChoice = StyleCatalog.Chef;
    }

    private void RenderProcessing()
    {
        StopProcessing();
        screen = "processing"; pulse.Stop(); currentBitmap?.Dispose(); currentBitmap = null; liveImage = null;
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions(cloud is null ? "*" : "3*,2*"), RowDefinitions = new RowDefinitions("*,Auto") };
        var panel = new StackPanel { Spacing = 16, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 620, Margin = new Thickness(0, 0, cloud is null ? 0 : 36, 0) };
        cookingImage = Art(sessionStyle?.Asset ?? "colors_party", 150, 150); cookingImage.RenderTransform = cookingMove;
        panel.Children.Add(cookingImage);
        var heading = Text(mode == PhotoMode.Original ? "On prépare vos souvenirs." : "Ça se prépare en cuisine.", 34, FontWeight.SemiBold);
        heading.TextAlignment = TextAlignment.Center; panel.Children.Add(heading);
        cookingPhrase = Text("", 22); cookingPhrase.TextAlignment = TextAlignment.Center; panel.Children.Add(cookingPhrase);
        var loader = new ProgressBar { Name = "ProcessingLoader", Foreground = Brush.Parse("#742C3F"), IsIndeterminate = !boothSettings.ReducedMotion, Minimum = 0, Maximum = 1, Value = 0, Height = 6, Width = 220 };
        panel.Children.Add(loader);
        var wait = Text("Comptez environ une à deux minutes pour les métamorphoses. Elles peuvent prendre un peu plus de temps.", 19);
        wait.TextAlignment = TextAlignment.Center; wait.IsVisible = mode == PhotoMode.Surprise; panel.Children.Add(wait);
        processingProgress = Text(controller.Message, 18); processingProgress.Foreground = Muted; processingProgress.TextAlignment = TextAlignment.Center;
        panel.Children.Add(processingProgress);
        var cancel = Button("Annuler", async () => { await controller.CancelAsync(); if (!controller.IsBusy) RenderHome(); }); cancel.Name = "CancelSession";
        cancel.HorizontalAlignment = HorizontalAlignment.Center; panel.Children.Add(cancel);
        cancel.IsVisible = cloud is null || controller.Session is null || !galleries.ContainsKey(controller.Session.Id);
        layout.Children.Add(panel);
        if (cloud is not null)
        {
            var follow = new StackPanel { Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
            follow.Children.Add(Text("La suite sur votre téléphone.", 30, FontWeight.SemiBold));
            var qr = new Image { Name = "ProcessingQr", IsVisible = false, HorizontalAlignment = HorizontalAlignment.Left };
            var ready = controller.Session is { } session && galleries.TryGetValue(session.Id, out var link);
            if (ready) ShowQr(qr, galleries[controller.Session!.Id]);
            follow.Children.Add(qr);
            var explanation = Text(ready ? "Scannez maintenant. Vos photos apparaîtront ici au fur et à mesure, même si vous quittez la borne." : "Votre galerie se prépare. Le QR apparaîtra ici dans un instant.", 20);
            explanation.Name = "ProcessingShareStatus"; follow.Children.Add(explanation);
            var next = Button("Une nouvelle séance", StartNextSessionAsync, true);
            next.Name = "ContinueOnPhone"; next.IsEnabled = ready; next.HorizontalAlignment = HorizontalAlignment.Stretch;
            follow.Children.Add(next);
            AddProjectionNote(follow);
            var hint = Text("Vous pouvez aussi rester ici pour découvrir et imprimer votre planche.", 17); hint.Foreground = Muted; follow.Children.Add(hint);
            Grid.SetColumn(follow, 1); layout.Children.Add(follow);
        }
        body.Content = layout; phraseIndex = -1; cookingClock.Restart();
        cooking.Tick -= AnimateCooking; cooking.Tick += AnimateCooking;
        cooking.Interval = TimeSpan.FromMilliseconds(boothSettings.ReducedMotion ? 1000 : 80);
        AnimateCooking(null, EventArgs.Empty); cooking.Start();
    }
    private void AnimateCooking(object? sender, EventArgs args)
    {
        var phrases = sessionStyle?.Phrases ?? StyleCatalog.Surprise.Phrases;
        var next = (int)(cookingClock.Elapsed.TotalSeconds / 5) % phrases.Length;
        if (next != phraseIndex) { phraseIndex = next; if (cookingPhrase is not null) cookingPhrase.Text = phrases[next]; }
        cookingMove.Y = boothSettings.ReducedMotion ? 0 : Math.Sin(cookingClock.Elapsed.TotalSeconds * 2 * Math.PI / 2.5) * 5;
    }
    private void StopProcessing()
    { cooking.Stop(); cookingClock.Stop(); cookingMove.Y = 0; cookingImage = null; cookingPhrase = null; processingProgress = null; }
}

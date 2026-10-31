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
using Avalonia.Styling;
using PhotoBooth.Core;

namespace PhotoBooth.App;

public sealed partial class MainWindow
{
    private StyleCatalog catalog = StyleCatalog.BuiltIn;
    private StyleCatalog? pendingCatalog;
    private readonly HashSet<string> selectedStyles = [StyleCatalog.Chef];
    private readonly Dictionary<string, ToggleButton> styleButtons = [];
    private TextBlock? styleSelectionSummary, styleSelectionHelp;
    private Button? styledStart, chefButton;
    private string StyleSummary => selectedStyles.Count == 0 ? "Choisissez vos univers." :
        selectedStyles.Count == 1 ? catalog.Find(selectedStyles.Single()).Label : $"{selectedStyles.Count} univers pour vos quatre photos.";
    private PhotoStyle? sessionStyle;
    private readonly Func<CancellationToken, Task<StyleCatalog>> catalogLoader;
    private readonly CancellationTokenSource lifetime = new();
    private bool refreshingCatalog;
    private string catalogStatus = "Les univers de départ sont disponibles.";
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
        var known = StyleCatalog.BuiltIn.Styles.Select(style => style.Asset).Append("wordmark").Append("chef");
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

    private bool MetamorphosisAvailable => !boothSettings.CanonCamera || cloud is not null;

    private void RenderHome()
    {
        if (!MetamorphosisAvailable) mode = PhotoMode.Original;
        resultReturn.Stop(); styleButtons.Clear(); styleSelectionSummary = null; styleSelectionHelp = null; styledStart = null; chefButton = null;
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
        choices.Children.Add(ChoiceRow(("Une photo", photoCount == 1, () => { photoCount = 1; if (selectedStyles.Count > 1) { selectedStyles.Clear(); selectedStyles.Add(StyleCatalog.Chef); } RenderHome(); }),
            ("Quatre photos", photoCount == 4, () => { photoCount = 4; RenderHome(); })));
        choices.Children.Add(Text("À votre façon", 20, FontWeight.SemiBold));
        var modes = ChoiceRow(("Au naturel", mode == PhotoMode.Original, () => { mode = PhotoMode.Original; RenderHome(); }),
            ("Métamorphose", mode == PhotoMode.Surprise, () => { mode = PhotoMode.Surprise; RenderStyles(); }));
        var metamorphosis = (Button)modes.Children[1];
        metamorphosis.IsEnabled = MetamorphosisAvailable;
        if (!MetamorphosisAvailable) ToolTip.SetTip(metamorphosis,"Activez la connexion en ligne dans les réglages.");
        choices.Children.Add(modes);
        var detail = Text(!MetamorphosisAvailable ? "Métamorphose demande une connexion." :
            mode == PhotoMode.Original ? "Vos photos, telles quelles." : StyleSummary, 18);
        detail.Foreground = Muted; choices.Children.Add(detail);
        var start = Button("Démarrer", BeginChosenSessionAsync, primary: true); start.Name = "StartSession";
        start.IsEnabled = mode == PhotoMode.Original || (catalog.Styles.Length > 0 && selectedStyles.Count > 0);
        start.HorizontalAlignment = HorizontalAlignment.Stretch; choices.Children.Add(start);
        var note = Text("À télécharger, à imprimer, à garder.", 17); note.Foreground = Muted; choices.Children.Add(note);
        Grid.SetColumn(choices, 1); grid.Children.Add(choices); body.Content = grid;
    }

    private async Task BeginChosenSessionAsync()
    {
        if (mode == PhotoMode.Surprise && !MetamorphosisAvailable)
            throw new InvalidOperationException("Activez la connexion en ligne dans les réglages pour utiliser Métamorphose avec le Canon.");
        if (mode == PhotoMode.Surprise && selectedStyles.Count == 0) return;
        var choices = selectedStyles.Count > 0 ? selectedStyles.ToArray() : new[] { StyleCatalog.Chef };
        sessionStyle = catalog.Find(choices.Length == 1 ? choices[0] : "colors_party");
        await controller.BeginAsync(new SessionOptions(photoCount, mode, styleChoices: choices));
    }

    private void RenderStyles()
    {
        if (!MetamorphosisAvailable) { mode = PhotoMode.Original; RenderHome(); return; }
        var oldOffset = screen == "styles" && body.Content is Grid previous
            ? previous.Children.OfType<ScrollViewer>().FirstOrDefault()?.Offset ?? default : default;
        StopProcessing(); screen = "styles"; styleButtons.Clear();
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        var title = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 14) };
        title.Children.Add(Text("Et si on changeait d’univers ?", compactHome ? 32 : 40, FontWeight.SemiBold));
        styleSelectionHelp = Text(photoCount == 1 ? "Un univers pour votre photo." : "Choisissez jusqu’à quatre univers. Ensuite, place aux poses.", 18);
        styleSelectionHelp.Foreground = Muted; title.Children.Add(styleSelectionHelp); layout.Children.Add(title);
        var sections = new StackPanel { Spacing = 24 };
        foreach (var category in StyleCatalog.Categories)
        {
            var styles = catalog.Styles.Where(style => style.Category == category.Id).ToArray();
            if (styles.Length == 0 && category.Id != "estelle") continue;
            var section = new StackPanel { Name = "Category_" + category.Id, Spacing = 10 };
            section.Children.Add(Text(category.Label, 28, FontWeight.SemiBold));
            sections.Children.Add(section);
            if (styles.Length == 0)
            {
                var waiting = Text("Les univers des sept tables seront bientôt ici.", 17);
                waiting.Foreground = Muted; section.Children.Add(waiting); continue;
            }
            var tiles = new UniformGrid { Columns = 3, Rows = (styles.Length + 2) / 3 };
            section.Children.Add(tiles);
            foreach (var style in styles)
            {
                var button = new ToggleButton { Name = "Style_" + style.Id, Background = ReadableTileBackground(style.Color),
                    Theme = (ControlTheme)Application.Current!.FindResource(typeof(Button))!,
                    Margin = new Thickness(0, 0, 12, 12), HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch, MinHeight = compactHome ? 148 : 170, Padding = new Thickness(14, 10) };
                button.Classes.Add("style-choice");
                button.Click += (_, _) => { ToggleStyle(style.Id); UpdateStyleSelection(); };
                var content = new StackPanel { Spacing = 2 };
                content.Children.Add(Art(style.Asset.Length > 0 ? style.Asset : style.Id, compactHome ? 58 : 70, compactHome ? 58 : 70));
                var label = Text(style.Label, 22, FontWeight.SemiBold); label.TextAlignment = TextAlignment.Center; content.Children.Add(label);
                var description = Text(style.Description, 16); description.TextAlignment = TextAlignment.Center; content.Children.Add(description);
                button.Content = content; AutomationProperties.SetName(button, style.Label);
                styleButtons.Add(style.Id, button); tiles.Children.Add(button);
            }
        }
        var scroll = new ScrollViewer { Name = "StyleScroll", Content = sections, Offset = oldOffset,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(scroll, 1); layout.Children.Add(scroll);
        var footer = new StackPanel { Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
        styleSelectionSummary = Text("", 17); styleSelectionSummary.Name = "StyleSelectionSummary";
        footer.Children.Add(styleSelectionSummary);
        var bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var back = Button("Retour", () => { RenderHome(); return Task.CompletedTask; }); back.Name = "BackFromStyles"; bottom.Children.Add(back);
        var chef = Button("La Surprise du Chef", () => { selectedStyles.Clear(); selectedStyles.Add(StyleCatalog.Chef); UpdateStyleSelection(); return Task.CompletedTask; });
        chefButton = chef; chef.Name = "Style_chef"; chef.Background = Brush.Parse("#F1E4BC"); chef.Margin = new Thickness(14, 0);
        chef.HorizontalAlignment = HorizontalAlignment.Stretch;
        var chefContent = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        chefContent.Children.Add(Art("chef", 44, 44));
        var chefWords = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        chefWords.Children.Add(Text("La Surprise du Chef", 21, FontWeight.SemiBold));
        chefWords.Children.Add(Text(photoCount == 1 ? "Un univers choisi pour vous." : "Le chef varie les univers.", 15)); chefContent.Children.Add(chefWords);
        chef.Content = chefContent; AutomationProperties.SetName(chef, "La Surprise du Chef");
        chef.IsEnabled = catalog.Styles.Length > 0; Grid.SetColumn(chef, 1); bottom.Children.Add(chef);
        styledStart = Button("C’est parti !", BeginChosenSessionAsync, true); styledStart.Name = "StartStyledSession";
        Grid.SetColumn(styledStart, 2); bottom.Children.Add(styledStart);
        footer.Children.Add(bottom); Grid.SetRow(footer, 2); layout.Children.Add(footer); body.Content = layout;
        UpdateStyleSelection();
    }

    private void ToggleStyle(string id)
    {
        if (photoCount == 1) { selectedStyles.Clear(); selectedStyles.Add(id); return; }
        selectedStyles.Remove(StyleCatalog.Chef);
        if (!selectedStyles.Remove(id) && selectedStyles.Count < photoCount) selectedStyles.Add(id);
    }

    private void UpdateStyleSelection()
    {
        var chef = selectedStyles.Contains(StyleCatalog.Chef);
        foreach (var (id, button) in styleButtons)
        {
            var chosen = selectedStyles.Contains(id);
            button.IsChecked = chosen;
            button.BorderBrush = chosen ? Ink : Brushes.Transparent;
            button.BorderThickness = new Thickness(3);
            button.IsEnabled = chosen || chef || photoCount == 1 || selectedStyles.Count < photoCount;
            AutomationProperties.SetHelpText(button, chosen ? "Univers sélectionné." : "Choisir cet univers.");
        }
        if (chefButton is not null)
        {
            chefButton.BorderBrush = chef ? Ink : Brushes.Transparent;
            chefButton.BorderThickness = new Thickness(3);
            AutomationProperties.SetHelpText(chefButton, chef ? "Le chef choisit les univers." : "Remplacer votre sélection par le choix du chef.");
        }
        if (styleSelectionSummary is not null)
            styleSelectionSummary.Text = catalog.Styles.Length == 0 ? "Aucun univers disponible. Revenez pour une photo au naturel." :
                chef ? (photoCount == 1 ? "Le chef choisit pour vous." : "Quatre photos, un mélange d’univers choisi pour vous.") :
                selectedStyles.Count == 0 ? "Choisissez au moins un univers ou laissez faire le chef." :
                photoCount == 1 ? StyleSummary : selectedStyles.Count switch {
                    1 => "Un univers, quatre interprétations.",
                    2 => "Deux univers, deux photos chacun.",
                    3 => "Trois univers, tous utilisés. L’un revient pour la quatrième photo.",
                    _ => "Quatre univers, une photo chacun. Retirez un choix pour en changer."
                };
        if (styledStart is not null) styledStart.IsEnabled = catalog.Styles.Length > 0 && selectedStyles.Count > 0;
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
        selectedStyles.RemoveWhere(id => id != StyleCatalog.Chef && !catalog.Styles.Any(s => s.Id == id));
        if (selectedStyles.Count == 0) selectedStyles.Add(StyleCatalog.Chef);
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

using System.Text.Json;
using System.Text.RegularExpressions;

namespace PhotoBooth.Core;

public sealed record PhotoStyle(string Id, string Label, string Description, string Asset, string Color, string[] Phrases, string Category = "other");
public sealed record StyleCategory(string Id, string Label);
public sealed record StyleCatalog(string Version, PhotoStyle[] Styles)
{
    public const string Chef = "chef";
    public const string Mixed = "mix";
    public static IReadOnlyList<StyleCategory> Categories { get; } = Array.AsReadOnly(new[] {
        new StyleCategory("estelle", "by Estelle"), new StyleCategory("essentials", "Les incontournables"), new StyleCategory("other", "Autres") });
    public static readonly PhotoStyle Surprise = new(Chef, "La Surprise du Chef", "Laissez le chef choisir votre univers.", "chef", "#F1E4BC",
        ["Le chef garde sa recette secrète.", "La brigade s’active.", "Vos sourires sont entre de bonnes mains."]);
    public static StyleCatalog BuiltIn { get; } = LoadBuiltIn();
    public PhotoStyle Find(string id) => Styles.FirstOrDefault(s => s.Id == id) ?? Surprise;
    public static bool ValidId(string id) => Regex.IsMatch(id, "^[a-z][a-z0-9_]{0,47}$", RegexOptions.CultureInvariant);
    public static StyleCatalog Parse(string json)
    {
        var result = JsonSerializer.Deserialize<StyleCatalog>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("La liste des univers est vide.");
        if (string.IsNullOrWhiteSpace(result.Version) || result.Version.Length > 80 || result.Styles is null || result.Styles.Length > 200 ||
            result.Styles.Select(s => s?.Id).Distinct().Count() != result.Styles.Length ||
            result.Styles.Any(s => s is null || s.Id is null || !ValidId(s.Id) || s.Id is Chef or Mixed || string.IsNullOrWhiteSpace(s.Label) || s.Label.Length > 48 ||
                s.Description is null || s.Description.Length > 140 || s.Asset is null || s.Asset.Length > 80 || s.Color is null ||
                !Categories.Any(category => category.Id == s.Category) ||
                !GuestCopy(s.Label) || !GuestCopy(s.Description) ||
                !Regex.IsMatch(s.Color, "^#[0-9A-Fa-f]{6}$") || s.Phrases is null || s.Phrases.Length is < 1 or > 20 ||
                s.Phrases.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 120 || !GuestCopy(p))))
            throw new InvalidDataException("La liste reçue n’est pas exploitable. Les univers précédents restent disponibles.");
        return result;
    }
    private static bool GuestCopy(string text) => !Regex.IsMatch(text, @"[\p{Pd}]|\b(AI|IA|OpenAI)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static StyleCatalog LoadBuiltIn()
    {
        using var stream = typeof(StyleCatalog).Assembly.GetManifestResourceStream("PhotoBooth.styles.json")!;
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }
}

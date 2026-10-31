using System.Text.Json;
using PhotoBooth.Imaging;

namespace PhotoBooth.Runtime;

public sealed record BoothSettings
{
    public const int CurrentHardwareDefaultsVersion = 1;
    public int HardwareDefaultsVersion { get; init; } = CurrentHardwareDefaultsVersion;
    public bool CanonCamera { get; init; } = true;
    public bool CanonUseOfficialSdk { get; init; }
    public bool CanonManualFocusConfirmed { get; init; }
    public bool ReducedMotion { get; init; }
    public bool FullscreenEnabled { get; init; }
    public bool PhysicalPrinting { get; init; } = true;
    public string PrinterName { get; init; } = "";
    public bool CloudEnabled { get; init; }
    public bool ViewerEnabled { get; init; }
    public bool ViewerIncludeTests { get; init; }
    public string GalleryBaseUrl { get; init; } = "";
    public string ApiBaseUrl { get; init; } = "https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api";
    public static BoothSettings Load(string path)
    {
        if (!File.Exists(path)) return new();
        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<BoothSettings>(json) ?? new();
        using var document = JsonDocument.Parse(json);
        // Older releases started in rehearsal mode. Upgrade those profiles once;
        // subsequently saved, explicit rehearsal choices remain available to tests.
        if (!document.RootElement.TryGetProperty(nameof(HardwareDefaultsVersion), out var version) ||
            version.GetInt32() < CurrentHardwareDefaultsVersion)
            settings = settings with { HardwareDefaultsVersion = CurrentHardwareDefaultsVersion, CanonCamera = true, PhysicalPrinting = true };
        return settings;
    }
    public Task SaveAsync(string path) => SessionStore.WriteAtomicAsync(path, JsonSerializer.SerializeToUtf8Bytes(this, SessionStore.JsonOptions));
}

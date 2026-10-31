using System.Text.Json;
using PhotoBooth.Imaging;

namespace PhotoBooth.Runtime;

public sealed record BoothSettings
{
    public bool CanonCamera { get; init; }
    public bool CanonUseOfficialSdk { get; init; }
    public bool CanonManualFocusConfirmed { get; init; }
    public bool ReducedMotion { get; init; }
    public bool PhysicalPrinting { get; init; }
    public string PrinterName { get; init; } = "";
    public bool CloudEnabled { get; init; }
    public bool ViewerEnabled { get; init; }
    public bool ViewerIncludeTests { get; init; }
    public string GalleryBaseUrl { get; init; } = "";
    public string ApiBaseUrl { get; init; } = "https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api";
    public static BoothSettings Load(string path) => File.Exists(path)
        ? JsonSerializer.Deserialize<BoothSettings>(File.ReadAllText(path)) ?? new() : new();
    public Task SaveAsync(string path) => SessionStore.WriteAtomicAsync(path, JsonSerializer.SerializeToUtf8Bytes(this, SessionStore.JsonOptions));
}

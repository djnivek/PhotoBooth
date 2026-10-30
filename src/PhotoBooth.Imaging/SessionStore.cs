using System.Security.Cryptography;
using System.Text.Json;

namespace PhotoBooth.Imaging;

public sealed record StoredAsset(string Key, string Sha256, long Bytes);
public sealed record PrintJournal(Guid RequestId, string SheetKey, string Sha256, string State,
    DateTimeOffset UpdatedAt, string? SpoolJobId = null);

public sealed class SessionStore
{
    public string Root { get; }
    public SessionStore(string root) => Root = Path.GetFullPath(root);

    public async Task<StoredAsset> SavePhotoAsync(Guid sessionId, byte[] jpeg, CancellationToken token = default)
    {
        var key = $"{sessionId:N}/{Guid.NewGuid():N}.jpg";
        await WriteAtomicAsync(Resolve(key), jpeg, token);
        return new(key, Hash(jpeg), jpeg.LongLength);
    }

    public string Resolve(string key)
    {
        // Keys never accept rooted paths, dot segments, backslashes or caller filenames.
        var parts = key.Split('/');
        if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out _) ||
            !parts[1].EndsWith(".jpg", StringComparison.Ordinal) ||
            !Guid.TryParseExact(parts[1][..^4], "N", out _))
            throw new ArgumentException("Invalid asset key.", nameof(key));
        return Path.Combine(Root, parts[0], parts[1]);
    }

    public async Task<byte[]> ReadVerifiedAsync(StoredAsset asset, CancellationToken token = default)
    {
        var bytes = await File.ReadAllBytesAsync(Resolve(asset.Key), token);
        if (bytes.LongLength != asset.Bytes || Hash(bytes) != asset.Sha256)
            throw new InvalidDataException("Le fichier photo a changé. Impression interrompue.");
        return bytes;
    }

    public Task WriteManifestAsync(Guid id, object manifest, CancellationToken token = default) =>
        WriteAtomicAsync(Path.Combine(Root, id.ToString("N"), "session.json"),
            JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions), token);

    public Task WritePrintJournalAsync(Guid id, PrintJournal journal, CancellationToken token = default) =>
        WriteAtomicAsync(Path.Combine(Root, id.ToString("N"), "print.json"),
            JsonSerializer.SerializeToUtf8Bytes(journal, JsonOptions), token);

    public static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task WriteAtomicAsync(string path, byte[] bytes, CancellationToken token = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, token);
                await stream.FlushAsync(token);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

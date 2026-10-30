using PhotoBooth.Core;

namespace PhotoBooth.Runtime;

/// <summary>One bounded request at startup or on operator action. No polling, no disk cache.</summary>
public static class StyleCatalogClient
{
    public static async Task<StyleCatalog> FetchAsync(string apiBaseUrl, CancellationToken token, HttpMessageHandler? handler = null)
    {
        var uri = new Uri(apiBaseUrl.TrimEnd('/') + "/styles");
        if (uri.Scheme != "https") throw new ArgumentException("Le catalogue doit utiliser HTTPS.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(6)); token = deadline.Token;
        using var client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(6) };
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        const int limit = 256 * 1024;
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("Le catalogue est trop volumineux.");
        await using var input = await response.Content.ReadAsStreamAsync(token);
        using var output = new MemoryStream(); var buffer = new byte[8192]; int read;
        while ((read = await input.ReadAsync(buffer, token)) > 0)
        { if (output.Length + read > limit) throw new InvalidDataException("Le catalogue est trop volumineux."); output.Write(buffer, 0, read); }
        return StyleCatalog.Parse(System.Text.Encoding.UTF8.GetString(output.ToArray()));
    }
}

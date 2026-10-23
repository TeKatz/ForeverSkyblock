using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public static class ReleaseManifestFallback
{
    private const string Owner = "TeKatz";
    private const string Repository = "ForeverSkyblock";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HttpClient Client = CreateClient();

    public static async Task<UpdateManifest?> CheckAsync(
        UpdateChannel channel,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        var tag = channel == UpdateChannel.Stable
            ? "gui-designer-stable"
            : "gui-designer-development";
        var uri = new Uri(
            $"https://github.com/{Owner}/{Repository}/releases/download/{tag}/update-manifest.json?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, timeout.Token);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, timeout.Token);
        if (manifest is null ||
            string.IsNullOrWhiteSpace(manifest.Commit) ||
            string.IsNullOrWhiteSpace(manifest.Sha256) ||
            manifest.Size <= 0)
        {
            throw new InvalidDataException("The fallback update manifest is incomplete.");
        }

        return manifest;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All
        })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ForeverGuiDesigner", "1.0"));
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        return client;
    }
}

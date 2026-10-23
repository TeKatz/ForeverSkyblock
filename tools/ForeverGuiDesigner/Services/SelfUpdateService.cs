using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ForeverGuiDesigner.Services;

public enum UpdateChannel
{
    Development,
    Stable
}

public sealed class UpdateManifest
{
    public string Channel { get; set; } = "development";
    public string Commit { get; set; } = string.Empty;
    public string Asset { get; set; } = "ForeverGuiDesigner.exe";
    public string Sha256 { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset BuiltAt { get; set; }
}

public sealed class UpdatePreferences
{
    public UpdateChannel Channel { get; set; } = UpdateChannel.Development;
    public bool AutoCheck { get; set; } = true;
}

public static class SelfUpdateService
{
    private const string Owner = "TeKatz";
    private const string Repository = "ForeverSkyblock";
    private const string ApplyArgument = "--apply-update";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static readonly HttpClient Client = CreateClient();

    private sealed class GitHubReleaseInfo
    {
        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("digest")]
        public string? Digest { get; set; }
    }

    public static string CurrentCommit
    {
        get
        {
            var value = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (string.IsNullOrWhiteSpace(value))
                return "local";

            var plus = value.IndexOf('+');
            if (plus >= 0)
                value = value[..plus];

            return value.Trim();
        }
    }

    public static string ShortCommit(string? commit)
    {
        if (string.IsNullOrWhiteSpace(commit))
            return "unknown";
        return commit.Length <= 8 ? commit : commit[..8];
    }

    public static UpdatePreferences LoadPreferences()
    {
        try
        {
            var path = PreferencesPath();
            if (!File.Exists(path))
                return new UpdatePreferences();
            return JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllText(path), JsonOptions)
                   ?? new UpdatePreferences();
        }
        catch
        {
            return new UpdatePreferences();
        }
    }

    public static void SavePreferences(UpdatePreferences preferences)
    {
        try
        {
            var path = PreferencesPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(preferences, JsonOptions));
        }
        catch
        {
            // Update preferences are optional; editor work must never fail because of them.
        }
    }

    public static async Task<UpdateManifest?> CheckAsync(UpdateChannel channel, CancellationToken cancellationToken = default)
    {
        // Checking for an update should be a tiny API request, not a release-asset
        // download. The old implementation fetched update-manifest.json through
        // GitHub's release download redirect and inherited the 10 minute download
        // timeout. On some Windows/network combinations that request can stall,
        // leaving the editor visibly stuck on "Checking…".
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));

        using var response = await Client.GetAsync(
            ReleaseApiUri(channel),
            HttpCompletionOption.ResponseContentRead,
            timeout.Token);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseInfo>(stream, JsonOptions, timeout.Token)
                      ?? throw new InvalidDataException("GitHub returned an empty update release response.");

        var asset = release.Assets.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, "ForeverGuiDesigner.exe", StringComparison.OrdinalIgnoreCase));
        if (asset is null)
            throw new InvalidDataException("The update release does not contain ForeverGuiDesigner.exe.");

        var commit = ExtractBuildCommit(release.Body);
        if (string.IsNullOrWhiteSpace(commit))
            throw new InvalidDataException("The update release does not identify its build commit.");

        var digest = asset.Digest;
        if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("GitHub did not provide a SHA-256 digest for the update executable.");

        return new UpdateManifest
        {
            Channel = channel == UpdateChannel.Stable ? "stable" : "development",
            Commit = commit,
            Asset = asset.Name,
            Sha256 = digest["sha256:".Length..].Trim().ToLowerInvariant(),
            Size = asset.Size,
            BuiltAt = release.PublishedAt ?? DateTimeOffset.MinValue,
            Notes = channel == UpdateChannel.Stable
                ? "Latest stable build from main."
                : "Latest development build from feature/forever-gui-designer-mvp."
        };
    }

    public static bool IsUpdateAvailable(UpdateManifest manifest)
        => !string.Equals(CurrentCommit, manifest.Commit, StringComparison.OrdinalIgnoreCase);

    public static async Task<string> DownloadAsync(
        UpdateChannel channel,
        UpdateManifest manifest,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ForeverGuiDesigner", "updates", manifest.Commit);
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, "ForeverGuiDesigner.new.exe");

        using var response = await Client.GetAsync(AssetUri(channel, manifest.Asset), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? manifest.Size;
        var buffer = new byte[1024 * 128];
        long readTotal = 0;

        // Keep the download streams inside their own scope. The previous version
        // tried to reopen the destination for SHA-256 verification while the writer
        // still held it with FileShare.None, so Windows correctly rejected our own
        // read attempt as "used by another process".
        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, useAsync: true))
        {
            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken);
                if (read <= 0)
                    break;
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                readTotal += read;
                if (total > 0)
                    progress?.Report(Math.Clamp((double)readTotal / total, 0, 1));
            }

            await output.FlushAsync(cancellationToken);
        }

        if (manifest.Size > 0 && readTotal != manifest.Size)
            throw new InvalidDataException($"Downloaded update size mismatch. Expected {manifest.Size} bytes, got {readTotal}.");

        var actualHash = await Sha256Async(destination, cancellationToken);
        if (!string.Equals(actualHash, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destination);
            throw new InvalidDataException("Downloaded update failed SHA-256 verification.");
        }

        progress?.Report(1);
        return destination;
    }

    public static void BeginApplyAndRestart(string downloadedExe, UpdateManifest manifest)
    {
        var currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
            throw new InvalidOperationException("Could not resolve the running editor executable.");
        if (!File.Exists(downloadedExe))
            throw new FileNotFoundException("The downloaded update could not be found.", downloadedExe);

        var updaterDirectory = Path.Combine(Path.GetTempPath(), "ForeverGuiDesigner", "updater", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(updaterDirectory);
        var updaterExe = Path.Combine(updaterDirectory, "ForeverGuiDesigner.Updater.exe");
        File.Copy(currentExe, updaterExe, overwrite: true);

        var start = new ProcessStartInfo(updaterExe)
        {
            UseShellExecute = false,
            WorkingDirectory = updaterDirectory
        };
        start.ArgumentList.Add(ApplyArgument);
        start.ArgumentList.Add(currentExe);
        start.ArgumentList.Add(downloadedExe);
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(manifest.Commit);

        if (Process.Start(start) is null)
            throw new InvalidOperationException("Could not start the update helper.");
    }

    public static async Task<bool> TryRunApplyModeAsync(string[] args)
    {
        if (args.Length < 5 || !string.Equals(args[0], ApplyArgument, StringComparison.Ordinal))
            return false;

        var targetExe = Path.GetFullPath(args[1]);
        var downloadedExe = Path.GetFullPath(args[2]);
        _ = int.TryParse(args[3], out var parentPid);
        var commit = args[4];

        if (parentPid > 0)
        {
            try
            {
                using var parent = Process.GetProcessById(parentPid);
                await parent.WaitForExitAsync();
            }
            catch (ArgumentException)
            {
                // Parent already exited.
            }
        }

        Exception? lastError = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                File.Copy(downloadedExe, targetExe, overwrite: true);
                lastError = null;
                break;
            }
            catch (IOException ex)
            {
                lastError = ex;
                await Task.Delay(200);
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
                await Task.Delay(200);
            }
        }

        if (lastError is not null)
        {
            System.Windows.MessageBox.Show(
                $"Forever GUI Designer could not replace the old executable.\n\n{lastError.Message}",
                "Update failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return true;
        }

        try
        {
            File.Delete(downloadedExe);
            var updateDirectory = Path.GetDirectoryName(downloadedExe);
            if (!string.IsNullOrWhiteSpace(updateDirectory) && Directory.Exists(updateDirectory) && !Directory.EnumerateFileSystemEntries(updateDirectory).Any())
                Directory.Delete(updateDirectory);
        }
        catch
        {
        }

        var restart = new ProcessStartInfo(targetExe)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(targetExe) ?? Environment.CurrentDirectory
        };
        restart.ArgumentList.Add("--updated-from");
        restart.ArgumentList.Add(commit);
        Process.Start(restart);

        ScheduleSelfCleanup();
        return true;
    }

    private static string ExtractBuildCommit(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        var match = Regex.Match(body, @"Build:\s*([0-9a-fA-F]{40})", RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : string.Empty;
    }

    private static async Task<string> Sha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, useAsync: true);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            // Large EXE downloads are allowed to take a while. CheckAsync adds its
            // own much shorter linked cancellation timeout for version checks.
            Timeout = TimeSpan.FromMinutes(10)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ForeverGuiDesigner", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static Uri ReleaseApiUri(UpdateChannel channel)
        => new($"https://api.github.com/repos/{Owner}/{Repository}/releases/tags/{Tag(channel)}");

    private static Uri AssetUri(UpdateChannel channel, string asset)
        => new($"https://github.com/{Owner}/{Repository}/releases/download/{Tag(channel)}/{Uri.EscapeDataString(asset)}");

    private static string Tag(UpdateChannel channel)
        => channel == UpdateChannel.Stable ? "gui-designer-stable" : "gui-designer-development";

    private static string PreferencesPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForeverGuiDesigner", "update-settings.json");

    private static void ScheduleSelfCleanup()
    {
        try
        {
            var directory = Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(directory))
                return;

            var command = $"timeout /t 2 /nobreak >nul & rmdir /s /q \"{directory}\"";
            Process.Start(new ProcessStartInfo("cmd.exe", "/d /c " + command)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch
        {
        }
    }
}

using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Resolves GUI slot previews directly from the checked-out Minecraft resource tree. The resolver
/// deliberately reads the working tree rather than a Git index so newly added/uncommitted project
/// assets are immediately available to Forever Studio.
/// </summary>
public sealed class MinecraftAssetResolver
{
    private readonly string _repoRoot;

    public MinecraftAssetResolver(string repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public string? ResolveItemTexture(string itemId)
    {
        if (!TryResolveProjectResourceId(itemId, out var ns, out var path))
            return null;

        var modelPath = AssetPath(ns, "models", "item", path + ".json");
        if (File.Exists(modelPath))
        {
            try
            {
                using var stream = File.OpenRead(modelPath);
                using var json = JsonDocument.Parse(stream);
                if (json.RootElement.TryGetProperty("textures", out var textures) &&
                    ResolveTextureMap(textures, ns) is { } ownTexture)
                    return ownTexture;

                if (json.RootElement.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.String &&
                    ResolveModelReference(parent.GetString(), ns, out var parentNamespace, out var parentResource))
                {
                    var inherited = ResolveTextureFromModel(
                        AssetPath(parentNamespace, "models", parentResource + ".json"),
                        parentNamespace,
                        0);
                    if (inherited is not null)
                        return inherited;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // A malformed/in-flight preview model must not make the designer unusable.
            }
        }

        var directItem = ResolveDirectTexture(ns, "item", path);
        if (directItem is not null)
            return directItem;

        // BlockItems commonly point at a block model, but locally inserted project blocks are also
        // allowed to exist before their item-model wrapper has been authored. Resolve the blockstate
        // and block model as a deterministic preview fallback instead of treating the resource id as
        // unknown. This is generic for every namespace in the checked-out project.
        return ResolveBlockTexture(ns, path);
    }

    public BitmapImage? LoadItemBitmap(string itemId)
    {
        var path = ResolveItemTexture(itemId);
        if (path is null)
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private string? ResolveBlockTexture(string ns, string path)
    {
        var blockstatePath = AssetPath(ns, "blockstates", path + ".json");
        if (File.Exists(blockstatePath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(blockstatePath));
                var modelReference = FindFirstBlockstateModel(document.RootElement);
                if (ResolveModelReference(modelReference, ns, out var modelNamespace, out var modelResource))
                {
                    var resolved = ResolveTextureFromModel(
                        AssetPath(modelNamespace, "models", modelResource + ".json"),
                        modelNamespace,
                        0);
                    if (resolved is not null)
                        return resolved;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Fall through to the conventional block-model path/direct texture.
            }
        }

        var conventional = ResolveTextureFromModel(
            AssetPath(ns, "models", "block", path + ".json"),
            ns,
            0);
        return conventional ?? ResolveDirectTexture(ns, "block", path);
    }

    private string? ResolveTextureFromModel(string modelPath, string modelNamespace, int depth)
    {
        if (depth > 8 || !File.Exists(modelPath))
            return null;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(modelPath));
            if (json.RootElement.TryGetProperty("textures", out var textures) &&
                ResolveTextureMap(textures, modelNamespace) is { } ownTexture)
                return ownTexture;

            if (json.RootElement.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.String &&
                ResolveModelReference(parent.GetString(), modelNamespace, out var parentNamespace, out var parentResource))
            {
                return ResolveTextureFromModel(
                    AssetPath(parentNamespace, "models", parentResource + ".json"),
                    parentNamespace,
                    depth + 1);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Preview resolution is best-effort; validation owns malformed-resource diagnostics.
        }
        return null;
    }

    private string? ResolveTextureMap(JsonElement textures, string defaultNamespace)
    {
        if (textures.ValueKind != JsonValueKind.Object)
            return null;

        var values = textures.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String)
            .ToDictionary(
                property => property.Name,
                property => property.Value.GetString() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        foreach (var key in new[] { "layer0", "all", "particle", "side", "top", "end", "front" })
        {
            if (values.TryGetValue(key, out var value) &&
                ResolveTextureValue(values, value, defaultNamespace, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) is { } resolved)
                return resolved;
        }

        foreach (var value in values.Values)
        {
            if (ResolveTextureValue(values, value, defaultNamespace, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) is { } resolved)
                return resolved;
        }
        return null;
    }

    private string? ResolveTextureValue(
        IReadOnlyDictionary<string, string> textures,
        string value,
        string defaultNamespace,
        HashSet<string> visited)
    {
        var current = value.Trim();
        if (current.StartsWith('#'))
        {
            var key = current[1..];
            if (!visited.Add(key) || !textures.TryGetValue(key, out var next))
                return null;
            return ResolveTextureValue(textures, next, defaultNamespace, visited);
        }

        if (!ResolveReference(current, defaultNamespace, out var ns, out var path))
            return null;
        var full = AssetPath(ns, "textures", path + ".png");
        return File.Exists(full) ? full : null;
    }

    private string? ResolveDirectTexture(string ns, string kind, string path)
    {
        var full = AssetPath(ns, "textures", kind, path + ".png");
        return File.Exists(full) ? full : null;
    }

    private bool TryResolveProjectResourceId(string value, out string ns, out string path)
    {
        ns = string.Empty;
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        var colon = trimmed.IndexOf(':');
        if (colon >= 0)
        {
            ns = trimmed[..colon].Trim();
            path = trimmed[(colon + 1)..].Trim();
            return ns.Length > 0 && path.Length > 0;
        }

        path = trimmed;
        var assetsRoot = Path.Combine(_repoRoot, "mod", "src", "main", "resources", "assets");
        if (Directory.Exists(assetsRoot))
        {
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(assetsRoot)
                             .OrderBy(candidate => Path.GetFileName(candidate).Equals("forever_skyblock", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                             .ThenBy(candidate => candidate, StringComparer.OrdinalIgnoreCase))
                {
                    var candidateNamespace = Path.GetFileName(directory);
                    if (HasProjectResource(candidateNamespace, path))
                    {
                        ns = candidateNamespace;
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Fall back to Minecraft's namespace for an unqualified id.
            }
        }

        ns = "minecraft";
        return true;
    }

    private bool HasProjectResource(string ns, string path)
        => File.Exists(AssetPath(ns, "models", "item", path + ".json")) ||
           File.Exists(AssetPath(ns, "blockstates", path + ".json")) ||
           File.Exists(AssetPath(ns, "models", "block", path + ".json")) ||
           File.Exists(AssetPath(ns, "textures", "item", path + ".png")) ||
           File.Exists(AssetPath(ns, "textures", "block", path + ".png"));

    private static string? FindFirstBlockstateModel(JsonElement root)
    {
        if (root.TryGetProperty("variants", out var variants) && variants.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in variants.EnumerateObject())
            {
                if (FindModelInCandidate(property.Value) is { } model)
                    return model;
            }
        }

        if (root.TryGetProperty("multipart", out var multipart) && multipart.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in multipart.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.Object && part.TryGetProperty("apply", out var apply) &&
                    FindModelInCandidate(apply) is { } model)
                    return model;
            }
        }
        return null;
    }

    private static string? FindModelInCandidate(JsonElement candidate)
    {
        if (candidate.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in candidate.EnumerateArray())
            {
                if (FindModelInCandidate(item) is { } model)
                    return model;
            }
            return null;
        }

        return candidate.ValueKind == JsonValueKind.Object &&
               candidate.TryGetProperty("model", out var modelProperty) &&
               modelProperty.ValueKind == JsonValueKind.String
            ? modelProperty.GetString()
            : null;
    }

    private static bool ResolveModelReference(
        string? reference,
        string defaultNamespace,
        out string ns,
        out string resource)
        => ResolveReference(reference, defaultNamespace, out ns, out resource);

    private static bool ResolveReference(
        string? reference,
        string defaultNamespace,
        out string ns,
        out string resource)
    {
        ns = defaultNamespace;
        resource = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
            return false;

        var value = reference.Trim();
        var colon = value.IndexOf(':');
        if (colon >= 0)
        {
            ns = value[..colon].Trim();
            resource = value[(colon + 1)..].Trim();
        }
        else
        {
            resource = value;
        }
        return ns.Length > 0 && resource.Length > 0;
    }

    private string AssetPath(string ns, params string[] parts)
    {
        var all = new List<string> { _repoRoot, "mod", "src", "main", "resources", "assets", ns };
        all.AddRange(parts);
        return Path.Combine(all.ToArray());
    }
}

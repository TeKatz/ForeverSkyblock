using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace ForeverGuiDesigner.Services;

internal sealed class MinecraftAssetResolver
{
    private readonly string _repoRoot;

    public MinecraftAssetResolver(string repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public string? ResolveItemTexture(string itemId)
    {
        if (!TrySplitId(itemId, out var ns, out var path))
            return null;

        var modelPath = AssetPath(ns, "models", "item", path + ".json");
        if (!File.Exists(modelPath))
            return ResolveDirectTexture(ns, "item", path);

        try
        {
            using var stream = File.OpenRead(modelPath);
            using var json = JsonDocument.Parse(stream);
            if (json.RootElement.TryGetProperty("textures", out var textures))
            {
                foreach (var key in new[] { "layer0", "all", "particle" })
                {
                    if (!textures.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String)
                        continue;
                    var resolved = ResolveTextureReference(value.GetString());
                    if (resolved is not null)
                        return resolved;
                }
            }

            if (json.RootElement.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.String)
            {
                var parentId = parent.GetString();
                if (!string.IsNullOrWhiteSpace(parentId) && !parentId.StartsWith("minecraft:item/", StringComparison.OrdinalIgnoreCase))
                {
                    if (TrySplitId(parentId!, out var pns, out var ppath))
                    {
                        var parentModel = AssetPath(pns, "models", ppath + ".json");
                        var inherited = ResolveTextureFromModel(parentModel, 0);
                        if (inherited is not null)
                            return inherited;
                    }
                }
            }
        }
        catch
        {
            // A malformed preview model must not make the designer unusable.
        }

        return ResolveDirectTexture(ns, "item", path);
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

    private string? ResolveTextureFromModel(string modelPath, int depth)
    {
        if (depth > 8 || !File.Exists(modelPath))
            return null;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(modelPath));
            if (json.RootElement.TryGetProperty("textures", out var textures))
            {
                foreach (var key in new[] { "layer0", "all", "particle" })
                {
                    if (textures.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        var resolved = ResolveTextureReference(value.GetString());
                        if (resolved is not null)
                            return resolved;
                    }
                }
            }
            if (json.RootElement.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.String &&
                TrySplitId(parent.GetString() ?? string.Empty, out var ns, out var path))
                return ResolveTextureFromModel(AssetPath(ns, "models", path + ".json"), depth + 1);
        }
        catch { }
        return null;
    }

    private string? ResolveTextureReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.StartsWith('#'))
            return null;
        if (!TrySplitId(reference, out var ns, out var path))
            return null;
        var full = AssetPath(ns, "textures", path + ".png");
        return File.Exists(full) ? full : null;
    }

    private string? ResolveDirectTexture(string ns, string kind, string path)
    {
        var full = AssetPath(ns, "textures", kind, path + ".png");
        return File.Exists(full) ? full : null;
    }

    private string AssetPath(string ns, params string[] parts)
    {
        var all = new List<string> { _repoRoot, "mod", "src", "main", "resources", "assets", ns };
        all.AddRange(parts);
        return Path.Combine(all.ToArray());
    }

    private static bool TrySplitId(string value, out string ns, out string path)
    {
        ns = "minecraft";
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var split = value.Split(':', 2);
        if (split.Length == 2)
        {
            ns = split[0];
            path = split[1];
        }
        else
        {
            path = split[0];
        }
        return ns.Length > 0 && path.Length > 0;
    }
}

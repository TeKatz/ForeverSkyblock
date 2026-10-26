using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Services;

public enum ForeverStudioLibraryItemKind
{
    Asset,
    Function,
    Animation,
    Template
}

public enum ForeverStudioLibraryPortDirection
{
    Input,
    Output
}

public sealed class ForeverStudioLibraryPort
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "any";
    public ForeverStudioLibraryPortDirection Direction { get; set; }
    public bool Required { get; set; }
    public bool Multiple { get; set; }
}

public sealed class ForeverStudioLibraryDependency
{
    public string ItemId { get; set; } = string.Empty;
    public int MinimumVersion { get; set; } = 1;
}

public sealed class ForeverStudioLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public ForeverStudioLibraryItemKind Kind { get; set; }
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GeneratorKey { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<string> Capabilities { get; set; } = [];
    public List<ForeverStudioLibraryPort> Ports { get; set; } = [];
    public List<ForeverStudioLibraryDependency> Dependencies { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverStudioLibraryPack
{
    public int ContractVersion { get; set; } = ForeverStudioLibraryCatalog.CurrentContractVersion;
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string DisplayName { get; set; } = string.Empty;
    public List<ForeverStudioLibraryItem> Items { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed record ForeverStudioLibraryLoadResult(
    ForeverStudioLibraryCatalog Catalog,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Versioned registry for reusable Machine Builder building blocks. Phase 1 intentionally ships only
/// a very small core set; Phase 2 can grow the library through data packs without changing the graph
/// or machine-document contracts established here.
/// </summary>
public sealed class ForeverStudioLibraryCatalog
{
    public const int CurrentContractVersion = 1;
    public const string RelativeLibraryDirectory =
        "mod/src/main/resources/data/forever_skyblock/forever_studio/library";

    private const long MaxPackBytes = 1024 * 1024;
    private const int MaxPackFiles = 128;
    private const int MaxItemsPerPack = 512;
    private const int MaxPortsPerItem = 64;
    private const int MaxDependenciesPerItem = 64;

    private readonly Dictionary<string, SortedDictionary<int, ForeverStudioLibraryItem>> _items =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 48,
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyList<ForeverStudioLibraryItem> AllItems
        => _items.Values
            .SelectMany(versions => versions.Values)
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Version)
            .ToList();

    public static ForeverStudioLibraryLoadResult Load(string? repoRoot)
    {
        var catalog = new ForeverStudioLibraryCatalog();
        var warnings = new List<string>();
        foreach (var item in CreateCoreItems())
            catalog.Register(item, "built-in core", warnings);

        if (string.IsNullOrWhiteSpace(repoRoot))
            return new ForeverStudioLibraryLoadResult(catalog, warnings);

        string root;
        try
        {
            root = Path.Combine(
                Path.GetFullPath(repoRoot.Trim()),
                RelativeLibraryDirectory.Replace('/', Path.DirectorySeparatorChar));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            warnings.Add($"Library path is invalid: {ex.Message}");
            return new ForeverStudioLibraryLoadResult(catalog, warnings);
        }

        if (!Directory.Exists(root))
            return new ForeverStudioLibraryLoadResult(catalog, warnings);

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(MaxPackFiles + 1)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warnings.Add($"Project library cannot be read: {ex.Message}");
            return new ForeverStudioLibraryLoadResult(catalog, warnings);
        }

        if (files.Count > MaxPackFiles)
        {
            warnings.Add($"Project library contains more than {MaxPackFiles} pack files. Only the first {MaxPackFiles} are loaded.");
            files = files.Take(MaxPackFiles).ToList();
        }

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            try
            {
                if (new FileInfo(file).Length > MaxPackBytes)
                {
                    warnings.Add($"{relative}: library pack exceeds the {MaxPackBytes / 1024} KiB safety limit.");
                    continue;
                }

                var pack = JsonSerializer.Deserialize<ForeverStudioLibraryPack>(File.ReadAllText(file), JsonOptions);
                if (pack is null)
                {
                    warnings.Add($"{relative}: empty library pack was ignored.");
                    continue;
                }

                if (pack.ContractVersion != CurrentContractVersion)
                {
                    warnings.Add($"{relative}: unsupported library contractVersion {pack.ContractVersion}; expected {CurrentContractVersion}.");
                    continue;
                }

                pack.Items ??= [];
                if (!IsSafeId(pack.Id) || pack.Version < 1 || pack.Items.Count > MaxItemsPerPack)
                {
                    warnings.Add($"{relative}: invalid pack identity/version or too many items.");
                    continue;
                }

                foreach (var item in pack.Items)
                    catalog.Register(item, relative, warnings);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                warnings.Add($"{relative}: {ex.Message}");
            }
        }

        catalog.ValidateDependencies(warnings);
        return new ForeverStudioLibraryLoadResult(catalog, warnings);
    }

    public ForeverStudioLibraryItem? Find(string? itemId, int version)
    {
        var id = NormalizeId(itemId);
        return _items.TryGetValue(id, out var versions) && versions.TryGetValue(version, out var item)
            ? item
            : null;
    }

    public ForeverStudioLibraryItem? FindLatest(string? itemId)
    {
        var id = NormalizeId(itemId);
        if (!_items.TryGetValue(id, out var versions) || versions.Count == 0)
            return null;
        return versions.Values.Last();
    }

    public IReadOnlyList<ForeverStudioLibraryItem> Search(
        string? query,
        ForeverStudioLibraryItemKind? kind = null)
    {
        var terms = (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return AllItems
            .Where(item => kind is null || item.Kind == kind)
            .Where(item => terms.Length == 0 || terms.All(term =>
                item.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    public IReadOnlyDictionary<ForeverStudioLibraryItemKind, int> LatestItemCounts()
        => Enum.GetValues<ForeverStudioLibraryItemKind>()
            .ToDictionary(
                kind => kind,
                kind => _items.Values.Count(versions => versions.Count > 0 && versions.Values.Last().Kind == kind));

    public bool ArePortsCompatible(ForeverStudioLibraryPort output, ForeverStudioLibraryPort input)
    {
        if (output.Direction != ForeverStudioLibraryPortDirection.Output ||
            input.Direction != ForeverStudioLibraryPortDirection.Input)
            return false;

        return output.DataType.Equals("any", StringComparison.OrdinalIgnoreCase) ||
               input.DataType.Equals("any", StringComparison.OrdinalIgnoreCase) ||
               output.DataType.Equals(input.DataType, StringComparison.OrdinalIgnoreCase);
    }

    private void Register(ForeverStudioLibraryItem item, string source, List<string> warnings)
    {
        Normalize(item);
        if (!ValidateItem(item, out var issue))
        {
            warnings.Add($"{source}: {issue}");
            return;
        }

        if (!_items.TryGetValue(item.Id, out var versions))
        {
            versions = [];
            _items[item.Id] = versions;
        }

        if (versions.ContainsKey(item.Version))
        {
            warnings.Add($"{source}: duplicate library item '{item.Id}' v{item.Version} was ignored.");
            return;
        }
        versions[item.Version] = item;
    }

    private void ValidateDependencies(List<string> warnings)
    {
        foreach (var item in AllItems)
        {
            foreach (var dependency in item.Dependencies)
            {
                if (!_items.TryGetValue(dependency.ItemId, out var versions) ||
                    !versions.Keys.Any(version => version >= dependency.MinimumVersion))
                {
                    warnings.Add(
                        $"Library item '{item.Id}' v{item.Version} depends on '{dependency.ItemId}' v{dependency.MinimumVersion}+ which is unavailable.");
                }
            }
        }
    }

    private static bool ValidateItem(ForeverStudioLibraryItem item, out string issue)
    {
        if (!IsSafeId(item.Id) || item.Version < 1)
        {
            issue = "library items require a safe ID and version >= 1.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(item.DisplayName) || item.DisplayName.Length > 128)
        {
            issue = $"library item '{item.Id}' requires a display name up to 128 characters.";
            return false;
        }
        if (item.Ports.Count > MaxPortsPerItem || item.Dependencies.Count > MaxDependenciesPerItem)
        {
            issue = $"library item '{item.Id}' exceeds a collection safety limit.";
            return false;
        }

        var portIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var port in item.Ports)
        {
            if (!IsSafeId(port.Id) || !IsSafeType(port.DataType) || !portIds.Add(port.Id))
            {
                issue = $"library item '{item.Id}' has an invalid or duplicate port.";
                return false;
            }
        }

        foreach (var dependency in item.Dependencies)
        {
            if (!IsSafeId(dependency.ItemId) || dependency.MinimumVersion < 1)
            {
                issue = $"library item '{item.Id}' has an invalid dependency.";
                return false;
            }
        }

        issue = string.Empty;
        return true;
    }

    private static void Normalize(ForeverStudioLibraryItem item)
    {
        item.Id = NormalizeId(item.Id);
        item.Category = string.IsNullOrWhiteSpace(item.Category) ? "Uncategorized" : item.Category.Trim();
        item.DisplayName = item.DisplayName?.Trim() ?? string.Empty;
        item.Description = item.Description?.Trim() ?? string.Empty;
        item.GeneratorKey = NormalizeId(item.GeneratorKey);
        item.Tags = (item.Tags ?? []).Select(NormalizeId).Where(IsSafeId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        item.Capabilities = (item.Capabilities ?? []).Select(NormalizeId).Where(IsSafeId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        item.Ports ??= [];
        item.Dependencies ??= [];

        foreach (var port in item.Ports)
        {
            port.Id = NormalizeId(port.Id);
            port.DataType = NormalizeType(port.DataType);
            port.DisplayName = string.IsNullOrWhiteSpace(port.DisplayName) ? Humanize(port.Id) : port.DisplayName.Trim();
        }
        foreach (var dependency in item.Dependencies)
            dependency.ItemId = NormalizeId(dependency.ItemId);
    }

    private static IEnumerable<ForeverStudioLibraryItem> CreateCoreItems()
    {
        yield return new ForeverStudioLibraryItem
        {
            Id = "core.block-root",
            Version = 1,
            Kind = ForeverStudioLibraryItemKind.Asset,
            Category = "Core / Structure",
            DisplayName = "Block Root",
            Description = "Stable root component for a Machine Builder document.",
            GeneratorKey = "core.block-root",
            Tags = ["core", "block", "root"],
            Capabilities = ["visual-root"]
        };
        yield return new ForeverStudioLibraryItem
        {
            Id = "core.machine-entry",
            Version = 1,
            Kind = ForeverStudioLibraryItemKind.Function,
            Category = "Core / Flow",
            DisplayName = "Machine Entry",
            Description = "Typed behavior-graph entry point. Rich event nodes arrive with the Phase 2 library.",
            GeneratorKey = "core.machine-entry",
            Tags = ["core", "flow", "event"],
            Ports =
            [
                new ForeverStudioLibraryPort
                {
                    Id = "flow",
                    DisplayName = "Flow",
                    DataType = "flow",
                    Direction = ForeverStudioLibraryPortDirection.Output
                }
            ]
        };
        yield return new ForeverStudioLibraryItem
        {
            Id = "core.animation-binding",
            Version = 1,
            Kind = ForeverStudioLibraryItemKind.Animation,
            Category = "Core / Animation",
            DisplayName = "Animation Binding",
            Description = "Foundation contract between a semantic component and an animation definition.",
            GeneratorKey = "core.animation-binding",
            Tags = ["core", "animation", "binding"],
            Ports =
            [
                new ForeverStudioLibraryPort
                {
                    Id = "component",
                    DisplayName = "Component",
                    DataType = "component",
                    Direction = ForeverStudioLibraryPortDirection.Input,
                    Required = true
                }
            ]
        };
        yield return new ForeverStudioLibraryItem
        {
            Id = "core.blank-machine",
            Version = 1,
            Kind = ForeverStudioLibraryItemKind.Template,
            Category = "Core / Templates",
            DisplayName = "Blank Machine",
            Description = "Minimal safe machine project used by the Phase 1 Create New Machine flow.",
            GeneratorKey = "core.blank-machine",
            Tags = ["core", "template", "blank"],
            Dependencies =
            [
                new ForeverStudioLibraryDependency { ItemId = "core.block-root", MinimumVersion = 1 }
            ]
        };
    }

    internal static string NormalizeId(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    internal static bool IsSafeId(string value)
        => value.Length is > 0 and <= 128 &&
           value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');

    private static string NormalizeType(string? value)
        => (value ?? "any").Trim().ToLowerInvariant();

    private static bool IsSafeType(string value)
        => value.Length is > 0 and <= 64 &&
           value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');

    private static string Humanize(string value)
        => string.Join(' ', value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]));
}

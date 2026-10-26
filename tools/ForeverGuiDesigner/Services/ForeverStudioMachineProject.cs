using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Services;

public enum StudioValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record StudioValidationIssue(StudioValidationSeverity Severity, string Code, string Message)
{
    public override string ToString() => $"{Severity.ToString().ToUpperInvariant()} · {Code} · {Message}";
}

public sealed class ForeverStudioMachineDocument
{
    public int FormatVersion { get; set; } = ForeverStudioMachineProjectService.CurrentFormatVersion;
    public string AssetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ForeverStudioMachineLibraryReference> LibraryManifest { get; set; } = [];
    public List<ForeverStudioMachineComponent> Components { get; set; } = [];
    public List<ForeverStudioMachineState> States { get; set; } = [];
    public ForeverStudioMachineBehaviorGraph Behavior { get; set; } = new();
    public ForeverStudioMachineGuiDefinition Gui { get; set; } = new();
    public List<ForeverStudioMachineAnimationBinding> Animations { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverStudioMachineLibraryReference
{
    public string ItemId { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
}

public sealed class ForeverStudioMachineComponent
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LibraryItemId { get; set; } = string.Empty;
    public int LibraryItemVersion { get; set; } = 1;
    public string? ParentId { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForeverStudioMachineState
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForeverStudioMachineBehaviorGraph
{
    public List<ForeverStudioMachineBehaviorNode> Nodes { get; set; } = [];
    public List<ForeverStudioMachineBehaviorConnection> Connections { get; set; } = [];
}

public sealed class ForeverStudioMachineBehaviorNode
{
    public string Id { get; set; } = string.Empty;
    public string LibraryItemId { get; set; } = string.Empty;
    public int LibraryItemVersion { get; set; } = 1;
    public double X { get; set; }
    public double Y { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForeverStudioMachineBehaviorConnection
{
    public string Id { get; set; } = string.Empty;
    public string FromNodeId { get; set; } = string.Empty;
    public string FromPortId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string ToPortId { get; set; } = string.Empty;
}

public sealed class ForeverStudioMachineGuiDefinition
{
    public string? ScreenId { get; set; }
    public List<ForeverStudioMachineGuiBinding> Bindings { get; set; } = [];
}

public sealed class ForeverStudioMachineGuiBinding
{
    public string Id { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string DataType { get; set; } = "any";
}

public sealed class ForeverStudioMachineAnimationBinding
{
    public string Id { get; set; } = string.Empty;
    public string LibraryItemId { get; set; } = string.Empty;
    public int LibraryItemVersion { get; set; } = 1;
    public string TargetComponentId { get; set; } = string.Empty;
    public string? SourceNodeId { get; set; }
    public string? SourcePortId { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record ForeverStudioMachineProjectLoadResult(
    string RelativePath,
    ForeverStudioMachineDocument? Document,
    string SourceHash,
    IReadOnlyList<StudioValidationIssue> Issues);

public sealed record ForeverStudioMachineProjectScanResult(
    IReadOnlyList<ForeverStudioMachineProjectLoadResult> Projects,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Canonical, versioned Machine Builder source format. This document is intentionally the authoring
/// source of truth; later phases project it into previews and finally generated Java/assets instead
/// of teaching every editor to mutate game files independently.
/// </summary>
public static class ForeverStudioMachineProjectService
{
    public const int CurrentFormatVersion = 1;
    public const string RelativeProjectDirectory =
        "mod/src/main/resources/data/forever_skyblock/forever_studio/machines";
    public const string MissingHash = "<missing>";

    private const long MaxProjectBytes = 2 * 1024 * 1024;
    private const int MaxProjectFiles = 256;
    private const int MaxComponents = 512;
    private const int MaxStates = 256;
    private const int MaxBehaviorNodes = 1024;
    private const int MaxBehaviorConnections = 4096;
    private const int MaxGuiBindings = 1024;
    private const int MaxAnimationBindings = 1024;
    private const int MaxPropertyCount = 256;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ForeverStudioMachineDocument CreateBlank(string assetId, string displayName)
    {
        var safeAssetId = NormalizeId(assetId);
        return new ForeverStudioMachineDocument
        {
            AssetId = safeAssetId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Humanize(safeAssetId) : displayName.Trim(),
            Description = "Machine Builder project",
            Components =
            [
                new ForeverStudioMachineComponent
                {
                    Id = "root",
                    DisplayName = "Block Root",
                    LibraryItemId = "core.block-root",
                    LibraryItemVersion = 1
                }
            ],
            States =
            [
                new ForeverStudioMachineState { Id = "idle", DisplayName = "Idle" },
                new ForeverStudioMachineState { Id = "active", DisplayName = "Active" }
            ],
            Behavior = new ForeverStudioMachineBehaviorGraph
            {
                Nodes =
                [
                    new ForeverStudioMachineBehaviorNode
                    {
                        Id = "machine_entry",
                        LibraryItemId = "core.machine-entry",
                        LibraryItemVersion = 1,
                        X = 80,
                        Y = 80
                    }
                ]
            }
        };
    }

    public static string RelativePathForAsset(string assetId)
        => $"{RelativeProjectDirectory}/{NormalizeId(assetId)}.json";

    public static string Serialize(ForeverStudioMachineDocument document)
    {
        Normalize(document);
        RebuildLibraryManifest(document);
        return JsonSerializer.Serialize(document, JsonOptions) + "\n";
    }

    public static ForeverStudioMachineProjectLoadResult Load(
        string repoRoot,
        string relativePath,
        ForeverStudioLibraryCatalog catalog)
    {
        var issues = new List<StudioValidationIssue>();
        if (!TryResolveInside(repoRoot, relativePath, out var fullPath))
        {
            issues.Add(Error("path", "Project path escapes the repository or is invalid."));
            return new ForeverStudioMachineProjectLoadResult(relativePath, null, MissingHash, issues);
        }

        if (!File.Exists(fullPath))
        {
            issues.Add(Error("missing", "Machine project file does not exist."));
            return new ForeverStudioMachineProjectLoadResult(relativePath, null, MissingHash, issues);
        }

        try
        {
            if (new FileInfo(fullPath).Length > MaxProjectBytes)
            {
                issues.Add(Error("size", $"Machine project exceeds the {MaxProjectBytes / 1024} KiB safety limit."));
                return new ForeverStudioMachineProjectLoadResult(relativePath, null, MissingHash, issues);
            }

            var text = File.ReadAllText(fullPath);
            var document = JsonSerializer.Deserialize<ForeverStudioMachineDocument>(text, JsonOptions);
            if (document is null)
            {
                issues.Add(Error("json", "Machine project is empty."));
                return new ForeverStudioMachineProjectLoadResult(relativePath, null, Hash(text), issues);
            }
            Normalize(document);
            issues.AddRange(Validate(document, catalog));
            return new ForeverStudioMachineProjectLoadResult(relativePath, document, Hash(text), issues);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            issues.Add(Error("load", ex.Message));
            return new ForeverStudioMachineProjectLoadResult(relativePath, null, MissingHash, issues);
        }
    }

    public static ForeverStudioMachineProjectScanResult Scan(string repoRoot, ForeverStudioLibraryCatalog catalog)
    {
        var warnings = new List<string>();
        if (!TryResolveInside(repoRoot, RelativeProjectDirectory, out var directory))
            return new ForeverStudioMachineProjectScanResult([], ["Machine project directory is invalid."]);
        if (!Directory.Exists(directory))
            return new ForeverStudioMachineProjectScanResult([], []);

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(MaxProjectFiles + 1)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ForeverStudioMachineProjectScanResult([], [$"Machine project scan failed: {ex.Message}"]);
        }
        if (files.Count > MaxProjectFiles)
        {
            warnings.Add($"Machine project directory contains more than {MaxProjectFiles} files; only the first {MaxProjectFiles} are scanned.");
            files = files.Take(MaxProjectFiles).ToList();
        }

        var projects = files
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Select(relative => Load(repoRoot, relative, catalog))
            .ToList();
        return new ForeverStudioMachineProjectScanResult(projects, warnings);
    }

    public static IReadOnlyList<StudioValidationIssue> Validate(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        var issues = new List<StudioValidationIssue>();
        Normalize(document);

        if (document.FormatVersion != CurrentFormatVersion)
            issues.Add(Error("format", $"Unsupported formatVersion {document.FormatVersion}; expected {CurrentFormatVersion}."));
        if (!IsSafeId(document.AssetId))
            issues.Add(Error("asset-id", "assetId must use lowercase letters, digits, '_', '-' or '.' and be at most 128 characters."));
        if (string.IsNullOrWhiteSpace(document.DisplayName) || document.DisplayName.Length > 128)
            issues.Add(Error("display-name", "displayName is required and must be at most 128 characters."));
        if (document.Description.Length > 2048)
            issues.Add(Error("description", "description must be at most 2048 characters."));
        if (document.Components.Count > MaxComponents || document.States.Count > MaxStates ||
            document.Behavior.Nodes.Count > MaxBehaviorNodes || document.Behavior.Connections.Count > MaxBehaviorConnections ||
            document.Gui.Bindings.Count > MaxGuiBindings || document.Animations.Count > MaxAnimationBindings)
        {
            issues.Add(Error("limits", "Machine project exceeds a collection safety limit."));
        }

        ValidateComponents(document, catalog, issues);
        ValidateStates(document, issues);
        ValidateBehavior(document, catalog, issues);
        ValidateGui(document, issues);
        ValidateAnimations(document, catalog, issues);
        ValidateManifest(document, catalog, issues);
        return issues;
    }

    private static void ValidateComponents(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in document.Components)
        {
            if (!IsSafeId(component.Id) || !ids.Add(component.Id))
                issues.Add(Error("component-id", $"Component '{component.Id}' has an invalid or duplicate ID."));
            if (component.Properties.Count > MaxPropertyCount)
                issues.Add(Error("component-properties", $"Component '{component.Id}' has too many properties."));
            if (!string.IsNullOrWhiteSpace(component.ParentId) && !document.Components.Any(candidate =>
                    candidate.Id.Equals(component.ParentId, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(Error("component-parent", $"Component '{component.Id}' references missing parent '{component.ParentId}'."));
            }
            var item = catalog.Find(component.LibraryItemId, component.LibraryItemVersion);
            if (item is null || item.Kind != ForeverStudioLibraryItemKind.Asset)
                issues.Add(Error("component-library", $"Component '{component.Id}' references unavailable Asset '{component.LibraryItemId}' v{component.LibraryItemVersion}."));
        }
    }

    private static void ValidateStates(ForeverStudioMachineDocument document, List<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in document.States)
        {
            if (!IsSafeId(state.Id) || !ids.Add(state.Id))
                issues.Add(Error("state-id", $"State '{state.Id}' has an invalid or duplicate ID."));
            if (state.Properties.Count > MaxPropertyCount)
                issues.Add(Error("state-properties", $"State '{state.Id}' has too many properties."));
        }
        if (!document.States.Any(state => state.Id.Equals("idle", StringComparison.OrdinalIgnoreCase)))
            issues.Add(Warning("state-idle", "No 'idle' state is declared; production generators will require an explicit default state."));
    }

    private static void ValidateBehavior(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodeItems = new Dictionary<string, ForeverStudioLibraryItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in document.Behavior.Nodes)
        {
            if (!IsSafeId(node.Id) || !nodeIds.Add(node.Id))
                issues.Add(Error("behavior-node-id", $"Behavior node '{node.Id}' has an invalid or duplicate ID."));
            if (!double.IsFinite(node.X) || !double.IsFinite(node.Y))
                issues.Add(Error("behavior-node-position", $"Behavior node '{node.Id}' has a non-finite canvas position."));
            if (node.Properties.Count > MaxPropertyCount)
                issues.Add(Error("behavior-node-properties", $"Behavior node '{node.Id}' has too many properties."));
            var item = catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is null || item.Kind != ForeverStudioLibraryItemKind.Function)
            {
                issues.Add(Error("behavior-node-library", $"Behavior node '{node.Id}' references unavailable Function '{node.LibraryItemId}' v{node.LibraryItemVersion}."));
                continue;
            }
            nodeItems[node.Id] = item;
        }

        var connectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var semanticConnections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var incomingCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var connection in document.Behavior.Connections)
        {
            if (!IsSafeId(connection.Id) || !connectionIds.Add(connection.Id))
                issues.Add(Error("behavior-connection-id", $"Connection '{connection.Id}' has an invalid or duplicate ID."));

            var semanticKey = $"{connection.FromNodeId}\n{connection.FromPortId}\n{connection.ToNodeId}\n{connection.ToPortId}";
            if (!semanticConnections.Add(semanticKey))
                issues.Add(Error("behavior-connection-duplicate", $"Connection '{connection.Id}' duplicates an existing graph edge."));

            var fromNode = document.Behavior.Nodes.FirstOrDefault(node =>
                node.Id.Equals(connection.FromNodeId, StringComparison.OrdinalIgnoreCase));
            var toNode = document.Behavior.Nodes.FirstOrDefault(node =>
                node.Id.Equals(connection.ToNodeId, StringComparison.OrdinalIgnoreCase));
            if (fromNode is null || toNode is null)
            {
                issues.Add(Error("behavior-connection-node", $"Connection '{connection.Id}' references a missing node."));
                continue;
            }
            if (!nodeItems.TryGetValue(fromNode.Id, out var fromItem) || !nodeItems.TryGetValue(toNode.Id, out var toItem))
                continue;

            var output = fromItem.Ports.FirstOrDefault(port =>
                port.Id.Equals(connection.FromPortId, StringComparison.OrdinalIgnoreCase) &&
                port.Direction == ForeverStudioLibraryPortDirection.Output);
            var input = toItem.Ports.FirstOrDefault(port =>
                port.Id.Equals(connection.ToPortId, StringComparison.OrdinalIgnoreCase) &&
                port.Direction == ForeverStudioLibraryPortDirection.Input);
            if (output is null || input is null || !catalog.ArePortsCompatible(output, input))
            {
                issues.Add(Error("behavior-connection-port", $"Connection '{connection.Id}' has missing or incompatible typed ports."));
                continue;
            }

            var inputKey = $"{toNode.Id}\n{input.Id}";
            incomingCounts[inputKey] = incomingCounts.GetValueOrDefault(inputKey) + 1;
            if (!input.Multiple && incomingCounts[inputKey] > 1)
            {
                issues.Add(Error(
                    "behavior-input-cardinality",
                    $"Input '{toNode.Id}.{input.Id}' accepts one connection but has {incomingCounts[inputKey]}."));
            }
        }

        foreach (var node in document.Behavior.Nodes)
        {
            if (!nodeItems.TryGetValue(node.Id, out var item))
                continue;
            foreach (var input in item.Ports.Where(port =>
                         port.Direction == ForeverStudioLibraryPortDirection.Input && port.Required))
            {
                var key = $"{node.Id}\n{input.Id}";
                if (incomingCounts.GetValueOrDefault(key) == 0)
                {
                    issues.Add(Warning(
                        "behavior-required-input",
                        $"Required input '{node.Id}.{input.Id}' is not connected yet."));
                }
            }
        }
    }

    private static void ValidateGui(ForeverStudioMachineDocument document, List<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in document.Gui.Bindings)
        {
            if (!IsSafeId(binding.Id) || !ids.Add(binding.Id))
                issues.Add(Error("gui-binding-id", $"GUI binding '{binding.Id}' has an invalid or duplicate ID."));
            if (string.IsNullOrWhiteSpace(binding.SourceId) || string.IsNullOrWhiteSpace(binding.TargetId))
                issues.Add(Error("gui-binding-target", $"GUI binding '{binding.Id}' requires sourceId and targetId."));
        }
    }

    private static void ValidateAnimations(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in document.Animations)
        {
            if (!IsSafeId(binding.Id) || !ids.Add(binding.Id))
                issues.Add(Error("animation-binding-id", $"Animation binding '{binding.Id}' has an invalid or duplicate ID."));
            if (binding.Properties.Count > MaxPropertyCount)
                issues.Add(Error("animation-properties", $"Animation binding '{binding.Id}' has too many properties."));
            var item = catalog.Find(binding.LibraryItemId, binding.LibraryItemVersion);
            if (item is null || item.Kind != ForeverStudioLibraryItemKind.Animation)
                issues.Add(Error("animation-library", $"Animation binding '{binding.Id}' references unavailable Animation '{binding.LibraryItemId}' v{binding.LibraryItemVersion}."));
            if (!document.Components.Any(component => component.Id.Equals(binding.TargetComponentId, StringComparison.OrdinalIgnoreCase)))
                issues.Add(Error("animation-target", $"Animation binding '{binding.Id}' references missing component '{binding.TargetComponentId}'."));
        }
    }

    private static void ValidateManifest(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var expected = ReferencedLibraryItems(document)
            .Distinct()
            .OrderBy(reference => reference.ItemId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reference => reference.Version)
            .ToList();
        var actual = document.LibraryManifest
            .DistinctBy(reference => (reference.ItemId.ToLowerInvariant(), reference.Version))
            .OrderBy(reference => reference.ItemId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reference => reference.Version)
            .ToList();

        foreach (var reference in expected)
        {
            if (catalog.Find(reference.ItemId, reference.Version) is null)
                issues.Add(Error("manifest-library", $"Referenced library item '{reference.ItemId}' v{reference.Version} is unavailable."));
        }

        if (!SameReferences(expected, actual))
            issues.Add(Info("manifest-refresh", "libraryManifest will be refreshed from canonical project references on save."));
    }

    private static bool SameReferences(
        IReadOnlyList<ForeverStudioMachineLibraryReference> left,
        IReadOnlyList<ForeverStudioMachineLibraryReference> right)
    {
        if (left.Count != right.Count)
            return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (!left[i].ItemId.Equals(right[i].ItemId, StringComparison.OrdinalIgnoreCase) ||
                left[i].Version != right[i].Version)
                return false;
        }
        return true;
    }

    private static void RebuildLibraryManifest(ForeverStudioMachineDocument document)
    {
        document.LibraryManifest = ReferencedLibraryItems(document)
            .DistinctBy(reference => (reference.ItemId.ToLowerInvariant(), reference.Version))
            .OrderBy(reference => reference.ItemId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reference => reference.Version)
            .ToList();
    }

    private static IEnumerable<ForeverStudioMachineLibraryReference> ReferencedLibraryItems(
        ForeverStudioMachineDocument document)
    {
        foreach (var component in document.Components)
            yield return new ForeverStudioMachineLibraryReference { ItemId = component.LibraryItemId, Version = component.LibraryItemVersion };
        foreach (var node in document.Behavior.Nodes)
            yield return new ForeverStudioMachineLibraryReference { ItemId = node.LibraryItemId, Version = node.LibraryItemVersion };
        foreach (var animation in document.Animations)
            yield return new ForeverStudioMachineLibraryReference { ItemId = animation.LibraryItemId, Version = animation.LibraryItemVersion };
    }

    private static void Normalize(ForeverStudioMachineDocument document)
    {
        document.AssetId = NormalizeId(document.AssetId);
        document.DisplayName = document.DisplayName?.Trim() ?? string.Empty;
        document.Description = document.Description?.Trim() ?? string.Empty;
        document.LibraryManifest ??= [];
        document.Components ??= [];
        document.States ??= [];
        document.Behavior ??= new ForeverStudioMachineBehaviorGraph();
        document.Gui ??= new ForeverStudioMachineGuiDefinition();
        document.Animations ??= [];
        document.Behavior.Nodes ??= [];
        document.Behavior.Connections ??= [];
        document.Gui.Bindings ??= [];

        foreach (var reference in document.LibraryManifest)
            reference.ItemId = NormalizeId(reference.ItemId);
        foreach (var component in document.Components)
        {
            component.Id = NormalizeId(component.Id);
            component.DisplayName = component.DisplayName?.Trim() ?? string.Empty;
            component.LibraryItemId = NormalizeId(component.LibraryItemId);
            component.ParentId = string.IsNullOrWhiteSpace(component.ParentId) ? null : NormalizeId(component.ParentId);
            component.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var state in document.States)
        {
            state.Id = NormalizeId(state.Id);
            state.DisplayName = state.DisplayName?.Trim() ?? string.Empty;
            state.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var node in document.Behavior.Nodes)
        {
            node.Id = NormalizeId(node.Id);
            node.LibraryItemId = NormalizeId(node.LibraryItemId);
            node.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var connection in document.Behavior.Connections)
        {
            connection.Id = NormalizeId(connection.Id);
            connection.FromNodeId = NormalizeId(connection.FromNodeId);
            connection.FromPortId = NormalizeId(connection.FromPortId);
            connection.ToNodeId = NormalizeId(connection.ToNodeId);
            connection.ToPortId = NormalizeId(connection.ToPortId);
        }
        foreach (var binding in document.Gui.Bindings)
        {
            binding.Id = NormalizeId(binding.Id);
            binding.SourceId = NormalizeId(binding.SourceId);
            binding.TargetId = NormalizeId(binding.TargetId);
            binding.DataType = string.IsNullOrWhiteSpace(binding.DataType) ? "any" : binding.DataType.Trim().ToLowerInvariant();
        }
        foreach (var animation in document.Animations)
        {
            animation.Id = NormalizeId(animation.Id);
            animation.LibraryItemId = NormalizeId(animation.LibraryItemId);
            animation.TargetComponentId = NormalizeId(animation.TargetComponentId);
            animation.SourceNodeId = string.IsNullOrWhiteSpace(animation.SourceNodeId) ? null : NormalizeId(animation.SourceNodeId);
            animation.SourcePortId = string.IsNullOrWhiteSpace(animation.SourcePortId) ? null : NormalizeId(animation.SourcePortId);
            animation.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool TryResolveInside(string repoRoot, string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        try
        {
            var root = Path.GetFullPath(repoRoot.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            fullPath = string.Empty;
            return false;
        }
    }

    public static string ComputeHash(string? text)
        => Hash(text ?? string.Empty);

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal static string NormalizeId(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    internal static bool IsSafeId(string value)
        => value.Length is > 0 and <= 128 &&
           value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');

    private static StudioValidationIssue Error(string code, string message)
        => new(StudioValidationSeverity.Error, code, message);

    private static StudioValidationIssue Warning(string code, string message)
        => new(StudioValidationSeverity.Warning, code, message);

    private static StudioValidationIssue Info(string code, string message)
        => new(StudioValidationSeverity.Info, code, message);

    private static string Humanize(string value)
        => string.Join(' ', value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]));
}

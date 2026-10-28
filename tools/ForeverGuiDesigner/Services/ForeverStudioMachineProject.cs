using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Services;

public sealed class ForeverStudioMachineDocument
{
    public int FormatVersion { get; set; } = ForeverStudioMachineProjectService.CurrentFormatVersion;
    public string AssetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Namespace { get; set; } = "forever_skyblock";
    public string MachineType { get; set; } = "custom";
    public List<ForeverStudioMachineComponent> Components { get; set; } = [];
    public List<ForeverStudioMachineState> States { get; set; } = [];
    public ForeverStudioMachineBehaviorGraph Behavior { get; set; } = new();
    public List<ForeverStudioMachineRecipeBinding> Recipes { get; set; } = [];
    public ForeverStudioMachineGuiDefinition Gui { get; set; } = new();
    public List<ForeverStudioMachineAnimationBinding> Animations { get; set; } = [];
    public ForeverStudioMachineLibraryManifest Library { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
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
    public bool IsInitial { get; set; }
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

public sealed class ForeverStudioMachineRecipeBinding
{
    public string Id { get; set; } = string.Empty;
    public string RecipeTypeId { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForeverStudioMachineGuiDefinition
{
    public string ScreenId { get; set; } = string.Empty;
    public List<ForeverStudioMachineGuiBinding> Bindings { get; set; } = [];
}

public sealed class ForeverStudioMachineGuiBinding
{
    public string Id { get; set; } = string.Empty;
    public string LibraryItemId { get; set; } = string.Empty;
    public int LibraryItemVersion { get; set; } = 1;
    public string SourceId { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForeverStudioMachineAnimationBinding
{
    public string Id { get; set; } = string.Empty;
    public string LibraryItemId { get; set; } = string.Empty;
    public int LibraryItemVersion { get; set; } = 1;
    public string TargetComponentId { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForeverStudioMachineLibraryManifest
{
    public int ContractVersion { get; set; } = ForeverStudioLibraryCatalog.CurrentContractVersion;
    public List<ForeverStudioMachineLibraryReference> References { get; set; } = [];
}

public sealed class ForeverStudioMachineLibraryReference
{
    public string ItemId { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
}

public sealed record ForeverStudioMachineProjectLoadResult(
    string RelativePath,
    ForeverStudioMachineDocument? Document,
    string SourceHash,
    IReadOnlyList<StudioValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);
}

public sealed record ForeverStudioMachineProjectScanResult(
    IReadOnlyList<ForeverStudioMachineProjectLoadResult> Projects,
    IReadOnlyList<string> Warnings);

public sealed record ForeverStudioMachineProjectSaveResult(
    string RelativePath,
    string FullPath,
    string SourceHash,
    string Json,
    IReadOnlyList<StudioValidationIssue> Issues);

/// <summary>
/// Canonical Phase-1 machine-project persistence. The project document is intentionally independent
/// from generated Java/resource files: later phases can compile this stable authoring model into the
/// mod while keeping the user's source machine definition versioned and reviewable.
/// </summary>
public static class ForeverStudioMachineProjectService
{
    public const int CurrentFormatVersion = 1;
    public const string RelativeDirectory =
        "mod/src/main/resources/data/forever_skyblock/forever_studio/machines";
    public const string MissingHash = "<missing>";

    private const long MaxProjectBytes = 1024 * 1024;
    private const int MaxProjectFiles = 512;
    private const int MaxComponents = 512;
    private const int MaxStates = 128;
    private const int MaxBehaviorNodes = 1024;
    private const int MaxBehaviorConnections = 4096;
    private const int MaxRecipes = 256;
    private const int MaxGuiBindings = 512;
    private const int MaxAnimationBindings = 512;
    private const int MaxLibraryReferences = 2048;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64
    };

    public static ForeverStudioMachineDocument CreateBlank(string assetId, string displayName)
    {
        var safeId = SanitizeAssetId(assetId);
        var name = string.IsNullOrWhiteSpace(displayName) ? Humanize(safeId) : displayName.Trim();
        var document = new ForeverStudioMachineDocument
        {
            AssetId = safeId,
            DisplayName = name,
            Description = "",
            MachineType = "custom",
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
                new ForeverStudioMachineState
                {
                    Id = "idle",
                    DisplayName = "Idle",
                    IsInitial = true
                }
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
        Normalize(document);
        RebuildLibraryManifest(document);
        return document;
    }

    public static string RelativePathForAsset(string assetId)
        => $"{RelativeDirectory}/{SanitizeAssetId(assetId)}.json";

    public static string Serialize(ForeverStudioMachineDocument document)
    {
        Normalize(document);
        RebuildLibraryManifest(document);
        return JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
    }

    public static ForeverStudioMachineProjectScanResult Scan(
        string repoRoot,
        ForeverStudioLibraryCatalog catalog)
    {
        var projects = new List<ForeverStudioMachineProjectLoadResult>();
        var warnings = new List<string>();
        var root = ResolveRepoPath(repoRoot, RelativeDirectory);
        if (!Directory.Exists(root))
            return new ForeverStudioMachineProjectScanResult(projects, warnings);

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(root, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(MaxProjectFiles + 1)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warnings.Add($"Machine project directory cannot be read: {ex.Message}");
            return new ForeverStudioMachineProjectScanResult(projects, warnings);
        }

        if (files.Count > MaxProjectFiles)
        {
            warnings.Add($"Machine project directory contains more than {MaxProjectFiles} files. Only the first {MaxProjectFiles} are indexed.");
            files = files.Take(MaxProjectFiles).ToList();
        }

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            try
            {
                if (new FileInfo(file).Length > MaxProjectBytes)
                {
                    projects.Add(new ForeverStudioMachineProjectLoadResult(
                        relative,
                        null,
                        HashFile(file),
                        [new StudioValidationIssue(
                            StudioValidationSeverity.Error,
                            $"Machine project exceeds the {MaxProjectBytes / 1024} KiB safety limit.")]));
                    continue;
                }

                projects.Add(LoadFile(repoRoot, file, catalog));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"{relative}: {ex.Message}");
            }
        }

        return new ForeverStudioMachineProjectScanResult(projects, warnings);
    }

    public static ForeverStudioMachineProjectLoadResult Load(
        string repoRoot,
        string assetId,
        ForeverStudioLibraryCatalog catalog)
    {
        var fullPath = ResolveRepoPath(repoRoot, RelativePathForAsset(assetId));
        if (!File.Exists(fullPath))
        {
            return new ForeverStudioMachineProjectLoadResult(
                RelativePathForAsset(assetId),
                null,
                MissingHash,
                [new StudioValidationIssue(StudioValidationSeverity.Info, "Machine project does not exist yet.")]);
        }
        return LoadFile(repoRoot, fullPath, catalog);
    }

    public static ForeverStudioMachineProjectSaveResult Save(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        string expectedSourceHash)
    {
        Normalize(document);
        RebuildLibraryManifest(document);
        var issues = Validate(document, catalog);
        if (issues.Any(issue => issue.Severity == StudioValidationSeverity.Error))
            throw new InvalidOperationException("Machine project cannot be saved while validation errors remain.");

        var json = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
        if (Encoding.UTF8.GetByteCount(json) > MaxProjectBytes)
            throw new InvalidOperationException($"Machine project exceeds the {MaxProjectBytes / 1024} KiB safety limit.");

        var relative = RelativePathForAsset(document.AssetId);
        var fullPath = ResolveRepoPath(repoRoot, relative);
        var currentHash = File.Exists(fullPath) ? HashFile(fullPath) : MissingHash;
        if (!string.Equals(currentHash, expectedSourceHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException(
                $"'{relative}' changed on disk after it was loaded. Reopen the project before saving to avoid overwriting external changes.");
        }

        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new IOException("Machine project target directory is unavailable.");
        Directory.CreateDirectory(directory);

        var token = Guid.NewGuid().ToString("N");
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.foreverstudio-{token}.tmp");
        var backupPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.foreverstudio-{token}.bak");
        var existed = File.Exists(fullPath);

        try
        {
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (existed)
                File.Copy(fullPath, backupPath, overwrite: true);

            File.Move(tempPath, fullPath, overwrite: true);
            var writtenHash = HashFile(fullPath);
            if (writtenHash == MissingHash)
                throw new IOException("Machine project was not present after the atomic move.");

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            return new ForeverStudioMachineProjectSaveResult(relative, fullPath, writtenHash, json, issues);
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, fullPath, overwrite: true);
                    File.Delete(backupPath);
                }
                else if (!existed && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Preserve the original exception. A leftover .bak/.tmp file is intentionally visible
                // for manual recovery instead of hiding a failed restore behind another exception.
            }
            throw;
        }
    }

    public static IReadOnlyList<StudioValidationIssue> Validate(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        var issues = new List<StudioValidationIssue>();
        if (document.FormatVersion != CurrentFormatVersion)
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Unsupported machine formatVersion {document.FormatVersion}; this Studio supports {CurrentFormatVersion}."));
            return issues;
        }

        if (!IsSafeAssetId(document.AssetId))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Asset ID must use lowercase a-z, 0-9, '_', '-' or '.'."));
        if (!IsSafeNamespace(document.Namespace))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Namespace must use lowercase resource-location characters."));
        if (string.IsNullOrWhiteSpace(document.DisplayName) || document.DisplayName.Length > 128)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Display name is required and may contain at most 128 characters."));
        if (document.Description.Length > 4096)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Description may contain at most 4096 characters."));

        if (document.Components.Count is < 1 or > MaxComponents)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"A machine needs 1..{MaxComponents} components."));
        if (document.States.Count is < 1 or > MaxStates)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"A machine needs 1..{MaxStates} states."));
        if (document.Behavior.Nodes.Count > MaxBehaviorNodes || document.Behavior.Connections.Count > MaxBehaviorConnections ||
            document.Recipes.Count > MaxRecipes || document.Gui.Bindings.Count > MaxGuiBindings ||
            document.Animations.Count > MaxAnimationBindings || document.Library.References.Count > MaxLibraryReferences)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Machine project exceeds a collection safety limit."));
        }

        ValidateComponents(document, catalog, issues);
        ValidateStates(document, issues);
        ValidateBehavior(document, catalog, issues);
        ValidateBindings(document, catalog, issues);
        ValidateLibraryManifest(document, catalog, issues);

        if (!issues.Any(issue => issue.Severity == StudioValidationSeverity.Error))
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Info,
                "Machine project structure, identities, graph references and library versions are valid."));
        }
        return issues;
    }

    public static string SanitizeAssetId(string? value)
    {
        var source = (value ?? string.Empty).Trim().ToLowerInvariant();
        var builder = new StringBuilder(source.Length);
        var previousUnderscore = false;
        foreach (var ch in source)
        {
            var normalized = char.IsLetterOrDigit(ch) ? ch : ch is '-' or '.' ? ch : '_';
            if (normalized == '_' && previousUnderscore)
                continue;
            builder.Append(normalized);
            previousUnderscore = normalized == '_';
        }
        var result = builder.ToString().Trim('_', '-', '.');
        return string.IsNullOrWhiteSpace(result) ? "new_machine" : result;
    }

    private static ForeverStudioMachineProjectLoadResult LoadFile(
        string repoRoot,
        string fullPath,
        ForeverStudioLibraryCatalog catalog)
    {
        var relative = Path.GetRelativePath(repoRoot, fullPath).Replace('\\', '/');
        var hash = HashFile(fullPath);
        ForeverStudioMachineDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ForeverStudioMachineDocument>(File.ReadAllText(fullPath), JsonOptions);
        }
        catch (JsonException ex)
        {
            return new ForeverStudioMachineProjectLoadResult(
                relative,
                null,
                hash,
                [new StudioValidationIssue(StudioValidationSeverity.Error, $"Machine JSON is invalid: {ex.Message}")]);
        }

        if (document is null)
        {
            return new ForeverStudioMachineProjectLoadResult(
                relative,
                null,
                hash,
                [new StudioValidationIssue(StudioValidationSeverity.Error, "Machine JSON is empty.")]);
        }

        Normalize(document);
        var issues = Validate(document, catalog);
        return new ForeverStudioMachineProjectLoadResult(relative, document, hash, issues);
    }

    private static void Normalize(ForeverStudioMachineDocument document)
    {
        document.AssetId = (document.AssetId ?? string.Empty).Trim().ToLowerInvariant();
        document.DisplayName = document.DisplayName?.Trim() ?? string.Empty;
        document.Description = document.Description?.Trim() ?? string.Empty;
        document.Namespace = string.IsNullOrWhiteSpace(document.Namespace)
            ? "forever_skyblock"
            : document.Namespace.Trim().ToLowerInvariant();
        document.MachineType = string.IsNullOrWhiteSpace(document.MachineType)
            ? "custom"
            : document.MachineType.Trim().ToLowerInvariant();
        document.Components ??= [];
        document.States ??= [];
        document.Behavior ??= new ForeverStudioMachineBehaviorGraph();
        document.Behavior.Nodes ??= [];
        document.Behavior.Connections ??= [];
        document.Recipes ??= [];
        document.Gui ??= new ForeverStudioMachineGuiDefinition();
        document.Gui.Bindings ??= [];
        document.Animations ??= [];
        document.Library ??= new ForeverStudioMachineLibraryManifest();
        document.Library.ContractVersion = ForeverStudioLibraryCatalog.CurrentContractVersion;
        document.Library.References ??= [];

        foreach (var component in document.Components)
        {
            component.Id = NormalizeLocalId(component.Id);
            component.DisplayName = string.IsNullOrWhiteSpace(component.DisplayName) ? Humanize(component.Id) : component.DisplayName.Trim();
            component.LibraryItemId = ForeverStudioLibraryCatalog.NormalizeId(component.LibraryItemId);
            component.ParentId = string.IsNullOrWhiteSpace(component.ParentId) ? null : NormalizeLocalId(component.ParentId);
            component.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var state in document.States)
        {
            state.Id = NormalizeLocalId(state.Id);
            state.DisplayName = string.IsNullOrWhiteSpace(state.DisplayName) ? Humanize(state.Id) : state.DisplayName.Trim();
            state.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var node in document.Behavior.Nodes)
        {
            node.Id = NormalizeLocalId(node.Id);
            node.LibraryItemId = ForeverStudioLibraryCatalog.NormalizeId(node.LibraryItemId);
            node.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var connection in document.Behavior.Connections)
        {
            connection.Id = NormalizeLocalId(connection.Id);
            connection.FromNodeId = NormalizeLocalId(connection.FromNodeId);
            connection.FromPortId = NormalizeLocalId(connection.FromPortId);
            connection.ToNodeId = NormalizeLocalId(connection.ToNodeId);
            connection.ToPortId = NormalizeLocalId(connection.ToPortId);
        }
        foreach (var recipe in document.Recipes)
        {
            recipe.Id = NormalizeLocalId(recipe.Id);
            recipe.RecipeTypeId = recipe.RecipeTypeId?.Trim().ToLowerInvariant() ?? string.Empty;
            recipe.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        document.Gui.ScreenId = NormalizeLocalId(document.Gui.ScreenId);
        foreach (var binding in document.Gui.Bindings)
        {
            binding.Id = NormalizeLocalId(binding.Id);
            binding.LibraryItemId = ForeverStudioLibraryCatalog.NormalizeId(binding.LibraryItemId);
            binding.SourceId = NormalizeLocalId(binding.SourceId);
            binding.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var animation in document.Animations)
        {
            animation.Id = NormalizeLocalId(animation.Id);
            animation.LibraryItemId = ForeverStudioLibraryCatalog.NormalizeId(animation.LibraryItemId);
            animation.TargetComponentId = NormalizeLocalId(animation.TargetComponentId);
            animation.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void RebuildLibraryManifest(ForeverStudioMachineDocument document)
    {
        var references = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void Add(string id, int version)
        {
            if (string.IsNullOrWhiteSpace(id) || version < 1)
                return;
            if (!references.TryGetValue(id, out var existing) || version > existing)
                references[id] = version;
        }

        foreach (var component in document.Components)
            Add(component.LibraryItemId, component.LibraryItemVersion);
        foreach (var node in document.Behavior.Nodes)
            Add(node.LibraryItemId, node.LibraryItemVersion);
        foreach (var binding in document.Gui.Bindings)
            Add(binding.LibraryItemId, binding.LibraryItemVersion);
        foreach (var animation in document.Animations)
            Add(animation.LibraryItemId, animation.LibraryItemVersion);

        document.Library.ContractVersion = ForeverStudioLibraryCatalog.CurrentContractVersion;
        document.Library.References = references
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ForeverStudioMachineLibraryReference { ItemId = pair.Key, Version = pair.Value })
            .ToList();
    }

    private static void ValidateComponents(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in document.Components)
        {
            if (!IsSafeLocalId(component.Id) || !ids.Add(component.Id))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Component IDs must be unique safe IDs."));
            if (catalog.Find(component.LibraryItemId, component.LibraryItemVersion) is not { Kind: ForeverStudioLibraryItemKind.Asset })
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Component '{component.Id}' references unavailable asset '{component.LibraryItemId}' v{component.LibraryItemVersion}."));
        }

        foreach (var component in document.Components)
        {
            if (component.ParentId is not null && !ids.Contains(component.ParentId))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Component '{component.Id}' references missing parent '{component.ParentId}'."));
            if (component.ParentId is not null && component.ParentId.Equals(component.Id, StringComparison.OrdinalIgnoreCase))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Component '{component.Id}' cannot parent itself."));
        }

        foreach (var component in document.Components)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = component;
            while (current.ParentId is { } parentId)
            {
                if (!visited.Add(current.Id))
                {
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Component hierarchy contains a cycle at '{component.Id}'."));
                    break;
                }
                current = document.Components.FirstOrDefault(candidate => candidate.Id.Equals(parentId, StringComparison.OrdinalIgnoreCase))!;
                if (current is null)
                    break;
            }
        }
    }

    private static void ValidateStates(ForeverStudioMachineDocument document, List<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in document.States)
        {
            if (!IsSafeLocalId(state.Id) || !ids.Add(state.Id))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "State IDs must be unique safe IDs."));
        }

        var initialCount = document.States.Count(state => state.IsInitial);
        if (initialCount != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Exactly one machine state must be marked as the initial state."));
    }

    private static void ValidateBehavior(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var nodes = new Dictionary<string, ForeverStudioMachineBehaviorNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in document.Behavior.Nodes)
        {
            if (!IsSafeLocalId(node.Id) || !nodes.TryAdd(node.Id, node))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Behavior node IDs must be unique safe IDs."));
                continue;
            }
            if (!double.IsFinite(node.X) || !double.IsFinite(node.Y) || Math.Abs(node.X) > 100000 || Math.Abs(node.Y) > 100000)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Behavior node '{node.Id}' has invalid canvas coordinates."));
            if (catalog.Find(node.LibraryItemId, node.LibraryItemVersion) is not { Kind: ForeverStudioLibraryItemKind.Function })
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Behavior node '{node.Id}' references unavailable function '{node.LibraryItemId}' v{node.LibraryItemVersion}."));
        }

        var connectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var semanticConnections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var incomingCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var connection in document.Behavior.Connections)
        {
            if (!IsSafeLocalId(connection.Id) || !connectionIds.Add(connection.Id))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Behavior connection IDs must be unique safe IDs."));
                continue;
            }

            var semanticKey = $"{connection.FromNodeId}\n{connection.FromPortId}\n{connection.ToNodeId}\n{connection.ToPortId}";
            if (!semanticConnections.Add(semanticKey))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    $"Connection '{connection.Id}' duplicates an existing graph edge."));
            }

            if (!nodes.TryGetValue(connection.FromNodeId, out var fromNode) || !nodes.TryGetValue(connection.ToNodeId, out var toNode))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Connection '{connection.Id}' references a missing node."));
                continue;
            }

            var fromItem = catalog.Find(fromNode.LibraryItemId, fromNode.LibraryItemVersion);
            var toItem = catalog.Find(toNode.LibraryItemId, toNode.LibraryItemVersion);
            var output = fromItem?.Ports.FirstOrDefault(port =>
                port.Id.Equals(connection.FromPortId, StringComparison.OrdinalIgnoreCase) &&
                port.Direction == ForeverStudioLibraryPortDirection.Output);
            var input = toItem?.Ports.FirstOrDefault(port =>
                port.Id.Equals(connection.ToPortId, StringComparison.OrdinalIgnoreCase) &&
                port.Direction == ForeverStudioLibraryPortDirection.Input);

            if (output is null || input is null || !catalog.ArePortsCompatible(output, input))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Connection '{connection.Id}' has missing or incompatible typed ports."));
                continue;
            }

            var inputKey = $"{toNode.Id}\n{input.Id}";
            incomingCounts[inputKey] = incomingCounts.GetValueOrDefault(inputKey) + 1;
            if (!input.Multiple && incomingCounts[inputKey] > 1)
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    $"Input '{toNode.Id}.{input.Id}' accepts a single connection but has {incomingCounts[inputKey]}."));
            }
        }

        foreach (var node in document.Behavior.Nodes)
        {
            var item = catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is not { Kind: ForeverStudioLibraryItemKind.Function })
                continue;
            foreach (var input in item.Ports.Where(port =>
                         port.Direction == ForeverStudioLibraryPortDirection.Input && port.Required))
            {
                var inputKey = $"{node.Id}\n{input.Id}";
                if (incomingCounts.GetValueOrDefault(inputKey) == 0)
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Warning,
                        $"Required input '{node.Id}.{input.Id}' is not connected yet."));
                }
            }
        }
    }

    private static void ValidateBindings(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var componentIds = document.Components.Select(component => component.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var guiIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in document.Gui.Bindings)
        {
            if (!IsSafeLocalId(binding.Id) || !guiIds.Add(binding.Id))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "GUI binding IDs must be unique safe IDs."));
            if (catalog.Find(binding.LibraryItemId, binding.LibraryItemVersion) is not { Kind: ForeverStudioLibraryItemKind.Asset })
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"GUI binding '{binding.Id}' references an unavailable asset library item."));
        }

        var animationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in document.Animations)
        {
            if (!IsSafeLocalId(binding.Id) || !animationIds.Add(binding.Id))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Animation binding IDs must be unique safe IDs."));
            if (!componentIds.Contains(binding.TargetComponentId))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Animation binding '{binding.Id}' targets missing component '{binding.TargetComponentId}'."));
            if (catalog.Find(binding.LibraryItemId, binding.LibraryItemVersion) is not { Kind: ForeverStudioLibraryItemKind.Animation })
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Animation binding '{binding.Id}' references an unavailable animation library item."));
        }
    }

    private static void ValidateLibraryManifest(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        if (document.Library.ContractVersion != ForeverStudioLibraryCatalog.CurrentContractVersion)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Machine library contractVersion does not match the installed Studio contract."));

        var manifestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in document.Library.References)
        {
            var key = $"{reference.ItemId}@{reference.Version}";
            if (!manifestKeys.Add(key))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Duplicate library reference '{key}'."));
            if (catalog.Find(reference.ItemId, reference.Version) is null)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Library reference '{key}' is unavailable."));
        }
    }

    private static string ResolveRepoPath(string repoRoot, string relativePath)
    {
        var root = Path.GetFullPath(repoRoot.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved Machine Builder path escaped the selected repository.");
        return full;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeLocalId(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsSafeLocalId(string value)
        => value.Length is > 0 and <= 128 &&
           value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');

    private static bool IsSafeAssetId(string value)
        => IsSafeLocalId(value) && value.All(character => !char.IsLetter(character) || char.IsLower(character));

    private static bool IsSafeNamespace(string value)
        => value.Length is > 0 and <= 64 &&
           value.All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.');

    private static string Humanize(string value)
        => string.Join(' ', value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]));
}

using System.Security.Cryptography;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Stable entry point used by Review & Build. Visual-only projects stay on the proven static
/// backend; projects with executable authoring are routed to the production runtime compiler.
/// </summary>
public static class ForeverStudioMachineCompilerService
{
    public const int ManifestSchemaVersion = ForeverStudioRuntimeCompiler.ManifestSchemaVersion;
    public const string ProductionKindStaticBlock = ForeverStudioMachineProductionService.ProductionKindStaticBlock;
    public const string ProductionKindRuntimeMachine = ForeverStudioRuntimeCompiler.ProductionKindRuntimeMachine;

    private const string MainModPath =
        "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java";
    private const string MissingHash = "<missing>";
    private const string BridgeAnchor = "RECIPE_SERIALIZERS.register(modBus);";
    private const string BridgeInvocation =
        "de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus);";
    private const string BridgeLine =
        "        de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus); // Forever Studio generated machines";

    public static ForeverStudioMachineProductionPlan BuildPlan(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!UsesRuntimeBackend(document))
        {
            var staticPlan = ForeverStudioMachineProductionService.BuildPlan(repoRoot, document, catalog);
            return NormalizeGeneratedRegistryBridge(repoRoot, staticPlan);
        }

        var runtime = ForeverStudioRuntimeCompiler.BuildPlan(repoRoot, document, catalog);
        var supportIssues = ForeverStudioRuntimeSupportPolicy.Validate(document);
        if (supportIssues.Count > 0)
            return runtime with
            {
                Changes = [],
                Issues = runtime.Issues.Concat(supportIssues).ToList()
            };
        var normalized = ForeverStudioRuntimePlanFixup.Normalize(repoRoot, document, runtime);
        normalized = ForeverStudioRuntimeComponentModelFixup.Normalize(repoRoot, document, catalog, normalized);
        return NormalizeGeneratedRegistryBridge(repoRoot, normalized);
    }

    public static ForeverStudioMachineProductionApplyResult Apply(ForeverStudioMachineProductionPlan plan)
        => ForeverStudioRuntimeCompiler.Apply(plan);

    public static bool UsesRuntimeBackend(ForeverStudioMachineDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return HasRuntimeAuthoring(document);
    }

    private static bool HasRuntimeAuthoring(ForeverStudioMachineDocument document)
    {
        var executable = document.Behavior.Nodes.Any(node =>
            !(node.Id.Equals("machine_entry", StringComparison.OrdinalIgnoreCase) &&
              node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase)));
        return executable || document.Behavior.Connections.Count > 0 || document.Recipes.Count > 0 ||
               document.Gui.Bindings.Count > 0 || !string.IsNullOrWhiteSpace(document.Gui.ScreenId) ||
               document.Animations.Count > 0;
    }

    /// <summary>
    /// Older repository revisions already installed the generated registry bridge without the
    /// ownership marker comment. The production backend historically only recognized the marked
    /// variant and could therefore append a second invocation. NeoForge then sees the same
    /// DeferredRegister registered twice and aborts during mod construction. Normalize both legacy
    /// and current bridge spellings to exactly one canonical invocation before Apply.
    /// </summary>
    private static ForeverStudioMachineProductionPlan NormalizeGeneratedRegistryBridge(
        string repoRoot,
        ForeverStudioMachineProductionPlan plan)
    {
        if (plan.ErrorCount > 0)
            return plan;

        var root = Path.GetFullPath(repoRoot.Trim());
        var fullPath = Path.GetFullPath(Path.Combine(
            root,
            MainModPath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(fullPath))
            return plan;

        var bridgeChanges = plan.Changes
            .Where(change => change.Kind == ForeverStudioMachineProductionChangeKind.JavaBridge &&
                             change.RelativePath.Equals(MainModPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var source = bridgeChanges.LastOrDefault()?.NewText ?? File.ReadAllText(fullPath);
        var canonical = CanonicalizeBridge(source);
        var current = NormalizeNewlines(File.ReadAllText(fullPath));

        var changes = plan.Changes
            .Where(change => !(change.Kind == ForeverStudioMachineProductionChangeKind.JavaBridge &&
                               change.RelativePath.Equals(MainModPath, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (!string.Equals(current, canonical, StringComparison.Ordinal))
        {
            changes.Add(new ForeverStudioMachineProductionChange
            {
                Kind = ForeverStudioMachineProductionChangeKind.JavaBridge,
                RelativePath = MainModPath,
                Description = "Normalize the one-time Forever Studio generated-machine registry bridge.",
                NewText = canonical,
                PreviewLines = canonical.Split('\n').Take(120).ToList(),
                FullPath = fullPath,
                OriginalHash = File.Exists(fullPath) ? HashFile(fullPath) : MissingHash,
                ExistedBefore = File.Exists(fullPath)
            });
        }

        return plan with { Changes = changes };
    }

    private static string CanonicalizeBridge(string source)
    {
        var lines = NormalizeNewlines(source).Split('\n').ToList();
        var kept = new List<string>(lines.Count);
        var firstBridgeIndex = -1;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Equals(BridgeInvocation, StringComparison.Ordinal) ||
                trimmed.StartsWith(BridgeInvocation + " //", StringComparison.Ordinal))
            {
                if (firstBridgeIndex < 0)
                    firstBridgeIndex = kept.Count;
                continue;
            }
            kept.Add(line);
        }

        var anchorIndex = kept.FindIndex(line => line.Contains(BridgeAnchor, StringComparison.Ordinal));
        if (anchorIndex >= 0)
        {
            kept.Insert(anchorIndex + 1, BridgeLine);
        }
        else if (firstBridgeIndex >= 0)
        {
            kept.Insert(Math.Min(firstBridgeIndex, kept.Count), BridgeLine);
        }
        else
        {
            // The underlying production backend owns the missing-anchor validation. If neither an
            // anchor nor a historical bridge exists, preserve its result rather than guessing.
            return NormalizeNewlines(source);
        }

        return string.Join("\n", kept);
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}

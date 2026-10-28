using System.Security.Cryptography;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Normalizes the runtime ownership manifest after the visual backend has removed no-op file writes.
/// A file remains Studio-owned even when its bytes are already current, so no-op files must not vanish
/// from generatedFiles or be mistaken for stale output on the next build.
/// </summary>
internal static class ForeverStudioRuntimePlanFixup
{
    private const string MissingHash = "<missing>";

    internal static ForeverStudioMachineProductionPlan Normalize(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioMachineProductionPlan plan)
    {
        if (!plan.ProductionKind.Equals(ForeverStudioRuntimeCompiler.ProductionKindRuntimeMachine, StringComparison.Ordinal))
            return plan;

        var asset = document.AssetId;
        var manifestPath = $"mod/src/main/resources/data/forever_skyblock/forever_studio/generated/{asset}.json";
        var intended = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"mod/src/main/resources/assets/forever_skyblock/models/block/{asset}.json",
            $"mod/src/main/resources/assets/forever_skyblock/models/item/{asset}.json",
            $"mod/src/main/resources/assets/forever_skyblock/blockstates/{asset}.json",
            $"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{asset}.json",
            "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json",
            "mod/src/main/java/de/tekatz/foreverskyblock/generated/ForeverStudioGeneratedMachines.java",
            $"mod/src/main/resources/data/forever_skyblock/forever_studio/runtime/{asset}.json",
            $"mod/src/main/resources/data/forever_skyblock/recipe/forever_studio/{asset}.json",
            manifestPath
        };

        var changes = plan.Changes
            .Where(change => !(change.Description.StartsWith("[DELETE] ", StringComparison.Ordinal) &&
                               intended.Contains(change.RelativePath)))
            .Where(change => !change.RelativePath.Equals(manifestPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var root = Path.GetFullPath(repoRoot.Trim());
        var fullManifest = Path.GetFullPath(Path.Combine(root, manifestPath.Replace('/', Path.DirectorySeparatorChar)));
        var manifestText = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schemaVersion"] = ForeverStudioRuntimeCompiler.ManifestSchemaVersion,
            ["assetId"] = document.AssetId,
            ["displayName"] = document.DisplayName,
            ["productionKind"] = ForeverStudioRuntimeCompiler.ProductionKindRuntimeMachine,
            ["machineProject"] = ForeverStudioMachineProjectService.RelativePathForAsset(document.AssetId),
            ["generatedFiles"] = intended.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;

        var existed = File.Exists(fullManifest);
        var current = existed ? NormalizeNewlines(File.ReadAllText(fullManifest)) : null;
        if (!string.Equals(current, NormalizeNewlines(manifestText), StringComparison.Ordinal))
        {
            changes.Add(new ForeverStudioMachineProductionChange
            {
                Kind = ForeverStudioMachineProductionChangeKind.BuildManifest,
                RelativePath = manifestPath,
                Description = "Record the complete Studio-owned runtime file set for deterministic rebuilds.",
                NewText = NormalizeNewlines(manifestText),
                PreviewLines = NormalizeNewlines(manifestText).Split('\n').Take(120).ToList(),
                FullPath = fullManifest,
                OriginalHash = existed ? HashFile(fullManifest) : MissingHash,
                ExistedBefore = existed
            });
        }

        return plan with { Changes = changes };
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}

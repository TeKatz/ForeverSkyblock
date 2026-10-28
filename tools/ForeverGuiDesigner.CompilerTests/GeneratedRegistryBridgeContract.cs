using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class GeneratedRegistryBridgeContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "forever studio bridge contract " + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var mainPath = Path.Combine(root, "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "ForeverSkyblock.java");
            Directory.CreateDirectory(Path.GetDirectoryName(mainPath)!);
            File.WriteAllText(mainPath, """
package de.tekatz.foreverskyblock;
public final class ForeverSkyblock {
    void register() {
        RECIPE_SERIALIZERS.register(modBus);
        de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus);
    }
}
""");

            var texturePath = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock", "textures", "block", "ascension_materials.png");
            Directory.CreateDirectory(Path.GetDirectoryName(texturePath)!);
            File.WriteAllBytes(texturePath, [0]);

            var load = ForeverStudioLibraryCatalog.Load(root);
            Check(load.Warnings.Count == 0, "Built-in Studio library must load for bridge contract: " + string.Join(" | ", load.Warnings));
            var housing = load.Catalog.Find("asset.housing", 1) ?? throw new InvalidOperationException("Missing built-in asset.housing.");
            var document = ForeverStudioMachineProjectService.CreateBlank("bridge_contract", "Bridge Contract");
            document.Components.Add(ForeverStudioVisualAssemblyService.CreateComponent(housing, document.Components));

            var plan = ForeverStudioMachineCompilerService.BuildPlan(root, document, load.Catalog);
            Check(plan.CanApply, "Legacy bridge normalization plan must be applyable: " + string.Join(" | ", plan.Issues.Select(issue => issue.Message)));
            _ = ForeverStudioMachineCompilerService.Apply(plan);

            var source = File.ReadAllText(mainPath).Replace("\r\n", "\n", StringComparison.Ordinal);
            const string invocation = "de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus);";
            var count = source.Split('\n').Count(line => line.Contains(invocation, StringComparison.Ordinal));
            Check(count == 1, $"Generated registry bridge must occur exactly once after Apply, found {count}.");
            Check(source.Contains(invocation + " // Forever Studio generated machines", StringComparison.Ordinal),
                "Legacy unmarked bridge must be normalized to the canonical owned bridge line.");

            var secondPlan = ForeverStudioMachineCompilerService.BuildPlan(root, document, load.Catalog);
            Check(!secondPlan.Changes.Any(change => change.Kind == ForeverStudioMachineProductionChangeKind.JavaBridge),
                "A canonical bridge must not schedule another Java bridge change on the next build.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

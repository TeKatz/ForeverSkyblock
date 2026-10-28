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

    public static ForeverStudioMachineProductionPlan BuildPlan(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!HasRuntimeAuthoring(document))
            return ForeverStudioMachineProductionService.BuildPlan(repoRoot, document, catalog);

        var runtime = ForeverStudioRuntimeCompiler.BuildPlan(repoRoot, document, catalog);
        return ForeverStudioRuntimePlanFixup.Normalize(repoRoot, document, runtime);
    }

    public static ForeverStudioMachineProductionApplyResult Apply(ForeverStudioMachineProductionPlan plan)
        => ForeverStudioRuntimeCompiler.Apply(plan);

    private static bool HasRuntimeAuthoring(ForeverStudioMachineDocument document)
    {
        var executable = document.Behavior.Nodes.Any(node =>
            !(node.Id.Equals("machine_entry", StringComparison.OrdinalIgnoreCase) &&
              node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase)));
        return executable || document.Behavior.Connections.Count > 0 || document.Recipes.Count > 0 ||
               document.Gui.Bindings.Count > 0 || !string.IsNullOrWhiteSpace(document.Gui.ScreenId) ||
               document.Animations.Count > 0;
    }
}

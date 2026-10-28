namespace ForeverGuiDesigner.Services;

/// <summary>
/// Explicit production boundary for graph semantics that have an authoritative Java implementation.
/// Nodes outside this set stay authorable in Studio, but Review & Build refuses to manufacture stub
/// behavior until their mutation/transfer contract is fully specified and implemented.
/// </summary>
internal static class ForeverStudioRuntimeSupportPolicy
{
    private static readonly HashSet<string> ExecutableNodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "core.machine-entry",
        "function.on-tick",
        "function.on-use",
        "function.on-redstone-changed",
        "function.on-neighbor-changed",
        "function.on-inventory-changed",
        "function.on-recipe-start",
        "function.on-recipe-complete",
        "function.on-state-enter",
        "function.item-input",
        "function.item-output",
        "function.item-storage",
        "function.fuel-slot",
        "function.upgrade-slot",
        "function.energy-storage",
        "function.has-energy",
        "function.fluid-tank",
        "function.has-fluid",
        "function.find-recipe",
        "function.recipe-available",
        "function.output-space",
        "function.process-progress",
        "function.process-duration",
        "function.secondary-output",
        "function.branch",
        "function.and",
        "function.or",
        "function.not",
        "function.compare-number",
        "function.compare-state",
        "function.random-chance",
        "function.number",
        "function.boolean",
        "function.clamp",
        "function.map-range",
        "function.set-state",
        "function.redstone-input",
        "function.standard-processor",
        "function.powered-processor",
        "function.fluid-processor",
        "function.generator-core",
        "function.redstone-controller"
    };

    internal static IReadOnlyList<StudioValidationIssue> Validate(ForeverStudioMachineDocument document)
    {
        var issues = new List<StudioValidationIssue>();
        foreach (var node in document.Behavior.Nodes)
        {
            if (ExecutableNodes.Contains(node.LibraryItemId)) continue;
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Function node '{node.Id}' ({node.LibraryItemId}) remains authoring-only. " +
                "Production is fail-closed because its stateful mutation/transfer semantics are not yet safe to emit."));
        }
        return issues;
    }
}

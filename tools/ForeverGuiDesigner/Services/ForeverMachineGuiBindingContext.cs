namespace ForeverGuiDesigner.Services;

public sealed record ForeverMachineGuiBindingSource(
    string Key,
    string Category,
    string DisplayName,
    string NodeId,
    string PortId,
    string DataType,
    string NodeLibraryItemId,
    int NodeLibraryItemVersion,
    IReadOnlyList<string> SuggestedWidgetKinds)
{
    public override string ToString() => $"{Category} · {DisplayName}";
}

/// <summary>
/// Human-facing projection of typed Function Graph outputs for the shared GUI editor. Raw node/port
/// ids remain the persisted runtime contract, but editor surfaces consume friendly categories,
/// labels and widget compatibility from this context instead of exposing technical ids directly.
/// </summary>
public sealed class ForeverMachineGuiBindingContext
{
    private ForeverMachineGuiBindingContext(IReadOnlyList<ForeverMachineGuiBindingSource> sources)
    {
        Sources = sources;
        ByKey = sources.ToDictionary(source => source.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ForeverMachineGuiBindingSource> Sources { get; }
    public IReadOnlyDictionary<string, ForeverMachineGuiBindingSource> ByKey { get; }

    public static ForeverMachineGuiBindingContext Build(
        ForeverStudioMachineDocument machine,
        ForeverStudioLibraryCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(catalog);

        var sources = new List<ForeverMachineGuiBindingSource>();
        foreach (var node in machine.Behavior.Nodes)
        {
            var libraryItem = catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (libraryItem is null)
                continue;

            foreach (var port in libraryItem.Ports.Where(port =>
                         port.Direction == ForeverStudioLibraryPortDirection.Output &&
                         !port.DataType.Equals("flow", StringComparison.OrdinalIgnoreCase)))
            {
                var category = CategoryFor(node.LibraryItemId);
                var displayName = $"{libraryItem.DisplayName} → {port.DisplayName}";
                sources.Add(new ForeverMachineGuiBindingSource(
                    Key(node.Id, port.Id),
                    category,
                    displayName,
                    node.Id,
                    port.Id,
                    port.DataType,
                    node.LibraryItemId,
                    node.LibraryItemVersion,
                    SuggestedWidgets(node.LibraryItemId, port.Id, port.DataType)));
            }
        }

        return new ForeverMachineGuiBindingContext(sources
            .OrderBy(source => CategoryOrder(source.Category))
            .ThenBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.Key, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    public ForeverMachineGuiBindingSource? Find(string nodeId, string portId)
        => ByKey.TryGetValue(Key(nodeId, portId), out var source) ? source : null;

    public IReadOnlyList<ForeverMachineGuiBindingSource> CompatibleWith(string widgetKind)
        => Sources.Where(source => source.SuggestedWidgetKinds.Contains(widgetKind, StringComparer.OrdinalIgnoreCase)).ToList();

    public static string Key(string nodeId, string portId) => $"{nodeId}::{portId}";

    private static string CategoryFor(string libraryItemId)
    {
        if (libraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("generator-core", StringComparison.OrdinalIgnoreCase))
            return "Processor";
        if (libraryItemId.Contains("energy", StringComparison.OrdinalIgnoreCase))
            return "Energy";
        if (libraryItemId.Contains("fluid", StringComparison.OrdinalIgnoreCase))
            return "Fluids";
        if (libraryItemId.Contains("input", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("fuel", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("filter", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("catalyst", StringComparison.OrdinalIgnoreCase))
            return "Inputs";
        if (libraryItemId.Contains("output", StringComparison.OrdinalIgnoreCase))
            return "Outputs";
        if (libraryItemId.Contains("storage", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("upgrade", StringComparison.OrdinalIgnoreCase))
            return "Storage";
        if (libraryItemId.Contains("state", StringComparison.OrdinalIgnoreCase))
            return "Machine State";
        return "Machine";
    }

    private static IReadOnlyList<string> SuggestedWidgets(string libraryItemId, string portId, string dataType)
    {
        var result = new List<string>();
        if (dataType.Equals("item", StringComparison.OrdinalIgnoreCase))
        {
            result.Add(IsOutputNode(libraryItemId) ? "output-slot" : "item-slot");
            result.Add("value");
        }
        else if (dataType.Equals("fluid", StringComparison.OrdinalIgnoreCase))
        {
            result.Add("fluid-gauge");
            result.Add("value");
        }
        else if (dataType.Equals("energy", StringComparison.OrdinalIgnoreCase))
        {
            result.Add("energy-bar");
            result.Add("value");
        }
        else if (dataType.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
                 dataType.Equals("state", StringComparison.OrdinalIgnoreCase))
        {
            result.Add("status");
            result.Add("value");
        }
        else if (dataType.Equals("number", StringComparison.OrdinalIgnoreCase))
        {
            if (libraryItemId.Contains("energy", StringComparison.OrdinalIgnoreCase) ||
                portId.Contains("energy", StringComparison.OrdinalIgnoreCase))
                result.Add("energy-bar");
            if (libraryItemId.Contains("fluid", StringComparison.OrdinalIgnoreCase))
                result.Add("fluid-gauge");
            if (portId.Contains("progress", StringComparison.OrdinalIgnoreCase) ||
                portId.Contains("duration", StringComparison.OrdinalIgnoreCase))
                result.Add("progress-bar");
            result.Add("value");
        }
        else
        {
            result.Add("value");
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsOutputNode(string id)
        => id.Contains("output", StringComparison.OrdinalIgnoreCase) ||
           id.Contains("storage", StringComparison.OrdinalIgnoreCase);

    private static int CategoryOrder(string category) => category switch
    {
        "Inputs" => 0,
        "Outputs" => 1,
        "Storage" => 2,
        "Processor" => 3,
        "Energy" => 4,
        "Fluids" => 5,
        "Machine State" => 6,
        _ => 7
    };
}

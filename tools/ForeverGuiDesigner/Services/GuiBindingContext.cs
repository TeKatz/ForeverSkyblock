using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Supplies typed runtime values to the shared GUI editor without making the GUI document itself
/// aware of Machine Builder, menus or any future context.
/// </summary>
public interface IGuiBindingContext
{
    string ContextId { get; }
    IReadOnlyList<GuiBindingSource> Sources { get; }
}

public sealed record GuiBindingSource(
    string Id,
    string Context,
    string Category,
    string DisplayName,
    string SourceId,
    string SourcePort,
    string DataType)
{
    public GuiBindingModel CreateBinding(string bindingId, string elementId, string targetProperty)
        => new()
        {
            Id = bindingId,
            ElementId = elementId,
            TargetProperty = targetProperty,
            Context = Context,
            SourceId = SourceId,
            SourcePort = SourcePort,
            DataType = DataType
        };
}

public sealed class MachineGuiBindingContext : IGuiBindingContext
{
    private readonly IReadOnlyList<GuiBindingSource> _sources;

    private MachineGuiBindingContext(IReadOnlyList<GuiBindingSource> sources)
    {
        _sources = sources;
    }

    public string ContextId => "machine";
    public IReadOnlyList<GuiBindingSource> Sources => _sources;

    public static MachineGuiBindingContext Create(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalog);

        var sources = new List<GuiBindingSource>();
        foreach (var node in document.Behavior.Nodes)
        {
            var item = catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is null)
                continue;

            var category = CategoryFor(node.LibraryItemId);
            foreach (var port in item.Ports.Where(port =>
                         port.Direction == ForeverStudioLibraryPortDirection.Output &&
                         !port.DataType.Equals("flow", StringComparison.OrdinalIgnoreCase)))
            {
                sources.Add(new GuiBindingSource(
                    $"{node.Id}.{port.Id}",
                    "machine",
                    category,
                    $"{item.DisplayName} · {port.DisplayName}",
                    node.Id,
                    port.Id,
                    port.DataType));
            }
        }

        return new MachineGuiBindingContext(sources
            .OrderBy(source => CategoryOrder(source.Category))
            .ThenBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.Id, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    public GuiBindingSource? Find(string sourceId, string sourcePort)
        => _sources.FirstOrDefault(source =>
            source.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase) &&
            source.SourcePort.Equals(sourcePort, StringComparison.OrdinalIgnoreCase));

    private static string CategoryFor(string libraryItemId)
    {
        if (libraryItemId.Contains("item-output", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("item-storage", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("secondary-output", StringComparison.OrdinalIgnoreCase))
            return "Outputs";
        if (libraryItemId.Contains("item-input", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("fuel-slot", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("filter-slot", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("catalyst-slot", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("upgrade-slot", StringComparison.OrdinalIgnoreCase))
            return "Inputs";
        if (libraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("generator-core", StringComparison.OrdinalIgnoreCase) ||
            libraryItemId.Contains("process-", StringComparison.OrdinalIgnoreCase))
            return "Processor";
        if (libraryItemId.Contains("energy", StringComparison.OrdinalIgnoreCase))
            return "Energy";
        if (libraryItemId.Contains("fluid", StringComparison.OrdinalIgnoreCase))
            return "Fluids";
        if (libraryItemId.Contains("state", StringComparison.OrdinalIgnoreCase))
            return "Machine State";
        return "Runtime Values";
    }

    private static int CategoryOrder(string category)
        => category switch
        {
            "Inputs" => 0,
            "Outputs" => 1,
            "Processor" => 2,
            "Energy" => 3,
            "Fluids" => 4,
            "Machine State" => 5,
            _ => 6
        };
}

using System.Text.Json;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Transitional persistence bridge between the shared GUI document and the existing Machine Project
/// schema. The compact Gui.Bindings list remains the runtime/compiler projection while the complete
/// shared document is preserved in the Machine Project's existing extension-data envelope. This lets
/// the standalone editor and Machine Builder round-trip layers/styles/effects/state without forcing a
/// runtime schema change during the editor migration.
/// </summary>
public static class ForeverMachineGuiDocumentBridge
{
    public const string ExtensionKey = "foreverGuiDocument";

    public static ForeverGuiDocument FromMachineDocument(ForeverStudioMachineDocument machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        var runtimeView = ForeverGuiDocumentService.FromMachineDocument(machine);
        if (machine.ExtensionData is null || !machine.ExtensionData.TryGetValue(ExtensionKey, out var saved))
            return runtimeView;
        if (saved.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Machine Project extension '{ExtensionKey}' must be a GUI document object.");

        ForeverGuiDocument shared;
        try
        {
            shared = ForeverGuiDocumentService.Deserialize(saved.GetRawText());
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            throw new InvalidDataException("The preserved shared GUI document is invalid and cannot be safely ignored.", ex);
        }

        shared.DocumentId = runtimeView.DocumentId;
        shared.Name = runtimeView.Name;
        if (!double.IsFinite(shared.Canvas.Width) || shared.Canvas.Width <= 0 ||
            !double.IsFinite(shared.Canvas.Height) || shared.Canvas.Height <= 0)
            shared.Canvas = runtimeView.Canvas;

        var savedElements = shared.Elements.ToDictionary(element => element.Id, StringComparer.OrdinalIgnoreCase);
        var savedBindings = shared.Bindings.ToDictionary(binding => binding.Id, StringComparer.OrdinalIgnoreCase);
        var runtimeElementIds = runtimeView.Elements.Select(element => element.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runtimeBindingIds = runtimeView.Bindings.Select(binding => binding.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mergedElements = new List<ForeverGuiElement>();
        foreach (var current in runtimeView.Elements)
        {
            if (!savedElements.TryGetValue(current.Id, out var previous))
            {
                mergedElements.Add(current);
                continue;
            }
            mergedElements.Add(MergeElement(previous, current));
        }
        // Preserve shared/static elements that have no compact Machine binding projection yet.
        mergedElements.AddRange(shared.Elements.Where(element => !runtimeElementIds.Contains(element.Id)));
        shared.Elements = mergedElements;

        var mergedBindings = new List<ForeverGuiBinding>();
        foreach (var current in runtimeView.Bindings)
        {
            if (!savedBindings.TryGetValue(current.Id, out var previous))
            {
                mergedBindings.Add(current);
                continue;
            }
            mergedBindings.Add(MergeBinding(previous, current));
        }
        mergedBindings.AddRange(shared.Bindings.Where(binding => !runtimeBindingIds.Contains(binding.Id)));
        shared.Bindings = mergedBindings;

        var assets = shared.Assets.ToDictionary(asset => asset.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var asset in runtimeView.Assets)
            assets[asset.Id] = asset;
        shared.Assets = assets.Values.OrderBy(asset => asset.Id, StringComparer.OrdinalIgnoreCase).ToList();
        return shared;
    }

    public static void ApplyToMachineDocument(ForeverStudioMachineDocument machine, ForeverGuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(document);

        // The runtime projection contains only machine-bound elements. Shared-only authoring data is
        // persisted below instead of being silently discarded or invented in the Java runtime.
        var machineBindings = document.Bindings
            .Where(binding => binding.Provider.Equals("machine", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(binding => binding.Id, StringComparer.OrdinalIgnoreCase);
        var runtimeProjection = new ForeverGuiDocument
        {
            DocumentId = document.DocumentId,
            Name = document.Name,
            Canvas = document.Canvas,
            Elements = document.Elements.Where(element =>
            {
                var bindingId = string.IsNullOrWhiteSpace(element.BindingId) ? element.Id : element.BindingId;
                return machineBindings.ContainsKey(bindingId);
            }).ToList(),
            Bindings = machineBindings.Values.ToList(),
            Assets = document.Assets
        };

        // Build the compact projection first so a malformed Machine binding cannot leave a partially
        // updated extension payload behind.
        machine.Gui = ForeverGuiDocumentService.ToMachineGuiDefinition(runtimeProjection);

        var serialized = ForeverGuiDocumentService.Serialize(document);
        using var parsed = JsonDocument.Parse(serialized);
        machine.ExtensionData ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        machine.ExtensionData[ExtensionKey] = parsed.RootElement.Clone();
    }

    private static ForeverGuiElement MergeElement(ForeverGuiElement previous, ForeverGuiElement current)
    {
        previous.Kind = current.Kind;
        previous.X = current.X;
        previous.Y = current.Y;
        previous.Width = current.Width;
        previous.Height = current.Height;
        previous.Order = current.Order;
        previous.AssetId = current.AssetId;
        previous.BindingId = current.BindingId;
        previous.Text = current.Text;
        return previous;
    }

    private static ForeverGuiBinding MergeBinding(ForeverGuiBinding previous, ForeverGuiBinding current)
    {
        var mergedProperties = previous.Properties.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone(),
            StringComparer.OrdinalIgnoreCase);
        foreach (var pair in current.Properties)
            mergedProperties[pair.Key] = pair.Value.Clone();

        previous.Provider = current.Provider;
        previous.SourceId = current.SourceId;
        previous.SourcePortId = current.SourcePortId;
        previous.SourceType = current.SourceType;
        previous.LibraryItemId = current.LibraryItemId;
        previous.LibraryItemVersion = current.LibraryItemVersion;
        previous.Properties = mergedProperties;
        return previous;
    }
}

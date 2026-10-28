using System.Text.Json;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

public enum ForeverGuiContractSeverity
{
    Info,
    Warning,
    Error
}

public sealed record ForeverGuiContractIssue(ForeverGuiContractSeverity Severity, string Message);

/// <summary>
/// Serialization, validation and migration boundary for the shared GUI document. During the staged
/// migration the existing standalone GuiLayoutModel and Machine Builder GUI bindings remain usable,
/// but both can round-trip through this contract without discarding provider-owned data.
/// </summary>
public static class ForeverGuiDocumentService
{
    private const string LegacyBridgeKey = "foreverShared";
    private const int MaxElements = 4096;
    private const int MaxAuxiliaryEntries = 4096;
    private const double MaxCanvasDimension = 8192;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        MaxDepth = 96
    };

    private sealed class LegacyDocumentBridge
    {
        public string DocumentId { get; set; } = string.Empty;
        public double CanvasSnap { get; set; } = 1;
        public string? CanvasBackgroundAssetId { get; set; }
        public Dictionary<string, JsonElement>? CanvasExtensionData { get; set; }
        public List<ForeverGuiLayer> Layers { get; set; } = [];
        public List<ForeverGuiAssetReference> Assets { get; set; } = [];
        public List<ForeverGuiBinding> Bindings { get; set; } = [];
        public List<ForeverGuiStyle> Styles { get; set; } = [];
        public List<ForeverGuiEffect> Effects { get; set; } = [];
        public List<ForeverGuiState> States { get; set; } = [];
    }

    private sealed class LegacyElementBridge
    {
        public double Order { get; set; }
        public string? LayerId { get; set; }
        public string? AssetId { get; set; }
        public string? BindingId { get; set; }
        public string? StyleId { get; set; }
        public List<string> EffectIds { get; set; } = [];
        public List<string> StateIds { get; set; } = [];
        public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static string Serialize(ForeverGuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Normalize(document);
        var issues = Validate(document);
        if (issues.Any(issue => issue.Severity == ForeverGuiContractSeverity.Error))
            throw new InvalidDataException("Shared GUI document is invalid: " + string.Join(" | ", issues.Select(issue => issue.Message)));
        return JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
    }

    public static ForeverGuiDocument Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("Shared GUI document is empty.");
        var document = JsonSerializer.Deserialize<ForeverGuiDocument>(json, JsonOptions)
                       ?? throw new InvalidDataException("Shared GUI document is empty.");
        Normalize(document);
        if (document.FormatVersion != ForeverGuiDocument.CurrentFormatVersion)
            throw new InvalidDataException(
                $"Unsupported shared GUI formatVersion {document.FormatVersion}; this Studio supports {ForeverGuiDocument.CurrentFormatVersion}.");
        return document;
    }

    public static ForeverGuiDocument FromLegacyLayout(GuiLayoutModel layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var bridge = ReadBridge<LegacyDocumentBridge>(layout.ExtensionData);
        var document = new ForeverGuiDocument
        {
            DocumentId = bridge?.DocumentId ?? string.Empty,
            Name = layout.Name,
            Canvas = new ForeverGuiCanvas
            {
                Width = layout.Width,
                Height = layout.Height,
                Snap = bridge?.CanvasSnap ?? 1,
                BackgroundAssetId = bridge?.CanvasBackgroundAssetId,
                ExtensionData = CloneDictionary(bridge?.CanvasExtensionData)
            },
            Layers = bridge?.Layers ?? [],
            Assets = bridge?.Assets ?? [],
            Bindings = bridge?.Bindings ?? [],
            Styles = bridge?.Styles ?? [],
            Effects = bridge?.Effects ?? [],
            States = bridge?.States ?? [],
            ExtensionData = CloneDictionaryExcept(layout.ExtensionData, LegacyBridgeKey)
        };

        for (var index = 0; index < layout.Elements.Count; index++)
        {
            var source = layout.Elements[index];
            var elementBridge = ReadBridge<LegacyElementBridge>(source.ExtensionData);
            document.Elements.Add(new ForeverGuiElement
            {
                Id = source.Id,
                Kind = source.Type.ToString(),
                X = source.X,
                Y = source.Y,
                Width = source.Width,
                Height = source.Height,
                Order = elementBridge?.Order ?? index,
                Visible = source.Visible,
                Locked = source.Locked,
                LayerId = elementBridge?.LayerId,
                Group = source.Group,
                AssetId = elementBridge?.AssetId,
                BindingId = elementBridge?.BindingId,
                StyleId = elementBridge?.StyleId,
                EffectIds = elementBridge?.EffectIds ?? [],
                StateIds = elementBridge?.StateIds ?? [],
                Text = source.Content,
                TexturePath = source.TexturePath,
                TextShadow = source.TextShadow,
                TextScale = source.TextScale,
                TextAlignment = source.TextAlignment,
                ItemId = source.ItemId,
                StackCount = source.StackCount,
                Durability = source.Durability,
                GhostPreview = source.GhostPreview,
                GlintPreview = source.GlintPreview,
                EditorSuppressed = source.EditorSuppressed,
                EditorTextOverride = source.EditorTextOverride,
                EditorZIndexOverride = source.EditorZIndexOverride,
                EditorCustomTextureOverride = source.EditorCustomTextureOverride,
                Properties = CloneDictionary(elementBridge?.Properties) ?? new(StringComparer.OrdinalIgnoreCase),
                ExtensionData = CloneDictionaryExcept(source.ExtensionData, LegacyBridgeKey),
                EditorSourceOwned = source.EditorSourceOwned
            });
        }

        Normalize(document);
        return document;
    }

    public static GuiLayoutModel ToLegacyLayout(ForeverGuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Normalize(document);
        var layout = new GuiLayoutModel
        {
            Name = document.Name,
            Width = RequireIntegral(document.Canvas.Width, "canvas.width"),
            Height = RequireIntegral(document.Canvas.Height, "canvas.height"),
            ExtensionData = CloneDictionary(document.ExtensionData)
        };

        layout.ExtensionData ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        layout.ExtensionData[LegacyBridgeKey] = JsonSerializer.SerializeToElement(new LegacyDocumentBridge
        {
            DocumentId = document.DocumentId,
            CanvasSnap = document.Canvas.Snap,
            CanvasBackgroundAssetId = document.Canvas.BackgroundAssetId,
            CanvasExtensionData = CloneDictionary(document.Canvas.ExtensionData),
            Layers = document.Layers,
            Assets = document.Assets,
            Bindings = document.Bindings,
            Styles = document.Styles,
            Effects = document.Effects,
            States = document.States
        }, JsonOptions);

        foreach (var source in document.Elements.OrderBy(element => element.Order).ThenBy(element => element.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<GuiElementType>(source.Kind, ignoreCase: true, out var type))
                throw new InvalidDataException($"Shared GUI element '{source.Id}' kind '{source.Kind}' cannot be represented by the legacy editor model.");

            var extension = CloneDictionary(source.ExtensionData) ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            extension[LegacyBridgeKey] = JsonSerializer.SerializeToElement(new LegacyElementBridge
            {
                Order = source.Order,
                LayerId = source.LayerId,
                AssetId = source.AssetId,
                BindingId = source.BindingId,
                StyleId = source.StyleId,
                EffectIds = source.EffectIds,
                StateIds = source.StateIds,
                Properties = CloneDictionary(source.Properties) ?? new(StringComparer.OrdinalIgnoreCase)
            }, JsonOptions);

            layout.Elements.Add(new GuiElementModel
            {
                Id = source.Id,
                Type = type,
                X = RequireIntegral(source.X, $"element '{source.Id}'.x"),
                Y = RequireIntegral(source.Y, $"element '{source.Id}'.y"),
                Width = RequireIntegral(source.Width, $"element '{source.Id}'.width"),
                Height = RequireIntegral(source.Height, $"element '{source.Id}'.height"),
                Content = source.Text,
                TexturePath = source.TexturePath,
                Visible = source.Visible,
                Locked = source.Locked,
                Group = source.Group,
                TextShadow = source.TextShadow,
                TextScale = source.TextScale,
                TextAlignment = source.TextAlignment,
                ItemId = source.ItemId,
                StackCount = source.StackCount,
                Durability = source.Durability,
                GhostPreview = source.GhostPreview,
                GlintPreview = source.GlintPreview,
                EditorSuppressed = source.EditorSuppressed,
                EditorTextOverride = source.EditorTextOverride,
                EditorZIndexOverride = source.EditorZIndexOverride,
                EditorCustomTextureOverride = source.EditorCustomTextureOverride,
                ExtensionData = extension,
                EditorSourceOwned = source.EditorSourceOwned
            });
        }
        return layout;
    }

    public static ForeverGuiDocument FromMachineDocument(ForeverStudioMachineDocument machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        var document = new ForeverGuiDocument
        {
            DocumentId = machine.Gui.ScreenId,
            Name = machine.DisplayName,
            Canvas = new ForeverGuiCanvas { Width = 176, Height = 166 }
        };

        var seenAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < machine.Gui.Bindings.Count; index++)
        {
            var source = machine.Gui.Bindings[index];
            var widget = ReadString(source.Properties, "widget", "value");
            var sourcePort = ReadString(source.Properties, "sourcePortId", string.Empty);
            var sourceType = ReadString(source.Properties, "sourceType", string.Empty);
            var label = ReadString(source.Properties, "label", widget);
            var order = ReadNumber(source.Properties, "order", index);

            document.Bindings.Add(new ForeverGuiBinding
            {
                Id = source.Id,
                Provider = "machine",
                SourceId = source.SourceId,
                SourcePortId = sourcePort,
                SourceType = sourceType,
                LibraryItemId = source.LibraryItemId,
                LibraryItemVersion = source.LibraryItemVersion,
                Properties = CloneDictionary(source.Properties) ?? new(StringComparer.OrdinalIgnoreCase)
            });
            if (!string.IsNullOrWhiteSpace(source.LibraryItemId) && seenAssets.Add(source.LibraryItemId))
            {
                document.Assets.Add(new ForeverGuiAssetReference
                {
                    Id = source.LibraryItemId,
                    Kind = "studio-library",
                    Resource = source.LibraryItemId
                });
            }
            document.Elements.Add(new ForeverGuiElement
            {
                Id = source.Id,
                Kind = widget,
                X = ReadNumber(source.Properties, "x", 0),
                Y = ReadNumber(source.Properties, "y", 0),
                Width = ReadNumber(source.Properties, "width", 18),
                Height = ReadNumber(source.Properties, "height", 18),
                Order = order,
                AssetId = source.LibraryItemId,
                BindingId = source.Id,
                Text = label
            });
        }
        Normalize(document);
        return document;
    }

    public static ForeverStudioMachineGuiDefinition ToMachineGuiDefinition(ForeverGuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Normalize(document);
        var bindings = document.Bindings.ToDictionary(binding => binding.Id, StringComparer.OrdinalIgnoreCase);
        var result = new ForeverStudioMachineGuiDefinition { ScreenId = document.DocumentId };

        foreach (var element in document.Elements.OrderBy(candidate => candidate.Order).ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase))
        {
            var bindingId = string.IsNullOrWhiteSpace(element.BindingId) ? element.Id : element.BindingId;
            if (!bindings.TryGetValue(bindingId, out var binding))
                throw new InvalidDataException($"Shared GUI element '{element.Id}' has no Machine binding. Current runtime output must fail closed instead of dropping it.");
            if (!binding.Provider.Equals("machine", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Shared GUI binding '{binding.Id}' belongs to provider '{binding.Provider}', not Machine Builder.");
            if (string.IsNullOrWhiteSpace(binding.SourceId))
                throw new InvalidDataException($"Shared GUI binding '{binding.Id}' has no machine source id.");

            var properties = CloneDictionary(binding.Properties) ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            properties["widget"] = JsonSerializer.SerializeToElement(element.Kind);
            properties["sourcePortId"] = JsonSerializer.SerializeToElement(binding.SourcePortId);
            properties["sourceType"] = JsonSerializer.SerializeToElement(binding.SourceType);
            properties["label"] = JsonSerializer.SerializeToElement(element.Text);
            properties["x"] = JsonSerializer.SerializeToElement(element.X);
            properties["y"] = JsonSerializer.SerializeToElement(element.Y);
            properties["width"] = JsonSerializer.SerializeToElement(element.Width);
            properties["height"] = JsonSerializer.SerializeToElement(element.Height);
            properties["order"] = JsonSerializer.SerializeToElement(element.Order);

            result.Bindings.Add(new ForeverStudioMachineGuiBinding
            {
                Id = binding.Id,
                LibraryItemId = string.IsNullOrWhiteSpace(element.AssetId) ? binding.LibraryItemId : element.AssetId,
                LibraryItemVersion = binding.LibraryItemVersion,
                SourceId = binding.SourceId,
                Properties = properties
            });
        }
        return result;
    }

    public static void ApplyToMachineDocument(ForeverStudioMachineDocument machine, ForeverGuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(machine);
        machine.Gui = ToMachineGuiDefinition(document);
    }

    public static IReadOnlyList<ForeverGuiContractIssue> Validate(ForeverGuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Normalize(document);
        var issues = new List<ForeverGuiContractIssue>();
        if (document.FormatVersion != ForeverGuiDocument.CurrentFormatVersion)
            issues.Add(Error($"Unsupported formatVersion {document.FormatVersion}."));
        if (!FinitePositive(document.Canvas.Width) || document.Canvas.Width > MaxCanvasDimension ||
            !FinitePositive(document.Canvas.Height) || document.Canvas.Height > MaxCanvasDimension)
            issues.Add(Error($"Canvas dimensions must be finite and within 0..{MaxCanvasDimension:0}."));
        if (!double.IsFinite(document.Canvas.Snap) || document.Canvas.Snap <= 0 || document.Canvas.Snap > 1024)
            issues.Add(Error("Canvas snap must be a finite positive value no greater than 1024."));
        if (document.Elements.Count > MaxElements)
            issues.Add(Error($"GUI document exceeds the {MaxElements}-element safety limit."));
        if (document.Layers.Count > MaxAuxiliaryEntries || document.Assets.Count > MaxAuxiliaryEntries ||
            document.Bindings.Count > MaxAuxiliaryEntries || document.Styles.Count > MaxAuxiliaryEntries ||
            document.Effects.Count > MaxAuxiliaryEntries || document.States.Count > MaxAuxiliaryEntries)
            issues.Add(Error($"GUI document exceeds the {MaxAuxiliaryEntries}-entry auxiliary collection safety limit."));

        ValidateIds(document.Elements.Select(value => value.Id), "element", issues);
        ValidateIds(document.Layers.Select(value => value.Id), "layer", issues);
        ValidateIds(document.Assets.Select(value => value.Id), "asset", issues);
        ValidateIds(document.Bindings.Select(value => value.Id), "binding", issues);
        ValidateIds(document.Styles.Select(value => value.Id), "style", issues);
        ValidateIds(document.Effects.Select(value => value.Id), "effect", issues);
        ValidateIds(document.States.Select(value => value.Id), "state", issues);

        var layers = document.Layers.Select(value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assets = document.Assets.Select(value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bindings = document.Bindings.Select(value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var styles = document.Styles.Select(value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effects = document.Effects.Select(value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var states = document.States.Select(value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var element in document.Elements)
        {
            if (!Finite(element.X) || !Finite(element.Y) || !FinitePositive(element.Width) || !FinitePositive(element.Height))
            {
                issues.Add(Error($"Element '{element.Id}' has non-finite position or non-positive size."));
                continue;
            }
            if (element.X < 0 || element.Y < 0 || element.X + element.Width > document.Canvas.Width + 0.0001 ||
                element.Y + element.Height > document.Canvas.Height + 0.0001)
                issues.Add(Error($"Element '{element.Id}' exceeds the GUI canvas."));
            if (!string.IsNullOrWhiteSpace(element.LayerId) && !layers.Contains(element.LayerId))
                issues.Add(Error($"Element '{element.Id}' references missing layer '{element.LayerId}'."));
            if (!string.IsNullOrWhiteSpace(element.AssetId) && document.Assets.Count > 0 && !assets.Contains(element.AssetId))
                issues.Add(Error($"Element '{element.Id}' references missing asset '{element.AssetId}'."));
            if (!string.IsNullOrWhiteSpace(element.BindingId) && !bindings.Contains(element.BindingId))
                issues.Add(Error($"Element '{element.Id}' references missing binding '{element.BindingId}'."));
            if (!string.IsNullOrWhiteSpace(element.StyleId) && !styles.Contains(element.StyleId))
                issues.Add(Error($"Element '{element.Id}' references missing style '{element.StyleId}'."));
            foreach (var effect in element.EffectIds.Where(effect => !effects.Contains(effect)))
                issues.Add(Error($"Element '{element.Id}' references missing effect '{effect}'."));
            foreach (var state in element.StateIds.Where(state => !states.Contains(state)))
                issues.Add(Error($"Element '{element.Id}' references missing state '{state}'."));
        }

        foreach (var layer in document.Layers.Where(layer => !string.IsNullOrWhiteSpace(layer.ParentId) && !layers.Contains(layer.ParentId!)))
            issues.Add(Error($"Layer '{layer.Id}' references missing parent layer '{layer.ParentId}'."));
        if (document.States.Count > 0 && document.States.Count(state => state.IsInitial) > 1)
            issues.Add(Error("GUI document may define at most one initial presentation state."));

        if (issues.Count == 0)
            issues.Add(new ForeverGuiContractIssue(ForeverGuiContractSeverity.Info, "Shared GUI document contract is valid."));
        return issues;
    }

    private static void Normalize(ForeverGuiDocument document)
    {
        document.Name ??= "Untitled GUI";
        document.DocumentId ??= string.Empty;
        document.Canvas ??= new ForeverGuiCanvas();
        document.Elements ??= [];
        document.Layers ??= [];
        document.Assets ??= [];
        document.Bindings ??= [];
        document.Styles ??= [];
        document.Effects ??= [];
        document.States ??= [];
        foreach (var element in document.Elements)
        {
            element.Id ??= string.Empty;
            element.Kind ??= string.Empty;
            element.Text ??= string.Empty;
            element.TextAlignment ??= "left";
            element.EffectIds ??= [];
            element.StateIds ??= [];
            element.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var layer in document.Layers) layer.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in document.Assets) asset.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in document.Bindings) binding.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in document.Styles) style.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var effect in document.Effects) effect.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in document.States) state.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateIds(IEnumerable<string> ids, string label, List<ForeverGuiContractIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
                issues.Add(Error($"GUI {label} id cannot be empty."));
            else if (!seen.Add(id))
                issues.Add(Error($"Duplicate GUI {label} id '{id}'."));
        }
    }

    private static ForeverGuiContractIssue Error(string message)
        => new(ForeverGuiContractSeverity.Error, message);

    private static bool Finite(double value) => double.IsFinite(value);
    private static bool FinitePositive(double value) => double.IsFinite(value) && value > 0;

    private static int RequireIntegral(double value, string field)
    {
        if (!double.IsFinite(value))
            throw new InvalidDataException($"{field} must be finite.");
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) > 0.0001 || rounded < int.MinValue || rounded > int.MaxValue)
            throw new InvalidDataException($"{field} cannot be represented by the legacy integer-coordinate model.");
        return (int)rounded;
    }

    private static string ReadString(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static double ReadNumber(Dictionary<string, JsonElement> properties, string key, double fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetDouble(out var result) && double.IsFinite(result)
            ? result
            : fallback;

    private static T? ReadBridge<T>(Dictionary<string, JsonElement>? extensionData) where T : class
    {
        if (extensionData is null || !extensionData.TryGetValue(LegacyBridgeKey, out var value) || value.ValueKind != JsonValueKind.Object)
            return null;
        try
        {
            return value.Deserialize<T>(JsonOptions);
        }
        catch (JsonException)
        {
            // The legacy file remains readable even if bridge metadata from a future version is not.
            // Validation/migration code can still surface the untouched extension value to the user.
            return null;
        }
    }

    private static Dictionary<string, JsonElement>? CloneDictionary(Dictionary<string, JsonElement>? source)
        => source?.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), source.Comparer);

    private static Dictionary<string, JsonElement>? CloneDictionaryExcept(Dictionary<string, JsonElement>? source, string excludedKey)
    {
        if (source is null) return null;
        return source.Where(pair => !pair.Key.Equals(excludedKey, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), source.Comparer);
    }
}

using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine widget palette hosted inside the existing shared Assets tab. It creates ordinary shared
/// GUI elements; resource binding happens in the same inspector through ForeverMachineGuiBindingContext.
/// </summary>
public partial class MainWindow
{
    private Border? _unifiedMachineAssetPalette;

    private sealed record UnifiedMachinePaletteItem(
        string Name,
        string Kind,
        string AssetId,
        double Width,
        double Height,
        string Preview)
    {
        public override string ToString() => Name;
    }

    private static readonly UnifiedMachinePaletteItem[] UnifiedMachinePaletteItems =
    [
        new("Item Input Slot", "item-slot", "asset.input-tray", 18, 18, "minecraft:iron_ingot|1"),
        new("Item Output Slot", "output-slot", "asset.output-tray", 18, 18, "minecraft:gold_ingot|1"),
        new("Storage / Fuel / Upgrade Slot", "item-slot", "asset.input-tray", 18, 18, "minecraft:coal|1"),
        new("Progress Bar", "progress-bar", "asset.screen", 48, 8, "0.65"),
        new("Energy Bar", "energy-bar", "asset.screen", 10, 52, "0.72"),
        new("Fluid Gauge", "fluid-gauge", "asset.fluid-window", 12, 52, "0.55"),
        new("Machine Status", "status", "asset.status-lamp", 12, 12, "true"),
        new("Runtime Value", "value", "asset.screen", 60, 10, "42")
    ];

    private void EnsureUnifiedGuiMachineAssetPalette()
    {
        if (_unifiedMachineAssetPalette is not null || AssetFilterBox.Parent is not StackPanel assetHeader)
            return;

        var card = new Border
        {
            Background = Brush("#142126"),
            BorderBrush = Brush("#35525A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 4, 0, 10),
            Visibility = Visibility.Collapsed
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "MACHINE",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Runtime-backed widgets. Add visually, then choose Bind To in the shared inspector.",
            Foreground = Brush("#82969E"),
            FontSize = 8.8,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 7)
        });
        var wrap = new WrapPanel();
        foreach (var item in UnifiedMachinePaletteItems)
        {
            var button = new Button
            {
                Content = item.Name,
                Tag = item,
                MinWidth = 112,
                Height = 30,
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(7, 3, 7, 3),
                ToolTip = $"Add {item.Name} to the current Machine GUI"
            };
            button.Click += (_, _) => AddUnifiedMachinePaletteItem(item);
            wrap.Children.Add(button);
        }
        stack.Children.Add(wrap);
        card.Child = stack;
        _unifiedMachineAssetPalette = card;
        assetHeader.Children.Add(card);
    }

    private void RefreshUnifiedGuiMachineAssetPalette()
    {
        EnsureUnifiedGuiMachineAssetPalette();
        if (_unifiedMachineAssetPalette is not null)
            _unifiedMachineAssetPalette.Visibility = _unifiedMachineGuiSession is null
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    private void AddUnifiedMachinePaletteItem(UnifiedMachinePaletteItem item)
    {
        if (_unifiedMachineGuiSession is null)
            return;

        try
        {
            var shared = ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);
            var id = CreateUnifiedMachineElementId(shared, item.Kind);
            var element = new ForeverGuiElement
            {
                Id = id,
                Kind = item.Kind,
                X = Math.Max(0, Math.Round((shared.Canvas.Width - item.Width) / 2.0)),
                Y = Math.Max(0, Math.Round((shared.Canvas.Height - item.Height) / 2.0)),
                Width = item.Width,
                Height = item.Height,
                Order = shared.Elements.Count == 0 ? 0 : shared.Elements.Max(candidate => candidate.Order) + 1,
                AssetId = item.AssetId,
                Text = item.Name,
                Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["previewValue"] = JsonSerializer.SerializeToElement(item.Preview)
                }
            };
            shared.Elements.Add(element);
            EnsureSharedAsset(shared, item.AssetId);

            PushUndo();
            var projected = ForeverGuiLegacyEditorProjection.ToLegacyLayout(shared);
            LoadLayoutIntoEditor(projected, null, clearHistory: false);
            var added = _layout.Elements.FirstOrDefault(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (added is not null)
                SetSelection([added]);
            MarkDirty();
            RefreshUnifiedMachineBindingInspector();
            StatusText.Text = $"Added {item.Name}. Choose its Machine binding in the inspector.";
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or JsonException)
        {
            MessageBox.Show(this, ex.Message, "Machine GUI element could not be added", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string CreateUnifiedMachineElementId(ForeverGuiDocument document, string kind)
    {
        var prefix = new string(kind.ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '_').ToArray()).Trim('_');
        if (string.IsNullOrWhiteSpace(prefix)) prefix = "widget";
        var used = document.Elements.Select(element => element.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var id = prefix;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = $"{prefix}_{suffix}";
        return id;
    }
}

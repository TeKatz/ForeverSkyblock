using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Runtime-style preview decoration for Machine widgets hosted on the established GUI canvas.
/// This deliberately follows the Phase-8 generation-detector pattern: normal WPF layout, dragging
/// and animation do not rebuild anything. Decoration runs only when RenderLayout replaces the
/// editable visual generation or when Machine context is explicitly refreshed.
/// </summary>
public partial class MainWindow
{
    private bool _unifiedMachinePreviewInitialized;
    private bool _unifiedMachinePreviewQueued;
    private FrameworkElement? _unifiedMachinePreviewAnchor;
    private int _unifiedMachinePreviewVisualCount = -1;

    public void EnableForeverUnifiedMachineGuiPreview()
    {
        if (_unifiedMachinePreviewInitialized)
            return;
        _unifiedMachinePreviewInitialized = true;
        CaptureUnifiedMachinePreviewGeneration();
        PreviewCanvas.LayoutUpdated += UnifiedMachinePreviewLayoutUpdated;
    }

    private void UnifiedMachinePreviewLayoutUpdated(object? sender, EventArgs e)
    {
        if (!UnifiedMachinePreviewGenerationChanged())
            return;
        CaptureUnifiedMachinePreviewGeneration();
        QueueUnifiedMachinePreviewRefresh();
    }

    private bool UnifiedMachinePreviewGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _unifiedMachinePreviewVisualCount ||
               !ReferenceEquals(anchor, _unifiedMachinePreviewAnchor);
    }

    private void CaptureUnifiedMachinePreviewGeneration()
    {
        _unifiedMachinePreviewVisualCount = _visuals.Count;
        _unifiedMachinePreviewAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private void QueueUnifiedMachinePreviewRefresh()
    {
        if (_unifiedMachinePreviewQueued)
            return;
        _unifiedMachinePreviewQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _unifiedMachinePreviewQueued = false;
            if (!IsLoaded)
                return;
            RefreshUnifiedMachineWidgetVisuals();
            CaptureUnifiedMachinePreviewGeneration();
        }));
    }

    private void RefreshUnifiedMachineWidgetVisuals()
    {
        if (_unifiedMachineGuiSession is null || _visuals.Count == 0)
            return;

        ForeverGuiDocument shared;
        try
        {
            shared = ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            return;
        }

        var elements = shared.Elements.ToDictionary(element => element.Id, StringComparer.OrdinalIgnoreCase);
        var bindings = shared.Bindings.ToDictionary(binding => binding.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border || !elements.TryGetValue(pair.Key.Id, out var element))
                continue;
            if (!IsMachinePreviewKind(element.Kind))
                continue;

            var bindingId = string.IsNullOrWhiteSpace(element.BindingId) ? element.Id : element.BindingId;
            bindings.TryGetValue(bindingId, out var binding);
            var preview = ReadUnifiedPreviewValue(element, binding);
            DecorateUnifiedMachineWidget(border, element.Kind, preview, element.Text);
        }
    }

    private static bool IsMachinePreviewKind(string kind)
        => kind is "item-slot" or "output-slot" or "progress-bar" or "energy-bar" or
                   "fluid-gauge" or "status" or "value";

    private void DecorateUnifiedMachineWidget(Border border, string kind, string preview, string label)
    {
        border.Opacity = 1;
        switch (kind)
        {
            case "item-slot":
            case "output-slot":
                border.Background = Brush("#8B8B8B");
                border.BorderBrush = Brush(kind == "output-slot" ? "#6B7A82" : "#4EB8BC");
                border.Child = UnifiedMachineItemPreview(preview);
                break;
            case "progress-bar":
                border.Background = Brush("#202B31");
                border.BorderBrush = Brush("#40535C");
                border.Child = UnifiedMachineMeter(preview, vertical: false, "#4EB8BC", border.Width, border.Height);
                break;
            case "energy-bar":
                border.Background = Brush("#202B31");
                border.BorderBrush = Brush("#40535C");
                border.Child = UnifiedMachineMeter(preview, vertical: true, "#B58BDA", border.Width, border.Height);
                break;
            case "fluid-gauge":
                border.Background = Brush("#202B31");
                border.BorderBrush = Brush("#40535C");
                border.Child = UnifiedMachineMeter(preview, vertical: true, "#4E9DD4", border.Width, border.Height);
                break;
            case "status":
                border.Background = Brush("#10171B");
                border.BorderBrush = Brush("#40535C");
                border.Child = new TextBlock
                {
                    Text = UnifiedPreviewTruthy(preview) ? "●" : "○",
                    Foreground = Brush(UnifiedPreviewTruthy(preview) ? "#4EB8BC" : "#65747B"),
                    FontSize = Math.Max(7, Math.Min(border.Width, border.Height) * 0.72),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                break;
            case "value":
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(preview) ? label : preview,
                    Foreground = Brush("#303030"),
                    FontSize = 7,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                break;
        }
    }

    private static FrameworkElement UnifiedMachineMeter(
        string preview,
        bool vertical,
        string fillColor,
        double width,
        double height)
    {
        var ratio = ParseUnifiedPreviewRatio(preview);
        var grid = new Grid { Background = Brush("#11181C"), Margin = new Thickness(1) };
        var fill = new Border
        {
            Background = Brush(fillColor),
            HorizontalAlignment = vertical ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
            VerticalAlignment = vertical ? VerticalAlignment.Bottom : VerticalAlignment.Stretch
        };
        if (vertical)
            fill.Height = Math.Max(1, Math.Max(1, height - 4) * ratio);
        else
            fill.Width = Math.Max(1, Math.Max(1, width - 4) * ratio);
        grid.Children.Add(fill);
        return grid;
    }

    private static FrameworkElement UnifiedMachineItemPreview(string preview)
    {
        var parts = preview.Split('|', 2, StringSplitOptions.TrimEntries);
        var item = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : "item";
        var count = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "1";
        var path = item.Contains(':') ? item[(item.IndexOf(':') + 1)..] : item;
        var words = path.Split(['_', '/', '-'], StringSplitOptions.RemoveEmptyEntries);
        var abbreviation = words.Length == 0
            ? "?"
            : string.Concat(words.Select(word => char.ToUpperInvariant(word[0])));
        if (abbreviation.Length > 3) abbreviation = abbreviation[..3];

        var grid = new Grid { Background = Brush("#B7B7B7"), Margin = new Thickness(2) };
        grid.Children.Add(new TextBlock
        {
            Text = abbreviation,
            Foreground = Brush("#303030"),
            FontSize = 6.5,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        grid.Children.Add(new TextBlock
        {
            Text = count,
            Foreground = Brushes.White,
            FontSize = 6.5,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 1, 0)
        });
        return grid;
    }

    private static string ReadUnifiedPreviewValue(ForeverGuiElement element, ForeverGuiBinding? binding)
    {
        if (element.Properties.TryGetValue("previewValue", out var own) && own.ValueKind == JsonValueKind.String)
            return own.GetString() ?? string.Empty;
        if (binding?.Properties.TryGetValue("previewValue", out var bound) == true && bound.ValueKind == JsonValueKind.String)
            return bound.GetString() ?? string.Empty;
        return element.Kind switch
        {
            "item-slot" => "minecraft:iron_ingot|1",
            "output-slot" => "minecraft:gold_ingot|1",
            "progress-bar" => "0.65",
            "energy-bar" => "0.72",
            "fluid-gauge" => "0.55",
            "status" => "true",
            _ => element.Text
        };
    }

    private static double ParseUnifiedPreviewRatio(string value)
    {
        var normalized = value.Trim().TrimEnd('%');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !double.IsFinite(result))
            return 0.65;
        if (value.TrimEnd().EndsWith('%')) result /= 100.0;
        return Math.Clamp(result, 0, 1);
    }

    private static bool UnifiedPreviewTruthy(string value)
        => value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("running", StringComparison.OrdinalIgnoreCase) || value.Equals("active", StringComparison.OrdinalIgnoreCase);
}

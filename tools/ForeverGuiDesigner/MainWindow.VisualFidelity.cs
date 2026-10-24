using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Final visual-fidelity pass applied after the Phase 5-8 decorators. The runtime machine
/// renderer in the mod is the source of truth: slot/button chrome, colors and Washing text
/// metrics mirror MachineSkin/WashingScreen rather than generic WPF controls.
///
/// This deliberately does not rebuild the canvas. It only replaces children inside the
/// already-created element borders so the Phase 8 performance guard remains effective.
/// </summary>
public partial class MainWindow
{
    private const string FidelitySlotTag = "__forever_fidelity_slot";
    private const string FidelityButtonTag = "__forever_fidelity_button";

    private readonly HashSet<Border> _fidelityButtonHooks = [];
    private readonly HashSet<Border> _fidelityHoveredButtons = [];
    private bool _visualFidelityInitialized;
    private bool _visualFidelityQueued;
    private bool _visualFidelityApplying;
    private FrameworkElement? _fidelityVisualAnchor;
    private int _fidelityVisualCount = -1;

    public void EnableForeverVisualFidelityPatch()
    {
        if (_visualFidelityInitialized)
            return;
        _visualFidelityInitialized = true;

        UseLayoutRounding = true;
        PreviewCanvas.SnapsToDevicePixels = true;
        RenderOptions.SetBitmapScalingMode(PreviewCanvas, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(PreviewCanvas, EdgeMode.Aliased);

        ApplyFidelityEditorTheme();
        CaptureFidelityVisualGeneration();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (!_visualFidelityApplying && FidelityVisualGenerationChanged())
                QueueVisualFidelityPass();
        };

        ElementsList.SelectionChanged += (_, _) => QueueVisualFidelityPass();
        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueueVisualFidelityPass();
        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) => QueueVisualFidelityPass();
        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) => QueueVisualFidelityPass();
        if (_phase6Timer is not null)
            _phase6Timer.Tick += (_, _) =>
            {
                if (IsWashingPhase6Active)
                    QueueVisualFidelityPass();
            };

        QueueVisualFidelityPass();
    }

    private void QueueVisualFidelityPass()
    {
        if (_visualFidelityQueued || _visualFidelityApplying)
            return;

        _visualFidelityQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            _visualFidelityQueued = false;
            if (_visualFidelityApplying)
                return;

            _visualFidelityApplying = true;
            try
            {
                ApplyFidelityEditorTheme();
                ApplyFidelityPreview();
                CaptureFidelityVisualGeneration();
            }
            finally
            {
                _visualFidelityApplying = false;
            }
        }));
    }

    private void ApplyFidelityEditorTheme()
    {
        // Windows' default TabItem template paints a light tab header. Explicit header
        // TextBlocks make Elements/Assets readable instead of inheriting the dark theme.
        foreach (var tab in FindVisualChildren<TabItem>(this))
        {
            var text = tab.Header switch
            {
                string value => value,
                TextBlock block => block.Text,
                _ => null
            };
            if (text is not ("Elements" or "Assets"))
                continue;

            tab.Foreground = Brush("#111820");
            if (tab.Header is not TextBlock)
            {
                tab.Header = new TextBlock
                {
                    Text = text,
                    Foreground = Brush("#111820"),
                    FontWeight = FontWeights.SemiBold
                };
            }
            else if (tab.Header is TextBlock block)
            {
                block.Foreground = Brush("#111820");
                block.FontWeight = FontWeights.SemiBold;
            }
        }

        if (_phase8Hierarchy is not null)
        {
            _phase8Hierarchy.Foreground = Brushes.White;
            var itemStyle = new Style(typeof(TreeViewItem));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            _phase8Hierarchy.Resources[typeof(TreeViewItem)] = itemStyle;
            foreach (var item in _phase8Hierarchy.Items.OfType<TreeViewItem>())
                ApplyWhiteHierarchyItem(item);

            if (_phase8Hierarchy.Parent is StackPanel hierarchyPanel)
            {
                foreach (var label in hierarchyPanel.Children.OfType<TextBlock>())
                    label.Foreground = Brushes.White;
            }
        }

        // The Properties/True WYSIWYG column lives in one StackPanel. Theme all labels
        // explicitly because several Windows themes otherwise resolve inherited text to black.
        if (SelectionText.Parent is StackPanel propertyPanel)
        {
            foreach (var text in FindVisualChildren<TextBlock>(propertyPanel))
            {
                if (ReferenceEquals(text, SelectionText))
                    continue;
                text.Foreground = Brushes.White;
            }
            foreach (var check in FindVisualChildren<CheckBox>(propertyPanel))
                check.Foreground = Brushes.White;
            foreach (var expander in FindVisualChildren<Expander>(propertyPanel))
                expander.Foreground = Brushes.White;
        }
    }

    private static void ApplyWhiteHierarchyItem(TreeViewItem item)
    {
        item.Foreground = Brushes.White;
        foreach (var child in item.Items.OfType<TreeViewItem>())
            ApplyWhiteHierarchyItem(child);
    }

    private void ApplyFidelityPreview()
    {
        var washing = IsWashingPhase6Active;
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            switch (model.Type)
            {
                case GuiElementType.Slot:
                    ApplyMachineSlotFidelity(model, border, washing);
                    break;
                case GuiElementType.Button:
                    ApplyMachineButtonFidelity(model, border, washing);
                    break;
                case GuiElementType.Text:
                    ApplyMachineTextFidelity(model, border, washing);
                    break;
            }
        }
    }

    private void ApplyMachineSlotFidelity(GuiElementModel model, Border border, bool washing)
    {
        var machine = model.Width >= 24 || model.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase);
        var accent = ResolveFidelityAccent();
        var signature = string.Join('|', FidelitySlotTag, machine, accent,
            model.ItemId ?? string.Empty, model.StackCount,
            model.Durability.ToString("0.###", CultureInfo.InvariantCulture),
            model.GhostPreview, model.GlintPreview, model.Width, model.Height);

        if (border.Child is Canvas existing && string.Equals(existing.Tag as string, signature, StringComparison.Ordinal))
        {
            ApplyFidelityOuterBorder(model, border);
            return;
        }

        var width = Math.Max(1, model.Width);
        var height = Math.Max(1, model.Height);
        var canvas = new Canvas
        {
            Tag = signature,
            Width = width,
            Height = height,
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);
        RenderOptions.SetBitmapScalingMode(canvas, BitmapScalingMode.NearestNeighbor);

        AddFidelityRect(canvas, 0, 0, width, height, "#070F18");
        AddFidelityRect(canvas, 1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2), machine ? "#6B818B" : "#425A68");
        AddFidelityRect(canvas, 2, 2, Math.Max(1, width - 4), Math.Max(1, height - 4), "#0D1B26");
        AddFidelityRect(canvas, 3, 3, Math.Max(1, width - 6), 1, "#09131E");
        AddFidelityRect(canvas, 3, Math.Max(0, height - 3), Math.Max(1, width - 6), 1, "#2F4552");

        if (machine)
        {
            AddFidelityRect(canvas, 1, 1, Math.Min(5, Math.Max(1, width - 2)), 1, accent);
            AddFidelityRect(canvas, Math.Max(1, width - 6), Math.Max(0, height - 2), Math.Min(5, Math.Max(1, width - 2)), 1, accent);
        }

        AddFidelityItemPreview(canvas, model, machine);
        border.Child = canvas;
        border.Background = Brushes.Transparent;
        ApplyFidelityOuterBorder(model, border);
    }

    private void AddFidelityItemPreview(Canvas canvas, GuiElementModel model, bool machine)
    {
        if (string.IsNullOrWhiteSpace(model.ItemId) || string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;

        var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap(model.ItemId);
        if (bitmap is null)
            return;

        var inset = machine ? 4 : 2;
        var size = Math.Max(1, Math.Min(16, Math.Min(model.Width - inset * 2, model.Height - inset * 2)));
        var image = new Image
        {
            Source = bitmap,
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            Opacity = model.GhostPreview ? .45 : 1,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(image, Math.Round((model.Width - size) / 2.0));
        Canvas.SetTop(image, Math.Round((model.Height - size) / 2.0));
        canvas.Children.Add(image);

        if (model.GlintPreview)
        {
            var glint = new Border
            {
                Width = size,
                Height = size,
                BorderBrush = Brush("#9C78D8"),
                BorderThickness = new Thickness(1),
                Opacity = .7,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(glint, Math.Round((model.Width - size) / 2.0));
            Canvas.SetTop(glint, Math.Round((model.Height - size) / 2.0));
            canvas.Children.Add(glint);
        }

        if (model.StackCount > 1)
        {
            var stack = new MinecraftPixelText(model.StackCount.ToString(CultureInfo.InvariantCulture), Brushes.White, true, .75, "right")
            {
                Width = Math.Max(8, model.Width - 2),
                Height = 8
            };
            Canvas.SetLeft(stack, 0);
            Canvas.SetTop(stack, Math.Max(0, model.Height - 8));
            canvas.Children.Add(stack);
        }

        if (model.Durability >= 0)
        {
            var barWidth = Math.Max(1, Math.Min(13, model.Width - 4));
            AddFidelityRect(canvas, 2, Math.Max(0, model.Height - 3), barWidth, 2, "#1A1A1A");
            AddFidelityRect(canvas, 2, Math.Max(0, model.Height - 3), barWidth * Math.Clamp(model.Durability, 0, 1), 1, "#55FF55");
        }
    }

    private void ApplyMachineButtonFidelity(GuiElementModel model, Border border, bool washing)
    {
        if (_fidelityButtonHooks.Add(border))
        {
            border.MouseEnter += (_, _) =>
            {
                _fidelityHoveredButtons.Add(border);
                QueueVisualFidelityPass();
            };
            border.MouseLeave += (_, _) =>
            {
                _fidelityHoveredButtons.Remove(border);
                QueueVisualFidelityPass();
            };
        }

        var hover = _fidelityHoveredButtons.Contains(border);
        var accent = ResolveFidelityAccent();
        var signature = string.Join('|', FidelityButtonTag, model.Content, model.Width, model.Height, hover, accent);
        if (border.Child is Canvas existing && string.Equals(existing.Tag as string, signature, StringComparison.Ordinal))
        {
            ApplyFidelityOuterBorder(model, border);
            return;
        }

        var width = Math.Max(1, model.Width);
        var height = Math.Max(1, model.Height);
        var canvas = new Canvas
        {
            Tag = signature,
            Width = width,
            Height = height,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);

        AddFidelityRect(canvas, 0, 0, width, height, "#08131D");
        AddFidelityRect(canvas, 1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2), hover ? accent : "#6A818C");
        var inner = new Border
        {
            Width = Math.Max(1, width - 4),
            Height = Math.Max(1, height - 4),
            Background = new LinearGradientBrush(
                Brush(hover ? "#37515E" : "#263B4B").Color,
                Brush("#172936").Color,
                90),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(inner, 2);
        Canvas.SetTop(inner, 2);
        canvas.Children.Add(inner);
        if (hover && width > 10 && height >= 4)
            AddFidelityRect(canvas, 5, height - 3, width - 10, 1, accent);

        var text = new MinecraftPixelText(
            string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content,
            Brush("#E6EEE9"),
            false,
            .92,
            "center")
        {
            Width = Math.Max(1, width - 10),
            Height = 8,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(text, 5);
        Canvas.SetTop(text, Math.Max(0, Math.Round((height - 8) / 2.0)));
        canvas.Children.Add(text);

        border.Child = canvas;
        border.Background = Brushes.Transparent;
        ApplyFidelityOuterBorder(model, border);
    }

    private void ApplyMachineTextFidelity(GuiElementModel model, Border border, bool washing)
    {
        var text = string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content;
        var color = "#E6EEE9";
        var scale = model.TextScale;
        var alignment = model.TextAlignment;
        var shadow = false;

        if (washing)
        {
            switch (model.Id)
            {
                case "screen_title":
                    color = "#E6EEE9";
                    scale = 1.05;
                    alignment = "left";
                    text = "WASHING";
                    break;
                case "forever_phase6_washing_status_text":
                    text = CurrentWashingStatus switch
                    {
                        "blocked" => "OUTPUT FULL",
                        "no_water" => "NO WATER",
                        "idle" => "IDLE",
                        _ => "WASHING"
                    };
                    color = WashingBlocked || !WashingHasWater ? "#FFBF76" : WashingRunning ? "#76D8D2" : "#A0B4BE";
                    scale = .73;
                    alignment = "left";
                    break;
                case "forever_phase6_washing_water_text":
                    text = WashingHasWater ? "WATER CONNECTED" : "WATER REQUIRED";
                    color = WashingHasWater ? "#9DDDF0" : "#FFBF76";
                    scale = .74;
                    alignment = "center";
                    break;
                case "forever_phase6_washing_label_input":
                case "forever_phase6_washing_label_output":
                    color = "#A0B4BE";
                    scale = .73;
                    alignment = "center";
                    break;
                case "forever_phase6_washing_time":
                    text = $"{CurrentProgress * 5.0:0.0} / 5.0 s";
                    color = "#A0B4BE";
                    scale = .65;
                    alignment = "center";
                    break;
                case "forever_phase6_washing_recipe_summary":
                    color = "#E6EEE9";
                    scale = .68;
                    alignment = "center";
                    break;
                case "forever_phase6_washing_hint_source":
                case "forever_phase6_washing_hint_water":
                    color = "#A0B4BE";
                    scale = .72;
                    alignment = "left";
                    break;
            }
        }
        else if (model.Id.Contains("label_", StringComparison.OrdinalIgnoreCase))
        {
            color = "#A0B4BE";
        }

        var required = EstimatePixelTextWidth(text, scale);
        border.ToolTip = required > model.Width + .01
            ? $"Text overflow: ~{required:0.#} px required, {model.Width} px available."
            : null;

        border.Child = new MinecraftPixelText(text, Brush(color), shadow, scale, alignment)
        {
            Width = Math.Max(1, model.Width),
            Height = Math.Max(1, model.Height)
        };
        border.Background = Brushes.Transparent;
        ApplyFidelityOuterBorder(model, border);
    }

    private void ApplyFidelityOuterBorder(GuiElementModel model, Border border)
    {
        if (_selection.Contains(model))
        {
            border.BorderBrush = Brush("#2E9BFF");
            border.BorderThickness = new Thickness(1.5);
        }
        else
        {
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0);
        }
    }

    private string ResolveFidelityAccent()
    {
        if (IsWashingPhase6Active)
            return WashingHasWater ? "#9DDDF0" : "#FFBF76";
        return _activeForeverTemplate?.ScreenClass switch
        {
            "AscensionScreen" => "#BCA4F3",
            "RefiningScreen" => "#76D8D2",
            "CrucibleScreen" => "#76D8D2",
            _ => "#76D8D2"
        };
    }

    private static double EstimatePixelTextWidth(string text, double scale)
    {
        var pixel = Math.Max(.5, Math.Clamp(scale, .25, 4));
        if (string.IsNullOrEmpty(text))
            return 0;
        var width = 0.0;
        foreach (var ch in text)
            width += ch == ' ' ? 4 * pixel : 6 * pixel;
        return Math.Max(0, width - pixel);
    }

    private static void AddFidelityRect(Canvas canvas, double x, double y, double width, double height, string color)
    {
        if (width <= 0 || height <= 0)
            return;
        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brush(color),
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }

    private bool FidelityVisualGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _fidelityVisualCount || !ReferenceEquals(anchor, _fidelityVisualAnchor);
    }

    private void CaptureFidelityVisualGeneration()
    {
        _fidelityVisualCount = _visuals.Count;
        _fidelityVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}

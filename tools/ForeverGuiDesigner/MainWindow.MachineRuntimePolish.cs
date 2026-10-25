using System.Globalization;
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
/// Final source-faithful visual pass for the non-washing machine previews.
///
/// The runtime controller owns state/progress/visibility. This pass deliberately owns only
/// presentation and mirrors the coordinates/colors used by the corresponding Minecraft
/// MachineScreen implementations. Keeping it separate makes it possible to compare the
/// designer preview directly against the Java source instead of growing another generic
/// approximation layer.
/// </summary>
public partial class MainWindow
{
    private const string MachineRuntimePolishTag = "__forever_machine_runtime_polish_v1";
    private bool _machineRuntimePolishInitialized;
    private bool _machineRuntimePolishQueued;
    private bool _machineRuntimePolishApplying;
    private string? _machineRuntimePolishSignature;
    private readonly HashSet<Border> _machineRuntimePolishButtonHooks = [];

    public void EnableMachineRuntimeVisualPolish()
    {
        if (_machineRuntimePolishInitialized)
            return;

        _machineRuntimePolishInitialized = true;
        PreviewCanvas.LayoutUpdated += (_, _) => QueueMachineRuntimeVisualPolish();
        if (_phase8TimelineTimer is not null)
            _phase8TimelineTimer.Tick += (_, _) => QueueMachineRuntimeVisualPolish();
        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueMachineRuntimeVisualPolish();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueueMachineRuntimeVisualPolish();
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueueMachineRuntimeVisualPolish();
        if (_phase8TimelineSlider is not null)
            _phase8TimelineSlider.ValueChanged += (_, _) => QueueMachineRuntimeVisualPolish();

        QueueMachineRuntimeVisualPolish();
    }

    private void QueueMachineRuntimeVisualPolish()
    {
        if (_machineRuntimePolishQueued || _machineRuntimePolishApplying)
            return;

        _machineRuntimePolishQueued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _machineRuntimePolishQueued = false;
            ApplyMachineRuntimeVisualPolish();
        }, DispatcherPriority.Render);
    }

    private void ApplyMachineRuntimeVisualPolish()
    {
        if (_machineRuntimePolishApplying)
            return;

        if (!IsExclusiveNonWashingMachineRuntimeActive)
        {
            RemoveMachineRuntimePolishOverlay();
            _machineRuntimePolishSignature = null;
            return;
        }

        _machineRuntimePolishApplying = true;
        try
        {
            ApplySourceMachineGeometry();
            HideApproximationTextures();
            ApplySourceMachineSlotStyling();
            ApplySourceMachineButtonStyling();
            HideLegacyInventoryLabels();

            var signature = string.Join('|',
                ActiveRuntimeScreen,
                CurrentTier,
                CurrentMode,
                CurrentMachineRuntimeState,
                _phase8TimelineProgress.ToString("0.000", CultureInfo.InvariantCulture),
                _layout.Width,
                _layout.Height,
                _visuals.Count);

            if (!string.Equals(signature, _machineRuntimePolishSignature, StringComparison.Ordinal) ||
                !PreviewCanvas.Children.OfType<FrameworkElement>().Any(e => Equals(e.Tag, MachineRuntimePolishTag)))
            {
                RebuildMachineRuntimePolishOverlay();
                _machineRuntimePolishSignature = signature;
            }
        }
        finally
        {
            _machineRuntimePolishApplying = false;
        }
    }

    private void ApplySourceMachineGeometry()
    {
        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen":
                SourceText("screen_title", 42, 12, 128, 10, 1.05, "left", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "status_text", 186, 12, 44, 8, .68, "left", StatusColor());
                SourceText(MachineRuntimePrefix + "heat_text", 22, 40, 204, 9, .76, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "tank_text", 22, 63, 95, 9, .68, "center", "#76D8D2");
                SourceText(MachineRuntimePrefix + "freezer_text", 131, 63, 95, 9, .66, "center", "#76D8D2");
                SourceText(MachineRuntimePrefix + "input_label", 20, 89, 60, 9, .70, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "output_label", 166, 89, 60, 9, .70, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "lava_text", 88, 129, 72, 8, .64, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "melt_label", 54, 133, 48, 7, .56, "center", CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal) ? "#FFBF76" : "#A0B4BE");
                SourceText(MachineRuntimePrefix + "freeze_label", 144, 133, 52, 7, .56, "center", CurrentMachineRuntimeState == "freezing" ? "#76D8D2" : "#A0B4BE");
                SourceText(MachineRuntimePrefix + "recipe_summary", 19, 178, 210, 8, .60, "center", "#A0B4BE");
                CollapseModel(MachineRuntimePrefix + "time_text");
                SourceButtonPosition("modeButton", 80, 151, 88, 22, CurrentMachineRuntimeState == "freezing" ? "Output: Obsidian" : "Output: Lava");
                SourceButtonPosition("recipes", 174, 151, 56, 22, "Recipes");
                break;

            case "RefiningScreen":
                SourceText("screen_title", 42, 12, 130, 10, 1.05, "left", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "status_text", 186, 12, 43, 8, .73, "left", StatusColor());
                SourceText(MachineRuntimePrefix + "energy_text", 22, 38, 204, 9, .70, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "material_label", 23, 65, 54, 9, .73, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "catalyst_label", 63, 65, 54, 9, .73, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "output_label", 155, 65, 54, 9, .73, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "time_text", 106, 104, 60, 8, .65, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "info_primary", 25, 145, 132, 8, .67, "left", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "info_secondary", 25, 157, 132, 8, .67, "left", "#A0B4BE");
                CollapseModel(MachineRuntimePrefix + "recipe_summary");
                SourceButtonPosition("recipes", 164, 144, 64, 22, "Recipes");
                break;

            case "AscensionScreen":
                SourceText("screen_title", 41, 12, 142, 10, 1.05, "left", "#BCA4F3");
                SourceText(MachineRuntimePrefix + "status_text", 193, 13, 35, 8, .73, "center", "#E6EEE9");
                // Source is centered at x=62, y=32. This intentionally sits higher than the old generic preview.
                SourceText(MachineRuntimePrefix + "core_label", 14, 32, 96, 9, .85, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "arrow", 110, 59, 25, 18, 2.10, "left", "#BCA4F3");
                SourceText(MachineRuntimePrefix + "reward", 154, 53, 60, 18, 1.60, "center", "#BCA4F3");
                SourceText(MachineRuntimePrefix + "perk_label", 154, 72, 61, 8, .73, "center", "#BCA4F3");
                SourceText(MachineRuntimePrefix + "conversion", 19, 94, 210, 9, .80, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "time_text", 176, 103, 50, 9, .85, "left", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "power_text", 176, 112, 50, 8, .65, "left", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "recipient", 40, 125, 117, 8, .77, "left", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "balance", 160, 125, 64, 8, .72, "left", "#BCA4F3");
                SourceText(MachineRuntimePrefix + "hint", 19, 164, 210, 8, .68, "center", "#A0B4BE");
                SourceButtonPosition("start", 20, 141, 208, 20,
                    CurrentMachineRuntimeState == "processing" ? "CONVERTING…" : "CONSUME CORE • GAIN 1 PP");
                break;

            case "CondenserScreen":
                SourceText("screen_title", 42, 12, 126, 10, 1.10, "left", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "status_text", 186, 12, 43, 8, .73, "left", StatusColor());
                SourceText(MachineRuntimePrefix + "module_state", 46, 34, 156, 8, .72, "center", "#76D8D2");
                SourceText(MachineRuntimePrefix + "garden_connected", 46, 34, 156, 8, .72, "center", "#8FE59B");
                SourceText(MachineRuntimePrefix + "input_label", 17, 48, 43, 9, .73, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "module_input_label", 64, 48, 49, 9, .73, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "output_label", 190, 48, 41, 9, .73, "center", "#A0B4BE");

                var mixing = CurrentMode == "mixing";
                var barX = mixing ? 111 : 83;
                var barW = mixing ? 70 : 98;
                SourceElementPosition(MachineRuntimePrefix + "process_scene", barX, 56, barW, 27);
                SourceText(MachineRuntimePrefix + "time_text", barX - 2, 80, barW + 4, 8, .65, "center", "#A0B4BE");
                SourceElementPosition(MachineRuntimePrefix + "process_meter", barX + 1, 88, Math.Max(1, barW - 2), 3);
                SourceText(MachineRuntimePrefix + "recipe_summary", 20, 95, 207, 9, .84, "left", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "freezing_label", 29, 116, 190, 8, .72, "center", "#76D8D2");
                SourceText(MachineRuntimePrefix + "freezing_text", 65, 143, 118, 8, .70, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "module_hint", 22, 190, 204, 8, .67, "center", "#A0B4BE");
                SourceText(MachineRuntimePrefix + "upgrade_label", 22, 207, 96, 8, .70, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "policies_label", 128, 207, 98, 8, .70, "center", "#E6EEE9");
                SourceText(MachineRuntimePrefix + "garden_time", 61, 134, 126, 8, .68, "center", "#CAE0CE");
                SourceText(MachineRuntimePrefix + "garden_specimen", 7, 139, 62, 8, .69, "center", "#BFD6C4");
                SourceText(MachineRuntimePrefix + "garden_harvest", 158, 139, 75, 8, .69, "center", "#BFD6C4");
                SourceText(MachineRuntimePrefix + "garden_routes", 22, 185, 204, 7, .60, "center", "#D0E7D4");
                SourceButtonPosition("filters", 20, 162, 120, 20, "Edit input locks");
                SourceButtonPosition("recipes", CurrentMode == "garden" ? 94 : 168, CurrentMode == "garden" ? 151 : 162, 60, 20, "Recipes");
                break;
        }
    }

    private void SourceText(string id, double x, double y, double width, double height, double scale, string alignment, string color)
    {
        if (FindVisual(id) is not { Model: var model, Border: var border })
            return;

        model.X = (int)Math.Round(x);
        model.Y = (int)Math.Round(y);
        model.Width = Math.Max(1, (int)Math.Round(width));
        model.Height = Math.Max(1, (int)Math.Round(height));
        model.TextScale = scale;
        model.TextAlignment = alignment;
        model.TextShadow = false;

        border.Width = width;
        border.Height = height;
        border.Background = Brushes.Transparent;
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
        border.Child = new MinecraftPixelText(
            string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content,
            Brush(color), false, scale, alignment)
        {
            Width = Math.Max(1, width),
            Height = Math.Max(1, height),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
    }

    private void SourceElementPosition(string id, double x, double y, double width, double height)
    {
        if (FindVisual(id) is not { Model: var model, Border: var border })
            return;
        model.X = (int)Math.Round(x);
        model.Y = (int)Math.Round(y);
        model.Width = Math.Max(1, (int)Math.Round(width));
        model.Height = Math.Max(1, (int)Math.Round(height));
        border.Width = width;
        border.Height = height;
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
    }

    private void SourceButtonPosition(string id, int x, int y, int width, int height, string? content = null)
    {
        if (FindVisual(id) is not { Model: var model, Border: var border })
            return;
        model.X = x;
        model.Y = y;
        model.Width = width;
        model.Height = height;
        if (!string.IsNullOrWhiteSpace(content))
            model.Content = content;
        border.Width = width;
        border.Height = height;
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
    }

    private (GuiElementModel Model, Border Border)? FindVisual(string id)
    {
        var model = _layout.Elements.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        if (model is null || !_visuals.TryGetValue(model, out var visual) || visual is not Border border)
            return null;
        return (model, border);
    }

    private void CollapseModel(string id)
    {
        if (FindVisual(id) is { Border: var border })
            border.Visibility = Visibility.Collapsed;
    }

    private void HideLegacyInventoryLabels()
    {
        foreach (var pair in _visuals)
        {
            if (pair.Key.Type != GuiElementType.Text)
                continue;
            if (pair.Key.Id.Contains("inventory_label", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key.Content?.Trim(), "Inventory", StringComparison.OrdinalIgnoreCase))
                pair.Value.Visibility = Visibility.Collapsed;
        }
    }

    private void HideApproximationTextures()
    {
        foreach (var pair in _visuals)
        {
            if (!pair.Key.Id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase))
                continue;
            if (pair.Key.Type == GuiElementType.Texture)
                pair.Value.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplySourceMachineSlotStyling()
    {
        foreach (var pair in _visuals)
        {
            if (pair.Key.Type != GuiElementType.Slot || pair.Value is not Border border || pair.Value.Visibility != Visibility.Visible)
                continue;
            ApplyMachineSlotFidelity(pair.Key, border, false);
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0);
        }
    }

    private void ApplySourceMachineButtonStyling()
    {
        foreach (var pair in _visuals)
        {
            if (pair.Key.Type != GuiElementType.Button || pair.Value is not Border border || pair.Value.Visibility != Visibility.Visible)
                continue;
            ApplySourceMachineButton(pair.Key, border, border.IsMouseOver);
            if (_machineRuntimePolishButtonHooks.Add(border))
            {
                border.MouseEnter += (_, _) =>
                {
                    if (IsExclusiveNonWashingMachineRuntimeActive && border.Tag is GuiElementModel model)
                        ApplySourceMachineButton(model, border, true);
                };
                border.MouseLeave += (_, _) =>
                {
                    if (IsExclusiveNonWashingMachineRuntimeActive && border.Tag is GuiElementModel model)
                        ApplySourceMachineButton(model, border, false);
                };
            }
        }
    }

    private void ApplySourceMachineButton(GuiElementModel model, Border border, bool hover)
    {
        var primary = (ActiveRuntimeScreen == "AscensionScreen" && model.Id.Equals("start", StringComparison.OrdinalIgnoreCase)) ||
                      (ActiveRuntimeScreen == "CondenserScreen" && model.Id.Equals("filters", StringComparison.OrdinalIgnoreCase)) ||
                      (ActiveRuntimeScreen == "CrucibleScreen" && model.Id.Contains("mode", StringComparison.OrdinalIgnoreCase));
        var accent = SourceFrameAccent();
        var width = Math.Max(1, model.Width);
        var height = Math.Max(1, model.Height);
        var canvas = new Canvas { Width = width, Height = height, ClipToBounds = true, IsHitTestVisible = false };
        AddPolishRect(canvas, 0, 0, width, height, "#08131D");
        AddPolishRect(canvas, 1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2), hover ? accent : "#6A818C");
        AddPolishGradient(canvas, 2, 2, Math.Max(1, width - 4), Math.Max(1, height - 4), hover ? "#37515E" : "#263B4B", "#172936");
        if (primary)
            AddPolishRect(canvas, 2, 2, 2, Math.Max(1, height - 4), accent);
        if (hover && width > 10 && height >= 4)
            AddPolishRect(canvas, 5, height - 3, width - 10, 1, accent);
        var text = new MinecraftPixelText(string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content, Brush("#E6EEE9"), false, .92, "center")
        {
            Width = Math.Max(1, width - 10),
            Height = 8,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(text, 5);
        Canvas.SetTop(text, Math.Max(0, Math.Round((height - 8) / 2.0)));
        canvas.Children.Add(text);
        border.Background = Brushes.Transparent;
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
        border.Child = canvas;
    }

    private void RebuildMachineRuntimePolishOverlay()
    {
        RemoveMachineRuntimePolishOverlay();
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        var canvas = new Canvas
        {
            Tag = MachineRuntimePolishTag,
            Width = _layout.Width,
            Height = _layout.Height,
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);
        Panel.SetZIndex(canvas, -940);

        DrawSourceFrame(canvas, SourceFrameAccent());
        DrawHeaderIcon(canvas);

        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen": DrawCrucibleSource(canvas); break;
            case "RefiningScreen": DrawRefiningSource(canvas); break;
            case "AscensionScreen": DrawAscensionSource(canvas); break;
            case "CondenserScreen": DrawCondenserSource(canvas); break;
        }

        DrawSourceInventory(canvas);
        PreviewCanvas.Children.Insert(0, canvas);
    }

    private void RemoveMachineRuntimePolishOverlay()
    {
        foreach (var child in PreviewCanvas.Children.OfType<FrameworkElement>().Where(e => Equals(e.Tag, MachineRuntimePolishTag)).ToList())
            PreviewCanvas.Children.Remove(child);
    }

    private void DrawSourceFrame(Canvas c, string accent)
    {
        var w = _layout.Width;
        var h = _layout.Height;
        AddPolishRect(c, 3, 4, w, h, "#66050910");
        AddPolishRect(c, 3, 0, Math.Max(1, w - 6), h, "#0C151F");
        AddPolishRect(c, 0, 3, w, Math.Max(1, h - 6), "#0C151F");
        AddPolishRect(c, 1, 4, Math.Max(1, w - 2), Math.Max(1, h - 8), "#425764");
        AddPolishRect(c, 4, 1, Math.Max(1, w - 8), Math.Max(1, h - 2), "#425764");
        AddPolishGradient(c, 3, 4, Math.Max(1, w - 6), Math.Max(1, h - 8), "#253746", "#15232F");
        AddPolishRect(c, 8, 7, Math.Max(1, w - 16), 20, "#101D29");
        AddPolishRect(c, 14, 27, Math.Max(1, w - 28), 1, "#526979");
        AddPolishRect(c, 14, 27, 52, 1, accent);
        AddPolishRect(c, 10, 33, 1, Math.Max(1, h - 45), "#344A58");
        AddPolishRect(c, w - 11, 33, 1, Math.Max(1, h - 45), "#344A58");
        foreach (var dx in new[] { 5.0, w - 7.0 })
        foreach (var dy in new[] { 5.0, h - 7.0 })
        {
            AddPolishRect(c, dx, dy, 2, 2, "#0C1923");
            AddPolishRect(c, dx, dy, 1, 1, "#91A5AE");
        }
        AddPolishRect(c, w / 2.0 - 14, h - 4, 28, 1, accent);
    }

    private void DrawSourcePanel(Canvas c, double x, double y, double w, double h, string edge)
    {
        if (w < 3 || h < 3)
            return;
        AddPolishRect(c, x, y, w, h, "#0A151F");
        AddPolishRect(c, x + 1, y + 1, w - 2, h - 2, edge);
        AddPolishGradient(c, x + 2, y + 2, w - 4, h - 4, "#1B2C39", "#12212D");
        AddPolishRect(c, x + 3, y + 1, w - 6, 1, "#637884");
    }

    private void DrawSourceInventory(Canvas c)
    {
        var inventoryY = ActiveRuntimeScreen switch
        {
            "CondenserScreen" => 260,
            "AscensionScreen" => 176,
            _ => 190
        };
        var hotbarY = ActiveRuntimeScreen switch
        {
            "CondenserScreen" => 324,
            "AscensionScreen" => 240,
            _ => 254
        };
        DrawSourcePanel(c, 18, inventoryY - 6, 212, hotbarY - inventoryY + 28, "#405866");
        AddPolishRect(c, 24, hotbarY - 6, _layout.Width - 48, 1, "#344C5B");
    }

    private void DrawHeaderIcon(Canvas c)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;
        var itemId = ActiveRuntimeScreen switch
        {
            "CrucibleScreen" => "forever_skyblock:crucible",
            "RefiningScreen" => "forever_skyblock:refining_chamber",
            "AscensionScreen" => "forever_skyblock:ascension_chamber",
            "CondenserScreen" => CurrentTier == "mk2" ? "forever_skyblock:condenser_mark_ii" : "forever_skyblock:condenser_mark_i",
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(itemId))
            return;
        var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap(itemId);
        if (bitmap is null)
            return;
        var image = new Image { Source = bitmap, Width = 16, Height = 16, Stretch = Stretch.Uniform, SnapsToDevicePixels = true, IsHitTestVisible = false };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(image, 18);
        Canvas.SetTop(image, 10);
        c.Children.Add(image);
    }

    private void DrawCrucibleSource(Canvas c)
    {
        DrawStatusDot(c, StatusColor());
        DrawSourcePanel(c, 18, 35, 212, 18, CurrentMachineRuntimeState == "no_heat" ? "#4B4F54" : "#5A4536");
        DrawSourcePanel(c, 18, 58, 103, 18, "#355B66");
        DrawSourcePanel(c, 127, 58, 103, 18, "#355B66");
        DrawSourcePanel(c, 18, 81, 212, 70, "#54616D");

        DrawSourcePanel(c, 111, 95, 26, 32, "#3D454B");
        AddPolishRect(c, 113, 106, 22, 19, "#FF6A20");
        AddPolishRect(c, 114, 106, 20, 1, "#FFB34D");

        AddPolishRect(c, 46, 141, 64, 4, "#637888");
        AddPolishRect(c, 138, 141, 64, 4, "#637888");
        var melting = CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal);
        var freezing = CurrentMachineRuntimeState == "freezing";
        if (melting)
            AddPolishRect(c, 46, 141, 64 * _phase8TimelineProgress, 4, "#FF6A20");
        if (freezing)
            AddPolishRect(c, 138, 141, 64 * _phase8TimelineProgress, 4, "#76D8D2");
    }

    private void DrawRefiningSource(Canvas c)
    {
        DrawStatusDot(c, StatusColor());
        DrawSourcePanel(c, 18, 35, 212, 20, "#465A68");
        AddPolishRect(c, 23, 49, 202, 3, "#2D3843");
        AddPolishRect(c, 23, 49, 101, 3, "#BCA4F3");
        DrawSourcePanel(c, 18, 60, 212, 72, "#54616D");
        DrawRefiningProcess(c, 111, 75, 50);
        AddPolishRect(c, 111, 115, 50, 3, "#07141E");
        AddPolishRect(c, 111, 115, 50 * _phase8TimelineProgress, 3, "#BCA4F3");
        DrawSourcePanel(c, 18, 138, 212, 34, "#465A68");
    }

    private void DrawRefiningProcess(Canvas c, double x, double y, double w)
    {
        AddPolishGradient(c, x, y, w, 27, "#121E30", "#273047");
        var cx = x + w / 2;
        var cy = y + 13;
        AddRotatingRing(c, cx, cy, 18, 18, "#6E80A2", 1, _phase8TimelineProgress * 360 + 45);
        AddRotatingRing(c, cx, cy, 8, 8, CurrentMachineRuntimeState is "blocked" or "no_power" ? "#E4B47C" : "#BCA4F3", 1, -_phase8TimelineProgress * 360 + 45);
        AddPolishRect(c, cx - 2, cy - 2, 4, 4, CurrentMachineRuntimeState == "running" ? "#E6DCFF" : "#7F8CA7");
    }

    private void DrawAscensionSource(Canvas c)
    {
        DrawSourcePanel(c, 190, 9, 40, 15, "#615372");
        DrawSourcePanel(c, 149, 46, 70, 41, "#8E4FD2");
        DrawSourcePanel(c, 23, 106, 146, 8, "#697487");
        AddPolishRect(c, 25, 108, 142 * _phase8TimelineProgress, 4, "#BCA4F3");
        DrawSourcePanel(c, 20, 120, 208, 17, "#576073");

        AddRotatingRing(c, 62, 66, 36, 36, "#6F7C8D", 2, _phase8TimelineProgress * 360);
        AddRotatingRing(c, 62, 66, 26, 26, "#6E449A", 2, -_phase8TimelineProgress * 360);

        // 11 px player-face footprint from PlayerFaceRenderer; neutral pixels keep the source geometry visible.
        AddPolishRect(c, 24, 123, 11, 11, "#49372D");
        AddPolishRect(c, 25, 124, 9, 4, "#C99B76");
        AddPolishRect(c, 26, 126, 2, 1, "#E8E8E8");
        AddPolishRect(c, 31, 126, 2, 1, "#E8E8E8");
    }

    private void DrawCondenserSource(Canvas c)
    {
        DrawStatusDot(c, StatusColor());
        if (CurrentMode == "garden")
        {
            DrawSourcePanel(c, 42, 31, 164, 12, "#315B45");
            DrawGardenScene(c);
            DrawSourcePanel(c, 18, 183, 212, 16, "#466252");
            AddOverlayText(c, "Garden output routes", 22, 185, 204, 7, .60, "center", "#D0E7D4");
            AddOverlayText(c, "Outputs stay separated by route", 22, 192, 204, 7, .60, "center", "#B4D1BC");
        }
        else
        {
            DrawSourcePanel(c, 42, 31, 164, 12, "#365A66");
            DrawSourcePanel(c, 14, 44, 220, 64, "#54616D");
            DrawCondenserProcess(c);
            DrawSourcePanel(c, 14, 113, 220, 43, "#465A68");
            if (CurrentMode == "freezing")
            {
                DrawSourcePanel(c, 62, 132, 124, 8, "#4E6879");
                AddPolishRect(c, 64, 134, 120 * _phase8TimelineProgress, 4, "#76D8D2");
            }
            DrawSourcePanel(c, 18, 187, 212, 12, "#5D6875");
        }

        DrawSourcePanel(c, 18, 203, 212, 51, CurrentTier == "mk2" ? "#4B5E6E" : "#3E454C");
        if (CurrentTier != "mk2")
        {
            AddOverlayText(c, "MK II automation upgrades unavailable", 27, 222, 194, 8, .72, "center", "#A0B4BE");
        }
        else
        {
            AddOverlayText(c, CurrentMode == "garden" ? "Primary" : "Main", 126, 216, 49, 7, .58, "center", "#A0B4BE");
            if (CurrentMode == "garden" || CurrentMode == "freezing")
                AddOverlayText(c, CurrentMode == "garden" ? "Secondary" : "Freezing", 179, 216, 49, 7, .58, "center", "#A0B4BE");
            AddOverlayText(c, "S0  G0  B0", 22, 247, 96, 7, .62, "center", "#B7C8D4");
        }
    }

    private void DrawCondenserProcess(Canvas c)
    {
        var mixing = CurrentMode == "mixing";
        var x = mixing ? 111.0 : 83.0;
        var w = mixing ? 70.0 : 98.0;
        AddPolishGradient(c, x, 56, w, 27, CurrentTier == "mk2" ? "#112B34" : "#2A211B", "#172A37");
        AddPolishRect(c, x + 2, 58, w - 4, 1, CurrentTier == "mk2" ? "#76D8D2" : "#E4B47C");
        var travel = Math.Max(0, (w - 12) * _phase8TimelineProgress);
        AddPolishRect(c, x + 5 + travel, 66, 7, 7, CurrentTier == "mk2" ? "#76D8D2" : "#E4B47C");
        AddPolishRect(c, x + 1, 88, w - 2, 3, "#263844");
        AddPolishRect(c, x + 1, 88, (w - 2) * _phase8TimelineProgress, 3, CurrentMachineRuntimeState == "blocked" ? "#FFBF76" : SourceFrameAccent());
    }

    private void DrawGardenScene(Canvas c)
    {
        DrawSourcePanel(c, 14, 44, 220, 138, "#335544");
        const double left = 20, right = 228, top = 50, ground = 112;
        AddPolishRect(c, left, top, right - left, 18, "#182F3B");
        AddPolishRect(c, left, 68, right - left, 20, "#234753");
        AddPolishRect(c, left, 88, right - left, 12, "#305849");
        AddPolishRect(c, left, 96, 52, 9, "#355F48");
        AddPolishRect(c, 58, 91, 66, 14, "#3B684C");
        AddPolishRect(c, 112, 94, right - 112, 11, "#315B45");
        AddPolishRect(c, left, 103, right - left, ground - 103, "#47763D");
        AddPolishRect(c, left, ground, right - left, 5, "#4B4033");
        AddPolishRect(c, left, 103, right - left, 3, "#6B9A4D");
        AddPolishRect(c, 199, 56, 16, 16, "#556F9B72");
        AddPolishRect(c, 202, 59, 10, 10, "#E8CF72");
        AddPolishRect(c, 204, 61, 6, 6, "#FFE29A");
        AddPolishRect(c, left, top, right - left, 3, "#B1CFC2");
        AddPolishRect(c, left, top, 3, ground - top, "#688D86");
        AddPolishRect(c, right - 3, top, 3, ground - top, "#688D86");
        AddPolishRect(c, left + 3, top + 3, right - left - 6, 1, "#B5C9BA");
        AddPolishRect(c, 56, top + 4, 1, ground - top - 8, "#445F9C98");
        AddPolishRect(c, 190, top + 4, 1, ground - top - 8, "#445F9C98");
        var lamp = left + 8 + (right - left - 33) * _phase8TimelineProgress;
        AddPolishRect(c, lamp, top + 4, 17, 2, "#98AAA0");
        AddPolishRect(c, lamp + 2, top + 6, 13, 1, CurrentMachineRuntimeState == "running" ? "#D8F5A4" : "#60776B");

        // Compact source-like tree profile for the designer's generic garden specimen preview.
        var growth = Math.Clamp(_phase8TimelineProgress, 0, 1);
        var height = 3 + 31 * growth;
        AddPolishRect(c, 123, 104 - height, 3, height, "#68452B");
        var crown = 3 + 13 * growth;
        AddPolishRect(c, 124 - crown, 80 - crown / 2, crown * 2, Math.Max(2, crown), "#347744");
        AddPolishRect(c, 124 - crown / 2, 76 - crown / 2, crown, Math.Max(2, crown / 2), "#65A95A");

        DrawSourcePanel(c, 66, 122, 116, 9, "#456250");
        AddPolishRect(c, 68, 124, 112 * _phase8TimelineProgress, 5, CurrentMachineRuntimeState == "blocked" ? "#FFBF76" : "#62C46B");
    }

    private void DrawStatusDot(Canvas c, string color)
        => AddPolishRect(c, 177, 12, 4, 4, color);

    private string StatusColor() => ActiveRuntimeScreen switch
    {
        "CrucibleScreen" when CurrentMachineRuntimeState is "no_heat" or "lava_full" or "output_full" => "#FFBF76",
        "CrucibleScreen" when CurrentMachineRuntimeState == "idle" => "#A0B4BE",
        "CrucibleScreen" => "#76D8D2",
        "RefiningScreen" when CurrentMachineRuntimeState is "blocked" or "no_power" => "#FFBF76",
        "RefiningScreen" when CurrentMachineRuntimeState == "idle" => "#A0B4BE",
        "RefiningScreen" => "#76D8D2",
        "CondenserScreen" when CurrentMachineRuntimeState == "blocked" => "#FFBF76",
        "CondenserScreen" when CurrentMode == "garden" => "#76D982",
        "CondenserScreen" => "#76D8D2",
        _ => "#E6EEE9"
    };

    private string SourceFrameAccent() => ActiveRuntimeScreen switch
    {
        "CrucibleScreen" => "#E4B47C",
        "RefiningScreen" or "AscensionScreen" => "#BCA4F3",
        "CondenserScreen" when CurrentMode == "garden" => "#9BD4A6",
        "CondenserScreen" when CurrentTier == "mk2" => "#76D8D2",
        "CondenserScreen" => "#E4B47C",
        _ => "#76D8D2"
    };

    private void AddOverlayText(Canvas c, string text, double x, double y, double width, double height, double scale, string alignment, string color)
    {
        var visual = new MinecraftPixelText(text, Brush(color), false, scale, alignment)
        {
            Width = width,
            Height = height,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, x);
        Canvas.SetTop(visual, y);
        c.Children.Add(visual);
    }

    private void AddRotatingRing(Canvas c, double cx, double cy, double width, double height, string color, double thickness, double angle)
    {
        var border = new Border
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent,
            BorderBrush = Brush(color),
            BorderThickness = new Thickness(thickness),
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new RotateTransform(angle)
        };
        Canvas.SetLeft(border, cx - width / 2);
        Canvas.SetTop(border, cy - height / 2);
        c.Children.Add(border);
    }

    private static void AddPolishRect(Canvas c, double x, double y, double width, double height, string color)
    {
        if (width <= 0 || height <= 0)
            return;
        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        c.Children.Add(rect);
    }

    private static void AddPolishGradient(Canvas c, double x, double y, double width, double height, string top, string bottom)
    {
        if (width <= 0 || height <= 0)
            return;
        var topColor = (Color)ColorConverter.ConvertFromString(top);
        var bottomColor = (Color)ColorConverter.ConvertFromString(bottom);
        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = new LinearGradientBrush(topColor, bottomColor, 90),
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        c.Children.Add(rect);
    }
}

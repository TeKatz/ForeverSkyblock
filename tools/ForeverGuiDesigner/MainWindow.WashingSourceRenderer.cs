using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Deterministic final compositor for the Washing Basin preview.
///
/// The legacy preview path combines a generated PNG, generic WPF element bodies and
/// several later phase decorators. That is useful for broad machine discovery, but it
/// is not a reliable source of truth for a screen that must match the runtime renderer.
/// This compositor mirrors WashingScreen + MachineScreen + MachineSkin directly and
/// always runs after the older decorators.
/// </summary>
public partial class MainWindow
{
    private const string WashingSourceBackdropTag = "__forever_washing_source_backdrop_v2";

    private bool _washingSourceRendererInitialized;
    private bool _washingSourceRendererQueued;
    private bool _washingSourceRendererApplying;
    private FrameworkElement? _washingSourceVisualAnchor;
    private int _washingSourceVisualCount = -1;

    public void EnableWashingSourceRenderer()
    {
        if (_washingSourceRendererInitialized)
            return;
        _washingSourceRendererInitialized = true;

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.Click += (_, _) => QueueWashingSourceRender();
        }

        ApplyButton.Click += (_, _) => QueueWashingSourceRender();

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueueWashingSourceRender();
        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) => QueueWashingSourceRender();
        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.Checked += (_, _) => QueueWashingSourceRender();
            _phase6PlayAnimations.Unchecked += (_, _) => QueueWashingSourceRender();
        }
        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) => QueueWashingSourceRender();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_washingSourceRendererApplying)
                return;
            if (WashingSourceGenerationChanged() || (IsWashingPhase6Active && !HasWashingSourceBackdrop()))
                QueueWashingSourceRender();
        };

        CaptureWashingSourceGeneration();
        QueueWashingSourceRender();
    }

    private void QueueWashingSourceRender()
    {
        if (_washingSourceRendererQueued || _washingSourceRendererApplying)
            return;

        _washingSourceRendererQueued = true;

        // SystemIdle is intentionally below the Background/ContextIdle/ApplicationIdle
        // work used by the older phases. This is the final presentation owner.
        Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(ApplyWashingSourceRender));
    }

    private void ApplyWashingSourceRender()
    {
        _washingSourceRendererQueued = false;
        if (_washingSourceRendererApplying)
            return;

        _washingSourceRendererApplying = true;
        try
        {
            if (!IsWashingPhase6Active)
            {
                RemoveWashingSourceBackdrop();
                CaptureWashingSourceGeneration();
                return;
            }

            // A valid source scan normally supplies all three machine slots. Keep a
            // defensive fallback for old layouts/scans where no machine slot survived.
            if (EnsureWashingSourceSlots())
            {
                RefreshElementList();
                RenderLayout();
                Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(QueueWashingSourceRender));
                return;
            }

            PreviewCanvas.Background = Brush("#111315");
            RebuildWashingSourceBackdrop();

            // Recreate stateful surfaces first, then overwrite every user-visible
            // machine element with the source-fidelity representation.
            DecorateWashingElements();
            EnsureStableWashingAnimationVisuals();
            UpdateStableWashingAnimationVisuals();
            ApplyPausedWashingMotionVisibility();
            HideGeneratedPanelMetadata();

            foreach (var pair in _visuals)
            {
                var model = pair.Key;
                if (pair.Value is not Border border)
                    continue;

                switch (model.Type)
                {
                    case GuiElementType.Slot:
                        ApplyMachineSlotFidelity(model, border, washing: true);
                        break;
                    case GuiElementType.Button:
                        ApplyMachineButtonFidelity(model, border, washing: true);
                        break;
                    case GuiElementType.Text:
                        ApplyMachineTextFidelity(model, border, washing: true);
                        break;
                }

                ApplyWashingSourceSurface(model, border);
            }

            ApplyWashingSourceLayering();
            CaptureFidelityVisualGeneration();
            CaptureMachinePresentationGeneration();
            CaptureWashingSourceGeneration();
        }
        finally
        {
            _washingSourceRendererApplying = false;
        }
    }

    private bool EnsureWashingSourceSlots()
    {
        var machineSlots = _layout.Elements
            .Where(element => element.Type == GuiElementType.Slot &&
                              element.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (machineSlots.Count > 0)
            return false;

        var slots = new[]
        {
            (Id: "machine_slot_1", X: 38, Y: 74),   // WashingMenu.INPUT_X=42, MACHINE_Y=78, MachineSkin pad=4
            (Id: "machine_slot_2", X: 170, Y: 74), // WashingMenu.OUTPUT_X=174
            (Id: "machine_slot_3", X: 198, Y: 74)  // WashingMenu.SECONDARY_OUTPUT_X=202
        };

        foreach (var slot in slots)
        {
            _layout.Elements.Add(new GuiElementModel
            {
                Id = CreateUniqueId(slot.Id),
                Type = GuiElementType.Slot,
                X = slot.X,
                Y = slot.Y,
                Width = 24,
                Height = 24,
                Content = $"{slot.X + 4},{slot.Y + 4}"
            });
        }
        return true;
    }

    private void ApplyWashingSourceSurface(GuiElementModel model, Border border)
    {
        switch (model.Id)
        {
            case "forever_phase6_washing_status":
                border.Background = Brush(WashingBlocked || !WashingHasWater
                    ? "#FFBF76"
                    : WashingRunning ? "#76D8D2" : "#A0B4BE");
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                border.Child = null;
                break;

            case "forever_phase6_washing_panel_water":
                SetWashingPanel(border, WashingHasWater ? "#355B66" : "#5C4939");
                break;

            case "forever_phase6_washing_panel_process":
                SetWashingPanel(border, "#54616D");
                break;

            case "forever_phase6_washing_panel_hints":
                SetWashingPanel(border, "#465A68");
                break;

            case "forever_phase6_washing_process_scene":
            case "forever_phase6_washing_meter_progress":
                // Stable-animation surfaces own these children. Do not replace them
                // with the generic Texture fallback.
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                break;
        }
    }

    private void ApplyWashingSourceLayering()
    {
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            var id = model.Id;
            var z = model.Type switch
            {
                GuiElementType.Slot when id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase) => 520,
                GuiElementType.Slot => 420,
                GuiElementType.Text => 620,
                GuiElementType.Button => 680,
                GuiElementType.Texture when id.Contains("panel_", StringComparison.OrdinalIgnoreCase) => 60,
                GuiElementType.Texture when id.Contains("process_scene", StringComparison.OrdinalIgnoreCase) => 220,
                GuiElementType.Texture when id.Contains("meter", StringComparison.OrdinalIgnoreCase) => 260,
                GuiElementType.Texture when id.Contains("status", StringComparison.OrdinalIgnoreCase) => 610,
                GuiElementType.Texture => 180,
                _ => 0
            };
            Panel.SetZIndex(pair.Value, z);
        }
    }

    private bool HasWashingSourceBackdrop()
        => PreviewCanvas.Children.OfType<FrameworkElement>()
            .Any(child => string.Equals(child.Tag as string, WashingSourceBackdropTag, StringComparison.Ordinal));

    private void RebuildWashingSourceBackdrop()
    {
        RemoveWashingSourceBackdrop();

        var width = _layout.Width;
        var height = _layout.Height;
        var accent = WashingHasWater ? "#9DDDF0" : "#FFBF76";
        var canvas = new Canvas
        {
            Tag = WashingSourceBackdropTag,
            Width = width,
            Height = height,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true,
            ClipToBounds = true
        };
        RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);
        RenderOptions.SetBitmapScalingMode(canvas, BitmapScalingMode.NearestNeighbor);

        // Opaque editor base hides the legacy generated preview image below this
        // compositor. Everything from here mirrors MachineSkin.frame().
        AddFidelityRect(canvas, 0, 0, width, height, "#111315");
        AddFidelityRect(canvas, 3, 4, width, height, "#66050910");
        AddFidelityRect(canvas, 3, 0, width - 6, height, "#0C151F");
        AddFidelityRect(canvas, 0, 3, width, height - 6, "#0C151F");
        AddFidelityRect(canvas, 1, 4, width - 2, height - 8, "#425764");
        AddFidelityRect(canvas, 4, 1, width - 8, height - 2, "#425764");

        var body = new Border
        {
            Width = Math.Max(1, width - 6),
            Height = Math.Max(1, height - 8),
            Background = new LinearGradientBrush(Brush("#253746").Color, Brush("#15232F").Color, 90),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(body, 3);
        Canvas.SetTop(body, 4);
        canvas.Children.Add(body);

        AddFidelityRect(canvas, 8, 7, width - 16, 20, "#101D29");
        AddFidelityRect(canvas, 14, 27, width - 28, 1, "#526979");
        AddFidelityRect(canvas, 14, 27, 52, 1, accent);
        AddFidelityRect(canvas, 10, 33, 1, height - 45, "#344A58");
        AddFidelityRect(canvas, width - 11, 33, 1, height - 45, "#344A58");

        foreach (var dx in new[] { 5, width - 7 })
        foreach (var dy in new[] { 5, height - 7 })
        {
            AddFidelityRect(canvas, dx, dy, 2, 2, "#0C1923");
            AddFidelityRect(canvas, dx, dy, 1, 1, "#91A5AE");
        }
        AddFidelityRect(canvas, width / 2.0 - 14, height - 4, 28, 1, accent);

        // MachineScreen.inventory(): exact Washing layout is inventoryY=190 / hotbarY=254.
        AddWashingSourcePanel(canvas, 18, 184, 212, 92, "#405866");
        AddFidelityRect(canvas, 24, 248, width - 48, 1, "#344C5B");

        AddWashingSourceMachineIcon(canvas);

        Panel.SetZIndex(canvas, -900);
        PreviewCanvas.Children.Insert(0, canvas);
    }

    private static void AddWashingSourcePanel(Canvas canvas, double x, double y, double width, double height, string edge)
    {
        AddFidelityRect(canvas, x, y, width, height, "#0A151F");
        AddFidelityRect(canvas, x + 1, y + 1, width - 2, height - 2, edge);

        var inner = new Border
        {
            Width = Math.Max(1, width - 4),
            Height = Math.Max(1, height - 4),
            Background = new LinearGradientBrush(Brush("#1B2C39").Color, Brush("#12212D").Color, 90),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(inner, x + 2);
        Canvas.SetTop(inner, y + 2);
        canvas.Children.Add(inner);
        AddFidelityRect(canvas, x + 3, y + 1, width - 6, 1, "#637884");
    }

    private void AddWashingSourceMachineIcon(Canvas canvas)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;

        var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap("forever_skyblock:washing_basin");
        if (bitmap is null)
            return;

        var image = new Image
        {
            Source = bitmap,
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(image, 18);
        Canvas.SetTop(image, 10);
        canvas.Children.Add(image);
    }

    private void RemoveWashingSourceBackdrop()
    {
        var existing = PreviewCanvas.Children.OfType<FrameworkElement>()
            .Where(child => string.Equals(child.Tag as string, WashingSourceBackdropTag, StringComparison.Ordinal))
            .ToList();
        foreach (var child in existing)
            PreviewCanvas.Children.Remove(child);
    }

    private bool WashingSourceGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _washingSourceVisualCount ||
               !ReferenceEquals(anchor, _washingSourceVisualAnchor);
    }

    private void CaptureWashingSourceGeneration()
    {
        _washingSourceVisualCount = _visuals.Count;
        _washingSourceVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}

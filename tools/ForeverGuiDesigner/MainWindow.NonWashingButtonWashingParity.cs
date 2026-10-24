using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Gives Crucible, Refining, Ascension and Condenser buttons the same structural
/// ownership model as the stable Washing preview.
///
/// The native Border owns background + outline, MinecraftPixelText is the direct child,
/// hover is local to that Border, and editor selection remains a separate adorner.
/// Keeping MinecraftPixelText as the direct child is important: Phase 8 only replaces
/// Text/Button children that are not MinecraftPixelText, which used to destroy the
/// nested non-Washing button Grid for one frame during selection changes.
/// </summary>
public partial class MainWindow
{
    private bool _nonWashingButtonWashingParityInitialized;
    private bool _nonWashingButtonWashingParityQueued;
    private FrameworkElement? _nonWashingButtonWashingParityVisualAnchor;
    private int _nonWashingButtonWashingParityVisualCount = -1;
    private readonly HashSet<Border> _nonWashingButtonWashingParityHooks = [];

    public void EnableNonWashingButtonWashingParity()
    {
        if (_nonWashingButtonWashingParityInitialized)
            return;
        _nonWashingButtonWashingParityInitialized = true;

        CaptureNonWashingButtonWashingParityGeneration();

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } load)
        {
            load.Click += (_, _) => QueueNonWashingButtonWashingParity();
        }

        ApplyButton.Click += (_, _) => QueueNonWashingButtonWashingParity();

        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueNonWashingButtonWashingParity();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueueNonWashingButtonWashingParity();
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueueNonWashingButtonWashingParity();

        // Match Washing's selection lifecycle: restore source button chrome synchronously
        // in the same input event. No deferred selection repair and no drift polling.
        PreviewCanvas.AddHandler(
            UIElement.MouseLeftButtonDownEvent,
            new System.Windows.Input.MouseButtonEventHandler((_, _) => SynchronizeNonWashingButtonSelection()),
            true);
        ElementsList.SelectionChanged += (_, _) => SynchronizeNonWashingButtonSelection();
        if (_phase8Hierarchy is not null)
            _phase8Hierarchy.SelectedItemChanged += (_, _) => SynchronizeNonWashingButtonSelection();

        // A new visual generation means RenderLayout replaced the actual Border objects.
        // Re-apply button ownership once for that generation only.
        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (!IsExclusiveNonWashingMachineRuntimeActive ||
                !NonWashingButtonWashingParityGenerationChanged())
            {
                return;
            }

            CaptureNonWashingButtonWashingParityGeneration();
            QueueNonWashingButtonWashingParity();
        };

        QueueNonWashingButtonWashingParity();
    }

    private void SynchronizeNonWashingButtonSelection()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        RestoreNonWashingButtonsFromWashingPattern();
    }

    private void QueueNonWashingButtonWashingParity()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        ReserveNonWashingButtonHookOwnership();

        if (_nonWashingButtonWashingParityQueued)
            return;
        _nonWashingButtonWashingParityQueued = true;

        Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
        {
            _nonWashingButtonWashingParityQueued = false;
            if (!IsExclusiveNonWashingMachineRuntimeActive)
                return;

            RestoreNonWashingButtonsFromWashingPattern();
            CaptureNonWashingButtonWashingParityGeneration();
        }));
    }

    private void ReserveNonWashingButtonHookOwnership()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        _nonWashingButtonWashingParityHooks.RemoveWhere(border => !_visuals.Values.Contains(border));
        _machineRuntimePolishButtonHooks.RemoveWhere(border => !_visuals.Values.Contains(border));
        _machineRuntimeButtonHooks.RemoveWhere(border => !_visuals.Values.Contains(border));

        foreach (var pair in _visuals)
        {
            if (pair.Key.Type != GuiElementType.Button || pair.Value is not Border border)
                continue;

            // Reserve this visual generation before the older runtime/polish paths can
            // register competing hover callbacks on the same Border.
            _machineRuntimePolishButtonHooks.Add(border);
            _machineRuntimeButtonHooks.Add(border);
        }
    }

    private void RestoreNonWashingButtonsFromWashingPattern()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        ReserveNonWashingButtonHookOwnership();

        foreach (var pair in _visuals)
        {
            if (pair.Key.Type != GuiElementType.Button ||
                pair.Value is not Border border ||
                pair.Value.Visibility != Visibility.Visible)
            {
                continue;
            }

            ApplyNonWashingButtonFromWashingPattern(pair.Key, border, border.IsMouseOver);
        }
    }

    private void ApplyNonWashingButtonFromWashingPattern(GuiElementModel model, Border border, bool hover)
    {
        var accent = SourceFrameAccent();
        var primary =
            (ActiveRuntimeScreen == "AscensionScreen" && model.Id.Equals("start", StringComparison.OrdinalIgnoreCase)) ||
            (ActiveRuntimeScreen == "CondenserScreen" && model.Id.Equals("filters", StringComparison.OrdinalIgnoreCase)) ||
            (ActiveRuntimeScreen == "CrucibleScreen" && model.Id.Contains("mode", StringComparison.OrdinalIgnoreCase));

        border.Background = CreateNonWashingButtonBackground(model, hover, primary, accent);
        border.BorderBrush = Brush(hover ? accent : "#6A818C");
        border.BorderThickness = new Thickness(1);
        border.ClipToBounds = true;
        border.SnapsToDevicePixels = true;

        var value = string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content;
        if (border.Child is MinecraftPixelText existing)
        {
            // Keep the same child object alive just like the source-owned dynamic text
            // surfaces. Phase 8 therefore sees a stable MinecraftPixelText at all times.
            existing.Text = value;
            existing.Width = Math.Max(1, model.Width - 4);
            existing.Height = 8;
            existing.HorizontalAlignment = HorizontalAlignment.Center;
            existing.VerticalAlignment = VerticalAlignment.Center;
            existing.IsHitTestVisible = false;
        }
        else
        {
            border.Child = new MinecraftPixelText(
                value,
                Brush("#E6EEE9"),
                false,
                .92,
                "center")
            {
                Width = Math.Max(1, model.Width - 4),
                Height = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
        }

        if (_nonWashingButtonWashingParityHooks.Add(border))
        {
            border.MouseEnter += (_, _) =>
            {
                if (IsExclusiveNonWashingMachineRuntimeActive && border.Tag is GuiElementModel current)
                    ApplyNonWashingButtonFromWashingPattern(current, border, hover: true);
            };
            border.MouseLeave += (_, _) =>
            {
                if (IsExclusiveNonWashingMachineRuntimeActive && border.Tag is GuiElementModel current)
                    ApplyNonWashingButtonFromWashingPattern(current, border, hover: false);
            };
        }
    }

    private Brush CreateNonWashingButtonBackground(
        GuiElementModel model,
        bool hover,
        bool primary,
        string accent)
    {
        var baseBrush = new LinearGradientBrush(
            Brush(hover ? "#37515E" : "#263B4B").Color,
            Brush("#172936").Color,
            90);

        if (!primary)
            return baseBrush;

        // Preserve the machine-specific two-pixel primary accent without wrapping the
        // text in a Grid. The direct MinecraftPixelText child is what keeps Phase 8 from
        // treating this source button as an undecorated generic editor button.
        var width = Math.Max(1, model.Width);
        var height = Math.Max(1, model.Height);
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(
            baseBrush,
            null,
            new RectangleGeometry(new Rect(0, 0, width, height))));
        drawing.Children.Add(new GeometryDrawing(
            Brush(accent),
            null,
            new RectangleGeometry(new Rect(1, 1, Math.Min(2, width), Math.Max(0, height - 2)))));

        return new DrawingBrush(drawing)
        {
            Viewbox = new Rect(0, 0, width, height),
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, 1, 1),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            Stretch = Stretch.Fill,
            TileMode = TileMode.None
        };
    }

    private bool NonWashingButtonWashingParityGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _nonWashingButtonWashingParityVisualCount ||
               !ReferenceEquals(anchor, _nonWashingButtonWashingParityVisualAnchor);
    }

    private void CaptureNonWashingButtonWashingParityGeneration()
    {
        _nonWashingButtonWashingParityVisualCount = _visuals.Count;
        _nonWashingButtonWashingParityVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}

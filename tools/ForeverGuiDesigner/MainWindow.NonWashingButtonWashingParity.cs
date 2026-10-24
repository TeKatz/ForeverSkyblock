using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Gives Crucible, Refining, Ascension and Condenser buttons the same ownership model
/// as the stable Washing preview:
/// - the real element Border owns background + native button border;
/// - hover is local to exactly that Border;
/// - editor selection is a separate runtime adorner and never becomes button chrome;
/// - selection/deselection cannot leave generic editor chrome on the real button;
/// - the final static pass runs after older source/polish writers.
///
/// Geometry/content/accent remain machine-specific; only the proven Washing lifecycle
/// and interaction model are shared.
/// </summary>
public partial class MainWindow
{
    private const string NonWashingButtonChromeTag = "__forever_non_washing_button_washing_parity_v2";

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

        // Selection is editor chrome only. The legacy runtime path still has an older
        // MouseDown synchronizer, so do not rely on handler registration order. Restore
        // once on MouseDown and, crucially, again on MouseUp after every MouseDown writer
        // and the phase-8 selection handles have finished their work.
        PreviewCanvas.AddHandler(
            UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler((_, _) => FinalizeNonWashingButtonInteraction()),
            true);
        PreviewCanvas.AddHandler(
            UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler((_, _) => FinalizeNonWashingButtonInteraction()),
            true);

        ElementsList.SelectionChanged += (_, _) => FinalizeNonWashingButtonInteraction();
        if (_phase8Hierarchy is not null)
            _phase8Hierarchy.SelectedItemChanged += (_, _) => FinalizeNonWashingButtonInteraction();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (!IsExclusiveNonWashingMachineRuntimeActive)
                return;

            var generationChanged = NonWashingButtonWashingParityGenerationChanged();
            if (generationChanged)
                CaptureNonWashingButtonWashingParityGeneration();

            // UpdateSelectionVisuals() and a few old decorators can still write the
            // native Border after selection. Detect that directly instead of depending
            // on event ordering. Queue only the button repair; never rebuild the machine.
            if (generationChanged || NonWashingButtonChromeDrifted())
                QueueNonWashingButtonWashingParity();
        };

        QueueNonWashingButtonWashingParity();
    }

    private void FinalizeNonWashingButtonInteraction()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        RestoreNonWashingButtonsFromWashingPattern();

        // Keep a final low-priority pass as a safety net for any older dispatcher work
        // queued by the same selection event. This pass touches buttons only.
        QueueNonWashingButtonWashingParity();
    }

    private void QueueNonWashingButtonWashingParity()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        ReserveNonWashingButtonHookOwnership();

        if (_nonWashingButtonWashingParityQueued)
            return;
        _nonWashingButtonWashingParityQueued = true;

        // Washing's source renderer is the last presentation owner. Match that rule for
        // non-Washing buttons only, without making another machine-wide render owner.
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

            // Prevent the older polish/runtime paths from registering another local
            // MouseEnter/MouseLeave owner for this exact visual generation.
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

        // Same native ownership as ApplyExclusiveWashingButtonChrome: the real Border
        // owns background and outline. Selection is drawn elsewhere as an adorner.
        border.Background = new LinearGradientBrush(
            Brush(hover ? "#37515E" : "#263B4B").Color,
            Brush("#172936").Color,
            90);
        border.BorderBrush = Brush(hover ? accent : "#6A818C");
        border.BorderThickness = new Thickness(1);
        border.ClipToBounds = true;
        border.SnapsToDevicePixels = true;

        var content = new Grid
        {
            Tag = NonWashingButtonChromeTag,
            Width = Math.Max(1, model.Width - 2),
            Height = Math.Max(1, model.Height - 2),
            IsHitTestVisible = false,
            ClipToBounds = true
        };

        if (primary)
        {
            content.Children.Add(new Rectangle
            {
                Width = 2,
                Fill = Brush(accent),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 1, 0, 1),
                IsHitTestVisible = false
            });
        }

        content.Children.Add(new MinecraftPixelText(
            string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content,
            Brush("#E6EEE9"),
            false,
            .92,
            "center")
        {
            Width = Math.Max(1, model.Width - 6),
            Height = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        });

        border.Child = content;

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

    private bool NonWashingButtonChromeDrifted()
    {
        var accent = SourceFrameAccent();

        foreach (var pair in _visuals)
        {
            if (pair.Key.Type != GuiElementType.Button ||
                pair.Value is not Border border ||
                pair.Value.Visibility != Visibility.Visible)
            {
                continue;
            }

            if (border.Child is not Grid { Tag: string tag } ||
                !string.Equals(tag, NonWashingButtonChromeTag, StringComparison.Ordinal))
            {
                return true;
            }

            if (border.Background is not LinearGradientBrush ||
                border.BorderThickness.Left != 1 ||
                border.BorderThickness.Top != 1 ||
                border.BorderThickness.Right != 1 ||
                border.BorderThickness.Bottom != 1)
            {
                return true;
            }

            var expected = Brush(border.IsMouseOver ? accent : "#6A818C").Color;
            if (border.BorderBrush is not SolidColorBrush actual || actual.Color != expected)
                return true;
        }

        return false;
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

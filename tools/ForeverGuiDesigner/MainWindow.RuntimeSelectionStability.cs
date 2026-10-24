using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Selection in the source-faithful machine previews is editor chrome only.
/// The base editor normally paints selection directly onto each element Border; the
/// exclusive machine renderers already use separate adorners, so letting both systems
/// run changes border thickness and can reflow/repaint the real GUI on every click.
///
/// Guard the legacy selection-restoration callbacks during the input event, then restore
/// only outer Border properties. No child visual, slot body, text glyph tree, runtime
/// surface or machine geometry is rebuilt just because an element was selected.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeSelectionStabilityInitialized;
    private bool _runtimeSelectionGuardActive;
    private bool _runtimeSelectionPreviousWashingApplying;
    private bool _runtimeSelectionPreviousMachineApplying;
    private int _runtimeSelectionGuardToken;

    public void EnableRuntimeSelectionStabilityFix()
    {
        if (_runtimeSelectionStabilityInitialized)
            return;
        _runtimeSelectionStabilityInitialized = true;

        // Tunnelling mouse input runs before the existing bubbling selection handlers.
        // This also intentionally runs before the context-menu right-click selector.
        PreviewCanvas.AddHandler(UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        ElementsList.AddHandler(UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        if (_phase8Hierarchy is not null)
        {
            _phase8Hierarchy.AddHandler(UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        }

        PreviewKeyDown += RuntimeSelectionPreviewKeyDown;
    }

    private bool RuntimeSelectionPreviewActive
        => (_washingExclusiveModeActive && IsWashingPhase6Active) ||
           (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive);

    private void RuntimeSelectionPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (RuntimeSelectionPreviewActive)
            BeginRuntimeSelectionGuard();
    }

    private void RuntimeSelectionPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive || Keyboard.FocusedElement is TextBox)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if ((control && e.Key == Key.A) ||
            ElementsList.IsKeyboardFocusWithin ||
            _phase8Hierarchy?.IsKeyboardFocusWithin == true)
        {
            BeginRuntimeSelectionGuard();
        }
    }

    private void BeginRuntimeSelectionGuard()
    {
        var token = ++_runtimeSelectionGuardToken;

        if (!_runtimeSelectionGuardActive)
        {
            _runtimeSelectionGuardActive = true;
            _runtimeSelectionPreviousWashingApplying = _washingRenderPipelineFixApplying;
            _runtimeSelectionPreviousMachineApplying = _machineRuntimeApplying;

            if (_washingExclusiveModeActive && IsWashingPhase6Active)
                _washingRenderPipelineFixApplying = true;
            if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
                _machineRuntimeApplying = true;
        }

        // ContextIdle runs after synchronous selection changes and the Phase 8
        // Background refresh, so the final state is normalized once, not repeatedly.
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (token != _runtimeSelectionGuardToken)
                return;
            EndRuntimeSelectionGuard();
        }));
    }

    private void EndRuntimeSelectionGuard()
    {
        if (!_runtimeSelectionGuardActive)
            return;

        _runtimeSelectionGuardActive = false;
        _washingRenderPipelineFixApplying = _runtimeSelectionPreviousWashingApplying;
        _machineRuntimeApplying = _runtimeSelectionPreviousMachineApplying;

        if (_washingExclusiveModeActive && IsWashingPhase6Active)
        {
            NormalizeWashingSelectionChrome();
            UpdateWashingSelectionAdorners();
        }
        else if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
        {
            NormalizeMachineRuntimeSelectionChrome();
            UpdateMachineRuntimeSelectionAdorners();
        }

        if (_phase8Initialized)
            EnsurePhase8OverlayVisuals();
    }

    private void NormalizeWashingSelectionChrome()
    {
        var accent = WashingHasWater ? "#9DDDF0" : "#FFBF76";

        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            switch (model.Type)
            {
                case GuiElementType.Slot:
                case GuiElementType.Text:
                case GuiElementType.PlayerInventory:
                case GuiElementType.Hotbar:
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                    break;

                case GuiElementType.Button:
                    border.BorderBrush = Brush(border.IsMouseOver ? accent : "#6A818C");
                    border.BorderThickness = new Thickness(1);
                    break;

                case GuiElementType.Texture:
                    NormalizeWashingTextureOuterChrome(model, border);
                    break;
            }
        }
    }

    private void NormalizeWashingTextureOuterChrome(GuiElementModel model, Border border)
    {
        switch (model.Id)
        {
            case "forever_phase6_washing_panel_water":
                border.BorderBrush = Brush(WashingHasWater ? "#355B66" : "#5C4939");
                border.BorderThickness = new Thickness(1);
                break;

            case "forever_phase6_washing_panel_process":
                border.BorderBrush = Brush("#54616D");
                border.BorderThickness = new Thickness(1);
                break;

            case "forever_phase6_washing_panel_hints":
                border.BorderBrush = Brush("#465A68");
                border.BorderThickness = new Thickness(1);
                break;

            default:
                // Status, process scene, meter and free runtime texture anchors draw
                // their own source-faithful chrome inside the child visual.
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                break;
        }
    }

    private void NormalizeMachineRuntimeSelectionChrome()
    {
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            switch (model.Type)
            {
                case GuiElementType.Slot:
                case GuiElementType.Text:
                case GuiElementType.PlayerInventory:
                case GuiElementType.Hotbar:
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                    break;

                case GuiElementType.Button:
                    border.BorderBrush = border.IsMouseOver ? Brush(RuntimeAccent()) : Brush("#6A818C");
                    border.BorderThickness = new Thickness(1);
                    break;

                case GuiElementType.Texture:
                    NormalizeMachineRuntimeTextureOuterChrome(model, border);
                    break;
            }
        }
    }

    private void NormalizeMachineRuntimeTextureOuterChrome(GuiElementModel model, Border border)
    {
        var id = model.Id;
        if (id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase) &&
            id.Contains("ring_", StringComparison.OrdinalIgnoreCase))
        {
            border.BorderBrush = Brush("#8E4FD2");
            border.BorderThickness = new Thickness(2);
            return;
        }

        if (id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase) &&
            id.Contains("_scene", StringComparison.OrdinalIgnoreCase))
        {
            border.BorderBrush = Brush(id.Contains("garden", StringComparison.OrdinalIgnoreCase) ? "#5A8A63" : "#566D7A");
            border.BorderThickness = new Thickness(1);
            return;
        }

        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
    }
}

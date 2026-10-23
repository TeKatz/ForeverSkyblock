using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Deterministic scheduling/slot-integrity fix for Washing.
///
/// The 0.8.3 source compositor used SystemIdle as its final pass. That can be
/// starved indefinitely while render-priority animation timers are active,
/// leaving RenderLayout()'s generic grey canvas visible after Apply properties.
/// This coordinator runs after the older Background decorators instead and
/// repairs all three source machine slots before invoking the source compositor.
/// </summary>
public partial class MainWindow
{
    private bool _washingRenderPipelineFixInitialized;
    private bool _washingRenderPipelineFixQueued;
    private bool _washingRenderPipelineFixApplying;
    private FrameworkElement? _washingRenderPipelineVisualAnchor;
    private int _washingRenderPipelineVisualCount = -1;

    public void EnableWashingRenderPipelineFix()
    {
        if (_washingRenderPipelineFixInitialized)
            return;
        _washingRenderPipelineFixInitialized = true;

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.Click += (_, _) => QueueDeterministicWashingRender();
        }

        ApplyButton.Click += (_, _) => QueueDeterministicWashingRender();
        ElementsList.SelectionChanged += (_, _) => QueueDeterministicWashingRender();

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueueDeterministicWashingRender();
        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) => QueueDeterministicWashingRender();
        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.Checked += (_, _) => QueueDeterministicWashingRender();
            _phase6PlayAnimations.Unchecked += (_, _) => QueueDeterministicWashingRender();
        }
        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) => QueueDeterministicWashingRender();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_washingRenderPipelineFixApplying)
                return;

            if (DeterministicWashingGenerationChanged() ||
                (IsWashingPhase6Active && !HasWashingSourceBackdrop()))
            {
                QueueDeterministicWashingRender();
            }
        };

        CaptureDeterministicWashingGeneration();
        QueueDeterministicWashingRender();
    }

    private void QueueDeterministicWashingRender()
    {
        if (_washingRenderPipelineFixQueued || _washingRenderPipelineFixApplying)
            return;

        _washingRenderPipelineFixQueued = true;

        // Background is intentional: all Phase 5/6 selection/load handlers were
        // registered earlier and enqueue their own Background work first. This pass
        // therefore runs after them, but unlike SystemIdle it cannot be starved by
        // the continuously ticking Render-priority animation timers.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyDeterministicWashingRender));
    }

    private void ApplyDeterministicWashingRender()
    {
        _washingRenderPipelineFixQueued = false;
        if (_washingRenderPipelineFixApplying)
            return;

        var needsSecondPass = false;
        _washingRenderPipelineFixApplying = true;
        try
        {
            if (!IsWashingPhase6Active)
            {
                CaptureDeterministicWashingGeneration();
                return;
            }

            // 0.8.3 only repaired the completely-empty case. A partial scan with
            // one or two machine slots therefore permanently bypassed the fallback.
            // Ensure each runtime Washing slot exists independently.
            if (EnsureCompleteWashingSourceSlots())
            {
                RefreshElementList();
                RenderLayout();
                needsSecondPass = true;
                return;
            }

            // RenderLayout() should create a visual for every visible slot model.
            // If another lifecycle pass left the visual dictionary incomplete,
            // rebuild once and then run the source compositor on the new generation.
            if (VisibleWashingMachineSlotMissingVisual())
            {
                RenderLayout();
                needsSecondPass = true;
                return;
            }

            // Invoke the existing source-derived compositor directly. Do not route
            // through QueueWashingSourceRender(), because that deliberately uses the
            // starvation-prone SystemIdle priority retained for 0.8.3 compatibility.
            ApplyWashingSourceRender();

            // Explicit final invariant: every visible Washing machine slot is visible
            // and sits above the process panel. This repairs any earlier decorator
            // that collapsed a slot visual without changing model.Visible.
            foreach (var pair in _visuals)
            {
                var model = pair.Key;
                if (!model.Visible || model.Type != GuiElementType.Slot ||
                    !model.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                pair.Value.Visibility = Visibility.Visible;
                if (pair.Value is Border border)
                    ApplyMachineSlotFidelity(model, border, washing: true);
                Panel.SetZIndex(pair.Value, 900);
            }

            CaptureFidelityVisualGeneration();
            CaptureMachinePresentationGeneration();
            CaptureWashingSourceGeneration();
            CaptureDeterministicWashingGeneration();
        }
        finally
        {
            _washingRenderPipelineFixApplying = false;
            if (needsSecondPass)
                QueueDeterministicWashingRender();
        }
    }

    private bool EnsureCompleteWashingSourceSlots()
    {
        var changed = false;
        var expected = new[]
        {
            (Id: "machine_slot_1", Content: "42,78", X: 38, Y: 74),
            (Id: "machine_slot_2", Content: "174,78", X: 170, Y: 74),
            (Id: "machine_slot_3", Content: "202,78", X: 198, Y: 74)
        };

        foreach (var slot in expected)
        {
            var existing = _layout.Elements.FirstOrDefault(element =>
                               element.Type == GuiElementType.Slot &&
                               string.Equals(element.Content, slot.Content, StringComparison.OrdinalIgnoreCase))
                           ?? _layout.Elements.FirstOrDefault(element =>
                               element.Type == GuiElementType.Slot &&
                               string.Equals(element.Id, slot.Id, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                continue;

            _layout.Elements.Add(new GuiElementModel
            {
                Id = CreateUniqueId(slot.Id),
                Type = GuiElementType.Slot,
                X = slot.X,
                Y = slot.Y,
                Width = 24,
                Height = 24,
                Content = slot.Content,
                Visible = true
            });
            changed = true;
        }

        return changed;
    }

    private bool VisibleWashingMachineSlotMissingVisual()
        => _layout.Elements.Any(model =>
            model.Visible &&
            model.Type == GuiElementType.Slot &&
            model.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase) &&
            !_visuals.ContainsKey(model));

    private bool DeterministicWashingGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _washingRenderPipelineVisualCount ||
               !ReferenceEquals(anchor, _washingRenderPipelineVisualAnchor);
    }

    private void CaptureDeterministicWashingGeneration()
    {
        _washingRenderPipelineVisualCount = _visuals.Count;
        _washingRenderPipelineVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}

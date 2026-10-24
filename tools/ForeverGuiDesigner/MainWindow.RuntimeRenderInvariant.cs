using System.Windows;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Final invariant for the editor/runtime boundary.
///
/// RenderLayout() intentionally rebuilds the editable visual tree from generic editor
/// controls. Every structural editor operation may call it: add/delete/cut/paste,
/// undo/redo, inspector apply, visibility/lock/layer changes, distribute, hierarchy
/// reorder, resize completion, source reload and several legacy helpers. A loaded
/// machine must therefore always get one final source-faithful rehydrate after a new
/// visual generation appears.
///
/// This coordinator never creates missing runtime/source models. It only reapplies the
/// active presentation to the models that still exist, so deleting or cutting an
/// editable element remains an editor change rather than being silently undone.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeRenderInvariantInitialized;
    private bool _runtimeRenderInvariantQueued;
    private FrameworkElement? _runtimeRenderInvariantVisualAnchor;
    private int _runtimeRenderInvariantVisualCount = -1;

    public void EnableRuntimeRenderInvariant()
    {
        if (_runtimeRenderInvariantInitialized)
            return;
        _runtimeRenderInvariantInitialized = true;

        CaptureRuntimeRenderInvariantGeneration();
        PreviewCanvas.LayoutUpdated += RuntimeRenderInvariantLayoutUpdated;
    }

    private void RuntimeRenderInvariantLayoutUpdated(object? sender, EventArgs e)
    {
        if (!RuntimeRenderInvariantGenerationChanged())
            return;

        // Capture the generation immediately. WPF can emit many LayoutUpdated events
        // for one RenderLayout; they must collapse into one final presentation pass.
        CaptureRuntimeRenderInvariantGeneration();

        if (!RuntimeSelectionPreviewActive)
            return;

        QueueRuntimeRenderInvariant();
    }

    private bool RuntimeRenderInvariantGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _runtimeRenderInvariantVisualCount ||
               !ReferenceEquals(anchor, _runtimeRenderInvariantVisualAnchor);
    }

    private void CaptureRuntimeRenderInvariantGeneration()
    {
        _runtimeRenderInvariantVisualCount = _visuals.Count;
        _runtimeRenderInvariantVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private void QueueRuntimeRenderInvariant()
    {
        if (_runtimeRenderInvariantQueued)
            return;

        _runtimeRenderInvariantQueued = true;

        // Existing Phase 5-9 decorators use Render/Background/ContextIdle work. Run at
        // ApplicationIdle so the exclusive source/runtime owner is guaranteed to be the
        // last presentation writer for this visual generation.
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(ApplyRuntimeRenderInvariant));
    }

    private void ApplyRuntimeRenderInvariant()
    {
        _runtimeRenderInvariantQueued = false;

        if (!RuntimeSelectionPreviewActive)
        {
            CaptureRuntimeRenderInvariantGeneration();
            return;
        }

        // Re-entrancy here is rare (normally the owner has already completed before the
        // dispatcher reaches ApplicationIdle), but retry instead of leaving a generic
        // generation stranded if a nested dispatcher frame happens to observe it busy.
        if ((_washingExclusiveModeActive && IsWashingPhase6Active && _washingRenderPipelineFixApplying) ||
            (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive && _machineRuntimeApplying))
        {
            QueueRuntimeRenderInvariant();
            return;
        }

        if (_washingExclusiveModeActive && IsWashingPhase6Active)
        {
            ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: true);
        }
        else if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
        {
            ApplyExclusiveMachineRuntimePresentation(ensureStructure: false, rebuildBackdrop: true);
        }

        CaptureRuntimeRenderInvariantGeneration();
    }
}

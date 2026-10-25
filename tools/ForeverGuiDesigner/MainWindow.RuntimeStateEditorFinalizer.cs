using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17B editor actions still submit their base-model frame first. Re-submit the semantic 17C
/// runtime state after those discrete edits so state overlays (especially Garden glass handling)
/// remain authoritative without adding another playback loop.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeStateEditorFinalizerInitialized;

    public void EnableForeverRuntimeStateEditorFinalizer()
    {
        if (_studioRuntimeStateEditorFinalizerInitialized)
            return;
        _studioRuntimeStateEditorFinalizerInitialized = true;

        // Also catches programmatic SetStudioAnimationTimelineTime() used by Stop, seek commands and
        // transport. Queueing rather than rendering inline ensures the legacy base render completes first.
        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.ValueChanged += (_, _) => QueueStudioRuntimeStatePreview();

        if (_studioAnimationElementCombo is not null)
            _studioAnimationElementCombo.SelectionChanged += (_, _) => QueueStudioRuntimeStatePreview();
        if (_studioAnimationKeyframeList is not null)
            _studioAnimationKeyframeList.SelectionChanged += (_, _) => QueueStudioRuntimeStatePreview();

        foreach (var box in _studioAnimationPositionBoxes.Concat(_studioAnimationScaleBoxes))
            if (box is not null)
                box.TextChanged += (_, _) => QueueStudioRuntimeStatePreview();

        if (_studioAnimationRotationAngleBox is not null)
            _studioAnimationRotationAngleBox.TextChanged += (_, _) => QueueStudioRuntimeStatePreview();
        if (_studioAnimationRotationAxisCombo is not null)
            _studioAnimationRotationAxisCombo.SelectionChanged += (_, _) => QueueStudioRuntimeStatePreview();
    }
}

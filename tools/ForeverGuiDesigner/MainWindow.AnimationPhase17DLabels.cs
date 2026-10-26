using System.Windows.Controls;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _studioAnimationPhase17DLabelsInitialized;

    public void EnableForeverAnimationPhase17DLabels()
    {
        if (_studioAnimationPhase17DLabelsInitialized)
            return;
        _studioAnimationPhase17DLabelsInitialized = true;

        if (_studioAnimationViewport?.Parent is Grid viewportGrid)
        {
            foreach (var border in viewportGrid.Children.OfType<Border>())
            {
                if (border.Child is TextBlock text && text.Text.StartsWith("BLOCK ANIMATION · PHASE 17", StringComparison.Ordinal))
                    text.Text = "BLOCK ANIMATION · PHASE 17D.4";
            }
        }

        if (_studioAnimationRuntimeValueText?.Parent is StackPanel inspector)
        {
            foreach (var text in inspector.Children.OfType<TextBlock>())
            {
                if (!text.Text.StartsWith("Phase 17C.3", StringComparison.Ordinal))
                    continue;
                text.Text = "Phase 17D.4 closes the authoring side of the runtime binding introduced in 17D.3. Current-asset runtime:{asset}:{component} targets from the Phase-17A registry are selectable beside JSON elements and use the same persisted transform / visibility tracks, interpolation, keyframe editing and dirty/review workflow. Preview first composes the source-backed renderer state, then applies authored transforms around the same component pivots used by the Java binder; missing source components remain absent. JSON tracks are composed together without destructive visibility index shifts, and target-local material resolution now follows each stable JSON target instead of only the currently selected one. Machine state, status and special-effect source predicates remain authoritative.";
                break;
            }
        }
    }
}

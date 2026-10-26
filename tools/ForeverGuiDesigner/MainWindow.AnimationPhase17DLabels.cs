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
                    text.Text = "BLOCK ANIMATION · PHASE 17D.3";
            }
        }

        if (_studioAnimationRuntimeValueText?.Parent is StackPanel inspector)
        {
            foreach (var text in inspector.Children.OfType<TextBlock>())
            {
                if (!text.Text.StartsWith("Phase 17C.3", StringComparison.Ordinal))
                    continue;
                text.Text = "Phase 17D.3 synchronizes the validated server-data animation snapshot to clients and binds persisted runtime:{asset}:{component} TargetIds directly to the matching source renderer components. Existing Light / Status and Special Effect timelines are now consumed in game, while runtime Transform / Visibility tracks are evaluated with the same Step / Linear / Smooth rules as Studio. Machine logic and source predicates remain authoritative: Studio may transform or hide an existing component, visually override a status presentation, or suppress a real effect, but it cannot invent missing source behavior. Direct runtime-target transform authoring and parity preview inside Studio remain the next Phase 17D integration step.";
                break;
            }
        }
    }
}

using System.Windows.Controls;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _studioAnimationPhase17CLabelsInitialized;

    public void EnableForeverAnimationPhase17CLabels()
    {
        if (_studioAnimationPhase17CLabelsInitialized)
            return;
        _studioAnimationPhase17CLabelsInitialized = true;

        if (_studioAnimationViewport?.Parent is Grid viewportGrid)
        {
            foreach (var border in viewportGrid.Children.OfType<Border>())
            {
                if (border.Child is TextBlock text && text.Text.StartsWith("BLOCK ANIMATION · PHASE 17", StringComparison.Ordinal))
                    text.Text = "BLOCK ANIMATION · PHASE 17C.3";
            }
        }

        if (_studioAnimationRuntimeValueText?.Parent is StackPanel inspector)
        {
            foreach (var text in inspector.Children.OfType<TextBlock>())
            {
                if (!text.Text.StartsWith("Phase 17B stabilizes", StringComparison.Ordinal))
                    continue;

                text.Text = "Phase 17C.3 now separates five animation concerns on the stable Phase 17A TargetId foundation: interpolated Position / Rotation / Scale, target-local Visibility, target-local Material State limited to existing source texture slots, machine-wide source-backed Light / Status state, and source-backed Special Effects with suppression-only authoring. All discrete channels use deterministic Step/Hold behavior; Phase 17C.1 runtime-state controls remain the base state and source Java/project files remain unchanged.";
                break;
            }
        }
    }
}

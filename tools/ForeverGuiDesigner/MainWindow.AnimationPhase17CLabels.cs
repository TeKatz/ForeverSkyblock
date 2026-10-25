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
                    text.Text = "BLOCK ANIMATION · PHASE 17C";
            }
        }

        if (_studioAnimationRuntimeValueText?.Parent is StackPanel inspector)
        {
            foreach (var text in inspector.Children.OfType<TextBlock>())
            {
                if (!text.Text.StartsWith("Phase 17B stabilizes", StringComparison.Ordinal))
                    continue;

                text.Text = "Phase 17C.1 adds source-backed machine/runtime state on top of the stable Phase 17A TargetId and Phase 17B transform timeline. Runtime states are preview-only and select real Forever Skyblock renderer predicates; source Java and project files remain unchanged.";
                break;
            }
        }
    }
}

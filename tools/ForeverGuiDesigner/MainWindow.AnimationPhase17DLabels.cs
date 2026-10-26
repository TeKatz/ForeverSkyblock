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
                    text.Text = "BLOCK ANIMATION · PHASE 17D.1";
            }
        }

        if (_studioAnimationRuntimeValueText?.Parent is StackPanel inspector)
        {
            foreach (var text in inspector.Children.OfType<TextBlock>())
            {
                if (!text.Text.StartsWith("Phase 17C.3", StringComparison.Ordinal))
                    continue;
                text.Text = "Phase 17D.1 persists the complete Phase 17A–17C authoring model as versioned project animation data: stable TargetIds, transforms, Visibility, Material, source-backed Runtime State, Light / Status and Special Effects. Save uses Review / Validate / Apply, unresolved and legacy targets are preserved diagnostically, and Java renderer/runtime source remains unchanged.";
                break;
            }
        }
    }
}

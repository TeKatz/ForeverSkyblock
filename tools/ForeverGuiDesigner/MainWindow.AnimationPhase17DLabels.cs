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
                    text.Text = "BLOCK ANIMATION · PHASE 17D.2";
            }
        }

        if (_studioAnimationRuntimeValueText?.Parent is StackPanel inspector)
        {
            foreach (var text in inspector.Children.OfType<TextBlock>())
            {
                if (!text.Text.StartsWith("Phase 17C.3", StringComparison.Ordinal))
                    continue;
                text.Text = "Phase 17D.2 keeps the complete Phase 17A–17C authoring model in the versioned project format introduced by 17D.1 and adds the matching NeoForge server-data runtime loader. Datapack reloads validate and atomically register animation documents by asset; the dedicated-server GameTest path verifies real resource loading. Existing Java renderers remain authoritative until the explicit client sync / TargetId binding step in Phase 17D.3.";
                break;
            }
        }
    }
}

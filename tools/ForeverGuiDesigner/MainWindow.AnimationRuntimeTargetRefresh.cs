using System.ComponentModel;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17D.4 refresh glue. Legacy Phase-17 source refreshes still rebuild the target combo with the
/// JSON prefix first; this layer reapplies the stable runtime-target view after source/facing changes
/// and after a persisted project document has been loaded.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeTargetRefreshInitialized;

    public void EnableForeverAnimationRuntimeTargetRefresh()
    {
        if (_studioRuntimeTargetRefreshInitialized || !_studioRuntimeTargetAuthoringInitialized)
            return;
        _studioRuntimeTargetRefreshInitialized = true;

        if (_studioBlockStateCombo is not null)
            _studioBlockStateCombo.SelectionChanged += (_, _) => QueueStudioRuntimeTargetSelectorRefresh();
        if (_studioBlockFacingCombo is not null)
            _studioBlockFacingCombo.SelectionChanged += (_, _) => QueueStudioRuntimeTargetSelectorRefresh();

        if (_studioAnimationPersistenceStatusText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioAnimationPersistenceStatusText, (_, _) =>
                {
                    var text = _studioAnimationPersistenceStatusText.Text ?? string.Empty;
                    if (!text.StartsWith("LOADED", StringComparison.OrdinalIgnoreCase))
                        return;
                    QueueStudioRuntimeTargetSelectorRefresh();
                    QueueStudioRuntimeTargetPreview();
                });
        }
    }
}

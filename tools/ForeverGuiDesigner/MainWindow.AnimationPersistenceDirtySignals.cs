using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Supplements the canonical dirty comparison with mutation signals whose list selection can stay
/// unchanged (for example clearing a channel while no exact keyframe is selected).
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPersistenceDirtySignalsInitialized;

    public void EnableForeverAnimationPersistenceDirtySignals()
    {
        if (_studioAnimationPersistenceDirtySignalsInitialized || !_studioAnimationPersistenceDirtyTrackingInitialized)
            return;
        _studioAnimationPersistenceDirtySignalsInitialized = true;

        if (_studioAnimationStatusText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioAnimationStatusText, (_, _) =>
                {
                    var text = _studioAnimationStatusText.Text ?? string.Empty;
                    if (IsStudioAnimationPersistenceMutationStatus(text))
                        QueueStudioAnimationPersistenceDirtyRecompute();
                });
        }

        if (_studioRuntimeStateSummaryText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioRuntimeStateSummaryText, (_, _) =>
                    QueueStudioAnimationPersistenceDirtyRecompute());
        }
    }

    private static bool IsStudioAnimationPersistenceMutationStatus(string text)
    {
        string[] signals =
        [
            "Keyframe stored",
            "Auto-keyed",
            "Neutral transform keyed",
            "Visibility keyed",
            "Material keyed",
            "Pasted keyframe",
            "Moved keyframe",
            "Keyframe removed",
            "Animation track cleared",
            "Status keyed",
            "Status keyframe removed",
            "Status channel cleared",
            "Effect keyed",
            "Effect keyframe removed",
            "Effect channel cleared"
        ];
        return signals.Any(signal => text.Contains(signal, StringComparison.OrdinalIgnoreCase));
    }
}

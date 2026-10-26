using System.Windows;

namespace ForeverGuiDesigner;

/// <summary>
/// Garden's transparent shell must never be the last frame submitted during transport playback.
/// Phase 17C owns the runtime scene, so finalize Garden synchronously after the legacy transport tick.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeStateGardenFinalizerInitialized;

    public void EnableForeverRuntimeStateGardenFinalizer()
    {
        if (_studioRuntimeStateGardenFinalizerInitialized || _studioAnimationTimer is null)
            return;

        _studioRuntimeStateGardenFinalizerInitialized = true;
        _studioAnimationTimer.Tick += (_, _) =>
        {
            if (_studioAnimationSurface?.Visibility != Visibility.Visible ||
                ResolveCurrentStudioRuntimeAssetId() != "garden_chamber_mark_i")
                return;

            RenderStudioRuntimeStatePreview(_studioAnimationTimeline?.Value ?? 0);
        };
    }
}

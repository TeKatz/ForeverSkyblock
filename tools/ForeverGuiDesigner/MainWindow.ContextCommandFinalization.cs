using System.Windows;

namespace ForeverGuiDesigner;

/// <summary>
/// Deterministic finalization for explicit editor command overrides.
///
/// The first repair pass used ContextIdle -> SystemIdle to restore user overrides after
/// the source-faithful machine owners. That ordering is not reliable while the editor's
/// render-priority timeline tick remains active, so text/Z overrides could be written
/// correctly and then immediately replaced by the authoritative machine renderer.
///
/// This handler is registered after every machine presentation owner and after the
/// original context-command repair handler. It performs no deferred dispatching: on a
/// layout generation where an existing owner changed the visual tree, explicit user
/// overrides are restored synchronously as the final handler in that same event.
/// The underlying repair methods are signature/Z guarded, so an already-correct visual
/// is a no-op and cannot sustain a LayoutUpdated loop.
/// </summary>
public partial class MainWindow
{
    private bool _contextCommandFinalizationInitialized;
    private bool _contextCommandFinalizationApplying;

    public void EnableContextCommandFinalizationFix()
    {
        if (_contextCommandFinalizationInitialized)
            return;
        _contextCommandFinalizationInitialized = true;

        // Disable the older idle-based restoration path. CUT/Delete and custom texture
        // already apply immediately; any later source/render overwrite is handled by
        // the deterministic finalizer below.
        _contextCommandRepairQueued = true;

        PreviewCanvas.LayoutUpdated += ContextCommandFinalizationLayoutUpdated;

        if (HasContextCommandOverrides())
            ApplyContextCommandFinalization();
    }

    private void ContextCommandFinalizationLayoutUpdated(object? sender, EventArgs e)
    {
        if (!HasContextCommandOverrides())
            return;

        ApplyContextCommandFinalization();
    }

    private void ApplyContextCommandFinalization()
    {
        if (_contextCommandFinalizationApplying || !_contextCommandRepairsInitialized)
            return;

        _contextCommandFinalizationApplying = true;
        try
        {
            ApplyContextCommandRepairPass();
        }
        finally
        {
            _contextCommandFinalizationApplying = false;
        }
    }
}

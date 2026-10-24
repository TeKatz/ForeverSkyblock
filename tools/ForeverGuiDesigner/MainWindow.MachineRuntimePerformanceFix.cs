using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Performance coordinator for the source-faithful non-Washing machine preview.
///
/// The original polish pass listened to both PreviewCanvas.LayoutUpdated and the
/// always-running Phase 8 16 ms timeline timer. Once a non-Washing machine was
/// active that meant a full text/button/slot restyle on virtually every frame.
/// Replacing Border.Child during that restyle invalidated WPF layout again, so the
/// LayoutUpdated hook could sustain the work even while the editor was otherwise idle.
///
/// Keep the old pass gated for compatibility and drive the source-parity renderer
/// from explicit state changes plus a cheap editable-visual generation detector.
/// Timeline progress only rebuilds the background/process overlay and is coalesced
/// to one-percent buckets; static controls are left untouched while animation advances.
/// </summary>
public partial class MainWindow
{
    private bool _machineRuntimePerformanceFixInitialized;
    private bool _machineRuntimePerformanceRefreshQueued;
    private bool _machineRuntimePerformanceStaticDirty;
    private FrameworkElement? _machineRuntimePerformanceVisualAnchor;
    private int _machineRuntimePerformanceVisualCount = -1;
    private string? _machineRuntimePerformanceStaticSignature;
    private string? _machineRuntimePerformanceDynamicSignature;

    public void EnableMachineRuntimePerformanceFix()
    {
        if (_machineRuntimePerformanceFixInitialized)
            return;

        _machineRuntimePerformanceFixInitialized = true;

        // Permanently gate the legacy QueueMachineRuntimeVisualPolish callbacks.
        // The optimized coordinator below calls the underlying source-style helpers
        // directly and therefore does not need to reopen this re-entrancy flag.
        _machineRuntimePolishApplying = true;

        // Phase 9 validation remains automatic, but a 500 ms polling heartbeat is
        // unnecessarily aggressive while animated machine previews are open.
        if (_phase9MaintenanceTimer is not null)
            _phase9MaintenanceTimer.Interval = TimeSpan.FromSeconds(2);

        CaptureMachineRuntimePerformanceGeneration();
        PreviewCanvas.LayoutUpdated += MachineRuntimePerformanceLayoutUpdated;

        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueMachineRuntimePerformanceRefresh(staticRefresh: true);
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueueMachineRuntimePerformanceRefresh(staticRefresh: true);
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueueMachineRuntimePerformanceRefresh(staticRefresh: true);
        if (_phase8TimelineSlider is not null)
            _phase8TimelineSlider.ValueChanged += (_, _) => QueueMachineRuntimePerformanceRefresh(staticRefresh: false);

        QueueMachineRuntimePerformanceRefresh(staticRefresh: true);
    }

    private void MachineRuntimePerformanceLayoutUpdated(object? sender, EventArgs e)
    {
        if (!MachineRuntimePerformanceGenerationChanged())
            return;

        // Capture immediately so the many LayoutUpdated notifications belonging to
        // one RenderLayout generation collapse into one background-priority refresh.
        CaptureMachineRuntimePerformanceGeneration();
        QueueMachineRuntimePerformanceRefresh(staticRefresh: true);
    }

    private bool MachineRuntimePerformanceGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _machineRuntimePerformanceVisualCount ||
               !ReferenceEquals(anchor, _machineRuntimePerformanceVisualAnchor);
    }

    private void CaptureMachineRuntimePerformanceGeneration()
    {
        _machineRuntimePerformanceVisualCount = _visuals.Count;
        _machineRuntimePerformanceVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private void QueueMachineRuntimePerformanceRefresh(bool staticRefresh)
    {
        _machineRuntimePerformanceStaticDirty |= staticRefresh;
        if (_machineRuntimePerformanceRefreshQueued)
            return;

        _machineRuntimePerformanceRefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _machineRuntimePerformanceRefreshQueued = false;
            if (!IsLoaded)
                return;
            ApplyMachineRuntimePerformanceRefresh();
        }));
    }

    private void ApplyMachineRuntimePerformanceRefresh()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
        {
            if (PreviewCanvas.Children.OfType<FrameworkElement>()
                .Any(element => Equals(element.Tag, MachineRuntimePolishTag)))
            {
                RemoveMachineRuntimePolishOverlay();
            }

            _machineRuntimePerformanceStaticDirty = false;
            _machineRuntimePerformanceStaticSignature = null;
            _machineRuntimePerformanceDynamicSignature = null;
            _machineRuntimePolishSignature = null;
            CaptureMachineRuntimePerformanceGeneration();
            return;
        }

        var staticSignature = string.Join('|',
            ActiveRuntimeScreen,
            CurrentTier,
            CurrentMode,
            CurrentMachineRuntimeState,
            _layout.Width,
            _layout.Height,
            _visuals.Count);

        var needsStaticRefresh = _machineRuntimePerformanceStaticDirty ||
                                 !string.Equals(
                                     staticSignature,
                                     _machineRuntimePerformanceStaticSignature,
                                     StringComparison.Ordinal);

        if (needsStaticRefresh)
        {
            _machineRuntimePerformanceStaticDirty = false;

            // Resolve the Condenser presentation from a real compatible recipe before
            // runtime text/timing is synchronized. Other machines keep their existing
            // source profile path and do not perform any recipe-directory scan.
            if (ActiveRuntimeScreen == "CondenserScreen")
                RefreshCondenserSourceRecipePreview();

            ApplyNonWashingSourceParityStatic();
            ApplySourceMachineGeometry();
            HideApproximationTextures();
            ApplySourceMachineSlotStyling();
            ApplySourceMachineButtonStyling();
            HideLegacyInventoryLabels();

            _machineRuntimePerformanceStaticSignature = staticSignature;
            CaptureMachineRuntimePerformanceGeneration();
        }

        // Minecraft's GUI presentation does not benefit from rebuilding a WPF tree
        // at monitor refresh rate. One-percent progress buckets keep the animation
        // responsive while bounding allocations and layout invalidations.
        var progressBucket = Math.Clamp(
            (int)Math.Round(_phase8TimelineProgress * 100.0, MidpointRounding.AwayFromZero),
            0,
            100);
        var dynamicSignature = staticSignature + "|p=" + progressBucket;
        var overlayExists = PreviewCanvas.Children.OfType<FrameworkElement>()
            .Any(element => Equals(element.Tag, MachineRuntimePolishTag));

        if (!overlayExists ||
            !string.Equals(dynamicSignature, _machineRuntimePerformanceDynamicSignature, StringComparison.Ordinal))
        {
            RebuildNonWashingSourceParityOverlayWithRecipes();
            _machineRuntimePerformanceDynamicSignature = dynamicSignature;
            _machineRuntimePolishSignature = dynamicSignature;
        }
    }
}

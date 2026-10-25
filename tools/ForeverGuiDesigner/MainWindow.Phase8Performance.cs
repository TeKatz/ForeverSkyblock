using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _phase8PerformanceHotfixInitialized;
    private bool _phase8PerformanceRefreshQueued;
    private FrameworkElement? _phase8PerformanceVisualAnchor;
    private int _phase8PerformanceVisualCount = -1;

    public void EnableForeverPhase8PerformanceHotfix()
    {
        if (_phase8PerformanceHotfixInitialized)
            return;

        _phase8PerformanceHotfixInitialized = true;

        // Phase 8 originally performed a full hierarchy rebuild, visual decoration
        // pass and resize-handle remove/recreate cycle from PreviewCanvas.LayoutUpdated.
        // Those operations themselves invalidate layout, causing a self-sustaining
        // UI-thread storm during navigation, dragging and timeline playback.
        //
        // Keep that legacy callback permanently gated and replace it with a cheap
        // generation detector below. A full Phase 8 refresh now happens only after
        // RenderLayout() has actually replaced the editable visual generation.
        _phase8OverlayBusy = true;
        CapturePhase8VisualGeneration();
        PreviewCanvas.LayoutUpdated += Phase8PerformanceLayoutUpdated;

        // RenderLayout is intentionally not used while dragging. Keep resize handles
        // following the selected element with only coordinate mutations; this does
        // not allocate controls or rebuild the hierarchy.
        PreviewCanvas.AddHandler(UIElement.MouseMoveEvent,
            new MouseEventHandler(Phase8PerformanceMouseMove), true);
        PreviewCanvas.AddHandler(UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler(Phase8PerformanceMouseUp), true);
    }

    private void Phase8PerformanceLayoutUpdated(object? sender, EventArgs e)
    {
        if (!Phase8VisualGenerationChanged())
            return;

        // Capture immediately so the many LayoutUpdated notifications belonging to
        // the same WPF layout pass collapse into one queued refresh.
        CapturePhase8VisualGeneration();
        QueuePhase8GenerationRefresh();
    }

    private bool Phase8VisualGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _phase8PerformanceVisualCount ||
               !ReferenceEquals(anchor, _phase8PerformanceVisualAnchor);
    }

    private void CapturePhase8VisualGeneration()
    {
        _phase8PerformanceVisualCount = _visuals.Count;
        _phase8PerformanceVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private void QueuePhase8GenerationRefresh()
    {
        if (_phase8PerformanceRefreshQueued)
            return;

        _phase8PerformanceRefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _phase8PerformanceRefreshQueued = false;
            if (!IsLoaded)
                return;

            // Do not reopen the legacy LayoutUpdated path. Refreshing here is safe:
            // resize handles and decorators may invalidate WPF layout, but the
            // editable visual generation itself does not change, so no second full
            // refresh is queued.
            _phase8OverlayBusy = true;
            RefreshPhase8Ui();
            CapturePhase8VisualGeneration();
        }));
    }

    private void Phase8PerformanceMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed || _selection.Count != 1)
            return;

        PositionPhase8HandlesWithoutRebuild();
    }

    private void Phase8PerformanceMouseUp(object sender, MouseButtonEventArgs e)
        => PositionPhase8HandlesWithoutRebuild();

    private void PositionPhase8HandlesWithoutRebuild()
    {
        if (_selection.Count != 1 || _phase8ResizeHandles.Count != 8)
            return;

        var model = _selection.First();
        if (model.Locked || !model.Visible)
            return;

        PositionPhase8ResizeHandles(model);
    }
}

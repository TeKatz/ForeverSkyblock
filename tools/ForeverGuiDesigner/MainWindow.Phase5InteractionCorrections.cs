using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _phase5LoopGuardScheduled;

    public void EnableForeverPhase5InteractionCorrections()
    {
        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            CorrectGardenRecipeVisibility();
            SuppressRedundantPhase5OverlayRefresh();
        };
    }

    private void CorrectGardenRecipeVisibility()
    {
        if (!IsCondenserPhase5Active)
            return;

        foreach (var pair in _visuals)
        {
            if (pair.Key.Id == "recipes")
            {
                pair.Value.Visibility = CurrentMode == "garden" ? Visibility.Collapsed : Visibility.Visible;
                if (CurrentMode != "garden")
                {
                    Canvas.SetLeft(pair.Value, pair.Key.X);
                    Canvas.SetTop(pair.Value, pair.Key.Y);
                }
            }
        }
    }

    private void SuppressRedundantPhase5OverlayRefresh()
    {
        if (_phase5LoopGuardScheduled || _phase5Refreshing || !_phase5RefreshQueued)
            return;

        var overlayExists = PreviewCanvas.Children
            .OfType<FrameworkElement>()
            .Any(child => string.Equals(child.Tag as string, Phase5OverlayTag, StringComparison.Ordinal));
        if (!overlayExists)
            return;

        _phase5LoopGuardScheduled = true;
        _phase5Refreshing = true;
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            _phase5Refreshing = false;
            _phase5LoopGuardScheduled = false;
        }));
    }
}

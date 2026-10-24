using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Removes the source Mixing-only "attach a Freezing Chamber" hint when the Mark I
/// attachment preview explicitly contains both Mixing and Freezing. The existing source
/// parity canvas stays authoritative for every other state.
/// </summary>
public partial class MainWindow
{
    private const string CondenserCombinedAttachmentMaskTag = "__forever_condenser_combined_attachment_mask_v1";

    private bool _condenserCombinedAttachmentPolishInitialized;
    private Canvas? _condenserCombinedAttachmentSourceCanvas;

    public void EnableCondenserCombinedAttachmentPolish()
    {
        if (_condenserCombinedAttachmentPolishInitialized)
            return;
        _condenserCombinedAttachmentPolishInitialized = true;

        if (_attachmentModeCombo is not null)
            _attachmentModeCombo.SelectionChanged += (_, _) => QueueCondenserCombinedAttachmentPolish();
        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueCondenserCombinedAttachmentPolish();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueueCondenserCombinedAttachmentPolish();
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueueCondenserCombinedAttachmentPolish();

        PreviewCanvas.LayoutUpdated += (_, _) => EnsureCondenserCombinedAttachmentPolish();
        QueueCondenserCombinedAttachmentPolish();
    }

    private void QueueCondenserCombinedAttachmentPolish()
        => Dispatcher.BeginInvoke(DispatcherPriority.Background,
            new Action(EnsureCondenserCombinedAttachmentPolish));

    private void EnsureCondenserCombinedAttachmentPolish()
    {
        var combined = IsCondenserPhase5Active &&
                       CurrentTier == "mk1" &&
                       string.Equals(_condenserMk1AttachmentMode, "mixing_freezing", StringComparison.Ordinal);

        if (!combined)
        {
            RemoveCondenserCombinedAttachmentMask();
            _condenserCombinedAttachmentSourceCanvas = null;
            return;
        }

        var sourceCanvas = PreviewCanvas.Children
            .OfType<Canvas>()
            .FirstOrDefault(canvas => Equals(canvas.Tag, MachineRuntimePolishTag));
        if (sourceCanvas is null)
            return;

        var existingMask = PreviewCanvas.Children
            .OfType<Canvas>()
            .FirstOrDefault(canvas => Equals(canvas.Tag, CondenserCombinedAttachmentMaskTag));
        if (ReferenceEquals(_condenserCombinedAttachmentSourceCanvas, sourceCanvas) && existingMask is not null)
            return;

        RemoveCondenserCombinedAttachmentMask();

        var mask = new Canvas
        {
            Tag = CondenserCombinedAttachmentMaskTag,
            Width = _layout.Width,
            Height = _layout.Height,
            Background = System.Windows.Media.Brushes.Transparent,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };

        // Mixing source parity normally places the MK I "attach Freezing" helper in
        // this panel. Repaint only that exact panel with the same source-parity helper;
        // runtime Freezing label/meter/text and machine slots remain above it at their
        // existing semantic Z layers.
        DrawSourcePanel(mask, 14, 113, 220, 43, "#465A68");
        Panel.SetZIndex(mask, 570);
        PreviewCanvas.Children.Add(mask);
        _condenserCombinedAttachmentSourceCanvas = sourceCanvas;

        // A source overlay rebuild can have re-applied Mixing visibility immediately
        // before this mask. Restore the combined runtime elements in the same pass.
        ApplyCondenserAttachmentOverride();
    }

    private void RemoveCondenserCombinedAttachmentMask()
    {
        foreach (var child in PreviewCanvas.Children
                     .OfType<Canvas>()
                     .Where(canvas => Equals(canvas.Tag, CondenserCombinedAttachmentMaskTag))
                     .ToList())
        {
            PreviewCanvas.Children.Remove(child);
        }
    }
}

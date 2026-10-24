using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Final interaction owner for source-faithful buttons on Crucible, Refining,
/// Ascension and Condenser previews.
///
/// Two older editor paths used to fight the source renderer:
///  1. the first RenderLayout generation could finish with generic button chrome,
///     so the source frame only appeared after the first MouseEnter;
///  2. the legacy runtime selection synchronizer called RestoreMachineRuntimeChrome
///     for every canvas click, restyling every button together.
///
/// This owner fixes both without touching Washing or the stable animation layer.
/// It applies source button chrome once after a new visual generation/state variant,
/// and suppresses only the obsolete global presentation callback for the duration of
/// one routed canvas MouseDown. Selection itself remains owned by
/// RuntimeSelectionStability and continues to use adorners only.
/// </summary>
public partial class MainWindow
{
    private bool _nonWashingSourceButtonOwnerInitialized;
    private bool _nonWashingSourceButtonOwnerQueued;
    private FrameworkElement? _nonWashingSourceButtonVisualAnchor;
    private int _nonWashingSourceButtonVisualCount = -1;

    private bool _nonWashingSourceButtonMouseSuppressionActive;
    private bool _nonWashingSourceButtonPreviousMachineApplying;

    public void EnableNonWashingSourceButtonOwnership()
    {
        if (_nonWashingSourceButtonOwnerInitialized)
            return;
        _nonWashingSourceButtonOwnerInitialized = true;

        CaptureNonWashingSourceButtonGeneration();

        // RuntimeSelectionStability is registered before this owner. It completes the
        // adorner-only selection during the tunnelling event. We then hold the legacy
        // runtime presentation writer until the matching bubbling event has passed.
        PreviewCanvas.AddHandler(
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(BeginNonWashingSourceButtonMouseSuppression),
            true);
        PreviewCanvas.AddHandler(
            UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(EndNonWashingSourceButtonMouseSuppression),
            true);

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (!NonWashingSourceButtonGenerationChanged())
                return;

            CaptureNonWashingSourceButtonGeneration();
            QueueNonWashingSourceButtonOwnership();
        };

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } load)
        {
            load.Click += (_, _) => QueueNonWashingSourceButtonOwnership();
        }

        ApplyButton.Click += (_, _) => QueueNonWashingSourceButtonOwnership();

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueueNonWashingSourceButtonOwnership();
        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueNonWashingSourceButtonOwnership();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueueNonWashingSourceButtonOwnership();
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueueNonWashingSourceButtonOwnership();

        QueueNonWashingSourceButtonOwnership();
    }

    private void BeginNonWashingSourceButtonMouseSuppression(object sender, MouseButtonEventArgs e)
    {
        if (_nonWashingSourceButtonMouseSuppressionActive ||
            !IsExclusiveNonWashingMachineRuntimeActive)
            return;

        _nonWashingSourceButtonMouseSuppressionActive = true;
        _nonWashingSourceButtonPreviousMachineApplying = _machineRuntimeApplying;
        _machineRuntimeApplying = true;
    }

    private void EndNonWashingSourceButtonMouseSuppression(object sender, MouseButtonEventArgs e)
    {
        if (!_nonWashingSourceButtonMouseSuppressionActive)
            return;

        _machineRuntimeApplying = _nonWashingSourceButtonPreviousMachineApplying;
        _nonWashingSourceButtonMouseSuppressionActive = false;
    }

    private void QueueNonWashingSourceButtonOwnership()
    {
        if (_nonWashingSourceButtonOwnerQueued)
            return;

        _nonWashingSourceButtonOwnerQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            _nonWashingSourceButtonOwnerQueued = false;
            ApplyNonWashingSourceButtonOwnership();
        }));
    }

    private void ApplyNonWashingSourceButtonOwnership()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        // This is intentionally the existing source renderer rather than a third
        // button skin. It also installs the per-Border MouseEnter/MouseLeave hooks;
        // the HashSet in MachineRuntimePolish keeps those hooks single-owner.
        ApplySourceMachineButtonStyling();
        CaptureNonWashingSourceButtonGeneration();
    }

    private bool NonWashingSourceButtonGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _nonWashingSourceButtonVisualCount ||
               !ReferenceEquals(anchor, _nonWashingSourceButtonVisualAnchor);
    }

    private void CaptureNonWashingSourceButtonGeneration()
    {
        _nonWashingSourceButtonVisualCount = _visuals.Count;
        _nonWashingSourceButtonVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}

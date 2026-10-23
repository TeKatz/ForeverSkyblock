using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private const string ActiveProcessingLabel = "Active Processing";

    private DispatcherTimer? _phase7ProcessingTimer;
    private ComboBoxItem? _phase7ActiveProcessingItem;
    private double _phase7ProcessingClock;
    private bool _phase7Initialized;

    public void EnableForeverPhase7Tools()
    {
        if (_phase7Initialized)
            return;

        _phase7Initialized = true;

        if (_foreverProgressCombo is not null)
        {
            // The old 72 px box was sufficient for percentages, but the live
            // processing mode should be readable instead of being truncated.
            _foreverProgressCombo.Width = 132;
            _foreverProgressCombo.ToolTip =
                "Preview a fixed progress amount or simulate a continuously running machine process.";

            _phase7ActiveProcessingItem = ComboItem(ActiveProcessingLabel, "0");
            _foreverProgressCombo.Items.Add(_phase7ActiveProcessingItem);
            _foreverProgressCombo.SelectionChanged += (_, _) =>
            {
                if (IsActiveProcessingSelected)
                {
                    _phase7ProcessingClock = 0;
                    _phase7ActiveProcessingItem.Tag = "0";

                    // A live Washing preview represents an actively processing
                    // machine, so switch away from Idle/Blocked/No water when the
                    // user explicitly asks for continuous processing.
                    if (IsWashingPhase6Active && _phase6WashingStatusCombo is not null)
                        _phase6WashingStatusCombo.SelectedIndex = 0;
                }

                HideGeneratedPanelMetadata();
                UpdatePhase7ProgressVisuals();
            };
        }

        // Phase 5 intentionally gives editable panels a human-readable Content
        // value. Imported/runtime panels use that value as editor metadata only;
        // it must never leak into the WYSIWYG preview as visible GUI text.
        PreviewCanvas.LayoutUpdated += (_, _) => HideGeneratedPanelMetadata();

        _phase7ProcessingTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _phase7ProcessingTimer.Tick += (_, _) =>
        {
            HideGeneratedPanelMetadata();

            if (!IsActiveProcessingSelected || _phase7ActiveProcessingItem is null)
                return;

            _phase7ProcessingClock += .05;
            const double cycleSeconds = 5.0;
            var progress = (_phase7ProcessingClock % cycleSeconds) / cycleSeconds;

            // CurrentProgress already reads the selected item's numeric Tag. By
            // updating that tag in-place all existing machine preview code sees
            // the same continuous progress value without changing selections or
            // rebuilding the Canvas.
            _phase7ActiveProcessingItem.Tag =
                (progress * 100.0).ToString("0.###", CultureInfo.InvariantCulture);

            UpdatePhase7ProgressVisuals();
        };
        _phase7ProcessingTimer.Start();

        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            HideGeneratedPanelMetadata();
            UpdatePhase7ProgressVisuals();
        }));
    }

    private bool IsActiveProcessingSelected
        => _phase7ActiveProcessingItem is not null &&
           ReferenceEquals(_foreverProgressCombo?.SelectedItem, _phase7ActiveProcessingItem);

    private void UpdatePhase7ProgressVisuals()
    {
        if (IsWashingPhase6Active)
        {
            EnsureStableWashingAnimationVisuals();
            UpdateStableWashingAnimationVisuals();

            foreach (var pair in _visuals)
            {
                if (!string.Equals(pair.Key.Id, "forever_phase6_washing_time", StringComparison.OrdinalIgnoreCase) ||
                    pair.Value is not Border { Child: TextBlock text })
                    continue;

                text.Text = $"{CurrentProgress * 5.0:0.0} / 5.0 s";
            }
        }

        // Phase 5 meters are simple persistent Grid + fill Border structures.
        // Update only their fill width so Active Processing stays flicker-free.
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (!IsForeverMeter(model) || pair.Value is not Border border)
                continue;

            if (string.Equals(model.Id, "forever_phase6_washing_meter_progress", StringComparison.OrdinalIgnoreCase))
                continue;

            if (border.Child is not Grid grid)
                continue;

            var fill = grid.Children.OfType<Border>()
                .FirstOrDefault(child => child.HorizontalAlignment == HorizontalAlignment.Left);
            if (fill is null)
                continue;

            fill.Width = Math.Max(0, (model.Width - 2) * CurrentProgress);
        }
    }

    private void HideGeneratedPanelMetadata()
    {
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            // Free widgets created by the user may intentionally retain their
            // helper label. Imported/generated Forever elements must look like
            // the actual machine GUI, not like annotated editor mockups.
            if (model.Id.StartsWith("forever_widget_", StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsForeverPanel(model) && border.Child is Grid panelGrid)
            {
                foreach (var text in panelGrid.Children.OfType<TextBlock>())
                    text.Opacity = 0;
            }

            // Also suppress generic Texture fallback labels for generated visual
            // anchors that deliberately have no PNG of their own.
            if (model.Type == Models.GuiElementType.Texture &&
                model.Id.StartsWith("forever_", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(model.TexturePath) &&
                border.Child is TextBlock fallback)
            {
                fallback.Opacity = 0;
            }
        }
    }
}

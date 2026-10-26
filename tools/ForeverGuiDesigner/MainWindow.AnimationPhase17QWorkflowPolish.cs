using System.Windows;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Final Phase-17Q workflow pass. It keeps established Phase-17B actions reachable after the shell
/// reflow and promotes interpolation to the command area without replacing either control or its
/// already-installed event handlers.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPhase17QWorkflowPolishInitialized;
    private TextBlock? _studioPhase17QInterpolationHint;

    public void EnableForeverAnimationPhase17QWorkflowPolish()
    {
        if (_studioAnimationPhase17QWorkflowPolishInitialized ||
            !_studioAnimationPhase17QInitialized ||
            _studioAnimationElementCombo?.Parent is not StackPanel targetCell ||
            targetCell.Parent is not WrapPanel commandRow)
            return;

        _studioAnimationPhase17QWorkflowPolishInitialized = true;
        CollapseStudioPhase17QLegacyDescription();
        PromoteStudioPhase17QMoveToTime(commandRow);
        PromoteStudioPhase17QInterpolation(commandRow);
    }

    private void CollapseStudioPhase17QLegacyDescription()
    {
        if (_studioAnimationStatusText?.Parent is not StackPanel inspector)
            return;

        foreach (var text in inspector.Children.OfType<TextBlock>())
        {
            if (text.Text.StartsWith("Phase 17B stabilizes", StringComparison.Ordinal) ||
                text.Text.StartsWith("Phase 17D.4 closes", StringComparison.Ordinal))
            {
                text.Visibility = Visibility.Collapsed;
                return;
            }
        }
    }

    private void PromoteStudioPhase17QMoveToTime(WrapPanel commandRow)
    {
        if (_studioAnimationTimeBox?.Parent is not Panel keyEditorParent)
            return;

        var legacyCommands = keyEditorParent.Children
            .OfType<WrapPanel>()
            .FirstOrDefault(panel => panel.Children.OfType<Button>().Any(button =>
                string.Equals(button.Content?.ToString(), "Move to time", StringComparison.Ordinal)));
        var moveButton = legacyCommands?.Children
            .OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Move to time", StringComparison.Ordinal));
        var topKeyCell = commandRow.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => panel.Children.OfType<TextBlock>().Any(text =>
                string.Equals(text.Text, "KEYFRAMES", StringComparison.Ordinal)));
        var topKeyCommands = topKeyCell?.Children.OfType<WrapPanel>().FirstOrDefault();
        if (moveButton is null || topKeyCommands is null)
            return;

        DetachStudioPhase17QControl(moveButton);
        moveButton.Content = "Move to time";
        moveButton.Width = 98;
        moveButton.Height = 30;
        moveButton.Margin = new Thickness(5, 0, 0, 0);
        moveButton.ToolTip =
            "Move the selected keyframe to the time in the keyframe editor. Existing keys are never overwritten silently.";
        topKeyCommands.Children.Add(moveButton);

        if (legacyCommands is not null)
            legacyCommands.Visibility = Visibility.Collapsed;
    }

    private void PromoteStudioPhase17QInterpolation(WrapPanel commandRow)
    {
        if (_studioAnimationInterpolationCombo is null)
            return;

        var oldParent = _studioAnimationInterpolationCombo.Parent as Panel;
        if (oldParent is not null)
        {
            var oldLabel = oldParent.Children
                .OfType<TextBlock>()
                .FirstOrDefault(text => string.Equals(text.Text, "Interpolation", StringComparison.OrdinalIgnoreCase));
            if (oldLabel is not null)
                oldLabel.Visibility = Visibility.Collapsed;
        }

        DetachStudioPhase17QControl(_studioAnimationInterpolationCombo);
        var cell = new StackPanel
        {
            Width = 145,
            Margin = new Thickness(0, 0, 12, 5)
        };
        cell.Children.Add(StudioPhase17QMiniLabel("INTERPOLATION"));
        _studioAnimationInterpolationCombo.Width = double.NaN;
        _studioAnimationInterpolationCombo.Margin = new Thickness(0);
        _studioAnimationInterpolationCombo.ToolTip =
            "Step: hold the previous transform. Linear: constant blend. Smooth: eased blend between transform keys.";
        cell.Children.Add(_studioAnimationInterpolationCombo);

        _studioPhase17QInterpolationHint = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 8,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(1, 3, 0, 0)
        };
        cell.Children.Add(_studioPhase17QInterpolationHint);
        _studioAnimationInterpolationCombo.SelectionChanged += (_, _) => RefreshStudioPhase17QInterpolationHint();
        RefreshStudioPhase17QInterpolationHint();

        var viewCell = commandRow.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => panel.Children.OfType<TextBlock>().Any(text =>
                string.Equals(text.Text, "VIEW", StringComparison.Ordinal)));
        var insertAt = viewCell is null ? commandRow.Children.Count : commandRow.Children.IndexOf(viewCell);
        commandRow.Children.Insert(Math.Max(0, insertAt), cell);
    }

    private void RefreshStudioPhase17QInterpolationHint()
    {
        if (_studioPhase17QInterpolationHint is null)
            return;

        _studioPhase17QInterpolationHint.Text = _studioAnimationInterpolationCombo?.SelectedItem?.ToString() switch
        {
            "Step" => "Hold until next key",
            "Linear" => "Constant blend",
            _ => "Eased blend"
        };
    }
}

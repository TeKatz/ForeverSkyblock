using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Shared Machine Builder presentation polish. Keeps custom Studio button chrome independent from
/// the default WPF Button template, balances the Function template browser and adds restrained
/// interaction feedback to workflow/template cards without changing authoring behavior.
/// </summary>
public partial class MainWindow
{
    private const string MachineBuilderUxChromeMarker = "ForeverStudio.MachineBuilder.ContentOnlyButton";
    private const string MachineBuilderUxInteractionMarker = "ForeverStudio.MachineBuilder.ButtonInteraction";
    private const string MachineBuilderUxCardMarker = "ForeverStudio.MachineBuilder.InteractiveCard";
    private const string MachineBuilderUxLayoutMarker = "ForeverStudio.MachineBuilder.TemplateLayout";

    private bool _machineBuilderUxPolishInitialized;

    public void EnableForeverMachineBuilderUxPolish()
    {
        if (_machineBuilderUxPolishInitialized)
            return;
        _machineBuilderUxPolishInitialized = true;

        // Normalize controls that already exist, then handle future Studio buttons exactly once when
        // they enter the visual tree. Do not poll LayoutUpdated: it is raised continuously by WPF and
        // a full-tree walk from that event becomes an unbounded UI-thread hot path during long sessions.
        if (_studioRoot is not null)
            NormalizeMachineBuilderStudioButtons(_studioRoot);

        EventManager.RegisterClassHandler(
            typeof(Button),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is Button button)
                    NormalizeMachineBuilderStudioButton(button);
            }),
            true);

        // Some editor surfaces materialize controls inside already-loaded or collapsed hosts. A
        // pointer-entry repair is therefore the final guard against the platform template resurfacing.
        // The normalizer is idempotent and ignores ordinary buttons, so this never becomes a tree scan.
        EventManager.RegisterClassHandler(
            typeof(Button),
            UIElement.MouseEnterEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is Button button)
                    NormalizeMachineBuilderStudioButton(button);
            }),
            true);

        PolishMachineBuilderWorkflowSteps();
        PolishFunctionTemplateBrowserLayout();
        PolishFunctionTemplateCards();

        // One post-startup pass catches controls materialized by initialization code queued at the
        // same dispatcher turn. This is intentionally one-shot rather than a recurring refresh.
        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(ApplyMachineBuilderUxPolish));
    }

    private void ApplyMachineBuilderUxPolish()
    {
        if (_studioRoot is not null)
            NormalizeMachineBuilderStudioButtons(_studioRoot);

        PolishMachineBuilderWorkflowSteps();
        PolishFunctionTemplateBrowserLayout();
        PolishFunctionTemplateCards();
    }

    /// <summary>
    /// Studio chrome buttons render their own Border surface. The stock WPF Button template adds a
    /// second hover/pressed background behind that surface, which is especially visible on stretched
    /// buttons. Replacing only those custom-border buttons with a content-only template removes the
    /// conflicting platform chrome while preserving each button's existing handlers and animations.
    /// </summary>
    private static void NormalizeMachineBuilderStudioButtons(DependencyObject root)
    {
        foreach (var button in FindStudioVisualChildren<Button>(root))
            NormalizeMachineBuilderStudioButton(button);
    }

    private static void NormalizeMachineBuilderStudioButton(Button button)
    {
        if (button.Content is not Border)
            return;

        if (button.Resources[MachineBuilderUxChromeMarker] is ControlTemplate installedTemplate &&
            ReferenceEquals(button.Template, installedTemplate))
            return;

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

        var template = new ControlTemplate(typeof(Button))
        {
            VisualTree = presenter
        };
        button.Template = template;
        button.Resources[MachineBuilderUxChromeMarker] = template;
        button.ApplyTemplate();

        // CreateStudioChromeButton already owns a TranslateTransform animation. Other custom
        // border buttons get a tiny scale response here so the interaction language is shared.
        if (button.RenderTransform is not TranslateTransform &&
            !button.Resources.Contains(MachineBuilderUxInteractionMarker))
        {
            button.Resources[MachineBuilderUxInteractionMarker] = true;
            InstallMachineBuilderButtonMicroInteraction(button);
        }
    }

    private static void InstallMachineBuilderButtonMicroInteraction(Button button)
    {
        var scale = new ScaleTransform(1, 1);
        button.RenderTransform = scale;
        button.RenderTransformOrigin = new Point(0.5, 0.5);

        button.MouseEnter += (_, _) => AnimateMachineBuilderScale(scale, 1.012, 125);
        button.MouseLeave += (_, _) => AnimateMachineBuilderScale(scale, 1.0, 145);
        button.PreviewMouseLeftButtonDown += (_, _) => AnimateMachineBuilderScale(scale, 0.985, 65);
        button.PreviewMouseLeftButtonUp += (_, _) => AnimateMachineBuilderScale(scale, 1.012, 90);
    }

    private void PolishMachineBuilderWorkflowSteps()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;

        var workflowHeading = FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace)
            .FirstOrDefault(text => string.Equals(text.Text, "GUIDED WORKFLOW", StringComparison.Ordinal));
        if (workflowHeading is null || VisualTreeHelper.GetParent(workflowHeading) is not StackPanel panel)
            return;

        foreach (var step in panel.Children.OfType<Border>())
        {
            if (step.Child is not Grid grid || !IsMachineBuilderWorkflowStep(grid) ||
                step.Resources.Contains(MachineBuilderUxCardMarker))
                continue;

            step.Resources[MachineBuilderUxCardMarker] = true;
            step.CornerRadius = new CornerRadius(8);
            step.Margin = new Thickness(0, 0, 0, 9);
            step.Cursor = Cursors.Hand;
            step.SnapsToDevicePixels = true;

            var number = grid.Children.OfType<TextBlock>()
                .FirstOrDefault(text => text.Text is "01" or "02" or "03" or "04" or "05" or "06");
            if (number is not null)
            {
                number.FontSize = 9.5;
                number.Margin = new Thickness(1, 1, 0, 0);
                number.FontWeight = FontWeights.Bold;
            }

            InstallMachineBuilderCardInteraction(step, translateX: 3.0, hoverScale: 1.008);
        }
    }

    private static bool IsMachineBuilderWorkflowStep(Grid grid)
        => grid.Children.OfType<TextBlock>()
            .Any(text => text.Text is "01" or "02" or "03" or "04" or "05" or "06");

    private void PolishFunctionTemplateBrowserLayout()
    {
        if (_functionTemplatesOverlay is null || _functionTemplatesOverlay.Child is not Grid root)
            return;

        if (!_functionTemplatesOverlay.Resources.Contains(MachineBuilderUxLayoutMarker))
        {
            _functionTemplatesOverlay.Resources[MachineBuilderUxLayoutMarker] = true;

            var body = root.Children.OfType<Grid>()
                .FirstOrDefault(grid => Grid.GetRow(grid) == 2 && grid.ColumnDefinitions.Count == 3);
            if (body is not null)
            {
                // The original fixed 390px browser left the detail panel visually empty on wide screens.
                // A proportional split gives the growing template library more room while keeping detail
                // information readable rather than stretched over most of the window.
                body.ColumnDefinitions[0].Width = new GridLength(0.44, GridUnitType.Star);
                body.ColumnDefinitions[0].MinWidth = 390;
                body.ColumnDefinitions[1].Width = new GridLength(16);
                body.ColumnDefinitions[2].Width = new GridLength(0.56, GridUnitType.Star);
                body.ColumnDefinitions[2].MinWidth = 470;
            }

            if (_functionTemplatesDetail is not null)
            {
                _functionTemplatesDetail.Margin = new Thickness(20, 18, 20, 20);
                _functionTemplatesDetail.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
        }

        RebuildFunctionTemplateFilterGrid();
    }

    private void RebuildFunctionTemplateFilterGrid()
    {
        if (_functionTemplateCategoryButtons.Count == 0)
            return;

        var first = _functionTemplateCategoryButtons.Values.FirstOrDefault();
        if (first?.Parent is Grid alreadyGrid &&
            string.Equals(alreadyGrid.Tag?.ToString(), "function_template_filter_grid", StringComparison.Ordinal))
            return;

        if (first?.Parent is not Panel oldPanel || oldPanel.Parent is not Grid host)
            return;

        var row = Grid.GetRow(oldPanel);
        var column = Grid.GetColumn(oldPanel);
        var columnSpan = Grid.GetColumnSpan(oldPanel);
        var rowSpan = Grid.GetRowSpan(oldPanel);

        foreach (var button in _functionTemplateCategoryButtons.Values.ToList())
            oldPanel.Children.Remove(button);

        host.Children.Remove(oldPanel);

        var filters = new Grid
        {
            Tag = "function_template_filter_grid",
            Margin = new Thickness(0, 9, 0, 5)
        };
        for (var index = 0; index < 3; index++)
            filters.ColumnDefinitions.Add(new ColumnDefinition());
        filters.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        filters.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var order = new[] { "All", "Processing", "Energy", "Storage", "Control", "Utility" };
        for (var index = 0; index < order.Length; index++)
        {
            if (!_functionTemplateCategoryButtons.TryGetValue(order[index], out var button))
                continue;

            button.Width = double.NaN;
            button.Height = 29;
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Margin = new Thickness(
                index % 3 == 0 ? 0 : 3,
                index < 3 ? 0 : 3,
                index % 3 == 2 ? 0 : 3,
                0);
            Grid.SetColumn(button, index % 3);
            Grid.SetRow(button, index / 3);
            filters.Children.Add(button);
        }

        Grid.SetRow(filters, row);
        Grid.SetColumn(filters, column);
        Grid.SetColumnSpan(filters, columnSpan);
        Grid.SetRowSpan(filters, rowSpan);
        host.Children.Add(filters);
    }

    private void PolishFunctionTemplateCards()
    {
        if (_functionTemplatesList is null)
            return;

        foreach (var card in _functionTemplatesList.Children.OfType<Border>())
        {
            if (card.Resources.Contains(MachineBuilderUxCardMarker))
                continue;

            card.Resources[MachineBuilderUxCardMarker] = true;
            card.CornerRadius = new CornerRadius(7);
            card.Margin = new Thickness(2, 0, 2, 7);
            card.Padding = new Thickness(11, 10, 11, 10);
            InstallMachineBuilderCardInteraction(card, translateX: 3.5, hoverScale: 1.006);

            // The template's existing click handler runs first and refreshes the detail content.
            // This handler then animates the newly-created detail state into place.
            card.MouseLeftButtonUp += (_, _) => AnimateFunctionTemplateDetailEntrance();
        }
    }

    private static void InstallMachineBuilderCardInteraction(Border card, double translateX, double hoverScale)
    {
        var scale = new ScaleTransform(1, 1);
        var translate = new TranslateTransform();
        var transforms = new TransformGroup();
        transforms.Children.Add(scale);
        transforms.Children.Add(translate);
        card.RenderTransform = transforms;
        card.RenderTransformOrigin = new Point(0.5, 0.5);

        var shadow = new DropShadowEffect
        {
            Color = ColorFrom("#54D4D0"),
            BlurRadius = 13,
            ShadowDepth = 0,
            Opacity = 0
        };
        card.Effect = shadow;

        card.MouseEnter += (_, _) =>
        {
            AnimateMachineBuilderScale(scale, hoverScale, 130);
            AnimateMachineBuilderDouble(translate, TranslateTransform.XProperty, translateX, 130);
            AnimateMachineBuilderDouble(shadow, DropShadowEffect.OpacityProperty, 0.16, 150);
        };
        card.MouseLeave += (_, _) =>
        {
            AnimateMachineBuilderScale(scale, 1.0, 150);
            AnimateMachineBuilderDouble(translate, TranslateTransform.XProperty, 0, 150);
            AnimateMachineBuilderDouble(shadow, DropShadowEffect.OpacityProperty, 0, 170);
        };
        card.PreviewMouseLeftButtonDown += (_, _) =>
        {
            AnimateMachineBuilderScale(scale, 0.987, 65);
            AnimateMachineBuilderDouble(translate, TranslateTransform.XProperty, Math.Max(1, translateX - 1.5), 65);
        };
        card.PreviewMouseLeftButtonUp += (_, _) =>
        {
            AnimateMachineBuilderScale(scale, hoverScale, 90);
            AnimateMachineBuilderDouble(translate, TranslateTransform.XProperty, translateX, 90);
        };
    }

    private void AnimateFunctionTemplateDetailEntrance()
    {
        if (_functionTemplatesDetail is null)
            return;

        var translate = _functionTemplatesDetail.RenderTransform as TranslateTransform;
        if (translate is null)
        {
            translate = new TranslateTransform();
            _functionTemplatesDetail.RenderTransform = translate;
        }

        _functionTemplatesDetail.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0.72, 1.0, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        translate.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(5, 0, TimeSpan.FromMilliseconds(165))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private static void AnimateMachineBuilderScale(ScaleTransform scale, double value, int milliseconds)
    {
        var animation = new DoubleAnimation(value, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static void AnimateMachineBuilderDouble(
        Animatable target,
        DependencyProperty property,
        double value,
        int milliseconds)
    {
        target.BeginAnimation(
            property,
            new DoubleAnimation(value, TimeSpan.FromMilliseconds(milliseconds))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
    }
}
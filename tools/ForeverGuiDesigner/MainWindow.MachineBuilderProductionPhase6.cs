using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder Phase 6: cross-phase production review and transactional build/apply.
/// Review is always reachable so unsupported runtime authoring is visible as an explicit blocker.
/// </summary>
public partial class MainWindow
{
    private sealed class MachineBuilderProductionChangeItem
    {
        public required ForeverStudioMachineProductionChange Change { get; init; }
        public override string ToString() => $"{Change.Kind}  ·  {Change.RelativePath}";
    }

    private bool _machineBuilderPhase6Initialized;
    private Border? _machineBuilderPhase6Surface;
    private Border? _machineBuilderReviewStep;
    private TextBlock? _machineBuilderPhase6Readiness;
    private TextBlock? _machineBuilderPhase6Summary;
    private ListBox? _machineBuilderPhase6IssueList;
    private ListBox? _machineBuilderPhase6ChangeList;
    private TextBox? _machineBuilderPhase6Preview;
    private TextBlock? _machineBuilderPhase6PreviewTitle;
    private Button? _machineBuilderPhase6ApplyButton;
    private ForeverStudioMachineProductionPlan? _machineBuilderProductionPlan;

    public void EnableForeverMachineBuilderProductionPhase6()
    {
        if (_machineBuilderPhase6Initialized || _studioMachineBuilderWorkspace is not DockPanel root)
            return;
        _machineBuilderPhase6Initialized = true;

        var page = root.Children.OfType<Grid>().FirstOrDefault(grid => grid.ColumnDefinitions.Count >= 5);
        if (page is null)
            return;

        _machineBuilderPhase6Surface = BuildMachineBuilderPhase6Surface();
        Grid.SetColumn(_machineBuilderPhase6Surface, 2);
        Grid.SetColumnSpan(_machineBuilderPhase6Surface, 3);
        Panel.SetZIndex(_machineBuilderPhase6Surface, 60);
        _machineBuilderPhase6Surface.Visibility = Visibility.Collapsed;
        page.Children.Add(_machineBuilderPhase6Surface);

        InstallMachineBuilderPhase6Workflow(page);
        InstallMachineBuilderPhase6ContinueButton();
        UpdateMachineBuilderPhase6Labels();
    }

    private void InstallMachineBuilderPhase6Workflow(Grid page)
    {
        var workflowCard = page.Children.OfType<Border>()
            .FirstOrDefault(border => Grid.GetColumn(border) == 0 && border.Child is StackPanel panel &&
                                      panel.Children.OfType<TextBlock>().Any(text => text.Text == "GUIDED WORKFLOW"));
        if (workflowCard?.Child is not StackPanel workflow)
            return;

        _machineBuilderReviewStep = FindMachineBuilderWorkflowStep(workflow, "Review & Build");
        if (_machineBuilderReviewStep is not null)
        {
            _machineBuilderReviewStep.Cursor = Cursors.Hand;
            _machineBuilderReviewStep.Opacity = 1;
            if (_machineBuilderReviewStep.Child is Grid grid)
            {
                var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
                var description = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                if (description is not null)
                    description.Text = "Validate, review generated files and apply atomically";
            }
            _machineBuilderReviewStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderProductionPhase6();
            StyleMachineBuilderPhase3Step(_machineBuilderReviewStep, active: false, available: true);
        }

        void HideOn(Border? step)
        {
            if (step is not null)
                step.MouseLeftButtonUp += (_, _) => HideMachineBuilderProductionPhase6();
        }
        HideOn(_machineBuilderIdentityStep);
        HideOn(_machineBuilderVisualStep);
        HideOn(_machineBuilderFunctionStep);
        HideOn(_machineBuilderGuiStep);
        HideOn(_machineBuilderAnimationStep);
    }

    private void InstallMachineBuilderPhase6ContinueButton()
    {
        if (_machineBuilderPhase5Surface is null)
            return;
        var actions = FindStudioVisualChildren<StackPanel>(_machineBuilderPhase5Surface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                                     panel.Children.OfType<Button>().Any(button =>
                                         FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "SAVE PROJECT")));
        if (actions is null || actions.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "machine_builder_phase6_continue", StringComparison.Ordinal)))
            return;

        var review = CreateStudioChromeButton("REVIEW & BUILD  →", 146, false);
        review.Tag = "machine_builder_phase6_continue";
        review.Height = 36;
        review.Margin = new Thickness(9, 0, 0, 0);
        review.Click += (_, _) => ShowMachineBuilderProductionPhase6();
        var save = actions.Children.OfType<Button>().FirstOrDefault(button =>
            FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "SAVE PROJECT"));
        var index = save is null ? actions.Children.Count : actions.Children.IndexOf(save);
        actions.Children.Insert(Math.Max(0, index), review);
    }

    private void UpdateMachineBuilderPhase6Labels()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;
        foreach (var text in FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace))
        {
            if (text.Text.StartsWith("PHASE 5 ·", StringComparison.OrdinalIgnoreCase))
                text.Text = "PHASE 6 · REVIEW & PRODUCTION";
            else if (text.Text.StartsWith("Phase 5 binds graph data", StringComparison.OrdinalIgnoreCase))
                text.Text = "Phase 6 closes the guided workflow with cross-phase validation, an exact generated-file review and rollback-capable production apply.";
        }
    }

    private Border BuildMachineBuilderPhase6Surface()
    {
        var surface = new Border
        {
            Background = Brush("#0D1215"),
            BorderBrush = Brush("#293A42"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18)
        };
        var shell = new Grid();
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        shell.RowDefinitions.Add(new RowDefinition());
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock
        {
            Text = "REVIEW & BUILD · PHASE 6",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Turn a Machine Project into real mod content",
            Foreground = Brush("#EFF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Nothing is written until the complete plan is valid. Every target is hash-snapshotted and the apply step rolls the whole transaction back if one write fails.",
            Foreground = Brush("#899CA4"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        header.Children.Add(heading);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var back = CreateStudioChromeButton("← ANIMATION", 108, false);
        back.Height = 36;
        back.Click += (_, _) =>
        {
            HideMachineBuilderProductionPhase6();
            ShowMachineBuilderPhase5(MachineBuilderPhase5Mode.Animation);
        };
        actions.Children.Add(back);
        var save = CreateStudioChromeButton("SAVE PROJECT", 126, false);
        save.Height = 36;
        save.Margin = new Thickness(9, 0, 0, 0);
        save.Click += (_, _) =>
        {
            SaveMachineBuilderProject();
            RefreshMachineBuilderProductionReview();
        };
        actions.Children.Add(save);
        var refresh = CreateStudioChromeButton("REFRESH REVIEW", 138, false);
        refresh.Height = 36;
        refresh.Margin = new Thickness(9, 0, 0, 0);
        refresh.Click += (_, _) => RefreshMachineBuilderProductionReview();
        actions.Children.Add(refresh);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        shell.Children.Add(header);

        var summary = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var summaryGrid = new Grid { Margin = new Thickness(14, 11, 14, 11) };
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        _machineBuilderPhase6Readiness = new TextBlock
        {
            Text = "REVIEW NOT BUILT",
            Foreground = Brush("#D8B26A"),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        summaryGrid.Children.Add(_machineBuilderPhase6Readiness);
        _machineBuilderPhase6Summary = new TextBlock
        {
            Foreground = Brush("#8FA1A8"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_machineBuilderPhase6Summary, 2);
        summaryGrid.Children.Add(_machineBuilderPhase6Summary);
        summary.Child = summaryGrid;
        Grid.SetRow(summary, 2);
        shell.Children.Add(summary);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());

        var validationCard = Phase6Card("VALIDATION", "Cross-phase blockers and diagnostics.");
        var validationPanel = (StackPanel)validationCard.Child;
        _machineBuilderPhase6IssueList = Phase6ListBox();
        validationPanel.Children.Add(_machineBuilderPhase6IssueList);
        Grid.SetColumn(validationCard, 0);
        body.Children.Add(validationCard);

        var changesCard = Phase6Card("PRODUCTION PLAN", "Exact files that will be created or replaced.");
        var changesPanel = (StackPanel)changesCard.Child;
        _machineBuilderPhase6ChangeList = Phase6ListBox();
        _machineBuilderPhase6ChangeList.SelectionChanged += (_, _) => RefreshMachineBuilderPhase6Preview();
        changesPanel.Children.Add(_machineBuilderPhase6ChangeList);
        Grid.SetColumn(changesCard, 2);
        body.Children.Add(changesCard);

        var previewCard = Phase6Card("GENERATED PREVIEW", "Selected target after generation.");
        var previewPanel = (StackPanel)previewCard.Child;
        _machineBuilderPhase6PreviewTitle = new TextBlock
        {
            Foreground = Brush("#D6E2E6"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        previewPanel.Children.Add(_machineBuilderPhase6PreviewTitle);
        _machineBuilderPhase6Preview = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brush("#081014"),
            Foreground = Brush("#B9C8CD"),
            BorderBrush = Brush("#2C4149"),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 9.5,
            MinHeight = 360
        };
        previewPanel.Children.Add(_machineBuilderPhase6Preview);
        Grid.SetColumn(previewCard, 4);
        body.Children.Add(previewCard);

        Grid.SetRow(body, 4);
        shell.Children.Add(body);

        var footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.Children.Add(new TextBlock
        {
            Text = "Current production backend: static visual block. Runtime behavior/GUI/recipes/animations fail closed until their compilers are implemented.",
            Foreground = Brush("#82969E"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 20, 0)
        });
        _machineBuilderPhase6ApplyButton = CreateStudioChromeButton("BUILD / APPLY MACHINE", 190, true);
        _machineBuilderPhase6ApplyButton.Height = 40;
        _machineBuilderPhase6ApplyButton.IsEnabled = false;
        _machineBuilderPhase6ApplyButton.Click += (_, _) => ApplyMachineBuilderProductionPlan();
        Grid.SetColumn(_machineBuilderPhase6ApplyButton, 1);
        footer.Children.Add(_machineBuilderPhase6ApplyButton);
        Grid.SetRow(footer, 5);
        shell.Children.Add(footer);

        surface.Child = shell;
        return surface;
    }

    private Border Phase6Card(string title, string subtitle)
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Brush("#7F929A"),
            FontSize = 9.3,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 10)
        });
        card.Child = panel;
        return card;
    }

    private static ListBox Phase6ListBox()
        => new()
        {
            MinHeight = 390,
            Background = Brush("#081014"),
            Foreground = Brush("#C0CDD1"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5),
            FontSize = 9.5,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

    private void ShowMachineBuilderProductionPhase6()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        if (_machineBuilderIdentityScroll is not null) _machineBuilderIdentityScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderContextScroll is not null) _machineBuilderContextScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderVisualSurface is not null) _machineBuilderVisualSurface.Visibility = Visibility.Collapsed;
        if (_machineBuilderFunctionSurface is not null) _machineBuilderFunctionSurface.Visibility = Visibility.Collapsed;
        if (_machineBuilderPhase5Surface is not null) _machineBuilderPhase5Surface.Visibility = Visibility.Collapsed;
        if (_machineBuilderPhase6Surface is not null) _machineBuilderPhase6Surface.Visibility = Visibility.Visible;

        StyleMachineBuilderPhase3Step(_machineBuilderIdentityStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderVisualStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderFunctionStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderGuiStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderAnimationStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderReviewStep, active: true, available: true);
        RefreshMachineBuilderProductionReview();
    }

    private void HideMachineBuilderProductionPhase6()
    {
        if (_machineBuilderPhase6Surface is not null)
            _machineBuilderPhase6Surface.Visibility = Visibility.Collapsed;
        StyleMachineBuilderPhase3Step(_machineBuilderReviewStep, active: false, available: true);
    }

    private void RefreshMachineBuilderProductionReview()
    {
        _machineBuilderProductionPlan = null;
        if (_machineBuilderDocument is null || string.IsNullOrWhiteSpace(_repoRoot))
        {
            SetMachineBuilderPhase6State("NO PROJECT", "Select a repository and Machine Project first.", "#E18B8B", canApply: false);
            return;
        }

        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        try
        {
            _machineBuilderProductionPlan = ForeverStudioMachineProductionService.BuildPlan(
                _repoRoot,
                _machineBuilderDocument,
                _machineBuilderLibraryLoad.Catalog);
        }
        catch (Exception ex)
        {
            SetMachineBuilderPhase6State("REVIEW FAILED", ex.Message, "#E18B8B", canApply: false);
            if (_machineBuilderPhase6IssueList is not null)
                _machineBuilderPhase6IssueList.ItemsSource = new[] { $"ERROR · {ex.Message}" };
            return;
        }

        if (_machineBuilderPhase6IssueList is not null)
            _machineBuilderPhase6IssueList.ItemsSource = _machineBuilderProductionPlan.Issues;
        if (_machineBuilderPhase6ChangeList is not null)
        {
            _machineBuilderPhase6ChangeList.ItemsSource = _machineBuilderProductionPlan.Changes
                .Select(change => new MachineBuilderProductionChangeItem { Change = change })
                .ToList();
            if (_machineBuilderPhase6ChangeList.Items.Count > 0)
                _machineBuilderPhase6ChangeList.SelectedIndex = 0;
        }
        RefreshMachineBuilderPhase6Preview();

        var saved = !_machineBuilderDirty &&
                    !_machineBuilderSourceHash.Equals(ForeverStudioMachineProjectService.MissingHash, StringComparison.Ordinal);
        var canApply = _machineBuilderProductionPlan.CanApply && saved;
        var readiness = _machineBuilderProductionPlan.ErrorCount > 0
            ? "BLOCKED"
            : !saved
                ? "SAVE REQUIRED"
                : _machineBuilderProductionPlan.Changes.Count == 0
                    ? "UP TO DATE"
                    : "READY TO BUILD";
        var color = _machineBuilderProductionPlan.ErrorCount > 0
            ? "#E18B8B"
            : canApply
                ? "#74C99A"
                : "#D8B26A";
        var summary =
            $"{_machineBuilderDocument.Components.Count - 1} visual component(s) · " +
            $"{Math.Max(0, _machineBuilderDocument.Behavior.Nodes.Count - 1)} function node(s) · " +
            $"{_machineBuilderDocument.Recipes.Count} recipe(s) · {_machineBuilderDocument.Gui.Bindings.Count} GUI binding(s) · " +
            $"{_machineBuilderDocument.Animations.Count} animation binding(s)\n" +
            $"{_machineBuilderProductionPlan.ErrorCount} error(s) · {_machineBuilderProductionPlan.WarningCount} warning(s) · " +
            $"{_machineBuilderProductionPlan.Changes.Count} planned file change(s)" +
            (saved ? string.Empty : " · save the Machine Project before Apply");
        SetMachineBuilderPhase6State(readiness, summary, color, canApply);
    }

    private void RefreshMachineBuilderPhase6Preview()
    {
        var item = _machineBuilderPhase6ChangeList?.SelectedItem as MachineBuilderProductionChangeItem;
        if (_machineBuilderPhase6PreviewTitle is not null)
            _machineBuilderPhase6PreviewTitle.Text = item is null
                ? "No generated file selected"
                : $"{item.Change.Kind} · {item.Change.RelativePath}\n{item.Change.Description}";
        if (_machineBuilderPhase6Preview is not null)
            _machineBuilderPhase6Preview.Text = item is null
                ? string.Empty
                : string.Join(Environment.NewLine, item.Change.PreviewLines);
    }

    private void SetMachineBuilderPhase6State(string readiness, string summary, string color, bool canApply)
    {
        if (_machineBuilderPhase6Readiness is not null)
        {
            _machineBuilderPhase6Readiness.Text = readiness;
            _machineBuilderPhase6Readiness.Foreground = Brush(color);
        }
        if (_machineBuilderPhase6Summary is not null)
            _machineBuilderPhase6Summary.Text = summary;
        if (_machineBuilderPhase6ApplyButton is not null)
            _machineBuilderPhase6ApplyButton.IsEnabled = canApply;
    }

    private void ApplyMachineBuilderProductionPlan()
    {
        if (_machineBuilderProductionPlan is not { CanApply: true } plan || _machineBuilderDirty ||
            string.IsNullOrWhiteSpace(_repoRoot))
        {
            RefreshMachineBuilderProductionReview();
            return;
        }

        var files = string.Join(Environment.NewLine, plan.Changes.Take(10).Select(change => $"• {change.RelativePath}"));
        if (plan.Changes.Count > 10)
            files += $"{Environment.NewLine}• … and {plan.Changes.Count - 10} more";
        var answer = MessageBox.Show(
            this,
            $"Build '{plan.DisplayName}' with the {plan.ProductionKind} backend?\n\n{files}\n\n" +
            "Every target is rechecked against the Review snapshot. A failed write rolls the transaction back.",
            "Apply Machine Builder production plan",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            var result = ForeverStudioMachineProductionService.Apply(plan);
            RefreshMachineBuilderAfterProductionApply();
            RefreshMachineBuilderProductionReview();
            MessageBox.Show(
                this,
                $"Machine '{result.AssetId}' was built successfully.\n\n{result.WrittenPaths.Count} file(s) were written and the Studio project scan was refreshed.",
                "Machine build complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"The production transaction was not applied completely. Any committed files were rolled back.\n\n{ex.Message}",
                "Machine build failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            RefreshMachineBuilderProductionReview();
        }
    }

    private void RefreshMachineBuilderAfterProductionApply()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;
        try
        {
            var scan = ForeverProjectScanner.Scan(_repoRoot);
            _foreverTemplates.Clear();
            _foreverTemplates.AddRange(scan.Templates);
            _studioProjectIndex = ForeverStudioProjectIndexer.Build(_repoRoot, scan);
            RefreshStudioProjectHome();
            RefreshMachineBuilderRepositoryContext();
        }
        catch
        {
            // Production succeeded even if an unrelated project-scanner file changed concurrently.
            // The user can use Rescan Project later; do not turn a successful transaction into failure.
        }
    }
}

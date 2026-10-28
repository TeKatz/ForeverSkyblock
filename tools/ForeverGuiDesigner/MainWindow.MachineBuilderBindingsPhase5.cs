using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder Phase 5: recipes, GUI/data bindings and animation bindings. The editor consumes
/// typed outputs from the Phase-4 behavior graph and writes only canonical Machine Project fields.
/// Production source/resource generation remains a Phase-6 concern.
/// </summary>
public partial class MainWindow
{
    private sealed record MachineBuilderPhase5Source(
        string NodeId,
        string PortId,
        string DisplayName,
        string DataType)
    {
        public override string ToString() => $"{DisplayName}  ·  {DataType}  ·  {NodeId}.{PortId}";
    }

    private sealed record MachineBuilderPhase5WidgetPreset(
        string Key,
        string Name,
        string AssetId,
        string[] AcceptedTypes,
        string Description)
    {
        public override string ToString() => Name;
    }

    private sealed class MachineBuilderPhase5RecipeItem
    {
        public required ForeverStudioMachineRecipeBinding Recipe { get; init; }
        public override string ToString() => $"{Recipe.Id}  ·  {Recipe.RecipeTypeId}";
    }

    private sealed class MachineBuilderPhase5GuiItem
    {
        public required ForeverStudioMachineGuiBinding Binding { get; init; }
        public required string Widget { get; init; }
        public required string Source { get; init; }
        public override string ToString() => $"{Binding.Id}  ·  {Widget}  ←  {Source}";
    }

    private sealed class MachineBuilderPhase5AnimationItem
    {
        public required ForeverStudioMachineAnimationBinding Binding { get; init; }
        public required string Preset { get; init; }
        public required string Source { get; init; }
        public override string ToString() => $"{Binding.Id}  ·  {Preset}  →  {Binding.TargetComponentId}  ←  {Source}";
    }

    private enum MachineBuilderPhase5Mode
    {
        Gui,
        Animation
    }

    private static readonly MachineBuilderPhase5WidgetPreset[] MachineBuilderPhase5WidgetPresets =
    [
        new("progress-bar", "Progress Bar", "asset.screen", ["number"], "Normalized process/timer progress."),
        new("energy-bar", "Energy Bar", "asset.screen", ["energy", "number"], "Energy amount or fill ratio."),
        new("item-slot", "Item Slot", "asset.input-tray", ["item"], "Input/storage item presentation."),
        new("output-slot", "Output Slot", "asset.output-tray", ["item"], "Output item presentation."),
        new("fluid-gauge", "Fluid Gauge", "asset.fluid-window", ["fluid", "number"], "Fluid contents or fill ratio."),
        new("status", "Status Indicator", "asset.status-lamp", ["boolean", "state"], "Machine state or boolean status."),
        new("value", "Value Label", "asset.screen", ["any"], "Generic textual/numeric presentation.")
    ];

    private bool _machineBuilderPhase5Initialized;
    private Border? _machineBuilderPhase5Surface;
    private Border? _machineBuilderGuiStep;
    private Border? _machineBuilderAnimationStep;
    private Grid? _machineBuilderPhase5ModeHost;
    private Button? _machineBuilderPhase5GuiTab;
    private Button? _machineBuilderPhase5AnimationTab;
    private TextBlock? _machineBuilderPhase5Status;
    private ListBox? _machineBuilderPhase5SourcesList;
    private ListBox? _machineBuilderPhase5RecipeList;
    private ListBox? _machineBuilderPhase5GuiBindingList;
    private ListBox? _machineBuilderPhase5AnimationList;
    private TextBox? _machineBuilderPhase5RecipeIdBox;
    private TextBox? _machineBuilderPhase5RecipeTypeBox;
    private TextBox? _machineBuilderPhase5RecipePropertiesBox;
    private TextBox? _machineBuilderPhase5ScreenIdBox;
    private TextBox? _machineBuilderPhase5GuiLabelBox;
    private ComboBox? _machineBuilderPhase5WidgetCombo;
    private ComboBox? _machineBuilderPhase5AnimationPresetCombo;
    private ComboBox? _machineBuilderPhase5AnimationComponentCombo;
    private ComboBox? _machineBuilderPhase5AnimationSourceCombo;
    private ComboBox? _machineBuilderPhase5AnimationAxisCombo;
    private TextBox? _machineBuilderPhase5AnimationScaleBox;
    private MachineBuilderPhase5Mode _machineBuilderPhase5Mode = MachineBuilderPhase5Mode.Gui;
    private List<MachineBuilderPhase5Source> _machineBuilderPhase5Sources = [];

    public void EnableForeverMachineBuilderBindingsPhase5()
    {
        if (_machineBuilderPhase5Initialized || _studioMachineBuilderWorkspace is not DockPanel root)
            return;
        _machineBuilderPhase5Initialized = true;

        var page = root.Children.OfType<Grid>().FirstOrDefault(grid => grid.ColumnDefinitions.Count >= 5);
        if (page is null)
            return;

        _machineBuilderPhase5Surface = BuildMachineBuilderPhase5Surface();
        Grid.SetColumn(_machineBuilderPhase5Surface, 2);
        Grid.SetColumnSpan(_machineBuilderPhase5Surface, 3);
        Panel.SetZIndex(_machineBuilderPhase5Surface, 50);
        _machineBuilderPhase5Surface.Visibility = Visibility.Collapsed;
        page.Children.Add(_machineBuilderPhase5Surface);

        InstallMachineBuilderPhase5Workflow(page);
        InstallMachineBuilderPhase5ContinueButton();
        UpdateMachineBuilderPhase5Labels();
        RefreshMachineBuilderPhase5();
    }

    private void InstallMachineBuilderPhase5Workflow(Grid page)
    {
        var workflowCard = page.Children.OfType<Border>()
            .FirstOrDefault(border => Grid.GetColumn(border) == 0 && border.Child is StackPanel panel &&
                                      panel.Children.OfType<TextBlock>().Any(text => text.Text == "GUIDED WORKFLOW"));
        if (workflowCard?.Child is not StackPanel workflow)
            return;

        _machineBuilderGuiStep = FindMachineBuilderWorkflowStep(workflow, "Recipes & GUI");
        _machineBuilderAnimationStep = FindMachineBuilderWorkflowStep(workflow, "Animation");

        PrepareMachineBuilderPhase5Step(_machineBuilderGuiStep, "Recipes, screens and typed data bindings", MachineBuilderPhase5Mode.Gui);
        PrepareMachineBuilderPhase5Step(_machineBuilderAnimationStep, "Preset motion driven by graph data", MachineBuilderPhase5Mode.Animation);

        if (_machineBuilderIdentityStep is not null)
            _machineBuilderIdentityStep.MouseLeftButtonUp += (_, _) => HideMachineBuilderPhase5Surface();
        if (_machineBuilderVisualStep is not null)
            _machineBuilderVisualStep.MouseLeftButtonUp += (_, _) => HideMachineBuilderPhase5Surface();
        if (_machineBuilderFunctionStep is not null)
            _machineBuilderFunctionStep.MouseLeftButtonUp += (_, _) => HideMachineBuilderPhase5Surface();
    }

    private void PrepareMachineBuilderPhase5Step(Border? step, string description, MachineBuilderPhase5Mode mode)
    {
        if (step is null)
            return;
        step.Cursor = Cursors.Hand;
        step.Opacity = 1;
        if (step.Child is Grid grid)
        {
            var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
            var text = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
            if (text is not null)
                text.Text = description;
        }
        step.MouseLeftButtonUp += (_, _) => ShowMachineBuilderPhase5(mode);
        StyleMachineBuilderPhase3Step(step, active: false, available: true);
    }

    private void InstallMachineBuilderPhase5ContinueButton()
    {
        if (_machineBuilderFunctionSurface is null)
            return;
        var actionPanel = FindStudioVisualChildren<StackPanel>(_machineBuilderFunctionSurface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                                     panel.Children.OfType<Button>().Any(button =>
                                         FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "SAVE PROJECT")));
        if (actionPanel is null || actionPanel.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "machine_builder_phase5_continue", StringComparison.Ordinal)))
            return;

        var button = CreateStudioChromeButton("BINDINGS  →", 112, false);
        button.Tag = "machine_builder_phase5_continue";
        button.Height = 36;
        button.Margin = new Thickness(9, 0, 0, 0);
        button.Click += (_, _) => ShowMachineBuilderPhase5(MachineBuilderPhase5Mode.Gui);
        var save = actionPanel.Children.OfType<Button>().FirstOrDefault(candidate =>
            FindStudioVisualChildren<TextBlock>(candidate).Any(text => text.Text == "SAVE PROJECT"));
        var index = save is null ? actionPanel.Children.Count : actionPanel.Children.IndexOf(save);
        actionPanel.Children.Insert(Math.Max(0, index), button);
    }

    private void UpdateMachineBuilderPhase5Labels()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;
        foreach (var text in FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace))
        {
            if (text.Text.StartsWith("PHASE 4 ·", StringComparison.OrdinalIgnoreCase))
                text.Text = "PHASE 5 · BINDINGS & ANIMATION";
            else if (text.Text.StartsWith("Phase 4 unlocks typed machine behavior", StringComparison.OrdinalIgnoreCase))
                text.Text = "Phase 5 binds graph data into recipes, GUI presentation and component animation. Review & Build remains locked until Phase 6.";
        }
    }

    private Border BuildMachineBuilderPhase5Surface()
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
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        shell.RowDefinitions.Add(new RowDefinition());
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = "BINDINGS · PHASE 5",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Connect machine data to presentation",
            Foreground = Brush("#EFF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        copy.Children.Add(new TextBlock
        {
            Text = "The Function Graph remains the source of truth. GUI widgets and animations consume its typed outputs and persist reversible bindings in the Machine Project.",
            Foreground = Brush("#899CA4"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        header.Children.Add(copy);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var function = CreateStudioChromeButton("← FUNCTION", 100, false);
        function.Height = 36;
        function.Click += (_, _) =>
        {
            HideMachineBuilderPhase5Surface();
            ShowMachineBuilderFunctionPhase4();
        };
        actions.Children.Add(function);
        var save = CreateStudioChromeButton("SAVE PROJECT", 126, true);
        save.Height = 36;
        save.Margin = new Thickness(9, 0, 0, 0);
        save.Click += (_, _) => SaveMachineBuilderProject();
        actions.Children.Add(save);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        shell.Children.Add(header);

        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
        _machineBuilderPhase5GuiTab = CreateStudioChromeButton("RECIPES & GUI", 132, false);
        _machineBuilderPhase5GuiTab.Height = 32;
        _machineBuilderPhase5GuiTab.Click += (_, _) => SetMachineBuilderPhase5Mode(MachineBuilderPhase5Mode.Gui);
        tabs.Children.Add(_machineBuilderPhase5GuiTab);
        _machineBuilderPhase5AnimationTab = CreateStudioChromeButton("ANIMATION", 108, false);
        _machineBuilderPhase5AnimationTab.Height = 32;
        _machineBuilderPhase5AnimationTab.Margin = new Thickness(8, 0, 0, 0);
        _machineBuilderPhase5AnimationTab.Click += (_, _) => SetMachineBuilderPhase5Mode(MachineBuilderPhase5Mode.Animation);
        tabs.Children.Add(_machineBuilderPhase5AnimationTab);
        Grid.SetRow(tabs, 1);
        shell.Children.Add(tabs);

        _machineBuilderPhase5ModeHost = new Grid();
        Grid.SetRow(_machineBuilderPhase5ModeHost, 3);
        shell.Children.Add(_machineBuilderPhase5ModeHost);

        _machineBuilderPhase5Status = new TextBlock
        {
            Foreground = Brush("#82969E"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, 9, 2, 0)
        };
        Grid.SetRow(_machineBuilderPhase5Status, 4);
        shell.Children.Add(_machineBuilderPhase5Status);
        surface.Child = shell;
        return surface;
    }

    private FrameworkElement BuildMachineBuilderPhase5GuiBody()
    {
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });

        var recipes = BuildMachineBuilderPhase5RecipeCard();
        Grid.SetColumn(recipes, 0);
        body.Children.Add(recipes);
        var gui = BuildMachineBuilderPhase5GuiCard();
        Grid.SetColumn(gui, 2);
        body.Children.Add(gui);
        var sources = BuildMachineBuilderPhase5SourceCard();
        Grid.SetColumn(sources, 4);
        body.Children.Add(sources);
        return body;
    }

    private Border BuildMachineBuilderPhase5RecipeCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(15) };
        panel.Children.Add(Phase5SectionTitle("RECIPES", "Declare recipe contracts consumed by behavior nodes."));
        _machineBuilderPhase5RecipeList = Phase5ListBox(132);
        _machineBuilderPhase5RecipeList.SelectionChanged += (_, _) => LoadSelectedMachineBuilderPhase5Recipe();
        panel.Children.Add(_machineBuilderPhase5RecipeList);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Recipe ID", 12));
        _machineBuilderPhase5RecipeIdBox = MachineBuilderTextBox();
        _machineBuilderPhase5RecipeIdBox.Height = 32;
        panel.Children.Add(_machineBuilderPhase5RecipeIdBox);
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Recipe type", 10));
        _machineBuilderPhase5RecipeTypeBox = MachineBuilderTextBox();
        _machineBuilderPhase5RecipeTypeBox.Height = 32;
        _machineBuilderPhase5RecipeTypeBox.ToolTip = "Example: forever_skyblock:machine_processing";
        panel.Children.Add(_machineBuilderPhase5RecipeTypeBox);
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Properties JSON", 10));
        _machineBuilderPhase5RecipePropertiesBox = MachineBuilderTextBox(multiline: true);
        _machineBuilderPhase5RecipePropertiesBox.Height = 92;
        _machineBuilderPhase5RecipePropertiesBox.Text = "{}";
        panel.Children.Add(_machineBuilderPhase5RecipePropertiesBox);

        var buttons = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        var apply = CreateStudioChromeButton("ADD / APPLY", double.NaN, false);
        apply.Height = 32;
        apply.Click += (_, _) => CommitMachineBuilderPhase5Recipe();
        buttons.Children.Add(apply);
        var remove = CreateStudioChromeButton("REMOVE", double.NaN, false);
        remove.Height = 32;
        remove.Click += (_, _) => RemoveSelectedMachineBuilderPhase5Recipe();
        Grid.SetColumn(remove, 2);
        buttons.Children.Add(remove);
        panel.Children.Add(buttons);
        card.Child = panel;
        return card;
    }

    private Border BuildMachineBuilderPhase5GuiCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(15) };
        panel.Children.Add(Phase5SectionTitle("GUI BINDINGS", "Bind typed graph outputs to semantic widget roles."));
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Screen ID", 8));
        _machineBuilderPhase5ScreenIdBox = MachineBuilderTextBox();
        _machineBuilderPhase5ScreenIdBox.Height = 32;
        _machineBuilderPhase5ScreenIdBox.LostKeyboardFocus += (_, _) => CommitMachineBuilderPhase5ScreenId();
        panel.Children.Add(_machineBuilderPhase5ScreenIdBox);

        _machineBuilderPhase5GuiBindingList = Phase5ListBox(178);
        _machineBuilderPhase5GuiBindingList.Margin = new Thickness(0, 12, 0, 0);
        panel.Children.Add(_machineBuilderPhase5GuiBindingList);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Widget role", 12));
        _machineBuilderPhase5WidgetCombo = new ComboBox
        {
            Height = 32,
            Background = Brush("#10191E"),
            Foreground = Brush("#E5EEF0"),
            BorderBrush = Brush("#354A53")
        };
        panel.Children.Add(_machineBuilderPhase5WidgetCombo);
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Label", 10));
        _machineBuilderPhase5GuiLabelBox = MachineBuilderTextBox();
        _machineBuilderPhase5GuiLabelBox.Height = 32;
        panel.Children.Add(_machineBuilderPhase5GuiLabelBox);

        var buttons = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        var add = CreateStudioChromeButton("ADD BINDING", double.NaN, true);
        add.Height = 34;
        add.Click += (_, _) => AddMachineBuilderPhase5GuiBinding();
        buttons.Children.Add(add);
        var remove = CreateStudioChromeButton("REMOVE", double.NaN, false);
        remove.Height = 34;
        remove.Click += (_, _) => RemoveSelectedMachineBuilderPhase5GuiBinding();
        Grid.SetColumn(remove, 2);
        buttons.Children.Add(remove);
        panel.Children.Add(buttons);
        card.Child = panel;
        return card;
    }

    private Border BuildMachineBuilderPhase5SourceCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(15) };
        panel.Children.Add(Phase5SectionTitle("BINDING SOURCES", "Outputs exported by the typed Function Graph."));
        _machineBuilderPhase5SourcesList = Phase5ListBox(360);
        _machineBuilderPhase5SourcesList.SelectionChanged += (_, _) => RefreshMachineBuilderPhase5WidgetChoices();
        panel.Children.Add(_machineBuilderPhase5SourcesList);
        panel.Children.Add(new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 12, 0, 0),
            Child = new TextBlock
            {
                Text = "Examples: Energy Storage.ratio → Energy Bar · Item Input.item → Item Slot · Process Progress.progress → Progress Bar.",
                Foreground = Brush("#7F939B"),
                FontSize = 9.3,
                LineHeight = 15,
                TextWrapping = TextWrapping.Wrap
            }
        });
        card.Child = panel;
        return card;
    }

    private FrameworkElement BuildMachineBuilderPhase5AnimationBody()
    {
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());

        var author = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(Phase5SectionTitle("ANIMATION BINDING", "Drive a component preset from optional graph data."));
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Preset", 10));
        _machineBuilderPhase5AnimationPresetCombo = Phase5ComboBox();
        panel.Children.Add(_machineBuilderPhase5AnimationPresetCombo);
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Target component", 10));
        _machineBuilderPhase5AnimationComponentCombo = Phase5ComboBox();
        panel.Children.Add(_machineBuilderPhase5AnimationComponentCombo);
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Dynamic source (optional)", 10));
        _machineBuilderPhase5AnimationSourceCombo = Phase5ComboBox();
        panel.Children.Add(_machineBuilderPhase5AnimationSourceCombo);

        var axisScale = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        axisScale.ColumnDefinitions.Add(new ColumnDefinition());
        axisScale.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        axisScale.ColumnDefinitions.Add(new ColumnDefinition());
        _machineBuilderPhase5AnimationAxisCombo = Phase5ComboBox();
        _machineBuilderPhase5AnimationAxisCombo.ItemsSource = new[] { "x", "y", "z" };
        _machineBuilderPhase5AnimationAxisCombo.SelectedIndex = 1;
        axisScale.Children.Add(_machineBuilderPhase5AnimationAxisCombo);
        _machineBuilderPhase5AnimationScaleBox = MachineBuilderTextBox();
        _machineBuilderPhase5AnimationScaleBox.Height = 32;
        _machineBuilderPhase5AnimationScaleBox.Text = "1";
        _machineBuilderPhase5AnimationScaleBox.ToolTip = "Source multiplier / preset intensity";
        Grid.SetColumn(_machineBuilderPhase5AnimationScaleBox, 2);
        axisScale.Children.Add(_machineBuilderPhase5AnimationScaleBox);
        panel.Children.Add(axisScale);

        var add = CreateStudioChromeButton("ADD ANIMATION BINDING", double.NaN, true);
        add.Height = 36;
        add.HorizontalAlignment = HorizontalAlignment.Stretch;
        add.Margin = new Thickness(0, 12, 0, 0);
        add.Click += (_, _) => AddMachineBuilderPhase5AnimationBinding();
        panel.Children.Add(add);
        author.Child = panel;
        body.Children.Add(author);

        var existing = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var existingPanel = new StackPanel { Margin = new Thickness(16) };
        existingPanel.Children.Add(Phase5SectionTitle("ANIMATION STACK", "Persistent preset/component/data-source relationships."));
        _machineBuilderPhase5AnimationList = Phase5ListBox(390);
        existingPanel.Children.Add(_machineBuilderPhase5AnimationList);
        var remove = CreateStudioChromeButton("REMOVE SELECTED", 150, false);
        remove.Height = 34;
        remove.HorizontalAlignment = HorizontalAlignment.Left;
        remove.Margin = new Thickness(0, 12, 0, 0);
        remove.Click += (_, _) => RemoveSelectedMachineBuilderPhase5AnimationBinding();
        existingPanel.Children.Add(remove);
        existingPanel.Children.Add(new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 16, 0, 0),
            Child = new TextBlock
            {
                Text = "Typical binding: Process Progress.progress → Rotate → rotor. Without a source, the preset remains a static/runtime-triggered animation contract.",
                Foreground = Brush("#7F939B"),
                FontSize = 9.3,
                LineHeight = 15,
                TextWrapping = TextWrapping.Wrap
            }
        });
        existing.Child = existingPanel;
        Grid.SetColumn(existing, 2);
        body.Children.Add(existing);
        return body;
    }

    private void ShowMachineBuilderPhase5(MachineBuilderPhase5Mode mode)
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        if (_machineBuilderIdentityScroll is not null) _machineBuilderIdentityScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderContextScroll is not null) _machineBuilderContextScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderVisualSurface is not null) _machineBuilderVisualSurface.Visibility = Visibility.Collapsed;
        if (_machineBuilderFunctionSurface is not null) _machineBuilderFunctionSurface.Visibility = Visibility.Collapsed;
        if (_machineBuilderPhase5Surface is not null) _machineBuilderPhase5Surface.Visibility = Visibility.Visible;

        StyleMachineBuilderPhase3Step(_machineBuilderIdentityStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderVisualStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderFunctionStep, active: false, available: true);
        SetMachineBuilderPhase5Mode(mode);
        RefreshMachineBuilderPhase5();
    }

    private void HideMachineBuilderPhase5Surface()
    {
        if (_machineBuilderPhase5Surface is not null)
            _machineBuilderPhase5Surface.Visibility = Visibility.Collapsed;
        StyleMachineBuilderPhase3Step(_machineBuilderGuiStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderAnimationStep, active: false, available: true);
    }

    private void SetMachineBuilderPhase5Mode(MachineBuilderPhase5Mode mode)
    {
        _machineBuilderPhase5Mode = mode;
        if (_machineBuilderPhase5ModeHost is null)
            return;
        _machineBuilderPhase5ModeHost.Children.Clear();
        _machineBuilderPhase5ModeHost.Children.Add(mode == MachineBuilderPhase5Mode.Gui
            ? BuildMachineBuilderPhase5GuiBody()
            : BuildMachineBuilderPhase5AnimationBody());
        StyleMachineBuilderPhase3Step(_machineBuilderGuiStep, active: mode == MachineBuilderPhase5Mode.Gui, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderAnimationStep, active: mode == MachineBuilderPhase5Mode.Animation, available: true);
        StyleMachineBuilderPhase5Tab(_machineBuilderPhase5GuiTab, mode == MachineBuilderPhase5Mode.Gui);
        StyleMachineBuilderPhase5Tab(_machineBuilderPhase5AnimationTab, mode == MachineBuilderPhase5Mode.Animation);
        RefreshMachineBuilderPhase5();
    }

    private static void StyleMachineBuilderPhase5Tab(Button? button, bool active)
    {
        if (button?.Content is not Border surface)
            return;
        surface.Background = Brush(active ? "#1A4144" : "#202D34");
        surface.BorderBrush = Brush(active ? "#65C8C8" : "#41555E");
        if (surface.Child is TextBlock text)
            text.Foreground = Brush(active ? "#F0FFFF" : "#E3ECEF");
    }

    private void RefreshMachineBuilderPhase5()
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        _machineBuilderPhase5Sources = BuildMachineBuilderPhase5Sources();

        if (_machineBuilderPhase5SourcesList is not null)
        {
            var selected = _machineBuilderPhase5SourcesList.SelectedItem as MachineBuilderPhase5Source;
            _machineBuilderPhase5SourcesList.ItemsSource = null;
            _machineBuilderPhase5SourcesList.ItemsSource = _machineBuilderPhase5Sources;
            _machineBuilderPhase5SourcesList.SelectedItem = selected is null
                ? _machineBuilderPhase5Sources.FirstOrDefault()
                : _machineBuilderPhase5Sources.FirstOrDefault(source => source.NodeId == selected.NodeId && source.PortId == selected.PortId)
                  ?? _machineBuilderPhase5Sources.FirstOrDefault();
        }
        if (_machineBuilderPhase5ScreenIdBox is not null)
        {
            _machineBuilderPhase5ScreenIdBox.Text = string.IsNullOrWhiteSpace(_machineBuilderDocument.Gui.ScreenId)
                ? $"{_machineBuilderDocument.AssetId}_screen"
                : _machineBuilderDocument.Gui.ScreenId;
        }
        RefreshMachineBuilderPhase5Recipes();
        RefreshMachineBuilderPhase5GuiBindings();
        RefreshMachineBuilderPhase5AnimationControls();
        RefreshMachineBuilderPhase5Animations();
        RefreshMachineBuilderPhase5WidgetChoices();
        RefreshMachineBuilderPhase5Status();
    }

    private List<MachineBuilderPhase5Source> BuildMachineBuilderPhase5Sources()
    {
        var result = new List<MachineBuilderPhase5Source>();
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return result;
        foreach (var node in _machineBuilderDocument.Behavior.Nodes)
        {
            var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is null)
                continue;
            foreach (var port in item.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Output &&
                                                          !port.DataType.Equals("flow", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(new MachineBuilderPhase5Source(node.Id, port.Id, $"{item.DisplayName} · {port.DisplayName}", port.DataType));
            }
        }
        return result.OrderBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void RefreshMachineBuilderPhase5WidgetChoices()
    {
        if (_machineBuilderPhase5WidgetCombo is null)
            return;
        var source = _machineBuilderPhase5SourcesList?.SelectedItem as MachineBuilderPhase5Source;
        var choices = MachineBuilderPhase5WidgetPresets
            .Where(preset => source is null || preset.AcceptedTypes.Contains("any", StringComparer.OrdinalIgnoreCase) ||
                             preset.AcceptedTypes.Contains(source.DataType, StringComparer.OrdinalIgnoreCase))
            .ToList();
        _machineBuilderPhase5WidgetCombo.ItemsSource = choices;
        if (choices.Count > 0)
            _machineBuilderPhase5WidgetCombo.SelectedIndex = 0;
    }

    private void RefreshMachineBuilderPhase5Recipes()
    {
        if (_machineBuilderPhase5RecipeList is null || _machineBuilderDocument is null)
            return;
        _machineBuilderPhase5RecipeList.ItemsSource = _machineBuilderDocument.Recipes
            .Select(recipe => new MachineBuilderPhase5RecipeItem { Recipe = recipe }).ToList();
    }

    private void LoadSelectedMachineBuilderPhase5Recipe()
    {
        var recipe = (_machineBuilderPhase5RecipeList?.SelectedItem as MachineBuilderPhase5RecipeItem)?.Recipe;
        if (recipe is null)
            return;
        if (_machineBuilderPhase5RecipeIdBox is not null) _machineBuilderPhase5RecipeIdBox.Text = recipe.Id;
        if (_machineBuilderPhase5RecipeTypeBox is not null) _machineBuilderPhase5RecipeTypeBox.Text = recipe.RecipeTypeId;
        if (_machineBuilderPhase5RecipePropertiesBox is not null)
        {
            var map = recipe.Properties.ToDictionary(pair => pair.Key, pair => pair.Value);
            _machineBuilderPhase5RecipePropertiesBox.Text = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private void CommitMachineBuilderPhase5Recipe()
    {
        if (_machineBuilderDocument is null)
            return;
        var id = SanitizeMachineBuilderGraphId(_machineBuilderPhase5RecipeIdBox?.Text ?? string.Empty);
        var type = (_machineBuilderPhase5RecipeTypeBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
        {
            SetMachineBuilderPhase5Status("Recipe ID and recipe type are required.", "#E18B8B");
            return;
        }

        Dictionary<string, JsonElement> properties;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(_machineBuilderPhase5RecipePropertiesBox?.Text)
                ? "{}"
                : _machineBuilderPhase5RecipePropertiesBox!.Text);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException("Properties must be a JSON object.");
            properties = document.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException ex)
        {
            SetMachineBuilderPhase5Status($"Recipe properties JSON is invalid: {ex.Message}", "#E18B8B");
            return;
        }

        var recipe = (_machineBuilderPhase5RecipeList?.SelectedItem as MachineBuilderPhase5RecipeItem)?.Recipe;
        if (recipe is null || (!recipe.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                               _machineBuilderDocument.Recipes.Any(existing => existing.Id.Equals(id, StringComparison.OrdinalIgnoreCase))))
        {
            if (_machineBuilderDocument.Recipes.Any(existing => existing.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                SetMachineBuilderPhase5Status($"Recipe ID '{id}' already exists.", "#E18B8B");
                return;
            }
            recipe = new ForeverStudioMachineRecipeBinding();
            _machineBuilderDocument.Recipes.Add(recipe);
        }
        recipe.Id = id;
        recipe.RecipeTypeId = type;
        recipe.Properties = properties;
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderPhase5Recipes();
        SetMachineBuilderPhase5Status($"Recipe '{id}' updated.", "#74C99A");
    }

    private void RemoveSelectedMachineBuilderPhase5Recipe()
    {
        if (_machineBuilderDocument is null ||
            (_machineBuilderPhase5RecipeList?.SelectedItem as MachineBuilderPhase5RecipeItem)?.Recipe is not { } recipe)
            return;
        _machineBuilderDocument.Recipes.Remove(recipe);
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderPhase5Recipes();
        SetMachineBuilderPhase5Status($"Removed recipe '{recipe.Id}'.", "#D8B26A");
    }

    private void CommitMachineBuilderPhase5ScreenId()
    {
        if (_machineBuilderDocument is null || _machineBuilderPhase5ScreenIdBox is null)
            return;
        var screenId = SanitizeMachineBuilderGraphId(_machineBuilderPhase5ScreenIdBox.Text);
        if (string.IsNullOrWhiteSpace(screenId))
            screenId = $"{_machineBuilderDocument.AssetId}_screen";
        _machineBuilderDocument.Gui.ScreenId = screenId;
        _machineBuilderPhase5ScreenIdBox.Text = screenId;
        MarkMachineBuilderPhase5Dirty();
    }

    private void AddMachineBuilderPhase5GuiBinding()
    {
        if (_machineBuilderDocument is null ||
            _machineBuilderPhase5SourcesList?.SelectedItem is not MachineBuilderPhase5Source source ||
            _machineBuilderPhase5WidgetCombo?.SelectedItem is not MachineBuilderPhase5WidgetPreset widget)
        {
            SetMachineBuilderPhase5Status("Select a graph source and compatible widget role first.", "#D8B26A");
            return;
        }
        CommitMachineBuilderPhase5ScreenId();
        var used = _machineBuilderDocument.Gui.Bindings.Select(binding => binding.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId($"gui_{widget.Key}_{source.NodeId}_{source.PortId}");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
        var binding = new ForeverStudioMachineGuiBinding
        {
            Id = id,
            LibraryItemId = widget.AssetId,
            LibraryItemVersion = 1,
            SourceId = source.NodeId,
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["widget"] = JsonSerializer.SerializeToElement(widget.Key),
                ["sourcePortId"] = JsonSerializer.SerializeToElement(source.PortId),
                ["sourceType"] = JsonSerializer.SerializeToElement(source.DataType),
                ["label"] = JsonSerializer.SerializeToElement((_machineBuilderPhase5GuiLabelBox?.Text ?? widget.Name).Trim()),
                ["order"] = JsonSerializer.SerializeToElement(_machineBuilderDocument.Gui.Bindings.Count)
            }
        };
        _machineBuilderDocument.Gui.Bindings.Add(binding);
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderPhase5GuiBindings();
        RefreshMachineBuilderPhase5Status();
        SetMachineBuilderPhase5Status($"Bound {source.NodeId}.{source.PortId} to {widget.Name}.", "#74C99A");
    }

    private void RefreshMachineBuilderPhase5GuiBindings()
    {
        if (_machineBuilderPhase5GuiBindingList is null || _machineBuilderDocument is null)
            return;
        _machineBuilderPhase5GuiBindingList.ItemsSource = _machineBuilderDocument.Gui.Bindings.Select(binding =>
        {
            var widget = ReadMachineBuilderPhase5String(binding.Properties, "widget", binding.LibraryItemId);
            var port = ReadMachineBuilderPhase5String(binding.Properties, "sourcePortId", "?");
            return new MachineBuilderPhase5GuiItem
            {
                Binding = binding,
                Widget = widget,
                Source = $"{binding.SourceId}.{port}"
            };
        }).ToList();
    }

    private void RemoveSelectedMachineBuilderPhase5GuiBinding()
    {
        if (_machineBuilderDocument is null ||
            (_machineBuilderPhase5GuiBindingList?.SelectedItem as MachineBuilderPhase5GuiItem)?.Binding is not { } binding)
            return;
        _machineBuilderDocument.Gui.Bindings.Remove(binding);
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderPhase5GuiBindings();
        SetMachineBuilderPhase5Status($"Removed GUI binding '{binding.Id}'.", "#D8B26A");
    }

    private void RefreshMachineBuilderPhase5AnimationControls()
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        if (_machineBuilderPhase5AnimationPresetCombo is not null)
        {
            var presets = _machineBuilderLibraryLoad.Catalog.LatestItems
                .Where(item => item.Kind == ForeverStudioLibraryItemKind.Animation)
                .OrderBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _machineBuilderPhase5AnimationPresetCombo.ItemsSource = presets;
            _machineBuilderPhase5AnimationPresetCombo.DisplayMemberPath = nameof(ForeverStudioLibraryItem.DisplayName);
            if (presets.Count > 0 && _machineBuilderPhase5AnimationPresetCombo.SelectedIndex < 0)
                _machineBuilderPhase5AnimationPresetCombo.SelectedIndex = 0;
        }
        if (_machineBuilderPhase5AnimationComponentCombo is not null)
        {
            _machineBuilderPhase5AnimationComponentCombo.ItemsSource = _machineBuilderDocument.Components;
            _machineBuilderPhase5AnimationComponentCombo.DisplayMemberPath = nameof(ForeverStudioMachineComponent.DisplayName);
            if (_machineBuilderDocument.Components.Count > 0 && _machineBuilderPhase5AnimationComponentCombo.SelectedIndex < 0)
                _machineBuilderPhase5AnimationComponentCombo.SelectedIndex = Math.Min(1, _machineBuilderDocument.Components.Count - 1);
        }
        if (_machineBuilderPhase5AnimationSourceCombo is not null)
        {
            var sources = _machineBuilderPhase5Sources
                .Where(source => source.DataType.Equals("number", StringComparison.OrdinalIgnoreCase) ||
                                 source.DataType.Equals("boolean", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var items = new List<object> { "(runtime / no graph source)" };
            items.AddRange(sources);
            _machineBuilderPhase5AnimationSourceCombo.ItemsSource = items;
            if (_machineBuilderPhase5AnimationSourceCombo.SelectedIndex < 0)
                _machineBuilderPhase5AnimationSourceCombo.SelectedIndex = 0;
        }
    }

    private void AddMachineBuilderPhase5AnimationBinding()
    {
        if (_machineBuilderDocument is null ||
            _machineBuilderPhase5AnimationPresetCombo?.SelectedItem is not ForeverStudioLibraryItem preset ||
            _machineBuilderPhase5AnimationComponentCombo?.SelectedItem is not ForeverStudioMachineComponent component)
        {
            SetMachineBuilderPhase5Status("Select an animation preset and target component.", "#D8B26A");
            return;
        }
        if (!double.TryParse(_machineBuilderPhase5AnimationScaleBox?.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale) ||
            !double.IsFinite(scale))
        {
            SetMachineBuilderPhase5Status("Animation scale must be a finite number.", "#E18B8B");
            return;
        }
        scale = Math.Clamp(scale, -1000, 1000);
        var used = _machineBuilderDocument.Animations.Select(binding => binding.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId($"anim_{preset.Id.Replace("animation.", string.Empty)}_{component.Id}");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
        var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["axis"] = JsonSerializer.SerializeToElement(_machineBuilderPhase5AnimationAxisCombo?.SelectedItem?.ToString() ?? "y"),
            ["scale"] = JsonSerializer.SerializeToElement(scale)
        };
        if (_machineBuilderPhase5AnimationSourceCombo?.SelectedItem is MachineBuilderPhase5Source source)
        {
            properties["sourceNodeId"] = JsonSerializer.SerializeToElement(source.NodeId);
            properties["sourcePortId"] = JsonSerializer.SerializeToElement(source.PortId);
            properties["sourceType"] = JsonSerializer.SerializeToElement(source.DataType);
        }
        _machineBuilderDocument.Animations.Add(new ForeverStudioMachineAnimationBinding
        {
            Id = id,
            LibraryItemId = preset.Id,
            LibraryItemVersion = preset.Version,
            TargetComponentId = component.Id,
            Properties = properties
        });
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderPhase5Animations();
        RefreshMachineBuilderPhase5Status();
        SetMachineBuilderPhase5Status($"Added {preset.DisplayName} binding for {component.DisplayName}.", "#74C99A");
    }

    private void RefreshMachineBuilderPhase5Animations()
    {
        if (_machineBuilderPhase5AnimationList is null || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        _machineBuilderPhase5AnimationList.ItemsSource = _machineBuilderDocument.Animations.Select(binding =>
        {
            var preset = _machineBuilderLibraryLoad.Catalog.Find(binding.LibraryItemId, binding.LibraryItemVersion)?.DisplayName
                         ?? binding.LibraryItemId;
            var node = ReadMachineBuilderPhase5String(binding.Properties, "sourceNodeId", string.Empty);
            var port = ReadMachineBuilderPhase5String(binding.Properties, "sourcePortId", string.Empty);
            return new MachineBuilderPhase5AnimationItem
            {
                Binding = binding,
                Preset = preset,
                Source = string.IsNullOrWhiteSpace(node) ? "runtime" : $"{node}.{port}"
            };
        }).ToList();
    }

    private void RemoveSelectedMachineBuilderPhase5AnimationBinding()
    {
        if (_machineBuilderDocument is null ||
            (_machineBuilderPhase5AnimationList?.SelectedItem as MachineBuilderPhase5AnimationItem)?.Binding is not { } binding)
            return;
        _machineBuilderDocument.Animations.Remove(binding);
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderPhase5Animations();
        SetMachineBuilderPhase5Status($"Removed animation binding '{binding.Id}'.", "#D8B26A");
    }

    private void RefreshMachineBuilderPhase5Status()
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        var issues = 0;
        foreach (var binding in _machineBuilderDocument.Gui.Bindings)
        {
            var sourcePort = ReadMachineBuilderPhase5String(binding.Properties, "sourcePortId", string.Empty);
            var source = _machineBuilderPhase5Sources.FirstOrDefault(candidate =>
                candidate.NodeId.Equals(binding.SourceId, StringComparison.OrdinalIgnoreCase) &&
                candidate.PortId.Equals(sourcePort, StringComparison.OrdinalIgnoreCase));
            if (source is null || _machineBuilderLibraryLoad.Catalog.Find(binding.LibraryItemId, binding.LibraryItemVersion) is null)
                issues++;
        }
        foreach (var binding in _machineBuilderDocument.Animations)
        {
            if (_machineBuilderLibraryLoad.Catalog.Find(binding.LibraryItemId, binding.LibraryItemVersion) is null ||
                !_machineBuilderDocument.Components.Any(component => component.Id.Equals(binding.TargetComponentId, StringComparison.OrdinalIgnoreCase)))
                issues++;
            var sourceNode = ReadMachineBuilderPhase5String(binding.Properties, "sourceNodeId", string.Empty);
            if (!string.IsNullOrWhiteSpace(sourceNode))
            {
                var sourcePort = ReadMachineBuilderPhase5String(binding.Properties, "sourcePortId", string.Empty);
                if (!_machineBuilderPhase5Sources.Any(source => source.NodeId.Equals(sourceNode, StringComparison.OrdinalIgnoreCase) &&
                                                                source.PortId.Equals(sourcePort, StringComparison.OrdinalIgnoreCase)))
                    issues++;
            }
        }
        SetMachineBuilderPhase5Status(
            $"{_machineBuilderDocument.Recipes.Count} recipe(s) · {_machineBuilderDocument.Gui.Bindings.Count} GUI binding(s) · {_machineBuilderDocument.Animations.Count} animation binding(s) · {issues} binding issue(s)",
            issues == 0 ? "#74C99A" : "#D8B26A");
    }

    private static string ReadMachineBuilderPhase5String(
        Dictionary<string, JsonElement> properties,
        string key,
        string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static TextBlock Phase5SectionTitle(string title, string subtitle)
    {
        var text = new TextBlock
        {
            Text = $"{title}\n{subtitle}",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 0, 0, 10)
        };
        return text;
    }

    private static ListBox Phase5ListBox(double minHeight)
        => new()
        {
            MinHeight = minHeight,
            MaxHeight = 430,
            Background = Brush("#091014"),
            Foreground = Brush("#C0CDD1"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5),
            FontSize = 9.5
        };

    private static ComboBox Phase5ComboBox()
        => new()
        {
            Height = 32,
            Background = Brush("#10191E"),
            Foreground = Brush("#E5EEF0"),
            BorderBrush = Brush("#354A53")
        };

    private void MarkMachineBuilderPhase5Dirty()
    {
        _machineBuilderDirty = true;
        RefreshMachineBuilderValidation();
    }

    private void SetMachineBuilderPhase5Status(string message, string color)
    {
        if (_machineBuilderPhase5Status is null)
            return;
        _machineBuilderPhase5Status.Text = message;
        _machineBuilderPhase5Status.Foreground = Brush(color);
    }
}

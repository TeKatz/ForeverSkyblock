using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder GUI phase: presents the behavior authored in Functions as a real screen layout
/// and owns concrete recipe authoring. It does not create or duplicate behavior topology.
/// </summary>
public partial class MainWindow
{
    private sealed record GuiDesignerWidgetPreset(
        string Key,
        string Name,
        string AssetId,
        string[] AcceptedTypes,
        double DefaultWidth,
        double DefaultHeight,
        string Description)
    {
        public override string ToString() => Name;
    }

    private sealed record GuiDesignerProcessNode(string NodeId, string DisplayName, string LibraryItemId)
    {
        public override string ToString() => DisplayName;
    }

    private sealed class GuiDesignerRecipeItem
    {
        public required ForeverStudioMachineRecipeBinding Recipe { get; init; }
        public override string ToString() => Recipe.Id;
    }

    private static readonly GuiDesignerWidgetPreset[] GuiDesignerWidgetPresets =
    [
        new("item-slot", "Item Slot", "asset.input-tray", ["item"], 18, 18, "Shows an item stack from a Function output."),
        new("output-slot", "Output Slot", "asset.output-tray", ["item"], 18, 18, "Shows produced/stored output items."),
        new("progress-bar", "Progress Bar", "asset.screen", ["number"], 24, 16, "Shows normalized processing or timer progress."),
        new("energy-bar", "Energy Bar", "asset.screen", ["energy", "number"], 12, 52, "Shows energy amount or fill ratio."),
        new("fluid-gauge", "Fluid Gauge", "asset.fluid-window", ["fluid", "number"], 16, 52, "Shows tank contents or fill ratio."),
        new("status", "Status Indicator", "asset.status-lamp", ["boolean", "state"], 12, 12, "Shows a boolean/state as a lamp."),
        new("value", "Value Label", "asset.screen", ["any"], 54, 12, "Shows arbitrary textual or numeric data.")
    ];

    private const double GuiDesignerScale = 2.0;
    private const double GuiDesignerLogicalWidth = 176;
    private const double GuiDesignerLogicalHeight = 166;

    private bool _guiDesignerPhase4Initialized;
    private FrameworkElement? _guiDesignerBody;
    private Canvas? _guiDesignerCanvas;
    private StackPanel? _guiDesignerInspector;
    private ListBox? _guiDesignerBindingList;
    private ComboBox? _guiDesignerSourceCombo;
    private ComboBox? _guiDesignerWidgetCombo;
    private TextBox? _guiDesignerLabelBox;
    private TextBox? _guiDesignerXBox;
    private TextBox? _guiDesignerYBox;
    private TextBox? _guiDesignerWidthBox;
    private TextBox? _guiDesignerHeightBox;
    private TextBox? _guiDesignerPreviewValueBox;
    private TextBlock? _guiDesignerStatus;
    private string? _guiDesignerSelectedBindingId;
    private string? _guiDesignerDraggingBindingId;
    private Point _guiDesignerDragStart;
    private double _guiDesignerDragOriginX;
    private double _guiDesignerDragOriginY;

    private ListBox? _guiDesignerRecipeList;
    private TextBox? _guiDesignerRecipeIdBox;
    private ComboBox? _guiDesignerRecipeProcessCombo;
    private TextBox? _guiDesignerRecipeTypeText;
    private TextBox? _guiDesignerRecipeInputItemBox;
    private TextBox? _guiDesignerRecipeInputCountBox;
    private TextBox? _guiDesignerRecipeOutputItemBox;
    private TextBox? _guiDesignerRecipeOutputCountBox;
    private TextBox? _guiDesignerRecipeDurationBox;
    private TextBox? _guiDesignerRecipeEnergyBox;

    public void EnableForeverMachineBuilderGuiDesignerPhase4()
    {
        if (_guiDesignerPhase4Initialized)
            return;
        _guiDesignerPhase4Initialized = true;

        if (_machineBuilderGuiStep is not null)
        {
            if (_machineBuilderGuiStep.Child is Grid grid)
            {
                var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
                var description = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                if (description is not null)
                    description.Text = "Lay out the machine screen, bind Function outputs and define actual recipes";
            }
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderGuiDesignerPhase4();
        }

        if (_machineBuilderPhase5GuiTab is not null)
        {
            var label = FindStudioVisualChildren<TextBlock>(_machineBuilderPhase5GuiTab).FirstOrDefault();
            if (label is not null)
                label.Text = "GUI DESIGNER";
            _machineBuilderPhase5GuiTab.Click += (_, _) => ShowMachineBuilderGuiDesignerPhase4();
        }

        if (_machineBuilderFunctionSurface is not null)
        {
            var continueButton = FindStudioVisualChildren<Button>(_machineBuilderFunctionSurface)
                .FirstOrDefault(button => string.Equals(
                    button.Tag?.ToString(),
                    "machine_builder_phase5_continue",
                    StringComparison.Ordinal));
            if (continueButton is not null)
            {
                var label = FindStudioVisualChildren<TextBlock>(continueButton).FirstOrDefault();
                if (label is not null)
                    label.Text = "GUI  →";
                continueButton.Click += (_, _) => ShowMachineBuilderGuiDesignerPhase4();
            }

            InstallFunctionDeleteShortcutForGuiWorkflow();
        }
    }

    private void InstallFunctionDeleteShortcutForGuiWorkflow()
    {
        if (_machineBuilderFunctionSurface is null)
            return;

        var actions = FindStudioVisualChildren<StackPanel>(_machineBuilderFunctionSurface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                panel.Children.OfType<Button>().Any(button =>
                    FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "AUTO LAYOUT")));
        if (actions is not null && !actions.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "function_delete_selected", StringComparison.Ordinal)))
        {
            var delete = CreateStudioChromeButton("DELETE SELECTED", 126, false);
            delete.Tag = "function_delete_selected";
            delete.Height = 31;
            delete.Margin = new Thickness(7, 0, 0, 0);
            delete.Click += (_, _) => DeleteSelectedFunctionNodeFromGuiWorkflow();
            actions.Children.Add(delete);
        }

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Delete || _machineBuilderFunctionSurface.Visibility != Visibility.Visible)
                return;
            DeleteSelectedFunctionNodeFromGuiWorkflow();
            e.Handled = true;
        };
    }

    private void DeleteSelectedFunctionNodeFromGuiWorkflow()
    {
        var node = GetSelectedMachineBuilderFunctionNode();
        if (node is null)
        {
            SetMachineBuilderFunctionStatus("Select a function node first.", "#D8B26A");
            return;
        }
        if (node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase))
        {
            SetMachineBuilderFunctionStatus("Machine Entry is required and cannot be deleted.", "#D8B26A");
            return;
        }
        DeleteMachineBuilderFunctionNode(node);
    }

    private void ShowMachineBuilderGuiDesignerPhase4()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        ShowMachineBuilderPhase5(MachineBuilderPhase5Mode.Gui);
        if (_machineBuilderPhase5ModeHost is null)
            return;

        _guiDesignerBody ??= BuildMachineBuilderGuiDesignerBody();
        _machineBuilderPhase5ModeHost.Children.Clear();
        _machineBuilderPhase5ModeHost.Children.Add(_guiDesignerBody);
        RefreshMachineBuilderGuiDesigner();
    }

    private FrameworkElement BuildMachineBuilderGuiDesignerBody()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(355) });

        var left = BuildGuiDesignerLeftColumn();
        Grid.SetColumn(left, 0);
        root.Children.Add(left);

        var preview = BuildGuiDesignerPreviewCard();
        Grid.SetColumn(preview, 2);
        root.Children.Add(preview);

        var inspector = BuildGuiDesignerInspectorCard();
        Grid.SetColumn(inspector, 4);
        root.Children.Add(inspector);
        return root;
    }

    private FrameworkElement BuildGuiDesignerLeftColumn()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var stack = new StackPanel();
        stack.Children.Add(BuildGuiDesignerAddWidgetCard());
        var recipe = BuildGuiDesignerRecipeCard();
        recipe.Margin = new Thickness(0, 12, 0, 0);
        stack.Children.Add(recipe);
        scroll.Content = stack;
        return scroll;
    }

    private Border BuildGuiDesignerAddWidgetCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(GuiDesignerTitle(
            "GUI WIDGETS",
            "Pick a typed output from Functions 03, then choose how it should look."));

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Function output", 4));
        _guiDesignerSourceCombo = Phase5ComboBox();
        _guiDesignerSourceCombo.SelectionChanged += (_, _) => RefreshGuiDesignerWidgetChoices();
        panel.Children.Add(_guiDesignerSourceCombo);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Widget", 10));
        _guiDesignerWidgetCombo = Phase5ComboBox();
        panel.Children.Add(_guiDesignerWidgetCombo);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Label", 10));
        _guiDesignerLabelBox = MachineBuilderTextBox();
        _guiDesignerLabelBox.Height = 32;
        _guiDesignerLabelBox.Text = "Widget";
        panel.Children.Add(_guiDesignerLabelBox);

        var add = CreateStudioChromeButton("ADD TO GUI", double.NaN, true);
        add.Height = 36;
        add.HorizontalAlignment = HorizontalAlignment.Stretch;
        add.Margin = new Thickness(0, 12, 0, 0);
        add.Click += (_, _) => AddGuiDesignerBinding();
        panel.Children.Add(add);

        panel.Children.Add(new TextBlock
        {
            Text = "This screen never creates behavior nodes. It only presents outputs that already exist in Functions 03.",
            Foreground = Brush("#7C9098"),
            FontSize = 9,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 14,
            Margin = new Thickness(0, 12, 0, 0)
        });
        card.Child = panel;
        return card;
    }

    private Border BuildGuiDesignerRecipeCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(GuiDesignerTitle(
            "ACTUAL RECIPES",
            "Define what the configured processor from Functions 03 really accepts and produces."));

        _guiDesignerRecipeList = Phase5ListBox(95);
        _guiDesignerRecipeList.SelectionChanged += (_, _) => LoadSelectedGuiDesignerRecipe();
        panel.Children.Add(_guiDesignerRecipeList);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Recipe ID", 10));
        _guiDesignerRecipeIdBox = MachineBuilderTextBox();
        _guiDesignerRecipeIdBox.Height = 30;
        _guiDesignerRecipeIdBox.Text = "recipe_1";
        panel.Children.Add(_guiDesignerRecipeIdBox);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Process from Functions 03", 8));
        _guiDesignerRecipeProcessCombo = Phase5ComboBox();
        _guiDesignerRecipeProcessCombo.SelectionChanged += (_, _) => RefreshGuiDesignerRecipeType();
        panel.Children.Add(_guiDesignerRecipeProcessCombo);

        _guiDesignerRecipeTypeText = MachineBuilderTextBox();
        _guiDesignerRecipeTypeText.Height = 28;
        _guiDesignerRecipeTypeText.IsReadOnly = true;
        _guiDesignerRecipeTypeText.Margin = new Thickness(0, 5, 0, 0);
        _guiDesignerRecipeTypeText.ToolTip = "Automatically derived from the selected process node.";
        panel.Children.Add(_guiDesignerRecipeTypeText);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Input item / count", 10));
        var input = new Grid();
        input.ColumnDefinitions.Add(new ColumnDefinition());
        input.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        input.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        _guiDesignerRecipeInputItemBox = MachineBuilderTextBox();
        _guiDesignerRecipeInputItemBox.Height = 30;
        _guiDesignerRecipeInputItemBox.Text = "minecraft:iron_ingot";
        input.Children.Add(_guiDesignerRecipeInputItemBox);
        _guiDesignerRecipeInputCountBox = MachineBuilderTextBox();
        _guiDesignerRecipeInputCountBox.Height = 30;
        _guiDesignerRecipeInputCountBox.Text = "1";
        Grid.SetColumn(_guiDesignerRecipeInputCountBox, 2);
        input.Children.Add(_guiDesignerRecipeInputCountBox);
        panel.Children.Add(input);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Output item / count", 8));
        var output = new Grid();
        output.ColumnDefinitions.Add(new ColumnDefinition());
        output.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        output.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        _guiDesignerRecipeOutputItemBox = MachineBuilderTextBox();
        _guiDesignerRecipeOutputItemBox.Height = 30;
        _guiDesignerRecipeOutputItemBox.Text = "minecraft:gold_ingot";
        output.Children.Add(_guiDesignerRecipeOutputItemBox);
        _guiDesignerRecipeOutputCountBox = MachineBuilderTextBox();
        _guiDesignerRecipeOutputCountBox.Height = 30;
        _guiDesignerRecipeOutputCountBox.Text = "1";
        Grid.SetColumn(_guiDesignerRecipeOutputCountBox, 2);
        output.Children.Add(_guiDesignerRecipeOutputCountBox);
        panel.Children.Add(output);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Duration ticks / FE per tick", 8));
        var process = new Grid();
        process.ColumnDefinitions.Add(new ColumnDefinition());
        process.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        process.ColumnDefinitions.Add(new ColumnDefinition());
        _guiDesignerRecipeDurationBox = MachineBuilderTextBox();
        _guiDesignerRecipeDurationBox.Height = 30;
        _guiDesignerRecipeDurationBox.Text = "100";
        process.Children.Add(_guiDesignerRecipeDurationBox);
        _guiDesignerRecipeEnergyBox = MachineBuilderTextBox();
        _guiDesignerRecipeEnergyBox.Height = 30;
        _guiDesignerRecipeEnergyBox.Text = "0";
        Grid.SetColumn(_guiDesignerRecipeEnergyBox, 2);
        process.Children.Add(_guiDesignerRecipeEnergyBox);
        panel.Children.Add(process);

        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        var apply = CreateStudioChromeButton("ADD / APPLY", double.NaN, true);
        apply.Height = 33;
        apply.Click += (_, _) => CommitGuiDesignerRecipe();
        actions.Children.Add(apply);
        var remove = CreateStudioChromeButton("REMOVE", double.NaN, false);
        remove.Height = 33;
        remove.Click += (_, _) => RemoveSelectedGuiDesignerRecipe();
        Grid.SetColumn(remove, 2);
        actions.Children.Add(remove);
        panel.Children.Add(actions);

        card.Child = panel;
        return card;
    }

    private Border BuildGuiDesignerPreviewCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(GuiDesignerTitle(
            "LIVE GUI PREVIEW",
            "Drag widgets directly. Coordinates are stored in logical Minecraft GUI pixels."));

        var stage = new Border
        {
            Background = Brush("#070B0D"),
            BorderBrush = Brush("#2A3D45"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(18)
        };
        var frame = new Border
        {
            Width = GuiDesignerLogicalWidth * GuiDesignerScale + 2,
            Height = GuiDesignerLogicalHeight * GuiDesignerScale + 2,
            Background = Brush("#C6C6C6"),
            BorderBrush = Brush("#F8F8F8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2)
        };
        _guiDesignerCanvas = new Canvas
        {
            Width = GuiDesignerLogicalWidth * GuiDesignerScale,
            Height = GuiDesignerLogicalHeight * GuiDesignerScale,
            Background = CreateGuiDesignerGridBrush(),
            ClipToBounds = true,
            Focusable = true
        };
        _guiDesignerCanvas.MouseLeftButtonDown += (_, e) =>
        {
            if (ReferenceEquals(e.OriginalSource, _guiDesignerCanvas))
            {
                _guiDesignerSelectedBindingId = null;
                RefreshGuiDesignerInspector();
                RefreshGuiDesignerCanvas();
            }
        };
        frame.Child = _guiDesignerCanvas;
        stage.Child = frame;
        Grid.SetRow(stage, 2);
        root.Children.Add(stage);

        _guiDesignerStatus = new TextBlock
        {
            Foreground = Brush("#82969E"),
            FontSize = 9.4,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(_guiDesignerStatus, 3);
        root.Children.Add(_guiDesignerStatus);

        card.Child = root;
        return card;
    }

    private Border BuildGuiDesignerInspectorCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        root.RowDefinitions.Add(new RowDefinition());

        root.Children.Add(GuiDesignerTitle(
            "WIDGET INSPECTOR",
            "Select a widget in the preview to edit binding, geometry and preview data."));

        _guiDesignerInspector = new StackPanel();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _guiDesignerInspector
        };
        Grid.SetRow(scroll, 2);
        root.Children.Add(scroll);
        card.Child = root;
        return card;
    }

    private static Brush CreateGuiDesignerGridBrush()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brush("#B8B8B8"), null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        var pen = new Pen(Brush("#A7A7A7"), 0.55);
        group.Children.Add(new GeometryDrawing(null, pen, Geometry.Parse("M 16,0 L 0,0 0,16")));
        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 16, 16),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None
        };
        brush.Freeze();
        return brush;
    }

    private void RefreshMachineBuilderGuiDesigner()
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        RefreshGuiDesignerSources();
        RefreshGuiDesignerRecipes();
        RefreshGuiDesignerBindings();
        RefreshGuiDesignerCanvas();
        RefreshGuiDesignerInspector();
        RefreshGuiDesignerRecipeProcessChoices();
        RefreshGuiDesignerRecipeType();
    }

    private void RefreshGuiDesignerSources()
    {
        if (_guiDesignerSourceCombo is null)
            return;
        _machineBuilderPhase5Sources = BuildMachineBuilderPhase5Sources();
        var previous = _guiDesignerSourceCombo.SelectedItem as MachineBuilderPhase5Source;
        _guiDesignerSourceCombo.ItemsSource = null;
        _guiDesignerSourceCombo.ItemsSource = _machineBuilderPhase5Sources;
        _guiDesignerSourceCombo.SelectedItem = previous is null
            ? _machineBuilderPhase5Sources.FirstOrDefault()
            : _machineBuilderPhase5Sources.FirstOrDefault(source =>
                source.NodeId.Equals(previous.NodeId, StringComparison.OrdinalIgnoreCase) &&
                source.PortId.Equals(previous.PortId, StringComparison.OrdinalIgnoreCase))
              ?? _machineBuilderPhase5Sources.FirstOrDefault();
        RefreshGuiDesignerWidgetChoices();
    }

    private void RefreshGuiDesignerWidgetChoices()
    {
        if (_guiDesignerWidgetCombo is null)
            return;
        var source = _guiDesignerSourceCombo?.SelectedItem as MachineBuilderPhase5Source;
        var choices = GuiDesignerWidgetPresets
            .Where(preset => source is null ||
                preset.AcceptedTypes.Contains("any", StringComparer.OrdinalIgnoreCase) ||
                preset.AcceptedTypes.Contains(source.DataType, StringComparer.OrdinalIgnoreCase))
            .ToList();
        _guiDesignerWidgetCombo.ItemsSource = choices;
        if (choices.Count > 0)
            _guiDesignerWidgetCombo.SelectedIndex = 0;
    }

    private void AddGuiDesignerBinding()
    {
        if (_machineBuilderDocument is null ||
            _guiDesignerSourceCombo?.SelectedItem is not MachineBuilderPhase5Source source ||
            _guiDesignerWidgetCombo?.SelectedItem is not GuiDesignerWidgetPreset widget)
        {
            SetGuiDesignerStatus("Choose a Function output and compatible widget first.", "#D8B26A");
            return;
        }

        if (string.IsNullOrWhiteSpace(_machineBuilderDocument.Gui.ScreenId))
            _machineBuilderDocument.Gui.ScreenId = $"{_machineBuilderDocument.AssetId}_screen";

        var used = _machineBuilderDocument.Gui.Bindings.Select(binding => binding.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId($"gui_{widget.Key}_{source.NodeId}_{source.PortId}");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");

        var index = _machineBuilderDocument.Gui.Bindings.Count;
        var x = 14 + (index % 5) * 30;
        var y = 28 + (index / 5) * 28;
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
                ["label"] = JsonSerializer.SerializeToElement(string.IsNullOrWhiteSpace(_guiDesignerLabelBox?.Text) ? widget.Name : _guiDesignerLabelBox!.Text.Trim()),
                ["x"] = JsonSerializer.SerializeToElement(x),
                ["y"] = JsonSerializer.SerializeToElement(y),
                ["width"] = JsonSerializer.SerializeToElement(widget.DefaultWidth),
                ["height"] = JsonSerializer.SerializeToElement(widget.DefaultHeight),
                ["previewValue"] = JsonSerializer.SerializeToElement(DefaultGuiDesignerPreviewValue(widget.Key)),
                ["order"] = JsonSerializer.SerializeToElement(index)
            }
        };
        _machineBuilderDocument.Gui.Bindings.Add(binding);
        _guiDesignerSelectedBindingId = binding.Id;
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderGuiDesigner();
        SetGuiDesignerStatus($"Added {widget.Name} bound to {source.DisplayName}.", "#74C99A");
    }

    private void RefreshGuiDesignerBindings()
    {
        if (_guiDesignerBindingList is null || _machineBuilderDocument is null)
            return;
        _guiDesignerBindingList.ItemsSource = _machineBuilderDocument.Gui.Bindings
            .OrderBy(binding => ReadGuiDesignerNumber(binding.Properties, "order", 0))
            .Select(binding => binding.Id)
            .ToList();
    }

    private void RefreshGuiDesignerCanvas()
    {
        if (_guiDesignerCanvas is null || _machineBuilderDocument is null)
            return;
        _guiDesignerCanvas.Children.Clear();

        var title = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(_machineBuilderDocument.DisplayName)
                ? _machineBuilderDocument.AssetId
                : _machineBuilderDocument.DisplayName,
            Foreground = Brush("#303030"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(title, 8 * GuiDesignerScale);
        Canvas.SetTop(title, 6 * GuiDesignerScale);
        _guiDesignerCanvas.Children.Add(title);

        foreach (var binding in _machineBuilderDocument.Gui.Bindings
                     .OrderBy(candidate => ReadGuiDesignerNumber(candidate.Properties, "order", 0)))
            DrawGuiDesignerBinding(binding);

        SetGuiDesignerStatus(
            $"{_machineBuilderDocument.Gui.Bindings.Count} widget(s) · {_machineBuilderDocument.Behavior.Nodes.Count} Function node(s) available as data sources · {_machineBuilderDocument.Recipes.Count} recipe(s)",
            "#82969E");
    }

    private void DrawGuiDesignerBinding(ForeverStudioMachineGuiBinding binding)
    {
        if (_guiDesignerCanvas is null)
            return;

        var widget = ReadGuiDesignerString(binding.Properties, "widget", "value");
        var x = ReadGuiDesignerNumber(binding.Properties, "x", 10);
        var y = ReadGuiDesignerNumber(binding.Properties, "y", 30);
        var width = Math.Max(6, ReadGuiDesignerNumber(binding.Properties, "width", 18));
        var height = Math.Max(6, ReadGuiDesignerNumber(binding.Properties, "height", 18));
        var selected = binding.Id.Equals(_guiDesignerSelectedBindingId, StringComparison.OrdinalIgnoreCase);

        var visual = BuildGuiDesignerWidgetVisual(binding, widget, selected);
        visual.Width = width * GuiDesignerScale;
        visual.Height = height * GuiDesignerScale;
        visual.Tag = binding.Id;
        visual.Cursor = Cursors.SizeAll;
        visual.MouseLeftButtonDown += (_, e) => BeginGuiDesignerDrag(binding, visual, e);
        visual.MouseMove += (_, e) => ContinueGuiDesignerDrag(binding, visual, e);
        visual.MouseLeftButtonUp += (_, e) => EndGuiDesignerDrag(binding, visual, e);
        Canvas.SetLeft(visual, Math.Clamp(x, 0, GuiDesignerLogicalWidth - width) * GuiDesignerScale);
        Canvas.SetTop(visual, Math.Clamp(y, 0, GuiDesignerLogicalHeight - height) * GuiDesignerScale);
        _guiDesignerCanvas.Children.Add(visual);
    }

    private Border BuildGuiDesignerWidgetVisual(ForeverStudioMachineGuiBinding binding, string widget, bool selected)
    {
        var preview = ReadGuiDesignerString(binding.Properties, "previewValue", DefaultGuiDesignerPreviewValue(widget));
        var label = ReadGuiDesignerString(binding.Properties, "label", widget);
        var border = new Border
        {
            Background = Brush(widget switch
            {
                "item-slot" or "output-slot" => "#8B8B8B",
                "energy-bar" => "#382E20",
                "fluid-gauge" => "#203C49",
                "status" => "#273B2B",
                _ => "#4B4B4B"
            }),
            BorderBrush = Brush(selected ? "#4FE1DB" : "#202020"),
            BorderThickness = new Thickness(selected ? 2 : 1),
            CornerRadius = new CornerRadius(widget is "item-slot" or "output-slot" ? 1 : 2),
            ToolTip = $"{label}\n{binding.SourceId}.{ReadGuiDesignerString(binding.Properties, "sourcePortId", "?")}" 
        };

        FrameworkElement child = widget switch
        {
            "progress-bar" => BuildGuiDesignerProgressVisual(preview, horizontal: true),
            "energy-bar" => BuildGuiDesignerProgressVisual(preview, horizontal: false),
            "fluid-gauge" => BuildGuiDesignerProgressVisual(preview, horizontal: false),
            "status" => BuildGuiDesignerStatusVisual(preview),
            "item-slot" or "output-slot" => BuildGuiDesignerItemVisual(preview),
            _ => new TextBlock
            {
                Text = preview,
                Foreground = Brushes.White,
                FontSize = 9,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2)
            }
        };
        border.Child = child;
        return border;
    }

    private static FrameworkElement BuildGuiDesignerProgressVisual(string preview, bool horizontal)
    {
        var value = double.TryParse(preview, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 0, 1)
            : 0.65;
        var grid = new Grid { Background = Brush("#202020"), Margin = new Thickness(2) };
        var fill = new Border
        {
            Background = Brush("#D9D9D9"),
            HorizontalAlignment = horizontal ? HorizontalAlignment.Left : HorizontalAlignment.Stretch,
            VerticalAlignment = horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Bottom
        };
        if (horizontal)
            fill.Width = Math.Max(2, 44 * value);
        else
            fill.Height = Math.Max(2, 44 * value);
        grid.Children.Add(fill);
        return grid;
    }

    private static FrameworkElement BuildGuiDesignerStatusVisual(string preview)
        => new TextBlock
        {
            Text = preview.Equals("true", StringComparison.OrdinalIgnoreCase) || preview == "1" ? "●" : "○",
            Foreground = Brushes.White,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

    private static FrameworkElement BuildGuiDesignerItemVisual(string preview)
    {
        var parts = preview.Split('|', 2);
        var item = parts[0];
        var count = parts.Length > 1 ? parts[1] : "1";
        var grid = new Grid();
        grid.Children.Add(new TextBlock
        {
            Text = ShortGuiDesignerItemName(item),
            Foreground = Brush("#F3F3F3"),
            FontSize = 7,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        grid.Children.Add(new TextBlock
        {
            Text = count,
            Foreground = Brushes.White,
            FontSize = 8,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 1, 0)
        });
        return grid;
    }

    private static string ShortGuiDesignerItemName(string item)
    {
        var path = item.Contains(':') ? item[(item.IndexOf(':') + 1)..] : item;
        var words = path.Split(['_', '/', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "ITEM";
        var abbreviation = string.Concat(words.Select(word => char.ToUpperInvariant(word[0])));
        return abbreviation.Length <= 3 ? abbreviation : abbreviation[..3];
    }

    private void BeginGuiDesignerDrag(ForeverStudioMachineGuiBinding binding, FrameworkElement visual, MouseButtonEventArgs e)
    {
        _guiDesignerSelectedBindingId = binding.Id;
        _guiDesignerDraggingBindingId = binding.Id;
        _guiDesignerDragStart = e.GetPosition(_guiDesignerCanvas);
        _guiDesignerDragOriginX = Canvas.GetLeft(visual);
        _guiDesignerDragOriginY = Canvas.GetTop(visual);
        visual.CaptureMouse();
        RefreshGuiDesignerInspector();
        e.Handled = true;
    }

    private void ContinueGuiDesignerDrag(ForeverStudioMachineGuiBinding binding, FrameworkElement visual, MouseEventArgs e)
    {
        if (_guiDesignerDraggingBindingId is null ||
            !_guiDesignerDraggingBindingId.Equals(binding.Id, StringComparison.OrdinalIgnoreCase) ||
            e.LeftButton != MouseButtonState.Pressed || _guiDesignerCanvas is null)
            return;
        var point = e.GetPosition(_guiDesignerCanvas);
        var left = Math.Clamp(_guiDesignerDragOriginX + point.X - _guiDesignerDragStart.X, 0, _guiDesignerCanvas.Width - visual.Width);
        var top = Math.Clamp(_guiDesignerDragOriginY + point.Y - _guiDesignerDragStart.Y, 0, _guiDesignerCanvas.Height - visual.Height);
        Canvas.SetLeft(visual, left);
        Canvas.SetTop(visual, top);
        e.Handled = true;
    }

    private void EndGuiDesignerDrag(ForeverStudioMachineGuiBinding binding, FrameworkElement visual, MouseButtonEventArgs e)
    {
        if (_guiDesignerDraggingBindingId is null ||
            !_guiDesignerDraggingBindingId.Equals(binding.Id, StringComparison.OrdinalIgnoreCase))
            return;
        visual.ReleaseMouseCapture();
        _guiDesignerDraggingBindingId = null;
        binding.Properties["x"] = JsonSerializer.SerializeToElement(Math.Round(Canvas.GetLeft(visual) / GuiDesignerScale, 2));
        binding.Properties["y"] = JsonSerializer.SerializeToElement(Math.Round(Canvas.GetTop(visual) / GuiDesignerScale, 2));
        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerInspector();
        e.Handled = true;
    }

    private void RefreshGuiDesignerInspector()
    {
        if (_guiDesignerInspector is null || _machineBuilderDocument is null)
            return;
        _guiDesignerInspector.Children.Clear();

        var binding = _machineBuilderDocument.Gui.Bindings.FirstOrDefault(candidate =>
            candidate.Id.Equals(_guiDesignerSelectedBindingId, StringComparison.OrdinalIgnoreCase));
        if (binding is null)
        {
            _guiDesignerInspector.Children.Add(new TextBlock
            {
                Text = "No widget selected",
                Foreground = Brush("#EFF5F6"),
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 0)
            });
            _guiDesignerInspector.Children.Add(new TextBlock
            {
                Text = "Add a Function output on the left or click a widget in the live preview.",
                Foreground = Brush("#7E929A"),
                FontSize = 9.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            return;
        }

        var widgetKey = ReadGuiDesignerString(binding.Properties, "widget", "value");
        _guiDesignerInspector.Children.Add(new TextBlock
        {
            Text = ReadGuiDesignerString(binding.Properties, "label", widgetKey),
            Foreground = Brush("#EFF5F6"),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        _guiDesignerInspector.Children.Add(new TextBlock
        {
            Text = $"{binding.Id}\n{binding.SourceId}.{ReadGuiDesignerString(binding.Properties, "sourcePortId", "?")}",
            Foreground = Brush("#788D95"),
            FontSize = 9,
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 5, 0, 12)
        });

        _guiDesignerInspector.Children.Add(MachineBuilderVisualInspectorLabel("Label"));
        var label = MachineBuilderTextBox();
        label.Height = 31;
        label.Text = ReadGuiDesignerString(binding.Properties, "label", widgetKey);
        _guiDesignerInspector.Children.Add(label);

        _guiDesignerInspector.Children.Add(MachineBuilderVisualInspectorLabel("Preview value", 10));
        var preview = MachineBuilderTextBox();
        preview.Height = 31;
        preview.Text = ReadGuiDesignerString(binding.Properties, "previewValue", DefaultGuiDesignerPreviewValue(widgetKey));
        preview.ToolTip = widgetKey is "item-slot" or "output-slot" ? "item_id|count, e.g. minecraft:coal|4" : "For bars use 0..1; status accepts true/false.";
        _guiDesignerInspector.Children.Add(preview);

        _guiDesignerInspector.Children.Add(MachineBuilderVisualInspectorLabel("Position X / Y", 10));
        var position = new Grid();
        position.ColumnDefinitions.Add(new ColumnDefinition());
        position.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        position.ColumnDefinitions.Add(new ColumnDefinition());
        var x = MachineBuilderTextBox();
        x.Height = 31;
        x.Text = ReadGuiDesignerNumber(binding.Properties, "x", 10).ToString("0.##", CultureInfo.InvariantCulture);
        position.Children.Add(x);
        var y = MachineBuilderTextBox();
        y.Height = 31;
        y.Text = ReadGuiDesignerNumber(binding.Properties, "y", 30).ToString("0.##", CultureInfo.InvariantCulture);
        Grid.SetColumn(y, 2);
        position.Children.Add(y);
        _guiDesignerInspector.Children.Add(position);

        _guiDesignerInspector.Children.Add(MachineBuilderVisualInspectorLabel("Width / Height", 10));
        var size = new Grid();
        size.ColumnDefinitions.Add(new ColumnDefinition());
        size.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        size.ColumnDefinitions.Add(new ColumnDefinition());
        var width = MachineBuilderTextBox();
        width.Height = 31;
        width.Text = ReadGuiDesignerNumber(binding.Properties, "width", 18).ToString("0.##", CultureInfo.InvariantCulture);
        size.Children.Add(width);
        var height = MachineBuilderTextBox();
        height.Height = 31;
        height.Text = ReadGuiDesignerNumber(binding.Properties, "height", 18).ToString("0.##", CultureInfo.InvariantCulture);
        Grid.SetColumn(height, 2);
        size.Children.Add(height);
        _guiDesignerInspector.Children.Add(size);

        var apply = CreateStudioChromeButton("APPLY WIDGET", double.NaN, true);
        apply.Height = 34;
        apply.HorizontalAlignment = HorizontalAlignment.Stretch;
        apply.Margin = new Thickness(0, 12, 0, 0);
        apply.Click += (_, _) => ApplyGuiDesignerWidgetEdits(binding, label.Text, preview.Text, x.Text, y.Text, width.Text, height.Text);
        _guiDesignerInspector.Children.Add(apply);

        var remove = CreateStudioChromeButton("REMOVE WIDGET", double.NaN, false);
        remove.Height = 34;
        remove.HorizontalAlignment = HorizontalAlignment.Stretch;
        remove.Margin = new Thickness(0, 7, 0, 0);
        remove.Click += (_, _) => RemoveGuiDesignerBinding(binding);
        _guiDesignerInspector.Children.Add(remove);

        _guiDesignerInspector.Children.Add(new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 14, 0, 0),
            Child = new TextBlock
            {
                Text = "The preview value is editor-only presentation data. Runtime values still come from the bound Function output.",
                Foreground = Brush("#7F939B"),
                FontSize = 9,
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    private void ApplyGuiDesignerWidgetEdits(
        ForeverStudioMachineGuiBinding binding,
        string label,
        string preview,
        string xText,
        string yText,
        string widthText,
        string heightText)
    {
        if (!TryGuiDesignerNumber(xText, out var x) || !TryGuiDesignerNumber(yText, out var y) ||
            !TryGuiDesignerNumber(widthText, out var width) || !TryGuiDesignerNumber(heightText, out var height))
        {
            SetGuiDesignerStatus("Position and size require numeric values.", "#E18B8B");
            return;
        }
        if (width < 4 || height < 4 || width > GuiDesignerLogicalWidth || height > GuiDesignerLogicalHeight)
        {
            SetGuiDesignerStatus("Widget width/height must stay within the GUI canvas.", "#E18B8B");
            return;
        }
        x = Math.Clamp(x, 0, GuiDesignerLogicalWidth - width);
        y = Math.Clamp(y, 0, GuiDesignerLogicalHeight - height);
        binding.Properties["label"] = JsonSerializer.SerializeToElement(label.Trim());
        binding.Properties["previewValue"] = JsonSerializer.SerializeToElement(preview.Trim());
        binding.Properties["x"] = JsonSerializer.SerializeToElement(x);
        binding.Properties["y"] = JsonSerializer.SerializeToElement(y);
        binding.Properties["width"] = JsonSerializer.SerializeToElement(width);
        binding.Properties["height"] = JsonSerializer.SerializeToElement(height);
        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerCanvas();
        RefreshGuiDesignerInspector();
        SetGuiDesignerStatus($"Updated widget '{binding.Id}'.", "#74C99A");
    }

    private void RemoveGuiDesignerBinding(ForeverStudioMachineGuiBinding binding)
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderDocument.Gui.Bindings.Remove(binding);
        _guiDesignerSelectedBindingId = null;
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderGuiDesigner();
        SetGuiDesignerStatus($"Removed widget '{binding.Id}'.", "#D8B26A");
    }

    private void RefreshGuiDesignerRecipeProcessChoices()
    {
        if (_guiDesignerRecipeProcessCombo is null || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        var choices = _machineBuilderDocument.Behavior.Nodes
            .Where(node => IsGuiDesignerProcessNode(node.LibraryItemId))
            .Select(node =>
            {
                var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
                return new GuiDesignerProcessNode(node.Id, $"{item?.DisplayName ?? node.LibraryItemId} · {node.Id}", node.LibraryItemId);
            })
            .ToList();
        var selectedId = (_guiDesignerRecipeProcessCombo.SelectedItem as GuiDesignerProcessNode)?.NodeId;
        _guiDesignerRecipeProcessCombo.ItemsSource = choices;
        _guiDesignerRecipeProcessCombo.SelectedItem = choices.FirstOrDefault(choice => choice.NodeId.Equals(selectedId, StringComparison.OrdinalIgnoreCase))
                                                      ?? choices.FirstOrDefault();
    }

    private static bool IsGuiDesignerProcessNode(string libraryItemId)
        => libraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
           libraryItemId.Contains("generator-core", StringComparison.OrdinalIgnoreCase) ||
           libraryItemId.Contains("recipe", StringComparison.OrdinalIgnoreCase);

    private void RefreshGuiDesignerRecipeType()
    {
        if (_guiDesignerRecipeTypeText is null)
            return;
        var process = _guiDesignerRecipeProcessCombo?.SelectedItem as GuiDesignerProcessNode;
        _guiDesignerRecipeTypeText.Text = InferGuiDesignerRecipeType(process?.LibraryItemId);
        if (_guiDesignerRecipeEnergyBox is not null && process is not null &&
            !process.LibraryItemId.Contains("powered", StringComparison.OrdinalIgnoreCase) &&
            !process.LibraryItemId.Contains("generator", StringComparison.OrdinalIgnoreCase))
        {
            _guiDesignerRecipeEnergyBox.Text = "0";
        }
    }

    private static string InferGuiDesignerRecipeType(string? libraryItemId)
    {
        if (libraryItemId?.Contains("fluid", StringComparison.OrdinalIgnoreCase) == true)
            return "forever_skyblock:fluid_processing";
        if (libraryItemId?.Contains("generator", StringComparison.OrdinalIgnoreCase) == true)
            return "forever_skyblock:generator";
        if (libraryItemId?.Contains("powered", StringComparison.OrdinalIgnoreCase) == true)
            return "forever_skyblock:powered_processing";
        return "forever_skyblock:machine_processing";
    }

    private void RefreshGuiDesignerRecipes()
    {
        if (_guiDesignerRecipeList is null || _machineBuilderDocument is null)
            return;
        var selectedId = (_guiDesignerRecipeList.SelectedItem as GuiDesignerRecipeItem)?.Recipe.Id;
        var items = _machineBuilderDocument.Recipes.Select(recipe => new GuiDesignerRecipeItem { Recipe = recipe }).ToList();
        _guiDesignerRecipeList.ItemsSource = items;
        _guiDesignerRecipeList.SelectedItem = items.FirstOrDefault(item => item.Recipe.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
    }

    private void LoadSelectedGuiDesignerRecipe()
    {
        var recipe = (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe;
        if (recipe is null)
            return;
        if (_guiDesignerRecipeIdBox is not null) _guiDesignerRecipeIdBox.Text = recipe.Id;
        if (_guiDesignerRecipeTypeText is not null) _guiDesignerRecipeTypeText.Text = recipe.RecipeTypeId;

        var processNodeId = ReadGuiDesignerString(recipe.Properties, "processorNodeId", string.Empty);
        if (_guiDesignerRecipeProcessCombo is not null)
            _guiDesignerRecipeProcessCombo.SelectedItem = _guiDesignerRecipeProcessCombo.Items
                .OfType<GuiDesignerProcessNode>()
                .FirstOrDefault(item => item.NodeId.Equals(processNodeId, StringComparison.OrdinalIgnoreCase));

        if (recipe.Properties.TryGetValue("input", out var input) && input.ValueKind == JsonValueKind.Object)
        {
            if (_guiDesignerRecipeInputItemBox is not null && input.TryGetProperty("item", out var item))
                _guiDesignerRecipeInputItemBox.Text = item.GetString() ?? string.Empty;
            if (_guiDesignerRecipeInputCountBox is not null && input.TryGetProperty("count", out var count) && count.TryGetInt32(out var amount))
                _guiDesignerRecipeInputCountBox.Text = amount.ToString(CultureInfo.InvariantCulture);
        }
        if (recipe.Properties.TryGetValue("output", out var output) && output.ValueKind == JsonValueKind.Object)
        {
            if (_guiDesignerRecipeOutputItemBox is not null && output.TryGetProperty("item", out var item))
                _guiDesignerRecipeOutputItemBox.Text = item.GetString() ?? string.Empty;
            if (_guiDesignerRecipeOutputCountBox is not null && output.TryGetProperty("count", out var count) && count.TryGetInt32(out var amount))
                _guiDesignerRecipeOutputCountBox.Text = amount.ToString(CultureInfo.InvariantCulture);
        }
        if (_guiDesignerRecipeDurationBox is not null && recipe.Properties.TryGetValue("processingTimeTicks", out var duration) && duration.TryGetInt32(out var ticks))
            _guiDesignerRecipeDurationBox.Text = ticks.ToString(CultureInfo.InvariantCulture);
        if (_guiDesignerRecipeEnergyBox is not null && recipe.Properties.TryGetValue("energy", out var energy) && energy.ValueKind == JsonValueKind.Object &&
            energy.TryGetProperty("fePerTick", out var fe) && fe.TryGetInt32(out var fePerTick))
            _guiDesignerRecipeEnergyBox.Text = fePerTick.ToString(CultureInfo.InvariantCulture);

        ApplySelectedRecipeToGuiPreview(recipe);
    }

    private void CommitGuiDesignerRecipe()
    {
        if (_machineBuilderDocument is null)
            return;
        var id = SanitizeMachineBuilderGraphId(_guiDesignerRecipeIdBox?.Text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(id))
        {
            SetGuiDesignerStatus("Recipe ID is required.", "#E18B8B");
            return;
        }
        var inputItem = (_guiDesignerRecipeInputItemBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        var outputItem = (_guiDesignerRecipeOutputItemBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsGuiDesignerResourceLocation(inputItem) || !IsGuiDesignerResourceLocation(outputItem))
        {
            SetGuiDesignerStatus("Input/output items must be resource locations such as minecraft:iron_ingot.", "#E18B8B");
            return;
        }
        if (!TryGuiDesignerInt(_guiDesignerRecipeInputCountBox?.Text, 1, 64, out var inputCount) ||
            !TryGuiDesignerInt(_guiDesignerRecipeOutputCountBox?.Text, 1, 64, out var outputCount) ||
            !TryGuiDesignerInt(_guiDesignerRecipeDurationBox?.Text, 1, 1_000_000, out var duration) ||
            !TryGuiDesignerInt(_guiDesignerRecipeEnergyBox?.Text, 0, 1_000_000, out var fePerTick))
        {
            SetGuiDesignerStatus("Counts, duration or FE/t contain unsupported values.", "#E18B8B");
            return;
        }
        if ((long)duration * fePerTick > int.MaxValue)
        {
            SetGuiDesignerStatus("Total recipe energy exceeds the supported range.", "#E18B8B");
            return;
        }

        var process = _guiDesignerRecipeProcessCombo?.SelectedItem as GuiDesignerProcessNode;
        var type = InferGuiDesignerRecipeType(process?.LibraryItemId);
        var selected = (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe;
        var recipe = selected;
        if (recipe is null || (!recipe.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                               _machineBuilderDocument.Recipes.Any(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase))))
        {
            if (_machineBuilderDocument.Recipes.Any(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                SetGuiDesignerStatus($"Recipe ID '{id}' already exists.", "#E18B8B");
                return;
            }
            recipe = new ForeverStudioMachineRecipeBinding();
            _machineBuilderDocument.Recipes.Add(recipe);
        }

        recipe.Id = id;
        recipe.RecipeTypeId = type;
        recipe.Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["authoring"] = JsonSerializer.SerializeToElement("gui-designer"),
            ["processorNodeId"] = JsonSerializer.SerializeToElement(process?.NodeId ?? string.Empty),
            ["input"] = JsonSerializer.SerializeToElement(new { item = inputItem, count = inputCount }),
            ["output"] = JsonSerializer.SerializeToElement(new { item = outputItem, count = outputCount }),
            ["processingTimeTicks"] = JsonSerializer.SerializeToElement(duration),
            ["energy"] = JsonSerializer.SerializeToElement(new
            {
                fePerTick,
                totalFe = duration * fePerTick
            })
        };

        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerRecipes();
        ApplySelectedRecipeToGuiPreview(recipe);
        SetGuiDesignerStatus($"Recipe '{id}' saved: {inputCount}× {inputItem} → {outputCount}× {outputItem}.", "#74C99A");
    }

    private void RemoveSelectedGuiDesignerRecipe()
    {
        if (_machineBuilderDocument is null ||
            (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe is not { } recipe)
            return;
        _machineBuilderDocument.Recipes.Remove(recipe);
        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerRecipes();
        SetGuiDesignerStatus($"Removed recipe '{recipe.Id}'.", "#D8B26A");
    }

    private void ApplySelectedRecipeToGuiPreview(ForeverStudioMachineRecipeBinding recipe)
    {
        if (_machineBuilderDocument is null)
            return;
        var inputItem = "item";
        var inputCount = 1;
        var outputItem = "item";
        var outputCount = 1;
        if (recipe.Properties.TryGetValue("input", out var input) && input.ValueKind == JsonValueKind.Object)
        {
            if (input.TryGetProperty("item", out var item)) inputItem = item.GetString() ?? inputItem;
            if (input.TryGetProperty("count", out var count) && count.TryGetInt32(out var value)) inputCount = value;
        }
        if (recipe.Properties.TryGetValue("output", out var output) && output.ValueKind == JsonValueKind.Object)
        {
            if (output.TryGetProperty("item", out var item)) outputItem = item.GetString() ?? outputItem;
            if (output.TryGetProperty("count", out var count) && count.TryGetInt32(out var value)) outputCount = value;
        }

        foreach (var binding in _machineBuilderDocument.Gui.Bindings)
        {
            var widget = ReadGuiDesignerString(binding.Properties, "widget", string.Empty);
            if (widget is not ("item-slot" or "output-slot"))
                continue;
            var node = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate =>
                candidate.Id.Equals(binding.SourceId, StringComparison.OrdinalIgnoreCase));
            if (node?.LibraryItemId.Contains("output", StringComparison.OrdinalIgnoreCase) == true || widget == "output-slot")
                binding.Properties["previewValue"] = JsonSerializer.SerializeToElement($"{outputItem}|{outputCount}");
            else
                binding.Properties["previewValue"] = JsonSerializer.SerializeToElement($"{inputItem}|{inputCount}");
        }
        RefreshGuiDesignerCanvas();
    }

    private static string DefaultGuiDesignerPreviewValue(string widget)
        => widget switch
        {
            "item-slot" => "minecraft:iron_ingot|1",
            "output-slot" => "minecraft:gold_ingot|1",
            "progress-bar" => "0.65",
            "energy-bar" => "0.72",
            "fluid-gauge" => "0.55",
            "status" => "true",
            _ => "42"
        };

    private static bool TryGuiDesignerNumber(string? value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && double.IsFinite(result);

    private static bool TryGuiDesignerInt(string? value, int min, int max, out int result)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= min && result <= max;

    private static string ReadGuiDesignerString(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static double ReadGuiDesignerNumber(Dictionary<string, JsonElement> properties, string key, double fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetDouble(out var result) ? result : fallback;

    private static bool IsGuiDesignerResourceLocation(string value)
    {
        var split = value.Split(':', 2);
        if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
            return false;
        return split[0].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.') &&
               split[1].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.' or '/');
    }

    private static TextBlock GuiDesignerTitle(string title, string subtitle)
        => new()
        {
            Text = $"{title}\n{subtitle}",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 0, 0, 8)
        };

    private void SetGuiDesignerStatus(string text, string color)
    {
        if (_guiDesignerStatus is not null)
        {
            _guiDesignerStatus.Text = text;
            _guiDesignerStatus.Foreground = Brush(color);
        }
        SetMachineBuilderPhase5Status(text, color);
    }
}

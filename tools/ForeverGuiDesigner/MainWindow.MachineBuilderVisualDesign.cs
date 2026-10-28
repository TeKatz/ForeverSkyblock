using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder Phase 3: visual assembly authoring. Assets from the Phase-2 library become
/// persistent machine components and are edited against the same canonical document used by every
/// later phase. The 3D viewport is a projection of component properties, never a second source of truth.
/// </summary>
public partial class MainWindow
{
    private sealed class MachineBuilderVisualAssetItem
    {
        public required ForeverStudioLibraryItem Item { get; init; }
        public override string ToString() => $"{Item.DisplayName}  ·  {Item.Category}";
    }

    private sealed class MachineBuilderVisualComponentItem
    {
        public required ForeverStudioMachineComponent Component { get; init; }
        public ForeverStudioLibraryItem? LibraryItem { get; init; }
        public override string ToString()
            => $"{Component.DisplayName}  ·  {LibraryItem?.DisplayName ?? Component.LibraryItemId}";
    }

    private bool _machineBuilderVisualPhase3Initialized;
    private Grid? _machineBuilderPhase3Page;
    private ScrollViewer? _machineBuilderIdentityScroll;
    private ScrollViewer? _machineBuilderContextScroll;
    private Border? _machineBuilderVisualSurface;
    private Border? _machineBuilderIdentityStep;
    private Border? _machineBuilderVisualStep;
    private TextBox? _machineBuilderVisualAssetSearch;
    private ListBox? _machineBuilderVisualAssetList;
    private ListBox? _machineBuilderVisualComponentList;
    private MinecraftBlockModelViewport? _machineBuilderVisualViewport;
    private TextBlock? _machineBuilderVisualStatus;
    private TextBlock? _machineBuilderVisualInspectorTitle;
    private TextBlock? _machineBuilderVisualInspectorLibrary;
    private TextBox? _machineBuilderVisualNameBox;
    private TextBox? _machineBuilderVisualTextureBox;
    private TextBox? _machineBuilderVisualAngleBox;
    private CheckBox? _machineBuilderVisualVisibleCheck;
    private readonly Dictionary<string, TextBox> _machineBuilderVisualNumericBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<MinecraftViewportEditMode, Button> _machineBuilderVisualModeButtons = [];
    private readonly Dictionary<string, Button> _machineBuilderVisualAxisButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _machineBuilderVisualRotationAxisButtons = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _machineBuilderVisualElementComponentIds = [];
    private IReadOnlyList<string> _machineBuilderVisualTextures = [];
    private string? _machineBuilderVisualSelectedComponentId;
    private string _machineBuilderVisualRotationAxis = "y";
    private bool _machineBuilderVisualUiUpdating;

    public void EnableForeverMachineBuilderVisualDesignPhase3()
    {
        if (_machineBuilderVisualPhase3Initialized || _studioMachineBuilderWorkspace is not DockPanel root)
            return;
        _machineBuilderVisualPhase3Initialized = true;

        _machineBuilderPhase3Page = root.Children.OfType<Grid>()
            .FirstOrDefault(grid => grid.ColumnDefinitions.Count >= 5);
        if (_machineBuilderPhase3Page is null)
            return;

        _machineBuilderIdentityScroll = _machineBuilderPhase3Page.Children.OfType<ScrollViewer>()
            .FirstOrDefault(scroll => Grid.GetColumn(scroll) == 2);
        _machineBuilderContextScroll = _machineBuilderPhase3Page.Children.OfType<ScrollViewer>()
            .FirstOrDefault(scroll => Grid.GetColumn(scroll) == 4);

        _machineBuilderVisualSurface = BuildMachineBuilderVisualDesignSurface();
        Grid.SetColumn(_machineBuilderVisualSurface, 2);
        Grid.SetColumnSpan(_machineBuilderVisualSurface, 3);
        Panel.SetZIndex(_machineBuilderVisualSurface, 30);
        _machineBuilderVisualSurface.Visibility = Visibility.Collapsed;
        _machineBuilderPhase3Page.Children.Add(_machineBuilderVisualSurface);

        InstallMachineBuilderPhase3WorkflowNavigation();
        InstallMachineBuilderVisualContinueButton();
        UpdateMachineBuilderPhase3Labels();
        RefreshMachineBuilderVisualAssetPalette();
    }

    private void InstallMachineBuilderPhase3WorkflowNavigation()
    {
        if (_machineBuilderPhase3Page is null)
            return;

        var workflowCard = _machineBuilderPhase3Page.Children.OfType<Border>()
            .FirstOrDefault(border => Grid.GetColumn(border) == 0 && border.Child is StackPanel panel &&
                                      panel.Children.OfType<TextBlock>().Any(text => text.Text == "GUIDED WORKFLOW"));
        if (workflowCard?.Child is not StackPanel workflow)
            return;

        _machineBuilderIdentityStep = FindMachineBuilderWorkflowStep(workflow, "Identity");
        _machineBuilderVisualStep = FindMachineBuilderWorkflowStep(workflow, "Visual Design");

        if (_machineBuilderIdentityStep is not null)
        {
            _machineBuilderIdentityStep.Cursor = Cursors.Hand;
            _machineBuilderIdentityStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderIdentityPhase3();
        }

        if (_machineBuilderVisualStep is not null)
        {
            _machineBuilderVisualStep.Cursor = Cursors.Hand;
            _machineBuilderVisualStep.Opacity = 1;
            if (_machineBuilderVisualStep.Child is Grid grid)
            {
                var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
                var description = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                if (description is not null)
                    description.Text = "Shape, assets and textures";
            }
            _machineBuilderVisualStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderVisualDesignPhase3();
        }

        SetMachineBuilderPhase3StepState(showVisual: false);
    }

    private static Border? FindMachineBuilderWorkflowStep(StackPanel workflow, string title)
    {
        foreach (var border in workflow.Children.OfType<Border>())
        {
            if (border.Child is not Grid grid)
                continue;
            var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
            if (copy?.Children.OfType<TextBlock>().FirstOrDefault()?.Text == title)
                return border;
        }
        return null;
    }

    private void InstallMachineBuilderVisualContinueButton()
    {
        if (_machineBuilderSaveButton?.Parent is not Grid actions || actions.Parent is not StackPanel identityPanel)
            return;
        if (identityPanel.Children.OfType<Button>()
            .Any(button => string.Equals(button.Tag?.ToString(), "machine_builder_visual_continue", StringComparison.Ordinal)))
            return;

        var continueButton = CreateStudioChromeButton("CONTINUE TO VISUAL DESIGN  →", double.NaN, true);
        continueButton.Tag = "machine_builder_visual_continue";
        continueButton.Height = 42;
        continueButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        continueButton.Margin = new Thickness(0, 14, 0, 0);
        continueButton.Click += (_, _) => ShowMachineBuilderVisualDesignPhase3();
        identityPanel.Children.Add(continueButton);
    }

    private void UpdateMachineBuilderPhase3Labels()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;

        foreach (var text in FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace))
        {
            if (text.Text.StartsWith("PHASE 2 ·", StringComparison.OrdinalIgnoreCase))
                text.Text = "PHASE 3 · VISUAL ASSEMBLY";
            else if (text.Text.StartsWith("Phase 2 establishes the reusable production catalog", StringComparison.OrdinalIgnoreCase))
            {
                text.Text =
                    "Phase 3 unlocks visual assembly. Place reusable assets, shape them in 3D and bind project textures; Function remains locked until Phase 4.";
            }
        }
    }

    private Border BuildMachineBuilderVisualDesignSurface()
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
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
        shell.RowDefinitions.Add(new RowDefinition());

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock
        {
            Text = "VISUAL DESIGN · PHASE 3",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Assemble the machine from reusable assets",
            Foreground = Brush("#EFF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Every placed asset becomes a stable semantic component that later Function, GUI and Animation phases can reference.",
            Foreground = Brush("#899CA4"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        header.Children.Add(heading);

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var identity = CreateStudioChromeButton("← IDENTITY", 104, false);
        identity.Height = 36;
        identity.Click += (_, _) => ShowMachineBuilderIdentityPhase3();
        headerActions.Children.Add(identity);
        var save = CreateStudioChromeButton("SAVE PROJECT", 126, true);
        save.Height = 36;
        save.Margin = new Thickness(9, 0, 0, 0);
        save.Click += (_, _) => SaveMachineBuilderProject();
        headerActions.Children.Add(save);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);
        shell.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

        var palette = BuildMachineBuilderVisualPalette();
        Grid.SetColumn(palette, 0);
        body.Children.Add(palette);

        var preview = BuildMachineBuilderVisualPreview();
        Grid.SetColumn(preview, 2);
        body.Children.Add(preview);

        var inspector = BuildMachineBuilderVisualInspector();
        Grid.SetColumn(inspector, 4);
        body.Children.Add(inspector);

        Grid.SetRow(body, 2);
        shell.Children.Add(body);
        surface.Child = shell;
        return surface;
    }

    private Border BuildMachineBuilderVisualPalette()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "ASSET PALETTE",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        };
        grid.Children.Add(title);

        _machineBuilderVisualAssetSearch = MachineBuilderTextBox();
        _machineBuilderVisualAssetSearch.Height = 34;
        _machineBuilderVisualAssetSearch.Margin = new Thickness(0, 8, 0, 0);
        _machineBuilderVisualAssetSearch.ToolTip = "Search visual assets by name, category or tag";
        _machineBuilderVisualAssetSearch.TextChanged += (_, _) => RefreshMachineBuilderVisualAssetPalette();
        Grid.SetRow(_machineBuilderVisualAssetSearch, 1);
        grid.Children.Add(_machineBuilderVisualAssetSearch);

        _machineBuilderVisualAssetList = new ListBox
        {
            Background = Brush("#091014"),
            Foreground = Brush("#D7E2E5"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            FontSize = 10.5,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        _machineBuilderVisualAssetList.MouseDoubleClick += (_, _) => AddSelectedMachineBuilderVisualAsset();
        Grid.SetRow(_machineBuilderVisualAssetList, 3);
        grid.Children.Add(_machineBuilderVisualAssetList);

        var add = CreateStudioChromeButton("+ ADD SELECTED ASSET", double.NaN, true);
        add.Height = 36;
        add.Margin = new Thickness(0, 8, 0, 0);
        add.HorizontalAlignment = HorizontalAlignment.Stretch;
        add.Click += (_, _) => AddSelectedMachineBuilderVisualAsset();
        Grid.SetRow(add, 4);
        grid.Children.Add(add);

        var assemblyLabel = new TextBlock
        {
            Text = "ASSEMBLY COMPONENTS",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        };
        Grid.SetRow(assemblyLabel, 6);
        grid.Children.Add(assemblyLabel);

        _machineBuilderVisualComponentList = new ListBox
        {
            Background = Brush("#091014"),
            Foreground = Brush("#D7E2E5"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            FontSize = 10.5,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        _machineBuilderVisualComponentList.SelectionChanged += (_, _) => MachineBuilderVisualComponentSelectionChanged();
        Grid.SetRow(_machineBuilderVisualComponentList, 7);
        grid.Children.Add(_machineBuilderVisualComponentList);

        var componentActions = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        componentActions.ColumnDefinitions.Add(new ColumnDefinition());
        componentActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        componentActions.ColumnDefinitions.Add(new ColumnDefinition());
        var duplicate = CreateStudioChromeButton("DUPLICATE", double.NaN, false);
        duplicate.Height = 34;
        duplicate.Click += (_, _) => DuplicateMachineBuilderVisualComponent();
        componentActions.Children.Add(duplicate);
        var delete = CreateStudioChromeButton("DELETE", double.NaN, false);
        delete.Height = 34;
        delete.Click += (_, _) => DeleteMachineBuilderVisualComponent();
        Grid.SetColumn(delete, 2);
        componentActions.Children.Add(delete);
        Grid.SetRow(componentActions, 8);
        grid.Children.Add(componentActions);

        card.Child = grid;
        return card;
    }

    private Border BuildMachineBuilderVisualPreview()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var toolbar = new Grid();
        toolbar.ColumnDefinitions.Add(new ColumnDefinition());
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var modePanel = new StackPanel { Orientation = Orientation.Horizontal };
        AddMachineBuilderVisualModeButton(modePanel, "SELECT", MinecraftViewportEditMode.None, 70);
        AddMachineBuilderVisualModeButton(modePanel, "MOVE", MinecraftViewportEditMode.Move, 66);
        AddMachineBuilderVisualModeButton(modePanel, "SCALE", MinecraftViewportEditMode.Scale, 68);
        modePanel.Children.Add(new Border
        {
            Width = 1,
            Height = 24,
            Background = Brush("#30434B"),
            Margin = new Thickness(5, 4, 10, 4)
        });
        foreach (var axis in new[] { "x", "y", "z" })
            AddMachineBuilderVisualAxisButton(modePanel, axis);
        toolbar.Children.Add(modePanel);

        var reset = CreateStudioChromeButton("RESET VIEW", 98, false);
        reset.Height = 32;
        reset.Click += (_, _) => _machineBuilderVisualViewport?.ResetView();
        Grid.SetColumn(reset, 1);
        toolbar.Children.Add(reset);
        grid.Children.Add(toolbar);

        var viewportFrame = new Border
        {
            Background = Brush("#090E11"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true
        };
        _machineBuilderVisualViewport = new MinecraftBlockModelViewport
        {
            MinHeight = 480,
            EditMode = MinecraftViewportEditMode.None,
            EditAxis = "x"
        };
        _machineBuilderVisualViewport.ElementSelected += MachineBuilderVisualViewportElementSelected;
        _machineBuilderVisualViewport.ElementDragDelta += MachineBuilderVisualViewportTransformDelta;
        viewportFrame.Child = _machineBuilderVisualViewport;
        Grid.SetRow(viewportFrame, 2);
        grid.Children.Add(viewportFrame);

        var footer = new Grid { Margin = new Thickness(2, 8, 2, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _machineBuilderVisualStatus = new TextBlock
        {
            Text = "Add an asset to begin.",
            Foreground = Brush("#82969E"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap
        };
        footer.Children.Add(_machineBuilderVisualStatus);
        var hint = new TextBlock
        {
            Text = "Left: select/drag · Middle: pan · Wheel: zoom · Right: precision nudges",
            Foreground = Brush("#61757E"),
            FontSize = 9,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(hint, 1);
        footer.Children.Add(hint);
        Grid.SetRow(footer, 3);
        grid.Children.Add(footer);

        card.Child = grid;
        return card;
    }

    private void AddMachineBuilderVisualModeButton(Panel parent, string label, MinecraftViewportEditMode mode, double width)
    {
        var button = CreateStudioChromeButton(label, width, false);
        button.Height = 32;
        button.Margin = new Thickness(0, 0, 6, 0);
        button.Click += (_, _) => SetMachineBuilderVisualViewportMode(mode);
        _machineBuilderVisualModeButtons[mode] = button;
        parent.Children.Add(button);
    }

    private void AddMachineBuilderVisualAxisButton(Panel parent, string axis)
    {
        var button = CreateStudioChromeButton(axis.ToUpperInvariant(), 38, false);
        button.Height = 32;
        button.Margin = new Thickness(0, 0, 5, 0);
        button.Click += (_, _) => SetMachineBuilderVisualViewportAxis(axis);
        _machineBuilderVisualAxisButtons[axis] = button;
        parent.Children.Add(button);
    }

    private Border BuildMachineBuilderVisualInspector()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = "COMPONENT INSPECTOR",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });
        _machineBuilderVisualInspectorTitle = new TextBlock
        {
            Text = "No component selected",
            Foreground = Brush("#EFF5F6"),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        };
        panel.Children.Add(_machineBuilderVisualInspectorTitle);
        _machineBuilderVisualInspectorLibrary = new TextBlock
        {
            Text = "Select a placed asset from the assembly list or viewport.",
            Foreground = Brush("#7E929A"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 14)
        };
        panel.Children.Add(_machineBuilderVisualInspectorLibrary);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Display name"));
        _machineBuilderVisualNameBox = MachineBuilderTextBox();
        _machineBuilderVisualNameBox.Height = 34;
        HookMachineBuilderVisualCommit(_machineBuilderVisualNameBox);
        panel.Children.Add(_machineBuilderVisualNameBox);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Position", 14));
        panel.Children.Add(BuildMachineBuilderVisualVectorEditor("position"));
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Size", 12));
        panel.Children.Add(BuildMachineBuilderVisualVectorEditor("size"));
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Pivot", 12));
        panel.Children.Add(BuildMachineBuilderVisualVectorEditor("pivot"));

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Rotation", 14));
        var rotationAxes = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var axis in new[] { "x", "y", "z" })
        {
            var button = CreateStudioChromeButton(axis.ToUpperInvariant(), 44, false);
            button.Height = 31;
            button.Margin = new Thickness(0, 0, 6, 0);
            button.Click += (_, _) => SetMachineBuilderVisualRotationAxis(axis);
            _machineBuilderVisualRotationAxisButtons[axis] = button;
            rotationAxes.Children.Add(button);
        }
        panel.Children.Add(rotationAxes);
        _machineBuilderVisualAngleBox = MachineBuilderTextBox();
        _machineBuilderVisualAngleBox.Height = 34;
        _machineBuilderVisualAngleBox.Margin = new Thickness(0, 7, 0, 0);
        _machineBuilderVisualAngleBox.ToolTip = "Rotation angle in degrees around the selected axis";
        HookMachineBuilderVisualCommit(_machineBuilderVisualAngleBox);
        panel.Children.Add(_machineBuilderVisualAngleBox);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Texture resource", 14));
        _machineBuilderVisualTextureBox = MachineBuilderTextBox();
        _machineBuilderVisualTextureBox.Height = 34;
        _machineBuilderVisualTextureBox.ToolTip = "Example: forever_skyblock:block/ascension_materials";
        HookMachineBuilderVisualCommit(_machineBuilderVisualTextureBox);
        panel.Children.Add(_machineBuilderVisualTextureBox);

        var textureHint = new TextBlock
        {
            Foreground = Brush("#647982"),
            FontSize = 9,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 5, 0, 0)
        };
        textureHint.Text = "Project block textures are discovered automatically. Type a resource path; matching project textures are listed in the field tooltip after opening Visual Design.";
        panel.Children.Add(textureHint);

        _machineBuilderVisualVisibleCheck = new CheckBox
        {
            Content = "Visible in block model",
            Foreground = Brush("#C8D5D9"),
            Margin = new Thickness(0, 14, 0, 0)
        };
        _machineBuilderVisualVisibleCheck.Click += (_, _) => CommitMachineBuilderVisualInspector();
        panel.Children.Add(_machineBuilderVisualVisibleCheck);

        var reset = CreateStudioChromeButton("RESET ASSET DEFAULTS", double.NaN, false);
        reset.Height = 36;
        reset.HorizontalAlignment = HorizontalAlignment.Stretch;
        reset.Margin = new Thickness(0, 18, 0, 0);
        reset.Click += (_, _) => ResetMachineBuilderVisualComponentDefaults();
        panel.Children.Add(reset);

        var note = new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 18, 0, 0),
            Child = new TextBlock
            {
                Text = "Phase 3 saves semantic components and transforms into the Machine Project. Game model/blockstate files are generated only by the final production pipeline, so visual authoring stays reversible.",
                Foreground = Brush("#7F939B"),
                FontSize = 9.5,
                LineHeight = 15,
                TextWrapping = TextWrapping.Wrap
            }
        };
        panel.Children.Add(note);

        scroll.Content = panel;
        card.Child = scroll;
        return card;
    }

    private static TextBlock MachineBuilderVisualInspectorLabel(string text, double top = 0)
        => new()
        {
            Text = text,
            Foreground = Brush("#BFCED2"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(1, top, 0, 6)
        };

    private Grid BuildMachineBuilderVisualVectorEditor(string prefix)
    {
        var grid = new Grid();
        for (var i = 0; i < 3; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        var axes = new[] { "x", "y", "z" };
        for (var i = 0; i < axes.Length; i++)
        {
            var axis = axes[i];
            var stack = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 2 ? 0 : 4, 0) };
            stack.Children.Add(new TextBlock
            {
                Text = axis.ToUpperInvariant(),
                Foreground = Brush("#61757E"),
                FontSize = 8.5,
                Margin = new Thickness(1, 0, 0, 3)
            });
            var box = MachineBuilderTextBox();
            box.Height = 32;
            HookMachineBuilderVisualCommit(box);
            _machineBuilderVisualNumericBoxes[$"{prefix}.{axis}"] = box;
            stack.Children.Add(box);
            Grid.SetColumn(stack, i);
            grid.Children.Add(stack);
        }
        return grid;
    }

    private void HookMachineBuilderVisualCommit(TextBox box)
    {
        box.LostKeyboardFocus += (_, _) => CommitMachineBuilderVisualInspector();
        box.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            CommitMachineBuilderVisualInspector();
            e.Handled = true;
        };
    }

    private void ShowMachineBuilderVisualDesignPhase3()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        _machineBuilderVisualTextures = ForeverStudioVisualAssemblyService.ScanBlockTextures(_repoRoot);
        if (_machineBuilderVisualTextureBox is not null)
        {
            _machineBuilderVisualTextureBox.ToolTip = _machineBuilderVisualTextures.Count == 0
                ? "No project block textures were discovered."
                : "Project textures:\n" + string.Join("\n", _machineBuilderVisualTextures.Take(24));
        }

        if (_machineBuilderIdentityScroll is not null)
            _machineBuilderIdentityScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderContextScroll is not null)
            _machineBuilderContextScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderVisualSurface is not null)
            _machineBuilderVisualSurface.Visibility = Visibility.Visible;

        SetMachineBuilderPhase3StepState(showVisual: true);
        RefreshMachineBuilderVisualAssetPalette();
        RefreshMachineBuilderVisualComponentList(_machineBuilderVisualSelectedComponentId);
        RefreshMachineBuilderVisualPreview();
    }

    private void ShowMachineBuilderIdentityPhase3()
    {
        CommitMachineBuilderVisualInspector();
        if (_machineBuilderVisualSurface is not null)
            _machineBuilderVisualSurface.Visibility = Visibility.Collapsed;
        if (_machineBuilderIdentityScroll is not null)
            _machineBuilderIdentityScroll.Visibility = Visibility.Visible;
        if (_machineBuilderContextScroll is not null)
            _machineBuilderContextScroll.Visibility = Visibility.Visible;
        SetMachineBuilderPhase3StepState(showVisual: false);
    }

    private void SetMachineBuilderPhase3StepState(bool showVisual)
    {
        StyleMachineBuilderPhase3Step(_machineBuilderIdentityStep, active: !showVisual, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderVisualStep, active: showVisual, available: true);
    }

    private static void StyleMachineBuilderPhase3Step(Border? border, bool active, bool available)
    {
        if (border is null)
            return;
        border.Background = Brush(active ? "#173035" : "#11191D");
        border.BorderBrush = Brush(active ? "#4B9B9E" : available ? "#36515A" : "#27363D");
        border.Opacity = available ? 1 : 0.62;
        if (border.Child is not Grid grid)
            return;
        var number = grid.Children.OfType<TextBlock>().FirstOrDefault();
        if (number is not null)
            number.Foreground = Brush(active ? "#74D2D0" : available ? "#78939D" : "#61727A");
        var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
        var title = copy?.Children.OfType<TextBlock>().FirstOrDefault();
        if (title is not null)
            title.Foreground = Brush(active ? "#E8F2F3" : available ? "#C7D5D9" : "#A2B0B5");
    }

    private void RefreshMachineBuilderVisualAssetPalette()
    {
        if (_machineBuilderVisualAssetList is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        var query = _machineBuilderVisualAssetSearch?.Text ?? string.Empty;
        var items = _machineBuilderLibraryLoad.Catalog
            .Search(query, ForeverStudioLibraryItemKind.Asset)
            .Where(item => item.Capabilities.Contains("visual-component", StringComparer.OrdinalIgnoreCase))
            .Select(item => new MachineBuilderVisualAssetItem { Item = item })
            .ToList();
        var selectedId = (_machineBuilderVisualAssetList.SelectedItem as MachineBuilderVisualAssetItem)?.Item.Id;
        _machineBuilderVisualAssetList.ItemsSource = items;
        _machineBuilderVisualAssetList.SelectedItem = items.FirstOrDefault(item =>
            string.Equals(item.Item.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        if (_machineBuilderVisualAssetList.SelectedItem is null && items.Count > 0)
            _machineBuilderVisualAssetList.SelectedIndex = 0;
    }

    private void AddSelectedMachineBuilderVisualAsset()
    {
        if (_machineBuilderDocument is null || _machineBuilderVisualAssetList?.SelectedItem is not MachineBuilderVisualAssetItem selected)
            return;
        var component = ForeverStudioVisualAssemblyService.CreateComponent(selected.Item, _machineBuilderDocument.Components);
        _machineBuilderDocument.Components.Add(component);
        _machineBuilderVisualSelectedComponentId = component.Id;
        MarkMachineBuilderVisualDirty();
        RefreshMachineBuilderVisualComponentList(component.Id);
        RefreshMachineBuilderVisualPreview();
        SetMachineBuilderVisualStatus($"Added {selected.Item.DisplayName} as '{component.Id}'.", "#74C99A");
    }

    private void RefreshMachineBuilderVisualComponentList(string? requestedId = null)
    {
        if (_machineBuilderVisualComponentList is null || _machineBuilderDocument is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        var id = requestedId ?? _machineBuilderVisualSelectedComponentId;
        var items = _machineBuilderDocument.Components
            .Where(component => !component.Id.Equals("root", StringComparison.OrdinalIgnoreCase))
            .Select(component => new MachineBuilderVisualComponentItem
            {
                Component = component,
                LibraryItem = _machineBuilderLibraryLoad.Catalog.Find(component.LibraryItemId, component.LibraryItemVersion)
            })
            .ToList();

        _machineBuilderVisualUiUpdating = true;
        try
        {
            _machineBuilderVisualComponentList.ItemsSource = items;
            _machineBuilderVisualComponentList.SelectedItem = items.FirstOrDefault(item =>
                string.Equals(item.Component.Id, id, StringComparison.OrdinalIgnoreCase));
            if (_machineBuilderVisualComponentList.SelectedItem is null && items.Count > 0)
                _machineBuilderVisualComponentList.SelectedIndex = 0;
            _machineBuilderVisualSelectedComponentId =
                (_machineBuilderVisualComponentList.SelectedItem as MachineBuilderVisualComponentItem)?.Component.Id;
        }
        finally
        {
            _machineBuilderVisualUiUpdating = false;
        }
        PopulateMachineBuilderVisualInspector();
    }

    private void MachineBuilderVisualComponentSelectionChanged()
    {
        if (_machineBuilderVisualUiUpdating)
            return;
        _machineBuilderVisualSelectedComponentId =
            (_machineBuilderVisualComponentList?.SelectedItem as MachineBuilderVisualComponentItem)?.Component.Id;
        PopulateMachineBuilderVisualInspector();
        SelectMachineBuilderVisualPreviewElement();
    }

    private ForeverStudioMachineComponent? GetSelectedMachineBuilderVisualComponent()
        => _machineBuilderDocument?.Components.FirstOrDefault(component =>
            string.Equals(component.Id, _machineBuilderVisualSelectedComponentId, StringComparison.OrdinalIgnoreCase));

    private void PopulateMachineBuilderVisualInspector()
    {
        var component = GetSelectedMachineBuilderVisualComponent();
        _machineBuilderVisualUiUpdating = true;
        try
        {
            if (_machineBuilderVisualInspectorTitle is not null)
                _machineBuilderVisualInspectorTitle.Text = component?.DisplayName ?? "No component selected";
            if (_machineBuilderVisualInspectorLibrary is not null)
            {
                var item = component is null || _machineBuilderLibraryLoad is null
                    ? null
                    : _machineBuilderLibraryLoad.Catalog.Find(component.LibraryItemId, component.LibraryItemVersion);
                _machineBuilderVisualInspectorLibrary.Text = component is null
                    ? "Select a placed asset from the assembly list or viewport."
                    : $"{component.Id}  ·  {component.LibraryItemId} v{component.LibraryItemVersion}\n{item?.Category ?? "Unknown library source"}";
            }

            SetMachineBuilderVisualInspectorEnabled(component is not null);
            if (component is null)
                return;

            if (_machineBuilderVisualNameBox is not null)
                _machineBuilderVisualNameBox.Text = component.DisplayName;
            SetMachineBuilderVisualVectorBoxes("position", ForeverStudioVisualAssemblyService.ReadVector(component, "position", [0, 0, 0]));
            SetMachineBuilderVisualVectorBoxes("size", ForeverStudioVisualAssemblyService.ReadVector(component, "size", [16, 16, 16]));
            SetMachineBuilderVisualVectorBoxes("pivot", ForeverStudioVisualAssemblyService.ReadVector(component, "pivot", [8, 8, 8]));
            _machineBuilderVisualRotationAxis = ForeverStudioVisualAssemblyService.ReadString(component, "rotationAxis", "y");
            if (_machineBuilderVisualRotationAxis is not ("x" or "y" or "z"))
                _machineBuilderVisualRotationAxis = "y";
            if (_machineBuilderVisualAngleBox is not null)
                _machineBuilderVisualAngleBox.Text = FormatMachineBuilderVisualNumber(
                    ForeverStudioVisualAssemblyService.ReadNumber(component, "rotationAngle", 0));
            if (_machineBuilderVisualTextureBox is not null)
                _machineBuilderVisualTextureBox.Text = ForeverStudioVisualAssemblyService.ReadString(
                    component, "texture", ForeverStudioVisualAssemblyService.DefaultTexture);
            if (_machineBuilderVisualVisibleCheck is not null)
                _machineBuilderVisualVisibleCheck.IsChecked = ForeverStudioVisualAssemblyService.ReadBoolean(component, "visible", true);
            UpdateMachineBuilderVisualRotationAxisButtons();
        }
        finally
        {
            _machineBuilderVisualUiUpdating = false;
        }
    }

    private void SetMachineBuilderVisualInspectorEnabled(bool enabled)
    {
        if (_machineBuilderVisualNameBox is not null) _machineBuilderVisualNameBox.IsEnabled = enabled;
        if (_machineBuilderVisualTextureBox is not null) _machineBuilderVisualTextureBox.IsEnabled = enabled;
        if (_machineBuilderVisualAngleBox is not null) _machineBuilderVisualAngleBox.IsEnabled = enabled;
        if (_machineBuilderVisualVisibleCheck is not null) _machineBuilderVisualVisibleCheck.IsEnabled = enabled;
        foreach (var box in _machineBuilderVisualNumericBoxes.Values) box.IsEnabled = enabled;
        foreach (var button in _machineBuilderVisualRotationAxisButtons.Values) button.IsEnabled = enabled;
    }

    private void SetMachineBuilderVisualVectorBoxes(string prefix, double[] values)
    {
        var axes = new[] { "x", "y", "z" };
        for (var i = 0; i < axes.Length; i++)
        {
            if (_machineBuilderVisualNumericBoxes.TryGetValue($"{prefix}.{axes[i]}", out var box))
                box.Text = FormatMachineBuilderVisualNumber(values.ElementAtOrDefault(i));
        }
    }

    private void CommitMachineBuilderVisualInspector()
    {
        if (_machineBuilderVisualUiUpdating || GetSelectedMachineBuilderVisualComponent() is not { } component)
            return;

        if (!TryReadMachineBuilderVisualVector("position", out var position) ||
            !TryReadMachineBuilderVisualVector("size", out var size) ||
            !TryReadMachineBuilderVisualVector("pivot", out var pivot) ||
            _machineBuilderVisualAngleBox is null ||
            !TryParseMachineBuilderVisualNumber(_machineBuilderVisualAngleBox.Text, out var angle))
        {
            SetMachineBuilderVisualStatus("Invalid transform value. Changes were not applied.", "#E18B8B");
            return;
        }

        for (var i = 0; i < 3; i++)
        {
            position[i] = Math.Clamp(position[i], -64, 64);
            size[i] = Math.Clamp(size[i], 0.05, 64);
            pivot[i] = Math.Clamp(pivot[i], -64, 64);
        }
        angle = Math.Clamp(angle, -360, 360);

        var name = _machineBuilderVisualNameBox?.Text.Trim();
        component.DisplayName = string.IsNullOrWhiteSpace(name) ? component.Id : name;
        ForeverStudioVisualAssemblyService.WriteVector(component, "position", position);
        ForeverStudioVisualAssemblyService.WriteVector(component, "size", size);
        ForeverStudioVisualAssemblyService.WriteVector(component, "pivot", pivot);
        ForeverStudioVisualAssemblyService.WriteString(component, "rotationAxis", _machineBuilderVisualRotationAxis);
        ForeverStudioVisualAssemblyService.WriteNumber(component, "rotationAngle", angle);
        ForeverStudioVisualAssemblyService.WriteString(
            component,
            "texture",
            string.IsNullOrWhiteSpace(_machineBuilderVisualTextureBox?.Text)
                ? ForeverStudioVisualAssemblyService.DefaultTexture
                : _machineBuilderVisualTextureBox!.Text.Trim());
        ForeverStudioVisualAssemblyService.WriteBoolean(component, "visible", _machineBuilderVisualVisibleCheck?.IsChecked != false);

        MarkMachineBuilderVisualDirty();
        RefreshMachineBuilderVisualComponentList(component.Id);
        RefreshMachineBuilderVisualPreview();
        SetMachineBuilderVisualStatus($"Updated {component.DisplayName}.", "#74C99A");
    }

    private bool TryReadMachineBuilderVisualVector(string prefix, out double[] values)
    {
        values = new double[3];
        var axes = new[] { "x", "y", "z" };
        for (var i = 0; i < axes.Length; i++)
        {
            if (!_machineBuilderVisualNumericBoxes.TryGetValue($"{prefix}.{axes[i]}", out var box) ||
                !TryParseMachineBuilderVisualNumber(box.Text, out values[i]))
                return false;
        }
        return true;
    }

    private static bool TryParseMachineBuilderVisualNumber(string value, out double result)
        => (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result)) &&
           double.IsFinite(result);

    private static string FormatMachineBuilderVisualNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private void DuplicateMachineBuilderVisualComponent()
    {
        if (_machineBuilderDocument is null || GetSelectedMachineBuilderVisualComponent() is not { } source ||
            _machineBuilderLibraryLoad?.Catalog.Find(source.LibraryItemId, source.LibraryItemVersion) is not { } asset)
            return;
        var copy = ForeverStudioVisualAssemblyService.CreateComponent(asset, _machineBuilderDocument.Components);
        copy.DisplayName = source.DisplayName + " Copy";
        copy.ParentId = source.ParentId;
        copy.Properties = source.Properties.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone(),
            StringComparer.OrdinalIgnoreCase);
        _machineBuilderDocument.Components.Add(copy);
        _machineBuilderVisualSelectedComponentId = copy.Id;
        MarkMachineBuilderVisualDirty();
        RefreshMachineBuilderVisualComponentList(copy.Id);
        RefreshMachineBuilderVisualPreview();
    }

    private void DeleteMachineBuilderVisualComponent()
    {
        if (_machineBuilderDocument is null || GetSelectedMachineBuilderVisualComponent() is not { } component)
            return;

        if (_machineBuilderDocument.Components.Any(child => string.Equals(child.ParentId, component.Id, StringComparison.OrdinalIgnoreCase)) ||
            _machineBuilderDocument.Animations.Any(binding => string.Equals(binding.TargetComponentId, component.Id, StringComparison.OrdinalIgnoreCase)) ||
            _machineBuilderDocument.Gui.Bindings.Any(binding => string.Equals(binding.SourceId, component.Id, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                this,
                "This component is already referenced by another component, GUI binding or animation. Remove those references first so the Machine Project cannot become orphaned.",
                "Component is still referenced",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                this,
                $"Delete visual component '{component.DisplayName}' ({component.Id})?",
                "Delete Machine Builder component",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _machineBuilderDocument.Components.Remove(component);
        _machineBuilderVisualSelectedComponentId = null;
        MarkMachineBuilderVisualDirty();
        RefreshMachineBuilderVisualComponentList();
        RefreshMachineBuilderVisualPreview();
    }

    private void ResetMachineBuilderVisualComponentDefaults()
    {
        if (_machineBuilderDocument is null || GetSelectedMachineBuilderVisualComponent() is not { } component ||
            _machineBuilderLibraryLoad?.Catalog.Find(component.LibraryItemId, component.LibraryItemVersion) is not { } asset)
            return;
        var defaults = ForeverStudioVisualAssemblyService.CreateComponent(asset, _machineBuilderDocument.Components);
        component.Properties = defaults.Properties;
        MarkMachineBuilderVisualDirty();
        PopulateMachineBuilderVisualInspector();
        RefreshMachineBuilderVisualPreview();
        SetMachineBuilderVisualStatus($"Reset {component.DisplayName} to {asset.DisplayName} defaults.", "#D8B26A");
    }

    private void SetMachineBuilderVisualRotationAxis(string axis)
    {
        if (_machineBuilderVisualUiUpdating || axis is not ("x" or "y" or "z"))
            return;
        _machineBuilderVisualRotationAxis = axis;
        UpdateMachineBuilderVisualRotationAxisButtons();
        CommitMachineBuilderVisualInspector();
    }

    private void UpdateMachineBuilderVisualRotationAxisButtons()
    {
        foreach (var (axis, button) in _machineBuilderVisualRotationAxisButtons)
            StyleMachineBuilderVisualToggle(button, axis.Equals(_machineBuilderVisualRotationAxis, StringComparison.OrdinalIgnoreCase));
    }

    private void SetMachineBuilderVisualViewportMode(MinecraftViewportEditMode mode)
    {
        if (_machineBuilderVisualViewport is not null)
            _machineBuilderVisualViewport.EditMode = mode;
        foreach (var (candidate, button) in _machineBuilderVisualModeButtons)
            StyleMachineBuilderVisualToggle(button, candidate == mode);
    }

    private void SetMachineBuilderVisualViewportAxis(string axis)
    {
        if (_machineBuilderVisualViewport is not null)
            _machineBuilderVisualViewport.EditAxis = axis;
        foreach (var (candidate, button) in _machineBuilderVisualAxisButtons)
            StyleMachineBuilderVisualToggle(button, candidate.Equals(axis, StringComparison.OrdinalIgnoreCase));
    }

    private static void StyleMachineBuilderVisualToggle(Button button, bool active)
    {
        if (button.Content is not Border surface)
            return;
        surface.Background = Brush(active ? "#1A4144" : "#202D34");
        surface.BorderBrush = Brush(active ? "#65C8C8" : "#41555E");
        if (surface.Child is TextBlock text)
            text.Foreground = Brush(active ? "#F0FFFF" : "#E3ECEF");
    }

    private void MachineBuilderVisualViewportElementSelected(int elementIndex)
    {
        if (elementIndex < 0 || elementIndex >= _machineBuilderVisualElementComponentIds.Count)
            return;
        var componentId = _machineBuilderVisualElementComponentIds[elementIndex];
        _machineBuilderVisualSelectedComponentId = componentId;
        if (_machineBuilderVisualComponentList?.ItemsSource is IEnumerable<MachineBuilderVisualComponentItem> items)
        {
            _machineBuilderVisualUiUpdating = true;
            try
            {
                _machineBuilderVisualComponentList.SelectedItem = items.FirstOrDefault(item =>
                    string.Equals(item.Component.Id, componentId, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _machineBuilderVisualUiUpdating = false;
            }
        }
        PopulateMachineBuilderVisualInspector();
    }

    private void MachineBuilderVisualViewportTransformDelta(
        int elementIndex,
        string axis,
        double delta,
        MinecraftViewportEditMode mode)
    {
        if (_machineBuilderDocument is null || elementIndex < 0 || elementIndex >= _machineBuilderVisualElementComponentIds.Count)
            return;
        var componentId = _machineBuilderVisualElementComponentIds[elementIndex];
        var component = _machineBuilderDocument.Components.FirstOrDefault(item =>
            string.Equals(item.Id, componentId, StringComparison.OrdinalIgnoreCase));
        if (component is null)
            return;
        var axisIndex = axis.ToLowerInvariant() switch { "x" => 0, "y" => 1, "z" => 2, _ => -1 };
        if (axisIndex < 0)
            return;

        var position = ForeverStudioVisualAssemblyService.ReadVector(component, "position", [0, 0, 0]);
        var size = ForeverStudioVisualAssemblyService.ReadVector(component, "size", [16, 16, 16]);
        var pivot = ForeverStudioVisualAssemblyService.ReadVector(component, "pivot", [8, 8, 8]);
        if (mode == MinecraftViewportEditMode.Move)
        {
            position[axisIndex] = Math.Clamp(position[axisIndex] + delta, -64, 64);
            pivot[axisIndex] = Math.Clamp(pivot[axisIndex] + delta, -64, 64);
            ForeverStudioVisualAssemblyService.WriteVector(component, "position", position);
            ForeverStudioVisualAssemblyService.WriteVector(component, "pivot", pivot);
        }
        else if (mode == MinecraftViewportEditMode.Scale)
        {
            size[axisIndex] = Math.Clamp(size[axisIndex] + delta, 0.05, 64);
            pivot[axisIndex] = position[axisIndex] + size[axisIndex] / 2;
            ForeverStudioVisualAssemblyService.WriteVector(component, "size", size);
            ForeverStudioVisualAssemblyService.WriteVector(component, "pivot", pivot);
        }
        else
        {
            return;
        }

        _machineBuilderVisualSelectedComponentId = component.Id;
        MarkMachineBuilderVisualDirty();
        PopulateMachineBuilderVisualInspector();
        RefreshMachineBuilderVisualPreview();
    }

    private void RefreshMachineBuilderVisualPreview()
    {
        if (_machineBuilderVisualViewport is null || _machineBuilderDocument is null ||
            _machineBuilderLibraryLoad is null || string.IsNullOrWhiteSpace(_repoRoot))
        {
            _machineBuilderVisualViewport?.ClearModel();
            return;
        }

        var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(
            _machineBuilderDocument,
            _machineBuilderLibraryLoad.Catalog,
            out _machineBuilderVisualElementComponentIds);
        _machineBuilderVisualViewport.SetModel(_repoRoot, model, 0, 0, forceRebuild: true);
        SelectMachineBuilderVisualPreviewElement();
        var componentCount = _machineBuilderDocument.Components.Count(component =>
            !component.Id.Equals("root", StringComparison.OrdinalIgnoreCase));
        SetMachineBuilderVisualStatus(
            $"{componentCount} component(s) · {model.Elements.Count} preview cuboid(s) · {_machineBuilderVisualViewport.MissingTextureCount} missing texture face(s)",
            _machineBuilderVisualViewport.MissingTextureCount == 0 ? "#74C99A" : "#D8B26A");
    }

    private void SelectMachineBuilderVisualPreviewElement()
    {
        if (_machineBuilderVisualViewport is null || string.IsNullOrWhiteSpace(_machineBuilderVisualSelectedComponentId))
            return;
        var index = _machineBuilderVisualElementComponentIds.FindIndex(id =>
            string.Equals(id, _machineBuilderVisualSelectedComponentId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _machineBuilderVisualViewport.SelectElement(index);
    }

    private void MarkMachineBuilderVisualDirty()
    {
        _machineBuilderDirty = true;
        RefreshMachineBuilderValidation();
    }

    private void SetMachineBuilderVisualStatus(string message, string color)
    {
        if (_machineBuilderVisualStatus is null)
            return;
        _machineBuilderVisualStatus.Text = message;
        _machineBuilderVisualStatus.Foreground = Brush(color);
    }
}

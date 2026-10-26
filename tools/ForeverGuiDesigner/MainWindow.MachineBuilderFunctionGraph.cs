using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder Phase 4: typed visual function graph authoring. The UI edits the canonical
/// Behavior.Nodes / Behavior.Connections contract directly; library function ports remain the
/// authority for connection compatibility and later production generation.
/// </summary>
public partial class MainWindow
{
    private const double MachineBuilderFunctionNodeWidth = 224;
    private const double MachineBuilderFunctionCanvasWidth = 1800;
    private const double MachineBuilderFunctionCanvasHeight = 1000;

    private bool _machineBuilderFunctionPhase4Initialized;
    private Border? _machineBuilderFunctionSurface;
    private Border? _machineBuilderFunctionStep;
    private TextBox? _machineBuilderFunctionSearch;
    private StackPanel? _machineBuilderFunctionPaletteItems;
    private Canvas? _machineBuilderFunctionCanvas;
    private ScrollViewer? _machineBuilderFunctionCanvasScroll;
    private TextBlock? _machineBuilderFunctionStatus;
    private StackPanel? _machineBuilderFunctionInspectorBody;
    private Button? _machineBuilderFunctionCancelLinkButton;
    private readonly Dictionary<string, Button> _machineBuilderFunctionFilterButtons = new(StringComparer.OrdinalIgnoreCase);
    private string _machineBuilderFunctionCategoryFilter = "all";
    private string? _machineBuilderFunctionSelectedNodeId;
    private (string NodeId, string PortId)? _machineBuilderFunctionPendingOutput;
    private string? _machineBuilderFunctionDraggingNodeId;
    private Point _machineBuilderFunctionDragStart;
    private double _machineBuilderFunctionDragNodeX;
    private double _machineBuilderFunctionDragNodeY;

    public void EnableForeverMachineBuilderFunctionGraphPhase4()
    {
        if (_machineBuilderFunctionPhase4Initialized || _studioMachineBuilderWorkspace is not DockPanel root)
            return;
        _machineBuilderFunctionPhase4Initialized = true;

        var page = root.Children.OfType<Grid>()
            .FirstOrDefault(grid => grid.ColumnDefinitions.Count >= 5);
        if (page is null)
            return;

        _machineBuilderFunctionSurface = BuildMachineBuilderFunctionSurface();
        Grid.SetColumn(_machineBuilderFunctionSurface, 2);
        Grid.SetColumnSpan(_machineBuilderFunctionSurface, 3);
        Panel.SetZIndex(_machineBuilderFunctionSurface, 40);
        _machineBuilderFunctionSurface.Visibility = Visibility.Collapsed;
        page.Children.Add(_machineBuilderFunctionSurface);

        InstallMachineBuilderFunctionWorkflowNavigation(page);
        InstallMachineBuilderFunctionContinueButton();
        UpdateMachineBuilderPhase4Labels();
        RefreshMachineBuilderFunctionPalette();
    }

    private void InstallMachineBuilderFunctionWorkflowNavigation(Grid page)
    {
        var workflowCard = page.Children.OfType<Border>()
            .FirstOrDefault(border => Grid.GetColumn(border) == 0 && border.Child is StackPanel panel &&
                                      panel.Children.OfType<TextBlock>().Any(text => text.Text == "GUIDED WORKFLOW"));
        if (workflowCard?.Child is not StackPanel workflow)
            return;

        _machineBuilderFunctionStep = FindMachineBuilderWorkflowStep(workflow, "Function");
        if (_machineBuilderFunctionStep is not null)
        {
            _machineBuilderFunctionStep.Cursor = Cursors.Hand;
            _machineBuilderFunctionStep.Opacity = 1;
            if (_machineBuilderFunctionStep.Child is Grid grid)
            {
                var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
                var description = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                if (description is not null)
                    description.Text = "Typed nodes and machine behavior";
            }
            _machineBuilderFunctionStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderFunctionPhase4();
            StyleMachineBuilderPhase3Step(_machineBuilderFunctionStep, active: false, available: true);
        }

        // Phase 3 installed these handlers first. These additional handlers only make sure the
        // Phase-4 overlay is hidden when navigating back to an earlier workflow step.
        if (_machineBuilderIdentityStep is not null)
            _machineBuilderIdentityStep.MouseLeftButtonUp += (_, _) => HideMachineBuilderFunctionSurface();
        if (_machineBuilderVisualStep is not null)
            _machineBuilderVisualStep.MouseLeftButtonUp += (_, _) => HideMachineBuilderFunctionSurface();
    }

    private void InstallMachineBuilderFunctionContinueButton()
    {
        if (_machineBuilderVisualSurface is null)
            return;

        var actionPanel = FindStudioVisualChildren<StackPanel>(_machineBuilderVisualSurface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                                     panel.Children.OfType<Button>().Any(button =>
                                         FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "SAVE PROJECT")));
        if (actionPanel is null || actionPanel.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "machine_builder_function_continue", StringComparison.Ordinal)))
            return;

        var continueButton = CreateStudioChromeButton("FUNCTION  →", 108, false);
        continueButton.Tag = "machine_builder_function_continue";
        continueButton.Height = 36;
        continueButton.Margin = new Thickness(9, 0, 0, 0);
        continueButton.Click += (_, _) => ShowMachineBuilderFunctionPhase4();

        var save = actionPanel.Children.OfType<Button>().FirstOrDefault(button =>
            FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "SAVE PROJECT"));
        var saveIndex = save is null ? actionPanel.Children.Count : actionPanel.Children.IndexOf(save);
        actionPanel.Children.Insert(Math.Max(0, saveIndex), continueButton);
    }

    private void UpdateMachineBuilderPhase4Labels()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;

        foreach (var text in FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace))
        {
            if (text.Text.StartsWith("PHASE 3 ·", StringComparison.OrdinalIgnoreCase))
                text.Text = "PHASE 4 · FUNCTION GRAPH";
            else if (text.Text.StartsWith("Phase 3 unlocks visual assembly", StringComparison.OrdinalIgnoreCase))
            {
                text.Text =
                    "Phase 4 unlocks typed machine behavior. Compose reusable functions as nodes and connect compatible ports; Recipes & GUI remain locked until Phase 5.";
            }
        }
    }

    private Border BuildMachineBuilderFunctionSurface()
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
            Text = "FUNCTION · PHASE 4",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Compose typed machine behavior",
            Foreground = Brush("#EFF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Functions are versioned library contracts. Nodes and connections are saved directly into the Machine Project and validated against typed ports.",
            Foreground = Brush("#899CA4"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        header.Children.Add(heading);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var visual = CreateStudioChromeButton("← VISUAL", 94, false);
        visual.Height = 36;
        visual.Click += (_, _) =>
        {
            HideMachineBuilderFunctionSurface();
            ShowMachineBuilderVisualDesignPhase3();
        };
        actions.Children.Add(visual);
        var save = CreateStudioChromeButton("SAVE PROJECT", 126, true);
        save.Height = 36;
        save.Margin = new Thickness(9, 0, 0, 0);
        save.Click += (_, _) => SaveMachineBuilderProject();
        actions.Children.Add(save);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        shell.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });

        var palette = BuildMachineBuilderFunctionPalette();
        Grid.SetColumn(palette, 0);
        body.Children.Add(palette);

        var graph = BuildMachineBuilderFunctionGraph();
        Grid.SetColumn(graph, 2);
        body.Children.Add(graph);

        var inspector = BuildMachineBuilderFunctionInspector();
        Grid.SetColumn(inspector, 4);
        body.Children.Add(inspector);

        Grid.SetRow(body, 2);
        shell.Children.Add(body);
        surface.Child = shell;
        return surface;
    }

    private Border BuildMachineBuilderFunctionPalette()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());

        var title = new Grid();
        title.ColumnDefinitions.Add(new ColumnDefinition());
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        title.Children.Add(new TextBlock
        {
            Text = "FUNCTION LIBRARY",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var hint = new TextBlock
        {
            Text = "ADD NODE",
            Foreground = Brush("#60757E"),
            FontSize = 8.5,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(hint, 1);
        title.Children.Add(hint);
        grid.Children.Add(title);

        _machineBuilderFunctionSearch = MachineBuilderTextBox();
        _machineBuilderFunctionSearch.Height = 34;
        _machineBuilderFunctionSearch.Margin = new Thickness(0, 9, 0, 0);
        _machineBuilderFunctionSearch.ToolTip = "Search functions by name, category, description or tag";
        _machineBuilderFunctionSearch.TextChanged += (_, _) => RefreshMachineBuilderFunctionPalette();
        Grid.SetRow(_machineBuilderFunctionSearch, 1);
        grid.Children.Add(_machineBuilderFunctionSearch);

        var filters = new WrapPanel { Margin = new Thickness(0, 9, 0, 4) };
        AddMachineBuilderFunctionFilter(filters, "all", "ALL", 48);
        AddMachineBuilderFunctionFilter(filters, "events", "EVENTS", 67);
        AddMachineBuilderFunctionFilter(filters, "inventory", "ITEMS", 63);
        AddMachineBuilderFunctionFilter(filters, "energy", "ENERGY", 69);
        AddMachineBuilderFunctionFilter(filters, "fluids", "FLUID", 62);
        AddMachineBuilderFunctionFilter(filters, "processing", "PROCESS", 77);
        AddMachineBuilderFunctionFilter(filters, "logic", "LOGIC", 59);
        AddMachineBuilderFunctionFilter(filters, "data", "DATA", 54);
        AddMachineBuilderFunctionFilter(filters, "state", "STATE", 57);
        AddMachineBuilderFunctionFilter(filters, "composites", "COMPOSITE", 91);
        Grid.SetRow(filters, 2);
        grid.Children.Add(filters);

        _machineBuilderFunctionPaletteItems = new StackPanel { Margin = new Thickness(0, 3, 0, 3) };
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brush("#091014"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5)
        };
        scroll.Content = _machineBuilderFunctionPaletteItems;
        Grid.SetRow(scroll, 3);
        grid.Children.Add(scroll);

        card.Child = grid;
        return card;
    }

    private void AddMachineBuilderFunctionFilter(Panel parent, string key, string label, double width)
    {
        var button = CreateStudioChromeButton(label, width, false);
        button.Height = 28;
        button.Margin = new Thickness(0, 0, 6, 6);
        button.Tag = key;
        button.Click += (_, _) =>
        {
            _machineBuilderFunctionCategoryFilter = key;
            UpdateMachineBuilderFunctionFilterButtons();
            RefreshMachineBuilderFunctionPalette();
        };
        _machineBuilderFunctionFilterButtons[key] = button;
        parent.Children.Add(button);
    }

    private void UpdateMachineBuilderFunctionFilterButtons()
    {
        foreach (var (key, button) in _machineBuilderFunctionFilterButtons)
        {
            if (button.Content is not Border surface)
                continue;
            var active = key.Equals(_machineBuilderFunctionCategoryFilter, StringComparison.OrdinalIgnoreCase);
            surface.Background = Brush(active ? "#1B3B3E" : "#202D34");
            surface.BorderBrush = Brush(active ? "#65C8C8" : "#41555E");
            if (surface.Child is TextBlock text)
                text.Foreground = Brush(active ? "#ECFFFF" : "#DDE7EA");
        }
    }

    private Border BuildMachineBuilderFunctionGraph()
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
        toolbar.Children.Add(new TextBlock
        {
            Text = "BEHAVIOR GRAPH",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });

        var graphActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var autoLayout = CreateStudioChromeButton("AUTO LAYOUT", 102, false);
        autoLayout.Height = 31;
        autoLayout.Click += (_, _) => AutoLayoutMachineBuilderFunctionGraph();
        graphActions.Children.Add(autoLayout);
        _machineBuilderFunctionCancelLinkButton = CreateStudioChromeButton("CANCEL LINK", 104, false);
        _machineBuilderFunctionCancelLinkButton.Height = 31;
        _machineBuilderFunctionCancelLinkButton.Margin = new Thickness(7, 0, 0, 0);
        _machineBuilderFunctionCancelLinkButton.IsEnabled = false;
        _machineBuilderFunctionCancelLinkButton.Click += (_, _) =>
        {
            _machineBuilderFunctionPendingOutput = null;
            SetMachineBuilderFunctionStatus("Connection mode cancelled.", "#82969E");
            RefreshMachineBuilderFunctionGraph();
        };
        graphActions.Children.Add(_machineBuilderFunctionCancelLinkButton);
        Grid.SetColumn(graphActions, 1);
        toolbar.Children.Add(graphActions);
        grid.Children.Add(toolbar);

        _machineBuilderFunctionCanvas = new Canvas
        {
            Width = MachineBuilderFunctionCanvasWidth,
            Height = MachineBuilderFunctionCanvasHeight,
            Background = CreateMachineBuilderFunctionCanvasBrush()
        };
        _machineBuilderFunctionCanvas.MouseLeftButtonDown += (_, e) =>
        {
            if (!ReferenceEquals(e.OriginalSource, _machineBuilderFunctionCanvas))
                return;
            _machineBuilderFunctionSelectedNodeId = null;
            RefreshMachineBuilderFunctionGraph();
            RefreshMachineBuilderFunctionInspector();
        };

        _machineBuilderFunctionCanvasScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            PanningMode = PanningMode.Both,
            Background = Brush("#080D10"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            Content = _machineBuilderFunctionCanvas
        };
        Grid.SetRow(_machineBuilderFunctionCanvasScroll, 2);
        grid.Children.Add(_machineBuilderFunctionCanvasScroll);

        _machineBuilderFunctionStatus = new TextBlock
        {
            Text = "Add or select a function node to begin.",
            Foreground = Brush("#82969E"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, 8, 2, 0)
        };
        Grid.SetRow(_machineBuilderFunctionStatus, 3);
        grid.Children.Add(_machineBuilderFunctionStatus);

        card.Child = grid;
        return card;
    }

    private static Brush CreateMachineBuilderFunctionCanvasBrush()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brush("#091014"), null, new RectangleGeometry(new Rect(0, 0, 24, 24))));
        var pen = new Pen(Brush("#18272E"), 0.7);
        pen.Freeze();
        var lines = new GeometryGroup();
        lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(24, 0)));
        lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, 24)));
        group.Children.Add(new GeometryDrawing(null, pen, lines));
        group.Freeze();
        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 24, 24),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None
        };
        brush.Freeze();
        return brush;
    }

    private Border BuildMachineBuilderFunctionInspector()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _machineBuilderFunctionInspectorBody = new StackPanel { Margin = new Thickness(16) };
        scroll.Content = _machineBuilderFunctionInspectorBody;
        card.Child = scroll;
        RefreshMachineBuilderFunctionInspector();
        return card;
    }

    private void ShowMachineBuilderFunctionPhase4()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        if (_machineBuilderIdentityScroll is not null)
            _machineBuilderIdentityScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderContextScroll is not null)
            _machineBuilderContextScroll.Visibility = Visibility.Collapsed;
        if (_machineBuilderVisualSurface is not null)
            _machineBuilderVisualSurface.Visibility = Visibility.Collapsed;
        if (_machineBuilderFunctionSurface is not null)
            _machineBuilderFunctionSurface.Visibility = Visibility.Visible;

        StyleMachineBuilderPhase3Step(_machineBuilderIdentityStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderVisualStep, active: false, available: true);
        StyleMachineBuilderPhase3Step(_machineBuilderFunctionStep, active: true, available: true);

        RefreshMachineBuilderFunctionPalette();
        if (_machineBuilderFunctionSelectedNodeId is null || !_machineBuilderDocument.Behavior.Nodes.Any(node =>
                node.Id.Equals(_machineBuilderFunctionSelectedNodeId, StringComparison.OrdinalIgnoreCase)))
        {
            _machineBuilderFunctionSelectedNodeId = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault()?.Id;
        }
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
    }

    private void HideMachineBuilderFunctionSurface()
    {
        if (_machineBuilderFunctionSurface is not null)
            _machineBuilderFunctionSurface.Visibility = Visibility.Collapsed;
        StyleMachineBuilderPhase3Step(_machineBuilderFunctionStep, active: false, available: true);
    }

    private void RefreshMachineBuilderFunctionPalette()
    {
        if (_machineBuilderFunctionPaletteItems is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        _machineBuilderFunctionPaletteItems.Children.Clear();

        var query = _machineBuilderFunctionSearch?.Text ?? string.Empty;
        var functions = _machineBuilderLibraryLoad.Catalog
            .Search(query, ForeverStudioLibraryItemKind.Function)
            .Where(MachineBuilderFunctionMatchesScope)
            .OrderBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groups = functions.GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var group in groups)
        {
            var header = new Grid { Margin = new Thickness(4, 7, 4, 5) };
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                Foreground = Brush("#6ECAC9"),
                FontSize = 8.8,
                FontWeight = FontWeights.Bold
            });
            var count = new TextBlock
            {
                Text = group.Count().ToString(),
                Foreground = Brush("#60747D"),
                FontSize = 8.8
            };
            Grid.SetColumn(count, 1);
            header.Children.Add(count);
            _machineBuilderFunctionPaletteItems.Children.Add(header);

            foreach (var item in group)
                _machineBuilderFunctionPaletteItems.Children.Add(BuildMachineBuilderFunctionPaletteItem(item));
        }

        if (functions.Count == 0)
        {
            _machineBuilderFunctionPaletteItems.Children.Add(new TextBlock
            {
                Text = "No functions match the current search/filter.",
                Foreground = Brush("#748891"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 12, 8, 8)
            });
        }
        UpdateMachineBuilderFunctionFilterButtons();
    }

    private bool MachineBuilderFunctionMatchesScope(ForeverStudioLibraryItem item)
    {
        if (_machineBuilderFunctionCategoryFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;
        var root = item.Category.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? item.Category;
        return root.Equals(_machineBuilderFunctionCategoryFilter, StringComparison.OrdinalIgnoreCase) ||
               item.Category.Contains(_machineBuilderFunctionCategoryFilter, StringComparison.OrdinalIgnoreCase) ||
               item.Tags.Any(tag => tag.Contains(_machineBuilderFunctionCategoryFilter, StringComparison.OrdinalIgnoreCase));
    }

    private Border BuildMachineBuilderFunctionPaletteItem(ForeverStudioLibraryItem item)
    {
        var border = new Border
        {
            Background = Brush("#10191E"),
            BorderBrush = Brush("#263A42"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(9, 7, 7, 7),
            Margin = new Thickness(2, 0, 2, 5),
            ToolTip = item.Description
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = item.DisplayName,
            Foreground = Brush("#E1EAEC"),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        copy.Children.Add(new TextBlock
        {
            Text = DescribeMachineBuilderFunctionPorts(item),
            Foreground = Brush("#71858E"),
            FontSize = 8.8,
            Margin = new Thickness(0, 3, 5, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        grid.Children.Add(copy);
        var add = CreateStudioChromeButton("+", 30, false);
        add.Height = 30;
        add.VerticalAlignment = VerticalAlignment.Center;
        add.ToolTip = $"Add {item.DisplayName} node";
        add.Click += (_, _) => AddMachineBuilderFunctionNode(item);
        Grid.SetColumn(add, 1);
        grid.Children.Add(add);
        border.Child = grid;
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount != 2)
                return;
            AddMachineBuilderFunctionNode(item);
            e.Handled = true;
        };
        return border;
    }

    private static string DescribeMachineBuilderFunctionPorts(ForeverStudioLibraryItem item)
    {
        var inputs = item.Ports.Count(port => port.Direction == ForeverStudioLibraryPortDirection.Input);
        var outputs = item.Ports.Count(port => port.Direction == ForeverStudioLibraryPortDirection.Output);
        var types = item.Ports.Select(port => port.DataType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        return $"{inputs} in · {outputs} out" + (types.Count == 0 ? string.Empty : $" · {string.Join(" / ", types)}");
    }

    private void AddMachineBuilderFunctionNode(ForeverStudioLibraryItem function)
    {
        if (_machineBuilderDocument is null || function.Kind != ForeverStudioLibraryItemKind.Function)
            return;

        var baseId = function.Id.StartsWith("function.", StringComparison.OrdinalIgnoreCase)
            ? function.Id[9..]
            : function.Id;
        baseId = SanitizeMachineBuilderGraphId(baseId);
        if (string.IsNullOrWhiteSpace(baseId))
            baseId = "function_node";
        var used = _machineBuilderDocument.Behavior.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = $"{baseId}_{suffix}";

        var index = _machineBuilderDocument.Behavior.Nodes.Count;
        var node = new ForeverStudioMachineBehaviorNode
        {
            Id = id,
            LibraryItemId = function.Id,
            LibraryItemVersion = function.Version,
            X = 70 + (index % 4) * 270,
            Y = 70 + (index / 4) * 170
        };
        _machineBuilderDocument.Behavior.Nodes.Add(node);
        _machineBuilderFunctionSelectedNodeId = node.Id;
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
        SetMachineBuilderFunctionStatus($"Added {function.DisplayName} as '{node.Id}'.", "#74C99A");
    }

    private void RefreshMachineBuilderFunctionGraph()
    {
        if (_machineBuilderFunctionCanvas is null)
            return;
        _machineBuilderFunctionCanvas.Children.Clear();
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;

        foreach (var connection in _machineBuilderDocument.Behavior.Connections)
            DrawMachineBuilderFunctionConnection(connection);
        foreach (var node in _machineBuilderDocument.Behavior.Nodes)
            DrawMachineBuilderFunctionNode(node);

        if (_machineBuilderFunctionCancelLinkButton is not null)
            _machineBuilderFunctionCancelLinkButton.IsEnabled = _machineBuilderFunctionPendingOutput is not null;

        var issues = ForeverStudioMachineProjectService.Validate(_machineBuilderDocument, _machineBuilderLibraryLoad.Catalog);
        var graphIssues = issues.Count(issue =>
            issue.Message.Contains("Behavior", StringComparison.OrdinalIgnoreCase) ||
            issue.Message.Contains("Connection", StringComparison.OrdinalIgnoreCase) ||
            issue.Message.Contains("input", StringComparison.OrdinalIgnoreCase));
        if (_machineBuilderFunctionPendingOutput is null)
        {
            SetMachineBuilderFunctionStatus(
                $"{_machineBuilderDocument.Behavior.Nodes.Count} node(s) · {_machineBuilderDocument.Behavior.Connections.Count} connection(s) · {graphIssues} graph issue(s)",
                graphIssues == 0 ? "#74C99A" : "#D8B26A");
        }
    }

    private void DrawMachineBuilderFunctionConnection(ForeverStudioMachineBehaviorConnection connection)
    {
        if (_machineBuilderFunctionCanvas is null || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        var fromNode = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
            node.Id.Equals(connection.FromNodeId, StringComparison.OrdinalIgnoreCase));
        var toNode = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
            node.Id.Equals(connection.ToNodeId, StringComparison.OrdinalIgnoreCase));
        if (fromNode is null || toNode is null)
            return;
        var fromItem = _machineBuilderLibraryLoad.Catalog.Find(fromNode.LibraryItemId, fromNode.LibraryItemVersion);
        var toItem = _machineBuilderLibraryLoad.Catalog.Find(toNode.LibraryItemId, toNode.LibraryItemVersion);
        var outputs = fromItem?.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Output).ToList() ?? [];
        var inputs = toItem?.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Input).ToList() ?? [];
        var outputIndex = outputs.FindIndex(port => port.Id.Equals(connection.FromPortId, StringComparison.OrdinalIgnoreCase));
        var inputIndex = inputs.FindIndex(port => port.Id.Equals(connection.ToPortId, StringComparison.OrdinalIgnoreCase));
        if (outputIndex < 0 || inputIndex < 0)
            return;

        var start = new Point(fromNode.X + MachineBuilderFunctionNodeWidth, fromNode.Y + 83 + outputIndex * 27);
        var end = new Point(toNode.X, toNode.Y + 83 + inputIndex * 27);
        var bend = Math.Max(55, Math.Abs(end.X - start.X) * 0.45);
        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        figure.Segments.Add(new BezierSegment(
            new Point(start.X + bend, start.Y),
            new Point(end.X - bend, end.Y),
            end,
            true));
        var geometry = new PathGeometry([figure]);
        var selected = connection.FromNodeId.Equals(_machineBuilderFunctionSelectedNodeId, StringComparison.OrdinalIgnoreCase) ||
                       connection.ToNodeId.Equals(_machineBuilderFunctionSelectedNodeId, StringComparison.OrdinalIgnoreCase);
        var outputPort = outputs[outputIndex];
        var path = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = Brush(selected ? FunctionPortColor(outputPort.DataType) : "#3F626B"),
            StrokeThickness = selected ? 2.4 : 1.7,
            Opacity = selected ? 0.95 : 0.72,
            IsHitTestVisible = false
        };
        _machineBuilderFunctionCanvas.Children.Add(path);
    }

    private void DrawMachineBuilderFunctionNode(ForeverStudioMachineBehaviorNode node)
    {
        if (_machineBuilderFunctionCanvas is null || _machineBuilderLibraryLoad is null)
            return;
        var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
        if (item is null)
            return;
        var selected = node.Id.Equals(_machineBuilderFunctionSelectedNodeId, StringComparison.OrdinalIgnoreCase);
        var card = new Border
        {
            Width = MachineBuilderFunctionNodeWidth,
            Background = Brush("#111A1F"),
            BorderBrush = Brush(selected ? "#69CCCB" : "#344B54"),
            BorderThickness = new Thickness(selected ? 1.5 : 1),
            CornerRadius = new CornerRadius(7),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = selected ? 14 : 9,
                Opacity = selected ? 0.30 : 0.18,
                ShadowDepth = 2
            }
        };

        var panel = new StackPanel();
        var header = new Border
        {
            Background = Brush(selected ? "#18383C" : "#18252B"),
            BorderBrush = Brush(selected ? "#4A9B9D" : "#2E424A"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(6, 6, 0, 0),
            Padding = new Thickness(10, 8, 9, 8),
            Cursor = Cursors.SizeAll
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = ReadMachineBuilderFunctionNodeLabel(node, item),
            Foreground = Brush("#EDF4F5"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        copy.Children.Add(new TextBlock
        {
            Text = node.Id,
            Foreground = Brush("#71868E"),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 8.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        headerGrid.Children.Add(copy);
        var badge = new Border
        {
            Background = Brush("#22343B"),
            BorderBrush = Brush("#3C5962"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = ShortMachineBuilderFunctionCategory(item.Category),
                Foreground = Brush("#8FC9CC"),
                FontSize = 7.5,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(badge, 1);
        headerGrid.Children.Add(badge);
        header.Child = headerGrid;
        header.MouseLeftButtonDown += (_, e) => BeginMachineBuilderFunctionNodeDrag(node, card, header, e);
        header.MouseMove += (_, e) => ContinueMachineBuilderFunctionNodeDrag(node, card, header, e);
        header.MouseLeftButtonUp += (_, e) => EndMachineBuilderFunctionNodeDrag(node, card, header, e);
        panel.Children.Add(header);

        var portsGrid = new Grid { Margin = new Thickness(7, 8, 7, 8) };
        portsGrid.ColumnDefinitions.Add(new ColumnDefinition());
        portsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        portsGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var inputs = new StackPanel();
        var outputs = new StackPanel();
        foreach (var port in item.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Input))
            inputs.Children.Add(BuildMachineBuilderFunctionPortButton(node, port));
        foreach (var port in item.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Output))
            outputs.Children.Add(BuildMachineBuilderFunctionPortButton(node, port));
        if (inputs.Children.Count == 0)
            inputs.Children.Add(MachineBuilderFunctionEmptyPortText("NO INPUTS", HorizontalAlignment.Left));
        if (outputs.Children.Count == 0)
            outputs.Children.Add(MachineBuilderFunctionEmptyPortText("NO OUTPUTS", HorizontalAlignment.Right));
        portsGrid.Children.Add(inputs);
        Grid.SetColumn(outputs, 2);
        portsGrid.Children.Add(outputs);
        panel.Children.Add(portsGrid);
        card.Child = panel;

        Canvas.SetLeft(card, Math.Clamp(node.X, 0, MachineBuilderFunctionCanvasWidth - MachineBuilderFunctionNodeWidth - 8));
        Canvas.SetTop(card, Math.Clamp(node.Y, 0, MachineBuilderFunctionCanvasHeight - 120));
        _machineBuilderFunctionCanvas.Children.Add(card);
    }

    private static TextBlock MachineBuilderFunctionEmptyPortText(string text, HorizontalAlignment alignment)
        => new()
        {
            Text = text,
            Foreground = Brush("#53666E"),
            FontSize = 7.8,
            HorizontalAlignment = alignment,
            Margin = new Thickness(3, 5, 3, 5)
        };

    private Button BuildMachineBuilderFunctionPortButton(
        ForeverStudioMachineBehaviorNode node,
        ForeverStudioLibraryPort port)
    {
        var output = port.Direction == ForeverStudioLibraryPortDirection.Output;
        var pending = output && _machineBuilderFunctionPendingOutput is { } source &&
                      source.NodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) &&
                      source.PortId.Equals(port.Id, StringComparison.OrdinalIgnoreCase);
        var button = new Button
        {
            Height = 24,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 1, 0, 2),
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = output ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            FocusVisualStyle = null,
            ToolTip = $"{port.DisplayName} · {port.DataType}" +
                      (port.Required ? " · required" : string.Empty) +
                      (port.Multiple ? " · multiple" : string.Empty)
        };
        ApplyStudioContentOnlyButtonTemplate(button);

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = output ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };
        var dot = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = Brush(FunctionPortColor(port.DataType)),
            BorderBrush = Brush(pending ? "#E9FFFF" : "#17252B"),
            BorderThickness = new Thickness(pending ? 1.5 : 1),
            VerticalAlignment = VerticalAlignment.Center
        };
        var label = new TextBlock
        {
            Text = port.DisplayName,
            Foreground = Brush(pending ? "#E9FFFF" : "#B8C8CD"),
            FontSize = 8.8,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 79
        };
        if (output)
        {
            stack.Children.Add(label);
            dot.Margin = new Thickness(5, 0, 0, 0);
            stack.Children.Add(dot);
        }
        else
        {
            dot.Margin = new Thickness(0, 0, 5, 0);
            stack.Children.Add(dot);
            stack.Children.Add(label);
        }
        button.Content = stack;
        button.Click += (_, _) => HandleMachineBuilderFunctionPortClick(node, port);
        return button;
    }

    private void HandleMachineBuilderFunctionPortClick(
        ForeverStudioMachineBehaviorNode node,
        ForeverStudioLibraryPort port)
    {
        _machineBuilderFunctionSelectedNodeId = node.Id;
        if (port.Direction == ForeverStudioLibraryPortDirection.Output)
        {
            var same = _machineBuilderFunctionPendingOutput is { } pending &&
                       pending.NodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) &&
                       pending.PortId.Equals(port.Id, StringComparison.OrdinalIgnoreCase);
            _machineBuilderFunctionPendingOutput = same ? null : (node.Id, port.Id);
            SetMachineBuilderFunctionStatus(
                same
                    ? "Connection mode cancelled."
                    : $"Linking {node.Id}.{port.Id} ({port.DataType}) · select a compatible input port.",
                same ? "#82969E" : FunctionPortColor(port.DataType));
            RefreshMachineBuilderFunctionGraph();
            RefreshMachineBuilderFunctionInspector();
            return;
        }

        if (_machineBuilderFunctionPendingOutput is not { } source || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
        {
            SetMachineBuilderFunctionStatus("Select an output port first, then choose its destination input.", "#D8B26A");
            RefreshMachineBuilderFunctionGraph();
            RefreshMachineBuilderFunctionInspector();
            return;
        }

        var fromNode = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate =>
            candidate.Id.Equals(source.NodeId, StringComparison.OrdinalIgnoreCase));
        if (fromNode is null)
        {
            _machineBuilderFunctionPendingOutput = null;
            RefreshMachineBuilderFunctionGraph();
            return;
        }
        var fromItem = _machineBuilderLibraryLoad.Catalog.Find(fromNode.LibraryItemId, fromNode.LibraryItemVersion);
        var outputPort = fromItem?.Ports.FirstOrDefault(candidate =>
            candidate.Id.Equals(source.PortId, StringComparison.OrdinalIgnoreCase) &&
            candidate.Direction == ForeverStudioLibraryPortDirection.Output);
        if (outputPort is null || !_machineBuilderLibraryLoad.Catalog.ArePortsCompatible(outputPort, port))
        {
            SetMachineBuilderFunctionStatus(
                $"Cannot connect {outputPort?.DataType ?? "unknown"} to {port.DataType}. Typed ports must be compatible.",
                "#E18B8B");
            RefreshMachineBuilderFunctionGraph();
            RefreshMachineBuilderFunctionInspector();
            return;
        }

        if (_machineBuilderDocument.Behavior.Connections.Any(connection =>
                connection.FromNodeId.Equals(fromNode.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.FromPortId.Equals(outputPort.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.ToPortId.Equals(port.Id, StringComparison.OrdinalIgnoreCase)))
        {
            _machineBuilderFunctionPendingOutput = null;
            SetMachineBuilderFunctionStatus("That typed connection already exists.", "#D8B26A");
            RefreshMachineBuilderFunctionGraph();
            RefreshMachineBuilderFunctionInspector();
            return;
        }

        if (!port.Multiple)
        {
            _machineBuilderDocument.Behavior.Connections.RemoveAll(connection =>
                connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.ToPortId.Equals(port.Id, StringComparison.OrdinalIgnoreCase));
        }

        var baseId = SanitizeMachineBuilderGraphId($"link_{fromNode.Id}_{outputPort.Id}_{node.Id}_{port.Id}");
        var used = _machineBuilderDocument.Behavior.Connections.Select(connection => connection.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var connectionId = baseId;
        for (var suffix = 2; used.Contains(connectionId); suffix++)
            connectionId = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
        _machineBuilderDocument.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
        {
            Id = connectionId,
            FromNodeId = fromNode.Id,
            FromPortId = outputPort.Id,
            ToNodeId = node.Id,
            ToPortId = port.Id
        });
        _machineBuilderFunctionPendingOutput = null;
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
        SetMachineBuilderFunctionStatus(
            $"Connected {fromNode.Id}.{outputPort.Id} → {node.Id}.{port.Id} ({port.DataType}).",
            "#74C99A");
    }

    private void BeginMachineBuilderFunctionNodeDrag(
        ForeverStudioMachineBehaviorNode node,
        Border card,
        Border header,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;
        _machineBuilderFunctionSelectedNodeId = node.Id;
        _machineBuilderFunctionDraggingNodeId = node.Id;
        _machineBuilderFunctionDragStart = e.GetPosition(_machineBuilderFunctionCanvas);
        _machineBuilderFunctionDragNodeX = Canvas.GetLeft(card);
        _machineBuilderFunctionDragNodeY = Canvas.GetTop(card);
        header.CaptureMouse();
        RefreshMachineBuilderFunctionInspector();
        e.Handled = true;
    }

    private void ContinueMachineBuilderFunctionNodeDrag(
        ForeverStudioMachineBehaviorNode node,
        Border card,
        Border header,
        MouseEventArgs e)
    {
        if (!header.IsMouseCaptured ||
            !string.Equals(_machineBuilderFunctionDraggingNodeId, node.Id, StringComparison.OrdinalIgnoreCase))
            return;
        var current = e.GetPosition(_machineBuilderFunctionCanvas);
        var x = Math.Clamp(_machineBuilderFunctionDragNodeX + current.X - _machineBuilderFunctionDragStart.X,
            0, MachineBuilderFunctionCanvasWidth - MachineBuilderFunctionNodeWidth - 8);
        var y = Math.Clamp(_machineBuilderFunctionDragNodeY + current.Y - _machineBuilderFunctionDragStart.Y,
            0, MachineBuilderFunctionCanvasHeight - 120);
        Canvas.SetLeft(card, x);
        Canvas.SetTop(card, y);
        node.X = x;
        node.Y = y;
    }

    private void EndMachineBuilderFunctionNodeDrag(
        ForeverStudioMachineBehaviorNode node,
        Border card,
        Border header,
        MouseButtonEventArgs e)
    {
        if (!header.IsMouseCaptured)
            return;
        header.ReleaseMouseCapture();
        _machineBuilderFunctionDraggingNodeId = null;
        node.X = Math.Clamp(Canvas.GetLeft(card), 0, MachineBuilderFunctionCanvasWidth - MachineBuilderFunctionNodeWidth - 8);
        node.Y = Math.Clamp(Canvas.GetTop(card), 0, MachineBuilderFunctionCanvasHeight - 120);
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
        e.Handled = true;
    }

    private void RefreshMachineBuilderFunctionInspector()
    {
        if (_machineBuilderFunctionInspectorBody is null)
            return;
        var body = _machineBuilderFunctionInspectorBody;
        body.Children.Clear();
        body.Children.Add(new TextBlock
        {
            Text = "FUNCTION INSPECTOR",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });

        var node = GetSelectedMachineBuilderFunctionNode();
        var item = node is null || _machineBuilderLibraryLoad is null
            ? null
            : _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
        if (node is null || item is null)
        {
            body.Children.Add(new TextBlock
            {
                Text = "No function selected",
                Foreground = Brush("#EFF5F6"),
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 0)
            });
            body.Children.Add(new TextBlock
            {
                Text = "Select a graph node or add a function from the library. Click an output port and then a compatible input to create a typed connection.",
                Foreground = Brush("#7E929A"),
                FontSize = 9.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            return;
        }

        body.Children.Add(new TextBlock
        {
            Text = ReadMachineBuilderFunctionNodeLabel(node, item),
            Foreground = Brush("#EFF5F6"),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });
        body.Children.Add(new TextBlock
        {
            Text = $"{node.Id}\n{item.Id} v{item.Version} · {item.Category}",
            Foreground = Brush("#788D95"),
            FontSize = 9.3,
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 13)
        });
        body.Children.Add(new TextBlock
        {
            Text = item.Description,
            Foreground = Brush("#AFC0C5"),
            FontSize = 10,
            LineHeight = 15,
            TextWrapping = TextWrapping.Wrap
        });

        body.Children.Add(MachineBuilderVisualInspectorLabel("Node label", 16));
        var labelBox = MachineBuilderTextBox();
        labelBox.Height = 34;
        labelBox.Text = ReadMachineBuilderFunctionNodeLabel(node, item);
        labelBox.LostKeyboardFocus += (_, _) => CommitMachineBuilderFunctionNodeLabel(node, item, labelBox.Text);
        labelBox.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            CommitMachineBuilderFunctionNodeLabel(node, item, labelBox.Text);
            e.Handled = true;
        };
        body.Children.Add(labelBox);

        body.Children.Add(MachineBuilderVisualInspectorLabel("Canvas position", 14));
        var position = new Grid();
        position.ColumnDefinitions.Add(new ColumnDefinition());
        position.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        position.ColumnDefinitions.Add(new ColumnDefinition());
        var x = MachineBuilderTextBox();
        x.Height = 32;
        x.Text = node.X.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        position.Children.Add(x);
        var y = MachineBuilderTextBox();
        y.Height = 32;
        y.Text = node.Y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        Grid.SetColumn(y, 2);
        position.Children.Add(y);
        body.Children.Add(position);
        var applyPosition = CreateStudioChromeButton("APPLY POSITION", double.NaN, false);
        applyPosition.Height = 32;
        applyPosition.HorizontalAlignment = HorizontalAlignment.Stretch;
        applyPosition.Margin = new Thickness(0, 7, 0, 0);
        applyPosition.Click += (_, _) => ApplyMachineBuilderFunctionNodePosition(node, x.Text, y.Text);
        body.Children.Add(applyPosition);

        body.Children.Add(MachineBuilderVisualInspectorLabel("Typed ports", 16));
        foreach (var port in item.Ports)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new Border
            {
                Width = 7,
                Height = 7,
                CornerRadius = new CornerRadius(4),
                Background = Brush(FunctionPortColor(port.DataType)),
                VerticalAlignment = VerticalAlignment.Center
            });
            var text = new TextBlock
            {
                Text = $"{(port.Direction == ForeverStudioLibraryPortDirection.Input ? "IN " : "OUT")}  {port.DisplayName} · {port.DataType}" +
                       (port.Required ? " · required" : string.Empty) +
                       (port.Multiple ? " · multiple" : string.Empty),
                Foreground = Brush("#B6C6CB"),
                FontSize = 9,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            body.Children.Add(row);
        }
        if (item.Ports.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = "This function has no typed ports.",
                Foreground = Brush("#71858E"),
                FontSize = 9.3
            });
        }

        body.Children.Add(MachineBuilderVisualInspectorLabel("Connections", 16));
        var connections = _machineBuilderDocument?.Behavior.Connections
            .Where(connection => connection.FromNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) ||
                                 connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
        if (connections.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = "No connections for this node.",
                Foreground = Brush("#71858E"),
                FontSize = 9.3
            });
        }
        else
        {
            foreach (var connection in connections)
                body.Children.Add(BuildMachineBuilderFunctionConnectionRow(connection));
        }

        var actions = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        var duplicate = CreateStudioChromeButton("DUPLICATE", double.NaN, false);
        duplicate.Height = 34;
        duplicate.Click += (_, _) => DuplicateMachineBuilderFunctionNode(node);
        actions.Children.Add(duplicate);
        var delete = CreateStudioChromeButton("DELETE", double.NaN, false);
        delete.Height = 34;
        delete.IsEnabled = !node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase);
        delete.ToolTip = delete.IsEnabled ? "Delete node and its connections" : "The Machine Entry node is required by the project foundation";
        delete.Click += (_, _) => DeleteMachineBuilderFunctionNode(node);
        Grid.SetColumn(delete, 2);
        actions.Children.Add(delete);
        body.Children.Add(actions);

        body.Children.Add(new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 18, 0, 0),
            Child = new TextBlock
            {
                Text = "Phase 4 authors topology and stable typed contracts only. Runtime Java is generated later, so graph edits remain reversible and reviewable in the Machine Project JSON.",
                Foreground = Brush("#7F939B"),
                FontSize = 9.3,
                LineHeight = 15,
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    private Border BuildMachineBuilderFunctionConnectionRow(ForeverStudioMachineBehaviorConnection connection)
    {
        var border = new Border
        {
            Background = Brush("#10191E"),
            BorderBrush = Brush("#2A3D45"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(8, 6, 5, 6),
            Margin = new Thickness(0, 0, 0, 5)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(new TextBlock
        {
            Text = $"{connection.FromNodeId}.{connection.FromPortId}\n→ {connection.ToNodeId}.{connection.ToPortId}",
            Foreground = Brush("#AFC0C5"),
            FontSize = 8.5,
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap
        });
        var remove = CreateStudioChromeButton("×", 28, false);
        remove.Height = 28;
        remove.ToolTip = "Remove connection";
        remove.Click += (_, _) => RemoveMachineBuilderFunctionConnection(connection);
        Grid.SetColumn(remove, 1);
        grid.Children.Add(remove);
        border.Child = grid;
        return border;
    }

    private ForeverStudioMachineBehaviorNode? GetSelectedMachineBuilderFunctionNode()
        => _machineBuilderDocument?.Behavior.Nodes.FirstOrDefault(node =>
            node.Id.Equals(_machineBuilderFunctionSelectedNodeId, StringComparison.OrdinalIgnoreCase));

    private void CommitMachineBuilderFunctionNodeLabel(
        ForeverStudioMachineBehaviorNode node,
        ForeverStudioLibraryItem item,
        string value)
    {
        var label = value.Trim();
        if (string.IsNullOrWhiteSpace(label) || label.Equals(item.DisplayName, StringComparison.Ordinal))
            node.Properties.Remove("label");
        else
            node.Properties["label"] = JsonSerializer.SerializeToElement(label[..Math.Min(label.Length, 128)]);
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
    }

    private void ApplyMachineBuilderFunctionNodePosition(
        ForeverStudioMachineBehaviorNode node,
        string xText,
        string yText)
    {
        if (!double.TryParse(xText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(yText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y) ||
            !double.IsFinite(x) || !double.IsFinite(y))
        {
            SetMachineBuilderFunctionStatus("Canvas position requires finite numeric X/Y values.", "#E18B8B");
            return;
        }
        node.X = Math.Clamp(x, 0, MachineBuilderFunctionCanvasWidth - MachineBuilderFunctionNodeWidth - 8);
        node.Y = Math.Clamp(y, 0, MachineBuilderFunctionCanvasHeight - 120);
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
    }

    private void DuplicateMachineBuilderFunctionNode(ForeverStudioMachineBehaviorNode source)
    {
        if (_machineBuilderDocument is null)
            return;
        var used = _machineBuilderDocument.Behavior.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId(source.Id + "_copy");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
        var copy = new ForeverStudioMachineBehaviorNode
        {
            Id = id,
            LibraryItemId = source.LibraryItemId,
            LibraryItemVersion = source.LibraryItemVersion,
            X = Math.Min(source.X + 34, MachineBuilderFunctionCanvasWidth - MachineBuilderFunctionNodeWidth - 8),
            Y = Math.Min(source.Y + 34, MachineBuilderFunctionCanvasHeight - 120),
            Properties = source.Properties.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase)
        };
        _machineBuilderDocument.Behavior.Nodes.Add(copy);
        _machineBuilderFunctionSelectedNodeId = copy.Id;
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
    }

    private void DeleteMachineBuilderFunctionNode(ForeverStudioMachineBehaviorNode node)
    {
        if (_machineBuilderDocument is null || node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase))
            return;
        var linkCount = _machineBuilderDocument.Behavior.Connections.Count(connection =>
            connection.FromNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) ||
            connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase));
        if (MessageBox.Show(
                this,
                $"Delete function node '{node.Id}'{(linkCount == 0 ? "" : $" and its {linkCount} connection(s)")}?",
                "Delete Machine Builder function node",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _machineBuilderDocument.Behavior.Connections.RemoveAll(connection =>
            connection.FromNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) ||
            connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase));
        _machineBuilderDocument.Behavior.Nodes.Remove(node);
        if (_machineBuilderFunctionPendingOutput is { } pending && pending.NodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase))
            _machineBuilderFunctionPendingOutput = null;
        _machineBuilderFunctionSelectedNodeId = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault()?.Id;
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
    }

    private void RemoveMachineBuilderFunctionConnection(ForeverStudioMachineBehaviorConnection connection)
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderDocument.Behavior.Connections.Remove(connection);
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
        SetMachineBuilderFunctionStatus($"Removed connection '{connection.Id}'.", "#D8B26A");
    }

    private void AutoLayoutMachineBuilderFunctionGraph()
    {
        if (_machineBuilderDocument is null || _machineBuilderDocument.Behavior.Nodes.Count == 0)
            return;
        var nodes = _machineBuilderDocument.Behavior.Nodes;
        var depths = nodes.ToDictionary(node => node.Id, _ => 0, StringComparer.OrdinalIgnoreCase);
        var indegree = nodes.ToDictionary(node => node.Id, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var connection in _machineBuilderDocument.Behavior.Connections)
        {
            if (indegree.ContainsKey(connection.ToNodeId) && indegree.ContainsKey(connection.FromNodeId))
                indegree[connection.ToNodeId]++;
        }
        var queue = new Queue<ForeverStudioMachineBehaviorNode>(nodes.Where(node => indegree[node.Id] == 0));
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.Id))
                continue;
            foreach (var connection in _machineBuilderDocument.Behavior.Connections.Where(connection =>
                         connection.FromNodeId.Equals(current.Id, StringComparison.OrdinalIgnoreCase)))
            {
                if (!depths.ContainsKey(connection.ToNodeId))
                    continue;
                depths[connection.ToNodeId] = Math.Max(depths[connection.ToNodeId], depths[current.Id] + 1);
                indegree[connection.ToNodeId] = Math.Max(0, indegree[connection.ToNodeId] - 1);
                if (indegree[connection.ToNodeId] == 0)
                {
                    var next = nodes.FirstOrDefault(node => node.Id.Equals(connection.ToNodeId, StringComparison.OrdinalIgnoreCase));
                    if (next is not null)
                        queue.Enqueue(next);
                }
            }
        }

        var maxDepth = 0;
        foreach (var node in nodes.Where(node => !visited.Contains(node.Id)))
            depths[node.Id] = ++maxDepth;
        foreach (var group in nodes.GroupBy(node => depths[node.Id]).OrderBy(group => group.Key))
        {
            var x = 60 + Math.Min(group.Key, 5) * 275;
            var y = 60d;
            foreach (var node in group.OrderBy(node => node.Id, StringComparer.OrdinalIgnoreCase))
            {
                node.X = Math.Clamp(x, 0, MachineBuilderFunctionCanvasWidth - MachineBuilderFunctionNodeWidth - 8);
                node.Y = Math.Clamp(y, 0, MachineBuilderFunctionCanvasHeight - 120);
                y += 170;
            }
        }
        MarkMachineBuilderFunctionDirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
        SetMachineBuilderFunctionStatus("Graph auto-layout applied from connection depth.", "#74C99A");
    }

    private static string ReadMachineBuilderFunctionNodeLabel(
        ForeverStudioMachineBehaviorNode node,
        ForeverStudioLibraryItem item)
    {
        if (node.Properties.TryGetValue("label", out var label) && label.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(label.GetString()))
            return label.GetString()!;
        return item.DisplayName;
    }

    private static string ShortMachineBuilderFunctionCategory(string category)
    {
        var root = category.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? category;
        return root.Length <= 10 ? root.ToUpperInvariant() : root[..10].ToUpperInvariant();
    }

    private static string FunctionPortColor(string dataType)
        => dataType.ToLowerInvariant() switch
        {
            "flow" => "#E1B866",
            "boolean" => "#E1848A",
            "number" => "#78C9A2",
            "item" => "#B89DE3",
            "fluid" => "#69AEE6",
            "energy" => "#E5C64E",
            "state" => "#E89567",
            "recipe" => "#C98CB8",
            "component" => "#71D0D0",
            _ => "#91A9B1"
        };

    private static string SanitizeMachineBuilderGraphId(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.' ? character : '_')
            .ToArray();
        var result = new string(chars).Trim('_', '-', '.');
        if (result.Length > 118)
            result = result[..118].TrimEnd('_', '-', '.');
        return result;
    }

    private void MarkMachineBuilderFunctionDirty()
    {
        _machineBuilderDirty = true;
        RefreshMachineBuilderValidation();
    }

    private void SetMachineBuilderFunctionStatus(string message, string color)
    {
        if (_machineBuilderFunctionStatus is null)
            return;
        _machineBuilderFunctionStatus.Text = message;
        _machineBuilderFunctionStatus.Foreground = Brush(color);
    }
}

using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private sealed record SemanticProcessBlockSpec(
        string Key,
        string Title,
        string Subtitle,
        string LibraryItemId,
        string Role,
        double X,
        double Y);

    private static readonly SemanticProcessBlockSpec[] SemanticProcessBlocks =
    [
        new("item-input", "ITEM INPUT", "Inventory source", "function.item-input", "input", 120, 250),
        new("powered-processor", "POWERED PROCESSOR", "Recipe + FE processing", "function.powered-processor", "processor", 430, 180),
        new("item-output", "ITEM OUTPUT", "Result storage", "function.item-output", "output", 760, 250),
        new("energy-storage", "ENERGY STORAGE", "FE buffer", "function.energy-storage", "energy", 430, 430)
    ];

    private bool _semanticProcessInitialized;
    private FrameworkElement? _semanticProcessBody;
    private StackPanel? _semanticProcessCanvas;
    private TextBlock? _semanticProcessSummary;
    private TextBox? _semanticInputItemBox;
    private TextBox? _semanticInputCountBox;
    private TextBox? _semanticOutputItemBox;
    private TextBox? _semanticOutputCountBox;
    private TextBox? _semanticDurationBox;
    private TextBox? _semanticEnergyPerTickBox;
    private ProgressBar? _semanticPreviewProgressBar;
    private TextBlock? _semanticPreviewStatus;
    private TextBlock? _semanticPreviewInput;
    private TextBlock? _semanticPreviewOutput;
    private TextBlock? _semanticPreviewEnergy;
    private DispatcherTimer? _semanticPreviewTimer;
    private double _semanticPreviewProgress;
    private bool _semanticPreviewRunning;

    public void EnableForeverMachineBuilderSemanticProcessPhase4()
    {
        if (_semanticProcessInitialized)
            return;
        _semanticProcessInitialized = true;

        if (_machineBuilderGuiStep is not null)
        {
            UpdateSemanticStepCopy(_machineBuilderGuiStep);
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderSemanticProcess();
        }

        if (_machineBuilderPhase5GuiTab is not null)
        {
            var label = FindStudioVisualChildren<TextBlock>(_machineBuilderPhase5GuiTab).FirstOrDefault();
            if (label is not null)
                label.Text = "PROCESS & GUI";
            _machineBuilderPhase5GuiTab.Click += (_, _) => ShowMachineBuilderSemanticProcess();
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
                    label.Text = "PROCESS  →";
                continueButton.Click += (_, _) => ShowMachineBuilderSemanticProcess();
            }
        }
    }

    private void UpdateSemanticStepCopy(Border step)
    {
        if (step.Child is not Grid grid)
            return;
        var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
        var texts = copy?.Children.OfType<TextBlock>().ToList();
        if (texts is null || texts.Count < 2)
            return;
        texts[1].Text = "Compose process blocks, preview behavior and let Studio author the contracts";
    }

    private void ShowMachineBuilderSemanticProcess()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        ShowMachineBuilderPhase5(MachineBuilderPhase5Mode.Gui);
        if (_machineBuilderPhase5ModeHost is null)
            return;

        _semanticProcessBody ??= BuildMachineBuilderSemanticProcessBody();
        _machineBuilderPhase5ModeHost.Children.Clear();
        _machineBuilderPhase5ModeHost.Children.Add(_semanticProcessBody);
        RefreshMachineBuilderSemanticProcess();
    }

    private FrameworkElement BuildMachineBuilderSemanticProcessBody()
    {
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });

        var palette = BuildSemanticPaletteCard();
        Grid.SetColumn(palette, 0);
        body.Children.Add(palette);

        var canvas = BuildSemanticCanvasCard();
        Grid.SetColumn(canvas, 2);
        body.Children.Add(canvas);

        var inspector = BuildSemanticInspectorCard();
        Grid.SetColumn(inspector, 4);
        body.Children.Add(inspector);
        return body;
    }

    private Border BuildSemanticPaletteCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(15) };
        panel.Children.Add(SemanticTitle(
            "BUILDING BLOCKS",
            "Drag a semantic block onto the process canvas, or click it to add."));

        foreach (var spec in SemanticProcessBlocks)
            panel.Children.Add(BuildSemanticPaletteEntry(spec));

        var recommended = CreateStudioChromeButton("ADD RECOMMENDED SET", double.NaN, true);
        recommended.Height = 36;
        recommended.HorizontalAlignment = HorizontalAlignment.Stretch;
        recommended.Margin = new Thickness(0, 14, 0, 0);
        recommended.Click += (_, _) =>
        {
            foreach (var spec in SemanticProcessBlocks)
                EnsureSemanticNode(spec);
            EnsureSemanticFlowConnection();
            MarkMachineBuilderPhase5Dirty();
            RefreshMachineBuilderSemanticProcess();
            SetMachineBuilderPhase5Status(
                "Added the powered-processor building block set. Configure the process on the right.",
                "#74C99A");
        };
        panel.Children.Add(recommended);

        panel.Children.Add(new TextBlock
        {
            Text = "Studio writes node IDs, typed flow and recipe/GUI JSON in the background. The Advanced Graph remains available at any time.",
            Foreground = Brush("#7F939B"),
            FontSize = 9.3,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 15,
            Margin = new Thickness(0, 14, 0, 0)
        });

        card.Child = panel;
        return card;
    }

    private Border BuildSemanticPaletteEntry(SemanticProcessBlockSpec spec)
    {
        var surface = new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#30474F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand,
            ToolTip = "Click to add, or drag onto the process canvas."
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = spec.Title,
            Foreground = Brush("#E9F3F4"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = spec.Subtitle,
            Foreground = Brush("#81959C"),
            FontSize = 9.2,
            Margin = new Thickness(0, 3, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "drag or click",
            Foreground = Brush("#62C8C7"),
            FontSize = 8.7,
            Margin = new Thickness(0, 6, 0, 0)
        });
        surface.Child = stack;
        surface.MouseLeftButtonUp += (_, _) =>
        {
            EnsureSemanticNode(spec);
            if (spec.Role == "processor")
                EnsureSemanticFlowConnection();
            MarkMachineBuilderPhase5Dirty();
            RefreshMachineBuilderSemanticProcess();
        };
        surface.MouseMove += (_, args) =>
        {
            if (args.LeftButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(surface, spec.Key, DragDropEffects.Copy);
        };
        return surface;
    }

    private Border BuildSemanticCanvasCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var root = new Grid { Margin = new Thickness(15) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(SemanticTitle(
            "PROCESS CANVAS",
            "A semantic view of the machine. This is not the low-level compiler graph."));

        var dropSurface = new Border
        {
            Background = Brush("#091014"),
            BorderBrush = Brush("#2A4149"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            MinHeight = 430,
            AllowDrop = true
        };
        dropSurface.DragOver += (_, args) =>
        {
            args.Effects = args.Data.GetDataPresent(DataFormats.StringFormat)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            args.Handled = true;
        };
        dropSurface.Drop += (_, args) =>
        {
            if (args.Data.GetData(DataFormats.StringFormat) is not string key)
                return;
            var spec = SemanticProcessBlocks.FirstOrDefault(candidate => candidate.Key == key);
            if (spec is null)
                return;
            EnsureSemanticNode(spec);
            if (spec.Role == "processor")
                EnsureSemanticFlowConnection();
            MarkMachineBuilderPhase5Dirty();
            RefreshMachineBuilderSemanticProcess();
        };
        _semanticProcessCanvas = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        dropSurface.Child = _semanticProcessCanvas;
        Grid.SetRow(dropSurface, 2);
        root.Children.Add(dropSurface);

        _semanticProcessSummary = new TextBlock
        {
            Foreground = Brush("#7F939B"),
            FontSize = 9.4,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(_semanticProcessSummary, 3);
        root.Children.Add(_semanticProcessSummary);

        card.Child = root;
        return card;
    }

    private Border BuildSemanticInspectorCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var outer = new Grid { Margin = new Thickness(15) };
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        outer.RowDefinitions.Add(new RowDefinition());
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        outer.Children.Add(SemanticTitle(
            "POWERED PROCESS",
            "Configure what the machine does. Studio builds the recipe contract and bindings."));

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var panel = new StackPanel();
        panel.Children.Add(SemanticSectionLabel("Input"));
        _semanticInputItemBox = SemanticField("minecraft:coal", "Item resource location");
        panel.Children.Add(_semanticInputItemBox);
        _semanticInputCountBox = SemanticField("4", "Input count");
        _semanticInputCountBox.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(_semanticInputCountBox);

        panel.Children.Add(SemanticSectionLabel("Output", 14));
        _semanticOutputItemBox = SemanticField("minecraft:redstone", "Item resource location");
        panel.Children.Add(_semanticOutputItemBox);
        _semanticOutputCountBox = SemanticField("1", "Output count");
        _semanticOutputCountBox.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(_semanticOutputCountBox);

        panel.Children.Add(SemanticSectionLabel("Processing", 14));
        _semanticDurationBox = SemanticField("100", "Duration in ticks");
        panel.Children.Add(_semanticDurationBox);
        _semanticEnergyPerTickBox = SemanticField("40", "Energy per tick (FE/t)");
        _semanticEnergyPerTickBox.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(_semanticEnergyPerTickBox);

        foreach (var box in new[]
                 {
                     _semanticInputItemBox, _semanticInputCountBox, _semanticOutputItemBox,
                     _semanticOutputCountBox, _semanticDurationBox, _semanticEnergyPerTickBox
                 })
        {
            box.TextChanged += (_, _) => RefreshSemanticPreviewText();
        }

        var apply = CreateStudioChromeButton("APPLY PROCESS", double.NaN, true);
        apply.Height = 38;
        apply.HorizontalAlignment = HorizontalAlignment.Stretch;
        apply.Margin = new Thickness(0, 14, 0, 0);
        apply.Click += (_, _) => ApplySemanticPoweredProcess();
        panel.Children.Add(apply);

        var advanced = CreateStudioChromeButton("OPEN ADVANCED GRAPH", double.NaN, false);
        advanced.Height = 34;
        advanced.HorizontalAlignment = HorizontalAlignment.Stretch;
        advanced.Margin = new Thickness(0, 8, 0, 0);
        advanced.Click += (_, _) =>
        {
            StopSemanticPreview();
            HideMachineBuilderPhase5Surface();
            ShowMachineBuilderFunctionPhase4();
        };
        panel.Children.Add(advanced);

        panel.Children.Add(BuildSemanticPreviewCard());

        scroll.Content = panel;
        Grid.SetRow(scroll, 2);
        outer.Children.Add(scroll);

        var note = new TextBlock
        {
            Text = "Preview is an authoring simulation only. Production remains blocked until the dedicated runtime compiler exists.",
            Foreground = Brush("#758A92"),
            FontSize = 8.9,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(note, 3);
        outer.Children.Add(note);

        card.Child = outer;
        return card;
    }

    private Border BuildSemanticPreviewCard()
    {
        var card = new Border
        {
            Background = Brush("#0A1115"),
            BorderBrush = Brush("#284148"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 16, 0, 0)
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "LIVE AUTHORING PREVIEW",
            Foreground = Brush("#72D5D3"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold
        });

        _semanticPreviewInput = PreviewLine();
        _semanticPreviewInput.Margin = new Thickness(0, 10, 0, 0);
        panel.Children.Add(_semanticPreviewInput);

        _semanticPreviewProgressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 12,
            Background = Brush("#15242A"),
            Foreground = Brush("#63C9C6"),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 10, 0, 0)
        };
        panel.Children.Add(_semanticPreviewProgressBar);

        _semanticPreviewStatus = PreviewLine();
        _semanticPreviewStatus.Margin = new Thickness(0, 7, 0, 0);
        panel.Children.Add(_semanticPreviewStatus);

        _semanticPreviewOutput = PreviewLine();
        _semanticPreviewOutput.Margin = new Thickness(0, 5, 0, 0);
        panel.Children.Add(_semanticPreviewOutput);

        _semanticPreviewEnergy = PreviewLine();
        _semanticPreviewEnergy.Margin = new Thickness(0, 5, 0, 0);
        panel.Children.Add(_semanticPreviewEnergy);

        var controls = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        controls.ColumnDefinitions.Add(new ColumnDefinition());
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        controls.ColumnDefinitions.Add(new ColumnDefinition());
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        controls.ColumnDefinitions.Add(new ColumnDefinition());

        var play = CreateStudioChromeButton("▶ PLAY", double.NaN, true);
        play.Height = 30;
        play.Click += (_, _) => StartSemanticPreview();
        controls.Children.Add(play);

        var pause = CreateStudioChromeButton("PAUSE", double.NaN, false);
        pause.Height = 30;
        pause.Click += (_, _) => PauseSemanticPreview();
        Grid.SetColumn(pause, 2);
        controls.Children.Add(pause);

        var reset = CreateStudioChromeButton("RESET", double.NaN, false);
        reset.Height = 30;
        reset.Click += (_, _) => ResetSemanticPreview();
        Grid.SetColumn(reset, 4);
        controls.Children.Add(reset);
        panel.Children.Add(controls);

        card.Child = panel;
        return card;
    }

    private void RefreshMachineBuilderSemanticProcess()
    {
        if (_machineBuilderDocument is null)
            return;

        LoadSemanticProcessFieldsFromDocument();
        RenderSemanticProcessCanvas();
        RefreshSemanticPreviewText();

        var input = FindSemanticNode("function.item-input", "input");
        var processor = FindSemanticNode("function.powered-processor", "processor");
        var output = FindSemanticNode("function.item-output", "output");
        var energy = FindSemanticNode("function.energy-storage", "energy");
        var count = new[] { input, processor, output, energy }.Count(node => node is not null);

        if (_semanticProcessSummary is not null)
        {
            _semanticProcessSummary.Text =
                $"{count}/4 recommended blocks present · " +
                $"{_machineBuilderDocument.Recipes.Count} recipe contract(s) · " +
                $"{_machineBuilderDocument.Gui.Bindings.Count} GUI binding(s)";
        }
    }

    private void RenderSemanticProcessCanvas()
    {
        if (_semanticProcessCanvas is null || _machineBuilderDocument is null)
            return;

        _semanticProcessCanvas.Children.Clear();

        var input = FindSemanticNode("function.item-input", "input");
        var processor = FindSemanticNode("function.powered-processor", "processor");
        var output = FindSemanticNode("function.item-output", "output");
        var energy = FindSemanticNode("function.energy-storage", "energy");

        if (input is null && processor is null && output is null && energy is null)
        {
            _semanticProcessCanvas.Children.Add(new TextBlock
            {
                Text = "Drop building blocks here to compose a process.",
                Foreground = Brush("#70858D"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 90, 0, 90)
            });
            return;
        }

        if (input is not null)
            _semanticProcessCanvas.Children.Add(SemanticCanvasNode("ITEM INPUT", CurrentInputCaption(), "#31545C"));

        if (input is not null && processor is not null)
            _semanticProcessCanvas.Children.Add(SemanticArrow("↓ item flow"));

        if (processor is not null)
        {
            var processRow = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            processRow.ColumnDefinitions.Add(new ColumnDefinition());
            processRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            processRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            var process = SemanticCanvasNode(
                "POWERED PROCESSOR",
                CurrentProcessCaption(),
                "#244B4D");
            processRow.Children.Add(process);

            if (energy is not null)
            {
                var energyNode = SemanticCanvasNode("FE BUFFER", "supplies process energy", "#4B4330");
                Grid.SetColumn(energyNode, 2);
                processRow.Children.Add(energyNode);
            }
            _semanticProcessCanvas.Children.Add(processRow);
        }
        else if (energy is not null)
        {
            _semanticProcessCanvas.Children.Add(SemanticCanvasNode("FE BUFFER", "energy storage", "#4B4330"));
        }

        if (processor is not null && output is not null)
            _semanticProcessCanvas.Children.Add(SemanticArrow("↓ result"));

        if (output is not null)
            _semanticProcessCanvas.Children.Add(SemanticCanvasNode("ITEM OUTPUT", CurrentOutputCaption(), "#465137"));
    }

    private Border SemanticCanvasNode(string title, string subtitle, string background)
    {
        var border = new Border
        {
            Background = Brush(background),
            BorderBrush = Brush("#547078"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 3, 0, 3)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brush("#F0F7F7"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Brush("#B2C0C4"),
            FontSize = 9.2,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        border.Child = stack;
        return border;
    }

    private static TextBlock SemanticArrow(string text)
        => new()
        {
            Text = text,
            Foreground = Brush("#5FBFBE"),
            FontSize = 9.2,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        };

    private ForeverStudioMachineBehaviorNode EnsureSemanticNode(SemanticProcessBlockSpec spec)
    {
        if (_machineBuilderDocument is null)
            throw new InvalidOperationException("No Machine Builder document is active.");

        var existing = FindSemanticNode(spec.LibraryItemId, spec.Role);
        if (existing is not null)
            return existing;

        var used = _machineBuilderDocument.Behavior.Nodes
            .Select(node => node.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId(spec.Role == "processor"
            ? "powered_processor"
            : spec.Role == "energy"
                ? "energy_storage"
                : $"item_{spec.Role}");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");

        var node = new ForeverStudioMachineBehaviorNode
        {
            Id = id,
            LibraryItemId = spec.LibraryItemId,
            LibraryItemVersion = 1,
            X = spec.X,
            Y = spec.Y,
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["semanticRole"] = JsonSerializer.SerializeToElement(spec.Role),
                ["semanticAuthoring"] = JsonSerializer.SerializeToElement("process-composer")
            }
        };
        _machineBuilderDocument.Behavior.Nodes.Add(node);
        return node;
    }

    private ForeverStudioMachineBehaviorNode? FindSemanticNode(string libraryItemId, string role)
    {
        if (_machineBuilderDocument is null)
            return null;

        return _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
                   node.LibraryItemId.Equals(libraryItemId, StringComparison.OrdinalIgnoreCase) &&
                   ReadSemanticString(node.Properties, "semanticRole", string.Empty)
                       .Equals(role, StringComparison.OrdinalIgnoreCase))
               ?? _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
                   node.LibraryItemId.Equals(libraryItemId, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureSemanticFlowConnection()
    {
        if (_machineBuilderDocument is null)
            return;

        var processor = FindSemanticNode("function.powered-processor", "processor");
        if (processor is null)
            return;

        var entry = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
            node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return;

        if (_machineBuilderDocument.Behavior.Connections.Any(connection =>
                connection.FromNodeId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.FromPortId.Equals("flow", StringComparison.OrdinalIgnoreCase) &&
                connection.ToNodeId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.ToPortId.Equals("flow-in", StringComparison.OrdinalIgnoreCase)))
            return;

        var used = _machineBuilderDocument.Behavior.Connections
            .Select(connection => connection.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId($"flow_{entry.Id}_{processor.Id}");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");

        _machineBuilderDocument.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
        {
            Id = id,
            FromNodeId = entry.Id,
            FromPortId = "flow",
            ToNodeId = processor.Id,
            ToPortId = "flow-in"
        });
    }

    private void ApplySemanticPoweredProcess()
    {
        if (_machineBuilderDocument is null)
            return;

        if (!TryReadSemanticProcessValues(
                out var inputItem,
                out var inputCount,
                out var outputItem,
                out var outputCount,
                out var duration,
                out var energyPerTick,
                out var error))
        {
            SetMachineBuilderPhase5Status(error, "#E18B8B");
            return;
        }

        var input = FindSemanticNode("function.item-input", "input");
        var processor = FindSemanticNode("function.powered-processor", "processor");
        var output = FindSemanticNode("function.item-output", "output");
        var energy = FindSemanticNode("function.energy-storage", "energy");

        if (input is null || processor is null || output is null || energy is null)
        {
            SetMachineBuilderPhase5Status(
                "Add Item Input, Powered Processor, Item Output and Energy Storage before applying the process.",
                "#D8B26A");
            return;
        }

        EnsureSemanticFlowConnection();

        const string recipeId = "process_main";
        var totalEnergy = checked(duration * energyPerTick);
        var recipe = _machineBuilderDocument.Recipes.FirstOrDefault(candidate =>
                         ReadSemanticString(candidate.Properties, "semanticOwner", string.Empty)
                             .Equals("process-composer", StringComparison.OrdinalIgnoreCase))
                     ?? _machineBuilderDocument.Recipes.FirstOrDefault(candidate =>
                         candidate.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase));

        if (recipe is null)
        {
            recipe = new ForeverStudioMachineRecipeBinding { Id = recipeId };
            _machineBuilderDocument.Recipes.Add(recipe);
        }

        recipe.Id = recipeId;
        recipe.RecipeTypeId = "forever_skyblock:powered_processing";
        recipe.Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["semanticOwner"] = JsonSerializer.SerializeToElement("process-composer"),
            ["preset"] = JsonSerializer.SerializeToElement("powered-processor"),
            ["input"] = JsonSerializer.SerializeToElement(new
            {
                item = inputItem,
                count = inputCount,
                sourceNodeId = input.Id
            }),
            ["output"] = JsonSerializer.SerializeToElement(new
            {
                item = outputItem,
                count = outputCount,
                targetNodeId = output.Id
            }),
            ["processingTimeTicks"] = JsonSerializer.SerializeToElement(duration),
            ["energy"] = JsonSerializer.SerializeToElement(new
            {
                fePerTick = energyPerTick,
                totalFe = totalEnergy,
                storageNodeId = energy.Id
            })
        };

        input.Properties["semanticRole"] = JsonSerializer.SerializeToElement("input");
        input.Properties["item"] = JsonSerializer.SerializeToElement(inputItem);
        input.Properties["count"] = JsonSerializer.SerializeToElement(inputCount);
        output.Properties["semanticRole"] = JsonSerializer.SerializeToElement("output");
        output.Properties["item"] = JsonSerializer.SerializeToElement(outputItem);
        output.Properties["count"] = JsonSerializer.SerializeToElement(outputCount);
        energy.Properties["semanticRole"] = JsonSerializer.SerializeToElement("energy");
        processor.Properties["semanticRole"] = JsonSerializer.SerializeToElement("processor");
        processor.Properties["recipeId"] = JsonSerializer.SerializeToElement(recipeId);
        processor.Properties["inputNodeId"] = JsonSerializer.SerializeToElement(input.Id);
        processor.Properties["outputNodeId"] = JsonSerializer.SerializeToElement(output.Id);
        processor.Properties["energyNodeId"] = JsonSerializer.SerializeToElement(energy.Id);
        processor.Properties["processingTimeTicks"] = JsonSerializer.SerializeToElement(duration);
        processor.Properties["energyPerTick"] = JsonSerializer.SerializeToElement(energyPerTick);
        processor.Properties["totalEnergy"] = JsonSerializer.SerializeToElement(totalEnergy);

        ApplySemanticGuiBindings(input, processor, output, energy);

        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderSemanticProcess();
        SetMachineBuilderPhase5Status(
            $"Powered process applied: {inputCount}× {inputItem} → {outputCount}× {outputItem} · {duration} ticks · {energyPerTick} FE/t.",
            "#74C99A");
    }

    private void ApplySemanticGuiBindings(
        ForeverStudioMachineBehaviorNode input,
        ForeverStudioMachineBehaviorNode processor,
        ForeverStudioMachineBehaviorNode output,
        ForeverStudioMachineBehaviorNode energy)
    {
        if (_machineBuilderDocument is null)
            return;

        _machineBuilderDocument.Gui.ScreenId = string.IsNullOrWhiteSpace(_machineBuilderDocument.Gui.ScreenId)
            ? $"{_machineBuilderDocument.AssetId}_screen"
            : _machineBuilderDocument.Gui.ScreenId;

        _machineBuilderDocument.Gui.Bindings.RemoveAll(binding =>
            ReadSemanticString(binding.Properties, "semanticOwner", string.Empty)
                .Equals("process-composer", StringComparison.OrdinalIgnoreCase));

        _machineBuilderDocument.Gui.Bindings.Add(
            SemanticGuiBinding("semantic_input_slot", "asset.input-tray", input.Id, "item", "item-slot", "Input", "graph-port", 0));
        _machineBuilderDocument.Gui.Bindings.Add(
            SemanticGuiBinding("semantic_output_slot", "asset.output-tray", output.Id, "stored", "output-slot", "Output", "capability", 1, "stored-item"));
        _machineBuilderDocument.Gui.Bindings.Add(
            SemanticGuiBinding("semantic_energy_bar", "asset.screen", energy.Id, "ratio", "energy-bar", "Energy", "graph-port", 2));
        _machineBuilderDocument.Gui.Bindings.Add(
            SemanticGuiBinding("semantic_progress_bar", "asset.screen", processor.Id, "progress", "progress-bar", "Progress", "graph-port", 3));
        _machineBuilderDocument.Gui.Bindings.Add(
            SemanticGuiBinding("semantic_status", "asset.status-lamp", processor.Id, "running", "status", "Running", "graph-port", 4));
    }

    private static ForeverStudioMachineGuiBinding SemanticGuiBinding(
        string id,
        string assetId,
        string sourceId,
        string sourcePortId,
        string widget,
        string label,
        string sourceKind,
        int order,
        string? sourceCapability = null)
    {
        var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["widget"] = JsonSerializer.SerializeToElement(widget),
            ["sourcePortId"] = JsonSerializer.SerializeToElement(sourcePortId),
            ["sourceType"] = JsonSerializer.SerializeToElement(widget.Contains("item", StringComparison.OrdinalIgnoreCase) ? "item" :
                widget.Contains("status", StringComparison.OrdinalIgnoreCase) ? "boolean" : "number"),
            ["label"] = JsonSerializer.SerializeToElement(label),
            ["order"] = JsonSerializer.SerializeToElement(order),
            ["semanticOwner"] = JsonSerializer.SerializeToElement("process-composer"),
            ["sourceKind"] = JsonSerializer.SerializeToElement(sourceKind)
        };
        if (!string.IsNullOrWhiteSpace(sourceCapability))
            properties["sourceCapability"] = JsonSerializer.SerializeToElement(sourceCapability);

        return new ForeverStudioMachineGuiBinding
        {
            Id = id,
            LibraryItemId = assetId,
            LibraryItemVersion = 1,
            SourceId = sourceId,
            Properties = properties
        };
    }

    private void LoadSemanticProcessFieldsFromDocument()
    {
        if (_machineBuilderDocument is null ||
            _semanticInputItemBox is null ||
            _semanticInputCountBox is null ||
            _semanticOutputItemBox is null ||
            _semanticOutputCountBox is null ||
            _semanticDurationBox is null ||
            _semanticEnergyPerTickBox is null)
            return;

        var recipe = _machineBuilderDocument.Recipes.FirstOrDefault(candidate =>
                         ReadSemanticString(candidate.Properties, "semanticOwner", string.Empty)
                             .Equals("process-composer", StringComparison.OrdinalIgnoreCase))
                     ?? _machineBuilderDocument.Recipes.FirstOrDefault(candidate =>
                         candidate.RecipeTypeId.Equals("forever_skyblock:powered_processing", StringComparison.OrdinalIgnoreCase));

        if (recipe is null)
            return;

        if (recipe.Properties.TryGetValue("input", out var input) && input.ValueKind == JsonValueKind.Object)
        {
            if (input.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.String)
                _semanticInputItemBox.Text = item.GetString() ?? _semanticInputItemBox.Text;
            if (input.TryGetProperty("count", out var count) && count.TryGetInt32(out var value))
                _semanticInputCountBox.Text = value.ToString(CultureInfo.InvariantCulture);
        }

        if (recipe.Properties.TryGetValue("output", out var output) && output.ValueKind == JsonValueKind.Object)
        {
            if (output.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.String)
                _semanticOutputItemBox.Text = item.GetString() ?? _semanticOutputItemBox.Text;
            if (output.TryGetProperty("count", out var count) && count.TryGetInt32(out var value))
                _semanticOutputCountBox.Text = value.ToString(CultureInfo.InvariantCulture);
        }

        if (recipe.Properties.TryGetValue("processingTimeTicks", out var duration) &&
            duration.TryGetInt32(out var durationValue))
            _semanticDurationBox.Text = durationValue.ToString(CultureInfo.InvariantCulture);

        if (recipe.Properties.TryGetValue("energy", out var energy) &&
            energy.ValueKind == JsonValueKind.Object &&
            energy.TryGetProperty("fePerTick", out var fePerTick) &&
            fePerTick.TryGetInt32(out var energyValue))
            _semanticEnergyPerTickBox.Text = energyValue.ToString(CultureInfo.InvariantCulture);
    }

    private bool TryReadSemanticProcessValues(
        out string inputItem,
        out int inputCount,
        out string outputItem,
        out int outputCount,
        out int duration,
        out int energyPerTick,
        out string error)
    {
        inputItem = (_semanticInputItemBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        outputItem = (_semanticOutputItemBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        inputCount = outputCount = duration = energyPerTick = 0;
        error = string.Empty;

        if (!IsSemanticResourceLocation(inputItem) || !IsSemanticResourceLocation(outputItem))
        {
            error = "Input and output items must be valid resource locations such as minecraft:coal.";
            return false;
        }

        if (!int.TryParse(_semanticInputCountBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out inputCount) ||
            inputCount is < 1 or > 64)
        {
            error = "Input count must be between 1 and 64.";
            return false;
        }

        if (!int.TryParse(_semanticOutputCountBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out outputCount) ||
            outputCount is < 1 or > 64)
        {
            error = "Output count must be between 1 and 64.";
            return false;
        }

        if (!int.TryParse(_semanticDurationBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out duration) ||
            duration is < 1 or > 1_000_000)
        {
            error = "Processing duration must be between 1 and 1,000,000 ticks.";
            return false;
        }

        if (!int.TryParse(_semanticEnergyPerTickBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out energyPerTick) ||
            energyPerTick is < 0 or > 1_000_000)
        {
            error = "Energy use must be between 0 and 1,000,000 FE/t.";
            return false;
        }

        if ((long)duration * energyPerTick > int.MaxValue)
        {
            error = "Total energy exceeds the supported authoring range.";
            return false;
        }

        return true;
    }

    private static bool IsSemanticResourceLocation(string value)
    {
        var split = value.Split(':', 2);
        if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
            return false;
        return split[0].All(character =>
                   char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.') &&
               split[1].All(character =>
                   char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.' or '/');
    }

    private void StartSemanticPreview()
    {
        if (!TryReadSemanticProcessValues(
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var error))
        {
            SetMachineBuilderPhase5Status(error, "#E18B8B");
            return;
        }

        _semanticPreviewTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _semanticPreviewTimer.Tick -= SemanticPreviewTick;
        _semanticPreviewTimer.Tick += SemanticPreviewTick;
        _semanticPreviewRunning = true;
        _semanticPreviewTimer.Start();
        RefreshSemanticPreviewText();
    }

    private void PauseSemanticPreview()
    {
        _semanticPreviewRunning = false;
        _semanticPreviewTimer?.Stop();
        RefreshSemanticPreviewText();
    }

    private void ResetSemanticPreview()
    {
        _semanticPreviewRunning = false;
        _semanticPreviewTimer?.Stop();
        _semanticPreviewProgress = 0;
        RefreshSemanticPreviewText();
    }

    private void StopSemanticPreview()
    {
        _semanticPreviewRunning = false;
        _semanticPreviewTimer?.Stop();
    }

    private void SemanticPreviewTick(object? sender, EventArgs e)
    {
        if (!_semanticPreviewRunning)
            return;

        if (!int.TryParse(_semanticDurationBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var duration) ||
            duration <= 0)
            duration = 100;

        _semanticPreviewProgress += 2d / duration;
        if (_semanticPreviewProgress >= 1)
        {
            _semanticPreviewProgress = 1;
            _semanticPreviewRunning = false;
            _semanticPreviewTimer?.Stop();
        }
        RefreshSemanticPreviewText();
    }

    private void RefreshSemanticPreviewText()
    {
        if (_semanticPreviewProgressBar is null)
            return;

        var inputItem = (_semanticInputItemBox?.Text ?? "minecraft:coal").Trim();
        var outputItem = (_semanticOutputItemBox?.Text ?? "minecraft:redstone").Trim();
        int.TryParse(_semanticInputCountBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var inputCount);
        int.TryParse(_semanticOutputCountBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var outputCount);
        int.TryParse(_semanticDurationBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var duration);
        int.TryParse(_semanticEnergyPerTickBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fePerTick);
        inputCount = Math.Max(1, inputCount);
        outputCount = Math.Max(1, outputCount);
        duration = Math.Max(1, duration);
        fePerTick = Math.Max(0, fePerTick);

        var totalEnergy = (long)duration * fePerTick;
        var consumed = (long)Math.Round(totalEnergy * Math.Clamp(_semanticPreviewProgress, 0, 1));
        _semanticPreviewProgressBar.Value = Math.Clamp(_semanticPreviewProgress * 100, 0, 100);

        if (_semanticPreviewInput is not null)
            _semanticPreviewInput.Text = $"Input   {inputCount}× {inputItem}";
        if (_semanticPreviewOutput is not null)
            _semanticPreviewOutput.Text = _semanticPreviewProgress >= 1
                ? $"Output  {outputCount}× {outputItem}  READY"
                : $"Output  {outputCount}× {outputItem}";
        if (_semanticPreviewEnergy is not null)
            _semanticPreviewEnergy.Text = $"Energy  {consumed:N0} / {totalEnergy:N0} FE · {fePerTick:N0} FE/t";
        if (_semanticPreviewStatus is not null)
        {
            var state = _semanticPreviewProgress >= 1
                ? "COMPLETE"
                : _semanticPreviewRunning
                    ? "RUNNING"
                    : _semanticPreviewProgress > 0
                        ? "PAUSED"
                        : "IDLE";
            _semanticPreviewStatus.Text =
                $"Status  {state} · {_semanticPreviewProgress * 100:0}% · {duration / 20d:0.##} s process";
        }

        RenderSemanticProcessCanvas();
    }

    private string CurrentInputCaption()
        => $"{Math.Max(1, ParseSemanticInt(_semanticInputCountBox?.Text, 1))}× {(_semanticInputItemBox?.Text ?? "item")}";

    private string CurrentOutputCaption()
        => $"{Math.Max(1, ParseSemanticInt(_semanticOutputCountBox?.Text, 1))}× {(_semanticOutputItemBox?.Text ?? "item")}";

    private string CurrentProcessCaption()
    {
        var duration = Math.Max(1, ParseSemanticInt(_semanticDurationBox?.Text, 100));
        var energy = Math.Max(0, ParseSemanticInt(_semanticEnergyPerTickBox?.Text, 40));
        return $"{duration} ticks · {energy} FE/t · {(long)duration * energy:N0} FE total";
    }

    private static int ParseSemanticInt(string? value, int fallback)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static string ReadSemanticString(
        Dictionary<string, JsonElement> properties,
        string key,
        string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static TextBlock SemanticTitle(string title, string subtitle)
        => new()
        {
            Text = $"{title}\n{subtitle}",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 0, 0, 10)
        };

    private static TextBlock SemanticSectionLabel(string text, double top = 0)
        => new()
        {
            Text = text.ToUpperInvariant(),
            Foreground = Brush("#A8BAC0"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, top, 0, 6)
        };

    private TextBox SemanticField(string value, string tooltip)
    {
        var box = MachineBuilderTextBox();
        box.Height = 32;
        box.Text = value;
        box.ToolTip = tooltip;
        return box;
    }

    private static TextBlock PreviewLine()
        => new()
        {
            Foreground = Brush("#C4D2D6"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap
        };
}

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
    private sealed record ComposerV2BlockSpec(
        string Key,
        string Category,
        string Title,
        string Subtitle,
        string LibraryItemId);

    private sealed record ComposerV2Target(
        ForeverStudioMachineBehaviorNode Node,
        ForeverStudioLibraryPort Port,
        string Label)
    {
        public override string ToString() => Label;
    }

    private static readonly ComposerV2BlockSpec[] ComposerV2Blocks =
    [
        new("item-input", "Inventory", "Item Input", "Receives an item stack", "function.item-input"),
        new("item-output", "Inventory", "Item Output", "Publishes a produced item", "function.item-output"),
        new("item-storage", "Inventory", "Item Storage", "General item buffer", "function.item-storage"),
        new("filter-slot", "Inventory", "Filter Slot", "Configures accepted items", "function.filter-slot"),
        new("catalyst-slot", "Inventory", "Catalyst Slot", "Optional process catalyst", "function.catalyst-slot"),
        new("fuel-slot", "Inventory", "Fuel Slot", "Fuel input for generators", "function.fuel-slot"),

        new("standard-processor", "Processing", "Standard Processor", "Generic recipe processing", "function.standard-processor"),
        new("powered-processor", "Processing", "Powered Processor", "Recipe processing with FE", "function.powered-processor"),
        new("fluid-processor", "Processing", "Fluid Processor", "Item/fluid process core", "function.fluid-processor"),
        new("process-progress", "Processing", "Process Progress", "Reusable normalized progress", "function.process-progress"),
        new("find-recipe", "Processing", "Find Recipe", "Looks up a matching recipe", "function.find-recipe"),

        new("energy-storage", "Energy", "Energy Storage", "FE buffer and fill ratio", "function.energy-storage"),
        new("consume-energy", "Energy", "Consume Energy", "Consumes FE from storage", "function.consume-energy"),
        new("generate-energy", "Energy", "Generate Energy", "Adds FE to storage", "function.generate-energy"),
        new("generator-core", "Energy", "Generator Core", "Fuel-to-FE behavior", "function.generator-core"),

        new("fluid-tank", "Fluids", "Fluid Tank", "Stores fluid and fill ratio", "function.fluid-tank"),
        new("consume-fluid", "Fluids", "Consume Fluid", "Drains a process fluid", "function.consume-fluid"),
        new("produce-fluid", "Fluids", "Produce Fluid", "Fills a target tank", "function.produce-fluid"),

        new("redstone-controller", "Control", "Redstone Controller", "Enable/disable policy", "function.redstone-controller"),
        new("redstone-input", "Control", "Redstone Input", "Reads signal strength", "function.redstone-input"),
        new("branch", "Control", "Branch", "Routes flow by condition", "function.branch"),
        new("timer", "Control", "Timer", "Tick timer or cooldown", "function.timer"),
        new("set-state", "Control", "Set Machine State", "Changes semantic state", "function.set-state"),

        new("on-tick", "Events", "On Tick", "Server-tick entry point", "function.on-tick"),
        new("on-use", "Events", "On Player Use", "Player-interaction entry", "function.on-use"),
        new("on-inventory-changed", "Events", "On Inventory Changed", "Inventory-change entry", "function.on-inventory-changed"),
        new("on-redstone-changed", "Events", "On Redstone Changed", "Signal-change entry", "function.on-redstone-changed")
    ];

    private bool _composerV2Initialized;
    private FrameworkElement? _composerV2Body;
    private WrapPanel? _composerV2Canvas;
    private StackPanel? _composerV2Inspector;
    private TextBlock? _composerV2Summary;
    private TextBlock? _composerV2PreviewText;
    private ProgressBar? _composerV2PreviewProgress;
    private string? _composerV2SelectedNodeId;
    private DispatcherTimer? _composerV2Timer;
    private double _composerV2Progress;
    private bool _composerV2Running;

    public void EnableForeverMachineBuilderSemanticComposerV2()
    {
        if (_composerV2Initialized)
            return;
        _composerV2Initialized = true;

        if (_machineBuilderGuiStep is not null)
        {
            if (_machineBuilderGuiStep.Child is Grid grid)
            {
                var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
                var description = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                if (description is not null)
                    description.Text = "Compose reusable machine blocks visually; Studio owns the JSON contracts";
            }
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderSemanticComposerV2();
        }

        if (_machineBuilderPhase5GuiTab is not null)
        {
            var label = FindStudioVisualChildren<TextBlock>(_machineBuilderPhase5GuiTab).FirstOrDefault();
            if (label is not null)
                label.Text = "MACHINE COMPOSER";
            _machineBuilderPhase5GuiTab.Click += (_, _) => ShowMachineBuilderSemanticComposerV2();
        }

        if (_machineBuilderFunctionSurface is not null)
        {
            var continueButton = FindStudioVisualChildren<Button>(_machineBuilderFunctionSurface)
                .FirstOrDefault(button => string.Equals(button.Tag?.ToString(), "machine_builder_phase5_continue", StringComparison.Ordinal));
            if (continueButton is not null)
            {
                var label = FindStudioVisualChildren<TextBlock>(continueButton).FirstOrDefault();
                if (label is not null)
                    label.Text = "COMPOSE  →";
                continueButton.Click += (_, _) => ShowMachineBuilderSemanticComposerV2();
            }

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
                delete.Click += (_, _) => DeleteSelectedComposerFunctionNode();
                actions.Children.Add(delete);
            }
        }

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Delete || _machineBuilderFunctionSurface?.Visibility != Visibility.Visible)
                return;
            DeleteSelectedComposerFunctionNode();
            e.Handled = true;
        };
    }

    private void DeleteSelectedComposerFunctionNode()
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

    private void ShowMachineBuilderSemanticComposerV2()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        ShowMachineBuilderPhase5(MachineBuilderPhase5Mode.Gui);
        if (_machineBuilderPhase5ModeHost is null)
            return;

        _composerV2Body ??= BuildComposerV2Body();
        _machineBuilderPhase5ModeHost.Children.Clear();
        _machineBuilderPhase5ModeHost.Children.Add(_composerV2Body);
        RefreshComposerV2();
    }

    private FrameworkElement BuildComposerV2Body()
    {
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(365) });

        var palette = BuildComposerV2Palette();
        Grid.SetColumn(palette, 0);
        body.Children.Add(palette);

        var canvas = BuildComposerV2Canvas();
        Grid.SetColumn(canvas, 2);
        body.Children.Add(canvas);

        var inspector = BuildComposerV2InspectorShell();
        Grid.SetColumn(inspector, 4);
        body.Children.Add(inspector);
        return body;
    }

    private Border BuildComposerV2Palette()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        root.Children.Add(ComposerV2Title("MACHINE BLOCKS", "Build many machine types from reusable semantic pieces."));

        var presets = new WrapPanel { Margin = new Thickness(0, 4, 0, 10) };
        AddComposerV2PresetButton(presets, "POWERED", ["item-input", "powered-processor", "item-output", "energy-storage"]);
        AddComposerV2PresetButton(presets, "FLUID", ["item-input", "fluid-tank", "fluid-processor", "item-output"]);
        AddComposerV2PresetButton(presets, "GENERATOR", ["fuel-slot", "generator-core", "energy-storage"]);
        AddComposerV2PresetButton(presets, "CONTROL", ["redstone-input", "redstone-controller", "branch"]);
        Grid.SetRow(presets, 1);
        root.Children.Add(presets);

        var stack = new StackPanel();
        foreach (var group in ComposerV2Blocks.GroupBy(spec => spec.Category))
        {
            stack.Children.Add(new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                Foreground = Brush("#72D5D3"),
                FontSize = 8.8,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2, 8, 2, 5)
            });
            foreach (var spec in group)
                stack.Children.Add(BuildComposerV2PaletteEntry(spec));
        }
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = stack
        };
        Grid.SetRow(scroll, 2);
        root.Children.Add(scroll);
        card.Child = root;
        return card;
    }

    private void AddComposerV2PresetButton(Panel parent, string label, string[] keys)
    {
        var button = CreateStudioChromeButton(label, 82, false);
        button.Height = 28;
        button.Margin = new Thickness(0, 0, 6, 6);
        button.Click += (_, _) => AddComposerV2Preset(keys);
        parent.Children.Add(button);
    }

    private Border BuildComposerV2PaletteEntry(ComposerV2BlockSpec spec)
    {
        var card = new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2E444C"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(9),
            Margin = new Thickness(0, 0, 0, 6),
            Cursor = Cursors.Hand,
            ToolTip = "Click or drag to add this block"
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = spec.Title,
            Foreground = Brush("#E7F0F2"),
            FontSize = 10.2,
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(new TextBlock
        {
            Text = spec.Subtitle,
            Foreground = Brush("#7D929A"),
            FontSize = 8.8,
            Margin = new Thickness(0, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        card.Child = panel;
        card.MouseLeftButtonUp += (_, _) => AddComposerV2Block(spec);
        card.MouseMove += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(card, "composer-v2:" + spec.Key, DragDropEffects.Copy);
        };
        return card;
    }

    private Border BuildComposerV2Canvas()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(ComposerV2Title("COMPOSITION", "Select blocks, connect compatible ports and preview the resulting behavior."));

        var drop = new Border
        {
            Background = Brush("#091014"),
            BorderBrush = Brush("#2A4149"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            AllowDrop = true,
            MinHeight = 430
        };
        drop.DragOver += (_, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        };
        drop.Drop += (_, e) =>
        {
            if (e.Data.GetData(DataFormats.StringFormat) is not string token || !token.StartsWith("composer-v2:", StringComparison.Ordinal))
                return;
            var spec = ComposerV2Blocks.FirstOrDefault(candidate => candidate.Key == token[12..]);
            if (spec is not null)
                AddComposerV2Block(spec);
        };
        _composerV2Canvas = new WrapPanel { Orientation = Orientation.Horizontal };
        drop.Child = _composerV2Canvas;
        Grid.SetRow(drop, 2);
        root.Children.Add(drop);

        _composerV2Summary = new TextBlock
        {
            Foreground = Brush("#7F939B"),
            FontSize = 9.2,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 9, 0, 0)
        };
        Grid.SetRow(_composerV2Summary, 3);
        root.Children.Add(_composerV2Summary);
        card.Child = root;
        return card;
    }

    private Border BuildComposerV2InspectorShell()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _composerV2Inspector = new StackPanel { Margin = new Thickness(15) };
        scroll.Content = _composerV2Inspector;
        card.Child = scroll;
        return card;
    }

    private void AddComposerV2Preset(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var spec = ComposerV2Blocks.FirstOrDefault(candidate => candidate.Key == key);
            if (spec is not null)
                AddComposerV2Block(spec, refresh: false);
        }
        ConnectComposerV2MachineEntry();
        MarkMachineBuilderPhase5Dirty();
        RefreshComposerV2();
    }

    private void AddComposerV2Block(ComposerV2BlockSpec spec, bool refresh = true)
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        var item = _machineBuilderLibraryLoad.Catalog.Find(spec.LibraryItemId, 1) ??
                   _machineBuilderLibraryLoad.Catalog.LatestItems.FirstOrDefault(candidate =>
                       candidate.Id.Equals(spec.LibraryItemId, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            SetMachineBuilderPhase5Status($"Library block '{spec.LibraryItemId}' is unavailable.", "#E18B8B");
            return;
        }

        AddMachineBuilderFunctionNode(item);
        var node = GetSelectedMachineBuilderFunctionNode();
        if (node is not null)
        {
            node.Properties["semanticAuthoring"] = JsonSerializer.SerializeToElement("composer-v2");
            node.Properties["semanticBlock"] = JsonSerializer.SerializeToElement(spec.Key);
            _composerV2SelectedNodeId = node.Id;
        }
        ConnectComposerV2MachineEntry();
        if (refresh)
            RefreshComposerV2();
    }

    private void ConnectComposerV2MachineEntry()
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        var entry = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
            node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return;
        var entryItem = _machineBuilderLibraryLoad.Catalog.Find(entry.LibraryItemId, entry.LibraryItemVersion);
        var flow = entryItem?.Ports.FirstOrDefault(port => port.Direction == ForeverStudioLibraryPortDirection.Output &&
                                                            port.DataType.Equals("flow", StringComparison.OrdinalIgnoreCase));
        if (flow is null)
            return;

        foreach (var node in _machineBuilderDocument.Behavior.Nodes.Where(candidate => candidate != entry))
        {
            var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            var flowIn = item?.Ports.FirstOrDefault(port => port.Direction == ForeverStudioLibraryPortDirection.Input &&
                                                            port.DataType.Equals("flow", StringComparison.OrdinalIgnoreCase));
            if (flowIn is null)
                continue;
            if (_machineBuilderDocument.Behavior.Connections.Any(connection =>
                    connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) &&
                    connection.ToPortId.Equals(flowIn.Id, StringComparison.OrdinalIgnoreCase)))
                continue;
            AddComposerV2Connection(entry, flow, node, flowIn);
            break;
        }
    }

    private void RefreshComposerV2()
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        RenderComposerV2Canvas();
        RenderComposerV2Inspector();
        if (_composerV2Summary is not null)
        {
            var authored = _machineBuilderDocument.Behavior.Nodes.Count(node =>
                ReadComposerV2String(node.Properties, "semanticAuthoring", string.Empty).Equals("composer-v2", StringComparison.OrdinalIgnoreCase));
            _composerV2Summary.Text =
                $"{_machineBuilderDocument.Behavior.Nodes.Count} behavior node(s) · {_machineBuilderDocument.Behavior.Connections.Count} connection(s) · " +
                $"{authored} Composer block(s) · {_machineBuilderDocument.Gui.Bindings.Count} GUI binding(s)";
        }
    }

    private void RenderComposerV2Canvas()
    {
        if (_composerV2Canvas is null || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        _composerV2Canvas.Children.Clear();
        var nodes = _machineBuilderDocument.Behavior.Nodes
            .Where(node => !node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => node.Y).ThenBy(node => node.X).ToList();
        if (nodes.Count == 0)
        {
            _composerV2Canvas.Children.Add(new TextBlock
            {
                Text = "Drop machine blocks here or choose one of the scenario presets.",
                Foreground = Brush("#72878F"),
                FontSize = 11,
                Margin = new Thickness(20, 90, 20, 90)
            });
            return;
        }

        foreach (var node in nodes)
        {
            var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is null)
                continue;
            _composerV2Canvas.Children.Add(BuildComposerV2NodeCard(node, item));
        }
    }

    private Border BuildComposerV2NodeCard(ForeverStudioMachineBehaviorNode node, ForeverStudioLibraryItem item)
    {
        var selected = node.Id.Equals(_composerV2SelectedNodeId, StringComparison.OrdinalIgnoreCase);
        var card = new Border
        {
            Width = 210,
            MinHeight = 112,
            Background = Brush(selected ? "#173438" : "#111A1F"),
            BorderBrush = Brush(selected ? "#69CCCB" : "#344B54"),
            BorderThickness = new Thickness(selected ? 1.5 : 1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10),
            Margin = new Thickness(6),
            Cursor = Cursors.Hand
        };
        var root = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock
        {
            Text = ReadMachineBuilderFunctionNodeLabel(node, item),
            Foreground = Brush("#EDF4F5"),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        var delete = CreateStudioChromeButton("×", 26, false);
        delete.Height = 26;
        delete.ToolTip = "Delete block and its connections";
        delete.Click += (_, e) =>
        {
            e.Handled = true;
            DeleteMachineBuilderFunctionNode(node);
            _composerV2SelectedNodeId = null;
            RefreshComposerV2();
        };
        Grid.SetColumn(delete, 1);
        header.Children.Add(delete);
        root.Children.Add(header);
        root.Children.Add(new TextBlock
        {
            Text = item.Category,
            Foreground = Brush("#72D5D3"),
            FontSize = 8.2,
            Margin = new Thickness(0, 3, 0, 0)
        });
        root.Children.Add(new TextBlock
        {
            Text = ComposerV2NodeSummary(node, item),
            Foreground = Brush("#9FB1B7"),
            FontSize = 8.8,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 0)
        });

        var outgoing = _machineBuilderDocument?.Behavior.Connections
            .Where(connection => connection.FromNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
        if (outgoing.Count > 0)
        {
            root.Children.Add(new TextBlock
            {
                Text = string.Join("\n", outgoing.Take(3).Select(connection => "→ " + ComposerV2NodeName(connection.ToNodeId))),
                Foreground = Brush("#62C8C7"),
                FontSize = 8.3,
                Margin = new Thickness(0, 7, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        card.Child = root;
        card.MouseLeftButtonUp += (_, _) =>
        {
            _composerV2SelectedNodeId = node.Id;
            _machineBuilderFunctionSelectedNodeId = node.Id;
            RenderComposerV2Canvas();
            RenderComposerV2Inspector();
        };
        return card;
    }

    private string ComposerV2NodeSummary(ForeverStudioMachineBehaviorNode node, ForeverStudioLibraryItem item)
    {
        if (node.LibraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) || node.LibraryItemId.Contains("generator-core", StringComparison.OrdinalIgnoreCase))
        {
            var ticks = ReadComposerV2Int(node.Properties, "durationTicks", 100);
            var fe = ReadComposerV2Int(node.Properties, "energyPerTick", 0);
            return fe > 0 ? $"{ticks} ticks · {fe} FE/t" : $"{ticks} ticks";
        }
        if (node.LibraryItemId.Contains("energy-storage", StringComparison.OrdinalIgnoreCase))
            return $"{ReadComposerV2Int(node.Properties, "capacity", 100000):N0} FE capacity";
        if (node.LibraryItemId.Contains("fluid", StringComparison.OrdinalIgnoreCase))
            return $"{ReadComposerV2Int(node.Properties, "capacityMb", 4000):N0} mB capacity";
        if (node.LibraryItemId.Contains("item", StringComparison.OrdinalIgnoreCase) || node.LibraryItemId.Contains("slot", StringComparison.OrdinalIgnoreCase))
            return ReadComposerV2String(node.Properties, "item", "Any item");
        return item.Description;
    }

    private void RenderComposerV2Inspector()
    {
        if (_composerV2Inspector is null || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        var panel = _composerV2Inspector;
        panel.Children.Clear();
        panel.Children.Add(ComposerV2Title("BLOCK INSPECTOR", "Configure the selected block without editing JSON."));

        var node = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate =>
            candidate.Id.Equals(_composerV2SelectedNodeId, StringComparison.OrdinalIgnoreCase));
        if (node is null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Select a block on the composition canvas.",
                Foreground = Brush("#869AA2"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });
            AddComposerV2GlobalActions(panel);
            return;
        }
        var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
        if (item is null)
            return;

        panel.Children.Add(new TextBlock
        {
            Text = item.DisplayName,
            Foreground = Brush("#EFF5F6"),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 3, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = item.Description,
            Foreground = Brush("#889CA4"),
            FontSize = 9.4,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 8)
        });

        AddComposerV2StringField(panel, node, "label", "Display label", item.DisplayName);

        if (node.LibraryItemId.Contains("item", StringComparison.OrdinalIgnoreCase) ||
            node.LibraryItemId.Contains("slot", StringComparison.OrdinalIgnoreCase))
        {
            AddComposerV2StringField(panel, node, "item", "Item / filter", "minecraft:stone");
            AddComposerV2IntField(panel, node, "count", "Stack amount", 1, 1, 64);
        }
        if (node.LibraryItemId.Equals("function.item-storage", StringComparison.OrdinalIgnoreCase))
            AddComposerV2IntField(panel, node, "capacitySlots", "Storage slots", 9, 1, 256);

        if (node.LibraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
            node.LibraryItemId.Equals("function.generator-core", StringComparison.OrdinalIgnoreCase))
        {
            AddComposerV2IntField(panel, node, "durationTicks", "Duration (ticks)", 100, 1, 1_000_000);
            AddComposerV2StringField(panel, node, "recipeGroup", "Recipe group", "default");
        }
        if (node.LibraryItemId.Equals("function.powered-processor", StringComparison.OrdinalIgnoreCase) ||
            node.LibraryItemId.Equals("function.generator-core", StringComparison.OrdinalIgnoreCase) ||
            node.LibraryItemId.Contains("energy", StringComparison.OrdinalIgnoreCase))
        {
            AddComposerV2IntField(panel, node, "energyPerTick", "FE / tick", 40, 0, 1_000_000);
        }
        if (node.LibraryItemId.Equals("function.energy-storage", StringComparison.OrdinalIgnoreCase))
        {
            AddComposerV2IntField(panel, node, "capacity", "Capacity (FE)", 100000, 1, int.MaxValue);
            AddComposerV2IntField(panel, node, "maxReceive", "Max receive (FE/t)", 4000, 0, int.MaxValue);
            AddComposerV2IntField(panel, node, "maxExtract", "Max extract (FE/t)", 0, 0, int.MaxValue);
        }
        if (node.LibraryItemId.Contains("fluid", StringComparison.OrdinalIgnoreCase))
        {
            AddComposerV2StringField(panel, node, "fluid", "Fluid", "minecraft:water");
            AddComposerV2IntField(panel, node, "amountMb", "Amount (mB)", 1000, 0, int.MaxValue);
            if (node.LibraryItemId.Equals("function.fluid-tank", StringComparison.OrdinalIgnoreCase))
                AddComposerV2IntField(panel, node, "capacityMb", "Tank capacity (mB)", 4000, 1, int.MaxValue);
        }
        if (node.LibraryItemId.Contains("redstone", StringComparison.OrdinalIgnoreCase))
            AddComposerV2StringField(panel, node, "redstoneMode", "Redstone mode", "enabled_when_powered");
        if (node.LibraryItemId.Equals("function.timer", StringComparison.OrdinalIgnoreCase))
            AddComposerV2IntField(panel, node, "durationTicks", "Timer ticks", 20, 1, 1_000_000);
        if (node.LibraryItemId.Equals("function.set-state", StringComparison.OrdinalIgnoreCase))
            AddComposerV2StringField(panel, node, "state", "Target state", "running");

        AddComposerV2Connections(panel, node, item);
        panel.Children.Add(BuildComposerV2Preview(node, item));
        AddComposerV2GlobalActions(panel);
    }

    private void AddComposerV2StringField(Panel panel, ForeverStudioMachineBehaviorNode node, string key, string label, string fallback)
    {
        panel.Children.Add(MachineBuilderVisualInspectorLabel(label, 10));
        var box = MachineBuilderTextBox();
        box.Height = 32;
        box.Text = ReadComposerV2String(node.Properties, key, fallback);
        box.LostKeyboardFocus += (_, _) =>
        {
            var value = box.Text.Trim();
            node.Properties[key] = JsonSerializer.SerializeToElement(value);
            MarkMachineBuilderPhase5Dirty();
            RefreshComposerV2();
        };
        panel.Children.Add(box);
    }

    private void AddComposerV2IntField(
        Panel panel,
        ForeverStudioMachineBehaviorNode node,
        string key,
        string label,
        int fallback,
        int min,
        int max)
    {
        panel.Children.Add(MachineBuilderVisualInspectorLabel(label, 10));
        var box = MachineBuilderTextBox();
        box.Height = 32;
        box.Text = ReadComposerV2Int(node.Properties, key, fallback).ToString(CultureInfo.InvariantCulture);
        box.LostKeyboardFocus += (_, _) =>
        {
            if (!int.TryParse(box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                value = fallback;
            value = Math.Clamp(value, min, max);
            box.Text = value.ToString(CultureInfo.InvariantCulture);
            node.Properties[key] = JsonSerializer.SerializeToElement(value);
            MarkMachineBuilderPhase5Dirty();
            RefreshComposerV2();
        };
        panel.Children.Add(box);
    }

    private void AddComposerV2Connections(Panel panel, ForeverStudioMachineBehaviorNode node, ForeverStudioLibraryItem item)
    {
        panel.Children.Add(MachineBuilderVisualInspectorLabel("Connections", 16));
        var outputs = item.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Output).ToList();
        if (outputs.Count > 0)
        {
            var source = new ComboBox
            {
                Height = 32,
                Background = Brush("#10191E"),
                Foreground = Brush("#E5EEF0"),
                BorderBrush = Brush("#354A53"),
                ItemsSource = outputs,
                DisplayMemberPath = nameof(ForeverStudioLibraryPort.DisplayName),
                SelectedIndex = 0
            };
            panel.Children.Add(source);

            var target = new ComboBox
            {
                Height = 32,
                Background = Brush("#10191E"),
                Foreground = Brush("#E5EEF0"),
                BorderBrush = Brush("#354A53"),
                Margin = new Thickness(0, 6, 0, 0)
            };
            void RefreshTargets()
            {
                target.ItemsSource = source.SelectedItem is ForeverStudioLibraryPort output
                    ? BuildComposerV2Targets(node, output)
                    : [];
                target.SelectedIndex = target.Items.Count > 0 ? 0 : -1;
            }
            source.SelectionChanged += (_, _) => RefreshTargets();
            RefreshTargets();
            panel.Children.Add(target);

            var connect = CreateStudioChromeButton("CONNECT BLOCKS", double.NaN, true);
            connect.Height = 34;
            connect.HorizontalAlignment = HorizontalAlignment.Stretch;
            connect.Margin = new Thickness(0, 7, 0, 0);
            connect.Click += (_, _) =>
            {
                if (source.SelectedItem is ForeverStudioLibraryPort output && target.SelectedItem is ComposerV2Target destination)
                {
                    AddComposerV2Connection(node, output, destination.Node, destination.Port);
                    MarkMachineBuilderPhase5Dirty();
                    RefreshComposerV2();
                }
                else
                {
                    SetMachineBuilderPhase5Status("No compatible target port is available for that output.", "#D8B26A");
                }
            };
            panel.Children.Add(connect);
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = "This block has no outgoing ports.",
                Foreground = Brush("#71858E"),
                FontSize = 9.2
            });
        }

        var connections = _machineBuilderDocument?.Behavior.Connections
            .Where(connection => connection.FromNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) ||
                                 connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
        foreach (var connection in connections)
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock
            {
                Text = $"{ComposerV2NodeName(connection.FromNodeId)} → {ComposerV2NodeName(connection.ToNodeId)}",
                Foreground = Brush("#AFC0C5"),
                FontSize = 8.8,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });
            var remove = CreateStudioChromeButton("×", 28, false);
            remove.Height = 28;
            remove.ToolTip = "Remove connection";
            remove.Click += (_, _) =>
            {
                RemoveMachineBuilderFunctionConnection(connection);
                RefreshComposerV2();
            };
            Grid.SetColumn(remove, 1);
            row.Children.Add(remove);
            panel.Children.Add(row);
        }
    }

    private List<ComposerV2Target> BuildComposerV2Targets(ForeverStudioMachineBehaviorNode sourceNode, ForeverStudioLibraryPort output)
    {
        var result = new List<ComposerV2Target>();
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return result;
        foreach (var node in _machineBuilderDocument.Behavior.Nodes.Where(node => node != sourceNode))
        {
            var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is null)
                continue;
            foreach (var port in item.Ports.Where(port => port.Direction == ForeverStudioLibraryPortDirection.Input &&
                                                          _machineBuilderLibraryLoad.Catalog.ArePortsCompatible(output, port)))
            {
                var occupied = !port.Multiple && _machineBuilderDocument.Behavior.Connections.Any(connection =>
                    connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase) &&
                    connection.ToPortId.Equals(port.Id, StringComparison.OrdinalIgnoreCase));
                if (!occupied)
                    result.Add(new ComposerV2Target(node, port, $"{ReadMachineBuilderFunctionNodeLabel(node, item)} · {port.DisplayName} ({port.DataType})"));
            }
        }
        return result;
    }

    private void AddComposerV2Connection(
        ForeverStudioMachineBehaviorNode fromNode,
        ForeverStudioLibraryPort fromPort,
        ForeverStudioMachineBehaviorNode toNode,
        ForeverStudioLibraryPort toPort)
    {
        if (_machineBuilderDocument is null)
            return;
        if (_machineBuilderDocument.Behavior.Connections.Any(connection =>
                connection.FromNodeId.Equals(fromNode.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.FromPortId.Equals(fromPort.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.ToNodeId.Equals(toNode.Id, StringComparison.OrdinalIgnoreCase) &&
                connection.ToPortId.Equals(toPort.Id, StringComparison.OrdinalIgnoreCase)))
            return;
        var used = _machineBuilderDocument.Behavior.Connections.Select(connection => connection.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = SanitizeMachineBuilderGraphId($"link_{fromNode.Id}_{fromPort.Id}_{toNode.Id}_{toPort.Id}");
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
        _machineBuilderDocument.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
        {
            Id = id,
            FromNodeId = fromNode.Id,
            FromPortId = fromPort.Id,
            ToNodeId = toNode.Id,
            ToPortId = toPort.Id
        });
    }

    private Border BuildComposerV2Preview(ForeverStudioMachineBehaviorNode node, ForeverStudioLibraryItem item)
    {
        var card = new Border
        {
            Background = Brush("#0A1115"),
            BorderBrush = Brush("#284148"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11),
            Margin = new Thickness(0, 16, 0, 0)
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "BEHAVIOR PREVIEW",
            Foreground = Brush("#72D5D3"),
            FontSize = 9.2,
            FontWeight = FontWeights.Bold
        });
        _composerV2PreviewText = new TextBlock
        {
            Text = ComposerV2PreviewDescription(node, item),
            Foreground = Brush("#B9C8CC"),
            FontSize = 9.2,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        panel.Children.Add(_composerV2PreviewText);
        _composerV2PreviewProgress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = _composerV2Progress * 100,
            Height = 11,
            Margin = new Thickness(0, 9, 0, 0),
            Background = Brush("#15242A"),
            Foreground = Brush("#63C9C6"),
            BorderThickness = new Thickness(0),
            Visibility = IsComposerV2TimedNode(node) ? Visibility.Visible : Visibility.Collapsed
        };
        panel.Children.Add(_composerV2PreviewProgress);
        if (IsComposerV2TimedNode(node))
        {
            var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var play = CreateStudioChromeButton("▶", 42, true);
            play.Height = 28;
            play.Click += (_, _) => StartComposerV2Preview();
            controls.Children.Add(play);
            var pause = CreateStudioChromeButton("Ⅱ", 42, false);
            pause.Height = 28;
            pause.Margin = new Thickness(6, 0, 0, 0);
            pause.Click += (_, _) => PauseComposerV2Preview();
            controls.Children.Add(pause);
            var reset = CreateStudioChromeButton("RESET", 62, false);
            reset.Height = 28;
            reset.Margin = new Thickness(6, 0, 0, 0);
            reset.Click += (_, _) => ResetComposerV2Preview();
            controls.Children.Add(reset);
            panel.Children.Add(controls);
        }
        card.Child = panel;
        return card;
    }

    private string ComposerV2PreviewDescription(ForeverStudioMachineBehaviorNode node, ForeverStudioLibraryItem item)
    {
        var incoming = _machineBuilderDocument?.Behavior.Connections.Count(connection => connection.ToNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase)) ?? 0;
        var outgoing = _machineBuilderDocument?.Behavior.Connections.Count(connection => connection.FromNodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase)) ?? 0;
        if (IsComposerV2TimedNode(node))
        {
            var duration = ReadComposerV2Int(node.Properties, "durationTicks", 100);
            var energy = ReadComposerV2Int(node.Properties, "energyPerTick", 0);
            var state = _composerV2Progress >= 1 ? "COMPLETE" : _composerV2Running ? "RUNNING" : _composerV2Progress > 0 ? "PAUSED" : "IDLE";
            return $"{state} · {_composerV2Progress * 100:0}% · {duration / 20d:0.##} s" +
                   (energy > 0 ? $" · {energy:N0} FE/t · {(long)duration * energy:N0} FE/cycle" : string.Empty) +
                   $"\n{incoming} incoming · {outgoing} outgoing connection(s)";
        }
        return $"{item.DisplayName} · {incoming} incoming · {outgoing} outgoing connection(s)\n{ComposerV2NodeSummary(node, item)}";
    }

    private void StartComposerV2Preview()
    {
        _composerV2Timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _composerV2Timer.Tick -= ComposerV2PreviewTick;
        _composerV2Timer.Tick += ComposerV2PreviewTick;
        _composerV2Running = true;
        _composerV2Timer.Start();
        RenderComposerV2Inspector();
    }

    private void PauseComposerV2Preview()
    {
        _composerV2Running = false;
        _composerV2Timer?.Stop();
        RenderComposerV2Inspector();
    }

    private void ResetComposerV2Preview()
    {
        _composerV2Running = false;
        _composerV2Timer?.Stop();
        _composerV2Progress = 0;
        RenderComposerV2Inspector();
    }

    private void ComposerV2PreviewTick(object? sender, EventArgs e)
    {
        if (!_composerV2Running || _machineBuilderDocument is null)
            return;
        var node = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate =>
            candidate.Id.Equals(_composerV2SelectedNodeId, StringComparison.OrdinalIgnoreCase));
        var duration = node is null ? 100 : Math.Max(1, ReadComposerV2Int(node.Properties, "durationTicks", 100));
        _composerV2Progress += 2d / duration;
        if (_composerV2Progress >= 1)
        {
            _composerV2Progress = 1;
            _composerV2Running = false;
            _composerV2Timer?.Stop();
        }
        if (_composerV2PreviewProgress is not null)
            _composerV2PreviewProgress.Value = _composerV2Progress * 100;
        if (_composerV2PreviewText is not null && node is not null && _machineBuilderLibraryLoad is not null)
        {
            var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            if (item is not null)
                _composerV2PreviewText.Text = ComposerV2PreviewDescription(node, item);
        }
    }

    private static bool IsComposerV2TimedNode(ForeverStudioMachineBehaviorNode node)
        => node.LibraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
           node.LibraryItemId.Equals("function.generator-core", StringComparison.OrdinalIgnoreCase) ||
           node.LibraryItemId.Equals("function.timer", StringComparison.OrdinalIgnoreCase) ||
           node.LibraryItemId.Equals("function.process-progress", StringComparison.OrdinalIgnoreCase);

    private void AddComposerV2GlobalActions(Panel panel)
    {
        var gui = CreateStudioChromeButton("AUTO-BUILD GUI BINDINGS", double.NaN, false);
        gui.Height = 34;
        gui.HorizontalAlignment = HorizontalAlignment.Stretch;
        gui.Margin = new Thickness(0, 16, 0, 0);
        gui.Click += (_, _) => GenerateComposerV2GuiBindings();
        panel.Children.Add(gui);

        var advanced = CreateStudioChromeButton("OPEN ADVANCED GRAPH", double.NaN, false);
        advanced.Height = 34;
        advanced.HorizontalAlignment = HorizontalAlignment.Stretch;
        advanced.Margin = new Thickness(0, 7, 0, 0);
        advanced.Click += (_, _) =>
        {
            _composerV2Running = false;
            _composerV2Timer?.Stop();
            HideMachineBuilderPhase5Surface();
            ShowMachineBuilderFunctionPhase4();
        };
        panel.Children.Add(advanced);
    }

    private void GenerateComposerV2GuiBindings()
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        _machineBuilderDocument.Gui.ScreenId = string.IsNullOrWhiteSpace(_machineBuilderDocument.Gui.ScreenId)
            ? $"{_machineBuilderDocument.AssetId}_screen"
            : _machineBuilderDocument.Gui.ScreenId;
        _machineBuilderDocument.Gui.Bindings.RemoveAll(binding =>
            ReadComposerV2String(binding.Properties, "semanticOwner", string.Empty).Equals("composer-v2", StringComparison.OrdinalIgnoreCase));

        var order = 0;
        foreach (var node in _machineBuilderDocument.Behavior.Nodes)
        {
            if (node.LibraryItemId.Equals("function.item-input", StringComparison.OrdinalIgnoreCase) ||
                node.LibraryItemId.Equals("function.filter-slot", StringComparison.OrdinalIgnoreCase) ||
                node.LibraryItemId.Equals("function.catalyst-slot", StringComparison.OrdinalIgnoreCase) ||
                node.LibraryItemId.Equals("function.fuel-slot", StringComparison.OrdinalIgnoreCase))
                AddComposerV2GuiBinding(node, "asset.input-tray", "item", "item-slot", "item", order++);
            else if (node.LibraryItemId.Equals("function.item-output", StringComparison.OrdinalIgnoreCase))
                AddComposerV2GuiBinding(node, "asset.output-tray", "stored", "output-slot", "item", order++, "capability", "stored-item");
            else if (node.LibraryItemId.Equals("function.item-storage", StringComparison.OrdinalIgnoreCase))
                AddComposerV2GuiBinding(node, "asset.input-tray", "stored", "item-slot", "item", order++);
            else if (node.LibraryItemId.Equals("function.energy-storage", StringComparison.OrdinalIgnoreCase))
                AddComposerV2GuiBinding(node, "asset.screen", "ratio", "energy-bar", "number", order++);
            else if (node.LibraryItemId.Equals("function.fluid-tank", StringComparison.OrdinalIgnoreCase))
                AddComposerV2GuiBinding(node, "asset.fluid-window", "ratio", "fluid-gauge", "number", order++);
            else if (node.LibraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
                     node.LibraryItemId.Equals("function.generator-core", StringComparison.OrdinalIgnoreCase))
            {
                var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
                if (item?.Ports.Any(port => port.Id.Equals("progress", StringComparison.OrdinalIgnoreCase) && port.Direction == ForeverStudioLibraryPortDirection.Output) == true)
                    AddComposerV2GuiBinding(node, "asset.screen", "progress", "progress-bar", "number", order++);
                if (item?.Ports.Any(port => port.Id.Equals("running", StringComparison.OrdinalIgnoreCase) && port.Direction == ForeverStudioLibraryPortDirection.Output) == true)
                    AddComposerV2GuiBinding(node, "asset.status-lamp", "running", "status", "boolean", order++);
            }
        }
        MarkMachineBuilderPhase5Dirty();
        RefreshComposerV2();
        SetMachineBuilderPhase5Status($"Generated {order} semantic GUI binding(s) from the current composition.", "#74C99A");
    }

    private void AddComposerV2GuiBinding(
        ForeverStudioMachineBehaviorNode node,
        string asset,
        string port,
        string widget,
        string type,
        int order,
        string sourceKind = "graph-port",
        string? capability = null)
    {
        if (_machineBuilderDocument is null)
            return;
        var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["widget"] = JsonSerializer.SerializeToElement(widget),
            ["sourcePortId"] = JsonSerializer.SerializeToElement(port),
            ["sourceType"] = JsonSerializer.SerializeToElement(type),
            ["label"] = JsonSerializer.SerializeToElement(ReadComposerV2String(node.Properties, "label", ComposerV2NodeName(node.Id))),
            ["order"] = JsonSerializer.SerializeToElement(order),
            ["semanticOwner"] = JsonSerializer.SerializeToElement("composer-v2"),
            ["sourceKind"] = JsonSerializer.SerializeToElement(sourceKind)
        };
        if (!string.IsNullOrWhiteSpace(capability))
            properties["sourceCapability"] = JsonSerializer.SerializeToElement(capability);
        _machineBuilderDocument.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
        {
            Id = SanitizeMachineBuilderGraphId($"composer_gui_{node.Id}_{widget}"),
            LibraryItemId = asset,
            LibraryItemVersion = 1,
            SourceId = node.Id,
            Properties = properties
        });
    }

    private string ComposerV2NodeName(string nodeId)
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return nodeId;
        var node = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate => candidate.Id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        if (node is null)
            return nodeId;
        var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
        return item is null ? nodeId : ReadMachineBuilderFunctionNodeLabel(node, item);
    }

    private static string ReadComposerV2String(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static int ReadComposerV2Int(Dictionary<string, JsonElement> properties, string key, int fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;

    private static TextBlock ComposerV2Title(string title, string subtitle)
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
}
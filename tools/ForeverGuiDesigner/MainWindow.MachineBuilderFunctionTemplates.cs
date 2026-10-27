using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Curated editable behavior templates for the Functions step. Templates are authoring shortcuts:
/// they materialize normal Behavior nodes/connections and optional GUI starter bindings, so the
/// result remains fully editable in Functions and GUI Designer.
/// </summary>
public partial class MainWindow
{
    private sealed record FunctionTemplateNode(
        string Key,
        string LibraryItemId,
        string Label,
        double X,
        double Y);

    private sealed record FunctionTemplateConnection(
        string FromKey,
        string FromPort,
        string ToKey,
        string ToPort);

    private sealed record FunctionTemplateGui(
        string NodeKey,
        string PortId,
        string Widget,
        string Label,
        double X,
        double Y,
        double Width,
        double Height,
        string PreviewValue);

    private sealed record FunctionLogicTemplate(
        string Id,
        string Category,
        string Title,
        string Description,
        string[] Tags,
        FunctionTemplateNode[] Nodes,
        FunctionTemplateConnection[] Connections,
        FunctionTemplateGui[] Gui)
    {
        public override string ToString() => Title;
    }

    private static FunctionTemplateNode TN(string key, string libraryItemId, string label, double x, double y)
        => new(key, libraryItemId, label, x, y);

    private static FunctionTemplateConnection TC(string fromKey, string fromPort, string toKey, string toPort)
        => new(fromKey, fromPort, toKey, toPort);

    private static FunctionTemplateGui TG(
        string nodeKey,
        string portId,
        string widget,
        string label,
        double x,
        double y,
        double width,
        double height,
        string previewValue)
        => new(nodeKey, portId, widget, label, x, y, width, height, previewValue);

    private static readonly FunctionLogicTemplate[] FunctionLogicTemplates =
    [
        new(
            "standard-processor", "Processing", "Standard Item Processor",
            "Single item input, editable recipe processor and one output buffer. A clean starting point for furnaces, crushers and cutters.",
            ["item", "recipe", "basic"],
            [
                TN("input", "function.item-input", "Input", 90, 260),
                TN("processor", "function.standard-processor", "Processor", 390, 180),
                TN("output", "function.item-storage", "Output", 700, 260)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 35, 52, 18, 18, "minecraft:iron_ingot|1"),
                TG("processor", "progress", "progress-bar", "Progress", 70, 54, 38, 14, "0.65"),
                TG("output", "stored", "output-slot", "Output", 123, 52, 18, 18, "minecraft:gold_ingot|1"),
                TG("processor", "running", "status", "Running", 82, 30, 12, 12, "true")
            ]),

        new(
            "powered-processor", "Processing", "Powered Item Processor",
            "Standard item processing plus FE storage. Suitable for most electrically powered machines.",
            ["item", "recipe", "energy", "fe"],
            [
                TN("input", "function.item-input", "Input", 80, 260),
                TN("processor", "function.powered-processor", "Powered Processor", 390, 180),
                TN("output", "function.item-storage", "Output", 700, 260),
                TN("energy", "function.energy-storage", "Energy Storage", 390, 430)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 28, 54, 18, 18, "minecraft:iron_ingot|1"),
                TG("processor", "progress", "progress-bar", "Progress", 68, 56, 42, 14, "0.65"),
                TG("output", "stored", "output-slot", "Output", 127, 54, 18, 18, "minecraft:gold_ingot|1"),
                TG("energy", "ratio", "energy-bar", "Energy", 154, 31, 12, 54, "0.72"),
                TG("processor", "running", "status", "Running", 82, 32, 12, 12, "true")
            ]),

        new(
            "dual-input-processor", "Processing", "Dual Input Processor",
            "Two independent item inputs feeding one powered process and one result buffer.",
            ["item", "dual-input", "recipe", "energy"],
            [
                TN("input_a", "function.item-input", "Primary Input", 60, 230),
                TN("input_b", "function.item-input", "Secondary Input", 60, 390),
                TN("processor", "function.powered-processor", "Powered Processor", 390, 180),
                TN("output", "function.item-storage", "Output", 720, 260),
                TN("energy", "function.energy-storage", "Energy Storage", 390, 430)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input_a", "item", "item-slot", "Input A", 27, 45, 18, 18, "minecraft:iron_ingot|1"),
                TG("input_b", "item", "item-slot", "Input B", 27, 72, 18, 18, "minecraft:redstone|1"),
                TG("processor", "progress", "progress-bar", "Progress", 67, 58, 42, 14, "0.50"),
                TG("output", "stored", "output-slot", "Output", 127, 58, 18, 18, "minecraft:gold_ingot|1"),
                TG("energy", "ratio", "energy-bar", "Energy", 154, 34, 12, 58, "0.70")
            ]),

        new(
            "catalyst-processor", "Processing", "Catalyst Processor",
            "Powered processing with a dedicated non-primary catalyst slot that can later be configured per recipe.",
            ["item", "catalyst", "recipe", "energy"],
            [
                TN("input", "function.item-input", "Input", 60, 230),
                TN("catalyst", "function.catalyst-slot", "Catalyst", 60, 390),
                TN("processor", "function.powered-processor", "Powered Processor", 390, 180),
                TN("output", "function.item-storage", "Output", 720, 260),
                TN("energy", "function.energy-storage", "Energy Storage", 390, 430)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 27, 48, 18, 18, "minecraft:iron_ingot|1"),
                TG("catalyst", "item", "item-slot", "Catalyst", 27, 76, 18, 18, "minecraft:blaze_powder|1"),
                TG("processor", "progress", "progress-bar", "Progress", 67, 60, 42, 14, "0.55"),
                TG("output", "stored", "output-slot", "Output", 127, 60, 18, 18, "minecraft:gold_ingot|1"),
                TG("energy", "ratio", "energy-bar", "Energy", 154, 36, 12, 56, "0.68")
            ]),

        new(
            "upgradeable-processor", "Processing", "Upgradeable Processor",
            "Powered processor with a dedicated upgrade inventory exposed for later speed, efficiency or capacity modifiers.",
            ["item", "upgrade", "recipe", "energy"],
            [
                TN("input", "function.item-input", "Input", 60, 230),
                TN("upgrade", "function.upgrade-slot", "Upgrade Slot", 60, 410),
                TN("processor", "function.powered-processor", "Powered Processor", 390, 180),
                TN("output", "function.item-storage", "Output", 720, 260),
                TN("energy", "function.energy-storage", "Energy Storage", 390, 430)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 28, 54, 18, 18, "minecraft:iron_ingot|1"),
                TG("upgrade", "stored", "item-slot", "Upgrade", 28, 91, 18, 18, "minecraft:redstone|1"),
                TG("processor", "progress", "progress-bar", "Progress", 68, 56, 42, 14, "0.65"),
                TG("output", "stored", "output-slot", "Output", 127, 54, 18, 18, "minecraft:gold_ingot|1"),
                TG("energy", "ratio", "energy-bar", "Energy", 154, 31, 12, 62, "0.72")
            ]),

        new(
            "multi-output-processor", "Processing", "Dual Output Processor",
            "Powered process with primary and secondary output buffers. Useful for by-products and split results.",
            ["item", "multi-output", "byproduct", "energy"],
            [
                TN("input", "function.item-input", "Input", 60, 270),
                TN("processor", "function.powered-processor", "Powered Processor", 370, 180),
                TN("output_a", "function.item-storage", "Primary Output", 700, 220),
                TN("output_b", "function.item-storage", "Secondary Output", 700, 390),
                TN("energy", "function.energy-storage", "Energy Storage", 370, 450)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 28, 60, 18, 18, "minecraft:iron_ingot|1"),
                TG("processor", "progress", "progress-bar", "Progress", 68, 62, 38, 14, "0.60"),
                TG("output_a", "stored", "output-slot", "Primary", 122, 48, 18, 18, "minecraft:gold_ingot|1"),
                TG("output_b", "stored", "output-slot", "Secondary", 122, 76, 18, 18, "minecraft:redstone|1"),
                TG("energy", "ratio", "energy-bar", "Energy", 154, 36, 12, 58, "0.75")
            ]),

        new(
            "fluid-processor", "Processing", "Fluid Processor",
            "Item plus fluid processing skeleton with a tank, item input and editable result buffer.",
            ["item", "fluid", "recipe"],
            [
                TN("input", "function.item-input", "Item Input", 60, 250),
                TN("tank", "function.fluid-tank", "Process Tank", 60, 430),
                TN("processor", "function.fluid-processor", "Fluid Processor", 390, 180),
                TN("output", "function.item-storage", "Output", 720, 260)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 28, 56, 18, 18, "minecraft:iron_ingot|1"),
                TG("processor", "progress", "progress-bar", "Progress", 67, 58, 40, 14, "0.50"),
                TG("output", "stored", "output-slot", "Output", 123, 56, 18, 18, "minecraft:gold_ingot|1"),
                TG("tank", "ratio", "fluid-gauge", "Fluid", 153, 34, 13, 62, "0.55")
            ]),

        new(
            "dual-tank-processor", "Processing", "Dual Tank Processor",
            "Fluid process with two independently visible tanks for mixing, washing or chemical-style machines.",
            ["fluid", "dual-tank", "mixing", "recipe"],
            [
                TN("input", "function.item-input", "Item Input", 50, 250),
                TN("tank_a", "function.fluid-tank", "Tank A", 50, 430),
                TN("processor", "function.fluid-processor", "Fluid Processor", 390, 180),
                TN("tank_b", "function.fluid-tank", "Tank B", 720, 430),
                TN("output", "function.item-storage", "Output", 720, 250)
            ],
            [TC("$entry", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 35, 58, 18, 18, "minecraft:iron_ingot|1"),
                TG("tank_a", "ratio", "fluid-gauge", "Tank A", 12, 35, 13, 62, "0.65"),
                TG("processor", "progress", "progress-bar", "Progress", 68, 60, 40, 14, "0.45"),
                TG("output", "stored", "output-slot", "Output", 123, 58, 18, 18, "minecraft:gold_ingot|1"),
                TG("tank_b", "ratio", "fluid-gauge", "Tank B", 151, 35, 13, 62, "0.35")
            ]),

        new(
            "basic-generator", "Energy", "Fuel Generator",
            "Fuel slot, generator core and FE storage with a ready-to-edit generator GUI.",
            ["generator", "fuel", "energy"],
            [
                TN("fuel", "function.fuel-slot", "Fuel", 80, 280),
                TN("generator", "function.generator-core", "Generator Core", 390, 180),
                TN("energy", "function.energy-storage", "Energy Storage", 710, 280)
            ],
            [TC("$entry", "flow", "generator", "flow-in")],
            [
                TG("fuel", "item", "item-slot", "Fuel", 35, 61, 18, 18, "minecraft:coal|1"),
                TG("generator", "progress", "progress-bar", "Burn", 70, 63, 42, 14, "0.40"),
                TG("generator", "running", "status", "Running", 84, 38, 12, 12, "true"),
                TG("energy", "ratio", "energy-bar", "Energy", 151, 35, 13, 64, "0.72")
            ]),

        new(
            "manual-generator", "Energy", "Player-Triggered Generator",
            "Generator cycle started by player interaction instead of the default machine entry flow.",
            ["generator", "manual", "use", "energy"],
            [
                TN("use", "function.on-use", "On Player Use", 70, 160),
                TN("fuel", "function.fuel-slot", "Fuel", 70, 360),
                TN("generator", "function.generator-core", "Generator Core", 400, 190),
                TN("energy", "function.energy-storage", "Energy Storage", 720, 300)
            ],
            [TC("use", "flow", "generator", "flow-in")],
            [
                TG("fuel", "item", "item-slot", "Fuel", 35, 61, 18, 18, "minecraft:coal|1"),
                TG("generator", "progress", "progress-bar", "Cycle", 70, 63, 42, 14, "0.40"),
                TG("energy", "ratio", "energy-bar", "Energy", 151, 35, 13, 64, "0.72")
            ]),

        new(
            "redstone-generator", "Energy", "Redstone-Gated Generator",
            "Fuel generator that only receives execution flow while the reusable redstone controller is enabled.",
            ["generator", "redstone", "control", "energy"],
            [
                TN("redstone", "function.redstone-input", "Redstone Input", 60, 150),
                TN("controller", "function.redstone-controller", "Redstone Controller", 300, 120),
                TN("branch", "function.branch", "Enabled?", 560, 120),
                TN("fuel", "function.fuel-slot", "Fuel", 300, 420),
                TN("generator", "function.generator-core", "Generator Core", 820, 180),
                TN("energy", "function.energy-storage", "Energy Storage", 820, 420)
            ],
            [
                TC("redstone", "strength", "controller", "signal"),
                TC("controller", "enabled", "branch", "condition"),
                TC("$entry", "flow", "branch", "flow-in"),
                TC("branch", "true", "generator", "flow-in")
            ],
            [
                TG("fuel", "item", "item-slot", "Fuel", 35, 61, 18, 18, "minecraft:coal|1"),
                TG("controller", "enabled", "status", "Redstone Enabled", 62, 35, 12, 12, "true"),
                TG("generator", "progress", "progress-bar", "Burn", 70, 63, 42, 14, "0.40"),
                TG("energy", "ratio", "energy-bar", "Energy", 151, 35, 13, 64, "0.72")
            ]),

        new(
            "redstone-processor", "Control", "Redstone-Gated Processor",
            "Powered item processor guarded by a typed redstone controller and branch.",
            ["processor", "redstone", "control", "energy"],
            [
                TN("redstone", "function.redstone-input", "Redstone Input", 50, 120),
                TN("controller", "function.redstone-controller", "Redstone Controller", 280, 110),
                TN("branch", "function.branch", "Enabled?", 530, 110),
                TN("input", "function.item-input", "Input", 280, 390),
                TN("processor", "function.powered-processor", "Powered Processor", 800, 180),
                TN("output", "function.item-storage", "Output", 1080, 300),
                TN("energy", "function.energy-storage", "Energy Storage", 800, 440)
            ],
            [
                TC("redstone", "strength", "controller", "signal"),
                TC("controller", "enabled", "branch", "condition"),
                TC("$entry", "flow", "branch", "flow-in"),
                TC("branch", "true", "processor", "flow-in")
            ],
            [
                TG("input", "item", "item-slot", "Input", 28, 58, 18, 18, "minecraft:iron_ingot|1"),
                TG("controller", "enabled", "status", "Enabled", 59, 34, 12, 12, "true"),
                TG("processor", "progress", "progress-bar", "Progress", 68, 60, 42, 14, "0.55"),
                TG("output", "stored", "output-slot", "Output", 126, 58, 18, 18, "minecraft:gold_ingot|1"),
                TG("energy", "ratio", "energy-bar", "Energy", 154, 35, 12, 60, "0.70")
            ]),

        new(
            "manual-processor", "Control", "Player-Triggered Processor",
            "A processing cycle that starts from the On Player Use event. Good for button-like machines and manual tools.",
            ["processor", "manual", "interaction"],
            [
                TN("use", "function.on-use", "On Player Use", 70, 160),
                TN("input", "function.item-input", "Input", 70, 360),
                TN("processor", "function.standard-processor", "Processor", 420, 190),
                TN("output", "function.item-storage", "Output", 750, 300)
            ],
            [TC("use", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 35, 58, 18, 18, "minecraft:iron_ingot|1"),
                TG("processor", "progress", "progress-bar", "Progress", 70, 60, 42, 14, "0.50"),
                TG("output", "stored", "output-slot", "Output", 126, 58, 18, 18, "minecraft:gold_ingot|1")
            ]),

        new(
            "inventory-trigger-processor", "Control", "Inventory-Triggered Processor",
            "Starts processing when a bound inventory changes; useful for responsive automation without manual interaction.",
            ["processor", "inventory", "event", "automation"],
            [
                TN("event", "function.on-inventory-changed", "On Inventory Changed", 60, 150),
                TN("input", "function.item-input", "Input", 60, 360),
                TN("processor", "function.standard-processor", "Processor", 420, 190),
                TN("output", "function.item-storage", "Output", 750, 300)
            ],
            [TC("event", "flow", "processor", "flow-in")],
            [
                TG("input", "item", "item-slot", "Input", 35, 58, 18, 18, "minecraft:iron_ingot|1"),
                TG("processor", "progress", "progress-bar", "Progress", 70, 60, 42, 14, "0.50"),
                TG("output", "stored", "output-slot", "Output", 126, 58, 18, 18, "minecraft:gold_ingot|1")
            ]),

        new(
            "item-storage", "Storage", "Item Storage Machine",
            "Minimal general-purpose item storage. No processing logic is imposed.",
            ["storage", "item", "inventory"],
            [TN("storage", "function.item-storage", "Item Storage", 360, 230)],
            [],
            [TG("storage", "stored", "item-slot", "Stored Item", 79, 61, 18, 18, "minecraft:iron_ingot|32")]),

        new(
            "filtered-storage", "Storage", "Filtered Item Storage",
            "Item storage paired with a visible filter slot, ready for custom insertion/extraction rules.",
            ["storage", "item", "filter"],
            [
                TN("filter", "function.filter-slot", "Filter", 160, 230),
                TN("storage", "function.item-storage", "Item Storage", 500, 230)
            ],
            [],
            [
                TG("filter", "item", "item-slot", "Filter", 54, 61, 18, 18, "minecraft:iron_ingot|1"),
                TG("storage", "stored", "item-slot", "Stored", 104, 61, 18, 18, "minecraft:iron_ingot|32")
            ]),

        new(
            "energy-buffer", "Storage", "Energy Buffer",
            "Standalone FE storage source for batteries, capacitors and transfer-oriented blocks.",
            ["storage", "energy", "fe"],
            [TN("energy", "function.energy-storage", "Energy Storage", 360, 230)],
            [],
            [TG("energy", "ratio", "energy-bar", "Energy", 82, 43, 12, 66, "0.72")]),

        new(
            "fluid-buffer", "Storage", "Fluid Tank Machine",
            "Standalone fluid storage source for tanks and fluid-interface blocks.",
            ["storage", "fluid", "tank"],
            [TN("tank", "function.fluid-tank", "Fluid Tank", 360, 230)],
            [],
            [TG("tank", "ratio", "fluid-gauge", "Fluid", 82, 43, 14, 66, "0.55")]),

        new(
            "redstone-switch", "Control", "Redstone State Controller",
            "Reusable redstone input and enable-policy pair without imposing processing behavior.",
            ["redstone", "control", "signal"],
            [
                TN("redstone", "function.redstone-input", "Redstone Input", 200, 220),
                TN("controller", "function.redstone-controller", "Redstone Controller", 520, 220)
            ],
            [TC("redstone", "strength", "controller", "signal")],
            [TG("controller", "enabled", "status", "Enabled", 82, 62, 12, 12, "true")]),

        new(
            "timed-controller", "Control", "Timed Cycle Controller",
            "Machine Entry drives a reusable timer. Its progress and elapsed outputs are ready for GUI use and further custom logic.",
            ["timer", "control", "cycle", "utility"],
            [TN("timer", "function.timer", "Cycle Timer", 390, 210)],
            [TC("$entry", "flow", "timer", "flow-in")],
            [
                TG("timer", "progress", "progress-bar", "Cycle", 66, 62, 44, 14, "0.45"),
                TG("timer", "elapsed", "status", "Elapsed", 82, 38, 12, 12, "false")
            ]),

        new(
            "fluid-drain-controller", "Utility", "Manual Fluid Drain",
            "Player interaction triggers a typed fluid-consume action connected to a tank. Useful as a base for drains and dispensers.",
            ["fluid", "manual", "action", "utility"],
            [
                TN("use", "function.on-use", "On Player Use", 60, 180),
                TN("tank", "function.fluid-tank", "Fluid Tank", 60, 410),
                TN("consume", "function.consume-fluid", "Consume Fluid", 430, 220)
            ],
            [
                TC("use", "flow", "consume", "flow-in"),
                TC("tank", "fluid", "consume", "fluid")
            ],
            [TG("tank", "ratio", "fluid-gauge", "Fluid", 82, 43, 14, 66, "0.65")])
    ];

    private bool _functionTemplatesInitialized;
    private Border? _functionTemplatesOverlay;
    private TextBox? _functionTemplatesSearch;
    private StackPanel? _functionTemplatesList;
    private StackPanel? _functionTemplatesDetail;
    private TextBlock? _functionTemplatesCount;
    private string _functionTemplatesCategory = "All";
    private string? _functionTemplatesSelectedId;
    private readonly Dictionary<string, Button> _functionTemplateCategoryButtons = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverMachineBuilderFunctionTemplatesPhase3()
    {
        if (_functionTemplatesInitialized || _machineBuilderFunctionSurface is null)
            return;
        _functionTemplatesInitialized = true;

        InstallFunctionTemplatesButton();
        InstallFunctionTemplatesOverlay();
    }

    private void InstallFunctionTemplatesButton()
    {
        if (_machineBuilderFunctionSurface is null)
            return;

        var actions = FindStudioVisualChildren<StackPanel>(_machineBuilderFunctionSurface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                panel.Children.OfType<Button>().Any(button =>
                    FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "AUTO LAYOUT")));
        if (actions is null || actions.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "function_templates", StringComparison.Ordinal)))
            return;

        var templates = CreateStudioChromeButton("TEMPLATES", 96, true);
        templates.Tag = "function_templates";
        templates.Height = 31;
        templates.Margin = new Thickness(0, 0, 7, 0);
        templates.ToolTip = "Open prebuilt editable machine logic templates";
        templates.Click += (_, _) => ShowFunctionTemplates();
        actions.Children.Insert(0, templates);
    }

    private void InstallFunctionTemplatesOverlay()
    {
        if (_machineBuilderFunctionSurface?.Child is not Grid shell)
            return;

        _functionTemplatesOverlay = BuildFunctionTemplatesOverlay();
        Grid.SetRow(_functionTemplatesOverlay, 0);
        Grid.SetRowSpan(_functionTemplatesOverlay, Math.Max(1, shell.RowDefinitions.Count));
        Panel.SetZIndex(_functionTemplatesOverlay, 200);
        _functionTemplatesOverlay.Visibility = Visibility.Collapsed;
        shell.Children.Add(_functionTemplatesOverlay);
    }

    private Border BuildFunctionTemplatesOverlay()
    {
        var overlay = new Border
        {
            Background = Brush("#0D1215"),
            BorderBrush = Brush("#35505A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18)
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
        root.RowDefinitions.Add(new RowDefinition());

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(new TextBlock
        {
            Text = "FUNCTION TEMPLATES · 03",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        title.Children.Add(new TextBlock
        {
            Text = "Start with a complete editable machine logic",
            Foreground = Brush("#EFF5F6"),
            FontSize = 23,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        title.Children.Add(new TextBlock
        {
            Text = "Templates create normal Function nodes and typed connections. Their starter GUI is immediately visible in 04; actual item recipes remain yours to define there.",
            Foreground = Brush("#899CA4"),
            FontSize = 10.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 20, 0)
        });
        header.Children.Add(title);
        var close = CreateStudioChromeButton("BACK TO FUNCTIONS", 148, false);
        close.Height = 36;
        close.Click += (_, _) => HideFunctionTemplates();
        Grid.SetColumn(close, 1);
        header.Children.Add(close);
        root.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        body.ColumnDefinitions.Add(new ColumnDefinition());

        var browser = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var browserGrid = new Grid { Margin = new Thickness(14) };
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition());

        var browserHeading = new Grid();
        browserHeading.ColumnDefinitions.Add(new ColumnDefinition());
        browserHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        browserHeading.Children.Add(new TextBlock
        {
            Text = "TEMPLATE LIBRARY",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });
        _functionTemplatesCount = new TextBlock
        {
            Foreground = Brush("#71858E"),
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_functionTemplatesCount, 1);
        browserHeading.Children.Add(_functionTemplatesCount);
        browserGrid.Children.Add(browserHeading);

        _functionTemplatesSearch = MachineBuilderTextBox();
        _functionTemplatesSearch.Height = 34;
        _functionTemplatesSearch.Margin = new Thickness(0, 9, 0, 0);
        _functionTemplatesSearch.ToolTip = "Search templates by name, purpose or tag";
        _functionTemplatesSearch.TextChanged += (_, _) => RefreshFunctionTemplatesList();
        Grid.SetRow(_functionTemplatesSearch, 1);
        browserGrid.Children.Add(_functionTemplatesSearch);

        var categories = new WrapPanel { Margin = new Thickness(0, 9, 0, 4) };
        foreach (var category in new[] { "All", "Processing", "Energy", "Storage", "Control", "Utility" })
        {
            var button = CreateStudioChromeButton(category.ToUpperInvariant(), category == "Processing" ? 92 : 72, false);
            button.Height = 27;
            button.Margin = new Thickness(0, 0, 6, 6);
            button.Tag = category;
            button.Click += (_, _) =>
            {
                _functionTemplatesCategory = category;
                RefreshFunctionTemplateCategoryButtons();
                RefreshFunctionTemplatesList();
            };
            _functionTemplateCategoryButtons[category] = button;
            categories.Children.Add(button);
        }
        Grid.SetRow(categories, 2);
        browserGrid.Children.Add(categories);

        _functionTemplatesList = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brush("#091014"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5),
            Content = _functionTemplatesList
        };
        Grid.SetRow(listScroll, 3);
        browserGrid.Children.Add(listScroll);
        browser.Child = browserGrid;
        body.Children.Add(browser);

        var detail = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        _functionTemplatesDetail = new StackPanel { Margin = new Thickness(18) };
        detail.Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _functionTemplatesDetail
        };
        Grid.SetColumn(detail, 2);
        body.Children.Add(detail);

        Grid.SetRow(body, 2);
        root.Children.Add(body);
        overlay.Child = root;
        return overlay;
    }

    private void ShowFunctionTemplates()
    {
        if (_functionTemplatesOverlay is null)
            return;
        _functionTemplatesOverlay.Visibility = Visibility.Visible;
        _functionTemplatesSelectedId ??= FunctionLogicTemplates.FirstOrDefault()?.Id;
        RefreshFunctionTemplateCategoryButtons();
        RefreshFunctionTemplatesList();
        RefreshFunctionTemplateDetail();
    }

    private void HideFunctionTemplates()
    {
        if (_functionTemplatesOverlay is not null)
            _functionTemplatesOverlay.Visibility = Visibility.Collapsed;
    }

    private void RefreshFunctionTemplateCategoryButtons()
    {
        foreach (var pair in _functionTemplateCategoryButtons)
        {
            if (pair.Value.Content is not Border surface)
                continue;
            var active = pair.Key.Equals(_functionTemplatesCategory, StringComparison.OrdinalIgnoreCase);
            surface.Background = Brush(active ? "#1A4144" : "#202D34");
            surface.BorderBrush = Brush(active ? "#65C8C8" : "#41555E");
            if (surface.Child is TextBlock text)
                text.Foreground = Brush(active ? "#F0FFFF" : "#DDE7EA");
        }
    }

    private void RefreshFunctionTemplatesList()
    {
        if (_functionTemplatesList is null)
            return;
        _functionTemplatesList.Children.Clear();

        var query = (_functionTemplatesSearch?.Text ?? string.Empty).Trim();
        var filtered = FunctionLogicTemplates
            .Where(template => _functionTemplatesCategory.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                               template.Category.Equals(_functionTemplatesCategory, StringComparison.OrdinalIgnoreCase))
            .Where(template => string.IsNullOrWhiteSpace(query) ||
                               template.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               template.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               template.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               template.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(template => template.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(template => template.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_functionTemplatesCount is not null)
            _functionTemplatesCount.Text = $"{filtered.Count} / {FunctionLogicTemplates.Length}";

        foreach (var template in filtered)
            _functionTemplatesList.Children.Add(BuildFunctionTemplateCard(template));

        if (filtered.Count == 0)
        {
            _functionTemplatesList.Children.Add(new TextBlock
            {
                Text = "No templates match this search/filter.",
                Foreground = Brush("#748891"),
                FontSize = 10,
                Margin = new Thickness(8, 14, 8, 8)
            });
        }
    }

    private Border BuildFunctionTemplateCard(FunctionLogicTemplate template)
    {
        var selected = template.Id.Equals(_functionTemplatesSelectedId, StringComparison.OrdinalIgnoreCase);
        var card = new Border
        {
            Background = Brush(selected ? "#173236" : "#10191E"),
            BorderBrush = Brush(selected ? "#63C7C5" : "#2A3D45"),
            BorderThickness = new Thickness(selected ? 1.4 : 1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(2, 0, 2, 6),
            Cursor = Cursors.Hand
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = template.Title,
            Foreground = Brush("#EAF2F3"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = template.Description,
            Foreground = Brush("#84979E"),
            FontSize = 9,
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 34,
            Margin = new Thickness(0, 4, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{template.Category} · {template.Nodes.Length} nodes · {template.Connections.Length} links · {template.Gui.Length} GUI widgets",
            Foreground = Brush("#68BDBC"),
            FontSize = 8.6,
            Margin = new Thickness(0, 7, 0, 0)
        });
        card.Child = stack;
        card.MouseLeftButtonUp += (_, _) =>
        {
            _functionTemplatesSelectedId = template.Id;
            RefreshFunctionTemplatesList();
            RefreshFunctionTemplateDetail();
        };
        return card;
    }

    private void RefreshFunctionTemplateDetail()
    {
        if (_functionTemplatesDetail is null)
            return;
        _functionTemplatesDetail.Children.Clear();

        var template = FunctionLogicTemplates.FirstOrDefault(candidate =>
            candidate.Id.Equals(_functionTemplatesSelectedId, StringComparison.OrdinalIgnoreCase));
        if (template is null)
        {
            _functionTemplatesDetail.Children.Add(new TextBlock
            {
                Text = "Select a template",
                Foreground = Brush("#EFF5F6"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold
            });
            return;
        }

        _functionTemplatesDetail.Children.Add(new TextBlock
        {
            Text = template.Category.ToUpperInvariant(),
            Foreground = Brush("#72D5D3"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold
        });
        _functionTemplatesDetail.Children.Add(new TextBlock
        {
            Text = template.Title,
            Foreground = Brush("#EFF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        _functionTemplatesDetail.Children.Add(new TextBlock
        {
            Text = template.Description,
            Foreground = Brush("#AFC0C5"),
            FontSize = 10.5,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 7, 0, 0)
        });

        var stats = new WrapPanel { Margin = new Thickness(0, 15, 0, 0) };
        foreach (var text in new[]
                 {
                     $"{template.Nodes.Length} NODES",
                     $"{template.Connections.Length} CONNECTIONS",
                     $"{template.Gui.Length} GUI WIDGETS",
                     template.Nodes.Any(node => IsTemplateRecipeProcess(node.LibraryItemId)) ? "04 RECIPE READY" : "NO RECIPE CORE"
                 })
        {
            stats.Children.Add(new Border
            {
                Background = Brush("#17262C"),
                BorderBrush = Brush("#36515A"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 7, 7),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brush("#9CCFD0"),
                    FontSize = 8,
                    FontWeight = FontWeights.Bold
                }
            });
        }
        _functionTemplatesDetail.Children.Add(stats);

        _functionTemplatesDetail.Children.Add(FunctionTemplateSectionLabel("Functions 03 topology", 10));
        foreach (var node in template.Nodes)
        {
            _functionTemplatesDetail.Children.Add(new TextBlock
            {
                Text = $"{node.Label}   ·   {node.LibraryItemId}",
                Foreground = Brush("#B9C7CB"),
                FontSize = 9.3,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 5)
            });
        }

        _functionTemplatesDetail.Children.Add(FunctionTemplateSectionLabel("GUI 04 starter layout", 13));
        if (template.Gui.Length == 0)
        {
            _functionTemplatesDetail.Children.Add(new TextBlock
            {
                Text = "No GUI is imposed. Build the screen manually in 04.",
                Foreground = Brush("#82969E"),
                FontSize = 9.3
            });
        }
        else
        {
            foreach (var gui in template.Gui)
            {
                _functionTemplatesDetail.Children.Add(new TextBlock
                {
                    Text = $"{gui.Label}  ←  {gui.NodeKey}.{gui.PortId}  ·  {gui.Widget}",
                    Foreground = Brush("#B9C7CB"),
                    FontSize = 9.3,
                    Margin = new Thickness(0, 0, 0, 5)
                });
            }
        }

        _functionTemplatesDetail.Children.Add(new Border
        {
            Background = Brush("#111C21"),
            BorderBrush = Brush("#31505A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11),
            Margin = new Thickness(0, 16, 0, 0),
            Child = new TextBlock
            {
                Text = "USE TEMPLATE replaces the current Function graph (except Machine Entry), removes stale recipes/GUI bindings, then creates this editable topology and its GUI starter layout. Item recipes are intentionally left empty for 04.",
                Foreground = Brush("#8FA4AA"),
                FontSize = 9.2,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 15
            }
        });

        var use = CreateStudioChromeButton("USE TEMPLATE", 140, true);
        use.Height = 38;
        use.HorizontalAlignment = HorizontalAlignment.Left;
        use.Margin = new Thickness(0, 14, 0, 0);
        use.Click += (_, _) => ApplyFunctionLogicTemplate(template);
        _functionTemplatesDetail.Children.Add(use);
    }

    private void ApplyFunctionLogicTemplate(FunctionLogicTemplate template)
    {
        if (_machineBuilderDocument is null)
            return;

        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        var missing = template.Nodes
            .Where(node => _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, 1) is null)
            .Select(node => node.LibraryItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missing.Count > 0)
        {
            MessageBox.Show(
                this,
                "This template cannot be applied because these Function contracts are unavailable:\n\n" + string.Join("\n", missing),
                "Forever Studio template",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var hasExistingWork = _machineBuilderDocument.Behavior.Nodes.Any(node =>
                                  !node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase)) ||
                              _machineBuilderDocument.Behavior.Connections.Count > 0 ||
                              _machineBuilderDocument.Gui.Bindings.Count > 0 ||
                              _machineBuilderDocument.Recipes.Count > 0;
        if (hasExistingWork && MessageBox.Show(
                this,
                $"Use template '{template.Title}'?\n\nThis replaces the current Function graph and clears existing GUI bindings and recipes so no stale references remain. Machine Entry and the rest of the project are preserved.",
                "Apply Function template",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var entry = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(node =>
            node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            MessageBox.Show(this, "Machine Entry is missing from this project.", "Forever Studio template", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _machineBuilderDocument.Behavior.Nodes.RemoveAll(node =>
            !node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase));
        _machineBuilderDocument.Behavior.Connections.Clear();
        _machineBuilderDocument.Gui.Bindings.Clear();
        _machineBuilderDocument.Recipes.Clear();

        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["$entry"] = entry.Id
        };
        var usedNodeIds = _machineBuilderDocument.Behavior.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in template.Nodes)
        {
            var baseId = SanitizeMachineBuilderGraphId(spec.Key);
            if (string.IsNullOrWhiteSpace(baseId))
                baseId = "template_node";
            var id = baseId;
            for (var suffix = 2; usedNodeIds.Contains(id); suffix++)
                id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
            usedNodeIds.Add(id);
            ids[spec.Key] = id;

            var libraryItem = _machineBuilderLibraryLoad.Catalog.Find(spec.LibraryItemId, 1)!;
            var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["templateId"] = JsonSerializer.SerializeToElement(template.Id),
                ["templateRole"] = JsonSerializer.SerializeToElement(spec.Key)
            };
            if (!spec.Label.Equals(libraryItem.DisplayName, StringComparison.Ordinal))
                properties["label"] = JsonSerializer.SerializeToElement(spec.Label);

            _machineBuilderDocument.Behavior.Nodes.Add(new ForeverStudioMachineBehaviorNode
            {
                Id = id,
                LibraryItemId = spec.LibraryItemId,
                LibraryItemVersion = 1,
                X = spec.X,
                Y = spec.Y,
                Properties = properties
            });
        }

        var usedConnectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in template.Connections)
        {
            if (!ids.TryGetValue(spec.FromKey, out var fromId) || !ids.TryGetValue(spec.ToKey, out var toId))
                continue;
            var baseId = SanitizeMachineBuilderGraphId($"tpl_{fromId}_{spec.FromPort}_{toId}_{spec.ToPort}");
            var id = baseId;
            for (var suffix = 2; usedConnectionIds.Contains(id); suffix++)
                id = SanitizeMachineBuilderGraphId($"{baseId}_{suffix}");
            usedConnectionIds.Add(id);
            _machineBuilderDocument.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
            {
                Id = id,
                FromNodeId = fromId,
                FromPortId = spec.FromPort,
                ToNodeId = toId,
                ToPortId = spec.ToPort
            });
        }

        SeedFunctionTemplateGui(template, ids);
        _machineBuilderFunctionSelectedNodeId = template.Nodes.Select(node => ids[node.Key]).FirstOrDefault();
        _machineBuilderFunctionPendingOutput = null;

        MarkMachineBuilderFunctionDirty();
        MarkMachineBuilderPhase5Dirty();
        RefreshMachineBuilderFunctionGraph();
        RefreshMachineBuilderFunctionInspector();
        RefreshMachineBuilderPhase5();
        RefreshMachineBuilderGuiDesigner();
        HideFunctionTemplates();
        SetMachineBuilderFunctionStatus(
            $"Applied template '{template.Title}': {template.Nodes.Length} node(s), {template.Connections.Length} connection(s), {template.Gui.Length} GUI starter widget(s).",
            "#74C99A");
    }

    private void SeedFunctionTemplateGui(FunctionLogicTemplate template, IReadOnlyDictionary<string, string> ids)
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;

        if (string.IsNullOrWhiteSpace(_machineBuilderDocument.Gui.ScreenId))
            _machineBuilderDocument.Gui.ScreenId = $"{_machineBuilderDocument.AssetId}_screen";

        var order = 0;
        foreach (var spec in template.Gui)
        {
            if (!ids.TryGetValue(spec.NodeKey, out var nodeId))
                continue;
            var node = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate =>
                candidate.Id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
            var item = node is null ? null : _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
            var port = item?.Ports.FirstOrDefault(candidate =>
                candidate.Direction == ForeverStudioLibraryPortDirection.Output &&
                candidate.Id.Equals(spec.PortId, StringComparison.OrdinalIgnoreCase));
            if (node is null || port is null)
                continue;

            var assetId = spec.Widget switch
            {
                "item-slot" => "asset.input-tray",
                "output-slot" => "asset.output-tray",
                "fluid-gauge" => "asset.fluid-window",
                "status" => "asset.status-lamp",
                _ => "asset.screen"
            };
            var bindingId = SanitizeMachineBuilderGraphId($"gui_tpl_{template.Id}_{spec.NodeKey}_{spec.PortId}_{order}");
            _machineBuilderDocument.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
            {
                Id = bindingId,
                LibraryItemId = assetId,
                LibraryItemVersion = 1,
                SourceId = nodeId,
                Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["widget"] = JsonSerializer.SerializeToElement(spec.Widget),
                    ["sourcePortId"] = JsonSerializer.SerializeToElement(spec.PortId),
                    ["sourceType"] = JsonSerializer.SerializeToElement(port.DataType),
                    ["label"] = JsonSerializer.SerializeToElement(spec.Label),
                    ["x"] = JsonSerializer.SerializeToElement(spec.X),
                    ["y"] = JsonSerializer.SerializeToElement(spec.Y),
                    ["width"] = JsonSerializer.SerializeToElement(spec.Width),
                    ["height"] = JsonSerializer.SerializeToElement(spec.Height),
                    ["previewValue"] = JsonSerializer.SerializeToElement(spec.PreviewValue),
                    ["order"] = JsonSerializer.SerializeToElement(order++),
                    ["templateId"] = JsonSerializer.SerializeToElement(template.Id),
                    ["templateOwned"] = JsonSerializer.SerializeToElement(true)
                }
            });
        }
    }

    private static bool IsTemplateRecipeProcess(string libraryItemId)
        => libraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
           libraryItemId.Contains("generator-core", StringComparison.OrdinalIgnoreCase);

    private static TextBlock FunctionTemplateSectionLabel(string text, double top = 0)
        => new()
        {
            Text = text.ToUpperInvariant(),
            Foreground = Brush("#8FA4AA"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, top, 0, 7)
        };
}

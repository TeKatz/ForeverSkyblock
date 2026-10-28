namespace ForeverGuiDesigner.Services;

/// <summary>
/// Broad built-in Phase-2 palette. These are authoring contracts rather than generated Java code:
/// later phases attach generators/editors to the stable GeneratorKey values without changing the
/// machine documents that already reference an item ID/version.
/// </summary>
public static class ForeverStudioProductionLibrary
{
    public static IEnumerable<ForeverStudioLibraryItem> CreateItems()
    {
        foreach (var spec in AssetSpecs)
            yield return Asset(spec.Id, spec.Category, spec.Name, spec.Description, spec.Tags, spec.Preview);

        foreach (var spec in FunctionSpecs)
            yield return Function(spec.Id, spec.Category, spec.Name, spec.Description, spec.Tags, spec.Profile);

        foreach (var spec in AnimationSpecs)
            yield return Animation(spec.Id, spec.Category, spec.Name, spec.Description, spec.Tags);

        foreach (var spec in TemplateSpecs)
            yield return Template(spec.Id, spec.Category, spec.Name, spec.Description, spec.Tags);
    }

    private static ForeverStudioLibraryItem Asset(
        string id,
        string category,
        string name,
        string description,
        string tags,
        string preview)
        => new()
        {
            Id = id,
            Kind = ForeverStudioLibraryItemKind.Asset,
            Category = category,
            DisplayName = name,
            Description = description,
            GeneratorKey = id,
            PreviewKey = preview,
            Tags = Split(tags),
            Capabilities = TagsContain(tags, "animatable")
                ? ["visual-component", "runtime-target", "animatable"]
                : ["visual-component", "model-element"],
            RecommendedFor = InferRecommendations(tags)
        };

    private static ForeverStudioLibraryItem Function(
        string id,
        string category,
        string name,
        string description,
        string tags,
        string profile)
    {
        var item = new ForeverStudioLibraryItem
        {
            Id = id,
            Kind = ForeverStudioLibraryItemKind.Function,
            Category = category,
            DisplayName = name,
            Description = description,
            GeneratorKey = id,
            PreviewKey = profile,
            Tags = Split(tags),
            Capabilities = profile.StartsWith("composite", StringComparison.OrdinalIgnoreCase)
                ? ["behavior-composite", "expandable"]
                : profile is "item-storage" or "energy-storage" or "fluid-storage"
                    ? ["storage", "gui-bindable"]
                    : ["behavior-node"],
            Composite = profile.StartsWith("composite", StringComparison.OrdinalIgnoreCase),
            RecommendedFor = InferRecommendations(tags),
            Ports = Ports(profile)
        };

        if (id is "function.process-progress" or "function.map-range")
            item.Capabilities.Add("animation-source");
        if (id == "function.process-progress")
            item.Capabilities.Add("gui-bindable");
        if (id == "function.set-state")
            item.Capabilities.Add("state-writer");
        return item;
    }

    private static ForeverStudioLibraryItem Animation(
        string id,
        string category,
        string name,
        string description,
        string tags)
        => new()
        {
            Id = id,
            Kind = ForeverStudioLibraryItemKind.Animation,
            Category = category,
            DisplayName = name,
            Description = description,
            GeneratorKey = id,
            PreviewKey = id,
            Tags = Split(tags),
            Capabilities = ["animation-preset", "component-binding"],
            RecommendedFor = InferRecommendations(tags),
            Ports =
            [
                P("component", "Component", "component", ForeverStudioLibraryPortDirection.Input, required: true),
                P("phase", "Phase", "number", ForeverStudioLibraryPortDirection.Input)
            ]
        };

    private static ForeverStudioLibraryItem Template(
        string id,
        string category,
        string name,
        string description,
        string tags)
        => new()
        {
            Id = id,
            Kind = ForeverStudioLibraryItemKind.Template,
            Category = category,
            DisplayName = name,
            Description = description,
            GeneratorKey = id,
            PreviewKey = id,
            Tags = Split(tags),
            Capabilities = ["machine-template", "composite"],
            Composite = true,
            RecommendedFor = InferRecommendations(tags),
            Dependencies =
            [
                new ForeverStudioLibraryDependency { ItemId = "core.block-root", MinimumVersion = 1 }
            ]
        };

    private static List<ForeverStudioLibraryPort> Ports(string profile)
        => profile switch
        {
            "event" => [P("flow", "Flow", "flow", ForeverStudioLibraryPortDirection.Output)],
            "item-input" => [P("item", "Item", "item", ForeverStudioLibraryPortDirection.Output)],
            "item-output" =>
            [
                P("item", "Item", "item", ForeverStudioLibraryPortDirection.Input),
                P("accepted", "Accepted", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "item-storage" =>
            [
                P("item", "Item", "item", ForeverStudioLibraryPortDirection.Input, multiple: true),
                P("stored", "Stored", "item", ForeverStudioLibraryPortDirection.Output, multiple: true)
            ],
            "item-query" =>
            [
                P("item", "Item", "item", ForeverStudioLibraryPortDirection.Input, required: true),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "item-action" =>
            [
                FlowIn(),
                P("item", "Item", "item", ForeverStudioLibraryPortDirection.Input, required: true),
                FlowOut()
            ],
            "energy-storage" =>
            [
                P("energy", "Energy", "energy", ForeverStudioLibraryPortDirection.Output),
                P("ratio", "Fill Ratio", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "energy-query" =>
            [
                P("energy", "Energy", "energy", ForeverStudioLibraryPortDirection.Input, required: true),
                P("required", "Required", "number", ForeverStudioLibraryPortDirection.Input),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "energy-action" =>
            [
                FlowIn(),
                P("energy", "Energy", "energy", ForeverStudioLibraryPortDirection.Input),
                P("amount", "Amount", "number", ForeverStudioLibraryPortDirection.Input),
                FlowOut()
            ],
            "fluid-storage" =>
            [
                P("fluid", "Fluid", "fluid", ForeverStudioLibraryPortDirection.Output),
                P("ratio", "Fill Ratio", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "fluid-query" =>
            [
                P("fluid", "Fluid", "fluid", ForeverStudioLibraryPortDirection.Input, required: true),
                P("amount", "Amount", "number", ForeverStudioLibraryPortDirection.Input),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "fluid-action" =>
            [
                FlowIn(),
                P("fluid", "Fluid", "fluid", ForeverStudioLibraryPortDirection.Input, required: true),
                P("amount", "Amount", "number", ForeverStudioLibraryPortDirection.Input),
                FlowOut()
            ],
            "recipe-lookup" =>
            [
                P("recipe", "Recipe", "recipe", ForeverStudioLibraryPortDirection.Output),
                P("found", "Found", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "recipe-query" =>
            [
                P("recipe", "Recipe", "recipe", ForeverStudioLibraryPortDirection.Input),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "recipe-action" => [FlowIn(), P("recipe", "Recipe", "recipe", ForeverStudioLibraryPortDirection.Input), FlowOut()],
            "progress" =>
            [
                P("progress", "Progress", "number", ForeverStudioLibraryPortDirection.Output),
                P("complete", "Complete", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "number" => [P("value", "Value", "number", ForeverStudioLibraryPortDirection.Output)],
            "boolean" => [P("value", "Value", "boolean", ForeverStudioLibraryPortDirection.Output)],
            "branch" =>
            [
                FlowIn(),
                P("condition", "Condition", "boolean", ForeverStudioLibraryPortDirection.Input, required: true),
                P("true", "True", "flow", ForeverStudioLibraryPortDirection.Output),
                P("false", "False", "flow", ForeverStudioLibraryPortDirection.Output)
            ],
            "boolean-multi" =>
            [
                P("values", "Values", "boolean", ForeverStudioLibraryPortDirection.Input, required: true, multiple: true),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "boolean-unary" =>
            [
                P("value", "Value", "boolean", ForeverStudioLibraryPortDirection.Input, required: true),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "compare-number" =>
            [
                P("a", "A", "number", ForeverStudioLibraryPortDirection.Input, required: true),
                P("b", "B", "number", ForeverStudioLibraryPortDirection.Input, required: true),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "compare-state" =>
            [
                P("state", "State", "state", ForeverStudioLibraryPortDirection.Input),
                P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "timer" =>
            [
                FlowIn(),
                P("elapsed", "Elapsed", "boolean", ForeverStudioLibraryPortDirection.Output),
                P("progress", "Progress", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "counter" =>
            [
                P("delta", "Delta", "number", ForeverStudioLibraryPortDirection.Input),
                P("value", "Value", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "number-transform" =>
            [
                P("value", "Value", "number", ForeverStudioLibraryPortDirection.Input, required: true),
                P("result", "Result", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "random" => [P("result", "Result", "boolean", ForeverStudioLibraryPortDirection.Output)],
            "state-action" => [FlowIn(), FlowOut(), P("state", "State", "state", ForeverStudioLibraryPortDirection.Output)],
            "redstone" =>
            [
                P("strength", "Strength", "number", ForeverStudioLibraryPortDirection.Output),
                P("powered", "Powered", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            "composite-process" =>
            [
                FlowIn(),
                P("running", "Running", "boolean", ForeverStudioLibraryPortDirection.Output),
                P("progress", "Progress", "number", ForeverStudioLibraryPortDirection.Output),
                FlowOut("completed", "Completed")
            ],
            "composite-energy" =>
            [
                FlowIn(),
                P("running", "Running", "boolean", ForeverStudioLibraryPortDirection.Output),
                P("energy", "Energy", "energy", ForeverStudioLibraryPortDirection.Output),
                P("progress", "Progress", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "composite-fluid" =>
            [
                FlowIn(),
                P("fluid", "Fluid", "fluid", ForeverStudioLibraryPortDirection.Output),
                P("progress", "Progress", "number", ForeverStudioLibraryPortDirection.Output)
            ],
            "composite-control" =>
            [
                P("signal", "Signal", "number", ForeverStudioLibraryPortDirection.Input),
                P("enabled", "Enabled", "boolean", ForeverStudioLibraryPortDirection.Output)
            ],
            _ => []
        };

    private static ForeverStudioLibraryPort P(
        string id,
        string name,
        string type,
        ForeverStudioLibraryPortDirection direction,
        bool required = false,
        bool multiple = false)
        => new()
        {
            Id = id,
            DisplayName = name,
            DataType = type,
            Direction = direction,
            Required = required,
            Multiple = multiple
        };

    private static ForeverStudioLibraryPort FlowIn()
        => P("flow-in", "Flow In", "flow", ForeverStudioLibraryPortDirection.Input, required: true);

    private static ForeverStudioLibraryPort FlowOut(string id = "flow-out", string name = "Flow Out")
        => P(id, name, "flow", ForeverStudioLibraryPortDirection.Output);

    private static List<string> Split(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool TagsContain(string tags, string value)
        => tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(tag => tag.Equals(value, StringComparison.OrdinalIgnoreCase));

    private static List<string> InferRecommendations(string tags)
    {
        var result = new List<string>();
        foreach (var candidate in new[] { "processor", "generator", "fluid", "energy", "storage", "inventory", "status", "transport" })
        {
            if (tags.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                result.Add(candidate);
        }
        return result;
    }

    private static readonly (string Id, string Category, string Name, string Description, string Tags, string Preview)[] AssetSpecs =
    [
        ("asset.housing", "Structure / Housing", "Machine Housing", "Primary machine shell with material and texture slots.", "housing,shell,casing", "cube"),
        ("asset.frame", "Structure / Housing", "Machine Frame", "Open structural frame for industrial silhouettes.", "frame,structure", "frame"),
        ("asset.panel", "Structure / Detail", "Panel", "Flat inset or raised machine panel.", "panel,detail", "panel"),
        ("asset.glass-chamber", "Structure / Chamber", "Glass Chamber", "Transparent chamber for fluid or processing visuals.", "glass,chamber,transparent", "chamber"),
        ("asset.tank", "Structure / Chamber", "Tank", "Reusable tank component with fluid binding role.", "tank,fluid,storage,animatable", "tank"),
        ("asset.pipe", "Structure / Transport", "Pipe", "Reusable pipe section for fluid or decorative routing.", "pipe,fluid,transport", "pipe"),
        ("asset.conveyor", "Structure / Transport", "Conveyor", "Conveyor assembly with an animatable belt target.", "conveyor,transport,belt,animatable", "conveyor"),
        ("asset.input-tray", "Structure / Transport", "Input Tray", "Input staging tray with a semantic machine role.", "input,tray,item,transport", "tray"),
        ("asset.output-tray", "Structure / Transport", "Output Tray", "Output staging tray with a semantic machine role.", "output,tray,item,transport", "tray"),
        ("asset.rotor", "Mechanical / Rotary", "Rotor", "Rotary component with centered pivot and animation target.", "rotor,spin,mechanical,animatable", "rotor"),
        ("asset.shaft", "Mechanical / Rotary", "Shaft", "Rotary shaft for motors, mixers and gear trains.", "shaft,spin,mechanical,animatable", "shaft"),
        ("asset.gear", "Mechanical / Rotary", "Gear", "Reusable animatable gear component.", "gear,spin,mechanical,animatable", "gear"),
        ("asset.fan", "Mechanical / Rotary", "Fan", "Fan or blade assembly with a rotary pivot.", "fan,spin,air,animatable", "fan"),
        ("asset.mixer", "Mechanical / Rotary", "Mixer", "Mixer head for continuous or progress-linked rotation.", "mixer,spin,processor,animatable", "mixer"),
        ("asset.drill", "Mechanical / Tool", "Drill", "Drill head with an axial rotation target.", "drill,tool,spin,animatable", "drill"),
        ("asset.press", "Mechanical / Linear", "Press Head", "Vertical press head with a linear motion target.", "press,piston,processor,animatable", "press"),
        ("asset.piston", "Mechanical / Linear", "Piston", "Linear piston component with configurable travel.", "piston,linear,mechanical,animatable", "piston"),
        ("asset.door", "Mechanical / Linear", "Machine Door", "Hinged or sliding access door.", "door,access,animatable", "door"),
        ("asset.valve", "Mechanical / Control", "Valve", "Valve handle for state or fluid-driven rotation.", "valve,fluid,control,animatable", "valve"),
        ("asset.nozzle", "Mechanical / Tool", "Nozzle", "Output or input anchor for fluid, gas and visual effects.", "nozzle,fluid,effect", "nozzle"),
        ("asset.coil", "Electrical / Detail", "Energy Coil", "Coil visual with energy animation hooks.", "coil,energy,power,animatable", "coil"),
        ("asset.energy-core", "Electrical / Detail", "Energy Core", "Central energy component suited to pulse and glow animation.", "core,energy,glow,animatable", "core"),
        ("asset.status-lamp", "Electrical / Indicator", "Status Lamp", "Semantic status indicator for state and light bindings.", "lamp,status,light,animatable", "lamp"),
        ("asset.screen", "Electrical / Indicator", "Display Screen", "Display surface for status or decorative textures.", "screen,display,status", "screen"),
        ("asset.vent", "Structure / Detail", "Vent", "Reusable ventilation grille detail.", "vent,detail", "vent"),
        ("asset.fluid-window", "Structure / Chamber", "Fluid Window", "Transparent observation window with fluid-level binding role.", "fluid,window,tank,animatable", "window"),
        ("asset.platform", "Structure / Base", "Machine Platform", "Base or plinth component for tall machines.", "base,platform,structure", "platform"),
        ("asset.cable-port", "Electrical / Connector", "Cable Port", "Semantic energy or network connector.", "cable,port,energy", "port")
    ];

    private static readonly (string Id, string Category, string Name, string Description, string Tags, string Profile)[] FunctionSpecs =
    [
        ("function.on-tick", "Events", "On Tick", "Starts a flow on the machine server tick.", "event,flow", "event"),
        ("function.on-use", "Events", "On Player Use", "Starts a flow when a player interacts with the machine.", "event,flow", "event"),
        ("function.on-redstone-changed", "Events", "On Redstone Changed", "Starts a flow when redstone input changes.", "event,redstone,flow", "event"),
        ("function.on-neighbor-changed", "Events", "On Neighbor Changed", "Starts a flow after a neighboring block update.", "event,neighbor,flow", "event"),
        ("function.on-inventory-changed", "Events", "On Inventory Changed", "Starts a flow when a bound inventory changes.", "event,inventory,flow", "event"),
        ("function.on-recipe-start", "Events", "On Recipe Start", "Starts a flow when processing begins.", "event,recipe,processor", "event"),
        ("function.on-recipe-complete", "Events", "On Recipe Complete", "Starts a flow after processing completes.", "event,recipe,processor", "event"),
        ("function.on-state-enter", "Events", "On State Enter", "Starts a flow when the machine enters a selected state.", "event,state,flow", "event"),

        ("function.item-input", "Inventory", "Item Input", "Declares and reads an item input slot.", "inventory,item,input", "item-input"),
        ("function.item-output", "Inventory", "Item Output", "Declares and writes an item output slot.", "inventory,item,output", "item-output"),
        ("function.item-storage", "Inventory", "Item Storage", "Generic bidirectional item storage.", "inventory,item,storage", "item-storage"),
        ("function.filter-slot", "Inventory", "Filter Slot", "Non-consumed slot used to configure accepted items.", "inventory,item,filter", "item-input"),
        ("function.catalyst-slot", "Inventory", "Catalyst Slot", "Recipe catalyst slot with optional consumption.", "inventory,item,catalyst,processor", "item-input"),
        ("function.fuel-slot", "Inventory", "Fuel Slot", "Fuel input slot for generator workflows.", "inventory,item,fuel,generator", "item-input"),
        ("function.upgrade-slot", "Inventory", "Upgrade Slot", "Upgrade inventory exposed to modifier functions.", "inventory,item,upgrade", "item-storage"),
        ("function.can-insert-item", "Inventory / Query", "Can Insert Item", "Checks whether an item can enter a target storage.", "inventory,item,query", "item-query"),
        ("function.can-extract-item", "Inventory / Query", "Can Extract Item", "Checks whether an item can be extracted.", "inventory,item,query", "item-query"),
        ("function.consume-item", "Inventory / Action", "Consume Item", "Consumes an item from a bound source.", "inventory,item,action,processor", "item-action"),
        ("function.produce-item", "Inventory / Action", "Produce Item", "Attempts to insert an item into an output storage.", "inventory,item,action,processor", "item-action"),

        ("function.energy-storage", "Energy", "Energy Storage", "Declares FE storage and exposes current fill ratio.", "energy,fe,storage", "energy-storage"),
        ("function.has-energy", "Energy / Query", "Has Energy", "Checks whether enough FE is available.", "energy,fe,query", "energy-query"),
        ("function.consume-energy", "Energy / Action", "Consume Energy", "Consumes FE from a bound energy storage.", "energy,fe,action,processor", "energy-action"),
        ("function.generate-energy", "Energy / Action", "Generate Energy", "Adds FE to a bound energy storage.", "energy,fe,action,generator", "energy-action"),
        ("function.transfer-energy", "Energy / Action", "Transfer Energy", "Pushes FE to an adjacent or output capability.", "energy,fe,transfer", "energy-action"),

        ("function.fluid-tank", "Fluids", "Fluid Tank", "Declares a fluid tank and exposes its fill ratio.", "fluid,tank,storage", "fluid-storage"),
        ("function.has-fluid", "Fluids / Query", "Has Fluid", "Checks fluid type and amount availability.", "fluid,tank,query", "fluid-query"),
        ("function.consume-fluid", "Fluids / Action", "Consume Fluid", "Drains fluid from a bound tank.", "fluid,tank,action,processor", "fluid-action"),
        ("function.produce-fluid", "Fluids / Action", "Produce Fluid", "Fills a target tank with produced fluid.", "fluid,tank,action,processor", "fluid-action"),
        ("function.transfer-fluid", "Fluids / Action", "Transfer Fluid", "Moves fluid to an adjacent or output handler.", "fluid,tank,transfer", "fluid-action"),

        ("function.find-recipe", "Processing", "Find Recipe", "Looks up a compatible recipe from configured inputs.", "recipe,processor,lookup", "recipe-lookup"),
        ("function.recipe-available", "Processing / Query", "Recipe Available", "Tests whether a recipe is available.", "recipe,processor,query", "recipe-query"),
        ("function.output-space", "Processing / Query", "Output Has Space", "Checks whether configured outputs can accept a result.", "output,processor,query", "recipe-query"),
        ("function.start-process", "Processing / Action", "Start Process", "Initializes processing state and progress.", "process,processor,action", "recipe-action"),
        ("function.process-progress", "Processing", "Process Progress", "Tracks normalized process progress.", "process,progress,processor", "progress"),
        ("function.complete-process", "Processing / Action", "Complete Process", "Finalizes a recipe and resets progress.", "process,processor,action", "recipe-action"),
        ("function.process-duration", "Processing / Data", "Process Duration", "Provides a configurable tick duration.", "process,time,number", "number"),
        ("function.chance-output", "Processing / Output", "Chance Output", "Produces a result according to a probability.", "output,chance,processor", "item-output"),
        ("function.secondary-output", "Processing / Output", "Secondary Output", "Declares an optional secondary recipe result.", "output,secondary,processor", "item-output"),

        ("function.branch", "Logic", "Branch", "Routes flow based on a boolean condition.", "logic,branch,boolean", "branch"),
        ("function.and", "Logic", "AND", "Boolean AND over multiple inputs.", "logic,boolean,and", "boolean-multi"),
        ("function.or", "Logic", "OR", "Boolean OR over multiple inputs.", "logic,boolean,or", "boolean-multi"),
        ("function.not", "Logic", "NOT", "Boolean inversion.", "logic,boolean,not", "boolean-unary"),
        ("function.compare-number", "Logic / Compare", "Compare Number", "Numeric comparison node.", "logic,compare,number", "compare-number"),
        ("function.compare-state", "Logic / Compare", "State Is", "Checks the current semantic machine state.", "logic,compare,state", "compare-state"),
        ("function.random-chance", "Logic / Random", "Random Chance", "Boolean probability gate.", "logic,random,chance", "random"),
        ("function.number", "Data", "Number", "Constant numeric value.", "data,number,constant", "number"),
        ("function.boolean", "Data", "Boolean", "Constant boolean value.", "data,boolean,constant", "boolean"),
        ("function.timer", "Data / Time", "Timer", "Reusable tick timer or cooldown.", "data,timer,time", "timer"),
        ("function.counter", "Data", "Counter", "Incrementing or decrementing numeric state.", "data,counter,number", "counter"),
        ("function.clamp", "Data / Math", "Clamp", "Clamps a number between configured bounds.", "data,math,number", "number-transform"),
        ("function.map-range", "Data / Math", "Map Range", "Maps a numeric value from one range to another.", "data,math,number,animation", "number-transform"),
        ("function.set-state", "State / Action", "Set Machine State", "Changes the semantic machine state.", "state,action,flow", "state-action"),
        ("function.redstone-input", "State / Signal", "Redstone Input", "Reads redstone strength and powered state.", "state,redstone,signal", "redstone"),

        ("function.standard-processor", "Composites / Processing", "Standard Processor", "Recipe lookup, validation, progress, consume and output as one expandable block.", "composite,processor,recipe", "composite-process"),
        ("function.powered-processor", "Composites / Processing", "Powered Processor", "Standard processor with FE checks and consumption.", "composite,processor,energy", "composite-energy"),
        ("function.fluid-processor", "Composites / Processing", "Fluid Processor", "Processor with item and fluid prerequisites.", "composite,processor,fluid", "composite-fluid"),
        ("function.generator-core", "Composites / Energy", "Generator Core", "Reusable fuel-to-FE generator behavior.", "composite,generator,energy", "composite-energy"),
        ("function.redstone-controller", "Composites / Control", "Redstone Controller", "Reusable enable/disable policy driven by redstone.", "composite,redstone,control", "composite-control")
    ];

    private static readonly (string Id, string Category, string Name, string Description, string Tags)[] AnimationSpecs =
    [
        ("animation.rotate", "Basic Motion", "Rotate", "Rotates a component around one axis.", "rotate,basic"),
        ("animation.translate", "Basic Motion", "Translate", "Moves a component along an axis or vector.", "translate,move,basic"),
        ("animation.scale", "Basic Motion", "Scale", "Scales a component over animation phase.", "scale,basic"),
        ("animation.visibility", "Basic Motion", "Visibility", "Shows or hides a component from phase or state.", "visibility,state"),
        ("animation.pulse", "Basic Motion", "Pulse", "Smooth repeated scale or intensity pulse.", "pulse,loop"),
        ("animation.bob", "Basic Motion", "Bob", "Vertical oscillation preset.", "bob,oscillate"),
        ("animation.oscillate", "Basic Motion", "Oscillate", "Generic ping-pong rotation or translation.", "oscillate,pingpong"),
        ("animation.shake", "Basic Motion", "Shake", "Short mechanical vibration preset.", "shake,vibration"),
        ("animation.rotor-spin", "Machine Motion", "Rotor Spin", "Continuous rotary machine motion.", "rotor,spin,loop,processor"),
        ("animation.press-stroke", "Machine Motion", "Press Stroke", "Down, hold and return press cycle.", "press,stroke,processor"),
        ("animation.piston-cycle", "Machine Motion", "Piston Cycle", "Linear extend and retract cycle.", "piston,linear,processor"),
        ("animation.conveyor-loop", "Machine Motion", "Conveyor Loop", "Looping conveyor or belt motion.", "conveyor,belt,transport"),
        ("animation.intake", "Machine Motion", "Intake", "Moves an item or component into the machine.", "intake,item,transport"),
        ("animation.output-eject", "Machine Motion", "Output Eject", "Moves an item or component out of the machine.", "eject,output,item"),
        ("animation.mixer-spin", "Machine Motion", "Mixer Spin", "Continuous mixer rotation preset.", "mixer,spin,processor"),
        ("animation.drill-spin", "Machine Motion", "Drill Spin", "Fast axial drill rotation.", "drill,spin,processor"),
        ("animation.pump-cycle", "Machine Motion", "Pump Cycle", "Reciprocating pump stroke.", "pump,piston,fluid"),
        ("animation.valve-turn", "Machine Motion", "Valve Turn", "State-linked valve rotation.", "valve,rotate,fluid"),
        ("animation.status-blink", "Effects", "Status Blink", "Blink effect for a status indicator.", "status,blink,light"),
        ("animation.heat-glow", "Effects", "Heat Glow", "Progress-linked heat intensity preset.", "heat,glow,processor"),
        ("animation.energy-pulse", "Effects", "Energy Pulse", "Looping pulse for coils or energy cores.", "energy,pulse,effect"),
        ("animation.fluid-flow", "Effects", "Fluid Flow", "Fluid movement or level animation preset.", "fluid,flow,effect"),
        ("animation.follow-progress", "Bindings", "Follow Process Progress", "Maps normalized machine progress onto animation phase.", "progress,binding,processor"),
        ("animation.state-switch", "Bindings", "State Switch", "Activates visual behavior from a semantic machine state.", "state,binding,status"),
        ("animation.spring", "Curves", "Spring", "Spring-like easing preset.", "curve,spring,easing"),
        ("animation.ping-pong", "Curves", "Ping Pong", "Reusable forward and reverse looping phase.", "curve,pingpong,loop")
    ];

    private static readonly (string Id, string Category, string Name, string Description, string Tags)[] TemplateSpecs =
    [
        ("template.simple-block", "Simple Blocks", "Simple Block", "Visual-only block project without a BlockEntity.", "template,block,simple"),
        ("template.processing-machine", "Machines", "Processing Machine", "Standard item processing machine skeleton.", "template,processor,recipe"),
        ("template.powered-processor", "Machines", "Powered Processor", "Processing machine with FE storage and power states.", "template,processor,energy"),
        ("template.dual-input-processor", "Machines", "Dual Input Processor", "Two-input recipe processor with one output.", "template,processor,dual-input"),
        ("template.fluid-processor", "Machines", "Fluid Processor", "Item and fluid processing machine skeleton.", "template,processor,fluid"),
        ("template.generator", "Machines", "Generator", "Fuel or input driven FE generator skeleton.", "template,generator,energy"),
        ("template.storage-machine", "Machines", "Storage Machine", "Inventory-focused machine skeleton.", "template,storage,inventory"),
        ("template.fuel-processor", "Machines", "Fuel Processor", "Processing machine with a dedicated fuel slot.", "template,processor,fuel"),
        ("template.attachment", "Advanced", "Attachment / Upgrade", "Small attachment or upgrade block project.", "template,attachment,upgrade"),
        ("template.advanced-custom", "Advanced", "Advanced Custom Machine", "Minimal machine project for unrestricted custom assembly.", "template,custom,advanced")
    ];
}

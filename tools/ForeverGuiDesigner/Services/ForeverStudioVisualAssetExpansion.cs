using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Built-in fallback for the expanded Phase-3 visual asset packs. Source checkouts load the JSON
/// packs from the repository; self-updated single-file builds register the same contracts in memory
/// when those project files are not present locally.
/// </summary>
public static class ForeverStudioVisualAssetExpansion
{
    private static readonly ConditionalWeakTable<ForeverStudioLibraryCatalog, object> ExtendedCatalogs = new();

    public static void EnsureRegistered(ForeverStudioLibraryLoadResult load)
    {
        ArgumentNullException.ThrowIfNull(load);
        var catalog = load.Catalog;
        if (ExtendedCatalogs.TryGetValue(catalog, out _))
            return;

        var register = typeof(ForeverStudioLibraryCatalog).GetMethod(
            "Register",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (register is null)
        {
            ExtendedCatalogs.Add(catalog, new object());
            return;
        }

        var warnings = new List<string>();
        foreach (var spec in Specs)
        {
            var id = $"asset.{spec.Id}";
            if (catalog.FindLatest(id) is not null)
                continue;

            var tags = Split(spec.Tags);
            var item = new ForeverStudioLibraryItem
            {
                Id = id,
                Version = 1,
                Kind = ForeverStudioLibraryItemKind.Asset,
                Category = spec.Category,
                DisplayName = spec.Name,
                Description = spec.Description,
                GeneratorKey = id,
                PreviewKey = spec.Preview,
                Tags = tags,
                Capabilities = tags.Contains("animatable", StringComparer.OrdinalIgnoreCase)
                    ? ["visual-component", "runtime-target", "animatable"]
                    : ["visual-component", "model-element"],
                RecommendedFor = InferRecommendations(tags)
            };

            try
            {
                register.Invoke(catalog, [item, "built-in visual expansion", warnings]);
            }
            catch (TargetInvocationException)
            {
                // All shipped specs are validated in CI/source builds; a malformed fallback entry
                // should never prevent the Machine Builder from opening.
            }
        }

        ExtendedCatalogs.Add(catalog, new object());
    }

    private static List<string> Split(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<string> InferRecommendations(IEnumerable<string> tags)
    {
        var set = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new[] { "processor", "generator", "fluid", "energy", "storage", "inventory", "status", "transport" }
            .Where(set.Contains)
            .ToList();
    }

    private static readonly (string Id, string Category, string Name, string Description, string Tags, string Preview)[] Specs =
    [
        ("reinforced-housing", "Structure / Housing", "Reinforced Housing", "Thick industrial shell for heavy-duty machines.", "housing,structure,reinforced,industrial", "cube"),
        ("side-casing", "Structure / Housing", "Side Casing", "Full-height side shell segment for modular housings.", "housing,casing,structure,side", "panel"),
        ("top-casing", "Structure / Housing", "Top Casing", "Top shell plate for enclosed machine silhouettes.", "housing,casing,structure,top", "panel"),
        ("bottom-casing", "Structure / Housing", "Bottom Casing", "Bottom shell plate or internal floor for a housing.", "housing,casing,structure,bottom", "panel"),
        ("support-beam", "Structure / Housing", "Support Beam", "Structural beam for frames, braces and tall assemblies.", "frame,beam,structure,support", "shaft"),
        ("corner-post", "Structure / Housing", "Corner Post", "Vertical structural post for framed machine bodies.", "frame,post,structure,corner", "shaft"),
        ("service-hatch", "Structure / Housing", "Service Hatch", "Compact access hatch for maintenance areas.", "door,hatch,access,structure,animatable", "door"),
        ("access-panel", "Structure / Housing", "Access Panel", "Removable machine panel for service detailing.", "panel,access,detail,structure", "panel"),
        ("heavy-base", "Structure / Base", "Heavy Base", "Wide reinforced plinth for large industrial machines.", "base,platform,structure,industrial", "platform"),
        ("pedestal", "Structure / Base", "Pedestal", "Raised pedestal for compact machines and terminals.", "base,pedestal,structure", "platform"),
        ("machine-foot", "Structure / Base", "Machine Foot", "Small support foot for lifted machine bodies.", "base,foot,support,structure", "platform"),
        ("mounting-rail", "Structure / Base", "Mounting Rail", "Narrow mounting rail for modules and subassemblies.", "rail,mount,structure", "shaft"),
        ("processing-chamber", "Structure / Chamber", "Processing Chamber", "General-purpose enclosed processing volume.", "chamber,processor,structure", "chamber"),
        ("pressure-vessel", "Structure / Chamber", "Pressure Vessel", "Reinforced vessel for pressure-oriented machine designs.", "tank,vessel,fluid,structure", "tank"),
        ("reaction-vessel", "Structure / Chamber", "Reaction Vessel", "Central tank-like reactor volume for processing machines.", "tank,reaction,processor,fluid", "tank"),
        ("observation-port", "Structure / Chamber", "Observation Port", "Small viewing aperture for chambers and tanks.", "window,chamber,detail", "window"),
        ("sight-glass", "Structure / Chamber", "Sight Glass", "Vertical inspection window for fluid or process levels.", "window,fluid,tank,indicator", "window"),
        ("feed-hopper", "Structure / Chamber", "Feed Hopper", "Open feed reservoir for item-processing machines.", "hopper,input,item,processor", "tray"),
        ("grille", "Structure / Detail", "Industrial Grille", "Vent-like grille for machine faces and covers.", "grille,vent,detail,structure", "vent"),
        ("heat-sink", "Structure / Detail", "Heat Sink", "Finned cooling detail for powered equipment.", "heatsink,cooling,thermal,detail", "vent"),
        ("ribbed-panel", "Structure / Detail", "Ribbed Panel", "Raised ribbed panel for industrial surface detail.", "panel,ribbed,detail,structure", "panel"),
        ("hazard-panel", "Structure / Detail", "Hazard Panel", "Dedicated plate for hazard-striped textures and warnings.", "panel,hazard,warning,detail", "panel"),
        ("corner-trim", "Structure / Detail", "Corner Trim", "Slim trim piece for breaking up large housings.", "trim,detail,structure", "shaft"),
        ("name-plate", "Structure / Detail", "Name Plate", "Small plate for labels, logos or machine IDs.", "plate,label,detail", "panel"),
        ("item-chute", "Transport / Item", "Item Chute", "Compact item routing chute between machine sections.", "item,transport,chute,output", "pipe"),
        ("item-duct", "Transport / Item", "Item Duct", "Enclosed item transport duct segment.", "item,transport,duct", "pipe"),
        ("loader-funnel", "Transport / Item", "Loader Funnel", "Input funnel for loaders, grinders and processors.", "item,input,funnel,transport", "tray"),
        ("ejector-port", "Transport / Item", "Ejector Port", "Directed output nozzle for item ejection points.", "item,output,transport,nozzle", "nozzle"),
        ("sorter-gate", "Transport / Item", "Sorter Gate", "Animated gate for diverters and item sorters.", "item,transport,gate,animatable", "door"),
        ("conveyor-roller", "Transport / Item", "Conveyor Roller", "Rotating roller element for conveyors and feeders.", "transport,conveyor,roller,animatable", "shaft"),
        ("fluid-pipe-straight", "Transport / Fluid", "Fluid Pipe Straight", "Straight fluid-routing pipe segment.", "fluid,pipe,transport", "pipe"),
        ("fluid-pipe-elbow", "Transport / Fluid", "Fluid Pipe Elbow", "Elbow-styled fluid routing element for compact layouts.", "fluid,pipe,transport,elbow", "pipe"),
        ("fluid-junction", "Transport / Fluid", "Fluid Junction", "Multi-branch pipe junction for fluid networks.", "fluid,pipe,junction,transport", "pipe"),
        ("fluid-manifold", "Transport / Fluid", "Fluid Manifold", "Distribution manifold for multiple fluid connections.", "fluid,manifold,transport", "pipe"),
        ("fluid-intake", "Transport / Fluid", "Fluid Intake", "Semantic intake connector for a machine tank or chamber.", "fluid,input,port,transport", "port"),
        ("fluid-output", "Transport / Fluid", "Fluid Output", "Semantic fluid outlet connector.", "fluid,output,port,transport", "port"),
        ("flywheel", "Mechanical / Rotary", "Flywheel", "Large rotating mass for engines and mechanical processors.", "mechanical,rotary,flywheel,animatable", "gear"),
        ("pulley", "Mechanical / Rotary", "Pulley", "Rotating pulley for belt-driven assemblies.", "mechanical,rotary,pulley,animatable", "gear"),
        ("bearing", "Mechanical / Rotary", "Bearing", "Compact support around shafts and rotating assemblies.", "mechanical,rotary,bearing", "shaft"),
        ("coupling", "Mechanical / Rotary", "Shaft Coupling", "Connector between two rotary machine sections.", "mechanical,rotary,coupling,shaft", "shaft"),
        ("turbine", "Mechanical / Rotary", "Turbine", "Blade assembly for fluid, steam or energy machines.", "mechanical,rotary,turbine,energy,animatable", "fan"),
        ("impeller", "Mechanical / Rotary", "Impeller", "Compact rotor for pumps and mixers.", "mechanical,rotary,fluid,impeller,animatable", "fan"),
        ("hand-wheel", "Mechanical / Rotary", "Hand Wheel", "Manual wheel control suitable for valves and hatches.", "mechanical,rotary,control,animatable", "valve"),
        ("ram", "Mechanical / Linear", "Ram", "Heavy linear actuator head for presses and crushers.", "mechanical,linear,ram,animatable", "piston"),
        ("lift-plate", "Mechanical / Linear", "Lift Plate", "Moving platform for elevators and vertical mechanisms.", "mechanical,linear,platform,animatable", "press"),
        ("sliding-carriage", "Mechanical / Linear", "Sliding Carriage", "Linear carriage for tool heads and transfer mechanisms.", "mechanical,linear,carriage,animatable", "panel"),
        ("clamp", "Mechanical / Linear", "Clamp", "Moving clamp jaw for fixtures and processing tools.", "mechanical,linear,clamp,animatable", "press"),
        ("crusher-jaw", "Mechanical / Tool", "Crusher Jaw", "Heavy moving jaw for crushing and compacting machines.", "mechanical,tool,crusher,processor,animatable", "press"),
        ("saw-blade", "Mechanical / Tool", "Saw Blade", "Rotary cutting disc for saw or cutter machines.", "mechanical,tool,saw,rotary,animatable", "gear"),
        ("cutter", "Mechanical / Tool", "Cutter Head", "Compact rotating cutter for machining assemblies.", "mechanical,tool,cutter,rotary,animatable", "drill"),
        ("injector", "Mechanical / Tool", "Injector", "Directed nozzle for injection or dispensing machines.", "mechanical,tool,injector,fluid", "nozzle"),
        ("battery-module", "Electrical / Power", "Battery Module", "Blocky energy-storage module for powered machines.", "electrical,energy,battery,storage", "cube"),
        ("capacitor-bank", "Electrical / Power", "Capacitor Bank", "Energy-storage visual with coil-like detailing.", "electrical,energy,capacitor,storage", "coil"),
        ("transformer", "Electrical / Power", "Transformer", "Power-conversion module for electrical machine designs.", "electrical,energy,power,transformer", "coil"),
        ("power-bus", "Electrical / Power", "Power Bus", "Linear internal power rail or busbar.", "electrical,energy,power,bus", "shaft"),
        ("generator-coil", "Electrical / Power", "Generator Coil", "Animatable energy coil for generators and turbines.", "electrical,energy,generator,coil,animatable", "coil"),
        ("cable-socket", "Electrical / Connector", "Cable Socket", "Compact cable attachment point.", "electrical,cable,connector,port", "port"),
        ("network-port", "Electrical / Connector", "Network Port", "Connector for machine networks and data links.", "electrical,network,connector,port", "port"),
        ("redstone-port", "Electrical / Connector", "Redstone Port", "Dedicated redstone signal connector.", "electrical,redstone,connector,port", "port"),
        ("maintenance-port", "Electrical / Connector", "Maintenance Port", "Service connector for diagnostics or upgrades.", "electrical,maintenance,connector,port", "port"),
        ("gauge", "Electrical / Indicator", "Gauge", "Compact instrument face for numeric machine state.", "electrical,indicator,gauge,status", "screen"),
        ("meter", "Electrical / Indicator", "Meter", "Display panel for energy, pressure or progress values.", "electrical,indicator,meter,status", "screen"),
        ("warning-beacon", "Electrical / Indicator", "Warning Beacon", "Prominent state lamp for alarms and warnings.", "electrical,indicator,warning,light,status,animatable", "lamp"),
        ("indicator-strip", "Electrical / Indicator", "Indicator Strip", "Slim status-light strip for machine fronts.", "electrical,indicator,status,light,animatable", "lamp"),
        ("button-panel", "Control / Interface", "Button Panel", "Operator panel for buttons and interaction textures.", "control,interface,panel,input", "panel"),
        ("control-console", "Control / Interface", "Control Console", "Primary operator display and controls surface.", "control,interface,screen,status", "screen"),
        ("lever-mount", "Control / Interface", "Lever Mount", "Mechanical control mount for lever-like interactions.", "control,interface,lever,animatable", "valve"),
        ("control-dial", "Control / Interface", "Control Dial", "Rotary dial for machine settings and state.", "control,interface,dial,animatable", "valve"),
        ("terminal-screen", "Control / Interface", "Terminal Screen", "Large machine terminal display surface.", "control,interface,screen,display", "screen"),
        ("radiator", "Thermal / Cooling", "Radiator", "Finned thermal-exchange surface.", "thermal,cooling,radiator,vent", "vent"),
        ("cooling-fin", "Thermal / Cooling", "Cooling Fin", "Small thermal fin detail for hot machine surfaces.", "thermal,cooling,fin,detail", "vent"),
        ("exhaust-port", "Thermal / Cooling", "Exhaust Port", "Directed exhaust or steam outlet.", "thermal,exhaust,steam,nozzle", "nozzle"),
        ("cooling-fan", "Thermal / Cooling", "Cooling Fan", "Animatable fan for active cooling systems.", "thermal,cooling,fan,animatable", "fan"),
        ("heat-exchanger", "Thermal / Cooling", "Heat Exchanger", "Pipe-based thermal transfer module.", "thermal,cooling,fluid,pipe", "pipe"),
        ("bolt-plate", "Decorative / Industrial", "Bolt Plate", "Small industrial plate for layered surface detailing.", "decorative,industrial,plate,detail", "panel"),
        ("pipe-clamp", "Decorative / Industrial", "Pipe Clamp", "Bracket-like detail for visually securing pipes.", "decorative,industrial,pipe,clamp,detail", "panel"),
        ("machine-bracket", "Decorative / Industrial", "Machine Bracket", "Structural bracket for supports and exposed machinery.", "decorative,industrial,bracket,structure", "frame"),
        ("guard-rail", "Decorative / Industrial", "Guard Rail", "Open safety frame for exposed moving parts.", "decorative,industrial,guard,frame", "frame"),
    ];
}

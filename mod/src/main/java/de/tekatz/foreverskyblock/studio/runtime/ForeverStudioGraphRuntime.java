package de.tekatz.foreverskyblock.studio.runtime;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.RecipeHolder;

import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;

/** Typed, fail-closed evaluator for Studio Function Graphs. */
public final class ForeverStudioGraphRuntime {
    private static final int MAX_DEPTH = 64;
    private ForeverStudioGraphRuntime() { }

    public static boolean allowsProcessor(ForeverStudioMachineBlockEntity machine) {
        ForeverStudioGraphSignals.beginTick(machine);
        if (machine.definition().graphNodes().isEmpty() || machine.definition().graphConnections().isEmpty()) return true;
        var processor = machine.definition().graphNodes().stream().filter(node -> isProcessor(node.kind())).findFirst().orElse(null);
        if (processor == null) return false;
        var incoming = incomingAll(machine, processor.id(), "flow-in");
        if (incoming.isEmpty()) return false;
        for (var connection : incoming)
            if (flow(machine, connection.fromNode(), connection.fromPort(), new HashSet<>(), 0)) return true;
        return false;
    }

    public static double animationPhase(ForeverStudioMachineBlockEntity machine,
                                        ForeverStudioMachineDefinition.AnimationDefinition animation) {
        ForeverStudioGraphSignals.beginTick(machine);
        double value;
        if (!animation.sourceNode().isBlank()) {
            value = isBooleanPort(animation.sourcePort())
                    ? (bool(machine, animation.sourceNode(), animation.sourcePort(), new HashSet<>(), 0) ? 1.0 : 0.0)
                    : number(machine, animation.sourceNode(), animation.sourcePort(), new HashSet<>(), 0);
        } else if (animation.preset().contains("progress")) {
            value = progress(machine);
        } else {
            long time = machine.getLevel() == null ? 0L : machine.getLevel().getGameTime();
            value = machine.running() ? (time % 40L) / 40.0 : 0.0;
        }
        String preset = animation.preset().toLowerCase(Locale.ROOT);
        if (preset.contains("blink")) {
            long time = machine.getLevel() == null ? 0L : machine.getLevel().getGameTime();
            value = value > 0 && (time / 8L) % 2L == 0L ? 1 : 0;
        } else if (preset.contains("pulse") || preset.contains("bob") || preset.contains("oscillate") || preset.contains("ping-pong")) {
            value = 0.5 - 0.5 * Math.cos(value * Math.PI * 2.0);
        } else if (preset.contains("spin") && animation.sourceNode().isBlank()) {
            long time = machine.getLevel() == null ? 0L : machine.getLevel().getGameTime();
            value = machine.running() ? (time % 20L) / 20.0 : 0.0;
        }
        return Math.clamp(value * animation.scale(), 0.0, 1.0);
    }

    public static boolean booleanValue(ForeverStudioMachineBlockEntity machine, String node, String port) {
        ForeverStudioGraphSignals.beginTick(machine);
        return bool(machine, node, port, new HashSet<>(), 0);
    }
    public static double numberValue(ForeverStudioMachineBlockEntity machine, String node, String port) {
        ForeverStudioGraphSignals.beginTick(machine);
        return number(machine, node, port, new HashSet<>(), 0);
    }

    private static boolean flow(ForeverStudioMachineBlockEntity machine, String nodeId, String port,
                                Set<String> visiting, int depth) {
        String key = "f:" + nodeId + ':' + port;
        if (depth > MAX_DEPTH || !visiting.add(key)) return false;
        try {
            var node = node(machine, nodeId);
            if (node == null) return false;
            return switch (node.kind()) {
                case "core.machine-entry", "function.on-tick" -> true;
                case "function.on-use" -> machine.playerUsePulse();
                case "function.on-inventory-changed" -> machine.inventoryChangedPulse();
                case "function.on-redstone-changed" -> ForeverStudioGraphSignals.redstoneChanged(machine);
                case "function.on-neighbor-changed" -> ForeverStudioGraphSignals.neighborChanged(machine);
                case "function.on-recipe-start" -> ForeverStudioGraphSignals.recipeStarted(machine);
                case "function.on-recipe-complete" -> ForeverStudioGraphSignals.recipeCompleted(machine);
                case "function.on-state-enter" -> ForeverStudioGraphSignals.stateEntered(machine) && stateMatches(machine, node);
                case "function.branch" -> {
                    boolean enabled = incomingFlow(machine, nodeId, visiting, depth + 1);
                    boolean condition = boolInput(machine, nodeId, "condition", visiting, depth + 1);
                    yield enabled && (port.equals("true") ? condition : port.equals("false") && !condition);
                }
                case "function.set-state" -> {
                    boolean enabled = incomingFlow(machine, nodeId, visiting, depth + 1);
                    if (enabled && port.equals("flow-out")) {
                        String target = string(node.properties(), "state", string(node.properties(), "targetState", ""));
                        if (!target.isBlank()) machine.setState(target);
                    }
                    yield enabled;
                }
                default -> isProcessor(node.kind()) ? machine.running() && port.equals("completed") : false;
            };
        } finally { visiting.remove(key); }
    }

    private static boolean bool(ForeverStudioMachineBlockEntity machine, String nodeId, String port,
                                Set<String> visiting, int depth) {
        String key = "b:" + nodeId + ':' + port;
        if (depth > MAX_DEPTH || !visiting.add(key)) return false;
        try {
            var node = node(machine, nodeId);
            if (node == null) return false;
            return switch (node.kind()) {
                case "function.boolean" -> boolProperty(node.properties(), "value", false);
                case "function.and" -> boolInputs(machine, nodeId, true, visiting, depth + 1);
                case "function.or" -> boolInputs(machine, nodeId, false, visiting, depth + 1);
                case "function.not" -> !boolInput(machine, nodeId, "value", visiting, depth + 1);
                case "function.compare-number" -> compare(numberInput(machine, nodeId, "a", visiting, depth + 1),
                        numberInput(machine, nodeId, "b", visiting, depth + 1), string(node.properties(), "operator", ">="));
                case "function.compare-state" -> stateMatches(machine, node);
                case "function.random-chance" -> randomChance(machine, node);
                case "function.has-energy" -> machine.energyStored() >= Math.max(0, (int)Math.round(
                        numberInputOrProperty(machine, nodeId, "required", node, 1, visiting, depth + 1)));
                case "function.has-fluid" -> hasFluid(machine, node, visiting, depth + 1);
                case "function.find-recipe", "function.recipe-available" -> recipeAvailable(machine, false);
                case "function.output-space" -> recipeAvailable(machine, true);
                case "function.process-progress" -> port.equals("complete")
                        ? ForeverStudioGraphSignals.recipeCompleted(machine) : machine.running();
                case "function.redstone-input" -> machine.redstoneStrength() > 0;
                case "function.redstone-controller" -> redstoneController(machine, nodeId, node, visiting, depth + 1);
                default -> isProcessor(node.kind()) && port.equals("running") && machine.running();
            };
        } finally { visiting.remove(key); }
    }

    private static double number(ForeverStudioMachineBlockEntity machine, String nodeId, String port,
                                 Set<String> visiting, int depth) {
        String key = "n:" + nodeId + ':' + port;
        if (depth > MAX_DEPTH || !visiting.add(key)) return 0;
        try {
            var node = node(machine, nodeId);
            if (node == null) return 0;
            return switch (node.kind()) {
                case "function.number" -> numeric(node.properties(), "value", 0);
                case "function.process-duration" -> numeric(node.properties(), "ticks", machine.duration());
                case "function.process-progress" -> progress(machine);
                case "function.energy-storage" -> port.equals("ratio")
                        ? ratio(machine.energyStored(), machine.definition().energyCapacity()) : machine.energyStored();
                case "function.fluid-tank" -> fluidValue(machine, nodeId, port);
                case "function.redstone-input" -> machine.redstoneStrength();
                case "function.clamp" -> {
                    double value = numberInput(machine, nodeId, "value", visiting, depth + 1);
                    double min = numeric(node.properties(), "min", 0), max = numeric(node.properties(), "max", 1);
                    yield Math.clamp(value, Math.min(min, max), Math.max(min, max));
                }
                case "function.map-range" -> map(numberInput(machine, nodeId, "value", visiting, depth + 1),
                        numeric(node.properties(), "fromMin", 0), numeric(node.properties(), "fromMax", 1),
                        numeric(node.properties(), "toMin", 0), numeric(node.properties(), "toMax", 1));
                default -> isProcessor(node.kind()) && port.equals("progress") ? progress(machine) : 0;
            };
        } finally { visiting.remove(key); }
    }

    private static boolean recipeAvailable(ForeverStudioMachineBlockEntity machine, boolean requireOutputSpace) {
        if (machine.getLevel() == null) return false;
        var input = new ForeverStudioMachineInput(machine.machineId(),
                machine.definition().inputSlots().stream().map(machine.inventory()::getStackInSlot).toList());
        for (RecipeHolder<ForeverStudioMachineRecipe> holder : machine.getLevel().getRecipeManager()
                .getAllRecipesFor(ForeverStudioGeneratedMachines.MACHINE_RECIPE_TYPE.get())) {
            var book = holder.value();
            if (!book.machine().equals(machine.machineId().toString())) continue;
            for (var entry : book.recipes()) {
                if (!entry.acceptsItems(input)) continue;
                boolean fluids = true;
                for (var requirement : entry.fluidInputs())
                    if (!machine.hasFluid(requirement.tank(), requirement.fluidId(), requirement.amount())) { fluids = false; break; }
                if (!fluids) continue;
                if (!requireOutputSpace || outputsFit(machine, entry)) return true;
            }
        }
        return false;
    }

    private static boolean outputsFit(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineRecipe.Entry entry) {
        if (entry.results().size() > machine.definition().outputSlots().size()) return false;
        for (int i = 0; i < entry.results().size(); i++) {
            ItemStack result = entry.results().get(i);
            ItemStack output = machine.inventory().getStackInSlot(machine.definition().outputSlots().get(i));
            if (!output.isEmpty() && (!ItemStack.isSameItemSameComponents(output, result)
                    || output.getCount() + result.getCount() > Math.min(64, output.getMaxStackSize()))) return false;
        }

        var fluidHandler = machine.fluidAutomation(null);
        for (var result : entry.fluidOutputs()) {
            if (fluidHandler == null || result.tank() < 0 || result.tank() >= fluidHandler.getTanks()) return false;
            var existing = fluidHandler.getFluidInTank(result.tank());
            if (!existing.isEmpty()) {
                ResourceLocation existingId = BuiltInRegistries.FLUID.getKey(existing.getFluid());
                if (!result.fluidId().equals(existingId)) return false;
            }
            if ((long)existing.getAmount() + result.amount() > fluidHandler.getTankCapacity(result.tank())) return false;
        }
        return true;
    }

    private static boolean incomingFlow(ForeverStudioMachineBlockEntity machine, String target, Set<String> visiting, int depth) {
        for (var c : incomingAll(machine, target, "flow-in"))
            if (flow(machine, c.fromNode(), c.fromPort(), visiting, depth + 1)) return true;
        return false;
    }
    private static boolean boolInput(ForeverStudioMachineBlockEntity machine, String target, String port, Set<String> visiting, int depth) {
        var c = incoming(machine, target, port);
        return c != null && bool(machine, c.fromNode(), c.fromPort(), visiting, depth + 1);
    }
    private static double numberInput(ForeverStudioMachineBlockEntity machine, String target, String port, Set<String> visiting, int depth) {
        var c = incoming(machine, target, port);
        return c == null ? 0 : number(machine, c.fromNode(), c.fromPort(), visiting, depth + 1);
    }
    private static double numberInputOrProperty(ForeverStudioMachineBlockEntity machine, String target, String port,
                                                ForeverStudioMachineDefinition.GraphNodeDefinition node, double fallback,
                                                Set<String> visiting, int depth) {
        var c = incoming(machine, target, port);
        return c == null ? numeric(node.properties(), port, fallback) : number(machine, c.fromNode(), c.fromPort(), visiting, depth + 1);
    }
    private static boolean boolInputs(ForeverStudioMachineBlockEntity machine, String target, boolean all, Set<String> visiting, int depth) {
        var list = incomingAll(machine, target, "values");
        if (list.isEmpty()) return false;
        for (var c : list) {
            boolean value = bool(machine, c.fromNode(), c.fromPort(), visiting, depth + 1);
            if (all && !value) return false;
            if (!all && value) return true;
        }
        return all;
    }

    private static ForeverStudioMachineDefinition.GraphConnectionDefinition incoming(ForeverStudioMachineBlockEntity machine, String node, String port) {
        return incomingAll(machine, node, port).stream().findFirst().orElse(null);
    }
    private static List<ForeverStudioMachineDefinition.GraphConnectionDefinition> incomingAll(ForeverStudioMachineBlockEntity machine, String node, String port) {
        return machine.definition().graphConnections().stream().filter(c -> c.toNode().equals(node) && c.toPort().equals(port)).toList();
    }
    private static ForeverStudioMachineDefinition.GraphNodeDefinition node(ForeverStudioMachineBlockEntity machine, String id) {
        return machine.definition().graphNodes().stream().filter(candidate -> candidate.id().equals(id)).findFirst().orElse(null);
    }
    private static boolean stateMatches(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineDefinition.GraphNodeDefinition node) {
        return machine.stateId().equalsIgnoreCase(string(node.properties(), "state", string(node.properties(), "stateId", machine.stateId())));
    }
    private static boolean randomChance(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineDefinition.GraphNodeDefinition node) {
        double chance = Math.clamp(numeric(node.properties(), "chance", .5), 0, 1);
        long time = machine.getLevel() == null ? 0 : machine.getLevel().getGameTime();
        long seed = machine.getBlockPos().asLong() ^ time ^ node.id().hashCode();
        seed ^= seed >>> 33; seed *= 0xff51afd7ed558ccdl; seed ^= seed >>> 33;
        return (seed & 0x1fffffffffffffL) / (double)0x1fffffffffffffL < chance;
    }
    private static boolean hasFluid(ForeverStudioMachineBlockEntity machine,
                                    ForeverStudioMachineDefinition.GraphNodeDefinition node,
                                    Set<String> visiting, int depth) {
        int tank = (int)Math.round(numeric(node.properties(), "tank", -1));
        var fluidConnection = incoming(machine, node.id(), "fluid");
        if (fluidConnection != null) {
            for (var definition : machine.definition().tanks())
                if (definition.nodeId().equals(fluidConnection.fromNode())) { tank = definition.tank(); break; }
        }
        if (tank < 0 || tank >= machine.definition().tanks().size()) return false;
        int amount = Math.max(1, (int)Math.round(numberInputOrProperty(machine, node.id(), "amount", node, 1, visiting, depth + 1)));
        String fluidName = string(node.properties(), "fluid", "").trim();
        if (fluidName.isEmpty()) return machine.fluidAmount(tank) >= amount;
        ResourceLocation id = ResourceLocation.tryParse(fluidName);
        return id != null && machine.hasFluid(tank, id, amount);
    }
    private static boolean redstoneController(ForeverStudioMachineBlockEntity machine, String nodeId,
                                              ForeverStudioMachineDefinition.GraphNodeDefinition node,
                                              Set<String> visiting, int depth) {
        var signal = incoming(machine, nodeId, "signal");
        double strength = signal == null ? machine.redstoneStrength()
                : number(machine, signal.fromNode(), signal.fromPort(), visiting, depth + 1);
        return strength > numeric(node.properties(), "threshold", 0);
    }
    private static double fluidValue(ForeverStudioMachineBlockEntity machine, String nodeId, String port) {
        for (var tank : machine.definition().tanks())
            if (tank.nodeId().equals(nodeId)) return port.equals("ratio") ? machine.fluidRatio(tank.tank()) : machine.fluidAmount(tank.tank());
        return 0;
    }
    private static boolean compare(double a, double b, String op) {
        return switch (op) { case ">" -> a > b; case "<" -> a < b; case "<=" -> a <= b;
            case "==", "=" -> Math.abs(a-b) < 1e-9; case "!=", "<>" -> Math.abs(a-b) >= 1e-9; default -> a >= b; };
    }
    private static double progress(ForeverStudioMachineBlockEntity machine) { return Math.clamp((double)machine.progress() / Math.max(1, machine.duration()), 0, 1); }
    private static double ratio(int value, int max) { return max <= 0 ? 0 : Math.clamp((double)value / max, 0, 1); }
    private static double map(double value, double a, double b, double c, double d) {
        if (Math.abs(b-a) < 1e-9) return c;
        double t = Math.clamp((value-a)/(b-a), 0, 1); return c + (d-c)*t;
    }
    private static boolean boolProperty(JsonObject object, String key, boolean fallback) {
        JsonElement value = object.get(key); return value != null && value.isJsonPrimitive() ? value.getAsBoolean() : fallback;
    }
    private static double numeric(JsonObject object, String key, double fallback) {
        JsonElement value = object.get(key); if (value == null || !value.isJsonPrimitive()) return fallback;
        try { double number = value.getAsDouble(); return Double.isFinite(number) ? number : fallback; } catch (RuntimeException ignored) { return fallback; }
    }
    private static String string(JsonObject object, String key, String fallback) {
        JsonElement value = object.get(key); return value != null && value.isJsonPrimitive() ? value.getAsString() : fallback;
    }
    private static boolean isBooleanPort(String port) { return port.equals("running") || port.equals("elapsed") || port.equals("complete") || port.equals("result") || port.equals("powered") || port.equals("found") || port.equals("enabled"); }
    private static boolean isProcessor(String kind) { return kind.equals("function.standard-processor") || kind.equals("function.powered-processor") || kind.equals("function.fluid-processor") || kind.equals("function.generator-core"); }
}

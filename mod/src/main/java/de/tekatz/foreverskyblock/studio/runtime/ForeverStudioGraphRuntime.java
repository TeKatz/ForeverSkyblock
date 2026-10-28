package de.tekatz.foreverskyblock.studio.runtime;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import net.minecraft.resources.ResourceLocation;

import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;

/**
 * Small fail-closed evaluator for the typed Function Graph emitted by Forever Studio.
 * Processing remains deterministic: graph values are queried from authoritative BlockEntity state,
 * and unknown/cyclic nodes evaluate false/zero instead of silently enabling behavior.
 */
public final class ForeverStudioGraphRuntime {
    private static final int MAX_DEPTH = 64;

    private ForeverStudioGraphRuntime() { }

    public static boolean allowsProcessor(ForeverStudioMachineBlockEntity machine) {
        ForeverStudioMachineDefinition definition = machine.definition();
        if (definition.graphNodes().isEmpty() || definition.graphConnections().isEmpty()) return true;
        ForeverStudioMachineDefinition.GraphNodeDefinition processor = definition.graphNodes().stream()
                .filter(node -> isProcessor(node.kind())).findFirst().orElse(null);
        if (processor == null) return false;
        List<ForeverStudioMachineDefinition.GraphConnectionDefinition> incoming = definition.graphConnections().stream()
                .filter(connection -> connection.toNode().equals(processor.id()) && connection.toPort().equals("flow-in"))
                .toList();
        if (incoming.isEmpty()) return false;
        for (ForeverStudioMachineDefinition.GraphConnectionDefinition connection : incoming)
            if (flow(machine, connection.fromNode(), connection.fromPort(), new HashSet<>(), 0)) return true;
        return false;
    }

    public static double animationPhase(ForeverStudioMachineBlockEntity machine,
                                        ForeverStudioMachineDefinition.AnimationDefinition animation) {
        double source;
        if (!animation.sourceNode().isBlank()) {
            source = number(machine, animation.sourceNode(), animation.sourcePort(), new HashSet<>(), 0);
            if (animation.sourcePort().equals("running") || animation.sourcePort().equals("elapsed") ||
                    animation.sourcePort().equals("complete") || animation.sourcePort().equals("result"))
                source = bool(machine, animation.sourceNode(), animation.sourcePort(), new HashSet<>(), 0) ? 1.0 : 0.0;
        } else if (animation.preset().contains("progress")) {
            source = normalizedProgress(machine);
        } else if (machine.running()) {
            long time = machine.getLevel() == null ? 0L : machine.getLevel().getGameTime();
            source = (time % 40L) / 40.0;
        } else {
            source = 0.0;
        }

        String preset = animation.preset().toLowerCase(Locale.ROOT);
        if (preset.contains("blink")) source = source > 0.0 && ((machine.getLevel() == null ? 0L : machine.getLevel().getGameTime()) / 8L) % 2L == 0L ? 1.0 : 0.0;
        else if (preset.contains("pulse") || preset.contains("bob") || preset.contains("oscillate") || preset.contains("ping-pong"))
            source = 0.5 - 0.5 * Math.cos(source * Math.PI * 2.0);
        else if (preset.contains("spin") && animation.sourceNode().isBlank())
            source = machine.running() ? ((machine.getLevel() == null ? 0L : machine.getLevel().getGameTime()) % 20L) / 20.0 : 0.0;

        return Math.clamp(source * animation.scale(), 0.0, 1.0);
    }

    public static boolean booleanValue(ForeverStudioMachineBlockEntity machine, String nodeId, String port) {
        return bool(machine, nodeId, port, new HashSet<>(), 0);
    }

    public static double numberValue(ForeverStudioMachineBlockEntity machine, String nodeId, String port) {
        return number(machine, nodeId, port, new HashSet<>(), 0);
    }

    private static boolean flow(ForeverStudioMachineBlockEntity machine, String nodeId, String port,
                                Set<String> visiting, int depth) {
        if (depth > MAX_DEPTH || !visiting.add("f:" + nodeId + ":" + port)) return false;
        try {
            ForeverStudioMachineDefinition.GraphNodeDefinition node = node(machine, nodeId);
            if (node == null) return false;
            String kind = node.kind();
            return switch (kind) {
                case "core.machine-entry", "function.on-tick" -> true;
                case "function.on-use" -> machine.playerUsePulse();
                case "function.on-inventory-changed" -> machine.inventoryChangedPulse();
                case "function.on-redstone-changed" -> machine.redstoneStrength() > 0;
                case "function.on-neighbor-changed" -> true;
                case "function.on-recipe-start" -> machine.running();
                case "function.on-recipe-complete" -> machine.progress() == 0 && machine.status() == ForeverStudioMachineBlockEntity.RUNNING;
                case "function.on-state-enter" -> stateMatches(machine, node);
                case "function.branch" -> {
                    boolean upstream = incomingFlow(machine, nodeId, visiting, depth + 1);
                    boolean condition = boolInput(machine, nodeId, "condition", visiting, depth + 1);
                    yield upstream && (port.equals("true") ? condition : port.equals("false") && !condition);
                }
                case "function.set-state" -> {
                    boolean upstream = incomingFlow(machine, nodeId, visiting, depth + 1);
                    if (upstream && port.equals("flow-out")) {
                        String target = stringProperty(node.properties(), "state", stringProperty(node.properties(), "targetState", ""));
                        if (!target.isBlank()) machine.setState(target);
                    }
                    yield upstream;
                }
                case "function.consume-item", "function.produce-item", "function.consume-energy",
                     "function.generate-energy", "function.transfer-energy", "function.consume-fluid",
                     "function.produce-fluid", "function.transfer-fluid", "function.start-process",
                     "function.complete-process" -> incomingFlow(machine, nodeId, visiting, depth + 1);
                default -> {
                    if (isProcessor(kind)) yield machine.running() && port.equals("completed");
                    yield incomingFlow(machine, nodeId, visiting, depth + 1);
                }
            };
        } finally {
            visiting.remove("f:" + nodeId + ":" + port);
        }
    }

    private static boolean incomingFlow(ForeverStudioMachineBlockEntity machine, String nodeId,
                                        Set<String> visiting, int depth) {
        for (ForeverStudioMachineDefinition.GraphConnectionDefinition connection : machine.definition().graphConnections()) {
            if (connection.toNode().equals(nodeId) && connection.toPort().equals("flow-in") &&
                    flow(machine, connection.fromNode(), connection.fromPort(), visiting, depth + 1)) return true;
        }
        return false;
    }

    private static boolean bool(ForeverStudioMachineBlockEntity machine, String nodeId, String port,
                                Set<String> visiting, int depth) {
        if (depth > MAX_DEPTH || !visiting.add("b:" + nodeId + ":" + port)) return false;
        try {
            ForeverStudioMachineDefinition.GraphNodeDefinition node = node(machine, nodeId);
            if (node == null) return false;
            String kind = node.kind();
            return switch (kind) {
                case "function.boolean" -> booleanProperty(node.properties(), "value", false);
                case "function.and" -> allBooleanInputs(machine, nodeId, "values", visiting, depth + 1);
                case "function.or" -> anyBooleanInputs(machine, nodeId, "values", visiting, depth + 1);
                case "function.not" -> !boolInput(machine, nodeId, "value", visiting, depth + 1);
                case "function.compare-number" -> compare(
                        numberInput(machine, nodeId, "a", visiting, depth + 1),
                        numberInput(machine, nodeId, "b", visiting, depth + 1),
                        stringProperty(node.properties(), "operator", ">="));
                case "function.compare-state" -> stateMatches(machine, node);
                case "function.random-chance" -> randomChance(machine, node);
                case "function.has-energy" -> machine.energyStored() >= Math.max(0,
                        (int)Math.round(numberInputOrProperty(machine, nodeId, "required", node, 1.0, visiting, depth + 1)));
                case "function.has-fluid" -> hasFluid(machine, node);
                case "function.recipe-available", "function.output-space" -> machine.recipeAvailable();
                case "function.process-progress" -> port.equals("complete")
                        ? machine.progress() >= machine.duration() : machine.running();
                case "function.redstone-input" -> port.equals("powered") && machine.redstoneStrength() > 0;
                case "function.redstone-controller" -> machine.redstoneStrength() > 0;
                case "function.timer" -> port.equals("elapsed") && number(machine, nodeId, "progress", visiting, depth + 1) >= 1.0;
                default -> {
                    if (isProcessor(kind)) yield port.equals("running") && machine.running();
                    yield false;
                }
            };
        } finally {
            visiting.remove("b:" + nodeId + ":" + port);
        }
    }

    private static double number(ForeverStudioMachineBlockEntity machine, String nodeId, String port,
                                 Set<String> visiting, int depth) {
        if (depth > MAX_DEPTH || !visiting.add("n:" + nodeId + ":" + port)) return 0.0;
        try {
            ForeverStudioMachineDefinition.GraphNodeDefinition node = node(machine, nodeId);
            if (node == null) return 0.0;
            String kind = node.kind();
            return switch (kind) {
                case "function.number" -> numberProperty(node.properties(), "value", 0.0);
                case "function.process-duration" -> numberProperty(node.properties(), "value",
                        numberProperty(node.properties(), "ticks", machine.duration()));
                case "function.process-progress" -> normalizedProgress(machine);
                case "function.energy-storage" -> port.equals("ratio")
                        ? ratio(machine.energyStored(), machine.definition().energyCapacity()) : machine.energyStored();
                case "function.fluid-tank" -> fluidNumber(machine, nodeId, port);
                case "function.redstone-input" -> machine.redstoneStrength();
                case "function.clamp" -> {
                    double value = numberInput(machine, nodeId, "value", visiting, depth + 1);
                    double min = numberProperty(node.properties(), "min", 0.0);
                    double max = numberProperty(node.properties(), "max", 1.0);
                    yield Math.clamp(value, Math.min(min, max), Math.max(min, max));
                }
                case "function.map-range" -> mapRange(
                        numberInput(machine, nodeId, "value", visiting, depth + 1),
                        numberProperty(node.properties(), "fromMin", 0.0), numberProperty(node.properties(), "fromMax", 1.0),
                        numberProperty(node.properties(), "toMin", 0.0), numberProperty(node.properties(), "toMax", 1.0));
                case "function.timer" -> timerProgress(machine, node);
                case "function.counter" -> numberProperty(node.properties(), "value", 0.0) +
                        numberInput(machine, nodeId, "delta", visiting, depth + 1);
                default -> isProcessor(kind) && port.equals("progress") ? normalizedProgress(machine) : 0.0;
            };
        } finally {
            visiting.remove("n:" + nodeId + ":" + port);
        }
    }

    private static boolean boolInput(ForeverStudioMachineBlockEntity machine, String targetNode, String targetPort,
                                     Set<String> visiting, int depth) {
        ForeverStudioMachineDefinition.GraphConnectionDefinition connection = incoming(machine, targetNode, targetPort);
        return connection != null && bool(machine, connection.fromNode(), connection.fromPort(), visiting, depth + 1);
    }

    private static double numberInput(ForeverStudioMachineBlockEntity machine, String targetNode, String targetPort,
                                      Set<String> visiting, int depth) {
        ForeverStudioMachineDefinition.GraphConnectionDefinition connection = incoming(machine, targetNode, targetPort);
        return connection == null ? 0.0 : number(machine, connection.fromNode(), connection.fromPort(), visiting, depth + 1);
    }

    private static double numberInputOrProperty(ForeverStudioMachineBlockEntity machine, String targetNode,
                                                String targetPort, ForeverStudioMachineDefinition.GraphNodeDefinition node,
                                                double fallback, Set<String> visiting, int depth) {
        ForeverStudioMachineDefinition.GraphConnectionDefinition connection = incoming(machine, targetNode, targetPort);
        return connection == null ? numberProperty(node.properties(), targetPort, fallback)
                : number(machine, connection.fromNode(), connection.fromPort(), visiting, depth + 1);
    }

    private static boolean allBooleanInputs(ForeverStudioMachineBlockEntity machine, String targetNode, String targetPort,
                                            Set<String> visiting, int depth) {
        List<ForeverStudioMachineDefinition.GraphConnectionDefinition> incoming = incomingAll(machine, targetNode, targetPort);
        if (incoming.isEmpty()) return false;
        for (ForeverStudioMachineDefinition.GraphConnectionDefinition connection : incoming)
            if (!bool(machine, connection.fromNode(), connection.fromPort(), visiting, depth + 1)) return false;
        return true;
    }

    private static boolean anyBooleanInputs(ForeverStudioMachineBlockEntity machine, String targetNode, String targetPort,
                                            Set<String> visiting, int depth) {
        for (ForeverStudioMachineDefinition.GraphConnectionDefinition connection : incomingAll(machine, targetNode, targetPort))
            if (bool(machine, connection.fromNode(), connection.fromPort(), visiting, depth + 1)) return true;
        return false;
    }

    private static ForeverStudioMachineDefinition.GraphConnectionDefinition incoming(
            ForeverStudioMachineBlockEntity machine, String targetNode, String targetPort) {
        return machine.definition().graphConnections().stream()
                .filter(connection -> connection.toNode().equals(targetNode) && connection.toPort().equals(targetPort))
                .findFirst().orElse(null);
    }

    private static List<ForeverStudioMachineDefinition.GraphConnectionDefinition> incomingAll(
            ForeverStudioMachineBlockEntity machine, String targetNode, String targetPort) {
        return machine.definition().graphConnections().stream()
                .filter(connection -> connection.toNode().equals(targetNode) && connection.toPort().equals(targetPort)).toList();
    }

    private static ForeverStudioMachineDefinition.GraphNodeDefinition node(ForeverStudioMachineBlockEntity machine, String id) {
        return machine.definition().graphNodes().stream().filter(node -> node.id().equals(id)).findFirst().orElse(null);
    }

    private static boolean stateMatches(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineDefinition.GraphNodeDefinition node) {
        String expected = stringProperty(node.properties(), "state", stringProperty(node.properties(), "stateId", machine.stateId()));
        return machine.stateId().equalsIgnoreCase(expected);
    }

    private static boolean randomChance(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineDefinition.GraphNodeDefinition node) {
        double chance = Math.clamp(numberProperty(node.properties(), "chance", 0.5), 0.0, 1.0);
        long time = machine.getLevel() == null ? 0L : machine.getLevel().getGameTime();
        long seed = machine.getBlockPos().asLong() ^ time ^ node.id().hashCode();
        seed ^= seed >>> 33; seed *= 0xff51afd7ed558ccdl; seed ^= seed >>> 33;
        double value = (seed & 0x1fffffffffffffL) / (double)0x1fffffffffffffL;
        return value < chance;
    }

    private static boolean hasFluid(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineDefinition.GraphNodeDefinition node) {
        int tank = (int)Math.round(numberProperty(node.properties(), "tank", 0));
        int amount = Math.max(1, (int)Math.round(numberProperty(node.properties(), "amount", 1)));
        ResourceLocation fluid = ResourceLocation.tryParse(stringProperty(node.properties(), "fluid", "minecraft:water"));
        return fluid != null && machine.hasFluid(tank, fluid, amount);
    }

    private static double fluidNumber(ForeverStudioMachineBlockEntity machine, String nodeId, String port) {
        for (ForeverStudioMachineDefinition.TankDefinition tank : machine.definition().tanks())
            if (tank.nodeId().equals(nodeId)) return port.equals("ratio") ? machine.fluidRatio(tank.tank()) : machine.fluidAmount(tank.tank());
        return 0.0;
    }

    private static double timerProgress(ForeverStudioMachineBlockEntity machine, ForeverStudioMachineDefinition.GraphNodeDefinition node) {
        double duration = Math.max(1.0, numberProperty(node.properties(), "duration", numberProperty(node.properties(), "ticks", 20.0)));
        long time = machine.getLevel() == null ? 0L : machine.getLevel().getGameTime();
        return (time % Math.max(1L, Math.round(duration))) / duration;
    }

    private static boolean compare(double a, double b, String operator) {
        return switch (operator) {
            case ">" -> a > b;
            case "<" -> a < b;
            case "<=" -> a <= b;
            case "==", "=" -> Math.abs(a - b) < 1.0e-9;
            case "!=", "<>" -> Math.abs(a - b) >= 1.0e-9;
            default -> a >= b;
        };
    }

    private static double normalizedProgress(ForeverStudioMachineBlockEntity machine) {
        return Math.clamp((double)machine.progress() / Math.max(1, machine.duration()), 0.0, 1.0);
    }
    private static double ratio(int amount, int capacity) { return capacity <= 0 ? 0.0 : Math.clamp((double)amount / capacity, 0.0, 1.0); }
    private static double mapRange(double value, double fromMin, double fromMax, double toMin, double toMax) {
        if (Math.abs(fromMax - fromMin) < 1.0e-9) return toMin;
        double t = Math.clamp((value - fromMin) / (fromMax - fromMin), 0.0, 1.0);
        return toMin + (toMax - toMin) * t;
    }

    private static boolean booleanProperty(JsonObject properties, String name, boolean fallback) {
        JsonElement value = properties.get(name);
        return value != null && value.isJsonPrimitive() ? value.getAsBoolean() : fallback;
    }
    private static double numberProperty(JsonObject properties, String name, double fallback) {
        JsonElement value = properties.get(name);
        if (value == null || !value.isJsonPrimitive()) return fallback;
        try { double result = value.getAsDouble(); return Double.isFinite(result) ? result : fallback; }
        catch (RuntimeException ignored) { return fallback; }
    }
    private static String stringProperty(JsonObject properties, String name, String fallback) {
        JsonElement value = properties.get(name);
        return value != null && value.isJsonPrimitive() ? value.getAsString() : fallback;
    }
    private static boolean isProcessor(String kind) {
        return kind.equals("function.standard-processor") || kind.equals("function.powered-processor") ||
               kind.equals("function.fluid-processor") || kind.equals("function.generator-core");
    }
}

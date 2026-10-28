package de.tekatz.foreverskyblock.studio.runtime;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import net.minecraft.resources.ResourceLocation;

import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/** Immutable data-driven runtime contract compiled from a Forever Studio Machine Project. */
public final class ForeverStudioMachineDefinition {
    public static final int CURRENT_SCHEMA = 2;
    public static final int DEFAULT_GUI_WIDTH = 176;
    public static final int DEFAULT_GUI_HEIGHT = 166;

    public record SlotDefinition(String nodeId, String role, int slot, int x, int y) {
        public boolean input() { return "input".equals(role); }
        public boolean output() { return "output".equals(role); }
        public boolean storage() { return "storage".equals(role); }
    }

    public record TankDefinition(String nodeId, int tank, int capacity, int maxFill, int maxDrain) { }

    public record WidgetDefinition(String id, String type, String sourceNode, String sourcePort,
                                   String label, int x, int y, int width, int height, int slot, int tank) {
        public boolean hasMachineSlot() { return slot >= 0; }
        public boolean hasTank() { return tank >= 0; }
    }

    public record GraphNodeDefinition(String id, String kind, JsonObject properties) { }
    public record GraphConnectionDefinition(String id, String fromNode, String fromPort, String toNode, String toPort) { }
    public record StateDefinition(String id, boolean initial) { }
    public record AnimationDefinition(String id, String preset, String targetComponent, String sourceNode,
                                      String sourcePort, String axis, double scale) { }

    private static final Map<ResourceLocation, ForeverStudioMachineDefinition> CACHE = new ConcurrentHashMap<>();

    private final ResourceLocation machineId;
    private final String displayName;
    private final String processorKind;
    private final boolean powered;
    private final int energyCapacity;
    private final int maxReceive;
    private final int guiWidth;
    private final int guiHeight;
    private final List<SlotDefinition> slots;
    private final List<TankDefinition> tanks;
    private final List<WidgetDefinition> widgets;
    private final List<GraphNodeDefinition> graphNodes;
    private final List<GraphConnectionDefinition> graphConnections;
    private final List<StateDefinition> states;
    private final List<AnimationDefinition> animations;

    private ForeverStudioMachineDefinition(ResourceLocation machineId, String displayName, String processorKind,
                                           boolean powered, int energyCapacity, int maxReceive,
                                           int guiWidth, int guiHeight,
                                           List<SlotDefinition> slots, List<TankDefinition> tanks,
                                           List<WidgetDefinition> widgets, List<GraphNodeDefinition> graphNodes,
                                           List<GraphConnectionDefinition> graphConnections,
                                           List<StateDefinition> states, List<AnimationDefinition> animations) {
        this.machineId = machineId;
        this.displayName = displayName;
        this.processorKind = processorKind;
        this.powered = powered;
        this.energyCapacity = energyCapacity;
        this.maxReceive = maxReceive;
        this.guiWidth = guiWidth;
        this.guiHeight = guiHeight;
        this.slots = List.copyOf(slots);
        this.tanks = List.copyOf(tanks);
        this.widgets = List.copyOf(widgets);
        this.graphNodes = List.copyOf(graphNodes);
        this.graphConnections = List.copyOf(graphConnections);
        this.states = List.copyOf(states);
        this.animations = List.copyOf(animations);
    }

    public static ForeverStudioMachineDefinition load(ResourceLocation machineId) {
        return CACHE.computeIfAbsent(machineId, ForeverStudioMachineDefinition::read);
    }

    public static void clearCache() { CACHE.clear(); }

    private static ForeverStudioMachineDefinition read(ResourceLocation machineId) {
        String resourcePath = "data/" + machineId.getNamespace() + "/forever_studio/runtime/" + machineId.getPath() + ".json";
        ClassLoader loader = ForeverStudioMachineDefinition.class.getClassLoader();
        try (InputStream stream = loader.getResourceAsStream(resourcePath)) {
            if (stream == null)
                throw new IllegalStateException("Missing Forever Studio runtime definition '" + resourcePath + "'.");
            JsonElement rootElement = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8));
            if (!rootElement.isJsonObject())
                throw new IllegalStateException("Forever Studio runtime definition must be a JSON object: " + resourcePath);
            return parse(machineId, rootElement.getAsJsonObject(), resourcePath);
        } catch (IOException ex) {
            throw new IllegalStateException("Cannot read Forever Studio runtime definition '" + resourcePath + "'.", ex);
        }
    }

    private static ForeverStudioMachineDefinition parse(ResourceLocation expectedId, JsonObject root, String source) {
        int schema = requiredInt(root, "schemaVersion", source);
        if (schema < 1 || schema > CURRENT_SCHEMA)
            throw new IllegalStateException("Unsupported Forever Studio runtime schema " + schema + " in " + source + ".");
        if (!expectedId.toString().equals(requiredString(root, "machine", source)))
            throw new IllegalStateException("Runtime definition machine id mismatch in " + source + ".");
        String displayName = requiredString(root, "displayName", source);

        JsonObject processor = requiredObject(root, "processor", source);
        boolean powered = optionalBoolean(processor, "powered", false);
        String processorKind = optionalString(processor, "kind", powered ? "powered" : "standard").toLowerCase(Locale.ROOT);
        if (!List.of("standard", "powered", "fluid", "generator").contains(processorKind))
            throw new IllegalStateException("Unsupported processor kind '" + processorKind + "' in " + source + ".");

        JsonObject energy = root.has("energy") && root.get("energy").isJsonObject()
                ? root.getAsJsonObject("energy") : new JsonObject();
        int capacity = powered ? rangedInt(energy, "capacity", 100_000, 1, 2_000_000_000, source) : 0;
        int maxReceive = powered ? rangedInt(energy, "maxReceive", 4_000, 1, 2_000_000_000, source) : 0;

        JsonObject gui = requiredObject(root, "gui", source);
        int width = rangedInt(gui, "width", DEFAULT_GUI_WIDTH, 64, 512, source);
        int height = rangedInt(gui, "height", DEFAULT_GUI_HEIGHT, 48, 512, source);

        List<SlotDefinition> slots = new ArrayList<>();
        for (JsonElement element : requiredArray(root, "inventory", source)) {
            if (!element.isJsonObject()) throw new IllegalStateException("Invalid inventory entry in " + source + ".");
            JsonObject slot = element.getAsJsonObject();
            String role = requiredString(slot, "role", source).toLowerCase(Locale.ROOT);
            if (!role.equals("input") && !role.equals("output") && !(schema >= 2 && role.equals("storage")))
                throw new IllegalStateException("Unsupported inventory role '" + role + "' in " + source + ".");
            slots.add(new SlotDefinition(requiredString(slot, "nodeId", source), role,
                    rangedInt(slot, "slot", -1, 0, 63, source),
                    rangedInt(slot, "x", 8, 0, width - 1, source),
                    rangedInt(slot, "y", 18, 0, height - 1, source)));
        }
        slots.sort(Comparator.comparingInt(SlotDefinition::slot));
        if (slots.stream().map(SlotDefinition::slot).distinct().count() != slots.size())
            throw new IllegalStateException("Duplicate machine slot index in " + source + ".");
        if (!processorKind.equals("generator") &&
                (slots.stream().noneMatch(SlotDefinition::input) || slots.stream().noneMatch(SlotDefinition::output)))
            throw new IllegalStateException("Runtime processor requires at least one input and one output slot in " + source + ".");
        if (processorKind.equals("generator") && slots.stream().noneMatch(SlotDefinition::input))
            throw new IllegalStateException("Generator runtime requires at least one fuel/input slot in " + source + ".");

        List<TankDefinition> tanks = new ArrayList<>();
        JsonArray tankArray = optionalArray(root, "fluids");
        for (JsonElement element : tankArray) {
            if (!element.isJsonObject()) throw new IllegalStateException("Invalid fluid tank entry in " + source + ".");
            JsonObject tank = element.getAsJsonObject();
            int index = rangedInt(tank, "tank", -1, 0, 7, source);
            int tankCapacity = rangedInt(tank, "capacity", 4_000, 1, 2_000_000_000, source);
            int maxFill = rangedInt(tank, "maxFill", tankCapacity, 1, 2_000_000_000, source);
            int maxDrain = rangedInt(tank, "maxDrain", tankCapacity, 1, 2_000_000_000, source);
            tanks.add(new TankDefinition(requiredString(tank, "nodeId", source), index, tankCapacity, maxFill, maxDrain));
        }
        tanks.sort(Comparator.comparingInt(TankDefinition::tank));
        if (tanks.stream().map(TankDefinition::tank).distinct().count() != tanks.size())
            throw new IllegalStateException("Duplicate fluid tank index in " + source + ".");
        for (int i = 0; i < tanks.size(); i++)
            if (tanks.get(i).tank() != i)
                throw new IllegalStateException("Fluid tank indexes must be contiguous from zero in " + source + ".");
        if (processorKind.equals("fluid") && tanks.isEmpty())
            throw new IllegalStateException("Fluid processor requires at least one runtime tank in " + source + ".");

        List<WidgetDefinition> widgets = new ArrayList<>();
        for (JsonElement element : requiredArray(gui, "widgets", source)) {
            if (!element.isJsonObject()) throw new IllegalStateException("Invalid GUI widget entry in " + source + ".");
            JsonObject widget = element.getAsJsonObject();
            int x = rangedInt(widget, "x", 0, 0, width - 1, source);
            int y = rangedInt(widget, "y", 0, 0, height - 1, source);
            int widgetWidth = rangedInt(widget, "width", 8, 1, width, source);
            int widgetHeight = rangedInt(widget, "height", 8, 1, height, source);
            if (x + widgetWidth > width || y + widgetHeight > height)
                throw new IllegalStateException("GUI widget exceeds the authored canvas in " + source + ".");
            widgets.add(new WidgetDefinition(requiredString(widget, "id", source), requiredString(widget, "type", source),
                    requiredString(widget, "sourceNode", source), requiredString(widget, "sourcePort", source),
                    optionalString(widget, "label", ""), x, y, widgetWidth, widgetHeight,
                    rangedInt(widget, "slot", -1, -1, 63, source),
                    rangedInt(widget, "tank", -1, -1, 7, source)));
        }

        List<StateDefinition> states = new ArrayList<>();
        List<GraphNodeDefinition> graphNodes = new ArrayList<>();
        List<GraphConnectionDefinition> graphConnections = new ArrayList<>();
        if (root.has("graph") && root.get("graph").isJsonObject()) {
            JsonObject graph = root.getAsJsonObject("graph");
            for (JsonElement element : optionalArray(graph, "states")) {
                if (!element.isJsonObject()) throw new IllegalStateException("Invalid graph state in " + source + ".");
                JsonObject state = element.getAsJsonObject();
                states.add(new StateDefinition(requiredString(state, "id", source), optionalBoolean(state, "initial", false)));
            }
            for (JsonElement element : optionalArray(graph, "nodes")) {
                if (!element.isJsonObject()) throw new IllegalStateException("Invalid graph node in " + source + ".");
                JsonObject node = element.getAsJsonObject();
                JsonObject properties = node.has("properties") && node.get("properties").isJsonObject()
                        ? node.getAsJsonObject("properties").deepCopy() : new JsonObject();
                graphNodes.add(new GraphNodeDefinition(requiredString(node, "id", source), requiredString(node, "kind", source), properties));
            }
            for (JsonElement element : optionalArray(graph, "connections")) {
                if (!element.isJsonObject()) throw new IllegalStateException("Invalid graph connection in " + source + ".");
                JsonObject connection = element.getAsJsonObject();
                graphConnections.add(new GraphConnectionDefinition(
                        requiredString(connection, "id", source),
                        requiredString(connection, "fromNode", source), requiredString(connection, "fromPort", source),
                        requiredString(connection, "toNode", source), requiredString(connection, "toPort", source)));
            }
        }
        if (states.isEmpty()) states.add(new StateDefinition("idle", true));
        if (states.stream().filter(StateDefinition::initial).count() != 1)
            throw new IllegalStateException("Runtime graph requires exactly one initial state in " + source + ".");

        List<AnimationDefinition> animations = new ArrayList<>();
        for (JsonElement element : optionalArray(root, "animations")) {
            if (!element.isJsonObject()) throw new IllegalStateException("Invalid animation entry in " + source + ".");
            JsonObject animation = element.getAsJsonObject();
            String axis = optionalString(animation, "axis", "y").toLowerCase(Locale.ROOT);
            if (!List.of("x", "y", "z").contains(axis))
                throw new IllegalStateException("Invalid animation axis '" + axis + "' in " + source + ".");
            double scale = rangedDouble(animation, "scale", 1.0, -10_000.0, 10_000.0, source);
            animations.add(new AnimationDefinition(requiredString(animation, "id", source),
                    requiredString(animation, "preset", source), requiredString(animation, "targetComponent", source),
                    optionalString(animation, "sourceNode", ""), optionalString(animation, "sourcePort", ""), axis, scale));
        }

        return new ForeverStudioMachineDefinition(expectedId, displayName, processorKind, powered,
                capacity, maxReceive, width, height, slots, tanks, widgets,
                graphNodes, graphConnections, states, animations);
    }

    private static JsonObject requiredObject(JsonObject owner, String name, String source) {
        if (!owner.has(name) || !owner.get(name).isJsonObject())
            throw new IllegalStateException("Missing object '" + name + "' in " + source + ".");
        return owner.getAsJsonObject(name);
    }
    private static JsonArray requiredArray(JsonObject owner, String name, String source) {
        if (!owner.has(name) || !owner.get(name).isJsonArray())
            throw new IllegalStateException("Missing array '" + name + "' in " + source + ".");
        return owner.getAsJsonArray(name);
    }
    private static JsonArray optionalArray(JsonObject owner, String name) {
        return owner.has(name) && owner.get(name).isJsonArray() ? owner.getAsJsonArray(name) : new JsonArray();
    }
    private static String requiredString(JsonObject owner, String name, String source) {
        String value = optionalString(owner, name, "");
        if (value.isBlank()) throw new IllegalStateException("Missing string '" + name + "' in " + source + ".");
        return value;
    }
    private static String optionalString(JsonObject owner, String name, String fallback) {
        return owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsString() : fallback;
    }
    private static boolean optionalBoolean(JsonObject owner, String name, boolean fallback) {
        return owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsBoolean() : fallback;
    }
    private static int requiredInt(JsonObject owner, String name, String source) {
        if (!owner.has(name) || !owner.get(name).isJsonPrimitive())
            throw new IllegalStateException("Missing integer '" + name + "' in " + source + ".");
        return owner.get(name).getAsInt();
    }
    private static int rangedInt(JsonObject owner, String name, int fallback, int min, int max, String source) {
        int value = owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsInt() : fallback;
        if (value < min || value > max)
            throw new IllegalStateException("Value '" + name + "' is outside " + min + ".." + max + " in " + source + ".");
        return value;
    }
    private static double rangedDouble(JsonObject owner, String name, double fallback, double min, double max, String source) {
        double value = owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsDouble() : fallback;
        if (!Double.isFinite(value) || value < min || value > max)
            throw new IllegalStateException("Value '" + name + "' is outside " + min + ".." + max + " in " + source + ".");
        return value;
    }

    public ResourceLocation machineId() { return machineId; }
    public String displayName() { return displayName; }
    public String processorKind() { return processorKind; }
    public boolean powered() { return powered; }
    public boolean generator() { return "generator".equals(processorKind); }
    public boolean fluidProcessor() { return "fluid".equals(processorKind); }
    public int energyCapacity() { return energyCapacity; }
    public int maxReceive() { return maxReceive; }
    public int guiWidth() { return guiWidth; }
    public int guiHeight() { return guiHeight; }
    public List<SlotDefinition> slots() { return slots; }
    public List<TankDefinition> tanks() { return tanks; }
    public List<WidgetDefinition> widgets() { return widgets; }
    public List<GraphNodeDefinition> graphNodes() { return graphNodes; }
    public List<GraphConnectionDefinition> graphConnections() { return graphConnections; }
    public List<StateDefinition> states() { return states; }
    public List<AnimationDefinition> animations() { return animations; }
    public int slotCount() { return slots.stream().mapToInt(SlotDefinition::slot).max().orElse(-1) + 1; }
    public int tankCount() { return tanks.size(); }
    public List<Integer> inputSlots() { return slots.stream().filter(SlotDefinition::input).map(SlotDefinition::slot).toList(); }
    public List<Integer> outputSlots() { return slots.stream().filter(SlotDefinition::output).map(SlotDefinition::slot).toList(); }
    public List<Integer> storageSlots() { return slots.stream().filter(SlotDefinition::storage).map(SlotDefinition::slot).toList(); }
    public int inputSlot() { return inputSlots().getFirst(); }
    public int outputSlot() { return outputSlots().getFirst(); }
    public boolean isInputSlot(int slot) { return inputSlots().contains(slot); }
    public boolean isOutputSlot(int slot) { return outputSlots().contains(slot); }
    public boolean isStorageSlot(int slot) { return storageSlots().contains(slot); }
    public int initialStateIndex() {
        for (int i = 0; i < states.size(); i++) if (states.get(i).initial()) return i;
        return 0;
    }
}

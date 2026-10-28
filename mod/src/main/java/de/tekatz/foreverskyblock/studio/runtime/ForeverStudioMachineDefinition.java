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

/** Immutable runtime contract compiled from a Forever Studio Machine Project. */
public final class ForeverStudioMachineDefinition {
    public static final int CURRENT_SCHEMA = 1;
    public static final int DEFAULT_GUI_WIDTH = 176;
    public static final int DEFAULT_GUI_HEIGHT = 166;

    public record SlotDefinition(String nodeId, String role, int slot, int x, int y) {
        public boolean input() { return "input".equals(role); }
        public boolean output() { return "output".equals(role); }
    }

    public record WidgetDefinition(String id, String type, String sourceNode, String sourcePort,
                                   String label, int x, int y, int width, int height, int slot) {
        public boolean hasMachineSlot() { return slot >= 0; }
    }

    private static final Map<ResourceLocation, ForeverStudioMachineDefinition> CACHE = new ConcurrentHashMap<>();
    private final ResourceLocation machineId;
    private final String displayName;
    private final boolean powered;
    private final int energyCapacity;
    private final int maxReceive;
    private final int guiWidth;
    private final int guiHeight;
    private final List<SlotDefinition> slots;
    private final List<WidgetDefinition> widgets;

    private ForeverStudioMachineDefinition(ResourceLocation machineId, String displayName, boolean powered,
                                           int energyCapacity, int maxReceive, int guiWidth, int guiHeight,
                                           List<SlotDefinition> slots, List<WidgetDefinition> widgets) {
        this.machineId = machineId;
        this.displayName = displayName;
        this.powered = powered;
        this.energyCapacity = energyCapacity;
        this.maxReceive = maxReceive;
        this.guiWidth = guiWidth;
        this.guiHeight = guiHeight;
        this.slots = List.copyOf(slots);
        this.widgets = List.copyOf(widgets);
    }

    public static ForeverStudioMachineDefinition load(ResourceLocation machineId) {
        return CACHE.computeIfAbsent(machineId, ForeverStudioMachineDefinition::read);
    }
    public static void clearCache() { CACHE.clear(); }

    private static ForeverStudioMachineDefinition read(ResourceLocation machineId) {
        String resourcePath = "data/" + machineId.getNamespace() + "/forever_studio/runtime/" + machineId.getPath() + ".json";
        ClassLoader loader = ForeverStudioMachineDefinition.class.getClassLoader();
        try (InputStream stream = loader.getResourceAsStream(resourcePath)) {
            if (stream == null) throw new IllegalStateException("Missing Forever Studio runtime definition '" + resourcePath + "'.");
            JsonElement rootElement = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8));
            if (!rootElement.isJsonObject()) throw new IllegalStateException("Forever Studio runtime definition must be a JSON object: " + resourcePath);
            return parse(machineId, rootElement.getAsJsonObject(), resourcePath);
        } catch (IOException ex) {
            throw new IllegalStateException("Cannot read Forever Studio runtime definition '" + resourcePath + "'.", ex);
        }
    }

    private static ForeverStudioMachineDefinition parse(ResourceLocation expectedId, JsonObject root, String source) {
        int schema = requiredInt(root, "schemaVersion", source);
        if (schema != CURRENT_SCHEMA) throw new IllegalStateException("Unsupported Forever Studio runtime schema " + schema + " in " + source + ".");
        if (!expectedId.toString().equals(requiredString(root, "machine", source))) throw new IllegalStateException("Runtime definition machine id mismatch in " + source + ".");
        String displayName = requiredString(root, "displayName", source);
        JsonObject processor = requiredObject(root, "processor", source);
        boolean powered = optionalBoolean(processor, "powered", false);
        JsonObject energy = root.has("energy") && root.get("energy").isJsonObject() ? root.getAsJsonObject("energy") : new JsonObject();
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
            if (!role.equals("input") && !role.equals("output")) throw new IllegalStateException("Unsupported inventory role '" + role + "' in " + source + ".");
            slots.add(new SlotDefinition(requiredString(slot, "nodeId", source), role,
                    rangedInt(slot, "slot", -1, 0, 63, source),
                    rangedInt(slot, "x", 8, 0, width - 1, source),
                    rangedInt(slot, "y", 18, 0, height - 1, source)));
        }
        slots.sort(Comparator.comparingInt(SlotDefinition::slot));
        if (slots.stream().map(SlotDefinition::slot).distinct().count() != slots.size()) throw new IllegalStateException("Duplicate machine slot index in " + source + ".");
        if (slots.stream().noneMatch(SlotDefinition::input) || slots.stream().noneMatch(SlotDefinition::output)) throw new IllegalStateException("Runtime processor requires at least one input and one output slot in " + source + ".");

        List<WidgetDefinition> widgets = new ArrayList<>();
        for (JsonElement element : requiredArray(gui, "widgets", source)) {
            if (!element.isJsonObject()) throw new IllegalStateException("Invalid GUI widget entry in " + source + ".");
            JsonObject widget = element.getAsJsonObject();
            int x = rangedInt(widget, "x", 0, 0, width - 1, source), y = rangedInt(widget, "y", 0, 0, height - 1, source);
            int widgetWidth = rangedInt(widget, "width", 8, 1, width, source), widgetHeight = rangedInt(widget, "height", 8, 1, height, source);
            if (x + widgetWidth > width || y + widgetHeight > height) throw new IllegalStateException("GUI widget exceeds the authored canvas in " + source + ".");
            widgets.add(new WidgetDefinition(requiredString(widget, "id", source), requiredString(widget, "type", source),
                    requiredString(widget, "sourceNode", source), requiredString(widget, "sourcePort", source),
                    optionalString(widget, "label", ""), x, y, widgetWidth, widgetHeight,
                    rangedInt(widget, "slot", -1, -1, 63, source)));
        }
        return new ForeverStudioMachineDefinition(expectedId, displayName, powered, capacity, maxReceive, width, height, slots, widgets);
    }

    private static JsonObject requiredObject(JsonObject owner, String name, String source) { if (!owner.has(name) || !owner.get(name).isJsonObject()) throw new IllegalStateException("Missing object '" + name + "' in " + source + "."); return owner.getAsJsonObject(name); }
    private static JsonArray requiredArray(JsonObject owner, String name, String source) { if (!owner.has(name) || !owner.get(name).isJsonArray()) throw new IllegalStateException("Missing array '" + name + "' in " + source + "."); return owner.getAsJsonArray(name); }
    private static String requiredString(JsonObject owner, String name, String source) { String value = optionalString(owner, name, ""); if (value.isBlank()) throw new IllegalStateException("Missing string '" + name + "' in " + source + "."); return value; }
    private static String optionalString(JsonObject owner, String name, String fallback) { return owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsString() : fallback; }
    private static boolean optionalBoolean(JsonObject owner, String name, boolean fallback) { return owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsBoolean() : fallback; }
    private static int requiredInt(JsonObject owner, String name, String source) { if (!owner.has(name) || !owner.get(name).isJsonPrimitive()) throw new IllegalStateException("Missing integer '" + name + "' in " + source + "."); return owner.get(name).getAsInt(); }
    private static int rangedInt(JsonObject owner, String name, int fallback, int min, int max, String source) { int value = owner.has(name) && owner.get(name).isJsonPrimitive() ? owner.get(name).getAsInt() : fallback; if (value < min || value > max) throw new IllegalStateException("Value '" + name + "' is outside " + min + ".." + max + " in " + source + "."); return value; }

    public ResourceLocation machineId() { return machineId; }
    public String displayName() { return displayName; }
    public boolean powered() { return powered; }
    public int energyCapacity() { return energyCapacity; }
    public int maxReceive() { return maxReceive; }
    public int guiWidth() { return guiWidth; }
    public int guiHeight() { return guiHeight; }
    public List<SlotDefinition> slots() { return slots; }
    public List<WidgetDefinition> widgets() { return widgets; }
    public int slotCount() { return slots.stream().mapToInt(SlotDefinition::slot).max().orElse(-1) + 1; }
    public List<Integer> inputSlots() { return slots.stream().filter(SlotDefinition::input).map(SlotDefinition::slot).toList(); }
    public List<Integer> outputSlots() { return slots.stream().filter(SlotDefinition::output).map(SlotDefinition::slot).toList(); }
    /** Compatibility helpers for older generated menus/tests. */
    public int inputSlot() { return inputSlots().getFirst(); }
    public int outputSlot() { return outputSlots().getFirst(); }
    public boolean isInputSlot(int slot) { return inputSlots().contains(slot); }
    public boolean isOutputSlot(int slot) { return outputSlots().contains(slot); }
}

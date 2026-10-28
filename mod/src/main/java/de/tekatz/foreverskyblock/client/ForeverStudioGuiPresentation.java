package de.tekatz.foreverskyblock.client;

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
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/** Client-side presentation sidecar compiled from the shared Forever Studio GUI document. */
final class ForeverStudioGuiPresentation {
    static final int CURRENT_SCHEMA = 1;

    record Effect(String kind, JsonObject properties) {
        double number(String key, double fallback) {
            if (!properties.has(key) || !properties.get(key).isJsonPrimitive()) return fallback;
            try {
                double value = properties.get(key).getAsDouble();
                return Double.isFinite(value) ? value : fallback;
            } catch (RuntimeException ex) {
                return fallback;
            }
        }

        String text(String key, String fallback) {
            if (!properties.has(key) || !properties.get(key).isJsonPrimitive()) return fallback;
            try { return properties.get(key).getAsString(); }
            catch (RuntimeException ex) { return fallback; }
        }
    }

    record ElementPresentation(JsonObject style, List<Effect> effects) {
        static final ElementPresentation EMPTY = new(new JsonObject(), List.of());

        double opacity() {
            if (!style.has("opacity") || !style.get("opacity").isJsonPrimitive()) return 1.0;
            try {
                double value = style.get("opacity").getAsDouble();
                return Double.isFinite(value) ? Math.max(0.0, Math.min(1.0, value)) : 1.0;
            } catch (RuntimeException ex) {
                return 1.0;
            }
        }

        String tint() {
            if (!style.has("tint") || !style.get("tint").isJsonPrimitive()) return "";
            try { return style.get("tint").getAsString(); }
            catch (RuntimeException ex) { return ""; }
        }

        Effect effect(String kind) {
            for (Effect effect : effects)
                if (effect.kind().equalsIgnoreCase(kind)) return effect;
            return null;
        }
    }

    private static final ForeverStudioGuiPresentation EMPTY = new(Map.of());
    private static final Map<ResourceLocation, ForeverStudioGuiPresentation> CACHE = new ConcurrentHashMap<>();

    private final Map<String, ElementPresentation> elements;

    private ForeverStudioGuiPresentation(Map<String, ElementPresentation> elements) {
        this.elements = Map.copyOf(elements);
    }

    static ForeverStudioGuiPresentation load(ResourceLocation machineId) {
        return CACHE.computeIfAbsent(machineId, ForeverStudioGuiPresentation::read);
    }

    static void clearCache() { CACHE.clear(); }

    ElementPresentation element(String widgetId) {
        return elements.getOrDefault(widgetId, ElementPresentation.EMPTY);
    }

    private static ForeverStudioGuiPresentation read(ResourceLocation machineId) {
        String resourcePath = "data/" + machineId.getNamespace() + "/forever_studio/gui/" + machineId.getPath() + ".json";
        ClassLoader loader = ForeverStudioGuiPresentation.class.getClassLoader();
        try (InputStream stream = loader.getResourceAsStream(resourcePath)) {
            // Older generated runtime outputs did not have a presentation sidecar. Preserve their
            // previous native rendering rather than making resource-pack reload compatibility fail.
            if (stream == null) return EMPTY;
            JsonElement parsed = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8));
            if (!parsed.isJsonObject())
                throw new IllegalStateException("Forever Studio GUI presentation must be an object: " + resourcePath);
            JsonObject root = parsed.getAsJsonObject();
            int schema = requiredInt(root, "schemaVersion", resourcePath);
            if (schema != CURRENT_SCHEMA)
                throw new IllegalStateException("Unsupported Forever Studio GUI presentation schema " + schema + " in " + resourcePath + ".");
            if (!machineId.toString().equals(requiredString(root, "machine", resourcePath)))
                throw new IllegalStateException("GUI presentation machine id mismatch in " + resourcePath + ".");
            JsonObject sourceElements = requiredObject(root, "elements", resourcePath);
            Map<String, ElementPresentation> elements = new java.util.HashMap<>();
            for (Map.Entry<String, JsonElement> entry : sourceElements.entrySet()) {
                if (!entry.getValue().isJsonObject())
                    throw new IllegalStateException("Invalid GUI presentation element '" + entry.getKey() + "' in " + resourcePath + ".");
                JsonObject source = entry.getValue().getAsJsonObject();
                JsonObject style = source.has("style") && source.get("style").isJsonObject()
                        ? source.getAsJsonObject("style").deepCopy() : new JsonObject();
                List<Effect> effects = new ArrayList<>();
                JsonArray array = source.has("effects") && source.get("effects").isJsonArray()
                        ? source.getAsJsonArray("effects") : new JsonArray();
                for (JsonElement effectElement : array) {
                    if (!effectElement.isJsonObject())
                        throw new IllegalStateException("Invalid GUI effect for '" + entry.getKey() + "' in " + resourcePath + ".");
                    JsonObject effect = effectElement.getAsJsonObject();
                    String kind = requiredString(effect, "kind", resourcePath);
                    JsonObject properties = effect.has("properties") && effect.get("properties").isJsonObject()
                            ? effect.getAsJsonObject("properties").deepCopy() : new JsonObject();
                    effects.add(new Effect(kind, properties));
                }
                elements.put(entry.getKey(), new ElementPresentation(style, List.copyOf(effects)));
            }
            return new ForeverStudioGuiPresentation(elements);
        } catch (IOException ex) {
            throw new IllegalStateException("Cannot read Forever Studio GUI presentation '" + resourcePath + "'.", ex);
        }
    }

    private static JsonObject requiredObject(JsonObject owner, String name, String source) {
        if (!owner.has(name) || !owner.get(name).isJsonObject())
            throw new IllegalStateException("Missing object '" + name + "' in " + source + ".");
        return owner.getAsJsonObject(name);
    }

    private static String requiredString(JsonObject owner, String name, String source) {
        if (!owner.has(name) || !owner.get(name).isJsonPrimitive())
            throw new IllegalStateException("Missing string '" + name + "' in " + source + ".");
        String value = owner.get(name).getAsString();
        if (value.isBlank()) throw new IllegalStateException("Missing string '" + name + "' in " + source + ".");
        return value;
    }

    private static int requiredInt(JsonObject owner, String name, String source) {
        if (!owner.has(name) || !owner.get(name).isJsonPrimitive())
            throw new IllegalStateException("Missing integer '" + name + "' in " + source + ".");
        return owner.get(name).getAsInt();
    }
}

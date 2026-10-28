package de.tekatz.foreverskyblock.client;

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

/** Client-only lookup for Studio component model manifests emitted by the runtime compiler. */
public final class ForeverStudioMachineComponentModels {
    public record ComponentModel(String id, ResourceLocation model) { }

    private static final Map<ResourceLocation, List<ComponentModel>> CACHE = new ConcurrentHashMap<>();

    private ForeverStudioMachineComponentModels() { }

    public static List<ComponentModel> modelsFor(ResourceLocation machineId) {
        return CACHE.computeIfAbsent(machineId, ForeverStudioMachineComponentModels::read);
    }

    public static void clearCache() {
        CACHE.clear();
    }

    private static List<ComponentModel> read(ResourceLocation machineId) {
        String resourcePath = "assets/" + machineId.getNamespace() + "/forever_studio/components/"
                + machineId.getPath() + ".json";
        ClassLoader loader = ForeverStudioMachineComponentModels.class.getClassLoader();
        try (InputStream stream = loader.getResourceAsStream(resourcePath)) {
            if (stream == null) return List.of();
            var root = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8));
            if (!root.isJsonObject()) return List.of();
            var object = root.getAsJsonObject();
            if (!object.has("machine") || !machineId.toString().equals(object.get("machine").getAsString())
                    || !object.has("components") || !object.get("components").isJsonArray()) return List.of();

            var result = new ArrayList<ComponentModel>();
            for (var element : object.getAsJsonArray("components")) {
                if (!element.isJsonObject()) continue;
                var component = element.getAsJsonObject();
                if (!component.has("id") || !component.has("model")) continue;
                String id = component.get("id").getAsString();
                ResourceLocation model = ResourceLocation.tryParse(component.get("model").getAsString());
                if (!id.isBlank() && model != null) result.add(new ComponentModel(id, model));
            }
            return List.copyOf(result);
        } catch (IOException | RuntimeException ignored) {
            return List.of();
        }
    }
}

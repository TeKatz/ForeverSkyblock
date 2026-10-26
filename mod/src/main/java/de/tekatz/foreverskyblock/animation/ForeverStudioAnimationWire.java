package de.tekatz.foreverskyblock.animation;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.util.Collection;
import java.util.LinkedHashMap;
import java.util.Map;

/** Canonical play-channel representation of the already validated format-v1 runtime snapshot. */
public final class ForeverStudioAnimationWire {
    public static final int MAX_DOCUMENTS = 128;
    public static final int MAX_JSON_CHARS = 1_500_000;
    private static final Gson GSON = new GsonBuilder().disableHtmlEscaping().create();

    private ForeverStudioAnimationWire() { }

    public static String encodeSnapshot(Collection<ForeverStudioAnimationDocument> documents) {
        if (documents == null || documents.size() > MAX_DOCUMENTS)
            throw new IllegalArgumentException("Invalid Forever Studio animation snapshot size");

        JsonArray array = new JsonArray();
        documents.stream()
                .sorted(java.util.Comparator.comparing(document ->
                        ForeverStudioAnimationRegistry.normalize(document.assetId())))
                .forEach(document -> array.add(GSON.toJsonTree(document)));
        String json = GSON.toJson(array);
        if (json.length() > MAX_JSON_CHARS)
            throw new IllegalArgumentException("Forever Studio animation snapshot exceeds the wire limit");
        return json;
    }

    public static Map<String, ForeverStudioAnimationDocument> decodeSnapshot(String json) {
        if (json == null || json.length() > MAX_JSON_CHARS)
            throw new IllegalArgumentException("Invalid Forever Studio animation snapshot payload");

        JsonElement root = JsonParser.parseString(json);
        if (!root.isJsonArray())
            throw new IllegalArgumentException("Forever Studio animation snapshot root must be an array");
        JsonArray array = root.getAsJsonArray();
        if (array.size() > MAX_DOCUMENTS)
            throw new IllegalArgumentException("Forever Studio animation snapshot contains too many documents");

        Map<String, ForeverStudioAnimationDocument> result = new LinkedHashMap<>();
        for (int index = 0; index < array.size(); index++) {
            JsonElement element = array.get(index);
            if (!element.isJsonObject())
                throw new IllegalArgumentException("Forever Studio animation snapshot entry " + index + " must be an object");
            JsonObject object = element.getAsJsonObject();
            ForeverStudioAnimationDocument document = ForeverStudioAnimationParser.parse(object);
            String key = ForeverStudioAnimationRegistry.normalize(document.assetId());
            if (result.putIfAbsent(key, document) != null)
                throw new IllegalArgumentException("Duplicate Forever Studio animation asset '" + document.assetId() + "'");
        }
        return Map.copyOf(result);
    }
}

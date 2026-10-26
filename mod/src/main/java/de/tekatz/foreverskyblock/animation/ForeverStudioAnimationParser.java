package de.tekatz.foreverskyblock.animation;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.io.Reader;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;

import static de.tekatz.foreverskyblock.animation.ForeverStudioAnimationDocument.*;

/** Strict format-v1 parser shared by datapack loading and unit tests. */
public final class ForeverStudioAnimationParser {
    public static final int CURRENT_FORMAT_VERSION = 1;

    private ForeverStudioAnimationParser() { }

    public static ForeverStudioAnimationDocument parse(Reader reader) {
        JsonElement root = JsonParser.parseReader(reader);
        if (!root.isJsonObject()) throw invalid("JSON root must be an object");
        return parse(root.getAsJsonObject());
    }

    public static ForeverStudioAnimationDocument parse(JsonObject root) {
        int formatVersion = requiredInt(root, "formatVersion");
        if (formatVersion != CURRENT_FORMAT_VERSION)
            throw invalid("Unsupported formatVersion " + formatVersion + "; runtime supports " + CURRENT_FORMAT_VERSION);

        String assetId = requiredString(root, "assetId").trim();
        String sourceModel = requiredString(root, "sourceModel").trim().replace('\\', '/');
        JsonObject animationObject = requiredObject(root, "animation");
        Animation animation = parseAnimation(animationObject);

        if (assetId.isEmpty()) throw invalid("assetId is required");
        if (sourceModel.isEmpty()) throw invalid("sourceModel is required");
        return new ForeverStudioAnimationDocument(formatVersion, assetId, sourceModel, animation);
    }

    private static Animation parseAnimation(JsonObject object) {
        String id = requiredString(object, "id").trim();
        if (id.isEmpty()) throw invalid("animation.id is required");
        String displayName = optionalString(object, "displayName", "").trim();
        double duration = requiredDouble(object, "durationSeconds");
        if (!Double.isFinite(duration) || duration < 0.1D || duration > 120D)
            throw invalid("animation.durationSeconds must be finite and between 0.1 and 120");
        boolean loop = optionalBoolean(object, "loop", true);

        List<Track> tracks = parseTracks(optionalArray(object, "tracks"), duration);
        Map<String, String> runtimeStateBase = parseStringMap(optionalObject(object, "runtimeStateBase"));
        List<StatusTrack> statusTracks = parseStatusTracks(optionalArray(object, "statusTracks"), duration);
        List<EffectTrack> effectTracks = parseEffectTracks(optionalArray(object, "effectTracks"), duration);
        return new Animation(id, displayName, duration, loop, tracks, runtimeStateBase, statusTracks, effectTracks);
    }

    private static List<Track> parseTracks(JsonArray array, double duration) {
        List<Track> result = new ArrayList<>();
        Set<String> stableIds = new HashSet<>();
        for (int index = 0; index < array.size(); index++) {
            JsonObject object = objectAt(array, index, "animation.tracks");
            String targetId = optionalString(object, "targetId", "").trim();
            String modelKey = optionalString(object, "modelKey", "").trim().replace('\\', '/');
            int elementIndex = optionalInt(object, "elementIndex", -1);
            if (targetId.isEmpty() && modelKey.isEmpty())
                throw invalid("animation.tracks[" + index + "] requires targetId or legacy modelKey");
            if (!targetId.isEmpty() && !stableIds.add(targetId))
                throw invalid("Duplicate stable targetId '" + targetId + "'");
            if (elementIndex < -1)
                throw invalid("animation.tracks[" + index + "].elementIndex cannot be below -1");
            result.add(new Track(targetId, modelKey, elementIndex,
                    parseKeyframes(optionalArray(object, "keyframes"), duration, targetId.isEmpty() ? modelKey : targetId)));
        }
        return List.copyOf(result);
    }

    private static List<Keyframe> parseKeyframes(JsonArray array, double duration, String trackLabel) {
        List<Keyframe> result = new ArrayList<>();
        Set<Long> times = new HashSet<>();
        for (int index = 0; index < array.size(); index++) {
            JsonObject object = objectAt(array, index, "keyframes");
            double time = requiredDouble(object, "timeSeconds");
            validateTime(time, "Track '" + trackLabel + "'");
            if (!times.add(timeKey(time))) throw invalid("Track '" + trackLabel + "' contains duplicate keyframe times");

            double[] position = vector(object, "position", new double[] {0, 0, 0});
            String axis = optionalString(object, "rotationAxis", "y").trim().toLowerCase(Locale.ROOT);
            if (!(axis.equals("x") || axis.equals("y") || axis.equals("z")))
                throw invalid("Track '" + trackLabel + "' contains invalid rotation axis '" + axis + "'");
            double rotation = optionalDouble(object, "rotationDegrees", 0D);
            if (!Double.isFinite(rotation)) throw invalid("Track '" + trackLabel + "' contains non-finite rotationDegrees");
            double[] scale = vector(object, "scale", new double[] {1, 1, 1});
            boolean visible = optionalBoolean(object, "visible", true);
            String textureState = optionalString(object, "textureState", "");
            String lightStatus = optionalString(object, "lightStatus", "");
            String specialEffect = optionalString(object, "specialEffect", "");
            String machineState = optionalString(object, "machineState", "Idle");
            String interpolationName = optionalString(object, "interpolation", "Smooth").trim().toUpperCase(Locale.ROOT);
            Interpolation interpolation;
            try {
                interpolation = Interpolation.valueOf(interpolationName);
            } catch (IllegalArgumentException exception) {
                throw invalid("Track '" + trackLabel + "' contains invalid interpolation '" + interpolationName + "'");
            }
            result.add(new Keyframe(time, position, axis, rotation, scale, visible,
                    textureState, lightStatus, specialEffect, machineState, interpolation));
        }
        result.sort(java.util.Comparator.comparingDouble(Keyframe::timeSeconds));
        return List.copyOf(result);
    }

    private static List<StatusTrack> parseStatusTracks(JsonArray array, double duration) {
        List<StatusTrack> result = new ArrayList<>();
        Set<String> ids = new HashSet<>();
        for (int index = 0; index < array.size(); index++) {
            JsonObject object = objectAt(array, index, "statusTracks");
            String axisId = requiredString(object, "axisId").trim();
            if (axisId.isEmpty()) throw invalid("A status track is missing axisId");
            if (!ids.add(axisId.toLowerCase(Locale.ROOT))) throw invalid("Duplicate status track '" + axisId + "'");
            List<StatusKeyframe> frames = new ArrayList<>();
            Set<Long> times = new HashSet<>();
            JsonArray keys = optionalArray(object, "keyframes");
            for (int keyIndex = 0; keyIndex < keys.size(); keyIndex++) {
                JsonObject key = objectAt(keys, keyIndex, "status keyframes");
                double time = requiredDouble(key, "timeSeconds");
                validateTime(time, "Status track '" + axisId + "'");
                if (!times.add(timeKey(time))) throw invalid("Status track '" + axisId + "' contains duplicate keyframe times");
                String optionId = requiredString(key, "optionId").trim();
                if (optionId.isEmpty()) throw invalid("Status track '" + axisId + "' contains an empty optionId");
                frames.add(new StatusKeyframe(time, optionId));
            }
            frames.sort(java.util.Comparator.comparingDouble(StatusKeyframe::timeSeconds));
            result.add(new StatusTrack(axisId, frames));
        }
        return List.copyOf(result);
    }

    private static List<EffectTrack> parseEffectTracks(JsonArray array, double duration) {
        List<EffectTrack> result = new ArrayList<>();
        Set<String> ids = new HashSet<>();
        for (int index = 0; index < array.size(); index++) {
            JsonObject object = objectAt(array, index, "effectTracks");
            String effectId = requiredString(object, "effectId").trim();
            if (effectId.isEmpty()) throw invalid("An effect track is missing effectId");
            if (!ids.add(effectId.toLowerCase(Locale.ROOT))) throw invalid("Duplicate effect track '" + effectId + "'");
            List<EffectKeyframe> frames = new ArrayList<>();
            Set<Long> times = new HashSet<>();
            JsonArray keys = optionalArray(object, "keyframes");
            for (int keyIndex = 0; keyIndex < keys.size(); keyIndex++) {
                JsonObject key = objectAt(keys, keyIndex, "effect keyframes");
                double time = requiredDouble(key, "timeSeconds");
                validateTime(time, "Effect track '" + effectId + "'");
                if (!times.add(timeKey(time))) throw invalid("Effect track '" + effectId + "' contains duplicate keyframe times");
                String modeName = requiredString(key, "mode").trim().toUpperCase(Locale.ROOT);
                EffectMode mode;
                try {
                    mode = EffectMode.valueOf(modeName);
                } catch (IllegalArgumentException exception) {
                    throw invalid("Unknown effect mode '" + modeName.toLowerCase(Locale.ROOT) + "'");
                }
                frames.add(new EffectKeyframe(time, mode));
            }
            frames.sort(java.util.Comparator.comparingDouble(EffectKeyframe::timeSeconds));
            result.add(new EffectTrack(effectId, frames));
        }
        return List.copyOf(result);
    }

    private static Map<String, String> parseStringMap(JsonObject object) {
        Map<String, String> result = new LinkedHashMap<>();
        for (Map.Entry<String, JsonElement> entry : object.entrySet()) {
            if (!entry.getValue().isJsonPrimitive() || !entry.getValue().getAsJsonPrimitive().isString())
                throw invalid("runtimeStateBase.'" + entry.getKey() + "' must be a string");
            String key = entry.getKey().trim();
            String value = entry.getValue().getAsString().trim();
            if (key.isEmpty() || value.isEmpty()) throw invalid("runtimeStateBase cannot contain empty keys or values");
            result.put(key, value);
        }
        return Map.copyOf(result);
    }

    private static void validateTime(double time, String label) {
        if (!Double.isFinite(time) || time < 0D) throw invalid(label + " contains an invalid keyframe time");
        // Format v1 deliberately preserves keys beyond duration. Studio reports those as warnings;
        // runtime loading must not turn a losslessly saved warning into a rejected document.
    }

    private static long timeKey(double time) { return Math.round(time * 1_000_000D); }

    private static double[] vector(JsonObject object, String name, double[] fallback) {
        if (!object.has(name)) return fallback.clone();
        JsonElement element = object.get(name);
        if (!element.isJsonArray() || element.getAsJsonArray().size() != 3)
            throw invalid(name + " must contain exactly three numbers");
        double[] values = new double[3];
        for (int i = 0; i < 3; i++) {
            try {
                values[i] = element.getAsJsonArray().get(i).getAsDouble();
            } catch (RuntimeException exception) {
                throw invalid(name + " must contain exactly three numbers");
            }
            if (!Double.isFinite(values[i])) throw invalid(name + " contains a non-finite number");
        }
        return values;
    }

    private static JsonObject requiredObject(JsonObject owner, String name) {
        JsonElement value = owner.get(name);
        if (value == null || !value.isJsonObject()) throw invalid(name + " must be an object");
        return value.getAsJsonObject();
    }

    private static JsonObject optionalObject(JsonObject owner, String name) {
        JsonElement value = owner.get(name);
        if (value == null || value.isJsonNull()) return new JsonObject();
        if (!value.isJsonObject()) throw invalid(name + " must be an object");
        return value.getAsJsonObject();
    }

    private static JsonArray optionalArray(JsonObject owner, String name) {
        JsonElement value = owner.get(name);
        if (value == null || value.isJsonNull()) return new JsonArray();
        if (!value.isJsonArray()) throw invalid(name + " must be an array");
        return value.getAsJsonArray();
    }

    private static JsonObject objectAt(JsonArray array, int index, String label) {
        JsonElement value = array.get(index);
        if (!value.isJsonObject()) throw invalid(label + "[" + index + "] must be an object");
        return value.getAsJsonObject();
    }

    private static String requiredString(JsonObject owner, String name) {
        JsonElement value = owner.get(name);
        if (value == null || !value.isJsonPrimitive() || !value.getAsJsonPrimitive().isString())
            throw invalid(name + " must be a string");
        return value.getAsString();
    }

    private static String optionalString(JsonObject owner, String name, String fallback) {
        JsonElement value = owner.get(name);
        if (value == null || value.isJsonNull()) return fallback;
        if (!value.isJsonPrimitive() || !value.getAsJsonPrimitive().isString()) throw invalid(name + " must be a string");
        return value.getAsString();
    }

    private static int requiredInt(JsonObject owner, String name) {
        JsonElement value = owner.get(name);
        if (value == null || !value.isJsonPrimitive() || !value.getAsJsonPrimitive().isNumber()) throw invalid(name + " must be an integer");
        try { return value.getAsInt(); } catch (RuntimeException exception) { throw invalid(name + " must be an integer"); }
    }

    private static int optionalInt(JsonObject owner, String name, int fallback) {
        if (!owner.has(name) || owner.get(name).isJsonNull()) return fallback;
        try { return owner.get(name).getAsInt(); } catch (RuntimeException exception) { throw invalid(name + " must be an integer"); }
    }

    private static double requiredDouble(JsonObject owner, String name) {
        JsonElement value = owner.get(name);
        if (value == null || !value.isJsonPrimitive() || !value.getAsJsonPrimitive().isNumber()) throw invalid(name + " must be a number");
        try { return value.getAsDouble(); } catch (RuntimeException exception) { throw invalid(name + " must be a number"); }
    }

    private static double optionalDouble(JsonObject owner, String name, double fallback) {
        if (!owner.has(name) || owner.get(name).isJsonNull()) return fallback;
        try { return owner.get(name).getAsDouble(); } catch (RuntimeException exception) { throw invalid(name + " must be a number"); }
    }

    private static boolean optionalBoolean(JsonObject owner, String name, boolean fallback) {
        if (!owner.has(name) || owner.get(name).isJsonNull()) return fallback;
        JsonElement value = owner.get(name);
        if (!value.isJsonPrimitive() || !value.getAsJsonPrimitive().isBoolean()) throw invalid(name + " must be a boolean");
        return value.getAsBoolean();
    }

    private static IllegalArgumentException invalid(String message) {
        return new IllegalArgumentException("Forever Studio animation: " + message);
    }
}

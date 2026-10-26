package de.tekatz.foreverskyblock.animation;

import java.util.List;
import java.util.Map;

/**
 * Immutable runtime view of Forever Studio animation format v1.
 * Unknown JSON properties are intentionally ignored by the parser so format extensions can remain
 * forward-compatible while formatVersion still guards breaking schema changes.
 */
public record ForeverStudioAnimationDocument(
        int formatVersion,
        String assetId,
        String sourceModel,
        Animation animation) {

    public ForeverStudioAnimationDocument {
        assetId = assetId == null ? "" : assetId;
        sourceModel = sourceModel == null ? "" : sourceModel;
        animation = animation == null ? Animation.empty() : animation;
    }

    public record Animation(
            String id,
            String displayName,
            double durationSeconds,
            boolean loop,
            List<Track> tracks,
            Map<String, String> runtimeStateBase,
            List<StatusTrack> statusTracks,
            List<EffectTrack> effectTracks) {
        public Animation {
            id = id == null ? "" : id;
            displayName = displayName == null ? "" : displayName;
            tracks = List.copyOf(tracks == null ? List.of() : tracks);
            runtimeStateBase = Map.copyOf(runtimeStateBase == null ? Map.of() : runtimeStateBase);
            statusTracks = List.copyOf(statusTracks == null ? List.of() : statusTracks);
            effectTracks = List.copyOf(effectTracks == null ? List.of() : effectTracks);
        }

        public static Animation empty() {
            return new Animation("", "", 6.0D, true, List.of(), Map.of(), List.of(), List.of());
        }
    }

    public record Track(String targetId, String modelKey, int elementIndex, List<Keyframe> keyframes) {
        public Track {
            targetId = targetId == null ? "" : targetId;
            modelKey = modelKey == null ? "" : modelKey;
            keyframes = List.copyOf(keyframes == null ? List.of() : keyframes);
        }
    }

    public record Keyframe(
            double timeSeconds,
            double[] position,
            String rotationAxis,
            double rotationDegrees,
            double[] scale,
            boolean visible,
            String textureState,
            String lightStatus,
            String specialEffect,
            String machineState,
            Interpolation interpolation) {
        public Keyframe {
            position = position == null ? new double[] {0, 0, 0} : position.clone();
            rotationAxis = rotationAxis == null ? "y" : rotationAxis;
            scale = scale == null ? new double[] {1, 1, 1} : scale.clone();
            textureState = textureState == null ? "" : textureState;
            lightStatus = lightStatus == null ? "" : lightStatus;
            specialEffect = specialEffect == null ? "" : specialEffect;
            machineState = machineState == null ? "Idle" : machineState;
            interpolation = interpolation == null ? Interpolation.SMOOTH : interpolation;
        }

        @Override public double[] position() { return position.clone(); }
        @Override public double[] scale() { return scale.clone(); }
    }

    public enum Interpolation {
        STEP, LINEAR, SMOOTH
    }

    public record StatusTrack(String axisId, List<StatusKeyframe> keyframes) {
        public StatusTrack {
            axisId = axisId == null ? "" : axisId;
            keyframes = List.copyOf(keyframes == null ? List.of() : keyframes);
        }
    }

    public record StatusKeyframe(double timeSeconds, String optionId) {
        public StatusKeyframe { optionId = optionId == null ? "" : optionId; }
    }

    public record EffectTrack(String effectId, List<EffectKeyframe> keyframes) {
        public EffectTrack {
            effectId = effectId == null ? "" : effectId;
            keyframes = List.copyOf(keyframes == null ? List.of() : keyframes);
        }
    }

    public record EffectKeyframe(double timeSeconds, EffectMode mode) {
        public EffectKeyframe { mode = mode == null ? EffectMode.SOURCE : mode; }
    }

    public enum EffectMode {
        SOURCE, SUPPRESSED
    }
}

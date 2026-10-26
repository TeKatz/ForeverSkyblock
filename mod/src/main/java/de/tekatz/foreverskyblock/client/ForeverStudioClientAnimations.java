package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationDocument;
import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationWire;

import java.util.Arrays;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.atomic.AtomicReference;

import static de.tekatz.foreverskyblock.animation.ForeverStudioAnimationDocument.*;

/**
 * Client-side immutable animation snapshot and format-v1 evaluator.
 * Runtime components are addressed only by the canonical runtime:{asset}:{component} TargetId.
 */
public final class ForeverStudioClientAnimations {
    private static final double EPSILON = 0.0001D;
    private static final AtomicReference<Map<String, ForeverStudioAnimationDocument>> SNAPSHOT =
            new AtomicReference<>(Map.of());
    private static volatile long installedGeneration = -1;
    private static Receiving receiving;

    private ForeverStudioClientAnimations() { }

    public static synchronized void accept(long generation, int index, int total, String data) {
        if (generation < 0 || index < 0 || total < 1 || index >= total || data == null)
            throw new IllegalArgumentException("Invalid Forever Studio animation client chunk");
        if (index == 0 || receiving == null || receiving.generation != generation)
            receiving = new Receiving(generation, total);
        if (receiving.generation != generation || receiving.parts.length != total)
            return;
        if (!receiving.received[index]) {
            receiving.parts[index] = data;
            receiving.received[index] = true;
            receiving.count++;
        }
        if (receiving.count != receiving.parts.length)
            return;

        StringBuilder json = new StringBuilder();
        for (String part : receiving.parts) json.append(part);
        Map<String, ForeverStudioAnimationDocument> parsed = ForeverStudioAnimationWire.decodeSnapshot(json.toString());
        SNAPSHOT.set(parsed);
        installedGeneration = receiving.generation;
        receiving = null;
    }

    public static synchronized void clear() {
        receiving = null;
        installedGeneration = -1;
        SNAPSHOT.set(Map.of());
    }

    public static long generation() { return installedGeneration; }
    public static int size() { return SNAPSHOT.get().size(); }

    public static Optional<ForeverStudioAnimationDocument> document(String assetId) {
        return Optional.ofNullable(SNAPSHOT.get().get(normalize(assetId)));
    }

    public static String runtimeTargetId(String assetId, String componentKey) {
        return "runtime:" + normalizeRuntimeToken(assetId) + ":" + normalizeRuntimeToken(componentKey);
    }

    public static Frame evaluateRuntime(String assetId, String componentKey, double sourcePhase) {
        Optional<ForeverStudioAnimationDocument> document = document(assetId);
        if (document.isEmpty()) return Frame.identity();
        String targetId = runtimeTargetId(assetId, componentKey);
        Track track = document.get().animation().tracks().stream()
                .filter(candidate -> targetId.equals(candidate.targetId()))
                .findFirst().orElse(null);
        return evaluate(track, phaseTime(document.get(), sourcePhase));
    }

    /** Status keys are presentation-only. Before the first key, the live source value remains authoritative. */
    public static String statusOption(String assetId, String axisId, double sourcePhase, String sourceOption) {
        Optional<ForeverStudioAnimationDocument> document = document(assetId);
        if (document.isEmpty()) return sourceOption;
        StatusTrack track = document.get().animation().statusTracks().stream()
                .filter(candidate -> axisId.equalsIgnoreCase(candidate.axisId()))
                .findFirst().orElse(null);
        if (track == null || track.keyframes().isEmpty()) return sourceOption;
        double time = phaseTime(document.get(), sourcePhase);
        String value = sourceOption;
        for (StatusKeyframe keyframe : track.keyframes()) {
            if (keyframe.timeSeconds() > time + EPSILON) break;
            value = keyframe.optionId();
        }
        return value;
    }

    /** Studio can suppress a real source effect, never manufacture one when the source predicate is false. */
    public static boolean effectEnabled(String assetId, String effectId, double sourcePhase, boolean sourcePredicate) {
        if (!sourcePredicate) return false;
        Optional<ForeverStudioAnimationDocument> document = document(assetId);
        if (document.isEmpty()) return true;
        EffectTrack track = document.get().animation().effectTracks().stream()
                .filter(candidate -> effectId.equalsIgnoreCase(candidate.effectId()))
                .findFirst().orElse(null);
        if (track == null || track.keyframes().isEmpty()) return true;
        double time = phaseTime(document.get(), sourcePhase);
        EffectMode mode = EffectMode.SOURCE;
        for (EffectKeyframe keyframe : track.keyframes()) {
            if (keyframe.timeSeconds() > time + EPSILON) break;
            mode = keyframe.mode();
        }
        return mode != EffectMode.SUPPRESSED;
    }

    /** For source effects without recipe phase, e.g. Crucible convection. */
    public static double elapsedTimelinePhase(String assetId, double elapsedSeconds) {
        Optional<ForeverStudioAnimationDocument> document = document(assetId);
        if (document.isEmpty()) return positiveModulo(elapsedSeconds, 6D) / 6D;
        double duration = Math.max(.0001D, document.get().animation().durationSeconds());
        double time = Math.max(0D, elapsedSeconds);
        if (document.get().animation().loop())
            time = positiveModulo(time, duration);
        else
            time = Math.min(time, duration);
        return Math.max(0D, Math.min(1D, time / duration));
    }

    static Frame evaluate(Track track, double currentTime) {
        if (track == null || track.keyframes().isEmpty()) return Frame.identity();
        var ordered = track.keyframes();
        for (Keyframe keyframe : ordered) {
            if (Math.abs(keyframe.timeSeconds() - currentTime) < EPSILON)
                return Frame.from(keyframe);
        }
        if (currentTime < ordered.getFirst().timeSeconds()) {
            Keyframe right = ordered.getFirst();
            if (right.timeSeconds() <= EPSILON) return Frame.from(right);
            double raw = Math.max(0D, Math.min(1D, currentTime / right.timeSeconds()));
            return interpolate(Frame.identity(), Frame.from(right), apply(right.interpolation(), raw));
        }
        if (currentTime > ordered.getLast().timeSeconds())
            return Frame.from(ordered.getLast());

        int rightIndex = -1;
        for (int i = 0; i < ordered.size(); i++) {
            if (ordered.get(i).timeSeconds() > currentTime) { rightIndex = i; break; }
        }
        if (rightIndex <= 0) return Frame.identity();
        Keyframe leftKey = ordered.get(rightIndex - 1);
        Keyframe rightKey = ordered.get(rightIndex);
        double span = Math.max(EPSILON, rightKey.timeSeconds() - leftKey.timeSeconds());
        double raw = Math.max(0D, Math.min(1D, (currentTime - leftKey.timeSeconds()) / span));
        return interpolate(Frame.from(leftKey), Frame.from(rightKey), apply(leftKey.interpolation(), raw));
    }

    private static double phaseTime(ForeverStudioAnimationDocument document, double sourcePhase) {
        double phase = Math.max(0D, Math.min(1D, sourcePhase));
        return phase * Math.max(.0001D, document.animation().durationSeconds());
    }

    private static double apply(Interpolation interpolation, double raw) {
        return switch (interpolation) {
            case STEP -> 0D;
            case LINEAR -> raw;
            case SMOOTH -> raw * raw * (3D - 2D * raw);
        };
    }

    private static Frame interpolate(Frame left, Frame right, double amount) {
        double[] position = new double[3];
        double[] scale = new double[3];
        for (int axis = 0; axis < 3; axis++) {
            position[axis] = lerp(left.position[axis], right.position[axis], amount);
            scale[axis] = lerp(left.scale[axis], right.scale[axis], amount);
        }
        boolean sameAxis = left.rotationAxis.equalsIgnoreCase(right.rotationAxis);
        return new Frame(position,
                left.rotationAxis,
                sameAxis ? lerp(left.rotationDegrees, right.rotationDegrees, amount) : left.rotationDegrees,
                scale, left.visible);
    }

    private static double lerp(double from, double to, double amount) { return from + (to - from) * amount; }
    private static double positiveModulo(double value, double modulus) { return ((value % modulus) + modulus) % modulus; }
    private static String normalize(String value) { return value == null ? "" : value.trim().toLowerCase(Locale.ROOT); }

    private static String normalizeRuntimeToken(String value) {
        String source = value == null ? "" : value.trim().toLowerCase(Locale.ROOT);
        StringBuilder result = new StringBuilder(source.length());
        boolean previousDash = false;
        for (int i = 0; i < source.length(); i++) {
            char character = source.charAt(i);
            if (Character.isLetterOrDigit(character) || character == '_' || character == '-') {
                if (character == '-') {
                    if (previousDash) continue;
                    previousDash = true;
                } else previousDash = false;
                result.append(character);
            } else if (!previousDash && result.length() > 0) {
                result.append('-');
                previousDash = true;
            }
        }
        while (result.length() > 0 && result.charAt(result.length() - 1) == '-') result.deleteCharAt(result.length() - 1);
        return result.toString();
    }

    public record Frame(double[] position, String rotationAxis, double rotationDegrees, double[] scale, boolean visible) {
        public Frame {
            position = position == null ? new double[] {0, 0, 0} : position.clone();
            rotationAxis = rotationAxis == null ? "y" : rotationAxis;
            scale = scale == null ? new double[] {1, 1, 1} : scale.clone();
        }
        @Override public double[] position() { return position.clone(); }
        @Override public double[] scale() { return scale.clone(); }
        public static Frame identity() { return new Frame(new double[] {0,0,0}, "y", 0D, new double[] {1,1,1}, true); }
        static Frame from(Keyframe keyframe) {
            return new Frame(keyframe.position(), keyframe.rotationAxis(), keyframe.rotationDegrees(), keyframe.scale(), keyframe.visible());
        }
        public boolean isIdentity() {
            return visible && Math.abs(rotationDegrees) < EPSILON
                    && Arrays.equals(position, new double[] {0,0,0})
                    && Arrays.equals(scale, new double[] {1,1,1});
        }
    }

    private static final class Receiving {
        final long generation;
        final String[] parts;
        final boolean[] received;
        int count;
        Receiving(long generation, int total) {
            this.generation = generation;
            this.parts = new String[total];
            this.received = new boolean[total];
        }
    }
}

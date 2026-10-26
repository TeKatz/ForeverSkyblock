package de.tekatz.foreverskyblock.animation;

import java.util.Collection;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.atomic.AtomicReference;

/**
 * Atomic server-side snapshot of project animations loaded from data packs.
 * Renderer/client consumption deliberately comes later in Phase 17D.3 through an explicit sync/binding layer.
 */
public final class ForeverStudioAnimationRegistry {
    private static final AtomicReference<Map<String, ForeverStudioAnimationDocument>> SNAPSHOT = new AtomicReference<>(Map.of());
    private static final AtomicLong GENERATION = new AtomicLong();

    private ForeverStudioAnimationRegistry() { }

    static long replace(Map<String, ForeverStudioAnimationDocument> documents) {
        SNAPSHOT.set(Map.copyOf(documents));
        return GENERATION.incrementAndGet();
    }

    public static long generation() {
        return GENERATION.get();
    }

    public static int size() {
        return SNAPSHOT.get().size();
    }

    public static Collection<ForeverStudioAnimationDocument> all() {
        return SNAPSHOT.get().values();
    }

    public static Optional<ForeverStudioAnimationDocument> forAsset(String assetId) {
        return Optional.ofNullable(SNAPSHOT.get().get(normalize(assetId)));
    }

    public static Optional<ForeverStudioAnimationDocument.Track> targetTrack(String assetId, String targetId) {
        if (targetId == null || targetId.isBlank()) return Optional.empty();
        return forAsset(assetId).flatMap(document -> document.animation().tracks().stream()
                .filter(track -> targetId.equals(track.targetId()))
                .findFirst());
    }

    public static Optional<ForeverStudioAnimationDocument.StatusTrack> statusTrack(String assetId, String axisId) {
        if (axisId == null || axisId.isBlank()) return Optional.empty();
        return forAsset(assetId).flatMap(document -> document.animation().statusTracks().stream()
                .filter(track -> axisId.equalsIgnoreCase(track.axisId()))
                .findFirst());
    }

    public static Optional<ForeverStudioAnimationDocument.EffectTrack> effectTrack(String assetId, String effectId) {
        if (effectId == null || effectId.isBlank()) return Optional.empty();
        return forAsset(assetId).flatMap(document -> document.animation().effectTracks().stream()
                .filter(track -> effectId.equalsIgnoreCase(track.effectId()))
                .findFirst());
    }

    static String normalize(String assetId) {
        return assetId == null ? "" : assetId.trim().toLowerCase(Locale.ROOT);
    }
}

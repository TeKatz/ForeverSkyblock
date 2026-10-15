package de.tekatz.foreverskyblock.progression;

import java.util.List;
import java.util.function.ToIntFunction;

/** Four profile-wide research stages; material tiers are read from Iron Jetpacks, not item IDs. */
public final class JetpackResearch {
    public static final List<String> IDS = List.of("forever_skyblock:jetpacks_beginner", "forever_skyblock:jetpacks_mid_game",
            "forever_skyblock:jetpacks_end_game", "forever_skyblock:jetpacks_creative");
    public static final List<Integer> COSTS = List.of(5, 10, 20, 40);
    private JetpackResearch() { }
    public static int stage(ToIntFunction<String> ranks) {
        int stage = 0;
        for (String id : IDS) { if (ranks.applyAsInt(id) < 1) break; stage++; }
        return stage;
    }
    public static int requiredStage(int tier, boolean creative) {
        return creative || tier < 0 ? 4 : tier <= 1 ? 1 : tier <= 3 ? 2 : 3;
    }
    public static String prerequisite(String id) {
        int index = IDS.indexOf(id);
        return index > 0 ? IDS.get(index - 1) : "";
    }
}

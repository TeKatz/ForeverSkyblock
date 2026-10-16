package de.tekatz.foreverskyblock.progression;

import java.util.List;
import java.util.Set;

/** Pure, server-owned definitions; no optional mod or Minecraft classes are loaded here. */
public final class ChallengeRules {
    public record Definition(String id, int reward, String category, int target, Set<String> objectives) {
        public Definition { objectives = Set.copyOf(objectives); }
    }
    private static Definition single(String id, int reward, String category) {
        return new Definition("forever_skyblock:" + id, reward, category, 1, Set.of("completed"));
    }
    private static Definition tools(String id, int reward, String material) {
        return new Definition("forever_skyblock:" + id, reward, "forever", 4, Set.of(
                "minecraft:" + material + "_pickaxe", "minecraft:" + material + "_axe",
                "minecraft:" + material + "_shovel", "minecraft:" + material + "_hoe"));
    }
    public static final Definition FOUNDATIONS = single("first_foundations", 1, "forever");
    public static final Definition STONEWORKER = single("stoneworker", 1, "forever");
    public static final Definition STONE_TOOLS = tools("permanent_tools_i", 2, "stone");
    public static final Definition IRON_TOOLS = tools("iron_age", 3, "iron");
    public static final Definition DIAMOND = single("diamond_standard", 3, "forever");
    public static final Definition NETHERITE = single("forever_forged", 5, "forever");
    public static final Definition NETHER = single("into_nether", 2, "combat");
    public static final Definition WITHER = single("withered_away", 5, "combat");
    public static final Definition END = single("final_dimension", 3, "combat");
    public static final Definition DRAGON = single("dragon_slayer", 6, "combat");
    public static final Definition VETERAN = new Definition("forever_skyblock:veteran", 3, "combat", 1000, Set.of());
    public static final List<Definition> ALL = List.of(FOUNDATIONS, STONEWORKER, STONE_TOOLS, IRON_TOOLS,
            DIAMOND, NETHERITE, NETHER, WITHER, END, DRAGON, VETERAN);
    private ChallengeRules() { }
    public static Definition find(String id) { return ALL.stream().filter(d -> d.id().equals(id)).findFirst().orElse(null); }
    public static int qualifiedTier(Set<String> completed) {
        return completed.contains(DRAGON.id()) ? 1 : completed.contains(FOUNDATIONS.id()) ? 0 : -1;
    }
    /** Replayable events retain bounded identity evidence until completion. No client can invoke this. */
    public static ProgressionProfile record(ProgressionProfile profile, String id, String evidence) {
        Definition definition = find(id);
        if (definition == null || profile.milestones().contains(id)) return profile;
        if (!definition.objectives().isEmpty() && !definition.objectives().contains(evidence)) return profile;
        if (definition == VETERAN) {
            try { if (!java.util.UUID.fromString(evidence).toString().equals(evidence)) return profile; }
            catch (IllegalArgumentException e) { return profile; }
        }
        var previous = profile.meta().progress().getOrDefault(id, MetaProgress.Progress.empty());
        if (previous.revision() != 1 || previous.evidence().contains(evidence)) return profile;
        var tokens = new java.util.LinkedHashSet<>(previous.evidence()); tokens.add(evidence);
        var progress = new java.util.LinkedHashMap<>(profile.meta().progress());
        progress.put(id, new MetaProgress.Progress(1, tokens));
        var next = profile.withMeta(profile.meta().withProgress(progress));
        return tokens.size() >= definition.target() ? next.award(id, definition.reward()) : next;
    }
}

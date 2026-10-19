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
    private static Definition evidence(String id, int reward, String category, String... objectives) {
        return new Definition("forever_skyblock:" + id, reward, category, 1, Set.of(objectives));
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

    public static final Definition AE2_STORAGE = evidence("beyond_starter_cell", 1, "ae2",
            "ae2:item_storage_cell_4k", "ae2:item_storage_cell_16k", "ae2:item_storage_cell_64k", "ae2:item_storage_cell_256k");
    public static final Definition AE2_AUTOCRAFT = single("autocrafting_online", 3, "ae2");
    public static final Definition AE2_CRAFTING_BRAIN = evidence("crafting_brain", 3, "ae2", "ae2:64k_crafting_storage");
    public static final Definition AE2_WIRELESS = single("wireless_operator", 2, "ae2");
    public static final Definition AE2_STORAGE_ARCHITECT = evidence("storage_architect", 4, "ae2", "ae2:item_storage_cell_256k");

    public static final Definition MEKANIZED = evidence("mekanized", 1, "mekanism", "mekanism:metallurgic_infuser");
    public static final Definition DIGITAL_EXTRACTION = single("digital_extraction", 5, "mekanism");

    public static final Definition ENERGIZED = single("energized", 1, "powah");

    public static final Definition SUPREMIUM = evidence("supremium", 4, "mystical_agriculture", "mysticalagriculture:supremium_essence");

    public static final List<Definition> ALL = List.of(FOUNDATIONS, STONEWORKER, STONE_TOOLS, IRON_TOOLS,
            DIAMOND, NETHERITE, NETHER, WITHER, END, DRAGON, VETERAN,
            AE2_STORAGE, AE2_AUTOCRAFT, AE2_CRAFTING_BRAIN, AE2_WIRELESS, AE2_STORAGE_ARCHITECT,
            MEKANIZED, DIGITAL_EXTRACTION, ENERGIZED, SUPREMIUM);
    private ChallengeRules() { }
    public static Definition find(String id) { return ALL.stream().filter(d -> d.id().equals(id)).findFirst().orElse(null); }
    public static int qualifiedTier(Set<String> completed) {
        return completed.contains(DRAGON.id()) ? 1 : completed.contains(FOUNDATIONS.id()) ? 0 : -1;
    }
    /** Replayable events retain bounded identity evidence until completion. No client can invoke this. */
    public static ProgressionProfile record(ProgressionProfile profile, String id, String evidence) {
        if(de.tekatz.foreverskyblock.editor.EditorRuntime.failed())return profile;
        Definition definition = find(id);
        if (definition == null || profile.milestones().contains(id)) return profile;
        if (!definition.objectives().isEmpty() && !definition.objectives().contains(evidence)) return profile;
        if (definition == VETERAN) {
            try { if (!java.util.UUID.fromString(evidence).toString().equals(evidence)) return profile; }
            catch (IllegalArgumentException e) { return profile; }
        }
        var override = de.tekatz.foreverskyblock.editor.EditorRuntime.quest(id);
        if (override != null && !de.tekatz.foreverskyblock.editor.QuestRules.unlocked(override, profile)) return profile;
        var previous = profile.meta().progress().getOrDefault(id, MetaProgress.Progress.empty());
        if (previous.revision() != 1 || previous.evidence().contains(evidence)) return profile;
        var tokens = new java.util.LinkedHashSet<>(previous.evidence()); tokens.add(evidence);
        var progress = new java.util.LinkedHashMap<>(profile.meta().progress());
        progress.put(id, new MetaProgress.Progress(1, tokens));
        var next = profile.withMeta(profile.meta().withProgress(progress));
        int target = override == null ? definition.target() : override.tasks().getFirst().count();
        return tokens.size() >= target ? override == null ? next.award(id, definition.reward())
                : de.tekatz.foreverskyblock.editor.QuestRules.award(next,id,override.rewardSp(),override.rewardPp()) : next;
    }
}

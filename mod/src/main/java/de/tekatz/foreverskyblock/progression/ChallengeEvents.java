package de.tekatz.foreverskyblock.progression;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.MobCategory;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.common.util.FakePlayer;

/** Adapts successful personal-action triggers, including optional technology integrations. */
public final class ChallengeEvents {
    public record Evidence(String challenge, String token) {
        public static Evidence completed(ChallengeRules.Definition definition) { return new Evidence(definition.id(), "completed"); }
    }
    private ChallengeEvents() { }
    public static boolean realPlayer(ServerPlayer player) { return player != null && !(player instanceof FakePlayer); }
    public static void crafted(ServerPlayer player, ResourceLocation recipe, List<ItemStack> ingredients) {
        if (!realPlayer(player)) return;
        var evidence = new ArrayList<Evidence>(TechChallengeEvents.crafted(player, recipe));
        evidence.addAll(craftEvidence(player, recipe, ingredients));
        if (!evidence.isEmpty()) ProgressionService.get(player.server).evidence(player, evidence);
    }
    /** Require the authoritative recipe ID, registered output and its actual consumed ingredient slots. */
    public static List<Evidence> craftEvidence(ServerPlayer player, ResourceLocation recipe, List<ItemStack> ingredients) {
        if (!recipe.getNamespace().equals(ForeverSkyblock.MOD_ID)) return List.of();
        var holder = player.serverLevel().getRecipeManager().byKey(recipe).orElse(null);
        if (holder == null) return List.of();
        String output = BuiltInRegistries.ITEM.getKey(holder.value().getResultItem(player.registryAccess()).getItem()).toString();
        var counts = new HashMap<String, Integer>();
        for (var stack : ingredients) if (!stack.isEmpty()) counts.merge(BuiltInRegistries.ITEM.getKey(stack.getItem()).toString(), 1, Integer::sum);
        String path = recipe.getPath();
        if (path.equals("cobblestone_from_fragments") && output.equals("minecraft:cobblestone")
                && counts.equals(Map.of("forever_skyblock:stone_fragment", 4))) return List.of(Evidence.completed(ChallengeRules.FOUNDATIONS));
        if (path.equals("stone_meal") && output.equals("forever_skyblock:stone_meal")
                && counts.equals(Map.of("forever_skyblock:mortar", 1, "forever_skyblock:stone_fragment", 4))) return List.of(Evidence.completed(ChallengeRules.STONEWORKER));
        for (var definition : List.of(ChallengeRules.STONE_TOOLS, ChallengeRules.IRON_TOOLS)) {
            if (!definition.objectives().contains(output) || !path.equals("tools/" + output.substring(10))) continue;
            String tool = output.substring(output.indexOf('_') + 1);
            int materialCount = switch (tool) { case "pickaxe" -> 5; case "axe" -> 4; default -> 3; };
            boolean stone = definition == ChallengeRules.STONE_TOOLS;
            var expected = new HashMap<>(Map.of(stone ? "forever_skyblock:stone_fragment" : "minecraft:iron_ingot", stone ? materialCount : materialCount - 1,
                    "minecraft:" + (stone ? "wooden_" : "stone_") + tool, 1,
                    stone ? "forever_skyblock:stone_meal" : "minecraft:chain", 1));
            if (!stone) expected.put("minecraft:iron_block", 1);
            if (counts.equals(expected))
                return List.of(new Evidence(definition.id(), output));
        }
        if (path.equals("tools/diamond_pickaxe") && output.equals("minecraft:diamond_pickaxe")
                && counts.equals(Map.of("minecraft:diamond", 5, "minecraft:iron_pickaxe", 1, "minecraft:obsidian", 1)))
            return List.of(Evidence.completed(ChallengeRules.DIAMOND));
        if (path.equals("tools/netherite_pickaxe_smithing") && output.equals("minecraft:netherite_pickaxe")
                && counts.equals(Map.of("minecraft:netherite_upgrade_smithing_template", 1, "minecraft:diamond_pickaxe", 1,
                "forever_skyblock:reinforced_netherite_ingot", 1))) return List.of(Evidence.completed(ChallengeRules.NETHERITE));
        return List.of();
    }
    /** Boss credit and repeatable contracts are reduced in one atomic profile update to avoid first-kill double payment. */
    public static void killed(ServerPlayer player, Entity victim) {
        if (!realPlayer(player) || victim.level().isClientSide() || victim == player) return;
        de.tekatz.foreverskyblock.editor.EditorEvents.emit(player,de.tekatz.foreverskyblock.editor.EditorDocument.TaskType.KILL,
                BuiltInRegistries.ENTITY_TYPE.getKey(victim.getType()).toString(),1,
                (victim.getType().getCategory() == MobCategory.MONSTER ? "h/" : "")+victim.getUUID());
        ProgressionService.get(player.server).killEvidence(player,
                victim.getType() == EntityType.WITHER, victim.getType() == EntityType.ENDER_DRAGON,
                victim.getType().getCategory() == MobCategory.MONSTER, victim.getUUID());
    }
}

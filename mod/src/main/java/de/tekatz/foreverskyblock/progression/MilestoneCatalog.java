package de.tekatz.foreverskyblock.progression;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import java.util.List;
import net.minecraft.world.Container;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

public final class MilestoneCatalog {
    public record Milestone(String id, int reward, Item icon) {
        public ChallengeRules.Definition definition() { return ChallengeRules.find(id); }
        public String category() { return definition().category(); }
        public int target() { return definition().target(); }
        public String titleKey() { return "milestone." + id.replace(':', '.'); }
        public String descriptionKey() { return titleKey() + ".description"; }
    }
    public static final Milestone FIRST_FOUNDATIONS = new Milestone(ForeverSkyblock.MOD_ID + ":first_foundations", 1, Items.COBBLESTONE);
    private static Milestone entry(ChallengeRules.Definition definition, Item icon) {
        return new Milestone(definition.id(), definition.reward(), icon);
    }
    public static final List<Milestone> MILESTONES = List.of(FIRST_FOUNDATIONS,
            entry(ChallengeRules.STONEWORKER, Items.FLINT), entry(ChallengeRules.STONE_TOOLS, Items.STONE_PICKAXE),
            entry(ChallengeRules.IRON_TOOLS, Items.IRON_PICKAXE), entry(ChallengeRules.DIAMOND, Items.DIAMOND_PICKAXE),
            entry(ChallengeRules.NETHERITE, Items.NETHERITE_PICKAXE), entry(ChallengeRules.NETHER, Items.NETHERRACK),
            entry(ChallengeRules.WITHER, Items.NETHER_STAR), entry(ChallengeRules.END, Items.END_STONE),
            entry(ChallengeRules.DRAGON, Items.DRAGON_HEAD), entry(ChallengeRules.VETERAN, Items.IRON_SWORD));
    private MilestoneCatalog() { }
    public static boolean isFragmentCraft(ItemStack result, Container ingredients) {
        if (!result.is(Items.COBBLESTONE)) return false;
        int slots = 0;
        for (int i = 0; i < ingredients.getContainerSize(); i++) {
            ItemStack stack = ingredients.getItem(i);
            if (stack.isEmpty()) continue;
            if (!stack.is(ForeverSkyblock.STONE_FRAGMENT.get())) return false;
            slots++;
        }
        return slots == 4;
    }
}

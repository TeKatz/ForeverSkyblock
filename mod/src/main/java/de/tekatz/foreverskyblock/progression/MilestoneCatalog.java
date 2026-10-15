package de.tekatz.foreverskyblock.progression;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import java.util.List;
import net.minecraft.world.Container;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

public final class MilestoneCatalog {
    public record Milestone(String id, int reward, Item icon) {
        public String titleKey() { return "milestone." + id.replace(':', '.'); }
        public String descriptionKey() { return titleKey() + ".description"; }
    }
    public static final Milestone FIRST_FOUNDATIONS = new Milestone(ForeverSkyblock.MOD_ID + ":first_foundations", 1, Items.COBBLESTONE);
    public static final List<Milestone> MILESTONES = List.of(FIRST_FOUNDATIONS);
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

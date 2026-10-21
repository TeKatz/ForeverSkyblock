package de.tekatz.foreverskyblock.recipe;

import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.RecipeInput;

public record RefiningInput(ItemStack primary, ItemStack catalyst) implements RecipeInput {
    @Override public ItemStack getItem(int index) {
        return switch (index) {
            case 0 -> primary;
            case 1 -> catalyst;
            default -> throw new IllegalArgumentException("Invalid input index");
        };
    }

    @Override public int size() { return 2; }
}

package de.tekatz.foreverskyblock.recipe;

import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.RecipeInput;

public record WashingInput(ItemStack stack) implements RecipeInput {
    @Override public ItemStack getItem(int index) {
        if (index != 0) throw new IllegalArgumentException("Invalid input index");
        return stack;
    }
    @Override public int size() { return 1; }
}

package de.tekatz.foreverskyblock.studio.runtime;

import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.RecipeInput;

public record ForeverStudioMachineInput(ResourceLocation machineId, ItemStack input) implements RecipeInput {
    @Override public ItemStack getItem(int index) {
        if (index != 0) throw new IllegalArgumentException("Invalid Forever Studio machine input index");
        return input;
    }

    @Override public int size() { return 1; }
}

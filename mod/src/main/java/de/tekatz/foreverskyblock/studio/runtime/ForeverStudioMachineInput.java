package de.tekatz.foreverskyblock.studio.runtime;

import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.RecipeInput;

import java.util.List;

/** Snapshot of all authored input slots for one Studio machine recipe lookup. */
public record ForeverStudioMachineInput(ResourceLocation machineId, List<ItemStack> inputs) implements RecipeInput {
    public ForeverStudioMachineInput {
        inputs = inputs.stream().map(ItemStack::copy).toList();
    }

    /** Compatibility constructor for the original single-input runtime contract. */
    public ForeverStudioMachineInput(ResourceLocation machineId, ItemStack input) {
        this(machineId, List.of(input));
    }

    @Override public ItemStack getItem(int index) {
        if (index < 0 || index >= inputs.size())
            throw new IllegalArgumentException("Invalid Forever Studio machine input index");
        return inputs.get(index);
    }

    @Override public int size() { return inputs.size(); }
}

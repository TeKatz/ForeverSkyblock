package de.tekatz.foreverskyblock.item;

import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;

public final class MortarItem extends Item {
    public MortarItem(Properties properties) { super(properties); }
    @Override public boolean hasCraftingRemainingItem(ItemStack stack) { return true; }
    @Override public ItemStack getCraftingRemainingItem(ItemStack stack) { return stack.copyWithCount(1); }
}

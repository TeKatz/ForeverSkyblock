package de.tekatz.foreverskyblock.blockentity;

import net.minecraft.core.Direction;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.items.IItemHandler;
import org.jetbrains.annotations.Nullable;

/**
 * Capability-facing adapter that preserves the Condenser's sided automation contract while allowing
 * Garden specimens to enter the dedicated specimen slot. Hidden upgrade slots remain inaccessible.
 */
public final class CondenserAutomationHandler implements IItemHandler {
    private final CondenserBlockEntity machine;
    private final IItemHandler delegate;

    private CondenserAutomationHandler(CondenserBlockEntity machine, @Nullable Direction side) {
        this.machine = machine;
        this.delegate = machine.automation(side);
    }

    public static IItemHandler create(CondenserBlockEntity machine, @Nullable Direction side) {
        return new CondenserAutomationHandler(machine, side);
    }

    @Override public int getSlots() { return delegate.getSlots(); }
    @Override public ItemStack getStackInSlot(int slot) { return delegate.getStackInSlot(slot); }
    @Override public int getSlotLimit(int slot) { return delegate.getSlotLimit(slot); }
    @Override public boolean isItemValid(int slot, ItemStack stack) { return delegate.isItemValid(slot, stack); }

    @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
        if (slot == CondenserBlockEntity.GARDEN_SPECIMEN && machine.hasGardenChamber()
                && delegate.isItemValid(slot, stack)) {
            return machine.inventory().insertItem(slot, stack, simulate);
        }
        return delegate.insertItem(slot, stack, simulate);
    }

    @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
        return delegate.extractItem(slot, amount, simulate);
    }
}

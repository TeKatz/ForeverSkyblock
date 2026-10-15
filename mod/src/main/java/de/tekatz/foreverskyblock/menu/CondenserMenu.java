package de.tekatz.foreverskyblock.menu;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.AbstractContainerMenu;
import net.minecraft.world.inventory.ContainerData;
import net.minecraft.world.inventory.ContainerLevelAccess;
import net.minecraft.world.inventory.SimpleContainerData;
import net.minecraft.world.inventory.Slot;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.SingleRecipeInput;
import net.neoforged.neoforge.items.ItemStackHandler;
import net.neoforged.neoforge.items.SlotItemHandler;

public final class CondenserMenu extends AbstractContainerMenu {
    private final ContainerData data;
    private final ContainerLevelAccess access;

    public CondenserMenu(int id, Inventory inventory) {
        this(id, inventory, new ItemStackHandler(2), new SimpleContainerData(3), ContainerLevelAccess.NULL);
    }
    public CondenserMenu(int id, Inventory inventory, CondenserBlockEntity machine) {
        this(id, inventory, machine.inventory(), machine.menuData(), ContainerLevelAccess.create(machine.getLevel(), machine.getBlockPos()));
    }
    private CondenserMenu(int id, Inventory playerInventory, ItemStackHandler inventory, ContainerData data, ContainerLevelAccess access) {
        super(ForeverSkyblock.CONDENSER_MENU.get(), id);
        this.data = data;
        this.access = access;
        addSlot(new SlotItemHandler(inventory, 0, 44, 37) {
            @Override public boolean mayPlace(ItemStack stack) {
                return playerInventory.player.level().getRecipeManager().getRecipeFor(ForeverSkyblock.CONDENSING.get(),
                        new SingleRecipeInput(stack), playerInventory.player.level()).isPresent();
            }
        });
        addSlot(new SlotItemHandler(inventory, 1, 116, 37) {
            @Override public boolean mayPlace(ItemStack stack) { return false; }
        });
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col + row * 9 + 9, 8 + col * 18, 106 + row * 18));
        for (int col = 0; col < 9; col++) addSlot(new Slot(playerInventory, col, 8 + col * 18, 164));
        addDataSlots(data);
    }
    public int progress() { return data.get(0); }
    public int duration() { return Math.max(1, data.get(1)); }
    public int status() { return data.get(2); }
    @Override public boolean stillValid(Player player) { return stillValid(access, player, ForeverSkyblock.CONDENSER_MARK_I.get()); }
    @Override public ItemStack quickMoveStack(Player player, int index) {
        Slot slot = slots.get(index);
        if (!slot.hasItem()) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem();
        ItemStack original = stack.copy();
        if (index < 2) {
            if (!moveItemStackTo(stack, 2, slots.size(), true)) return ItemStack.EMPTY;
        } else if (slots.get(0).mayPlace(stack)) {
            if (!moveItemStackTo(stack, 0, 1, false)) return ItemStack.EMPTY;
        } else if (index < 29) {
            if (!moveItemStackTo(stack, 29, 38, false)) return ItemStack.EMPTY;
        } else if (!moveItemStackTo(stack, 2, 29, false)) return ItemStack.EMPTY;
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack);
        return original;
    }
}

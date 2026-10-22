package de.tekatz.foreverskyblock.menu;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.RefiningChamberBlockEntity;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.AbstractContainerMenu;
import net.minecraft.world.inventory.ContainerData;
import net.minecraft.world.inventory.ContainerLevelAccess;
import net.minecraft.world.inventory.SimpleContainerData;
import net.minecraft.world.inventory.Slot;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.items.ItemStackHandler;
import net.neoforged.neoforge.items.SlotItemHandler;
import org.jetbrains.annotations.Nullable;

public final class RefiningMenu extends AbstractContainerMenu {
    public static final int PRIMARY_X = 42, CATALYST_X = 82, OUTPUT_X = 174, MACHINE_Y = 78;
    public static final int MACHINE_SLOT_COUNT = RefiningChamberBlockEntity.MACHINE_SLOTS;
    private final ContainerData data;
    private final ContainerLevelAccess access;
    private final net.minecraft.world.level.Level ownerLevel;

    public RefiningMenu(int id, Inventory inventory) {
        this(id, inventory, new ItemStackHandler(MACHINE_SLOT_COUNT), new SimpleContainerData(RefiningChamberBlockEntity.DATA_COUNT), ContainerLevelAccess.NULL);
    }
    public RefiningMenu(int id, Inventory inventory, RefiningChamberBlockEntity machine) {
        this(id, inventory, machine.inventory(), machine.menuData(), ContainerLevelAccess.create(machine.getLevel(), machine.getBlockPos()));
    }
    private RefiningMenu(int id, Inventory playerInventory, ItemStackHandler machineInventory, ContainerData data, ContainerLevelAccess access) {
        super(ForeverSkyblock.REFINING_CHAMBER_MENU.get(), id);
        this.data = data;
        this.access = access;
        this.ownerLevel = playerInventory.player.level();
        addSlot(new SlotItemHandler(machineInventory, RefiningChamberBlockEntity.PRIMARY, PRIMARY_X, MACHINE_Y) {
            @Override public boolean mayPlace(ItemStack stack) { return RefiningChamberBlockEntity.acceptsPrimary(ownerLevel, stack); }
        });
        addSlot(new SlotItemHandler(machineInventory, RefiningChamberBlockEntity.CATALYST, CATALYST_X, MACHINE_Y) {
            @Override public boolean mayPlace(ItemStack stack) { return RefiningChamberBlockEntity.acceptsCatalyst(ownerLevel, stack); }
        });
        addSlot(new SlotItemHandler(machineInventory, RefiningChamberBlockEntity.OUTPUT, OUTPUT_X, MACHINE_Y) {
            @Override public boolean mayPlace(ItemStack stack) { return false; }
        });
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col + row * 9 + 9, MachineLayout.WASHING.inventoryX(col), MachineLayout.WASHING.inventoryRowY(row)));
        for (int col = 0; col < 9; col++) addSlot(new Slot(playerInventory, col, MachineLayout.WASHING.inventoryX(col), MachineLayout.WASHING.hotbarY()));
        addDataSlots(data);
    }
    public int progress() { return data.get(0); }
    public int duration() { return Math.max(1, data.get(1)); }
    public int status() { return data.get(2); }
    public int energy() { return data.get(3); }
    public int energyCapacity() { return Math.max(1, data.get(4)); }
    @Override public boolean stillValid(Player player) { return stillValid(access, player, ForeverSkyblock.REFINING_CHAMBER.get()); }
    @Override public ItemStack quickMoveStack(Player player, int index) {
        if (index < 0 || index >= slots.size()) return ItemStack.EMPTY;
        Slot slot = slots.get(index);
        if (!slot.hasItem() || !slot.mayPickup(player)) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem();
        ItemStack original = stack.copy();
        if (index < MACHINE_SLOT_COUNT) {
            if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, slots.size(), true)) return ItemStack.EMPTY;
        } else if (slots.get(RefiningChamberBlockEntity.PRIMARY).mayPlace(stack)) {
            if (!moveItemStackTo(stack, RefiningChamberBlockEntity.PRIMARY, RefiningChamberBlockEntity.PRIMARY + 1, false)) return ItemStack.EMPTY;
        } else if (slots.get(RefiningChamberBlockEntity.CATALYST).mayPlace(stack)) {
            if (!moveItemStackTo(stack, RefiningChamberBlockEntity.CATALYST, RefiningChamberBlockEntity.CATALYST + 1, false)) return ItemStack.EMPTY;
        } else {
            int hotbarStart = MACHINE_SLOT_COUNT + 27;
            if (index < hotbarStart) { if (!moveItemStackTo(stack, hotbarStart, MACHINE_SLOT_COUNT + 36, false)) return ItemStack.EMPTY; }
            else if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, hotbarStart, false)) return ItemStack.EMPTY;
        }
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack);
        return original;
    }
}

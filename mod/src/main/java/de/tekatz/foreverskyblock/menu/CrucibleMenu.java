package de.tekatz.foreverskyblock.menu;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
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

public final class CrucibleMenu extends AbstractContainerMenu {
    public static final int INPUT_X = 42, OUTPUT_X = 188, MACHINE_Y = 100;
    public static final int BUTTON_TOGGLE_MODE = 0;
    public static final int MACHINE_SLOT_COUNT = CrucibleBlockEntity.MACHINE_SLOTS;

    private final ContainerData data;
    private final ContainerLevelAccess access;
    private final @Nullable CrucibleBlockEntity machine;

    public CrucibleMenu(int id, Inventory inventory) {
        this(id, inventory, new ItemStackHandler(MACHINE_SLOT_COUNT), new SimpleContainerData(CrucibleBlockEntity.DATA_COUNT), ContainerLevelAccess.NULL, null);
    }

    public CrucibleMenu(int id, Inventory inventory, CrucibleBlockEntity machine) {
        this(id, inventory, machine.inventory(), machine.menuData(), ContainerLevelAccess.create(machine.getLevel(), machine.getBlockPos()), machine);
    }

    private CrucibleMenu(int id, Inventory playerInventory, ItemStackHandler inventory, ContainerData data,
                         ContainerLevelAccess access, @Nullable CrucibleBlockEntity machine) {
        super(ForeverSkyblock.CRUCIBLE_MENU.get(), id);
        this.data = data;
        this.access = access;
        this.machine = machine;
        addSlot(new SlotItemHandler(inventory, CrucibleBlockEntity.INPUT, INPUT_X, MACHINE_Y) {
            @Override public boolean mayPlace(ItemStack stack) { return CrucibleBlockEntity.acceptsInput(stack); }
        });
        addSlot(new SlotItemHandler(inventory, CrucibleBlockEntity.OUTPUT, OUTPUT_X, MACHINE_Y) {
            @Override public boolean mayPlace(ItemStack stack) { return false; }
        });
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col + row * 9 + 9, MachineLayout.WASHING.inventoryX(col), MachineLayout.WASHING.inventoryRowY(row)));
        for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col, MachineLayout.WASHING.inventoryX(col), MachineLayout.WASHING.hotbarY()));
        addDataSlots(data);
    }

    public int progress() { return data.get(0); }
    public int duration() { return Math.max(1, data.get(1)); }
    public int lavaMb() { return data.get(2); }
    public int heatTier() { return data.get(3); }
    public boolean hasTank() { return data.get(4) != 0; }
    public boolean hasFreezer() { return data.get(5) != 0; }
    public int mode() { return data.get(6); }
    public int status() { return data.get(7); }
    public int freezeProgress() { return data.get(8); }
    public int freezeDuration() { return CrucibleBlockEntity.FREEZE_DURATION; }
    public boolean canToggleMode() { return hasTank() && hasFreezer(); }

    @Override public boolean clickMenuButton(Player player, int id) {
        if (id != BUTTON_TOGGLE_MODE || machine == null || !machine.hasTank() || !machine.hasFreezer()) return false;
        machine.toggleMode();
        broadcastChanges();
        return true;
    }

    @Override public boolean stillValid(Player player) { return stillValid(access, player, ForeverSkyblock.CRUCIBLE.get()); }

    @Override public ItemStack quickMoveStack(Player player, int index) {
        if (index < 0 || index >= slots.size()) return ItemStack.EMPTY;
        Slot slot = slots.get(index);
        if (!slot.hasItem() || !slot.mayPickup(player)) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem();
        ItemStack original = stack.copy();
        if (index < MACHINE_SLOT_COUNT) {
            if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, slots.size(), true)) return ItemStack.EMPTY;
        } else if (CrucibleBlockEntity.acceptsInput(stack)) {
            if (!moveItemStackTo(stack, CrucibleBlockEntity.INPUT, CrucibleBlockEntity.INPUT + 1, false)) return ItemStack.EMPTY;
        } else {
            int hotbarStart = MACHINE_SLOT_COUNT + 27;
            if (index < hotbarStart) {
                if (!moveItemStackTo(stack, hotbarStart, MACHINE_SLOT_COUNT + 36, false)) return ItemStack.EMPTY;
            } else if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, hotbarStart, false)) return ItemStack.EMPTY;
        }
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack);
        return original;
    }
}

package de.tekatz.foreverskyblock.menu;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity;
import java.util.UUID;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.player.*;
import net.minecraft.world.inventory.*;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.items.*;

public final class AscensionMenu extends AbstractContainerMenu {
    private final AscensionChamberBlockEntity machine;
    private final ContainerData data;
    private final ContainerLevelAccess access;
    public AscensionMenu(int id, Inventory player) { this(id, player, null); }
    public AscensionMenu(int id, Inventory player, AscensionChamberBlockEntity machine) {
        super(ForeverSkyblock.ASCENSION_MENU.get(), id);
        this.machine = machine;
        data = machine == null ? new SimpleContainerData(11) : machine.menuData();
        access = machine == null ? ContainerLevelAccess.NULL : ContainerLevelAccess.create(machine.getLevel(), machine.getBlockPos());
        var input = machine == null ? new ItemStackHandler(1) : machine.inventory();
        addSlot(new SlotItemHandler(input, 0, 54, 58) {
            @Override public boolean mayPlace(ItemStack stack) { return !busy() && stack.is(ForeverSkyblock.FOREVER_CORE.get()) && stack.getCount() >= 1; }
            @Override public boolean mayPickup(Player player) { return !busy(); }
            @Override public int getMaxStackSize() { return 1; }
        });
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(player, col + row * 9 + 9, MachineLayout.ASCENSION.inventoryX(col), MachineLayout.ASCENSION.inventoryRowY(row)));
        for (int col = 0; col < 9; col++) addSlot(new Slot(player, col, MachineLayout.ASCENSION.inventoryX(col), MachineLayout.ASCENSION.hotbarY()));
        addDataSlots(data);
    }
    public int progress() { return data.get(0); }
    public int status() { return data.get(1); }
    public boolean busy() { return data.get(2) != 0; }
    public UUID owner() {
        long most = 0, least = 0;
        for (int i = 0; i < 4; i++) { most |= (data.get(3 + i) & 65535L) << (i * 16); least |= (data.get(7 + i) & 65535L) << (i * 16); }
        return new UUID(most, least);
    }
    @Override public boolean stillValid(Player player) { return stillValid(access, player, ForeverSkyblock.ASCENSION_CHAMBER.get()); }
    @Override public boolean clickMenuButton(Player player, int button) {
        if (button != 0 || machine == null || !stillValid(player) || !(player instanceof ServerPlayer serverPlayer)) return false;
        boolean started = machine.start(serverPlayer); broadcastChanges(); return started;
    }
    @Override public ItemStack quickMoveStack(Player player, int index) {
        if (index < 0 || index >= slots.size()) return ItemStack.EMPTY;
        Slot slot = slots.get(index);
        if (!slot.hasItem() || !slot.mayPickup(player)) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem(), original = stack.copy();
        if (index == 0) { if (!moveItemStackTo(stack, 1, 37, true)) return ItemStack.EMPTY; }
        else if (!busy() && stack.is(ForeverSkyblock.FOREVER_CORE.get())) { if (!moveItemStackTo(stack, 0, 1, false)) return ItemStack.EMPTY; }
        else if (index < 28) { if (!moveItemStackTo(stack, 28, 37, false)) return ItemStack.EMPTY; }
        else if (!moveItemStackTo(stack, 1, 28, false)) return ItemStack.EMPTY;
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack); return original;
    }
}

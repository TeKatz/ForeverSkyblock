package de.tekatz.foreverskyblock.menu;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.AbstractContainerMenu;
import net.minecraft.world.inventory.ClickType;
import net.minecraft.world.inventory.ContainerData;
import net.minecraft.world.inventory.ContainerLevelAccess;
import net.minecraft.world.inventory.DataSlot;
import net.minecraft.world.inventory.SimpleContainerData;
import net.minecraft.world.inventory.Slot;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.items.ItemStackHandler;
import net.neoforged.neoforge.items.SlotItemHandler;
import org.jetbrains.annotations.Nullable;

public final class CondenserMenu extends AbstractContainerMenu {
    public static final int FIRST_X = 28, SECOND_X = 50, INPUT_Y = 38, OUTPUT_X = 126;
    private final ContainerData data;
    private final ContainerLevelAccess access;
    private final DataSlot editMode = DataSlot.standalone();
    private final @Nullable CondenserBlockEntity machine;
    public CondenserMenu(int id, Inventory inventory) {
        this(id, inventory, new ItemStackHandler(3), new SimpleContainerData(CondenserBlockEntity.DATA_COUNT), ContainerLevelAccess.NULL, null);
    }
    public CondenserMenu(int id, Inventory inventory, CondenserBlockEntity machine) {
        this(id, inventory, machine.inventory(), machine.menuData(), ContainerLevelAccess.create(machine.getLevel(), machine.getBlockPos()), machine);
    }
    private CondenserMenu(int id, Inventory playerInventory, ItemStackHandler inventory, ContainerData data,
                          ContainerLevelAccess access, @Nullable CondenserBlockEntity machine) {
        super(ForeverSkyblock.CONDENSER_MENU.get(), id);
        this.data = data;
        this.access = access;
        this.machine = machine;
        this.ownerLevel = playerInventory.player.level();
        addSlot(inputSlot(inventory, 0, FIRST_X, playerInventory));
        addSlot(new SlotItemHandler(inventory, 1, OUTPUT_X, INPUT_Y) {
            @Override public boolean mayPlace(ItemStack stack) { return false; }
        });
        addSlot(inputSlot(inventory, 2, SECOND_X, playerInventory));
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col + row * 9 + 9, 8 + col * 18, 124 + row * 18));
        for (int col = 0; col < 9; col++) addSlot(new Slot(playerInventory, col, 8 + col * 18, 182));
        addDataSlots(data);
        addDataSlot(editMode);
    }
    private Slot inputSlot(ItemStackHandler inventory, int index, int x, Inventory playerInventory) {
        return new SlotItemHandler(inventory, index, x, INPUT_Y) {
            @Override public boolean isActive() { return index == 0 || hasModule(); }
            @Override public boolean mayPlace(ItemStack stack) {
                return isActive() && !editing() && acceptsGhost(index, stack)
                        && (machine == null || inventory.isItemValid(index, stack));
            }
            @Override public boolean mayPickup(Player player) { return isActive() && !editing() && super.mayPickup(player); }
        };
    }
    public int progress() { return data.get(0); }
    public int duration() { return Math.max(1, data.get(1)); }
    public int status() { return data.get(2); }
    public boolean hasModule() { return data.get(3) != 0; }
    public boolean editing() { return editMode.get() != 0; }
    public ItemStack filter(int slot) {
        if (!CondenserBlockEntity.isInput(slot)) return ItemStack.EMPTY;
        int base = slot == 0 ? 4 : 6;
        int id = (data.get(base) & 65535) | (data.get(base + 1) & 65535) << 16;
        return new ItemStack(BuiltInRegistries.ITEM.byId(id));
    }
    public boolean acceptsGhost(int slot, ItemStack stack) {
        if (!CondenserBlockEntity.isInput(slot) || slot == 2 && !hasModule()) return false;
        if (stack.isEmpty()) return true;
        var level = ownerLevel;
        return CondenserBlockEntity.acceptsInput(level, stack, hasModule())
                && (slots.get(slot).getItem().isEmpty() || ItemStack.isSameItem(slots.get(slot).getItem(), stack))
                && (editing() || filter(slot).isEmpty() || ItemStack.isSameItem(filter(slot), stack));
    }
    private net.minecraft.world.level.Level ownerLevel;
    @Override public boolean clickMenuButton(Player player, int button) {
        if (button != 0 || !stillValid(player)) return false;
        editMode.set(editing() ? 0 : 1);
        broadcastChanges();
        return true;
    }
    public boolean setGhost(Player player, int slot, ItemStack stack) {
        if (machine == null || !editing() || !stillValid(player)) return false;
        boolean accepted = machine.setFilter(slot, stack);
        broadcastChanges();
        return accepted;
    }
    public boolean transferRecipe(Player player, net.minecraft.resources.ResourceLocation recipeId, boolean maximum) {
        if (machine == null || !stillValid(player)) return false;
        var holder = player.level().getRecipeManager().byKey(recipeId).orElse(null);
        if (holder == null || !(holder.value() instanceof de.tekatz.foreverskyblock.recipe.CondenserRecipe recipe)) return false;
        CondenserTransfer.Plan plan = CondenserTransfer.plan(this, recipe, maximum);
        if (!plan.success()) { broadcastChanges(); return false; }
        // Apply the complete server-side plan together. Failure never mutates or drops any items.
        for (int i = 0; i < 36; i++) getSlot(i + 3).set(plan.inventory().get(i));
        machine.inventory().setStackInSlot(0, plan.inputs().get(0));
        if (recipe.requiresModule()) machine.inventory().setStackInSlot(2, plan.inputs().get(1));
        player.getInventory().setChanged();
        broadcastChanges();
        return true;
    }
    @Override public void clicked(int slotId, int button, ClickType type, Player player) {
        if (CondenserBlockEntity.isInput(slotId) && (editing() || !slots.get(slotId).isActive())) return;
        super.clicked(slotId, button, type, player);
    }
    @Override public boolean canDragTo(Slot slot) {
        return slot.isActive() && (!editing() || !CondenserBlockEntity.isInput(slot.index)) && super.canDragTo(slot);
    }
    @Override public boolean stillValid(Player player) { return stillValid(access, player, ForeverSkyblock.CONDENSER_MARK_I.get()); }
    @Override public ItemStack quickMoveStack(Player player, int index) {
        if (index < 0 || index >= slots.size()) return ItemStack.EMPTY;
        Slot slot = slots.get(index);
        if (!slot.hasItem() || !slot.mayPickup(player)) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem();
        ItemStack original = stack.copy();
        if (index < 3) {
            if (!moveItemStackTo(stack, 3, slots.size(), true)) return ItemStack.EMPTY;
        } else {
            boolean moved = false;
            if (slots.get(0).mayPlace(stack)) moved = moveItemStackTo(stack, 0, 1, false);
            if (!stack.isEmpty() && slots.get(2).mayPlace(stack)) moved |= moveItemStackTo(stack, 2, 3, false);
            if (!moved) {
                if (index < 30) {
                    if (!moveItemStackTo(stack, 30, 39, false)) return ItemStack.EMPTY;
                } else if (!moveItemStackTo(stack, 3, 30, false)) return ItemStack.EMPTY;
            }
        }
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack);
        return original;
    }
}

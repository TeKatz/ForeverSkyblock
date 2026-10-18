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
    public static final int MAIN_INPUT_X = 30, MIXING_INPUT_X = 80, MAIN_OUTPUT_X = 202, MAIN_Y = 61;
    public static final int FREEZING_INPUT_X = 30, FREEZING_OUTPUT_X = 202, FREEZING_Y = 128;
    public static final int GARDEN_SPECIMEN_X = 30, GARDEN_PRIMARY_X = 174, GARDEN_SECONDARY_X = 202, GARDEN_Y = 136;
    public static final int MACHINE_SLOT_COUNT = CondenserBlockEntity.MACHINE_SLOTS;

    private final ContainerData data;
    private final ContainerLevelAccess access;
    private final DataSlot editMode = DataSlot.standalone();
    private final @Nullable CondenserBlockEntity machine;
    private final net.minecraft.world.level.Level ownerLevel;

    public CondenserMenu(int id, Inventory inventory) {
        this(id, inventory, new ItemStackHandler(MACHINE_SLOT_COUNT), new SimpleContainerData(CondenserBlockEntity.DATA_COUNT), ContainerLevelAccess.NULL, null);
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
        addSlot(inputSlot(inventory, CondenserBlockEntity.INPUT, MAIN_INPUT_X, MAIN_Y));
        addSlot(outputSlot(inventory, CondenserBlockEntity.OUTPUT, MAIN_OUTPUT_X, MAIN_Y));
        addSlot(inputSlot(inventory, CondenserBlockEntity.SECOND_INPUT, MIXING_INPUT_X, MAIN_Y));
        addSlot(inputSlot(inventory, CondenserBlockEntity.FREEZING_INPUT, FREEZING_INPUT_X, FREEZING_Y));
        addSlot(outputSlot(inventory, CondenserBlockEntity.FREEZING_OUTPUT, FREEZING_OUTPUT_X, FREEZING_Y));
        addSlot(gardenSpecimenSlot(inventory));
        addSlot(gardenOutputSlot(inventory, CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY, GARDEN_PRIMARY_X));
        addSlot(gardenOutputSlot(inventory, CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY, GARDEN_SECONDARY_X));
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col + row * 9 + 9, MachineLayout.CONDENSER.inventoryX(col), MachineLayout.CONDENSER.inventoryRowY(row)));
        for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col, MachineLayout.CONDENSER.inventoryX(col), MachineLayout.CONDENSER.hotbarY()));
        addDataSlots(data);
        addDataSlot(editMode);
    }

    private Slot inputSlot(ItemStackHandler inventory, int index, int x, int y) {
        return new SlotItemHandler(inventory, index, x, y) {
            @Override public boolean isActive() { return inputActive(index); }
            @Override public boolean mayPlace(ItemStack stack) {
                return isActive() && !editing() && acceptsGhost(index, stack)
                        && (machine == null || inventory.isItemValid(index, stack));
            }
            @Override public boolean mayPickup(Player player) { return isActive() && !editing() && super.mayPickup(player); }
        };
    }
    private Slot outputSlot(ItemStackHandler inventory, int index, int x, int y) {
        return new SlotItemHandler(inventory, index, x, y) {
            @Override public boolean isActive() {
                return !hasGardenChamber() && (index == CondenserBlockEntity.OUTPUT || hasFreezingChamber());
            }
            @Override public boolean mayPlace(ItemStack stack) { return false; }
            @Override public boolean mayPickup(Player player) { return isActive() && super.mayPickup(player); }
        };
    }
    private Slot gardenSpecimenSlot(ItemStackHandler inventory) {
        return new SlotItemHandler(inventory, CondenserBlockEntity.GARDEN_SPECIMEN, GARDEN_SPECIMEN_X, GARDEN_Y) {
            @Override public boolean isActive() { return hasGardenChamber(); }
            @Override public boolean mayPlace(ItemStack stack) {
                return isActive() && !stack.isEmpty() && CondenserBlockEntity.acceptsGardenInput(ownerLevel, stack)
                        && (machine == null || inventory.isItemValid(CondenserBlockEntity.GARDEN_SPECIMEN, stack));
            }
            @Override public boolean mayPickup(Player player) { return isActive() && super.mayPickup(player); }
            @Override public int getMaxStackSize() { return 1; }
        };
    }
    private Slot gardenOutputSlot(ItemStackHandler inventory, int index, int x) {
        return new SlotItemHandler(inventory, index, x, GARDEN_Y) {
            @Override public boolean isActive() { return hasGardenChamber(); }
            @Override public boolean mayPlace(ItemStack stack) { return false; }
            @Override public boolean mayPickup(Player player) { return isActive() && super.mayPickup(player); }
        };
    }
    private boolean inputActive(int index) {
        if (hasGardenChamber()) return false;
        return switch (index) {
            case CondenserBlockEntity.INPUT -> true;
            case CondenserBlockEntity.SECOND_INPUT -> hasMixingChamber();
            case CondenserBlockEntity.FREEZING_INPUT -> hasFreezingChamber();
            default -> false;
        };
    }

    public int progress() { return data.get(0); }
    public int duration() { return Math.max(1, data.get(1)); }
    public int status() { return data.get(2); }
    public boolean hasMixingChamber() { return data.get(3) != 0; }
    public boolean hasModule() { return hasMixingChamber(); }
    public boolean hasFreezingChamber() { return data.get(4) != 0; }
    public int freezingProgress() { return data.get(5); }
    public int freezingStatus() { return data.get(6); }
    public int machineTier() { return Math.max(1, data.get(13)); }
    public boolean hasGardenChamber() { return data.get(14) != 0; }
    public int gardenProgress() { return data.get(15); }
    public int gardenDuration() { return Math.max(1, data.get(16)); }
    public int gardenStatus() { return data.get(17); }
    public boolean editing() { return editMode.get() != 0; }
    public int moduleState() { return (hasMixingChamber() ? 1 : 0) | (hasFreezingChamber() ? 2 : 0); }

    public ItemStack filter(int slot) {
        if (!CondenserBlockEntity.isInput(slot)) return ItemStack.EMPTY;
        int base = switch (slot) {
            case CondenserBlockEntity.INPUT -> 7;
            case CondenserBlockEntity.SECOND_INPUT -> 9;
            case CondenserBlockEntity.FREEZING_INPUT -> 11;
            default -> -1;
        };
        if (base < 0) return ItemStack.EMPTY;
        int id = (data.get(base) & 65535) | (data.get(base + 1) & 65535) << 16;
        return new ItemStack(BuiltInRegistries.ITEM.byId(id));
    }
    public boolean acceptsGhost(int slot, ItemStack stack) {
        if (hasGardenChamber() || !CondenserBlockEntity.isInput(slot) || !inputActive(slot)) return false;
        if (stack.isEmpty()) return true;
        boolean acceptable = slot == CondenserBlockEntity.FREEZING_INPUT
                || CondenserBlockEntity.acceptsInput(ownerLevel, stack, hasMixingChamber(), machineTier());
        return acceptable
                && (slots.get(slot).getItem().isEmpty() || ItemStack.isSameItem(slots.get(slot).getItem(), stack))
                && (editing() || filter(slot).isEmpty() || ItemStack.isSameItem(filter(slot), stack));
    }

    @Override public boolean clickMenuButton(Player player, int button) {
        if (button != 0 || !stillValid(player) || hasGardenChamber()) return false;
        editMode.set(editing() ? 0 : 1);
        broadcastChanges();
        return true;
    }
    public boolean setGhost(Player player, int slot, ItemStack stack) {
        if (machine == null || hasGardenChamber() || !editing() || !stillValid(player)) return false;
        boolean accepted = machine.setFilter(slot, stack);
        broadcastChanges();
        return accepted;
    }
    public boolean transferRecipe(Player player, net.minecraft.resources.ResourceLocation recipeId, boolean maximum) {
        if (machine == null || hasGardenChamber() || !stillValid(player)) return false;
        var holder = player.level().getRecipeManager().byKey(recipeId).orElse(null);
        if (holder == null || !(holder.value() instanceof de.tekatz.foreverskyblock.recipe.CondenserRecipe recipe)) return false;
        CondenserTransfer.Plan plan = CondenserTransfer.plan(this, recipe, maximum);
        if (!plan.success()) { broadcastChanges(); return false; }
        // Condensing transfers never touch the parallel Freezing or Garden storage.
        for (int i = 0; i < 36; i++) getSlot(i + MACHINE_SLOT_COUNT).set(plan.inventory().get(i));
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, plan.inputs().get(0));
        if (recipe.requiresModule()) machine.inventory().setStackInSlot(CondenserBlockEntity.SECOND_INPUT, plan.inputs().get(1));
        player.getInventory().setChanged();
        broadcastChanges();
        return true;
    }

    private boolean isMachineInputSlot(int slotId) {
        return CondenserBlockEntity.isInput(slotId) || slotId == CondenserBlockEntity.GARDEN_SPECIMEN;
    }

    @Override public void clicked(int slotId, int button, ClickType type, Player player) {
        if (slotId >= 0 && slotId < MACHINE_SLOT_COUNT && !slots.get(slotId).isActive()) return;
        if (CondenserBlockEntity.isInput(slotId) && editing()) return;
        super.clicked(slotId, button, type, player);
    }
    @Override public boolean canDragTo(Slot slot) {
        int menuSlot = slots.indexOf(slot);
        boolean machineInput = menuSlot >= 0 && menuSlot < MACHINE_SLOT_COUNT && isMachineInputSlot(menuSlot);
        return slot.isActive() && (!editing() || !machineInput || menuSlot == CondenserBlockEntity.GARDEN_SPECIMEN) && super.canDragTo(slot);
    }
    @Override public boolean stillValid(Player player) {
        return access.evaluate((level, pos) -> (level.getBlockState(pos).is(ForeverSkyblock.CONDENSER_MARK_I.get())
                || level.getBlockState(pos).is(ForeverSkyblock.CONDENSER_MARK_II.get()))
                && player.distanceToSqr(pos.getX() + .5, pos.getY() + .5, pos.getZ() + .5) <= 64.0, true);
    }

    @Override public ItemStack quickMoveStack(Player player, int index) {
        if (index < 0 || index >= slots.size()) return ItemStack.EMPTY;
        Slot slot = slots.get(index);
        if (!slot.hasItem() || !slot.mayPickup(player)) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem();
        ItemStack original = stack.copy();
        if (index < MACHINE_SLOT_COUNT) {
            if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, slots.size(), true)) return ItemStack.EMPTY;
        } else if (hasGardenChamber()) {
            if (!slots.get(CondenserBlockEntity.GARDEN_SPECIMEN).mayPlace(stack)
                    || !moveItemStackTo(stack, CondenserBlockEntity.GARDEN_SPECIMEN, CondenserBlockEntity.GARDEN_SPECIMEN + 1, false)) {
                int hotbarStart = MACHINE_SLOT_COUNT + 27;
                if (index < hotbarStart) {
                    if (!moveItemStackTo(stack, hotbarStart, MACHINE_SLOT_COUNT + 36, false)) return ItemStack.EMPTY;
                } else if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, hotbarStart, false)) return ItemStack.EMPTY;
            }
        } else {
            // The Freezing input accepts every item, so it is deliberately never a generic shift-click sink.
            boolean moved = false;
            if (slots.get(CondenserBlockEntity.INPUT).mayPlace(stack))
                moved = moveItemStackTo(stack, CondenserBlockEntity.INPUT, CondenserBlockEntity.INPUT + 1, false);
            if (!stack.isEmpty() && slots.get(CondenserBlockEntity.SECOND_INPUT).mayPlace(stack))
                moved |= moveItemStackTo(stack, CondenserBlockEntity.SECOND_INPUT, CondenserBlockEntity.SECOND_INPUT + 1, false);
            if (!moved) {
                int hotbarStart = MACHINE_SLOT_COUNT + 27;
                if (index < hotbarStart) {
                    if (!moveItemStackTo(stack, hotbarStart, MACHINE_SLOT_COUNT + 36, false)) return ItemStack.EMPTY;
                } else if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, hotbarStart, false)) return ItemStack.EMPTY;
            }
        }
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack);
        return original;
    }
}

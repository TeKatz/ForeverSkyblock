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
    public static final int GARDEN_SPECIMEN_X = 30, GARDEN_PRIMARY_X = 174, GARDEN_SECONDARY_X = 202, GARDEN_Y = 151;
    public static final int UPGRADE_1_X = 30, UPGRADE_2_X = 58, UPGRADE_3_X = 86, UPGRADE_Y = 224;
    public static final int MACHINE_SLOT_COUNT = CondenserBlockEntity.MACHINE_SLOTS;
    public static final int BUTTON_FILTERS = 0;
    public static final int BUTTON_POLICY_MAIN = 10, BUTTON_POLICY_FREEZING = 11,
            BUTTON_POLICY_GARDEN_PRIMARY = 12, BUTTON_POLICY_GARDEN_SECONDARY = 13;

    private final ContainerData data;
    private final ContainerLevelAccess access;
    private final DataSlot editMode = DataSlot.standalone();
    private final DataSlot[] outputTotals = new DataSlot[4];
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
        addSlot(upgradeSlot(inventory, CondenserBlockEntity.UPGRADE_1, UPGRADE_1_X));
        addSlot(upgradeSlot(inventory, CondenserBlockEntity.UPGRADE_2, UPGRADE_2_X));
        addSlot(upgradeSlot(inventory, CondenserBlockEntity.UPGRADE_3, UPGRADE_3_X));
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col + row * 9 + 9, MachineLayout.CONDENSER.inventoryX(col), MachineLayout.CONDENSER.inventoryRowY(row)));
        for (int col = 0; col < 9; col++)
            addSlot(new Slot(playerInventory, col, MachineLayout.CONDENSER.inventoryX(col), MachineLayout.CONDENSER.hotbarY()));
        addDataSlots(data);
        addDataSlot(editMode);
        int[] tracked = {CondenserBlockEntity.OUTPUT, CondenserBlockEntity.FREEZING_OUTPUT,
                CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY, CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY};
        for (int i = 0; i < tracked.length; i++) {
            final int machineSlot = tracked[i];
            DataSlot total = machine == null ? DataSlot.standalone() : new DataSlot() {
                @Override public int get() { return serverOutputTotal(machineSlot); }
                @Override public void set(int value) { }
            };
            outputTotals[i] = total;
            addDataSlot(total);
        }
    }

    private int serverOutputTotal(int slot) {
        if (machine == null) return 0;
        int total = machine.inventory().getStackInSlot(slot).getCount();
        int lane = switch (slot) {
            case CondenserBlockEntity.OUTPUT -> 0;
            case CondenserBlockEntity.FREEZING_OUTPUT -> 1;
            case CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY -> 2;
            case CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY -> 3;
            default -> -1;
        };
        if (lane >= 0) for (int segment = 0; segment < 2; segment++)
            total += machine.outputBuffers().getStackInSlot(lane * 2 + segment).getCount();
        return total;
    }
    private ItemStack displayBufferedCount(int slot, ItemStack stack) {
        if (!ownerLevel.isClientSide || bufferUpgrades() <= 0 || stack.isEmpty()) return stack;
        int total = storedOutputCount(slot);
        return total > stack.getCount() ? stack.copyWithCount(total) : stack;
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
            @Override public ItemStack getItem() { return displayBufferedCount(index, super.getItem()); }
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
            @Override public ItemStack getItem() { return displayBufferedCount(index, super.getItem()); }
            @Override public boolean mayPlace(ItemStack stack) { return false; }
            @Override public boolean mayPickup(Player player) { return isActive() && super.mayPickup(player); }
        };
    }
    private Slot upgradeSlot(ItemStackHandler inventory, int index, int x) {
        return new SlotItemHandler(inventory, index, x, UPGRADE_Y) {
            @Override public boolean isActive() { return machineTier() >= 2; }
            @Override public boolean mayPlace(ItemStack stack) {
                return isActive() && CondenserBlockEntity.isUpgradeCard(stack)
                        && (machine == null || inventory.isItemValid(index, stack));
            }
            @Override public int getMaxStackSize() { return 1; }
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
    public int speedUpgrades() { return data.get(18); }
    public int gardenUpgrades() { return data.get(19); }
    public int bufferUpgrades() { return data.get(20); }
    public int batchUpgrades() { return data.get(21); }
    public int upgradeCapacityUsed() { return data.get(22); }
    public boolean editing() { return editMode.get() != 0; }
    public int moduleState() { return (hasMixingChamber() ? 1 : 0) | (hasFreezingChamber() ? 2 : 0); }

    public int outputPolicy(int slot) {
        return switch (slot) {
            case CondenserBlockEntity.OUTPUT -> data.get(23);
            case CondenserBlockEntity.FREEZING_OUTPUT -> data.get(24);
            case CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY -> data.get(25);
            case CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY -> data.get(26);
            default -> CondenserBlockEntity.POLICY_BLOCK;
        };
    }
    public int storedOutputCount(int slot) {
        return switch (slot) {
            case CondenserBlockEntity.OUTPUT -> outputTotals[0].get();
            case CondenserBlockEntity.FREEZING_OUTPUT -> outputTotals[1].get();
            case CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY -> outputTotals[2].get();
            case CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY -> outputTotals[3].get();
            default -> 0;
        };
    }

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
        if (!stillValid(player)) return false;
        if (button == BUTTON_FILTERS) {
            if (hasGardenChamber()) return false;
            editMode.set(editing() ? 0 : 1);
            broadcastChanges();
            return true;
        }
        if (machine == null || machineTier() < 2) return false;
        int slot = switch (button) {
            case BUTTON_POLICY_MAIN -> CondenserBlockEntity.OUTPUT;
            case BUTTON_POLICY_FREEZING -> CondenserBlockEntity.FREEZING_OUTPUT;
            case BUTTON_POLICY_GARDEN_PRIMARY -> CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY;
            case BUTTON_POLICY_GARDEN_SECONDARY -> CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY;
            default -> -1;
        };
        if (slot < 0 || !machine.outputActive(slot)) return false;
        machine.cycleOutputPolicy(slot);
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
        } else if (machineTier() >= 2 && CondenserBlockEntity.isUpgradeCard(stack)
                && moveItemStackTo(stack, CondenserBlockEntity.UPGRADE_1, CondenserBlockEntity.UPGRADE_3 + 1, false)) {
            // Upgrade installed.
        } else if (hasGardenChamber()) {
            if (!slots.get(CondenserBlockEntity.GARDEN_SPECIMEN).mayPlace(stack)
                    || !moveItemStackTo(stack, CondenserBlockEntity.GARDEN_SPECIMEN, CondenserBlockEntity.GARDEN_SPECIMEN + 1, false)) {
                int hotbarStart = MACHINE_SLOT_COUNT + 27;
                if (index < hotbarStart) {
                    if (!moveItemStackTo(stack, hotbarStart, MACHINE_SLOT_COUNT + 36, false)) return ItemStack.EMPTY;
                } else if (!moveItemStackTo(stack, MACHINE_SLOT_COUNT, hotbarStart, false)) return ItemStack.EMPTY;
            }
        } else {
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

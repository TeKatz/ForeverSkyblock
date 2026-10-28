package de.tekatz.foreverskyblock.studio.runtime;

import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.resources.ResourceLocation;
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

import java.util.ArrayList;
import java.util.List;

/** One synchronized menu protocol for all Studio-compiled runtime machines. */
public final class ForeverStudioMachineMenu extends AbstractContainerMenu {
    private final ResourceLocation machineId;
    private final ForeverStudioMachineDefinition definition;
    private final ContainerData data;
    private final ContainerLevelAccess access;
    private final net.minecraft.world.level.Level ownerLevel;
    private final int machineMenuSlots;
    private final List<Integer> inputMenuIndices;
    private final List<Integer> storageMenuIndices;

    public ForeverStudioMachineMenu(int id, Inventory inventory, RegistryFriendlyByteBuf extraData) {
        this(id, inventory, readMachineId(extraData));
    }

    private ForeverStudioMachineMenu(int id, Inventory inventory, ResourceLocation machineId) {
        this(id, inventory, machineId, ForeverStudioMachineDefinition.load(machineId),
                new ItemStackHandler(Math.max(1, ForeverStudioMachineDefinition.load(machineId).slotCount())),
                new SimpleContainerData(ForeverStudioMachineBlockEntity.DATA_COUNT), ContainerLevelAccess.NULL);
    }

    public ForeverStudioMachineMenu(int id, Inventory inventory, ForeverStudioMachineBlockEntity machine) {
        this(id, inventory, machine.machineId(), machine.definition(), machine.inventory(), machine.menuData(),
                ContainerLevelAccess.create(machine.getLevel(), machine.getBlockPos()));
    }

    private ForeverStudioMachineMenu(int id, Inventory playerInventory, ResourceLocation machineId,
                                     ForeverStudioMachineDefinition definition, ItemStackHandler machineInventory,
                                     ContainerData data, ContainerLevelAccess access) {
        super(ForeverStudioGeneratedMachines.MACHINE_MENU.get(), id);
        this.machineId = machineId;
        this.definition = definition;
        this.data = data;
        this.access = access;
        this.ownerLevel = playerInventory.player.level();

        int menuIndex = 0;
        List<Integer> inputs = new ArrayList<>();
        List<Integer> storage = new ArrayList<>();
        for (ForeverStudioMachineDefinition.SlotDefinition slot : definition.slots()) {
            final boolean input = slot.input();
            final boolean storageSlot = slot.storage();
            final int inputIndex = input ? definition.inputSlots().indexOf(slot.slot()) : -1;
            SlotItemHandler menuSlot = new SlotItemHandler(machineInventory, slot.slot(), slot.x(), slot.y()) {
                @Override public boolean mayPlace(ItemStack stack) {
                    if (storageSlot) return true;
                    return input && ForeverStudioMachineBlockEntity.acceptsInput(ownerLevel, machineId, inputIndex, stack);
                }
            };
            addSlot(menuSlot);
            if (input) inputs.add(menuIndex);
            if (storageSlot) storage.add(menuIndex);
            menuIndex++;
        }
        this.machineMenuSlots = menuIndex;
        this.inputMenuIndices = List.copyOf(inputs);
        this.storageMenuIndices = List.copyOf(storage);

        int inventoryY = definition.guiHeight() + 14;
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 9; col++)
                addSlot(new Slot(playerInventory, col + row * 9 + 9, 8 + col * 18, inventoryY + row * 18));
        int hotbarY = inventoryY + 58;
        for (int col = 0; col < 9; col++) addSlot(new Slot(playerInventory, col, 8 + col * 18, hotbarY));
        addDataSlots(data);
    }

    private static ResourceLocation readMachineId(RegistryFriendlyByteBuf buffer) {
        ResourceLocation id = ResourceLocation.tryParse(buffer.readUtf(256));
        if (id == null) throw new IllegalArgumentException("Invalid Forever Studio machine id");
        return id;
    }

    public ResourceLocation machineId() { return machineId; }
    public ForeverStudioMachineDefinition definition() { return definition; }
    public int progress() { return data.get(0); }
    public int duration() { return Math.max(1, data.get(1)); }
    public int status() { return data.get(2); }
    public int energy() { return data.get(3); }
    public int energyCapacity() { return Math.max(0, data.get(4)); }
    public int fluidAmount(int tank) {
        return tank < 0 || tank >= ForeverStudioMachineBlockEntity.MAX_SYNC_TANKS
                ? 0 : Math.max(0, data.get(ForeverStudioMachineBlockEntity.FLUID_DATA_START + tank));
    }
    public int fluidCapacity(int tank) {
        return tank < 0 || tank >= definition.tanks().size() ? 0 : definition.tanks().get(tank).capacity();
    }
    public int stateIndex() { return Math.max(0, data.get(ForeverStudioMachineBlockEntity.STATE_DATA_INDEX)); }
    public double animationPhase(int index) {
        if (index < 0 || index >= ForeverStudioMachineBlockEntity.MAX_SYNC_ANIMATIONS) return 0.0;
        return Math.clamp(data.get(ForeverStudioMachineBlockEntity.ANIMATION_DATA_START + index) / 10_000.0, 0.0, 1.0);
    }

    @Override public boolean stillValid(Player player) {
        return access.evaluate((level, pos) -> level.getBlockEntity(pos) instanceof ForeverStudioMachineBlockEntity machine
                && machine.machineId().equals(machineId)
                && player.distanceToSqr(pos.getX() + 0.5, pos.getY() + 0.5, pos.getZ() + 0.5) <= 64.0, true);
    }

    @Override public ItemStack quickMoveStack(Player player, int index) {
        if (index < 0 || index >= slots.size()) return ItemStack.EMPTY;
        Slot slot = slots.get(index);
        if (!slot.hasItem() || !slot.mayPickup(player)) return ItemStack.EMPTY;
        ItemStack stack = slot.getItem();
        ItemStack original = stack.copy();
        if (index < machineMenuSlots) {
            if (!moveItemStackTo(stack, machineMenuSlots, slots.size(), true)) return ItemStack.EMPTY;
        } else {
            boolean moved = moveToCandidates(stack, inputMenuIndices);
            if (!moved) moved = moveToCandidates(stack, storageMenuIndices);
            if (!moved) {
                int hotbarStart = machineMenuSlots + 27;
                if (index < hotbarStart) {
                    if (!moveItemStackTo(stack, hotbarStart, machineMenuSlots + 36, false)) return ItemStack.EMPTY;
                } else if (!moveItemStackTo(stack, machineMenuSlots, hotbarStart, false)) return ItemStack.EMPTY;
            }
        }
        if (stack.isEmpty()) slot.setByPlayer(ItemStack.EMPTY); else slot.setChanged();
        if (stack.getCount() == original.getCount()) return ItemStack.EMPTY;
        slot.onTake(player, stack);
        return original;
    }

    private boolean moveToCandidates(ItemStack stack, List<Integer> indices) {
        for (int target : indices) {
            if (slots.get(target).mayPlace(stack) && moveItemStackTo(stack, target, target + 1, false)) return true;
        }
        return false;
    }
}

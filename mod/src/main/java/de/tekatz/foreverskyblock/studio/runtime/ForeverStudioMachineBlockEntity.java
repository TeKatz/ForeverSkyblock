package de.tekatz.foreverskyblock.studio.runtime;

import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.HolderLookup;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.MenuProvider;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.AbstractContainerMenu;
import net.minecraft.world.inventory.ContainerData;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.material.Fluid;
import net.minecraft.world.level.material.Fluids;
import net.neoforged.neoforge.energy.IEnergyStorage;
import net.neoforged.neoforge.fluids.FluidStack;
import net.neoforged.neoforge.fluids.capability.IFluidHandler;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

import java.util.ArrayList;
import java.util.List;

/** Shared server runtime for Studio-compiled item, fluid, energy and graph-driven machines. */
public final class ForeverStudioMachineBlockEntity extends BlockEntity implements MenuProvider {
    public static final int MAX_SYNC_TANKS = 8;
    public static final int MAX_SYNC_ANIMATIONS = 8;
    public static final int FLUID_DATA_START = 5;
    public static final int STATE_DATA_INDEX = FLUID_DATA_START + MAX_SYNC_TANKS;
    public static final int ANIMATION_DATA_START = STATE_DATA_INDEX + 1;
    public static final int DATA_COUNT = ANIMATION_DATA_START + MAX_SYNC_ANIMATIONS;

    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2, NO_POWER = 3, GRAPH_BLOCKED = 4;

    private record Match(RecipeHolder<ForeverStudioMachineRecipe> holder,
                         ForeverStudioMachineRecipe.Entry entry,
                         int index) { }

    private static final class FluidCell {
        private @Nullable ResourceLocation fluid;
        private int amount;
        private final int capacity;
        private final int maxFill;
        private final int maxDrain;

        private FluidCell(ForeverStudioMachineDefinition.TankDefinition definition) {
            capacity = definition.capacity();
            maxFill = definition.maxFill();
            maxDrain = definition.maxDrain();
        }

        private boolean empty() { return fluid == null || amount <= 0; }
        private boolean matches(ResourceLocation id) { return !empty() && id.equals(fluid); }
        private int space() { return Math.max(0, capacity - amount); }
        private void normalize() { if (amount <= 0) { amount = 0; fluid = null; } }
    }

    private final ResourceLocation machineId;
    private final ForeverStudioMachineDefinition definition;
    private final ItemStackHandler inventory;
    private final List<FluidCell> fluids;
    private final IItemHandler automation;
    private final IFluidHandler fluidAccess;
    private final IEnergyStorage energyAccess;

    private int progress;
    private int duration = 1;
    private int status = IDLE;
    private int energy;
    private int stateIndex;
    private int playerUsePulse;
    private int inventoryChangedPulse;
    private @Nullable ResourceLocation activeRecipe;
    private int activeRecipeIndex = -1;

    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            if (index == 0) return progress;
            if (index == 1) return Math.max(1, duration);
            if (index == 2) return status;
            if (index == 3) return energy;
            if (index == 4) return definition.powered() ? definition.energyCapacity() : 0;
            if (index >= FLUID_DATA_START && index < FLUID_DATA_START + MAX_SYNC_TANKS) {
                int tank = index - FLUID_DATA_START;
                return tank < fluids.size() ? fluids.get(tank).amount : 0;
            }
            if (index == STATE_DATA_INDEX) return stateIndex;
            if (index >= ANIMATION_DATA_START && index < ANIMATION_DATA_START + MAX_SYNC_ANIMATIONS) {
                int animation = index - ANIMATION_DATA_START;
                return (int)Math.round(Math.clamp(animationPhase(animation), 0.0, 1.0) * 10_000.0);
            }
            return 0;
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public ForeverStudioMachineBlockEntity(BlockPos pos, BlockState state) {
        this(ForeverStudioGeneratedMachines.MACHINE_BLOCK_ENTITY.get(), pos, state, machineIdFromState(state));
    }

    ForeverStudioMachineBlockEntity(BlockEntityType<?> type, BlockPos pos, BlockState state, ResourceLocation machineId) {
        super(type, pos, state);
        this.machineId = machineId;
        definition = ForeverStudioMachineDefinition.load(machineId);
        stateIndex = definition.initialStateIndex();
        inventory = new ItemStackHandler(Math.max(1, definition.slotCount())) {
            @Override public boolean isItemValid(int slot, ItemStack stack) {
                if (definition.isStorageSlot(slot)) return true;
                if (!definition.isInputSlot(slot)) return false;
                return level == null || acceptsInput(level, ForeverStudioMachineBlockEntity.this.machineId,
                        definition.inputSlots().indexOf(slot), stack);
            }
            @Override protected void onContentsChanged(int slot) {
                inventoryChangedPulse = 2;
                setChanged();
            }
        };
        fluids = definition.tanks().stream().map(FluidCell::new).toList();
        automation = new Automation();
        fluidAccess = new FluidAccess();
        energyAccess = new EnergyAccess();
    }

    private static ResourceLocation machineIdFromState(BlockState state) {
        if (state.getBlock() instanceof ForeverStudioMachineBlock machineBlock) return machineBlock.machineId();
        throw new IllegalStateException("Forever Studio machine BlockEntity created for a non-Studio block.");
    }

    public ResourceLocation machineId() { return machineId; }
    public ForeverStudioMachineDefinition definition() { return definition; }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }
    public IItemHandler itemAutomation(@Nullable Direction side) { return automation; }
    public @Nullable IEnergyStorage energyAutomation(@Nullable Direction side) { return definition.powered() ? energyAccess : null; }
    public @Nullable IFluidHandler fluidAutomation(@Nullable Direction side) { return fluids.isEmpty() ? null : fluidAccess; }
    public int energyStored() { return energy; }
    public int progress() { return progress; }
    public int duration() { return Math.max(1, duration); }
    public boolean running() { return status == RUNNING; }
    public int status() { return status; }
    public int stateIndex() { return stateIndex; }
    public String stateId() { return definition.states().get(Math.clamp(stateIndex, 0, definition.states().size() - 1)).id(); }
    public boolean playerUsePulse() { return playerUsePulse > 0; }
    public boolean inventoryChangedPulse() { return inventoryChangedPulse > 0; }
    public int redstoneStrength() { return level == null ? 0 : level.getBestNeighborSignal(worldPosition); }

    public void markPlayerUse() {
        playerUsePulse = 2;
        setChanged();
    }

    public boolean setState(String id) {
        for (int i = 0; i < definition.states().size(); i++) {
            if (definition.states().get(i).id().equalsIgnoreCase(id)) {
                if (stateIndex != i) { stateIndex = i; setChanged(); }
                return true;
            }
        }
        return false;
    }

    public int fluidAmount(int tank) { return tank >= 0 && tank < fluids.size() ? fluids.get(tank).amount : 0; }
    public int fluidCapacity(int tank) { return tank >= 0 && tank < fluids.size() ? fluids.get(tank).capacity : 0; }
    public double fluidRatio(int tank) {
        int capacity = fluidCapacity(tank);
        return capacity <= 0 ? 0.0 : Math.clamp((double)fluidAmount(tank) / capacity, 0.0, 1.0);
    }
    public boolean hasFluid(int tank, ResourceLocation fluid, int amount) {
        return tank >= 0 && tank < fluids.size() && fluids.get(tank).matches(fluid) && fluids.get(tank).amount >= amount;
    }

    public double animationPhase(int index) {
        if (index < 0 || index >= definition.animations().size()) return 0.0;
        return ForeverStudioGraphRuntime.animationPhase(this, definition.animations().get(index));
    }

    public static boolean acceptsInput(Level level, ResourceLocation machineId, ItemStack stack) {
        return acceptsInput(level, machineId, 0, stack);
    }

    public static boolean acceptsInput(Level level, ResourceLocation machineId, int inputIndex, ItemStack stack) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverStudioGeneratedMachines.MACHINE_RECIPE_TYPE.get()).stream()
                .map(RecipeHolder::value)
                .filter(recipe -> recipe.machine().equals(machineId.toString()))
                .anyMatch(recipe -> recipe.acceptsItem(inputIndex, stack));
    }

    private ForeverStudioMachineInput currentInput() {
        return new ForeverStudioMachineInput(machineId, definition.inputSlots().stream().map(inventory::getStackInSlot).toList());
    }

    private @Nullable Match findRecipe() {
        if (level == null) return null;
        ForeverStudioMachineInput input = currentInput();
        for (RecipeHolder<ForeverStudioMachineRecipe> holder : level.getRecipeManager()
                .getAllRecipesFor(ForeverStudioGeneratedMachines.MACHINE_RECIPE_TYPE.get())) {
            ForeverStudioMachineRecipe book = holder.value();
            if (!book.machine().equals(machineId.toString())) continue;
            for (int i = 0; i < book.recipes().size(); i++) {
                ForeverStudioMachineRecipe.Entry entry = book.recipes().get(i);
                if (entry.acceptsItems(input) && fluidInputsPresent(entry)) return new Match(holder, entry, i);
            }
        }
        return null;
    }

    private boolean fluidInputsPresent(ForeverStudioMachineRecipe.Entry entry) {
        for (ForeverStudioMachineRecipe.FluidRequirement fluid : entry.fluidInputs())
            if (!hasFluid(fluid.tank(), fluid.fluidId(), fluid.amount())) return false;
        return true;
    }

    public static void tick(Level level, BlockPos pos, BlockState state, ForeverStudioMachineBlockEntity machine) {
        machine.process();
        if (machine.playerUsePulse > 0) machine.playerUsePulse--;
        if (machine.inventoryChangedPulse > 0) machine.inventoryChangedPulse--;
    }

    private void process() {
        if (!ForeverStudioGraphRuntime.allowsProcessor(this)) {
            resetProgressIfNeeded();
            status = GRAPH_BLOCKED;
            return;
        }

        Match match = findRecipe();
        if (match == null) {
            resetProgressIfNeeded();
            status = IDLE;
            return;
        }
        ForeverStudioMachineRecipe.Entry entry = match.entry();
        if (!match.holder().id().equals(activeRecipe) || match.index() != activeRecipeIndex) {
            progress = 0;
            activeRecipe = match.holder().id();
            activeRecipeIndex = match.index();
            setChanged();
        }
        duration = entry.processingTime();
        if (!outputsFit(entry.results()) || !fluidOutputsFit(entry.fluidOutputs())) {
            status = BLOCKED;
            return;
        }
        if (definition.powered() && !definition.generator() && energy < entry.energyPerTick()) {
            status = NO_POWER;
            return;
        }
        if (definition.generator() && entry.energyOutputPerTick() > 0 &&
                energy > definition.energyCapacity() - entry.energyOutputPerTick()) {
            status = BLOCKED;
            return;
        }

        status = RUNNING;
        if (definition.powered() && !definition.generator() && entry.energyPerTick() > 0)
            energy -= entry.energyPerTick();
        if (definition.generator() && entry.energyOutputPerTick() > 0)
            energy = Math.min(definition.energyCapacity(), energy + entry.energyOutputPerTick());
        progress++;
        setChanged();

        if (progress >= duration) {
            for (int i = 0; i < entry.inputs().size(); i++)
                inventory.extractItem(definition.inputSlots().get(i), entry.inputs().get(i).count(), false);
            for (ForeverStudioMachineRecipe.FluidRequirement fluid : entry.fluidInputs())
                drainCell(fluid.tank(), fluid.fluidId(), fluid.amount());
            insertOutputs(entry.results());
            for (ForeverStudioMachineRecipe.FluidRequirement fluid : entry.fluidOutputs())
                fillCell(fluid.tank(), fluid.fluidId(), fluid.amount());
            progress = 0;
            activeRecipe = null;
            activeRecipeIndex = -1;
            setChanged();
        }
    }

    private void resetProgressIfNeeded() {
        if (progress != 0 || activeRecipe != null || activeRecipeIndex != -1) {
            progress = 0;
            activeRecipe = null;
            activeRecipeIndex = -1;
            setChanged();
        }
        duration = 1;
    }

    private boolean outputsFit(List<ItemStack> results) {
        if (results.size() > definition.outputSlots().size()) return false;
        List<ItemStack> simulated = new ArrayList<>();
        for (int slot : definition.outputSlots()) simulated.add(inventory.getStackInSlot(slot).copy());
        for (int i = 0; i < results.size(); i++) {
            ItemStack result = results.get(i), output = simulated.get(i);
            if (!output.isEmpty() && (!ItemStack.isSameItemSameComponents(output, result) ||
                    output.getCount() + result.getCount() > Math.min(64, output.getMaxStackSize()))) return false;
            if (output.isEmpty()) simulated.set(i, result.copy()); else output.grow(result.getCount());
        }
        return true;
    }

    private boolean fluidOutputsFit(List<ForeverStudioMachineRecipe.FluidRequirement> results) {
        if (results.isEmpty()) return true;
        List<ResourceLocation> ids = new ArrayList<>();
        List<Integer> amounts = new ArrayList<>();
        for (FluidCell cell : fluids) { ids.add(cell.fluid); amounts.add(cell.amount); }
        for (ForeverStudioMachineRecipe.FluidRequirement result : results) {
            if (result.tank() < 0 || result.tank() >= fluids.size()) return false;
            FluidCell cell = fluids.get(result.tank());
            ResourceLocation existing = ids.get(result.tank());
            int amount = amounts.get(result.tank());
            if (existing != null && !existing.equals(result.fluidId())) return false;
            if ((long)amount + result.amount() > cell.capacity) return false;
            ids.set(result.tank(), result.fluidId());
            amounts.set(result.tank(), amount + result.amount());
        }
        return true;
    }

    private void insertOutputs(List<ItemStack> results) {
        for (int i = 0; i < results.size(); i++) {
            int slot = definition.outputSlots().get(i);
            ItemStack output = inventory.getStackInSlot(slot), result = results.get(i);
            if (output.isEmpty()) inventory.setStackInSlot(slot, result.copy());
            else output.grow(result.getCount());
        }
    }

    private int fillCell(int tank, ResourceLocation fluid, int amount) {
        if (tank < 0 || tank >= fluids.size() || amount <= 0) return 0;
        FluidCell cell = fluids.get(tank);
        if (!cell.empty() && !cell.matches(fluid)) return 0;
        int filled = Math.min(amount, cell.space());
        if (filled <= 0) return 0;
        cell.fluid = fluid;
        cell.amount += filled;
        setChanged();
        return filled;
    }

    private int drainCell(int tank, ResourceLocation fluid, int amount) {
        if (tank < 0 || tank >= fluids.size() || amount <= 0) return 0;
        FluidCell cell = fluids.get(tank);
        if (!cell.matches(fluid)) return 0;
        int drained = Math.min(amount, cell.amount);
        cell.amount -= drained;
        cell.normalize();
        if (drained > 0) setChanged();
        return drained;
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        tag.putInt("Duration", duration);
        tag.putInt("Status", status);
        tag.putInt("Energy", energy);
        tag.putInt("StateIndex", stateIndex);
        tag.putInt("ActiveRecipeIndex", activeRecipeIndex);
        if (activeRecipe != null) tag.putString("ActiveRecipe", activeRecipe.toString());
        for (int i = 0; i < fluids.size(); i++) {
            FluidCell cell = fluids.get(i);
            if (!cell.empty()) {
                tag.putString("Fluid" + i, cell.fluid.toString());
                tag.putInt("FluidAmount" + i, cell.amount);
            }
        }
    }

    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        inventory.deserializeNBT(registries, tag.getCompound("Inventory"));
        progress = Math.max(0, tag.getInt("Progress"));
        duration = Math.max(1, tag.getInt("Duration"));
        status = Math.max(IDLE, Math.min(GRAPH_BLOCKED, tag.getInt("Status")));
        energy = Math.max(0, Math.min(definition.energyCapacity(), tag.getInt("Energy")));
        stateIndex = Math.clamp(tag.getInt("StateIndex"), 0, definition.states().size() - 1);
        activeRecipeIndex = tag.contains("ActiveRecipeIndex") ? tag.getInt("ActiveRecipeIndex") : -1;
        activeRecipe = ResourceLocation.tryParse(tag.getString("ActiveRecipe"));
        for (int i = 0; i < fluids.size(); i++) {
            FluidCell cell = fluids.get(i);
            ResourceLocation id = ResourceLocation.tryParse(tag.getString("Fluid" + i));
            int amount = Math.clamp(tag.getInt("FluidAmount" + i), 0, cell.capacity);
            cell.fluid = id;
            cell.amount = id == null ? 0 : amount;
            cell.normalize();
        }
    }

    @Override public Component getDisplayName() { return Component.literal(definition.displayName()); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) {
        return new ForeverStudioMachineMenu(id, playerInventory, this);
    }

    private final class Automation implements IItemHandler {
        @Override public int getSlots() { return inventory.getSlots(); }
        @Override public ItemStack getStackInSlot(int slot) { return inventory.getStackInSlot(slot); }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return definition.isInputSlot(slot) || definition.isStorageSlot(slot)
                    ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            return definition.isOutputSlot(slot) || definition.isStorageSlot(slot)
                    ? inventory.extractItem(slot, amount, simulate) : ItemStack.EMPTY;
        }
        @Override public int getSlotLimit(int slot) { return inventory.getSlotLimit(slot); }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            return (definition.isInputSlot(slot) || definition.isStorageSlot(slot)) && inventory.isItemValid(slot, stack);
        }
    }

    private final class EnergyAccess implements IEnergyStorage {
        @Override public int receiveEnergy(int maxReceive, boolean simulate) {
            if (!definition.powered()) return 0;
            int received = Math.min(Math.max(0, maxReceive),
                    Math.min(definition.maxReceive(), definition.energyCapacity() - energy));
            if (!simulate && received > 0) { energy += received; setChanged(); }
            return received;
        }
        @Override public int extractEnergy(int maxExtract, boolean simulate) {
            if (!definition.generator()) return 0;
            int extracted = Math.min(Math.max(0, maxExtract), energy);
            if (!simulate && extracted > 0) { energy -= extracted; setChanged(); }
            return extracted;
        }
        @Override public int getEnergyStored() { return energy; }
        @Override public int getMaxEnergyStored() { return definition.powered() ? definition.energyCapacity() : 0; }
        @Override public boolean canExtract() { return definition.generator(); }
        @Override public boolean canReceive() { return definition.powered() && !definition.generator(); }
    }

    private final class FluidAccess implements IFluidHandler {
        @Override public int getTanks() { return fluids.size(); }
        @Override public FluidStack getFluidInTank(int tank) {
            if (tank < 0 || tank >= fluids.size()) return FluidStack.EMPTY;
            FluidCell cell = fluids.get(tank);
            if (cell.empty()) return FluidStack.EMPTY;
            Fluid fluid = BuiltInRegistries.FLUID.get(cell.fluid);
            return fluid == null || fluid == Fluids.EMPTY ? FluidStack.EMPTY : new FluidStack(fluid, cell.amount);
        }
        @Override public int getTankCapacity(int tank) { return fluidCapacity(tank); }
        @Override public boolean isFluidValid(int tank, FluidStack stack) {
            return tank >= 0 && tank < fluids.size() && !stack.isEmpty();
        }
        @Override public int fill(FluidStack resource, FluidAction action) {
            if (resource.isEmpty()) return 0;
            ResourceLocation id = BuiltInRegistries.FLUID.getKey(resource.getFluid());
            if (id == null) return 0;
            int remaining = resource.getAmount();
            int filledTotal = 0;
            for (int i = 0; i < fluids.size() && remaining > 0; i++) {
                FluidCell cell = fluids.get(i);
                if (!cell.empty() && !cell.matches(id)) continue;
                int filled = Math.min(remaining, Math.min(cell.maxFill, cell.space()));
                if (filled <= 0) continue;
                if (action.execute()) fillCell(i, id, filled);
                remaining -= filled;
                filledTotal += filled;
            }
            return filledTotal;
        }
        @Override public FluidStack drain(FluidStack resource, FluidAction action) {
            if (resource.isEmpty()) return FluidStack.EMPTY;
            ResourceLocation id = BuiltInRegistries.FLUID.getKey(resource.getFluid());
            if (id == null) return FluidStack.EMPTY;
            int remaining = resource.getAmount();
            int drainedTotal = 0;
            for (int i = 0; i < fluids.size() && remaining > 0; i++) {
                FluidCell cell = fluids.get(i);
                if (!cell.matches(id)) continue;
                int drained = Math.min(remaining, Math.min(cell.maxDrain, cell.amount));
                if (drained <= 0) continue;
                if (action.execute()) drainCell(i, id, drained);
                remaining -= drained;
                drainedTotal += drained;
            }
            return drainedTotal <= 0 ? FluidStack.EMPTY : new FluidStack(resource.getFluid(), drainedTotal);
        }
        @Override public FluidStack drain(int maxDrain, FluidAction action) {
            if (maxDrain <= 0) return FluidStack.EMPTY;
            for (int i = 0; i < fluids.size(); i++) {
                FluidCell cell = fluids.get(i);
                if (cell.empty()) continue;
                Fluid fluid = BuiltInRegistries.FLUID.get(cell.fluid);
                if (fluid == null || fluid == Fluids.EMPTY) continue;
                int drained = Math.min(maxDrain, Math.min(cell.maxDrain, cell.amount));
                if (action.execute()) drainCell(i, cell.fluid, drained);
                return new FluidStack(fluid, drained);
            }
            return FluidStack.EMPTY;
        }
    }
}

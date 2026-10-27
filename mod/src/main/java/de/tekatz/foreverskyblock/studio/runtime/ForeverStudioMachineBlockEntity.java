package de.tekatz.foreverskyblock.studio.runtime;

import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.HolderLookup;
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
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.neoforge.energy.IEnergyStorage;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

/** Shared server runtime for Studio-compiled standard and powered item processors. */
public final class ForeverStudioMachineBlockEntity extends BlockEntity implements MenuProvider {
    public static final int DATA_COUNT = 5;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2, NO_POWER = 3;

    private final ResourceLocation machineId;
    private final ForeverStudioMachineDefinition definition;
    private final ItemStackHandler inventory;
    private final RecipeManager.CachedCheck<ForeverStudioMachineInput, ForeverStudioMachineRecipe> recipeCheck;
    private final IItemHandler automation;
    private final IEnergyStorage energyAccess;

    private int progress;
    private int duration = 1;
    private int status = IDLE;
    private int energy;
    private @Nullable ResourceLocation activeRecipe;
    private int activeRecipeIndex = -1;

    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            return switch (index) {
                case 0 -> progress;
                case 1 -> Math.max(1, duration);
                case 2 -> status;
                case 3 -> energy;
                case 4 -> definition.powered() ? definition.energyCapacity() : 0;
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public ForeverStudioMachineBlockEntity(BlockPos pos, BlockState state) {
        this(ForeverStudioGeneratedMachines.MACHINE_BLOCK_ENTITY.get(), pos, state, machineIdFromState(state));
    }

    /** Package-private constructor used by tests that supply an explicit machine id. */
    ForeverStudioMachineBlockEntity(BlockPos pos, BlockState state, ResourceLocation machineId) {
        this(ForeverStudioGeneratedMachines.MACHINE_BLOCK_ENTITY.get(), pos, state, machineId);
    }

    /** Package-private constructor used by GameTests with an isolated, valid test BlockEntityType. */
    ForeverStudioMachineBlockEntity(BlockEntityType<?> type, BlockPos pos, BlockState state, ResourceLocation machineId) {
        super(type, pos, state);
        this.machineId = machineId;
        definition = ForeverStudioMachineDefinition.load(machineId);
        inventory = new ItemStackHandler(Math.max(2, definition.slotCount())) {
            @Override public boolean isItemValid(int slot, ItemStack stack) {
                return slot == definition.inputSlot() && (level == null || acceptsInput(level, ForeverStudioMachineBlockEntity.this.machineId, stack));
            }
            @Override protected void onContentsChanged(int slot) { setChanged(); }
        };
        recipeCheck = RecipeManager.createCheck(ForeverStudioGeneratedMachines.MACHINE_RECIPE_TYPE.get());
        automation = new Automation();
        energyAccess = new EnergyAccess();
    }

    private static ResourceLocation machineIdFromState(BlockState state) {
        if (state.getBlock() instanceof ForeverStudioMachineBlock machineBlock)
            return machineBlock.machineId();
        throw new IllegalStateException("Forever Studio machine BlockEntity created for a non-Studio block.");
    }

    public ResourceLocation machineId() { return machineId; }
    public ForeverStudioMachineDefinition definition() { return definition; }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }
    public IItemHandler itemAutomation(@Nullable Direction side) { return automation; }
    public @Nullable IEnergyStorage energyAutomation(@Nullable Direction side) { return definition.powered() ? energyAccess : null; }

    public static boolean acceptsInput(Level level, ResourceLocation machineId, ItemStack stack) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverStudioGeneratedMachines.MACHINE_RECIPE_TYPE.get()).stream()
                .map(RecipeHolder::value)
                .filter(recipe -> recipe.machine().equals(machineId.toString()))
                .anyMatch(recipe -> recipe.acceptsItem(stack));
    }

    private ForeverStudioMachineInput currentInput() {
        return new ForeverStudioMachineInput(machineId, inventory.getStackInSlot(definition.inputSlot()));
    }

    private @Nullable RecipeHolder<ForeverStudioMachineRecipe> findRecipeBook() {
        if (level == null) return null;
        return recipeCheck.getRecipeFor(currentInput(), level).orElse(null);
    }

    public static void tick(Level level, BlockPos pos, BlockState state, ForeverStudioMachineBlockEntity machine) {
        machine.process();
    }

    private void process() {
        RecipeHolder<ForeverStudioMachineRecipe> holder = findRecipeBook();
        if (holder == null) {
            resetProgressIfNeeded();
            status = IDLE;
            return;
        }
        ForeverStudioMachineRecipe book = holder.value();
        ForeverStudioMachineRecipe.Entry entry = book.find(currentInput()).orElse(null);
        if (entry == null) {
            resetProgressIfNeeded();
            status = IDLE;
            return;
        }
        int recipeIndex = book.recipes().indexOf(entry);
        if (!holder.id().equals(activeRecipe) || recipeIndex != activeRecipeIndex) {
            progress = 0;
            activeRecipe = holder.id();
            activeRecipeIndex = recipeIndex;
            setChanged();
        }
        duration = entry.processingTime();
        ItemStack result = entry.result();
        if (!fits(result)) { status = BLOCKED; return; }
        if (definition.powered() && energy < entry.energyPerTick()) { status = NO_POWER; return; }

        status = RUNNING;
        if (definition.powered() && entry.energyPerTick() > 0) energy -= entry.energyPerTick();
        progress++;
        setChanged();
        if (progress >= duration) {
            inventory.extractItem(definition.inputSlot(), entry.inputCount(), false);
            insertOutput(result);
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

    private boolean fits(ItemStack result) {
        ItemStack output = inventory.getStackInSlot(definition.outputSlot());
        return output.isEmpty() || ItemStack.isSameItemSameComponents(output, result)
                && output.getCount() + result.getCount() <= Math.min(64, output.getMaxStackSize());
    }

    private void insertOutput(ItemStack result) {
        ItemStack output = inventory.getStackInSlot(definition.outputSlot());
        if (output.isEmpty()) inventory.setStackInSlot(definition.outputSlot(), result.copy());
        else output.grow(result.getCount());
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        tag.putInt("Duration", duration);
        tag.putInt("Status", status);
        tag.putInt("Energy", energy);
        tag.putInt("ActiveRecipeIndex", activeRecipeIndex);
        if (activeRecipe != null) tag.putString("ActiveRecipe", activeRecipe.toString());
    }

    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        inventory.deserializeNBT(registries, tag.getCompound("Inventory"));
        progress = Math.max(0, tag.getInt("Progress"));
        duration = Math.max(1, tag.getInt("Duration"));
        status = Math.max(IDLE, Math.min(NO_POWER, tag.getInt("Status")));
        energy = Math.max(0, Math.min(definition.energyCapacity(), tag.getInt("Energy")));
        activeRecipeIndex = tag.contains("ActiveRecipeIndex") ? tag.getInt("ActiveRecipeIndex") : -1;
        activeRecipe = ResourceLocation.tryParse(tag.getString("ActiveRecipe"));
    }

    @Override public Component getDisplayName() { return Component.literal(definition.displayName()); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) {
        return new ForeverStudioMachineMenu(id, playerInventory, this);
    }

    private final class Automation implements IItemHandler {
        @Override public int getSlots() { return inventory.getSlots(); }
        @Override public ItemStack getStackInSlot(int slot) { return inventory.getStackInSlot(slot); }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return slot == definition.inputSlot() ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            return slot == definition.outputSlot() ? inventory.extractItem(slot, amount, simulate) : ItemStack.EMPTY;
        }
        @Override public int getSlotLimit(int slot) { return inventory.getSlotLimit(slot); }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            return slot == definition.inputSlot() && inventory.isItemValid(slot, stack);
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
        @Override public int extractEnergy(int maxExtract, boolean simulate) { return 0; }
        @Override public int getEnergyStored() { return energy; }
        @Override public int getMaxEnergyStored() { return definition.powered() ? definition.energyCapacity() : 0; }
        @Override public boolean canExtract() { return false; }
        @Override public boolean canReceive() { return definition.powered(); }
    }
}

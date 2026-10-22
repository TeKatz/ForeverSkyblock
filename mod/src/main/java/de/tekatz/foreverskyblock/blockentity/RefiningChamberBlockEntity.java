package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.menu.RefiningMenu;
import de.tekatz.foreverskyblock.recipe.RefiningInput;
import de.tekatz.foreverskyblock.recipe.RefiningRecipe;
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
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.neoforge.energy.IEnergyStorage;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

public final class RefiningChamberBlockEntity extends BlockEntity implements MenuProvider {
    public static final int PRIMARY = 0, CATALYST = 1, OUTPUT = 2, MACHINE_SLOTS = 3;
    public static final int DATA_COUNT = 5;
    public static final int ENERGY_CAPACITY = 100_000, MAX_RECEIVE = 4_000;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2, NO_POWER = 3;

    private int progress;
    private int duration = RefiningRecipe.DEFAULT_TIME;
    private int status = IDLE;
    private int energy;
    private @Nullable ResourceLocation progressRecipe;
    private final RecipeManager.CachedCheck<RefiningInput, RefiningRecipe> recipeCheck = RecipeManager.createCheck(ForeverSkyblock.REFINING.get());

    private final ItemStackHandler inventory = new ItemStackHandler(MACHINE_SLOTS) {
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            if (level == null) return slot == PRIMARY || slot == CATALYST;
            return slot == PRIMARY ? acceptsPrimary(level, stack) : slot == CATALYST && acceptsCatalyst(level, stack);
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); }
    };
    private final IItemHandler inputAccess = new Automation(true, false);
    private final IItemHandler outputAccess = new Automation(false, true);
    private final IItemHandler combinedAccess = new Automation(true, true);
    private final IEnergyStorage energyAccess = new EnergyAccess();

    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            return switch (index) {
                case 0 -> progress;
                case 1 -> duration;
                case 2 -> status;
                case 3 -> energy;
                case 4 -> ENERGY_CAPACITY;
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public RefiningChamberBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.REFINING_CHAMBER_ENTITY.get(), pos, state); }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }
    public IEnergyStorage energyAutomation(@Nullable Direction side) { return energyAccess; }
    public IItemHandler automation(@Nullable Direction side) { return side == Direction.DOWN ? outputAccess : side == null ? combinedAccess : inputAccess; }

    public static boolean acceptsPrimary(Level level, ItemStack stack) {
        return !stack.isEmpty() && level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.REFINING.get()).stream()
                .anyMatch(holder -> holder.value().acceptsIngredient(stack));
    }
    public static boolean acceptsCatalyst(Level level, ItemStack stack) {
        return !stack.isEmpty() && level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.REFINING.get()).stream()
                .anyMatch(holder -> holder.value().acceptsCatalyst(stack));
    }

    private RefiningInput currentInput() { return new RefiningInput(inventory.getStackInSlot(PRIMARY), inventory.getStackInSlot(CATALYST)); }
    private @Nullable RecipeHolder<RefiningRecipe> findRecipe() {
        if (level == null) return null;
        return recipeCheck.getRecipeFor(currentInput(), level).orElse(null);
    }
    public static void tick(Level level, BlockPos pos, BlockState state, RefiningChamberBlockEntity chamber) { chamber.process(); }

    private void process() {
        RecipeHolder<RefiningRecipe> holder = findRecipe();
        if (holder == null) {
            if (progress != 0 || progressRecipe != null) { progress = 0; progressRecipe = null; setChanged(); }
            status = IDLE;
            return;
        }
        RefiningRecipe recipe = holder.value();
        if (!holder.id().equals(progressRecipe)) { progress = 0; progressRecipe = holder.id(); setChanged(); }
        duration = recipe.processingTime();
        ItemStack result = recipe.result();
        if (!fits(result)) { status = BLOCKED; return; }
        if (energy < recipe.energyPerTick()) { status = NO_POWER; return; }
        status = RUNNING;
        energy -= recipe.energyPerTick();
        progress++;
        setChanged();
        if (progress >= duration) {
            inventory.extractItem(PRIMARY, recipe.ingredientCount(), false);
            inventory.extractItem(CATALYST, recipe.catalystCount(), false);
            insertOutput(result);
            progress = 0;
            progressRecipe = null;
        }
    }

    private boolean fits(ItemStack result) {
        ItemStack out = inventory.getStackInSlot(OUTPUT);
        return out.isEmpty() || ItemStack.isSameItemSameComponents(out, result)
                && out.getCount() + result.getCount() <= Math.min(64, out.getMaxStackSize());
    }
    private void insertOutput(ItemStack result) {
        ItemStack out = inventory.getStackInSlot(OUTPUT);
        if (out.isEmpty()) inventory.setStackInSlot(OUTPUT, result.copy()); else out.grow(result.getCount());
        setChanged();
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        tag.putInt("Energy", energy);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        inventory.deserializeNBT(registries, tag.getCompound("Inventory"));
        progress = Math.max(0, tag.getInt("Progress"));
        energy = Math.max(0, Math.min(ENERGY_CAPACITY, tag.getInt("Energy")));
        progressRecipe = ResourceLocation.tryParse(tag.getString("ProgressRecipe"));
    }

    @Override public Component getDisplayName() { return Component.translatable("block.forever_skyblock.refining_chamber"); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) { return new RefiningMenu(id, playerInventory, this); }

    private final class Automation implements IItemHandler {
        private final boolean insert, extract;
        private Automation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return MACHINE_SLOTS; }
        @Override public ItemStack getStackInSlot(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getStackInSlot(slot) : ItemStack.EMPTY; }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return insert && (slot == PRIMARY || slot == CATALYST) ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            return extract && slot == OUTPUT ? inventory.extractItem(slot, amount, simulate) : ItemStack.EMPTY;
        }
        @Override public int getSlotLimit(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getSlotLimit(slot) : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return insert && inventory.isItemValid(slot, stack); }
    }

    private final class EnergyAccess implements IEnergyStorage {
        @Override public int receiveEnergy(int maxReceive, boolean simulate) {
            int received = Math.min(Math.max(0, maxReceive), Math.min(MAX_RECEIVE, ENERGY_CAPACITY - energy));
            if (!simulate && received > 0) { energy += received; setChanged(); }
            return received;
        }
        @Override public int extractEnergy(int maxExtract, boolean simulate) { return 0; }
        @Override public int getEnergyStored() { return energy; }
        @Override public int getMaxEnergyStored() { return ENERGY_CAPACITY; }
        @Override public boolean canExtract() { return false; }
        @Override public boolean canReceive() { return true; }
    }
}

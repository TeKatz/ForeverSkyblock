package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.menu.WashingMenu;
import de.tekatz.foreverskyblock.recipe.WashingInput;
import de.tekatz.foreverskyblock.recipe.WashingRecipe;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.HolderLookup;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.tags.FluidTags;
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
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

public final class WashingBasinBlockEntity extends BlockEntity implements MenuProvider {
    public static final int INPUT = 0, OUTPUT = 1, SECONDARY_OUTPUT = 2;
    public static final int MACHINE_SLOTS = 3;
    private static final int BONUS_BUFFER = 3, INVENTORY_SLOTS = 4;
    public static final int DATA_COUNT = 4;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2, NO_WATER = 3;

    private int progress;
    private int duration = WashingRecipe.DEFAULT_TIME;
    private int status;
    private ResourceLocation progressRecipe;
    private final RecipeManager.CachedCheck<WashingInput, WashingRecipe> recipeCheck = RecipeManager.createCheck(ForeverSkyblock.WASHING.get());

    private final ItemStackHandler inventory = new ItemStackHandler(INVENTORY_SLOTS) {
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            return slot == INPUT && level != null && acceptsInput(level, stack);
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); }
    };

    private final IItemHandler inputAccess = new Automation(true, false);
    private final IItemHandler outputAccess = new Automation(false, true);
    private final IItemHandler combinedAccess = new Automation(true, true);

    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            return switch (index) {
                case 0 -> progress;
                case 1 -> duration;
                case 2 -> status;
                case 3 -> hasWaterSource() ? 1 : 0;
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public WashingBasinBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.WASHING_BASIN_ENTITY.get(), pos, state); }

    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }

    public static boolean acceptsInput(Level level, ItemStack stack) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.WASHING.get()).stream()
                .anyMatch(holder -> holder.value().accepts(stack));
    }

    public boolean hasWaterSource() {
        if (level == null) return false;
        for (Direction direction : Direction.values()) {
            BlockPos adjacent = worldPosition.relative(direction);
            if (!level.hasChunkAt(adjacent)) continue;
            var fluid = level.getFluidState(adjacent);
            if (fluid.is(FluidTags.WATER) && fluid.isSource()) return true;
        }
        return false;
    }

    public IItemHandler automation(@Nullable Direction side) {
        return side == null ? combinedAccess : side == Direction.DOWN ? outputAccess : inputAccess;
    }

    private WashingInput currentInput() { return new WashingInput(inventory.getStackInSlot(INPUT)); }
    private @Nullable RecipeHolder<WashingRecipe> findRecipe() {
        if (level == null || inventory.getStackInSlot(INPUT).isEmpty()) return null;
        return recipeCheck.getRecipeFor(currentInput(), level).orElse(null);
    }

    public static void tick(Level level, BlockPos pos, BlockState state, WashingBasinBlockEntity basin) { basin.process(); }

    /**
     * ItemStackHandler restores the serialized slot count from NBT. Basins saved before the second output and bonus
     * buffer therefore deserialize as the old two-slot layout. Expand those inventories in-place while preserving
     * every slot that existed in the save. The same guard also makes ticking resilient to any undersized legacy data.
     */
    private void ensureInventorySize() {
        if (inventory.getSlots() == INVENTORY_SLOTS) return;
        int preservedSlots = Math.min(inventory.getSlots(), INVENTORY_SLOTS);
        ItemStack[] preserved = new ItemStack[preservedSlots];
        for (int slot = 0; slot < preservedSlots; slot++) preserved[slot] = inventory.getStackInSlot(slot).copy();
        inventory.setSize(INVENTORY_SLOTS);
        for (int slot = 0; slot < preservedSlots; slot++) inventory.setStackInSlot(slot, preserved[slot]);
    }

    private boolean fits(int slot, ItemStack result) {
        if (result.isEmpty()) return true;
        ItemStack output = inventory.getStackInSlot(slot);
        return output.isEmpty() || ItemStack.isSameItemSameComponents(output, result)
                && output.getCount() + result.getCount() <= Math.min(64, output.getMaxStackSize());
    }

    private void insertOutput(int slot, ItemStack result) {
        if (result.isEmpty()) return;
        ItemStack existing = inventory.getStackInSlot(slot);
        inventory.setStackInSlot(slot, result.copyWithCount(existing.getCount() + result.getCount()));
    }

    private boolean bonusBufferCanAccept(ItemStack result) {
        if (result.isEmpty()) return true;
        ItemStack queued = inventory.getStackInSlot(BONUS_BUFFER);
        return queued.isEmpty() || ItemStack.isSameItemSameComponents(queued, result)
                && queued.getCount() + result.getCount() <= Math.min(64, queued.getMaxStackSize());
    }

    /**
     * The basin deliberately exposes only two output slots. Guaranteed secondary output uses the second slot first;
     * chance-based byproducts wait here and are moved into that same output channel as soon as it becomes available.
     */
    private void flushBonusBuffer() {
        ItemStack queued = inventory.getStackInSlot(BONUS_BUFFER);
        if (queued.isEmpty() || !fits(SECONDARY_OUTPUT, queued)) return;
        insertOutput(SECONDARY_OUTPUT, queued);
        inventory.setStackInSlot(BONUS_BUFFER, ItemStack.EMPTY);
    }

    private void process() {
        ensureInventorySize();
        flushBonusBuffer();
        RecipeHolder<WashingRecipe> holder = findRecipe();
        if (holder == null) {
            if (progress != 0 || progressRecipe != null) { progress = 0; progressRecipe = null; setChanged(); }
            status = IDLE;
            return;
        }
        WashingRecipe recipe = holder.value();
        if (!holder.id().equals(progressRecipe)) { progress = 0; progressRecipe = holder.id(); setChanged(); }
        duration = recipe.processingTime();
        if (!hasWaterSource()) { status = NO_WATER; return; }

        ItemStack primary = recipe.result();
        ItemStack secondary = recipe.secondaryResult();
        if (!fits(OUTPUT, primary) || !fits(SECONDARY_OUTPUT, secondary)
                || recipe.hasBonusResult() && !bonusBufferCanAccept(recipe.bonusResult())) {
            status = BLOCKED;
            return;
        }

        status = RUNNING;
        progress++;
        setChanged();
        if (progress >= duration) {
            inventory.extractItem(INPUT, recipe.ingredientCount(), false);
            insertOutput(OUTPUT, primary);
            insertOutput(SECONDARY_OUTPUT, secondary);
            if (recipe.hasBonusResult() && level != null && level.random.nextInt(100) < recipe.bonusChancePercent())
                insertOutput(BONUS_BUFFER, recipe.bonusResult());
            progress = 0;
            flushBonusBuffer();
        }
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
    }

    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        inventory.deserializeNBT(registries, tag.getCompound("Inventory"));
        ensureInventorySize();
        progress = Math.max(0, Math.min(32766, tag.getInt("Progress")));
        progressRecipe = ResourceLocation.tryParse(tag.getString("ProgressRecipe"));
    }

    @Override public Component getDisplayName() { return Component.translatable("block.forever_skyblock.washing_basin"); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory inventory, Player player) { return new WashingMenu(id, inventory, this); }

    private final class Automation implements IItemHandler {
        private final boolean insert, extract;
        private Automation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return MACHINE_SLOTS; }
        @Override public ItemStack getStackInSlot(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getStackInSlot(slot) : ItemStack.EMPTY; }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return insert && slot == INPUT ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            return extract && (slot == OUTPUT || slot == SECONDARY_OUTPUT) ? inventory.extractItem(slot, amount, simulate) : ItemStack.EMPTY;
        }
        @Override public int getSlotLimit(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getSlotLimit(slot) : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return insert && slot == INPUT && inventory.isItemValid(slot, stack); }
    }
}

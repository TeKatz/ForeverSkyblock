package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
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
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

public final class CondenserBlockEntity extends BlockEntity implements MenuProvider {
    // Original indices stay stable. Module storage is appended for old-save compatibility.
    public static final int INPUT = 0, OUTPUT = 1, SECOND_INPUT = 2, FREEZING_INPUT = 3, FREEZING_OUTPUT = 4;
    public static final int MACHINE_SLOTS = 5, DATA_COUNT = 13;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2;
    public static final int FREEZING_REQUIRED_ITEMS = 32;

    private int progress;
    private int duration = CondenserRecipe.DEFAULT_TIME;
    private int status;
    private int freezingProgress;
    private int freezingStatus;
    private ResourceLocation progressRecipe;
    private final ItemStack[] filters = { ItemStack.EMPTY, ItemStack.EMPTY, ItemStack.EMPTY };
    private final RecipeManager.CachedCheck<CondenserInput, CondenserRecipe> recipeCheck = RecipeManager.createCheck(ForeverSkyblock.CONDENSING.get());
    private boolean visualDirty = true;
    private long lastVisualSync, visualReceivedAt;

    public CondenserBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.CONDENSER_ENTITY.get(), pos, state); }

    public float visualProgress(float partialTick) {
        if (level == null) return progress;
        boolean active = getBlockState().getValue(CondenserBlock.ACTIVE);
        float elapsed = level.isClientSide && active ? Math.max(0, level.getGameTime() - visualReceivedAt) + partialTick : 0;
        return (progress + elapsed) % Math.max(1, duration);
    }
    public int visualDuration() { return duration; }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }

    private void syncVisual() {
        if (level == null || level.isClientSide) return;
        long now = level.getGameTime();
        boolean active = getBlockState().getValue(CondenserBlock.ACTIVE);
        if (visualDirty || active && now - lastVisualSync >= 20) {
            level.sendBlockUpdated(worldPosition, getBlockState(), getBlockState(), 2);
            visualDirty = false;
            lastVisualSync = now;
        }
    }
    @Override public net.minecraft.network.protocol.game.ClientboundBlockEntityDataPacket getUpdatePacket() {
        return net.minecraft.network.protocol.game.ClientboundBlockEntityDataPacket.create(this);
    }
    @Override public CompoundTag getUpdateTag(HolderLookup.Provider registries) { return saveWithoutMetadata(registries); }

    private final ItemStackHandler inventory = new ItemStackHandler(MACHINE_SLOTS) {
        @Override public int getSlotLimit(int slot) { return 64; }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            if (stack.isEmpty() || level == null || !isInput(slot) || !acceptsFilter(slot, stack)) return false;
            if (slot == FREEZING_INPUT) return hasFreezingChamber();
            if (slot == SECOND_INPUT && !hasMixingChamber()) return false;
            return acceptsInput(level, stack, hasMixingChamber());
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); visualDirty = true; }
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
                case 3 -> hasMixingChamber() ? 1 : 0;
                case 4 -> hasFreezingChamber() ? 1 : 0;
                case 5 -> freezingProgress;
                case 6 -> freezingStatus;
                case 7, 8, 9, 10, 11, 12 -> {
                    int filter = (index - 7) / 2;
                    int id = BuiltInRegistries.ITEM.getId(filters[filter].getItem());
                    yield ((index - 7) % 2 == 0 ? id : id >>> 16) & 65535;
                }
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public static boolean isInput(int slot) { return slot == INPUT || slot == SECOND_INPUT || slot == FREEZING_INPUT; }
    public static boolean isOutput(int slot) { return slot == OUTPUT || slot == FREEZING_OUTPUT; }
    /** Compatibility alias retained for the existing condensing recipe/JEI path. */
    public boolean hasModule() { return hasMixingChamber(); }
    public boolean hasMixingChamber() { return hasAdjacent(ForeverSkyblock.MIXING_CHAMBER_MARK_I.get()); }
    public boolean hasFreezingChamber() { return hasAdjacent(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get()); }
    private boolean hasAdjacent(Block block) {
        if (level == null) return false;
        for (Direction direction : Direction.values()) {
            BlockPos adjacent = worldPosition.relative(direction);
            if (level.hasChunkAt(adjacent) && level.getBlockState(adjacent).is(block)) return true;
        }
        return false;
    }

    public static boolean acceptsInput(Level level, ItemStack stack, boolean mixing) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get()).stream()
                .anyMatch(holder -> (!holder.value().requiresModule() || mixing) && holder.value().accepts(stack));
    }
    private static int filterIndex(int slot) {
        return switch (slot) { case INPUT -> 0; case SECOND_INPUT -> 1; case FREEZING_INPUT -> 2; default -> -1; };
    }
    public ItemStack filter(int slot) {
        int index = filterIndex(slot);
        return index >= 0 ? filters[index].copy() : ItemStack.EMPTY;
    }
    public boolean acceptsFilter(int slot, ItemStack stack) {
        ItemStack filter = filter(slot);
        return filter.isEmpty() || ItemStack.isSameItem(filter, stack);
    }
    public boolean setFilter(int slot, ItemStack stack) {
        int index = filterIndex(slot);
        if (index < 0 || slot == SECOND_INPUT && !hasMixingChamber() || slot == FREEZING_INPUT && !hasFreezingChamber()) return false;
        ItemStack current = inventory.getStackInSlot(slot);
        if (!stack.isEmpty()) {
            if (level == null || slot != FREEZING_INPUT && !acceptsInput(level, stack, hasMixingChamber())) return false;
            if (!current.isEmpty() && !ItemStack.isSameItem(current, stack)) return false;
        }
        filters[index] = stack.isEmpty() ? ItemStack.EMPTY : new ItemStack(stack.getItem());
        setChanged();
        return true;
    }

    public IItemHandler automation(@Nullable Direction side) {
        return side == null ? combinedAccess : side == Direction.DOWN ? outputAccess : inputAccess;
    }
    private CondenserInput currentInput(boolean mixing) {
        return new CondenserInput(inventory.getStackInSlot(INPUT), mixing ? inventory.getStackInSlot(SECOND_INPUT) : ItemStack.EMPTY);
    }
    private @Nullable RecipeHolder<CondenserRecipe> findRecipe(boolean mixing) {
        if (level == null || inventory.getStackInSlot(INPUT).isEmpty()) return null;
        return recipeCheck.getRecipeFor(currentInput(mixing), level).orElse(null);
    }

    public static void tick(Level level, BlockPos pos, BlockState state, CondenserBlockEntity machine) { machine.process(); machine.syncVisual(); }
    private void process() {
        boolean condensing = processCondensing(hasMixingChamber());
        boolean freezing = processFreezing(hasFreezingChamber());
        setActive(condensing || freezing);
    }
    private boolean processCondensing(boolean mixing) {
        RecipeHolder<CondenserRecipe> holder = findRecipe(mixing);
        if (holder == null) {
            if (progress != 0 || progressRecipe != null) { progress = 0; progressRecipe = null; setChanged(); }
            status = IDLE;
            return false;
        }
        CondenserRecipe recipe = holder.value();
        if (!holder.id().equals(progressRecipe)) { progress = 0; progressRecipe = holder.id(); setChanged(); }
        duration = recipe.processingTime();
        ItemStack result = recipe.result();
        ItemStack output = inventory.getStackInSlot(OUTPUT);
        boolean fits = result.getCount() <= Math.min(64, result.getMaxStackSize()) && (output.isEmpty()
                || ItemStack.isSameItemSameComponents(output, result)
                && output.getCount() + result.getCount() <= Math.min(64, output.getMaxStackSize()));
        if (!fits) { status = BLOCKED; return false; }
        status = RUNNING;
        progress++;
        setChanged();
        if (progress >= duration) {
            CondenserInput input = currentInput(mixing);
            inventory.extractItem(INPUT, recipe.primaryConsumption(input), false);
            if (recipe.requiresModule()) inventory.extractItem(SECOND_INPUT, recipe.secondaryConsumption(input), false);
            inventory.setStackInSlot(OUTPUT, result.copyWithCount(output.getCount() + result.getCount()));
            progress = 0;
        }
        return true;
    }
    private boolean processFreezing(boolean connected) {
        if (!connected) { freezingStatus = IDLE; return false; }
        ItemStack input = inventory.getStackInSlot(FREEZING_INPUT);
        if (input.isEmpty()) { freezingStatus = IDLE; return false; }
        boolean completes = freezingProgress + 1 >= FREEZING_REQUIRED_ITEMS;
        ItemStack output = inventory.getStackInSlot(FREEZING_OUTPUT);
        boolean fitsIce = output.isEmpty() || output.is(Items.ICE) && output.getCount() < Math.min(64, output.getMaxStackSize());
        if (completes && !fitsIce) { freezingStatus = BLOCKED; return false; }
        freezingStatus = RUNNING;
        inventory.extractItem(FREEZING_INPUT, 1, false);
        freezingProgress++;
        if (freezingProgress >= FREEZING_REQUIRED_ITEMS) {
            inventory.setStackInSlot(FREEZING_OUTPUT, output.isEmpty() ? new ItemStack(Items.ICE) : output.copyWithCount(output.getCount() + 1));
            freezingProgress = 0;
        }
        setChanged();
        return true;
    }
    private void setActive(boolean active) {
        if (level != null && getBlockState().getValue(CondenserBlock.ACTIVE) != active) {
            visualDirty = true;
            level.setBlock(worldPosition, getBlockState().setValue(CondenserBlock.ACTIVE, active), 3);
        }
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        tag.putInt("FreezingProgress", freezingProgress);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
        for (int i = 0; i < filters.length; i++) if (!filters[i].isEmpty())
            tag.putString("InputFilter" + i, BuiltInRegistries.ITEM.getKey(filters[i].getItem()).toString());
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        visualReceivedAt = level == null ? 0 : level.getGameTime();
        CompoundTag saved = tag.getCompound("Inventory").copy();
        saved.putInt("Size", MACHINE_SLOTS);
        inventory.deserializeNBT(registries, saved);
        progress = Math.max(0, Math.min(32766, tag.getInt("Progress")));
        freezingProgress = Math.max(0, Math.min(FREEZING_REQUIRED_ITEMS - 1, tag.getInt("FreezingProgress")));
        progressRecipe = ResourceLocation.tryParse(tag.getString("ProgressRecipe"));
        for (int i = 0; i < filters.length; i++) {
            ResourceLocation id = ResourceLocation.tryParse(tag.getString("InputFilter" + i));
            filters[i] = id == null ? ItemStack.EMPTY : BuiltInRegistries.ITEM.getOptional(id).map(ItemStack::new).orElse(ItemStack.EMPTY);
        }
    }

    @Override public Component getDisplayName() { return Component.translatable("block.forever_skyblock.condenser_mark_i"); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) { return new CondenserMenu(id, playerInventory, this); }

    private final class Automation implements IItemHandler {
        private final boolean insert, extract;
        private Automation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return MACHINE_SLOTS; }
        @Override public ItemStack getStackInSlot(int slot) {
            if (slot < 0 || slot >= MACHINE_SLOTS) return ItemStack.EMPTY;
            if (slot == SECOND_INPUT && !hasMixingChamber()) return ItemStack.EMPTY;
            if ((slot == FREEZING_INPUT || slot == FREEZING_OUTPUT) && !hasFreezingChamber()) return ItemStack.EMPTY;
            return inventory.getStackInSlot(slot);
        }
        @Override public int getSlotLimit(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getSlotLimit(slot) : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return insert && slot >= 0 && slot < MACHINE_SLOTS && inventory.isItemValid(slot, stack); }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return isItemValid(slot, stack) ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            if (!extract) return ItemStack.EMPTY;
            if (slot == OUTPUT) return inventory.extractItem(slot, amount, simulate);
            if (slot == FREEZING_OUTPUT && hasFreezingChamber()) return inventory.extractItem(slot, amount, simulate);
            return ItemStack.EMPTY;
        }
    }
}

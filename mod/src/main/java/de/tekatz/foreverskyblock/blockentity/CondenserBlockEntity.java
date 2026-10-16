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
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

public final class CondenserBlockEntity extends BlockEntity implements MenuProvider {
    // Keep the original input/output indices stable when loading older two-slot inventories.
    public static final int INPUT = 0, OUTPUT = 1, SECOND_INPUT = 2, DATA_COUNT = 8;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2;
    private int progress;
    private int duration = CondenserRecipe.DEFAULT_TIME;
    private int status;
    private ResourceLocation progressRecipe;
    private final ItemStack[] filters = { ItemStack.EMPTY, ItemStack.EMPTY };
    private final RecipeManager.CachedCheck<CondenserInput, CondenserRecipe> recipeCheck = RecipeManager.createCheck(ForeverSkyblock.CONDENSING.get());
    private boolean visualDirty = true;
    private long lastVisualSync, visualReceivedAt;
    public float visualProgress(float partialTick) {
        if (level == null) return progress;
        boolean active = getBlockState().getValue(CondenserBlock.ACTIVE);
        float elapsed = level.isClientSide && active ? Math.max(0, level.getGameTime() - visualReceivedAt) + partialTick : 0;
        return (progress + elapsed) % Math.max(1, duration);
    }
    private void syncVisual() {
        if (level == null || level.isClientSide) return;
        long now = level.getGameTime();
        boolean active = getBlockState().getValue(CondenserBlock.ACTIVE);
        if (visualDirty || active && now - lastVisualSync >= 20) {
            level.sendBlockUpdated(worldPosition, getBlockState(), getBlockState(), 2);
            visualDirty = false; lastVisualSync = now;
        }
    }
    @Override public net.minecraft.network.protocol.game.ClientboundBlockEntityDataPacket getUpdatePacket() {
        return net.minecraft.network.protocol.game.ClientboundBlockEntityDataPacket.create(this);
    }
    @Override public CompoundTag getUpdateTag(HolderLookup.Provider registries) { return saveWithoutMetadata(registries); }
    private final ItemStackHandler inventory = new ItemStackHandler(3) {
        @Override public int getSlotLimit(int slot) { return 64; }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            return isInput(slot) && (slot != SECOND_INPUT || hasModule()) && level != null
                    && acceptsInput(level, stack, hasModule()) && acceptsFilter(slot, stack);
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); visualDirty = true; }
    };
    private final IItemHandler inputAccess = new Automation(true, false);
    private final IItemHandler outputAccess = new Automation(false, true);
    private final IItemHandler combinedAccess = new Automation(true, true);
    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            return switch (index) {
                case 0 -> progress; case 1 -> duration; case 2 -> status; case 3 -> hasModule() ? 1 : 0;
                case 4, 5, 6, 7 -> {
                    int id = BuiltInRegistries.ITEM.getId(filters[(index - 4) / 2].getItem());
                    yield (index % 2 == 0 ? id : id >>> 16) & 65535;
                }
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };
    public CondenserBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.CONDENSER_ENTITY.get(), pos, state); }
    public int visualDuration() { return duration; }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }
    public static boolean isInput(int slot) { return slot == INPUT || slot == SECOND_INPUT; }
    public boolean hasModule() {
        if (level == null) return false;
        for (Direction direction : Direction.values()) {
            BlockPos adjacent = worldPosition.relative(direction);
            if (level.hasChunkAt(adjacent) && level.getBlockState(adjacent).is(ForeverSkyblock.MIXING_CHAMBER_MARK_I.get())) return true;
        }
        return false;
    }
    public static boolean acceptsInput(Level level, ItemStack stack, boolean module) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get()).stream()
                .anyMatch(holder -> (!holder.value().requiresModule() || module) && holder.value().accepts(stack));
    }
    public ItemStack filter(int slot) { return isInput(slot) ? filters[slot == INPUT ? 0 : 1].copy() : ItemStack.EMPTY; }
    public boolean acceptsFilter(int slot, ItemStack stack) {
        ItemStack filter = filter(slot);
        return filter.isEmpty() || ItemStack.isSameItem(filter, stack);
    }
    public boolean setFilter(int slot, ItemStack stack) {
        if (!isInput(slot) || slot == SECOND_INPUT && !hasModule()) return false;
        ItemStack current = inventory.getStackInSlot(slot);
        if (!stack.isEmpty() && (level == null || !acceptsInput(level, stack, hasModule())
                || !current.isEmpty() && !ItemStack.isSameItem(current, stack))) return false;
        // Filters deliberately match item identity, independent of count and item components.
        filters[slot == INPUT ? 0 : 1] = stack.isEmpty() ? ItemStack.EMPTY : new ItemStack(stack.getItem());
        setChanged();
        return true;
    }
    public IItemHandler automation(@Nullable Direction side) { return side == null ? combinedAccess : side == Direction.DOWN ? outputAccess : inputAccess; }
    private @Nullable RecipeHolder<CondenserRecipe> findRecipe() {
        if (level == null || inventory.getStackInSlot(INPUT).isEmpty()) return null;
        return recipeCheck.getRecipeFor(new CondenserInput(inventory.getStackInSlot(INPUT),
                hasModule() ? inventory.getStackInSlot(SECOND_INPUT) : ItemStack.EMPTY), level).orElse(null);
    }
    public static void tick(Level level, BlockPos pos, BlockState state, CondenserBlockEntity machine) { machine.process(); machine.syncVisual(); }
    private void process() {
        RecipeHolder<CondenserRecipe> holder = findRecipe();
        if (holder == null) {
            if (progress != 0 || progressRecipe != null) { progress = 0; progressRecipe = null; setChanged(); }
            status = IDLE;
            setActive(false);
            return;
        }
        CondenserRecipe recipe = holder.value();
        if (!holder.id().equals(progressRecipe)) { progress = 0; progressRecipe = holder.id(); setChanged(); }
        duration = recipe.processingTime();
        ItemStack result = recipe.result();
        ItemStack output = inventory.getStackInSlot(OUTPUT);
        boolean fits = result.getCount() <= Math.min(64, result.getMaxStackSize()) && (output.isEmpty()
                || ItemStack.isSameItemSameComponents(output, result) && output.getCount() + result.getCount() <= Math.min(64, output.getMaxStackSize()));
        if (!fits) { status = BLOCKED; setActive(false); return; }
        status = RUNNING;
        setActive(true);
        progress++;
        setChanged();
        if (progress >= duration) {
            inventory.extractItem(INPUT, 1, false);
            if (recipe.requiresModule()) inventory.extractItem(SECOND_INPUT, 1, false);
            inventory.setStackInSlot(OUTPUT, result.copyWithCount(output.getCount() + result.getCount()));
            progress = 0;
        }
    }
    private void setActive(boolean active) {
        if (level != null && getBlockState().getValue(CondenserBlock.ACTIVE) != active)
            { visualDirty = true; level.setBlock(worldPosition, getBlockState().setValue(CondenserBlock.ACTIVE, active), 3); }
    }
    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
        for (int i = 0; i < filters.length; i++) if (!filters[i].isEmpty())
            tag.putString("InputFilter" + i, BuiltInRegistries.ITEM.getKey(filters[i].getItem()).toString());
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        visualReceivedAt = level == null ? 0 : level.getGameTime();
        CompoundTag saved = tag.getCompound("Inventory").copy();
        saved.putInt("Size", 3);
        inventory.deserializeNBT(registries, saved);
        progress = Math.max(0, Math.min(32766, tag.getInt("Progress")));
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
        @Override public int getSlots() { return 3; }
        @Override public ItemStack getStackInSlot(int slot) { return slot == SECOND_INPUT && !hasModule() ? ItemStack.EMPTY : inventory.getStackInSlot(slot); }
        @Override public int getSlotLimit(int slot) { return inventory.getSlotLimit(slot); }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return insert && inventory.isItemValid(slot, stack); }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return isItemValid(slot, stack) ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            return extract && slot == OUTPUT ? inventory.extractItem(slot, amount, simulate) : ItemStack.EMPTY;
        }
    }
}

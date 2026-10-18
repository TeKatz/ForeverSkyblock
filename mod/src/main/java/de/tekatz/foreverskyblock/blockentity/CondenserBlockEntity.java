package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import de.tekatz.foreverskyblock.recipe.GardenInput;
import de.tekatz.foreverskyblock.recipe.GardenRecipe;
import de.tekatz.foreverskyblock.recipe.CondenserJob;
import java.util.Comparator;
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
    public static final int GARDEN_SPECIMEN = 5, GARDEN_OUTPUT_PRIMARY = 6, GARDEN_OUTPUT_SECONDARY = 7;
    public static final int MACHINE_SLOTS = 8, DATA_COUNT = 18;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2;
    public static final int FREEZING_REQUIRED_ITEMS = 32;

    private int progress;
    private int duration = CondenserRecipe.DEFAULT_TIME;
    private int status;
    private int freezingProgress;
    private int freezingStatus;
    private int gardenProgress;
    private int gardenDuration = GardenRecipe.DEFAULT_TIME;
    private int gardenStatus;
    private ResourceLocation progressRecipe;
    private ResourceLocation gardenRecipe;
    private final ItemStack[] filters = { ItemStack.EMPTY, ItemStack.EMPTY, ItemStack.EMPTY };
    private boolean visualDirty = true;
    private long lastVisualSync, visualReceivedAt = -1;
    private int lastVisualState = -1;
    private CondenserJob visualJob = CondenserJob.IDLE;
    private ItemStack visualResult = ItemStack.EMPTY;

    public CondenserBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.CONDENSER_ENTITY.get(), pos, state); }

    public int machineTier() { return getBlockState().is(ForeverSkyblock.CONDENSER_MARK_II.get()) ? 2 : 1; }

    public float visualProgress(float partialTick) {
        if (level == null) return hasGardenChamber() ? gardenProgress : progress;
        if (visualReceivedAt < 0) visualReceivedAt = level.getGameTime();
        boolean active = visualStatus() == RUNNING;
        int base = hasGardenChamber() ? gardenProgress : progress;
        int max = hasGardenChamber() ? gardenDuration : duration;
        float elapsed = level.isClientSide && active ? Math.max(0, level.getGameTime() - visualReceivedAt) + partialTick : 0;
        // Extrapolate at most one sync interval. Never fabricate another cycle on stale data.
        return Math.clamp(base + Math.min(20F, elapsed), 0F, Math.max(1, max));
    }
    public int visualDuration() { return hasGardenChamber() ? gardenDuration : duration; }
    public int visualStatus() { return hasGardenChamber() ? gardenStatus : status; }
    public int visualFreezingStatus() { return hasFreezingChamber() ? freezingStatus : IDLE; }
    public int visualFreezingProgress() { return freezingProgress; }
    public CondenserJob visualJob() { return visualJob; }
    public ItemStack visualResult() { return visualResult; }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }

    private void syncVisual() {
        if (level == null || level.isClientSide) return;
        long now = level.getGameTime();
        boolean active = getBlockState().getValue(CondenserBlock.ACTIVE);
        int state = status | freezingStatus << 2 | gardenStatus << 4
                | (hasGardenChamber() ? 1 : 0) << 6 | (hasMixingChamber() ? 1 : 0) << 7
                | (hasFreezingChamber() ? 1 : 0) << 8;
        if (visualDirty || state != lastVisualState || active && now - lastVisualSync >= 20) {
            level.sendBlockUpdated(worldPosition, getBlockState(), getBlockState(), 2);
            visualDirty = false;
            lastVisualSync = now;
            lastVisualState = state;
        }
    }
    @Override public net.minecraft.network.protocol.game.ClientboundBlockEntityDataPacket getUpdatePacket() {
        return net.minecraft.network.protocol.game.ClientboundBlockEntityDataPacket.create(this);
    }
    @Override public CompoundTag getUpdateTag(HolderLookup.Provider registries) {
        CompoundTag tag = saveWithoutMetadata(registries);
        // These fields are sent to viewers only; they do not change saved production state or menu protocol.
        CompoundTag visual = new CompoundTag();
        visual.putInt("Duration", duration);
        visual.putInt("GardenDuration", gardenDuration);
        visual.putInt("Status", status);
        visual.putInt("GardenStatus", gardenStatus);
        visual.putInt("FreezingStatus", freezingStatus);
        var holder = hasGardenChamber() ? null : findRecipe(hasMixingChamber());
        visual.putInt("Job", CondenserJob.of(holder == null ? null : holder.value()).ordinal());
        if (holder != null) visual.put("Result", holder.value().result().save(registries));
        tag.put("Visual", visual);
        return tag;
    }

    private final ItemStackHandler inventory = new ItemStackHandler(MACHINE_SLOTS) {
        @Override public int getSlotLimit(int slot) { return slot == GARDEN_SPECIMEN ? 1 : 64; }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            if (stack.isEmpty() || level == null) return false;
            if (slot == GARDEN_SPECIMEN) return hasGardenChamber() && acceptsGardenInput(level, stack);
            if (hasGardenChamber()) return false;
            if (!isInput(slot) || !acceptsFilter(slot, stack)) return false;
            if (slot == FREEZING_INPUT) return hasFreezingChamber();
            if (slot == SECOND_INPUT && !hasMixingChamber()) return false;
            return acceptsInput(level, stack, hasMixingChamber(), machineTier());
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
                case 13 -> machineTier();
                case 14 -> hasGardenChamber() ? 1 : 0;
                case 15 -> gardenProgress;
                case 16 -> gardenDuration;
                case 17 -> gardenStatus;
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public static boolean isInput(int slot) { return slot == INPUT || slot == SECOND_INPUT || slot == FREEZING_INPUT; }
    public static boolean isOutput(int slot) {
        return slot == OUTPUT || slot == FREEZING_OUTPUT || slot == GARDEN_OUTPUT_PRIMARY || slot == GARDEN_OUTPUT_SECONDARY;
    }
    /** Compatibility alias retained for the existing condensing recipe/JEI path. */
    public boolean hasModule() { return hasMixingChamber(); }
    public boolean hasGardenChamber() { return machineTier() >= 2 && hasAdjacent(ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get()); }
    public boolean hasMixingChamber() { return !hasGardenChamber() && hasAdjacent(ForeverSkyblock.MIXING_CHAMBER_MARK_I.get()); }
    public boolean hasFreezingChamber() { return !hasGardenChamber() && hasAdjacent(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get()); }
    private boolean hasAdjacent(Block block) {
        if (level == null) return false;
        for (Direction direction : Direction.values()) {
            BlockPos adjacent = worldPosition.relative(direction);
            if (level.hasChunkAt(adjacent) && level.getBlockState(adjacent).is(block)) return true;
        }
        return false;
    }

    public static boolean acceptsInput(Level level, ItemStack stack, boolean mixing) {
        return acceptsInput(level, stack, mixing, 1);
    }
    public static boolean acceptsInput(Level level, ItemStack stack, boolean mixing, int tier) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get()).stream()
                .anyMatch(holder -> holder.value().availableToTier(tier)
                        && (!holder.value().requiresModule() || mixing) && holder.value().accepts(stack));
    }
    public static boolean acceptsGardenInput(Level level, ItemStack stack) {
        if (stack.isEmpty()) return false;
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.GARDEN.get()).stream()
                .anyMatch(holder -> holder.value().accepts(stack));
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
        if (hasGardenChamber() || index < 0 || slot == SECOND_INPUT && !hasMixingChamber() || slot == FREEZING_INPUT && !hasFreezingChamber()) return false;
        ItemStack current = inventory.getStackInSlot(slot);
        if (!stack.isEmpty()) {
            if (level == null || slot != FREEZING_INPUT && !acceptsInput(level, stack, hasMixingChamber(), machineTier())) return false;
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
    public static @Nullable RecipeHolder<CondenserRecipe> findMatchingRecipe(Level level, CondenserInput input, int tier) {
        if (input.first().isEmpty()) return null;
        Comparator<RecipeHolder<CondenserRecipe>> order = Comparator
                .comparingInt((RecipeHolder<CondenserRecipe> holder) -> holder.value().requiresModule() ? 1 : 0).reversed()
                .thenComparing(Comparator.comparingInt((RecipeHolder<CondenserRecipe> holder) -> holder.value().minimumTier()).reversed())
                .thenComparing(holder -> holder.id().toString());
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get()).stream()
                .filter(holder -> holder.value().availableToTier(tier) && holder.value().matches(input, level))
                .sorted(order)
                .findFirst().orElse(null);
    }
    private @Nullable RecipeHolder<CondenserRecipe> findRecipe(boolean mixing) {
        if (level == null) return null;
        return findMatchingRecipe(level, currentInput(mixing), machineTier());
    }
    private @Nullable RecipeHolder<GardenRecipe> findGardenRecipe() {
        if (level == null) return null;
        ItemStack specimen = inventory.getStackInSlot(GARDEN_SPECIMEN);
        if (specimen.isEmpty()) return null;
        GardenInput input = new GardenInput(specimen);
        return level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.GARDEN.get()).stream()
                .filter(holder -> holder.value().matches(input, level))
                .sorted(Comparator.comparing(holder -> holder.id().toString()))
                .findFirst().orElse(null);
    }

    public static void tick(Level level, BlockPos pos, BlockState state, CondenserBlockEntity machine) { machine.process(); machine.syncVisual(); }
    private void process() {
        if (hasGardenChamber()) {
            boolean garden = processGarden();
            setActive(garden);
            return;
        }
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
        boolean fits = canFit(output, result);
        if (!fits) { status = BLOCKED; return false; }
        status = RUNNING;
        progress++;
        setChanged();
        if (progress >= duration) {
            CondenserInput input = currentInput(mixing);
            inventory.extractItem(INPUT, recipe.primaryConsumption(input), false);
            if (recipe.requiresModule()) inventory.extractItem(SECOND_INPUT, recipe.secondaryConsumption(input), false);
            inventory.setStackInSlot(OUTPUT, merge(output, result));
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
    private boolean processGarden() {
        RecipeHolder<GardenRecipe> holder = findGardenRecipe();
        if (holder == null) {
            if (gardenProgress != 0 || gardenRecipe != null) { gardenProgress = 0; gardenRecipe = null; setChanged(); }
            gardenStatus = IDLE;
            return false;
        }
        GardenRecipe recipe = holder.value();
        if (!holder.id().equals(gardenRecipe)) { gardenProgress = 0; gardenRecipe = holder.id(); setChanged(); }
        gardenDuration = recipe.processingTime();
        ItemStack primary = recipe.primaryResult();
        ItemStack secondary = recipe.secondaryResult();
        ItemStack primarySlot = inventory.getStackInSlot(GARDEN_OUTPUT_PRIMARY);
        ItemStack secondarySlot = inventory.getStackInSlot(GARDEN_OUTPUT_SECONDARY);
        if (!canFit(primarySlot, primary) || !canFit(secondarySlot, secondary)) {
            gardenStatus = BLOCKED;
            return false;
        }
        gardenStatus = RUNNING;
        gardenProgress++;
        setChanged();
        if (gardenProgress >= gardenDuration) {
            inventory.setStackInSlot(GARDEN_OUTPUT_PRIMARY, merge(primarySlot, primary));
            inventory.setStackInSlot(GARDEN_OUTPUT_SECONDARY, merge(secondarySlot, secondary));
            gardenProgress = 0;
        }
        return true;
    }
    private static boolean canFit(ItemStack output, ItemStack result) {
        return result.getCount() <= Math.min(64, result.getMaxStackSize()) && (output.isEmpty()
                || ItemStack.isSameItemSameComponents(output, result)
                && output.getCount() + result.getCount() <= Math.min(64, output.getMaxStackSize()));
    }
    private static ItemStack merge(ItemStack output, ItemStack result) {
        return output.isEmpty() ? result.copy() : output.copyWithCount(output.getCount() + result.getCount());
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
        tag.putInt("GardenProgress", gardenProgress);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
        if (gardenRecipe != null) tag.putString("GardenRecipe", gardenRecipe.toString());
        for (int i = 0; i < filters.length; i++) if (!filters[i].isEmpty())
            tag.putString("InputFilter" + i, BuiltInRegistries.ITEM.getKey(filters[i].getItem()).toString());
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        visualReceivedAt = level == null ? -1 : level.getGameTime();
        if (tag.contains("Visual")) {
            CompoundTag visual = tag.getCompound("Visual");
            duration = Math.clamp(visual.getInt("Duration"), 1, 32767);
            gardenDuration = Math.clamp(visual.getInt("GardenDuration"), 1, 32767);
            status = visual.getInt("Status");
            gardenStatus = visual.getInt("GardenStatus");
            freezingStatus = visual.getInt("FreezingStatus");
            visualJob = CondenserJob.fromId(visual.getInt("Job"));
            visualResult = ItemStack.parseOptional(registries, visual.getCompound("Result"));
        }
        CompoundTag saved = tag.getCompound("Inventory").copy();
        saved.putInt("Size", MACHINE_SLOTS);
        inventory.deserializeNBT(registries, saved);
        progress = Math.max(0, Math.min(32766, tag.getInt("Progress")));
        freezingProgress = Math.max(0, Math.min(FREEZING_REQUIRED_ITEMS - 1, tag.getInt("FreezingProgress")));
        gardenProgress = Math.max(0, Math.min(32766, tag.getInt("GardenProgress")));
        progressRecipe = ResourceLocation.tryParse(tag.getString("ProgressRecipe"));
        gardenRecipe = ResourceLocation.tryParse(tag.getString("GardenRecipe"));
        for (int i = 0; i < filters.length; i++) {
            ResourceLocation id = ResourceLocation.tryParse(tag.getString("InputFilter" + i));
            filters[i] = id == null ? ItemStack.EMPTY : BuiltInRegistries.ITEM.getOptional(id).map(ItemStack::new).orElse(ItemStack.EMPTY);
        }
    }

    @Override public Component getDisplayName() {
        return Component.translatable(machineTier() >= 2 ? "block.forever_skyblock.condenser_mark_ii" : "block.forever_skyblock.condenser_mark_i");
    }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) { return new CondenserMenu(id, playerInventory, this); }

    private final class Automation implements IItemHandler {
        private final boolean insert, extract;
        private Automation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return MACHINE_SLOTS; }
        @Override public ItemStack getStackInSlot(int slot) {
            if (slot < 0 || slot >= MACHINE_SLOTS) return ItemStack.EMPTY;
            if (hasGardenChamber()) {
                if (slot != GARDEN_SPECIMEN && slot != GARDEN_OUTPUT_PRIMARY) return ItemStack.EMPTY;
            } else {
                if (slot == GARDEN_SPECIMEN || slot == GARDEN_OUTPUT_PRIMARY || slot == GARDEN_OUTPUT_SECONDARY) return ItemStack.EMPTY;
                if (slot == SECOND_INPUT && !hasMixingChamber()) return ItemStack.EMPTY;
                if ((slot == FREEZING_INPUT || slot == FREEZING_OUTPUT) && !hasFreezingChamber()) return ItemStack.EMPTY;
            }
            return inventory.getStackInSlot(slot);
        }
        @Override public int getSlotLimit(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getSlotLimit(slot) : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return insert && slot >= 0 && slot < MACHINE_SLOTS && inventory.isItemValid(slot, stack); }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return isItemValid(slot, stack) ? inventory.insertItem(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            if (!extract) return ItemStack.EMPTY;
            if (hasGardenChamber()) {
                // Secondary harvest is exposed exclusively through the adjacent Garden Chamber.
                if (slot == GARDEN_OUTPUT_PRIMARY)
                    return inventory.extractItem(slot, amount, simulate);
                return ItemStack.EMPTY;
            }
            if (slot == OUTPUT) return inventory.extractItem(slot, amount, simulate);
            if (slot == FREEZING_OUTPUT && hasFreezingChamber()) return inventory.extractItem(slot, amount, simulate);
            return ItemStack.EMPTY;
        }
    }
}

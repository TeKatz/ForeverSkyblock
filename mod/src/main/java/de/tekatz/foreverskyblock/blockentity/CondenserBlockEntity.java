package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import de.tekatz.foreverskyblock.recipe.CondenserJob;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import de.tekatz.foreverskyblock.recipe.GardenInput;
import de.tekatz.foreverskyblock.recipe.GardenRecipe;
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
import net.minecraft.world.item.Item;
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
    // Original indices stay stable. Upgrade storage is appended for old-save compatibility.
    public static final int INPUT = 0, OUTPUT = 1, SECOND_INPUT = 2, FREEZING_INPUT = 3, FREEZING_OUTPUT = 4;
    public static final int GARDEN_SPECIMEN = 5, GARDEN_OUTPUT_PRIMARY = 6, GARDEN_OUTPUT_SECONDARY = 7;
    public static final int UPGRADE_1 = 8, UPGRADE_2 = 9, UPGRADE_3 = 10;
    public static final int BASE_MACHINE_SLOTS = 8, MACHINE_SLOTS = 11, DATA_COUNT = 27;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2;
    public static final int FREEZING_REQUIRED_ITEMS = 32;
    public static final int UPGRADE_CAPACITY = 3;
    public static final int POLICY_BLOCK = 0, POLICY_VOID_EXCESS = 1, POLICY_VOID_ALL = 2;

    private static final int INPUT_BUFFER_LANES = 3;
    private static final int OUTPUT_BUFFER_LANES = 4;
    private static final int BUFFER_SEGMENTS_PER_LANE = 2;

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
    private final int[] outputPolicies = { POLICY_BLOCK, POLICY_BLOCK, POLICY_BLOCK, POLICY_BLOCK };
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
        return Math.clamp(base + Math.min(20F, elapsed), 0F, Math.max(1, max));
    }
    public int visualDuration() { return hasGardenChamber() ? gardenDuration : duration; }
    public int visualStatus() { return hasGardenChamber() ? gardenStatus : status; }
    public int visualFreezingStatus() { return hasFreezingChamber() ? freezingStatus : IDLE; }
    public int visualFreezingProgress() { return freezingProgress; }
    public CondenserJob visualJob() { return visualJob; }
    public ItemStack visualResult() { return visualResult; }
    public ItemStackHandler inventory() { return inventory; }
    public ItemStackHandler inputBuffers() { return inputBuffers; }
    public ItemStackHandler outputBuffers() { return outputBuffers; }
    public ContainerData menuData() { return data; }

    private void syncVisual() {
        if (level == null || level.isClientSide) return;
        long now = level.getGameTime();
        boolean active = getBlockState().getValue(CondenserBlock.ACTIVE);
        int state = status | freezingStatus << 2 | gardenStatus << 4
                | (hasGardenChamber() ? 1 : 0) << 6 | (hasMixingChamber() ? 1 : 0) << 7
                | (hasFreezingChamber() ? 1 : 0) << 8 | upgradeCapacityUsed() << 9;
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
        @Override public int getSlotLimit(int slot) {
            if (slot == GARDEN_SPECIMEN || isUpgradeSlot(slot)) return 1;
            return 64;
        }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            if (stack.isEmpty() || level == null) return false;
            if (isUpgradeSlot(slot)) return canInstallUpgrade(slot, stack);
            if (slot == GARDEN_SPECIMEN) return hasGardenChamber() && acceptsGardenInput(level, stack);
            if (hasGardenChamber()) return false;
            if (!isInput(slot) || !acceptsFilter(slot, stack)) return false;
            if (slot == FREEZING_INPUT) return hasFreezingChamber();
            if (slot == SECOND_INPUT && !hasMixingChamber()) return false;
            return acceptsInput(level, stack, hasMixingChamber(), machineTier());
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); visualDirty = true; }
    };

    // Two 64-item hidden segments per lane. Removing a Buffer Card never destroys stored items: existing
    // segments continue draining, but new automation/production can only write into currently unlocked segments.
    private final ItemStackHandler inputBuffers = new ItemStackHandler(INPUT_BUFFER_LANES * BUFFER_SEGMENTS_PER_LANE) {
        @Override protected void onContentsChanged(int slot) { setChanged(); }
    };
    private final ItemStackHandler outputBuffers = new ItemStackHandler(OUTPUT_BUFFER_LANES * BUFFER_SEGMENTS_PER_LANE) {
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
                case 18 -> speedUpgradeCount();
                case 19 -> gardenUpgradeCount();
                case 20 -> bufferUpgradeCount();
                case 21 -> batchUpgradeCount();
                case 22 -> upgradeCapacityUsed();
                case 23 -> outputPolicy(OUTPUT);
                case 24 -> outputPolicy(FREEZING_OUTPUT);
                case 25 -> outputPolicy(GARDEN_OUTPUT_PRIMARY);
                case 26 -> outputPolicy(GARDEN_OUTPUT_SECONDARY);
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
    public static boolean isUpgradeSlot(int slot) { return slot >= UPGRADE_1 && slot <= UPGRADE_3; }
    public static boolean isUpgradeCard(ItemStack stack) { return upgradeCost(stack) > 0; }
    public static int upgradeCost(ItemStack stack) {
        if (stack.is(ForeverSkyblock.BATCH_PROCESSING_CARD.get())) return 2;
        if (stack.is(ForeverSkyblock.PROCESSING_SPEED_CARD.get()) || stack.is(ForeverSkyblock.GARDEN_ACCELERATOR_CARD.get())
                || stack.is(ForeverSkyblock.BUFFER_EXPANSION_CARD.get())) return 1;
        return 0;
    }
    private static int upgradeMax(Item item) {
        if (item == ForeverSkyblock.PROCESSING_SPEED_CARD.get()) return 3;
        if (item == ForeverSkyblock.GARDEN_ACCELERATOR_CARD.get()) return 2;
        if (item == ForeverSkyblock.BUFFER_EXPANSION_CARD.get()) return 2;
        if (item == ForeverSkyblock.BATCH_PROCESSING_CARD.get()) return 1;
        return 0;
    }
    public int speedUpgradeCount() { return installedCount(ForeverSkyblock.PROCESSING_SPEED_CARD.get()); }
    public int gardenUpgradeCount() { return installedCount(ForeverSkyblock.GARDEN_ACCELERATOR_CARD.get()); }
    public int bufferUpgradeCount() { return installedCount(ForeverSkyblock.BUFFER_EXPANSION_CARD.get()); }
    public int batchUpgradeCount() { return installedCount(ForeverSkyblock.BATCH_PROCESSING_CARD.get()); }
    private int installedCount(Item item) {
        if (machineTier() < 2) return 0;
        int count = 0;
        for (int slot = UPGRADE_1; slot <= UPGRADE_3; slot++) if (inventory.getStackInSlot(slot).is(item)) count++;
        return count;
    }
    public int upgradeCapacityUsed() {
        if (machineTier() < 2) return 0;
        int used = 0;
        for (int slot = UPGRADE_1; slot <= UPGRADE_3; slot++) used += upgradeCost(inventory.getStackInSlot(slot));
        return Math.min(UPGRADE_CAPACITY, used);
    }
    public boolean canInstallUpgrade(int slot, ItemStack stack) {
        if (machineTier() < 2 || !isUpgradeSlot(slot) || !isUpgradeCard(stack)) return false;
        ItemStack current = inventory.getStackInSlot(slot);
        int currentCost = upgradeCost(current);
        int newCost = upgradeCost(stack);
        if (upgradeCapacityUsed() - currentCost + newCost > UPGRADE_CAPACITY) return false;
        int existing = installedCount(stack.getItem()) - (current.is(stack.getItem()) ? 1 : 0);
        return existing + 1 <= upgradeMax(stack.getItem());
    }

    public static int applyProcessingSpeed(int baseTicks, int speedCards) {
        return switch (Math.clamp(speedCards, 0, 3)) {
            case 1 -> Math.max(1, (int) Math.ceil(baseTicks * .80));
            case 2 -> Math.max(1, (int) Math.ceil(baseTicks * .65));
            case 3 -> Math.max(1, (int) Math.ceil(baseTicks * .50));
            default -> Math.max(1, baseTicks);
        };
    }
    public static int applyGardenSpeed(int baseTicks, int speedCards, int gardenCards) {
        int speedAdjusted = applyProcessingSpeed(baseTicks, speedCards);
        double multiplier = 1.0 + .25 * Math.clamp(gardenCards, 0, 2);
        return Math.max(1, (int) Math.ceil(speedAdjusted / multiplier));
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

    public static boolean acceptsInput(Level level, ItemStack stack, boolean mixing) { return acceptsInput(level, stack, mixing, 1); }
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

    public int outputPolicy(int slot) {
        if (machineTier() < 2) return POLICY_BLOCK;
        int index = outputPolicyIndex(slot);
        return index < 0 ? POLICY_BLOCK : outputPolicies[index];
    }
    public void cycleOutputPolicy(int slot) {
        if (machineTier() < 2 || !outputActive(slot)) return;
        int index = outputPolicyIndex(slot);
        if (index < 0) return;
        outputPolicies[index] = (outputPolicies[index] + 1) % 3;
        setChanged();
        visualDirty = true;
    }
    private static int outputPolicyIndex(int slot) {
        return switch (slot) {
            case OUTPUT -> 0;
            case FREEZING_OUTPUT -> 1;
            case GARDEN_OUTPUT_PRIMARY -> 2;
            case GARDEN_OUTPUT_SECONDARY -> 3;
            default -> -1;
        };
    }
    public boolean outputActive(int slot) {
        return switch (slot) {
            case OUTPUT -> !hasGardenChamber();
            case FREEZING_OUTPUT -> hasFreezingChamber();
            case GARDEN_OUTPUT_PRIMARY, GARDEN_OUTPUT_SECONDARY -> hasGardenChamber();
            default -> false;
        };
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
        flushAllBuffers();
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
        duration = machineTier() >= 2 ? applyProcessingSpeed(recipe.processingTime(), speedUpgradeCount()) : recipe.processingTime();
        CondenserInput input = currentInput(mixing);
        int primaryNeed = recipe.primaryConsumption(input);
        int secondaryNeed = recipe.requiresModule() ? recipe.secondaryConsumption(input) : 0;
        ItemStack result = recipe.result();

        int multiplier = 1;
        if (machineTier() >= 2 && batchUpgradeCount() > 0
                && availableInput(INPUT, input.first()) >= primaryNeed * 2
                && (!recipe.requiresModule() || availableInput(SECOND_INPUT, input.second()) >= secondaryNeed * 2)
                && canStoreProduced(OUTPUT, result, result.getCount() * 2)) {
            multiplier = 2;
        }
        if (!canStoreProduced(OUTPUT, result, result.getCount() * multiplier)) {
            status = BLOCKED;
            return false;
        }
        status = RUNNING;
        progress++;
        setChanged();
        if (progress >= duration) {
            consumeInput(INPUT, input.first(), primaryNeed * multiplier);
            if (recipe.requiresModule()) consumeInput(SECOND_INPUT, input.second(), secondaryNeed * multiplier);
            storeProduced(OUTPUT, result, result.getCount() * multiplier);
            progress = 0;
        }
        return true;
    }

    private boolean processFreezing(boolean connected) {
        if (!connected) { freezingStatus = IDLE; return false; }
        ItemStack input = inventory.getStackInSlot(FREEZING_INPUT);
        if (input.isEmpty()) { freezingStatus = IDLE; return false; }
        boolean completes = freezingProgress + 1 >= FREEZING_REQUIRED_ITEMS;
        ItemStack ice = new ItemStack(Items.ICE);
        if (completes && !canStoreProduced(FREEZING_OUTPUT, ice, 1)) { freezingStatus = BLOCKED; return false; }
        freezingStatus = RUNNING;
        consumeInput(FREEZING_INPUT, input, 1);
        freezingProgress++;
        if (freezingProgress >= FREEZING_REQUIRED_ITEMS) {
            storeProduced(FREEZING_OUTPUT, ice, 1);
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
        gardenDuration = applyGardenSpeed(recipe.processingTime(), speedUpgradeCount(), gardenUpgradeCount());
        ItemStack primary = recipe.primaryResult();
        ItemStack secondary = recipe.secondaryResult();
        if (!canStoreProduced(GARDEN_OUTPUT_PRIMARY, primary, primary.getCount())
                || !canStoreProduced(GARDEN_OUTPUT_SECONDARY, secondary, secondary.getCount())) {
            gardenStatus = BLOCKED;
            return false;
        }
        gardenStatus = RUNNING;
        gardenProgress++;
        setChanged();
        if (gardenProgress >= gardenDuration) {
            storeProduced(GARDEN_OUTPUT_PRIMARY, primary, primary.getCount());
            storeProduced(GARDEN_OUTPUT_SECONDARY, secondary, secondary.getCount());
            gardenProgress = 0;
        }
        return true;
    }

    private int inputLane(int slot) {
        return switch (slot) { case INPUT -> 0; case SECOND_INPUT -> 1; case FREEZING_INPUT -> 2; default -> -1; };
    }
    private int outputLane(int slot) {
        return switch (slot) {
            case OUTPUT -> 0;
            case FREEZING_OUTPUT -> 1;
            case GARDEN_OUTPUT_PRIMARY -> 2;
            case GARDEN_OUTPUT_SECONDARY -> 3;
            default -> -1;
        };
    }
    private static int segmentIndex(int lane, int segment) { return lane * BUFFER_SEGMENTS_PER_LANE + segment; }

    private int availableInput(int slot, ItemStack template) {
        if (template.isEmpty()) return 0;
        int total = 0;
        ItemStack visible = inventory.getStackInSlot(slot);
        if (ItemStack.isSameItemSameComponents(visible, template)) total += visible.getCount();
        int lane = inputLane(slot);
        if (lane >= 0) for (int s = 0; s < BUFFER_SEGMENTS_PER_LANE; s++) {
            ItemStack buffered = inputBuffers.getStackInSlot(segmentIndex(lane, s));
            if (ItemStack.isSameItemSameComponents(buffered, template)) total += buffered.getCount();
        }
        return total;
    }

    private void consumeInput(int slot, ItemStack template, int amount) {
        if (amount <= 0 || template.isEmpty()) return;
        int remaining = amount;
        ItemStack visible = inventory.getStackInSlot(slot);
        if (ItemStack.isSameItemSameComponents(visible, template)) {
            int take = Math.min(remaining, visible.getCount());
            inventory.extractItem(slot, take, false);
            remaining -= take;
        }
        int lane = inputLane(slot);
        if (lane >= 0) for (int s = 0; s < BUFFER_SEGMENTS_PER_LANE && remaining > 0; s++) {
            int index = segmentIndex(lane, s);
            ItemStack buffered = inputBuffers.getStackInSlot(index);
            if (!ItemStack.isSameItemSameComponents(buffered, template)) continue;
            int take = Math.min(remaining, buffered.getCount());
            inputBuffers.extractItem(index, take, false);
            remaining -= take;
        }
    }

    private boolean canStoreProduced(int slot, ItemStack template, int count) {
        int policy = outputPolicy(slot);
        if (policy == POLICY_VOID_ALL || policy == POLICY_VOID_EXCESS) return true;
        return outputFreeSpace(slot, template) >= count;
    }

    private int outputFreeSpace(int slot, ItemStack template) {
        if (template.isEmpty()) return 0;
        int space = stackSpace(inventory.getStackInSlot(slot), template);
        int lane = outputLane(slot);
        int unlocked = machineTier() >= 2 ? bufferUpgradeCount() : 0;
        if (lane >= 0) for (int s = 0; s < unlocked; s++)
            space += stackSpace(outputBuffers.getStackInSlot(segmentIndex(lane, s)), template);
        return space;
    }
    private static int stackSpace(ItemStack target, ItemStack template) {
        int max = Math.min(64, template.getMaxStackSize());
        if (target.isEmpty()) return max;
        return ItemStack.isSameItemSameComponents(target, template) ? Math.max(0, max - target.getCount()) : 0;
    }

    private void storeProduced(int slot, ItemStack template, int count) {
        int policy = outputPolicy(slot);
        if (policy == POLICY_VOID_ALL || count <= 0 || template.isEmpty()) return;
        int remaining = count;
        remaining = mergeIntoVisible(slot, template, remaining);
        int lane = outputLane(slot);
        int unlocked = machineTier() >= 2 ? bufferUpgradeCount() : 0;
        if (lane >= 0) for (int s = 0; s < unlocked && remaining > 0; s++) {
            int index = segmentIndex(lane, s);
            remaining = mergeIntoHandler(outputBuffers, index, template, remaining);
        }
        // VOID_EXCESS deliberately discards any remainder. BLOCK never reaches this point without enough space.
    }

    private int mergeIntoVisible(int slot, ItemStack template, int amount) {
        ItemStack target = inventory.getStackInSlot(slot);
        int space = stackSpace(target, template);
        int add = Math.min(space, amount);
        if (add <= 0) return amount;
        inventory.setStackInSlot(slot, target.isEmpty() ? template.copyWithCount(add) : target.copyWithCount(target.getCount() + add));
        return amount - add;
    }
    private static int mergeIntoHandler(ItemStackHandler handler, int slot, ItemStack template, int amount) {
        ItemStack target = handler.getStackInSlot(slot);
        int space = stackSpace(target, template);
        int add = Math.min(space, amount);
        if (add <= 0) return amount;
        handler.setStackInSlot(slot, target.isEmpty() ? template.copyWithCount(add) : target.copyWithCount(target.getCount() + add));
        return amount - add;
    }

    private ItemStack insertInputWithBuffer(int slot, ItemStack stack, boolean simulate) {
        if (!isInput(slot) || stack.isEmpty() || !inventory.isItemValid(slot, stack)) return stack;
        ItemStack remainder = inventory.insertItem(slot, stack, simulate);
        if (remainder.isEmpty() || machineTier() < 2) return remainder;
        int lane = inputLane(slot);
        int unlocked = bufferUpgradeCount();
        if (lane < 0 || unlocked <= 0) return remainder;
        for (int s = 0; s < unlocked && !remainder.isEmpty(); s++) {
            int index = segmentIndex(lane, s);
            remainder = inputBuffers.insertItem(index, remainder, simulate);
        }
        return remainder;
    }

    private void flushAllBuffers() {
        for (int slot : new int[]{INPUT, SECOND_INPUT, FREEZING_INPUT}) flushInputLane(slot);
        for (int slot : new int[]{OUTPUT, FREEZING_OUTPUT, GARDEN_OUTPUT_PRIMARY, GARDEN_OUTPUT_SECONDARY}) flushOutputLane(slot);
    }
    private void flushInputLane(int slot) {
        int lane = inputLane(slot);
        if (lane < 0) return;
        for (int s = 0; s < BUFFER_SEGMENTS_PER_LANE; s++) {
            int index = segmentIndex(lane, s);
            ItemStack buffered = inputBuffers.getStackInSlot(index);
            if (buffered.isEmpty()) continue;
            ItemStack remainder = inventory.insertItem(slot, buffered, false);
            int moved = buffered.getCount() - remainder.getCount();
            if (moved > 0) inputBuffers.extractItem(index, moved, false);
        }
    }
    private void flushOutputLane(int slot) {
        int lane = outputLane(slot);
        if (lane < 0) return;
        for (int s = 0; s < BUFFER_SEGMENTS_PER_LANE; s++) {
            int index = segmentIndex(lane, s);
            ItemStack buffered = outputBuffers.getStackInSlot(index);
            if (buffered.isEmpty()) continue;
            int remaining = mergeIntoVisible(slot, buffered, buffered.getCount());
            int moved = buffered.getCount() - remaining;
            if (moved > 0) outputBuffers.extractItem(index, moved, false);
        }
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
        tag.put("InputBuffers", inputBuffers.serializeNBT(registries));
        tag.put("OutputBuffers", outputBuffers.serializeNBT(registries));
        tag.putInt("Progress", progress);
        tag.putInt("FreezingProgress", freezingProgress);
        tag.putInt("GardenProgress", gardenProgress);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
        if (gardenRecipe != null) tag.putString("GardenRecipe", gardenRecipe.toString());
        for (int i = 0; i < filters.length; i++) if (!filters[i].isEmpty())
            tag.putString("InputFilter" + i, BuiltInRegistries.ITEM.getKey(filters[i].getItem()).toString());
        for (int i = 0; i < outputPolicies.length; i++) tag.putInt("OutputPolicy" + i, outputPolicies[i]);
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
        if (tag.contains("InputBuffers")) inputBuffers.deserializeNBT(registries, tag.getCompound("InputBuffers"));
        if (tag.contains("OutputBuffers")) outputBuffers.deserializeNBT(registries, tag.getCompound("OutputBuffers"));
        progress = Math.max(0, Math.min(32766, tag.getInt("Progress")));
        freezingProgress = Math.max(0, Math.min(FREEZING_REQUIRED_ITEMS - 1, tag.getInt("FreezingProgress")));
        gardenProgress = Math.max(0, Math.min(32766, tag.getInt("GardenProgress")));
        progressRecipe = ResourceLocation.tryParse(tag.getString("ProgressRecipe"));
        gardenRecipe = ResourceLocation.tryParse(tag.getString("GardenRecipe"));
        for (int i = 0; i < filters.length; i++) {
            ResourceLocation id = ResourceLocation.tryParse(tag.getString("InputFilter" + i));
            filters[i] = id == null ? ItemStack.EMPTY : BuiltInRegistries.ITEM.getOptional(id).map(ItemStack::new).orElse(ItemStack.EMPTY);
        }
        for (int i = 0; i < outputPolicies.length; i++)
            outputPolicies[i] = tag.contains("OutputPolicy" + i) ? Math.clamp(tag.getInt("OutputPolicy" + i), POLICY_BLOCK, POLICY_VOID_ALL) : POLICY_BLOCK;
    }

    @Override public Component getDisplayName() {
        return Component.translatable(machineTier() >= 2 ? "block.forever_skyblock.condenser_mark_ii" : "block.forever_skyblock.condenser_mark_i");
    }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) { return new CondenserMenu(id, playerInventory, this); }

    public ItemStack extractAutomationOutput(int slot, int amount, boolean simulate) {
        if (!isOutput(slot) || amount <= 0) return ItemStack.EMPTY;
        ItemStack extracted = inventory.extractItem(slot, amount, simulate);
        if (!simulate && !extracted.isEmpty()) flushOutputLane(slot);
        return extracted;
    }

    public void dropBufferedItems(java.util.function.Consumer<ItemStack> sink) {
        for (int i = 0; i < inputBuffers.getSlots(); i++) {
            ItemStack stack = inputBuffers.getStackInSlot(i);
            if (!stack.isEmpty()) sink.accept(stack.copy());
        }
        for (int i = 0; i < outputBuffers.getSlots(); i++) {
            ItemStack stack = outputBuffers.getStackInSlot(i);
            if (!stack.isEmpty()) sink.accept(stack.copy());
        }
    }

    private final class Automation implements IItemHandler {
        private final boolean insert, extract;
        private Automation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return BASE_MACHINE_SLOTS; }
        @Override public ItemStack getStackInSlot(int slot) {
            if (slot < 0 || slot >= BASE_MACHINE_SLOTS) return ItemStack.EMPTY;
            if (hasGardenChamber()) {
                if (slot != GARDEN_SPECIMEN && slot != GARDEN_OUTPUT_PRIMARY) return ItemStack.EMPTY;
            } else {
                if (slot == GARDEN_SPECIMEN || slot == GARDEN_OUTPUT_PRIMARY || slot == GARDEN_OUTPUT_SECONDARY) return ItemStack.EMPTY;
                if (slot == SECOND_INPUT && !hasMixingChamber()) return ItemStack.EMPTY;
                if ((slot == FREEZING_INPUT || slot == FREEZING_OUTPUT) && !hasFreezingChamber()) return ItemStack.EMPTY;
            }
            return inventory.getStackInSlot(slot);
        }
        @Override public int getSlotLimit(int slot) { return slot >= 0 && slot < BASE_MACHINE_SLOTS ? inventory.getSlotLimit(slot) : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            return insert && slot >= 0 && slot < BASE_MACHINE_SLOTS && inventory.isItemValid(slot, stack);
        }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) {
            return isItemValid(slot, stack) ? insertInputWithBuffer(slot, stack, simulate) : stack;
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            if (!extract) return ItemStack.EMPTY;
            if (hasGardenChamber()) {
                if (slot == GARDEN_OUTPUT_PRIMARY) return extractAutomationOutput(slot, amount, simulate);
                return ItemStack.EMPTY;
            }
            if (slot == OUTPUT || slot == FREEZING_OUTPUT && hasFreezingChamber())
                return extractAutomationOutput(slot, amount, simulate);
            return ItemStack.EMPTY;
        }
    }
}

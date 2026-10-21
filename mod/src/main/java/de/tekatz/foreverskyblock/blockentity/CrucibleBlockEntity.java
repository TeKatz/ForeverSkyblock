package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.CrucibleBlock;
import de.tekatz.foreverskyblock.menu.CrucibleMenu;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.HolderLookup;
import net.minecraft.core.registries.BuiltInRegistries;
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
import net.minecraft.world.item.Items;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.material.Fluids;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.fluids.FluidStack;
import net.neoforged.neoforge.fluids.capability.IFluidHandler;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

public final class CrucibleBlockEntity extends BlockEntity implements MenuProvider {
    public static final int INPUT = 0, OUTPUT = 1, MACHINE_SLOTS = 2;
    public static final int DATA_COUNT = 9;
    public static final int CAPACITY = 4000, BATCH_MB = 1000;
    public static final int COBBLE_PER_BATCH = 8, FRAGMENTS_PER_BATCH = 40;
    public static final int NETHERRACK_LAVA_MB = 250, NETHERRACK_DURATION = 80;
    public static final int FREEZE_DURATION = 100;
    public static final int MODE_LAVA = 0, MODE_OBSIDIAN = 1;
    public static final int IDLE = 0, HEATING = 1, NO_HEAT = 2, STORAGE_FULL = 3, OUTPUT_BLOCKED = 4, FREEZING = 5;
    public static final int HEAT_NONE = 0, HEAT_TORCH = 1, HEAT_CAMPFIRE = 2, HEAT_LAVA = 3, HEAT_SUPERHEATING = 4;

    private static final ResourceLocation SUPERHEATING = ResourceLocation.fromNamespaceAndPath("mekanism", "superheating_element");

    private int progress;
    private int duration = durationFor(HEAT_TORCH);
    private int freezeProgress;
    private int lavaMb;
    private int mode = MODE_LAVA;
    private int status = IDLE;
    private int heatTier;
    private boolean hasTank;
    private boolean hasFreezer;
    private @Nullable Direction tankDirection;
    private @Nullable ResourceLocation progressInput;

    private final ItemStackHandler inventory = new ItemStackHandler(MACHINE_SLOTS) {
        @Override public boolean isItemValid(int slot, ItemStack stack) { return slot == INPUT && acceptsInput(stack); }
        @Override protected void onContentsChanged(int slot) { setChanged(); }
    };
    private final IItemHandler inputAccess = new ItemAutomation(true, false);
    private final IItemHandler outputAccess = new ItemAutomation(false, true);
    private final IItemHandler combinedAccess = new ItemAutomation(true, true);
    private final IFluidHandler fluidAccess = new LavaHandler();

    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            return switch (index) {
                case 0 -> progress;
                case 1 -> duration;
                case 2 -> lavaMb;
                case 3 -> heatTier;
                case 4 -> hasTank ? 1 : 0;
                case 5 -> hasFreezer ? 1 : 0;
                case 6 -> mode;
                case 7 -> status;
                case 8 -> freezeProgress;
                default -> 0;
            };
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return DATA_COUNT; }
    };

    public CrucibleBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.CRUCIBLE_ENTITY.get(), pos, state); }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }
    public int lavaMb() { return lavaMb; }
    public int heatTier() { return heatTier; }
    public boolean hasTank() { return hasTank; }
    public boolean hasFreezer() { return hasFreezer; }
    public int mode() { return mode; }
    public int status() { return status; }
    public int freezeProgress() { return freezeProgress; }

    public static boolean acceptsInput(ItemStack stack) {
        return stack.is(Items.COBBLESTONE) || stack.is(ForeverSkyblock.STONE_FRAGMENT.get()) || stack.is(Items.GRAVEL);
    }
    public static int requiredCount(ItemStack stack) {
        if (stack.is(Items.COBBLESTONE)) return COBBLE_PER_BATCH;
        if (stack.is(ForeverSkyblock.STONE_FRAGMENT.get())) return FRAGMENTS_PER_BATCH;
        if (stack.is(Items.GRAVEL)) return 1;
        return 0;
    }
    public static int durationFor(int tier) {
        return switch (tier) {
            case HEAT_TORCH -> 6000;
            case HEAT_CAMPFIRE -> 1600;
            case HEAT_LAVA -> 600;
            case HEAT_SUPERHEATING -> 160;
            default -> 1;
        };
    }
    public static String heatTierName(int tier) {
        return switch (tier) {
            case HEAT_TORCH -> "Torch";
            case HEAT_CAMPFIRE -> "Campfire";
            case HEAT_LAVA -> "Lava";
            case HEAT_SUPERHEATING -> "Superheating Element";
            default -> "No heat";
        };
    }

    public IItemHandler automation(@Nullable Direction side) { return side == null ? combinedAccess : side == Direction.DOWN ? outputAccess : inputAccess; }
    public IFluidHandler fluidAutomation(@Nullable Direction side) { return fluidAccess; }
    public void toggleMode() {
        refreshNeighbors();
        if (!hasTank || !hasFreezer) return;
        mode = mode == MODE_LAVA ? MODE_OBSIDIAN : MODE_LAVA;
        resetFreezing();
        setChanged();
    }
    public static void tick(Level level, BlockPos pos, BlockState state, CrucibleBlockEntity crucible) { crucible.process(); }

    private void process() {
        if (level == null) return;
        refreshNeighbors();
        heatTier = detectHeatTier();
        normalizeMode();
        ItemStack input = inventory.getStackInSlot(INPUT);
        boolean netherrackProcess = input.is(Items.GRAVEL);

        if (mode == MODE_LAVA && hasTank && !netherrackProcess) pushLavaToTank();
        boolean freezeReady = !netherrackProcess && mode == MODE_OBSIDIAN && hasFreezer && lavaMb >= BATCH_MB;
        boolean outputBlocked = freezeReady && !canAdd(Items.OBSIDIAN);
        boolean freezing = false;
        if (freezeReady && !outputBlocked) freezing = advanceFreezing();
        else if (!freezeReady) resetFreezing();

        ResourceLocation inputId = input.isEmpty() ? null : BuiltInRegistries.ITEM.getKey(input.getItem());
        if (inputId == null || !inputId.equals(progressInput)) {
            progress = 0;
            progressInput = inputId;
            setChanged();
        }

        if (netherrackProcess) {
            duration = NETHERRACK_DURATION;
            if (lavaMb < NETHERRACK_LAVA_MB) { status = NO_HEAT; return; }
            if (!canAdd(Items.NETHERRACK)) { status = OUTPUT_BLOCKED; return; }
            status = HEATING;
            progress++;
            setChanged();
            if (progress >= duration) {
                inventory.extractItem(INPUT, 1, false);
                changeLava(-NETHERRACK_LAVA_MB);
                addOutput(Items.NETHERRACK);
                progress = 0;
                progressInput = inventory.getStackInSlot(INPUT).isEmpty() ? null
                        : BuiltInRegistries.ITEM.getKey(inventory.getStackInSlot(INPUT).getItem());
            }
            return;
        }

        duration = durationFor(heatTier);
        int required = requiredCount(input);
        if (required == 0 || input.getCount() < required) {
            status = outputBlocked ? OUTPUT_BLOCKED : freezing ? FREEZING : IDLE;
            return;
        }
        if (heatTier == HEAT_NONE) { status = freezing ? FREEZING : NO_HEAT; return; }
        if (lavaMb + BATCH_MB > CAPACITY) { status = outputBlocked ? OUTPUT_BLOCKED : freezing ? FREEZING : STORAGE_FULL; return; }
        status = HEATING;
        progress++;
        setChanged();
        if (progress >= duration) {
            inventory.extractItem(INPUT, required, false);
            changeLava(BATCH_MB);
            progress = 0;
            progressInput = inventory.getStackInSlot(INPUT).isEmpty() ? null
                    : BuiltInRegistries.ITEM.getKey(inventory.getStackInSlot(INPUT).getItem());
        }
    }

    private boolean advanceFreezing() {
        freezeProgress++;
        setChanged();
        if (freezeProgress >= FREEZE_DURATION) {
            addOutput(Items.OBSIDIAN);
            changeLava(-BATCH_MB);
            freezeProgress = 0;
        }
        return true;
    }
    private void resetFreezing() { if (freezeProgress != 0) { freezeProgress = 0; setChanged(); } }
    private int detectHeatTier() {
        if (level == null) return HEAT_NONE;
        BlockState below = level.getBlockState(worldPosition.below());
        ResourceLocation id = BuiltInRegistries.BLOCK.getKey(below.getBlock());
        if (SUPERHEATING.equals(id)) return HEAT_SUPERHEATING;
        if (below.getFluidState().is(FluidTags.LAVA) && below.getFluidState().isSource()) return HEAT_LAVA;
        if (below.is(Blocks.CAMPFIRE) || below.is(Blocks.SOUL_CAMPFIRE)) return HEAT_CAMPFIRE;
        if (below.is(Blocks.TORCH)) return HEAT_TORCH;
        return HEAT_NONE;
    }
    private void refreshNeighbors() {
        hasTank = false; hasFreezer = false; tankDirection = null;
        if (level == null) return;
        for (Direction direction : Direction.values()) {
            BlockPos adjacent = worldPosition.relative(direction);
            if (!level.hasChunkAt(adjacent)) continue;
            BlockState state = level.getBlockState(adjacent);
            if (state.is(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get())) hasFreezer = true;
            ResourceLocation id = BuiltInRegistries.BLOCK.getKey(state.getBlock());
            if (!hasTank && "mekanism".equals(id.getNamespace()) && id.getPath().endsWith("fluid_tank")) {
                IFluidHandler handler = level.getCapability(Capabilities.FluidHandler.BLOCK, adjacent, direction.getOpposite());
                if (handler != null) { hasTank = true; tankDirection = direction; }
            }
        }
    }
    private void normalizeMode() {
        if (hasTank && !hasFreezer) { if (mode != MODE_LAVA) resetFreezing(); mode = MODE_LAVA; }
        else if (!hasTank && hasFreezer) mode = MODE_OBSIDIAN;
        else if (!hasFreezer) resetFreezing();
    }
    private void pushLavaToTank() {
        if (level == null || tankDirection == null || lavaMb <= 0) return;
        BlockPos targetPos = worldPosition.relative(tankDirection);
        IFluidHandler target = level.getCapability(Capabilities.FluidHandler.BLOCK, targetPos, tankDirection.getOpposite());
        if (target == null) return;
        int sent = target.fill(new FluidStack(Fluids.LAVA, Math.min(lavaMb, BATCH_MB)), IFluidHandler.FluidAction.EXECUTE);
        if (sent > 0) changeLava(-sent);
    }
    private boolean canAdd(net.minecraft.world.item.Item item) {
        ItemStack out = inventory.getStackInSlot(OUTPUT);
        return out.isEmpty() || out.is(item) && out.getCount() < out.getMaxStackSize();
    }
    private void addOutput(net.minecraft.world.item.Item item) {
        ItemStack out = inventory.getStackInSlot(OUTPUT);
        if (out.isEmpty()) inventory.setStackInSlot(OUTPUT, new ItemStack(item)); else out.grow(1);
        setChanged();
    }
    private void changeLava(int delta) {
        int next = Math.max(0, Math.min(CAPACITY, lavaMb + delta));
        if (next == lavaMb) return;
        lavaMb = next;
        updateFillState();
        setChanged();
    }
    private void updateFillState() {
        if (level == null) return;
        BlockState state = getBlockState();
        if (!state.hasProperty(CrucibleBlock.FILL)) return;
        int fill = lavaMb <= 0 ? 0 : Math.max(1, Math.min(8, (lavaMb * 8 + CAPACITY - 1) / CAPACITY));
        if (state.getValue(CrucibleBlock.FILL) != fill) level.setBlock(worldPosition, state.setValue(CrucibleBlock.FILL, fill), Block.UPDATE_CLIENTS);
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        tag.putInt("FreezeProgress", freezeProgress);
        tag.putInt("Lava", lavaMb);
        tag.putInt("Mode", mode);
        if (progressInput != null) tag.putString("ProgressInput", progressInput.toString());
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        inventory.deserializeNBT(registries, tag.getCompound("Inventory"));
        progress = Math.max(0, tag.getInt("Progress"));
        freezeProgress = Math.max(0, Math.min(FREEZE_DURATION - 1, tag.getInt("FreezeProgress")));
        lavaMb = Math.max(0, Math.min(CAPACITY, tag.getInt("Lava")));
        mode = tag.getInt("Mode") == MODE_OBSIDIAN ? MODE_OBSIDIAN : MODE_LAVA;
        progressInput = ResourceLocation.tryParse(tag.getString("ProgressInput"));
    }
    @Override public Component getDisplayName() { return Component.literal("Crucible"); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory inventory, Player player) { return new CrucibleMenu(id, inventory, this); }

    private final class ItemAutomation implements IItemHandler {
        private final boolean insert, extract;
        private ItemAutomation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return MACHINE_SLOTS; }
        @Override public ItemStack getStackInSlot(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getStackInSlot(slot) : ItemStack.EMPTY; }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) { return insert && slot == INPUT ? inventory.insertItem(slot, stack, simulate) : stack; }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) { return extract && slot == OUTPUT ? inventory.extractItem(slot, amount, simulate) : ItemStack.EMPTY; }
        @Override public int getSlotLimit(int slot) { return slot >= 0 && slot < MACHINE_SLOTS ? inventory.getSlotLimit(slot) : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return insert && slot == INPUT && inventory.isItemValid(slot, stack); }
    }
    private final class LavaHandler implements IFluidHandler {
        @Override public int getTanks() { return 1; }
        @Override public FluidStack getFluidInTank(int tank) { return tank == 0 && lavaMb > 0 ? new FluidStack(Fluids.LAVA, lavaMb) : FluidStack.EMPTY; }
        @Override public int getTankCapacity(int tank) { return tank == 0 ? CAPACITY : 0; }
        @Override public boolean isFluidValid(int tank, FluidStack stack) { return tank == 0 && stack.getFluid() == Fluids.LAVA; }
        @Override public int fill(FluidStack resource, FluidAction action) { return 0; }
        @Override public FluidStack drain(FluidStack resource, FluidAction action) {
            if (resource.isEmpty() || resource.getFluid() != Fluids.LAVA) return FluidStack.EMPTY;
            return drain(resource.getAmount(), action);
        }
        @Override public FluidStack drain(int maxDrain, FluidAction action) {
            int amount = Math.min(Math.max(0, maxDrain), lavaMb);
            if (amount <= 0) return FluidStack.EMPTY;
            if (action.execute()) changeLava(-amount);
            return new FluidStack(Fluids.LAVA, amount);
        }
    }
}

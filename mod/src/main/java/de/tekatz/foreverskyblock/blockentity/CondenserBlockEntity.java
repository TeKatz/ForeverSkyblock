package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
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
import net.minecraft.world.item.crafting.SingleRecipeInput;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.neoforge.items.IItemHandler;
import net.neoforged.neoforge.items.ItemStackHandler;
import org.jetbrains.annotations.Nullable;

public final class CondenserBlockEntity extends BlockEntity implements MenuProvider {
    public static final int INPUT = 0, OUTPUT = 1;
    public static final int IDLE = 0, RUNNING = 1, BLOCKED = 2;
    private int progress;
    private int duration = CondenserRecipe.DEFAULT_TIME;
    private int status;
    private ResourceLocation progressRecipe;
    private final RecipeManager.CachedCheck<SingleRecipeInput, CondenserRecipe> recipeCheck = RecipeManager.createCheck(ForeverSkyblock.CONDENSING.get());
    private final ItemStackHandler inventory = new ItemStackHandler(2) {
        @Override public int getSlotLimit(int slot) { return 64; }
        @Override public boolean isItemValid(int slot, ItemStack stack) {
            return slot == INPUT && level != null && findRecipe(stack) != null;
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); }
    };
    private final IItemHandler inputAccess = new Automation(true, false);
    private final IItemHandler outputAccess = new Automation(false, true);
    private final IItemHandler combinedAccess = new Automation(true, true);
    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) { return switch (index) { case 0 -> progress; case 1 -> duration; case 2 -> status; default -> 0; }; }
        @Override public void set(int index, int value) {
            switch (index) { case 0 -> progress = value; case 1 -> duration = value; case 2 -> status = value; default -> { } }
        }
        @Override public int getCount() { return 3; }
    };

    public CondenserBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.CONDENSER_ENTITY.get(), pos, state); }
    public ItemStackHandler inventory() { return inventory; }
    public ContainerData menuData() { return data; }
    public IItemHandler automation(@Nullable Direction side) { return side == null ? combinedAccess : side == Direction.DOWN ? outputAccess : inputAccess; }

    private @Nullable RecipeHolder<CondenserRecipe> findRecipe(ItemStack input) {
        if (level == null || input.isEmpty()) return null;
        return recipeCheck.getRecipeFor(new SingleRecipeInput(input), level).orElse(null);
    }

    public static void tick(Level level, BlockPos pos, BlockState state, CondenserBlockEntity machine) { machine.process(); }

    private void process() {
        RecipeHolder<CondenserRecipe> holder = findRecipe(inventory.getStackInSlot(INPUT));
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
        boolean fits = output.isEmpty() || (ItemStack.isSameItemSameComponents(output, result)
                && output.getCount() + result.getCount() <= Math.min(64, output.getMaxStackSize()));
        if (!fits) { status = BLOCKED; setActive(false); return; }
        status = RUNNING;
        setActive(true);
        progress++;
        setChanged();
        if (progress >= duration) {
            // Both slots are changed on the server thread only after the complete result fits.
            inventory.extractItem(INPUT, 1, false);
            inventory.setStackInSlot(OUTPUT, result.copyWithCount(output.getCount() + result.getCount()));
            progress = 0;
        }
    }

    private void setActive(boolean active) {
        if (level != null && getBlockState().getValue(CondenserBlock.ACTIVE) != active)
            level.setBlock(worldPosition, getBlockState().setValue(CondenserBlock.ACTIVE, active), 3);
    }

    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries));
        tag.putInt("Progress", progress);
        if (progressRecipe != null) tag.putString("ProgressRecipe", progressRecipe.toString());
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        CompoundTag saved = tag.getCompound("Inventory").copy();
        saved.putInt("Size", 2);
        inventory.deserializeNBT(registries, saved);
        progress = Math.max(0, Math.min(32766, tag.getInt("Progress")));
        progressRecipe = ResourceLocation.tryParse(tag.getString("ProgressRecipe"));
    }
    @Override public Component getDisplayName() { return Component.translatable("block.forever_skyblock.condenser_mark_i"); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory playerInventory, Player player) { return new CondenserMenu(id, playerInventory, this); }

    private final class Automation implements IItemHandler {
        private final boolean insert, extract;
        private Automation(boolean insert, boolean extract) { this.insert = insert; this.extract = extract; }
        @Override public int getSlots() { return 2; }
        @Override public ItemStack getStackInSlot(int slot) { return inventory.getStackInSlot(slot); }
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

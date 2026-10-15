package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.AscensionChamberBlock;
import de.tekatz.foreverskyblock.item.ForeverCore;
import de.tekatz.foreverskyblock.menu.AscensionMenu;
import de.tekatz.foreverskyblock.progression.*;
import java.io.IOException;
import java.util.UUID;
import net.minecraft.core.*;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.MenuProvider;
import net.minecraft.world.entity.player.*;
import net.minecraft.world.inventory.*;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.neoforge.items.*;

public final class AscensionChamberBlockEntity extends BlockEntity implements MenuProvider {
    public static final int DURATION = 200;
    public static final int READY = 0, RUNNING = 1, OFFLINE = 2, STORAGE_ERROR = 3, INVALID = 4, CLAIMED = 5, COMPLETE = 6;
    private UUID owner, core;
    private int progress, status;
    private final ItemStackHandler inventory = new ItemStackHandler(1) {
        @Override public int getSlotLimit(int slot) { return 1; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return !busy() && stack.is(ForeverSkyblock.FOREVER_CORE.get()); }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            return busy() ? ItemStack.EMPTY : super.extractItem(slot, amount, simulate);
        }
        @Override protected void onContentsChanged(int slot) { setChanged(); }
    };
    // Expose the read/insert/extract contract only; external pipes cannot replace a locked stack directly.
    private final IItemHandler automation = new IItemHandler() {
        @Override public int getSlots() { return 1; }
        @Override public ItemStack getStackInSlot(int slot) { return inventory.getStackInSlot(slot).copy(); }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) { return inventory.insertItem(slot, stack, simulate); }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) { return inventory.extractItem(slot, amount, simulate); }
        @Override public int getSlotLimit(int slot) { return 1; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return inventory.isItemValid(slot, stack); }
    };
    private final ContainerData data = new ContainerData() {
        @Override public int get(int index) {
            if (index == 0) return progress;
            if (index == 1) return status;
            if (index == 2) return busy() ? 1 : 0;
            if (owner == null) return 0;
            int part = index - 3;
            long value = part < 4 ? owner.getMostSignificantBits() : owner.getLeastSignificantBits();
            return (int) ((value >>> ((part % 4) * 16)) & 65535);
        }
        @Override public void set(int index, int value) { }
        @Override public int getCount() { return 11; }
    };
    public AscensionChamberBlockEntity(BlockPos pos, BlockState state) { super(ForeverSkyblock.ASCENSION_ENTITY.get(), pos, state); }
    public ItemStackHandler inventory() { return inventory; }
    public IItemHandler automation(Direction side) { return automation; }
    public ContainerData menuData() { return data; }
    public boolean busy() { return owner != null; }
    public boolean start(ServerPlayer player) {
        if (level == null || level.isClientSide || player.server != level.getServer() || busy()) return false;
        UUID identity = ForeverCore.identity(inventory.getStackInSlot(0));
        if (identity == null) { status = INVALID; setChanged(); return false; }
        owner = player.getUUID(); core = identity; progress = 0; status = RUNNING; setChanged();
        return true;
    }
    public static void tick(Level level, BlockPos pos, BlockState state, AscensionChamberBlockEntity chamber) {
        if (!chamber.busy()) { chamber.active(false); return; }
        var player = level.getServer().getPlayerList().getPlayer(chamber.owner);
        if (player == null) { chamber.status = OFFLINE; chamber.active(false); return; }
        chamber.advance(player);
    }
    /** Called on the server thread, with the player bound when Start was pressed. */
    public void advance(ServerPlayer player) {
        if (level == null || level.isClientSide || !busy() || !owner.equals(player.getUUID()) || player.server != level.getServer()) return;
        if (!core.equals(ForeverCore.identity(inventory.getStackInSlot(0)))) { finish(INVALID); return; }
        status = RUNNING; active(true);
        if (progress < DURATION) { progress++; setChanged(); }
        if (progress < DURATION) return;
        try {
            var result = ProgressionService.get(player.server).redeemCore(player, core);
            if (result == CoreRedemptions.Result.CLAIMED_BY_OTHER) { finish(CLAIMED); return; }
            // Both a new credit and a replay consume this exact Core; a receipt never pays twice.
            inventory.setStackInSlot(0, ItemStack.EMPTY);
            finish(COMPLETE);
        } catch (IOException | IllegalArgumentException | ArithmeticException e) {
            status = STORAGE_ERROR; active(false); setChanged();
            // Retry only on an explicit Start action; do not hammer a failing profile store every tick.
            owner = null; core = null; progress = 0;
            com.mojang.logging.LogUtils.getLogger().error("Ascension could not commit the Core at {}", worldPosition, e);
        }
    }
    private void finish(int result) { owner = null; core = null; progress = 0; status = result; active(false); setChanged(); }
    private void active(boolean value) {
        if (level != null && getBlockState().getValue(AscensionChamberBlock.ACTIVE) != value)
            level.setBlock(worldPosition, getBlockState().setValue(AscensionChamberBlock.ACTIVE, value), 3);
    }
    @Override protected void saveAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.saveAdditional(tag, registries);
        tag.put("Inventory", inventory.serializeNBT(registries)); tag.putInt("Progress", progress);
        if (owner != null && core != null) { tag.putUUID("Owner", owner); tag.putUUID("Core", core); }
    }
    @Override protected void loadAdditional(CompoundTag tag, HolderLookup.Provider registries) {
        super.loadAdditional(tag, registries);
        owner = null; core = null; progress = 0; status = READY;
        inventory.deserializeNBT(registries, tag.getCompound("Inventory"));
        if (tag.hasUUID("Owner") && tag.hasUUID("Core")) {
            owner = tag.getUUID("Owner"); core = tag.getUUID("Core");
            progress = Math.clamp(tag.getInt("Progress"), 0, DURATION); status = OFFLINE;
        }
    }
    @Override public Component getDisplayName() { return Component.translatable("block.forever_skyblock.ascension_chamber"); }
    @Override public AbstractContainerMenu createMenu(int id, Inventory inventory, Player player) { return new AscensionMenu(id, inventory, this); }
}

package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import org.jetbrains.annotations.Nullable;
import net.neoforged.neoforge.items.IItemHandler;

/** Greenhouse display and secondary harvest port. All inventory and processing stay in the Condenser. */
public final class GardenChamberBlockEntity extends BlockEntity {
    // Stable capability identity; resolve the live controller for every operation, never cache its inventory.
    private final IItemHandler harvest = new IItemHandler() {
        @Override public int getSlots() { return 1; }
        @Override public int getSlotLimit(int slot) { return slot == 0 ? 64 : 0; }
        @Override public boolean isItemValid(int slot, ItemStack stack) { return false; }
        @Override public ItemStack insertItem(int slot, ItemStack stack, boolean simulate) { return stack; }
        @Override public ItemStack getStackInSlot(int slot) {
            var controller = harvestController(slot);
            return controller == null ? ItemStack.EMPTY
                    : controller.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY).copy();
        }
        @Override public ItemStack extractItem(int slot, int amount, boolean simulate) {
            var controller = harvestController(slot);
            return controller == null || amount <= 0 ? ItemStack.EMPTY
                    : controller.inventory().extractItem(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY, amount, simulate);
        }
    };

    public GardenChamberBlockEntity(BlockPos pos, BlockState state) {
        super(ForeverSkyblock.GARDEN_CHAMBER_ENTITY.get(), pos, state);
    }

    public @Nullable CondenserBlockEntity linkedCondenser() {
        if (level == null || isRemoved()) return null;
        CondenserBlockEntity fallback = null;
        for (Direction direction : Direction.values()) {
            if (!level.hasChunkAt(worldPosition.relative(direction))) continue;
            BlockEntity entity = level.getBlockEntity(worldPosition.relative(direction));
            if (entity instanceof CondenserBlockEntity condenser
                    && condenser.machineTier() >= 2
                    && condenser.hasGardenChamber()) {
                if (condenser.visualStatus() == CondenserBlockEntity.RUNNING) return condenser;
                if (fallback == null) fallback = condenser;
            }
        }
        return fallback;
    }

    public @Nullable IItemHandler automation(@Nullable Direction side) {
        return side == null || side == Direction.DOWN ? harvest : null;
    }

    private @Nullable CondenserBlockEntity harvestController(int slot) {
        if (slot != 0 || level == null || level.isClientSide || isRemoved()) return null;
        return linkedCondenser();
    }

    public boolean running() {
        CondenserBlockEntity condenser = linkedCondenser();
        return condenser != null && condenser.visualStatus() == CondenserBlockEntity.RUNNING;
    }
}

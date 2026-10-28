package de.tekatz.foreverskyblock.studio.runtime;

import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.Containers;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.EntityBlock;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.entity.BlockEntityTicker;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.resources.ResourceLocation;
import org.jetbrains.annotations.Nullable;

/** Generic registered block used by every runtime-capable machine emitted by Forever Studio. */
public final class ForeverStudioMachineBlock extends Block implements EntityBlock {
    private final ResourceLocation machineId;

    public ForeverStudioMachineBlock(ResourceLocation machineId, Properties properties) {
        super(properties);
        this.machineId = machineId;
    }

    public ResourceLocation machineId() { return machineId; }

    @Override public @Nullable BlockEntity newBlockEntity(BlockPos pos, BlockState state) {
        return new ForeverStudioMachineBlockEntity(pos, state);
    }

    @Override public <T extends BlockEntity> @Nullable BlockEntityTicker<T> getTicker(
            Level level, BlockState state, BlockEntityType<T> type) {
        if (level.isClientSide || type != de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.MACHINE_BLOCK_ENTITY.get())
            return null;
        return (tickerLevel, pos, tickerState, blockEntity) ->
                ForeverStudioMachineBlockEntity.tick(tickerLevel, pos, tickerState, (ForeverStudioMachineBlockEntity) blockEntity);
    }

    @Override protected InteractionResult useWithoutItem(
            BlockState state, Level level, BlockPos pos, Player player, BlockHitResult hit) {
        if (!level.isClientSide && player instanceof ServerPlayer serverPlayer
                && level.getBlockEntity(pos) instanceof ForeverStudioMachineBlockEntity machine) {
            machine.markPlayerUse();
            serverPlayer.openMenu(machine, buffer -> buffer.writeUtf(machine.machineId().toString(), 256));
        }
        return InteractionResult.sidedSuccess(level.isClientSide);
    }

    @Override protected void onRemove(BlockState state, Level level, BlockPos pos, BlockState newState, boolean moving) {
        if (!level.isClientSide && !state.is(newState.getBlock())
                && level.getBlockEntity(pos) instanceof ForeverStudioMachineBlockEntity machine) {
            for (int slot = 0; slot < machine.inventory().getSlots(); slot++) {
                ItemStackDrop.drop(level, pos, machine.inventory().getStackInSlot(slot));
            }
        }
        super.onRemove(state, level, pos, newState, moving);
    }

    private static final class ItemStackDrop {
        private static void drop(Level level, BlockPos pos, net.minecraft.world.item.ItemStack stack) {
            if (!stack.isEmpty()) Containers.dropItemStack(level, pos.getX(), pos.getY(), pos.getZ(), stack);
        }
    }
}

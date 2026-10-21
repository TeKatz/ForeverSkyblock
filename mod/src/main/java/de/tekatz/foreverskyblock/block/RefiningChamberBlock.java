package de.tekatz.foreverskyblock.block;

import com.mojang.serialization.MapCodec;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.RefiningChamberBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.network.chat.Component;
import net.minecraft.world.Containers;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.BaseEntityBlock;
import net.minecraft.world.level.block.RenderShape;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.entity.BlockEntityTicker;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.BlockHitResult;

public final class RefiningChamberBlock extends BaseEntityBlock {
    public static final MapCodec<RefiningChamberBlock> CODEC = simpleCodec(RefiningChamberBlock::new);
    public RefiningChamberBlock(Properties properties) { super(properties); }
    @Override protected MapCodec<? extends BaseEntityBlock> codec() { return CODEC; }
    @Override public Component getName() { return Component.literal("Refining Condenser"); }
    @Override protected RenderShape getRenderShape(BlockState state) { return RenderShape.MODEL; }
    @Override public BlockEntity newBlockEntity(BlockPos pos, BlockState state) { return new RefiningChamberBlockEntity(pos, state); }
    @Override public <T extends BlockEntity> BlockEntityTicker<T> getTicker(Level level, BlockState state, BlockEntityType<T> type) {
        return level.isClientSide ? null : createTickerHelper(type, ForeverSkyblock.REFINING_CHAMBER_ENTITY.get(), RefiningChamberBlockEntity::tick);
    }
    @Override protected InteractionResult useWithoutItem(BlockState state, Level level, BlockPos pos, Player player, BlockHitResult hit) {
        if (!level.isClientSide && level.getBlockEntity(pos) instanceof RefiningChamberBlockEntity chamber) player.openMenu(chamber);
        return InteractionResult.sidedSuccess(level.isClientSide);
    }
    @Override protected void onRemove(BlockState state, Level level, BlockPos pos, BlockState newState, boolean moving) {
        if (!level.isClientSide && !state.is(newState.getBlock()) && level.getBlockEntity(pos) instanceof RefiningChamberBlockEntity chamber) {
            for (int slot = 0; slot < chamber.inventory().getSlots(); slot++)
                Containers.dropItemStack(level, pos.getX(), pos.getY(), pos.getZ(), chamber.inventory().getStackInSlot(slot));
        }
        super.onRemove(state, level, pos, newState, moving);
    }
}

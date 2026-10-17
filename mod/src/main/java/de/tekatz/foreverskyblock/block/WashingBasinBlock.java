package de.tekatz.foreverskyblock.block;

import com.mojang.serialization.MapCodec;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import net.minecraft.core.BlockPos;
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

public final class WashingBasinBlock extends BaseEntityBlock {
    public static final MapCodec<WashingBasinBlock> CODEC = simpleCodec(WashingBasinBlock::new);

    public WashingBasinBlock(Properties properties) { super(properties); }

    @Override protected MapCodec<? extends BaseEntityBlock> codec() { return CODEC; }
    @Override protected RenderShape getRenderShape(BlockState state) { return RenderShape.MODEL; }
    @Override public BlockEntity newBlockEntity(BlockPos pos, BlockState state) { return new WashingBasinBlockEntity(pos, state); }
    @Override public <T extends BlockEntity> BlockEntityTicker<T> getTicker(Level level, BlockState state, BlockEntityType<T> type) {
        return level.isClientSide ? null : createTickerHelper(type, ForeverSkyblock.WASHING_BASIN_ENTITY.get(), WashingBasinBlockEntity::tick);
    }
    @Override protected InteractionResult useWithoutItem(BlockState state, Level level, BlockPos pos, Player player, BlockHitResult hit) {
        if (!level.isClientSide && level.getBlockEntity(pos) instanceof WashingBasinBlockEntity basin) player.openMenu(basin);
        return InteractionResult.sidedSuccess(level.isClientSide);
    }
    @Override protected void onRemove(BlockState state, Level level, BlockPos pos, BlockState newState, boolean moving) {
        if (!level.isClientSide && !state.is(newState.getBlock()) && level.getBlockEntity(pos) instanceof WashingBasinBlockEntity basin) {
            for (int slot = 0; slot < basin.inventory().getSlots(); slot++)
                Containers.dropItemStack(level, pos.getX(), pos.getY(), pos.getZ(), basin.inventory().getStackInSlot(slot));
        }
        super.onRemove(state, level, pos, newState, moving);
    }
}

package de.tekatz.foreverskyblock.block;

import com.mojang.serialization.MapCodec;
import de.tekatz.foreverskyblock.blockentity.GardenChamberBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.BaseEntityBlock;
import net.minecraft.world.level.block.RenderShape;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;

/** Passive greenhouse module. Processing remains owned by the adjacent Condenser Mark II. */
public final class GardenChamberBlock extends BaseEntityBlock {
    public static final MapCodec<GardenChamberBlock> CODEC = simpleCodec(GardenChamberBlock::new);

    public GardenChamberBlock(Properties properties) { super(properties); }

    @Override protected MapCodec<? extends BaseEntityBlock> codec() { return CODEC; }
    @Override protected RenderShape getRenderShape(BlockState state) { return RenderShape.MODEL; }
    @Override public BlockEntity newBlockEntity(BlockPos pos, BlockState state) { return new GardenChamberBlockEntity(pos, state); }
}

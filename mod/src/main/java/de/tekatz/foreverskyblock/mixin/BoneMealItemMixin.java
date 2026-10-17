package de.tekatz.foreverskyblock.mixin;

import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.BoneMealItem;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.SaplingBlock;
import net.minecraft.world.level.block.state.BlockState;
import org.jetbrains.annotations.Nullable;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** One Bone Meal use advances a placed sapling through both vanilla growth stages. */
@Mixin(BoneMealItem.class)
public abstract class BoneMealItemMixin {
    @Inject(method = "applyBonemeal", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$instantSaplingGrowth(ItemStack stack, Level level, BlockPos pos,
                                                               @Nullable Player player, CallbackInfoReturnable<Boolean> cir) {
        BlockState state = level.getBlockState(pos);
        if (!(state.getBlock() instanceof SaplingBlock sapling)) return;
        if (!sapling.isValidBonemealTarget(level, pos, state)) {
            cir.setReturnValue(false);
            return;
        }
        if (level instanceof ServerLevel server) {
            sapling.advanceTree(server, pos, state, server.getRandom());
            BlockState advanced = server.getBlockState(pos);
            if (advanced.getBlock() == sapling) sapling.advanceTree(server, pos, advanced, server.getRandom());
            stack.shrink(1);
        }
        cir.setReturnValue(true);
    }
}

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

/**
 * Removes the random Bone Meal wait from placed saplings.
 * One use primes the sapling and repeatedly asks the vanilla TreeGrower to generate a tree.
 * The bounded retries matter for growers such as Oak that can randomly choose a larger variant
 * which may not fit even when a normal tree would. Vanilla placement checks remain authoritative.
 */
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
            for (int attempt = 0; attempt < 32; attempt++) {
                BlockState current = server.getBlockState(pos);
                if (current.getBlock() != sapling) break;
                sapling.advanceTree(server, pos, current, server.getRandom());
            }
            stack.shrink(1);
        }
        cir.setReturnValue(true);
    }
}

package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.TechChallengeEvents;
import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.ItemInteractionResult;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.BlockHitResult;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Pseudo;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Pseudo
@Mixin(targets = "owmii.powah.block.energizing.EnergizingOrbBlock", remap = false)
public abstract class PowahEnergizingOrbMixin {
    @Inject(method = "useItemOn", at = @At("HEAD"))
    private void foreverSkyblock$takeCompletedOutput(ItemStack held, BlockState state, Level level, BlockPos pos,
            Player player, InteractionHand hand, BlockHitResult hit, CallbackInfoReturnable<ItemInteractionResult> callback) {
        if (player instanceof ServerPlayer serverPlayer && TechChallengeEvents.powahOrbHasCompletedOutput(level, pos))
            TechChallengeEvents.energizedOutputTaken(serverPlayer);
    }
}

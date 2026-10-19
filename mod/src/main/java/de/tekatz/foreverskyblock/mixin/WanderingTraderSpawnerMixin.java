package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.WorldQoLService;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.npc.WanderingTraderSpawner;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Stop the entire vanilla caravan attempt before it can independently create either llama. */
@Mixin(WanderingTraderSpawner.class)
public abstract class WanderingTraderSpawnerMixin {
    @Inject(method = "spawn", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$gateCaravan(ServerLevel level,
                                           CallbackInfoReturnable<Boolean> result) {
        if (!WorldQoLService.wanderingTraderAllowed(level.getServer())) result.setReturnValue(false);
    }
}

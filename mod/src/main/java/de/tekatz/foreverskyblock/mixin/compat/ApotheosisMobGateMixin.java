package de.tekatz.foreverskyblock.mixin.compat;

import de.tekatz.foreverskyblock.progression.ApotheosisProgressionService;
import net.neoforged.neoforge.event.entity.living.FinalizeSpawnEvent;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(targets = "dev.shadowsoffire.apotheosis.mobs.ApothMobEvents", remap = false)
public abstract class ApotheosisMobGateMixin {
    @Inject(method = "finalizeMobSpawns", at = @At("HEAD"), cancellable = true, remap = false)
    private void forever$blockApotheosisMobProcessing(FinalizeSpawnEvent event, CallbackInfo ci) {
        var server = event.getLevel().getLevel().getServer();
        if (server != null && ApotheosisProgressionService.dormant(server)) ci.cancel();
    }
}
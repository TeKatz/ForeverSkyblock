package de.tekatz.foreverskyblock.mixin.compat;

import de.tekatz.foreverskyblock.progression.ApotheosisProgressionService;
import net.neoforged.neoforge.event.entity.EntityJoinLevelEvent;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Prevent the native default Haven attachment from granting player or mob tier augments before Forever awakens Apotheosis. */
@Mixin(targets = "dev.shadowsoffire.apotheosis.AdventureEvents", remap = false)
public abstract class ApotheosisTierAugmentGateMixin {
    @Inject(method = "applyMissedTierAugments", at = @At("HEAD"), cancellable = true, remap = false)
    private void forever$blockDormantTierAugments(EntityJoinLevelEvent event, CallbackInfo ci) {
        if (!event.getLevel().isClientSide() && event.getLevel().getServer() != null
                && ApotheosisProgressionService.dormant(event.getLevel().getServer())) {
            ci.cancel();
        }
    }
}

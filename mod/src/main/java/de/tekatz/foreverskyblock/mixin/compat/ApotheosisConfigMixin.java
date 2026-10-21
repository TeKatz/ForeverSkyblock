package de.tekatz.foreverskyblock.mixin.compat;

import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Forever owns World Tier activation, so Apotheosis' manual tier switch must stay disabled. */
@Mixin(targets = "dev.shadowsoffire.apotheosis.AdventureConfig", remap = false)
public abstract class ApotheosisConfigMixin {
    @Shadow(remap = false) public static boolean enableManualWorldTierChanges;

    @Inject(method = "load", at = @At("TAIL"), remap = false)
    private static void forever$disableManualTierChanges(CallbackInfo ci) {
        enableManualWorldTierChanges = false;
    }
}

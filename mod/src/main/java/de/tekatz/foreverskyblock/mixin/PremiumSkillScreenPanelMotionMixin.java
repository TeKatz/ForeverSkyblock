package de.tekatz.foreverskyblock.mixin;

import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/**
 * Challenge/category panels are rebuilt when fresh server snapshots arrive. Their old per-panel birth animation made
 * a perfectly idle Challenges page visibly shift again seconds after opening it. Panel rebuilds must be position-stable.
 */
@Mixin(targets="de.tekatz.foreverskyblock.client.PremiumSkillScreen$MenuPanel")
public abstract class PremiumSkillScreenPanelMotionMixin {
    @Inject(method="entryOffset",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$keepRebuiltPanelsStationary(CallbackInfoReturnable<Integer> cir){cir.setReturnValue(0);}
}
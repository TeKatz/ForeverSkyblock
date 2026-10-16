package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.TechChallengeEvents;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Pseudo;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Pseudo
@Mixin(targets = "appeng.crafting.execution.CraftingCpuLogic", remap = false)
public abstract class Ae2CraftingCpuMixin {
    @Inject(method = "finishJob", at = @At("HEAD"))
    private void foreverSkyblock$completedPersonalJob(boolean success, CallbackInfo callback) {
        TechChallengeEvents.ae2AutocraftFinished(this, success);
    }
}

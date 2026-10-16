package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.TechChallengeEvents;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Pseudo;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Pseudo
@Mixin(targets = "mekanism.common.tile.machine.TileEntityDigitalMiner", remap = false)
public abstract class MekanismDigitalMinerMixin {
    @Inject(method = "setReplace", at = @At("RETURN"))
    private void foreverSkyblock$successfulMine(CallbackInfoReturnable<Boolean> callback) {
        if (callback.getReturnValueZ()) TechChallengeEvents.mekanismDigitalMinerMined(this);
    }
}

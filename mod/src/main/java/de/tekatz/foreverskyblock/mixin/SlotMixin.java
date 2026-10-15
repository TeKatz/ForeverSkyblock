package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.JetpackGate;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.ResultSlot;
import net.minecraft.world.inventory.Slot;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(Slot.class)
public abstract class SlotMixin {
    @Inject(method = "mayPickup", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$gate(Player player, CallbackInfoReturnable<Boolean> callback) {
        if ((Object) this instanceof ResultSlot && !JetpackGate.canUse(player, ((Slot) (Object) this).getItem()))
            callback.setReturnValue(false);
    }
}

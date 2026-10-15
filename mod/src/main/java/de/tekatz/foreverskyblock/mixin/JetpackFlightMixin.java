package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.JetpackGate;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Pseudo;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Pseudo
@Mixin(targets = "com.blakebr0.ironjetpacks.util.JetpackUtils", remap = false)
public abstract class JetpackFlightMixin {
    @Inject(method = "getEquippedJetpack", at = @At("RETURN"), cancellable = true)
    private static void foreverSkyblock$gate(Player player, CallbackInfoReturnable<ItemStack> callback) {
        if (!JetpackGate.canUse(player, callback.getReturnValue())) callback.setReturnValue(ItemStack.EMPTY);
    }
}

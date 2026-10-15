package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.JetpackGate;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.Slot;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Pseudo;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Coerce;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Pseudo
@Mixin(targets = "appeng.menu.slot.CraftingTermSlot", remap = false)
public abstract class Ae2CraftingSlotMixin {
    @Inject(method = "mayPickup", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$pickup(Player player, CallbackInfoReturnable<Boolean> callback) {
        if (!JetpackGate.canUse(player, ((Slot) (Object) this).getItem())) callback.setReturnValue(false);
    }
    @Inject(method = "craftItem", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$craft(Player player, @Coerce Object storage, @Coerce Object counter, CallbackInfoReturnable<ItemStack> callback) {
        if (!JetpackGate.canUse(player, ((Slot) (Object) this).getItem())) callback.setReturnValue(ItemStack.EMPTY);
    }
}

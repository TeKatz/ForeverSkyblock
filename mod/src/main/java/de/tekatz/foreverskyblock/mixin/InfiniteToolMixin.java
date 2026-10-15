package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.tools.ToolPolicy;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.ModifyVariable;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(ItemStack.class)
public abstract class InfiniteToolMixin {
    @Inject(method = "isDamageableItem", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$permanentTool(CallbackInfoReturnable<Boolean> callback) {
        if (ToolPolicy.includes((ItemStack) (Object) this)) callback.setReturnValue(false);
    }
    @Inject(method = "getDamageValue", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$ignoreOldWear(CallbackInfoReturnable<Integer> callback) {
        if (ToolPolicy.includes((ItemStack) (Object) this)) callback.setReturnValue(0);
    }
    @ModifyVariable(method = "setDamageValue", at = @At("HEAD"), argsOnly = true)
    private int foreverSkyblock$preventDirectDamage(int damage) {
        return ToolPolicy.includes((ItemStack) (Object) this) ? 0 : damage;
    }
}

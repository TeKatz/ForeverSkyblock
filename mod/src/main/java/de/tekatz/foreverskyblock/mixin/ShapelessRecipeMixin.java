package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.JetpackGate;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.ShapelessRecipe;
import net.minecraft.world.level.Level;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(ShapelessRecipe.class)
public abstract class ShapelessRecipeMixin {
    @Inject(method = "matches(Lnet/minecraft/world/item/crafting/CraftingInput;Lnet/minecraft/world/level/Level;)Z", at = @At("RETURN"), cancellable = true)
    private void foreverSkyblock$gate(CraftingInput input, Level level, CallbackInfoReturnable<Boolean> callback) {
        if (callback.getReturnValue() && !JetpackGate.canCraft(level, ((ShapelessRecipe) (Object) this).getResultItem(level.registryAccess())))
            callback.setReturnValue(false);
    }
}

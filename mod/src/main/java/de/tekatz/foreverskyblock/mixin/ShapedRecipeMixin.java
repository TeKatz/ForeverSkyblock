package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.JetpackGate;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.ShapedRecipe;
import net.minecraft.world.level.Level;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(ShapedRecipe.class)
public abstract class ShapedRecipeMixin {
    @Inject(method = "assemble(Lnet/minecraft/world/item/crafting/CraftingInput;Lnet/minecraft/core/HolderLookup$Provider;)Lnet/minecraft/world/item/ItemStack;", at = @At("RETURN"), cancellable = true)
    private void foreverSkyblock$coreIdentity(CraftingInput input, net.minecraft.core.HolderLookup.Provider registries,
                                             CallbackInfoReturnable<net.minecraft.world.item.ItemStack> callback) {
        var result = callback.getReturnValue();
        if (result.is(de.tekatz.foreverskyblock.ForeverSkyblock.FOREVER_CORE.get()))
            callback.setReturnValue(de.tekatz.foreverskyblock.item.ForeverCore.stamp(result));
    }
    @Inject(method = "matches(Lnet/minecraft/world/item/crafting/CraftingInput;Lnet/minecraft/world/level/Level;)Z", at = @At("RETURN"), cancellable = true)
    private void foreverSkyblock$gate(CraftingInput input, Level level, CallbackInfoReturnable<Boolean> callback) {
        if (callback.getReturnValue() && !JetpackGate.canCraft(level, ((ShapedRecipe) (Object) this).getResultItem(level.registryAccess())))
            callback.setReturnValue(false);
    }
}

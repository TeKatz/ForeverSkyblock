package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.JetpackGate;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.level.Level;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Pseudo;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Pseudo
@Mixin(targets = "appeng.crafting.pattern.AECraftingPattern", remap = false)
public abstract class Ae2PatternMixin {
    @Inject(method = "assemble", at = @At("RETURN"), cancellable = true)
    private void foreverSkyblock$gate(CraftingInput input, Level level, CallbackInfoReturnable<ItemStack> callback) {
        // Cached AE2 outputs cannot preserve a fresh Core identity for each operation.
        if (callback.getReturnValue().is(de.tekatz.foreverskyblock.ForeverSkyblock.FOREVER_CORE.get())
                || !JetpackGate.canCraft(level, callback.getReturnValue())) callback.setReturnValue(ItemStack.EMPTY);
    }
}

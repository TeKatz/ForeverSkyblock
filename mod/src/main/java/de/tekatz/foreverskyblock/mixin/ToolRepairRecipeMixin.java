package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.tools.ToolPolicy;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.RepairItemRecipe;
import net.minecraft.world.level.Level;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(RepairItemRecipe.class)
public abstract class ToolRepairRecipeMixin {
    @Inject(method = "matches(Lnet/minecraft/world/item/crafting/CraftingInput;Lnet/minecraft/world/level/Level;)Z", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$noPermanentToolRepair(CraftingInput input, Level level, CallbackInfoReturnable<Boolean> callback) {
        for (int i = 0; i < input.size(); i++) if (ToolPolicy.includes(input.getItem(i))) { callback.setReturnValue(false); return; }
    }
}

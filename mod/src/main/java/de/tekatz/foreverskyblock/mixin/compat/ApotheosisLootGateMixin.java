package de.tekatz.foreverskyblock.mixin.compat;

import de.tekatz.foreverskyblock.progression.ApotheosisProgressionService;
import it.unimi.dsi.fastutil.objects.ObjectArrayList;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.storage.loot.LootContext;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(targets = "dev.shadowsoffire.apotheosis.loot.modifiers.ContextualLootModifier", remap = false)
public abstract class ApotheosisLootGateMixin {
    @Inject(method = "doApply(Lit/unimi/dsi/fastutil/objects/ObjectArrayList;Lnet/minecraft/world/level/storage/loot/LootContext;)Lit/unimi/dsi/fastutil/objects/ObjectArrayList;",
            at = @At("HEAD"), cancellable = true, remap = false)
    private void forever$blockApotheosisLoot(ObjectArrayList<ItemStack> loot, LootContext context,
                                             CallbackInfoReturnable<ObjectArrayList<ItemStack>> cir) {
        var server = context.getLevel().getServer();
        if (server != null && ApotheosisProgressionService.dormant(server)) cir.setReturnValue(loot);
    }
}
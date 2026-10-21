package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.ApotheosisProgressionEvents;
import net.minecraft.tags.DamageTypeTags;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.item.Items;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(ItemEntity.class)
public abstract class DiamondSacrificeMixin {
    @Inject(method = "hurt", at = @At("RETURN"))
    private void forever$diamondBurned(DamageSource source, float amount, CallbackInfoReturnable<Boolean> cir) {
        ItemEntity self = (ItemEntity) (Object) this;
        if (!cir.getReturnValue() || !source.is(DamageTypeTags.IS_FIRE) || !self.isRemoved() || !self.getItem().is(Items.DIAMOND)) return;
        ApotheosisProgressionEvents.consumeDiamondSacrifice(self);
    }
}

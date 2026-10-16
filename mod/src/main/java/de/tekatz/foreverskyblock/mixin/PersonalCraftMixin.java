package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.ChallengeEvents;
import java.util.List;
import net.minecraft.advancements.CriteriaTriggers;
import net.minecraft.advancements.critereon.RecipeCraftedTrigger;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(RecipeCraftedTrigger.class)
public abstract class PersonalCraftMixin {
    @Inject(method = "trigger", at = @At("TAIL"))
    private void foreverSkyblock$personalCraft(ServerPlayer player, ResourceLocation recipe, List<ItemStack> ingredients, CallbackInfo callback) {
        if ((Object) this == CriteriaTriggers.RECIPE_CRAFTED) ChallengeEvents.crafted(player, recipe, ingredients);
    }
}

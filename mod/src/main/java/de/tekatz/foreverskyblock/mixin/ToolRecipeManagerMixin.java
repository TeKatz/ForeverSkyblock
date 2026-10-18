package de.tekatz.foreverskyblock.mixin;

import com.google.gson.JsonElement;
import de.tekatz.foreverskyblock.recipe.PackRecipes;
import java.util.Map;
import net.minecraft.core.HolderLookup;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.packs.resources.ResourceManager;
import net.minecraft.util.profiling.ProfilerFiller;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.item.crafting.RecipeManager;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.ModifyVariable;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(RecipeManager.class)
public abstract class ToolRecipeManagerMixin {
    @Shadow @Final private HolderLookup.Provider registries;
    @Inject(method = "apply(Ljava/util/Map;Lnet/minecraft/server/packs/resources/ResourceManager;Lnet/minecraft/util/profiling/ProfilerFiller;)V", at = @At("TAIL"))
    private void foreverSkyblock$removeAlternatives(Map<ResourceLocation, JsonElement> json, ResourceManager resources, ProfilerFiller profiler, CallbackInfo callback) {
        var manager = (RecipeManager) (Object) this;
        manager.replaceRecipes(manager.getRecipes());
    }
    @ModifyVariable(method = "replaceRecipes", at = @At("HEAD"), argsOnly = true)
    private Iterable<RecipeHolder<?>> foreverSkyblock$authoritativeTools(Iterable<RecipeHolder<?>> recipes) {
        return PackRecipes.filter(recipes, registries);
    }
}

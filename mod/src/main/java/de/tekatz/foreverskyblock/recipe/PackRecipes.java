package de.tekatz.foreverskyblock.recipe;

import de.tekatz.foreverskyblock.tools.ToolRecipes;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.HolderLookup;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.crafting.RecipeHolder;

/** Keep the pack's priced hopper recipes authoritative, including after /reload. */
public final class PackRecipes {
    public static final ResourceLocation HOPPER = ResourceLocation.parse("woodenhopper:wooden_hopper");
    public static final ResourceLocation HOPPER_RECIPE = ResourceLocation.parse("forever_skyblock:wooden_hopper");
    public static final ResourceLocation IRON_HOPPER = ResourceLocation.withDefaultNamespace("hopper");
    public static final ResourceLocation IRON_HOPPER_RECIPE = ResourceLocation.parse("forever_skyblock:hopper");
    private PackRecipes() { }
    public static List<RecipeHolder<?>> filter(Iterable<RecipeHolder<?>> recipes, HolderLookup.Provider registries) {
        var tools = ToolRecipes.filter(recipes, registries);
        // Optional dependency: never delete a third-party recipe if our conditional replacement is absent.
        boolean replacement = tools.stream().anyMatch(r -> r.id().equals(HOPPER_RECIPE));
        boolean ironReplacement = tools.stream().anyMatch(r -> r.id().equals(IRON_HOPPER_RECIPE));
        if (!replacement && !ironReplacement) return tools;
        var result = new ArrayList<RecipeHolder<?>>();
        for (var recipe : tools) {
            var item = recipe.value().getResultItem(registries);
            var output = BuiltInRegistries.ITEM.getKey(item.getItem());
            if (replacement && output.equals(HOPPER) && !recipe.id().equals(HOPPER_RECIPE)) continue;
            if (ironReplacement && output.equals(IRON_HOPPER) && !recipe.id().equals(IRON_HOPPER_RECIPE)) continue;
            result.add(recipe);
        }
        return result;
    }
}

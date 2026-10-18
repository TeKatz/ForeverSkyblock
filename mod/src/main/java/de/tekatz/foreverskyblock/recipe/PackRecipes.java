package de.tekatz.foreverskyblock.recipe;

import de.tekatz.foreverskyblock.tools.ToolRecipes;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.HolderLookup;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.crafting.RecipeHolder;

/** Keep the pack's priced Wooden Hopper authoritative, including after /reload. */
public final class PackRecipes {
    public static final ResourceLocation HOPPER = ResourceLocation.parse("woodenhopper:wooden_hopper");
    public static final ResourceLocation HOPPER_RECIPE = ResourceLocation.parse("forever_skyblock:wooden_hopper");
    private PackRecipes() { }
    public static List<RecipeHolder<?>> filter(Iterable<RecipeHolder<?>> recipes, HolderLookup.Provider registries) {
        var tools = ToolRecipes.filter(recipes, registries);
        // Optional dependency: never delete a third-party recipe if our conditional replacement is absent.
        boolean replacement = tools.stream().anyMatch(r -> r.id().equals(HOPPER_RECIPE));
        if (!replacement) return tools;
        var result = new ArrayList<RecipeHolder<?>>();
        for (var recipe : tools) {
            var item = recipe.value().getResultItem(registries);
            if (!BuiltInRegistries.ITEM.getKey(item.getItem()).equals(HOPPER) || recipe.id().equals(HOPPER_RECIPE))
                result.add(recipe);
        }
        return result;
    }
}

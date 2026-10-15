package de.tekatz.foreverskyblock.tools;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.HolderLookup;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.crafting.RecipeHolder;

/** Keep one authoritative recipe ID per tool, including mod/datapack alternatives at resource reload. */
public final class ToolRecipes {
    private ToolRecipes() { }
    public static boolean allowed(RecipeHolder<?> holder, HolderLookup.Provider registries) {
        var output = holder.value().getResultItem(registries);
        if (!ToolPolicy.includes(output)) return true;
        var item = BuiltInRegistries.ITEM.getKey(output.getItem());
        var intended = ResourceLocation.withDefaultNamespace(item.getPath() + (item.getPath().startsWith("netherite_") ? "_smithing" : ""));
        return holder.id().equals(intended);
    }
    public static List<RecipeHolder<?>> filter(Iterable<RecipeHolder<?>> recipes, HolderLookup.Provider registries) {
        var filtered = new ArrayList<RecipeHolder<?>>();
        for (var recipe : recipes) if (allowed(recipe, registries)) filtered.add(recipe);
        return filtered;
    }
}

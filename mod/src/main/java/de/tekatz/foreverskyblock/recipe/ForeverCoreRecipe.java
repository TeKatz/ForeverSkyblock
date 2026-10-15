package de.tekatz.foreverskyblock.recipe;

import com.mojang.serialization.MapCodec;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.item.ForeverCore;
import java.util.LinkedHashMap;
import java.util.ArrayList;
import net.minecraft.core.HolderLookup;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.*;

/** A shaped recipe with a fresh identity for every assembled Core, including automated crafting. */
public final class ForeverCoreRecipe extends ShapedRecipe {
    private final ShapedRecipe definition;
    public ForeverCoreRecipe(ShapedRecipe definition) {
        super(definition.getGroup(), definition.category(), pattern(definition), definition.getResultItem(null), definition.showNotification());
        this.definition = definition;
        if (!definition.getResultItem(null).is(ForeverSkyblock.FOREVER_CORE.get()) || definition.getResultItem(null).getCount() != 1)
            throw new IllegalArgumentException("Forever Core recipes must produce exactly one Core");
    }
    private static ShapedRecipePattern pattern(ShapedRecipe recipe) {
        var keys = new LinkedHashMap<Character, Ingredient>();
        var rows = new ArrayList<String>();
        for (int y = 0; y < recipe.getHeight(); y++) {
            StringBuilder row = new StringBuilder();
            for (int x = 0; x < recipe.getWidth(); x++) {
                int index = y * recipe.getWidth() + x;
                var ingredient = recipe.getIngredients().get(index);
                char key = ingredient.isEmpty() ? ' ' : (char) ('A' + index);
                if (key != ' ') keys.put(key, ingredient);
                row.append(key);
            }
            rows.add(row.toString());
        }
        return ShapedRecipePattern.of(keys, rows);
    }
    @Override public ItemStack assemble(CraftingInput input, HolderLookup.Provider registries) { return ForeverCore.create(); }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverSkyblock.FOREVER_CORE_SERIALIZER.get(); }
    public static final class Serializer implements RecipeSerializer<ForeverCoreRecipe> {
        private final ShapedRecipe.Serializer shaped = new ShapedRecipe.Serializer();
        @Override public MapCodec<ForeverCoreRecipe> codec() { return shaped.codec().xmap(ForeverCoreRecipe::new, value -> value.definition); }
        @Override public StreamCodec<RegistryFriendlyByteBuf, ForeverCoreRecipe> streamCodec() {
            return shaped.streamCodec().map(ForeverCoreRecipe::new, value -> value.definition);
        }
    }
}

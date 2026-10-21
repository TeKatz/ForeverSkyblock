package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.client.ApotheosisClientState;
import de.tekatz.foreverskyblock.progression.ApotheosisStage;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import mezz.jei.api.constants.RecipeTypes;
import mezz.jei.api.constants.VanillaTypes;
import mezz.jei.api.runtime.IJeiRuntime;
import net.minecraft.client.Minecraft;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.CraftingRecipe;
import net.minecraft.world.item.crafting.RecipeHolder;

/** Removes the entire Apotheosis item surface from JEI while Forever's pre-Nether hard lock is active. */
public final class ApotheosisJeiVisibility {
    private static final Set<String> NAMESPACES = Set.of("apotheosis", "apothic_attributes", "apothic_enchanting", "apothic_spawners");
    private static IJeiRuntime runtime;
    private static List<ItemStack> ingredients = List.of();
    private static List<RecipeHolder<CraftingRecipe>> recipes = List.of();
    private static final List<ItemStack> hiddenIngredients = new ArrayList<>();
    private static final List<RecipeHolder<CraftingRecipe>> hiddenRecipes = new ArrayList<>();

    private ApotheosisJeiVisibility() { }

    public static void available(IJeiRuntime value) {
        runtime = value;
        ingredients = value.getIngredientManager().getAllItemStacks().stream()
                .filter(ApotheosisJeiVisibility::belongsToApotheosis).map(ItemStack::copy).toList();
        var level = Minecraft.getInstance().level;
        recipes = level == null ? List.of() : value.getRecipeManager().createRecipeLookup(RecipeTypes.CRAFTING).get()
                .filter(recipe -> belongsToApotheosis(recipe.value().getResultItem(level.registryAccess()))).toList();
        hiddenIngredients.clear();
        hiddenRecipes.clear();
        refresh();
    }

    public static void unavailable() {
        runtime = null;
        ingredients = List.of();
        recipes = List.of();
        hiddenIngredients.clear();
        hiddenRecipes.clear();
    }

    public static void refresh() {
        if (runtime == null) return;
        boolean locked = ApotheosisClientState.stage() == ApotheosisStage.DORMANT;
        var manager = runtime.getIngredientManager();
        if (locked) {
            var hide = new ArrayList<ItemStack>();
            for (ItemStack stack : ingredients) if (!hiddenIngredients.contains(stack)) {
                hide.add(stack); hiddenIngredients.add(stack);
            }
            if (!hide.isEmpty()) manager.removeIngredientsAtRuntime(VanillaTypes.ITEM_STACK, hide);
            var hideRecipes = new ArrayList<RecipeHolder<CraftingRecipe>>();
            for (var recipe : recipes) if (!hiddenRecipes.contains(recipe)) {
                hideRecipes.add(recipe); hiddenRecipes.add(recipe);
            }
            if (!hideRecipes.isEmpty()) runtime.getRecipeManager().hideRecipes(RecipeTypes.CRAFTING, hideRecipes);
        } else {
            if (!hiddenIngredients.isEmpty()) manager.addIngredientsAtRuntime(VanillaTypes.ITEM_STACK, List.copyOf(hiddenIngredients));
            if (!hiddenRecipes.isEmpty()) runtime.getRecipeManager().unhideRecipes(RecipeTypes.CRAFTING, List.copyOf(hiddenRecipes));
            hiddenIngredients.clear();
            hiddenRecipes.clear();
        }
    }

    private static boolean belongsToApotheosis(ItemStack stack) {
        return stack != null && !stack.isEmpty() && NAMESPACES.contains(BuiltInRegistries.ITEM.getKey(stack.getItem()).getNamespace());
    }
}
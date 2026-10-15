package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.progression.JetpackGate;
import java.util.ArrayList;
import java.util.List;
import mezz.jei.api.constants.RecipeTypes;
import mezz.jei.api.constants.VanillaTypes;
import mezz.jei.api.runtime.IJeiRuntime;
import net.minecraft.client.Minecraft;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.CraftingRecipe;
import net.minecraft.world.item.crafting.RecipeHolder;

/** Retain component-bearing variants so removed entries can be restored without restarting JEI. */
public final class JetpackJeiVisibility {
    private static IJeiRuntime runtime;
    private static List<ItemStack> ingredients = List.of();
    private static List<RecipeHolder<CraftingRecipe>> recipes = List.of();
    private static final List<ItemStack> hiddenIngredients = new ArrayList<>();
    private static final List<RecipeHolder<CraftingRecipe>> hiddenRecipes = new ArrayList<>();
    private JetpackJeiVisibility() { }
    public static void available(IJeiRuntime value) {
        runtime = value;
        ingredients = value.getIngredientManager().getAllItemStacks().stream()
                .filter(s -> JetpackGate.requiredStage(s) > 0).map(ItemStack::copy).toList();
        var level = Minecraft.getInstance().level;
        recipes = level == null ? List.of() : value.getRecipeManager().createRecipeLookup(RecipeTypes.CRAFTING).get()
                .filter(r -> JetpackGate.requiredStage(r.value().getResultItem(level.registryAccess())) > 0).toList();
        hiddenIngredients.clear(); hiddenRecipes.clear();
        ClientSkills.onVisibilityChanged(JetpackJeiVisibility::refresh);
        refresh();
    }
    public static void unavailable() {
        runtime = null; ingredients = List.of(); recipes = List.of();
        hiddenIngredients.clear(); hiddenRecipes.clear();
        ClientSkills.onVisibilityChanged(() -> { });
    }
    public static void refresh() {
        if (runtime == null) return;
        var level = Minecraft.getInstance().level;
        if (level == null) return;
        int stage = ClientSkills.jetpackStage();
        var manager = runtime.getIngredientManager();
        var hide = new ArrayList<ItemStack>();
        var show = new ArrayList<ItemStack>();
        for (ItemStack stack : ingredients) {
            boolean locked = JetpackGate.requiredStage(stack) > stage;
            if (locked && !hiddenIngredients.contains(stack)) { hide.add(stack); hiddenIngredients.add(stack); }
            else if (!locked && hiddenIngredients.remove(stack)) show.add(stack);
        }
        if (!hide.isEmpty()) manager.removeIngredientsAtRuntime(VanillaTypes.ITEM_STACK, hide);
        if (!show.isEmpty()) manager.addIngredientsAtRuntime(VanillaTypes.ITEM_STACK, show);
        var hideRecipes = new ArrayList<RecipeHolder<CraftingRecipe>>();
        var showRecipes = new ArrayList<RecipeHolder<CraftingRecipe>>();
        for (var recipe : recipes) {
            boolean locked = JetpackGate.requiredStage(recipe.value().getResultItem(level.registryAccess())) > stage;
            if (locked && !hiddenRecipes.contains(recipe)) { hideRecipes.add(recipe); hiddenRecipes.add(recipe); }
            else if (!locked && hiddenRecipes.remove(recipe)) showRecipes.add(recipe);
        }
        if (!hideRecipes.isEmpty()) runtime.getRecipeManager().hideRecipes(RecipeTypes.CRAFTING, hideRecipes);
        if (!showRecipes.isEmpty()) runtime.getRecipeManager().unhideRecipes(RecipeTypes.CRAFTING, showRecipes);
    }
}

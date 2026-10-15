package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.client.CondenserScreen;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import mezz.jei.api.IModPlugin;
import mezz.jei.api.JeiPlugin;
import mezz.jei.api.recipe.RecipeType;
import mezz.jei.api.registration.IGuiHandlerRegistration;
import mezz.jei.api.registration.IRecipeCatalystRegistration;
import mezz.jei.api.registration.IRecipeCategoryRegistration;
import mezz.jei.api.registration.IRecipeRegistration;
import mezz.jei.api.registration.IRecipeTransferRegistration;
import net.minecraft.client.Minecraft;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.crafting.RecipeHolder;

@JeiPlugin
public final class CondenserJeiPlugin implements IModPlugin {
    public static final RecipeType<RecipeHolder<CondenserRecipe>> TYPE = RecipeType.createRecipeHolderType(
            ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "condensing"));
    @Override public ResourceLocation getPluginUid() { return ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "jei"); }
    @Override public void onRuntimeAvailable(mezz.jei.api.runtime.IJeiRuntime runtime) { JetpackJeiVisibility.available(runtime); }
    @Override public void onRuntimeUnavailable() { JetpackJeiVisibility.unavailable(); if (transfers != null) transfers.clearPending(); }
    @Override public void registerCategories(IRecipeCategoryRegistration registration) {
        registration.addRecipeCategories(new CondensingCategory(registration.getJeiHelpers().getGuiHelper()));
    }
    @Override public void registerRecipes(IRecipeRegistration registration) {
        var level = Minecraft.getInstance().level;
        if (level != null) registration.addRecipes(TYPE, level.getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get()));
    }
    @Override public void registerRecipeCatalysts(IRecipeCatalystRegistration registration) {
        registration.addRecipeCatalyst(ForeverSkyblock.CONDENSER_MARK_I.get(), TYPE);
    }
    private CondenserRecipeTransferHandler transfers;
    @Override public void registerRecipeTransferHandlers(IRecipeTransferRegistration registration) {
        transfers = new CondenserRecipeTransferHandler(registration.getTransferHelper());
        registration.addRecipeTransferHandler(transfers, TYPE);
    }
    @Override public void registerGuiHandlers(IGuiHandlerRegistration registration) {
        registration.addGhostIngredientHandler(CondenserScreen.class, new CondenserGhostHandler());
        registration.addRecipeClickArea(CondenserScreen.class, 77, 40, 38, 18, TYPE);
    }
}

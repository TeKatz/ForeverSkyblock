package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import mezz.jei.api.gui.builder.IRecipeLayoutBuilder;
import mezz.jei.api.gui.drawable.IDrawable;
import mezz.jei.api.gui.ingredient.IRecipeSlotsView;
import mezz.jei.api.helpers.IGuiHelper;
import mezz.jei.api.recipe.IFocusGroup;
import mezz.jei.api.recipe.RecipeType;
import mezz.jei.api.recipe.category.IRecipeCategory;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.crafting.RecipeHolder;

public final class CondensingCategory implements IRecipeCategory<RecipeHolder<CondenserRecipe>> {
    private final IDrawable icon;
    private final IDrawable slot;
    public CondensingCategory(IGuiHelper helper) {
        icon = helper.createDrawableItemLike(ForeverSkyblock.CONDENSER_MARK_I.get());
        slot = helper.getSlotDrawable();
    }
    @Override public RecipeType<RecipeHolder<CondenserRecipe>> getRecipeType() { return CondenserJeiPlugin.TYPE; }
    @Override public Component getTitle() { return Component.translatable("jei.forever_skyblock.condensing"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 150; }
    @Override public int getHeight() { return 68; }
    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<CondenserRecipe> holder, IFocusGroup focuses) {
        builder.addInputSlot(22, 10).addIngredients(holder.value().ingredient());
        builder.addOutputSlot(112, 10).addItemStack(holder.value().result());
    }
    @Override public void draw(RecipeHolder<CondenserRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF19242C);
        slot.draw(g, 21, 9);
        slot.draw(g, 111, 9);
        g.fill(51, 15, 98, 21, 0xFF46545C);
        int ticks = holder.value().processingTime();
        int width = (int) ((System.currentTimeMillis() / 50 % ticks) * 47 / ticks);
        g.fill(51, 15, 51 + width, 21, 0xFF36A9B8);
        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks / 20.0F), 75, 36, 0xFFE6EDF0);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.no_power"), 75, 51, 0xFFB0D7DD);
    }
}

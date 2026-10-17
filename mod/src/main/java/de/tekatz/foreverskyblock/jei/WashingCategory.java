package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.recipe.WashingRecipe;
import java.util.Arrays;
import java.util.List;
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
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.item.crafting.RecipeHolder;

public final class WashingCategory implements IRecipeCategory<RecipeHolder<WashingRecipe>> {
    private final IDrawable icon;
    private final IDrawable slot;

    public WashingCategory(IGuiHelper helper) {
        icon = helper.createDrawableItemLike(ForeverSkyblock.WASHING_BASIN.get());
        slot = helper.getSlotDrawable();
    }

    @Override public RecipeType<RecipeHolder<WashingRecipe>> getRecipeType() { return CondenserJeiPlugin.WASHING_TYPE; }
    @Override public Component getTitle() { return Component.translatable("jei.forever_skyblock.washing"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 150; }
    @Override public int getHeight() { return 90; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<WashingRecipe> holder, IFocusGroup focuses) {
        builder.addInputSlot(10, 10).addItemStacks(counted(holder.value().ingredient(), holder.value().ingredientCount()));
        builder.addOutputSlot(112, 10).addItemStack(holder.value().result());
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, 67)
                .addItemStack(new ItemStack(ForeverSkyblock.WASHING_BASIN.get()));
    }

    private static List<ItemStack> counted(Ingredient ingredient, int count) {
        return Arrays.stream(ingredient.getItems()).map(stack -> stack.copyWithCount(count)).toList();
    }

    @Override public void draw(RecipeHolder<WashingRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF19242C);
        slot.draw(g, 9, 9);
        slot.draw(g, 111, 9);
        g.fill(59, 15, 98, 21, 0xFF46545C);
        int ticks = holder.value().processingTime();
        int width = (int) ((System.currentTimeMillis() / 50 % ticks) * 39 / ticks);
        g.fill(59, 15, 59 + width, 21, 0xFF36A9B8);
        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks / 20.0F), 75, 36, 0xFFE6EDF0);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.no_power"), 75, 50, 0xFFB0D7DD);
        g.drawString(font, Component.translatable("jei.forever_skyblock.water_source_required"), 31, 67, 0xFF7FE8F2, false);
        g.drawString(font, Component.translatable("jei.forever_skyblock.water_not_consumed"), 31, 78, 0xFFB0D7DD, false);
    }
}

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
    @Override public int getWidth() { return 176; }
    @Override public int getHeight() { return 102; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<WashingRecipe> holder, IFocusGroup focuses) {
        WashingRecipe recipe = holder.value();
        builder.addInputSlot(10, 10).addItemStacks(counted(recipe.ingredient(), recipe.ingredientCount()));
        builder.addOutputSlot(106, 10).addItemStack(recipe.result());
        if (recipe.hasSecondaryResult()) builder.addOutputSlot(130, 10).addItemStack(recipe.secondaryResult());
        if (recipe.hasBonusResult()) builder.addOutputSlot(154, 10).addItemStack(recipe.bonusResult());
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, 69)
                .addItemStack(new ItemStack(ForeverSkyblock.WASHING_BASIN.get()));
    }

    private static List<ItemStack> counted(Ingredient ingredient, int count) {
        return Arrays.stream(ingredient.getItems()).map(stack -> stack.copyWithCount(count)).toList();
    }

    @Override public void draw(RecipeHolder<WashingRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        WashingRecipe recipe = holder.value();
        g.fill(0, 0, getWidth(), getHeight(), 0xFF19242C);
        slot.draw(g, 9, 9);
        slot.draw(g, 105, 9);
        if (recipe.hasSecondaryResult()) slot.draw(g, 129, 9);
        if (recipe.hasBonusResult()) slot.draw(g, 153, 9);
        slot.draw(g, 9, 68);
        g.fill(52, 15, 96, 21, 0xFF46545C);
        int ticks = recipe.processingTime();
        int width = (int) ((System.currentTimeMillis() / 50 % ticks) * 44 / ticks);
        g.fill(52, 15, 52 + width, 21, 0xFF36A9B8);
        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks / 20.0F), 74, 36, 0xFFE6EDF0);
        if (recipe.hasBonusResult())
            g.drawCenteredString(font, Component.literal(recipe.bonusChancePercent() + "% bonus"), 142, 36, 0xFFFFC76D);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.no_power"), 88, 50, 0xFFB0D7DD);
        g.drawString(font, Component.translatable("jei.forever_skyblock.water_source_required"), 34, 70, 0xFF7FE8F2, false);
        g.drawString(font, Component.translatable("jei.forever_skyblock.water_not_consumed"), 34, 84, 0xFFB0D7DD, false);
    }
}

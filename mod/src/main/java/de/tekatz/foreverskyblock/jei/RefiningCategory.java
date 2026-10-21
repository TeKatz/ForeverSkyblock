package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.recipe.RefiningRecipe;
import java.util.Arrays;
import java.util.List;
import mezz.jei.api.gui.builder.IRecipeLayoutBuilder;
import mezz.jei.api.gui.drawable.IDrawable;
import mezz.jei.api.gui.ingredient.IRecipeSlotsView;
import mezz.jei.api.helpers.IGuiHelper;
import mezz.jei.api.recipe.IFocusGroup;
import mezz.jei.api.recipe.category.IRecipeCategory;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.item.crafting.RecipeHolder;

public final class RefiningCategory implements IRecipeCategory<RecipeHolder<RefiningRecipe>> {
    private final IDrawable icon;
    private final IDrawable slot;
    public RefiningCategory(IGuiHelper helper) {
        icon = helper.createDrawableItemLike(ForeverSkyblock.REFINING_CHAMBER.get());
        slot = helper.getSlotDrawable();
    }
    @Override public mezz.jei.api.recipe.RecipeType<RecipeHolder<RefiningRecipe>> getRecipeType() { return CondenserJeiPlugin.REFINING_TYPE; }
    @Override public Component getTitle() { return Component.literal("Refining Condenser"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 176; }
    @Override public int getHeight() { return 82; }
    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<RefiningRecipe> holder, IFocusGroup focuses) {
        RefiningRecipe recipe = holder.value();
        builder.addInputSlot(10, 10).addItemStacks(counted(recipe.ingredient(), recipe.ingredientCount()));
        builder.addInputSlot(42, 10).addItemStacks(counted(recipe.catalyst(), recipe.catalystCount()));
        builder.addOutputSlot(138, 10).addItemStack(recipe.result());
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, 55).addItemStack(new ItemStack(ForeverSkyblock.REFINING_CHAMBER.get()));
    }
    private static List<ItemStack> counted(Ingredient ingredient, int count) {
        return Arrays.stream(ingredient.getItems()).map(stack -> stack.copyWithCount(count)).toList();
    }
    @Override public void draw(RecipeHolder<RefiningRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        RefiningRecipe recipe = holder.value();
        g.fill(0, 0, getWidth(), getHeight(), 0xFF19242C);
        slot.draw(g, 9, 9); slot.draw(g, 41, 9); slot.draw(g, 137, 9); slot.draw(g, 9, 54);
        g.fill(76, 15, 126, 21, 0xFF46545C);
        int ticks = recipe.processingTime();
        int width = (int)((System.currentTimeMillis()/50 % ticks) * 50 / ticks);
        g.fill(76, 15, 76 + width, 21, 0xFF8A6FE8);
        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks/20F), 101, 34, 0xFFE6EDF0);
        g.drawCenteredString(font, Component.literal(recipe.energyPerTick() + " FE/t"), 101, 48, 0xFFBFAFFF);
    }
}

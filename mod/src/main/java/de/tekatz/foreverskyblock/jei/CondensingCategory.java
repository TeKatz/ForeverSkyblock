package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
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
    @Override public int getWidth() { return 176; }
    @Override public int getHeight() { return 120; }
    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<CondenserRecipe> holder, IFocusGroup focuses) {
        builder.addInputSlot(10, 10).addItemStacks(counted(holder.value().ingredient(), holder.value().ingredientCount()));
        if (holder.value().requiresModule()) builder.addInputSlot(34, 10).addIngredients(holder.value().secondaryIngredient());
        builder.addOutputSlot(140, 10).addItemStack(holder.value().result());
        int catalystY = 69;
        if (holder.value().minimumTier() >= 2) {
            builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, catalystY)
                    .addItemStack(new ItemStack(ForeverSkyblock.CONDENSER_MARK_II.get()));
            catalystY += 20;
        }
        if (holder.value().requiresModule()) {
            builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, catalystY)
                    .addItemStack(new ItemStack(ForeverSkyblock.MIXING_CHAMBER_MARK_I.get()));
        }
    }
    private static List<ItemStack> counted(Ingredient ingredient, int count) {
        return Arrays.stream(ingredient.getItems()).map(stack -> stack.copyWithCount(count)).toList();
    }
    @Override public void draw(RecipeHolder<CondenserRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF19242C);
        slot.draw(g, 9, 9);
        slot.draw(g, 139, 9);
        if (holder.value().requiresModule()) slot.draw(g, 33, 9);
        g.fill(64, 15, 112, 21, 0xFF46545C);
        int ticks = holder.value().processingTime();
        int width = (int) ((System.currentTimeMillis() / 50 % ticks) * 48 / ticks);
        g.fill(64, 15, 64 + width, 21, 0xFF36A9B8);
        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks / 20.0F), 88, 36, 0xFFE6EDF0);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.no_power"), 88, 51, 0xFFB0D7DD);
        int y = 69;
        if (holder.value().minimumTier() >= 2) {
            slot.draw(g, 9, y - 1);
            g.drawString(font, Component.translatable("jei.forever_skyblock.tier_ii_required"), 34, y + 3, 0xFFFFCB69, false);
            y += 20;
        }
        if (holder.value().requiresModule()) {
            slot.draw(g, 9, y - 1);
            g.drawString(font, Component.translatable("jei.forever_skyblock.module_required"), 34, y + 2, 0xFFFFCB69, false);
            g.drawString(font, Component.translatable("jei.forever_skyblock.inputs_swappable"), 34, y + 13, 0xFFB0D7DD, false);
        }
    }
}

package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.recipe.GardenRecipe;
import java.util.Arrays;
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
import net.minecraft.world.item.crafting.RecipeHolder;

public final class GardenCategory implements IRecipeCategory<RecipeHolder<GardenRecipe>> {
    private final IDrawable icon;
    private final IDrawable slot;

    public GardenCategory(IGuiHelper helper) {
        icon = helper.createDrawableItemLike(ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        slot = helper.getSlotDrawable();
    }

    @Override public RecipeType<RecipeHolder<GardenRecipe>> getRecipeType() { return CondenserJeiPlugin.GARDEN_TYPE; }
    @Override public Component getTitle() { return Component.translatable("jei.forever_skyblock.garden"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 176; }
    @Override public int getHeight() { return 108; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<GardenRecipe> holder, IFocusGroup focuses) {
        builder.addInputSlot(10, 10).addItemStacks(Arrays.stream(holder.value().specimen().getItems()).map(ItemStack::copy).toList());
        builder.addOutputSlot(116, 10).addItemStack(holder.value().primaryResult());
        builder.addOutputSlot(144, 10).addItemStack(holder.value().secondaryResult());
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, 77)
                .addItemStack(new ItemStack(ForeverSkyblock.CONDENSER_MARK_II.get()));
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 34, 77)
                .addItemStack(new ItemStack(ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get()));
    }

    @Override public void draw(RecipeHolder<GardenRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF17271F);
        g.fill(42, 8, 106, 31, 0xFF315F48);
        g.fill(42, 8, 106, 18, 0xFF456F69);
        g.fill(42, 18, 106, 31, 0xFF365E36);
        slot.draw(g, 9, 9);
        slot.draw(g, 115, 9);
        slot.draw(g, 143, 9);
        slot.draw(g, 9, 76);
        slot.draw(g, 33, 76);
        g.fill(48, 40, 128, 46, 0xFF40584A);
        int ticks = holder.value().processingTime();
        int width = (int) ((System.currentTimeMillis() / 50 % ticks) * 80 / ticks);
        g.fill(48, 40, 48 + width, 46, 0xFF62C46B);
        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks / 20.0F), 88, 53, 0xFFE6F0E8);
        g.drawString(font, Component.translatable("jei.forever_skyblock.garden_specimen_retained"), 10, 65, 0xFF9FE0A8, false);
        g.drawString(font, Component.translatable("jei.forever_skyblock.garden_requires"), 58, 80, 0xFFC8DCCB, false);
        g.drawString(font, Component.translatable("jei.forever_skyblock.garden_exclusive"), 10, 96, 0xFF8FA99A, false);
    }
}

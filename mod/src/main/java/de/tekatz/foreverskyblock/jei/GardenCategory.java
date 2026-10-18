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
    @Override public int getHeight() { return 92; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<GardenRecipe> holder, IFocusGroup focuses) {
        builder.addInputSlot(12, 18).addItemStacks(Arrays.stream(holder.value().specimen().getItems()).map(ItemStack::copy).toList());
        builder.addOutputSlot(118, 18).addItemStack(holder.value().primaryResult());
        builder.addOutputSlot(146, 18).addItemStack(holder.value().secondaryResult());
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 12, 67)
                .addItemStack(new ItemStack(ForeverSkyblock.CONDENSER_MARK_II.get()));
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 36, 67)
                .addItemStack(new ItemStack(ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get()));
    }

    @Override public void draw(RecipeHolder<GardenRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF17231D);
        g.fill(1, 1, getWidth() - 1, getHeight() - 1, 0xFF1D2B24);
        slot.draw(g, 11, 17);
        slot.draw(g, 117, 17);
        slot.draw(g, 145, 17);
        slot.draw(g, 11, 66);
        slot.draw(g, 35, 66);

        // JEI stays schematic: no decorative tree-growth scene, just a clear process arrow.
        g.fill(49, 25, 104, 29, 0xFF43594B);
        g.fill(49, 25, 92, 29, 0xFF62C46B);
        g.fill(103, 22, 107, 32, 0xFF62C46B);
        g.fill(107, 24, 111, 30, 0xFF62C46B);

        var font = Minecraft.getInstance().font;
        int ticks = holder.value().processingTime();
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.duration", ticks / 20.0F), 80, 39, 0xFFE6F0E8);
        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.garden_specimen_retained"), 88, 52, 0xFFA7D8AE);
        g.drawString(font, Component.translatable("jei.forever_skyblock.garden_mk2"), 61, 66, 0xFFC8DCCB, false);
        g.drawString(font, Component.translatable("jei.forever_skyblock.garden_chamber"), 61, 78, 0xFF91B89A, false);
    }
}

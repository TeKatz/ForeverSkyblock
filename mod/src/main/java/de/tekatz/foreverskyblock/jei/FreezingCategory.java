package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
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
import net.minecraft.world.item.Items;

public final class FreezingCategory implements IRecipeCategory<FreezingCategory.Process> {
    public enum Process { ICE }
    public static final RecipeType<Process> TYPE = RecipeType.create(ForeverSkyblock.MOD_ID, "freezing", Process.class);

    private final IDrawable icon;
    private final IDrawable slot;

    public FreezingCategory(IGuiHelper helper) {
        icon = helper.createDrawableItemLike(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get());
        slot = helper.getSlotDrawable();
    }

    @Override public RecipeType<Process> getRecipeType() { return TYPE; }
    @Override public Component getTitle() { return Component.translatable("jei.forever_skyblock.freezing"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 150; }
    @Override public int getHeight() { return 90; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, Process recipe, IFocusGroup focuses) {
        // Do not register every item in the pack as a JEI input: the machine intentionally accepts all items,
        // and doing so would pollute every item's Uses page. The visual input slot below communicates the wildcard.
        builder.addOutputSlot(112, 10).addItemStack(new ItemStack(Items.ICE));
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, 67)
                .addItemStack(new ItemStack(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get()));
    }

    @Override public void draw(Process recipe, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF19242C);
        slot.draw(g, 9, 9);
        slot.draw(g, 111, 9);

        var font = Minecraft.getInstance().font;
        g.drawCenteredString(font, Component.literal("?"), 18, 14, 0xFFE6EDF0);
        g.drawString(font, Component.literal("×32"), 31, 15, 0xFFB0D7DD, false);

        g.fill(59, 15, 98, 21, 0xFF46545C);
        int width = (int) ((System.currentTimeMillis() / 50 % 32) * 39 / 32);
        g.fill(59, 15, 59 + width, 21, 0xFF36A9B8);

        g.drawCenteredString(font, Component.translatable("jei.forever_skyblock.freezing_items"), 75, 36, 0xFFE6EDF0);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.no_power"), 75, 51, 0xFFB0D7DD);
        g.drawString(font, Component.translatable("jei.forever_skyblock.module_required"), 31, 67, 0xFFFFCB69, false);
        g.drawString(font, Component.translatable("jei.forever_skyblock.freezing_any_item"), 31, 78, 0xFFB0D7DD, false);
    }
}

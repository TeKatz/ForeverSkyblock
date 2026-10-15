package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import mezz.jei.api.gui.builder.IRecipeLayoutBuilder;
import mezz.jei.api.gui.drawable.IDrawable;
import mezz.jei.api.gui.ingredient.IRecipeSlotsView;
import mezz.jei.api.helpers.IGuiHelper;
import mezz.jei.api.recipe.*;
import mezz.jei.api.recipe.category.IRecipeCategory;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;

public final class AscensionCategory implements IRecipeCategory<AscensionCategory.Conversion> {
    public enum Conversion { CORE }
    public static final RecipeType<Conversion> TYPE = RecipeType.create(ForeverSkyblock.MOD_ID, "ascension", Conversion.class);
    private final IDrawable icon;
    public AscensionCategory(IGuiHelper helper) { icon = helper.createDrawableItemLike(ForeverSkyblock.ASCENSION_CHAMBER.get()); }
    @Override public RecipeType<Conversion> getRecipeType() { return TYPE; }
    @Override public Component getTitle() { return Component.translatable("block.forever_skyblock.ascension_chamber"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 160; }
    @Override public int getHeight() { return 76; }
    @Override public void setRecipe(IRecipeLayoutBuilder builder, Conversion recipe, IFocusGroup focuses) {
        builder.addInputSlot(12, 12).addItemStack(new ItemStack(ForeverSkyblock.FOREVER_CORE.get()));
    }
    @Override public void draw(Conversion recipe, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        var font = Minecraft.getInstance().font;
        g.drawString(font, Component.literal("→  +1 PP"), 40, 17, 0xFFC59DFF, false);
        int y = 39;
        for (var line : font.split(Component.translatable("jei.forever_skyblock.ascension"), 148)) {
            g.drawString(font, line, 6, y, 0xFFE0D2EF, false); y += 10;
        }
    }
}

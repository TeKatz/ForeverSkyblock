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
    @Override public int getHeight() { return 104; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, RecipeHolder<RefiningRecipe> holder, IFocusGroup focuses) {
        RefiningRecipe recipe = holder.value();
        builder.addInputSlot(10, 27).addItemStacks(counted(recipe.ingredient(), recipe.ingredientCount()));
        builder.addInputSlot(42, 27).addItemStacks(counted(recipe.catalyst(), recipe.catalystCount()));
        builder.addOutputSlot(138, 27).addItemStack(recipe.result());
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 10, 80).addItemStack(new ItemStack(ForeverSkyblock.REFINING_CHAMBER.get()));
    }
    private static List<ItemStack> counted(Ingredient ingredient, int count) {
        return Arrays.stream(ingredient.getItems()).map(stack -> stack.copyWithCount(count)).toList();
    }

    @Override public void draw(RecipeHolder<RefiningRecipe> holder, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        RefiningRecipe recipe = holder.value();
        var font = Minecraft.getInstance().font;
        int ticks = Math.max(1, recipe.processingTime());
        int energyPerTick = recipe.energyPerTick();
        long totalEnergy = (long) ticks * energyPerTick;

        g.fill(0, 0, getWidth(), getHeight(), 0xFF111A22);
        g.fill(1, 1, getWidth() - 1, getHeight() - 1, 0xFF22303A);
        g.fill(4, 4, getWidth() - 4, 20, 0xFF334653);
        g.fill(4, 23, getWidth() - 4, 67, 0xFF192731);
        g.fill(4, 71, getWidth() - 4, 100, 0xFF2A3540);

        centered(g, Component.literal("MATERIAL"), 19, 11, 43, 0xFF9EB0BA, .62F);
        centered(g, Component.literal("CATALYST"), 51, 11, 48, 0xFF9EB0BA, .62F);
        centered(g, Component.literal("OUTPUT"), 147, 11, 43, 0xFF9EB0BA, .62F);

        slot.draw(g, 9, 26);
        slot.draw(g, 41, 26);
        slot.draw(g, 137, 26);
        slot.draw(g, 9, 79);

        g.fill(76, 31, 126, 39, 0xFF3D4B56);
        int width = (int)((System.currentTimeMillis() / 50 % ticks) * 48 / ticks);
        if (width > 0) g.fill(77, 32, 77 + width, 38, 0xFF8A6FE8);
        g.fill(127, 34, 134, 36, 0xFF8797A1);
        g.fill(132, 32, 134, 38, 0xFF8797A1);
        centered(g, Component.literal("REFINE"), 101, 45, 52, 0xFFE2D9FF, .67F);
        centered(g, Component.literal(String.format(java.util.Locale.ROOT, "%.1f s", ticks / 20F)), 101, 56, 56, 0xFFB8C7CF, .67F);

        g.drawString(font, Component.literal("Machine"), 31, 84, 0xFFAEBCC4, false);
        centered(g, Component.literal(energyPerTick + " FE/t"), 105, 79, 72, 0xFFC6B5FF, .72F);
        centered(g, Component.literal(formatEnergy(totalEnergy) + " total"), 105, 90, 86, 0xFF9DABB4, .62F);
    }

    private static String formatEnergy(long energy) {
        if (energy >= 1_000_000) return String.format(java.util.Locale.ROOT, "%.2f MFE", energy / 1_000_000D);
        if (energy >= 1_000) return String.format(java.util.Locale.ROOT, "%.1f kFE", energy / 1_000D);
        return energy + " FE";
    }
    private static void centered(GuiGraphics g, Component text, int centerX, int y, int maxWidth, int color, float maximumScale) {
        var font = Minecraft.getInstance().font;
        float scale = Math.min(maximumScale, (float) maxWidth / Math.max(1, font.width(text)));
        g.pose().pushPose();
        g.pose().translate(centerX, y, 0);
        g.pose().scale(scale, scale, 1.0F);
        g.drawString(font, text, -font.width(text) / 2, 0, color, false);
        g.pose().popPose();
    }
}

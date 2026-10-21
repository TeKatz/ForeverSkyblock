package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
import mezz.jei.api.gui.builder.IRecipeLayoutBuilder;
import mezz.jei.api.gui.drawable.IDrawable;
import mezz.jei.api.gui.ingredient.IRecipeSlotsView;
import mezz.jei.api.helpers.IGuiHelper;
import mezz.jei.api.recipe.IFocusGroup;
import mezz.jei.api.recipe.RecipeIngredientRole;
import mezz.jei.api.recipe.RecipeType;
import mezz.jei.api.recipe.category.IRecipeCategory;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

public final class CrucibleCategory implements IRecipeCategory<CrucibleCategory.Process> {
    public enum Process { COBBLE_LAVA, FRAGMENTS_LAVA, NETHERRACK, OBSIDIAN }
    public static final RecipeType<Process> TYPE = RecipeType.create(ForeverSkyblock.MOD_ID, "crucible", Process.class);
    private final IDrawable icon;
    private final IDrawable slot;
    public CrucibleCategory(IGuiHelper helper) {
        icon = helper.createDrawableItemLike(ForeverSkyblock.CRUCIBLE.get());
        slot = helper.getSlotDrawable();
    }
    @Override public RecipeType<Process> getRecipeType() { return TYPE; }
    @Override public Component getTitle() { return Component.literal("Crucible Melting"); }
    @Override public IDrawable getIcon() { return icon; }
    @Override public int getWidth() { return 160; }
    @Override public int getHeight() { return 104; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, Process recipe, IFocusGroup focuses) {
        if (recipe == Process.COBBLE_LAVA) {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(Items.COBBLESTONE, CrucibleBlockEntity.COBBLE_PER_BATCH));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.LAVA_BUCKET));
        } else if (recipe == Process.FRAGMENTS_LAVA) {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), CrucibleBlockEntity.FRAGMENTS_PER_BATCH));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.LAVA_BUCKET));
        } else if (recipe == Process.NETHERRACK) {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(Items.GRAVEL));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.NETHERRACK));
        } else {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(Items.LAVA_BUCKET));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.OBSIDIAN));
            builder.addSlot(RecipeIngredientRole.CATALYST, 126, 83).addItemStack(new ItemStack(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get()));
        }
        builder.addSlot(RecipeIngredientRole.CATALYST, 14, 83).addItemStack(new ItemStack(ForeverSkyblock.CRUCIBLE.get()));
    }

    @Override public void draw(Process recipe, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF241D19);
        slot.draw(g, 13, 11); slot.draw(g, 125, 11);
        int track = recipe == Process.OBSIDIAN ? 0xFF40565A : 0xFF59463B;
        int fill = recipe == Process.OBSIDIAN ? 0xFF50D4DE : 0xFFFF6A20;
        g.fill(55, 17, 105, 23, track); g.fill(55, 17, 91, 23, fill);
        if (recipe == Process.COBBLE_LAVA) {
            centeredFit(g, Component.literal("8 Cobblestone → 1000 mB lava"), 80, 37, 148, 0xFFFFC58D, 1.0F);
            centeredFit(g, Component.literal("Torch 300s · Campfire 80s"), 80, 51, 148, 0xFFB9C0C4, 1.0F);
            centeredFit(g, Component.literal("Lava 30s · Superheat 8s"), 80, 62, 148, 0xFFB9C0C4, 1.0F);
        } else if (recipe == Process.FRAGMENTS_LAVA) {
            centeredFit(g, Component.literal("40 Stone Fragments → 1000 mB lava"), 80, 37, 148, 0xFFFFC58D, 1.0F);
            centeredFit(g, Component.literal("Torch 300s · Campfire 80s"), 80, 51, 148, 0xFFB9C0C4, 1.0F);
            centeredFit(g, Component.literal("Lava 30s · Superheat 8s"), 80, 62, 148, 0xFFB9C0C4, 1.0F);
        } else if (recipe == Process.NETHERRACK) {
            centeredFit(g, Component.literal("1 Gravel + 250 mB lava → Netherrack"), 80, 37, 148, 0xFFFFC58D, .95F);
            centeredFit(g, Component.literal("Uses stored lava · 4s"), 80, 52, 148, 0xFFB9C0C4, 1.0F);
            centeredFit(g, Component.literal("Fully renewable Nether material"), 80, 63, 148, 0xFF9EADB5, .92F);
        } else {
            centeredFit(g, Component.literal("1000 mB lava → 1 Obsidian"), 80, 37, 148, 0xFFB9EDF2, 1.0F);
            centeredFit(g, Component.literal("Adjacent Freezing Chamber · 5s"), 80, 52, 148, 0xFFB9D9E5, 1.0F);
            centeredFit(g, Component.literal("Uses the Crucible's stored lava"), 80, 63, 148, 0xFF9EADB5, .92F);
        }
        var font = Minecraft.getInstance().font;
        g.drawString(font, Component.literal("Crucible"), 35, 87, 0xFFB0B7BC, false);
        if (recipe == Process.OBSIDIAN) centeredFit(g, Component.literal("Freezing"), 108, 87, 34, 0xFFB0D7DD, .82F);
    }
    private static void centeredFit(GuiGraphics g, Component text, int centerX, int y, int maxWidth, int color, float maximumScale) {
        var font = Minecraft.getInstance().font;
        float scale = Math.min(maximumScale, (float) maxWidth / Math.max(1, font.width(text)));
        g.pose().pushPose(); g.pose().translate(centerX, y, 0); g.pose().scale(scale, scale, 1.0F);
        g.drawString(font, text, -font.width(text) / 2, 0, color, false); g.pose().popPose();
    }
}

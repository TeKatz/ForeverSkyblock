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

public final class CrucibleCategory implements IRecipeCategory<CrucibleCategory.Process> {
    public enum Process { COBBLE_LAVA, FRAGMENTS_LAVA, OBSIDIAN }
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
    @Override public int getHeight() { return 96; }

    @Override public void setRecipe(IRecipeLayoutBuilder builder, Process recipe, IFocusGroup focuses) {
        if (recipe == Process.COBBLE_LAVA) {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(Items.COBBLESTONE));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.LAVA_BUCKET));
        } else if (recipe == Process.FRAGMENTS_LAVA) {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.LAVA_BUCKET));
        } else {
            builder.addInputSlot(14, 12).addItemStack(new ItemStack(Items.LAVA_BUCKET));
            builder.addOutputSlot(126, 12).addItemStack(new ItemStack(Items.OBSIDIAN));
            builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 76, 67)
                    .addItemStack(new ItemStack(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get()));
        }
        builder.addSlot(mezz.jei.api.recipe.RecipeIngredientRole.CATALYST, 14, 67)
                .addItemStack(new ItemStack(ForeverSkyblock.CRUCIBLE.get()));
    }

    @Override public void draw(Process recipe, IRecipeSlotsView slots, GuiGraphics g, double mouseX, double mouseY) {
        g.fill(0, 0, getWidth(), getHeight(), 0xFF241D19);
        slot.draw(g, 13, 11);
        slot.draw(g, 125, 11);
        var font = Minecraft.getInstance().font;
        g.fill(55, 17, 105, 23, 0xFF59463B);
        g.fill(55, 17, 91, 23, 0xFFFF6A20);
        Component middle = recipe == Process.OBSIDIAN ? Component.literal("1000 mB + Freezing") : Component.literal("→ 1000 mB lava");
        g.drawCenteredString(font, middle, 80, 38, 0xFFFFC58D);
        if (recipe != Process.OBSIDIAN) {
            g.drawCenteredString(font, Component.literal("Torch 300s · Campfire 80s · Lava 30s · Superheat 8s"), 80, 51, 0xFFB9C0C4);
        } else {
            g.drawCenteredString(font, Component.literal("Adjacent Freezing Chamber converts stored lava"), 80, 51, 0xFFB9D9E5);
        }
        g.drawString(font, Component.literal("Machine"), 34, 71, 0xFFB0B7BC, false);
        if (recipe == Process.OBSIDIAN) g.drawString(font, Component.literal("Freezing"), 96, 71, 0xFFB0D7DD, false);
    }
}

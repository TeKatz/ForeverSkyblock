package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.menu.MachineLayout;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.inventory.AbstractContainerScreen;
import net.minecraft.client.renderer.Rect2i;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.inventory.AbstractContainerMenu;

/**
 * Shared base for all custom machine container screens.
 *
 * IMPORTANT RENDERING CONTRACT:
 * Machine GUIs intentionally stay in AbstractContainerScreen's native coordinate system.
 * Do not apply a per-screen PoseStack scale/translation here and do not remap mouse coordinates.
 * Vanilla owns leftPos/topPos, slot rendering, carried-item rendering, hover detection and clicks.
 * Custom slot frames are drawn from the same leftPos/topPos origin, so frames and ItemStacks always
 * use one coordinate space even after a window resize or Minecraft GUI-scale change.
 */
public abstract class MachineScreen<M extends AbstractContainerMenu> extends AbstractContainerScreen<M> {
    protected final MachineLayout layout;
    protected final MachineSkin skin;
    private final int machineSlots;

    protected MachineScreen(M menu, Inventory inventory, Component title, MachineLayout layout, boolean ascension, int machineSlots) {
        super(menu, inventory, title);
        this.layout = layout;
        this.skin = new MachineSkin(ascension);
        this.machineSlots = machineSlots;
        imageWidth = layout.width();
        imageHeight = layout.height();
    }

    /** Absolute screen-space rectangle for JEI/ghost targets, using the exact same origin as Vanilla slots. */
    public Rect2i screenArea(int relativeX, int relativeY, int w, int h) {
        return new Rect2i(leftPos + relativeX, topPos + relativeY, w, h);
    }

    @Override
    public void render(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        renderBackground(g, mouseX, mouseY, partialTick);
        super.render(g, mouseX, mouseY, partialTick);
        renderTooltip(g, mouseX, mouseY);
        if (hoveredSlot == null || !hoveredSlot.hasItem()) renderMachineTooltip(g, mouseX, mouseY);
    }

    protected void renderMachineTooltip(GuiGraphics g, int x, int y) { }

    @Override
    protected boolean isHovering(int x, int y, int w, int h, double mx, double my) {
        if (w == 16 && h == 16) {
            for (int i = 0; i < machineSlots; i++) {
                if (menu.getSlot(i).x == x && menu.getSlot(i).y == y) {
                    return super.isHovering(x - 4, y - 4, 24, 24, mx, my);
                }
            }
        }
        return super.isHovering(x, y, w, h, mx, my);
    }

    protected void inventory(GuiGraphics g) {
        MachineSkin.panel(g, leftPos + 18, topPos + layout.inventoryY() - 6, 212, layout.hotbarY() - layout.inventoryY() + 28, 0xFF405866);
        g.fill(leftPos + 24, topPos + layout.hotbarY() - 6, leftPos + imageWidth - 24, topPos + layout.hotbarY() - 5, 0xFF344C5B);
        for (int row = 0; row < 3; row++) {
            for (int col = 0; col < 9; col++) {
                skin.slot(g, leftPos + layout.inventoryX(col), topPos + layout.inventoryRowY(row), false);
            }
        }
        for (int col = 0; col < 9; col++) {
            skin.slot(g, leftPos + layout.inventoryX(col), topPos + layout.hotbarY(), false);
        }
    }

    @Override
    protected void renderLabels(GuiGraphics g, int mouseX, int mouseY) { }
}

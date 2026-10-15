package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.inventory.AbstractContainerScreen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;

public final class CondenserScreen extends AbstractContainerScreen<CondenserMenu> {
    public CondenserScreen(CondenserMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title);
        imageWidth = 176;
        imageHeight = 190;
        inventoryLabelY = 94;
    }
    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        g.fill(x, y, x + imageWidth, y + imageHeight, 0xFF10171C);
        g.fill(x + 1, y + 1, x + imageWidth - 1, y + imageHeight - 1, 0xFF66757C);
        g.fill(x + 3, y + 3, x + imageWidth - 3, y + imageHeight - 3, 0xFF28333B);
        g.fill(x + 7, y + 23, x + 169, y + 91, 0xFF19242C);
        slot(g, x + 44, y + 37);
        slot(g, x + 116, y + 37);
        g.fill(x + 72, y + 41, x + 104, y + 49, 0xFF3D505B);
        int width = Math.min(32, menu.progress() * 32 / menu.duration());
        g.fill(x + 72, y + 41, x + 72 + width, y + 49, 0xFF49C7D4);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.input"), x + 52, y + 26, 0xFFA8B7BE);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.output"), x + 124, y + 26, 0xFFA8B7BE);
        String key = switch (menu.status()) {
            case CondenserBlockEntity.RUNNING -> "processing";
            case CondenserBlockEntity.BLOCKED -> "blocked";
            default -> "idle";
        };
        int color = menu.status() == CondenserBlockEntity.BLOCKED ? 0xFFFFC16E : 0xFF9FE3E9;
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock." + key), x + 88, y + 62, color);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.no_power"), x + 88, y + 77, 0xFF99A9B1);
        if (menu.status() == CondenserBlockEntity.RUNNING) {
            int dot = (int) (System.currentTimeMillis() / 160 % 4);
            for (int i = 0; i < 4; i++) g.fill(x + 77 + i * 6, y + 54, x + 80 + i * 6, y + 57,
                    i == dot ? 0xFFAFFAFF : 0xFF35606A);
        }
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++) slot(g, x + 8 + col * 18, y + 106 + row * 18);
        for (int col = 0; col < 9; col++) slot(g, x + 8 + col * 18, y + 164);
    }
    private static void slot(GuiGraphics g, int x, int y) {
        g.fill(x - 1, y - 1, x + 17, y + 17, 0xFF10171C);
        g.fill(x, y, x + 17, y + 17, 0xFF687981);
        g.fill(x, y, x + 16, y + 16, 0xFF34434D);
    }
    @Override protected void renderLabels(GuiGraphics g, int mouseX, int mouseY) {
        g.drawString(font, title, titleLabelX, titleLabelY, 0xFFE3EDF0, false);
        g.drawString(font, playerInventoryTitle, inventoryLabelX, inventoryLabelY, 0xFFB9C8CE, false);
    }
    @Override public void render(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        super.render(g, mouseX, mouseY, partialTick);
        renderTooltip(g, mouseX, mouseY);
    }
}

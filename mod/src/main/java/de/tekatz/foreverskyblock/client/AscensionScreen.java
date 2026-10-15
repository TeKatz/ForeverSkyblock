package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity;
import de.tekatz.foreverskyblock.menu.AscensionMenu;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.Tooltip;
import net.minecraft.client.gui.screens.inventory.AbstractContainerScreen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;

public final class AscensionScreen extends AbstractContainerScreen<AscensionMenu> {
    private Button start;
    public AscensionScreen(AscensionMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title);
        imageWidth = 176; imageHeight = 228; inventoryLabelY = 132;
    }
    private static Component text(String key, Object... args) { return Component.translatable("gui.forever_skyblock.ascension." + key, args); }
    @Override protected void init() {
        super.init();
        start = addRenderableWidget(Button.builder(text("start"), b -> minecraft.gameMode.handleInventoryButtonClick(menu.containerId, 0))
                .bounds(leftPos + 8, topPos + 103, 160, 20).build());
        start.setTooltip(Tooltip.create(text("help")));
    }
    @Override protected void containerTick() {
        super.containerTick();
        start.active = !menu.busy() && menu.getSlot(0).hasItem();
    }
    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        g.fill(x, y, x + imageWidth, y + imageHeight, 0xFF506571);
        g.fill(x + 1, y + 1, x + imageWidth - 1, y + imageHeight - 1, 0xFF14212B);
        g.fill(x + 1, y + 1, x + imageWidth - 1, y + 3, 0xFFC59DFF);
        g.fill(x + 8, y + 24, x + 168, y + 96, 0xFF0D1720);
        slot(g, x + 25, y + 38);
        g.drawString(font, text("conversion"), x + 50, y + 32, 0xFFE8EFF4, false);
        g.drawString(font, text("duration"), x + 50, y + 45, 0xFFAFBFCB, false);
        g.fill(x + 50, y + 59, x + 157, y + 65, 0xFF314451);
        g.fill(x + 50, y + 59, x + 50 + menu.progress() * 107 / AscensionChamberBlockEntity.DURATION, y + 65, 0xFFBC91FF);
        String status = switch (menu.status()) { case 1 -> "running"; case 2 -> "offline"; case 3 -> "storage_error"; case 4 -> "invalid"; case 5 -> "claimed"; case 6 -> "complete"; default -> "ready"; };
        g.drawCenteredString(font, text(status), x + 88, y + 72, menu.status() >= 3 && menu.status() <= 5 ? 0xFFFFB394 : 0xFFCEB3FF);
        Component recipient = minecraft.player.getDisplayName();
        if (menu.busy()) {
            var info = minecraft.getConnection().getPlayerInfo(menu.owner());
            recipient = info == null ? text("offline_player") : Component.literal(info.getProfile().getName());
        }
        g.drawCenteredString(font, font.substrByWidth(text("recipient", recipient), 154), x + 88, y + 85, 0xFFAFBFCB);
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++) slot(g, x + 8 + col * 18, y + 144 + row * 18);
        for (int col = 0; col < 9; col++) slot(g, x + 8 + col * 18, y + 202);
        if (menu.status() == AscensionChamberBlockEntity.RUNNING) {
            int dot = (int) (System.currentTimeMillis() / 180 % 4);
            g.fill(x + 16, y + 60 + dot * 4, x + 19, y + 63 + dot * 4, 0xFFC59DFF);
        }
    }
    private static void slot(GuiGraphics g, int x, int y) {
        g.fill(x - 1, y - 1, x + 17, y + 17, 0xFF526673);
        g.fill(x, y, x + 16, y + 16, 0xFF263C49);
    }
    @Override protected void renderLabels(GuiGraphics g, int mouseX, int mouseY) {
        g.drawString(font, title, titleLabelX, titleLabelY, 0xFFE8EFF4, false);
        g.drawString(font, playerInventoryTitle, inventoryLabelX, inventoryLabelY, 0xFFAFBFCB, false);
    }
    @Override public void render(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        super.render(g, mouseX, mouseY, partialTick); renderTooltip(g, mouseX, mouseY);
    }
}

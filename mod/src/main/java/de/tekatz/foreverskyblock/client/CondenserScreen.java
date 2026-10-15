package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.network.CondenserNetwork;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.inventory.AbstractContainerScreen;
import net.minecraft.client.renderer.Rect2i;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.inventory.ClickType;
import net.minecraft.world.inventory.Slot;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.network.PacketDistributor;

public final class CondenserScreen extends AbstractContainerScreen<CondenserMenu> {
    private Button filters;
    public CondenserScreen(CondenserMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title);
        imageWidth = 176; imageHeight = 208; inventoryLabelY = 112;
    }
    @Override protected void init() {
        super.init();
        filters = addRenderableWidget(Button.builder(Component.empty(), button -> {
            minecraft.gameMode.handleInventoryButtonClick(menu.containerId, 0);
        }).bounds(leftPos + 8, topPos + 86, 160, 20).build());
        updateButton();
    }
    private void updateButton() {
        if (filters != null) filters.setMessage(Component.translatable("gui.forever_skyblock." + (menu.editing() ? "finish_filters" : "edit_filters")));
    }
    public Rect2i inputArea(int slot) {
        return new Rect2i(leftPos + (slot == 0 ? CondenserMenu.FIRST_X : CondenserMenu.SECOND_X), topPos + CondenserMenu.INPUT_Y, 16, 16);
    }
    public void setGhost(int slot, ItemStack stack) {
        if (menu.editing() && menu.acceptsGhost(slot, stack)) PacketDistributor.sendToServer(new CondenserNetwork.Filter(
                menu.containerId, slot, BuiltInRegistries.ITEM.getKey(stack.getItem())));
    }
    @Override protected void slotClicked(Slot slot, int slotId, int button, ClickType type) {
        if (menu.editing() && CondenserBlockEntity.isInput(slotId)) {
            if (button == 1) setGhost(slotId, ItemStack.EMPTY);
            else if (!menu.getCarried().isEmpty()) setGhost(slotId, menu.getCarried());
            return;
        }
        super.slotClicked(slot, slotId, button, type);
    }
    @Override public boolean mouseReleased(double x, double y, int button) {
        if (menu.editing() && button == 0 && !menu.getCarried().isEmpty()) {
            for (int slot : new int[]{0, 2}) if (inputArea(slot).contains((int) x, (int) y) && menu.getSlot(slot).isActive()) {
                setGhost(slot, menu.getCarried());
                // Let the base screen finish its drag state; the server blocks real input clicks in edit mode.
                super.mouseReleased(x, y, button);
                return true;
            }
        }
        return super.mouseReleased(x, y, button);
    }
    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        g.fill(x, y, x + imageWidth, y + imageHeight, 0xFF10171C);
        g.fill(x + 1, y + 1, x + imageWidth - 1, y + imageHeight - 1, 0xFF66757C);
        g.fill(x + 3, y + 3, x + imageWidth - 3, y + imageHeight - 3, 0xFF28333B);
        g.fill(x + 7, y + 22, x + 169, y + 82, 0xFF19242C);
        slot(g, x + CondenserMenu.FIRST_X, y + CondenserMenu.INPUT_Y);
        if (menu.hasModule()) slot(g, x + CondenserMenu.SECOND_X, y + CondenserMenu.INPUT_Y);
        slot(g, x + CondenserMenu.OUTPUT_X, y + CondenserMenu.INPUT_Y);
        for (int input : new int[]{0, 2}) if (menu.getSlot(input).isActive()) {
            int sx = input == 0 ? CondenserMenu.FIRST_X : CondenserMenu.SECOND_X;
            ItemStack filter = menu.filter(input);
            if (!filter.isEmpty()) {
                if (menu.getSlot(input).getItem().isEmpty()) {
                    g.renderItem(filter, x + sx, y + CondenserMenu.INPUT_Y);
                    g.fill(x + sx, y + CondenserMenu.INPUT_Y, x + sx + 16, y + CondenserMenu.INPUT_Y + 16, 0x88404C55);
                }
                g.fill(x + sx, y + 56, x + sx + 16, y + 58, 0xFFFFCB69);
            } else if (menu.editing()) g.fill(x + sx, y + 56, x + sx + 16, y + 58, 0xFF49C7D4);
        }
        g.fill(x + 78, y + 42, x + 114, y + 50, 0xFF3D505B);
        int width = Math.min(36, menu.progress() * 36 / menu.duration());
        g.fill(x + 78, y + 42, x + 78 + width, y + 50, 0xFF49C7D4);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.input"), x + (menu.hasModule() ? 47 : 36), y + 26, 0xFFA8B7BE);
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock.output"), x + 134, y + 26, 0xFFA8B7BE);
        String key = switch (menu.status()) { case CondenserBlockEntity.RUNNING -> "processing"; case CondenserBlockEntity.BLOCKED -> "blocked"; default -> "idle"; };
        g.drawCenteredString(font, Component.translatable("gui.forever_skyblock." + key), x + 88, y + 64,
                menu.status() == CondenserBlockEntity.BLOCKED ? 0xFFFFC16E : 0xFF9FE3E9);
        if (menu.status() == CondenserBlockEntity.RUNNING) {
            int dot = (int) (System.currentTimeMillis() / 160 % 4);
            for (int i = 0; i < 4; i++) g.fill(x + 83 + i * 6, y + 54, x + 86 + i * 6, y + 57,
                    i == dot ? 0xFFAFFAFF : 0xFF35606A);
        }
        for (int row = 0; row < 3; row++) for (int col = 0; col < 9; col++) slot(g, x + 8 + col * 18, y + 124 + row * 18);
        for (int col = 0; col < 9; col++) slot(g, x + 8 + col * 18, y + 182);
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
        updateButton();
        super.render(g, mouseX, mouseY, partialTick);
        renderTooltip(g, mouseX, mouseY);
        if (filters.isHovered()) g.renderComponentTooltip(font, java.util.List.of(
                Component.translatable("gui.forever_skyblock.filter_help"), Component.translatable("gui.forever_skyblock.filter_clear")), mouseX, mouseY);
        for (int slot : new int[]{0, 2}) if (menu.getSlot(slot).isActive() && inputArea(slot).contains(mouseX, mouseY)) {
            if (menu.editing() || !menu.filter(slot).isEmpty()) {
                var lines = new java.util.ArrayList<Component>();
                lines.add(Component.translatable(menu.filter(slot).isEmpty() ? "gui.forever_skyblock.filter_empty" : "gui.forever_skyblock.filter_set", menu.filter(slot).getHoverName()));
                if (menu.editing()) lines.add(Component.translatable("gui.forever_skyblock.filter_clear"));
                g.renderComponentTooltip(font, lines, mouseX, mouseY);
            }
        }
    }
}

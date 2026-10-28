package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlockEntity;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineDefinition;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineMenu;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.inventory.AbstractContainerScreen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;

/** Generic native-coordinate screen for GUI layouts authored in Forever Studio. */
public final class ForeverStudioMachineScreen extends AbstractContainerScreen<ForeverStudioMachineMenu> {
    private static final int BG = 0xFF151D22;
    private static final int PANEL = 0xFF202B31;
    private static final int EDGE = 0xFF40535C;
    private static final int TEXT = 0xFFE7F0F2;
    private static final int MUTED = 0xFF91A3AA;
    private static final int ACCENT = 0xFF4EB8BC;
    private static final int WARN = 0xFFE2B765;

    public ForeverStudioMachineScreen(ForeverStudioMachineMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title);
        imageWidth = Math.max(176, menu.definition().guiWidth());
        imageHeight = menu.definition().guiHeight() + 96;
    }

    @Override public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics, mouseX, mouseY, partialTick);
        super.render(graphics, mouseX, mouseY, partialTick);
        renderTooltip(graphics, mouseX, mouseY);
    }

    @Override protected void renderBg(GuiGraphics graphics, float partialTick, int mouseX, int mouseY) {
        int x = leftPos;
        int y = topPos;
        ForeverStudioMachineDefinition definition = menu.definition();
        graphics.fill(x, y, x + imageWidth, y + imageHeight, BG);
        frame(graphics, x, y, imageWidth, imageHeight);
        graphics.fill(x + 1, y + 1, x + imageWidth - 1, y + definition.guiHeight(), PANEL);
        graphics.drawString(font, Component.literal(definition.displayName()), x + 8, y + 7, TEXT, false);

        for (ForeverStudioMachineDefinition.WidgetDefinition widget : definition.widgets()) drawWidget(graphics, widget, x, y);
        for (ForeverStudioMachineDefinition.SlotDefinition slot : definition.slots())
            slotFrame(graphics, x + slot.x(), y + slot.y(), slot.input(), slot.storage());

        int inventoryY = y + definition.guiHeight() + 8;
        graphics.fill(x + 4, inventoryY, x + imageWidth - 4, y + imageHeight - 4, 0xFF10171B);
        graphics.drawString(font, playerInventoryTitle, x + 8, inventoryY + 2, MUTED, false);
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 9; col++)
                slotFrame(graphics, x + 8 + col * 18, y + definition.guiHeight() + 14 + row * 18, false, false);
        for (int col = 0; col < 9; col++)
            slotFrame(graphics, x + 8 + col * 18, y + definition.guiHeight() + 72, false, false);
    }

    private void drawWidget(GuiGraphics graphics, ForeverStudioMachineDefinition.WidgetDefinition widget, int originX, int originY) {
        int x = originX + widget.x();
        int y = originY + widget.y();
        int w = widget.width();
        int h = widget.height();
        switch (widget.type()) {
            case "item-slot", "output-slot" -> { }
            case "progress-bar" -> meter(graphics, x, y, w, h, menu.progress() / (float)menu.duration(), ACCENT);
            case "energy-bar" -> meter(graphics, x, y, w, h,
                    menu.energyCapacity() <= 0 ? 0F : menu.energy() / (float)menu.energyCapacity(), 0xFFB58BDA);
            case "fluid-gauge" -> meter(graphics, x, y, w, h,
                    widget.tank() < 0 || menu.fluidCapacity(widget.tank()) <= 0 ? 0F
                            : menu.fluidAmount(widget.tank()) / (float)menu.fluidCapacity(widget.tank()), 0xFF4E9DD4);
            case "status" -> {
                int color = switch (menu.status()) {
                    case ForeverStudioMachineBlockEntity.RUNNING -> ACCENT;
                    case ForeverStudioMachineBlockEntity.BLOCKED,
                         ForeverStudioMachineBlockEntity.NO_POWER,
                         ForeverStudioMachineBlockEntity.GRAPH_BLOCKED -> WARN;
                    default -> 0xFF52636B;
                };
                graphics.fill(x, y, x + w, y + h, 0xFF0D1417);
                graphics.fill(x + 2, y + 2, x + Math.max(3, w - 2), y + Math.max(3, h - 2), color);
            }
            case "value" -> graphics.drawString(font, runtimeValue(widget), x,
                    y + Math.max(0, (h - 8) / 2), TEXT, false);
            default -> { }
        }
    }

    private String runtimeValue(ForeverStudioMachineDefinition.WidgetDefinition widget) {
        if (widget.tank() >= 0) {
            int amount = menu.fluidAmount(widget.tank());
            int capacity = menu.fluidCapacity(widget.tank());
            return widget.sourcePort().equals("ratio")
                    ? (capacity <= 0 ? "0%" : Math.round(amount * 100F / capacity) + "%")
                    : amount + " mB";
        }
        return switch (widget.sourcePort()) {
            case "progress" -> Math.round(menu.progress() * 100F / menu.duration()) + "%";
            case "running" -> menu.status() == ForeverStudioMachineBlockEntity.RUNNING ? "RUNNING" : "IDLE";
            case "energy" -> menu.energy() + " FE";
            case "ratio" -> menu.energyCapacity() <= 0 ? "0%" : Math.round(menu.energy() * 100F / menu.energyCapacity()) + "%";
            case "state" -> menu.stateIndex() < menu.definition().states().size()
                    ? menu.definition().states().get(menu.stateIndex()).id() : "";
            default -> "";
        };
    }

    private static void meter(GuiGraphics graphics, int x, int y, int width, int height, float value, int color) {
        float clamped = Math.max(0F, Math.min(1F, value));
        graphics.fill(x, y, x + width, y + height, 0xFF0B1114);
        graphics.fill(x + 1, y + 1, x + width - 1, y + height - 1, 0xFF28343A);
        boolean vertical = height > width;
        if (vertical) {
            int fill = Math.round(Math.max(0, height - 2) * clamped);
            graphics.fill(x + 1, y + height - 1 - fill, x + width - 1, y + height - 1, color);
        } else {
            int fill = Math.round(Math.max(0, width - 2) * clamped);
            graphics.fill(x + 1, y + 1, x + 1 + fill, y + height - 1, color);
        }
    }

    private static void frame(GuiGraphics graphics, int x, int y, int width, int height) {
        graphics.fill(x, y, x + width, y + 1, EDGE);
        graphics.fill(x, y + height - 1, x + width, y + height, EDGE);
        graphics.fill(x, y, x + 1, y + height, EDGE);
        graphics.fill(x + width - 1, y, x + width, y + height, EDGE);
    }

    private static void slotFrame(GuiGraphics graphics, int x, int y, boolean input, boolean storage) {
        int border = input ? ACCENT : storage ? 0xFF9A7BC2 : 0xFF5A6B73;
        graphics.fill(x - 1, y - 1, x + 17, y + 17, border);
        graphics.fill(x, y, x + 16, y + 16, 0xFF11181C);
    }

    @Override protected void renderLabels(GuiGraphics graphics, int mouseX, int mouseY) { }
}

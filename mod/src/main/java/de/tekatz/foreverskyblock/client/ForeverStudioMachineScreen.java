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

    private final ForeverStudioGuiPresentation presentation;
    private String pressedWidgetId;

    public ForeverStudioMachineScreen(ForeverStudioMachineMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title);
        imageWidth = Math.max(176, menu.definition().guiWidth());
        imageHeight = menu.definition().guiHeight() + 96;
        presentation = ForeverStudioGuiPresentation.load(menu.definition().machineId());
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

        for (ForeverStudioMachineDefinition.WidgetDefinition widget : definition.widgets())
            drawWidget(graphics, widget, x, y, mouseX, mouseY);
        for (ForeverStudioMachineDefinition.SlotDefinition slot : definition.slots()) {
            ForeverStudioMachineDefinition.WidgetDefinition widget = definition.widgets().stream()
                    .filter(candidate -> candidate.hasMachineSlot() && candidate.slot() == slot.slot())
                    .findFirst().orElse(null);
            if (widget == null) {
                slotFrame(graphics, x + slot.x(), y + slot.y(), slot.input(), slot.storage(), 1.0, -1);
            } else {
                drawStyledSlot(graphics, slot, widget, x, y, mouseX, mouseY);
            }
        }

        int inventoryY = y + definition.guiHeight() + 8;
        graphics.fill(x + 4, inventoryY, x + imageWidth - 4, y + imageHeight - 4, 0xFF10171B);
        graphics.drawString(font, playerInventoryTitle, x + 8, inventoryY + 2, MUTED, false);
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 9; col++)
                slotFrame(graphics, x + 8 + col * 18, y + definition.guiHeight() + 14 + row * 18, false, false, 1.0, -1);
        for (int col = 0; col < 9; col++)
            slotFrame(graphics, x + 8 + col * 18, y + definition.guiHeight() + 72, false, false, 1.0, -1);
    }

    private void drawWidget(
            GuiGraphics graphics,
            ForeverStudioMachineDefinition.WidgetDefinition widget,
            int originX,
            int originY,
            int mouseX,
            int mouseY) {
        if (widget.type().equals("item-slot") || widget.type().equals("output-slot")) return;

        int x = originX + widget.x();
        int y = originY + widget.y();
        int w = widget.width();
        int h = widget.height();
        ForeverStudioGuiPresentation.ElementPresentation visual = presentation.element(widget.id());
        double opacity = effectiveOpacity(visual);
        boolean transformed = beginVisualTransform(graphics, visual, widget.id(), x, y, w, h, mouseX, mouseY);
        drawVisualBackdrop(graphics, visual, x, y, w, h, opacity);

        int accent = styledColor(visual, ACCENT, opacity);
        switch (widget.type()) {
            case "progress-bar" -> meter(graphics, x, y, w, h,
                    menu.progress() / (float)menu.duration(), accent, opacity);
            case "energy-bar" -> meter(graphics, x, y, w, h,
                    menu.energyCapacity() <= 0 ? 0F : menu.energy() / (float)menu.energyCapacity(),
                    styledColor(visual, 0xFFB58BDA, opacity), opacity);
            case "fluid-gauge" -> meter(graphics, x, y, w, h,
                    widget.tank() < 0 || menu.fluidCapacity(widget.tank()) <= 0 ? 0F
                            : menu.fluidAmount(widget.tank()) / (float)menu.fluidCapacity(widget.tank()),
                    styledColor(visual, 0xFF4E9DD4, opacity), opacity);
            case "status" -> {
                int baseColor = switch (menu.status()) {
                    case ForeverStudioMachineBlockEntity.RUNNING -> ACCENT;
                    case ForeverStudioMachineBlockEntity.BLOCKED,
                         ForeverStudioMachineBlockEntity.NO_POWER,
                         ForeverStudioMachineBlockEntity.GRAPH_BLOCKED -> WARN;
                    default -> 0xFF52636B;
                };
                int color = styledColor(visual, baseColor, opacity);
                graphics.fill(x, y, x + w, y + h, withOpacity(0xFF0D1417, opacity));
                graphics.fill(x + 2, y + 2, x + Math.max(3, w - 2), y + Math.max(3, h - 2), color);
            }
            case "value" -> graphics.drawString(font, runtimeValue(widget), x,
                    y + Math.max(0, (h - 8) / 2), styledColor(visual, TEXT, opacity), false);
            default -> { }
        }
        if (transformed) graphics.pose().popPose();
    }

    private void drawStyledSlot(
            GuiGraphics graphics,
            ForeverStudioMachineDefinition.SlotDefinition slot,
            ForeverStudioMachineDefinition.WidgetDefinition widget,
            int originX,
            int originY,
            int mouseX,
            int mouseY) {
        int x = originX + slot.x();
        int y = originY + slot.y();
        ForeverStudioGuiPresentation.ElementPresentation visual = presentation.element(widget.id());
        double opacity = effectiveOpacity(visual);
        boolean transformed = beginVisualTransform(graphics, visual, widget.id(), x - 1, y - 1, 18, 18, mouseX, mouseY);
        drawVisualBackdrop(graphics, visual, x - 1, y - 1, 18, 18, opacity);
        int fallback = slot.input() ? ACCENT : slot.storage() ? 0xFF9A7BC2 : 0xFF5A6B73;
        slotFrame(graphics, x, y, slot.input(), slot.storage(), opacity, styledColor(visual, fallback, opacity));
        if (transformed) graphics.pose().popPose();
    }

    private boolean beginVisualTransform(
            GuiGraphics graphics,
            ForeverStudioGuiPresentation.ElementPresentation visual,
            String widgetId,
            int x,
            int y,
            int width,
            int height,
            int mouseX,
            int mouseY) {
        boolean hovered = mouseX >= x && mouseX < x + width && mouseY >= y && mouseY < y + height;
        ForeverStudioGuiPresentation.Effect hover = visual.effect("hover");
        ForeverStudioGuiPresentation.Effect pressed = visual.effect("pressed-offset");
        double scale = hovered && hover != null ? clamp(hover.number("scale", 1.05), 0.25, 4.0) : 1.0;
        double offsetX = widgetId.equals(pressedWidgetId) && pressed != null ? clamp(pressed.number("x", 1), -64, 64) : 0;
        double offsetY = widgetId.equals(pressedWidgetId) && pressed != null ? clamp(pressed.number("y", 1), -64, 64) : 0;
        if (Math.abs(scale - 1.0) < 0.0001 && Math.abs(offsetX) < 0.0001 && Math.abs(offsetY) < 0.0001)
            return false;

        double centerX = x + width / 2.0;
        double centerY = y + height / 2.0;
        graphics.pose().pushPose();
        graphics.pose().translate(centerX + offsetX, centerY + offsetY, 0);
        graphics.pose().scale((float)scale, (float)scale, 1F);
        graphics.pose().translate(-centerX, -centerY, 0);
        return true;
    }

    private static void drawVisualBackdrop(
            GuiGraphics graphics,
            ForeverStudioGuiPresentation.ElementPresentation visual,
            int x,
            int y,
            int width,
            int height,
            double opacity) {
        ForeverStudioGuiPresentation.Effect shadow = visual.effect("shadow");
        if (shadow != null) {
            int depth = (int)Math.round(clamp(shadow.number("depth", 2), -16, 16));
            double effectOpacity = clamp(shadow.number("opacity", 0.55), 0, 1) * opacity;
            graphics.fill(x + depth, y + depth, x + width + depth, y + height + depth,
                    withOpacity(0xFF000000, effectOpacity));
        }
        ForeverStudioGuiPresentation.Effect glow = visual.effect("glow");
        if (glow != null) {
            int color = parseColor(glow.text("color", "#4EB8BC"), ACCENT);
            double effectOpacity = clamp(glow.number("opacity", 0.8), 0, 1) * opacity;
            graphics.fill(x - 2, y - 2, x + width + 2, y + height + 2,
                    withOpacity(color, effectOpacity * 0.20));
            graphics.fill(x - 1, y - 1, x + width + 1, y + height + 1,
                    withOpacity(color, effectOpacity * 0.35));
        }
    }

    private double effectiveOpacity(ForeverStudioGuiPresentation.ElementPresentation visual) {
        double opacity = visual.opacity();
        ForeverStudioGuiPresentation.Effect pulse = visual.effect("pulse");
        if (pulse == null) return opacity;
        double minOpacity = clamp(pulse.number("minOpacity", 0.65), 0, 1);
        double duration = clamp(pulse.number("durationMs", 900), 100, 60_000);
        double phase = (System.currentTimeMillis() % Math.max(1L, Math.round(duration * 2))) / duration;
        double triangle = phase <= 1.0 ? phase : 2.0 - phase;
        return opacity * (minOpacity + (1.0 - minOpacity) * triangle);
    }

    private int styledColor(ForeverStudioGuiPresentation.ElementPresentation visual, int fallback, double opacity) {
        int color = visual.tint().isBlank() ? fallback : parseColor(visual.tint(), fallback);
        return withOpacity(color, opacity);
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

    private static void meter(
            GuiGraphics graphics,
            int x,
            int y,
            int width,
            int height,
            float value,
            int color,
            double opacity) {
        float clamped = Math.max(0F, Math.min(1F, value));
        graphics.fill(x, y, x + width, y + height, withOpacity(0xFF0B1114, opacity));
        graphics.fill(x + 1, y + 1, x + width - 1, y + height - 1, withOpacity(0xFF28343A, opacity));
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

    private static void slotFrame(
            GuiGraphics graphics,
            int x,
            int y,
            boolean input,
            boolean storage,
            double opacity,
            int styledBorder) {
        int border = styledBorder >= 0 ? styledBorder : input ? ACCENT : storage ? 0xFF9A7BC2 : 0xFF5A6B73;
        graphics.fill(x - 1, y - 1, x + 17, y + 17, withOpacity(border, opacity));
        graphics.fill(x, y, x + 16, y + 16, withOpacity(0xFF11181C, opacity));
    }

    private String widgetAt(double mouseX, double mouseY) {
        for (ForeverStudioMachineDefinition.WidgetDefinition widget : menu.definition().widgets()) {
            double x = leftPos + widget.x();
            double y = topPos + widget.y();
            if (mouseX >= x && mouseX < x + widget.width() && mouseY >= y && mouseY < y + widget.height())
                return widget.id();
        }
        return null;
    }

    @Override public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) pressedWidgetId = widgetAt(mouseX, mouseY);
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override public boolean mouseReleased(double mouseX, double mouseY, int button) {
        try {
            return super.mouseReleased(mouseX, mouseY, button);
        } finally {
            if (button == 0) pressedWidgetId = null;
        }
    }

    private static int parseColor(String value, int fallback) {
        if (value == null || value.isBlank() || value.charAt(0) != '#') return fallback;
        try {
            String hex = value.substring(1);
            if (hex.length() == 6) return 0xFF000000 | Integer.parseUnsignedInt(hex, 16);
            if (hex.length() == 8) return (int)Long.parseLong(hex, 16);
            return fallback;
        } catch (NumberFormatException ex) {
            return fallback;
        }
    }

    private static int withOpacity(int color, double opacity) {
        int alpha = (color >>> 24) & 0xFF;
        int scaled = (int)Math.round(alpha * clamp(opacity, 0, 1));
        return (color & 0x00FFFFFF) | (scaled << 24);
    }

    private static double clamp(double value, double min, double max) {
        return Math.max(min, Math.min(max, value));
    }

    @Override protected void renderLabels(GuiGraphics graphics, int mouseX, int mouseY) { }
}

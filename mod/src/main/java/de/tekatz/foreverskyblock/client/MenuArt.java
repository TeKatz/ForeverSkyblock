package de.tekatz.foreverskyblock.client;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;

/** Code-drawn menu marks stay crisp at every GUI scale and require no item-model placeholders. */
final class MenuArt {
    static final int WHITE = 0xFFF2F8FD, MUTED = 0xFF9DB4C5, DIM = 0xFF638399,
            CYAN = 0xFF51D7ED, GOLD = 0xFFFFCB75, GREEN = 0xFF69DDA7, BORDER = 0xFF294759;
    private MenuArt() { }
    static Component t(String key, Object... args) { return Component.translatable("gui.forever_skyblock.overhaul." + key, args); }
    static void panel(GuiGraphics g, int x, int y, int w, int h, int color, int border) {
        if (w < 2 || h < 2) return;
        g.fill(x, y, x + w, y + h, color);
        g.fill(x, y, x + w, y + 1, border); g.fill(x, y + h - 1, x + w, y + h, border);
        g.fill(x, y, x + 1, y + h, border); g.fill(x + w - 1, y, x + w, y + h, border);
    }
    static void label(GuiGraphics g, Component text, int x, int y, int w, int color, float scale) {
        if (w <= 0) return;
        var font = Minecraft.getInstance().font;
        int available = Math.max(1, (int) (w / scale));
        String value = text.getString();
        if (font.width(value) > available) value = font.plainSubstrByWidth(value, Math.max(1, available - font.width("…"))) + "…";
        g.pose().pushPose(); g.pose().translate(x, y, 0); g.pose().scale(scale, scale, 1);
        g.drawString(font, value, 0, 0, color, false); g.pose().popPose();
    }
    static void centered(GuiGraphics g, Component text, int x, int y, int w, int h, int color, float scale) {
        var font = Minecraft.getInstance().font;
        float fit = Math.min(scale, w / (float) Math.max(1, font.width(text.getString())));
        label(g, text, x + Math.max(0, Math.round((w - font.width(text.getString()) * fit) / 2)),
                y + Math.round((h - 8 * fit) / 2), w, color, fit);
    }
    static int wrappedHeight(Component text, int width, float scale) {
        return Minecraft.getInstance().font.split(text, Math.max(1, (int) (width / scale))).size() * Math.round(11 * scale);
    }
    static int wrapped(GuiGraphics g, Component text, int x, int y, int width, int color, float scale, int maxLines) {
        int n = 0;
        for (var line : Minecraft.getInstance().font.split(text, Math.max(1, (int) (width / scale)))) {
            if (n++ >= maxLines) break;
            g.pose().pushPose(); g.pose().translate(x, y, 0); g.pose().scale(scale, scale, 1);
            g.drawString(Minecraft.getInstance().font, line, 0, 0, color, false); g.pose().popPose();
            y += Math.round(11 * scale);
        }
        return y;
    }
    static void circle(GuiGraphics g, int cx, int cy, int radius, int color) {
        for (int dy = -radius; dy <= radius; dy++) {
            int dx = (int) Math.floor(Math.sqrt(radius * radius - dy * dy));
            g.fill(cx - dx, cy + dy, cx + dx + 1, cy + dy + 1, color);
        }
    }
    static void line(GuiGraphics g, int x1, int y1, int x2, int y2, int color) {
        int steps = Math.max(Math.abs(x2 - x1), Math.abs(y2 - y1));
        for (int i = 0; i <= steps; i++) {
            float t = steps == 0 ? 0 : i / (float) steps;
            int x = Math.round(x1 + (x2 - x1) * t), y = Math.round(y1 + (y2 - y1) * t);
            g.fill(x, y, x + 1, y + 1, color);
        }
    }
    static void bar(GuiGraphics g, int x, int y, int width, double ratio, int color) {
        g.fill(x, y, x + width, y + 3, 0xFF193444);
        g.fill(x, y, x + (int) Math.round(width * Math.clamp(ratio, 0, 1)), y + 3, color);
    }
    static void mark(GuiGraphics g, int x, int y, int kind, int color) {
        // SP: open crystal; PP: four-point star; Tier: stepped crest. 16x16 design grid.
        if (kind == 0) {
            line(g,x+8,y+1,x+14,y+7,color); line(g,x+14,y+7,x+8,y+15,color);
            line(g,x+8,y+15,x+2,y+7,color); line(g,x+2,y+7,x+8,y+1,color);
            line(g,x+2,y+7,x+14,y+7,color); line(g,x+8,y+1,x+8,y+15,color);
        } else if (kind == 1) {
            for (int i = -6; i <= 6; i++) { int r = Math.max(1, 4 - Math.abs(i)); g.fill(x+8-r,y+8+i,x+9+r,y+9+i,color); }
        } else if (kind == 2) {
            line(g,x+2,y+2,x+14,y+2,color); line(g,x+2,y+2,x+3,y+10,color);
            line(g,x+14,y+2,x+13,y+10,color); line(g,x+3,y+10,x+8,y+15,color);
            line(g,x+8,y+15,x+13,y+10,color); g.fill(x+5,y+5,x+11,y+6,color); g.fill(x+6,y+8,x+10,y+9,color);
        } else if (kind == 3) {
            circle(g,x+8,y+8,7,color); circle(g,x+8,y+8,6,0xFF10232F);
            line(g,x+8,y+8,x+8,y+4,color); line(g,x+8,y+8,x+11,y+10,color);
        } else {
            g.fill(x+2,y+6,x+14,y+8,color); g.fill(x+4,y+8,x+12,y+11,0xFF648E90);
            g.fill(x+6,y+11,x+10,y+14,0xFF648E90); g.fill(x+7,y+2,x+9,y+6,color);
        }
    }
}

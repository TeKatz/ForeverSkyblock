package de.tekatz.foreverskyblock.mixin;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Keeps compact K-menu controls readable at GUI scale 3 without clipping labels. */
@Mixin(targets = "de.tekatz.foreverskyblock.client.PremiumSkillScreen$ChromeButton")
public abstract class PremiumChromeButtonVisualMixin {
    private static final int WHITE = 0xFFF5FBFF;
    private static final int LIGHT = 0xFFD7E5F0;
    private static final int DIM = 0xFF6F8799;
    private static final int CYAN = 0xFF2CCBFF;

    @Shadow @Final private boolean selected;

    @Inject(method = "renderWidget", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$renderChromeButton(GuiGraphics g, int mouseX, int mouseY, float partialTick,
                                                     CallbackInfo callback) {
        Button self = (Button) (Object) this;
        Font font = Minecraft.getInstance().font;
        int x = self.getX();
        int y = self.getY();
        int w = self.getWidth();
        int h = self.getHeight();
        boolean hovered = self.isMouseOver(mouseX, mouseY) || self.isFocused();

        int border = selected ? CYAN : hovered ? 0xFF4D7893 : 0xFF294B60;
        int fill = selected ? 0xE0184967 : hovered ? 0xDA132D40 : 0xC20B1B28;
        g.fill(x + 1, y + 2, x + w + 1, y + h + 2, 0x32000000);
        panel(g, x, y, w, h, fill, border);
        if (selected) {
            g.fill(x + 1, y + 1, x + w - 1, y + 2, 0xC33AD8FF);
            glow(g, x, y, w, h, CYAN);
        }

        String label = self.getMessage().getString();
        float preferred = h <= 18 ? 0.58F : 0.66F;
        int raw = Math.max(1, font.width(label));
        float fit = (w - 8) / (float) raw;
        float scale = Math.max(0.46F, Math.min(preferred, fit));
        int rendered = Math.round(raw * scale);
        int tx = x + Math.max(4, (w - rendered) / 2);
        int ty = y + Math.max(3, (h - Math.round(8 * scale)) / 2);

        g.pose().pushPose();
        g.pose().scale(scale, scale, 1F);
        g.drawString(font, label, Math.round(tx / scale), Math.round(ty / scale),
                self.active ? (selected ? WHITE : LIGHT) : DIM, selected);
        g.pose().popPose();
        callback.cancel();
    }

    private static void panel(GuiGraphics g, int x, int y, int w, int h, int fill, int border) {
        g.fill(x + 1, y + 1, x + w - 1, y + h - 1, fill);
        g.fill(x, y, x + w, y + 1, border);
        g.fill(x, y + h - 1, x + w, y + h, border);
        g.fill(x, y, x + 1, y + h, border);
        g.fill(x + w - 1, y, x + w, y + h, border);
    }

    private static void glow(GuiGraphics g, int x, int y, int w, int h, int color) {
        int rgb = color & 0x00FFFFFF;
        g.fill(x - 2, y + 1, x - 1, y + h - 1, rgb | 0x30000000);
        g.fill(x + w + 1, y + 1, x + w + 2, y + h - 1, rgb | 0x30000000);
        g.fill(x + 1, y - 2, x + w - 1, y - 1, rgb | 0x30000000);
        g.fill(x + 1, y + h + 1, x + w - 1, y + h + 2, rgb | 0x30000000);
    }
}

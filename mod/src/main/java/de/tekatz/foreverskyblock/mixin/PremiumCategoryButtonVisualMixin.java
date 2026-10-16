package de.tekatz.foreverskyblock.mixin;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Flatter, cleaner category rows matching the Design-2 filter rail. */
@Mixin(targets = "de.tekatz.foreverskyblock.client.PremiumSkillScreen$CategoryButton")
public abstract class PremiumCategoryButtonVisualMixin {
    private static final int WHITE = 0xFFF4FAFF;
    private static final int LIGHT = 0xFFD2E2EE;
    private static final int MUTED = 0xFF71899D;
    private static final int CYAN = 0xFF2CCBFF;

    @Shadow @Final private String category;
    @Shadow @Final private ItemStack icon;
    @Shadow @Final private int count;
    @Shadow @Final private boolean selected;

    @Inject(method = "renderWidget", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$renderCategoryRow(GuiGraphics g, int mouseX, int mouseY, float partialTick,
                                                    CallbackInfo callback) {
        Button self = (Button) (Object) this;
        Font font = Minecraft.getInstance().font;
        int x = self.getX();
        int y = self.getY();
        int w = self.getWidth();
        int h = self.getHeight();
        boolean hovered = self.isMouseOver(mouseX, mouseY) || self.isFocused();
        int accent = categoryColor(category);

        if (selected) {
            g.fill(x + 2, y + 2, x + w + 2, y + h + 3, 0x3A000000);
            g.fill(x + 1, y + 1, x + w - 1, y + h - 1, 0xD5183D58);
            outline(g, x, y, w, h, CYAN);
            g.fill(x + 1, y + 1, x + 3, y + h - 1, accent);
            glow(g, x, y, w, h, CYAN);
        } else {
            if (hovered) g.fill(x + 1, y + 1, x + w - 1, y + h - 1, 0x7A173044);
            g.fill(x + 5, y + h - 1, x + w - 5, y + h, 0x45264155);
        }

        int iconY = y + (h - 16) / 2;
        if (h >= 19) {
            if (selected) g.fill(x + 5, iconY - 1, x + 23, iconY + 17, 0x54101F2C);
            g.renderItem(icon, x + 6, iconY);
        }

        int textX = x + (h >= 19 ? 29 : 6);
        float scale = h <= 20 ? 0.57F : 0.66F;
        text(g, font, self.getMessage(), textX, y + (h - 7) / 2,
                selected ? WHITE : LIGHT, scale, Math.max(12, w - (textX - x) - 25), selected);

        String number = Integer.toString(count);
        int numberW = Math.max(12, (int) Math.ceil(font.width(number) * scale));
        text(g, font, Component.literal(number), x + w - numberW - 7, y + (h - 7) / 2,
                selected ? 0xFF9FDFFF : MUTED, scale, numberW + 2, false);
        callback.cancel();
    }

    private static int categoryColor(String category) {
        return switch (category) {
            case "forever" -> 0xFF48E78B;
            case "ae2" -> 0xFFC76CF7;
            case "mekanism" -> 0xFF23D9D2;
            case "powah" -> 0xFFFFC447;
            case "mystical_agriculture" -> 0xFF66E56C;
            case "combat" -> 0xFFFF606B;
            case "contracts" -> 0xFFFFC76A;
            default -> CYAN;
        };
    }

    private static void outline(GuiGraphics g, int x, int y, int w, int h, int color) {
        g.fill(x, y, x + w, y + 1, color);
        g.fill(x, y + h - 1, x + w, y + h, color);
        g.fill(x, y, x + 1, y + h, color);
        g.fill(x + w - 1, y, x + w, y + h, color);
    }

    private static void glow(GuiGraphics g, int x, int y, int w, int h, int color) {
        int rgb = color & 0x00FFFFFF;
        g.fill(x - 2, y + 1, x - 1, y + h - 1, rgb | 0x3A000000);
        g.fill(x + w + 1, y + 1, x + w + 2, y + h - 1, rgb | 0x3A000000);
        g.fill(x + 1, y - 2, x + w - 1, y - 1, rgb | 0x3A000000);
        g.fill(x + 1, y + h + 1, x + w - 1, y + h + 2, rgb | 0x3A000000);
    }

    private static void text(GuiGraphics g, Font font, Component component, int x, int y, int color,
                             float scale, int maxWidth, boolean shadow) {
        String value = component.getString();
        value = font.plainSubstrByWidth(value, Math.max(1, (int) (maxWidth / scale)));
        g.pose().pushPose();
        g.pose().scale(scale, scale, 1F);
        g.drawString(font, value, Math.round(x / scale), Math.round(y / scale), color, shadow);
        g.pose().popPose();
    }
}

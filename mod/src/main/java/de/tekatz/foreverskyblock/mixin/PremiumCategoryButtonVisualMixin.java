package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.PremiumIconResolver;
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

/** Design-2 revision 3 category rail with real mod icons and stable text/count lanes. */
@Mixin(targets = "de.tekatz.foreverskyblock.client.PremiumSkillScreen$CategoryButton")
public abstract class PremiumCategoryButtonVisualMixin {
    private static final int WHITE = 0xFFF4FAFF;
    private static final int LIGHT = 0xFFD2E2EE;
    private static final int MUTED = 0xFF71899D;
    private static final int CYAN = 0xFF2CCBFF;

    @Shadow @Final private String category;
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
            g.fill(x + 2, y + 2, x + w + 2, y + h + 3, 0x35000000);
            g.fill(x + 1, y + 1, x + w - 1, y + h - 1, 0xDE15384F);
            outline(g, x, y, w, h, CYAN);
            g.fill(x + 1, y + 1, x + 3, y + h - 1, accent);
            glow(g, x, y, w, h, CYAN);
        } else {
            if (hovered) g.fill(x + 1, y + 1, x + w - 1, y + h - 1, 0x8A173044);
            g.fill(x + 5, y + h - 1, x + w - 5, y + h, 0x45264155);
        }

        ItemStack icon = PremiumIconResolver.categoryIcon(category);
        int iconX = x + 6;
        int iconY = y + (h - 16) / 2;
        if (selected) g.fill(iconX - 2, iconY - 1, iconX + 18, iconY + 17, 0x4B0E1E2A);
        renderScaledItem(g, icon, iconX, iconY, h <= 18 ? 0.82F : 0.92F);

        int textX = x + 28;
        float scale = h <= 18 ? 0.63F : 0.69F;
        String number = Integer.toString(count);
        int numberW = Math.max(12, (int) Math.ceil(font.width(number) * scale));
        int countX = x + w - numberW - 7;
        int textW = Math.max(18, countX - textX - 6);
        int textY = y + Math.max(3, (h - Math.round(8 * scale)) / 2);

        text(g, font, self.getMessage(), textX, textY, selected ? WHITE : LIGHT, scale, textW, selected);
        text(g, font, Component.literal(number), countX, textY,
                selected ? 0xFFA6E7FF : MUTED, scale, numberW + 2, false);
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
        g.fill(x - 2, y + 1, x - 1, y + h - 1, rgb | 0x33000000);
        g.fill(x + w + 1, y + 1, x + w + 2, y + h - 1, rgb | 0x33000000);
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

    private static void renderScaledItem(GuiGraphics g, ItemStack stack, int x, int y, float scale) {
        g.pose().pushPose();
        g.pose().translate(x, y, 0);
        g.pose().scale(scale, scale, 1F);
        g.renderItem(stack, 0, 0);
        g.pose().popPose();
    }
}

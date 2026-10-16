package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import net.minecraft.ChatFormatting;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.network.chat.Component;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Design-2 challenge cards with protected text lanes and mockup-style depth. */
@Mixin(targets = "de.tekatz.foreverskyblock.client.PremiumSkillScreen$ChallengeCardButton")
public abstract class PremiumChallengeCardVisualMixin {
    private static final int WHITE = 0xFFF7FBFF;
    private static final int LIGHT = 0xFFD7E6F2;
    private static final int MUTED = 0xFF9AAFC0;
    private static final int CYAN = 0xFF35D2FF;
    private static final int GREEN = 0xFF43E58A;

    @Shadow @Final private MilestoneCatalog.Milestone milestone;

    @Inject(method = "renderWidget", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$renderPremiumChallengeCard(GuiGraphics g, int mouseX, int mouseY, float partialTick,
                                                             CallbackInfo callback) {
        Button self = (Button) (Object) this;
        Font font = Minecraft.getInstance().font;
        int bx = self.getX();
        int by = self.getY();
        int bw = self.getWidth();
        int bh = self.getHeight();
        boolean hovered = self.isMouseOver(mouseX, mouseY) || self.isFocused();

        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        boolean complete = ready && state.milestones().contains(milestone.id());
        int current = ready ? Math.max(0, state.progress().getOrDefault(milestone.id(), 0)) : 0;
        int target = Math.max(1, milestone.target());
        if (complete) current = target;

        int accent = complete ? GREEN : categoryColor(milestone.category());
        int fill = complete ? 0xE20A2824 : hovered ? 0xE7193042 : 0xE30D1D2A;
        int border = complete || hovered ? accent : 0xFF2C4B60;

        // Card depth: shadow, dark body, category rail and a faint upper sheen.
        g.fill(bx + 2, by + 3, bx + bw + 2, by + bh + 4, 0x52000000);
        panel(g, bx, by, bw, bh, fill, border);
        g.fill(bx, by + 2, bx + 2, by + bh - 2, accent);
        g.fill(bx + 2, by + 1, bx + bw - 2, by + 2, (accent & 0x00FFFFFF) | 0x36000000);
        if (hovered || complete) glow(g, bx, by, bw, bh, accent);

        // Scenic depth without drawing a second opaque item over the card text.
        int artW = Math.min(42, Math.max(24, bw / 5));
        int artX = bx + bw - artW - 3;
        g.fill(artX, by + 3, bx + bw - 3, by + bh - 3, (accent & 0x00FFFFFF) | 0x0E000000);
        g.fill(artX + 5, by + 7, bx + bw - 7, by + 9, (accent & 0x00FFFFFF) | 0x12000000);
        g.fill(artX + 11, by + 13, bx + bw - 4, by + 15, 0x12000000);
        g.fill(artX + 2, by + bh - 14, bx + bw - 9, by + bh - 12, (accent & 0x00FFFFFF) | 0x10000000);

        int iconBox = Math.min(bh >= 66 ? 27 : 24, bh - 18);
        int iconX = bx + 7;
        int iconY = by + 6;
        panel(g, iconX, iconY, iconBox, iconBox, 0xD7122938, 0x663A6078);
        g.fill(iconX + 1, iconY + 1, iconX + iconBox - 1, iconY + 2,
                (accent & 0x00FFFFFF) | 0x42000000);
        g.renderItem(milestone.icon().getDefaultInstance(), iconX + (iconBox - 16) / 2, iconY + (iconBox - 16) / 2);

        int textX = iconX + iconBox + 7;
        int rightInset = complete ? 18 : 8;
        float titleScale = bh >= 66 ? 0.72F : 0.64F;
        float bodyScale = bh >= 66 ? 0.57F : 0.51F;
        float metaScale = bh >= 66 ? 0.56F : 0.50F;

        text(g, font, self.getMessage().copy().withStyle(ChatFormatting.BOLD), textX, by + 6, WHITE,
                titleScale, bw - (textX - bx) - rightInset, true);

        int descY = by + (bh >= 66 ? 20 : 18);
        int descWidth = bw - (textX - bx) - 8;
        wrappedText(g, font, Component.translatable(milestone.descriptionKey()), textX, descY, LIGHT,
                bodyScale, descWidth, bh >= 66 ? 2 : 1);

        int categoryY = by + (bh >= 66 ? 39 : 30);
        text(g, font, Component.literal(categoryName(milestone.category())), textX, categoryY, accent,
                metaScale, bw - (textX - bx) - 8, false);

        if (complete) {
            text(g, font, Component.literal("✔").withStyle(ChatFormatting.BOLD), bx + bw - 14, by + 5,
                    GREEN, 0.76F, 12, true);
        }

        String rewardText = "+" + milestone.reward() + " SP";
        int rewardW = Math.max(31, (int) Math.ceil(font.width(rewardText) * metaScale) + 8);
        int rewardX = bx + bw - rewardW - 6;
        int rewardY = by + bh - 18;
        panel(g, rewardX, rewardY, rewardW, 13, 0xEA123A62, 0xFF3E82C1);
        g.fill(rewardX + 1, rewardY + 1, rewardX + rewardW - 1, rewardY + 2, 0x663BCBFF);
        text(g, font, Component.literal(rewardText).withStyle(ChatFormatting.BOLD), rewardX + 4, rewardY + 3,
                WHITE, metaScale, rewardW - 7, true);

        int barX = bx + 7;
        int barY = by + bh - 8;
        int barW = Math.max(24, rewardX - barX - 7);
        progress(g, barX, barY, barW, 5, current / (double) target, complete ? GREEN : CYAN);
        String progressText = current + " / " + target;
        int progressW = Math.max(24, (int) Math.ceil(font.width(progressText) * metaScale) + 2);
        text(g, font, Component.literal(progressText), barX + Math.max(0, barW - progressW), barY - 8,
                complete ? GREEN : MUTED, metaScale, progressW, false);

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

    private static String categoryName(String category) {
        return switch (category) {
            case "forever" -> "Forever";
            case "ae2" -> "Applied Energistics 2";
            case "mekanism" -> "Mekanism";
            case "powah" -> "Powah";
            case "mystical_agriculture" -> "Mystical Agriculture";
            case "combat" -> "Combat & Dimensions";
            case "contracts" -> "Contracts";
            default -> "Challenge";
        };
    }

    private static void panel(GuiGraphics g, int x, int y, int w, int h, int fill, int border) {
        if (w <= 1 || h <= 1) return;
        g.fill(x + 1, y + 1, x + w - 1, y + h - 1, fill);
        outline(g, x, y, w, h, border);
        g.fill(x + 2, y + 2, x + w - 2, y + 3, 0x213CD1F5);
        g.fill(x + 2, y + h - 3, x + w - 2, y + h - 2, 0x34000000);
    }

    private static void glow(GuiGraphics g, int x, int y, int w, int h, int color) {
        int rgb = color & 0x00FFFFFF;
        g.fill(x - 2, y + 1, x - 1, y + h - 1, rgb | 0x35000000);
        g.fill(x + w + 1, y + 1, x + w + 2, y + h - 1, rgb | 0x35000000);
        g.fill(x + 1, y - 2, x + w - 1, y - 1, rgb | 0x35000000);
        g.fill(x + 1, y + h + 1, x + w - 1, y + h + 2, rgb | 0x35000000);
    }

    private static void progress(GuiGraphics g, int x, int y, int w, int h, double ratio, int color) {
        g.fill(x, y, x + w, y + h, 0xF0020910);
        outline(g, x, y, w, h, 0xFF4A6A80);
        int fill = (int) Math.round(Math.max(0, w - 2) * Math.clamp(ratio, 0D, 1D));
        if (fill > 0) {
            g.fill(x + 1, y + 1, x + 1 + fill, y + h - 1, color);
            g.fill(x + 1, y + 1, x + 1 + fill, y + 2, (color & 0x00FFFFFF) | 0xF0000000);
        }
    }

    private static void outline(GuiGraphics g, int x, int y, int w, int h, int color) {
        g.fill(x, y, x + w, y + 1, color);
        g.fill(x, y + h - 1, x + w, y + h, color);
        g.fill(x, y, x + 1, y + h, color);
        g.fill(x + w - 1, y, x + w, y + h, color);
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

    private static void wrappedText(GuiGraphics g, Font font, Component component, int x, int y, int color,
                                    float scale, int maxWidth, int maxLines) {
        int logicalWidth = Math.max(1, (int) (maxWidth / scale));
        int shown = 0;
        for (var line : font.split(component, logicalWidth)) {
            if (shown++ >= maxLines) break;
            g.pose().pushPose();
            g.pose().scale(scale, scale, 1F);
            g.drawString(font, line, Math.round(x / scale), Math.round(y / scale), color, false);
            g.pose().popPose();
            y += Math.max(6, Math.round(9 * scale));
        }
    }
}

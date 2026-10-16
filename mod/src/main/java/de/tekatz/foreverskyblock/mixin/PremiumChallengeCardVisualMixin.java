package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.client.PremiumIconResolver;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import net.minecraft.ChatFormatting;
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

/** Design-2 revision 3 challenge cards: correct integration icons, clean spacing and readable type. */
@Mixin(targets = "de.tekatz.foreverskyblock.client.PremiumSkillScreen$ChallengeCardButton")
public abstract class PremiumChallengeCardVisualMixin {
    private static final int WHITE = 0xFFF7FBFF;
    private static final int LIGHT = 0xFFD8E6F1;
    private static final int MUTED = 0xFF9AAFC0;
    private static final int CYAN = 0xFF35D2FF;
    private static final int GREEN = 0xFF43E58A;

    @Shadow @Final private MilestoneCatalog.Milestone milestone;

    @Inject(method = "renderWidget", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$renderRevision3Card(GuiGraphics g, int mouseX, int mouseY, float partialTick,
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
        int fill = complete ? 0xEC092620 : hovered ? 0xF0122A3B : 0xEF0C1C29;
        int border = complete || hovered ? accent : 0xFF29485D;

        g.fill(bx + 2, by + 3, bx + bw + 2, by + bh + 4, 0x50000000);
        panel(g, bx, by, bw, bh, fill, border);
        g.fill(bx, by + 2, bx + 2, by + bh - 2, accent);
        g.fill(bx + 2, by + 1, bx + bw - 2, by + 2, (accent & 0x00FFFFFF) | 0x30000000);
        if (hovered || complete) glow(g, bx, by, bw, bh, accent);

        int iconBox = bh >= 60 ? 26 : 24;
        int iconX = bx + 7;
        int iconY = by + 6;
        panel(g, iconX, iconY, iconBox, iconBox, 0xE0102736, 0x663A6078);
        g.fill(iconX + 1, iconY + 1, iconX + iconBox - 1, iconY + 2,
                (accent & 0x00FFFFFF) | 0x3A000000);
        ItemStack icon = PremiumIconResolver.challengeIcon(milestone);
        g.renderItem(icon, iconX + (iconBox - 16) / 2, iconY + (iconBox - 16) / 2);

        int textX = iconX + iconBox + 7;
        int rightPad = complete ? 18 : 7;
        float titleScale = bh >= 60 ? 0.74F : 0.69F;
        float bodyScale = bh >= 60 ? 0.61F : 0.57F;
        float metaScale = bh >= 60 ? 0.59F : 0.55F;
        int textWidth = Math.max(28, bw - (textX - bx) - rightPad);

        text(g, font, self.getMessage().copy().withStyle(ChatFormatting.BOLD), textX, by + 6, WHITE,
                titleScale, textWidth, true);
        text(g, font, Component.translatable(milestone.descriptionKey()), textX, by + 19, LIGHT,
                bodyScale, textWidth, false);
        text(g, font, Component.literal(categoryName(milestone.category())), textX, by + 31, accent,
                metaScale, textWidth, false);

        if (complete) {
            text(g, font, Component.literal("✔").withStyle(ChatFormatting.BOLD), bx + bw - 15, by + 5,
                    GREEN, 0.78F, 13, true);
        }

        String rewardText = "+" + milestone.reward() + " SP";
        int rewardW = Math.max(31, (int) Math.ceil(font.width(rewardText) * metaScale) + 9);
        int rewardX = bx + bw - rewardW - 6;
        int rewardY = by + bh - 18;
        panel(g, rewardX, rewardY, rewardW, 13, 0xEE123A62, 0xFF3E82C1);
        g.fill(rewardX + 1, rewardY + 1, rewardX + rewardW - 1, rewardY + 2, 0x663BCBFF);
        text(g, font, Component.literal(rewardText).withStyle(ChatFormatting.BOLD), rewardX + 4, rewardY + 3,
                WHITE, metaScale, rewardW - 7, true);

        int barX = bx + 7;
        int barY = by + bh - 8;
        int barW = Math.max(24, rewardX - barX - 7);
        progress(g, barX, barY, barW, 5, current / (double) target, complete ? GREEN : CYAN);
        String progressText = current + " / " + target;
        int progressW = Math.max(25, (int) Math.ceil(font.width(progressText) * metaScale) + 2);
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
        g.fill(x + 2, y + 2, x + w - 2, y + 3, 0x203CD1F5);
        g.fill(x + 2, y + h - 3, x + w - 2, y + h - 2, 0x32000000);
    }

    private static void glow(GuiGraphics g, int x, int y, int w, int h, int color) {
        int rgb = color & 0x00FFFFFF;
        g.fill(x - 2, y + 1, x - 1, y + h - 1, rgb | 0x30000000);
        g.fill(x + w + 1, y + 1, x + w + 2, y + h - 1, rgb | 0x30000000);
        g.fill(x + 1, y - 2, x + w - 1, y - 1, rgb | 0x30000000);
        g.fill(x + 1, y + h + 1, x + w - 1, y + h + 2, rgb | 0x30000000);
    }

    private static void progress(GuiGraphics g, int x, int y, int w, int h, double ratio, int color) {
        g.fill(x, y, x + w, y + h, 0xF0020910);
        outline(g, x, y, w, h, 0xFF4A6A80);
        int fill = (int) Math.round(Math.max(0, w - 2) * Math.clamp(ratio, 0D, 1D));
        if (fill > 0) {
            g.fill(x + 1, y + 1, x + 1 + fill, y + h - 1, color);
            g.fill(x + 1, y + 1, x + 1 + fill, y + 2, (color & 0x00FFFFFF) | 0xE8000000);
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
}

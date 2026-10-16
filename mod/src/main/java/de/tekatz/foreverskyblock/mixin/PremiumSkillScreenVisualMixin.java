package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.client.PremiumMenuLayout;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import net.minecraft.ChatFormatting;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/**
 * Design-2 revision 3 presentation layer.
 *
 * <p>The base screen remains responsible for navigation, hit boxes and progression actions. This
 * mixin fixes the mockup-facing presentation: header geometry, text hierarchy, card depth and the
 * minimum readable type size used by the large content tabs.</p>
 */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumSkillScreenVisualMixin {
    private static final int WHITE = 0xFFF6FAFF;
    private static final int LIGHT = 0xFFD6E6F4;
    private static final int MUTED = 0xFF93A9BC;
    private static final int CYAN = 0xFF2CCBFF;
    private static final int CYAN_LIGHT = 0xFF7DEBFF;
    private static final int GOLD = 0xFFFFC447;
    private static final int BORDER = 0xFF25455D;

    @Shadow private PremiumMenuLayout layout;
    @Shadow private int x;
    @Shadow private int y;

    @Inject(method = "drawShell", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$drawRevision3Shell(GuiGraphics g, CallbackInfo callback) {
        Font font = Minecraft.getInstance().font;
        int left = x + layout.padding();
        int right = x + layout.width() - layout.padding();
        int top = y + layout.padding();
        int headerBottom = y + layout.navTop() - 5;
        int headerHeight = Math.max(34, headerBottom - top);

        // Main shell: deep navy, restrained rim light and one real drop shadow.
        g.fill(x - 3, y + 4, x + layout.width() + 3, y + layout.height() + 5, 0x72000000);
        g.fill(x - 1, y - 1, x + layout.width() + 1, y + layout.height() + 1, 0xA7000710);
        panel(g, x, y, layout.width(), layout.height(), 0xED05101A, 0xFF284A62);
        g.fill(x + 2, y + 2, x + layout.width() - 2, y + 3, 0x4E65DFFF);

        boolean compact = layout.compact();
        int closeSpace = 22;
        int brandW = compact ? Math.min(142, layout.width() / 4) : 188;
        int infoW = !layout.tiny() && layout.width() >= 600 ? (compact ? 72 : 90) : 0;

        // Branding is intentionally not boxed like a stat card. The reference logo floats on the
        // dark header background; this keeps the left side visually lighter and less mechanical.
        ambientGlow(g, left + 45, top + headerHeight / 2, Math.min(78, brandW / 2), 0x231DCAFF);
        renderScaledItem(g, Items.GRASS_BLOCK.getDefaultInstance(), left + 6,
                top + Math.max(6, (headerHeight - 20) / 2), compact ? 1.18F : 1.35F);
        text(g, font, Component.literal("FOREVER").withStyle(ChatFormatting.BOLD), left + 32, top + 7,
                WHITE, compact ? 0.96F : 1.10F, brandW - 36, true);
        text(g, font, Component.literal("SKYBLOCK").withStyle(ChatFormatting.BOLD), left + 32,
                top + (compact ? 23 : 26), CYAN, compact ? 1.00F : 1.14F, brandW - 36, true);

        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        int statLeft = left + brandW + 6;
        int statRight = right - closeSpace - (infoW > 0 ? infoW + 5 : 0);
        int gap = 4;
        int available = Math.max(180, statRight - statLeft - gap * 2);
        int spW = Math.max(76, available / 4);
        int ppW = Math.max(76, available / 4);
        int tierW = Math.max(96, statRight - statLeft - spW - ppW - gap * 2);

        statCard(g, font, statLeft, top, spW, headerHeight,
                Items.DIAMOND.getDefaultInstance(), "SKILL POINTS",
                ready ? Integer.toString(state.points()) : "—", "Use SP to unlock skills.", CYAN, 0x2519BFEF);
        statCard(g, font, statLeft + spW + gap, top, ppW, headerHeight,
                Items.NETHER_STAR.getDefaultInstance(), "PERK POINTS",
                ready ? Integer.toString(state.perkPoints()) : "—", "Spend PP on global perks.", GOLD, 0x27FFB72A);
        tierCard(g, font, statLeft + spW + ppW + gap * 2, top, tierW, headerHeight, state);

        if (infoW > 0) {
            int ix = statRight + 5;
            panel(g, ix, top, infoW, headerHeight, 0xE20E2031, 0xFF294D69);
            g.fill(ix + 1, top + 1, ix + infoW - 1, top + 2, 0x3F37CFFF);

            renderScaledItem(g, Items.CLOCK.getDefaultInstance(), ix + 5, top + 5, 0.92F);
            text(g, font, Component.literal("PLAYTIME"), ix + 23, top + 4, MUTED, 0.56F, infoW - 27, false);
            text(g, font, Component.literal("—").withStyle(ChatFormatting.BOLD), ix + 23, top + 15,
                    WHITE, 0.76F, infoW - 27, true);

            if (headerHeight >= 43) {
                renderScaledItem(g, Items.GRASS_BLOCK.getDefaultInstance(), ix + 5, top + 28, 0.92F);
                text(g, font, Component.literal("ISLAND LEVEL"), ix + 23, top + 27, MUTED, 0.54F, infoW - 27, false);
                text(g, font, Component.literal("—").withStyle(ChatFormatting.BOLD), ix + 23, top + 38,
                        WHITE, 0.76F, infoW - 27, true);
            }
        }

        int navTop = y + layout.navTop();
        g.fill(left, navTop - 2, right, navTop, 0x382AC7FA);
        panel(g, left, navTop, right - left, layout.navHeight(), 0xE006141F, 0xFF294B61);
        g.fill(left + 1, navTop + 1, right - 1, navTop + 2, 0x242FC8F5);
        callback.cancel();
    }

    /**
     * The original compact path shrank large-card text to ~0.52-0.64x. Keep small toolbar controls
     * compact, but prevent wide content labels from becoming unreadable at GUI scale 3.
     */
    @Inject(method = "drawScaled", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$keepWideTextReadable(GuiGraphics g, Component component, int dx, int dy, int color,
                                                       float scale, int maxWidth, CallbackInfo callback) {
        if (layout == null || layout.tiny() || maxWidth < 88 || scale >= 0.72F) return;
        text(g, Minecraft.getInstance().font, component, dx, dy, color, 0.72F, maxWidth, false);
        callback.cancel();
    }

    @Inject(method = "drawWrappedScaled", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$keepWrappedTextReadable(GuiGraphics g, Component component, int dx, int dy,
                                                          int maxWidth, int color, float scale, int maxLines,
                                                          CallbackInfoReturnable<Integer> callback) {
        if (layout == null || layout.tiny() || maxWidth < 88 || scale >= 0.69F) return;
        Font font = Minecraft.getInstance().font;
        float adjusted = 0.69F;
        int logicalWidth = Math.max(1, (int) (maxWidth / adjusted));
        int shown = 0;
        int lineY = dy;
        for (var line : font.split(component, logicalWidth)) {
            if (shown++ >= maxLines) break;
            g.pose().pushPose();
            g.pose().scale(adjusted, adjusted, 1F);
            g.drawString(font, line, Math.round(dx / adjusted), Math.round(lineY / adjusted), color, false);
            g.pose().popPose();
            lineY += Math.max(7, Math.round(10 * adjusted));
        }
        callback.setReturnValue(lineY);
    }

    @Inject(method = "softPanel", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$softPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border,
                                                   CallbackInfo callback) {
        if (pw <= 1 || ph <= 1) {
            callback.cancel();
            return;
        }
        g.fill(px + 2, py + 3, px + pw + 2, py + ph + 3, 0x38000000);
        g.fill(px + 1, py + 1, px + pw - 1, py + ph - 1, fill);
        g.fill(px + 2, py + 2, px + pw - 2, py + 3, 0x213DB7D8);
        g.fill(px + 2, py + ph - 3, px + pw - 2, py + ph - 2, 0x30000000);
        outline(g, px, py, pw, ph, border);
        callback.cancel();
    }

    @Inject(method = "shadowPanel", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$shadowPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border,
                                                     CallbackInfo callback) {
        g.fill(px - 3, py + 3, px + pw + 3, py + ph + 5, 0x62000000);
        panel(g, px, py, pw, ph, fill, border);
        callback.cancel();
    }

    @Inject(method = "glowOutline", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$glowOutline(GuiGraphics g, int px, int py, int pw, int ph, int color,
                                                     CallbackInfo callback) {
        int rgb = color & 0x00FFFFFF;
        g.fill(px - 2, py, px - 1, py + ph, rgb | 0x3B000000);
        g.fill(px + pw + 1, py, px + pw + 2, py + ph, rgb | 0x3B000000);
        g.fill(px, py - 2, px + pw, py - 1, rgb | 0x3B000000);
        g.fill(px, py + ph + 1, px + pw, py + ph + 2, rgb | 0x3B000000);
        outline(g, px, py, pw, ph, color);
        callback.cancel();
    }

    @Inject(method = "progressBar", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$progressBar(GuiGraphics g, int px, int py, int pw, int ph, double ratio, int color,
                                                     CallbackInfo callback) {
        int h = Math.max(4, ph);
        g.fill(px + 1, py + 2, px + pw + 1, py + h + 2, 0x3B000000);
        g.fill(px, py, px + pw, py + h, 0xF0020910);
        outline(g, px, py, pw, h, 0xFF466980);
        int inner = Math.max(0, pw - 2);
        int fillWidth = (int) Math.round(inner * Math.clamp(ratio, 0D, 1D));
        if (fillWidth > 0) {
            g.fill(px + 1, py + 1, px + 1 + fillWidth, py + h - 1, color);
            if (h >= 4) g.fill(px + 1, py + 1, px + 1 + fillWidth, py + 2,
                    (color & 0x00FFFFFF) | 0xE8000000);
        }
        callback.cancel();
    }

    private static void statCard(GuiGraphics g, Font font, int sx, int sy, int sw, int sh, ItemStack icon,
                                 String label, String value, String subtitle, int accent, int aura) {
        panel(g, sx, sy, sw, sh, 0xE20B1A28, 0xFF294D66);
        g.fill(sx + 1, sy + 1, sx + sw - 1, sy + 2, aura);

        int box = 26;
        int iconX = sx + 5;
        int iconY = sy + Math.max(5, (sh - box) / 2);
        ambientGlow(g, iconX + box / 2, iconY + box / 2, 24, aura);
        panel(g, iconX, iconY, box, box, 0x8D102A3A, 0x352E667E);
        renderScaledItem(g, icon, iconX + 4, iconY + 4, 1.08F);

        int tx = sx + 36;
        int tw = Math.max(20, sw - 41);
        text(g, font, Component.literal(label), tx, sy + 5, MUTED, 0.59F, tw, false);
        text(g, font, Component.literal(value).withStyle(ChatFormatting.BOLD), tx, sy + 17,
                accent, 0.90F, tw, true);
        if (sh >= 45) text(g, font, Component.literal(subtitle), tx, sy + 35, LIGHT, 0.50F, tw, false);
    }

    private static void tierCard(GuiGraphics g, Font font, int sx, int sy, int sw, int sh,
                                 de.tekatz.foreverskyblock.network.SkillNetwork.Snapshot state) {
        panel(g, sx, sy, sw, sh, 0xE20B1A28, 0xFF294D66);
        g.fill(sx + 1, sy + 1, sx + sw - 1, sy + 2, 0x323BCBFA);

        boolean ready = state != null && state.available();
        int tier = ready ? state.shopTier() : -1;
        drawTierBadge(g, font, sx + 6, sy + Math.max(4, (sh - 32) / 2), tier);

        int tx = sx + 41;
        int tw = Math.max(28, sw - 48);
        text(g, font, Component.literal("TIER STATUS"), tx, sy + 4, MUTED, 0.58F, tw, false);
        String tierText = switch (tier) {
            case 0 -> "Tier 0 — Foundations";
            case 1 -> "Tier 1 — Conqueror";
            case 2 -> "Tier 2 — Engineer";
            case 3 -> "Tier 3 — Mastery";
            case 4 -> "Tier 4 — Ascended";
            default -> "—";
        };
        text(g, font, Component.literal(tierText).withStyle(ChatFormatting.BOLD), tx, sy + 15,
                WHITE, 0.76F, tw, true);

        if (ready && sh >= 42) {
            int done = (int) MilestoneCatalog.MILESTONES.stream()
                    .filter(m -> state.milestones().contains(m.id())).count();
            int total = Math.max(1, MilestoneCatalog.MILESTONES.size());
            int barX = tx;
            int barW = Math.max(24, sw - (tx - sx) - 8);
            int barY = sy + sh - 9;
            progress(g, barX, barY, barW, 5, done / (double) total, GOLD);
            String count = done + " / " + total;
            int countW = Math.max(28, (int) Math.ceil(font.width(count) * 0.53F) + 2);
            text(g, font, Component.literal(count), barX + Math.max(0, barW - countW), barY - 8,
                    LIGHT, 0.53F, countW, false);
        }
    }

    private static void drawTierBadge(GuiGraphics g, Font font, int bx, int by, int tier) {
        int bronze = 0xFFB96832;
        int bronzeLight = 0xFFE09A5A;
        int bronzeDark = 0xFF65351F;
        g.fill(bx + 5, by, bx + 21, by + 2, bronzeDark);
        g.fill(bx + 2, by + 2, bx + 24, by + 5, bronze);
        g.fill(bx, by + 5, bx + 26, by + 27, bronze);
        g.fill(bx + 2, by + 27, bx + 24, by + 30, bronzeDark);
        g.fill(bx + 5, by + 30, bx + 21, by + 32, bronzeDark);
        g.fill(bx + 4, by + 6, bx + 22, by + 25, 0xFF7B4328);
        outline(g, bx + 4, by + 6, 18, 19, bronzeLight);
        g.fill(bx + 6, by + 8, bx + 20, by + 9, 0x66FFD29B);
        String value = tier >= 0 ? Integer.toString(tier) : "—";
        float scale = 0.72F;
        int logical = font.width(value);
        int px = bx + 13 - Math.round(logical * scale / 2F);
        text(g, font, Component.literal(value).withStyle(ChatFormatting.BOLD), px, by + 12,
                WHITE, scale, 14, true);
    }

    private static void panel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border) {
        if (pw <= 1 || ph <= 1) return;
        g.fill(px + 1, py + 1, px + pw - 1, py + ph - 1, fill);
        outline(g, px, py, pw, ph, border == 0 ? BORDER : border);
        g.fill(px + 2, py + 2, px + pw - 2, py + 3, 0x183CD1F5);
        g.fill(px + 2, py + ph - 3, px + pw - 2, py + ph - 2, 0x28000000);
    }

    private static void outline(GuiGraphics g, int ox, int oy, int ow, int oh, int color) {
        if (ow <= 0 || oh <= 0) return;
        g.fill(ox, oy, ox + ow, oy + 1, color);
        g.fill(ox, oy + oh - 1, ox + ow, oy + oh, color);
        g.fill(ox, oy, ox + 1, oy + oh, color);
        g.fill(ox + ow - 1, oy, ox + ow, oy + oh, color);
    }

    private static void progress(GuiGraphics g, int px, int py, int pw, int ph, double ratio, int color) {
        g.fill(px, py, px + pw, py + ph, 0xF0020910);
        outline(g, px, py, pw, ph, 0xFF496B80);
        int fill = (int) Math.round(Math.max(0, pw - 2) * Math.clamp(ratio, 0D, 1D));
        if (fill > 0) {
            g.fill(px + 1, py + 1, px + 1 + fill, py + ph - 1, color);
            g.fill(px + 1, py + 1, px + 1 + fill, py + 2, (color & 0x00FFFFFF) | 0xE8000000);
        }
    }

    private static void text(GuiGraphics g, Font font, Component component, int dx, int dy, int color,
                             float scale, int maxWidth, boolean shadow) {
        String value = component.getString();
        int logicalWidth = Math.max(1, (int) (maxWidth / scale));
        value = font.plainSubstrByWidth(value, logicalWidth);
        g.pose().pushPose();
        g.pose().scale(scale, scale, 1F);
        g.drawString(font, value, Math.round(dx / scale), Math.round(dy / scale), color, shadow);
        g.pose().popPose();
    }

    private static void renderScaledItem(GuiGraphics g, ItemStack stack, int dx, int dy, float scale) {
        g.pose().pushPose();
        g.pose().translate(dx, dy, 0);
        g.pose().scale(scale, scale, 1F);
        g.renderItem(stack, 0, 0);
        g.pose().popPose();
    }

    private static void ambientGlow(GuiGraphics g, int cx, int cy, int radius, int color) {
        int rgb = color & 0x00FFFFFF;
        for (int i = 4; i >= 1; i--) {
            int r = Math.max(2, radius * i / 4);
            int alpha = 6 + (4 - i) * 4;
            g.fill(cx - r, cy - r / 2, cx + r, cy + r / 2, rgb | (alpha << 24));
        }
    }
}

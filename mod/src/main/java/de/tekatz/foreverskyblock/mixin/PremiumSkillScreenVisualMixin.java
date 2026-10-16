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

/**
 * Design-2 presentation layer for the K-menu.
 *
 * <p>The underlying screen continues to own layout, controls and authoritative progression data.
 * This mixin only refines visual hierarchy, spacing, depth and mockup-style header treatment.</p>
 */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumSkillScreenVisualMixin {
    private static final int WHITE = 0xFFF6FAFF;
    private static final int LIGHT = 0xFFD6E6F4;
    private static final int MUTED = 0xFF93A9BC;
    private static final int DIM = 0xFF60788D;
    private static final int CYAN = 0xFF2CCBFF;
    private static final int GOLD = 0xFFFFC447;
    private static final int BORDER = 0xFF25455D;

    @Shadow private PremiumMenuLayout layout;
    @Shadow private int x;
    @Shadow private int y;

    @Inject(method = "drawShell", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$drawDesign2Shell(GuiGraphics g, CallbackInfo callback) {
        Font font = Minecraft.getInstance().font;
        int left = x + layout.padding();
        int right = x + layout.width() - layout.padding();
        int top = y + layout.padding();
        int headerBottom = y + layout.navTop() - 5;
        int headerHeight = Math.max(34, headerBottom - top);

        g.fill(x - 3, y + 4, x + layout.width() + 3, y + layout.height() + 5, 0x76000000);
        g.fill(x - 1, y - 1, x + layout.width() + 1, y + layout.height() + 1, 0xA9000710);
        panel(g, x, y, layout.width(), layout.height(), 0xE405101B, 0xFF284A62);
        g.fill(x + 2, y + 2, x + layout.width() - 2, y + 3, 0x5A65DFFF);
        g.fill(x + 2, y + layout.height() - 3, x + layout.width() - 2, y + layout.height() - 2, 0x40000000);

        boolean compact = layout.compact();
        int closeSpace = 22;
        int infoW = !layout.tiny() && layout.width() >= 600 ? (compact ? 76 : 98) : 0;
        int brandW = compact ? Math.min(146, layout.width() / 4) : 198;

        // Brand panel: keep the hierarchy clean. The previous decorative sparkles looked detached
        // from the logo at scale 3, so the visual weight now comes from the block, type and glow.
        panel(g, left, top, brandW, headerHeight, 0xB9091825, 0x00234258);
        ambientGlow(g, left + 18, top + headerHeight / 2, Math.min(50, brandW / 3), 0x241DCAFF);
        renderScaledItem(g, Items.GRASS_BLOCK.getDefaultInstance(), left + 7,
                top + Math.max(5, (headerHeight - 23) / 2), compact ? 1.35F : 1.48F);
        text(g, font, Component.literal("FOREVER").withStyle(ChatFormatting.BOLD), left + 34, top + 7,
                WHITE, compact ? 0.90F : 1.08F, brandW - 39, true);
        text(g, font, Component.literal("SKYBLOCK").withStyle(ChatFormatting.BOLD), left + 34,
                top + (compact ? 21 : 25), CYAN, compact ? 1.02F : 1.14F, brandW - 39, true);
        if (!compact) {
            text(g, font, Component.literal("SKILLS  •  CHALLENGES  •  PROGRESS"), left + 34, top + 45,
                    DIM, 0.52F, brandW - 40, false);
        }

        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        int statLeft = left + brandW + 5;
        int statRight = right - closeSpace - (infoW > 0 ? infoW + 5 : 0);
        int gap = 4;
        int available = Math.max(120, statRight - statLeft - gap * 2);
        int spW = Math.max(62, available / 4);
        int ppW = Math.max(62, available / 4);
        int tierW = Math.max(76, statRight - statLeft - spW - ppW - gap * 2);

        statCard(g, font, statLeft, top, spW, headerHeight,
                Items.DIAMOND.getDefaultInstance(), "SKILL POINTS", ready ? Integer.toString(state.points()) : "—",
                "Use SP to unlock skills.", CYAN, 0x2419BFEF);
        statCard(g, font, statLeft + spW + gap, top, ppW, headerHeight,
                Items.NETHER_STAR.getDefaultInstance(), "PERK POINTS", ready ? Integer.toString(state.perkPoints()) : "—",
                "Spend PP on global perks.", GOLD, 0x2AFFB72A);
        tierCard(g, font, statLeft + spW + ppW + gap * 2, top, tierW, headerHeight, state);

        if (infoW > 0) {
            int ix = statRight + 5;
            panel(g, ix, top, infoW, headerHeight, 0xDA102237, 0xFF294D69);
            g.fill(ix + 1, top + 1, ix + infoW - 1, top + 2, 0x4437CFFF);

            renderScaledItem(g, Items.CLOCK.getDefaultInstance(), ix + 5, top + 5, 0.78F);
            text(g, font, Component.literal("PLAYTIME"), ix + 21, top + 5, MUTED, 0.48F, infoW - 24, false);
            text(g, font, Component.literal("—").withStyle(ChatFormatting.BOLD), ix + 21, top + 14,
                    WHITE, 0.66F, infoW - 24, true);

            if (headerHeight >= 46) {
                renderScaledItem(g, Items.GRASS_BLOCK.getDefaultInstance(), ix + 5, top + 29, 0.76F);
                text(g, font, Component.literal("ISLAND LEVEL"), ix + 21, top + 28, MUTED, 0.46F,
                        infoW - 24, false);
                text(g, font, Component.literal("—").withStyle(ChatFormatting.BOLD), ix + 21, top + 37,
                        WHITE, 0.66F, infoW - 24, true);
            }
        }

        int navTop = y + layout.navTop();
        g.fill(left, navTop - 2, right, navTop, 0x3E2AC7FA);
        panel(g, left, navTop, right - left, layout.navHeight(), 0xD8071420, 0xFF294B61);
        g.fill(left + 1, navTop + 1, right - 1, navTop + 2, 0x262FC8F5);
        callback.cancel();
    }

    @Inject(method = "drawChallengeChrome", at = @At("TAIL"))
    private void foreverSkyblock$challengeAtmosphere(GuiGraphics g, CallbackInfo callback) {
        int mx = x + layout.challengeMainX();
        int mw = layout.challengeMainWidth();
        int gridTop = y + layout.challengeGridTop();
        int bottom = y + layout.contentBottom();
        if (mw < 80 || bottom - gridTop < 30) return;

        // Low-contrast geometry supplies depth behind the cards without competing with text.
        int base = bottom - 3;
        int start = mx + Math.max(8, mw / 9);
        for (int i = 0; i < 8; i++) {
            int w = 8 + (i % 3) * 5;
            int h = 8 + ((i * 7) % Math.max(10, Math.min(34, bottom - gridTop - 6)));
            int px = start + i * Math.max(13, (mw - 30) / 9);
            if (px + w >= mx + mw - 6) break;
            g.fill(px, base - h, px + w, base, 0x0C1E6A83);
            if ((i & 1) == 0) g.fill(px + 2, base - h + 3, px + w - 2, base - h + 4, 0x103AC4E8);
        }
        g.fill(mx + 7, gridTop - 1, mx + mw - 7, gridTop, 0x2737C7F2);
    }

    @Inject(method = "softPanel", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$softPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border,
                                                   CallbackInfo callback) {
        if (pw <= 1 || ph <= 1) {
            callback.cancel();
            return;
        }
        g.fill(px + 2, py + 3, px + pw + 2, py + ph + 3, 0x36000000);
        g.fill(px + 1, py + 1, px + pw - 1, py + ph - 1, fill);
        g.fill(px + 2, py + 2, px + pw - 2, py + 3, 0x203DB7D8);
        g.fill(px + 2, py + ph - 3, px + pw - 2, py + ph - 2, 0x34000000);
        outline(g, px, py, pw, ph, border);
        callback.cancel();
    }

    @Inject(method = "shadowPanel", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$shadowPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border,
                                                     CallbackInfo callback) {
        g.fill(px - 3, py + 3, px + pw + 3, py + ph + 5, 0x64000000);
        g.fill(px - 1, py - 1, px + pw + 1, py + ph + 1, 0x3025BCE8);
        panel(g, px, py, pw, ph, fill, border);
        callback.cancel();
    }

    @Inject(method = "glowOutline", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$glowOutline(GuiGraphics g, int px, int py, int pw, int ph, int color,
                                                     CallbackInfo callback) {
        int rgb = color & 0x00FFFFFF;
        g.fill(px - 3, py + 1, px - 2, py + ph - 1, rgb | 0x1C000000);
        g.fill(px + pw + 2, py + 1, px + pw + 3, py + ph - 1, rgb | 0x1C000000);
        g.fill(px + 1, py - 3, px + pw - 1, py - 2, rgb | 0x1C000000);
        g.fill(px + 1, py + ph + 2, px + pw - 1, py + ph + 3, rgb | 0x1C000000);
        g.fill(px - 2, py, px - 1, py + ph, rgb | 0x41000000);
        g.fill(px + pw + 1, py, px + pw + 2, py + ph, rgb | 0x41000000);
        g.fill(px, py - 2, px + pw, py - 1, rgb | 0x41000000);
        g.fill(px, py + ph + 1, px + pw, py + ph + 2, rgb | 0x41000000);
        outline(g, px, py, pw, ph, color);
        callback.cancel();
    }

    @Inject(method = "progressBar", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$progressBar(GuiGraphics g, int px, int py, int pw, int ph, double ratio, int color,
                                                     CallbackInfo callback) {
        int h = Math.max(4, ph);
        g.fill(px + 1, py + 2, px + pw + 1, py + h + 2, 0x40000000);
        g.fill(px, py, px + pw, py + h, 0xE1020A11);
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

    private static void tierCard(GuiGraphics g, Font font, int sx, int sy, int sw, int sh,
                                 de.tekatz.foreverskyblock.network.SkillNetwork.Snapshot state) {
        panel(g, sx, sy, sw, sh, 0xDD0B1A28, 0xFF294D66);
        g.fill(sx + 1, sy + 1, sx + sw - 1, sy + 2, 0x323BCBFA);
        boolean ready = state != null && state.available();
        int tier = ready ? state.shopTier() : -1;

        drawTierBadge(g, font, sx + 6, sy + Math.max(5, (sh - 18) / 2), tier);
        int tx = sx + 30;
        text(g, font, Component.literal("TIER STATUS"), tx, sy + 4, MUTED,
                sh < 48 ? 0.49F : 0.58F, sw - 36, false);

        String tierText = switch (tier) {
            case 0 -> "Tier 0 - Foundations";
            case 1 -> "Tier 1 - Conqueror";
            case 2 -> "Tier 2 - Engineer";
            case 3 -> "Tier 3 - Mastery";
            case 4 -> "Tier 4 - Ascended";
            default -> "—";
        };
        text(g, font, Component.literal(tierText).withStyle(ChatFormatting.BOLD), tx,
                sy + (sh < 48 ? 15 : 17), WHITE, sh < 48 ? 0.62F : 0.72F, sw - 36, true);

        if (ready && sh >= 40) {
            int done = (int) MilestoneCatalog.MILESTONES.stream().filter(m -> state.milestones().contains(m.id())).count();
            int total = Math.max(1, MilestoneCatalog.MILESTONES.size());
            int barX = tx;
            int barW = Math.max(22, sw - (tx - sx) - 8);
            int barY = sy + sh - 9;
            progress(g, barX, barY, barW, 4, done / (double) total, GOLD);
            text(g, font, Component.literal(done + " / " + total), barX + Math.max(0, barW - 34), barY - 8,
                    LIGHT, 0.48F, 34, false);
        }
    }

    private static void statCard(GuiGraphics g, Font font, int sx, int sy, int sw, int sh, ItemStack icon,
                                 String label, String value, String subtitle, int color, int aura) {
        panel(g, sx, sy, sw, sh, 0xDD0B1A28, 0xFF294D66);
        g.fill(sx + 1, sy + 1, sx + sw - 1, sy + 2, aura);
        ambientGlow(g, sx + 13, sy + sh / 2, 21, aura);
        renderScaledItem(g, icon, sx + 5, sy + Math.max(5, (sh - 19) / 2), 1.08F);
        int tx = sx + 26;
        text(g, font, Component.literal(label), tx, sy + 4, MUTED,
                sh < 48 ? 0.48F : 0.58F, sw - 31, false);
        text(g, font, Component.literal(value).withStyle(ChatFormatting.BOLD), tx,
                sy + (sh < 48 ? 15 : 16), color, sh < 48 ? 0.72F : 0.86F, sw - 31, true);
        if (subtitle != null && sh >= 46) {
            text(g, font, Component.literal(subtitle), tx, sy + 32, LIGHT, 0.41F, sw - 31, false);
        }
    }

    private static void drawTierBadge(GuiGraphics g, Font font, int bx, int by, int tier) {
        int dark = 0xFF6B351A;
        int bronze = 0xFFC87835;
        int light = 0xFFFFB464;
        g.fill(bx + 4, by, bx + 14, by + 18, dark);
        g.fill(bx + 2, by + 2, bx + 16, by + 16, dark);
        g.fill(bx, by + 5, bx + 18, by + 13, dark);
        g.fill(bx + 4, by + 2, bx + 14, by + 16, bronze);
        g.fill(bx + 2, by + 5, bx + 16, by + 13, bronze);
        g.fill(bx + 5, by + 3, bx + 13, by + 4, light);
        String badge = tier < 0 ? "?" : Integer.toString(tier);
        int raw = Math.max(1, font.width(badge));
        float scale = 0.56F;
        int tw = Math.round(raw * scale);
        text(g, font, Component.literal(badge).withStyle(ChatFormatting.BOLD), bx + 9 - tw / 2, by + 6,
                WHITE, scale, 12, true);
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
        g.fill(px, py, px + pw, py + ph, 0xE1020A11);
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

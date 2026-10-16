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
 * Final Design-2 polish layer for the K-menu.
 *
 * <p>The underlying screen keeps owning layout, controls and data flow. This mixin deliberately
 * changes only presentation: the mockup-style header, richer panel depth, glow treatment and
 * progress bars. Keeping the visual pass isolated avoids touching progression behaviour.</p>
 */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumSkillScreenVisualMixin {
    private static final int WHITE = 0xFFF6FAFF;
    private static final int LIGHT = 0xFFD6E6F4;
    private static final int MUTED = 0xFF93A9BC;
    private static final int DIM = 0xFF60788D;
    private static final int CYAN = 0xFF2CCBFF;
    private static final int CYAN_LIGHT = 0xFF7DEBFF;
    private static final int GOLD = 0xFFFFC447;
    private static final int GREEN = 0xFF48E58A;
    private static final int NAVY = 0xE607121E;
    private static final int PANEL = 0xE80A1825;
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

        // Deep translucent shell with a cool inner rim and a real drop shadow.
        g.fill(x - 3, y + 4, x + layout.width() + 3, y + layout.height() + 5, 0x76000000);
        g.fill(x - 1, y - 1, x + layout.width() + 1, y + layout.height() + 1, 0xA9000710);
        panel(g, x, y, layout.width(), layout.height(), 0xE405101B, 0xFF284A62);
        g.fill(x + 2, y + 2, x + layout.width() - 2, y + 3, 0x5A65DFFF);
        g.fill(x + 2, y + layout.height() - 3, x + layout.width() - 2, y + layout.height() - 2, 0x40000000);

        boolean compact = layout.compact();
        int closeSpace = 22;
        int infoW = !layout.tiny() && layout.width() >= 600 ? (compact ? 74 : 98) : 0;
        int brandW = compact ? Math.min(146, layout.width() / 4) : 198;

        // Logo / identity area. The larger type hierarchy mirrors the approved mockup instead of
        // looking like two ordinary text labels.
        panel(g, left, top, brandW, headerHeight, 0xB9091825, 0x00234258);
        ambientGlow(g, left + 17, top + headerHeight / 2, Math.min(54, brandW / 3), 0x2A1DCAFF);
        renderScaledItem(g, Items.GRASS_BLOCK.getDefaultInstance(), left + 7, top + Math.max(5, (headerHeight - 23) / 2), 1.35F);
        float logoScale = compact ? 0.92F : 1.08F;
        text(g, font, Component.literal("FOREVER").withStyle(ChatFormatting.BOLD), left + 34, top + 7,
                WHITE, logoScale, brandW - 39, true);
        text(g, font, Component.literal("SKYBLOCK").withStyle(ChatFormatting.BOLD), left + 34,
                top + (compact ? 22 : 25), CYAN, compact ? 0.96F : 1.14F, brandW - 39, true);
        if (!compact) {
            text(g, font, Component.literal("SKILLS  •  CHALLENGES  •  PROGRESS"), left + 34, top + 45,
                    DIM, 0.52F, brandW - 40, false);
        }
        sparkle(g, left + brandW - 17, top + 8, CYAN_LIGHT);
        sparkle(g, left + brandW - 29, top + headerHeight - 13, 0xFF6AAFD0);

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
                compact ? null : "Use SP to unlock skills.", CYAN, 0x2419BFEF);
        statCard(g, font, statLeft + spW + gap, top, ppW, headerHeight,
                Items.NETHER_STAR.getDefaultInstance(), "PERK POINTS", ready ? Integer.toString(state.perkPoints()) : "—",
                compact ? null : "Spend PP on global perks.", GOLD, 0x2AFFB72A);
        tierCard(g, font, statLeft + spW + ppW + gap * 2, top, tierW, headerHeight, state);

        if (infoW > 0) {
            int ix = statRight + 5;
            panel(g, ix, top, infoW, headerHeight, 0xDA102237, 0xFF294D69);
            g.fill(ix + 1, top + 1, ix + infoW - 1, top + 2, 0x4437CFFF);
            renderScaledItem(g, Items.CLOCK.getDefaultInstance(), ix + 5, top + 6, 0.82F);
            text(g, font, Component.literal("PLAYTIME"), ix + 22, top + 5, MUTED, 0.50F, infoW - 25, false);
            text(g, font, Component.literal("—").withStyle(ChatFormatting.BOLD), ix + 22, top + 14, WHITE, 0.69F, infoW - 25, true);
            if (headerHeight >= 48) {
                renderScaledItem(g, Items.GRASS_BLOCK.getDefaultInstance(), ix + 5, top + 29, 0.82F);
                text(g, font, Component.literal("ISLAND LEVEL"), ix + 22, top + 28, MUTED, 0.50F, infoW - 25, false);
                text(g, font, Component.literal("—").withStyle(ChatFormatting.BOLD), ix + 22, top + 37, WHITE, 0.69F, infoW - 25, true);
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

        // Faint blocky skyline in the empty space between cards. It gives the challenge area the
        // layered scenic depth of the mockup while keeping card text perfectly readable.
        int base = bottom - 3;
        int start = mx + Math.max(8, mw / 9);
        for (int i = 0; i < 8; i++) {
            int w = 8 + (i % 3) * 5;
            int h = 8 + ((i * 7) % Math.max(10, Math.min(34, bottom - gridTop - 6)));
            int px = start + i * Math.max(13, (mw - 30) / 9);
            if (px + w >= mx + mw - 6) break;
            g.fill(px, base - h, px + w, base, 0x101E6A83);
            if ((i & 1) == 0) g.fill(px + 2, base - h + 3, px + w - 2, base - h + 4, 0x123AC4E8);
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
        g.fill(px + 2, py + 3, px + pw + 2, py + ph + 3, 0x3F000000);
        g.fill(px + 1, py + 1, px + pw - 1, py + ph - 1, fill);
        g.fill(px + 2, py + 2, px + pw - 2, py + 3, 0x243DB7D8);
        g.fill(px + 2, py + ph - 3, px + pw - 2, py + ph - 2, 0x38000000);
        outline(g, px, py, pw, ph, border);
        callback.cancel();
    }

    @Inject(method = "shadowPanel", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$shadowPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border,
                                                     CallbackInfo callback) {
        g.fill(px - 3, py + 3, px + pw + 3, py + ph + 5, 0x6A000000);
        g.fill(px - 1, py - 1, px + pw + 1, py + ph + 1, 0x3425BCE8);
        panel(g, px, py, pw, ph, fill, border);
        callback.cancel();
    }

    @Inject(method = "glowOutline", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$glowOutline(GuiGraphics g, int px, int py, int pw, int ph, int color,
                                                     CallbackInfo callback) {
        int rgb = color & 0x00FFFFFF;
        g.fill(px - 3, py + 1, px - 2, py + ph - 1, rgb | 0x21000000);
        g.fill(px + pw + 2, py + 1, px + pw + 3, py + ph - 1, rgb | 0x21000000);
        g.fill(px + 1, py - 3, px + pw - 1, py - 2, rgb | 0x21000000);
        g.fill(px + 1, py + ph + 2, px + pw - 1, py + ph + 3, rgb | 0x21000000);
        g.fill(px - 2, py, px - 1, py + ph, rgb | 0x47000000);
        g.fill(px + pw + 1, py, px + pw + 2, py + ph, rgb | 0x47000000);
        g.fill(px, py - 2, px + pw, py - 1, rgb | 0x47000000);
        g.fill(px, py + ph + 1, px + pw, py + ph + 2, rgb | 0x47000000);
        outline(g, px, py, pw, ph, color);
        callback.cancel();
    }

    @Inject(method = "progressBar", at = @At("HEAD"), cancellable = true)
    private static void foreverSkyblock$progressBar(GuiGraphics g, int px, int py, int pw, int ph, double ratio, int color,
                                                     CallbackInfo callback) {
        int h = Math.max(4, ph);
        g.fill(px + 1, py + 2, px + pw + 1, py + h + 2, 0x45000000);
        g.fill(px, py, px + pw, py + h, 0xE1020A11);
        outline(g, px, py, pw, h, 0xFF466980);
        int inner = Math.max(0, pw - 2);
        int fillWidth = (int) Math.round(inner * Math.clamp(ratio, 0D, 1D));
        if (fillWidth > 0) {
            g.fill(px + 1, py + 1, px + 1 + fillWidth, py + h - 1, color);
            if (h >= 4) g.fill(px + 1, py + 1, px + 1 + fillWidth, py + 2, (color & 0x00FFFFFF) | 0xE8000000);
        }
        callback.cancel();
    }

    private static void tierCard(GuiGraphics g, Font font, int sx, int sy, int sw, int sh,
                                 de.tekatz.foreverskyblock.network.SkillNetwork.Snapshot state) {
        panel(g, sx, sy, sw, sh, 0xDD0B1A28, 0xFF294D66);
        g.fill(sx + 1, sy + 1, sx + sw - 1, sy + 2, 0x323BCBFA);
        renderScaledItem(g, Items.COPPER_INGOT.getDefaultInstance(), sx + 6, sy + Math.max(5, (sh - 16) / 2), 1.0F);
        int tx = sx + 27;
        text(g, font, Component.literal("TIER STATUS"), tx, sy + 4, MUTED, sh < 48 ? 0.51F : 0.60F, sw - 33, false);
        boolean ready = state != null && state.available();
        int tier = ready ? state.shopTier() : -1;
        String tierText = switch (tier) {
            case 0 -> "0 — Foundations";
            case 1 -> "1 — Conqueror";
            case 2 -> "2 — Engineer";
            case 3 -> "3 — Mastery";
            case 4 -> "4 — Ascended";
            default -> "—";
        };
        text(g, font, Component.literal(tierText).withStyle(ChatFormatting.BOLD), tx, sy + (sh < 48 ? 15 : 17), WHITE,
                sh < 48 ? 0.66F : 0.76F, sw - 33, true);
        if (ready && sh >= 42) {
            int done = (int) MilestoneCatalog.MILESTONES.stream().filter(m -> state.milestones().contains(m.id())).count();
            int total = Math.max(1, MilestoneCatalog.MILESTONES.size());
            int barX = tx;
            int barW = Math.max(22, sw - (tx - sx) - 8);
            int barY = sy + sh - 10;
            progress(g, barX, barY, barW, 5, done / (double) total, GOLD);
            text(g, font, Component.literal(done + " / " + total), barX + Math.max(0, barW - 34), barY - 8,
                    LIGHT, 0.50F, 34, false);
        }
    }

    private static void statCard(GuiGraphics g, Font font, int sx, int sy, int sw, int sh, ItemStack icon,
                                 String label, String value, String subtitle, int color, int aura) {
        panel(g, sx, sy, sw, sh, 0xDD0B1A28, 0xFF294D66);
        g.fill(sx + 1, sy + 1, sx + sw - 1, sy + 2, aura);
        ambientGlow(g, sx + 13, sy + sh / 2, 23, aura);
        renderScaledItem(g, icon, sx + 5, sy + Math.max(5, (sh - 19) / 2), 1.12F);
        int tx = sx + 26;
        text(g, font, Component.literal(label), tx, sy + 4, MUTED, sh < 48 ? 0.50F : 0.60F, sw - 31, false);
        text(g, font, Component.literal(value).withStyle(ChatFormatting.BOLD), tx, sy + (sh < 48 ? 15 : 17), color,
                sh < 48 ? 0.76F : 0.92F, sw - 31, true);
        if (subtitle != null && sh >= 54) {
            text(g, font, Component.literal(subtitle), tx, sy + 34, LIGHT, 0.47F, sw - 31, false);
        }
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
            int alpha = 7 + (4 - i) * 5;
            g.fill(cx - r, cy - r / 2, cx + r, cy + r / 2, rgb | (alpha << 24));
        }
    }

    private static void sparkle(GuiGraphics g, int sx, int sy, int color) {
        g.fill(sx, sy - 3, sx + 1, sy + 4, color);
        g.fill(sx - 3, sy, sx + 4, sy + 1, color);
        g.fill(sx - 1, sy - 1, sx + 2, sy + 2, (color & 0x00FFFFFF) | 0xB0000000);
    }
}

package de.tekatz.foreverskyblock.client;

import static de.tekatz.foreverskyblock.client.MenuArt.*;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.ChallengeRules;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import java.util.Locale;
import net.minecraft.ChatFormatting;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;

/**
 * Renders the K-menu header with the six approved image assets.
 *
 * The PNGs are scaled-only derivatives of the six user-approved concept images. No alternate art is generated
 * or substituted here. Header geometry remains responsive while the exact asset content is preserved.
 */
public final class KMenuHeaderArt {
    private static final ResourceLocation BRAND = texture("header_brand.png");
    private static final ResourceLocation SKILL_POINTS = texture("header_skill_points.png");
    private static final ResourceLocation PERK_POINTS = texture("header_perk_points.png");
    private static final ResourceLocation SHOP_TIER = texture("header_shop_tier.png");
    private static final ResourceLocation PLAYTIME = texture("header_playtime.png");
    private static final ResourceLocation ISLAND_LEVEL = texture("header_island_level.png");

    private static final int BRAND_SOURCE_WIDTH = 150;
    private static final int BRAND_SOURCE_HEIGHT = 63;
    private static final int ICON_SOURCE_SIZE = 24;

    private KMenuHeaderArt() { }

    private static ResourceLocation texture(String file) {
        return ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "textures/gui/kmenu/" + file);
    }

    public static void render(GuiGraphics graphics, int screenX, int screenY, PremiumMenuLayout layout) {
        int left = screenX + layout.padding();
        int top = screenY + layout.padding();
        int height = layout.headerHeight() - 5;
        int available = layout.width() - 2 * layout.padding() - 23;
        int brandWidth = layout.tiny() ? 0 : layout.compact() ? 104 : 154;

        if (brandWidth > 0) {
            drawImage(graphics, BRAND, left, top, Math.max(1, brandWidth - 4), height,
                    BRAND_SOURCE_WIDTH, BRAND_SOURCE_HEIGHT);
        }

        int remaining = available - brandWidth;
        int gap = 4;
        int skillWidth = (int) (remaining * .18);
        int perkWidth = skillWidth;
        int statsWidth = (int) (remaining * .25);
        int tierWidth = remaining - skillWidth - perkWidth - statsWidth - gap * 3;
        int sectionX = left + brandWidth;

        SkillNetwork.Snapshot state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();

        drawCurrency(graphics, sectionX, top, skillWidth, height, SKILL_POINTS,
                t("sp"), ready ? literal(state.points()) : literal("—"), CYAN);
        sectionX += skillWidth + gap;

        drawCurrency(graphics, sectionX, top, perkWidth, height, PERK_POINTS,
                t("pp"), ready ? literal(state.perkPoints()) : literal("—"), GOLD);
        sectionX += perkWidth + gap;

        panel(graphics, sectionX, top, tierWidth, height, 0xB9102532, BORDER);
        boolean tierIcon = tierWidth > 85;
        int tierIconSize = iconSize(height);
        if (tierIcon) {
            drawImage(graphics, SHOP_TIER, sectionX + 5, top + (height - tierIconSize) / 2,
                    tierIconSize, tierIconSize, ICON_SOURCE_SIZE, ICON_SOURCE_SIZE);
        }
        int tierTextX = sectionX + (tierIcon ? tierIconSize + 10 : 4);
        int tierTextWidth = tierWidth - (tierIcon ? tierIconSize + 14 : 8);
        centered(graphics, t("tier"), tierTextX, top + 3, tierTextWidth, 12, MUTED, .62F);
        centered(graphics, ready ? meta("tier." + state.shopTier()) : literal("—"),
                tierTextX, top + 15, tierTextWidth, 15, WHITE, .78F);
        if (height >= 44) {
            int completed = ready ? completed(state) : 0;
            bar(graphics, tierTextX, top + height - 9, tierTextWidth,
                    completed / (double) MilestoneCatalog.MILESTONES.size(), GOLD);
            if (height > 55) {
                centered(graphics, literal(completed + " / " + MilestoneCatalog.MILESTONES.size()),
                        tierTextX, top + 34, tierTextWidth, 10, MUTED, .6F);
            }
        }
        sectionX += tierWidth + gap;

        panel(graphics, sectionX, top, statsWidth, height, 0xB9102532, BORDER);
        int rowHeight = height / 2;
        SkillNetwork.MenuStatistics statistics = state == null
                ? SkillNetwork.MenuStatistics.UNKNOWN : state.statistics();
        drawStat(graphics, sectionX, top, statsWidth, rowHeight, PLAYTIME,
                t("playtime"), playtime(statistics.playTicks()), CYAN);
        drawStat(graphics, sectionX, top + rowHeight, statsWidth, height - rowHeight, ISLAND_LEVEL,
                t("island"), statistics.islandLevel() < 0
                        ? literal("—")
                        : literal(statistics.islandLevel() + " / " + ChallengeRules.ALL.size()), GREEN);
    }

    private static void drawCurrency(GuiGraphics graphics, int x, int y, int width, int height,
                                     ResourceLocation icon, Component title, Component value, int color) {
        panel(graphics, x, y, width, height, 0xB9102532, BORDER);
        boolean showIcon = width >= 57;
        int size = iconSize(height);
        int textLane = showIcon ? size + 10 : 2;
        if (showIcon) {
            drawImage(graphics, icon, x + 5, y + (height - size) / 2,
                    size, size, ICON_SOURCE_SIZE, ICON_SOURCE_SIZE);
        }
        centered(graphics, title, x + textLane, y + 4, width - textLane - 3, 12, MUTED, .62F);
        centered(graphics, value.copy().withStyle(ChatFormatting.BOLD), x + textLane, y + 18,
                width - textLane - 3, height - 24, color, height < 45 ? .95F : 1.1F);
    }

    private static void drawStat(GuiGraphics graphics, int x, int y, int width, int height,
                                 ResourceLocation icon, Component title, Component value, int color) {
        boolean showIcon = width > 68;
        int size = height >= 28 ? 20 : 18;
        int textOffset = showIcon ? size + 10 : 4;
        if (showIcon) {
            drawImage(graphics, icon, x + 5, y + (height - size) / 2,
                    size, size, ICON_SOURCE_SIZE, ICON_SOURCE_SIZE);
        }
        centered(graphics, title, x + textOffset, y + 1, width - textOffset - 4, 10, MUTED, .53F);
        centered(graphics, value, x + textOffset, y + 11, width - textOffset - 4,
                Math.max(9, height - 12), WHITE, .7F);
    }

    private static int iconSize(int headerHeight) {
        if (headerHeight >= 55) return 24;
        if (headerHeight >= 44) return 20;
        return 18;
    }

    private static void drawImage(GuiGraphics graphics, ResourceLocation texture,
                                  int x, int y, int width, int height, int sourceWidth, int sourceHeight) {
        if (width <= 0 || height <= 0) return;
        graphics.pose().pushPose();
        graphics.pose().translate(x, y, 0.0F);
        graphics.pose().scale(width / (float) sourceWidth, height / (float) sourceHeight, 1.0F);
        graphics.blit(texture, 0, 0, 0, 0, sourceWidth, sourceHeight, sourceWidth, sourceHeight);
        graphics.pose().popPose();
    }

    private static int completed(SkillNetwork.Snapshot state) {
        return (int) MilestoneCatalog.MILESTONES.stream()
                .filter(milestone -> state.milestones().contains(milestone.id()))
                .count();
    }

    private static Component playtime(long ticks) {
        if (ticks < 0) return literal("—");
        long minutes = ticks / 1200;
        return literal(minutes / 60 + "h " + String.format(Locale.ROOT, "%02d", minutes % 60) + "m");
    }

    private static Component t(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.overhaul." + key, args);
    }

    private static Component meta(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.meta." + key, args);
    }

    private static Component literal(Object value) {
        return Component.literal(String.valueOf(value));
    }
}

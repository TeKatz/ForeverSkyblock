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
        int left=screenX+layout.padding(),top=screenY+layout.padding(),height=layout.headerHeight()-5;
        int available=layout.width()-2*layout.padding()-23;
        int brandWidth=layout.tiny()?0:layout.compact()?104:154;
        if(brandWidth>0)EditorClient.group(graphics,"header.brand",left,top,brandWidth-4,height,
                ()->drawImage(graphics,BRAND,left,top,brandWidth-4,height,BRAND_SOURCE_WIDTH,BRAND_SOURCE_HEIGHT));
        int remaining=available-brandWidth,gap=4,skillWidth=(int)(remaining*.18),perkWidth=skillWidth,statsWidth=(int)(remaining*.25);
        int tierWidth=remaining-skillWidth-perkWidth-statsWidth-gap*3;
        int spX=left+brandWidth,ppX=spX+skillWidth+gap,tierX=ppX+perkWidth+gap,statsX=tierX+tierWidth+gap;
        var state=ClientSkills.snapshot();boolean ready=state!=null&&state.available();
        EditorClient.group(graphics,"header.sp",spX,top,skillWidth,height,()->drawCurrency(graphics,spX,top,skillWidth,height,SKILL_POINTS,t("sp"),ready?literal(state.points()):literal("—"),CYAN));
        EditorClient.group(graphics,"header.pp",ppX,top,perkWidth,height,()->drawCurrency(graphics,ppX,top,perkWidth,height,PERK_POINTS,t("pp"),ready?literal(state.perkPoints()):literal("—"),GOLD));
        EditorClient.group(graphics,"header.tier",tierX,top,tierWidth,height,()->drawTier(graphics,tierX,top,tierWidth,height,state,ready));
        EditorClient.group(graphics,"header.stats",statsX,top,statsWidth,height,()->{
            panel(graphics,statsX,top,statsWidth,height,0xB9102532,BORDER);
            var stats=state==null?SkillNetwork.MenuStatistics.UNKNOWN:state.statistics();int row=height/2;
            drawStat(graphics,statsX,top,statsWidth,row,PLAYTIME,t("playtime"),playtime(stats.playTicks()),CYAN);
            drawStat(graphics,statsX,top+row,statsWidth,height-row,ISLAND_LEVEL,t("island"),stats.islandLevel()<0?literal("—"):literal(stats.islandLevel()+" / "+ChallengeRules.ALL.size()),GREEN);
        });
    }
    private static void drawTier(GuiGraphics graphics,int x,int top,int width,int height,SkillNetwork.Snapshot state,boolean ready){
        panel(graphics,x,top,width,height,0xB9102532,BORDER);boolean icon=width>85;int size=iconSize(height);
        if(icon)drawImage(graphics,SHOP_TIER,x+5,top+(height-size)/2,size,size,ICON_SOURCE_SIZE,ICON_SOURCE_SIZE);
        int tx=x+(icon?size+10:4),tw=width-(icon?size+14:8);
        centered(graphics,t("tier"),tx,top+3,tw,12,MUTED,.62F);
        centered(graphics,ready?meta("tier."+state.shopTier()):literal("—"),tx,top+15,tw,15,WHITE,.78F);
        if(height>=44){int count=ready?completed(state):0;bar(graphics,tx,top+height-9,tw,count/(double)Math.max(1,EditorClient.questCount()),GOLD);
            if(height>55)centered(graphics,literal(count+" / "+EditorClient.questCount()),tx,top+34,tw,10,MUTED,.6F);}
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
        if(EditorClient.customized())return EditorClient.completedCount();
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

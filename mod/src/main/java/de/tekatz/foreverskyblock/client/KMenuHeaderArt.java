package de.tekatz.foreverskyblock.client;

import static de.tekatz.foreverskyblock.client.MenuArt.*;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.editor.EditorCodec;
import de.tekatz.foreverskyblock.editor.EditorDocument.Style;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.ChallengeRules;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import java.util.Locale;
import net.minecraft.ChatFormatting;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.Items;

/**
 * Renders the K-menu header with the approved image assets while exposing its useful sub-parts to K-menu Studio.
 * Parent groups still move/resize as one unit, but labels, values and icons have their own editor hit boxes.
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
        if(brandWidth>0){
            var brand=EditorClient.rect("header.brand",left,top,brandWidth-4,height);
            if(brand.width()>0&&brand.height()>0)drawImage(graphics,BRAND,brand.x(),brand.y(),brand.width(),brand.height(),BRAND_SOURCE_WIDTH,BRAND_SOURCE_HEIGHT);
        }
        int remaining=available-brandWidth,gap=4,skillWidth=(int)(remaining*.18),perkWidth=skillWidth,statsWidth=(int)(remaining*.25);
        int tierWidth=remaining-skillWidth-perkWidth-statsWidth-gap*3;
        int spX=left+brandWidth,ppX=spX+skillWidth+gap,tierX=ppX+perkWidth+gap,statsX=tierX+tierWidth+gap;
        var state=ClientSkills.snapshot();boolean ready=state!=null&&state.available();
        drawCurrency(graphics,"header.sp",spX,top,skillWidth,height,SKILL_POINTS,t("sp"),ready?literal(state.points()):literal("—"),CYAN);
        drawCurrency(graphics,"header.pp",ppX,top,perkWidth,height,PERK_POINTS,t("pp"),ready?literal(state.perkPoints()):literal("—"),GOLD);
        drawTier(graphics,tierX,top,tierWidth,height,state,ready);
        var statsRect=EditorClient.rect("header.stats",statsX,top,statsWidth,height);
        if(statsRect.width()>0&&statsRect.height()>0){
            Style style=EditorClient.style("header.stats");
            panel(graphics,statsRect.x(),statsRect.y(),statsRect.width(),statsRect.height(),EditorCodec.color(style.fill(),0xB9102532),EditorCodec.color(style.border(),BORDER));
            var stats=state==null?SkillNetwork.MenuStatistics.UNKNOWN:state.statistics();int row=statsRect.height()/2;
            drawStat(graphics,"header.stats.playtime",statsRect.x(),statsRect.y(),statsRect.width(),row,PLAYTIME,t("playtime"),playtime(stats.playTicks()),CYAN,style);
            drawStat(graphics,"header.stats.island",statsRect.x(),statsRect.y()+row,statsRect.width(),statsRect.height()-row,ISLAND_LEVEL,t("island"),stats.islandLevel()<0?literal("—"):literal(stats.islandLevel()+" / "+ChallengeRules.ALL.size()),GREEN,style);
        }
    }

    private static void drawTier(GuiGraphics graphics,int x,int top,int width,int height,SkillNetwork.Snapshot state,boolean ready){
        var r=EditorClient.rect("header.tier",x,top,width,height);if(r.width()<=0||r.height()<=0)return;
        Style parent=EditorClient.style("header.tier");
        panel(graphics,r.x(),r.y(),r.width(),r.height(),EditorCodec.color(parent.fill(),0xB9102532),EditorCodec.color(parent.border(),BORDER));
        boolean icon=r.width()>85;int size=iconSize(r.height());
        if(icon)drawEditableIcon(graphics,"header.tier.icon",SHOP_TIER,r.x()+5,r.y()+(r.height()-size)/2,size,size);
        int tx=r.x()+(icon?size+10:4),tw=r.width()-(icon?size+14:8);
        drawEditableText(graphics,"header.tier.label",t("tier"),tx,r.y()+3,tw,12,MUTED,.62F,parent,true);
        drawEditableText(graphics,"header.tier.value",ready?meta("tier."+state.shopTier()):literal("—"),tx,r.y()+15,tw,15,WHITE,.78F,parent,false);
        if(r.height()>=44){int count=ready?completed(state):0;bar(graphics,tx,r.y()+r.height()-9,tw,count/(double)Math.max(1,EditorClient.questCount()),GOLD);
            if(r.height()>55)centered(graphics,literal(count+" / "+EditorClient.questCount()),tx,r.y()+34,tw,10,EditorCodec.color(parent.text(),MUTED),.6F*parent.fontScale());}
    }

    private static void drawCurrency(GuiGraphics graphics,String id,int x,int y,int width,int height,
                                     ResourceLocation icon,Component title,Component value,int color) {
        var r=EditorClient.rect(id,x,y,width,height);if(r.width()<=0||r.height()<=0)return;
        Style parent=EditorClient.style(id);
        panel(graphics,r.x(),r.y(),r.width(),r.height(),EditorCodec.color(parent.fill(),0xB9102532),EditorCodec.color(parent.border(),BORDER));
        boolean showIcon=r.width()>=57;int size=iconSize(r.height());int textLane=showIcon?size+10:2;
        if(showIcon)drawEditableIcon(graphics,id+".icon",icon,r.x()+5,r.y()+(r.height()-size)/2,size,size);
        drawEditableText(graphics,id+".label",title,r.x()+textLane,r.y()+4,r.width()-textLane-3,12,MUTED,.62F,parent,true);
        drawEditableText(graphics,id+".value",value.copy().withStyle(ChatFormatting.BOLD),r.x()+textLane,r.y()+18,r.width()-textLane-3,r.height()-24,color,r.height()<45?.95F:1.1F,parent,false);
    }

    private static void drawStat(GuiGraphics graphics,String id,int x,int y,int width,int height,
                                 ResourceLocation icon,Component title,Component value,int color,Style parent) {
        boolean showIcon=width>68;int size=height>=28?20:18;int textOffset=showIcon?size+10:4;
        if(showIcon)drawEditableIcon(graphics,id+".icon",icon,x+5,y+(height-size)/2,size,size);
        drawEditableText(graphics,id+".label",title,x+textOffset,y+1,width-textOffset-4,10,MUTED,.53F,parent,true);
        drawEditableText(graphics,id+".value",value,x+textOffset,y+11,width-textOffset-4,Math.max(9,height-12),WHITE,.7F,parent,false);
    }

    private static void drawEditableIcon(GuiGraphics graphics,String id,ResourceLocation fallback,int x,int y,int width,int height){
        var r=EditorClient.rect(id,x,y,width,height);if(r.width()<=0||r.height()<=0)return;Style style=EditorClient.style(id);
        if(style.icon().isEmpty()){drawImage(graphics,fallback,r.x(),r.y(),r.width(),r.height(),ICON_SOURCE_SIZE,ICON_SOURCE_SIZE);return;}
        var item=BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(style.icon())).orElse(Items.BARRIER);
        graphics.pose().pushPose();graphics.pose().translate(r.x(),r.y(),0);graphics.pose().scale(r.width()/16F*style.itemScale(),r.height()/16F*style.itemScale(),1);graphics.renderItem(item.getDefaultInstance(),0,0);graphics.pose().popPose();
    }

    private static void drawEditableText(GuiGraphics graphics,String id,Component fallback,int x,int y,int width,int height,int color,float scale,Style parent,boolean editableLabel){
        var r=EditorClient.rect(id,x,y,width,height);if(r.width()<=0||r.height()<=0)return;Style style=EditorClient.style(id);
        Component value=fallback;
        if(editableLabel){String label=style.label(fallback.getString(),EditorClient.german());if(!label.equals(fallback.getString()))value=Component.literal(label);}
        int inherited=EditorCodec.color(parent.text(),color);int actual=EditorCodec.color(style.text(),inherited);
        centered(graphics,value,r.x(),r.y(),r.width(),r.height(),actual,scale*parent.fontScale()*style.fontScale());
    }

    private static int iconSize(int headerHeight) {
        if(headerHeight>=55)return 24;if(headerHeight>=44)return 20;return 18;
    }

    private static void drawImage(GuiGraphics graphics,ResourceLocation texture,int x,int y,int width,int height,int sourceWidth,int sourceHeight) {
        if(width<=0||height<=0)return;
        graphics.pose().pushPose();graphics.pose().translate(x,y,0.0F);graphics.pose().scale(width/(float)sourceWidth,height/(float)sourceHeight,1.0F);
        graphics.blit(texture,0,0,0,0,sourceWidth,sourceHeight,sourceWidth,sourceHeight);graphics.pose().popPose();
    }

    private static int completed(SkillNetwork.Snapshot state) {
        if(EditorClient.customized())return EditorClient.completedCount();
        return (int)MilestoneCatalog.MILESTONES.stream().filter(milestone->state.milestones().contains(milestone.id())).count();
    }

    private static Component playtime(long ticks) {
        if(ticks<0)return literal("—");long minutes=ticks/1200;return literal(minutes/60+"h "+String.format(Locale.ROOT,"%02d",minutes%60)+"m");
    }

    private static Component t(String key,Object...args){return Component.translatable("gui.forever_skyblock.overhaul."+key,args);}
    private static Component meta(String key,Object...args){return Component.translatable("gui.forever_skyblock.meta."+key,args);}
    private static Component literal(Object value){return Component.literal(String.valueOf(value));}
}
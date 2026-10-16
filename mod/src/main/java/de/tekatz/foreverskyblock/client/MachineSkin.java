package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.Font;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;

/** UV patches sample the approved artwork directly; no fixed labels or example inventory are baked into the UI. */
final class MachineSkin {
    static final int LIGHT=0xFFE6EDF2,MUTED=0xFFA9B7C2,CYAN=0xFF50D4DE,VIOLET=0xFFB78AFF;
    private final ResourceLocation atlas;
    final boolean ascension;
    MachineSkin(boolean ascension) {
        this.ascension=ascension;
        atlas=ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID,"textures/gui/"+(ascension?"ascension":"condenser")+"_design.png");
    }
    void patch(GuiGraphics g,int x,int y,int width,int height,int u,int v,int sourceWidth,int sourceHeight) {
        if(width<=0||height<=0)return;
        g.pose().pushPose();g.pose().translate(x,y,0);g.pose().scale((float)width/sourceWidth,(float)height/sourceHeight,1);
        g.blit(atlas,0,0,(float)u,(float)v,sourceWidth,sourceHeight,1536,1024);g.pose().popPose();
    }
    void frame(GuiGraphics g,int x,int y,int width,int height) {
        g.fill(x,y,x+width,y+height,0xFF202831);
        int sx=ascension?292:270,sy=20,sw=ascension?954:996,sh=ascension?982:974;
        int b=28,c=64;
        patch(g,x,y,width,7,sx,sy,sw,b);patch(g,x,y+height-7,width,7,sx,sy+sh-b,sw,b);
        patch(g,x,y+7,7,height-14,sx,sy+b,b,sh-b*2);patch(g,x+width-7,y+7,7,height-14,sx+sw-b,sy+b,b,sh-b*2);
        patch(g,x,y,16,16,sx,sy,c,c);patch(g,x+width-16,y,16,16,sx+sw-c,sy,c,c);
        patch(g,x,y+height-16,16,16,sx,sy+sh-c,c,c);patch(g,x+width-16,y+height-16,16,16,sx+sw-c,sy+sh-c,c,c);
        g.fill(x+15,y+28,x+width-15,y+29,0xFF48525F);
    }
    static void panel(GuiGraphics g,int x,int y,int w,int h,int edge) {
        g.fill(x,y,x+w,y+h,edge);g.fill(x+1,y+1,x+w-1,y+h-1,0xFF0E1620);
        g.fill(x+2,y+2,x+w-2,y+h-2,0xFF18212B);
    }
    void slot(GuiGraphics g,int x,int y,boolean machine) {
        int padding=machine?4:2,size=machine?24:20;
        if(ascension && machine) {
            // A symmetric inset avoids stretching the inventory atlas bevel across the core slot.
            int left=x-padding,top=y-padding;
            panel(g,left,top,size,size,0xFF788493);
            g.fill(left+1,top+1,left+size-1,top+2,0xFFB7C3D0);
            g.fill(left+1,top+2,left+2,top+size-1,0xFFB7C3D0);
        }
        else if(ascension)patch(g,x-padding,y-padding,size,size,648,686,82,66);
        else patch(g,x-padding,y-padding,size,size,632,655,84,76);
    }
    void lock(GuiGraphics g,int x,int y) {
        if(ascension){g.fill(x+2,y,x+7,y+2,0xFFBCC8D3);g.fill(x+1,y+2,x+8,y+8,0xFF6D7C89);g.fill(x+4,y+4,x+5,y+7,0xFF111923);}
        else patch(g,x,y,8,10,464,229,29,42);
    }
    void button(GuiGraphics g,int x,int y,int width,int height,boolean primary,boolean enabled,boolean hover) {
        int u,v,sw,sh;
        if(ascension){u=370;v=527;sw=797;sh=74;}
        else if(primary){u=340;v=470;sw=400;sh=67;}
        else{u=951;v=473;sw=248;sh=62;}
        if(!enabled)g.setColor(.55F,.55F,.55F,1);
        int b=12,d=4;
        patch(g,x,y,d,height,u,v,b,sh);patch(g,x+d,y,width-d*2,height,u+b,v,sw-b*2,sh);patch(g,x+width-d,y,d,height,u+sw-b,v,b,sh);
        g.setColor(1,1,1,1);
        // Text is drawn live over a clean center; artwork patches contain no example button label.
        g.fill(x+4,y+3,x+width-4,y+height-3,ascension?(enabled?0xFF7837BD:0xFF40354D):0xFF272C32);
        if(hover&&enabled) {
            int color=ascension?VIOLET:primary?0xFFFFC183:CYAN;
            int fill=ascension?0xFF934BD6:primary?0xFF534236:0xFF304D59;
            g.fill(x+4,y+3,x+width-4,y+height-3,fill);
            g.fill(x+3,y+2,x+width-3,y+3,color);
            g.fill(x+3,y+height-3,x+width-3,y+height-2,color);
            g.fill(x+3,y+3,x+4,y+height-3,color);
            g.fill(x+width-4,y+3,x+width-3,y+height-3,color);
        }
    }
    static void text(GuiGraphics g,Font font,Component message,int x,int y,int maxWidth,int color,float maximumScale) {
        float scale=Math.min(maximumScale,(float)maxWidth/Math.max(1,font.width(message)));
        g.pose().pushPose();g.pose().translate(x,y,0);g.pose().scale(scale,scale,1);g.drawString(font,message,0,0,color,false);g.pose().popPose();
    }
    static void centered(GuiGraphics g,Font font,Component message,int cx,int y,int maxWidth,int color,float maximumScale) {
        float scale=Math.min(maximumScale,(float)maxWidth/Math.max(1,font.width(message)));
        text(g,font,message,Math.round(cx-font.width(message)*scale/2),y,maxWidth,color,maximumScale);
    }
}

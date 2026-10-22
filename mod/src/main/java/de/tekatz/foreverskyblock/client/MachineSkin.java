package de.tekatz.foreverskyblock.client;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.Font;
import net.minecraft.network.chat.Component;

/** Pixel-aligned instrument panels. Slots never scale or move independently of Vanilla items. */
final class MachineSkin {
    static final int LIGHT=0xFFE6EEE9, MUTED=0xFFA0B4BE, CYAN=0xFF76D8D2, VIOLET=0xFFBCA4F3;
    static final int COPPER=0xFFE4B47C, GREEN=0xFF9BD4A6, ICE=0xFF9DDDF0;
    final boolean ascension;
    private int accent;
    MachineSkin(boolean ascension) { this.ascension=ascension; accent=ascension?VIOLET:CYAN; }
    void accent(int color) { accent=color; }
    void frame(GuiGraphics g,int x,int y,int width,int height) {
        g.fill(x+3,y+4,x+width+3,y+height+4,0x66050910);
        g.fill(x+3,y,x+width-3,y+height,0xFF0C151F);
        g.fill(x,y+3,x+width,y+height-3,0xFF0C151F);
        g.fill(x+1,y+4,x+width-1,y+height-4,0xFF425764);
        g.fill(x+4,y+1,x+width-4,y+height-1,0xFF425764);
        g.fillGradient(x+3,y+4,x+width-3,y+height-4,0xFF253746,0xFF15232F);
        g.fill(x+8,y+7,x+width-8,y+27,0xFF101D29);
        g.fill(x+14,y+27,x+width-14,y+28,0xFF526979);
        g.fill(x+14,y+27,x+66,y+28,accent);
        g.fill(x+10,y+33,x+11,y+height-12,0xFF344A58);
        g.fill(x+width-11,y+33,x+width-10,y+height-12,0xFF344A58);
        for(int dx:new int[]{5,width-7}) for(int dy:new int[]{5,height-7}) {
            g.fill(x+dx,y+dy,x+dx+2,y+dy+2,0xFF0C1923);
            g.fill(x+dx,y+dy,x+dx+1,y+dy+1,0xFF91A5AE);
        }
        g.fill(x+width/2-14,y+height-4,x+width/2+14,y+height-3,accent);
    }
    static void panel(GuiGraphics g,int x,int y,int w,int h,int edge) {
        if(w<3||h<3)return;
        g.fill(x,y,x+w,y+h,0xFF0A151F);
        g.fill(x+1,y+1,x+w-1,y+h-1,edge);
        g.fillGradient(x+2,y+2,x+w-2,y+h-2,0xFF1B2C39,0xFF12212D);
        g.fill(x+3,y+1,x+w-3,y+2,0xFF637884);
    }
    void slot(GuiGraphics g,int x,int y,boolean machine) {
        int pad=machine?4:2, n=machine?24:20, left=x-pad, top=y-pad;
        g.fill(left,top,left+n,top+n,0xFF070F18);
        g.fill(left+1,top+1,left+n-1,top+n-1,machine?0xFF6B818B:0xFF425A68);
        g.fill(left+2,top+2,left+n-2,top+n-2,0xFF0D1B26);
        g.fill(left+3,top+3,left+n-3,top+4,0xFF09131E);
        g.fill(left+3,top+n-3,left+n-3,top+n-2,0xFF2F4552);
        if(machine) {
            g.fill(left+1,top+1,left+6,top+2,accent);
            g.fill(left+n-6,top+n-2,left+n-1,top+n-1,accent);
        }
    }
    void lock(GuiGraphics g,int x,int y) {
        g.fill(x+2,y,x+6,y+1,LIGHT);g.fill(x+1,y+1,x+2,y+4,LIGHT);g.fill(x+6,y+1,x+7,y+4,LIGHT);
        g.fill(x,y+4,x+8,y+10,accent);g.fill(x+3,y+6,x+5,y+9,0xFF152331);
    }
    void button(GuiGraphics g,int x,int y,int width,int height,boolean primary,boolean enabled,boolean hover) {
        int edge=!enabled?0xFF3B4B58:hover?accent:0xFF6A818C;
        g.fill(x,y,x+width,y+height,0xFF08131D);
        g.fill(x+1,y+1,x+width-1,y+height-1,edge);
        g.fillGradient(x+2,y+2,x+width-2,y+height-2,hover&&enabled?0xFF37515E:0xFF263B4B,0xFF172936);
        if(primary&&enabled)g.fill(x+2,y+2,x+4,y+height-2,accent);
        if(hover&&enabled)g.fill(x+5,y+height-3,x+width-5,y+height-2,accent);
    }
    /** Animated light travels only inside the filled portion of an existing progress meter. */
    static void meter(GuiGraphics g,int x,int y,int w,int h,float ratio,int color,float time,boolean running) {
        g.fill(x,y,x+w,y+h,0xFF07141E);
        int filled=Math.round(Math.clamp(ratio,0F,1F)*w);
        if(filled==0)return;
        g.fill(x,y,x+filled,y+h,color);
        g.fill(x,y+h-1,x+filled,y+h,0x550A1824);
        if(running) {
            int center=Math.round((time%40)/40F*(filled+10))-5;
            for(int i=-3;i<=3;i++) {
                int xx=center+i;
                if(xx>=0&&xx<filled)g.fill(x+xx,y,x+xx+1,y+h,((4-Math.abs(i))*24<<24)|0xE8FFF3);
            }
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

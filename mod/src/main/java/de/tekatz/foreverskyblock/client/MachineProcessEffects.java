package de.tekatz.foreverskyblock.client;

import com.mojang.math.Axis;
import net.minecraft.client.gui.GuiGraphics;

/** Decorative process cutaways never own production state or cover a real item slot. */
final class MachineProcessEffects {
    private MachineProcessEffects() { }
    static void washing(GuiGraphics g,int x,int y,int w,float phase,boolean running,boolean blocked) {
        int color=blocked?MachineSkin.COPPER:MachineSkin.ICE;
        g.fillGradient(x,y,x+w,y+26,0xFF102431,0xFF224754);
        g.fill(x,y+25,x+w,y+27,0xFF708E98);
        for(int i=0;i<4;i++) {
            float t=(phase*8+i*.25F)%1;
            int alpha=Math.round((float)Math.sin(Math.PI*t)*150);
            int r=3+Math.round(t*(w/2F-6));
            g.fill(x+w/2-r,y+6+i*4,x+w/2+r,y+7+i*4,(alpha<<24)|(color&0xFFFFFF));
        }
        g.fill(x+5,y+16,x+w-5,y+18,0xFF839FA3);
        for(int i=7;i<w-6;i+=6)g.fill(x+i,y+17,x+i+3,y+21,0xFF294652);
        if(running)for(int i=0;i<5;i++) {
            float t=(phase*7+i*.2F)%1;
            float strength=(float)Math.sin(Math.PI*t);
            int xx=x+9+i*(w-18)/5,yy=y+2+Math.round(t*21);
            g.fill(xx,yy,xx+2,yy+2,(Math.round(strength*230)<<24)|0xE1D3AD);
        }
        g.fill(x,y,x+2,y+27,0xFF527787);g.fill(x+w-2,y,x+w,y+27,0xFF527787);
    }
    static void refining(GuiGraphics g,int x,int y,int w,float phase,boolean running,boolean blocked) {
        int cx=x+w/2, cy=y+13, tint=blocked?MachineSkin.COPPER:MachineSkin.VIOLET;
        g.fillGradient(x,y,x+w,y+27,0xFF121E30,0xFF273047);
        for(int i=0;i<2;i++) {
            g.pose().pushPose();g.pose().translate(cx,cy,0);
            g.pose().mulPose(Axis.ZP.rotationDegrees(phase*360*(i==0?1:-1)+45));
            int r=9-i*4;
            g.fill(-r,-r,r,-r+1,i==0?0xFF6E80A2:tint);g.fill(-r,r-1,r,r,i==0?0xFF6E80A2:tint);
            g.fill(-r,-r,-r+1,r,i==0?0xFF6E80A2:tint);g.fill(r-1,-r,r,r,i==0?0xFF6E80A2:tint);
            g.pose().popPose();
        }
        g.fill(cx-2,cy-2,cx+2,cy+2,running?0xFFE6DCFF:0xFF7F8CA7);
        for(int i=0;i<3;i++) {
            float t=(phase*8+i/3F)%1;
            int xx=x+2+Math.round(t*(w-4));
            if(running)g.fill(xx,y+24,xx+1,y+25,((int)Math.round(Math.sin(Math.PI*t)*220)<<24)|(tint&0xFFFFFF));
        }
    }
}

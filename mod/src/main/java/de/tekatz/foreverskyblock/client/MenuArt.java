package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.editor.EditorCodec;
import de.tekatz.foreverskyblock.editor.EditorDocument.Style;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;

/** Code-drawn menu marks stay crisp at every GUI scale and require no item-model placeholders. */
final class MenuArt {
    private static Style scope=Style.defaults();
    static Style pushStyle(Style next){var prior=scope;scope=next==null?Style.defaults():next;return prior;}
    static final int WHITE = 0xFFF2F8FD, MUTED = 0xFF9DB4C5, DIM = 0xFF638399,
            CYAN = 0xFF51D7ED, GOLD = 0xFFFFCB75, GREEN = 0xFF69DDA7, BORDER = 0xFF294759;
    private MenuArt() { }
    static Component t(String key, Object... args) { return Component.translatable("gui.forever_skyblock.overhaul." + key, args); }

    private static String effect(Style s){return s==null||s.effect()==null?"NONE":s.effect();}
    private static float strength(Style s){return s==null?1F:Math.clamp(s.effectStrength(),.1F,4F);}
    private static int effectColor(Style s,int fallback){return EditorCodec.color(s==null?"":s.effectColor(),fallback);}
    private static float pulse(){return .55F+.45F*(float)Math.sin(System.nanoTime()/310_000_000D);}
    private static int alpha(int color,int a){return (Math.clamp(a,0,255)<<24)|(color&0xFFFFFF);}
    private static int mix(int a,int b,float ratio){
        ratio=Math.clamp(ratio,0F,1F);float inv=1-ratio;
        int aa=(int)(((a>>>24)&255)*inv+((b>>>24)&255)*ratio),ar=(int)(((a>>>16)&255)*inv+((b>>>16)&255)*ratio);
        int ag=(int)(((a>>>8)&255)*inv+((b>>>8)&255)*ratio),ab=(int)((a&255)*inv+(b&255)*ratio);
        return aa<<24|ar<<16|ag<<8|ab;
    }

    static void panel(GuiGraphics g, int x, int y, int w, int h, int color, int border) {
        color=EditorCodec.color(scope.fill(),color);border=EditorCodec.color(scope.border(),border);
        if (w < 2 || h < 2) return;
        String fx=effect(scope);float power=strength(scope);int fxColor=effectColor(scope,CYAN);
        if(fx.equals("GRADIENT")){
            float amount=Math.min(.7F,.16F*power);
            for(int row=0;row<h;row++){
                float t=h<=1?0:row/(float)(h-1),blend=amount*(1-Math.abs(t*2-1)*.55F);
                g.fill(x,y+row,x+w,y+row+1,mix(color,fxColor,blend*(1-t*.45F)));
            }
        }else{
            int fill=fx.equals("GLASS")?mix(color,0xA80A1B27,Math.min(.55F,.18F*power)):color;
            g.fill(x, y, x + w, y + h, fill);
            if(fx.equals("GLASS"))g.fill(x+1,y+1,x+w-1,y+2,alpha(fxColor,(int)(38*power)));
        }
        if(fx.equals("SCANLINES")){
            int overlay=alpha(fxColor,(int)Math.min(90,18*power));
            for(int row=2;row<h-1;row+=4)g.fill(x+1,y+row,x+w-1,y+row+1,overlay);
        }
        int actualBorder=border;
        if(fx.equals("PULSE"))actualBorder=mix(border,fxColor,Math.min(1F,pulse()*.38F*power));
        g.fill(x, y, x + w, y + 1, actualBorder); g.fill(x, y + h - 1, x + w, y + h, actualBorder);
        g.fill(x, y, x + 1, y + h, actualBorder); g.fill(x + w - 1, y, x + w, y + h, actualBorder);
        if(fx.equals("GLOW")||fx.equals("PULSE")){
            int glow=alpha(fxColor,(int)Math.min(150,(fx.equals("PULSE")?pulse():.7F)*54*power));
            g.fill(x-1,y-1,x+w+1,y,glow);g.fill(x-1,y+h,x+w+1,y+h+1,glow);
            g.fill(x-1,y,x,y+h,glow);g.fill(x+w,y,x+w+1,y+h,glow);
        }
    }

    /** Small backdrop used by authored item/header icons so effects are visible without replacing the icon itself. */
    static void effectBackdrop(GuiGraphics g,int x,int y,int w,int h,Style style){
        String fx=effect(style);if(fx.equals("NONE")||w<=0||h<=0)return;
        float power=strength(style);int c=effectColor(style,CYAN);
        if(fx.equals("GLOW")||fx.equals("PULSE")){
            int a=(int)Math.min(130,(fx.equals("PULSE")?pulse():.72F)*46*power);
            g.fill(x-2,y-2,x+w+2,y-1,alpha(c,a));g.fill(x-2,y+h+1,x+w+2,y+h+2,alpha(c,a));
            g.fill(x-2,y-1,x-1,y+h+1,alpha(c,a));g.fill(x+w+1,y-1,x+w+2,y+h+1,alpha(c,a));
        }else if(fx.equals("GLASS"))g.fill(x-2,y-2,x+w+2,y+h+2,alpha(c,(int)Math.min(70,18*power)));
    }

    static void label(GuiGraphics g, Component text, int x, int y, int w, int color, float scale) {
        scale*=scope.fontScale();color=EditorCodec.color(scope.text(),color);
        labelRaw(g,text,x,y,w,color,scale);
    }
    private static void labelRaw(GuiGraphics g, Component text, int x, int y, int w, int color, float scale) {
        if (w <= 0) return;
        var font = Minecraft.getInstance().font;
        int available = Math.max(1, (int) (w / scale));
        String value = text.getString();
        if (font.width(value) > available) value = font.plainSubstrByWidth(value, Math.max(1, available - font.width("…"))) + "…";
        String fx=effect(scope);int fxColor=effectColor(scope,CYAN);float power=strength(scope);
        g.pose().pushPose(); g.pose().translate(x, y, 0); g.pose().scale(scale, scale, 1);
        if(fx.equals("GLOW")||fx.equals("PULSE")){
            int glow=alpha(fxColor,(int)Math.min(150,(fx.equals("PULSE")?pulse():.7F)*45*power));
            g.drawString(font,value,-1,0,glow,false);g.drawString(font,value,1,0,glow,false);g.drawString(font,value,0,-1,glow,false);g.drawString(font,value,0,1,glow,false);
        }
        g.drawString(font, value, 0, 0, color, scope.textShadow()); g.pose().popPose();
    }
    static void centered(GuiGraphics g, Component text, int x, int y, int w, int h, int color, float scale) {
        scale*=scope.fontScale();color=EditorCodec.color(scope.text(),color);
        var font = Minecraft.getInstance().font;
        float fit = Math.min(scale, w / (float) Math.max(1, font.width(text.getString())));
        labelRaw(g, text, x + Math.max(0, Math.round((w - font.width(text.getString()) * fit) / 2)),
                y + Math.round((h - 8 * fit) / 2), w, color, fit);
    }
    static int wrappedHeight(Component text, int width, float scale) {
        return Minecraft.getInstance().font.split(text, Math.max(1, (int) (width / scale))).size() * Math.round(11 * scale);
    }
    static int wrapped(GuiGraphics g, Component text, int x, int y, int width, int color, float scale, int maxLines) {
        scale*=scope.fontScale();color=EditorCodec.color(scope.text(),color);
        int n = 0;
        for (var line : Minecraft.getInstance().font.split(text, Math.max(1, (int) (width / scale)))) {
            if (n++ >= maxLines) break;
            String fx=effect(scope);int fxColor=effectColor(scope,CYAN);float power=strength(scope);
            g.pose().pushPose(); g.pose().translate(x, y, 0); g.pose().scale(scale, scale, 1);
            if(fx.equals("GLOW")||fx.equals("PULSE")){
                int glow=alpha(fxColor,(int)Math.min(145,(fx.equals("PULSE")?pulse():.7F)*42*power));
                g.drawString(Minecraft.getInstance().font,line,-1,0,glow,false);g.drawString(Minecraft.getInstance().font,line,1,0,glow,false);
            }
            g.drawString(Minecraft.getInstance().font, line, 0, 0, color, scope.textShadow()); g.pose().popPose();
            y += Math.round(11 * scale);
        }
        return y;
    }
    static void circle(GuiGraphics g, int cx, int cy, int radius, int color) {
        for (int dy = -radius; dy <= radius; dy++) {
            int dx = (int) Math.floor(Math.sqrt(radius * radius - dy * dy));
            g.fill(cx - dx, cy + dy, cx + dx + 1, cy + dy + 1, color);
        }
    }
    static void line(GuiGraphics g, int x1, int y1, int x2, int y2, int color) {
        int steps = Math.max(Math.abs(x2 - x1), Math.abs(y2 - y1));
        for (int i = 0; i <= steps; i++) {
            float t = steps == 0 ? 0 : i / (float) steps;
            int x = Math.round(x1 + (x2 - x1) * t), y = Math.round(y1 + (y2 - y1) * t);
            g.fill(x, y, x + 1, y + 1, color);
        }
    }
    static void bar(GuiGraphics g, int x, int y, int width, double ratio, int color) {
        g.fill(x, y, x + width, y + 3, 0xFF193444);
        g.fill(x, y, x + (int) Math.round(width * Math.clamp(ratio, 0, 1)), y + 3, color);
    }
    static void arrow(GuiGraphics g,int x1,int y1,int x2,int y2,int radius,int color){
        double length=Math.hypot(x2-x1,y2-y1);if(length<radius+3)return;double ux=(x2-x1)/length,uy=(y2-y1)/length;
        int tx=(int)(x2-ux*(radius+3)),ty=(int)(y2-uy*(radius+3));line(g,x1,y1,tx,ty,color);
        line(g,tx,ty,(int)(tx-ux*6-uy*4),(int)(ty-uy*6+ux*4),color);line(g,tx,ty,(int)(tx-ux*6+uy*4),(int)(ty-uy*6-ux*4),color);
    }
    static void mark(GuiGraphics g, int x, int y, int kind, int color) {
        // SP: open crystal; PP: four-point star; Tier: stepped crest. 16x16 design grid.
        if (kind == 0) {
            line(g,x+8,y+1,x+14,y+7,color); line(g,x+14,y+7,x+8,y+15,color);
            line(g,x+8,y+15,x+2,y+7,color); line(g,x+2,y+7,x+8,y+1,color);
            line(g,x+2,y+7,x+14,y+7,color); line(g,x+8,y+1,x+8,y+15,color);
        } else if (kind == 1) {
            for (int i = -6; i <= 6; i++) { int r = Math.max(1, 4 - Math.abs(i)); g.fill(x+8-r,y+8+i,x+9+r,y+9+i,color); }
        } else if (kind == 2) {
            line(g,x+2,y+2,x+14,y+2,color); line(g,x+2,y+2,x+3,y+10,color);
            line(g,x+14,y+2,x+13,y+10,color); line(g,x+3,y+10,x+8,y+15,color);
            line(g,x+8,y+15,x+13,y+10,color); g.fill(x+5,y+5,x+11,y+6,color); g.fill(x+6,y+8,x+10,y+9,color);
        } else if (kind == 3) {
            circle(g,x+8,y+8,7,color); circle(g,x+8,y+8,6,0xFF10232F);
            line(g,x+8,y+8,x+8,y+4,color); line(g,x+8,y+8,x+11,y+10,color);
        } else {
            g.fill(x+2,y+6,x+14,y+8,color); g.fill(x+4,y+8,x+12,y+11,0xFF648E90);
            g.fill(x+6,y+11,x+10,y+14,0xFF648E90); g.fill(x+7,y+2,x+9,y+6,color);
        }
    }
}

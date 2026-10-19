package de.tekatz.foreverskyblock.client;

import java.util.Locale;
import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** Small native ARGB picker used by K-menu Studio instead of requiring hand-written color codes. */
public final class EditorColorPicker extends Screen {
    private static final int[] PALETTE={
            0xFFFFFFFF,0xFFF2F8FD,0xFF9DB4C5,0xFF638399,0xFF07111B,0xFF0B1C2A,
            0xFF51D7ED,0xFF69DDA7,0xFFFFCB75,0xFFFF929D,0xFFBD9AF2,0xFF4D95A1,
            0xFF173846,0xFF123044,0xFF294759,0xFF376578,0xFF14384D,0xFF0D2331
    };
    private final Screen parent;
    private final Consumer<String> save;
    private int alpha=255,red=255,green=255,blue=255,dragChannel=-1;
    private EditBox hex;
    private boolean syncing;

    public EditorColorPicker(Screen parent,String current,Consumer<String> save){
        super(Component.literal(EditorClient.german()?"Farbe auswählen":"Choose color"));this.parent=parent;this.save=save;parse(current);
    }

    @Override protected void init(){
        int boxW=Math.min(360,width-36),left=(width-boxW)/2;
        hex=addRenderableWidget(new EditBox(font,left+88,50,boxW-88,20,Component.literal("AARRGGBB")));
        hex.setMaxLength(9);hex.setValue(value());hex.setResponder(this::fromHex);
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Übernehmen":"Apply"),b->{save.accept(value());minecraft.setScreen(parent);}).bounds(left+boxW-212,height-34,66,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Standard":"Default"),b->{save.accept("");minecraft.setScreen(parent);}).bounds(left+boxW-140,height-34,66,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Abbrechen":"Cancel"),b->onClose()).bounds(left+boxW-68,height-34,66,20).build());
    }

    private void parse(String value){
        if(value==null||value.isBlank())return;String s=value.replace("#","");if(s.length()!=8)return;
        try{long n=Long.parseLong(s,16);alpha=(int)(n>>24)&255;red=(int)(n>>16)&255;green=(int)(n>>8)&255;blue=(int)n&255;}catch(NumberFormatException ignored){}
    }
    private String value(){return String.format(Locale.ROOT,"%02X%02X%02X%02X",alpha,red,green,blue);}
    private void fromHex(String text){if(syncing)return;String s=text.replace("#","");if(s.length()!=8)return;try{parse(s);}catch(RuntimeException ignored){}}
    private void syncHex(){if(hex==null)return;syncing=true;hex.setValue(value());syncing=false;}
    private int left(){return (width-Math.min(360,width-36))/2;}
    private int boxW(){return Math.min(360,width-36);}
    private int barsLeft(){return left()+88;}
    private int barsWidth(){return boxW()-88;}
    private int barY(int channel){return 86+channel*29;}
    private int channel(int index){return switch(index){case 0->alpha;case 1->red;case 2->green;default->blue;};}
    private void channel(int index,int value){value=Math.clamp(value,0,255);switch(index){case 0->alpha=value;case 1->red=value;case 2->green=value;default->blue=value;}syncHex();}
    private int composed(int channel,int value){int a=alpha,r=red,g=green,b=blue;switch(channel){case 0->a=value;case 1->r=value;case 2->g=value;case 3->b=value;}return (a<<24)|(r<<16)|(g<<8)|b;}

    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xFF07111B);int left=left(),box=boxW();
        MenuArt.panel(g,left-8,20,box+16,height-62,0xFF0B1C2A,MenuArt.BORDER);
        g.drawString(font,title,left,29,MenuArt.WHITE);g.drawString(font,"AARRGGBB",left,56,MenuArt.MUTED);
        String[] names=EditorClient.german()?new String[]{"Alpha","Rot","Grün","Blau"}:new String[]{"Alpha","Red","Green","Blue"};
        for(int c=0;c<4;c++){
            int y=barY(c),bx=barsLeft(),bw=barsWidth();g.drawString(font,names[c]+"  "+channel(c),left,y+3,MenuArt.MUTED);
            for(int i=0;i<bw;i++){int v=(int)Math.round(i*255D/Math.max(1,bw-1));int color=composed(c,v);if(c==0)color=0xFF000000|(color&0xFFFFFF);g.fill(bx+i,y,bx+i+1,y+12,color);}
            int marker=bx+(int)Math.round(channel(c)*(bw-1)/255D);g.fill(marker-1,y-2,marker+2,y+14,0xFFFFFFFF);
        }
        int py=212,sw=28,gap=5;g.drawString(font,EditorClient.german()?"Schnellfarben":"Palette",left,py-14,MenuArt.MUTED);
        for(int i=0;i<PALETTE.length;i++){int col=i%9,row=i/9,x=left+col*(sw+gap),y=py+row*(sw+gap);g.fill(x,y,x+sw,y+sw,PALETTE[i]);g.fill(x,y,x+sw,y+1,0xFFFFFFFF);g.fill(x,y,x+1,y+sw,0xFFFFFFFF);}
        int previewY=Math.min(height-72,py+2*(sw+gap)+9);g.drawString(font,EditorClient.german()?"Vorschau":"Preview",left,previewY,MenuArt.MUTED);
        g.fill(left+70,previewY-5,left+box,previewY+17,0xFF23313B);g.fill(left+72,previewY-3,left+box-2,previewY+15,(alpha<<24)|(red<<16)|(green<<8)|blue);
        super.render(g,mx,my,dt);
    }

    private boolean setFromMouse(double mx,double my){
        int bx=barsLeft(),bw=barsWidth();for(int c=0;c<4;c++){int y=barY(c);if(mx>=bx&&mx<bx+bw&&my>=y-3&&my<=y+16){dragChannel=c;channel(c,(int)Math.round((mx-bx)*255D/Math.max(1,bw-1)));return true;}}
        int left=left(),py=212,sw=28,gap=5;for(int i=0;i<PALETTE.length;i++){int col=i%9,row=i/9,x=left+col*(sw+gap),y=py+row*(sw+gap);if(mx>=x&&mx<x+sw&&my>=y&&my<y+sw){int n=PALETTE[i];alpha=(n>>>24)&255;red=(n>>>16)&255;green=(n>>>8)&255;blue=n&255;syncHex();return true;}}
        return false;
    }
    @Override public boolean mouseClicked(double mx,double my,int button){if(button==0&&setFromMouse(mx,my))return true;return super.mouseClicked(mx,my,button);}
    @Override public boolean mouseDragged(double mx,double my,int button,double dx,double dy){if(button==0&&dragChannel>=0){int bx=barsLeft(),bw=barsWidth();channel(dragChannel,(int)Math.round((mx-bx)*255D/Math.max(1,bw-1)));return true;}return super.mouseDragged(mx,my,button,dx,dy);}
    @Override public boolean mouseReleased(double mx,double my,int button){dragChannel=-1;return super.mouseReleased(mx,my,button);}
    @Override public void onClose(){minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}
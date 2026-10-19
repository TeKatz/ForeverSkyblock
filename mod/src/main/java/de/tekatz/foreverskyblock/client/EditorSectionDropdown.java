package de.tekatz.foreverskyblock.client;

import java.util.List;
import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** Compact modal dropdown used by the Studio inspector instead of stacking every section header. */
public final class EditorSectionDropdown extends Screen {
    public record Option(String id,String english,String german) {
        String label(){return EditorClient.german()?german:english;}
    }
    private final Screen parent;private final List<Option> options;private final String current;private final Consumer<String> select;
    public EditorSectionDropdown(Screen parent,List<Option> options,String current,Consumer<String> select){
        super(Component.literal(EditorClient.german()?"Inspector-Bereich":"Inspector section"));this.parent=parent;this.options=List.copyOf(options);this.current=current;this.select=select;
    }
    @Override protected void init(){
        int w=Math.min(310,width-36),left=(width-w)/2,row=23,total=options.size()*row+54,top=Math.max(24,(height-total)/2);
        for(int i=0;i<options.size();i++){
            var option=options.get(i);String prefix=option.id().equals(current)?"✓ ":"";int y=top+30+i*row;
            addRenderableWidget(Button.builder(Component.literal(prefix+option.label()),b->{select.accept(option.id());minecraft.setScreen(parent);}).bounds(left,y,w,20).build());
        }
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Schließen":"Close"),b->onClose()).bounds(left+w-70,top+34+options.size()*row,70,20).build());
    }
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xB407111B);int w=Math.min(310,width-36),left=(width-w)/2,row=23,total=options.size()*row+54,top=Math.max(24,(height-total)/2);
        MenuArt.panel(g,left-8,top-8,w+16,total+16,0xFF0B1C2A,MenuArt.BORDER);g.drawString(font,title,left,top+4,MenuArt.WHITE);super.render(g,mx,my,dt);
    }
    @Override public void onClose(){minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}

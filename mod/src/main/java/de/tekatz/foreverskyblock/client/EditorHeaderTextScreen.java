package de.tekatz.foreverskyblock.client;

import java.util.function.BiConsumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** Focused editor for the visible labels inside the existing K-menu header. Blank values restore the built-in text. */
public final class EditorHeaderTextScreen extends Screen {
    private final Screen parent;
    private final String originalEn,originalDe;
    private final BiConsumer<String,String> save;
    private EditBox english,german;

    public EditorHeaderTextScreen(Screen parent,String english,String german,BiConsumer<String,String> save){
        super(Component.literal(EditorClient.german()?"Header-Text":"Header text"));this.parent=parent;this.originalEn=english;this.originalDe=german;this.save=save;
    }
    @Override protected void init(){
        int w=Math.min(430,width-40),x=(width-w)/2;
        english=addRenderableWidget(new EditBox(font,x,68,w,20,Component.literal("English")));english.setMaxLength(120);english.setValue(originalEn);
        german=addRenderableWidget(new EditBox(font,x,116,w,20,Component.literal("Deutsch")));german.setMaxLength(120);german.setValue(originalDe);
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Übernehmen":"Apply"),b->{save.accept(english.getValue(),german.getValue());minecraft.setScreen(parent);}).bounds(x+w-212,height-38,66,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Standard":"Default"),b->{save.accept("","");minecraft.setScreen(parent);}).bounds(x+w-140,height-38,66,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Abbrechen":"Cancel"),b->onClose()).bounds(x+w-68,height-38,66,20).build());
        setInitialFocus(EditorClient.german()?german:english);
    }
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xFF07111B);int w=Math.min(430,width-40),x=(width-w)/2;
        MenuArt.panel(g,x-10,22,w+20,142,0xFF0B1C2A,MenuArt.BORDER);g.drawString(font,title,x,32,MenuArt.WHITE);
        g.drawString(font,"English",x,56,MenuArt.MUTED);g.drawString(font,"Deutsch",x,104,MenuArt.MUTED);
        MenuArt.label(g,Component.literal(EditorClient.german()?"Leer lassen = Standardtext des K-Menüs":"Leave blank to use the built-in K-menu label"),x,145,w,MenuArt.MUTED,.72F);
        super.render(g,mx,my,dt);
    }
    @Override public void onClose(){minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}
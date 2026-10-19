package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.editor.EditorDocument;
import java.util.Arrays;
import java.util.Locale;
import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** Focused effect editor; intentionally uses native effects rather than arbitrary shader code. */
public final class EditorStyleEffectScreen extends Screen {
    public record Result(String effect,String color,float strength,boolean shadow) { }
    private static final String[] EFFECTS={"NONE","GLOW","PULSE","GRADIENT","SCANLINES","GLASS"};
    private final Screen parent;private final Consumer<Result> save;
    private String effect,color;private float strength;private boolean shadow;
    private EditBox strengthBox;

    public EditorStyleEffectScreen(Screen parent, EditorDocument.Style style, Consumer<Result> save){
        super(Component.literal(EditorClient.german()?"Visuelle Effekte":"Visual effects"));this.parent=parent;this.save=save;
        effect=style.effect();color=style.effectColor();strength=style.effectStrength();shadow=style.textShadow();
    }

    @Override protected void init(){
        int w=Math.min(390,width-40),left=(width-w)/2,y=48;
        addRenderableWidget(Button.builder(Component.literal((EditorClient.german()?"Effekt: ":"Effect: ")+effectLabel()),b->{int i=Arrays.asList(EFFECTS).indexOf(effect);effect=EFFECTS[(Math.max(0,i)+1)%EFFECTS.length];rebuildWidgets();}).bounds(left,y,w,20).build());
        addRenderableWidget(Button.builder(Component.literal((EditorClient.german()?"Effektfarbe":"Effect color")+(color.isBlank()?" · Default":" · #"+color.replace("#",""))),b->minecraft.setScreen(new EditorColorPicker(this,color,v->{color=v;minecraft.setScreen(this);}))).bounds(left,y+28,w,20).build());
        strengthBox=addRenderableWidget(new EditBox(font,left+118,y+58,w-118,20,Component.literal("Strength")));strengthBox.setMaxLength(6);strengthBox.setValue(String.format(Locale.ROOT,"%.2f",strength));
        addRenderableWidget(Button.builder(Component.literal((EditorClient.german()?"Textschatten: ":"Text shadow: ")+(shadow?"ON":"OFF")),b->{shadow=!shadow;b.setMessage(Component.literal((EditorClient.german()?"Textschatten: ":"Text shadow: ")+(shadow?"ON":"OFF")));}).bounds(left,y+86,w,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Übernehmen":"Apply"),b->apply()).bounds(left+w-212,height-34,66,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Standard":"Default"),b->{save.accept(new Result("NONE","",1F,false));minecraft.setScreen(parent);}).bounds(left+w-140,height-34,66,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Abbrechen":"Cancel"),b->onClose()).bounds(left+w-68,height-34,66,20).build());
    }
    private String effectLabel(){return EditorClient.german()?switch(effect){case "NONE"->"Keiner";case "GLOW"->"Leuchten";case "PULSE"->"Pulsieren";case "GRADIENT"->"Farbverlauf";case "SCANLINES"->"Scanlines";case "GLASS"->"Glas";default->effect;}:effect.charAt(0)+effect.substring(1).toLowerCase(Locale.ROOT);}
    private void apply(){
        try{strength=Math.clamp(Float.parseFloat(strengthBox.getValue().replace(',','.')),.1F,4F);}catch(NumberFormatException ignored){strength=1F;}
        save.accept(new Result(effect,color,strength,shadow));minecraft.setScreen(parent);
    }
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xFF07111B);int w=Math.min(390,width-40),left=(width-w)/2,y=20;
        MenuArt.panel(g,left-10,y,w+20,height-54,0xFF0B1C2A,MenuArt.BORDER);g.drawString(font,title,left,y+8,MenuArt.WHITE);
        g.drawString(font,EditorClient.german()?"Stärke (0.1–4.0)":"Strength (0.1–4.0)",left,109,MenuArt.MUTED);
        var preview=new EditorDocument.Style(0,0,0,0,EditorDocument.Anchor.START,EditorDocument.Anchor.START,1,1,"","","",false,"","","",effect,color,strength,shadow);
        int py=170,ph=Math.min(105,height-235);var old=MenuArt.pushStyle(preview);
        try{MenuArt.panel(g,left,py,w,ph,0xD10D202D,MenuArt.BORDER);MenuArt.centered(g,Component.literal(EditorClient.german()?"Live-Vorschau · Forever Skyblock":"Live preview · Forever Skyblock"),left+12,py+16,w-24,25,MenuArt.WHITE,1F);MenuArt.centered(g,Component.literal("SP  42   ·   Tier I"),left+12,py+48,w-24,22,MenuArt.CYAN,.82F);}finally{MenuArt.pushStyle(old);}
        super.render(g,mx,my,dt);
    }
    @Override public void onClose(){minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}

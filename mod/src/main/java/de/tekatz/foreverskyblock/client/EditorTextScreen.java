package de.tekatz.foreverskyblock.client;

import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.*;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** Long quest descriptions use a native multiline field, retaining intentional paragraphs. */
final class EditorTextScreen extends Screen {
    private final Screen parent;private final Consumer<String> save;private final String original;private MultiLineEditBox text;
    EditorTextScreen(Screen parent,String original,Consumer<String> save){super(Component.literal("Text"));this.parent=parent;this.original=original;this.save=save;}
    @Override protected void init(){text=addRenderableWidget(new MultiLineEditBox(font,22,34,width-44,height-80,Component.empty(),title));text.setCharacterLimit(6000);text.setValue(original);
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Übernehmen":"Apply"),b->{save.accept(text.getValue());minecraft.setScreen(parent);}).bounds(width-162,height-34,68,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Abbrechen":"Cancel"),b->onClose()).bounds(width-90,height-34,68,20).build());setInitialFocus(text);}
    @Override public void render(GuiGraphics g,int mx,int my,float dt){g.fill(0,0,width,height,0xFF07121D);g.drawString(font,EditorClient.german()?"Beschreibung bearbeiten":"Edit description",22,15,MenuArt.WHITE);super.render(g,mx,my,dt);}
    @Override public void onClose(){minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}

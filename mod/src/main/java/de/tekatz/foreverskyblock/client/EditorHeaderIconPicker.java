package de.tekatz.foreverskyblock.client;

import java.util.*;
import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.*;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;

/** Searchable item picker for replacing one of the built-in header pictograms. */
public final class EditorHeaderIconPicker extends Screen {
    private final Screen parent;private final Consumer<String> select;
    private List<ResourceLocation> matches=List.of();private EditBox search;private int scroll;
    public EditorHeaderIconPicker(Screen parent,Consumer<String> select){super(Component.literal(EditorClient.german()?"Header-Icon":"Header icon"));this.parent=parent;this.select=select;}
    @Override protected void init(){
        search=addRenderableWidget(new EditBox(font,24,32,width-190,20,title));search.setMaxLength(128);search.setHint(Component.literal(EditorClient.german()?"Item suchen…":"Search item…"));search.setResponder(s->{scroll=0;filter();});
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Standard":"Default"),b->{select.accept("");onClose();}).bounds(width-160,32,64,20).build());
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Zurück":"Back"),b->onClose()).bounds(width-90,32,64,20).build());filter();setInitialFocus(search);
    }
    private void filter(){String q=search.getValue().toLowerCase(Locale.ROOT);matches=BuiltInRegistries.ITEM.keySet().stream().filter(id->id.toString().contains(q)||BuiltInRegistries.ITEM.get(id).getDescription().getString().toLowerCase(Locale.ROOT).contains(q)).sorted(Comparator.comparing(ResourceLocation::toString)).toList();}
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xFF07111B);g.drawString(font,EditorClient.german()?"Icon aus registrierten Items wählen":"Choose an icon from registered items",24,14,MenuArt.WHITE);super.render(g,mx,my,dt);
        int rows=Math.max(1,(height-82)/24);for(int i=0;i<rows&&i+scroll<matches.size();i++){var id=matches.get(i+scroll);int y=62+i*24;MenuArt.panel(g,24,y,width-48,22,my>=y&&my<y+22?0xFF1A3E51:0xFF0D2231,MenuArt.BORDER);g.renderItem(BuiltInRegistries.ITEM.get(id).getDefaultInstance(),29,y+3);MenuArt.label(g,Component.literal(id.toString()),52,y+7,width-84,MenuArt.WHITE,.82F);}
    }
    @Override public boolean mouseClicked(double x,double y,int button){if(button==0&&x>=24&&x<width-24&&y>=62){int i=scroll+(int)((y-62)/24);if(i<matches.size()&&y<height-18){select.accept(matches.get(i).toString());onClose();return true;}}return super.mouseClicked(x,y,button);}
    @Override public boolean mouseScrolled(double x,double y,double h,double v){scroll=Math.clamp(scroll-(int)(v*3),0,Math.max(0,matches.size()-Math.max(1,(height-82)/24)));return true;}
    @Override public void onClose(){minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}
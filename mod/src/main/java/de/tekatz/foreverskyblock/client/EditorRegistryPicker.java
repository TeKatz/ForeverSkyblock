package de.tekatz.foreverskyblock.client;

import java.util.*;
import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.*;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.Items;

/** Search registered IDs and localized item names without requiring an optional recipe browser. */
final class EditorRegistryPicker extends Screen {
    private final Screen parent;private final Consumer<String> select;private final String kind;
    private List<ResourceLocation> matches=List.of();private EditBox search;private int scroll;
    EditorRegistryPicker(Screen parent,String kind,Consumer<String> select){super(Component.literal("Registry"));this.parent=parent;this.kind=kind;this.select=select;}
    @Override protected void init(){
        search=addRenderableWidget(new EditBox(font,24,30,width-112,20,Component.literal("Search")));search.setMaxLength(128);search.setHint(Component.literal(EditorClient.german()?"ID oder Itemname suchen…":"Search an ID or item name…"));search.setResponder(s->{scroll=0;filter();});
        addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Zurück":"Back"),b->onClose()).bounds(width-80,30,56,20).build());filter();setInitialFocus(search);
    }
    private void filter(){String q=search.getValue().toLowerCase(Locale.ROOT);var keys=switch(kind){case "block"->BuiltInRegistries.BLOCK.keySet();case "entity"->BuiltInRegistries.ENTITY_TYPE.keySet();default->BuiltInRegistries.ITEM.keySet();};
        matches=keys.stream().filter(id->id.toString().contains(q)||kind.equals("item")&&BuiltInRegistries.ITEM.get(id).getDescription().getString().toLowerCase(Locale.ROOT).contains(q)).sorted(Comparator.comparing(ResourceLocation::toString)).toList();}
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){g.fill(0,0,width,height,0xFF07121D);g.drawString(font,EditorClient.german()?"Registrierte Einträge auswählen":"Choose a registered entry",24,12,MenuArt.WHITE);super.render(g,mx,my,dt);
        int rows=Math.max(1,(height-80)/24);for(int i=0;i<rows&&i+scroll<matches.size();i++){var id=matches.get(i+scroll);int y=62+i*24;MenuArt.panel(g,24,y,width-48,22,my>=y&&my<y+22?0xFF1A3E51:0xFF0D2231,MenuArt.BORDER);
            if(kind.equals("item"))g.renderItem(BuiltInRegistries.ITEM.get(id).getDefaultInstance(),29,y+3);else if(kind.equals("block"))g.renderItem(BuiltInRegistries.BLOCK.get(id).asItem().getDefaultInstance(),29,y+3);else g.renderItem(Items.SPAWNER.getDefaultInstance(),29,y+3);
            MenuArt.label(g,Component.literal(id.toString()),52,y+7,width-84,MenuArt.WHITE,.9F);}
    }
    @Override public boolean mouseClicked(double x,double y,int button){if(button==0&&x>=24&&x<width-24&&y>=62){int i=scroll+(int)((y-62)/24);if(i<matches.size()&&y<height-18){select.accept(matches.get(i).toString());onClose();return true;}}return super.mouseClicked(x,y,button);}
    @Override public boolean mouseScrolled(double x,double y,double h,double v){scroll=Math.clamp(scroll-(int)(v*3),0,Math.max(0,matches.size()-Math.max(1,(height-80)/24)));return true;}
    @Override public boolean isPauseScreen(){return false;}
    @Override public void onClose(){minecraft.setScreen(parent);}
}

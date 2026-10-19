package de.tekatz.foreverskyblock.client;

import com.google.gson.*;
import de.tekatz.foreverskyblock.editor.*;
import de.tekatz.foreverskyblock.network.EditorNetwork;
import java.nio.file.*;
import java.util.*;
import java.util.function.*;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.*;
import net.minecraft.client.gui.screens.*;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.Items;
import org.lwjgl.glfw.GLFW;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** Three-pane authoring workspace with native preview, graph tools and typed property inspectors. */
public final class KMenuEditorScreen extends Screen {
    private enum Mode { LAYOUT,QUESTS,GRAPH,SHOP,CONTRACTS,CHAPTERS,SETTINGS }
    private final Screen parent;
    private EditorSession session;
    private EditorDocument opened;
    private Mode mode=Mode.LAYOUT;
    private String selected="header.sp",filter="",page="CHALLENGES",variant="",linkSource="",message="";
    private int listScroll,propertyScroll,leftWidth,rightWidth,canvasLeft,canvasRight,canvasTop=82,canvasBottom,propertyY,viewportIndex;
    private int previewState,simulation,maxPropertyScroll;
    private boolean linkTool,unlinkTool,resizeTool,tryMode,panning,focusMode;
    private final Map<String,String> pending=new LinkedHashMap<>();
    private final List<FieldLabel> fieldLabels=new ArrayList<>();
    private final Set<String> collapsedSections=new HashSet<>();
    private record FieldLabel(String label,int x,int y) { }
    private record ActionOption(Component label,EditorAction action) { }
    private EditBox search;
    private PremiumSkillScreen previewMenu;
    private Screen previewScreen;
    private EditorDocument displayDocument;
    private Map<String,EditorClient.Rect> previewBounds=Map.of();
    private float previewScale=1,previewX,previewY,zoom=1,panX=28,panY=30,layoutZoom=1,layoutPanX,layoutPanY;
    private String dragging="";private double dragX,dragY;private float startX,startY;private int startW,startH;
    private EditorDocument gesture;
    private long lastDraft;
    private static final int[][] VIEWPORTS={{640,360},{480,270},{944,522}};
    private static final String[] PREVIEW_DETAILS={"Menu","Quest detail","Shop detail","Contract detail"};
    private static final String[] PREVIEW_DETAILS_DE={"Menü","Quest-Details","Shop-Details","Contract-Details"};
    private static final String[] PREVIEW_STATES={"Live","Locked","Available","Completed"};
    private static final String[] PREVIEW_STATES_DE={"Live","Gesperrt","Verfügbar","Abgeschlossen"};

    public KMenuEditorScreen(Screen parent,EditorDocument document){super(Component.literal("K-menu editor"));this.parent=parent;opened=document;session=new EditorSession(document);}
    private static Component tr(String en,String de){return Component.literal(EditorClient.german()?de:en);}
    private void button(int x,int y,int w,String en,String de,EditorAction action){addRenderableWidget(Button.builder(tr(en,de),b->run(action)).bounds(x,y,w,18).build());}
    @FunctionalInterface private interface EditorAction { void run() throws Exception; }
    private void run(EditorAction action){try{action.run();}catch(Exception e){message=e.getMessage()==null?"Operation failed":e.getMessage();}}
    private String modeName(Mode value){return switch(value){case LAYOUT->"Layout";case QUESTS->"Quests";case GRAPH->EditorClient.german()?"Nodes":"Graph";case SHOP->"Shop";case CONTRACTS->"Contracts";case CHAPTERS->EditorClient.german()?"Kapitel":"Chapters";case SETTINGS->EditorClient.german()?"Optionen":"Settings";};}
    private String pageLabel(){String value=page.charAt(0)+page.substring(1).toLowerCase(Locale.ROOT);return (EditorClient.german()?"Seite: ":"Page: ")+value;}
    private String detailLabel(){return (EditorClient.german()?"Details: ":"Detail: ")+(EditorClient.german()?PREVIEW_DETAILS_DE[previewState]:PREVIEW_DETAILS[previewState]);}
    private String stateLabel(){return (EditorClient.german()?"Status: ":"State: ")+(EditorClient.german()?PREVIEW_STATES_DE[simulation]:PREVIEW_STATES[simulation]);}
    private boolean focused(){return focusMode&&mode==Mode.LAYOUT;}

    @Override protected void init(){
        boolean focus=focused();
        leftWidth=focus?0:width>=900?152:118;
        rightWidth=focus?0:width>=900?220:Math.max(168,width/3);
        canvasLeft=focus?8:leftWidth+9;
        canvasRight=focus?width-8:width-rightWidth-9;
        canvasBottom=height-28;
        clearWidgets();fieldLabels.clear();

        button(width-124,7,52,"Help","Hilfe",this::openHelp);
        button(width-66,7,58,"Close","Schließen",this::onClose);
        int tabWidth=Math.max(38,(width-20)/Mode.values().length);
        for(var m:Mode.values()){
            String name=modeName(m),shown=m==mode?"["+name+"]":name;
            button(8+m.ordinal()*tabWidth,30,tabWidth-3,shown,shown,()->{applyPending();mode=m;if(m!=Mode.LAYOUT)focusMode=false;selected=defaultSelection();listScroll=propertyScroll=0;pending.clear();rebuild();});
        }

        button(8,54,38,"Undo","Zurück",()->{pending.clear();session.undo();rebuild();});
        button(49,54,38,"Redo","Vor",()->{pending.clear();session.redo();rebuild();});
        button(92,54,62,"Files…","Dateien…",this::openFileMenu);
        button(159,54,66,"Validate","Prüfen",this::validateDraft);
        button(230,54,66,"Publish","Veröff.",this::confirmPublish);
        if(width>390)button(301,54,62,"More…","Mehr…",this::openMoreMenu);

        if(!focus){
            search=addRenderableWidget(new EditBox(font,8,84,leftWidth-8,18,tr("Search elements","Elemente suchen")));
            search.setMaxLength(120);search.setValue(filter);search.setHint(tr("Search…","Suchen…"));search.setResponder(s->{filter=s;listScroll=0;});
            button(width-rightWidth,84,rightWidth-8,"Apply changes [Enter]","Änderungen übernehmen [Enter]",()->{applyPending();rebuild();});
        } else search=null;

        if(mode==Mode.LAYOUT)layoutTools();else if(mode==Mode.GRAPH||mode==Mode.SHOP)graphTools();else contentTools();

        if(!focus){
            button(8,height-49,leftWidth-8,"Remove / disable selected","Auswahl entfernen / deaktiv.",this::removeSelected);
            propertyY=112-propertyScroll;inspector();maxPropertyScroll=Math.max(0,propertyY+propertyScroll-height+34);
        } else maxPropertyScroll=0;
        buildPreview();
    }
    private void rebuild(){init();}
    private void validateDraft(){applyPending();EditorCodec.validateUpdate(opened,session.current());message=tr("Document valid. Publish to activate it.","Dokument gültig. Zum Aktivieren veröffentlichen.").getString();}
    private String collection(){return switch(mode){case QUESTS,GRAPH->"quests";case SHOP->"products";case CONTRACTS->"contracts";case CHAPTERS->"chapters";case SETTINGS->"settings";default->"layout";};}
    private String defaultSelection(){return switch(mode){case LAYOUT->"header.sp";case QUESTS,GRAPH->session.current().quests().getFirst().id();case SHOP->session.current().products().getFirst().id();case CONTRACTS->session.current().contracts().getFirst().id();case CHAPTERS->session.current().chapters().getFirst().id();case SETTINGS->"settings";};}

    private void layoutTools(){
        int area=canvasRight-canvasLeft,gap=3,w=Math.max(52,(area-gap*2)/3);
        button(canvasLeft,84,w,pageLabel(),pageLabel(),()->{String[] pages={"CHALLENGES","SHOP","PROFILE","OVERVIEW","RESEARCH","PERKS"};page=pages[(Arrays.asList(pages).indexOf(page)+1)%pages.length];rebuild();});
        String viewport=(EditorClient.german()?"Ansicht: ":"Viewport: ")+VIEWPORTS[viewportIndex][0]+"×"+VIEWPORTS[viewportIndex][1];
        button(canvasLeft+w+gap,84,w,viewport,viewport,()->{viewportIndex=(viewportIndex+1)%VIEWPORTS.length;rebuild();});
        String interaction=tryMode?(EditorClient.german()?"Interaktion: Test":"Interaction: Try"):(EditorClient.german()?"Interaktion: Auswahl":"Interaction: Select");
        button(canvasLeft+2*(w+gap),84,w,interaction,interaction,()->{tryMode=!tryMode;rebuild();});

        int count=area>=570?6:area>=470?5:area>=370?4:3;
        int sw=Math.max(48,(area-gap*(count-1))/count),x=canvasLeft;
        String edit=resizeTool?(EditorClient.german()?"Bearbeiten: Größe":"Edit: Resize"):(EditorClient.german()?"Bearbeiten: Bewegen":"Edit: Move");
        button(x,106,sw,edit,edit,()->{resizeTool=!resizeTool;rebuild();});x+=sw+gap;
        String target=variant.isEmpty()?(EditorClient.german()?"Alle Größen":"All sizes"):(EditorClient.german()?"Nur diese Größe":"This size");
        button(x,106,sw,target,target,()->{variant=variant.isEmpty()?(viewportIndex==1?"small/":viewportIndex==2?"wide/":"compact/"):"";rebuild();});x+=sw+gap;
        button(x,106,sw,"+ Text","+ Text",this::addDecoration);x+=sw+gap;
        if(count>=4){String detail=detailLabel();button(x,106,sw,detail,detail,()->{previewState=(previewState+1)%4;rebuild();});x+=sw+gap;}
        if(count>=5){String state=stateLabel();button(x,106,sw,state,state,()->{simulation=(simulation+1)%4;rebuild();});x+=sw+gap;}
        if(count>=6){button(x,106,sw,focusMode?"Show panels":"Focus preview",focusMode?"Panels zeigen":"Vorschau fokussieren",()->{focusMode=!focusMode;rebuild();});}
    }
    private void graphTools(){
        int w=Math.max(35,(canvasRight-canvasLeft-8)/4);
        button(canvasLeft,84,w,mode==Mode.SHOP?"Requirements":linkTool?"Link ✓":"Link",mode==Mode.SHOP?"Voraussetzungen":linkTool?"Link ✓":"Link",()->{if(mode==Mode.SHOP){chooseQuestDependency(selected,false);return;}linkTool=!linkTool;unlinkTool=false;linkSource="";rebuild();});
        button(canvasLeft+w+2,84,w,"Unlink","Trennen",()->{if(mode==Mode.SHOP){chooseQuestDependency(selected,true);return;}unlinkTool=!unlinkTool;linkTool=false;linkSource="";rebuild();});
        button(canvasLeft+2*(w+2),84,w,"Auto arrange","Auto-Ordnen",this::arrange);
        button(canvasLeft+3*(w+2),84,w,"Center view","Ansicht zentrieren",()->{zoom=1;panX=28;panY=30;});
        button(canvasLeft,106,70,mode==Mode.SHOP?"+ Product":"+ Quest",mode==Mode.SHOP?"+ Angebot":"+ Quest",this::addEntry);
        button(canvasLeft+73,106,72,"Duplicate","Duplizieren",this::duplicate);
        if(canvasRight-canvasLeft>205)button(canvasLeft+148,106,62,"Disable","Deaktiv.",this::disable);
        if(mode==Mode.SHOP&&canvasRight-canvasLeft>275)button(canvasLeft+213,106,60,"+ Item","+ Item",this::addStack);
    }
    private void contentTools(){
        String add=mode==Mode.QUESTS?"+ Quest":mode==Mode.CONTRACTS?"+ Contract":mode==Mode.CHAPTERS?"+ Chapter":"+ New";
        String addDe=mode==Mode.QUESTS?"+ Quest":mode==Mode.CONTRACTS?"+ Contract":mode==Mode.CHAPTERS?"+ Kapitel":"+ Neu";
        button(canvasLeft,84,72,add,addDe,this::addEntry);button(canvasLeft+75,84,72,"Duplicate","Duplizieren",this::duplicate);
        if(canvasRight-canvasLeft>215)button(canvasLeft+150,84,62,"Disable","Deaktiv.",this::disable);
        if(mode==Mode.QUESTS){button(canvasLeft,106,82,"+ Objective","+ Aufgabe",this::addTask);button(canvasLeft+85,106,82,"Open graph","Graph öffnen",()->{applyPending();mode=Mode.GRAPH;rebuild();});}
        if(mode==Mode.SHOP)button(canvasLeft,106,70,"+ Item","+ Item",this::addStack);
    }

    private EditorDocument cachedDocument;private JsonObject cachedRoot;
    private JsonObject root(){if(cachedDocument!=session.current()){cachedDocument=session.current();cachedRoot=EditorCodec.JSON.toJsonTree(cachedDocument).getAsJsonObject();}return cachedRoot;}
    private JsonObject target(JsonObject root){
        if(mode==Mode.SETTINGS)return root.getAsJsonObject("settings");
        if(mode==Mode.LAYOUT){if(selected.startsWith("decoration/")){String id=selected.substring(11);for(var e:root.getAsJsonArray("decorations"))if(e.getAsJsonObject().get("id").getAsString().equals(id))return e.getAsJsonObject();return null;}
            var map=root.getAsJsonObject("layout");String key=variant+selected;if(!map.has(key))map.add(key,EditorCodec.JSON.toJsonTree(Style.defaults()));return map.getAsJsonObject(key);}
        for(var e:root.getAsJsonArray(collection()))if(e.getAsJsonObject().get("id").getAsString().equals(selected))return e.getAsJsonObject();return null;
    }
    private JsonElement at(JsonObject obj,String path){JsonElement current=obj;for(String p:path.split("\\."))current=current.isJsonArray()?current.getAsJsonArray().get(Integer.parseInt(p)):current.getAsJsonObject().get(p);return current;}
    private void put(JsonObject obj,String path,JsonElement value){String[] parts=path.split("\\.");JsonElement current=obj;for(int i=0;i<parts.length-1;i++)current=current.isJsonArray()?current.getAsJsonArray().get(Integer.parseInt(parts[i])):current.getAsJsonObject().get(parts[i]);if(current.isJsonArray())current.getAsJsonArray().set(Integer.parseInt(parts[parts.length-1]),value);else current.getAsJsonObject().add(parts[parts.length-1],value);}
    private void applyPending(){
        if(pending.isEmpty())return;var changes=new LinkedHashMap<>(pending);
        session.edit(root->{var obj=target(root);if(obj==null)obj=new JsonObject();for(var change:changes.entrySet()) {
            String key=change.getKey(),s=change.getValue();
            if(key.startsWith("node.")){var map=root.getAsJsonObject("nodes");var node=map.has(selected)?map.getAsJsonObject(selected):EditorCodec.JSON.toJsonTree(GraphGeometry.point(session.current(),selected,mode==Mode.SHOP)).getAsJsonObject();String field=key.substring(5);node.addProperty(field,field.equals("radius")?Integer.parseInt(s):Float.parseFloat(s));map.add(selected,node);continue;}
            JsonElement old=at(obj,key),value;
            if(old.isJsonPrimitive()&&old.getAsJsonPrimitive().isNumber())value=new JsonPrimitive(old.toString().contains(".")?Double.parseDouble(s):Long.parseLong(s));
            else if(old.isJsonPrimitive()&&old.getAsJsonPrimitive().isBoolean())value=new JsonPrimitive(Boolean.parseBoolean(s));else value=new JsonPrimitive(s);
            put(obj,key,value);
        }});pending.clear();buildPreview();
    }
    private static String propertyLabel(String label){if(!EditorClient.german())return switch(label){case "width"->"Width (0 = automatic)";case "height"->"Height (0 = automatic)";case "fill"->"Background color";case "border"->"Border color";case "text"->"Text color";default->label;};return switch(label){case "width"->"Breite (0 = automatisch)";case "height"->"Höhe (0 = automatisch)";case "fontScale"->"Schriftgröße";case "itemScale"->"Icon-Größe";case "fill"->"Hintergrundfarbe";case "border"->"Rahmenfarbe";case "text"->"Textfarbe";case "Horizontal anchor"->"Horizontale Verankerung";case "Vertical anchor"->"Vertikale Verankerung";case "Hidden"->"Ausgeblendet";case "Enabled"->"Aktiviert";case "Title · English"->"Titel · Englisch";case "Description · English"->"Beschreibung · Englisch";case "Icon / item ID"->"Icon / Item-ID";case "Required tier (-1 = none)"->"Benötigter Tier (-1 = keiner)";case "Chapter"->"Kapitel";case "Reward · SP"->"Belohnung · SP";case "Reward · PP"->"Belohnung · PP";case "Complete objectives"->"Aufgaben erfüllen";case "Prerequisites"->"Voraussetzungen";case "Price · SP"->"Preis · SP";case "Price · PP"->"Preis · PP";case "Target ID"->"Ziel-ID";case "Stack size"->"Stapelgröße";case "Order"->"Reihenfolge";case "Grid size"->"Rastergröße";case "Snap to grid"->"Am Raster einrasten";case "Animation milliseconds"->"Animationsdauer (ms)";case "Scroll speed"->"Scrollgeschwindigkeit";case "Card gap"->"Kartenabstand";case "Category gap"->"Kategorieabstand";case "Quest graph view"->"Quests als Node-Baum";case "Node · radius"->"Node · Radius";case "Contract type"->"Vertragstyp";case "Consumed item"->"Verbrauchtes Item";case "Required quantity"->"Benötigte Menge";case "Reward · SP (1–10)"->"Belohnung · SP (1–10)";case "Page"->"Seite";case "Live value"->"Live-Wert";default->label.replace("Objective ","Aufgabe ").replace(" · quantity"," · Menge").replace("Package item ","Paket-Item ");};}
    private String choiceLabel(String value){for(var chapter:session.current().chapters())if(chapter.id().equals(value))return chapter.title().value(EditorClient.german());if(!EditorClient.german())return value.isEmpty()?"—":value;return switch(value){case "true"->"Ja";case "false"->"Nein";case "ALL"->"Alle";case "ANY"->"Mindestens eines";case "START"->"Anfang";case "CENTER"->"Mitte";case "END"->"Ende";case "CRAFT"->"Herstellen";case "SMELT"->"Schmelzen";case "PICKUP"->"Aufheben";case "KILL"->"Besiegen";case "DIMENSION"->"Dimension betreten";case "BREAK"->"Block abbauen";case "PLACE"->"Block platzieren";case "ADVANCEMENT"->"Fortschritt erreichen";case "RESOURCE"->"Ressourcenabgabe";case "TEXT"->"Text";case "PANEL"->"Fläche";case "ITEM"->"Item";case "BAR"->"Fortschrittsbalken";case ""->"—";default->value;};}
    private void field(JsonObject obj,String path,String label){
        var value=at(obj,path);if(value==null||value.isJsonNull())return;int y=propertyY;propertyY+=35;
        if(y<108||y+32>height-31)return;int x=width-rightWidth;
        fieldLabels.add(new FieldLabel(propertyLabel(label),x,y));
        var input=addRenderableWidget(new EditBox(font,x,y+12,rightWidth-31,18,Component.literal(label)));input.setMaxLength(path.startsWith("description")?6000:512);input.setValue(pending.getOrDefault(path,value.getAsString()));input.setCursorPosition(0);input.setHighlightPos(0);input.setResponder(s->pending.put(path,s));
        if(path.contains("description"))button(width-28,y+12,20,"…","…",()->minecraft.setScreen(new EditorTextScreen(this,input.getValue(),s->pending.put(path,s))));
        else if(path.equals("icon")||path.equals("item")||path.endsWith(".item")||path.endsWith(".target")&&!taskPickerKind(obj,path).isEmpty())button(width-28,y+12,20,"…","…",()->{String kind=path.endsWith(".target")?taskPickerKind(obj,path):"item";minecraft.setScreen(new EditorRegistryPicker(this,kind,s->pending.put(path,s)));});
    }
    private String taskPickerKind(JsonObject obj,String path){String typePath=path.substring(0,path.lastIndexOf('.'))+".type";String type=pending.getOrDefault(typePath,at(obj,typePath).getAsString());return switch(type){case "KILL"->"entity";case "BREAK","PLACE"->"block";case "CRAFT","SMELT","PICKUP"->"item";default->"";};}
    private void choice(JsonObject obj,String path,String label,String... values){int y=propertyY;propertyY+=35;if(y<108||y+32>height-31)return;int x=width-rightWidth;fieldLabels.add(new FieldLabel(propertyLabel(label),x,y));String current=pending.getOrDefault(path,at(obj,path).getAsString());String shown=choiceLabel(current);
        button(x,y+12,rightWidth-8,shown,shown,()->{int index=Arrays.asList(values).indexOf(current);pending.put(path,values[(index+1)%values.length]);rebuild();});}
    private boolean section(String id,String en,String de){
        int y=propertyY;propertyY+=22;boolean collapsed=collapsedSections.contains(id);
        if(y>=108&&y+18<=height-31){String prefix=collapsed?"[+] ":"[-] ";button(width-rightWidth,y,rightWidth-8,prefix+en,prefix+de,()->{if(collapsed)collapsedSections.remove(id);else collapsedSections.add(id);rebuild();});}
        return !collapsed;
    }
    private void inspector(){
        var obj=target(root());if(obj==null){if(mode==Mode.SHOP&&selected.startsWith("shop:")&&section("shop.node","Node","Node"))nodeFields();return;}
        if(mode==Mode.LAYOUT&&!selected.startsWith("decoration/")) {
            if(section("layout.position","Position & size","Position & Größe"))for(String key:List.of("x","y","width","height"))field(obj,key,key);
            if(section("layout.anchors","Anchors","Verankerung")){choice(obj,"anchorX","Horizontal anchor","START","CENTER","END");choice(obj,"anchorY","Vertical anchor","START","CENTER","END");}
            boolean textStyle=selected.startsWith("nav.")||selected.startsWith("profile.")||selected.startsWith("card.")&&!selected.equals("card.icon")||selected.equals("card")||selected.startsWith("header.")&&!selected.equals("header.brand")||selected.equals("detail.title")||selected.equals("detail.prose");
            boolean iconStyle=selected.startsWith("nav.")||selected.equals("card.icon")||selected.equals("detail.icon");
            boolean panelStyle=selected.startsWith("nav.")||selected.startsWith("header.")&&!selected.equals("header.brand")||selected.equals("card")||selected.startsWith("body.")||selected.equals("categories")||selected.equals("detail")||selected.equals("detail.prose")||selected.equals("profile.stats");
            if(section("layout.appearance","Appearance","Darstellung")){if(textStyle){field(obj,"fontScale","fontScale");field(obj,"text","text");}if(iconStyle){field(obj,"itemScale","itemScale");field(obj,"icon","Icon / item ID");}if(panelStyle){field(obj,"fill","fill");field(obj,"border","border");}choice(obj,"hidden","Hidden","false","true");}
            if(section("layout.alignment","Alignment","Ausrichtung")){inspectorAction("Center horizontally","Horizontal zentrieren",()->align(false));inspectorAction("Center vertically","Vertikal zentrieren",()->align(true));}
            return;
        }
        if(mode==Mode.SETTINGS){
            if(section("settings.server","Server & recovery","Server & Wiederherstellung"))serverTools();
            if(section("settings.editor","Editor behavior","Editor-Verhalten")){field(obj,"grid","Grid size");choice(obj,"snap","Snap to grid","true","false");field(obj,"animationMillis","Animation milliseconds");field(obj,"scrollSpeed","Scroll speed");field(obj,"cardGap","Card gap");field(obj,"categoryGap","Category gap");choice(obj,"questGraph","Quest graph view","false","true");}
            return;
        }
        if(section("content.general","Content & status","Inhalt & Status")){
            if(obj.has("id"))readOnlyId(obj.get("id").getAsString());
            if(obj.has("enabled"))choice(obj,"enabled","Enabled","true","false");
            if(obj.has("title")){field(obj,"title.en","Title · English");field(obj,"title.de","Titel · Deutsch");}
            if(obj.has("description")){field(obj,"description.en","Description · English");field(obj,"description.de","Beschreibung · Deutsch");}
            if(obj.has("icon"))field(obj,"icon","Icon / item ID");
        }
        if(mode==Mode.LAYOUT){
            if(section("decoration.behavior","Element behavior","Element-Verhalten")){choice(obj,"type","Element","TEXT","PANEL","ITEM","BAR");choice(obj,"page","Page","ALL","OVERVIEW","CHALLENGES","SHOP","RESEARCH","PERKS","PROFILE");choice(obj,"binding","Live value","","SP","PP","TIER","PLAYTIME","ISLAND","DEATHS","KILLS","CHALLENGES");}
            if(section("decoration.geometry","Position & appearance","Position & Darstellung"))for(String key:List.of("x","y","width","height","color"))field(obj,key,key);return;
        }
        if(mode==Mode.CHAPTERS){if(section("chapter.order","Chapter order","Kapitel-Reihenfolge"))field(obj,"order","Order");return;}
        if(mode==Mode.QUESTS||mode==Mode.GRAPH){
            if(section("quest.progression","Progression & rewards","Fortschritt & Belohnungen")){
                if(obj.has("requiredTier"))field(obj,"requiredTier","Required tier (-1 = none)");
                choice(obj,"chapter","Chapter",session.current().chapters().stream().filter(c->!c.id().equals("contracts")).map(Chapter::id).toArray(String[]::new));
                field(obj,"rewardSp","Reward · SP");field(obj,"rewardPp","Reward · PP");choice(obj,"taskMatch","Complete objectives","ALL","ANY");choice(obj,"prerequisiteMatch","Prerequisites","ALL","ANY");
            }
            if(section("quest.objectives","Objectives","Aufgaben")){
                var tasks=obj.getAsJsonArray("tasks");for(int i=0;i<tasks.size();i++) {
                    var task=tasks.get(i).getAsJsonObject();boolean legacy=task.get("type").getAsString().equals("LEGACY");
                    if(!legacy){choice(obj,"tasks."+i+".type","Objective "+(i+1),"CRAFT","SMELT","PICKUP","KILL","DIMENSION","BREAK","PLACE","ADVANCEMENT");field(obj,"tasks."+i+".target","Target ID");}
                    field(obj,"tasks."+i+".count","Objective "+(i+1)+" · quantity");
                    if(!legacy&&opened.quest(selected)==null&&tasks.size()>1){final int taskIndex=i;inspectorAction("Remove objective","Aufgabe entfernen",()->{applyPending();session.edit(r->find(r.getAsJsonArray("quests"),selected).getAsJsonArray("tasks").remove(taskIndex));rebuild();});}
                }
            }
            if(section("quest.dependencies","Dependencies & node","Abhängigkeiten & Node")){prerequisites(obj);nodeFields();}return;
        }
        if(mode==Mode.SHOP){
            if(section("shop.rules","Pricing & requirements","Preise & Voraussetzungen")){if(obj.has("requiredTier"))field(obj,"requiredTier","Required tier (-1 = none)");field(obj,"spCost","Price · SP");field(obj,"ppCost","Price · PP");prerequisites(obj);}
            if(section("shop.contents","Package contents","Paket-Inhalt")){var stacks=obj.getAsJsonArray("contents");for(int i=0;i<stacks.size();i++){field(obj,"contents."+i+".item","Package item "+(i+1));field(obj,"contents."+i+".count","Stack size");if(opened.product(selected)==null&&stacks.size()>1){final int stackIndex=i;inspectorAction("Remove item","Item entfernen",()->{applyPending();session.edit(r->find(r.getAsJsonArray("products"),selected).getAsJsonArray("contents").remove(stackIndex));rebuild();});}}}
            if(section("shop.node","Node","Node"))nodeFields();return;
        }
        if(mode==Mode.CONTRACTS&&section("contract.rules","Contract rules","Contract-Regeln")){if(obj.has("requiredTier"))field(obj,"requiredTier","Required tier (-1 = none)");choice(obj,"type","Contract type","RESOURCE","WITHER","DRAGON");if(obj.get("type").getAsString().equals("RESOURCE"))field(obj,"item","Consumed item");field(obj,"count","Required quantity");field(obj,"rewardSp","Reward · SP (1–10)");}
    }
    private void align(boolean vertical){applyPending();var rect=previewBounds.get(selected);if(rect==null)return;int[] viewport=VIEWPORTS[viewportIndex];int delta=vertical?(viewport[1]-rect.height())/2-rect.y():(viewport[0]-rect.width())/2-rect.x();session.edit(r->{var obj=target(r);String key=vertical?"y":"x";obj.addProperty(key,obj.get(key).getAsInt()+delta);});rebuild();}
    private void inspectorAction(String en,String de,Runnable action){int y=propertyY;propertyY+=26;if(y>=108&&y+20<=height-31)button(width-rightWidth,y,rightWidth-8,en,de,action::run);}
    private void readOnlyId(String id){int y=propertyY;propertyY+=35;if(y<108||y+32>height-31)return;fieldLabels.add(new FieldLabel("ID",width-rightWidth,y));var box=addRenderableWidget(new EditBox(font,width-rightWidth,y+12,rightWidth-8,18,Component.literal("ID")));box.setMaxLength(128);box.setValue(id);box.setCursorPosition(0);box.setHighlightPos(0);box.setEditable(false);}
    private void prerequisites(JsonObject obj){int y=propertyY;propertyY+=40;if(y<108||y+35>height-31)return;fieldLabels.add(new FieldLabel((EditorClient.german()?"Voraussetzungen: ":"Prerequisites: ")+obj.getAsJsonArray("requires").size(),width-rightWidth,y));button(width-rightWidth,y+12,rightWidth-8,"Connect in graph","Im Graph verbinden",()->{applyPending();if(mode==Mode.SHOP){chooseQuestDependency(selected,hasShiftDown());return;}mode=Mode.GRAPH;linkTool=true;linkSource="";rebuild();});}
    private void nodeFields(){var obj=new JsonObject();obj.add("node",EditorCodec.JSON.toJsonTree(GraphGeometry.point(session.current(),selected,mode==Mode.SHOP)));field(obj,"node.x","Node · X");field(obj,"node.y","Node · Y");field(obj,"node.radius","Node · radius");int y=propertyY;propertyY+=38;if(y>=108&&y+35<=height-31)button(width-rightWidth,y+12,rightWidth-8,"Reset node position","Node-Position zurücksetzen",()->{session.edit(r->r.getAsJsonObject("nodes").remove(selected));rebuild();});}

    private EditorDocument editorPreviewDocument(EditorDocument source){
        if(tryMode)return source;
        var json=EditorCodec.JSON.toJsonTree(source).getAsJsonObject();
        json.getAsJsonObject("settings").addProperty("animationMillis",0);
        return EditorCodec.decode(json.toString());
    }
    private void buildPreview(){
        if(minecraft==null||mode!=Mode.LAYOUT)return;
        displayDocument=editorPreviewDocument(session.current());
        EditorClient.simulation(simulation);EditorClient.preview(displayDocument);try{previewMenu=new PremiumSkillScreen(true);int[] v=VIEWPORTS[viewportIndex];previewMenu.init(minecraft,v[0],v[1]);previewMenu.editorPage(page);previewScreen=previewMenu.editorPreviewDetail(previewState);}finally{EditorClient.preview(null);}
    }
    private List<String> entries(){List<String> values;
        if(mode==Mode.LAYOUT){var ids=new ArrayList<>(EditorDefaults.ELEMENTS);for(var w:session.current().decorations())ids.add("decoration/"+w.id());values=ids;}
        else if(mode==Mode.SETTINGS)values=List.of("settings");else {var out=new ArrayList<String>();if(mode==Mode.SHOP)out.addAll(GraphGeometry.SHOP_ROOTS);for(var e:root().getAsJsonArray(collection()))out.add(e.getAsJsonObject().get("id").getAsString());values=out;}
        return values.stream().filter(s->s.toLowerCase(Locale.ROOT).contains(filter.toLowerCase(Locale.ROOT))||entryName(s).toLowerCase(Locale.ROOT).contains(filter.toLowerCase(Locale.ROOT))).toList();
    }
    private String entryName(String id){if(mode==Mode.LAYOUT||mode==Mode.SETTINGS)return id;if(mode==Mode.SHOP&&id.startsWith("shop:"))return id.substring(5);for(var e:root().getAsJsonArray(collection())){var o=e.getAsJsonObject();if(o.get("id").getAsString().equals(id))return EditorCodec.JSON.fromJson(o.get("title"),Text.class).value(EditorClient.german());}return id;}
    private Component contextHint(){return switch(mode){
        case LAYOUT->tr(focusMode?"Focus preview: click elements directly; drag to move/resize; wheel zooms; Alt disables snapping.":"Layout: select an element on the left or in the preview. Drag to move/resize; wheel zooms; Alt disables snapping.",focusMode?"Vorschau-Fokus: Elemente direkt anklicken; ziehen zum Bewegen/Skalieren; Mausrad zoomt; Alt deaktiviert Raster.":"Layout: Element links oder in der Vorschau wählen. Ziehen bewegt/skaliert; Mausrad zoomt; Alt deaktiviert Raster.");
        case QUESTS->tr("Quests: edit content and objectives on the right; use Open graph for prerequisite links.","Quests: Inhalt und Aufgaben rechts bearbeiten; Voraussetzungen über Graph öffnen verbinden.");
        case GRAPH->tr("Graph: drag nodes to move, wheel to zoom. Link selects prerequisite first, dependent quest second.","Graph: Nodes ziehen, Mausrad zoomt. Link wählt zuerst Voraussetzung, dann abhängige Quest.");
        case SHOP->tr("Shop: edit products and requirements; graph position changes presentation, not purchase rules.","Shop: Angebote und Voraussetzungen bearbeiten; Node-Position ändert nur die Darstellung, nicht Kaufregeln.");
        case CONTRACTS->tr("Contracts: configure supported targets and rewards; published identities stay protected.","Contracts: unterstützte Ziele und Belohnungen konfigurieren; veröffentlichte Identitäten bleiben geschützt.");
        case CHAPTERS->tr("Chapters: edit names, icons and ordering used by challenge categories.","Kapitel: Namen, Icons und Reihenfolge der Challenge-Kategorien bearbeiten.");
        case SETTINGS->tr("Settings: editor behavior plus server reload/backup recovery. Use Files for draft/import/export actions.","Optionen: Editor-Verhalten sowie Server-Neuladen/Backup. Entwurf/Import/Export liegen unter Dateien.");
    };}
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float dt) { }
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xFF07111B);g.drawString(font,tr("FOREVER / K-MENU STUDIO","FOREVER / K-MENÜ STUDIO"),9,11,MenuArt.WHITE);
        MenuArt.label(g,Component.literal("r"+opened.revision()+(session.dirty()?" · *":"")),Math.min(230,width/2),11,Math.max(1,width/2-80),MenuArt.CYAN,.8F);
        if(!focused()){
            MenuArt.panel(g,5,78,leftWidth,height-106,0xFF0B1C2A,MenuArt.BORDER);MenuArt.panel(g,width-rightWidth-5,78,rightWidth+1,height-106,0xFF0B1C2A,MenuArt.BORDER);
            drawEntries(g,mx,my);
        }
        MenuArt.panel(g,canvasLeft,128,Math.max(1,canvasRight-canvasLeft),Math.max(1,canvasBottom-128),0xFF081723,MenuArt.BORDER);
        g.enableScissor(canvasLeft,128,canvasRight,canvasBottom);
        if(mode==Mode.LAYOUT)drawPreview(g,mx,my,dt);else if(mode==Mode.GRAPH||mode==Mode.SHOP)drawGraph(g,mx,my);else drawContent(g);
        g.disableScissor();
        if(!focused())for(var label:fieldLabels)MenuArt.label(g,Component.literal(label.label()),label.x(),label.y(),rightWidth-10,MenuArt.MUTED,.7F);
        super.render(g,mx,my,dt);
        String text=!message.isEmpty()?message:!EditorClient.status().isEmpty()?EditorClient.status():contextHint().getString()+"  ·  "+(EditorClient.german()?"Auswahl: ":"Selected: ")+selected;
        MenuArt.label(g,Component.literal(text),9,height-19,width-18,text.toLowerCase(Locale.ROOT).contains("invalid")?0xFFFF9898:MenuArt.MUTED,.75F);
        if(!linkSource.isEmpty())MenuArt.label(g,tr("Choose the dependent node","Abhängigen Node auswählen"),canvasLeft+4,canvasBottom-14,canvasRight-canvasLeft-8,MenuArt.GOLD,.65F);
    }
    private void drawEntries(GuiGraphics g,int mx,int my){var entries=entries();int rows=Math.max(1,(height-145)/25);listScroll=Math.clamp(listScroll,0,Math.max(0,entries.size()-rows));
        for(int i=0;i<rows&&i+listScroll<entries.size();i++){String id=entries.get(i+listScroll);int y=109+i*25;MenuArt.panel(g,8,y,leftWidth-8,23,id.equals(selected)?0xFF1B4053:my>=y&&my<y+23?0xFF142D3E:0xFF0C2030,id.equals(selected)?MenuArt.CYAN:MenuArt.BORDER);MenuArt.label(g,Component.literal(entryName(id)),12,y+7,leftWidth-17,id.equals(selected)?MenuArt.WHITE:MenuArt.MUTED,.75F);}
    }
    private void drawPreview(GuiGraphics g,int mx,int my,float dt){if(previewScreen==null)return;int[] v=VIEWPORTS[viewportIndex];previewScale=Math.min((canvasRight-canvasLeft-12)/(float)v[0],(canvasBottom-138)/(float)v[1]);previewScale=Math.max(.05F,previewScale)*layoutZoom;previewX=canvasLeft+(canvasRight-canvasLeft-v[0]*previewScale)/2+layoutPanX;previewY=134+(canvasBottom-134-v[1]*previewScale)/2+layoutPanY;
        EditorClient.preview(displayDocument==null?session.current():displayDocument);try {g.pose().pushPose();g.pose().translate(previewX,previewY,0);g.pose().scale(previewScale,previewScale,1);previewScreen.render(g,tryMode?(int)((mx-previewX)/previewScale):-100,tryMode?(int)((my-previewY)/previewScale):-100,dt);g.flush();g.pose().popPose();previewBounds=EditorClient.bounds();}finally{EditorClient.preview(null);}
        var r=previewBounds.get(selected);if(r!=null&&!tryMode){int x=(int)(previewX+r.x()*previewScale),y=(int)(previewY+r.y()*previewScale),w=(int)(r.width()*previewScale),h=(int)(r.height()*previewScale);outline(g,x,y,w,h,MenuArt.CYAN);g.fill(x+w-3,y+h-3,x+w+3,y+h+3,MenuArt.GOLD);}
    }
    private void outline(GuiGraphics g,int x,int y,int w,int h,int color){g.fill(x,y,x+w,y+1,color);g.fill(x,y+h-1,x+w,y+h,color);g.fill(x,y,x+1,y+h,color);g.fill(x+w-1,y,x+w,y+h,color);}
    private Node node(String id,int index){return GraphGeometry.point(session.current(),id,mode==Mode.SHOP);}
    private List<String> graphIds(){if(mode!=Mode.SHOP)return session.current().quests().stream().map(Quest::id).toList();var list=new ArrayList<>(GraphGeometry.SHOP_ROOTS);for(String q:GraphGeometry.shopQuests(session.current()))list.add("shop:quest/"+q);for(var p:session.current().products())if(p.enabled()&&!p.id().equals(de.tekatz.foreverskyblock.progression.ShopCatalog.TEST.id()))list.add(p.id());return list;}
    private String graphName(String id){return id.startsWith("shop:quest/")?session.current().quest(id.substring(11)).title().value(EditorClient.german()):id.startsWith("shop:")?entryName(id):mode==Mode.SHOP?session.current().product(id).title().value(EditorClient.german()):session.current().quest(id).title().value(EditorClient.german());}
    private String graphIcon(String id){return id.startsWith("shop:quest/")?session.current().quest(id.substring(11)).icon():id.startsWith("shop:")?id.equals("shop:tier1")?"minecraft:dragon_head":id.equals("shop:reach")?"minecraft:spyglass":"minecraft:nether_star":mode==Mode.SHOP?session.current().product(id).icon():session.current().quest(id).icon();}
    private void focusSelected(){if(mode!=Mode.GRAPH&&mode!=Mode.SHOP)return;var n=node(selected,graphIds().indexOf(selected));panX=(canvasRight-canvasLeft)/2F-n.x()*zoom;panY=(canvasBottom-134)/2F-n.y()*zoom;}
    private void drawGraph(GuiGraphics g,int mx,int my){
        int step=Math.max(8,(int)(session.current().settings().grid()*zoom*4));for(int x=canvasLeft+(int)(panX%step);x<canvasRight;x+=step)for(int y=130+(int)(panY%step);y<canvasBottom;y+=step)g.fill(x,y,x+1,y+1,0xFF234052);
        var ids=graphIds();
        if(mode==Mode.GRAPH)for(int i=0;i<ids.size();i++){var q=session.current().quest(ids.get(i));var b=node(q.id(),i);for(String dep:q.requires()){int j=ids.indexOf(dep);if(j<0)continue;var a=node(dep,j);MenuArt.arrow(g,gx(a.x()),gy(a.y()),gx(b.x()),gy(b.y()),(int)(b.radius()*zoom),q.prerequisiteMatch()==Match.ANY?MenuArt.GOLD:0xFF4D95A1);}}
        if(mode==Mode.SHOP){
            String[][] edges={{"shop:permanent","shop:reach"},{"shop:tier0","shop:tier1"}};
            for(var edge:edges)graphEdge(g,edge[0],edge[1],MenuArt.BORDER);
            for(var p:session.current().products()){for(String prerequisite:p.requires())graphEdge(g,"shop:quest/"+prerequisite,p.id(),MenuArt.GOLD);if(p.enabled()&&p.requiredTier()<=1&&!p.id().equals(de.tekatz.foreverskyblock.progression.ShopCatalog.TEST.id()))graphEdge(g,p.requiredTier()==0?"shop:tier0":"shop:tier1",p.id(),MenuArt.BORDER);}
        }
        for(int i=0;i<ids.size();i++){String id=ids.get(i);var n=node(id,i);if(id.equals(dragging)&&gesture!=null)n=gesture.nodes().getOrDefault(id,n);int x=gx(n.x()),y=gy(n.y()),r=Math.max(6,(int)(n.radius()*zoom));
            boolean enabled=id.startsWith("shop:")||mode==Mode.SHOP?true:session.current().quest(id).enabled();int color=id.equals(selected)?MenuArt.CYAN:!enabled?MenuArt.DIM:MenuArt.BORDER;
            MenuArt.circle(g,x,y,r+2,color);MenuArt.circle(g,x,y,r,0xFF123044);
            String icon=graphIcon(id);var item=BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(icon)).orElse(Items.BARRIER);
            g.pose().pushPose();g.pose().translate(x-8*zoom,y-8*zoom,0);g.pose().scale(zoom,zoom,1);g.renderItem(item.getDefaultInstance(),0,0);g.pose().popPose();
            String name=graphName(id);MenuArt.centered(g,Component.literal(name),x-(int)(61*zoom),y+r+(int)(7*zoom),Math.max(1,(int)(122*zoom)),Math.max(1,(int)(22*zoom)),MenuArt.WHITE,.7F*zoom);
        }
    }
    private void graphEdge(GuiGraphics g,String source,String destination,int color){var a=GraphGeometry.point(session.current(),source,true);var b=GraphGeometry.point(session.current(),destination,true);MenuArt.arrow(g,gx(a.x()),gy(a.y()),gx(b.x()),gy(b.y()),(int)(b.radius()*zoom),color);}
    private int gx(float x){return (int)(canvasLeft+panX+x*zoom);}private int gy(float y){return (int)(134+panY+y*zoom);}
    private void drawContent(GuiGraphics g){var o=target(root());if(o==null)return;int x=canvasLeft+12,y=145,w=canvasRight-canvasLeft-24;
        if(o.has("icon")&&!o.get("icon").getAsString().isEmpty())g.renderItem(BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(o.get("icon").getAsString())).orElse(Items.BARRIER).getDefaultInstance(),x,y);
        if(o.has("title"))MenuArt.wrapped(g,Component.literal(EditorCodec.JSON.fromJson(o.get("title"),Text.class).value(EditorClient.german())),x+25,y,w-25,MenuArt.WHITE,.9F,3);
        y+=36;if(o.has("description"))y=MenuArt.wrapped(g,Component.literal(EditorCodec.JSON.fromJson(o.get("description"),Text.class).value(EditorClient.german())),x,y,w,MenuArt.MUTED,.8F,10)+15;
        if(mode==Mode.QUESTS){var q=session.current().quest(selected);for(var t:q.tasks()){MenuArt.label(g,Component.literal(t.type()+" · "+t.target()+" × "+t.count()),x,y,w,MenuArt.CYAN,.65F);y+=16;}MenuArt.label(g,Component.literal("+"+q.rewardSp()+" SP / +"+q.rewardPp()+" PP"),x,y+8,w,MenuArt.GOLD,.9F);}
        else if(mode==Mode.SETTINGS)MenuArt.wrapped(g,tr("Layout changes stay in your draft until you publish. Export writes one portable ZIP or the selected local repository.","Layoutänderungen bleiben bis zur Veröffentlichung im Entwurf. Der Export erzeugt eine ZIP oder schreibt in die gewählte lokale Repo."),x,y,w,MenuArt.MUTED,.8F,20);
        else if(mode==Mode.CONTRACTS)MenuArt.label(g,tr("Resource turn-in consumes items on the server.","Ressourcenabgabe verbraucht Items auf dem Server."),x,y,w,MenuArt.GOLD,.7F);
    }
    @Override public boolean mouseClicked(double mx,double my,int button){
        if(!focused()&&mx<leftWidth+1&&my>=109&&my<109+Math.max(1,(height-145)/25)*25){var list=entries();int i=listScroll+(int)((my-109)/25);if(i<list.size()){run(()->{applyPending();selected=list.get(i);propertyScroll=0;pending.clear();focusSelected();rebuild();});return true;}}
        if(mx>=canvasLeft&&mx<canvasRight&&my>=130&&my<canvasBottom){
            if(!pending.isEmpty()){try{applyPending();}catch(RuntimeException e){message=e.getMessage();return true;}}
            if(mode==Mode.LAYOUT){if(button==2){layoutZoom=1;layoutPanX=layoutPanY=0;return true;}if(tryMode){EditorClient.preview(session.current());try{boolean handled=previewScreen.mouseClicked((mx-previewX)/previewScale,(my-previewY)/previewScale,button);previewScreen=previewMenu.editorActivePreview();return handled;}finally{EditorClient.preview(null);}}
                var entries=new ArrayList<>(previewBounds.entrySet());Collections.reverse(entries);String hit="";for(var e:entries)if(e.getValue().contains((mx-previewX)/previewScale,(my-previewY)/previewScale)){hit=e.getKey();break;}
                if(!hit.isEmpty()){selected=hit;pending.clear();propertyScroll=0;beginGesture(mx,my);var o=target(root());startX=o.get("x").getAsFloat();startY=o.get("y").getAsFloat();startW=o.get("width").getAsInt()==0?previewBounds.get(hit).width():o.get("width").getAsInt();startH=o.get("height").getAsInt()==0?previewBounds.get(hit).height():o.get("height").getAsInt();rebuild();return true;}
                panning=true;dragX=mx;dragY=my;return true;
            }else if(mode==Mode.GRAPH||mode==Mode.SHOP){var ids=graphIds();String hit="";for(int i=ids.size()-1;i>=0;i--){var n=node(ids.get(i),i);if(Math.hypot(mx-gx(n.x()),my-gy(n.y()))<=n.radius()*zoom+4){hit=ids.get(i);break;}}
                if(!hit.isEmpty()){String id=hit;if(linkTool||unlinkTool){if(linkSource.isEmpty()){linkSource=id;return true;}String source=linkSource;linkSource="";run(()->connect(source,id,unlinkTool));return true;}
                    selected=id;pending.clear();propertyScroll=0;var n=node(id,ids.indexOf(id));beginGesture(mx,my);startX=n.x();startY=n.y();rebuild();return true;}
                panning=true;dragX=mx;dragY=my;return true;
            }
        }
        return super.mouseClicked(mx,my,button);
    }
    private void beginGesture(double x,double y){dragging=selected;dragX=x;dragY=y;gesture=session.current();}
    @Override public boolean mouseDragged(double mx,double my,int button,double dx,double dy){
        if(!dragging.isEmpty()&&button==0){run(()->{var json=EditorCodec.JSON.toJsonTree(session.current()).getAsJsonObject();int grid=session.current().settings().grid();boolean snap=session.current().settings().snap()&&!hasAltDown();
            if(mode==Mode.LAYOUT){var o=target(json);if(o==null)return;float scale=previewScale;
                if(resizeTool){o.addProperty("width",Math.max(1,EditorSession.snap(startW+(mx-dragX)/scale,grid,snap)));o.addProperty("height",Math.max(1,EditorSession.snap(startH+(my-dragY)/scale,grid,snap)));}
                else{o.addProperty("x",EditorSession.snap(startX+(mx-dragX)/scale,grid,snap));o.addProperty("y",EditorSession.snap(startY+(my-dragY)/scale,grid,snap));}
            }else {var n=new Node(EditorSession.snap(startX+(mx-dragX)/zoom,grid,snap),EditorSession.snap(startY+(my-dragY)/zoom,grid,snap),node(dragging,graphIds().indexOf(dragging)).radius());json.getAsJsonObject("nodes").add(dragging,EditorCodec.JSON.toJsonTree(n));}
            gesture=EditorCodec.decode(json.toString());if(mode==Mode.LAYOUT){displayDocument=editorPreviewDocument(gesture);EditorClient.preview(displayDocument);try{previewMenu.init(minecraft,VIEWPORTS[viewportIndex][0],VIEWPORTS[viewportIndex][1]);previewMenu.editorPage(page);previewScreen=previewMenu.editorPreviewDetail(previewState);}finally{EditorClient.preview(null);}}
        });return true;}
        if(panning){if(mode==Mode.LAYOUT){layoutPanX+=dx;layoutPanY+=dy;}else{panX+=dx;panY+=dy;}return true;}return super.mouseDragged(mx,my,button,dx,dy);
    }
    @Override public boolean mouseReleased(double x,double y,int b){if(!dragging.isEmpty()){if(gesture!=null)session.replace(gesture);dragging="";gesture=null;rebuild();return true;}panning=false;return super.mouseReleased(x,y,b);}
    @Override public boolean mouseScrolled(double x,double y,double h,double v){if(!focused()&&x>width-rightWidth-8){propertyScroll=Math.clamp(propertyScroll-(int)(v*42),0,maxPropertyScroll);rebuild();return true;}if(!focused()&&x<leftWidth+3){listScroll=Math.max(0,listScroll-(int)(v*3));return true;}if(mode==Mode.GRAPH||mode==Mode.SHOP){float next=Math.clamp((float)(zoom*Math.pow(1.12,v)),.35F,2);panX=(float)(x-canvasLeft-(x-canvasLeft-panX)*next/zoom);panY=(float)(y-134-(y-134-panY)*next/zoom);zoom=next;return true;}if(mode==Mode.LAYOUT&&!tryMode){float next=Math.clamp((float)(layoutZoom*Math.pow(1.12,v)),.5F,4),ratio=next/layoutZoom;float cx=(canvasLeft+canvasRight)/2F,cy=(134+canvasBottom)/2F;layoutPanX=(float)(x-cx-(x-cx-layoutPanX)*ratio);layoutPanY=(float)(y-cy-(y-cy-layoutPanY)*ratio);layoutZoom=next;return true;}if(mode==Mode.LAYOUT&&tryMode){EditorClient.preview(session.current());try{return previewScreen.mouseScrolled((x-previewX)/previewScale,(y-previewY)/previewScale,h,v);}finally{EditorClient.preview(null);}}return super.mouseScrolled(x,y,h,v);}
    @Override public boolean keyPressed(int key,int scan,int modifiers){if(hasControlDown()&&key==GLFW.GLFW_KEY_S){saveDraft();return true;}if(hasControlDown()&&key==GLFW.GLFW_KEY_Z){pending.clear();if(hasShiftDown())session.redo();else session.undo();rebuild();return true;}if(key==GLFW.GLFW_KEY_ENTER){run(()->{applyPending();rebuild();});return true;}if(key==GLFW.GLFW_KEY_F1){openHelp();return true;}return super.keyPressed(key,scan,modifiers);}
    private void connect(String source,String destination,boolean remove){
        if(source.equals(destination))return;
        if(mode==Mode.SHOP){message=tr("Shop prerequisites are selected from quests in the inspector.","Shop-Voraussetzungen werden im Eigenschaftenfenster aus Quests gewählt.").getString();chooseQuestDependency(destination,remove);return;}
        session.edit(r->{var q=find(r.getAsJsonArray("quests"),destination);var a=q.getAsJsonArray("requires");var values=new LinkedHashSet<String>();for(var e:a)values.add(e.getAsString());if(remove)values.remove(source);else values.add(source);q.add("requires",EditorCodec.JSON.toJsonTree(values));});rebuild();
    }
    private void chooseQuestDependency(String id,boolean remove){
        if(session.current().product(id)==null){message=tr("Select a shop product first.","Wähle zuerst ein Shop-Angebot aus.").getString();return;}
        var quests=session.current().quests();minecraft.setScreen(new Screen(tr("Quest prerequisite","Quest-Voraussetzung")){
            int offset;
            @Override protected void init(){int row=0;for(int i=offset;i<quests.size()&&row<(height-65)/24;i++){var q=quests.get(i);addRenderableWidget(Button.builder(Component.literal(q.title().value(EditorClient.german())),b->{session.edit(r->{var p=find(r.getAsJsonArray("products"),id);var a=p.getAsJsonArray("requires");var values=new LinkedHashSet<String>();for(var v:a)values.add(v.getAsString());if(remove)values.remove(q.id());else values.add(q.id());p.add("requires",EditorCodec.JSON.toJsonTree(values));});minecraft.setScreen(KMenuEditorScreen.this);}).bounds(25,30+row++*24,width-50,20).build());}}
            @Override public void renderBackground(GuiGraphics g,int x,int y,float dt) { }
            @Override public void render(GuiGraphics g,int x,int y,float dt){g.fill(0,0,width,height,0xFF07111B);g.drawString(font,title,25,12,MenuArt.WHITE);super.render(g,x,y,dt);}
            @Override public boolean mouseScrolled(double x,double y,double h,double v){offset=Math.clamp(offset-(int)v*3,0,Math.max(0,quests.size()-5));clearWidgets();init();return true;}
            @Override public void onClose(){minecraft.setScreen(KMenuEditorScreen.this);}
        });
    }
    private static JsonObject find(JsonArray a,String id){for(var e:a)if(e.getAsJsonObject().get("id").getAsString().equals(id))return e.getAsJsonObject();throw new IllegalArgumentException("Entry missing: "+id);}
    private String suffix(){return UUID.randomUUID().toString().substring(0,8);}
    private void addEntry(){applyPending();String id;
        if(mode==Mode.QUESTS||mode==Mode.GRAPH){id="forever_skyblock:quest/"+suffix();var q=new Quest(id,true,new Text("New quest","Neue Quest"),new Text("Describe the objective.","Beschreibe die Aufgabe."),"minecraft:crafting_table",session.current().chapters().getFirst().id(),List.of(new Task("objective_1",TaskType.CRAFT,"minecraft:crafting_table",1)),Match.ALL,List.of(),Match.ALL,-1,1,0);session.edit(r->r.getAsJsonArray("quests").add(EditorCodec.JSON.toJsonTree(q)));}
        else if(mode==Mode.SHOP){id="forever_skyblock:shop/custom/"+suffix();var p=new Product(id,true,new Text("New package","Neues Paket"),new Text("Describe this package.","Beschreibe dieses Paket."),"minecraft:chest",0,1,0,List.of(),List.of(new EditorDocument.Stack("minecraft:cobblestone",1)));session.edit(r->r.getAsJsonArray("products").add(EditorCodec.JSON.toJsonTree(p)));}
        else if(mode==Mode.CONTRACTS){id="forever_skyblock:contract/custom/"+suffix();var c=new Contract(id,true,new Text("New contract","Neuer Vertrag"),new Text("Submit the required resources.","Gib die erforderlichen Ressourcen ab."),"minecraft:cobblestone",ContractType.RESOURCE,"minecraft:cobblestone",64,1,0);session.edit(r->r.getAsJsonArray("contracts").add(EditorCodec.JSON.toJsonTree(c)));}
        else if(mode==Mode.CHAPTERS){id="chapter_"+suffix();var c=new Chapter(id,new Text("New chapter","Neues Kapitel"),"minecraft:book",session.current().chapters().size());session.edit(r->r.getAsJsonArray("chapters").add(EditorCodec.JSON.toJsonTree(c)));}
        else return;selected=id;propertyScroll=0;pending.clear();focusSelected();rebuild();
    }
    private void duplicate(){applyPending();if(mode==Mode.LAYOUT||mode==Mode.SETTINGS||mode==Mode.CHAPTERS){addEntry();return;}String old=selected;String prefix=mode==Mode.SHOP?"forever_skyblock:shop/custom/":mode==Mode.CONTRACTS?"forever_skyblock:contract/custom/":"forever_skyblock:quest/";String id=prefix+suffix();session.edit(r->{var q=find(r.getAsJsonArray(collection()),old).deepCopy();q.addProperty("id",id);var title=q.getAsJsonObject("title");title.addProperty("en",title.get("en").getAsString().substring(0,Math.min(150,title.get("en").getAsString().length()))+" (copy)");title.addProperty("de",title.get("de").getAsString().substring(0,Math.min(150,title.get("de").getAsString().length()))+" (Kopie)");if(q.has("tasks")&&q.getAsJsonArray("tasks").get(0).getAsJsonObject().get("type").getAsString().equals("LEGACY")){q.add("tasks",EditorCodec.JSON.toJsonTree(List.of(new Task("objective_1",TaskType.CRAFT,"minecraft:cobblestone",1))));}r.getAsJsonArray(collection()).add(q);});selected=id;propertyScroll=0;pending.clear();focusSelected();rebuild();}
    private void disable(){applyPending();session.edit(r->{var obj=target(r);if(obj!=null&&obj.has("enabled"))obj.addProperty("enabled",!obj.get("enabled").getAsBoolean());});rebuild();}
    private void removeSelected(){applyPending();if(mode==Mode.SETTINGS)return;if(mode==Mode.LAYOUT){if(selected.startsWith("decoration/")){String id=selected.substring(11);session.edit(r->{var array=r.getAsJsonArray("decorations");for(int i=0;i<array.size();i++)if(array.get(i).getAsJsonObject().get("id").getAsString().equals(id)){array.remove(i);break;}});selected="header.sp";}else session.edit(r->r.getAsJsonObject("layout").add(variant+selected,EditorCodec.JSON.toJsonTree(Style.defaults())));rebuild();return;}
        if(selected.startsWith("shop:")){session.edit(r->r.getAsJsonObject("nodes").remove(selected));rebuild();return;}
        boolean published=mode==Mode.QUESTS||mode==Mode.GRAPH?opened.quest(selected)!=null:mode==Mode.SHOP?opened.product(selected)!=null:mode==Mode.CONTRACTS?opened.contract(selected)!=null:opened.chapters().stream().anyMatch(c->c.id().equals(selected));
        if(published){var object=target(root());if(object!=null&&object.has("enabled"))session.edit(r->target(r).addProperty("enabled",false));else message=tr("Published chapters are retained to preserve references.","Veröffentlichte Kapitel bleiben für bestehende Verweise erhalten.").getString();rebuild();return;}
        String id=selected;session.edit(r->{var array=r.getAsJsonArray(collection());for(int i=0;i<array.size();i++)if(array.get(i).getAsJsonObject().get("id").getAsString().equals(id)){array.remove(i);break;}r.getAsJsonObject("nodes").remove(id);for(String kind:List.of("quests","products"))for(var item:r.getAsJsonArray(kind)){var obj=item.getAsJsonObject();var deps=obj.getAsJsonArray("requires");for(int i=deps.size()-1;i>=0;i--)if(deps.get(i).getAsString().equals(id))deps.remove(i);}});selected=defaultSelection();pending.clear();rebuild();}
    private void addTask(){applyPending();var q=session.current().quest(selected);if(q==null||q.legacy()){message=tr("Duplicate a built-in quest to use custom objectives.","Dupliziere eine bestehende Quest für eigene Aufgaben.").getString();return;}session.edit(r->find(r.getAsJsonArray("quests"),selected).getAsJsonArray("tasks").add(EditorCodec.JSON.toJsonTree(new Task("objective_"+suffix(),TaskType.CRAFT,"minecraft:cobblestone",1))));rebuild();}
    private void addStack(){applyPending();session.edit(r->find(r.getAsJsonArray("products"),selected).getAsJsonArray("contents").add(EditorCodec.JSON.toJsonTree(new EditorDocument.Stack("minecraft:cobblestone",1))));rebuild();}
    private void addDecoration(){applyPending();String id="text_"+suffix();var w=new Decoration(id,page,DecorationType.TEXT,new Text("New text","Neuer Text"),"minecraft:paper","",20,20,120,16,"FF51D7ED");session.edit(r->r.getAsJsonArray("decorations").add(EditorCodec.JSON.toJsonTree(w)));selected="decoration/"+id;propertyScroll=0;rebuild();}
    private void arrange(){applyPending();if(mode==Mode.GRAPH)session.edit(r->r.add("nodes",EditorCodec.JSON.toJsonTree(GraphGeometry.arrange(session.current()))));else session.edit(r->{var map=r.getAsJsonObject("nodes");for(String id:graphIds())map.remove(id);});focusSelected();rebuild();}
    private void serverTools(){int y=propertyY;propertyY+=48;if(y<108||y+42>height-31)return;button(width-rightWidth,y,rightWidth-8,"Reload server config","Serverdatei neu laden",()->confirm(tr("Reload the server's K-menu file?","K-Menü-Datei des Servers neu laden?"),()->serverOperation(2)));button(width-rightWidth,y+22,rightWidth-8,"Restore published backup","Veröffentlichte Sicherung laden",()->confirm(tr("Restore the last published backup? Newer quest IDs will remain disabled.","Letzte veröffentlichte Sicherung laden? Neuere Quest-IDs bleiben deaktiviert erhalten."),()->serverOperation(3)));}
    private void serverOperation(int action){net.neoforged.neoforge.network.PacketDistributor.sendToServer(new EditorNetwork.Request(action,UUID.randomUUID(),0,1,opened.revision(),""));}
    private void reconcile(){applyPending();var latest=EditorClient.document();if(latest.revision()==opened.revision()){message=tr("Already using the current server revision.","Der Entwurf basiert auf der aktuellen Serverversion.").getString();return;}var result=EditorMerge.reconcile(opened,session.current(),latest,true);if(result.conflicts().isEmpty()){opened=latest;session.replace(result.document());rebuild();message=tr("Draft reconciled.","Entwurf abgeglichen.").getString();return;}String paths=result.conflicts().stream().limit(8).map(EditorMerge.Conflict::path).collect(java.util.stream.Collectors.joining("\n"));confirm(tr("Conflicts in:\n"+paths+"\nKeep your values? No uses server values for these fields.","Konflikte in:\n"+paths+"\nEigene Werte behalten? Nein übernimmt hier die Serverwerte."),()->{opened=latest;session.replace(result.document());rebuild();},()->{var merged=EditorMerge.reconcile(opened,session.current(),latest,false);opened=latest;session.replace(merged.document());rebuild();});}
    private boolean saveDraft(){try{applyPending();EditorStore.atomic(EditorClient.directory().resolve("draft.json"),EditorCodec.encode(session.current()));lastDraft=System.nanoTime();message=tr("Draft saved","Entwurf gespeichert").getString();return true;}catch(Exception e){message=e.getMessage();return false;}}
    private void loadDraft(){run(()->{var d=EditorStore.read(EditorClient.directory().resolve("draft.json"));session.replace(d.withRevision(opened.revision()));pending.clear();rebuild();message=tr("Draft loaded; compare before publishing.","Entwurf geladen; vor Veröffentlichung vergleichen.").getString();});}
    private void chooseImport(){pathDialog(tr("Import JSON / exported ZIP","JSON / exportierte ZIP importieren"),"",path->{var d=path.toString().toLowerCase(Locale.ROOT).endsWith(".zip")?EditorStore.importZip(path):EditorStore.read(path);session.replace(d.withRevision(opened.revision()));pending.clear();minecraft.setScreen(this);});}
    @FunctionalInterface private interface PathAction{void apply(Path path)throws Exception;}
    private void pathDialog(Component heading,String initial,PathAction action){minecraft.setScreen(new Screen(heading){EditBox path;String error="";
        @Override protected void init(){path=addRenderableWidget(new EditBox(font,22,height/2-12,width-44,20,title));path.setMaxLength(2048);path.setValue(initial);addRenderableWidget(Button.builder(tr("Continue","Weiter"),b->{try{action.apply(Path.of(path.getValue()));}catch(Exception e){error=e.getMessage();}}).bounds(width-170,height/2+22,70,20).build());addRenderableWidget(Button.builder(tr("Cancel","Abbrechen"),b->onClose()).bounds(width-94,height/2+22,70,20).build());setInitialFocus(path);}
        @Override public void renderBackground(GuiGraphics g,int x,int y,float dt) { }
        @Override public void render(GuiGraphics g,int x,int y,float dt){g.fill(0,0,width,height,0xFF07111B);g.drawString(font,title,22,height/2-35,MenuArt.WHITE);MenuArt.label(g,Component.literal(error),22,height/2+54,width-44,0xFFFFA0A0,.8F);super.render(g,x,y,dt);}
        @Override public void onClose(){minecraft.setScreen(KMenuEditorScreen.this);}
    });}
    private void export(){applyPending();confirm(tr("Export directly to a local Git repository? Choose No for a portable ZIP.","Direkt in eine lokale Git-Repo exportieren? Nein erzeugt eine portable ZIP."),()->pathDialog(tr("ForeverSkyblock repository folder","ForeverSkyblock-Repositoryordner"),"",path->{EditorStore.exportRepository(session.current(),path);message="Exported: "+path; minecraft.setScreen(this);}),()->run(()->{Path zip=EditorClient.directory().resolve("exports/kmenu-r"+opened.revision()+"-"+System.currentTimeMillis()+".zip");EditorStore.exportZip(session.current(),zip);message="Exported: "+zip;minecraft.setScreen(this);}));}
    private void confirmPublish(){applyPending();EditorCodec.validateUpdate(opened,session.current());confirm(tr("Publish this draft for every player on this server? Existing completed quests keep their rewards.","Diesen Entwurf für alle Spieler auf diesem Server aktivieren? Abgeschlossene Quests behalten ihre Belohnungen."),()->{saveDraft();EditorClient.publish(session.current());});}
    private void confirm(Component text,Runnable yes){confirm(text,yes,()->minecraft.setScreen(this));}
    private void confirm(Component text,Runnable yes,Runnable no){minecraft.setScreen(new ConfirmScreen(ok->{minecraft.setScreen(this);run(()->{if(ok)yes.run();else no.run();});},tr("K-menu editor","K-Menü-Editor"),text));}

    private ActionOption action(String en,String de,EditorAction action){return new ActionOption(tr(en,de),action);}
    private void actionMenu(Component title,ActionOption... actions){
        minecraft.setScreen(new Screen(title){
            @Override protected void init(){int w=Math.min(280,width-40),x=(width-w)/2,y=Math.max(34,(height-actions.length*24-28)/2);for(int i=0;i<actions.length;i++){ActionOption option=actions[i];addRenderableWidget(Button.builder(option.label(),b->{minecraft.setScreen(KMenuEditorScreen.this);KMenuEditorScreen.this.run(option.action());}).bounds(x,y+i*24,w,20).build());}addRenderableWidget(Button.builder(tr("Back","Zurück"),b->onClose()).bounds(x,y+actions.length*24+8,w,20).build());}
            @Override public void renderBackground(GuiGraphics g,int x,int y,float dt) { }
            @Override public void render(GuiGraphics g,int x,int y,float dt){g.fill(0,0,width,height,0xFF07111B);g.drawCenteredString(font,title,width/2,16,MenuArt.WHITE);super.render(g,x,y,dt);}
            @Override public void onClose(){minecraft.setScreen(KMenuEditorScreen.this);}
        });
    }
    private void openFileMenu(){actionMenu(tr("Files & drafts","Dateien & Entwürfe"),action("Save draft","Entwurf speichern",this::saveDraft),action("Load draft","Entwurf laden",this::loadDraft),action("Import JSON / ZIP","JSON / ZIP importieren",this::chooseImport),action("Export","Exportieren",this::export));}
    private void openMoreMenu(){actionMenu(tr("More actions","Weitere Aktionen"),action("Reset draft","Entwurf zurücksetzen",()->confirm(tr("Reset draft to the opened revision?","Entwurf auf die geöffnete Version zurücksetzen?"),()->{session.reset();pending.clear();rebuild();})),action("Merge newer server changes","Neuere Serveränderungen abgleichen",this::reconcile));}
    private void openHelp(){
        Mode helpMode=mode;
        minecraft.setScreen(new Screen(tr("K-menu Studio quick help","K-Menü Studio Kurzhilfe")){
            @Override protected void init(){addRenderableWidget(Button.builder(tr("Back to editor","Zurück zum Editor"),b->onClose()).bounds(width/2-70,height-32,140,20).build());}
            @Override public void renderBackground(GuiGraphics g,int x,int y,float dt) { }
            @Override public void render(GuiGraphics g,int mx,int my,float dt){
                g.fill(0,0,width,height,0xFF07111B);int boxW=Math.min(620,width-40),left=(width-boxW)/2,y=20;MenuArt.panel(g,left,y,boxW,height-60,0xFF0B1C2A,MenuArt.BORDER);MenuArt.centered(g,title,left+10,y+10,boxW-20,20,MenuArt.WHITE,1F);y+=40;
                y=MenuArt.wrapped(g,tr("1. Choose a workspace tab.  2. Select content on the left or directly in the canvas.  3. Edit grouped properties on the right and press Enter to apply.  4. Validate before publishing.","1. Arbeitsbereich oben wählen.  2. Inhalt links oder direkt auf der Fläche auswählen.  3. Gruppierte Eigenschaften rechts bearbeiten und mit Enter übernehmen.  4. Vor dem Veröffentlichen prüfen."),left+18,y,boxW-36,MenuArt.MUTED,.8F,8)+14;
                y=MenuArt.wrapped(g,tr("Files contains draft save/load plus import/export. More contains destructive reset and revision reconciliation. Undo/Redo also work with Ctrl+Z / Ctrl+Shift+Z; Ctrl+S saves the draft; F1 opens this help.","Dateien enthält Entwurf speichern/laden sowie Import/Export. Mehr enthält Reset und Versions-Abgleich. Undo/Redo funktionieren auch mit Ctrl+Z / Ctrl+Shift+Z; Ctrl+S speichert; F1 öffnet diese Hilfe."),left+18,y,boxW-36,MenuArt.MUTED,.8F,8)+14;
                Component modeHelp=switch(helpMode){case LAYOUT->tr("Layout: Select mode is intentionally animation-stable. Try mode runs the real menu interactions and animations. Use Focus preview to hide side panels temporarily.","Layout: Auswahlmodus bleibt absichtlich animationsstabil. Testmodus führt echte Menüinteraktionen und Animationen aus. Vorschau fokussieren blendet Seitenleisten temporär aus.");case QUESTS->tr("Quests: built-in tasks keep protected trigger semantics. Duplicate a built-in quest before replacing its objective structure.","Quests: eingebaute Aufgaben behalten geschützte Trigger-Semantik. Eine eingebaute Quest duplizieren, bevor die Aufgabenstruktur ersetzt wird.");case GRAPH->tr("Graph: Link = prerequisite first, dependent quest second. Unlink uses the same order. Drag empty space to pan; wheel zooms.","Graph: Link = zuerst Voraussetzung, dann abhängige Quest. Trennen nutzt dieselbe Reihenfolge. Leere Fläche ziehen verschiebt; Mausrad zoomt.");case SHOP->tr("Shop: node position is visual. Tier, price and quest requirements remain server-authoritative purchase rules.","Shop: Node-Position ist visuell. Tier, Preis und Quest-Voraussetzungen bleiben serverseitige Kaufregeln.");case CONTRACTS->tr("Contracts: only the supported resource / Wither / Dragon types are authorable without code changes.","Contracts: ohne Codeänderung sind nur die unterstützten Ressourcen-/Wither-/Dragon-Typen erstellbar.");case CHAPTERS->tr("Chapters: order controls the challenge category list; icons use registered item IDs.","Kapitel: Reihenfolge steuert die Challenge-Kategorien; Icons nutzen registrierte Item-IDs.");case SETTINGS->tr("Settings: server recovery actions are separated from normal editing because they affect the authoritative published file.","Optionen: Server-Wiederherstellung ist vom normalen Bearbeiten getrennt, da sie die authoritative veröffentlichte Datei betrifft.");};
                MenuArt.wrapped(g,modeHelp,left+18,y,boxW-36,MenuArt.CYAN,.82F,10);super.render(g,mx,my,dt);
            }
            @Override public void onClose(){minecraft.setScreen(KMenuEditorScreen.this);}
        });
    }

    public void serverReply(EditorNetwork.Reply r){if(r.kind()==2){message=r.message();if(message.startsWith("Published")){pending.clear();opened=EditorClient.document();session=new EditorSession(opened);rebuild();}}else if(r.kind()==0&&r.revision()!=opened.revision())message=tr("Server revision changed. Your draft is preserved; reopen to reconcile.","Serverversion geändert. Dein Entwurf bleibt erhalten; zum Abgleichen neu öffnen.").getString();}
    @Override public void onFilesDrop(List<Path> paths){if(paths.size()!=1){message="Drop one JSON or export ZIP";return;}run(()->{Path p=paths.getFirst();var d=p.toString().endsWith(".zip")?EditorStore.importZip(p):EditorStore.read(p);session.replace(d.withRevision(opened.revision()));pending.clear();rebuild();});}
    @Override public void onClose(){if(session.dirty()||!pending.isEmpty())confirm(tr("Save your draft before closing?","Entwurf vor dem Schließen speichern?"),()->{if(saveDraft())minecraft.setScreen(parent);},()->minecraft.setScreen(parent));else minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}

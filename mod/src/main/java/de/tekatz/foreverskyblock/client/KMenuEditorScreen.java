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
    private int previewState,simulation;private boolean linkTool,unlinkTool,resizeTool,tryMode,panning;
    private final Map<String,String> pending=new LinkedHashMap<>();
    private final List<FieldLabel> fieldLabels=new ArrayList<>();
    private record FieldLabel(String label,int x,int y) { }
    private EditBox search;
    private PremiumSkillScreen previewMenu;
    private Screen previewScreen;
    private Map<String,EditorClient.Rect> previewBounds=Map.of();
    private float previewScale=1,previewX,previewY,zoom=1,panX=28,panY=30;
    private String dragging="";private double dragX,dragY;private float startX,startY;private int startW,startH;
    private EditorDocument gesture;
    private long lastDraft;
    private static final int[][] VIEWPORTS={{640,360},{480,270},{944,522}};

    public KMenuEditorScreen(Screen parent,EditorDocument document){super(Component.literal("K-menu editor"));this.parent=parent;opened=document;session=new EditorSession(document);}
    private static Component tr(String en,String de){return Component.literal(EditorClient.german()?de:en);}
    private void button(int x,int y,int w,String en,String de,Runnable action){addRenderableWidget(Button.builder(tr(en,de),b->run(action)).bounds(x,y,w,18).build());}
    private void run(Runnable action){try{action.run();}catch(Exception e){message=e.getMessage()==null?"Operation failed":e.getMessage();}}
    @Override protected void init(){
        leftWidth=width>=900?166:122;rightWidth=width>=900?240:Math.max(170,width/3);canvasLeft=leftWidth+9;canvasRight=width-rightWidth-9;canvasBottom=height-28;
        clearWidgets();fieldLabels.clear();
        button(width-66,7,58,"Close","Schließen",this::onClose);
        int tabWidth=Math.max(38,(width-20)/Mode.values().length);for(var m:Mode.values())button(8+m.ordinal()*tabWidth,30,tabWidth-3,m.name(),switch(m){case LAYOUT->"Layout";case QUESTS->"Quests";case GRAPH->"Nodes";case SHOP->"Shop";case CONTRACTS->"Contracts";case CHAPTERS->"Kapitel";case SETTINGS->"Optionen";},()->{applyPending();mode=m;selected=defaultSelection();listScroll=propertyScroll=0;pending.clear();rebuild();});
        button(8,54,38,"Undo","Zurück",()->{pending.clear();session.undo();rebuild();});button(49,54,38,"Redo","Vor",()->{pending.clear();session.redo();rebuild();});
        button(90,54,48,"Draft","Entwurf",this::saveDraft);button(141,54,48,"Load","Laden",this::loadDraft);
        button(192,54,48,"Import","Import",this::chooseImport);button(243,54,48,"Export","Export",this::export);
        button(294,54,48,"Check","Prüfen",()->{applyPending();EditorCodec.validateUpdate(opened,session.current());message=tr("Document valid. Publish to activate it.","Dokument gültig. Zum Aktivieren veröffentlichen.").getString();});
        button(345,54,60,"Publish","Aktivieren",this::confirmPublish);
        if(width>500)button(408,54,60,"Reset","Reset",()->confirm(tr("Reset draft to the opened revision?","Entwurf auf die geöffnete Version zurücksetzen?"),()->{session.reset();pending.clear();rebuild();}));
        search=addRenderableWidget(new EditBox(font,8,84,leftWidth-8,18,tr("Search","Suchen")));search.setMaxLength(120);search.setValue(filter);search.setResponder(s->{filter=s;listScroll=0;});
        button(width-rightWidth,84,rightWidth-8,"Apply properties [Enter]","Eigenschaften übernehmen [Enter]",()->{applyPending();rebuild();});
        if(mode==Mode.LAYOUT)layoutTools();else if(mode==Mode.GRAPH||mode==Mode.SHOP)graphTools();else contentTools();
        propertyY=112-propertyScroll;inspector();buildPreview();
    }
    private void rebuild(){init();}
    private String collection(){return switch(mode){case QUESTS,GRAPH->"quests";case SHOP->"products";case CONTRACTS->"contracts";case CHAPTERS->"chapters";case SETTINGS->"settings";default->"layout";};}
    private String defaultSelection(){return switch(mode){case LAYOUT->"header.sp";case QUESTS,GRAPH->session.current().quests().getFirst().id();case SHOP->session.current().products().getFirst().id();case CONTRACTS->session.current().contracts().getFirst().id();case CHAPTERS->session.current().chapters().getFirst().id();case SETTINGS->"settings";};}
    private void layoutTools(){
        button(canvasLeft,84,Math.max(45,(canvasRight-canvasLeft)/3-2),page,page,()->{String[] pages={"CHALLENGES","SHOP","PROFILE","OVERVIEW","RESEARCH","PERKS"};page=pages[(Arrays.asList(pages).indexOf(page)+1)%pages.length];buildPreview();});
        int w=Math.max(45,(canvasRight-canvasLeft)/3-2);button(canvasLeft+w+2,84,w,VIEWPORTS[viewportIndex][0]+"×"+VIEWPORTS[viewportIndex][1],VIEWPORTS[viewportIndex][0]+"×"+VIEWPORTS[viewportIndex][1],()->{viewportIndex=(viewportIndex+1)%VIEWPORTS.length;buildPreview();});
        button(canvasLeft+2*(w+2),84,w,tryMode?"Try":"Select",tryMode?"Testen":"Auswahl",()->{tryMode=!tryMode;rebuild();});
        button(canvasLeft,106,48,resizeTool?"Resize":"Move",resizeTool?"Größe":"Bewegen",()->{resizeTool=!resizeTool;rebuild();});
        button(canvasLeft+51,106,48,variant.isEmpty()?"All":"Size",variant.isEmpty()?"Alle":"Größe",()->{variant=variant.isEmpty()?(viewportIndex==1?"small/":viewportIndex==2?"wide/":"compact/"):"";rebuild();});
        button(canvasLeft+102,106,48,"+ Text","+ Text",this::addDecoration);
        if(canvasRight-canvasLeft>205)button(canvasLeft+153,106,48,"Detail","Details",()->{previewState=(previewState+1)%4;buildPreview();});
        if(canvasRight-canvasLeft>255)button(canvasLeft+204,106,48,"State "+simulation,"Status "+simulation,()->{simulation=(simulation+1)%4;buildPreview();rebuild();});
    }
    private void graphTools(){
        int w=Math.max(35,(canvasRight-canvasLeft-8)/4);
        button(canvasLeft,84,w,linkTool?"Link ✓":"Link",linkTool?"Link ✓":"Link",()->{linkTool=!linkTool;unlinkTool=false;linkSource="";rebuild();});
        button(canvasLeft+w+2,84,w,"Unlink","Trennen",()->{unlinkTool=!unlinkTool;linkTool=false;linkSource="";rebuild();});
        button(canvasLeft+2*(w+2),84,w,"Arrange","Ordnen",this::arrange);
        button(canvasLeft+3*(w+2),84,w,"Center","Zentrieren",()->{zoom=1;panX=28;panY=30;});
        button(canvasLeft,106,60,"+ New","+ Neu",this::addEntry);button(canvasLeft+63,106,64,"Duplicate","Duplizieren",this::duplicate);
        if(canvasRight-canvasLeft>185)button(canvasLeft+130,106,55,"Disable","Deaktiv.",this::disable);
        if(mode==Mode.SHOP&&canvasRight-canvasLeft>245)button(canvasLeft+188,106,54,"+ Item","+ Item",this::addStack);
    }
    private void contentTools(){
        button(canvasLeft,84,54,"+ New","+ Neu",this::addEntry);button(canvasLeft+57,84,64,"Duplicate","Duplizieren",this::duplicate);
        if(canvasRight-canvasLeft>178)button(canvasLeft+124,84,54,"Disable","Deaktiv.",this::disable);
        if(mode==Mode.QUESTS){button(canvasLeft,106,70,"+ Objective","+ Aufgabe",this::addTask);button(canvasLeft+73,106,70,"Node graph","Node-Baum",()->{applyPending();mode=Mode.GRAPH;rebuild();});}
        if(mode==Mode.SHOP)button(canvasLeft,106,70,"+ Item","+ Item",this::addStack);
    }
    private JsonObject root(){return EditorCodec.JSON.toJsonTree(session.current()).getAsJsonObject();}
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
        session.edit(root->{var obj=target(root);if(obj==null)return;for(var change:changes.entrySet()) {
            String key=change.getKey(),s=change.getValue();JsonElement old=at(obj,key),value;
            if(old.isJsonPrimitive()&&old.getAsJsonPrimitive().isNumber())value=new JsonPrimitive(old.toString().contains(".")?Double.parseDouble(s):Long.parseLong(s));
            else if(old.isJsonPrimitive()&&old.getAsJsonPrimitive().isBoolean())value=new JsonPrimitive(Boolean.parseBoolean(s));else value=new JsonPrimitive(s);
            put(obj,key,value);
        }});pending.clear();buildPreview();
    }
    private void field(JsonObject obj,String path,String label){
        var value=at(obj,path);if(value==null||value.isJsonNull())return;int y=propertyY;propertyY+=35;
        if(y<108||y+32>height-31)return;int x=width-rightWidth;
        fieldLabels.add(new FieldLabel(label,x,y));
        var input=addRenderableWidget(new EditBox(font,x,y+12,rightWidth-31,18,Component.literal(label)));input.setMaxLength(path.startsWith("description")?6000:512);input.setValue(pending.getOrDefault(path,value.getAsString()));input.setResponder(s->pending.put(path,s));
        if(path.contains("description"))button(width-28,y+12,20,"…","…",()->minecraft.setScreen(new EditorTextScreen(this,input.getValue(),s->pending.put(path,s))));
        else if(path.equals("icon")||path.endsWith(".target")||path.equals("item")||path.endsWith(".item"))button(width-28,y+12,20,"…","…",()->{String kind="item";if(path.endsWith(".target")){String type=at(obj,path.substring(0,path.lastIndexOf('.'))+".type").getAsString();kind=type.equals("KILL")?"entity":type.equals("BREAK")||type.equals("PLACE")?"block":"item";}String pickerKind=kind;minecraft.setScreen(new EditorRegistryPicker(this,pickerKind,s->pending.put(path,s)));});
    }
    private void choice(JsonObject obj,String path,String label,String... values){int y=propertyY;propertyY+=35;if(y<108||y+32>height-31)return;int x=width-rightWidth;fieldLabels.add(new FieldLabel(label,x,y));String current=pending.getOrDefault(path,at(obj,path).getAsString());
        button(x,y+12,rightWidth-8,current,current,()->{int index=Arrays.asList(values).indexOf(current);pending.put(path,values[(index+1)%values.length]);rebuild();});}
    private void inspector(){
        var obj=target(root());if(obj==null)return;
        if(mode==Mode.LAYOUT&&!selected.startsWith("decoration/")) {
            for(String key:List.of("x","y","width","height"))field(obj,key,key);
            choice(obj,"anchorX","Horizontal anchor","START","CENTER","END");choice(obj,"anchorY","Vertical anchor","START","CENTER","END");
            for(String key:List.of("fontScale","itemScale","fill","border","text"))field(obj,key,key);
            choice(obj,"hidden","Hidden","false","true");return;
        }
        if(mode==Mode.SETTINGS){field(obj,"grid","Grid size");choice(obj,"snap","Snap to grid","true","false");field(obj,"animationMillis","Animation milliseconds");field(obj,"scrollSpeed","Scroll speed");choice(obj,"questGraph","Quest graph view","false","true");return;}
        if(obj.has("enabled"))choice(obj,"enabled","Enabled","true","false");
        if(obj.has("title")){field(obj,"title.en","Title · English");field(obj,"title.de","Titel · Deutsch");}
        if(obj.has("description")){field(obj,"description.en","Description · English");field(obj,"description.de","Beschreibung · Deutsch");}
        if(obj.has("icon"))field(obj,"icon","Icon / item ID");
        if(mode==Mode.LAYOUT){choice(obj,"type","Element","TEXT","PANEL","ITEM","BAR");choice(obj,"page","Page","ALL","OVERVIEW","CHALLENGES","SHOP","RESEARCH","PERKS","PROFILE");choice(obj,"binding","Live value","","SP","PP","TIER","PLAYTIME","ISLAND","DEATHS","KILLS","CHALLENGES");for(String key:List.of("x","y","width","height","color"))field(obj,key,key);return;}
        if(mode==Mode.CHAPTERS){field(obj,"order","Order");return;}
        if(obj.has("requiredTier"))field(obj,"requiredTier","Required tier (-1 = none)");
        if(mode==Mode.QUESTS||mode==Mode.GRAPH){
            choice(obj,"chapter","Chapter",session.current().chapters().stream().map(Chapter::id).toArray(String[]::new));
            field(obj,"rewardSp","Reward · SP");field(obj,"rewardPp","Reward · PP");choice(obj,"taskMatch","Complete objectives","ALL","ANY");choice(obj,"prerequisiteMatch","Prerequisites","ALL","ANY");
            var tasks=obj.getAsJsonArray("tasks");for(int i=0;i<tasks.size();i++) {
                var task=tasks.get(i).getAsJsonObject();boolean legacy=task.get("type").getAsString().equals("LEGACY");
                if(!legacy){choice(obj,"tasks."+i+".type","Objective "+(i+1),"CRAFT","SMELT","PICKUP","KILL","DIMENSION","BREAK","PLACE","ADVANCEMENT");field(obj,"tasks."+i+".target","Target ID");}
                field(obj,"tasks."+i+".count","Objective "+(i+1)+" · quantity");
            }
            prerequisites(obj);nodeFields();return;
        }
        if(mode==Mode.SHOP){field(obj,"spCost","Price · SP");field(obj,"ppCost","Price · PP");prerequisites(obj);var stacks=obj.getAsJsonArray("contents");for(int i=0;i<stacks.size();i++){field(obj,"contents."+i+".item","Package item "+(i+1));field(obj,"contents."+i+".count","Stack size");}nodeFields();return;}
        if(mode==Mode.CONTRACTS){choice(obj,"type","Contract type","RESOURCE","WITHER","DRAGON");if(obj.get("type").getAsString().equals("RESOURCE"))field(obj,"item","Consumed item");field(obj,"count","Required quantity");field(obj,"rewardSp","Reward · SP (1–10)");}
    }
    private void prerequisites(JsonObject obj){int y=propertyY;propertyY+=40;if(y<108||y+35>height-31)return;fieldLabels.add(new FieldLabel("Prerequisites: "+obj.getAsJsonArray("requires").size(),width-rightWidth,y));button(width-rightWidth,y+12,rightWidth-8,"Connect in node view","Im Node-Baum verbinden",()->{applyPending();if(mode==Mode.SHOP){chooseQuestDependency(selected,hasShiftDown());return;}mode=Mode.GRAPH;linkTool=true;linkSource="";rebuild();});}
    private void nodeFields(){int y=propertyY;propertyY+=38;if(y>=108&&y+35<=height-31)button(width-rightWidth,y+12,rightWidth-8,"Reset node position","Node-Position zurücksetzen",()->{session.edit(r->r.getAsJsonObject("nodes").remove(selected));rebuild();});}
    private void buildPreview(){
        if(minecraft==null||mode!=Mode.LAYOUT)return;
        EditorClient.simulation(simulation);EditorClient.preview(session.current());try{previewMenu=new PremiumSkillScreen(true);int[] v=VIEWPORTS[viewportIndex];previewMenu.init(minecraft,v[0],v[1]);previewMenu.editorPage(page);previewScreen=previewMenu.editorPreviewDetail(previewState);}finally{EditorClient.preview(null);}
    }
    private List<String> entries(){List<String> values;
        if(mode==Mode.LAYOUT){var ids=new ArrayList<>(EditorDefaults.ELEMENTS);for(var w:session.current().decorations())ids.add("decoration/"+w.id());values=ids;}
        else if(mode==Mode.SETTINGS)values=List.of("settings");else {var out=new ArrayList<String>();for(var e:root().getAsJsonArray(collection()))out.add(e.getAsJsonObject().get("id").getAsString());values=out;}
        return values.stream().filter(s->s.toLowerCase(Locale.ROOT).contains(filter.toLowerCase(Locale.ROOT))||entryName(s).toLowerCase(Locale.ROOT).contains(filter.toLowerCase(Locale.ROOT))).toList();
    }
    private String entryName(String id){if(mode==Mode.LAYOUT||mode==Mode.SETTINGS)return id;for(var e:root().getAsJsonArray(collection())){var o=e.getAsJsonObject();if(o.get("id").getAsString().equals(id))return EditorCodec.JSON.fromJson(o.get("title"),Text.class).value(EditorClient.german());}return id;}
    @Override public void render(GuiGraphics g,int mx,int my,float dt){
        g.fill(0,0,width,height,0xFF07111B);g.drawString(font,tr("FOREVER / K-MENU STUDIO","FOREVER / K-MENÜ STUDIO"),9,11,MenuArt.WHITE);
        MenuArt.label(g,Component.literal("r"+opened.revision()+(session.dirty()?" · *":"")),Math.min(230,width/2),11,Math.max(1,width/2-80),MenuArt.CYAN,.8F);
        MenuArt.panel(g,5,78,leftWidth,height-106,0xFF0B1C2A,MenuArt.BORDER);MenuArt.panel(g,width-rightWidth-5,78,rightWidth+1,height-106,0xFF0B1C2A,MenuArt.BORDER);
        MenuArt.panel(g,canvasLeft,128,Math.max(1,canvasRight-canvasLeft),Math.max(1,canvasBottom-128),0xFF081723,MenuArt.BORDER);
        drawEntries(g,mx,my);
        g.enableScissor(canvasLeft,128,canvasRight,canvasBottom);
        if(mode==Mode.LAYOUT)drawPreview(g,mx,my,dt);else if(mode==Mode.GRAPH||mode==Mode.SHOP)drawGraph(g,mx,my);else drawContent(g);
        g.disableScissor();
        for(var label:fieldLabels)MenuArt.label(g,Component.literal(label.label()),label.x(),label.y(),rightWidth-10,MenuArt.MUTED,.7F);
        super.render(g,mx,my,dt);
        String text=!message.isEmpty()?message:EditorClient.status();MenuArt.label(g,Component.literal(text),9,height-19,width-18,text.toLowerCase(Locale.ROOT).contains("invalid")?0xFFFF9898:MenuArt.MUTED,.75F);
        if(!linkSource.isEmpty())MenuArt.label(g,tr("Choose the dependent node","Abhängigen Node auswählen"),canvasLeft+4,canvasBottom-14,canvasRight-canvasLeft-8,MenuArt.GOLD,.65F);
    }
    private void drawEntries(GuiGraphics g,int mx,int my){var entries=entries();int rows=Math.max(1,(height-145)/25);listScroll=Math.clamp(listScroll,0,Math.max(0,entries.size()-rows));
        for(int i=0;i<rows&&i+listScroll<entries.size();i++){String id=entries.get(i+listScroll);int y=109+i*25;MenuArt.panel(g,8,y,leftWidth-8,23,id.equals(selected)?0xFF1B4053:my>=y&&my<y+23?0xFF142D3E:0xFF0C2030,id.equals(selected)?MenuArt.CYAN:MenuArt.BORDER);MenuArt.label(g,Component.literal(entryName(id)),12,y+7,leftWidth-17,id.equals(selected)?MenuArt.WHITE:MenuArt.MUTED,.75F);}
    }
    private void drawPreview(GuiGraphics g,int mx,int my,float dt){if(previewScreen==null)return;int[] v=VIEWPORTS[viewportIndex];previewScale=Math.min((canvasRight-canvasLeft-12)/(float)v[0],(canvasBottom-138)/(float)v[1]);previewScale=Math.max(.05F,previewScale);previewX=canvasLeft+(canvasRight-canvasLeft-v[0]*previewScale)/2;previewY=134+(canvasBottom-134-v[1]*previewScale)/2;
        EditorClient.preview(gesture==null?session.current():gesture);try {g.pose().pushPose();g.pose().translate(previewX,previewY,0);g.pose().scale(previewScale,previewScale,1);previewScreen.render(g,tryMode?(int)((mx-previewX)/previewScale):-100,tryMode?(int)((my-previewY)/previewScale):-100,dt);g.flush();g.pose().popPose();previewBounds=EditorClient.bounds();}finally{EditorClient.preview(null);}
        var r=previewBounds.get(selected);if(r!=null&&!tryMode){int x=(int)(previewX+r.x()*previewScale),y=(int)(previewY+r.y()*previewScale),w=(int)(r.width()*previewScale),h=(int)(r.height()*previewScale);outline(g,x,y,w,h,MenuArt.CYAN);g.fill(x+w-3,y+h-3,x+w+3,y+h+3,MenuArt.GOLD);}
    }
    private void outline(GuiGraphics g,int x,int y,int w,int h,int color){g.fill(x,y,x+w,y+1,color);g.fill(x,y+h-1,x+w,y+h,color);g.fill(x,y,x+1,y+h,color);g.fill(x+w-1,y,x+w,y+h,color);}
    private Node node(String id,int index){return session.current().nodes().getOrDefault(id,new Node(60+(index%3)*140,45+(index/3)*115,18));}
    private List<String> graphIds(){return mode==Mode.SHOP?session.current().products().stream().filter(p->p.enabled()).map(Product::id).toList():session.current().quests().stream().map(Quest::id).toList();}
    private void drawGraph(GuiGraphics g,int mx,int my){
        int step=Math.max(8,(int)(session.current().settings().grid()*zoom*4));for(int x=canvasLeft+(int)(panX%step);x<canvasRight;x+=step)for(int y=130+(int)(panY%step);y<canvasBottom;y+=step)g.fill(x,y,x+1,y+1,0xFF234052);
        var ids=graphIds();
        if(mode==Mode.GRAPH)for(int i=0;i<ids.size();i++){var q=session.current().quest(ids.get(i));var b=node(q.id(),i);for(String dep:q.requires()){int j=ids.indexOf(dep);if(j<0)continue;var a=node(dep,j);MenuArt.line(g,gx(a.x()),gy(a.y()),gx(b.x()),gy(b.y()),q.prerequisiteMatch()==Match.ANY?MenuArt.GOLD:0xFF4D95A1);}}
        if(mode==Mode.SHOP){var qids=session.current().quests().stream().filter(q->session.current().products().stream().anyMatch(p->p.requires().contains(q.id()))).map(Quest::id).toList();int i=0;for(String id:qids){int x=gx(-100),y=gy(30+i++*70);MenuArt.circle(g,x,y,12,MenuArt.GOLD);MenuArt.centered(g,Component.literal(session.current().quest(id).title().value(EditorClient.german())),x-52,y+17,104,18,MenuArt.GOLD,.6F);for(int j=0;j<ids.size();j++)if(session.current().product(ids.get(j)).requires().contains(id)){var b=node(ids.get(j),j);MenuArt.line(g,x,y,gx(b.x()),gy(b.y()),MenuArt.GOLD);}}}
        for(int i=0;i<ids.size();i++){String id=ids.get(i);var n=node(id,i);if(id.equals(dragging)&&gesture!=null)n=gesture.nodes().getOrDefault(id,n);int x=gx(n.x()),y=gy(n.y()),r=Math.max(6,(int)(n.radius()*zoom));
            boolean enabled=mode==Mode.SHOP?session.current().product(id).enabled():session.current().quest(id).enabled();int color=id.equals(selected)?MenuArt.CYAN:!enabled?MenuArt.DIM:MenuArt.BORDER;
            MenuArt.circle(g,x,y,r+2,color);MenuArt.circle(g,x,y,r,0xFF123044);
            String icon=mode==Mode.SHOP?session.current().product(id).icon():session.current().quest(id).icon();var item=BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(icon)).orElse(Items.BARRIER);
            g.pose().pushPose();g.pose().translate(x-8*zoom,y-8*zoom,0);g.pose().scale(zoom,zoom,1);g.renderItem(item.getDefaultInstance(),0,0);g.pose().popPose();
            String name=mode==Mode.SHOP?session.current().product(id).title().value(EditorClient.german()):session.current().quest(id).title().value(EditorClient.german());MenuArt.centered(g,Component.literal(name),x-61,y+r+7,122,22,MenuArt.WHITE,.7F);
        }
    }
    private int gx(float x){return (int)(canvasLeft+panX+x*zoom);}private int gy(float y){return (int)(134+panY+y*zoom);}
    private void drawContent(GuiGraphics g){var o=target(root());if(o==null)return;int x=canvasLeft+12,y=145,w=canvasRight-canvasLeft-24;
        if(o.has("icon"))g.renderItem(BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(o.get("icon").getAsString())).orElse(Items.BARRIER).getDefaultInstance(),x,y);
        if(o.has("title"))MenuArt.wrapped(g,Component.literal(EditorCodec.JSON.fromJson(o.get("title"),Text.class).value(EditorClient.german())),x+25,y,w-25,MenuArt.WHITE,.9F,3);
        y+=36;if(o.has("description"))y=MenuArt.wrapped(g,Component.literal(EditorCodec.JSON.fromJson(o.get("description"),Text.class).value(EditorClient.german())),x,y,w,MenuArt.MUTED,.8F,10)+15;
        if(mode==Mode.QUESTS){var q=session.current().quest(selected);for(var t:q.tasks()){MenuArt.label(g,Component.literal(t.type()+" · "+t.target()+" × "+t.count()),x,y,w,MenuArt.CYAN,.65F);y+=16;}MenuArt.label(g,Component.literal("+"+q.rewardSp()+" SP / +"+q.rewardPp()+" PP"),x,y+8,w,MenuArt.GOLD,.9F);}
        else if(mode==Mode.SETTINGS)MenuArt.wrapped(g,tr("Layout changes stay in your draft until you publish. Export writes one portable ZIP or the selected local repository.","Layoutänderungen bleiben bis zur Veröffentlichung im Entwurf. Der Export erzeugt eine ZIP oder schreibt in die gewählte lokale Repo."),x,y,w,MenuArt.MUTED,.8F,20);
        else if(mode==Mode.CONTRACTS)MenuArt.label(g,tr("Resource turn-in consumes items on the server.","Ressourcenabgabe verbraucht Items auf dem Server."),x,y,w,MenuArt.GOLD,.7F);
    }
    @Override public boolean mouseClicked(double mx,double my,int button){
        if(mx<leftWidth+1&&my>=109&&my<canvasBottom){var list=entries();int i=listScroll+(int)((my-109)/25);if(i<list.size()){run(()->{applyPending();selected=list.get(i);propertyScroll=0;pending.clear();rebuild();});return true;}}
        if(mx>=canvasLeft&&mx<canvasRight&&my>=130&&my<canvasBottom){
            if(mode==Mode.LAYOUT){if(tryMode){EditorClient.preview(session.current());try{boolean handled=previewScreen.mouseClicked((mx-previewX)/previewScale,(my-previewY)/previewScale,button);previewScreen=previewMenu.editorActivePreview();return handled;}finally{EditorClient.preview(null);}}
                var entries=new ArrayList<>(previewBounds.entrySet());Collections.reverse(entries);String hit="";for(var e:entries)if(e.getValue().contains((mx-previewX)/previewScale,(my-previewY)/previewScale)){hit=e.getKey();break;}
                if(!hit.isEmpty()){selected=hit;pending.clear();propertyScroll=0;beginGesture(mx,my);var o=target(root());startX=o.get("x").getAsFloat();startY=o.get("y").getAsFloat();startW=o.get("width").getAsInt()==0?previewBounds.get(hit).width():o.get("width").getAsInt();startH=o.get("height").getAsInt()==0?previewBounds.get(hit).height():o.get("height").getAsInt();rebuild();return true;}
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
            gesture=EditorCodec.decode(json.toString());if(mode==Mode.LAYOUT){EditorClient.preview(gesture);try{previewMenu.init(minecraft,VIEWPORTS[viewportIndex][0],VIEWPORTS[viewportIndex][1]);previewMenu.editorPage(page);previewScreen=previewMenu.editorPreviewDetail(previewState);}finally{EditorClient.preview(null);}}
        });return true;}
        if(panning){panX+=dx;panY+=dy;return true;}return super.mouseDragged(mx,my,button,dx,dy);
    }
    @Override public boolean mouseReleased(double x,double y,int b){if(!dragging.isEmpty()){if(gesture!=null)session.replace(gesture);dragging="";gesture=null;rebuild();return true;}panning=false;return super.mouseReleased(x,y,b);}
    @Override public boolean mouseScrolled(double x,double y,double h,double v){if(x>width-rightWidth-8){propertyScroll=Math.max(0,propertyScroll-(int)(v*42));rebuild();return true;}if(x<leftWidth+3){listScroll=Math.max(0,listScroll-(int)(v*3));return true;}if(mode==Mode.GRAPH||mode==Mode.SHOP){float next=Math.clamp((float)(zoom*Math.pow(1.12,v)),.35F,2);panX=(float)(x-canvasLeft-(x-canvasLeft-panX)*next/zoom);panY=(float)(y-134-(y-134-panY)*next/zoom);zoom=next;return true;}if(mode==Mode.LAYOUT&&tryMode){EditorClient.preview(session.current());try{return previewScreen.mouseScrolled((x-previewX)/previewScale,(y-previewY)/previewScale,h,v);}finally{EditorClient.preview(null);}}return super.mouseScrolled(x,y,h,v);}
    @Override public boolean keyPressed(int key,int scan,int modifiers){if(hasControlDown()&&key==GLFW.GLFW_KEY_S){saveDraft();return true;}if(hasControlDown()&&key==GLFW.GLFW_KEY_Z){pending.clear();if(hasShiftDown())session.redo();else session.undo();rebuild();return true;}if(key==GLFW.GLFW_KEY_ENTER){run(()->{applyPending();rebuild();});return true;}return super.keyPressed(key,scan,modifiers);}
    private void connect(String source,String destination,boolean remove){
        if(source.equals(destination))return;
        if(mode==Mode.SHOP){message=tr("Shop prerequisites are selected from quests in the inspector.","Shop-Voraussetzungen werden im Eigenschaftenfenster aus Quests gewählt.").getString();chooseQuestDependency(destination,remove);return;}
        session.edit(r->{var q=find(r.getAsJsonArray("quests"),destination);var a=q.getAsJsonArray("requires");var values=new LinkedHashSet<String>();for(var e:a)values.add(e.getAsString());if(remove)values.remove(source);else values.add(source);q.add("requires",EditorCodec.JSON.toJsonTree(values));});rebuild();
    }
    private void chooseQuestDependency(String id,boolean remove){
        var quests=session.current().quests();minecraft.setScreen(new Screen(tr("Quest prerequisite","Quest-Voraussetzung")){
            int offset;
            @Override protected void init(){int row=0;for(int i=offset;i<quests.size()&&row<(height-65)/24;i++){var q=quests.get(i);addRenderableWidget(Button.builder(Component.literal(q.title().value(EditorClient.german())),b->{session.edit(r->{var p=find(r.getAsJsonArray("products"),id);var a=p.getAsJsonArray("requires");var values=new LinkedHashSet<String>();for(var v:a)values.add(v.getAsString());if(remove)values.remove(q.id());else values.add(q.id());p.add("requires",EditorCodec.JSON.toJsonTree(values));});minecraft.setScreen(KMenuEditorScreen.this);}).bounds(25,30+row++*24,width-50,20).build());}}
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
        else return;selected=id;propertyScroll=0;pending.clear();rebuild();
    }
    private void duplicate(){applyPending();if(mode==Mode.LAYOUT||mode==Mode.SETTINGS||mode==Mode.CHAPTERS){addEntry();return;}String old=selected;String prefix=mode==Mode.SHOP?"forever_skyblock:shop/custom/":mode==Mode.CONTRACTS?"forever_skyblock:contract/custom/":"forever_skyblock:quest/";String id=prefix+suffix();session.edit(r->{var q=find(r.getAsJsonArray(collection()),old).deepCopy();q.addProperty("id",id);if(q.has("tasks")&&q.getAsJsonArray("tasks").get(0).getAsJsonObject().get("type").getAsString().equals("LEGACY")){q.add("tasks",EditorCodec.JSON.toJsonTree(List.of(new Task("objective_1",TaskType.CRAFT,"minecraft:cobblestone",1))));}r.getAsJsonArray(collection()).add(q);});selected=id;propertyScroll=0;pending.clear();rebuild();}
    private void disable(){applyPending();session.edit(r->{var obj=target(r);if(obj!=null&&obj.has("enabled"))obj.addProperty("enabled",!obj.get("enabled").getAsBoolean());});rebuild();}
    private void addTask(){applyPending();var q=session.current().quest(selected);if(q==null||q.legacy()){message=tr("Duplicate a built-in quest to use custom objectives.","Dupliziere eine bestehende Quest für eigene Aufgaben.").getString();return;}session.edit(r->find(r.getAsJsonArray("quests"),selected).getAsJsonArray("tasks").add(EditorCodec.JSON.toJsonTree(new Task("objective_"+suffix(),TaskType.CRAFT,"minecraft:cobblestone",1))));rebuild();}
    private void addStack(){applyPending();session.edit(r->find(r.getAsJsonArray("products"),selected).getAsJsonArray("contents").add(EditorCodec.JSON.toJsonTree(new EditorDocument.Stack("minecraft:cobblestone",1))));rebuild();}
    private void addDecoration(){applyPending();String id="text_"+suffix();var w=new Decoration(id,page,DecorationType.TEXT,new Text("New text","Neuer Text"),"minecraft:paper","",20,20,120,16,"FF51D7ED");session.edit(r->r.getAsJsonArray("decorations").add(EditorCodec.JSON.toJsonTree(w)));selected="decoration/"+id;propertyScroll=0;rebuild();}
    private void arrange(){applyPending();var ids=graphIds();session.edit(r->{var map=r.getAsJsonObject("nodes");for(int i=0;i<ids.size();i++){String id=ids.get(i);int depth=mode==Mode.GRAPH?depth(id,new HashSet<>()):i/3;map.add(id,EditorCodec.JSON.toJsonTree(new Node(60+(i%3)*140,45+depth*115,18)));}});rebuild();}
    private int depth(String id,Set<String> stack){if(!stack.add(id))return 0;var q=session.current().quest(id);int result=0;if(q!=null)for(String dep:q.requires())result=Math.max(result,1+depth(dep,new HashSet<>(stack)));return result;}
    private void saveDraft(){run(()->{applyPending();EditorStore.atomic(EditorClient.directory().resolve("draft.json"),EditorCodec.encode(session.current()));lastDraft=System.nanoTime();message=tr("Draft saved","Entwurf gespeichert").getString();});}
    private void loadDraft(){run(()->{var d=EditorStore.read(EditorClient.directory().resolve("draft.json"));session.replace(d.withRevision(opened.revision()));pending.clear();rebuild();message=tr("Draft loaded; compare before publishing.","Entwurf geladen; vor Veröffentlichung vergleichen.").getString();});}
    private void chooseImport(){pathDialog(tr("Import JSON / exported ZIP","JSON / exportierte ZIP importieren"),"",path->{var d=path.toString().toLowerCase(Locale.ROOT).endsWith(".zip")?EditorStore.importZip(path):EditorStore.read(path);session.replace(d.withRevision(opened.revision()));pending.clear();minecraft.setScreen(this);});}
    @FunctionalInterface private interface PathAction{void apply(Path path)throws Exception;}
    private void pathDialog(Component heading,String initial,PathAction action){minecraft.setScreen(new Screen(heading){EditBox path;String error="";
        @Override protected void init(){path=addRenderableWidget(new EditBox(font,22,height/2-12,width-44,20,title));path.setMaxLength(2048);path.setValue(initial);addRenderableWidget(Button.builder(tr("Continue","Weiter"),b->{try{action.apply(Path.of(path.getValue()));}catch(Exception e){error=e.getMessage();}}).bounds(width-170,height/2+22,70,20).build());addRenderableWidget(Button.builder(tr("Cancel","Abbrechen"),b->onClose()).bounds(width-94,height/2+22,70,20).build());setInitialFocus(path);}
        @Override public void render(GuiGraphics g,int x,int y,float dt){g.fill(0,0,width,height,0xFF07111B);g.drawString(font,title,22,height/2-35,MenuArt.WHITE);MenuArt.label(g,Component.literal(error),22,height/2+54,width-44,0xFFFFA0A0,.8F);super.render(g,x,y,dt);}
        @Override public void onClose(){minecraft.setScreen(KMenuEditorScreen.this);}
    });}
    private void export(){applyPending();confirm(tr("Export directly to a local Git repository? Choose No for a portable ZIP.","Direkt in eine lokale Git-Repo exportieren? Nein erzeugt eine portable ZIP."),()->pathDialog(tr("ForeverSkyblock repository folder","ForeverSkyblock-Repositoryordner"),"",path->{EditorStore.exportRepository(session.current(),path);message="Exported: "+path; minecraft.setScreen(this);}),()->run(()->{Path zip=EditorClient.directory().resolve("exports/kmenu-r"+opened.revision()+"-"+System.currentTimeMillis()+".zip");EditorStore.exportZip(session.current(),zip);message="Exported: "+zip;minecraft.setScreen(this);}));}
    private void confirmPublish(){applyPending();EditorCodec.validateUpdate(opened,session.current());confirm(tr("Publish this draft for every player on this server? Existing completed quests keep their rewards.","Diesen Entwurf für alle Spieler auf diesem Server aktivieren? Abgeschlossene Quests behalten ihre Belohnungen."),()->{saveDraft();EditorClient.publish(session.current());});}
    private void confirm(Component text,Runnable yes){confirm(text,yes,()->minecraft.setScreen(this));}
    private void confirm(Component text,Runnable yes,Runnable no){minecraft.setScreen(new ConfirmScreen(ok->{minecraft.setScreen(this);run(ok?yes:no);},tr("K-menu editor","K-Menü-Editor"),text));}
    public void serverReply(EditorNetwork.Reply r){if(r.kind()==2){message=r.message();if(message.startsWith("Published")){pending.clear();opened=EditorClient.document();session=new EditorSession(opened);rebuild();}}else if(r.kind()==0&&r.revision()!=opened.revision())message=tr("Server revision changed. Your draft is preserved; reopen to reconcile.","Serverversion geändert. Dein Entwurf bleibt erhalten; zum Abgleichen neu öffnen.").getString();}
    @Override public void onFilesDrop(List<Path> paths){if(paths.size()!=1){message="Drop one JSON or export ZIP";return;}run(()->{Path p=paths.getFirst();var d=p.toString().endsWith(".zip")?EditorStore.importZip(p):EditorStore.read(p);session.replace(d.withRevision(opened.revision()));pending.clear();rebuild();});}
    @Override public void onClose(){if(session.dirty()||!pending.isEmpty())confirm(tr("Save your draft before closing?","Entwurf vor dem Schließen speichern?"),()->{saveDraft();minecraft.setScreen(parent);},()->minecraft.setScreen(parent));else minecraft.setScreen(parent);}
    @Override public boolean isPauseScreen(){return false;}
}

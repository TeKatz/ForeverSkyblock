package de.tekatz.foreverskyblock.mixin;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import de.tekatz.foreverskyblock.client.*;
import de.tekatz.foreverskyblock.editor.*;
import de.tekatz.foreverskyblock.progression.ShopCatalog;
import java.lang.reflect.Field;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import org.objectweb.asm.Opcodes;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.*;
import org.spongepowered.asm.mixin.injection.callback.*;

/** Player-facing usability layer for the Studio workspace. */
@Mixin(KMenuEditorScreen.class)
public abstract class KMenuEditorUxMixin extends Screen {
    @Shadow private int leftWidth;
    @Shadow private int rightWidth;
    @Shadow private int propertyY;
    @Shadow private int propertyScroll;
    @Shadow private int canvasLeft;
    @Shadow private int canvasRight;
    @Shadow private String selected;
    @Shadow private String variant;
    @Shadow private String page;
    @Shadow private String message;
    @Shadow private boolean focusMode;
    @Shadow private float panX;
    @Shadow private Map<String,String> pending;
    @Shadow private EditorSession session;
    @Shadow private EditorDocument opened;

    @Unique private boolean foreverSkyblock$sectionSelectorPlaced;
    @Unique private String foreverSkyblock$activeSection="";

    protected KMenuEditorUxMixin(){super(Component.empty());}

    @Redirect(method="init",at=@At(value="FIELD",target="Lde/tekatz/foreverskyblock/client/KMenuEditorScreen;leftWidth:I",opcode=Opcodes.PUTFIELD))
    private void foreverSkyblock$narrowElementRail(KMenuEditorScreen instance,int value){leftWidth=value==0?0:Math.min(value,width>=900?118:108);}

    @Redirect(method="init",at=@At(value="FIELD",target="Lde/tekatz/foreverskyblock/client/KMenuEditorScreen;rightWidth:I",opcode=Opcodes.PUTFIELD))
    private void foreverSkyblock$narrowInspector(KMenuEditorScreen instance,int value){rightWidth=value==0?0:Math.min(value,width>=900?196:184);}

    @Inject(method="init",at=@At("HEAD"))
    private void foreverSkyblock$prepareWorkspace(CallbackInfo ci){
        foreverSkyblock$sectionSelectorPlaced=false;
        if("OVERVIEW".equals(page))page="RESEARCH";
    }

    @Inject(method="init",at=@At("TAIL"))
    private void foreverSkyblock$finishWorkspace(CallbackInfo ci){
        foreverSkyblock$shortenButtons();
        boolean hasFocus=children().stream().anyMatch(listener->listener instanceof Button button&&switch(button.getMessage().getString()){
            case "Focus","Fokus","Panels"->true;default->false;
        });
        if(foreverSkyblock$isLayoutSelection()&&!hasFocus){
            addRenderableWidget(Button.builder(Component.literal(focusMode?"Panels":(EditorClient.german()?"Fokus":"Focus")),b->{focusMode=!focusMode;minecraft.setScreen((Screen)(Object)this);}).bounds(368,54,54,18).build());
        }
        foreverSkyblock$disableInvalidShopActions();
    }

    @Unique private boolean foreverSkyblock$isLayoutSelection(){return selected!=null&&(EditorDefaults.ELEMENTS.contains(selected)||selected.startsWith("decoration/"));}

    @Unique private void foreverSkyblock$shortenButtons(){
        for(var listener:children())if(listener instanceof Button button){
            String original=button.getMessage().getString(),text=original;
            text=foreverSkyblock$strip(text,"Page: ","Seite: ","Viewport: ","Ansicht: ","Interaction: ","Interaktion: ","Edit: ","Bearbeiten: ","Detail: ","Details: ","State: ","Status: ");
            text=switch(text){
                case "Zurück"->"Undo";case "Vor"->"Redo";
                case "All sizes"->"All";case "Alle Größen"->"Alle";case "This size"->"This";case "Nur diese Größe"->"Diese";
                case "Focus preview"->"Focus";case "Vorschau fokussieren"->"Fokus";case "Show panels"->"Panels";case "Panels zeigen"->"Panels";
                case "Apply changes [Enter]"->"Apply";case "Änderungen übernehmen [Enter]"->"Übernehmen";
                case "Remove / disable selected"->"Delete";case "Auswahl entfernen / deaktiv."->"Löschen";
                case "Requirements"->"Requires";case "Voraussetzungen"->"Requires";case "Auto arrange"->"Arrange";case "Auto-Ordnen"->"Ordnen";
                case "Center view"->"Center";case "Ansicht zentrieren"->"Zentrieren";default->text;};
            int limit=Math.max(8,button.getWidth()-8);String clipped=text;
            while(clipped.length()>1&&minecraft.font.width(clipped)>limit)clipped=clipped.substring(0,clipped.length()-1);
            if(!clipped.equals(text)){while(clipped.length()>1&&minecraft.font.width(clipped+"…")>limit)clipped=clipped.substring(0,clipped.length()-1);clipped+="…";}
            if(!clipped.equals(original))button.setMessage(Component.literal(clipped));
        }
    }
    @Unique private static String foreverSkyblock$strip(String value,String...prefixes){for(String prefix:prefixes)if(value.startsWith(prefix))return value.substring(prefix.length());return value;}

    /** One fixed dropdown replaces a stack of inspector section bars. */
    @Inject(method="section",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$sectionDropdown(String id,String english,String german,CallbackInfoReturnable<Boolean> cir){
        var options=foreverSkyblock$sections();
        if(options.stream().noneMatch(o->o.id().equals(foreverSkyblock$activeSection)))foreverSkyblock$activeSection=options.isEmpty()?id:options.getFirst().id();
        if(!foreverSkyblock$sectionSelectorPlaced){
            foreverSkyblock$sectionSelectorPlaced=true;propertyY=137-propertyScroll;
            if(rightWidth>0){
                var active=options.stream().filter(o->o.id().equals(foreverSkyblock$activeSection)).findFirst().orElse(new EditorSectionDropdown.Option(id,english,german));
                String label=(EditorClient.german()?active.german():active.english())+" ▼";Screen editor=(Screen)(Object)this;
                addRenderableWidget(Button.builder(Component.literal(label),b->minecraft.setScreen(new EditorSectionDropdown(editor,options,foreverSkyblock$activeSection,next->{foreverSkyblock$activeSection=next;propertyScroll=0;}))).bounds(width-rightWidth,112,rightWidth-8,20).build());
            }
        }
        boolean open=id.equals(foreverSkyblock$activeSection);
        if(open&&id.equals("layout.appearance"))foreverSkyblock$appearanceActions();
        cir.setReturnValue(open);
    }

    @Unique private List<EditorSectionDropdown.Option> foreverSkyblock$sections(){
        String mode=foreverSkyblock$mode();var out=new ArrayList<EditorSectionDropdown.Option>();
        if(mode.equals("LAYOUT")){
            if(selected!=null&&selected.startsWith("decoration/")){out.add(new EditorSectionDropdown.Option("decoration.behavior","Content & behavior","Inhalt & Verhalten"));out.add(new EditorSectionDropdown.Option("decoration.geometry","Geometry","Geometrie"));}
            else {out.add(new EditorSectionDropdown.Option("layout.position","Position & size","Position & Größe"));out.add(new EditorSectionDropdown.Option("layout.anchors","Anchors","Verankerung"));out.add(new EditorSectionDropdown.Option("layout.appearance","Appearance & effects","Darstellung & Effekte"));out.add(new EditorSectionDropdown.Option("layout.alignment","Alignment","Ausrichtung"));}
        }else if(mode.equals("SETTINGS")){out.add(new EditorSectionDropdown.Option("settings.server","Server & recovery","Server & Wiederherstellung"));out.add(new EditorSectionDropdown.Option("settings.editor","Editor behavior","Editor-Verhalten"));}
        else {
            out.add(new EditorSectionDropdown.Option("content.general","Content","Inhalt"));
            if(mode.equals("QUESTS")||mode.equals("GRAPH")){out.add(new EditorSectionDropdown.Option("quest.progression","Progression & rewards","Progression & Belohnung"));out.add(new EditorSectionDropdown.Option("quest.objectives","Objectives","Aufgaben"));out.add(new EditorSectionDropdown.Option("quest.dependencies","Dependencies & node","Abhängigkeiten & Node"));}
            else if(mode.equals("SHOP")){out.add(new EditorSectionDropdown.Option("shop.rules","Price & requirements","Preis & Anforderungen"));out.add(new EditorSectionDropdown.Option("shop.contents","Package contents","Paketinhalt"));out.add(new EditorSectionDropdown.Option("shop.node","Node position","Node-Position"));}
            else if(mode.equals("CONTRACTS"))out.add(new EditorSectionDropdown.Option("contract.rules","Contract rules","Vertragsregeln"));
            else if(mode.equals("CHAPTERS"))out.add(new EditorSectionDropdown.Option("chapter.order","Chapter order","Kapitelreihenfolge"));
        }
        return out;
    }

    @Unique private void foreverSkyblock$appearanceActions(){
        if(rightWidth<=0)return;Screen editor=(Screen)(Object)this;int y=propertyY;propertyY+=24;
        if(y>=137&&y+20<=height-31)addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Effekte / Shader…":"Effects / shaders…"),b->{
            var style=foreverSkyblock$style();minecraft.setScreen(new EditorStyleEffectScreen(editor,style,result->{foreverSkyblock$updateEffects(result);minecraft.setScreen(editor);}));
        }).bounds(width-rightWidth,y,rightWidth-8,20).build());
        if(foreverSkyblock$isHeaderLabel()){
            y=propertyY;propertyY+=24;if(y>=137&&y+20<=height-31){var style=foreverSkyblock$style();String en=style.labelEn(),de=style.labelDe();
                addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Text bearbeiten…":"Edit text…"),b->minecraft.setScreen(new EditorHeaderTextScreen(editor,en,de,(nextEn,nextDe)->foreverSkyblock$updateLabel(nextEn,nextDe)))).bounds(width-rightWidth,y,rightWidth-8,20).build());}
        }else if(foreverSkyblock$isHeaderIcon()){
            y=propertyY;propertyY+=24;if(y>=137&&y+20<=height-31)addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Icon auswählen…":"Choose icon…"),b->minecraft.setScreen(new EditorHeaderIconPicker(editor,this::foreverSkyblock$updateIcon))).bounds(width-rightWidth,y,rightWidth-8,20).build());
        }
    }

    @Inject(method="field",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$hideIrrelevantHeaderFields(JsonObject obj,String path,String label,CallbackInfo ci){
        if(!foreverSkyblock$isHeaderLeaf())return;
        if(path.equals("fill")||path.equals("border")||foreverSkyblock$isHeaderIcon()&&(path.equals("fontScale")||path.equals("text")))ci.cancel();
    }

    @Inject(method="field",at=@At("RETURN"))
    private void foreverSkyblock$addColorPicker(JsonObject obj,String path,String label,CallbackInfo ci){
        if(!(path.equals("fill")||path.equals("border")||path.equals("text")||path.equals("color")||path.equals("effectColor")))return;
        int y=propertyY-35;if(y<137||y+32>height-31||rightWidth<=0)return;
        String current=pending.getOrDefault(path,foreverSkyblock$valueAt(obj,path));Screen editor=(Screen)(Object)this;
        addRenderableWidget(Button.builder(Component.literal("#"),b->minecraft.setScreen(new EditorColorPicker(editor,current,value->pending.put(path,value)))).bounds(width-28,y+12,20,18).build());
    }

    @Inject(method="entryName",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$friendlyElementNames(String id,CallbackInfoReturnable<String> cir){String name=foreverSkyblock$friendly(id);if(name!=null)cir.setReturnValue(name);}

    @Inject(method="entries",at=@At("RETURN"),cancellable=true)
    private void foreverSkyblock$cleanEntryRail(CallbackInfoReturnable<List<String>> cir){
        String mode=foreverSkyblock$mode();var entries=new ArrayList<>(cir.getReturnValue());
        if(mode.equals("SHOP")){
            entries.remove(ShopCatalog.TEST.id());entries.removeAll(GraphGeometry.SHOP_ROOTS);
            entries.removeIf(id->{var p=session.current().product(id);return p!=null&&!p.enabled()&&id.startsWith("forever_skyblock:shop/custom/");});
        }else if(mode.equals("QUESTS")||mode.equals("GRAPH"))entries.removeIf(id->{var q=session.current().quest(id);return q!=null&&!q.enabled()&&id.startsWith("forever_skyblock:quest/");});
        else if(mode.equals("CONTRACTS"))entries.removeIf(id->{var c=session.current().contract(id);return c!=null&&!c.enabled()&&id.startsWith("forever_skyblock:contract/custom/");});
        cir.setReturnValue(entries);
    }

    @Inject(method="removeSelected",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$deleteCustomEntry(CallbackInfo ci){
        String mode=foreverSkyblock$mode();
        if((mode.equals("QUESTS")||mode.equals("GRAPH"))&&selected.startsWith("forever_skyblock:quest/")){
            boolean published=opened.quest(selected)!=null;String id=selected;session.edit(root->foreverSkyblock$removeQuest(root,id,published));foreverSkyblock$afterDelete();ci.cancel();return;
        }
        if(mode.equals("SHOP")&&(selected.equals(ShopCatalog.TEST.id())||selected.startsWith("forever_skyblock:shop/custom/"))){
            boolean published=opened.product(selected)!=null;String id=selected;session.edit(root->foreverSkyblock$removeProduct(root,id,published||id.equals(ShopCatalog.TEST.id())));foreverSkyblock$afterDelete();ci.cancel();return;
        }
        if(mode.equals("CONTRACTS")&&selected.startsWith("forever_skyblock:contract/custom/")){
            boolean published=opened.contract(selected)!=null;String id=selected;session.edit(root->foreverSkyblock$removeArrayEntry(root,"contracts",id,published));foreverSkyblock$afterDelete();ci.cancel();
        }
    }

    @Unique private void foreverSkyblock$removeQuest(JsonObject root,String id,boolean tombstone){
        foreverSkyblock$removeArrayEntry(root,"quests",id,tombstone);root.getAsJsonObject("nodes").remove(id);root.getAsJsonObject("nodes").remove("shop:quest/"+id);
        for(var e:root.getAsJsonArray("quests"))foreverSkyblock$removeString(e.getAsJsonObject().getAsJsonArray("requires"),id);
        for(var e:root.getAsJsonArray("products"))foreverSkyblock$removeString(e.getAsJsonObject().getAsJsonArray("requires"),id);
    }
    @Unique private void foreverSkyblock$removeProduct(JsonObject root,String id,boolean tombstone){foreverSkyblock$removeArrayEntry(root,"products",id,tombstone);root.getAsJsonObject("nodes").remove(id);}
    @Unique private void foreverSkyblock$removeArrayEntry(JsonObject root,String collection,String id,boolean tombstone){
        JsonArray a=root.getAsJsonArray(collection);for(int i=0;i<a.size();i++){var obj=a.get(i).getAsJsonObject();if(!obj.get("id").getAsString().equals(id))continue;if(tombstone)obj.addProperty("enabled",false);else a.remove(i);break;}
    }
    @Unique private static void foreverSkyblock$removeString(JsonArray array,String value){for(int i=array.size()-1;i>=0;i--)if(array.get(i).getAsString().equals(value))array.remove(i);}
    @Unique private void foreverSkyblock$afterDelete(){
        pending.clear();propertyScroll=0;String mode=foreverSkyblock$mode();
        if(mode.equals("SHOP"))selected=session.current().products().stream().filter(p->p.enabled()&&!p.id().equals(ShopCatalog.TEST.id())).map(EditorDocument.Product::id).findFirst().orElse("shop:tier0");
        else if(mode.equals("CONTRACTS"))selected=session.current().contracts().stream().filter(EditorDocument.Contract::enabled).map(EditorDocument.Contract::id).findFirst().orElse("settings");
        else selected=session.current().quests().stream().filter(EditorDocument.Quest::enabled).map(EditorDocument.Quest::id).findFirst().orElse("settings");
        message=EditorClient.german()?"Gelöscht. Veröffentlichte Einträge bleiben intern als unsichtbarer Tombstone erhalten.":"Deleted. Published entries remain internally as an invisible tombstone.";
        minecraft.setScreen((Screen)(Object)this);
    }

    @Inject(method={"duplicate","disable","addStack"},at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$guardProductActions(CallbackInfo ci){
        if(foreverSkyblock$mode().equals("SHOP")&&session.current().product(selected)==null){message=EditorClient.german()?"Wähle zuerst ein Shop-Angebot aus.":"Select a Shop product first.";ci.cancel();}
    }
    @Unique private void foreverSkyblock$disableInvalidShopActions(){
        if(!foreverSkyblock$mode().equals("SHOP")||session.current().product(selected)!=null)return;
        for(var child:children())if(child instanceof Button b){String t=b.getMessage().getString();if(t.equals("Requires")||t.equals("Unlink")||t.equals("Duplicate")||t.equals("Duplizieren")||t.equals("Disable")||t.equals("Deaktiv.")||t.equals("+ Item"))b.active=false;}
    }

    @Inject(method="mouseScrolled",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$horizontalShopPan(double mx,double my,double horizontal,double vertical,CallbackInfoReturnable<Boolean> cir){
        if(foreverSkyblock$mode().equals("SHOP")&&Screen.hasShiftDown()&&mx>=canvasLeft&&mx<canvasRight){panX+=(float)((horizontal!=0?horizontal:vertical)*42D);cir.setReturnValue(true);}
    }

    @Unique private boolean foreverSkyblock$isHeaderLeaf(){return selected!=null&&selected.startsWith("header.")&&(selected.endsWith(".icon")||selected.endsWith(".label")||selected.endsWith(".value"));}
    @Unique private boolean foreverSkyblock$isHeaderIcon(){return selected!=null&&selected.endsWith(".icon")&&selected.startsWith("header.");}
    @Unique private boolean foreverSkyblock$isHeaderLabel(){return selected!=null&&selected.endsWith(".label")&&selected.startsWith("header.");}
    @Unique private String foreverSkyblock$styleKey(){return variant+selected;}
    @Unique private EditorDocument.Style foreverSkyblock$style(){return session.current().layout().getOrDefault(foreverSkyblock$styleKey(),EditorDocument.Style.defaults());}

    @Unique private void foreverSkyblock$updateLabel(String english,String german){
        String key=foreverSkyblock$styleKey();session.edit(root->{var layout=root.getAsJsonObject("layout");if(!layout.has(key))layout.add(key,EditorCodec.JSON.toJsonTree(EditorDocument.Style.defaults()));var style=layout.getAsJsonObject(key);style.addProperty("labelEn",english);style.addProperty("labelDe",german);});
    }
    @Unique private void foreverSkyblock$updateIcon(String icon){
        String key=foreverSkyblock$styleKey();session.edit(root->{var layout=root.getAsJsonObject("layout");if(!layout.has(key))layout.add(key,EditorCodec.JSON.toJsonTree(EditorDocument.Style.defaults()));layout.getAsJsonObject(key).addProperty("icon",icon);});
    }
    @Unique private void foreverSkyblock$updateEffects(EditorStyleEffectScreen.Result result){
        String key=foreverSkyblock$styleKey();session.edit(root->{var layout=root.getAsJsonObject("layout");if(!layout.has(key))layout.add(key,EditorCodec.JSON.toJsonTree(EditorDocument.Style.defaults()));var style=layout.getAsJsonObject(key);style.addProperty("effect",result.effect());style.addProperty("effectColor",result.color());style.addProperty("effectStrength",result.strength());style.addProperty("textShadow",result.shadow());});
    }
    @Unique private static String foreverSkyblock$valueAt(JsonObject obj,String path){JsonElement current=obj;for(String part:path.split("\\.")){if(current==null)return "";current=current.isJsonArray()?current.getAsJsonArray().get(Integer.parseInt(part)):current.getAsJsonObject().get(part);}return current==null||current.isJsonNull()?"":current.getAsString();}

    @Unique private String foreverSkyblock$mode(){
        try{Field f=KMenuEditorScreen.class.getDeclaredField("mode");f.setAccessible(true);Object value=f.get(this);return value==null?"":value.toString();}catch(ReflectiveOperationException e){return "";}
    }

    @Unique private static String foreverSkyblock$friendly(String id){
        boolean de=EditorClient.german();return switch(id){
            case "header.brand"->de?"Header / Logo":"Header / Brand";
            case "header.sp"->"Header / SP";case "header.sp.icon"->"SP / Icon";case "header.sp.label"->de?"SP / Text":"SP / Label";case "header.sp.value"->de?"SP / Wert":"SP / Value";
            case "header.pp"->"Header / PP";case "header.pp.icon"->"PP / Icon";case "header.pp.label"->de?"PP / Text":"PP / Label";case "header.pp.value"->de?"PP / Wert":"PP / Value";
            case "header.tier"->"Header / Tier";case "header.tier.icon"->"Tier / Icon";case "header.tier.label"->de?"Tier / Text":"Tier / Label";case "header.tier.value"->de?"Tier / Wert":"Tier / Value";
            case "header.stats"->de?"Header / Statistik":"Header / Stats";
            case "header.stats.playtime.icon"->de?"Spielzeit / Icon":"Playtime / Icon";case "header.stats.playtime.label"->de?"Spielzeit / Text":"Playtime / Label";case "header.stats.playtime.value"->de?"Spielzeit / Wert":"Playtime / Value";
            case "header.stats.island.icon"->"Island / Icon";case "header.stats.island.label"->de?"Island / Text":"Island / Label";case "header.stats.island.value"->de?"Island / Wert":"Island / Value";
            case "nav.overview"->de?"Tab / Übersicht (alt)":"Tab / Overview (legacy)";case "nav.challenges"->"Tab / Challenges";case "nav.shop"->"Tab / Shop";case "nav.research"->"Tab / Research";case "nav.perks"->"Tab / Perks";case "nav.profile"->"Tab / Profile";
            case "search"->de?"Challenges / Suche":"Challenges / Search";case "categories"->de?"Challenges / Kategorien":"Challenges / Categories";
            case "card"->de?"Challenge / Karte":"Challenge / Card";case "card.title"->de?"Karte / Titel":"Card / Title";case "card.description"->de?"Karte / Beschreibung":"Card / Description";case "card.icon"->"Card / Icon";case "card.reward"->de?"Karte / Belohnung":"Card / Reward";
            case "detail"->de?"Details / Fenster":"Detail / Window";case "detail.title"->de?"Details / Titel":"Detail / Title";case "detail.icon"->"Detail / Icon";case "detail.prose"->de?"Details / Text":"Detail / Text";
            case "profile.identity"->de?"Profil / Identität":"Profile / Identity";case "profile.stats"->de?"Profil / Statistik":"Profile / Stats";
            default->id.startsWith("body.")?(de?"Bereich / ":"Area / ")+id.substring(5):null;};
    }
}

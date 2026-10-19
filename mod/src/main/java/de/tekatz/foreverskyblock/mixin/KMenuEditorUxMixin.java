package de.tekatz.foreverskyblock.mixin;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import de.tekatz.foreverskyblock.client.*;
import de.tekatz.foreverskyblock.editor.*;
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
    @Shadow private String selected;
    @Shadow private String variant;
    @Shadow private boolean focusMode;
    @Shadow private Map<String,String> pending;
    @Shadow private EditorSession session;

    protected KMenuEditorUxMixin(){super(Component.empty());}

    /** Keep the preview as the dominant surface even at the common GUI-scale-2 logical width. */
    @Redirect(method="init",at=@At(value="FIELD",target="Lde/tekatz/foreverskyblock/client/KMenuEditorScreen;leftWidth:I",opcode=Opcodes.PUTFIELD))
    private void foreverSkyblock$narrowElementRail(KMenuEditorScreen instance,int value){leftWidth=value==0?0:Math.min(value,width>=900?118:108);}

    @Redirect(method="init",at=@At(value="FIELD",target="Lde/tekatz/foreverskyblock/client/KMenuEditorScreen;rightWidth:I",opcode=Opcodes.PUTFIELD))
    private void foreverSkyblock$narrowInspector(KMenuEditorScreen instance,int value){rightWidth=value==0?0:Math.min(value,width>=900?196:184);}

    /** The layout workspace always gets one obvious full-preview toggle, but never duplicates the native control. */
    @Inject(method="init",at=@At("TAIL"))
    private void foreverSkyblock$finishWorkspace(CallbackInfo ci){
        foreverSkyblock$shortenButtons();
        boolean hasFocus=children().stream().anyMatch(listener->listener instanceof Button button&&switch(button.getMessage().getString()){
            case "Focus","Fokus","Panels"->true;default->false;
        });
        if(foreverSkyblock$isLayoutSelection()&&!hasFocus){
            addRenderableWidget(Button.builder(Component.literal(focusMode?"Panels":(EditorClient.german()?"Fokus":"Focus")),b->{focusMode=!focusMode;minecraft.setScreen((Screen)(Object)this);}).bounds(368,54,54,18).build());
        }
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
                case "Apply changes [Enter]"->"Apply [Enter]";case "Änderungen übernehmen [Enter]"->"Übernehmen [Enter]";
                case "Remove / disable selected"->"Remove / disable";case "Auswahl entfernen / deaktiv."->"Entf. / deaktiv.";
                case "Requirements"->"Requires";case "Voraussetzungen"->"Voraussetz.";case "Auto arrange"->"Arrange";case "Auto-Ordnen"->"Ordnen";
                case "Center view"->"Center";case "Ansicht zentrieren"->"Zentrieren";default->text;};
            int limit=Math.max(8,button.getWidth()-8);String clipped=text;
            while(clipped.length()>1&&minecraft.font.width(clipped)>limit)clipped=clipped.substring(0,clipped.length()-1);
            if(!clipped.equals(text)){while(clipped.length()>1&&minecraft.font.width(clipped+"…")>limit)clipped=clipped.substring(0,clipped.length()-1);clipped+="…";}
            if(!clipped.equals(original))button.setMessage(Component.literal(clipped));
        }
    }
    @Unique private static String foreverSkyblock$strip(String value,String...prefixes){for(String prefix:prefixes)if(value.startsWith(prefix))return value.substring(prefix.length());return value;}

    /** Header leaf controls should not pretend to own a panel background. */
    @Inject(method="field",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$hideIrrelevantHeaderFields(JsonObject obj,String path,String label,CallbackInfo ci){
        if(!foreverSkyblock$isHeaderLeaf())return;
        if(path.equals("fill")||path.equals("border")||foreverSkyblock$isHeaderIcon()&&(path.equals("fontScale")||path.equals("text")))ci.cancel();
    }

    /** Color-valued fields retain exact hex entry, but now also have a visual picker next to them. */
    @Inject(method="field",at=@At("RETURN"))
    private void foreverSkyblock$addColorPicker(JsonObject obj,String path,String label,CallbackInfo ci){
        if(!(path.equals("fill")||path.equals("border")||path.equals("text")||path.equals("color")))return;
        int y=propertyY-35;if(y<108||y+32>height-31||rightWidth<=0)return;
        String current=pending.getOrDefault(path,foreverSkyblock$valueAt(obj,path));Screen editor=(Screen)(Object)this;
        addRenderableWidget(Button.builder(Component.literal("#"),b->minecraft.setScreen(new EditorColorPicker(editor,current,value->pending.put(path,value)))).bounds(width-28,y+12,20,18).build());
    }

    /** Label/icon leaves put their main editing action at the top of the inspector. */
    @Inject(method="inspector",at=@At("HEAD"))
    private void foreverSkyblock$headerLeafActions(CallbackInfo ci){
        if(rightWidth<=0||!foreverSkyblock$isHeaderLeaf())return;
        int y=propertyY;propertyY+=26;if(y<108||y+20>height-31)return;Screen editor=(Screen)(Object)this;
        if(foreverSkyblock$isHeaderLabel()){
            var style=foreverSkyblock$style();String en=style.labelEn(),de=style.labelDe();
            addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Text bearbeiten…":"Edit text…"),b->minecraft.setScreen(new EditorHeaderTextScreen(editor,en,de,(nextEn,nextDe)->foreverSkyblock$updateLabel(nextEn,nextDe)))).bounds(width-rightWidth,y,rightWidth-8,20).build());
        }else if(foreverSkyblock$isHeaderIcon()){
            addRenderableWidget(Button.builder(Component.literal(EditorClient.german()?"Icon auswählen…":"Choose icon…"),b->minecraft.setScreen(new EditorHeaderIconPicker(editor,this::foreverSkyblock$updateIcon))).bounds(width-rightWidth,y,rightWidth-8,20).build());
        }
    }

    @Inject(method="entryName",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$friendlyElementNames(String id,CallbackInfoReturnable<String> cir){
        String name=foreverSkyblock$friendly(id);if(name!=null)cir.setReturnValue(name);
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
    @Unique private static String foreverSkyblock$valueAt(JsonObject obj,String path){JsonElement current=obj;for(String part:path.split("\\.")){if(current==null)return "";current=current.isJsonArray()?current.getAsJsonArray().get(Integer.parseInt(part)):current.getAsJsonObject().get(part);}return current==null||current.isJsonNull()?"":current.getAsString();}

    @Unique private static String foreverSkyblock$friendly(String id){
        boolean de=EditorClient.german();return switch(id){
            case "header.brand"->de?"Header / Logo":"Header / Brand";
            case "header.sp"->"Header / SP";case "header.sp.icon"->"SP / Icon";case "header.sp.label"->de?"SP / Text":"SP / Label";case "header.sp.value"->de?"SP / Wert":"SP / Value";
            case "header.pp"->"Header / PP";case "header.pp.icon"->"PP / Icon";case "header.pp.label"->de?"PP / Text":"PP / Label";case "header.pp.value"->de?"PP / Wert":"PP / Value";
            case "header.tier"->"Header / Tier";case "header.tier.icon"->"Tier / Icon";case "header.tier.label"->de?"Tier / Text":"Tier / Label";case "header.tier.value"->de?"Tier / Wert":"Tier / Value";
            case "header.stats"->de?"Header / Statistik":"Header / Stats";
            case "header.stats.playtime.icon"->de?"Spielzeit / Icon":"Playtime / Icon";case "header.stats.playtime.label"->de?"Spielzeit / Text":"Playtime / Label";case "header.stats.playtime.value"->de?"Spielzeit / Wert":"Playtime / Value";
            case "header.stats.island.icon"->"Island / Icon";case "header.stats.island.label"->de?"Island / Text":"Island / Label";case "header.stats.island.value"->de?"Island / Wert":"Island / Value";
            case "nav.overview"->de?"Tab / Übersicht":"Tab / Overview";case "nav.challenges"->"Tab / Challenges";case "nav.shop"->"Tab / Shop";case "nav.research"->"Tab / Research";case "nav.perks"->"Tab / Perks";case "nav.profile"->"Tab / Profile";
            case "search"->de?"Challenges / Suche":"Challenges / Search";case "categories"->de?"Challenges / Kategorien":"Challenges / Categories";
            case "card"->de?"Challenge / Karte":"Challenge / Card";case "card.title"->de?"Karte / Titel":"Card / Title";case "card.description"->de?"Karte / Beschreibung":"Card / Description";case "card.icon"->"Card / Icon";case "card.reward"->de?"Karte / Belohnung":"Card / Reward";
            case "detail"->de?"Details / Fenster":"Detail / Window";case "detail.title"->de?"Details / Titel":"Detail / Title";case "detail.icon"->"Detail / Icon";case "detail.prose"->de?"Details / Text":"Detail / Text";
            case "profile.identity"->de?"Profil / Identität":"Profile / Identity";case "profile.stats"->de?"Profil / Statistik":"Profile / Stats";
            default->id.startsWith("body.")?(de?"Bereich / ":"Area / ")+id.substring(5):null;};
    }
}
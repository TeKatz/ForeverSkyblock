package de.tekatz.foreverskyblock.client;

import com.google.gson.*;
import de.tekatz.foreverskyblock.editor.*;
import de.tekatz.foreverskyblock.network.EditorNetwork;
import java.nio.file.*;
import java.util.*;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.network.PacketDistributor;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** Published client state is separate from private authoring drafts and preview overrides. */
public final class EditorClient {
    public record Rect(int x,int y,int width,int height){public boolean contains(double mx,double my){return mx>=x&&my>=y&&mx<x+width&&my<y+height;}}
    public record QuestState(Map<String,Integer> counts,boolean complete,boolean unlocked) { }
    private static EditorDocument published,preview;
    private static final Map<String,Rect> bounds=new LinkedHashMap<>();
    private static Map<String,QuestState> progress=Map.of();
    private static boolean canEdit;
    private static String status="";
    private static int viewport=640,simulation;
    private EditorClient() { }
    public static EditorDocument document(){if(preview!=null)return preview;if(published!=null)return published;return Defaults.VALUE;}
    private static final class Defaults {static final EditorDocument VALUE=EditorDefaults.create();}
    public static int questCount(){return (int)document().quests().stream().filter(Quest::enabled).count();}
    public static int completedCount(){var snapshot=ClientSkills.snapshot();return (int)document().quests().stream().filter(q->q.enabled()&&(q.legacy()?snapshot!=null&&snapshot.milestones().contains(q.id()):state(q.id()).complete())).count();}
    public static boolean customized(){return published!=null||preview!=null;}
    public static java.util.List<de.tekatz.foreverskyblock.progression.ShopCatalog.Product> products(){return EditorRuntime.products(document());}
    public static de.tekatz.foreverskyblock.progression.ShopCatalog.Product product(String id){return products().stream().filter(p->p.id().equals(id)).findFirst().orElse(null);}
    public static java.util.List<de.tekatz.foreverskyblock.progression.ContractCatalog.Contract> contracts(){return EditorRuntime.contracts(document());}
    public static void simulation(int value){simulation=value;}
    public static int simulation(){return preview==null?0:simulation;}
    public static boolean canEdit(){return canEdit;}
    public static boolean german(){return Minecraft.getInstance().getLanguageManager().getSelected().startsWith("de_");}
    public static String status(){return status;}
    public static void status(String value){status=value;}
    public static QuestState state(String id){return progress.getOrDefault(id,new QuestState(Map.of(),false,false));}
    public static void beginFrame(int width){viewport=width;bounds.clear();}
    public static Map<String,Rect> bounds(){return new LinkedHashMap<>(bounds);}
    public static void preview(EditorDocument d){preview=d;}
    public static Style style(String id){var d=document();String variant=viewport<470?"small/":viewport<760?"compact/":"wide/";return d.layout().getOrDefault(variant+id,d.layout().getOrDefault(id,Style.defaults()));}
    public static void capture(String id,int x,int y,int w,int h){bounds.put(id,new Rect(x,y,w,h));}
    public static void group(GuiGraphics g,String id,int x,int y,int w,int h,Runnable painter){
        var rect=rect(id,x,y,w,h);if(rect.width()<=0||rect.height()<=0||w<=0||h<=0)return;
        var previous=MenuArt.pushStyle(style(id));g.pose().pushPose();
        try{g.pose().translate(rect.x(),rect.y(),0);g.pose().scale(rect.width()/(float)w,rect.height()/(float)h,1);g.pose().translate(-x,-y,0);painter.run();}
        finally{g.pose().popPose();MenuArt.pushStyle(previous);}
    }
    public static Rect rect(String id,int x,int y,int width,int height) {
        var s=style(id);int w=s.width()==0?width:s.width(),h=s.height()==0?height:s.height();
        int dx=s.anchorX()==Anchor.CENTER?(width-w)/2:s.anchorX()==Anchor.END?width-w:0;
        int dy=s.anchorY()==Anchor.CENTER?(height-h)/2:s.anchorY()==Anchor.END?height-h:0;
        var r=new Rect(x+s.x()+dx,y+s.y()+dy,s.hidden()?0:w,s.hidden()?0:h);bounds.put(id,r);return r;
    }
    public static void item(GuiGraphics g,ItemStack stack,int x,int y,String id) {
        var s=style(id);var r=rect(id,x,y,16,16);if(r.width()==0||r.height()==0)return;
        if(!s.icon().isEmpty())stack=net.minecraft.core.registries.BuiltInRegistries.ITEM.getOptional(net.minecraft.resources.ResourceLocation.parse(s.icon())).orElse(net.minecraft.world.item.Items.BARRIER).getDefaultInstance();
        g.pose().pushPose();g.pose().translate(r.x(),r.y(),0);g.pose().scale(r.width()/16F*s.itemScale(),r.height()/16F*s.itemScale(),1);g.renderItem(stack,0,0);g.pose().popPose();
    }
    public static void text(GuiGraphics g,Component text,int x,int y,int w,int h,int color,float scale,String id,boolean wrap) {
        var s=style(id);var r=rect(id,x,y,w,h);if(r.width()<=0||r.height()<=0)return;
        int c=EditorCodec.color(s.text(),color);float fs=scale*s.fontScale();
        if(wrap)MenuArt.wrapped(g,text,r.x(),r.y(),r.width(),c,fs,Math.max(1,(int)(r.height()/(11*fs))));
        else MenuArt.centered(g,text,r.x(),r.y(),r.width(),r.height(),c,fs);
    }
    public static void requestOpen(){PacketDistributor.sendToServer(new EditorNetwork.Request(0,UUID.randomUUID(),0,1,0,""));}
    public static void publish(EditorDocument d){
        String value=EditorCodec.WIRE.toJson(d);EditorCodec.decode(value);UUID id=UUID.randomUUID();int total=Math.max(1,(value.length()+EditorNetwork.CHUNK-1)/EditorNetwork.CHUNK);
        for(int i=0;i<total;i++)PacketDistributor.sendToServer(new EditorNetwork.Request(1,id,i,total,d.revision(),value.substring(i*EditorNetwork.CHUNK,Math.min(value.length(),(i+1)*EditorNetwork.CHUNK))));
        status="Publishing…";
    }
    public static void accept(EditorNetwork.Reply reply) {
        canEdit=reply.canEdit();if(!reply.message().isEmpty())status=reply.message();
        if(reply.kind()==0) {
            published=EditorCodec.decode(reply.data());EditorRuntime.publish(published);
            var mc=Minecraft.getInstance();
            if(reply.open())mc.setScreen(new KMenuEditorScreen(mc.screen,published));
            else if(mc.screen instanceof PremiumSkillScreen s)s.editorReload();
        }else if(reply.kind()==1){
            var map=new LinkedHashMap<String,QuestState>();var obj=JsonParser.parseString(reply.data()).getAsJsonObject();
            obj.entrySet().forEach(e->map.put(e.getKey(),EditorCodec.JSON.fromJson(e.getValue(),QuestState.class)));progress=Map.copyOf(map);if(Minecraft.getInstance().screen instanceof PremiumSkillScreen s)s.editorProgressRefresh();
        }
        if(Minecraft.getInstance().screen instanceof KMenuEditorScreen s)s.serverReply(reply);
    }
    public static void clear(){published=preview=null;progress=Map.of();canEdit=false;status="";bounds.clear();EditorRuntime.publish(null);}
    public static Path directory(){
        var mc=Minecraft.getInstance();String identity=mc.getCurrentServer()==null?"local":mc.getCurrentServer().ip;
        String key=EditorStore.hash(identity.getBytes(java.nio.charset.StandardCharsets.UTF_8)).substring(0,16);
        return mc.gameDirectory.toPath().resolve("forever_skyblock/editor").resolve(key);
    }
    public static void decorations(GuiGraphics g,String page,int left,int top) {
        for(var w:document().decorations())if(w.page().equals("ALL")||w.page().equals(page)) {
            int x=left+w.x(),y=top+w.y(),color=EditorCodec.color(w.color(),MenuArt.CYAN);bounds.put("decoration/"+w.id(),new Rect(x,y,w.width(),w.height()));
            String value=w.title().value(german());var s=ClientSkills.snapshot();
            if(s!=null&&!w.binding().isEmpty())value=switch(w.binding()) {
                case "SP"->""+s.points();case "PP"->""+s.perkPoints();case "TIER"->""+s.shopTier();case "PLAYTIME"->s.statistics().playTicks()/1200+"m";
                case "ISLAND"->""+s.statistics().islandLevel();case "DEATHS"->""+s.statistics().deaths();case "KILLS"->""+s.statistics().mobKills();case "CHALLENGES"->""+completedCount();default->value;};
            switch(w.type()) {
                case TEXT->MenuArt.centered(g,Component.literal(value),x,y,w.width(),w.height(),color,1);
                case PANEL->MenuArt.panel(g,x,y,w.width(),w.height(),color,MenuArt.BORDER);
                case ITEM->{var item=net.minecraft.core.registries.BuiltInRegistries.ITEM.getOptional(net.minecraft.resources.ResourceLocation.parse(w.icon())).orElse(net.minecraft.world.item.Items.BARRIER);g.pose().pushPose();g.pose().translate(x,y,0);g.pose().scale(w.width()/16F,w.height()/16F,1);g.renderItem(item.getDefaultInstance(),0,0);g.pose().popPose();}
                case BAR->MenuArt.bar(g,x,y,w.width(),s==null?0:completedCount()/(double)Math.max(1,questCount()),color);
            }
        }
    }
}

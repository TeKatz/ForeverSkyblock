package de.tekatz.foreverskyblock.editor;

import de.tekatz.foreverskyblock.network.EditorNetwork;
import de.tekatz.foreverskyblock.progression.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.fml.loading.FMLPaths;
import net.neoforged.neoforge.network.PacketDistributor;

/** Server owns authorization, active rules, publication revision and durable storage. */
public final class EditorService {
    private static final Map<MinecraftServer,EditorService> SERVICES=new IdentityHashMap<>();
    private final MinecraftServer server;
    private final EditorStore store;
    private EditorDocument document;
    private final Map<UUID,Long> sent=new HashMap<>(), lastRequest=new HashMap<>();
    private final Map<UUID,Upload> uploads=new HashMap<>();
    private String loadError="";
    private static final class Upload {
        UUID id;long revision,started;int total,next;StringBuilder data=new StringBuilder();
    }
    private EditorService(MinecraftServer server) {
        this.server=server;store=new EditorStore(FMLPaths.CONFIGDIR.get().resolve("forever_skyblock/kmenu.json"));
        try {
            document=store.load();
            if(!store.exists())try(var in=EditorService.class.getResourceAsStream("/assets/forever_skyblock/editor/kmenu.json")) {
                if(in!=null)document=EditorCodec.decode(new String(in.readNBytes(EditorDocument.MAX_BYTES+1),StandardCharsets.UTF_8));
            }
        }catch(IOException|RuntimeException e){document=EditorDefaults.create();loadError="Could not load K-menu config: "+e.getMessage();com.mojang.logging.LogUtils.getLogger().error(loadError,e);}
        EditorRuntime.publish(document);
        EditorRuntime.failed(!loadError.isEmpty());
    }
    public static EditorService get(MinecraftServer server){return SERVICES.computeIfAbsent(server,EditorService::new);}
    public static void stop(MinecraftServer server){SERVICES.remove(server);EditorRuntime.publish(null);}
    public static void logout(MinecraftServer server,UUID id){var s=SERVICES.get(server);if(s!=null){s.sent.remove(id);s.uploads.remove(id);s.lastRequest.remove(id);}}
    public EditorDocument document(){return document;}
    public void event(ServerPlayer p,QuestRules.Event event){if(loadError.isEmpty())ProgressionService.get(server).editorEvent(p,document,event);}
    private static boolean connected(ServerPlayer p){return p.connection!=null&&p.connection.getConnection().isConnected()&&net.neoforged.neoforge.network.registration.NetworkRegistry.hasChannel(p.connection,EditorNetwork.Reply.TYPE.id());}
    private void reply(ServerPlayer p,int kind,String data,String message,boolean open){if(connected(p))PacketDistributor.sendToPlayer(p,new EditorNetwork.Reply(kind,document.revision(),data,message,ProgressionService.canTestPoints(p),open));}
    public void sync(ServerPlayer p,ProgressionProfile profile,boolean opening) {
        if(!connected(p))return;
        if(!Objects.equals(sent.get(p.getUUID()),document.revision())){reply(p,0,EditorCodec.WIRE.toJson(document),loadError,false);sent.put(p.getUUID(),document.revision());}
        var progress=new LinkedHashMap<String,Object>();
        for(var q:document.quests())progress.put(q.id(),Map.of("counts",QuestRules.counts(q,profile),"complete",profile.milestones().contains(q.id()),"unlocked",QuestRules.unlocked(q,profile)));
        reply(p,1,EditorCodec.WIRE.toJson(progress),"",false);
    }
    public void request(ServerPlayer p,EditorNetwork.Request request) {
        if(!ProgressionService.canTestPoints(p)){reply(p,2,"","Administrator permission required",false);return;}
        long now=System.nanoTime();
        try {
            if(request.action()==0){
                long prior=lastRequest.getOrDefault(p.getUUID(),0L);if(now-prior<250_000_000L)return;lastRequest.put(p.getUUID(),now);
                reply(p,0,EditorCodec.WIRE.toJson(document),loadError,true);return;
            }
            if(request.action()==1){receive(p,request,now);return;}
            if(request.revision()!=document.revision())throw new IOException("Conflict: server revision changed");
            long prior=lastRequest.getOrDefault(p.getUUID(),0L);if(now-prior<250_000_000L)throw new IOException("Please wait before another operation");lastRequest.put(p.getUUID(),now);
            if(request.action()==2){
                var next=store.load();EditorCodec.validateUpdate(document,next);activate(next.withRevision(Math.addExact(document.revision(),1)));reply(p,2,"","Reloaded",false);
            }else if(request.action()==3){
                var backup=EditorMerge.rollback(document,store.readBackup());EditorCodec.validateUpdate(document,backup);activate(backup.withRevision(Math.addExact(document.revision(),1)));reply(p,2,"","Backup restored",false);
            }
        }catch(IOException|RuntimeException e){uploads.remove(p.getUUID());reply(p,2,"",e.getMessage()==null?"Editor operation failed":e.getMessage().substring(0,Math.min(950,e.getMessage().length())),false);}
    }
    private void receive(ServerPlayer p,EditorNetwork.Request r,long now) throws IOException {
        if(!loadError.isEmpty())throw new IOException(loadError);
        if(r.revision()!=document.revision())throw new IOException("Conflict: another administrator published a newer revision. Reopen and compare your draft.");
        Upload upload=uploads.get(p.getUUID());
        if(r.index()==0){
            long prior=lastRequest.getOrDefault(p.getUUID(),0L);if(now-prior<250_000_000L)throw new IOException("Please wait before publishing again");lastRequest.put(p.getUUID(),now);
            upload=new Upload();upload.id=r.transfer();upload.revision=r.revision();upload.total=r.total();upload.started=now;uploads.put(p.getUUID(),upload);
        }
        if(upload==null||!upload.id.equals(r.transfer())||upload.revision!=r.revision()||upload.total!=r.total()||upload.next!=r.index()||now-upload.started>30_000_000_000L)throw new IOException("Invalid or expired upload sequence");
        upload.data.append(r.data());upload.next++;if(upload.data.length()>EditorDocument.MAX_BYTES)throw new IOException("Document too large");
        if(upload.next<upload.total)return;
        uploads.remove(p.getUUID());var next=EditorCodec.decode(upload.data.toString());EditorCodec.validateUpdate(document,next);validateRegistry(next);
        activate(next.withRevision(Math.addExact(document.revision(),1)));reply(p,2,"","Published revision "+document.revision(),false);
        com.mojang.logging.LogUtils.getLogger().info("K-menu revision {} published by {}",document.revision(),p.getUUID());
    }
    private void validateRegistry(EditorDocument d) {
        for(var q:d.quests())if(!q.legacy())for(var task:q.tasks()) {
            var id=ResourceLocation.parse(task.target());boolean exists=switch(task.type()) {
                case CRAFT,SMELT,PICKUP -> BuiltInRegistries.ITEM.containsKey(id);
                case BREAK,PLACE -> BuiltInRegistries.BLOCK.containsKey(id);
                case KILL -> task.target().equals("minecraft:any_hostile")||BuiltInRegistries.ENTITY_TYPE.containsKey(id);
                case DIMENSION -> server.levelKeys().stream().anyMatch(key->key.location().equals(id));
                case ADVANCEMENT -> server.getAdvancements().get(id)!=null;
                default -> true;
            };EditorCodec.require(exists,"Unknown quest target: "+task.target());
        }
        for(var p:d.products())if(ShopCatalog.PRODUCTS.stream().noneMatch(x->x.id().equals(p.id())))for(var stack:p.contents())EditorCodec.require(BuiltInRegistries.ITEM.containsKey(ResourceLocation.parse(stack.item())),"Unknown package item: "+stack.item());
        for(var c:d.contracts())if(c.type()==EditorDocument.ContractType.RESOURCE&&ContractCatalog.CONTRACTS.stream().noneMatch(x->x.id().equals(c.id())))EditorCodec.require(BuiltInRegistries.ITEM.containsKey(ResourceLocation.parse(c.item())),"Unknown contract item: "+c.item());
    }
    private void activate(EditorDocument next) throws IOException {
        store.save(next);document=next;EditorRuntime.publish(next);EditorRuntime.failed(false);sent.clear();loadError="";
        for(var p:server.getPlayerList().getPlayers())ProgressionService.get(server).refreshMenu(p);
    }
}

package de.tekatz.foreverskyblock.editor;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.ChallengeEvents;
import net.minecraft.commands.Commands;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.RegisterCommandsEvent;
import net.neoforged.neoforge.event.entity.player.*;
import net.neoforged.neoforge.event.level.BlockEvent;
import net.neoforged.neoforge.event.server.*;
import static de.tekatz.foreverskyblock.editor.EditorDocument.TaskType.*;

@EventBusSubscriber(modid=ForeverSkyblock.MOD_ID)
public final class EditorEvents {
    private record Deferred(ServerPlayer player,EditorDocument.TaskType type,String id,java.util.function.BooleanSupplier succeeded) { }
    private static final java.util.Map<net.minecraft.server.MinecraftServer,java.util.List<Deferred>> DEFERRED=new java.util.IdentityHashMap<>();
    private record Pending(ServerPlayer player,java.util.List<QuestRules.Event> events) { }
    private static final java.util.Map<net.minecraft.server.MinecraftServer,java.util.Map<java.util.UUID,Pending>> PENDING=new java.util.IdentityHashMap<>();
    private EditorEvents() { }
    public static void emit(ServerPlayer p,EditorDocument.TaskType type,String target,int count,String token){
        if(!ChallengeEvents.realPlayer(p)||count<=0)return;
        var service=EditorService.get(p.server);
        if(service.document().quests().stream().noneMatch(q->q.enabled()&&!q.legacy()&&q.tasks().stream().anyMatch(t->t.type()==type)))return;
        var players=PENDING.computeIfAbsent(p.server,s->new java.util.LinkedHashMap<>());var pending=players.computeIfAbsent(p.getUUID(),id->new Pending(p,new java.util.ArrayList<>()));
        if(pending.events().size()<512)pending.events().add(new QuestRules.Event(type,target,Math.min(count,1_000_000),token));
    }
    @SubscribeEvent public static void start(ServerAboutToStartEvent e){EditorService.get(e.getServer());}
    @SubscribeEvent public static void stop(ServerStoppedEvent e){DEFERRED.remove(e.getServer());EditorService.stop(e.getServer());}
    @SubscribeEvent public static void stopping(ServerStoppingEvent e){flush(e.getServer());}
    @SubscribeEvent public static void logout(PlayerEvent.PlayerLoggedOutEvent e){if(e.getEntity() instanceof ServerPlayer p){var map=PENDING.get(p.server);var pending=map==null?null:map.remove(p.getUUID());if(pending!=null)EditorService.get(p.server).events(p,pending.events());EditorService.logout(p.server,p.getUUID());}}
    @SubscribeEvent public static void craft(PlayerEvent.ItemCraftedEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,CRAFT,BuiltInRegistries.ITEM.getKey(e.getCrafting().getItem()).toString(),e.getCrafting().getCount(),"");}
    @SubscribeEvent public static void smelt(PlayerEvent.ItemSmeltedEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,SMELT,BuiltInRegistries.ITEM.getKey(e.getSmelting().getItem()).toString(),e.getSmelting().getCount(),"");}
    @SubscribeEvent public static void pickup(ItemEntityPickupEvent.Post e){if(e.getPlayer() instanceof ServerPlayer p)emit(p,PICKUP,BuiltInRegistries.ITEM.getKey(e.getOriginalStack().getItem()).toString(),e.getOriginalStack().getCount()-e.getCurrentStack().getCount(),"");}
    @SubscribeEvent public static void dimension(PlayerEvent.PlayerChangedDimensionEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,DIMENSION,e.getTo().location().toString(),1,"");}
    @SubscribeEvent(priority=net.neoforged.bus.api.EventPriority.LOWEST) public static void broken(BlockEvent.BreakEvent e){
        if(!e.isCanceled()&&e.getPlayer() instanceof ServerPlayer p){var level=p.serverLevel();var pos=e.getPos().immutable();var block=e.getState().getBlock();defer(p,BREAK,BuiltInRegistries.BLOCK.getKey(block).toString(),()->!e.isCanceled()&&!level.getBlockState(pos).is(block));}
    }
    @SubscribeEvent(priority=net.neoforged.bus.api.EventPriority.LOWEST) public static void placed(BlockEvent.EntityPlaceEvent e){
        if(!e.isCanceled()&&e.getEntity() instanceof ServerPlayer p){var level=p.serverLevel();var pos=e.getPos().immutable();var block=e.getPlacedBlock().getBlock();defer(p,PLACE,BuiltInRegistries.BLOCK.getKey(block).toString(),()->!e.isCanceled()&&level.getBlockState(pos).is(block));}
    }
    private static void defer(ServerPlayer p,EditorDocument.TaskType type,String id,java.util.function.BooleanSupplier succeeded){if(!ChallengeEvents.realPlayer(p))return;var list=DEFERRED.computeIfAbsent(p.server,s->new java.util.ArrayList<>());if(list.size()<4096)list.add(new Deferred(p,type,id,succeeded));}
    @SubscribeEvent public static void afterTick(net.neoforged.neoforge.event.tick.ServerTickEvent.Post e){var list=DEFERRED.remove(e.getServer());if(list!=null)for(var action:list)if(!action.player().isRemoved()&&action.succeeded().getAsBoolean())emit(action.player(),action.type(),action.id(),1,"");flush(e.getServer());}
    public static void flush(net.minecraft.server.MinecraftServer server){var players=PENDING.remove(server);if(players!=null)for(var pending:players.values())EditorService.get(server).events(pending.player(),pending.events());}
    @SubscribeEvent public static void advancement(AdvancementEvent.AdvancementEarnEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,ADVANCEMENT,e.getAdvancement().id().toString(),1,"");}
    @SubscribeEvent public static void commands(RegisterCommandsEvent e){e.getDispatcher().register(Commands.literal("forevereditor").requires(s->s.hasPermission(2)||s.getEntity() instanceof ServerPlayer p&&p.server.isSingleplayerOwner(p.getGameProfile())).executes(c->{var p=c.getSource().getPlayerOrException();EditorService.get(p.server).request(p,new de.tekatz.foreverskyblock.network.EditorNetwork.Request(0,java.util.UUID.randomUUID(),0,1,0,""));return 1;}));}
}

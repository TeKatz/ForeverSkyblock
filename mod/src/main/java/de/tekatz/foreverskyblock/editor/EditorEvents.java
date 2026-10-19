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
    private EditorEvents() { }
    public static void emit(ServerPlayer p,EditorDocument.TaskType type,String target,int count,String token){if(ChallengeEvents.realPlayer(p)&&count>0)EditorService.get(p.server).event(p,new QuestRules.Event(type,target,Math.min(count,1_000_000),token));}
    @SubscribeEvent public static void start(ServerAboutToStartEvent e){EditorService.get(e.getServer());}
    @SubscribeEvent public static void stop(ServerStoppedEvent e){EditorService.stop(e.getServer());}
    @SubscribeEvent public static void logout(PlayerEvent.PlayerLoggedOutEvent e){if(e.getEntity() instanceof ServerPlayer p)EditorService.logout(p.server,p.getUUID());}
    @SubscribeEvent public static void craft(PlayerEvent.ItemCraftedEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,CRAFT,BuiltInRegistries.ITEM.getKey(e.getCrafting().getItem()).toString(),e.getCrafting().getCount(),"");}
    @SubscribeEvent public static void smelt(PlayerEvent.ItemSmeltedEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,SMELT,BuiltInRegistries.ITEM.getKey(e.getSmelting().getItem()).toString(),e.getSmelting().getCount(),"");}
    @SubscribeEvent public static void pickup(ItemEntityPickupEvent.Post e){if(e.getPlayer() instanceof ServerPlayer p)emit(p,PICKUP,BuiltInRegistries.ITEM.getKey(e.getOriginalStack().getItem()).toString(),e.getOriginalStack().getCount()-e.getCurrentStack().getCount(),"");}
    @SubscribeEvent public static void dimension(PlayerEvent.PlayerChangedDimensionEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,DIMENSION,e.getTo().location().toString(),1,"");}
    @SubscribeEvent(priority=net.neoforged.bus.api.EventPriority.MONITOR) public static void broken(BlockEvent.BreakEvent e){if(!e.isCanceled()&&e.getPlayer() instanceof ServerPlayer p)emit(p,BREAK,BuiltInRegistries.BLOCK.getKey(e.getState().getBlock()).toString(),1,"");}
    @SubscribeEvent(priority=net.neoforged.bus.api.EventPriority.MONITOR) public static void placed(BlockEvent.EntityPlaceEvent e){if(!e.isCanceled()&&e.getEntity() instanceof ServerPlayer p)emit(p,PLACE,BuiltInRegistries.BLOCK.getKey(e.getPlacedBlock().getBlock()).toString(),1,"");}
    @SubscribeEvent public static void advancement(AdvancementEvent.AdvancementEarnEvent e){if(e.getEntity() instanceof ServerPlayer p)emit(p,ADVANCEMENT,e.getAdvancement().id().toString(),1,"");}
    @SubscribeEvent public static void commands(RegisterCommandsEvent e){e.getDispatcher().register(Commands.literal("forevereditor").requires(s->s.hasPermission(2)||s.getEntity() instanceof ServerPlayer p&&p.server.isSingleplayerOwner(p.getGameProfile())).executes(c->{var p=c.getSource().getPlayerOrException();EditorService.get(p.server).request(p,new de.tekatz.foreverskyblock.network.EditorNetwork.Request(0,java.util.UUID.randomUUID(),0,1,0,""));return 1;}));}
}

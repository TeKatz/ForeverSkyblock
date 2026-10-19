package de.tekatz.foreverskyblock.progression;

import com.mojang.brigadier.arguments.IntegerArgumentType;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.commands.Commands;
import net.minecraft.commands.arguments.EntityArgument;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.common.util.FakePlayer;
import net.neoforged.neoforge.event.RegisterCommandsEvent;
import net.neoforged.neoforge.event.entity.player.PlayerContainerEvent;
import net.neoforged.neoforge.event.entity.player.PlayerEvent;
import net.neoforged.neoforge.event.server.ServerStoppedEvent;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class ProgressionEvents {
    private ProgressionEvents() { }
    @SubscribeEvent public static void login(PlayerEvent.PlayerLoggedInEvent event) { refresh(event); }
    @SubscribeEvent public static void logout(PlayerEvent.PlayerLoggedOutEvent event) {
        if (event.getEntity() instanceof ServerPlayer player) ProgressionService.logout(player.server, player.getUUID());
    }
    @SubscribeEvent public static void respawn(PlayerEvent.PlayerRespawnEvent event) { refresh(event); }
    @SubscribeEvent public static void dimension(PlayerEvent.PlayerChangedDimensionEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && ChallengeEvents.realPlayer(player)) {
            var definition = event.getTo().equals(net.minecraft.world.level.Level.NETHER) ? ChallengeRules.NETHER
                    : event.getTo().equals(net.minecraft.world.level.Level.END) ? ChallengeRules.END : null;
            if (definition != null) ProgressionService.get(player.server).evidence(player,
                    java.util.List.of(ChallengeEvents.Evidence.completed(definition)));
        }
        refresh(event);
    }
    @SubscribeEvent public static void containerOpened(PlayerContainerEvent.Open event) {
        if (event.getEntity() instanceof ServerPlayer player)
            TechChallengeEvents.wirelessCraftingOpened(player, event.getContainer());
    }
    private static void refresh(PlayerEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && !(player instanceof FakePlayer))
            ProgressionService.get(player.server).login(player);
    }
    @SubscribeEvent public static void crafted(PlayerEvent.ItemCraftedEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && !(player instanceof FakePlayer)
                && MilestoneCatalog.isFragmentCraft(event.getCrafting(), event.getInventory()))
            ProgressionService.get(player.server).evidence(player, java.util.List.of(ChallengeEvents.Evidence.completed(ChallengeRules.FOUNDATIONS)));
    }
    @SubscribeEvent public static void stopped(ServerStoppedEvent event) { ProgressionService.stop(event.getServer()); }

    @SubscribeEvent public static void commands(RegisterCommandsEvent event) {
        event.getDispatcher().register(Commands.literal("foreverskills")
                .executes(context -> { var player = context.getSource().getPlayerOrException(); ProgressionService.get(player.server).open(player); return 1; })
                .then(Commands.literal("grant").requires(source -> source.hasPermission(2))
                        .then(Commands.argument("player", EntityArgument.player())
                                .then(Commands.argument("points", IntegerArgumentType.integer(1, 1000)).executes(context -> {
                                    var player = EntityArgument.getPlayer(context, "player");
                                    boolean granted = ProgressionService.get(player.server).grant(player, IntegerArgumentType.getInteger(context, "points"));
                                    if (granted) context.getSource().sendSuccess(() -> Component.translatable("message.forever_skyblock.points_granted", player.getDisplayName()), true);
                                    return granted ? 1 : 0;
                                }))))
                .then(Commands.literal("admin").requires(source -> source.hasPermission(2))
                        .then(Commands.literal("sp")
                                .executes(context->{var player=context.getSource().getPlayerOrException();boolean ok=ProgressionService.get(player.server).grant(player,1);return adminResult(context.getSource(),player,ok,"+1 SP");})
                                .then(Commands.argument("player",EntityArgument.player())
                                        .then(Commands.argument("amount",IntegerArgumentType.integer(1,1000)).executes(context->{
                                            var player=EntityArgument.getPlayer(context,"player");int amount=IntegerArgumentType.getInteger(context,"amount");
                                            boolean ok=ProgressionService.get(player.server).grant(player,amount);return adminResult(context.getSource(),player,ok,"+"+amount+" SP");
                                        }))))
                        .then(Commands.literal("perk")
                                .executes(context->{var player=context.getSource().getPlayerOrException();boolean ok=admin(player).adminGrantPerks(player,1);return adminResult(context.getSource(),player,ok,"+1 PP");})
                                .then(Commands.argument("player",EntityArgument.player())
                                        .executes(context->{var player=EntityArgument.getPlayer(context,"player");boolean ok=admin(player).adminGrantPerks(player,1);return adminResult(context.getSource(),player,ok,"+1 PP");})
                                        .then(Commands.argument("amount",IntegerArgumentType.integer(1,1000)).executes(context->{
                                            var player=EntityArgument.getPlayer(context,"player");int amount=IntegerArgumentType.getInteger(context,"amount");boolean ok=admin(player).adminGrantPerks(player,amount);return adminResult(context.getSource(),player,ok,"+"+amount+" PP");
                                        }))))
                        .then(Commands.literal("tierup")
                                .executes(context->{var player=context.getSource().getPlayerOrException();boolean ok=admin(player).adminTierUp(player);return adminResult(context.getSource(),player,ok,"Shop Tier +1");})
                                .then(Commands.argument("player",EntityArgument.player()).executes(context->{var player=EntityArgument.getPlayer(context,"player");boolean ok=admin(player).adminTierUp(player);return adminResult(context.getSource(),player,ok,"Shop Tier +1");})))
                        .then(Commands.literal("tier")
                                .then(Commands.argument("player",EntityArgument.player())
                                        .then(Commands.argument("tier",IntegerArgumentType.integer(0,4)).executes(context->{var player=EntityArgument.getPlayer(context,"player");int tier=IntegerArgumentType.getInteger(context,"tier");boolean ok=admin(player).adminSetTier(player,tier);return adminResult(context.getSource(),player,ok,"Shop Tier "+tier);}))))
                        .then(Commands.literal("hardreset")
                                .executes(context->{var player=context.getSource().getPlayerOrException();boolean ok=admin(player).adminHardReset(player);return adminResult(context.getSource(),player,ok,"HARD RESET");})
                                .then(Commands.argument("player",EntityArgument.player()).executes(context->{var player=EntityArgument.getPlayer(context,"player");boolean ok=admin(player).adminHardReset(player);return adminResult(context.getSource(),player,ok,"HARD RESET");})))));
    }

    /** The interface is attached at runtime by ProgressionServiceAdminMixin. */
    private static ProgressionAdminAccess admin(ServerPlayer player){return (ProgressionAdminAccess)(Object)ProgressionService.get(player.server);}
    private static int adminResult(net.minecraft.commands.CommandSourceStack source,ServerPlayer player,boolean ok,String action){
        if(ok)source.sendSuccess(()->Component.literal("[Forever Skyblock] "+player.getGameProfile().getName()+": "+action),true);
        else source.sendFailure(Component.literal("[Forever Skyblock] Admin operation failed: "+action));
        return ok?1:0;
    }
}

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
import net.neoforged.neoforge.event.entity.player.PlayerEvent;
import net.neoforged.neoforge.event.server.ServerStoppedEvent;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class ProgressionEvents {
    private ProgressionEvents() { }
    @SubscribeEvent public static void login(PlayerEvent.PlayerLoggedInEvent event) { refresh(event); }
    @SubscribeEvent public static void respawn(PlayerEvent.PlayerRespawnEvent event) { refresh(event); }
    @SubscribeEvent public static void dimension(PlayerEvent.PlayerChangedDimensionEvent event) { refresh(event); }
    private static void refresh(PlayerEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && !(player instanceof FakePlayer))
            ProgressionService.get(player.server).login(player);
    }
    @SubscribeEvent public static void crafted(PlayerEvent.ItemCraftedEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && !(player instanceof FakePlayer)
                && MilestoneCatalog.isFragmentCraft(event.getCrafting(), event.getInventory()))
            ProgressionService.get(player.server).award(player, MilestoneCatalog.FIRST_FOUNDATIONS);
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
                                })))));
    }
}

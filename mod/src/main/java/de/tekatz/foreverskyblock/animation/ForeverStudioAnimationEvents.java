package de.tekatz.foreverskyblock.animation;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.network.ForeverStudioAnimationNetwork;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.AddReloadListenerEvent;
import net.neoforged.neoforge.event.OnDatapackSyncEvent;

/** Server-data registration and authoritative snapshot synchronization. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class ForeverStudioAnimationEvents {
    private ForeverStudioAnimationEvents() { }

    @SubscribeEvent
    public static void addReloadListener(AddReloadListenerEvent event) {
        event.addListener(new ForeverStudioAnimationReloadListener());
    }

    @SubscribeEvent
    public static void datapackSync(OnDatapackSyncEvent event) {
        ServerPlayer player = event.getPlayer();
        if (player != null) {
            ForeverStudioAnimationNetwork.send(player);
            return;
        }
        for (ServerPlayer connected : event.getPlayerList().getPlayers())
            ForeverStudioAnimationNetwork.send(connected);
    }
}

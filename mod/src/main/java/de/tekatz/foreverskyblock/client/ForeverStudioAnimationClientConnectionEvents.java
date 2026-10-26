package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.ClientPlayerNetworkEvent;

/** Prevents a previous server's Studio snapshot from leaking into the next connection. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, value = Dist.CLIENT)
public final class ForeverStudioAnimationClientConnectionEvents {
    private ForeverStudioAnimationClientConnectionEvents() { }

    @SubscribeEvent
    public static void loggedOut(ClientPlayerNetworkEvent.LoggingOut event) {
        ForeverStudioClientAnimations.clear();
    }
}

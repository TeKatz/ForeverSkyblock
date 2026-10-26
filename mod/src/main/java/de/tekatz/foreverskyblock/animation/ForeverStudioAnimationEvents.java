package de.tekatz.foreverskyblock.animation;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.AddReloadListenerEvent;

/** Server-data registration only. Client synchronization/binding is Phase 17D.3. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class ForeverStudioAnimationEvents {
    private ForeverStudioAnimationEvents() { }

    @SubscribeEvent
    public static void addReloadListener(AddReloadListenerEvent event) {
        event.addListener(new ForeverStudioAnimationReloadListener());
    }
}

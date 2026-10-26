package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.network.ForeverStudioAnimationNetwork;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.fml.event.lifecycle.FMLClientSetupEvent;

/** Physical-client bootstrap for the common animation sync payload. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD, value = Dist.CLIENT)
public final class ForeverStudioAnimationClientSetup {
    private ForeverStudioAnimationClientSetup() { }

    @SubscribeEvent
    public static void setup(FMLClientSetupEvent event) {
        event.enqueueWork(() -> ForeverStudioAnimationNetwork.setClientReceiver(chunk ->
                ForeverStudioClientAnimations.accept(chunk.generation(), chunk.index(), chunk.total(), chunk.data())));
    }
}

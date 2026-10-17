package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.RegisterMenuScreensEvent;
import net.neoforged.neoforge.client.event.RegisterKeyMappingsEvent;
import net.neoforged.fml.event.lifecycle.FMLClientSetupEvent;
import de.tekatz.foreverskyblock.network.SkillNetwork;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD, value = Dist.CLIENT)
public final class ClientEvents {
    private ClientEvents() { }
    @SubscribeEvent public static void registerKeys(RegisterKeyMappingsEvent event) { event.register(ClientSkills.OPEN); }
    @SubscribeEvent public static void setup(FMLClientSetupEvent event) {
        event.enqueueWork(() -> {
            SkillNetwork.setClientReceiver(ClientSkills::accept);
            de.tekatz.foreverskyblock.progression.JetpackGate.setClientStage(ClientSkills::jetpackStage);
        });
    }
    @SubscribeEvent public static void registerRenderers(net.neoforged.neoforge.client.event.EntityRenderersEvent.RegisterRenderers event) {
        event.registerBlockEntityRenderer(ForeverSkyblock.CONDENSER_ENTITY.get(), CondenserRenderer::new);
        event.registerBlockEntityRenderer(ForeverSkyblock.ASCENSION_ENTITY.get(), AscensionRenderer::new);
    }
    @SubscribeEvent public static void registerScreens(RegisterMenuScreensEvent event) {
        event.register(ForeverSkyblock.ASCENSION_MENU.get(), AscensionScreen::new);
        event.register(ForeverSkyblock.CONDENSER_MENU.get(), CondenserScreen::new);
        event.register(ForeverSkyblock.WASHING_BASIN_MENU.get(), WashingScreen::new);
    }
}

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
        event.enqueueWork(() -> SkillNetwork.setClientReceiver(ClientSkills::accept));
    }
    @SubscribeEvent public static void registerScreens(RegisterMenuScreensEvent event) {
        event.register(ForeverSkyblock.CONDENSER_MENU.get(), CondenserScreen::new);
    }
}

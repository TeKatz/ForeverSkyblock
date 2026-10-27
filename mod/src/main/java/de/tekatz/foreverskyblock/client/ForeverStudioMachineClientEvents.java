package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.RegisterMenuScreensEvent;

/** Client bridge kept outside generated source so the runtime compiler owns only data and registrations. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD, value = Dist.CLIENT)
public final class ForeverStudioMachineClientEvents {
    private ForeverStudioMachineClientEvents() { }

    @SubscribeEvent
    public static void registerScreens(RegisterMenuScreensEvent event) {
        if (ForeverStudioGeneratedMachines.MACHINE_MENU.isBound())
            event.register(ForeverStudioGeneratedMachines.MACHINE_MENU.get(), ForeverStudioMachineScreen::new);
    }
}

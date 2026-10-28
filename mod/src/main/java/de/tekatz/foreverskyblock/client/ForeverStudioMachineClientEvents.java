package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import de.tekatz.foreverskyblock.network.ForeverStudioMachineNetwork;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlock;
import net.minecraft.client.resources.model.ModelResourceLocation;
import net.minecraft.core.registries.BuiltInRegistries;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.fml.event.lifecycle.FMLClientSetupEvent;
import net.neoforged.neoforge.client.event.EntityRenderersEvent;
import net.neoforged.neoforge.client.event.ModelEvent;
import net.neoforged.neoforge.client.event.RegisterMenuScreensEvent;

/** Client bridge kept outside generated source so the runtime compiler owns only data and registrations. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD, value = Dist.CLIENT)
public final class ForeverStudioMachineClientEvents {
    private ForeverStudioMachineClientEvents() { }

    @SubscribeEvent
    public static void setup(FMLClientSetupEvent event) {
        event.enqueueWork(() -> ForeverStudioMachineNetwork.setClientReceiver(ForeverStudioMachineClientState::accept));
    }

    @SubscribeEvent
    public static void registerAdditionalModels(ModelEvent.RegisterAdditional event) {
        ForeverStudioMachineComponentModels.clearCache();
        for (var block : BuiltInRegistries.BLOCK) {
            if (!(block instanceof ForeverStudioMachineBlock machineBlock)) continue;
            for (var component : ForeverStudioMachineComponentModels.modelsFor(machineBlock.machineId()))
                event.register(ModelResourceLocation.standalone(component.model()));
        }
    }

    @SubscribeEvent
    public static void registerScreens(RegisterMenuScreensEvent event) {
        if (ForeverStudioGeneratedMachines.MACHINE_MENU.isBound())
            event.register(ForeverStudioGeneratedMachines.MACHINE_MENU.get(), ForeverStudioMachineScreen::new);
    }

    @SubscribeEvent
    public static void registerRenderers(EntityRenderersEvent.RegisterRenderers event) {
        if (ForeverStudioGeneratedMachines.MACHINE_BLOCK_ENTITY.isBound())
            event.registerBlockEntityRenderer(ForeverStudioGeneratedMachines.MACHINE_BLOCK_ENTITY.get(), ForeverStudioMachineRenderer::new);
    }
}

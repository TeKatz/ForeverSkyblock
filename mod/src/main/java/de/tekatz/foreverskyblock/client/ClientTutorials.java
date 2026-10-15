package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.client.Minecraft;
import net.minecraft.client.tutorial.TutorialSteps;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.ClientPlayerNetworkEvent;

/** Disable Vanilla's introductory movement/tree/inventory/crafting tutorial when entering a world. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, value = Dist.CLIENT)
public final class ClientTutorials {
    private ClientTutorials() { }
    @SubscribeEvent public static void login(ClientPlayerNetworkEvent.LoggingIn event) {
        var minecraft = Minecraft.getInstance();
        if (minecraft.options.tutorialStep != TutorialSteps.NONE) minecraft.getTutorial().setStep(TutorialSteps.NONE);
    }
}

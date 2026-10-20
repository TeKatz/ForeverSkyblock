package de.tekatz.foreverskyblock;

import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.packs.PackType;
import net.minecraft.server.packs.repository.Pack;
import net.minecraft.server.packs.repository.PackSource;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.AddPackFindersEvent;

/** Registers the small client-resource pack that adds Nether progression item translations. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class NetherProgressionTranslations {
    private NetherProgressionTranslations() { }

    @SubscribeEvent
    private static void addPackFinders(AddPackFindersEvent event) {
        event.addPackFinders(
                ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "nether_progression_translations"),
                PackType.CLIENT_RESOURCES,
                Component.literal("Forever Skyblock Nether Progression Translations"),
                PackSource.BUILT_IN,
                true,
                Pack.Position.TOP);
    }
}

package de.tekatz.foreverskyblock;

import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.packs.PackType;
import net.minecraft.server.packs.repository.Pack;
import net.minecraft.server.packs.repository.PackSource;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.ModList;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.AddPackFindersEvent;

/**
 * Gives Forever Skyblock's fortress loot rules deterministic priority over optional third-party loot modifiers
 * without turning those optional mods into hard runtime dependencies.
 */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class FortressLootOverrides {
    private FortressLootOverrides() { }

    @SubscribeEvent
    private static void addPackFinders(AddPackFindersEvent event) {
        if (ModList.get().isLoaded("apotheosis")) {
            addOverridePack(event, "fortress_loot_apotheosis", "Forever Skyblock Apotheosis Fortress Loot Override");
        }
        if (ModList.get().isLoaded("sophisticatedbackpacks")) {
            addOverridePack(event, "fortress_loot_sophisticated_backpacks", "Forever Skyblock Sophisticated Backpacks Fortress Loot Override");
        }
    }

    private static void addOverridePack(AddPackFindersEvent event, String path, String displayName) {
        event.addPackFinders(
                ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, path),
                PackType.SERVER_DATA,
                Component.literal(displayName),
                PackSource.BUILT_IN,
                true,
                Pack.Position.TOP);
    }
}

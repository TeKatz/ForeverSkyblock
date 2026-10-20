package de.tekatz.foreverskyblock;

import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.packs.PackType;
import net.minecraft.server.packs.repository.Pack;
import net.minecraft.server.packs.repository.PackSource;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.AddPackFindersEvent;

/**
 * Gives Forever Skyblock's fortress loot rules a deterministic priority over third-party global loot modifiers.
 */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class FortressLootOverrides {
    private FortressLootOverrides() { }

    @SubscribeEvent
    private static void addPackFinders(AddPackFindersEvent event) {
        event.addPackFinders(
                ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "fortress_loot_overrides"),
                PackType.SERVER_DATA,
                Component.literal("Forever Skyblock Fortress Loot Overrides"),
                PackSource.BUILT_IN,
                true,
                Pack.Position.TOP);
    }
}

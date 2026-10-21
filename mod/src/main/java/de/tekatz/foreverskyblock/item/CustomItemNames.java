package de.tekatz.foreverskyblock.item;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.component.DataComponents;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.Item;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.event.ModifyDefaultComponentsEvent;

/** Stable player-facing names for custom progression items, including JEI ingredient names. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD)
public final class CustomItemNames {
    private CustomItemNames() { }

    @SubscribeEvent
    public static void apply(ModifyDefaultComponentsEvent event) {
        name(event, ForeverSkyblock.REFINING_CHAMBER_ITEM.get(), "Refining Condenser");
        name(event, ForeverSkyblock.MINERAL_BLEND.get(), "Mineral Blend");
        name(event, ForeverSkyblock.CARBON_MATRIX.get(), "Carbon Matrix");
    }

    private static void name(ModifyDefaultComponentsEvent event, Item target, String name) {
        event.modifyMatching(item -> item == target,
                builder -> builder.set(DataComponents.ITEM_NAME, Component.literal(name)));
    }
}

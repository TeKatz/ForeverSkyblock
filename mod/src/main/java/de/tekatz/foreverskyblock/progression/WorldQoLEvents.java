package de.tekatz.foreverskyblock.progression;

import com.mojang.logging.LogUtils;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import java.io.IOException;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.monster.Phantom;
import net.minecraft.world.entity.npc.WanderingTrader;
import net.minecraft.world.item.Items;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.common.util.FakePlayer;
import net.neoforged.neoforge.event.entity.EntityJoinLevelEvent;
import net.neoforged.neoforge.event.entity.player.ItemEntityPickupEvent;
import net.neoforged.neoforge.event.server.ServerStoppedEvent;

/** World-wide QoL gates deliberately stay independent from personal challenge completion. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class WorldQoLEvents {
    private WorldQoLEvents() { }

    @SubscribeEvent public static void itemPickedUp(ItemEntityPickupEvent.Post event) {
        if (!(event.getPlayer() instanceof ServerPlayer player) || player instanceof FakePlayer
                || !event.getOriginalStack().is(Items.EMERALD)) return;
        try {
            if (WorldQoLService.unlockTrader(player.server))
                player.sendSystemMessage(Component.translatable("message.forever_skyblock.wandering_trader_unlocked"));
        } catch (IOException | RuntimeException e) {
            LogUtils.getLogger().error("Could not unlock Wandering Trader for world", e);
        }
    }

    @SubscribeEvent public static void entityJoins(EntityJoinLevelEvent event) {
        if (!(event.getLevel() instanceof ServerLevel level)) return;
        if (event.getEntity() instanceof WanderingTrader && !WorldQoLService.wanderingTraderAllowed(level.getServer()))
            event.setCanceled(true);
        else if (event.getEntity() instanceof Phantom && WorldQoLService.upgradeActive(level.getServer(), ShopCatalog.NO_PHANTOMS.id()))
            event.setCanceled(true);
    }

    @SubscribeEvent public static void stopped(ServerStoppedEvent event) { WorldQoLService.stop(event.getServer()); }
}

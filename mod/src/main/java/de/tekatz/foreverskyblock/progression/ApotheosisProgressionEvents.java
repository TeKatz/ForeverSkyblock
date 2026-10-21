package de.tekatz.foreverskyblock.progression;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import java.util.UUID;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.Items;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.common.util.FakePlayer;
import net.neoforged.neoforge.event.entity.item.ItemTossEvent;
import net.neoforged.neoforge.event.entity.player.PlayerEvent;
import net.neoforged.neoforge.event.tick.PlayerTickEvent;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID)
public final class ApotheosisProgressionEvents {
    public static final String DIAMOND_SACRIFICER = "forever_skyblock_diamond_sacrificer";

    private ApotheosisProgressionEvents() { }

    @SubscribeEvent
    public static void login(PlayerEvent.PlayerLoggedInEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && !(player instanceof FakePlayer))
            ApotheosisProgressionService.sync(player);
    }

    @SubscribeEvent
    public static void respawn(PlayerEvent.PlayerRespawnEvent event) {
        if (event.getEntity() instanceof ServerPlayer player && !(player instanceof FakePlayer))
            ApotheosisProgressionService.sync(player);
    }

    @SubscribeEvent
    public static void dimension(PlayerEvent.PlayerChangedDimensionEvent event) {
        if (!(event.getEntity() instanceof ServerPlayer player) || player instanceof FakePlayer) return;
        if (event.getTo().equals(net.minecraft.world.level.Level.NETHER))
            ApotheosisProgressionService.advance(player.server, ApotheosisStage.HAVEN, player);
        else ApotheosisProgressionService.sync(player);
    }

    @SubscribeEvent
    public static void tick(PlayerTickEvent.Post event) {
        if (!(event.getEntity() instanceof ServerPlayer player) || player instanceof FakePlayer) return;
        if ((player.tickCount % 40) == 0) ApotheosisProgressionService.sync(player);
    }

    @SubscribeEvent
    public static void tossed(ItemTossEvent event) {
        if (!(event.getPlayer() instanceof ServerPlayer player) || player instanceof FakePlayer) return;
        if (!event.getEntity().getItem().is(Items.DIAMOND)) return;
        if (ApotheosisProgressionService.stage(player.server) != ApotheosisStage.HAVEN) return;
        event.getEntity().getPersistentData().putUUID(DIAMOND_SACRIFICER, player.getUUID());
    }

    public static boolean consumeDiamondSacrifice(net.minecraft.world.entity.item.ItemEntity item) {
        if (!(item.level() instanceof net.minecraft.server.level.ServerLevel level) || !item.getItem().is(Items.DIAMOND)) return false;
        CompoundTag data = item.getPersistentData();
        if (!data.hasUUID(DIAMOND_SACRIFICER)) return false;
        UUID playerId = data.getUUID(DIAMOND_SACRIFICER);
        ServerPlayer source = level.getServer().getPlayerList().getPlayer(playerId);
        return ApotheosisProgressionService.stage(level.getServer()) == ApotheosisStage.HAVEN
                && ApotheosisProgressionService.advance(level.getServer(), ApotheosisStage.FRONTIER, source);
    }
}
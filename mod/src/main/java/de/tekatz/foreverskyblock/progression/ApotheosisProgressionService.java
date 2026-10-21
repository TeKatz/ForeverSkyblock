package de.tekatz.foreverskyblock.progression;

import com.mojang.logging.LogUtils;
import de.tekatz.foreverskyblock.network.ApotheosisNetwork;
import java.io.IOException;
import java.lang.reflect.Method;
import net.minecraft.network.chat.Component;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.fml.ModList;
import org.slf4j.Logger;

/** Keeps Forever's world-owned progression authoritative over Apotheosis' player-owned World Tier. */
public final class ApotheosisProgressionService {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final String APOTHEOSIS = "apotheosis";

    private ApotheosisProgressionService() { }

    public static ApotheosisStage stage(MinecraftServer server) {
        return WorldQoLService.apotheosisStage(server);
    }

    public static boolean dormant(MinecraftServer server) {
        return stage(server) == ApotheosisStage.DORMANT;
    }

    public static boolean advance(MinecraftServer server, ApotheosisStage requested, ServerPlayer source) {
        try {
            if (!WorldQoLService.advanceApotheosis(server, requested)) return false;
            for (ServerPlayer player : server.getPlayerList().getPlayers()) sync(player);
            Component tierName = Component.translatable("text.apotheosis.world_tier." + requested.id());
            server.getPlayerList().broadcastSystemMessage(
                    Component.literal("[Forever Skyblock] Apotheosis → ").append(tierName), false);
            return true;
        } catch (IOException | RuntimeException e) {
            LOGGER.error("Could not advance Apotheosis world progression to {}", requested, e);
            if (source != null) source.sendSystemMessage(
                    Component.literal("[Forever Skyblock] Apotheosis progression could not be saved."));
            return false;
        }
    }

    public static void sync(ServerPlayer player) {
        ApotheosisStage stage = stage(player.server);
        ApotheosisNetwork.send(player, stage);
        if (!stage.awakened() || !ModList.get().isLoaded(APOTHEOSIS)) return;
        syncNativeTier(player, stage);
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static void syncNativeTier(ServerPlayer player, ApotheosisStage stage) {
        try {
            Class<?> tierClass = Class.forName("dev.shadowsoffire.apotheosis.tiers.WorldTier");
            Object nativeTier = Enum.valueOf((Class<? extends Enum>) tierClass.asSubclass(Enum.class), stage.name());
            Method setTier = tierClass.getMethod("setTier", net.minecraft.world.entity.player.Player.class, tierClass);
            setTier.invoke(null, player, nativeTier);
        } catch (ReflectiveOperationException | LinkageError e) {
            LOGGER.error("Apotheosis is installed but its WorldTier integration could not be synchronized", e);
        }
    }
}

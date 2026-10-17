package de.tekatz.foreverskyblock.progression;

import com.mojang.logging.LogUtils;
import java.io.IOException;
import java.nio.file.Path;
import java.util.IdentityHashMap;
import java.util.Map;
import java.util.UUID;
import net.minecraft.server.MinecraftServer;
import net.minecraft.world.level.GameRules;
import net.minecraft.world.level.storage.LevelResource;
import org.slf4j.Logger;

/** Server-side owner of world-wide QoL state. Player profiles only provide durable purchase receipts. */
public final class WorldQoLService {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final Map<MinecraftServer, Context> CONTEXTS = new IdentityHashMap<>();

    private record Context(Path root, WorldIdentityStore identities, WorldProgressStore progress) { }

    private WorldQoLService() { }

    private static synchronized Context context(MinecraftServer server) {
        return CONTEXTS.computeIfAbsent(server, key -> new Context(
                key.getWorldPath(LevelResource.ROOT), new WorldIdentityStore(), new WorldProgressStore()));
    }

    public static WorldProgressStore.State state(MinecraftServer server) throws IOException {
        Context context = context(server);
        UUID world = context.identities().loadOrCreate(context.root());
        return context.progress().loadOrCreate(context.root(), world);
    }

    public static WorldProgressStore.State enableUpgrade(MinecraftServer server, String id) throws IOException {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || !product.worldUpgrade()) throw new IllegalArgumentException("Not a world upgrade");
        Context context = context(server);
        UUID world = context.identities().loadOrCreate(context.root());
        WorldProgressStore.State next = context.progress().enableUpgrade(context.root(), world, id);
        applyRules(server, next);
        return next;
    }

    public static boolean unlockTrader(MinecraftServer server) throws IOException {
        Context context = context(server);
        UUID world = context.identities().loadOrCreate(context.root());
        WorldProgressStore.State before = context.progress().loadOrCreate(context.root(), world);
        if (before.wanderingTraderUnlocked()) return false;
        context.progress().unlockTrader(context.root(), world);
        return true;
    }

    public static boolean wanderingTraderAllowed(MinecraftServer server) {
        try { return state(server).wanderingTraderUnlocked(); }
        catch (IOException | RuntimeException e) {
            LOGGER.error("Could not read Wandering Trader world gate", e);
            return false;
        }
    }

    public static boolean upgradeActive(MinecraftServer server, String id) {
        try { return state(server).active(id); }
        catch (IOException | RuntimeException e) {
            LOGGER.error("Could not read world upgrade {}", id, e);
            return false;
        }
    }

    public static void applyRules(MinecraftServer server) throws IOException {
        applyRules(server, state(server));
    }

    private static void applyRules(MinecraftServer server, WorldProgressStore.State state) {
        var level = server.overworld();
        GameRules rules = server.getGameRules();
        if (state.active(ShopCatalog.NO_PHANTOMS.id()))
            rules.getRule(GameRules.RULE_DOINSOMNIA).set(false, server);
        if (state.active(ShopCatalog.NO_RAIN.id())) {
            rules.getRule(GameRules.RULE_WEATHER_CYCLE).set(false, server);
            level.setWeatherParameters(24_000, 0, false, false);
        }
        if (state.active(ShopCatalog.PERMANENT_DAY.id())) {
            rules.getRule(GameRules.RULE_DAYLIGHT).set(false, server);
            level.setDayTime(1000L);
        }
    }

    public static synchronized void stop(MinecraftServer server) { CONTEXTS.remove(server); }
}

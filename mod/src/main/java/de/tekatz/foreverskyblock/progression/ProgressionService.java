package de.tekatz.foreverskyblock.progression;

import com.mojang.logging.LogUtils;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import java.io.IOException;
import java.nio.file.Path;
import java.util.IdentityHashMap;
import java.util.Map;
import net.minecraft.network.chat.Component;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.fml.loading.FMLPaths;
import org.slf4j.Logger;

public final class ProgressionService {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final Map<MinecraftServer, ProgressionService> SERVICES = new IdentityHashMap<>();
    private final ProfileStore store;
    private final CoreRedemptions redemptions;
    private final boolean local;
    private final WorldIdentityStore worldIdentities;
    private final Path worldRoot;
    private final String worldName;
    public ProgressionService(ProfileStore store, boolean local) {
        this(store, local, null, "");
    }
    private ProgressionService(ProfileStore store, boolean local, Path worldRoot, String worldName) {
        this.store = store; this.local = local; this.worldRoot = worldRoot; this.worldName = worldName;
        this.worldIdentities = new WorldIdentityStore();
        this.redemptions = new CoreRedemptions(store.file(new java.util.UUID(0, 0)).getParent().resolve("core_claims"), store);
    }
    public CoreRedemptions.Result redeemCore(ServerPlayer player, java.util.UUID core) throws IOException {
        var result = redemptions.redeem(core, player.getUUID());
        if (result == CoreRedemptions.Result.CREDITED) {
            if (connected(player)) player.sendSystemMessage(SkillMessages.perk());
            sync(player, false, "perk_earned");
        }
        return result;
    }
    public static ProgressionService get(MinecraftServer server) {
        return SERVICES.computeIfAbsent(server, key -> {
            boolean local = !key.isDedicatedServer();
            Path path = FMLPaths.GAMEDIR.get().resolve("forever_skyblock/progression").resolve(local ? "local" : "server");
            return new ProgressionService(new ProfileStore(path), local,
                    key.getWorldPath(net.minecraft.world.level.storage.LevelResource.ROOT), key.getWorldData().getLevelName());
        });
    }
    public static void logout(MinecraftServer server, java.util.UUID player) {
        var service = SERVICES.get(server);
        if (service != null) service.store.evict(player);
    }
    public static void stop(MinecraftServer server) { SERVICES.remove(server); }
    public void login(ServerPlayer player) {
        try { SkillCatalog.apply(player, store.load(player.getUUID())); sync(player, false, ""); }
        catch (IOException e) { error(player, false, e); }
    }
    public void open(ServerPlayer player) { sync(player, true, ""); }
    public void buy(ServerPlayer player, String id) {
        SkillCatalog.Skill skill = SkillCatalog.find(id);
        if (skill == null) { sync(player, false, "unknown_skill"); return; }
        try {
            ProgressionProfile previous = store.load(player.getUUID());
            String message;
            if (previous.rank(id) >= skill.maxRank()) message = "already_owned";
            else if (!skill.available()) message = "missing_jetpacks";
            else if (!skill.canPurchase(previous)) message = "requires_previous";
            else if (previous.perkPoints() < skill.perkCost()) message = "not_enough_perks";
            else if (previous.points() < skill.cost()) message = "not_enough_points";
            else {
                ProgressionProfile next = store.update(player.getUUID(), state -> state.purchase(id, skill.cost(), skill.perkCost(), skill.maxRank()));
                SkillCatalog.apply(player, next);
                message = "purchased";
            }
            sync(player, false, message);
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }
    public synchronized void buyProduct(ServerPlayer player, String id) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null) { sync(player, false, "unknown_product"); return; }
        if (product.developmentOnly() && !canTestPoints(player)) { sync(player, false, "test_denied"); return; }
        java.util.UUID world;
        try { world = currentWorld(); }
        catch (IOException e) {
            LOGGER.error("Could not read world identity for {}", player.getUUID(), e);
            sync(player, false, "world_unavailable"); return;
        }
        try {
            ProgressionProfile previous = store.load(player.getUUID());
            String message;
            if (previous.meta().claimed(world, product.id())) message = "world_already_claimed";
            else if (previous.meta().shopTier() < product.requiredTier()) message = "world_requires_tier";
            else if (previous.points() < product.spCost()) message = "not_enough_points";
            else {
                store.update(player.getUUID(), state -> state.purchaseWorld(world, product, java.util.UUID.randomUUID()));
                message = "world_purchase_done";
            }
            sync(player, false, message);
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }
    public int perkPoints(ServerPlayer player) throws IOException { return store.load(player.getUUID()).perkPoints(); }
    public int jetpackStage(ServerPlayer player) {
        try { return JetpackResearch.stage(store.load(player.getUUID())::rank); }
        catch (IOException e) { return 0; }
    }
    public static boolean canTestPoints(ServerPlayer player) {
        return player.hasPermissions(2) || player.server.isSingleplayerOwner(player.getGameProfile());
    }
    /** Temporary developer action: fixed server-side amount, never a client-supplied balance. */
    public void testPoints(ServerPlayer player) {
        if (!canTestPoints(player)) { sync(player, false, "test_denied"); return; }
        try {
            store.update(player.getUUID(), state -> state.grant(10));
            sync(player, false, "test_points_added");
        } catch (IOException e) { error(player, false, e); }
        catch (IllegalArgumentException | ArithmeticException e) { sync(player, false, "reset_limit"); }
    }
    public void reset(ServerPlayer player) {
        try {
            ProgressionProfile next = store.update(player.getUUID(), SkillCatalog::reset);
            SkillCatalog.apply(player, next);
            sync(player, false, "reset_done");
        } catch (IOException e) { error(player, false, e); }
        catch (IllegalArgumentException | ArithmeticException e) { sync(player, false, "reset_limit"); }
    }
    public boolean evidence(ServerPlayer player, java.util.List<ChallengeEvents.Evidence> events) {
        if (!ChallengeEvents.realPlayer(player) || events.isEmpty()) return false;
        try {
            ProgressionProfile previous = store.load(player.getUUID());
            ProgressionProfile next = store.update(player.getUUID(), state -> {
                for (var event : events) state = ChallengeRules.record(state, event.challenge(), event.token());
                return state;
            });
            if (next.equals(previous)) return false;
            boolean completed = false;
            for (var milestone : MilestoneCatalog.MILESTONES) {
                if (!previous.milestones().contains(milestone.id()) && next.milestones().contains(milestone.id())) {
                    if (connected(player)) player.sendSystemMessage(SkillMessages.milestone(milestone)); completed = true;
                }
            }
            if (connected(player) && next.meta().shopTier() > previous.meta().shopTier()) player.sendSystemMessage(Component.translatable(
                    "message.forever_skyblock.tier_unlocked", Component.translatable("gui.forever_skyblock.meta.tier." + next.meta().shopTier())));
            sync(player, false, completed ? "milestone_earned" : "");
            return true;
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }
    public boolean grant(ServerPlayer player, int amount) {
        try { store.update(player.getUUID(), state -> state.grant(amount)); sync(player, false, "points_granted"); return true; }
        catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }
    private static boolean connected(ServerPlayer player) {
        return player.connection != null && player.connection.getConnection().isConnected();
    }
    private java.util.UUID currentWorld() throws IOException {
        if (worldRoot == null) throw new IOException("World identity unavailable outside a running server");
        return worldIdentities.loadOrCreate(worldRoot);
    }
    private void sync(ServerPlayer player, boolean open, String message) {
        try {
            ProgressionProfile profile = store.load(player.getUUID());
            SkillNetwork.WorldState world;
            try {
                java.util.UUID identity = currentWorld();
                world = new SkillNetwork.WorldState(true, worldName, profile.meta().worldClaims().getOrDefault(identity, java.util.Set.of()));
            } catch (IOException e) {
                LOGGER.error("Could not read world identity for {}", player.getUUID(), e);
                world = new SkillNetwork.WorldState(false, worldName, java.util.Set.of());
            }
            SkillNetwork.send(player, profile, local, open, true, message, world);
        }
        catch (IOException e) { error(player, open, e); }
    }
    private void error(ServerPlayer player, boolean open, Exception e) {
        LOGGER.error("Could not read or save progression for {}", player.getUUID(), e);
        if (connected(player)) player.sendSystemMessage(Component.translatable("gui.forever_skyblock.skills.storage_error"));
        SkillNetwork.send(player, ProgressionProfile.empty(), local, open, false, "storage_error",
                new SkillNetwork.WorldState(false, worldName, java.util.Set.of()));
    }
}

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
    private final boolean local;
    public ProgressionService(ProfileStore store, boolean local) { this.store = store; this.local = local; }
    public static ProgressionService get(MinecraftServer server) {
        return SERVICES.computeIfAbsent(server, key -> {
            boolean local = !key.isDedicatedServer();
            Path path = FMLPaths.GAMEDIR.get().resolve("forever_skyblock/progression").resolve(local ? "local" : "server");
            return new ProgressionService(new ProfileStore(path), local);
        });
    }
    public static void logout(MinecraftServer server, java.util.UUID player) {
        var service = SERVICES.get(server);
        if (service != null) service.store.evict(player);
    }
    public static void stop(MinecraftServer server) { SERVICES.remove(server); }
    public void login(ServerPlayer player) {
        try { SkillCatalog.apply(player, store.load(player.getUUID())); }
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
            else if (previous.points() < skill.cost()) message = "not_enough_points";
            else {
                ProgressionProfile next = store.update(player.getUUID(), state -> state.purchase(id, skill.cost(), skill.maxRank()));
                SkillCatalog.apply(player, next);
                message = "purchased";
            }
            sync(player, false, message);
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }
    public boolean award(ServerPlayer player, MilestoneCatalog.Milestone milestone) {
        try {
            ProgressionProfile previous = store.load(player.getUUID());
            ProgressionProfile next = store.update(player.getUUID(), state -> state.award(milestone.id(), milestone.reward()));
            if (next.equals(previous)) return false;
            player.sendSystemMessage(Component.translatable("message.forever_skyblock.milestone", Component.translatable(milestone.titleKey()), milestone.reward()));
            sync(player, false, "milestone_earned");
            return true;
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }
    public boolean grant(ServerPlayer player, int amount) {
        try { store.update(player.getUUID(), state -> state.grant(amount)); sync(player, false, "points_granted"); return true; }
        catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }
    private void sync(ServerPlayer player, boolean open, String message) {
        try { SkillNetwork.send(player, store.load(player.getUUID()), local, open, true, message); }
        catch (IOException e) { error(player, open, e); }
    }
    private void error(ServerPlayer player, boolean open, Exception e) {
        LOGGER.error("Could not read or save progression for {}", player.getUUID(), e);
        player.sendSystemMessage(Component.translatable("gui.forever_skyblock.skills.storage_error"));
        SkillNetwork.send(player, ProgressionProfile.empty(), local, open, false, "storage_error");
    }
}

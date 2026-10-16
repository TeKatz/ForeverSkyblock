package de.tekatz.foreverskyblock.progression;

import com.mojang.logging.LogUtils;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;
import org.slf4j.Logger;

/** Optional-mod challenge adapters. Optional classes are accessed reflectively so absent mods remain safe. */
public final class TechChallengeEvents {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final Set<String> REPORTED = ConcurrentHashMap.newKeySet();

    private TechChallengeEvents() { }

    /** Personal crafting evidence shared by normal crafting and AE2's manual crafting terminal. */
    public static List<ChallengeEvents.Evidence> crafted(ServerPlayer player, ResourceLocation recipe) {
        var holder = player.serverLevel().getRecipeManager().byKey(recipe).orElse(null);
        if (holder == null) return List.of();
        ItemStack result = holder.value().getResultItem(player.registryAccess());
        if (result.isEmpty()) return List.of();
        String output = BuiltInRegistries.ITEM.getKey(result.getItem()).toString();
        var evidence = new ArrayList<ChallengeEvents.Evidence>();
        if (ChallengeRules.AE2_STORAGE.objectives().contains(output))
            evidence.add(new ChallengeEvents.Evidence(ChallengeRules.AE2_STORAGE.id(), output));
        if (ChallengeRules.AE2_CRAFTING_BRAIN.objectives().contains(output))
            evidence.add(new ChallengeEvents.Evidence(ChallengeRules.AE2_CRAFTING_BRAIN.id(), output));
        if (ChallengeRules.AE2_STORAGE_ARCHITECT.objectives().contains(output))
            evidence.add(new ChallengeEvents.Evidence(ChallengeRules.AE2_STORAGE_ARCHITECT.id(), output));
        if (ChallengeRules.MEKANIZED.objectives().contains(output))
            evidence.add(new ChallengeEvents.Evidence(ChallengeRules.MEKANIZED.id(), output));
        if (ChallengeRules.SUPREMIUM.objectives().contains(output))
            evidence.add(new ChallengeEvents.Evidence(ChallengeRules.SUPREMIUM.id(), output));
        return evidence;
    }

    public static void wirelessCraftingOpened(ServerPlayer player, Object menu) {
        if (!ChallengeEvents.realPlayer(player) || menu == null) return;
        if (menu.getClass().getName().equals("appeng.menu.me.items.WirelessCraftingTermMenu"))
            complete(player, ChallengeRules.AE2_WIRELESS);
    }

    /** Called at the start of AE2 CraftingCpuLogic.finishJob while its requester record still exists. */
    public static void ae2AutocraftFinished(Object logic, boolean success) {
        if (!success || logic == null) return;
        try {
            Object job = field(logic, "job");
            if (job == null) return;
            Object playerIdValue = field(job, "playerId");
            if (!(playerIdValue instanceof Integer playerId)) return;
            Object cluster = field(logic, "cluster");
            Object levelValue = cluster.getClass().getMethod("getLevel").invoke(cluster);
            if (!(levelValue instanceof ServerLevel level)) return;
            Class<?> registry = Class.forName("appeng.api.features.IPlayerRegistry");
            Method getConnected = registry.getMethod("getConnected", MinecraftServer.class, int.class);
            Object connected = getConnected.invoke(null, level.getServer(), playerId.intValue());
            if (connected instanceof ServerPlayer player && ChallengeEvents.realPlayer(player))
                complete(player, ChallengeRules.AE2_AUTOCRAFT);
        } catch (ReflectiveOperationException | LinkageError e) {
            reportOnce("ae2-autocraft", e);
        }
    }

    /** Successful Digital Miner block replacement, credited only to its connected security owner. */
    public static void mekanismDigitalMinerMined(Object miner) {
        if (miner == null) return;
        try {
            Object ownerValue = miner.getClass().getMethod("getOwnerUUID").invoke(miner);
            if (!(ownerValue instanceof java.util.UUID owner)) return;
            Object levelValue = miner.getClass().getMethod("getLevel").invoke(miner);
            if (!(levelValue instanceof ServerLevel level)) return;
            ServerPlayer player = level.getServer().getPlayerList().getPlayer(owner);
            if (ChallengeEvents.realPlayer(player)) complete(player, ChallengeRules.DIGITAL_EXTRACTION);
        } catch (ReflectiveOperationException | LinkageError e) {
            reportOnce("mekanism-digital-miner", e);
        }
    }

    /** Powah's slot zero is output-only and is populated only when an Energizing recipe has completed. */
    public static boolean powahOrbHasCompletedOutput(Level level, BlockPos pos) {
        if (level == null || level.isClientSide()) return false;
        Object tile = level.getBlockEntity(pos);
        if (tile == null || !tile.getClass().getName().equals("owmii.powah.block.energizing.EnergizingOrbTile")) return false;
        try {
            Object inventory = tile.getClass().getMethod("getInventory").invoke(tile);
            Object stack = inventory.getClass().getMethod("getStackInSlot", int.class).invoke(inventory, 0);
            return stack instanceof ItemStack item && !item.isEmpty();
        } catch (ReflectiveOperationException | LinkageError e) {
            reportOnce("powah-energizing-orb", e);
            return false;
        }
    }

    public static void energizedOutputTaken(ServerPlayer player) {
        if (ChallengeEvents.realPlayer(player)) complete(player, ChallengeRules.ENERGIZED);
    }

    private static void complete(ServerPlayer player, ChallengeRules.Definition definition) {
        ProgressionService.get(player.server).evidence(player, List.of(ChallengeEvents.Evidence.completed(definition)));
    }

    private static Object field(Object target, String name) throws ReflectiveOperationException {
        Field field = target.getClass().getDeclaredField(name);
        field.setAccessible(true);
        return field.get(target);
    }

    private static void reportOnce(String adapter, Throwable error) {
        if (REPORTED.add(adapter)) LOGGER.warn("Forever Skyblock optional challenge adapter '{}' could not inspect the installed mod; the affected challenge will not award until compatibility is corrected.", adapter, error);
    }
}

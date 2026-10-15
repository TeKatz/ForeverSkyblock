package de.tekatz.foreverskyblock.progression;

import de.tekatz.foreverskyblock.compat.IronJetpacksBridge;
import java.util.function.IntSupplier;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.level.Level;
import net.neoforged.fml.ModList;
import net.neoforged.neoforge.common.CommonHooks;

public final class JetpackGate {
    private static IntSupplier clientStage = () -> 0;
    private JetpackGate() { }
    public static boolean installed() { return ModList.get().isLoaded("ironjetpacks"); }
    public static void setClientStage(IntSupplier supplier) { clientStage = supplier; }
    public static int requiredStage(ItemStack stack) {
        if (stack.isEmpty() || !BuiltInRegistries.ITEM.getKey(stack.getItem()).getNamespace().equals("ironjetpacks")) return 0;
        return installed() ? IronJetpacksBridge.requiredStage(stack) : 4;
    }
    public static boolean canUse(Player player, ItemStack stack) {
        int needed = requiredStage(stack);
        if (needed == 0) return true;
        int stage = player instanceof ServerPlayer serverPlayer ? ProgressionService.get(serverPlayer.server).jetpackStage(serverPlayer)
                : player.level().isClientSide() ? clientStage.getAsInt() : 0;
        return stage >= needed;
    }
    /** Ownerless crafting machines share the highest research of players currently on the server. */
    public static int automationStage(Level level) {
        if (level.isClientSide()) return clientStage.getAsInt();
        Player crafting = CommonHooks.getCraftingPlayer();
        if (crafting instanceof ServerPlayer p) return ProgressionService.get(p.server).jetpackStage(p);
        var server = level.getServer();
        if (server == null) return 0;
        int stage = 0;
        for (ServerPlayer p : server.getPlayerList().getPlayers()) stage = Math.max(stage, ProgressionService.get(server).jetpackStage(p));
        return stage;
    }
    public static boolean canCraft(Level level, ItemStack output) {
        int needed = requiredStage(output);
        return needed == 0 || automationStage(level) >= needed;
    }
}

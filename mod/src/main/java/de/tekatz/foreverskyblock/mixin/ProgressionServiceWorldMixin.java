package de.tekatz.foreverskyblock.mixin;

import com.mojang.logging.LogUtils;
import de.tekatz.foreverskyblock.progression.MetaProgress;
import de.tekatz.foreverskyblock.progression.ProfileStore;
import de.tekatz.foreverskyblock.progression.ProgressionProfile;
import de.tekatz.foreverskyblock.progression.ProgressionService;
import de.tekatz.foreverskyblock.progression.ShopCatalog;
import de.tekatz.foreverskyblock.progression.WorldQoLService;
import java.io.IOException;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Set;
import java.util.UUID;
import net.minecraft.server.level.ServerPlayer;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Bridges profile purchase receipts to world-wide effects without changing the profile schema or network payload. */
@Mixin(ProgressionService.class)
public abstract class ProgressionServiceWorldMixin {
    @Shadow @Final private ProfileStore store;
    @Shadow private UUID currentWorld() throws IOException { throw new AssertionError(); }
    @Shadow private void sync(ServerPlayer player, boolean open, String message) { throw new AssertionError(); }

    @Inject(method = {"login", "open"}, at = @At("HEAD"))
    private void foreverSkyblock$reconcileOnAccess(ServerPlayer player, CallbackInfo callback) {
        try { reconcile(player); }
        catch (IOException | RuntimeException e) { LogUtils.getLogger().error("Could not reconcile world upgrades for {}", player.getUUID(), e); }
    }

    @Inject(method = "buyProduct", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$reconcileBeforePurchase(ServerPlayer player, String id, CallbackInfo callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || !product.worldUpgrade()) return;
        try { reconcile(player); }
        catch (IOException | RuntimeException e) {
            LogUtils.getLogger().error("Could not verify world upgrade state for {}", player.getUUID(), e);
            sync(player, false, "world_unavailable");
            callback.cancel();
        }
    }

    @Inject(method = "buyProduct", at = @At("TAIL"))
    private void foreverSkyblock$activatePurchasedUpgrade(ServerPlayer player, String id, CallbackInfo callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || !product.worldUpgrade()) return;
        try {
            UUID world = currentWorld();
            ProgressionProfile profile = store.load(player.getUUID());
            if (!profile.meta().claimed(world, id)) return;
            boolean wasActive = WorldQoLService.state(player.server).active(id);
            if (!wasActive) {
                WorldQoLService.enableUpgrade(player.server, id);
                sync(player, false, "world_upgrade_activated");
            }
        } catch (IOException | RuntimeException e) {
            // The paid profile claim is the durable recovery source; next login/menu open retries activation.
            LogUtils.getLogger().error("World upgrade {} was paid but could not be activated immediately for {}", id, player.getUUID(), e);
        }
    }

    private void reconcile(ServerPlayer player) throws IOException {
        UUID world = currentWorld();
        ProgressionProfile profile = store.load(player.getUUID());
        for (String id : profile.meta().worldClaims().getOrDefault(world, Set.of())) {
            ShopCatalog.Product product = ShopCatalog.find(id);
            if (product != null && product.worldUpgrade() && !WorldQoLService.state(player.server).active(id))
                WorldQoLService.enableUpgrade(player.server, id);
        }
        var worldState = WorldQoLService.state(player.server);
        if (!worldState.upgrades().isEmpty()) {
            store.update(player.getUUID(), current -> mirrorWorldUpgrades(current, world, worldState.upgrades()));
        }
        WorldQoLService.applyRules(player.server);
    }

    private static ProgressionProfile mirrorWorldUpgrades(ProgressionProfile profile, UUID world, Set<String> upgrades) {
        Set<String> existing = profile.meta().worldClaims().getOrDefault(world, Set.of());
        if (existing.containsAll(upgrades)) return profile;
        var claims = new LinkedHashMap<UUID, Set<String>>(profile.meta().worldClaims());
        var products = new LinkedHashSet<>(existing); products.addAll(upgrades); claims.put(world, Set.copyOf(products));
        MetaProgress meta = profile.meta();
        return profile.withMeta(new MetaProgress(meta.shopTier(), meta.progress(), meta.accounting(), claims,
                meta.purchaseReceipts(), meta.contracts(), meta.pendingContracts(), meta.deliveries()));
    }
}

package de.tekatz.foreverskyblock.progression;

import com.mojang.logging.LogUtils;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.item.SupplyCacheItem;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import java.io.IOException;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
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
    public ProgressionService(ProfileStore store, boolean local) { this(store, local, null, ""); }
    private ProgressionService(ProfileStore store, boolean local, Path worldRoot, String worldName) {
        this.store = store; this.local = local; this.worldRoot = worldRoot; this.worldName = worldName;
        this.worldIdentities = new WorldIdentityStore();
        this.redemptions = new CoreRedemptions(store.file(new UUID(0, 0)).getParent().resolve("core_claims"), store);
    }
    public CoreRedemptions.Result redeemCore(ServerPlayer player, UUID core) throws IOException {
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
    public static void logout(MinecraftServer server, UUID player) {
        var service = SERVICES.get(server); if (service != null) service.store.evict(player);
    }
    public static void stop(MinecraftServer server) { SERVICES.remove(server); }
    public void login(ServerPlayer player) {
        de.tekatz.foreverskyblock.editor.EditorService.get(player.server);
        try {
            String recovery = recoverState(player);
            SkillCatalog.apply(player, store.load(player.getUUID()));
            sync(player, false, recovery);
        } catch (IOException e) { error(player, false, e); }
    }
    public void open(ServerPlayer player) {
        try { sync(player, true, recoverState(player)); }
        catch (IOException e) { error(player, true, e); }
    }
    private String recoverState(ServerPlayer player) throws IOException {
        if (!recoverPendingContract(player)) return "contract_recovery_required";
        if (!recoverPendingDelivery(player)) return "delivery_recovery_required";
        return "";
    }
    public void buy(ServerPlayer player, String id) {
        SkillCatalog.Skill skill = SkillCatalog.find(id);
        if (skill == null) { sync(player, false, "unknown_skill"); return; }
        try {
            ProgressionProfile previous = store.load(player.getUUID()); String message;
            if (previous.rank(id) >= skill.maxRank()) message = "already_owned";
            else if (!skill.available()) message = "missing_jetpacks";
            else if (!skill.canPurchase(previous)) message = "requires_previous";
            else if (previous.perkPoints() < skill.perkCost()) message = "not_enough_perks";
            else if (previous.points() < skill.cost()) message = "not_enough_points";
            else {
                ProgressionProfile next = store.update(player.getUUID(), state -> state.purchase(id, skill.cost(), skill.perkCost(), skill.maxRank()));
                SkillCatalog.apply(player, next); message = "purchased";
            }
            sync(player, false, message);
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }
    public synchronized void buyProduct(ServerPlayer player, String id) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null) { sync(player, false, "unknown_product"); return; }
        if (product.developmentOnly() && !canTestPoints(player)) { sync(player, false, "test_denied"); return; }
        if (!productAvailable(product)) { sync(player, false, "product_unavailable"); return; }
        UUID world;
        try { world = currentWorld(); }
        catch (IOException e) { LOGGER.error("Could not read world identity for {}", player.getUUID(), e); sync(player, false, "world_unavailable"); return; }
        try {
            ProgressionProfile previous = store.load(player.getUUID()); String message;
            if (previous.meta().claimed(world, product.id())) message = "world_already_claimed";
            else if (!de.tekatz.foreverskyblock.editor.EditorRuntime.productUnlocked(id, previous)) message = "world_requires_tier";
            else if (previous.meta().shopTier() < product.requiredTier()) message = "world_requires_tier";
            else if (previous.perkPoints() < product.ppCost()) message = "not_enough_perks";
            else if (previous.points() < product.spCost()) message = "not_enough_points";
            else {
                store.update(player.getUUID(), state -> state.purchaseWorld(world, product, UUID.randomUUID()));
                message = product.deliverable() ? "world_purchase_ready" : "world_purchase_done";
            }
            sync(player, false, message);
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }

    /** Claims the durable entitlement into one unique owner/world-bound cache item. */
    public synchronized void claimProduct(ServerPlayer player, String id) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || !product.deliverable()) { sync(player, false, "unknown_product"); return; }
        if (!productAvailable(product)) { sync(player, false, "product_unavailable"); return; }
        try {
            if (!recoverPendingDelivery(player)) { sync(player, false, "delivery_recovery_required"); return; }
            UUID world = currentWorld();
            ProgressionProfile profile = store.load(player.getUUID());
            var entry = profile.meta().delivery(world, product.id());
            if (entry == null) { sync(player, false, "delivery_not_purchased"); return; }
            UUID receipt = entry.getKey();
            switch (entry.getValue().stage()) {
                case READY -> {
                    if (firstEmptyMainSlot(player) < 0) { sync(player, false, "delivery_inventory_full"); return; }
                    store.update(player.getUUID(), state -> state.beginDelivery(receipt));
                    if (!completeDispatch(player, receipt)) { sync(player, false, "delivery_recovery_required"); return; }
                    sync(player, false, "delivery_delivered");
                }
                case DISPATCHING -> sync(player, false, recoverPendingDelivery(player) ? "delivery_delivered" : "delivery_recovery_required");
                case DELIVERED -> sync(player, false, "delivery_already_delivered");
                case OPENING -> sync(player, false, recoverPendingDelivery(player) ? "delivery_opened" : "delivery_recovery_required");
                case OPENED -> sync(player, false, "delivery_opened");
            }
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }

    /** Right-click path for a unique supply cache. Package contents never come from the client payload. */
    public synchronized boolean openSupplyCache(ServerPlayer player, ItemStack cache) {
        SupplyCacheItem.Marker marker = SupplyCacheItem.marker(cache);
        if (marker == null || !marker.ownerId().equals(player.getUUID())) return false;
        try {
            UUID world = currentWorld();
            if (!world.equals(marker.worldId())) { sync(player, false, "delivery_wrong_world"); return true; }
            ProgressionProfile profile = store.load(player.getUUID());
            var delivery = profile.meta().deliveries().get(marker.receiptId());
            if (delivery == null || !delivery.worldId().equals(world) || !delivery.productId().equals(marker.productId())) return false;
            ShopCatalog.Product product = ShopCatalog.find(delivery.productId());
            if (product == null || !product.deliverable() || !productAvailable(product)) { sync(player, false, "product_unavailable"); return true; }
            if (delivery.stage() == MetaProgress.DeliveryStage.OPENED) { sync(player, false, "delivery_opened"); return true; }
            if (delivery.stage() != MetaProgress.DeliveryStage.DELIVERED && delivery.stage() != MetaProgress.DeliveryStage.OPENING) {
                sync(player, false, "delivery_not_ready"); return true;
            }
            if (delivery.stage() == MetaProgress.DeliveryStage.DELIVERED) {
                if (!hasContentSpace(player, product, cache)) { sync(player, false, "delivery_inventory_full"); return true; }
                store.update(player.getUUID(), state -> state.beginOpening(marker.receiptId()));
            }
            if (!completeOpening(player, marker.receiptId())) { sync(player, false, "delivery_recovery_required"); return true; }
            sync(player, false, "delivery_opened");
            return true;
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return true; }
    }

    /** Resource turn-ins use a durable intent plus forced player-data checkpoints around inventory removal. */
    public synchronized void submitContract(ServerPlayer player, String id, UUID request) {
        ContractCatalog.Contract contract = ContractCatalog.find(id);
        if (contract == null || !de.tekatz.foreverskyblock.editor.EditorRuntime.contractEnabled(id) || !contract.resource() || request == null) { sync(player, false, "unknown_contract"); return; }
        try {
            if (!recoverPendingContract(player)) { sync(player, false, "contract_recovery_required"); return; }
            ProgressionProfile before = store.load(player.getUUID());
            if (before.meta().shopTier() < contract.requiredTier()) { sync(player, false, "contract_locked"); return; }
            Item item = contractItem(contract.itemId());
            if (item == null) { sync(player, false, "contract_unavailable"); return; }
            int inventoryBefore = inventoryCount(player, item);
            if (inventoryBefore < contract.target()) { sync(player, false, "contract_missing_resources"); return; }
            player.server.getPlayerList().saveAll();
            ProgressionProfile begun = store.update(player.getUUID(), state -> ContractRules.beginTurnIn(state, contract, request, inventoryBefore));
            var pending = begun.meta().pendingContracts().get(request);
            if (pending == null) { sync(player, false, ""); return; }
            if (inventoryCount(player, item) != pending.beforeCount() || !consume(player, item, pending.itemCount())) {
                LOGGER.error("Contract inventory changed while durable intent {} was being prepared for {}", request, player.getUUID());
                sync(player, false, "contract_recovery_required"); return;
            }
            player.server.getPlayerList().saveAll();
            ProgressionProfile finished = store.update(player.getUUID(), state -> ContractRules.finishTurnIn(state, request));
            if (finished.meta().pendingContracts().containsKey(request)) { sync(player, false, "contract_recovery_required"); return; }
            if (connected(player)) player.sendSystemMessage(Component.literal(contract.titleEn() + ": ")
                    .append(Component.translatable("message.forever_skyblock.skill_points", contract.rewardSp())));
            sync(player, false, "contract_completed");
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); }
    }

    public boolean killEvidence(ServerPlayer player, boolean wither, boolean dragon, boolean monster, UUID victim) {
        if (!ChallengeEvents.realPlayer(player) || victim == null || (!wither && !dragon && !monster)) return false;
        try {
            ProgressionProfile previous = store.load(player.getUUID());
            ProgressionProfile next = store.update(player.getUUID(), state -> {
                String token = victim.toString();
                if (wither) state = ContractRules.recordBoss(state, ChallengeRules.WITHER, ContractCatalog.WITHER_HUNTER, token);
                if (dragon) state = ContractRules.recordBoss(state, ChallengeRules.DRAGON, ContractCatalog.DRAGON_REBORN, token);
                if (monster) state = ChallengeRules.record(state, ChallengeRules.VETERAN.id(), token);
                return state;
            });
            if (wither) recordIsland(player, ChallengeRules.WITHER.id(), "completed");
            if (dragon) recordIsland(player, ChallengeRules.DRAGON.id(), "completed");
            if (monster) recordIsland(player, ChallengeRules.VETERAN.id(), victim.toString());
            if (next.equals(previous)) return false;
            notifyProgress(player, previous, next);
            sync(player, false, "");
            return true;
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }

    public void editorEvents(ServerPlayer player, de.tekatz.foreverskyblock.editor.EditorDocument document,
                            java.util.List<de.tekatz.foreverskyblock.editor.QuestRules.Event> events) {
        if (!ChallengeEvents.realPlayer(player)) return;
        try {
            var previous = store.load(player.getUUID());
            var next = store.update(player.getUUID(), state -> de.tekatz.foreverskyblock.editor.QuestRules.recordBatch(state,document,events));
            if (!next.equals(previous)) {
                for(var quest:document.quests()) if(!previous.milestones().contains(quest.id()) && next.milestones().contains(quest.id()) && connected(player))
                    player.sendSystemMessage(Component.literal(quest.title().en()+" +"+quest.rewardSp()+" SP / +"+quest.rewardPp()+" PP"));
                sync(player,false,"");
            }
        } catch (IOException | RuntimeException e) { error(player,false,e); }
    }
    public int perkPoints(ServerPlayer player) throws IOException { return store.load(player.getUUID()).perkPoints(); }
    public int jetpackStage(ServerPlayer player) {
        try { return JetpackResearch.stage(store.load(player.getUUID())::rank); } catch (IOException e) { return 0; }
    }
    public static boolean canTestPoints(ServerPlayer player) { return player.hasPermissions(2) || player.server.isSingleplayerOwner(player.getGameProfile()); }
    public void testPoints(ServerPlayer player) {
        if (!canTestPoints(player)) { sync(player, false, "test_denied"); return; }
        try { store.update(player.getUUID(), state -> state.grant(10)); sync(player, false, "test_points_added"); }
        catch (IOException e) { error(player, false, e); }
        catch (IllegalArgumentException | ArithmeticException e) { sync(player, false, "reset_limit"); }
    }
    public void reset(ServerPlayer player) {
        try { ProgressionProfile next = store.update(player.getUUID(), SkillCatalog::reset); SkillCatalog.apply(player, next); sync(player, false, "reset_done"); }
        catch (IOException e) { error(player, false, e); }
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
            for (var event : events) recordIsland(player, event.challenge(), event.token());
            if (next.equals(previous)) return false;
            notifyProgress(player, previous, next);
            sync(player, false, ""); return true;
        } catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }
    private static void recordIsland(ServerPlayer player, String id, String token) {
        try { WorldQoLService.recordMilestone(player.server, id, token); }
        catch (IOException | RuntimeException e) { LOGGER.error("Could not save island milestone {}", id, e); }
    }
    /** Read-only refresh; unlike opening the menu this does not retry transactions. */
    public void refreshMenu(ServerPlayer player) { sync(player, false, ""); }

    private void notifyProgress(ServerPlayer player, ProgressionProfile previous, ProgressionProfile next) {
        for (var milestone : MilestoneCatalog.MILESTONES)
            if (!previous.milestones().contains(milestone.id()) && next.milestones().contains(milestone.id()) && connected(player))
            {
                var override=de.tekatz.foreverskyblock.editor.EditorRuntime.quest(milestone.id());
                if(override!=null&&(override.rewardSp()!=milestone.reward()||override.rewardPp()>0))
                    player.sendSystemMessage(Component.literal(override.title().en()+" +"+override.rewardSp()+" SP / +"+override.rewardPp()+" PP"));
                else player.sendSystemMessage(SkillMessages.milestone(milestone));
            }
        if (connected(player) && next.meta().shopTier() > previous.meta().shopTier()) player.sendSystemMessage(Component.translatable(
                "message.forever_skyblock.tier_unlocked", Component.translatable("gui.forever_skyblock.meta.tier." + next.meta().shopTier())));
        for (var contract : ContractCatalog.all()) {
            long oldCount = previous.meta().contracts().getOrDefault(contract.id(), MetaProgress.ContractState.empty()).completions();
            long newCount = next.meta().contracts().getOrDefault(contract.id(), MetaProgress.ContractState.empty()).completions();
            if (newCount > oldCount && connected(player)) player.sendSystemMessage(Component.literal(contract.titleEn() + ": ")
                    .append(Component.translatable("message.forever_skyblock.skill_points", contract.rewardSp())));
        }
    }
    public boolean grant(ServerPlayer player, int amount) {
        try { store.update(player.getUUID(), state -> state.grant(amount)); sync(player, false, "points_granted"); return true; }
        catch (IOException | IllegalArgumentException | ArithmeticException e) { error(player, false, e); return false; }
    }

    private synchronized boolean recoverPendingContract(ServerPlayer player) throws IOException {
        ProgressionProfile profile = store.load(player.getUUID());
        if (profile.meta().pendingContracts().isEmpty()) return true;
        var entry = profile.meta().pendingContracts().entrySet().iterator().next();
        UUID request = entry.getKey(); MetaProgress.PendingContract pending = entry.getValue();
        Item item = contractItem(pending.itemId());
        if (item == null) return false;
        int current = inventoryCount(player, item);
        if (current == pending.beforeCount()) {
            if (!consume(player, item, pending.itemCount()) || inventoryCount(player, item) != pending.afterCount()) return false;
            player.server.getPlayerList().saveAll();
        } else if (current != pending.afterCount()) {
            LOGGER.error("Ambiguous contract recovery for {}: expected item count {} or {}, found {}", player.getUUID(),
                    pending.beforeCount(), pending.afterCount(), current);
            return false;
        }
        store.update(player.getUUID(), state -> ContractRules.finishTurnIn(state, request));
        return true;
    }

    private synchronized boolean recoverPendingDelivery(ServerPlayer player) throws IOException {
        ProgressionProfile profile = store.load(player.getUUID());
        for (var entry : profile.meta().deliveries().entrySet()) {
            if (entry.getValue().stage() == MetaProgress.DeliveryStage.DISPATCHING) return completeDispatch(player, entry.getKey());
            if (entry.getValue().stage() == MetaProgress.DeliveryStage.OPENING) return completeOpening(player, entry.getKey());
        }
        return true;
    }

    private boolean completeDispatch(ServerPlayer player, UUID receipt) throws IOException {
        ProgressionProfile profile = store.load(player.getUUID());
        MetaProgress.DeliveryReceipt delivery = profile.meta().deliveries().get(receipt);
        if (delivery == null) return false;
        if (delivery.stage() == MetaProgress.DeliveryStage.DELIVERED || delivery.stage() == MetaProgress.DeliveryStage.OPENING
                || delivery.stage() == MetaProgress.DeliveryStage.OPENED) return true;
        if (delivery.stage() != MetaProgress.DeliveryStage.DISPATCHING) return false;
        ShopCatalog.Product product = ShopCatalog.find(delivery.productId());
        if (product == null || !product.deliverable() || !productAvailable(product)) return false;
        ItemStack existing = findCache(player, receipt);
        if (existing == null) {
            int slot = firstEmptyMainSlot(player); if (slot < 0) return false;
            player.getInventory().setItem(slot, SupplyCacheItem.create(product, receipt, player.getUUID(), delivery.worldId()));
            player.getInventory().setChanged();
            player.server.getPlayerList().saveAll();
        }
        store.update(player.getUUID(), state -> state.finishDelivery(receipt));
        return true;
    }

    private boolean completeOpening(ServerPlayer player, UUID receipt) throws IOException {
        ProgressionProfile profile = store.load(player.getUUID());
        MetaProgress.DeliveryReceipt delivery = profile.meta().deliveries().get(receipt);
        if (delivery == null) return false;
        if (delivery.stage() == MetaProgress.DeliveryStage.OPENED) return true;
        if (delivery.stage() != MetaProgress.DeliveryStage.OPENING) return false;
        ShopCatalog.Product product = ShopCatalog.find(delivery.productId());
        if (product == null || !product.deliverable() || !productAvailable(product)) return false;
        ItemStack cache = findCache(player, receipt);
        if (cache != null) {
            SupplyCacheItem.Marker marker = SupplyCacheItem.marker(cache);
            if (marker == null || !marker.ownerId().equals(player.getUUID()) || !marker.worldId().equals(delivery.worldId())
                    || !marker.productId().equals(delivery.productId()) || !hasContentSpace(player, product, cache)) return false;
            List<ItemStack> contents = productStacks(product);
            if (contents == null) return false;
            cache.shrink(1);
            for (ItemStack stack : contents) {
                int slot = firstEmptyMainSlot(player);
                if (slot < 0) throw new IllegalStateException("Validated delivery space disappeared");
                player.getInventory().setItem(slot, stack);
            }
            player.getInventory().setChanged();
            player.server.getPlayerList().saveAll();
        }
        // If the cache is absent after an OPENING intent, the preceding player-data checkpoint
        // durably stored the consumed cache and package contents. Finalize rather than duplicate.
        store.update(player.getUUID(), state -> state.finishOpening(receipt));
        return true;
    }

    public static boolean productAvailable(ShopCatalog.Product product) { return product != null && (!product.deliverable() || productStacks(product) != null); }
    private static List<ItemStack> productStacks(ShopCatalog.Product product) {
        try {
            var result = new ArrayList<ItemStack>();
            for (var entry : product.contents()) {
                Item item = BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(entry.itemId())).orElse(null);
                if (item == null) return null;
                ItemStack stack = new ItemStack(item, entry.count());
                if (entry.count() > stack.getMaxStackSize()) return null;
                result.add(stack);
            }
            return List.copyOf(result);
        } catch (RuntimeException e) { return null; }
    }
    private static boolean hasContentSpace(ServerPlayer player, ShopCatalog.Product product, ItemStack cache) {
        int empty = 0; boolean cacheUsesMainSlot = false; var inventory = player.getInventory();
        for (int slot = 0; slot < Math.min(36, inventory.getContainerSize()); slot++) {
            ItemStack stack = inventory.getItem(slot);
            if (stack.isEmpty()) empty++;
            else if (stack == cache) cacheUsesMainSlot = true;
        }
        return empty + (cacheUsesMainSlot ? 1 : 0) >= product.contents().size();
    }
    private static int firstEmptyMainSlot(ServerPlayer player) {
        var inventory = player.getInventory();
        for (int slot = 0; slot < Math.min(36, inventory.getContainerSize()); slot++) if (inventory.getItem(slot).isEmpty()) return slot;
        return -1;
    }
    private static ItemStack findCache(ServerPlayer player, UUID receipt) {
        var inventory = player.getInventory();
        for (int slot = 0; slot < inventory.getContainerSize(); slot++) {
            ItemStack stack = inventory.getItem(slot); SupplyCacheItem.Marker marker = SupplyCacheItem.marker(stack);
            if (marker != null && marker.receiptId().equals(receipt)) return stack;
        }
        return null;
    }

    public static int inventoryCount(ServerPlayer player, String itemId) {
        Item item = contractItem(itemId); return item == null ? 0 : inventoryCount(player, item);
    }
    public static boolean contractItemAvailable(String itemId) { return contractItem(itemId) != null; }
    private static Item contractItem(String id) {
        try { return BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(id)).orElse(null); }
        catch (RuntimeException e) { return null; }
    }
    private static int inventoryCount(ServerPlayer player, Item item) {
        int count = 0; var inventory = player.getInventory();
        for (int slot = 0; slot < inventory.getContainerSize(); slot++) {
            var stack = inventory.getItem(slot); if (stack.is(item)) count = Math.addExact(count, stack.getCount());
        }
        return count;
    }
    private static boolean consume(ServerPlayer player, Item item, int amount) {
        if (amount < 1 || inventoryCount(player, item) < amount) return false;
        int remaining = amount; var inventory = player.getInventory();
        for (int slot = 0; slot < inventory.getContainerSize() && remaining > 0; slot++) {
            var stack = inventory.getItem(slot); if (!stack.is(item)) continue;
            int take = Math.min(remaining, stack.getCount()); stack.shrink(take); remaining -= take;
        }
        inventory.setChanged(); return remaining == 0;
    }
    private static boolean connected(ServerPlayer player) { return player.connection != null && player.connection.getConnection().isConnected(); }
    private UUID currentWorld() throws IOException {
        if (worldRoot == null) throw new IOException("World identity unavailable outside a running server");
        return worldIdentities.loadOrCreate(worldRoot);
    }
    private void sync(ServerPlayer player, boolean open, String message) {
        try {
            ProgressionProfile profile = store.load(player.getUUID()); SkillNetwork.WorldState world;
            try {
                UUID identity = currentWorld();
                world = new SkillNetwork.WorldState(true, worldName, profile.meta().worldClaims().getOrDefault(identity, java.util.Set.of()), identity);
            } catch (IOException e) {
                LOGGER.error("Could not read world identity for {}", player.getUUID(), e);
                world = new SkillNetwork.WorldState(false, worldName, java.util.Set.of());
            }
            SkillNetwork.send(player, profile, local, open, true, message, world);
            de.tekatz.foreverskyblock.editor.EditorService.get(player.server).sync(player, profile, open);
        } catch (IOException e) { error(player, open, e); }
    }
    private void error(ServerPlayer player, boolean open, Exception e) {
        LOGGER.error("Could not read or save progression for {}", player.getUUID(), e);
        if (connected(player)) player.sendSystemMessage(Component.translatable("gui.forever_skyblock.skills.storage_error"));
        SkillNetwork.send(player, ProgressionProfile.empty(), local, open, false, "storage_error",
                new SkillNetwork.WorldState(false, worldName, java.util.Set.of()));
    }
}

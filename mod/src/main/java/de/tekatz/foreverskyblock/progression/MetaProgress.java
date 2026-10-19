package de.tekatz.foreverskyblock.progression;

import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;

/** Bounded permanent challenge, accounting, world-entitlement, contract and delivery state. */
public record MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting,
                           Map<UUID, Set<String>> worldClaims, Map<UUID, PurchaseReceipt> purchaseReceipts,
                           Map<String, ContractState> contracts, Map<UUID, PendingContract> pendingContracts,
                           Map<UUID, DeliveryReceipt> deliveries) {
    public record Progress(int revision, Set<String> evidence) {
        public Progress {
            if (revision < 1 || revision > 10000 || evidence.size() > 1000) throw new IllegalArgumentException("Invalid objective state");
            evidence = Set.copyOf(evidence);
            for (String token : evidence) if (token.isBlank() || token.length() > 128) throw new IllegalArgumentException("Invalid objective evidence");
        }
        public static Progress empty() { return new Progress(1, Set.of()); }
    }
    public record Accounting(long spEarned, long ppEarned, long spSpent, long ppSpent,
                             long spRefunded, long ppRefunded, long spAdmin, boolean legacyHistoryUnknown) {
        public Accounting {
            for (long value : new long[]{spEarned, ppEarned, spSpent, ppSpent, spRefunded, ppRefunded, spAdmin})
                if (value < 0) throw new IllegalArgumentException("Invalid accounting total");
        }
        public static Accounting empty(boolean legacy) { return new Accounting(0, 0, 0, 0, 0, 0, 0, legacy); }
        public Accounting change(long earned, long perks, long spent, long perksSpent, long refund, long perksRefund, long admin) {
            return new Accounting(Math.addExact(spEarned, earned), Math.addExact(ppEarned, perks), Math.addExact(spSpent, spent),
                    Math.addExact(ppSpent, perksSpent), Math.addExact(spRefunded, refund), Math.addExact(ppRefunded, perksRefund),
                    Math.addExact(spAdmin, admin), legacyHistoryUnknown);
        }
    }
    public record PurchaseReceipt(UUID worldId, String productId, int spCost, int ppCost, int requiredTier) {
        public PurchaseReceipt {
            if (worldId == null) throw new IllegalArgumentException("Missing receipt world");
            ProgressionProfile.validateId(productId);
            if (spCost < 1 || spCost > ProgressionProfile.MAX_POINTS || ppCost < 0 || ppCost > ProgressionProfile.MAX_POINTS
                    || requiredTier < 0 || requiredTier > 4)
                throw new IllegalArgumentException("Invalid purchase receipt");
        }
        public PurchaseReceipt(UUID worldId, String productId, int spCost, int requiredTier) {
            this(worldId, productId, spCost, 0, requiredTier);
        }
    }
    public record ContractState(int revision, int progress, long completions,
                                List<String> recentEvidence, List<UUID> recentRequests) {
        public ContractState {
            if (revision < 1 || revision > 10000 || progress < 0 || progress > 1_000_000
                    || completions < 0 || completions > 1_000_000_000L
                    || recentEvidence.size() > 32 || recentRequests.size() > 32)
                throw new IllegalArgumentException("Invalid contract state");
            recentEvidence = List.copyOf(recentEvidence);
            recentRequests = List.copyOf(recentRequests);
            if (new java.util.HashSet<>(recentEvidence).size() != recentEvidence.size()
                    || new java.util.HashSet<>(recentRequests).size() != recentRequests.size())
                throw new IllegalArgumentException("Duplicate contract replay guard");
            for (String token : recentEvidence)
                if (token == null || token.isBlank() || token.length() > 128) throw new IllegalArgumentException("Invalid contract evidence");
        }
        public static ContractState empty() { return new ContractState(1, 0, 0, List.of(), List.of()); }
    }
    public record PendingContract(String contractId, String itemId, int itemCount, int beforeCount,
                                  int rewardSp, int requiredTier) {
        public PendingContract {
            ProgressionProfile.validateId(contractId);
            ProgressionProfile.validateId(itemId);
            if (itemCount < 1 || itemCount > 1_000_000 || beforeCount < itemCount || beforeCount > 1_000_000
                    || rewardSp < 1 || rewardSp > 10 || requiredTier < 0 || requiredTier > 4)
                throw new IllegalArgumentException("Invalid pending contract");
        }
        public int afterCount() { return beforeCount - itemCount; }
    }

    public enum DeliveryStage { READY, DISPATCHING, DELIVERED, OPENING, OPENED }

    /** Receipt key is the immutable purchase transaction UUID stored in purchaseReceipts. */
    public record DeliveryReceipt(UUID worldId, String productId, DeliveryStage stage) {
        public DeliveryReceipt {
            if (worldId == null || stage == null) throw new IllegalArgumentException("Invalid delivery receipt");
            ProgressionProfile.validateId(productId);
        }
    }

    public MetaProgress {
        if (shopTier < -1 || shopTier > 4 || progress.size() > 4096 || accounting == null
                || worldClaims.size() > 1024 || purchaseReceipts.size() > 4096
                || contracts.size() > 128 || pendingContracts.size() > 1 || deliveries.size() > 4096)
            throw new IllegalArgumentException("Invalid meta progression");
        progress = Map.copyOf(progress);
        progress.forEach((id, state) -> {
            ProgressionProfile.validateId(id);
            var definition = ChallengeRules.find(id);
            if (definition != null && state.revision() == 1) {
                if (state.evidence().size() > definition.target()) throw new IllegalArgumentException("Objective exceeds target");
                for (String token : state.evidence()) {
                    if (!definition.objectives().isEmpty() && !definition.objectives().contains(token)) throw new IllegalArgumentException("Invalid objective token");
                    if (definition == ChallengeRules.VETERAN && !java.util.UUID.fromString(token).toString().equals(token)) throw new IllegalArgumentException("Invalid victim UUID");
                }
            }
        });
        if (progress.values().stream().mapToInt(p -> p.evidence().size()).sum() > 8192) throw new IllegalArgumentException("Evidence limit reached");

        var claims = new java.util.LinkedHashMap<UUID, Set<String>>();
        int totalClaims = 0;
        for (var entry : worldClaims.entrySet()) {
            if (entry.getKey() == null || entry.getValue().size() > 128) throw new IllegalArgumentException("Invalid world claims");
            var values = Set.copyOf(entry.getValue());
            for (String id : values) ProgressionProfile.validateId(id);
            totalClaims = Math.addExact(totalClaims, values.size());
            claims.put(entry.getKey(), values);
        }
        if (totalClaims > 4096) throw new IllegalArgumentException("World claim limit reached");
        var finalizedClaims = Map.copyOf(claims);
        var finalizedReceipts = Map.copyOf(purchaseReceipts);
        for (var entry : finalizedReceipts.entrySet()) {
            var transaction = entry.getKey(); var receipt = entry.getValue();
            if (transaction == null || receipt == null || !finalizedClaims.getOrDefault(receipt.worldId(), Set.of()).contains(receipt.productId()))
                throw new IllegalArgumentException("Receipt has no matching world claim");
        }
        worldClaims = finalizedClaims;
        purchaseReceipts = finalizedReceipts;

        contracts = Map.copyOf(contracts);
        contracts.forEach((id, state) -> {
            ProgressionProfile.validateId(id);
            if (state == null) throw new IllegalArgumentException("Missing contract state");
            var definition = ContractCatalog.find(id);
            if (definition != null && !de.tekatz.foreverskyblock.editor.EditorRuntime.customContract(id) && state.revision() == 1 && state.progress() >= definition.target())
                throw new IllegalArgumentException("Completed contract progress must reset before persistence");
        });
        pendingContracts = Map.copyOf(pendingContracts);
        pendingContracts.forEach((id, pending) -> {
            if (id == null || pending == null) throw new IllegalArgumentException("Invalid pending contract entry");
        });

        deliveries = Map.copyOf(deliveries);
        int activeDelivery = 0;
        for (var entry : deliveries.entrySet()) {
            UUID transaction = entry.getKey(); DeliveryReceipt delivery = entry.getValue();
            PurchaseReceipt purchase = purchaseReceipts.get(transaction);
            if (transaction == null || delivery == null || purchase == null
                    || !purchase.worldId().equals(delivery.worldId()) || !purchase.productId().equals(delivery.productId()))
                throw new IllegalArgumentException("Delivery has no matching purchase receipt");
            if (delivery.stage() == DeliveryStage.DISPATCHING || delivery.stage() == DeliveryStage.OPENING) activeDelivery++;
        }
        if (activeDelivery > 1) throw new IllegalArgumentException("Multiple active delivery transactions");
    }

    public MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting,
                        Map<UUID, Set<String>> worldClaims, Map<UUID, PurchaseReceipt> purchaseReceipts,
                        Map<String, ContractState> contracts, Map<UUID, PendingContract> pendingContracts) {
        this(shopTier, progress, accounting, worldClaims, purchaseReceipts, contracts, pendingContracts, Map.of());
    }
    public MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting,
                        Map<UUID, Set<String>> worldClaims, Map<UUID, PurchaseReceipt> purchaseReceipts) {
        this(shopTier, progress, accounting, worldClaims, purchaseReceipts, Map.of(), Map.of(), Map.of());
    }
    public MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting) {
        this(shopTier, progress, accounting, Map.of(), Map.of(), Map.of(), Map.of(), Map.of());
    }
    public static MetaProgress empty(boolean legacy) {
        return new MetaProgress(-1, Map.of(), Accounting.empty(legacy), Map.of(), Map.of(), Map.of(), Map.of(), Map.of());
    }
    public MetaProgress withProgress(Map<String, Progress> value) {
        return new MetaProgress(shopTier, value, accounting, worldClaims, purchaseReceipts, contracts, pendingContracts, deliveries);
    }
    public MetaProgress withTier(int qualified) {
        return new MetaProgress(Math.max(shopTier, qualified), progress, accounting, worldClaims, purchaseReceipts, contracts, pendingContracts, deliveries);
    }
    public MetaProgress withContracts(Map<String, ContractState> value) {
        return new MetaProgress(shopTier, progress, accounting, worldClaims, purchaseReceipts, value, pendingContracts, deliveries);
    }
    public MetaProgress withPendingContracts(Map<UUID, PendingContract> value) {
        return new MetaProgress(shopTier, progress, accounting, worldClaims, purchaseReceipts, contracts, value, deliveries);
    }
    public MetaProgress withDeliveries(Map<UUID, DeliveryReceipt> value) {
        return new MetaProgress(shopTier, progress, accounting, worldClaims, purchaseReceipts, contracts, pendingContracts, value);
    }
    public MetaProgress account(long earned, long perks, long spent, long perksSpent, long refund, long perksRefund, long admin) {
        return new MetaProgress(shopTier, progress, accounting.change(earned, perks, spent, perksSpent, refund, perksRefund, admin),
                worldClaims, purchaseReceipts, contracts, pendingContracts, deliveries);
    }
    public boolean claimed(UUID world, String product) { return worldClaims.getOrDefault(world, Set.of()).contains(product); }
    public Map.Entry<UUID, DeliveryReceipt> delivery(UUID world, String product) {
        for (var entry : deliveries.entrySet())
            if (entry.getValue().worldId().equals(world) && entry.getValue().productId().equals(product)) return entry;
        return null;
    }
}

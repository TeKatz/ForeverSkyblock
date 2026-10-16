package de.tekatz.foreverskyblock.progression;

import java.util.Map;
import java.util.Set;
import java.util.UUID;

/** Bounded permanent challenge, accounting and current-world entitlement state. */
public record MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting,
                           Map<UUID, Set<String>> worldClaims, Map<UUID, PurchaseReceipt> purchaseReceipts) {
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
    public record PurchaseReceipt(UUID worldId, String productId, int spCost, int requiredTier) {
        public PurchaseReceipt {
            if (worldId == null) throw new IllegalArgumentException("Missing receipt world");
            ProgressionProfile.validateId(productId);
            if (spCost < 1 || spCost > ProgressionProfile.MAX_POINTS || requiredTier < 0 || requiredTier > 4)
                throw new IllegalArgumentException("Invalid purchase receipt");
        }
    }
    public MetaProgress {
        if (shopTier < -1 || shopTier > 4 || progress.size() > 4096 || accounting == null
                || worldClaims.size() > 1024 || purchaseReceipts.size() > 4096)
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
        worldClaims = Map.copyOf(claims);
        purchaseReceipts = Map.copyOf(purchaseReceipts);
        purchaseReceipts.forEach((transaction, receipt) -> {
            if (transaction == null || receipt == null || !worldClaims.getOrDefault(receipt.worldId(), Set.of()).contains(receipt.productId()))
                throw new IllegalArgumentException("Receipt has no matching world claim");
        });
    }
    public MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting) {
        this(shopTier, progress, accounting, Map.of(), Map.of());
    }
    public static MetaProgress empty(boolean legacy) { return new MetaProgress(-1, Map.of(), Accounting.empty(legacy), Map.of(), Map.of()); }
    public MetaProgress withProgress(Map<String, Progress> value) { return new MetaProgress(shopTier, value, accounting, worldClaims, purchaseReceipts); }
    public MetaProgress withTier(int qualified) { return new MetaProgress(Math.max(shopTier, qualified), progress, accounting, worldClaims, purchaseReceipts); }
    public MetaProgress account(long earned, long perks, long spent, long perksSpent, long refund, long perksRefund, long admin) {
        return new MetaProgress(shopTier, progress, accounting.change(earned, perks, spent, perksSpent, refund, perksRefund, admin), worldClaims, purchaseReceipts);
    }
    public boolean claimed(UUID world, String product) { return worldClaims.getOrDefault(world, Set.of()).contains(product); }
}

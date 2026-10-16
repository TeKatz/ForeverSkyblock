package de.tekatz.foreverskyblock.progression;

import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;

/** Immutable profile snapshots keep failed saves and duplicate requests from changing the balance. */
public record ProgressionProfile(int points, Set<String> milestones, Map<String, Integer> skills,
                                 int perkPoints, Map<String, Integer> perkInvestments, Set<java.util.UUID> redeemedCores, MetaProgress meta) {
    public static final int MAX_POINTS = 1_000_000;
    public ProgressionProfile {
        if (points < 0 || points > MAX_POINTS || perkPoints < 0 || perkPoints > MAX_POINTS) throw new IllegalArgumentException("Invalid point balance");
        milestones = Set.copyOf(milestones);
        meta = meta.withTier(ChallengeRules.qualifiedTier(milestones));
        skills = Map.copyOf(skills);
        perkInvestments = Map.copyOf(perkInvestments);
        redeemedCores = Set.copyOf(redeemedCores);
        if (redeemedCores.size() > 16384) throw new IllegalArgumentException("Core receipt limit reached");
        for (var entry : perkInvestments.entrySet()) {
            validateId(entry.getKey());
            if (!skills.containsKey(entry.getKey()) || entry.getValue() < 1 || entry.getValue() > MAX_POINTS)
                throw new IllegalArgumentException("Invalid perk investment");
        }
        if (milestones.size() > 4096 || skills.size() > 128) throw new IllegalArgumentException("Profile too large");
        for (String id : milestones) validateId(id);
        skills.forEach((id, rank) -> {
            validateId(id);
            if (rank < 1 || rank > 100) throw new IllegalArgumentException("Invalid skill rank");
        });
    }
    public ProgressionProfile(int points, Set<String> milestones, Map<String, Integer> skills,
                              int perkPoints, Map<String, Integer> perkInvestments, Set<java.util.UUID> redeemedCores) {
        this(points, milestones, skills, perkPoints, perkInvestments, redeemedCores, MetaProgress.empty(true));
    }
    public ProgressionProfile withMeta(MetaProgress value) {
        return new ProgressionProfile(points, milestones, skills, perkPoints, perkInvestments, redeemedCores, value);
    }
    public ProgressionProfile(int points, Set<String> milestones, Map<String, Integer> skills) {
        this(points, milestones, skills, 0, Map.of(), Set.of());
    }
    public ProgressionProfile redeem(java.util.UUID core) {
        if (redeemedCores.contains(core)) return this;
        var receipts = new LinkedHashSet<>(redeemedCores);
        receipts.add(core);
        return new ProgressionProfile(points, milestones, skills, Math.addExact(perkPoints, 1), perkInvestments, receipts,
                meta.account(0, 1, 0, 0, 0, 0, 0));
    }
    public static ProgressionProfile empty() { return new ProgressionProfile(0, Set.of(), Map.of(), 0, Map.of(), Set.of(), MetaProgress.empty(false)); }
    public int rank(String id) { return skills.getOrDefault(id, 0); }
    public ProgressionProfile award(String id, int reward) {
        validateId(id);
        if (reward < 1 || reward > MAX_POINTS) throw new IllegalArgumentException("Invalid reward");
        if (milestones.contains(id)) return this;
        var reached = new LinkedHashSet<>(milestones);
        reached.add(id);
        return new ProgressionProfile(Math.addExact(points, reward), reached, skills, perkPoints, perkInvestments, redeemedCores,
                meta.account(reward, 0, 0, 0, 0, 0, 0));
    }
    ProgressionProfile repeatableReward(int reward) {
        if (reward < 1 || reward > 10) throw new IllegalArgumentException("Invalid repeatable reward");
        return new ProgressionProfile(Math.addExact(points, reward), milestones, skills, perkPoints, perkInvestments, redeemedCores,
                meta.account(reward, 0, 0, 0, 0, 0, 0));
    }
    public ProgressionProfile purchase(String id, int cost, int maximumRank) { return purchase(id, cost, 0, maximumRank); }
    public ProgressionProfile purchase(String id, int cost, int perkCost, int maximumRank) {
        validateId(id);
        if (perkCost < 0) throw new IllegalArgumentException("Invalid perk cost");
        if (cost < 1 || maximumRank < 1 || maximumRank > 100) throw new IllegalArgumentException("Invalid skill definition");
        if (points < cost || perkPoints < perkCost || rank(id) >= maximumRank) return this;
        var purchased = new LinkedHashMap<>(skills); purchased.put(id, rank(id) + 1);
        var paid = new LinkedHashMap<>(perkInvestments); if (perkCost > 0) paid.merge(id, perkCost, Math::addExact);
        return new ProgressionProfile(points - cost, milestones, purchased, perkPoints - perkCost, paid, redeemedCores,
                meta.account(0, 0, cost, perkCost, 0, 0, 0));
    }
    public ProgressionProfile purchaseWorld(java.util.UUID world, ShopCatalog.Product product, java.util.UUID transaction) {
        if (world == null || transaction == null || product == null) throw new IllegalArgumentException("Invalid world purchase");
        if (meta.purchaseReceipts().containsKey(transaction) || meta.claimed(world, product.id())
                || meta.shopTier() < product.requiredTier() || points < product.spCost()) return this;
        var claims = new LinkedHashMap<java.util.UUID, Set<String>>(meta.worldClaims());
        var products = new LinkedHashSet<>(claims.getOrDefault(world, Set.of())); products.add(product.id()); claims.put(world, Set.copyOf(products));
        var receipts = new LinkedHashMap<>(meta.purchaseReceipts());
        receipts.put(transaction, new MetaProgress.PurchaseReceipt(world, product.id(), product.spCost(), product.requiredTier()));
        var nextMeta = new MetaProgress(meta.shopTier(), meta.progress(),
                meta.accounting().change(0, 0, product.spCost(), 0, 0, 0, 0), claims, receipts, meta.contracts(), meta.pendingContracts());
        return new ProgressionProfile(points - product.spCost(), milestones, skills, perkPoints, perkInvestments, redeemedCores, nextMeta);
    }
    /** Refund known purchases without clearing completed milestones, contracts, tiers or unknown future skill data. */
    public ProgressionProfile resetSkills(Map<String, Integer> costs) {
        var remaining = new LinkedHashMap<>(skills);
        int refunded = points, perksRefunded = perkPoints;
        var paid = new LinkedHashMap<>(perkInvestments);
        for (var entry : costs.entrySet()) {
            validateId(entry.getKey());
            if (entry.getValue() < 1) throw new IllegalArgumentException("Invalid skill cost");
            Integer rank = remaining.remove(entry.getKey());
            if (rank != null) {
                refunded = Math.addExact(refunded, Math.multiplyExact(rank, entry.getValue()));
                perksRefunded = Math.addExact(perksRefunded, paid.getOrDefault(entry.getKey(), 0));
                paid.remove(entry.getKey());
            }
        }
        return new ProgressionProfile(refunded, milestones, remaining, perksRefunded, paid, redeemedCores,
                meta.account(0, 0, 0, 0, refunded - points, perksRefunded - perkPoints, 0));
    }
    public ProgressionProfile grant(int amount) {
        if (amount < 1) throw new IllegalArgumentException("Invalid grant");
        return new ProgressionProfile(Math.addExact(points, amount), milestones, skills, perkPoints, perkInvestments, redeemedCores,
                meta.account(0, 0, 0, 0, 0, 0, amount));
    }
    public static void validateId(String id) {
        if (id == null || id.length() > 128 || !id.matches("[a-z0-9_.-]+:[a-z0-9_./-]+"))
            throw new IllegalArgumentException("Invalid progression identifier");
    }
}

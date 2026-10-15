package de.tekatz.foreverskyblock.progression;

import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;

/** Immutable profile snapshots keep failed saves and duplicate requests from changing the balance. */
public record ProgressionProfile(int points, Set<String> milestones, Map<String, Integer> skills) {
    public static final int MAX_POINTS = 1_000_000;
    public ProgressionProfile {
        if (points < 0 || points > MAX_POINTS) throw new IllegalArgumentException("Invalid point balance");
        milestones = Set.copyOf(milestones);
        skills = Map.copyOf(skills);
        if (milestones.size() > 4096 || skills.size() > 128) throw new IllegalArgumentException("Profile too large");
        for (String id : milestones) validateId(id);
        skills.forEach((id, rank) -> {
            validateId(id);
            if (rank < 1 || rank > 100) throw new IllegalArgumentException("Invalid skill rank");
        });
    }
    public static ProgressionProfile empty() { return new ProgressionProfile(0, Set.of(), Map.of()); }
    public int rank(String id) { return skills.getOrDefault(id, 0); }
    public ProgressionProfile award(String id, int reward) {
        validateId(id);
        if (reward < 1 || reward > MAX_POINTS) throw new IllegalArgumentException("Invalid reward");
        if (milestones.contains(id)) return this;
        var reached = new LinkedHashSet<>(milestones);
        reached.add(id);
        return new ProgressionProfile(Math.addExact(points, reward), reached, skills);
    }
    public ProgressionProfile purchase(String id, int cost, int maximumRank) {
        validateId(id);
        if (cost < 1 || maximumRank < 1 || maximumRank > 100) throw new IllegalArgumentException("Invalid skill definition");
        if (points < cost || rank(id) >= maximumRank) return this;
        var purchased = new LinkedHashMap<>(skills);
        purchased.put(id, rank(id) + 1);
        return new ProgressionProfile(points - cost, milestones, purchased);
    }
    public ProgressionProfile grant(int amount) {
        if (amount < 1) throw new IllegalArgumentException("Invalid grant");
        return new ProgressionProfile(Math.addExact(points, amount), milestones, skills);
    }
    public static void validateId(String id) {
        if (id == null || id.length() > 128 || !id.matches("[a-z0-9_.-]+:[a-z0-9_./-]+"))
            throw new IllegalArgumentException("Invalid progression identifier");
    }
}

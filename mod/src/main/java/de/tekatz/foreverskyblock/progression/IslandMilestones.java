package de.tekatz.foreverskyblock.progression;

import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;

/** Shared, world-local evidence. Never awards currency or imports permanent profile history. */
public final class IslandMilestones {
    private IslandMilestones() { }

    public static Map<String, Set<String>> record(Map<String, Set<String>> previous, String id, String token) {
        var definition = ChallengeRules.find(id);
        if (definition == null || token == null) return previous;
        if (!definition.objectives().isEmpty() && !definition.objectives().contains(token)) return previous;
        if (definition == ChallengeRules.VETERAN) {
            try { if (!java.util.UUID.fromString(token).toString().equals(token)) return previous; }
            catch (IllegalArgumentException e) { return previous; }
        }
        var tokens = previous.getOrDefault(id, Set.of());
        if (tokens.size() >= definition.target() || tokens.contains(token)) return previous;
        var nextTokens = new LinkedHashSet<>(tokens);
        nextTokens.add(token);
        var next = new LinkedHashMap<>(previous);
        next.put(id, Set.copyOf(nextTokens));
        return Map.copyOf(next);
    }

    public static int level(Map<String, Set<String>> evidence) {
        return (int) ChallengeRules.ALL.stream()
                .filter(d -> evidence.getOrDefault(d.id(), Set.of()).size() >= d.target()).count();
    }
}

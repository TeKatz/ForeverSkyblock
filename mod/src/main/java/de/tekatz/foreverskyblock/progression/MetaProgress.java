package de.tekatz.foreverskyblock.progression;

import java.util.Map;
import java.util.Set;

/** Phase-2 state only. World purchases and contracts will introduce their own validated records later. */
public record MetaProgress(int shopTier, Map<String, Progress> progress, Accounting accounting) {
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
    public MetaProgress {
        if (shopTier < -1 || shopTier > 4 || progress.size() > 4096 || accounting == null) throw new IllegalArgumentException("Invalid meta progression");
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
    }
    public static MetaProgress empty(boolean legacy) { return new MetaProgress(-1, Map.of(), Accounting.empty(legacy)); }
    public MetaProgress withProgress(Map<String, Progress> value) { return new MetaProgress(shopTier, value, accounting); }
    public MetaProgress withTier(int qualified) { return new MetaProgress(Math.max(shopTier, qualified), progress, accounting); }
    public MetaProgress account(long earned, long perks, long spent, long perksSpent, long refund, long perksRefund, long admin) {
        return new MetaProgress(shopTier, progress, accounting.change(earned, perks, spent, perksSpent, refund, perksRefund, admin));
    }
}

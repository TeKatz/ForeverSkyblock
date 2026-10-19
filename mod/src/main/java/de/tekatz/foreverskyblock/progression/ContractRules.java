package de.tekatz.foreverskyblock.progression;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.UUID;

/** Pure reducers for repeatable contracts. Inventory I/O is deliberately kept in ProgressionService. */
public final class ContractRules {
    private static final int REPLAY_GUARD = 32;
    private ContractRules() { }

    public static ProgressionProfile recordBoss(ProgressionProfile profile, ChallengeRules.Definition unique,
                                                ContractCatalog.Contract contract, String evidence) {
        contract = ContractCatalog.find(contract.id());
        if (contract == null || !de.tekatz.foreverskyblock.editor.EditorRuntime.contractEnabled(contract.id())) return ChallengeRules.record(profile, unique.id(), "completed");
        boolean uniqueAlreadyCompleted = profile.milestones().contains(unique.id());
        ProgressionProfile next = ChallengeRules.record(profile, unique.id(), "completed");
        if (!uniqueAlreadyCompleted || next.meta().shopTier() < contract.requiredTier()) return next;
        return recordAction(next, contract, evidence);
    }

    public static ProgressionProfile recordAction(ProgressionProfile profile, ContractCatalog.Contract contract, String evidence) {
        if (!de.tekatz.foreverskyblock.editor.EditorRuntime.contractEnabled(contract.id()) || contract.resource() || profile.meta().shopTier() < contract.requiredTier()
                || evidence == null || evidence.isBlank() || evidence.length() > 128) return profile;
        var previous = profile.meta().contracts().getOrDefault(contract.id(), MetaProgress.ContractState.empty());
        if (previous.revision() != 1 || previous.recentEvidence().contains(evidence)) return profile;
        int progress = previous.progress() + 1;
        long completions = previous.completions();
        boolean payout = progress >= contract.target();
        if (payout) { progress = 0; completions = Math.addExact(completions, 1); }
        var updated = new MetaProgress.ContractState(1, progress, completions,
                push(previous.recentEvidence(), evidence), previous.recentRequests());
        var states = new LinkedHashMap<>(profile.meta().contracts()); states.put(contract.id(), updated);
        ProgressionProfile next = profile.withMeta(profile.meta().withContracts(states));
        return payout ? next.repeatableReward(contract.rewardSp()) : next;
    }

    public static ProgressionProfile beginTurnIn(ProgressionProfile profile, ContractCatalog.Contract contract,
                                                  UUID request, int beforeCount) {
        if (!de.tekatz.foreverskyblock.editor.EditorRuntime.contractEnabled(contract.id()) || !contract.resource() || request == null || profile.meta().shopTier() < contract.requiredTier()
                || beforeCount < contract.target() || !profile.meta().pendingContracts().isEmpty()) return profile;
        var state = profile.meta().contracts().getOrDefault(contract.id(), MetaProgress.ContractState.empty());
        if (state.revision() != 1 || state.recentRequests().contains(request)) return profile;
        var pending = new LinkedHashMap<>(profile.meta().pendingContracts());
        pending.put(request, new MetaProgress.PendingContract(contract.id(), contract.itemId(), contract.target(), beforeCount,
                contract.rewardSp(), contract.requiredTier()));
        return profile.withMeta(profile.meta().withPendingContracts(pending));
    }

    public static ProgressionProfile finishTurnIn(ProgressionProfile profile, UUID request) {
        var pending = profile.meta().pendingContracts().get(request);
        if (pending == null) return profile;
        var previous = profile.meta().contracts().getOrDefault(pending.contractId(), MetaProgress.ContractState.empty());
        if (previous.revision() != 1) return profile;
        var updated = new MetaProgress.ContractState(1, previous.progress(), Math.addExact(previous.completions(), 1),
                previous.recentEvidence(), push(previous.recentRequests(), request));
        var states = new LinkedHashMap<>(profile.meta().contracts()); states.put(pending.contractId(), updated);
        var pendingMap = new LinkedHashMap<>(profile.meta().pendingContracts()); pendingMap.remove(request);
        var meta = profile.meta().withContracts(states).withPendingContracts(pendingMap);
        return profile.withMeta(meta).repeatableReward(pending.rewardSp());
    }

    private static <T> List<T> push(List<T> previous, T value) {
        if (previous.contains(value)) return previous;
        var result = new ArrayList<>(previous); result.add(value);
        while (result.size() > REPLAY_GUARD) result.remove(0);
        return List.copyOf(result);
    }
}

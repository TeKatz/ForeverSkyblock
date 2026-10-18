package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.Test;

class IslandMilestonesTest {
    @Test void sharedObjectivesCountOnceWithoutCurrencyOrProfileHistory() {
        Map<String, Set<String>> evidence = Map.of();
        for (String tool : ChallengeRules.STONE_TOOLS.objectives())
            evidence = IslandMilestones.record(evidence, ChallengeRules.STONE_TOOLS.id(), tool);
        assertEquals(1, IslandMilestones.level(evidence));
        assertSame(evidence, IslandMilestones.record(evidence, ChallengeRules.STONE_TOOLS.id(), "minecraft:stone_pickaxe"));
        assertEquals(0, IslandMilestones.level(Map.of()));
    }
    @Test void rejectsUnknownAndForgedTokensAndBoundsBossEvidence() {
        Map<String, Set<String>> empty = Map.of();
        assertSame(empty, IslandMilestones.record(empty, "forever_skyblock:missing", "completed"));
        assertSame(empty, IslandMilestones.record(empty, ChallengeRules.FOUNDATIONS.id(), "wrong"));
        assertSame(empty, IslandMilestones.record(empty, ChallengeRules.VETERAN.id(), "not a uuid"));
        var next = IslandMilestones.record(empty, ChallengeRules.DRAGON.id(), "completed");
        assertEquals(1, IslandMilestones.level(next));
        assertSame(next, IslandMilestones.record(next, ChallengeRules.DRAGON.id(), "completed"));
        var veteran = IslandMilestones.record(empty, ChallengeRules.VETERAN.id(), UUID.randomUUID().toString());
        assertEquals(0, IslandMilestones.level(veteran));
    }
}

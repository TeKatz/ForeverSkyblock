package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class JetpackResearchTest {
    @TempDir Path root;
    @Test void materialTiersAndCreativeAreSeparated() {
        assertEquals(1, JetpackResearch.requiredStage(0, false));
        assertEquals(1, JetpackResearch.requiredStage(1, false));
        assertEquals(2, JetpackResearch.requiredStage(2, false));
        assertEquals(2, JetpackResearch.requiredStage(3, false));
        assertEquals(3, JetpackResearch.requiredStage(4, false));
        assertEquals(3, JetpackResearch.requiredStage(99, false));
        assertEquals(4, JetpackResearch.requiredStage(0, true));
        assertEquals(4, JetpackResearch.requiredStage(-1, false));
    }
    @Test void stagesAreSequentialAndRefundsPersistWithoutMilestoneReset() throws Exception {
        UUID id = UUID.randomUUID(); ProfileStore store = new ProfileStore(root);
        var costs = new LinkedHashMap<String, Integer>();
        var profile = new ProgressionProfile(100, Set.of("forever_skyblock:first_foundations"), Map.of());
        store.update(id, before -> profile);
        for (int i = 0; i < 4; i++) {
            String skill = JetpackResearch.IDS.get(i); int cost = JetpackResearch.COSTS.get(i);
            costs.put(skill, cost);
            assertEquals(i, JetpackResearch.stage(store.load(id)::rank));
            store.update(id, state -> state.purchase(skill, cost, 1));
            assertEquals(i + 1, JetpackResearch.stage(new ProfileStore(root).load(id)::rank));
        }
        assertEquals(25, store.load(id).points());
        var reset = store.update(id, state -> state.resetSkills(costs));
        assertEquals(100, reset.points()); assertEquals(0, JetpackResearch.stage(reset::rank));
        assertTrue(reset.milestones().contains("forever_skyblock:first_foundations"));
        assertEquals(reset, store.update(id, state -> state.resetSkills(costs)));
    }
    @Test void aLaterRankAloneDoesNotGrantEarlierStages() {
        var invalid = new ProgressionProfile(0, Set.of(), Map.of(JetpackResearch.IDS.get(3), 1));
        assertEquals(0, JetpackResearch.stage(invalid::rank));
        assertEquals(JetpackResearch.IDS.get(1), JetpackResearch.prerequisite(JetpackResearch.IDS.get(2)));
    }
}

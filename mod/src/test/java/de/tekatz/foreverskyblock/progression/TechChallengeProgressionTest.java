package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.util.List;
import org.junit.jupiter.api.Test;

class TechChallengeProgressionTest {
    @Test void phaseFourCatalogAddsOnlyApprovedBoundedChallenges() {
        List<ChallengeRules.Definition> tech = List.of(ChallengeRules.AE2_STORAGE, ChallengeRules.AE2_AUTOCRAFT,
                ChallengeRules.AE2_CRAFTING_BRAIN, ChallengeRules.AE2_WIRELESS, ChallengeRules.AE2_STORAGE_ARCHITECT,
                ChallengeRules.MEKANIZED, ChallengeRules.DIGITAL_EXTRACTION, ChallengeRules.ENERGIZED, ChallengeRules.SUPREMIUM);
        assertEquals(9, tech.size());
        assertEquals(24, tech.stream().mapToInt(ChallengeRules.Definition::reward).sum());
        assertEquals(20, ChallengeRules.ALL.size());
        assertEquals(58, ChallengeRules.ALL.stream().mapToInt(ChallengeRules.Definition::reward).sum());
        assertNull(ChallengeRules.find("forever_skyblock:quantum_link"));
        assertNull(ChallengeRules.find("forever_skyblock:industrial_mastery"));
        assertNull(ChallengeRules.find("forever_skyblock:mythic_power"));
    }

    @Test void exactTechnologyEvidencePaysOnceAndWrongIdsDoNot() {
        var p = ProgressionProfile.empty();
        assertEquals(p, ChallengeRules.record(p, ChallengeRules.AE2_STORAGE.id(), "ae2:storage_cell_4k"));
        p = ChallengeRules.record(p, ChallengeRules.AE2_STORAGE.id(), "ae2:item_storage_cell_4k");
        assertEquals(1, p.points());
        assertTrue(p.milestones().contains(ChallengeRules.AE2_STORAGE.id()));
        assertEquals(p, ChallengeRules.record(p, ChallengeRules.AE2_STORAGE.id(), "ae2:item_storage_cell_256k"));
        p = ChallengeRules.record(p, ChallengeRules.AE2_STORAGE_ARCHITECT.id(), "ae2:item_storage_cell_256k");
        assertEquals(5, p.points());
        p = ChallengeRules.record(p, ChallengeRules.SUPREMIUM.id(), "mysticalagriculture:supremium_essence");
        assertEquals(9, p.points());
        assertEquals(9, p.meta().accounting().spEarned());
    }

    @Test void technologyProgressDoesNotChangePermanentTierRules() {
        var p = ProgressionProfile.empty();
        for (var d : List.of(ChallengeRules.AE2_AUTOCRAFT, ChallengeRules.DIGITAL_EXTRACTION, ChallengeRules.ENERGIZED))
            p = ChallengeRules.record(p, d.id(), "completed");
        assertEquals(9, p.points());
        assertEquals(-1, p.meta().shopTier());
        p = ChallengeRules.record(p, ChallengeRules.FOUNDATIONS.id(), "completed");
        assertEquals(0, p.meta().shopTier());
        p = ChallengeRules.record(p, ChallengeRules.DRAGON.id(), "completed");
        assertEquals(1, p.meta().shopTier());
    }
}

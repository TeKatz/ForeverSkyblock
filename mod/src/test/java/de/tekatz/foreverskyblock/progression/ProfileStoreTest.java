package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class ProfileStoreTest {
    @TempDir Path root;
    private static final String MILESTONE = "forever_skyblock:first_foundations";
    private static final String SKILL = "forever_skyblock:extended_reach";
    @Test void milestoneIsOnceAcrossReloadsAndWorldDeletion() throws Exception {
        UUID id = UUID.randomUUID();
        Path profiles = root.resolve("profiles");
        Files.createDirectories(root.resolve("saves/world-a"));
        ProfileStore first = new ProfileStore(profiles);
        assertEquals(1, first.update(id, state -> state.award(MILESTONE, 1)).points());
        Files.delete(root.resolve("saves/world-a"));
        Files.createDirectories(root.resolve("saves/world-b"));
        ProfileStore second = new ProfileStore(profiles);
        assertEquals(1, second.update(id, state -> state.award(MILESTONE, 1)).points());
        assertEquals(1, second.load(id).milestones().size());
    }
    @Test void purchasePersistsAndCannotBeChargedTwice() throws Exception {
        UUID id = UUID.randomUUID();
        ProfileStore store = new ProfileStore(root);
        store.update(id, state -> state.award(MILESTONE, 1));
        var purchased = store.update(id, state -> state.purchase(SKILL, 1, 1));
        assertEquals(0, purchased.points()); assertEquals(1, purchased.rank(SKILL));
        ProfileStore reloaded = new ProfileStore(root);
        assertEquals(purchased, reloaded.load(id));
        assertEquals(purchased, reloaded.update(id, state -> state.purchase(SKILL, 1, 1)));
    }
    @Test void insufficientFundsCannotUnlockOrModifyBalance() throws Exception {
        UUID id = UUID.randomUUID();
        ProfileStore store = new ProfileStore(root);
        assertEquals(ProgressionProfile.empty(), store.update(id, state -> state.purchase(SKILL, 1, 1)));
        assertFalse(Files.exists(store.file(id)));
    }
    @Test void playersHaveIndependentProfiles() throws Exception {
        UUID first = UUID.randomUUID(), second = UUID.randomUUID();
        ProfileStore store = new ProfileStore(root);
        store.update(first, state -> state.award(MILESTONE, 1));
        assertEquals(0, store.load(second).points());
        assertEquals(1, new ProfileStore(root).load(first).points());
    }
    @Test void corruptPrimaryDoesNotSilentlyRollbackToStaleBackup() throws Exception {
        UUID id = UUID.randomUUID(); ProfileStore store = new ProfileStore(root);
        store.update(id, state -> state.award(MILESTONE, 1));
        store.update(id, state -> state.purchase(SKILL, 1, 1));
        String previousBackup = Files.readString(root.resolve(id + ".json.bak"));
        Files.writeString(store.file(id), "corrupted");
        assertThrows(IOException.class, () -> new ProfileStore(root).load(id));
        assertEquals("corrupted", Files.readString(store.file(id)));
        assertEquals(previousBackup, Files.readString(root.resolve(id + ".json.bak")));
        Files.delete(store.file(id));
        assertThrows(IOException.class, () -> new ProfileStore(root).load(id));
    }
    @Test void corruptOrFutureProfilesAreNeverSilentlyReset() throws Exception {
        UUID id = UUID.randomUUID();
        ProfileStore store = new ProfileStore(root);
        Files.writeString(store.file(id), "corrupt");
        assertThrows(IOException.class, () -> store.update(id, state -> state.award(MILESTONE, 1)));
        assertEquals("corrupt", Files.readString(store.file(id)));
        Files.writeString(store.file(id), "{\"schema_version\":4}");
        assertThrows(IOException.class, () -> new ProfileStore(root).update(id, state -> state.award(MILESTONE, 1)));
        assertEquals("{\"schema_version\":4}", Files.readString(store.file(id)));
    }
    @Test void failedWriteDoesNotCommitAnInMemoryReward() throws Exception {
        Path blocked = root.resolve("blocked"); Files.writeString(blocked, "not a directory");
        UUID id = UUID.randomUUID(); ProfileStore store = new ProfileStore(blocked);
        assertThrows(IOException.class, () -> store.update(id, state -> state.award(MILESTONE, 1)));
        assertEquals(0, store.load(id).points());
    }
    @Test void concurrentDuplicateClaimsOnlyEarnOneReward() throws Exception {
        UUID id = UUID.randomUUID(); ProfileStore store = new ProfileStore(root);
        try (var executor = java.util.concurrent.Executors.newFixedThreadPool(4)) {
            var futures = new java.util.ArrayList<java.util.concurrent.Future<?>>();
            for (int i = 0; i < 20; i++) futures.add(executor.submit(() -> {
                try { store.update(id, state -> state.award(MILESTONE, 1)); }
                catch (IOException e) { throw new RuntimeException(e); }
            }));
            for (var future : futures) future.get();
        }
        assertEquals(1, store.load(id).points());
    }
    @Test void resetRefundsOncePersistsAndKeepsMilestoneClaims() throws Exception {
        UUID id = UUID.randomUUID();
        ProfileStore store = new ProfileStore(root);
        var costs = java.util.Map.of(SKILL, 1);
        store.update(id, state -> state.award(MILESTONE, 1).purchase(SKILL, 1, 1));
        var reset = store.update(id, state -> state.resetSkills(costs));
        assertEquals(1, reset.points()); assertEquals(0, reset.rank(SKILL));
        assertTrue(reset.milestones().contains(MILESTONE));
        assertEquals(reset, new ProfileStore(root).load(id));
        assertEquals(reset, store.update(id, state -> state.resetSkills(costs)));
        assertEquals(reset, store.update(id, state -> state.award(MILESTONE, 1)));
        assertEquals(0, store.update(id, state -> state.purchase(SKILL, 1, 1)).points());
    }
    @Test void resetPreservesUnspentPointsOtherProfilesAndUnknownSkills() throws Exception {
        UUID id = UUID.randomUUID(), other = UUID.randomUUID();
        ProfileStore store = new ProfileStore(root);
        var original = new ProgressionProfile(7, java.util.Set.of(MILESTONE),
                java.util.Map.of(SKILL, 2, "other:future_skill", 1));
        store.update(id, state -> original);
        var reset = store.update(id, state -> state.resetSkills(java.util.Map.of(SKILL, 3)));
        assertEquals(13, reset.points()); assertEquals(0, reset.rank(SKILL));
        assertEquals(1, reset.rank("other:future_skill"));
        assertEquals(ProgressionProfile.empty(), store.load(other));
    }
    @Test void resetLimitAndFailedWriteKeepPurchasedProfile() throws Exception {
        UUID id = UUID.randomUUID(); ProfileStore store = new ProfileStore(root);
        var purchased = new ProgressionProfile(ProgressionProfile.MAX_POINTS, java.util.Set.of(MILESTONE), java.util.Map.of(SKILL, 1));
        store.update(id, state -> purchased);
        assertThrows(IllegalArgumentException.class, () -> store.update(id, state -> state.resetSkills(java.util.Map.of(SKILL, 1))));
        assertEquals(purchased, store.load(id));
        assertEquals(purchased, new ProfileStore(root).load(id));
        store.update(id, state -> new ProgressionProfile(7, state.milestones(), state.skills()));
        var before = store.load(id);
        Files.delete(root.resolve(id + ".json.bak"));
        Files.createDirectory(root.resolve(id + ".json.bak"));
        Files.writeString(root.resolve(id + ".json.bak/block"), "prevent replacement");
        assertThrows(IOException.class, () -> store.update(id, state -> state.resetSkills(java.util.Map.of(SKILL, 1))));
        assertEquals(before, store.load(id));
        assertEquals(before, new ProfileStore(root).load(id));
    }

}

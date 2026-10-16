package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.nio.file.*;
import java.util.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

final class CoreRedemptionsTest {
    @TempDir Path root;
    private static final String CREATIVE = "forever_skyblock:jetpacks_creative";
    @Test void sameCorePaysOnceAcrossWorldsRestartsAndPlayers() throws Exception {
        UUID player = UUID.randomUUID(), other = UUID.randomUUID(), core = UUID.randomUUID();
        var profiles = new ProfileStore(root.resolve("profiles"));
        var claims = new CoreRedemptions(root.resolve("claims"), profiles);
        assertEquals(CoreRedemptions.Result.CREDITED, claims.redeem(core, player));
        assertEquals(CoreRedemptions.Result.ALREADY_CREDITED, claims.redeem(core, player));
        profiles = new ProfileStore(root.resolve("profiles"));
        claims = new CoreRedemptions(root.resolve("claims"), profiles);
        assertEquals(CoreRedemptions.Result.ALREADY_CREDITED, claims.redeem(core, player));
        assertEquals(CoreRedemptions.Result.CLAIMED_BY_OTHER, claims.redeem(core, other));
        assertEquals(1, profiles.load(player).perkPoints()); assertEquals(0, profiles.load(other).perkPoints());
        assertEquals(0, profiles.load(player).points());
    }
    @Test void failedProfileWriteKeepsClaimAndRetryFinishesExactlyOnce() throws Exception {
        UUID player = UUID.randomUUID(), core = UUID.randomUUID();
        Path profilesPath = root.resolve("profiles"); Files.writeString(profilesPath, "blocked");
        var profiles = new ProfileStore(profilesPath);
        var claims = new CoreRedemptions(root.resolve("claims"), profiles);
        assertThrows(java.io.IOException.class, () -> claims.redeem(core, player));
        assertEquals(0, profiles.load(player).perkPoints());
        assertTrue(Files.exists(root.resolve("claims/" + core + ".claim")));
        Files.delete(profilesPath);
        assertEquals(CoreRedemptions.Result.CREDITED, claims.redeem(core, player));
        assertEquals(CoreRedemptions.Result.ALREADY_CREDITED, claims.redeem(core, player));
        assertEquals(1, new ProfileStore(profilesPath).load(player).perkPoints());
    }
    @Test void claimFailureNeverCreatesCreditAndCorruptClaimsAreNotOverwritten() throws Exception {
        UUID player = UUID.randomUUID(), core = UUID.randomUUID();
        Path blocked = root.resolve("claims"); Files.writeString(blocked, "blocked");
        var profiles = new ProfileStore(root.resolve("profiles"));
        assertThrows(java.io.IOException.class, () -> new CoreRedemptions(blocked, profiles).redeem(core, player));
        assertEquals(0, profiles.load(player).perkPoints());
        Files.delete(blocked); Files.createDirectories(blocked);
        Path claim = blocked.resolve(core + ".claim"); Files.writeString(claim, "corrupt");
        assertThrows(java.io.IOException.class, () -> new CoreRedemptions(blocked, profiles).redeem(core, player));
        assertEquals("corrupt", Files.readString(claim)); assertEquals(0, profiles.load(player).perkPoints());
    }
    @Test void dualCostPurchaseAndResetRefundBothCurrenciesWithoutRearmingCores() throws Exception {
        var profiles = new ProfileStore(root); UUID player = UUID.randomUUID(), core = UUID.randomUUID();
        var noPerks = profiles.update(player, state -> state.grant(40));
        assertEquals(noPerks, noPerks.purchase(CREATIVE, 40, 1, 1));
        var purchased = profiles.update(player, state -> state.redeem(core).purchase(CREATIVE, 40, 1, 1));
        assertEquals(0, purchased.points()); assertEquals(0, purchased.perkPoints()); assertEquals(1, purchased.rank(CREATIVE));
        assertEquals(purchased, purchased.purchase(CREATIVE, 40, 1, 1));
        var reset = profiles.update(player, state -> state.resetSkills(Map.of(CREATIVE, 40)));
        assertEquals(40, reset.points()); assertEquals(1, reset.perkPoints());
        assertEquals(reset, reset.resetSkills(Map.of(CREATIVE, 40))); assertEquals(reset, reset.redeem(core));
        assertEquals(reset, new ProfileStore(root).load(player));
        var onlyPerks = ProgressionProfile.empty().redeem(UUID.randomUUID());
        assertEquals(onlyPerks, onlyPerks.purchase(CREATIVE, 40, 1, 1));
    }
    @Test void legacyCreativeIsRefundedWithoutMintingPerkPoints() throws Exception {
        UUID id = UUID.randomUUID();
        Files.writeString(root.resolve(id + ".json"), """
                {"schema_version":1,"player_uuid":"%s","points":7,"milestones":["forever_skyblock:first_foundations"],
                 "skills":{"forever_skyblock:jetpacks_beginner":1,"forever_skyblock:jetpacks_mid_game":1,
                           "forever_skyblock:jetpacks_end_game":1,"forever_skyblock:jetpacks_creative":1}}
                """.formatted(id));
        var store = new ProfileStore(root); var migrated = store.load(id);
        assertEquals(47, migrated.points()); assertEquals(0, migrated.perkPoints()); assertEquals(0, migrated.rank(CREATIVE));
        assertEquals(1, migrated.rank("forever_skyblock:jetpacks_end_game")); assertEquals(1, migrated.milestones().size());
        store.update(id, state -> state.grant(1));
        assertEquals(48, new ProfileStore(root).load(id).points());
        assertTrue(Files.readString(root.resolve(id + ".json")).contains("\"schema_version\": 4"));
    }
    @Test void racingRedemptionsAndFailedLimitCannotDuplicateOrLoseTheClaim() throws Exception {
        var profiles = new ProfileStore(root.resolve("profiles"));
        var claims = new CoreRedemptions(root.resolve("claims"), profiles);
        UUID player = UUID.randomUUID(), core = UUID.randomUUID();
        try (var executor = java.util.concurrent.Executors.newFixedThreadPool(4)) {
            var futures = new ArrayList<java.util.concurrent.Future<CoreRedemptions.Result>>();
            for (int i = 0; i < 20; i++) futures.add(executor.submit(() -> claims.redeem(core, player)));
            int credits = 0; for (var future : futures) if (future.get() == CoreRedemptions.Result.CREDITED) credits++;
            assertEquals(1, credits);
        }
        assertEquals(1, profiles.load(player).perkPoints());
        assertThrows(IllegalArgumentException.class, () -> profiles.update(player,
                state -> new ProgressionProfile(0, Set.of(), Map.of(), ProgressionProfile.MAX_POINTS + 1, Map.of(), Set.of())));
        assertEquals(1, profiles.load(player).perkPoints());
    }
}

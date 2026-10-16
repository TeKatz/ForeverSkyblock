package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import com.google.gson.JsonParser;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class ChallengeProgressionTest {
    @TempDir Path root;
    private static ProgressionProfile complete(ProgressionProfile p, ChallengeRules.Definition d) {
        return ChallengeRules.record(p, d.id(), "completed");
    }
    @Test void distinctToolsPersistAcrossWorldsAndResetWithoutReplayingRewards() throws Exception {
        UUID id = UUID.randomUUID(); var store = new ProfileStore(root);
        for (String tool : List.of("pickaxe", "axe", "shovel")) {
            store.update(id, p -> ChallengeRules.record(p, ChallengeRules.STONE_TOOLS.id(), "minecraft:stone_" + tool));
        }
        var partial = store.load(id);
        assertEquals(0, partial.points()); assertEquals(3, partial.meta().progress().get(ChallengeRules.STONE_TOOLS.id()).evidence().size());
        assertEquals(partial, store.update(id, p -> ChallengeRules.record(p, ChallengeRules.STONE_TOOLS.id(), "minecraft:stone_pickaxe")));
        store = new ProfileStore(root);
        var done = store.update(id, p -> ChallengeRules.record(p, ChallengeRules.STONE_TOOLS.id(), "minecraft:stone_hoe"));
        assertEquals(2, done.points()); assertTrue(done.milestones().contains(ChallengeRules.STONE_TOOLS.id()));
        var reset = store.update(id, p -> p.purchase("test:skill", 2, 1).resetSkills(Map.of("test:skill", 2)));
        assertEquals(done.meta().progress(), reset.meta().progress()); assertEquals(2, reset.points());
        assertEquals(reset, new ProfileStore(root).update(id, p -> ChallengeRules.record(p, ChallengeRules.STONE_TOOLS.id(), "minecraft:stone_hoe")));
    }
    @Test void thousandUniqueKillsAcrossReloadsPayOnceAndStopGrowing() throws Exception {
        UUID id = UUID.randomUUID(); var store = new ProfileStore(root);
        // Persist in three batches, representing successive worlds, with duplicate event delivery.
        for (int[] range : List.of(new int[]{0,630}, new int[]{630,850}, new int[]{850,1000})) {
            store.update(id, p -> {
                for (int n=range[0]; n<range[1]; n++) {
                    String victim = new UUID(0,n).toString();
                    p=ChallengeRules.record(p, ChallengeRules.VETERAN.id(), victim);
                    p=ChallengeRules.record(p, ChallengeRules.VETERAN.id(), victim);
                }
                return p;
            });
            store = new ProfileStore(root);
            assertEquals(range[1], store.load(id).meta().progress().get(ChallengeRules.VETERAN.id()).evidence().size());
        }
        var done = store.load(id); assertEquals(3, done.points());
        assertEquals(done, store.update(id, p -> ChallengeRules.record(p, ChallengeRules.VETERAN.id(), UUID.randomUUID().toString())));
        assertEquals(3, done.meta().accounting().spEarned());
    }
    @Test void tierUnlockIsAtomicMonotonicAndIndependentOfAdminGrantsOrRefunds() throws Exception {
        UUID id=UUID.randomUUID(); var store=new ProfileStore(root);
        assertEquals(-1, store.update(id, p -> p.grant(100)).meta().shopTier());
        assertEquals(0, store.update(id, p -> complete(p, ChallengeRules.FOUNDATIONS)).meta().shopTier());
        var dragon=store.update(id, p -> complete(p, ChallengeRules.DRAGON));
        assertEquals(107, dragon.points()); assertEquals(1, dragon.meta().shopTier());
        var reset=store.update(id,p -> p.purchase("test:skill",100,1).resetSkills(Map.of("test:skill",100)));
        assertEquals(1,reset.meta().shopTier()); assertEquals(107,reset.points());
        assertEquals(reset,new ProfileStore(root).update(id,p -> complete(p,ChallengeRules.DRAGON)));
        var higher=reset.withMeta(new MetaProgress(4,reset.meta().progress(),reset.meta().accounting()));
        assertEquals(4, complete(higher,ChallengeRules.NETHER).meta().shopTier());
    }
    @Test void schemaTwoMigrationPreservesEveryLegacyFieldAndDoesNotInventHistory() throws Exception {
        UUID id=UUID.randomUUID(),core=UUID.randomUUID();
        String legacy="""
                {"schema_version":2,"player_uuid":"%s","points":17,"perk_points":2,
                 "milestones":["forever_skyblock:first_foundations","future:challenge"],
                 "skills":{"forever_skyblock:extended_reach":1,"forever_skyblock:jetpacks_creative":1,"future:skill":2},
                 "perk_investments":{"forever_skyblock:jetpacks_creative":1},"redeemed_cores":["%s"]}
                """.formatted(id,core);
        Files.writeString(root.resolve(id+".json"),legacy);
        var store=new ProfileStore(root);var p=store.load(id);
        assertEquals(17,p.points());assertEquals(2,p.perkPoints());assertEquals(0,p.meta().shopTier());
        assertTrue(p.meta().accounting().legacyHistoryUnknown());assertEquals(0,p.meta().accounting().spEarned());
        assertEquals(p,store.update(id,s -> complete(s,ChallengeRules.FOUNDATIONS)));
        assertEquals(p,store.update(id,s -> s.redeem(core)));
        var next=store.update(id,s -> ChallengeRules.record(s,ChallengeRules.IRON_TOOLS.id(),"minecraft:iron_hoe"));
        assertEquals(p.skills(),next.skills());assertEquals(p.perkInvestments(),next.perkInvestments());assertEquals(p.redeemedCores(),next.redeemedCores());
        assertTrue(next.milestones().contains("future:challenge"));assertEquals(17,next.points());
        assertEquals(3,JsonParser.parseString(Files.readString(store.file(id))).getAsJsonObject().get("schema_version").getAsInt());
        assertEquals(next,new ProfileStore(root).load(id));
    }
    @Test void oldAndNewAccountingDistinguishEarningSpendingRefundsAndAdminPoints() {
        var p=complete(ProgressionProfile.empty(),ChallengeRules.DRAGON).grant(40).redeem(UUID.randomUUID());
        p=p.purchase("test:creative",40,1,1).resetSkills(Map.of("test:creative",40));
        var a=p.meta().accounting();assertFalse(a.legacyHistoryUnknown());
        assertEquals(6,a.spEarned());assertEquals(1,a.ppEarned());assertEquals(40,a.spAdmin());
        assertEquals(40,a.spSpent());assertEquals(1,a.ppSpent());assertEquals(40,a.spRefunded());assertEquals(1,a.ppRefunded());
        assertEquals(p,p.resetSkills(Map.of("test:creative",40)));
    }
    @Test void failedCommitCannotAdvanceCounterClaimTierOrAccounting() throws Exception {
        UUID id=UUID.randomUUID();var store=new ProfileStore(root);
        var before=store.update(id,p -> p.grant(10));
        var backup=root.resolve(id+".json.bak");Files.createDirectory(backup);Files.writeString(backup.resolve("blocked"),"x");
        assertThrows(IOException.class,()->store.update(id,p -> complete(p,ChallengeRules.DRAGON)));
        assertEquals(before,store.load(id));assertEquals(before,new ProfileStore(root).load(id));
        Files.delete(backup.resolve("blocked"));Files.delete(backup);
        assertEquals(1,store.update(id,p -> complete(p,ChallengeRules.DRAGON)).meta().shopTier());
        assertEquals(16,new ProfileStore(root).load(id).points());
    }
    @Test void concurrentDuplicateEvidenceCannotDoubleCountOrPay() throws Exception {
        var store=new ProfileStore(root);UUID id=UUID.randomUUID();
        try(var workers=java.util.concurrent.Executors.newFixedThreadPool(4)) {
            var futures=new ArrayList<java.util.concurrent.Future<?>>();
            for(int i=0;i<20;i++)futures.add(workers.submit(()->{
                try { store.update(id,p -> complete(p,ChallengeRules.DRAGON)); }catch(IOException e){throw new RuntimeException(e);}
            }));
            for(var future:futures)future.get();
        }
        var p=new ProfileStore(root).load(id);assertEquals(6,p.points());assertEquals(1,p.meta().shopTier());assertEquals(6,p.meta().accounting().spEarned());
    }
    @Test void unknownIdsAndFutureRevisionsSurviveWithoutBeingInterpreted() throws Exception {
        UUID id=UUID.randomUUID();var store=new ProfileStore(root);
        var meta=new MetaProgress(2,Map.of("future:objective",new MetaProgress.Progress(8,Set.of("future-token")),
                ChallengeRules.STONE_TOOLS.id(),new MetaProgress.Progress(2,Set.of("future-tool"))),MetaProgress.Accounting.empty(true));
        var before=store.update(id,p -> new ProgressionProfile(12,Set.of("future:claim"),Map.of("future:skill",1),2,Map.of(),Set.of(),meta));
        assertEquals(before,new ProfileStore(root).load(id));
        assertEquals(before,ChallengeRules.record(before,ChallengeRules.STONE_TOOLS.id(),"minecraft:stone_hoe"));
        assertEquals(before,ChallengeRules.record(before,"future:objective","future-token"));
        assertEquals(2,before.grant(1).meta().shopTier());
    }
    @Test void badEvidenceAndBalanceOverflowCannotCreatePartialRewards() {
        var p=ProgressionProfile.empty();assertEquals(p,ChallengeRules.record(p,ChallengeRules.STONE_TOOLS.id(),"minecraft:diamond_hoe"));
        assertEquals(p,ChallengeRules.record(p,ChallengeRules.VETERAN.id(),"not-a-uuid"));
        var full=p.grant(ProgressionProfile.MAX_POINTS);
        assertThrows(IllegalArgumentException.class,()->complete(full,ChallengeRules.DRAGON));
        assertEquals(-1,full.meta().shopTier());assertTrue(full.milestones().isEmpty());
        assertThrows(IllegalArgumentException.class,()->new MetaProgress(0,Map.of(ChallengeRules.STONE_TOOLS.id(),new MetaProgress.Progress(1,Set.of("garbage"))),MetaProgress.Accounting.empty(false)));
    }
}

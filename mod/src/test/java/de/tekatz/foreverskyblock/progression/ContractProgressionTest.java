package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import com.google.gson.JsonParser;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class ContractProgressionTest {
    @TempDir Path root;

    private static ProgressionProfile tierOne() {
        return ChallengeRules.record(ProgressionProfile.empty(), ChallengeRules.DRAGON.id(), "completed");
    }

    @Test void firstDragonIsUniqueOnlyAndLaterDragonsAreRepeatable() {
        var p=ProgressionProfile.empty();
        p=ContractRules.recordBoss(p,ChallengeRules.DRAGON,ContractCatalog.DRAGON_REBORN,"dragon-1");
        assertEquals(6,p.points());assertEquals(1,p.meta().shopTier());
        assertEquals(0,p.meta().contracts().getOrDefault(ContractCatalog.DRAGON_REBORN.id(),MetaProgress.ContractState.empty()).completions());
        p=ContractRules.recordBoss(p,ChallengeRules.DRAGON,ContractCatalog.DRAGON_REBORN,"dragon-2");
        assertEquals(7,p.points());assertEquals(1,p.meta().contracts().get(ContractCatalog.DRAGON_REBORN.id()).completions());
        var duplicate=ContractRules.recordBoss(p,ChallengeRules.DRAGON,ContractCatalog.DRAGON_REBORN,"dragon-2");
        assertEquals(p,duplicate);
        p=ContractRules.recordBoss(p,ChallengeRules.DRAGON,ContractCatalog.DRAGON_REBORN,"dragon-3");
        assertEquals(8,p.points());assertEquals(2,p.meta().contracts().get(ContractCatalog.DRAGON_REBORN.id()).completions());
    }

    @Test void witherCounterResetsOnlyAfterThreeAdditionalKills() {
        var p=tierOne();
        p=ContractRules.recordBoss(p,ChallengeRules.WITHER,ContractCatalog.WITHER_HUNTER,"wither-unique");
        assertEquals(11,p.points());
        for(int i=1;i<=2;i++)p=ContractRules.recordBoss(p,ChallengeRules.WITHER,ContractCatalog.WITHER_HUNTER,"wither-a"+i);
        assertEquals(11,p.points());assertEquals(2,p.meta().contracts().get(ContractCatalog.WITHER_HUNTER.id()).progress());
        p=ContractRules.recordBoss(p,ChallengeRules.WITHER,ContractCatalog.WITHER_HUNTER,"wither-a3");
        assertEquals(12,p.points());assertEquals(0,p.meta().contracts().get(ContractCatalog.WITHER_HUNTER.id()).progress());
        assertEquals(1,p.meta().contracts().get(ContractCatalog.WITHER_HUNTER.id()).completions());
    }

    @Test void durableTurnInIntentPaysOnlyWhenFinalizedAndRejectsRequestReplay() {
        var p=tierOne();UUID request=UUID.randomUUID();
        p=ContractRules.beginTurnIn(p,ContractCatalog.SINGULARITY,request,12);
        assertEquals(6,p.points());assertEquals(1,p.meta().pendingContracts().size());
        assertEquals(12,p.meta().pendingContracts().get(request).beforeCount());
        assertEquals(8,p.meta().pendingContracts().get(request).afterCount());
        var begun=p;
        assertEquals(begun,ContractRules.beginTurnIn(begun,ContractCatalog.SINGULARITY,request,12));
        p=ContractRules.finishTurnIn(p,request);
        assertEquals(7,p.points());assertTrue(p.meta().pendingContracts().isEmpty());
        assertEquals(1,p.meta().contracts().get(ContractCatalog.SINGULARITY.id()).completions());
        assertEquals(p,ContractRules.finishTurnIn(p,request));
        assertEquals(p,ContractRules.beginTurnIn(p,ContractCatalog.SINGULARITY,request,12));
    }

    @Test void resourceContractsRequireTierAndExactConfiguredMinimum() {
        var p=ProgressionProfile.empty();UUID request=UUID.randomUUID();
        assertEquals(p,ContractRules.beginTurnIn(p,ContractCatalog.NITRO,request,64));
        p=tierOne();
        assertEquals(p,ContractRules.beginTurnIn(p,ContractCatalog.NITRO,request,31));
        assertFalse(ContractRules.beginTurnIn(p,ContractCatalog.NITRO,request,32).meta().pendingContracts().isEmpty());
    }

    @Test void schemaFourLoadsUnchangedAndNextContractWriteUsesSchemaFive() throws Exception {
        UUID player=UUID.randomUUID();
        Files.writeString(root.resolve(player+".json"),"""
                {"schema_version":4,"player_uuid":"%s","points":9,"perk_points":0,
                 "milestones":["forever_skyblock:dragon_slayer"],"skills":{},"perk_investments":{},"redeemed_cores":[],
                 "meta":{"shop_tier":1,"challenge_progress":{},
                 "accounting":{"spEarned":6,"ppEarned":0,"spSpent":0,"ppSpent":0,"spRefunded":0,"ppRefunded":0,"spAdmin":3,"legacyHistoryUnknown":false},
                 "world_claims":{},"purchase_receipts":{}}}
                """.formatted(player));
        var store=new ProfileStore(root);var loaded=store.load(player);
        assertEquals(9,loaded.points());assertTrue(loaded.meta().contracts().isEmpty());assertTrue(loaded.meta().pendingContracts().isEmpty());
        UUID request=UUID.randomUUID();
        var begun=store.update(player,p->ContractRules.beginTurnIn(p,ContractCatalog.SINGULARITY,request,4));
        assertEquals(1,begun.meta().pendingContracts().size());
        assertEquals(5,JsonParser.parseString(Files.readString(store.file(player))).getAsJsonObject().get("schema_version").getAsInt());
        assertEquals(begun,new ProfileStore(root).load(player));
    }

    @Test void skillResetPreservesContractProgressAndCompletions() {
        var p=tierOne().grant(10).purchase("test:skill",2,1);
        p=ContractRules.recordBoss(p,ChallengeRules.WITHER,ContractCatalog.WITHER_HUNTER,"wither-unique");
        p=ContractRules.recordBoss(p,ChallengeRules.WITHER,ContractCatalog.WITHER_HUNTER,"wither-1");
        var before=p.meta().contracts();
        var reset=p.resetSkills(Map.of("test:skill",2));
        assertEquals(before,reset.meta().contracts());assertEquals(1,reset.meta().shopTier());
    }
}

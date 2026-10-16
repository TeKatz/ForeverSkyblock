package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import com.google.gson.JsonParser;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class WorldPurchaseTest {
    @TempDir Path root;

    private static ProgressionProfile ready() {
        var base = ProgressionProfile.empty().grant(20);
        return base.withMeta(new MetaProgress(1, base.meta().progress(), base.meta().accounting()));
    }

    @Test void tierCostWorldScopeReceiptAndAccountingCommitTogether() throws Exception {
        UUID player=UUID.randomUUID(), first=UUID.randomUUID(), second=UUID.randomUUID(); var store=new ProfileStore(root);
        assertEquals(ProgressionProfile.empty(), ProgressionProfile.empty().purchaseWorld(first,ShopCatalog.TEST,UUID.randomUUID()));
        store.update(player, ignored->ready());
        var bought=store.update(player,p->p.purchaseWorld(first,ShopCatalog.TEST,UUID.randomUUID()));
        assertEquals(15,bought.points()); assertTrue(bought.meta().claimed(first,ShopCatalog.TEST.id()));
        assertEquals(1,bought.meta().purchaseReceipts().size()); assertEquals(5,bought.meta().accounting().spSpent());
        assertEquals(bought,store.update(player,p->p.purchaseWorld(first,ShopCatalog.TEST,UUID.randomUUID())));
        var other=store.update(player,p->p.purchaseWorld(second,ShopCatalog.TEST,UUID.randomUUID()));
        assertEquals(10,other.points()); assertEquals(2,other.meta().purchaseReceipts().size());
        assertEquals(10,other.meta().accounting().spSpent());
        assertEquals(other,other.resetSkills(Map.of()));
        assertEquals(other,new ProfileStore(root).load(player));
    }

    @Test void schemaThreeMigratesWithoutClaimsOrCurrencyChangesAndWritesSchemaFour() throws Exception {
        UUID player=UUID.randomUUID();
        Files.writeString(root.resolve(player+".json"),"""
                {"schema_version":3,"player_uuid":"%s","points":9,"perk_points":2,
                 "milestones":["future:claim"],"skills":{"future:skill":1},"perk_investments":{},"redeemed_cores":[],
                 "meta":{"shop_tier":1,"challenge_progress":{},"accounting":{"spEarned":4,"ppEarned":0,"spSpent":1,
                 "ppSpent":0,"spRefunded":0,"ppRefunded":0,"spAdmin":0,"legacyHistoryUnknown":true}}}
                """.formatted(player));
        var store=new ProfileStore(root);var migrated=store.load(player);
        assertEquals(9,migrated.points());assertEquals(1,migrated.meta().shopTier());assertTrue(migrated.meta().worldClaims().isEmpty());
        UUID world=UUID.randomUUID();var bought=store.update(player,p->p.purchaseWorld(world,ShopCatalog.TEST,UUID.randomUUID()));
        assertEquals(4,bought.points());assertTrue(bought.milestones().contains("future:claim"));assertEquals(6,bought.meta().accounting().spSpent());
        assertEquals(4,JsonParser.parseString(Files.readString(store.file(player))).getAsJsonObject().get("schema_version").getAsInt());
    }

    @Test void everyWriteAheadCrashCutHasOneDeterministicOutcome() throws Exception {
        UUID world=UUID.randomUUID();
        for(var stage:ProfileStore.WriteStage.values()) {
            Path path=root.resolve(stage.name());UUID player=UUID.randomUUID();new ProfileStore(path).update(player,p->ready());
            var faulty=new ProfileStore(path,at->{if(at==stage)throw new IOException("cut at "+stage);});
            assertThrows(IOException.class,()->faulty.update(player,p->p.purchaseWorld(world,ShopCatalog.TEST,UUID.randomUUID())));
            if(stage==ProfileStore.WriteStage.AFTER_BACKUP) {
                var unchanged=new ProfileStore(path).load(player);assertEquals(20,unchanged.points());assertFalse(unchanged.meta().claimed(world,ShopCatalog.TEST.id()));
            } else {
                if(stage==ProfileStore.WriteStage.AFTER_JOURNAL)Files.writeString(new ProfileStore(path).file(player),"stale-or-corrupt");
                var recovered=new ProfileStore(path).load(player);assertEquals(15,recovered.points());assertTrue(recovered.meta().claimed(world,ShopCatalog.TEST.id()));
                assertEquals(1,recovered.meta().purchaseReceipts().size());assertFalse(Files.exists(new ProfileStore(path).journal(player)));
            }
        }
    }

    @Test void concurrentSpamCreatesOneClaimOneDebitAndOneReceipt() throws Exception {
        UUID player=UUID.randomUUID(),world=UUID.randomUUID();var store=new ProfileStore(root);store.update(player,p->ready());
        try(var workers=java.util.concurrent.Executors.newFixedThreadPool(8)) {
            var futures=new ArrayList<java.util.concurrent.Future<?>>();
            for(int i=0;i<64;i++)futures.add(workers.submit(()->{
                try{store.update(player,p->p.purchaseWorld(world,ShopCatalog.TEST,UUID.randomUUID()));}catch(IOException e){throw new RuntimeException(e);}
            }));
            for(var future:futures)future.get();
        }
        var result=new ProfileStore(root).load(player);assertEquals(15,result.points());assertEquals(Set.of(ShopCatalog.TEST.id()),result.meta().worldClaims().get(world));
        assertEquals(1,result.meta().purchaseReceipts().size());assertEquals(5,result.meta().accounting().spSpent());
    }
}

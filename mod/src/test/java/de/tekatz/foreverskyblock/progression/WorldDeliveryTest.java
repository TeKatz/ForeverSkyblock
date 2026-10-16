package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import com.google.gson.JsonParser;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class WorldDeliveryTest {
    @TempDir Path root;

    private static ProgressionProfile ready(int points) {
        var base = ProgressionProfile.empty().grant(points);
        return base.withMeta(new MetaProgress(1, base.meta().progress(), base.meta().accounting()));
    }

    @Test void realWorldPurchaseCreatesOneDurableDeliveryAndChargesOnce() {
        UUID world = UUID.randomUUID(), transaction = UUID.randomUUID();
        var product = ShopCatalog.ADVANCED_AE2_STARTER;
        var bought = ready(30).purchaseWorld(world, product, transaction);
        assertEquals(18, bought.points());
        assertEquals(12, bought.meta().accounting().spSpent());
        assertTrue(bought.meta().claimed(world, product.id()));
        assertEquals(MetaProgress.DeliveryStage.READY, bought.meta().deliveries().get(transaction).stage());
        assertEquals(bought, bought.purchaseWorld(world, product, UUID.randomUUID()));
    }

    @Test void deliveryStagesAreStrictAndSkillResetCannotRearmThem() {
        UUID world = UUID.randomUUID(), transaction = UUID.randomUUID();
        var bought = ready(30).purchaseWorld(world, ShopCatalog.ADVANCED_AE2_STARTER, transaction);
        assertEquals(bought, bought.finishDelivery(transaction));
        var dispatching = bought.beginDelivery(transaction);
        assertEquals(MetaProgress.DeliveryStage.DISPATCHING, dispatching.meta().deliveries().get(transaction).stage());
        var delivered = dispatching.finishDelivery(transaction);
        assertEquals(MetaProgress.DeliveryStage.DELIVERED, delivered.meta().deliveries().get(transaction).stage());
        assertEquals(delivered, delivered.beginDelivery(transaction));
        var opening = delivered.beginOpening(transaction);
        assertEquals(MetaProgress.DeliveryStage.OPENING, opening.meta().deliveries().get(transaction).stage());
        var opened = opening.finishOpening(transaction);
        assertEquals(MetaProgress.DeliveryStage.OPENED, opened.meta().deliveries().get(transaction).stage());
        assertEquals(opened, opened.finishOpening(transaction));
        assertEquals(opened, opened.resetSkills(Map.of()));
    }

    @Test void contractProfilesStaySchemaFiveUntilFirstRealDeliveryPurchase() throws Exception {
        UUID player = UUID.randomUUID();
        var base = ready(30);
        var contracts = Map.of(ContractCatalog.WITHER_HUNTER.id(), new MetaProgress.ContractState(1, 1, 0, java.util.List.of(), java.util.List.of()));
        var withContract = base.withMeta(new MetaProgress(1, base.meta().progress(), base.meta().accounting(),
                Map.of(), Map.of(), contracts, Map.of()));
        var store = new ProfileStore(root);
        store.update(player, ignored -> withContract);
        assertEquals(5, JsonParser.parseString(Files.readString(store.file(player))).getAsJsonObject().get("schema_version").getAsInt());
        UUID world = UUID.randomUUID();
        store.update(player, profile -> profile.purchaseWorld(world, ShopCatalog.ADVANCED_AE2_STARTER, UUID.randomUUID()));
        assertEquals(6, JsonParser.parseString(Files.readString(store.file(player))).getAsJsonObject().get("schema_version").getAsInt());
        var reloaded = new ProfileStore(root).load(player);
        assertEquals(18, reloaded.points());
        assertEquals(1, reloaded.meta().deliveries().size());
        assertEquals(1, reloaded.meta().contracts().get(ContractCatalog.WITHER_HUNTER.id()).progress());
    }
}

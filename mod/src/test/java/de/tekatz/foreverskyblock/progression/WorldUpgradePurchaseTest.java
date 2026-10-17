package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.util.UUID;
import org.junit.jupiter.api.Test;

class WorldUpgradePurchaseTest {
    private static ProgressionProfile ready(int points) {
        var base = ProgressionProfile.empty().grant(points);
        return base.withMeta(new MetaProgress(0, base.meta().progress(), base.meta().accounting()));
    }

    @Test void worldUpgradePricesAreSmallExplicitAndNonDelivering() {
        assertEquals(1, ShopCatalog.NO_PHANTOMS.spCost());
        assertEquals(2, ShopCatalog.NO_RAIN.spCost());
        assertEquals(3, ShopCatalog.PERMANENT_DAY.spCost());
        assertTrue(ShopCatalog.NO_PHANTOMS.worldUpgrade());
        assertTrue(ShopCatalog.NO_RAIN.worldUpgrade());
        assertTrue(ShopCatalog.PERMANENT_DAY.worldUpgrade());
        assertFalse(ShopCatalog.NO_PHANTOMS.deliverable());
        assertFalse(ShopCatalog.NO_RAIN.deliverable());
        assertFalse(ShopCatalog.PERMANENT_DAY.deliverable());
    }

    @Test void worldUpgradeChargesOnceAndCreatesNoDelivery() {
        UUID world = UUID.randomUUID();
        var bought = ready(10).purchaseWorld(world, ShopCatalog.NO_RAIN, UUID.randomUUID());
        assertEquals(8, bought.points());
        assertEquals(2, bought.meta().accounting().spSpent());
        assertTrue(bought.meta().claimed(world, ShopCatalog.NO_RAIN.id()));
        assertTrue(bought.meta().deliveries().isEmpty());
        assertEquals(bought, bought.purchaseWorld(world, ShopCatalog.NO_RAIN, UUID.randomUUID()));
    }

    @Test void worldUpgradesRemainIndependentPerStableWorldIdentity() {
        UUID first = UUID.randomUUID(), second = UUID.randomUUID();
        var firstPurchase = ready(10).purchaseWorld(first, ShopCatalog.NO_PHANTOMS, UUID.randomUUID());
        var secondPurchase = firstPurchase.purchaseWorld(second, ShopCatalog.NO_PHANTOMS, UUID.randomUUID());
        assertEquals(8, secondPurchase.points());
        assertTrue(secondPurchase.meta().claimed(first, ShopCatalog.NO_PHANTOMS.id()));
        assertTrue(secondPurchase.meta().claimed(second, ShopCatalog.NO_PHANTOMS.id()));
    }
}

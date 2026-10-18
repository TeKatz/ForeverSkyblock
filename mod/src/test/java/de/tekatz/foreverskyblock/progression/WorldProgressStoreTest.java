package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class WorldProgressStoreTest {
    @TempDir Path root;

    @Test void traderAndWorldUpgradesPersistTogether() throws Exception {
        UUID world = UUID.randomUUID();
        var store = new WorldProgressStore();
        assertFalse(store.loadOrCreate(root, world).wanderingTraderUnlocked());
        store.unlockTrader(root, world);
        store.enableUpgrade(root, world, ShopCatalog.NO_RAIN.id());
        store.enableUpgrade(root, world, ShopCatalog.PERMANENT_DAY.id());
        var reloaded = new WorldProgressStore().loadOrCreate(root, world);
        assertTrue(reloaded.wanderingTraderUnlocked());
        assertTrue(reloaded.active(ShopCatalog.NO_RAIN.id()));
        assertTrue(reloaded.active(ShopCatalog.PERMANENT_DAY.id()));
        assertFalse(reloaded.active(ShopCatalog.NO_PHANTOMS.id()));
    }

    @Test void validBackupRepairsMissingPrimary() throws Exception {
        UUID world = UUID.randomUUID();
        var store = new WorldProgressStore();
        store.enableUpgrade(root, world, ShopCatalog.NO_PHANTOMS.id());
        Files.delete(store.file(root));
        var recovered = new WorldProgressStore().loadOrCreate(root, world);
        assertTrue(recovered.active(ShopCatalog.NO_PHANTOMS.id()));
        assertTrue(Files.exists(store.file(root)));
    }

    @Test void wrongWorldIdentityFailsClosed() throws Exception {
        UUID first = UUID.randomUUID();
        var store = new WorldProgressStore();
        store.unlockTrader(root, first);
        assertThrows(java.io.IOException.class, () -> new WorldProgressStore().loadOrCreate(root, UUID.randomUUID()));
    }

    @Test void oldWorldMigratesWithoutImportingPermanentHistoryAndKeepsAllGates() throws Exception {
        UUID world = UUID.randomUUID();
        Path file = new WorldProgressStore().file(root);
        Files.createDirectories(file.getParent());
        Files.writeString(file, "{\"schema_version\":1,\"world_uuid\":\""+world+"\",\"wandering_trader_unlocked\":true,\"world_upgrades\":[]}");
        var store = new WorldProgressStore();
        assertEquals(0,store.loadOrCreate(root,world).islandLevel());
        store.recordMilestone(root,world,ChallengeRules.FOUNDATIONS.id(),"completed");
        store.enableUpgrade(root,world,ShopCatalog.NO_RAIN.id());
        var saved = new WorldProgressStore().loadOrCreate(root,world);
        assertTrue(saved.wanderingTraderUnlocked()); assertTrue(saved.active(ShopCatalog.NO_RAIN.id()));
        assertEquals(1,saved.islandLevel());
        Files.delete(file);
        assertEquals(1,new WorldProgressStore().loadOrCreate(root,world).islandLevel());
    }
}

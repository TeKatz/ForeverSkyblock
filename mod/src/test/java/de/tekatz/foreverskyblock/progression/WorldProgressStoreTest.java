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
}

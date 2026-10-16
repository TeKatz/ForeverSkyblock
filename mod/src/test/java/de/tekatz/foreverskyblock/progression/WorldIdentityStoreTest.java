package de.tekatz.foreverskyblock.progression;

import static org.junit.jupiter.api.Assertions.*;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class WorldIdentityStoreTest {
    @TempDir Path root;

    @Test void renameRestartAndSaveCopyRetainIdentityWhileNewSaveGetsAnother() throws Exception {
        var store = new WorldIdentityStore(); Path original = root.resolve("World A");
        UUID identity = store.loadOrCreate(original);
        assertEquals(identity, new WorldIdentityStore().loadOrCreate(original));
        Path renamed = root.resolve("Renamed World"); Files.move(original, renamed);
        assertEquals(identity, new WorldIdentityStore().loadOrCreate(renamed));
        Path copy = root.resolve("Copied World"); Files.createDirectories(copy.resolve("data"));
        Files.copy(store.file(renamed), store.file(copy), StandardCopyOption.REPLACE_EXISTING);
        Files.copy(store.backup(renamed), store.backup(copy), StandardCopyOption.REPLACE_EXISTING);
        assertEquals(identity, new WorldIdentityStore().loadOrCreate(copy), "A copied save keeps the claim scope and cannot duplicate purchases");
        assertNotEquals(identity, new WorldIdentityStore().loadOrCreate(root.resolve("New World")));
    }

    @Test void oneValidCopyRepairsMissingOrCorruptIdentityButConflictFailsClosed() throws Exception {
        var store = new WorldIdentityStore(); Path world = root.resolve("world"); UUID identity = store.loadOrCreate(world);
        Files.delete(store.file(world));
        assertEquals(identity, new WorldIdentityStore().loadOrCreate(world));
        assertTrue(Files.exists(store.file(world)));
        Files.writeString(store.file(world), "corrupt");
        assertEquals(identity, new WorldIdentityStore().loadOrCreate(world));
        Files.writeString(store.backup(world), "{\"schema_version\":1,\"world_uuid\":\"" + UUID.randomUUID() + "\"}");
        assertThrows(IOException.class, () -> new WorldIdentityStore().loadOrCreate(world));
    }
}

package de.tekatz.foreverskyblock.progression;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.charset.StandardCharsets;
import java.nio.file.AtomicMoveNotSupportedException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.nio.file.StandardOpenOption;
import java.util.UUID;

/** Immutable identity copied with a save, so renamed or cloned saves cannot re-arm world claims. */
public final class WorldIdentityStore {
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final long MAX_SIZE = 4096;

    public synchronized UUID loadOrCreate(Path worldRoot) throws IOException {
        Path primary = file(worldRoot), backup = backup(worldRoot);
        boolean hasPrimary = Files.exists(primary), hasBackup = Files.exists(backup);
        if (!hasPrimary && !hasBackup) {
            UUID created = UUID.randomUUID();
            byte[] bytes = encode(created);
            writeAtomically(primary, bytes);
            writeAtomically(backup, bytes);
            return created;
        }
        UUID first = readIfValid(primary), second = readIfValid(backup);
        if (first != null && second != null && !first.equals(second))
            throw new IOException("Conflicting world identities; refusing to create duplicate claim scope");
        UUID identity = first != null ? first : second;
        if (identity == null) throw new IOException("World identity and backup are invalid");
        byte[] bytes = encode(identity);
        if (first == null) writeAtomically(primary, bytes);
        if (second == null) writeAtomically(backup, bytes);
        return identity;
    }

    Path file(Path worldRoot) { return worldRoot.resolve("data/forever_skyblock_world_identity.json"); }
    Path backup(Path worldRoot) { return worldRoot.resolve("data/forever_skyblock_world_identity.json.bak"); }

    private static byte[] encode(UUID identity) {
        JsonObject json = new JsonObject();
        json.addProperty("schema_version", 1);
        json.addProperty("world_uuid", identity.toString());
        return (GSON.toJson(json) + "\n").getBytes(StandardCharsets.UTF_8);
    }

    private static UUID readIfValid(Path file) {
        if (!Files.exists(file)) return null;
        try {
            if (Files.size(file) > MAX_SIZE) return null;
            JsonObject json = JsonParser.parseString(Files.readString(file, StandardCharsets.UTF_8)).getAsJsonObject();
            if (json.get("schema_version").getAsInt() != 1) return null;
            return UUID.fromString(json.get("world_uuid").getAsString());
        } catch (IOException | RuntimeException ignored) { return null; }
    }

    private static void writeAtomically(Path destination, byte[] data) throws IOException {
        Files.createDirectories(destination.getParent());
        Path temporary = Files.createTempFile(destination.getParent(), "world-id-", ".tmp");
        try {
            try (FileChannel channel = FileChannel.open(temporary, StandardOpenOption.WRITE)) {
                ByteBuffer buffer = ByteBuffer.wrap(data);
                while (buffer.hasRemaining()) channel.write(buffer);
                channel.force(true);
            }
            try { Files.move(temporary, destination, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE); }
            catch (AtomicMoveNotSupportedException e) { Files.move(temporary, destination, StandardCopyOption.REPLACE_EXISTING); }
        } finally { Files.deleteIfExists(temporary); }
    }
}

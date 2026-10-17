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
import java.util.LinkedHashSet;
import java.util.Set;
import java.util.UUID;

/** World-owned progression that must survive player/profile changes and save renames. */
public final class WorldProgressStore {
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final long MAX_SIZE = 32_768;
    private State cached;

    public record State(UUID worldId, boolean wanderingTraderUnlocked, Set<String> upgrades) {
        public State {
            if (worldId == null || upgrades == null || upgrades.size() > 64) throw new IllegalArgumentException("Invalid world progression");
            upgrades = Set.copyOf(upgrades);
            for (String id : upgrades) ProgressionProfile.validateId(id);
        }
        public boolean active(String id) { return upgrades.contains(id); }
    }

    public synchronized State loadOrCreate(Path worldRoot, UUID expectedWorld) throws IOException {
        if (worldRoot == null || expectedWorld == null) throw new IOException("World progression identity unavailable");
        if (cached != null) {
            if (!cached.worldId().equals(expectedWorld)) throw new IOException("Cached world progression identity mismatch");
            return cached;
        }
        Path primary = file(worldRoot), backup = backup(worldRoot);
        State first = readIfValid(primary, expectedWorld);
        State second = readIfValid(backup, expectedWorld);
        if (first == null && second == null) {
            if (Files.exists(primary) || Files.exists(backup)) throw new IOException("World progression and backup are invalid");
            State created = new State(expectedWorld, false, Set.of());
            persist(worldRoot, created);
            cached = created;
            return created;
        }
        State chosen = first != null ? first : second;
        byte[] bytes = encode(chosen);
        if (first == null) writeAtomically(primary, bytes);
        if (second == null || !second.equals(chosen)) writeAtomically(backup, bytes);
        cached = chosen;
        return chosen;
    }

    public synchronized State unlockTrader(Path worldRoot, UUID expectedWorld) throws IOException {
        State current = loadOrCreate(worldRoot, expectedWorld);
        if (current.wanderingTraderUnlocked()) return current;
        State next = new State(expectedWorld, true, current.upgrades());
        persist(worldRoot, next); cached = next; return next;
    }

    public synchronized State enableUpgrade(Path worldRoot, UUID expectedWorld, String id) throws IOException {
        ProgressionProfile.validateId(id);
        State current = loadOrCreate(worldRoot, expectedWorld);
        if (current.active(id)) return current;
        var upgrades = new LinkedHashSet<>(current.upgrades()); upgrades.add(id);
        State next = new State(expectedWorld, current.wanderingTraderUnlocked(), upgrades);
        persist(worldRoot, next); cached = next; return next;
    }

    Path file(Path worldRoot) { return worldRoot.resolve("data/forever_skyblock_world_progress.json"); }
    Path backup(Path worldRoot) { return worldRoot.resolve("data/forever_skyblock_world_progress.json.bak"); }

    private static State readIfValid(Path file, UUID expectedWorld) {
        if (!Files.exists(file)) return null;
        try {
            if (Files.size(file) > MAX_SIZE) return null;
            JsonObject json = JsonParser.parseString(Files.readString(file, StandardCharsets.UTF_8)).getAsJsonObject();
            if (json.get("schema_version").getAsInt() != 1) return null;
            UUID world = UUID.fromString(json.get("world_uuid").getAsString());
            if (!expectedWorld.equals(world)) return null;
            boolean trader = json.get("wandering_trader_unlocked").getAsBoolean();
            var upgrades = new LinkedHashSet<String>();
            for (var value : json.getAsJsonArray("world_upgrades")) if (!upgrades.add(value.getAsString())) return null;
            return new State(world, trader, upgrades);
        } catch (IOException | RuntimeException ignored) { return null; }
    }

    private static byte[] encode(State state) {
        JsonObject json = new JsonObject();
        json.addProperty("schema_version", 1);
        json.addProperty("world_uuid", state.worldId().toString());
        json.addProperty("wandering_trader_unlocked", state.wanderingTraderUnlocked());
        json.add("world_upgrades", GSON.toJsonTree(state.upgrades().stream().sorted().toList()));
        return (GSON.toJson(json) + "\n").getBytes(StandardCharsets.UTF_8);
    }

    private static void persist(Path worldRoot, State state) throws IOException {
        byte[] bytes = encode(state);
        if (bytes.length > MAX_SIZE) throw new IOException("World progression exceeds size limit");
        writeAtomically(worldRoot.resolve("data/forever_skyblock_world_progress.json"), bytes);
        writeAtomically(worldRoot.resolve("data/forever_skyblock_world_progress.json.bak"), bytes);
    }

    private static void writeAtomically(Path destination, byte[] data) throws IOException {
        Files.createDirectories(destination.getParent());
        Path temporary = Files.createTempFile(destination.getParent(), "world-progress-", ".tmp");
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

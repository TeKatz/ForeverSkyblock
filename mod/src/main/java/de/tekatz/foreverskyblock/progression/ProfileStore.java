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
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.UUID;
import java.util.function.UnaryOperator;

/** Stored outside saves/: deleting a world does not delete the player's progression. */
public final class ProfileStore {
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final int SCHEMA = 1;
    private static final long MAX_FILE_SIZE = 1_048_576;
    private final Path directory;
    private final Map<UUID, ProgressionProfile> cache = new HashMap<>();

    public ProfileStore(Path directory) { this.directory = directory; }
    public synchronized ProgressionProfile load(UUID player) throws IOException {
        ProgressionProfile cached = cache.get(player);
        if (cached != null) return cached;
        Path file = file(player);
        Path backup = backup(player);
        ProgressionProfile result;
        if (!Files.exists(file) && !Files.exists(backup)) result = ProgressionProfile.empty();
        else {
            try { result = read(file, player); }
            catch (UnsupportedSchemaException e) { throw e; }
            catch (IOException original) {
                if (!Files.exists(backup)) throw original;
                result = read(backup, player);
                System.getLogger(ProfileStore.class.getName()).log(System.Logger.Level.WARNING,
                        "Recovered progression profile from its validated backup for " + player);
            }
        }
        cache.put(player, result);
        return result;
    }
    public synchronized ProgressionProfile update(UUID player, UnaryOperator<ProgressionProfile> operation) throws IOException {
        ProgressionProfile previous = load(player);
        ProgressionProfile next = operation.apply(previous);
        if (next.equals(previous)) return previous;
        Files.createDirectories(directory);
        // Write only validated data to the backup, even when recovering a corrupted primary file.
        if (Files.exists(file(player))) writeAtomically(backup(player), encode(player, previous));
        writeAtomically(file(player), encode(player, next));
        cache.put(player, next);
        return next;
    }
    public synchronized void evict(UUID player) { cache.remove(player); }
    public Path file(UUID player) { return directory.resolve(player + ".json"); }
    private Path backup(UUID player) { return directory.resolve(player + ".json.bak"); }
    private static byte[] encode(UUID player, ProgressionProfile profile) {
        JsonObject json = new JsonObject();
        json.addProperty("schema_version", SCHEMA);
        json.addProperty("player_uuid", player.toString());
        json.addProperty("points", profile.points());
        json.add("milestones", GSON.toJsonTree(profile.milestones().stream().sorted().toList()));
        json.add("skills", GSON.toJsonTree(new java.util.TreeMap<>(profile.skills())));
        return (GSON.toJson(json) + "\n").getBytes(StandardCharsets.UTF_8);
    }
    private static ProgressionProfile read(Path file, UUID player) throws IOException {
        if (Files.size(file) > MAX_FILE_SIZE) throw new IOException("Progression profile exceeds size limit");
        try {
            JsonObject json = JsonParser.parseString(Files.readString(file, StandardCharsets.UTF_8)).getAsJsonObject();
            int schema = integer(json, "schema_version");
            if (schema != SCHEMA) throw new UnsupportedSchemaException();
            if (!player.toString().equals(json.get("player_uuid").getAsString())) throw new IllegalArgumentException("Profile UUID mismatch");
            int points = integer(json, "points");
            var milestones = new LinkedHashSet<String>();
            for (var id : json.getAsJsonArray("milestones")) milestones.add(id.getAsString());
            var skills = new LinkedHashMap<String, Integer>();
            for (var entry : json.getAsJsonObject("skills").entrySet()) {
                skills.put(entry.getKey(), entry.getValue().getAsBigDecimal().intValueExact());
            }
            return new ProgressionProfile(points, milestones, skills);
        } catch (UnsupportedSchemaException e) { throw e; }
        catch (RuntimeException e) { throw new IOException("Invalid progression profile", e); }
    }
    private static int integer(JsonObject json, String key) { return json.get(key).getAsBigDecimal().intValueExact(); }
    private static void writeAtomically(Path destination, byte[] data) throws IOException {
        Path temporary = Files.createTempFile(destination.getParent(), "profile-", ".tmp");
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
    private static final class UnsupportedSchemaException extends IOException {
        private UnsupportedSchemaException() { super("Unsupported progression profile schema; refusing to overwrite it"); }
    }
}

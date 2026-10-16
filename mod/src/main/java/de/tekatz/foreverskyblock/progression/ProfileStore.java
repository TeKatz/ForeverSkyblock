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
    private static final int SCHEMA = 5;
    private static final long MAX_FILE_SIZE = 1_048_576;
    private final Path directory;
    private final Map<UUID, ProgressionProfile> cache = new HashMap<>();
    private final FaultInjector faults;

    enum WriteStage { AFTER_BACKUP, AFTER_JOURNAL, AFTER_PRIMARY }
    @FunctionalInterface interface FaultInjector { void at(WriteStage stage) throws IOException; }

    public ProfileStore(Path directory) { this(directory, stage -> { }); }
    ProfileStore(Path directory, FaultInjector faults) { this.directory = directory; this.faults = faults; }
    public synchronized ProgressionProfile load(UUID player) throws IOException {
        ProgressionProfile recovered = recoverJournal(player);
        if (recovered != null) { cache.put(player, recovered); return recovered; }
        ProgressionProfile cached = cache.get(player);
        if (cached != null) return cached;
        Path file = file(player);
        Path backup = backup(player);
        ProgressionProfile result;
        if (!Files.exists(file) && !Files.exists(backup)) result = ProgressionProfile.empty();
        else {
            if (!Files.exists(file)) throw new IOException("Primary progression profile missing; recovery required");
            result = read(file, player);
        }
        cache.put(player, result);
        return result;
    }
    public synchronized ProgressionProfile update(UUID player, UnaryOperator<ProgressionProfile> operation) throws IOException {
        ProgressionProfile previous = load(player);
        ProgressionProfile next = operation.apply(previous);
        if (next.equals(previous)) return previous;
        byte[] encoded = encode(player, next);
        if (encoded.length > MAX_FILE_SIZE) throw new IOException("Progression profile exceeds size limit");
        Files.createDirectories(directory);
        try {
            if (Files.exists(file(player))) writeAtomically(backup(player), encode(player, previous));
            faults.at(WriteStage.AFTER_BACKUP);
            Files.createDirectories(journal(player).getParent());
            writeAtomically(journal(player), encodeJournal(player, encoded));
            faults.at(WriteStage.AFTER_JOURNAL);
            writeAtomically(file(player), encoded);
            faults.at(WriteStage.AFTER_PRIMARY);
            Files.deleteIfExists(journal(player));
            cache.put(player, next);
            return next;
        } catch (IOException | RuntimeException e) {
            cache.remove(player);
            throw e;
        }
    }
    public synchronized void evict(UUID player) { cache.remove(player); }
    public Path file(UUID player) { return directory.resolve(player + ".json"); }
    private Path backup(UUID player) { return directory.resolve(player + ".json.bak"); }
    Path journal(UUID player) { return directory.resolve("transactions").resolve(player + ".json"); }

    private static byte[] encode(UUID player, ProgressionProfile profile) {
        JsonObject json = new JsonObject();
        // Keep untouched/non-contract profiles on schema 4. The first durable contract state upgrades to schema 5.
        boolean hasContracts = !profile.meta().contracts().isEmpty() || !profile.meta().pendingContracts().isEmpty();
        json.addProperty("schema_version", hasContracts ? 5 : 4);
        json.addProperty("player_uuid", player.toString());
        json.addProperty("points", profile.points());
        json.addProperty("perk_points", profile.perkPoints());
        json.add("perk_investments", GSON.toJsonTree(new java.util.TreeMap<>(profile.perkInvestments())));
        json.add("redeemed_cores", GSON.toJsonTree(profile.redeemedCores().stream().map(UUID::toString).sorted().toList()));
        json.add("milestones", GSON.toJsonTree(profile.milestones().stream().sorted().toList()));
        json.add("skills", GSON.toJsonTree(new java.util.TreeMap<>(profile.skills())));
        JsonObject meta = new JsonObject();
        meta.addProperty("shop_tier", profile.meta().shopTier());
        JsonObject progress = new JsonObject();
        new java.util.TreeMap<>(profile.meta().progress()).forEach((id, state) -> {
            JsonObject value = new JsonObject(); value.addProperty("revision", state.revision());
            value.add("evidence", GSON.toJsonTree(state.evidence().stream().sorted().toList())); progress.add(id, value);
        });
        meta.add("challenge_progress", progress);
        meta.add("accounting", GSON.toJsonTree(profile.meta().accounting()));
        JsonObject worldClaims = new JsonObject();
        profile.meta().worldClaims().entrySet().stream().sorted(Map.Entry.comparingByKey()).forEach(entry ->
                worldClaims.add(entry.getKey().toString(), GSON.toJsonTree(entry.getValue().stream().sorted().toList())));
        meta.add("world_claims", worldClaims);
        JsonObject receipts = new JsonObject();
        profile.meta().purchaseReceipts().entrySet().stream().sorted(Map.Entry.comparingByKey()).forEach(entry -> {
            JsonObject receipt = new JsonObject();
            receipt.addProperty("world_uuid", entry.getValue().worldId().toString());
            receipt.addProperty("product_id", entry.getValue().productId());
            receipt.addProperty("sp_cost", entry.getValue().spCost());
            receipt.addProperty("required_tier", entry.getValue().requiredTier());
            receipts.add(entry.getKey().toString(), receipt);
        });
        meta.add("purchase_receipts", receipts);
        if (hasContracts) {
            JsonObject contracts = new JsonObject();
            new java.util.TreeMap<>(profile.meta().contracts()).forEach((id, state) -> {
                JsonObject value = new JsonObject(); value.addProperty("revision", state.revision());
                value.addProperty("progress", state.progress()); value.addProperty("completions", state.completions());
                value.add("recent_evidence", GSON.toJsonTree(state.recentEvidence()));
                value.add("recent_requests", GSON.toJsonTree(state.recentRequests().stream().map(UUID::toString).toList()));
                contracts.add(id, value);
            });
            meta.add("contracts", contracts);
            JsonObject pending = new JsonObject();
            profile.meta().pendingContracts().entrySet().stream().sorted(Map.Entry.comparingByKey()).forEach(entry -> {
                var state = entry.getValue(); JsonObject value = new JsonObject();
                value.addProperty("contract_id", state.contractId()); value.addProperty("item_id", state.itemId());
                value.addProperty("item_count", state.itemCount()); value.addProperty("before_count", state.beforeCount());
                value.addProperty("reward_sp", state.rewardSp()); value.addProperty("required_tier", state.requiredTier());
                pending.add(entry.getKey().toString(), value);
            });
            meta.add("pending_contracts", pending);
        }
        json.add("meta", meta);
        return (GSON.toJson(json) + "\n").getBytes(StandardCharsets.UTF_8);
    }
    private static byte[] encodeJournal(UUID player, byte[] nextProfile) {
        JsonObject journal = new JsonObject();
        journal.addProperty("journal_version", 1);
        journal.addProperty("player_uuid", player.toString());
        journal.add("next_profile", JsonParser.parseString(new String(nextProfile, StandardCharsets.UTF_8)));
        return (GSON.toJson(journal) + "\n").getBytes(StandardCharsets.UTF_8);
    }
    private static ProgressionProfile read(Path file, UUID player) throws IOException {
        if (Files.size(file) > MAX_FILE_SIZE) throw new IOException("Progression profile exceeds size limit");
        try { return read(JsonParser.parseString(Files.readString(file, StandardCharsets.UTF_8)).getAsJsonObject(), player); }
        catch (UnsupportedSchemaException e) { throw e; }
        catch (RuntimeException e) { throw new IOException("Invalid progression profile", e); }
    }
    private static ProgressionProfile read(JsonObject json, UUID player) throws IOException {
        try {
            int schema = integer(json, "schema_version");
            if (schema < 1 || schema > SCHEMA) throw new UnsupportedSchemaException();
            if (!player.toString().equals(json.get("player_uuid").getAsString())) throw new IllegalArgumentException("Profile UUID mismatch");
            int points = integer(json, "points");
            var milestones = new LinkedHashSet<String>();
            for (var id : json.getAsJsonArray("milestones")) milestones.add(id.getAsString());
            var skills = new LinkedHashMap<String, Integer>();
            for (var entry : json.getAsJsonObject("skills").entrySet()) skills.put(entry.getKey(), entry.getValue().getAsBigDecimal().intValueExact());
            if (schema == 1) {
                Integer creative = skills.remove("forever_skyblock:jetpacks_creative");
                if (creative != null) points = Math.addExact(points, Math.multiplyExact(creative, 40));
                return new ProgressionProfile(points, milestones, skills).withMeta(MetaProgress.empty(true)
                        .account(0, 0, 0, 0, creative == null ? 0 : Math.multiplyExact(creative, 40), 0, 0));
            }
            var paid = new LinkedHashMap<String, Integer>();
            for (var entry : json.getAsJsonObject("perk_investments").entrySet()) paid.put(entry.getKey(), entry.getValue().getAsBigDecimal().intValueExact());
            var coreReceipts = new LinkedHashSet<UUID>();
            for (var core : json.getAsJsonArray("redeemed_cores")) coreReceipts.add(UUID.fromString(core.getAsString()));
            MetaProgress meta = schema == 2 ? MetaProgress.empty(true) : readMeta(json.getAsJsonObject("meta"), schema);
            return new ProgressionProfile(points, milestones, skills, integer(json, "perk_points"), paid, coreReceipts, meta);
        } catch (UnsupportedSchemaException e) { throw e; }
        catch (RuntimeException e) { throw new IOException("Invalid progression profile", e); }
    }
    private static MetaProgress readMeta(JsonObject meta, int schema) {
        var progress = new LinkedHashMap<String, MetaProgress.Progress>();
        for (var entry : meta.getAsJsonObject("challenge_progress").entrySet()) {
            var state = entry.getValue().getAsJsonObject(); var tokens = new LinkedHashSet<String>();
            for (var token : state.getAsJsonArray("evidence")) {
                if (!token.isJsonPrimitive() || !token.getAsJsonPrimitive().isString() || !tokens.add(token.getAsString()))
                    throw new IllegalArgumentException("Invalid or duplicate evidence");
            }
            progress.put(entry.getKey(), new MetaProgress.Progress(integer(state, "revision"), tokens));
        }
        var a = meta.getAsJsonObject("accounting");
        if (!a.get("legacyHistoryUnknown").isJsonPrimitive() || !a.get("legacyHistoryUnknown").getAsJsonPrimitive().isBoolean())
            throw new IllegalArgumentException("Invalid history flag");
        var claims = new LinkedHashMap<UUID, java.util.Set<String>>();
        var purchaseReceipts = new LinkedHashMap<UUID, MetaProgress.PurchaseReceipt>();
        if (schema >= 4) {
            for (var entry : meta.getAsJsonObject("world_claims").entrySet()) {
                var products = new LinkedHashSet<String>();
                for (var product : entry.getValue().getAsJsonArray())
                    if (!product.isJsonPrimitive() || !product.getAsJsonPrimitive().isString() || !products.add(product.getAsString()))
                        throw new IllegalArgumentException("Invalid or duplicate world claim");
                claims.put(UUID.fromString(entry.getKey()), products);
            }
            for (var entry : meta.getAsJsonObject("purchase_receipts").entrySet()) {
                var receipt = entry.getValue().getAsJsonObject();
                purchaseReceipts.put(UUID.fromString(entry.getKey()), new MetaProgress.PurchaseReceipt(
                        UUID.fromString(receipt.get("world_uuid").getAsString()), receipt.get("product_id").getAsString(),
                        integer(receipt, "sp_cost"), integer(receipt, "required_tier")));
            }
        }
        var contracts = new LinkedHashMap<String, MetaProgress.ContractState>();
        var pendingContracts = new LinkedHashMap<UUID, MetaProgress.PendingContract>();
        if (schema >= 5) {
            for (var entry : meta.getAsJsonObject("contracts").entrySet()) {
                var state = entry.getValue().getAsJsonObject();
                var evidence = new java.util.ArrayList<String>();
                for (var value : state.getAsJsonArray("recent_evidence")) evidence.add(value.getAsString());
                var requests = new java.util.ArrayList<UUID>();
                for (var value : state.getAsJsonArray("recent_requests")) requests.add(UUID.fromString(value.getAsString()));
                contracts.put(entry.getKey(), new MetaProgress.ContractState(integer(state, "revision"), integer(state, "progress"),
                        total(state, "completions"), evidence, requests));
            }
            for (var entry : meta.getAsJsonObject("pending_contracts").entrySet()) {
                var state = entry.getValue().getAsJsonObject();
                pendingContracts.put(UUID.fromString(entry.getKey()), new MetaProgress.PendingContract(
                        state.get("contract_id").getAsString(), state.get("item_id").getAsString(), integer(state, "item_count"),
                        integer(state, "before_count"), integer(state, "reward_sp"), integer(state, "required_tier")));
            }
        }
        return new MetaProgress(integer(meta, "shop_tier"), progress, new MetaProgress.Accounting(
                total(a, "spEarned"), total(a, "ppEarned"), total(a, "spSpent"), total(a, "ppSpent"),
                total(a, "spRefunded"), total(a, "ppRefunded"), total(a, "spAdmin"), a.get("legacyHistoryUnknown").getAsBoolean()),
                claims, purchaseReceipts, contracts, pendingContracts);
    }
    private ProgressionProfile recoverJournal(UUID player) throws IOException {
        Path journal = journal(player);
        if (!Files.exists(journal)) return null;
        if (Files.size(journal) > MAX_FILE_SIZE + 4096) throw new IOException("Progression journal exceeds size limit");
        try {
            JsonObject wrapper = JsonParser.parseString(Files.readString(journal, StandardCharsets.UTF_8)).getAsJsonObject();
            if (integer(wrapper, "journal_version") != 1 || !player.toString().equals(wrapper.get("player_uuid").getAsString()))
                throw new IllegalArgumentException("Invalid progression journal identity");
            JsonObject nextJson = wrapper.getAsJsonObject("next_profile");
            ProgressionProfile next = read(nextJson, player);
            byte[] encoded = (GSON.toJson(nextJson) + "\n").getBytes(StandardCharsets.UTF_8);
            if (encoded.length > MAX_FILE_SIZE) throw new IOException("Progression profile exceeds size limit");
            Files.createDirectories(directory);
            writeAtomically(file(player), encoded);
            Files.delete(journal);
            return next;
        } catch (IOException e) { throw e; }
        catch (RuntimeException e) { throw new IOException("Invalid progression journal", e); }
    }
    private static long total(JsonObject json, String key) { return json.get(key).getAsBigDecimal().longValueExact(); }
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

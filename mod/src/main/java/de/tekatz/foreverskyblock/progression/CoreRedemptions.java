package de.tekatz.foreverskyblock.progression;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.UUID;

/** Durable authority-wide claims precede profile credit; replay is idempotent through profile receipts. */
public final class CoreRedemptions {
    public enum Result { CREDITED, ALREADY_CREDITED, CLAIMED_BY_OTHER }
    private final Path directory;
    private final ProfileStore profiles;
    public CoreRedemptions(Path directory, ProfileStore profiles) { this.directory = directory; this.profiles = profiles; }
    public synchronized Result redeem(UUID core, UUID player) throws IOException {
        Files.createDirectories(directory);
        Path claim = directory.resolve(core + ".claim");
        if (!Files.exists(claim)) {
            Path temporary = Files.createTempFile(directory, "claim-", ".tmp");
            try {
                try (var channel = FileChannel.open(temporary, StandardOpenOption.WRITE)) {
                    var buffer = ByteBuffer.wrap((player + "\n").getBytes(StandardCharsets.UTF_8));
                    while (buffer.hasRemaining()) channel.write(buffer);
                    channel.force(true);
                }
                // An exclusive hard link publishes the complete record without replacing another claimant.
                try { Files.createLink(claim, temporary); }
                catch (java.nio.file.FileAlreadyExistsException raced) { /* Validate the existing owner below. */ }
            } finally { Files.deleteIfExists(temporary); }
        }
        if (Files.size(claim) > 64) throw new IOException("Invalid Core claim");
        final UUID owner;
        try { owner = UUID.fromString(Files.readString(claim, StandardCharsets.UTF_8).strip()); }
        catch (IllegalArgumentException e) { throw new IOException("Invalid Core claim", e); }
        if (!owner.equals(player)) return Result.CLAIMED_BY_OTHER;
        var previous = profiles.load(player);
        profiles.update(player, state -> state.redeem(core));
        return previous.redeemedCores().contains(core) ? Result.ALREADY_CREDITED : Result.CREDITED;
    }
}

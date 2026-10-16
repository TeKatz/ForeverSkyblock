package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.MetaProgress;
import de.tekatz.foreverskyblock.progression.ProgressionProfile;
import de.tekatz.foreverskyblock.progression.ProgressionService;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;
import java.util.function.Consumer;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.neoforge.network.PacketDistributor;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;

public final class SkillNetwork {
    private static Consumer<Snapshot> clientReceiver = snapshot -> { };
    private SkillNetwork() { }
    public static void setClientReceiver(Consumer<Snapshot> receiver) { clientReceiver = receiver; }
    public static void register(RegisterPayloadHandlersEvent event) {
        var registrar = event.registrar("skills-6");
        registrar.playToServer(Request.TYPE, Request.CODEC, (request, context) -> {
            if (!(context.player() instanceof ServerPlayer player)) return;
            var service = ProgressionService.get(player.server);
            if (request.action() == 0) service.open(player);
            else if (request.action() == 1) service.buy(player, request.skillId());
            else if (request.action() == 2) service.reset(player);
            else if (request.action() == 3) service.testPoints(player);
            else if (request.action() == 4) service.buyProduct(player, request.skillId());
        });
        registrar.playToClient(Snapshot.TYPE, Snapshot.CODEC, (snapshot, context) -> clientReceiver.accept(snapshot));
    }
    public static void send(ServerPlayer player, ProgressionProfile profile, boolean local, boolean open, boolean available,
                            String message, WorldState world) {
        // Server-side actions can finish while a player has no live connection (disconnect boundary and GameTests).
        // Persistence remains authoritative; the next login/open request supplies a fresh snapshot.
        if (player.connection == null || !player.connection.getConnection().isConnected()
                || !net.neoforged.neoforge.network.registration.NetworkRegistry.hasChannel(player.connection, Snapshot.TYPE.id())) return;
        var skills = new LinkedHashMap<String, Integer>();
        for (var skill : SkillCatalog.SKILLS) if (profile.rank(skill.id()) > 0) skills.put(skill.id(), profile.rank(skill.id()));
        var milestones = new LinkedHashSet<String>();
        for (var milestone : MilestoneCatalog.MILESTONES) if (profile.milestones().contains(milestone.id())) milestones.add(milestone.id());
        var progress = new LinkedHashMap<String, Integer>();
        for (var milestone : MilestoneCatalog.MILESTONES) {
            var state = profile.meta().progress().get(milestone.id());
            progress.put(milestone.id(), profile.milestones().contains(milestone.id()) ? milestone.target()
                    : state == null ? 0 : state.revision() != 1 ? -1 : Math.min(milestone.target(), state.evidence().size()));
        }
        PacketDistributor.sendToPlayer(player, new Snapshot(profile.points(), profile.perkPoints(), skills, milestones, local, open,
                available, message, ProgressionService.canTestPoints(player), profile.meta().shopTier(), progress,
                profile.meta().accounting(), world.available(), world.name(), world.claims()));
    }
    public record WorldState(boolean available, String name, Set<String> claims) {
        public WorldState { name = name == null ? "" : name; claims = Set.copyOf(claims); }
    }
    public record Request(int action, String skillId) implements CustomPacketPayload {
        public static final Type<Request> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "skill_request"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Request> CODEC = new StreamCodec<>() {
            @Override public Request decode(RegistryFriendlyByteBuf buffer) { return new Request(buffer.readUnsignedByte(), buffer.readUtf(128)); }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Request request) { buffer.writeByte(request.action()); buffer.writeUtf(request.skillId(), 128); }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
    public record Snapshot(int points, int perkPoints, Map<String, Integer> skills, Set<String> milestones, boolean local, boolean open,
                           boolean available, String message, boolean canTestPoints, int shopTier, Map<String, Integer> progress,
                           MetaProgress.Accounting accounting, boolean worldAvailable, String worldName,
                           Set<String> worldClaims) implements CustomPacketPayload {
        public Snapshot {
            skills = Map.copyOf(skills); milestones = Set.copyOf(milestones); progress = Map.copyOf(progress); worldClaims = Set.copyOf(worldClaims);
            if (shopTier < -1 || shopTier > 4 || progress.size() > 128 || skills.size() > 128 || milestones.size() > 128
                    || worldClaims.size() > 128 || worldName == null || worldName.length() > 128)
                throw new IllegalArgumentException("Invalid progression snapshot");
            progress.forEach((id, count) -> {
                ProgressionProfile.validateId(id);
                if (count < -1 || count > 1000) throw new IllegalArgumentException("Invalid challenge progress");
            });
            for (String id : worldClaims) ProgressionProfile.validateId(id);
        }
        public Snapshot(int points, int perkPoints, Map<String, Integer> skills, Set<String> milestones, boolean local,
                        boolean open, boolean available, String message, boolean canTestPoints) {
            this(points, perkPoints, skills, milestones, local, open, available, message, canTestPoints,
                    de.tekatz.foreverskyblock.progression.ChallengeRules.qualifiedTier(milestones), Map.of(),
                    MetaProgress.Accounting.empty(true), false, "", Set.of());
        }
        public static final Type<Snapshot> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "skill_snapshot"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Snapshot> CODEC = new StreamCodec<>() {
            @Override public Snapshot decode(RegistryFriendlyByteBuf buffer) {
                int points = buffer.readVarInt();
                if (points < 0 || points > ProgressionProfile.MAX_POINTS) throw new IllegalArgumentException("Invalid skill balance");
                int perks = buffer.readVarInt();
                if (perks < 0 || perks > ProgressionProfile.MAX_POINTS) throw new IllegalArgumentException("Invalid perk balance");
                int flags = buffer.readUnsignedByte();
                String message = buffer.readUtf(128);
                int size = size(buffer);
                var skills = new LinkedHashMap<String, Integer>();
                for (int i = 0; i < size; i++) {
                    String id = buffer.readUtf(128); ProgressionProfile.validateId(id);
                    int rank = buffer.readVarInt();
                    if (rank < 1 || rank > 100) throw new IllegalArgumentException("Invalid skill rank");
                    skills.put(id, rank);
                }
                size = size(buffer);
                var milestones = new LinkedHashSet<String>();
                for (int i = 0; i < size; i++) { String id = buffer.readUtf(128); ProgressionProfile.validateId(id); milestones.add(id); }
                int tier = buffer.readVarInt();
                size = size(buffer);
                var progress = new LinkedHashMap<String, Integer>();
                for (int i = 0; i < size; i++) {
                    String id = buffer.readUtf(128); int count = buffer.readVarInt();
                    if (progress.put(id, count) != null) throw new IllegalArgumentException("Duplicate progress entry");
                }
                var accounting = new MetaProgress.Accounting(buffer.readVarLong(), buffer.readVarLong(), buffer.readVarLong(),
                        buffer.readVarLong(), buffer.readVarLong(), buffer.readVarLong(), buffer.readVarLong(), buffer.readBoolean());
                boolean worldAvailable = buffer.readBoolean();
                String worldName = buffer.readUtf(128);
                size = size(buffer);
                var worldClaims = new LinkedHashSet<String>();
                for (int i = 0; i < size; i++) {
                    String id = buffer.readUtf(128); ProgressionProfile.validateId(id);
                    if (!worldClaims.add(id)) throw new IllegalArgumentException("Duplicate world claim");
                }
                return new Snapshot(points, perks, skills, milestones, (flags & 1) != 0, (flags & 2) != 0,
                        (flags & 4) != 0, message, (flags & 8) != 0, tier, progress, accounting,
                        worldAvailable, worldName, worldClaims);
            }
            private int size(RegistryFriendlyByteBuf buffer) {
                int count = buffer.readVarInt();
                if (count < 0 || count > 128) throw new IllegalArgumentException("Progression packet too large");
                return count;
            }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Snapshot value) {
                buffer.writeVarInt(value.points());
                buffer.writeVarInt(value.perkPoints());
                buffer.writeByte((value.local() ? 1 : 0) | (value.open() ? 2 : 0) | (value.available() ? 4 : 0) | (value.canTestPoints() ? 8 : 0));
                buffer.writeUtf(value.message(), 128);
                buffer.writeVarInt(value.skills().size());
                value.skills().forEach((id, rank) -> { buffer.writeUtf(id, 128); buffer.writeVarInt(rank); });
                buffer.writeVarInt(value.milestones().size());
                value.milestones().forEach(id -> buffer.writeUtf(id, 128));
                buffer.writeVarInt(value.shopTier());
                buffer.writeVarInt(value.progress().size());
                value.progress().forEach((id, count) -> { buffer.writeUtf(id, 128); buffer.writeVarInt(count); });
                var a = value.accounting();
                for (long total : new long[]{a.spEarned(), a.ppEarned(), a.spSpent(), a.ppSpent(), a.spRefunded(), a.ppRefunded(), a.spAdmin()}) buffer.writeVarLong(total);
                buffer.writeBoolean(a.legacyHistoryUnknown());
                buffer.writeBoolean(value.worldAvailable());
                buffer.writeUtf(value.worldName(), 128);
                buffer.writeVarInt(value.worldClaims().size());
                value.worldClaims().forEach(id -> buffer.writeUtf(id, 128));
            }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
        public int rank(String skill) { return skills.getOrDefault(skill, 0); }
    }
}

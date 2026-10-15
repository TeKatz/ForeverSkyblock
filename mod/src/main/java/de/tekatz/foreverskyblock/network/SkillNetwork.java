package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
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
        var registrar = event.registrar("skills-2");
        registrar.playToServer(Request.TYPE, Request.CODEC, (request, context) -> {
            if (!(context.player() instanceof ServerPlayer player)) return;
            var service = ProgressionService.get(player.server);
            if (request.action() == 0) service.open(player);
            else if (request.action() == 1) service.buy(player, request.skillId());
            else if (request.action() == 2) service.reset(player);
        });
        registrar.playToClient(Snapshot.TYPE, Snapshot.CODEC, (snapshot, context) -> clientReceiver.accept(snapshot));
    }
    public static void send(ServerPlayer player, ProgressionProfile profile, boolean local, boolean open, boolean available, String message) {
        var skills = new LinkedHashMap<String, Integer>();
        for (var skill : SkillCatalog.SKILLS) if (profile.rank(skill.id()) > 0) skills.put(skill.id(), profile.rank(skill.id()));
        var milestones = new LinkedHashSet<String>();
        for (var milestone : MilestoneCatalog.MILESTONES) if (profile.milestones().contains(milestone.id())) milestones.add(milestone.id());
        PacketDistributor.sendToPlayer(player, new Snapshot(profile.points(), skills, milestones, local, open, available, message));
    }
    public record Request(int action, String skillId) implements CustomPacketPayload {
        public static final Type<Request> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "skill_request"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Request> CODEC = new StreamCodec<>() {
            @Override public Request decode(RegistryFriendlyByteBuf buffer) { return new Request(buffer.readUnsignedByte(), buffer.readUtf(128)); }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Request request) { buffer.writeByte(request.action()); buffer.writeUtf(request.skillId(), 128); }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
    public record Snapshot(int points, Map<String, Integer> skills, Set<String> milestones, boolean local, boolean open,
                           boolean available, String message) implements CustomPacketPayload {
        public Snapshot { skills = Map.copyOf(skills); milestones = Set.copyOf(milestones); }
        public static final Type<Snapshot> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "skill_snapshot"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Snapshot> CODEC = new StreamCodec<>() {
            @Override public Snapshot decode(RegistryFriendlyByteBuf buffer) {
                int points = buffer.readVarInt();
                if (points < 0 || points > ProgressionProfile.MAX_POINTS) throw new IllegalArgumentException("Invalid skill balance");
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
                return new Snapshot(points, skills, milestones, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, message);
            }
            private int size(RegistryFriendlyByteBuf buffer) {
                int count = buffer.readVarInt();
                if (count < 0 || count > 128) throw new IllegalArgumentException("Progression packet too large");
                return count;
            }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Snapshot value) {
                buffer.writeVarInt(value.points());
                buffer.writeByte((value.local() ? 1 : 0) | (value.open() ? 2 : 0) | (value.available() ? 4 : 0));
                buffer.writeUtf(value.message(), 128);
                buffer.writeVarInt(value.skills().size());
                value.skills().forEach((id, rank) -> { buffer.writeUtf(id, 128); buffer.writeVarInt(rank); });
                buffer.writeVarInt(value.milestones().size());
                value.milestones().forEach(id -> buffer.writeUtf(id, 128));
            }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
        public int rank(String skill) { return skills.getOrDefault(skill, 0); }
    }
}

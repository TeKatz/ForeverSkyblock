package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationRegistry;
import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationWire;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.network.PacketDistributor;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;
import net.neoforged.neoforge.network.registration.NetworkRegistry;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;

/** S2C synchronization for the immutable server-data animation snapshot loaded in Phase 17D.2. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD)
public final class ForeverStudioAnimationNetwork {
    static final int CHUNK_CHARS = 12_000;
    static final int MAX_CHUNKS = 128;
    private static Consumer<SyncChunk> clientReceiver = ignored -> { };
    private static long cachedGeneration = Long.MIN_VALUE;
    private static List<String> cachedChunks = List.of();

    private ForeverStudioAnimationNetwork() { }

    public static void setClientReceiver(Consumer<SyncChunk> receiver) {
        clientReceiver = receiver == null ? ignored -> { } : receiver;
    }

    @SubscribeEvent
    public static void register(RegisterPayloadHandlersEvent event) {
        var registrar = event.registrar("forever-studio-animation-1");
        registrar.playToClient(SyncChunk.TYPE, SyncChunk.CODEC, (payload, context) -> clientReceiver.accept(payload));
    }

    public static void send(ServerPlayer player) {
        if (player == null || player.connection == null || !player.connection.getConnection().isConnected()
                || !NetworkRegistry.hasChannel(player.connection, SyncChunk.TYPE.id()))
            return;

        long generation = ForeverStudioAnimationRegistry.generation();
        List<String> chunks = snapshotChunks(generation);
        for (int index = 0; index < chunks.size(); index++)
            PacketDistributor.sendToPlayer(player, new SyncChunk(generation, index, chunks.size(), chunks.get(index)));
    }

    private static synchronized List<String> snapshotChunks(long generation) {
        if (generation == cachedGeneration)
            return cachedChunks;

        String json = ForeverStudioAnimationWire.encodeSnapshot(ForeverStudioAnimationRegistry.all());
        List<String> chunks = split(json);
        if (chunks.size() > MAX_CHUNKS)
            throw new IllegalStateException("Forever Studio animation snapshot requires too many network chunks");
        cachedGeneration = generation;
        cachedChunks = List.copyOf(chunks);
        return cachedChunks;
    }

    static List<String> split(String value) {
        if (value.isEmpty()) return List.of("");
        List<String> result = new ArrayList<>();
        int start = 0;
        while (start < value.length()) {
            int end = Math.min(value.length(), start + CHUNK_CHARS);
            if (end < value.length() && end > start && Character.isHighSurrogate(value.charAt(end - 1)))
                end--;
            result.add(value.substring(start, end));
            start = end;
        }
        return result;
    }

    public record SyncChunk(long generation, int index, int total, String data) implements CustomPacketPayload {
        public SyncChunk {
            if (generation < 0 || index < 0 || total < 1 || total > MAX_CHUNKS || index >= total
                    || data == null || data.length() > CHUNK_CHARS)
                throw new IllegalArgumentException("Invalid Forever Studio animation sync chunk");
        }

        public static final Type<SyncChunk> TYPE = new Type<>(
                ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "studio_animation_sync"));
        public static final StreamCodec<RegistryFriendlyByteBuf, SyncChunk> CODEC = new StreamCodec<>() {
            @Override public SyncChunk decode(RegistryFriendlyByteBuf buffer) {
                return new SyncChunk(buffer.readVarLong(), buffer.readVarInt(), buffer.readVarInt(), buffer.readUtf(CHUNK_CHARS));
            }
            @Override public void encode(RegistryFriendlyByteBuf buffer, SyncChunk value) {
                buffer.writeVarLong(value.generation());
                buffer.writeVarInt(value.index());
                buffer.writeVarInt(value.total());
                buffer.writeUtf(value.data(), CHUNK_CHARS);
            }
        };

        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
}

package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.ApotheosisStage;
import java.util.function.Consumer;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.network.PacketDistributor;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD)
public final class ApotheosisNetwork {
    private static Consumer<ApotheosisStage> clientReceiver = stage -> { };

    private ApotheosisNetwork() { }

    public static void setClientReceiver(Consumer<ApotheosisStage> receiver) {
        clientReceiver = receiver == null ? stage -> { } : receiver;
    }

    @SubscribeEvent
    public static void register(RegisterPayloadHandlersEvent event) {
        event.registrar("apotheosis-progression-1")
                .playToClient(StagePayload.TYPE, StagePayload.CODEC,
                        (payload, context) -> clientReceiver.accept(payload.stage()));
    }

    public static void send(ServerPlayer player, ApotheosisStage stage) {
        if (player.connection == null || !player.connection.getConnection().isConnected()) return;
        if (!net.neoforged.neoforge.network.registration.NetworkRegistry.hasChannel(player.connection, StagePayload.TYPE.id())) return;
        PacketDistributor.sendToPlayer(player, new StagePayload(stage));
    }

    public record StagePayload(ApotheosisStage stage) implements CustomPacketPayload {
        public StagePayload {
            if (stage == null) throw new IllegalArgumentException("Missing Apotheosis stage");
        }

        public static final Type<StagePayload> TYPE = new Type<>(
                ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "apotheosis_stage"));
        public static final StreamCodec<RegistryFriendlyByteBuf, StagePayload> CODEC = new StreamCodec<>() {
            @Override
            public StagePayload decode(RegistryFriendlyByteBuf buffer) {
                int ordinal = buffer.readUnsignedByte();
                if (ordinal < 0 || ordinal >= ApotheosisStage.values().length)
                    throw new IllegalArgumentException("Invalid Apotheosis stage");
                return new StagePayload(ApotheosisStage.values()[ordinal]);
            }

            @Override
            public void encode(RegistryFriendlyByteBuf buffer, StagePayload payload) {
                buffer.writeByte(payload.stage().ordinal());
            }
        };

        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
}
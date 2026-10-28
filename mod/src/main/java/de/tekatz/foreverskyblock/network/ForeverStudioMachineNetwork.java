package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerLevel;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.network.PacketDistributor;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;
import net.neoforged.neoforge.network.registration.NetworkRegistry;

import java.util.Arrays;
import java.util.Map;
import java.util.WeakHashMap;
import java.util.function.Consumer;

/** Compact S2C state stream used by the generic Studio block renderer. */
@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, bus = EventBusSubscriber.Bus.MOD)
public final class ForeverStudioMachineNetwork {
    private static final int MAX_TANKS = ForeverStudioMachineBlockEntity.MAX_SYNC_TANKS;
    private static final int MAX_ANIMATIONS = ForeverStudioMachineBlockEntity.MAX_SYNC_ANIMATIONS;
    private static final Map<ForeverStudioMachineBlockEntity, Integer> LAST_HASH = new WeakHashMap<>();
    private static Consumer<State> clientReceiver = ignored -> { };

    private ForeverStudioMachineNetwork() { }

    public static void setClientReceiver(Consumer<State> receiver) {
        clientReceiver = receiver == null ? ignored -> { } : receiver;
    }

    @SubscribeEvent
    public static void register(RegisterPayloadHandlersEvent event) {
        event.registrar("forever-studio-machine-1")
                .playToClient(State.TYPE, State.CODEC, (payload, context) -> clientReceiver.accept(payload));
    }

    /** Called from the server ticker. State is sampled at most every four ticks and only sent on change. */
    public static void tick(ForeverStudioMachineBlockEntity machine) {
        if (!(machine.getLevel() instanceof ServerLevel level)) return;
        long gameTime = level.getGameTime();
        if ((gameTime & 3L) != (machine.getBlockPos().asLong() & 3L)) return;

        State state = State.capture(machine);
        int hash = state.runtimeHash();
        Integer previous = LAST_HASH.put(machine, hash);
        if (previous != null && previous == hash) return;

        for (var player : level.players()) {
            if (player.distanceToSqr(machine.getBlockPos().getX() + 0.5,
                    machine.getBlockPos().getY() + 0.5,
                    machine.getBlockPos().getZ() + 0.5) > 192.0 * 192.0) continue;
            if (player.connection == null || !player.connection.getConnection().isConnected()
                    || !NetworkRegistry.hasChannel(player.connection, State.TYPE.id())) continue;
            PacketDistributor.sendToPlayer(player, state);
        }
    }

    public record State(BlockPos pos, int progress, int duration, int status, int energy, int stateIndex,
                        int[] tankAmounts, int[] animationPhases) implements CustomPacketPayload {
        public State {
            pos = pos.immutable();
            duration = Math.max(1, duration);
            tankAmounts = tankAmounts == null ? new int[0] : tankAmounts.clone();
            animationPhases = animationPhases == null ? new int[0] : animationPhases.clone();
            if (tankAmounts.length > MAX_TANKS || animationPhases.length > MAX_ANIMATIONS)
                throw new IllegalArgumentException("Studio machine sync exceeds runtime limits");
        }

        @Override public int[] tankAmounts() { return tankAmounts.clone(); }
        @Override public int[] animationPhases() { return animationPhases.clone(); }

        public int runtimeHash() {
            int result = pos.hashCode();
            result = 31 * result + progress;
            result = 31 * result + duration;
            result = 31 * result + status;
            result = 31 * result + energy;
            result = 31 * result + stateIndex;
            result = 31 * result + Arrays.hashCode(tankAmounts);
            result = 31 * result + Arrays.hashCode(animationPhases);
            return result;
        }

        public double animationPhase(int index) {
            return index >= 0 && index < animationPhases.length
                    ? Math.clamp(animationPhases[index] / 10_000.0, 0.0, 1.0) : 0.0;
        }

        public static State capture(ForeverStudioMachineBlockEntity machine) {
            int[] tanks = new int[Math.min(MAX_TANKS, machine.definition().tankCount())];
            for (int i = 0; i < tanks.length; i++) tanks[i] = machine.fluidAmount(i);
            int[] animations = new int[Math.min(MAX_ANIMATIONS, machine.definition().animations().size())];
            for (int i = 0; i < animations.length; i++)
                animations[i] = (int)Math.round(Math.clamp(machine.animationPhase(i), 0.0, 1.0) * 10_000.0);
            return new State(machine.getBlockPos(), machine.progress(), machine.duration(), machine.status(),
                    machine.energyStored(), machine.stateIndex(), tanks, animations);
        }

        public static final Type<State> TYPE = new Type<>(
                ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "studio_machine_state"));

        public static final StreamCodec<RegistryFriendlyByteBuf, State> CODEC = new StreamCodec<>() {
            @Override public State decode(RegistryFriendlyByteBuf buffer) {
                BlockPos pos = buffer.readBlockPos();
                int progress = buffer.readVarInt();
                int duration = buffer.readVarInt();
                int status = buffer.readVarInt();
                int energy = buffer.readVarInt();
                int stateIndex = buffer.readVarInt();
                int tankCount = buffer.readVarInt();
                if (tankCount < 0 || tankCount > MAX_TANKS) throw new IllegalArgumentException("Invalid Studio tank sync count");
                int[] tanks = new int[tankCount];
                for (int i = 0; i < tankCount; i++) tanks[i] = buffer.readVarInt();
                int animationCount = buffer.readVarInt();
                if (animationCount < 0 || animationCount > MAX_ANIMATIONS) throw new IllegalArgumentException("Invalid Studio animation sync count");
                int[] animations = new int[animationCount];
                for (int i = 0; i < animationCount; i++) animations[i] = buffer.readVarInt();
                return new State(pos, progress, duration, status, energy, stateIndex, tanks, animations);
            }

            @Override public void encode(RegistryFriendlyByteBuf buffer, State state) {
                buffer.writeBlockPos(state.pos());
                buffer.writeVarInt(state.progress());
                buffer.writeVarInt(state.duration());
                buffer.writeVarInt(state.status());
                buffer.writeVarInt(state.energy());
                buffer.writeVarInt(state.stateIndex());
                buffer.writeVarInt(state.tankAmounts.length);
                for (int value : state.tankAmounts) buffer.writeVarInt(value);
                buffer.writeVarInt(state.animationPhases.length);
                for (int value : state.animationPhases) buffer.writeVarInt(value);
            }
        };

        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
}

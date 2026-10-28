package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.network.ForeverStudioMachineNetwork;
import net.minecraft.core.BlockPos;

import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

/** Client-only runtime state cache populated by ForeverStudioMachineNetwork. */
public final class ForeverStudioMachineClientState {
    private static final Map<BlockPos, ForeverStudioMachineNetwork.State> STATES = new ConcurrentHashMap<>();

    private ForeverStudioMachineClientState() { }

    public static void accept(ForeverStudioMachineNetwork.State state) {
        if (state != null) STATES.put(state.pos(), state);
    }

    public static Optional<ForeverStudioMachineNetwork.State> get(BlockPos pos) {
        return Optional.ofNullable(STATES.get(pos));
    }

    public static void clear() { STATES.clear(); }
}

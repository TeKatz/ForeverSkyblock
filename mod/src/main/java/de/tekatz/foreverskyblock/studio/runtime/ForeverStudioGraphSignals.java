package de.tekatz.foreverskyblock.studio.runtime;

import java.util.Collections;
import java.util.Map;
import java.util.WeakHashMap;

/**
 * Tick-scoped edge detector for event-style Function Graph nodes.
 *
 * <p>The graph evaluator runs before the processor mutates machine state for the current tick. By
 * comparing the machine snapshot visible at the beginning of consecutive ticks we get stable,
 * one-tick pulses for recipe start/completion, redstone changes and semantic state transitions.
 * Neighbor updates are pushed explicitly by {@link ForeverStudioMachineBlock}.</p>
 */
final class ForeverStudioGraphSignals {
    private static final Map<ForeverStudioMachineBlockEntity, State> STATES =
            Collections.synchronizedMap(new WeakHashMap<>());

    private ForeverStudioGraphSignals() { }

    static void beginTick(ForeverStudioMachineBlockEntity machine) {
        long tick = machine.getLevel() == null ? Long.MIN_VALUE : machine.getLevel().getGameTime();
        State state = state(machine);
        if (state.tick == tick) return;

        int redstone = machine.redstoneStrength();
        int status = machine.status();
        int progress = machine.progress();
        String semanticState = machine.stateId();

        if (state.initialized) {
            state.redstoneChanged = redstone != state.redstone;
            state.recipeStarted = status == ForeverStudioMachineBlockEntity.RUNNING
                    && state.status != ForeverStudioMachineBlockEntity.RUNNING
                    && progress > 0;
            state.recipeCompleted = status == ForeverStudioMachineBlockEntity.RUNNING
                    && state.progress > 0 && progress == 0;
            state.stateEntered = !semanticState.equalsIgnoreCase(state.semanticState);
        } else {
            state.redstoneChanged = false;
            state.recipeStarted = false;
            state.recipeCompleted = false;
            state.stateEntered = false;
            state.initialized = true;
        }

        state.neighborChanged = state.pendingNeighborChanged;
        state.pendingNeighborChanged = false;
        state.redstone = redstone;
        state.status = status;
        state.progress = progress;
        state.semanticState = semanticState;
        state.tick = tick;
    }

    static void markNeighborChanged(ForeverStudioMachineBlockEntity machine) {
        state(machine).pendingNeighborChanged = true;
    }

    static boolean redstoneChanged(ForeverStudioMachineBlockEntity machine) {
        beginTick(machine);
        return state(machine).redstoneChanged;
    }

    static boolean neighborChanged(ForeverStudioMachineBlockEntity machine) {
        beginTick(machine);
        return state(machine).neighborChanged;
    }

    static boolean recipeStarted(ForeverStudioMachineBlockEntity machine) {
        beginTick(machine);
        return state(machine).recipeStarted;
    }

    static boolean recipeCompleted(ForeverStudioMachineBlockEntity machine) {
        beginTick(machine);
        return state(machine).recipeCompleted;
    }

    static boolean stateEntered(ForeverStudioMachineBlockEntity machine) {
        beginTick(machine);
        return state(machine).stateEntered;
    }

    private static State state(ForeverStudioMachineBlockEntity machine) {
        synchronized (STATES) {
            return STATES.computeIfAbsent(machine, ignored -> new State());
        }
    }

    private static final class State {
        private boolean initialized;
        private long tick = Long.MIN_VALUE;
        private int redstone;
        private int status;
        private int progress;
        private String semanticState = "";
        private boolean pendingNeighborChanged;
        private boolean redstoneChanged;
        private boolean neighborChanged;
        private boolean recipeStarted;
        private boolean recipeCompleted;
        private boolean stateEntered;
    }
}

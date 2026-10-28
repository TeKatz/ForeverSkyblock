package de.tekatz.foreverskyblock.studio.runtime;

/**
 * Source-level compatibility marker consumed by Forever Studio before it emits runtime-machine
 * output. Bump this value whenever the generated runtime contract requires Java support that an
 * older repository checkout cannot provide.
 */
public final class ForeverStudioRuntimeContract {
    public static final int COMPILER_CONTRACT_VERSION = 2;

    private ForeverStudioRuntimeContract() {
    }
}

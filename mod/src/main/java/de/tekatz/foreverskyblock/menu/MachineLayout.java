package de.tekatz.foreverskyblock.menu;

/**
 * Shared immutable native GUI coordinates.
 *
 * Machine screens intentionally do not apply their own viewport scaling. AbstractContainerScreen owns
 * centering, resize handling, slot coordinates and mouse coordinates so vanilla-rendered ItemStacks always
 * stay on the exact same coordinate grid as the custom slot backgrounds.
 */
public record MachineLayout(int width, int height, int inventoryY, int hotbarY) {
    // MK II uses the extra vertical room for its automation/upgrade panel. Keeping the same layout for MK I
    // avoids a second menu protocol and leaves a compact "MK II only" explanation instead of dead controls.
    public static final MachineLayout CONDENSER = new MachineLayout(248, 352, 260, 324);
    public static final MachineLayout WASHING = new MachineLayout(248, 286, 190, 254);
    public static final MachineLayout ASCENSION = new MachineLayout(248, 270, 176, 240);

    public int inventoryX(int column) { return 24 + column * 22; }
    public int inventoryRowY(int row) { return inventoryY + row * 20; }
}

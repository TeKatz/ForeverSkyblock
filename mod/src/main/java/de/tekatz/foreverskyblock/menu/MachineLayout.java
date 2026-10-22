package de.tekatz.foreverskyblock.menu;

/**
 * Common logical coordinates shared by machine menus and screens.
 *
 * These values are always native AbstractContainerScreen coordinates. Do not add per-screen scaling or
 * viewport remapping here: Slot.x/Slot.y, custom slot frames, JEI areas and mouse interaction must stay on
 * the same leftPos/topPos coordinate grid so resizing the game window cannot introduce item/slot drift.
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

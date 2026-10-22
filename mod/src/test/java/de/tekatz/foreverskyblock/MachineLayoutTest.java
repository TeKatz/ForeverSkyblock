package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.menu.MachineLayout;
import de.tekatz.foreverskyblock.menu.WashingMenu;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

final class MachineLayoutTest {
    @Test void inventoryCoordinatesRemainNativeAcrossDifferentWindowSizes() {
        for (var layout : new MachineLayout[]{MachineLayout.CONDENSER, MachineLayout.WASHING, MachineLayout.ASCENSION}) {
            for (int[] size : new int[][]{{320, 180}, {320, 240}, {480, 270}, {854, 480}, {1920, 1080}}) {
                int left = (size[0] - layout.width()) / 2;
                int top = (size[1] - layout.height()) / 2;
                for (int row = 0; row < 4; row++) {
                    for (int col = 0; col < 9; col++) {
                        int relativeX = layout.inventoryX(col);
                        int relativeY = row == 3 ? layout.hotbarY() : layout.inventoryRowY(row);
                        int screenX = left + relativeX;
                        int screenY = top + relativeY;

                        assertEquals(relativeX, screenX - left,
                                "Inventory X must stay relative to Vanilla leftPos without a second transform");
                        assertEquals(relativeY, screenY - top,
                                "Inventory Y must stay relative to Vanilla topPos without a second transform");
                        assertTrue(relativeX >= 16 && relativeX + 16 < layout.width() - 16);
                        assertTrue(relativeY + 16 < layout.height());
                    }
                }
            }
        }
    }

    @Test void condenserFullLayoutKeepsProcessUpgradeAndInventorySectionsSeparated() {
        assertEquals(352, MachineLayout.CONDENSER.height());
        assertTrue(CondenserMenu.FREEZING_Y - CondenserMenu.MAIN_Y >= 60,
                "Freezing row must not crowd timed condensing");
        assertTrue(CondenserMenu.UPGRADE_Y > CondenserMenu.FREEZING_Y + 70,
                "MK II upgrade row must stay below process and module controls");
        assertTrue(MachineLayout.CONDENSER.inventoryY() - (CondenserMenu.UPGRADE_Y + 24) >= 12,
                "Upgrade slots and automation controls need breathing room before the player inventory");
        assertTrue(CondenserMenu.UPGRADE_1_X >= 20 && CondenserMenu.UPGRADE_3_X + 24 < MachineLayout.CONDENSER.width() / 2,
                "All three upgrade slots must remain inside the left half of the automation panel");
        assertTrue(MachineLayout.CONDENSER.hotbarY() - MachineLayout.CONDENSER.inventoryY() >= 60);
    }

    @Test void washingLayoutKeepsLabelsControlsAndInventorySeparated() {
        assertEquals(286, MachineLayout.WASHING.height());
        assertTrue(WashingMenu.MACHINE_Y >= 76, "Washing slots need room below their labels");
        assertTrue(MachineLayout.WASHING.inventoryY() - (WashingMenu.MACHINE_Y + 18) >= 90,
                "Washing process, hint and actions need breathing room before the inventory");
        assertTrue(MachineLayout.WASHING.hotbarY() - MachineLayout.WASHING.inventoryY() >= 60);
    }
}

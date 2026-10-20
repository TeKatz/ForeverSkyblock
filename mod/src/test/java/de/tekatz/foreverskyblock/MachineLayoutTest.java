package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.menu.MachineLayout;
import de.tekatz.foreverskyblock.menu.WashingMenu;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class MachineLayoutTest {
    @Test void allThirtySixSlotCentersRoundTripAtSmallAndLargeGuiSizes() {
        for(var layout:new MachineLayout[]{MachineLayout.CONDENSER,MachineLayout.WASHING,MachineLayout.ASCENSION})for(int[] size:new int[][]{{320,180},{320,240},{480,270},{854,480},{1920,1080}}) {
            int left=(size[0]-layout.width())/2,top=(size[1]-layout.height())/2;var v=layout.viewport(size[0],size[1],left,top);
            assertTrue(v.x()>=5.9&&v.y()>=5.9);assertTrue(v.x()+layout.width()*v.scale()<=size[0]-5.9);assertTrue(v.y()+layout.height()*v.scale()<=size[1]-5.9);
            for(int row=0;row<4;row++)for(int col=0;col<9;col++) {
                int x=left+layout.inventoryX(col)+8,y=top+(row==3?layout.hotbarY():layout.inventoryRowY(row))+8;
                assertEquals(x,v.logicalX(v.screenX(x)),.00001);assertEquals(y,v.logicalY(v.screenY(y)),.00001);
                assertTrue(layout.inventoryX(col)>=16&&layout.inventoryX(col)+16<layout.width()-16);assertTrue(y-top+8<layout.height());
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

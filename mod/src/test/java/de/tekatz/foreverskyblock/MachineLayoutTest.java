package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.menu.MachineLayout;
import de.tekatz.foreverskyblock.client.MachineAnimation;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class MachineLayoutTest {
    @Test void allThirtySixSlotCentersRoundTripAtSmallAndLargeGuiSizes() {
        for(var layout:new MachineLayout[]{MachineLayout.CONDENSER,MachineLayout.ASCENSION})for(int[] size:new int[][]{{320,180},{320,240},{480,270},{854,480},{1920,1080}}) {
            int left=(size[0]-layout.width())/2,top=(size[1]-layout.height())/2;var v=layout.viewport(size[0],size[1],left,top);
            assertTrue(v.x()>=5.9&&v.y()>=5.9);assertTrue(v.x()+layout.width()*v.scale()<=size[0]-5.9);assertTrue(v.y()+layout.height()*v.scale()<=size[1]-5.9);
            for(int row=0;row<4;row++)for(int col=0;col<9;col++) {
                int x=left+layout.inventoryX(col)+8,y=top+(row==3?layout.hotbarY():layout.inventoryRowY(row))+8;
                assertEquals(x,v.logicalX(v.screenX(x)),.00001);assertEquals(y,v.logicalY(v.screenY(y)),.00001);
                assertTrue(layout.inventoryX(col)>=16&&layout.inventoryX(col)+16<layout.width()-16);assertTrue(y-top+8<layout.height());
            }
        }
    }
    @Test void condenserFullLayoutKeepsProcessSectionsSeparatedFromInventory() {
        assertEquals(296, MachineLayout.CONDENSER.height());
        assertTrue(CondenserMenu.FREEZING_Y - CondenserMenu.MAIN_Y >= 60, "Freezing row must not crowd timed condensing");
        assertTrue(MachineLayout.CONDENSER.inventoryY() - (CondenserMenu.FREEZING_Y + 24) >= 50,
                "Machine controls need breathing room before the player inventory");
        assertTrue(MachineLayout.CONDENSER.hotbarY() - MachineLayout.CONDENSER.inventoryY() >= 60);
    }
    @Test void movingPressAndCounterRotatingFramesCannotIntersectTheirHousing() {
        for(int i=0;i<=1000;i++) {
            float compression=MachineAnimation.compression(i/1000F);assertTrue(compression>=0&&compression<=2.20001F);
            assertTrue(10-compression>=7.7999F);assertTrue(10-compression+1.35F<13);
            double theta=i*Math.PI/500;double outerExtent=4.5*(Math.abs(Math.cos(theta))+Math.abs(Math.sin(theta)));
            assertTrue(8-outerExtent>.5&&8+outerExtent<15.5);
        }
        assertEquals(0,MachineAnimation.compression(0),.0001);assertEquals(0,MachineAnimation.compression(1),.0001);
    }
}

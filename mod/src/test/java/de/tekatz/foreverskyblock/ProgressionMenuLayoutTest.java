package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.ProgressionMenuLayout;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class ProgressionMenuLayoutTest {
    @Test void cardsAndControlsFitSupportedSmallAndLargeGuiViewports() {
        for (int[] size : new int[][]{{320,180},{320,240},{480,270},{640,360},{960,540},{1920,1080}}) {
            var layout=ProgressionMenuLayout.fit(size[0],size[1]);
            assertTrue(layout.width()<=size[0]-12 && layout.height()<=size[1]-12);
            assertTrue(layout.contentWidth()>=280, "Keep native text readable in compact navigation");
            assertEquals(layout.width()-10,layout.contentX()+layout.contentWidth());
            for(boolean search:new boolean[]{false,true}) {
                assertTrue(layout.cardsHeight(search)>=24);
                assertTrue(layout.cardsTop(search)+layout.cardsHeight(search)<=layout.height()-38);
                assertTrue(layout.cardsTop(search)>=(search?86:62));
            }
            if(!layout.compact()) {
                assertTrue(42+5*24+20<layout.height()-38,"Six sidebar sections fit above footer");
                assertTrue(layout.contentX()>126,"Sidebar does not intersect cards");
            }
        }
    }
}

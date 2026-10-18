package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.MenuMotion;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class MenuMotionTest {
    @Test void wheelReversalsStayBoundedAndConvergeAtDifferentFrameRates() {
        for (int fps : new int[]{30, 60, 144}) {
            var scroll = new MenuMotion(); scroll.scroll(500, 180);
            for (int i=1; i<=fps; i++) assertTrue(scroll.advance(i*1_000_000_000L/fps)<=180);
            assertEquals(180,scroll.value(),.05);
            scroll.scroll(-999,180);
            for (int i=fps+1;i<=fps*2;i++) assertTrue(scroll.advance(i*1_000_000_000L/fps)>=0);
            assertEquals(0,scroll.value(),.05);
        }
    }
    @Test void circleCornersAreNotClickableAndRevealSettles() {
        assertTrue(MenuMotion.insideNode(12,0,0,0,18));
        assertFalse(MenuMotion.insideNode(17,17,0,0,18));
        assertEquals(0,MenuMotion.reveal(100,100));
        assertEquals(1,MenuMotion.reveal(100,300_000_100));
    }
}

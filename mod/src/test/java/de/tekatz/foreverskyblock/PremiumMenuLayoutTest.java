package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.PremiumMenuLayout;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class PremiumMenuLayoutTest {
    @Test void scaleThreeFullHdKeepsSidebarAndThreeChallengeColumns() {
        var layout = PremiumMenuLayout.fit(640, 360);
        assertEquals(630, layout.width());
        assertEquals(350, layout.height());
        assertTrue(layout.challengeSidebarWidth() >= 122);
        assertEquals(3, layout.challengeColumns());
        assertTrue(layout.challengeMainWidth() >= 440);
        assertTrue(layout.challengeGridHeight() >= 200);
    }

    @Test void largerGuiKeepsTheSameDesignStructure() {
        var layout = PremiumMenuLayout.fit(960, 540);
        assertEquals(944, layout.width());
        assertEquals(522, layout.height());
        assertTrue(layout.challengeSidebarWidth() > 0);
        assertEquals(3, layout.challengeColumns());
        assertTrue(layout.challengeCardHeight() >= 70);
    }

    @Test void onlyVerySmallViewportsCollapseTheSidebar() {
        var layout = PremiumMenuLayout.fit(430, 240);
        assertTrue(layout.tiny());
        assertEquals(0, layout.challengeSidebarWidth());
        assertTrue(layout.challengeColumns() >= 1);
    }
}

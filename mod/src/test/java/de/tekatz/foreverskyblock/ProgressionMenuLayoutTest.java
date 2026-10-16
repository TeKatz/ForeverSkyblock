package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.ProgressionMenuLayout;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

final class ProgressionMenuLayoutTest {
    @Test
    void designTwoLayoutKeepsPrimaryNavigationAndChallengeGridInsideViewport() {
        for (int[] size : new int[][]{{320,180},{320,240},{480,270},{640,360},{960,540},{1920,1080}}) {
            var layout = ProgressionMenuLayout.fit(size[0], size[1]);

            assertTrue(layout.width() <= size[0] - 12);
            assertTrue(layout.height() <= size[1] - 12);
            assertTrue(layout.navTop() >= 30);
            assertTrue(layout.navTop() + layout.navHeight() < layout.contentTop());
            assertTrue(layout.contentTop() < layout.height());
            assertTrue(layout.challengeMainX() >= layout.padding());
            assertEquals(layout.width() - layout.padding(), layout.challengeMainX() + layout.challengeMainWidth());
            assertTrue(layout.challengeGridTop() > layout.contentTop());
            assertTrue(layout.challengeGridTop() + layout.challengeGridHeight() <= layout.height() - layout.padding());
            assertTrue(layout.challengeColumns() >= 1 && layout.challengeColumns() <= 3);

            if (!layout.compact()) {
                assertEquals(194, layout.challengeSidebarWidth());
                assertEquals(3, layout.challengeColumns(), "960x540 target keeps the Design-2 three-column challenge grid");
            } else {
                assertEquals(0, layout.challengeSidebarWidth(), "Small viewports collapse only the filter sidebar, not text scale");
            }
            if (layout.tiny()) {
                assertTrue(layout.challengeGridHeight() >= 24, "Tiny fallback still leaves a scrollable challenge area");
            }
        }
    }
}

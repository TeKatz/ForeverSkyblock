package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.GardenCropProfile;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class GardenCropProfileTest {
    @Test void wheatStaysRootedBelowTheLampAndMaturesBeforeHarvest() {
        var crop = GardenCropProfile.WHEAT;
        for (var stalk : crop.stalks()) {
            assertTrue(Math.abs(stalk.x()) < 3 && Math.abs(stalk.z()) < 3);
            float previous = 0;
            for (int i = 0; i <= 1000; i++) {
                float phase = i / 1000F, growth = crop.growth(phase, stalk);
                assertTrue(growth >= previous && growth <= 1);
                assertTrue(3.25F + crop.height() * stalk.height() + 2.2F < 13.15F);
                assertTrue(crop.ear(phase, stalk) >= 0 && crop.ear(phase, stalk) <= 1);
                assertTrue(Math.abs(crop.sway(phase, stalk, true)) <= 2.2F);
                assertEquals(0, crop.sway(phase, stalk, false));
                previous = growth;
            }
            assertEquals(1, crop.growth(.86F, stalk));
            assertEquals(1, crop.ear(.86F, stalk));
            assertEquals(0, crop.sway(0, stalk, true));
            assertEquals(0, crop.sway(1, stalk, true));
        }
        assertEquals(crop.young(), crop.stemColor(0));
        assertEquals(crop.matureStem(), crop.stemColor(.86F));
        assertEquals(crop.matureGrain(), crop.grainColor(.86F));
    }
}

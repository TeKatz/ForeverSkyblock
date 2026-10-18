package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.MachineAnimation;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class MachineAnimationTest {
    @Test void pressFeedsHoldsReleasesAndReturnsBeforeNextCycle() {
        assertEquals(0, MachineAnimation.press(.1F));
        assertEquals(1, MachineAnimation.press(.55F));
        assertEquals(0, MachineAnimation.press(.9F));
        assertTrue(MachineAnimation.press(.35F) > 0 && MachineAnimation.press(.35F) < 1);
        for (int i = -100; i <= 1100; i++) {
            float stroke = MachineAnimation.press(i / 1000F);
            assertTrue(stroke >= 0 && stroke <= 1);
            assertTrue(10.4F - 2.6F * stroke >= 7.79F, "Ram must clear the workpiece and anvil");
        }
    }
    @Test void gardenIsRootedAndMatureBeforeHarvestSweep() {
        float previous = 0;
        for (int i = 0; i <= 1000; i++) {
            float phase = i / 1000F, growth = MachineAnimation.growth(phase);
            assertTrue(growth >= previous && growth <= 1);
            assertTrue(3.25F + 1.2F + 5.5F * growth + 2.2F < 13.15F,
                    "Crown must clear the grow-light carriage at every growth stage");
            if (phase <= .86F) assertEquals(0, MachineAnimation.harvest(phase));
            previous = growth;
        }
        assertEquals(1, MachineAnimation.growth(.86F));
        assertEquals(1, MachineAnimation.harvest(1));
    }
    @Test void phaseClampsAtCompletionInsteadOfInventingAnotherCycle() {
        assertEquals(0, MachineAnimation.phase(-1, 160));
        assertEquals(.5F, MachineAnimation.phase(160, 320));
        assertEquals(1, MachineAnimation.phase(9999, 320));
        assertEquals(1, MachineAnimation.phase(1, 0));
    }
}

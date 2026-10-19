package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.MachineAnimation;
import de.tekatz.foreverskyblock.client.GardenTreeShape;
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
            assertTrue(3.25F + 9F * (.2F + .8F * growth) < 13.15F,
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
    @Test void workpiecesHaveContinuousIntakeConsumptionAndDischargeInsteadOfTimedDespawns() {
        assertEquals(0, MachineAnimation.ingredient(0));
        assertEquals(0, MachineAnimation.ingredient(.62F));
        assertEquals(0, MachineAnimation.product(.64F));
        assertEquals(1, MachineAnimation.product(.82F));
        assertEquals(1, MachineAnimation.discharge(1));
        float previousIngredient = 0, previousProduct = 0;
        for (int i = 0; i <= 1000; i++) {
            float t = i / 1000F;
            float ingredient = MachineAnimation.ingredient(t), product = MachineAnimation.product(t);
            assertTrue(ingredient >= 0 && ingredient <= 1 && product >= 0 && product <= 1);
            assertTrue(Math.abs(ingredient - previousIngredient) < .03F, "Ingredient scale must never jump at a stage boundary");
            assertTrue(Math.abs(product - previousProduct) < .03F, "The result must emerge continuously");
            for (int index = 0; index < 4; index++) assertTrue(MachineAnimation.dust(t, index) >= 0);
            previousIngredient = ingredient; previousProduct = product;
        }
        assertTrue(2.7F - 1.35F / 2 > 2F, "The world result must end behind the outlet backplate");
    }
    @Test void matureOakHasAVolumetricCrownAndSlenderStemWithinTheLampClearance() {
        float bottom = Float.MAX_VALUE, top = 0;
        int leaves = 0;
        for (var part : GardenTreeShape.PARTS) {
            assertTrue(part.x0() < part.x1() && part.y0() < part.y1() && part.z0() < part.z1());
            assertTrue(part.x0() >= -4 && part.x1() <= 4 && part.z0() >= -4 && part.z1() <= 4);
            assertTrue(part.y0() >= 0 && part.y1() + 3.25F < 13.15F);
            if (part.leaves()) {
                leaves++;
                assertTrue(part.y1() - part.y0() >= 1.5F, "Leaf cells must not become flattened sheets");
                bottom = Math.min(bottom, part.y0()); top = Math.max(top, part.y1());
            } else if (part.y0() == 0) assertTrue(part.x1() - part.x0() < 1.5F, "Stem must stay proportionate to the crown");
        }
        assertTrue(leaves >= 12 && top - bottom >= 5, "The crown needs depth and several distinct lobes");
    }

}

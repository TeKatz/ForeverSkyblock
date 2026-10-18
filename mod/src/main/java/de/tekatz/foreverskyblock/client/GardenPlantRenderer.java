package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import net.minecraft.client.renderer.MultiBufferSource;

/** Rooted growth shared by the greenhouse and its miniature controller display. */
final class GardenPlantRenderer {
    private GardenPlantRenderer() { }
    static void tree(PoseStack p, MultiBufferSource buffers, float phase, int light, int overlay) {
        float growth = MachineAnimation.growth(phase);
        float crown = MachineAnimation.smooth(.20F, .78F, phase);
        float height = 1.2F + 5.5F * growth;
        float radius = .7F + 3.4F * crown;
        float width = .35F + .65F * growth;
        MachineGeometry.botanicalBox(p, buffers, false, -width, 0, -width, width, height, width, light, overlay);
        float canopy = height - .5F;
        MachineGeometry.botanicalBox(p, buffers, true, -radius, canopy, -radius, radius, canopy + 1.6F, radius, light, overlay);
        MachineGeometry.botanicalBox(p, buffers, true, -radius * .75F, canopy + 1.6F, -radius * .75F,
                radius * .75F, canopy + 2.7F, radius * .75F, light, overlay);
        if (growth > .35F) {
            MachineGeometry.botanicalBox(p, buffers, false, -radius * .65F, height * .68F, -.3F,
                    -width, height * .68F + .45F, .3F, light, overlay);
            MachineGeometry.botanicalBox(p, buffers, false, width, height * .80F, -.3F,
                    radius * .55F, height * .80F + .45F, .3F, light, overlay);
        }
    }
}

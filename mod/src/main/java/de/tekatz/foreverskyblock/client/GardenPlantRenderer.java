package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import net.minecraft.client.renderer.MultiBufferSource;

/** Rooted growth shared by the greenhouse and its miniature controller display. */
final class GardenPlantRenderer {
    private GardenPlantRenderer() { }
    static void tree(PoseStack p, MultiBufferSource buffers, float phase, int light, int overlay) {
        float growth = MachineAnimation.growth(phase);
        float crown = MachineAnimation.smooth(.20F, .78F, phase);
        float height = .2F + .8F * growth;
        float radius = .15F + .85F * crown;
        float trunk = .45F + .55F * growth;
        for (var part : GardenTreeShape.PARTS) {
            if (part.faces() == 0) continue;
            float width = part.leaves() || part.y0() > 3.7F ? radius : trunk;
            MachineGeometry.botanicalBox(p, buffers, part.leaves(),
                    part.x0() * width, part.y0() * height, part.z0() * width,
                    part.x1() * width, part.y1() * height, part.z1() * width, light, overlay, part.faces());
        }
    }
}

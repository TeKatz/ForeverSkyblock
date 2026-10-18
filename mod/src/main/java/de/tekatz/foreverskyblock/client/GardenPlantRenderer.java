package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import net.minecraft.client.renderer.MultiBufferSource;

/** Rooted growth shared by the greenhouse and its miniature controller display. */
final class GardenPlantRenderer {
    private GardenPlantRenderer() { }
    static GardenCropProfile cropProfile(net.minecraft.world.item.ItemStack specimen) {
        return specimen.is(net.minecraft.world.item.Items.WHEAT_SEEDS) ? GardenCropProfile.WHEAT : null;
    }
    static void plant(PoseStack p, MultiBufferSource b, net.minecraft.world.item.ItemStack specimen,
                      float phase, boolean running, int light, int overlay) {
        var profile = cropProfile(specimen);
        if (profile != null) crop(p, b, phase, running, profile, light, overlay);
        else if (specimen.is(net.minecraft.world.item.Items.OAK_SAPLING)) tree(p, b, phase, light, overlay);
    }
    static void crop(PoseStack p, MultiBufferSource b, float phase, boolean running,
                     GardenCropProfile profile, int light, int overlay) {
        for (var stalk : profile.stalks()) {
            float growth = profile.growth(phase, stalk);
            float height = profile.height() * stalk.height() * (.12F + .88F * growth);
            float sway = profile.sway(phase, stalk, running);
            p.pushPose(); p.translate(stalk.x(), 0, stalk.z());
            p.mulPose(com.mojang.math.Axis.ZP.rotationDegrees(sway));
            int stem = profile.stemColor(phase);
            MachineGeometry.plantBox(p, b, stem, -.13F, 0, -.13F, .13F, height, .13F, light, overlay);
            // Tapered paired leaves stay attached to the rooted stalk through every stage.
            for (int side = -1; side <= 1; side += 2) {
                p.pushPose(); p.translate(0, height * (side < 0 ? .38F : .55F), 0);
                p.mulPose(com.mojang.math.Axis.ZP.rotationDegrees(side * 51));
                MachineGeometry.plantBox(p, b, stem, -.18F, 0, -.11F, .18F, 1.8F * growth, .11F, light, overlay);
                p.popPose();
            }
            float ear = profile.ear(phase, stalk);
            if (ear > .001F) {
                int grain = profile.grainColor(phase);
                for (int i = 0; i < 4; i++) {
                    float span = (.43F - i * .065F) * ear, yy = height + i * .4F * ear;
                    MachineGeometry.plantBox(p, b, grain, -span, yy, -.26F * ear,
                            span, yy + .48F * ear, .26F * ear, light, overlay);
                }
                MachineGeometry.plantBox(p, b, grain, -.055F, height + 1.5F * ear, -.055F,
                        .055F, height + 2.2F * ear, .055F, light, overlay);
            }
            p.popPose();
        }
    }
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

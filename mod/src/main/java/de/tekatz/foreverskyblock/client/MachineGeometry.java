package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.resources.ResourceLocation;

/** Small opaque cuboids; all dimensions use the same 16-unit coordinates as the static models. */
final class MachineGeometry {
    private static final ResourceLocation MATERIALS = ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "textures/block/machine_materials.png");
    private static final ResourceLocation LIGHTS = ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "textures/block/machine_lights.png");
    static void box(PoseStack poses, MultiBufferSource buffers, int material, float x0, float y0, float z0,
                    float x1, float y1, float z1, int light, int overlay) {
        boolean glow = material >= 6;
        var buffer = buffers.getBuffer(RenderType.entityCutoutNoCull(glow ? LIGHTS : MATERIALS));
        float u0 = glow ? (material - 6) / 4.0F : (material % 3) / 3.0F;
        float v0 = glow ? 0 : (material / 3) / 2.0F;
        float du = glow ? 0.249F : Math.max(0.002F, (x1-x0)/48.0F), dv = glow ? 0.999F : Math.max(0.002F, (y1-y0)/32.0F);
        float[][][] faces = {
            {{x0,y0,z0},{x0,y1,z0},{x1,y1,z0},{x1,y0,z0}},
            {{x1,y0,z1},{x1,y1,z1},{x0,y1,z1},{x0,y0,z1}},
            {{x0,y0,z1},{x0,y1,z1},{x0,y1,z0},{x0,y0,z0}},
            {{x1,y0,z0},{x1,y1,z0},{x1,y1,z1},{x1,y0,z1}},
            {{x0,y1,z0},{x0,y1,z1},{x1,y1,z1},{x1,y1,z0}},
            {{x0,y0,z1},{x0,y0,z0},{x1,y0,z0},{x1,y0,z1}}
        };
        float[][] normals = {{0,0,-1},{0,0,1},{-1,0,0},{1,0,0},{0,1,0},{0,-1,0}};
        float[][] uv = {{u0,v0+dv},{u0,v0},{u0+du,v0},{u0+du,v0+dv}};
        for (int face=0; face<6; face++) for (int i=0; i<4; i++) {
            float[] p=faces[face][i], n=normals[face];
            float shade = face == 4 ? 1 : face == 5 ? 0.65F : face < 2 ? 0.9F : 0.8F;
            if (glow) shade = 1;
            buffer.addVertex(poses.last(), p[0],p[1],p[2]).setColor(shade,shade,shade,1)
                    .setUv(uv[i][0],uv[i][1]).setOverlay(overlay).setLight(glow ? 15728880 : light)
                    .setNormal(poses.last(),n[0],n[1],n[2]);
        }
    }
    private MachineGeometry() { }
}

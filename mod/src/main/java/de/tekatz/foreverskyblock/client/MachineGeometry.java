package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.resources.ResourceLocation;

/** Small opaque cuboids; all dimensions use the same 16-unit coordinates as the static models. */
final class MachineGeometry {
    private static final ResourceLocation BARK = ResourceLocation.withDefaultNamespace("textures/block/oak_log.png");
    private static final ResourceLocation LEAVES = ResourceLocation.withDefaultNamespace("textures/block/oak_leaves.png");
    static void botanicalBox(PoseStack poses, MultiBufferSource buffers, boolean leaves,
                             float x0, float y0, float z0, float x1, float y1, float z1, int light, int overlay, int faces) {
        box(poses, buffers, leaves ? LEAVES : BARK, 0, 0, 1, 1,
                leaves ? .40F : 1F, leaves ? .72F : 1F, leaves ? .27F : 1F,
                x0, y0, z0, x1, y1, z1, light, overlay, false, faces);
    }
    private static final ResourceLocation MATERIALS = ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "textures/block/machine_materials.png");
    private static final ResourceLocation WHITE = ResourceLocation.withDefaultNamespace("textures/block/white_concrete.png");
    static void plantBox(PoseStack p, MultiBufferSource b, int color,
                         float x0, float y0, float z0, float x1, float y1, float z1, int light, int overlay) {
        box(p, b, WHITE, 0, 0, 1, 1, ((color >> 16) & 255) / 255F,
                ((color >> 8) & 255) / 255F, (color & 255) / 255F,
                x0, y0, z0, x1, y1, z1, light, overlay, false);
    }
    private static final ResourceLocation LIGHTS = ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "textures/block/machine_lights.png");
    static void box(PoseStack poses, MultiBufferSource buffers, int material, float x0, float y0, float z0,
                    float x1, float y1, float z1, int light, int overlay) {
        if (material == 10) {
            box(poses, buffers, WHITE, 0, 0, 1, 1, 1F, .65F, .25F,
                    x0, y0, z0, x1, y1, z1, light, overlay, true);
            return;
        }
        boolean glow = material >= 6;
        float u0 = glow ? (material - 6) / 4.0F : (material % 3) / 3.0F;
        float v0 = glow ? 0 : (material / 3) / 2.0F;
        float du = glow ? 0.249F : Math.max(0.002F, (x1-x0)/48.0F), dv = glow ? 0.999F : Math.max(0.002F, (y1-y0)/32.0F);
        box(poses, buffers, glow ? LIGHTS : MATERIALS, u0, v0, du, dv, 1, 1, 1,
                x0, y0, z0, x1, y1, z1, light, overlay, glow);
    }
    private static void box(PoseStack poses, MultiBufferSource buffers, ResourceLocation texture,
                            float u0, float v0, float du, float dv, float red, float green, float blue,
                            float x0, float y0, float z0, float x1, float y1, float z1, int light, int overlay, boolean glow) {
        box(poses, buffers, texture, u0, v0, du, dv, red, green, blue,
                x0, y0, z0, x1, y1, z1, light, overlay, glow, 63);
    }
    private static void box(PoseStack poses, MultiBufferSource buffers, ResourceLocation texture,
                            float u0, float v0, float du, float dv, float red, float green, float blue,
                            float x0, float y0, float z0, float x1, float y1, float z1, int light, int overlay, boolean glow, int faces) {
        var buffer = buffers.getBuffer(RenderType.entityCutoutNoCull(texture));
        // Emit directly: the old per-box vertex/normal arrays caused avoidable allocation for every frame.
        var pose = poses.last();
        int packedLight = glow ? 15728880 : light;
        float side = glow ? 1 : .9F, edge = glow ? 1 : .8F, bottom = glow ? 1 : .65F;
        if ((faces & 1) != 0) quad(pose, buffer, x0,y0,z0, x0,y1,z0, x1,y1,z0, x1,y0,z0, 0,0,-1,
                u0,v0,du,dv, red*side,green*side,blue*side, packedLight,overlay);
        if ((faces & 2) != 0) quad(pose, buffer, x1,y0,z1, x1,y1,z1, x0,y1,z1, x0,y0,z1, 0,0,1,
                u0,v0,du,dv, red*side,green*side,blue*side, packedLight,overlay);
        if ((faces & 4) != 0) quad(pose, buffer, x0,y0,z1, x0,y1,z1, x0,y1,z0, x0,y0,z0, -1,0,0,
                u0,v0,du,dv, red*edge,green*edge,blue*edge, packedLight,overlay);
        if ((faces & 8) != 0) quad(pose, buffer, x1,y0,z0, x1,y1,z0, x1,y1,z1, x1,y0,z1, 1,0,0,
                u0,v0,du,dv, red*edge,green*edge,blue*edge, packedLight,overlay);
        if ((faces & 16) != 0) quad(pose, buffer, x0,y1,z0, x0,y1,z1, x1,y1,z1, x1,y1,z0, 0,1,0,
                u0,v0,du,dv, red,green,blue, packedLight,overlay);
        if ((faces & 32) != 0) quad(pose, buffer, x0,y0,z1, x0,y0,z0, x1,y0,z0, x1,y0,z1, 0,-1,0,
                u0,v0,du,dv, red*bottom,green*bottom,blue*bottom, packedLight,overlay);
    }
    private static void quad(PoseStack.Pose pose, VertexConsumer buffer,
                             float ax,float ay,float az, float bx,float by,float bz,
                             float cx,float cy,float cz, float dx,float dy,float dz,
                             float nx,float ny,float nz, float u,float v,float du,float dv,
                             float red,float green,float blue, int light,int overlay) {
        vertex(pose,buffer,ax,ay,az,u,v+dv,nx,ny,nz,red,green,blue,light,overlay);
        vertex(pose,buffer,bx,by,bz,u,v,nx,ny,nz,red,green,blue,light,overlay);
        vertex(pose,buffer,cx,cy,cz,u+du,v,nx,ny,nz,red,green,blue,light,overlay);
        vertex(pose,buffer,dx,dy,dz,u+du,v+dv,nx,ny,nz,red,green,blue,light,overlay);
    }
    private static void vertex(PoseStack.Pose pose, VertexConsumer buffer, float x,float y,float z,
                               float u,float v, float nx,float ny,float nz,
                               float red,float green,float blue, int light,int overlay) {
        buffer.addVertex(pose,x,y,z).setColor(red,green,blue,1).setUv(u,v)
                .setOverlay(overlay).setLight(light).setNormal(pose,nx,ny,nz);
    }
    private MachineGeometry() { }
}

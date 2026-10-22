package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import de.tekatz.foreverskyblock.block.CrucibleBlock;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;

/** Shows the stored lava as a rising, visible fill inside the open crucible model. */
public final class CrucibleRenderer implements BlockEntityRenderer<CrucibleBlockEntity> {
    public CrucibleRenderer(BlockEntityRendererProvider.Context context) { }

    @Override public void render(CrucibleBlockEntity crucible, float partialTick, PoseStack poses,
                                 MultiBufferSource buffers, int light, int overlay) {
        if (!crucible.getBlockState().hasProperty(CrucibleBlock.FILL)) return;
        int fill = crucible.getBlockState().getValue(CrucibleBlock.FILL);
        if (fill <= 0) return;
        float top = 4.35F + fill * 1.05F;
        poses.pushPose();
        poses.scale(1 / 16F, 1 / 16F, 1 / 16F);
        MachineGeometry.plantBox(poses, buffers, 0xFF6A18, 3.1F, 4.15F, 3.1F, 12.9F, top, 12.9F, light, overlay);
        MachineGeometry.plantBox(poses, buffers, 0xFF9A2E, 2.95F, top, 2.95F, 13.05F, top + .14F, 13.05F, 15728880, overlay);
        float time=crucible.getLevel()==null?0:crucible.getLevel().getGameTime()+(net.minecraft.client.Minecraft.getInstance().isPaused()?0:partialTick);
        // Thermal convection is tied to stored hot lava, independent of recipe activity.
        for(int i=0;i<4;i++) {
            float drift=(time/90F+i*.25F)%1;
            float size=.12F+.28F*(float)Math.sin(Math.PI*drift);
            float x=4.2F+i*2.2F,z=6+(float)Math.sin(i*2.4F+drift*6.28F)*2;
            MachineGeometry.plantBox(poses,buffers,0xFFD28A,x-size,top+.15F,z-size,x+size,top+.18F,z+size,15728880,overlay);
        }
        poses.popPose();
    }

    @Override public int getViewDistance() { return 48; }
}

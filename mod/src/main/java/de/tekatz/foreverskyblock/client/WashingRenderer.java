package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;

/** A shallow wash surface and bounded circulation rings; dry basins remain visibly empty. */
public final class WashingRenderer implements BlockEntityRenderer<WashingBasinBlockEntity> {
    public WashingRenderer(BlockEntityRendererProvider.Context context) { }
    @Override public void render(WashingBasinBlockEntity machine,float partial,PoseStack p,MultiBufferSource b,int light,int overlay) {
        if(machine.visual.amount()==0)return;
        long now=machine.getLevel()==null?0:machine.getLevel().getGameTime();
        float phase=machine.visual.phase(now,Minecraft.getInstance().isPaused()?0:partial);
        boolean running=machine.visual.status()==WashingBasinBlockEntity.RUNNING;
        p.pushPose();p.scale(1/16F,1/16F,1/16F);
        MachineGeometry.plantBox(p,b,0x347C91,3.05F,9.1F,3.05F,12.95F,9.2F,12.95F,light,overlay);
        for(int i=0;i<3;i++) {
            float wave=running?(phase*9+i/3F)%1:(i+1)/4F;
            float r=1+3.5F*wave, thickness=running?.04F+.09F*(float)Math.sin(wave*Math.PI):.06F;
            int color=running?0x8BD5D6:0x528D9A;
            MachineGeometry.plantBox(p,b,color,8-r,9.22F,8-r,8+r,9.25F,8-r+thickness,light,overlay);
            MachineGeometry.plantBox(p,b,color,8-r,9.22F,8+r-thickness,8+r,9.25F,8+r,light,overlay);
            MachineGeometry.plantBox(p,b,color,8-r,9.22F,8-r,8-r+thickness,9.25F,8+r,light,overlay);
            MachineGeometry.plantBox(p,b,color,8+r-thickness,9.22F,8-r,8+r,9.25F,8+r,light,overlay);
        }
        p.popPose();
    }
    @Override public int getViewDistance() { return 32; }
}

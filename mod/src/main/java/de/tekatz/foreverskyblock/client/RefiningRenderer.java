package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.blockentity.RefiningChamberBlockEntity;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;

/** Opposed insets share one synchronized reactor, stopping exactly on power loss or blocked output. */
public final class RefiningRenderer implements BlockEntityRenderer<RefiningChamberBlockEntity> {
    public RefiningRenderer(BlockEntityRendererProvider.Context context) { }
    @Override public void render(RefiningChamberBlockEntity machine,float partial,PoseStack p,MultiBufferSource b,int light,int overlay) {
        long now=machine.getLevel()==null?0:machine.getLevel().getGameTime();
        float phase=machine.visual.phase(now,Minecraft.getInstance().isPaused()?0:partial);
        int status=machine.visual.status();
        int glow=status==RefiningChamberBlockEntity.RUNNING?7:status>=2?10:4;
        p.pushPose();p.scale(1/16F,1/16F,1/16F);
        for(int side=0;side<2;side++) {
            p.pushPose();p.translate(8,8,side==0?2.15F:13.85F);
            if(side==1)p.mulPose(Axis.YP.rotationDegrees(180));
            p.pushPose();p.mulPose(Axis.ZP.rotationDegrees(phase*720));
            for(int i=0;i<4;i++) {
                p.pushPose();p.mulPose(Axis.ZP.rotationDegrees(i*90));
                MachineGeometry.box(p,b,34,-3,-3,-.3F,3,-2.7F,.3F,light,overlay);
                MachineGeometry.box(p,b,glow,-1.8F,-3.06F,-.36F,1.8F,-2.96F,-.31F,light,overlay);p.popPose();
            }
            p.popPose();
            p.mulPose(Axis.ZP.rotationDegrees(45-phase*360));
            MachineGeometry.box(p,b,status==1?7:4,-.9F,-.9F,-.5F,.9F,.9F,.5F,light,overlay);
            p.popPose();
        }
        p.popPose();
    }
    @Override public int getViewDistance() { return 32; }
}

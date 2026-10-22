package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.block.AscensionChamberBlock;
import de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.*;

public final class AscensionRenderer implements BlockEntityRenderer<AscensionChamberBlockEntity> {
    public AscensionRenderer(BlockEntityRendererProvider.Context context) { }
    @Override public void render(AscensionChamberBlockEntity machine,float partialTick,PoseStack p,MultiBufferSource buffers,int light,int overlay) {
        p.pushPose();p.translate(.5,0,.5);
        p.mulPose(Axis.YP.rotationDegrees(-machine.getBlockState().getValue(AscensionChamberBlock.FACING).toYRot()-180));
        p.translate(-.5,0,-.5);p.scale(1/16F,1/16F,1/16F);
        boolean active=machine.getBlockState().getValue(AscensionChamberBlock.ACTIVE);
        float phase=active?machine.visualProgress(partialTick)/AscensionChamberBlockEntity.DURATION:0;
        ring(p,buffers,4.5F,2.8F,active?phase*180:0,active,light,overlay);
        ring(p,buffers,3.15F,1.6F,active?-phase*360:0,active,light,overlay);
        if(!machine.inventory().getStackInSlot(0).isEmpty()) {
            p.pushPose();p.translate(8,8+(active?Math.sin(phase*Math.PI*4)*.18:0),2.5);
            if(active)p.mulPose(Axis.ZP.rotationDegrees((float)Math.sin(phase*Math.PI*2)*9));
            MachineGeometry.box(p,buffers,32,-1.4F,-1.4F,-1.0F,1.4F,1.4F,.9F,light,overlay);
            for(float x:new float[]{-1.15F,.75F}) for(float y:new float[]{-1.15F,.75F})
                MachineGeometry.box(p,buffers,37,x,y,-1.12F,x+.4F,y+.4F,-1.04F,light,overlay);
            p.mulPose(Axis.ZP.rotationDegrees(45));
            MachineGeometry.box(p,buffers,7,-.65F,-.65F,-1.15F,.65F,.65F,-1.13F,light,overlay);
            MachineGeometry.box(p,buffers,8,-.25F,-.25F,-1.18F,.25F,.25F,-1.16F,light,overlay);
            p.popPose();
        }
        if(active)for(int i=0;i<6;i++) {
            double angle=phase*Math.PI*4+i*Math.PI/3;
            float x=8+(float)Math.cos(angle)*2.3F,y=8+(float)Math.sin(angle)*2.3F;
            MachineGeometry.box(p,buffers,7,x,y,2.1F,x+.13F,y+.13F,2.25F,light,overlay);
        }
        p.popPose();
    }
    private static void ring(PoseStack p,MultiBufferSource buffers,float radius,float z,float angle,boolean active,int light,int overlay) {
        p.pushPose();p.translate(8,8,z);p.mulPose(Axis.ZP.rotationDegrees(angle));
        for(int i=0;i<4;i++) {
            p.pushPose();p.mulPose(Axis.ZP.rotationDegrees(i*90));
            MachineGeometry.box(p,buffers,34,-radius+.5F,-radius,0,radius,-radius+.5F,.35F,light,overlay);
            MachineGeometry.box(p,buffers,active?7:4,-radius*.55F,-radius+.12F,-.055F,radius*.55F,-radius+.36F,-.025F,light,overlay);
            p.popPose();
        }
        p.popPose();
    }
    @Override public int getViewDistance() { return 48; }
}

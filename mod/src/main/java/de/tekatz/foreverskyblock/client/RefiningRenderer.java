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
    private static final String ASSET="refining_chamber";

    public RefiningRenderer(BlockEntityRendererProvider.Context context) { }
    @Override public void render(RefiningChamberBlockEntity machine,float partial,PoseStack p,MultiBufferSource b,int light,int overlay) {
        long now=machine.getLevel()==null?0:machine.getLevel().getGameTime();
        float phase=machine.visual.phase(now,Minecraft.getInstance().isPaused()?0:partial);
        int sourceStatus=machine.visual.status();
        String sourceOption=switch(sourceStatus){
            case RefiningChamberBlockEntity.RUNNING->"running";
            case RefiningChamberBlockEntity.BLOCKED->"blocked";
            case RefiningChamberBlockEntity.NO_POWER->"no-power";
            default->"idle";
        };
        String visualStatus=ForeverStudioRendererBinding.status(ASSET,"status",phase,sourceOption);
        int glow="running".equalsIgnoreCase(visualStatus)?7:
                ("blocked".equalsIgnoreCase(visualStatus)||"no-power".equalsIgnoreCase(visualStatus))?10:4;
        int core="running".equalsIgnoreCase(visualStatus)?7:4;
        p.pushPose();p.scale(1/16F,1/16F,1/16F);
        side(p,b,false,phase,glow,core,light,overlay);
        side(p,b,true,phase,glow,core,light,overlay);
        p.popPose();
    }

    private static void side(PoseStack p,MultiBufferSource b,boolean back,float phase,int glow,int core,int light,int overlay) {
        float z=back?13.85F:2.15F;
        String outerKey=back?"refining_outer_back":"refining_outer_front";
        String coreKey=back?"refining_core_back":"refining_core_front";
        ForeverStudioRendererBinding.component(p,ASSET,outerKey,phase,8,8,z,()->{
            p.pushPose();p.translate(8,8,z);
            if(back)p.mulPose(Axis.YP.rotationDegrees(180));
            p.mulPose(Axis.ZP.rotationDegrees(phase*720));
            for(int i=0;i<4;i++) {
                p.pushPose();p.mulPose(Axis.ZP.rotationDegrees(i*90));
                MachineGeometry.box(p,b,34,-3,-3,-.3F,3,-2.7F,.3F,light,overlay);
                MachineGeometry.box(p,b,glow,-1.8F,-3.06F,-.36F,1.8F,-2.96F,-.31F,light,overlay);p.popPose();
            }
            p.popPose();
        });
        ForeverStudioRendererBinding.component(p,ASSET,coreKey,phase,8,8,z,()->{
            p.pushPose();p.translate(8,8,z);
            if(back)p.mulPose(Axis.YP.rotationDegrees(180));
            p.mulPose(Axis.ZP.rotationDegrees(45-phase*360));
            MachineGeometry.box(p,b,core,-.9F,-.9F,-.5F,.9F,.9F,.5F,light,overlay);
            p.popPose();
        });
    }
    @Override public int getViewDistance() { return 32; }
}

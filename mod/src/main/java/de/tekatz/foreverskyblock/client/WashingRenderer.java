package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;

/** A shallow wash surface and bounded circulation rings; dry basins remain visibly empty. */
public final class WashingRenderer implements BlockEntityRenderer<WashingBasinBlockEntity> {
    private static final String ASSET="washing_basin";

    public WashingRenderer(BlockEntityRendererProvider.Context context) { }
    @Override public void render(WashingBasinBlockEntity machine,float partial,PoseStack p,MultiBufferSource b,int light,int overlay) {
        if(machine.visual.amount()==0)return;
        long now=machine.getLevel()==null?0:machine.getLevel().getGameTime();
        float phase=machine.visual.phase(now,Minecraft.getInstance().isPaused()?0:partial);
        int sourceStatus=machine.visual.status();
        boolean running=sourceStatus==WashingBasinBlockEntity.RUNNING;
        String sourceOption=switch(sourceStatus){
            case WashingBasinBlockEntity.RUNNING->"running";
            case WashingBasinBlockEntity.BLOCKED->"blocked";
            case WashingBasinBlockEntity.NO_WATER->"no-water";
            default->"idle";
        };
        boolean visualRunning="running".equalsIgnoreCase(ForeverStudioRendererBinding.status(ASSET,"status",phase,sourceOption));
        p.pushPose();p.scale(1/16F,1/16F,1/16F);
        ForeverStudioRendererBinding.component(p,ASSET,"washing_surface",phase,8,9.15,8,
                ()->MachineGeometry.plantBox(p,b,0x347C91,3.05F,9.1F,3.05F,12.95F,9.2F,12.95F,light,overlay));
        if(ForeverStudioRendererBinding.effect(ASSET,"circulation-rings",phase,true)) {
            for(int i=0;i<3;i++) {
                int index=i;
                float wave=running?(phase*9+i/3F)%1:(i+1)/4F;
                float r=1+3.5F*wave, thickness=running?.04F+.09F*(float)Math.sin(wave*Math.PI):.06F;
                int color=visualRunning?0x8BD5D6:0x528D9A;
                ForeverStudioRendererBinding.component(p,ASSET,"washing_ring_"+index,phase,8,9.23,8,()->{
                    MachineGeometry.plantBox(p,b,color,8-r,9.22F,8-r,8+r,9.25F,8-r+thickness,light,overlay);
                    MachineGeometry.plantBox(p,b,color,8-r,9.22F,8+r-thickness,8+r,9.25F,8+r,light,overlay);
                    MachineGeometry.plantBox(p,b,color,8-r,9.22F,8-r,8-r+thickness,9.25F,8+r,light,overlay);
                    MachineGeometry.plantBox(p,b,color,8+r-thickness,9.22F,8-r,8+r,9.25F,8+r,light,overlay);
                });
            }
        }
        p.popPose();
    }
    @Override public int getViewDistance() { return 32; }
}

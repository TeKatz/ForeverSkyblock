package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.*;
import net.minecraft.client.renderer.entity.ItemRenderer;
import net.minecraft.world.item.ItemDisplayContext;
import net.minecraft.world.item.ItemStack;

public final class CondenserRenderer implements BlockEntityRenderer<CondenserBlockEntity> {
    private final ItemRenderer items;
    public CondenserRenderer(BlockEntityRendererProvider.Context context) { items = context.getItemRenderer(); }
    @Override public void render(CondenserBlockEntity machine, float partialTick, PoseStack p, MultiBufferSource buffers, int light, int overlay) {
        p.pushPose(); p.translate(.5,0,.5);
        p.mulPose(Axis.YP.rotationDegrees(-machine.getBlockState().getValue(CondenserBlock.FACING).toYRot()-180));
        p.translate(-.5,0,-.5); p.scale(1/16F,1/16F,1/16F);
        boolean active = machine.getBlockState().getValue(CondenserBlock.ACTIVE);
        float phase = active ? machine.visualProgress(partialTick)/Math.max(1,machine.visualDuration()) : 0;
        float compression = active ? MachineAnimation.compression(phase) : 0;
        float jaw = 10-compression;
        MachineGeometry.box(p,buffers,3,4.5F,jaw,1.25F,11.5F,jaw+1.35F,3.8F,light,overlay);
        MachineGeometry.box(p,buffers,2,7.2F,jaw+1.36F,1.7F,8.8F,13,3.3F,light,overlay);
        ItemStack input = machine.inventory().getStackInSlot(0);
        if (!input.isEmpty()) {
            item(p,buffers,input,8,6.5F,2.4F,2.6F,light,overlay);
            item(p,buffers,input,1.5F,7.5F,1.1F,2.0F,light,overlay);
        }
        if (active) for (int i=0;i<5;i++) {
            float x=5.1F+(i*1.37F)%5.6F, y=5.4F+(float)Math.abs(Math.sin(phase*9+i))*1.3F;
            MachineGeometry.box(p,buffers,phase<.55F?1:3,x,y,1.4F,x+.3F,y+.3F,1.7F,light,overlay);
        }
        ItemStack output=machine.inventory().getStackInSlot(1);
        if(!output.isEmpty()) item(p,buffers,output,8,1.8F,.9F,1.5F,light,overlay);
        p.popPose();
    }
    private void item(PoseStack p, MultiBufferSource buffers, ItemStack stack, float x,float y,float z,float size,int light,int overlay) {
        p.pushPose();p.translate(x,y,z);p.mulPose(Axis.YP.rotationDegrees(180));p.scale(size,size,size);
        items.renderStatic(stack,ItemDisplayContext.FIXED,light,overlay,p,buffers,null,0);p.popPose();
    }
    @Override public int getViewDistance() { return 48; }
}

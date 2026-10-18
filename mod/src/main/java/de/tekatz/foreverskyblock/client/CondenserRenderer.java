package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;
import net.minecraft.client.renderer.entity.ItemRenderer;
import net.minecraft.world.item.ItemDisplayContext;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

public final class CondenserRenderer implements BlockEntityRenderer<CondenserBlockEntity> {
    private final ItemRenderer items;
    public CondenserRenderer(BlockEntityRendererProvider.Context context) { items = context.getItemRenderer(); }

    @Override public void render(CondenserBlockEntity machine, float partialTick, PoseStack p, MultiBufferSource buffers, int light, int overlay) {
        p.pushPose();
        p.translate(.5, 0, .5);
        p.mulPose(Axis.YP.rotationDegrees(-machine.getBlockState().getValue(CondenserBlock.FACING).toYRot() - 180));
        p.translate(-.5, 0, -.5);
        p.scale(1 / 16F, 1 / 16F, 1 / 16F);

        boolean active = machine.getBlockState().getValue(CondenserBlock.ACTIVE);
        float phase = active ? machine.visualProgress(partialTick) / Math.max(1, machine.visualDuration()) : 0;
        if (machine.hasGardenChamber()) renderGardenMode(machine, phase, active, p, buffers, light, overlay);
        else renderCondensingMode(machine, phase, active, p, buffers, light, overlay);

        p.popPose();
    }

    private void renderCondensingMode(CondenserBlockEntity machine, float phase, boolean active, PoseStack p,
                                      MultiBufferSource buffers, int light, int overlay) {
        float compression = active ? MachineAnimation.compression(phase) : 0;
        float jaw = 10 - compression;
        MachineGeometry.box(p, buffers, 3, 4.5F, jaw, 1.25F, 11.5F, jaw + 1.35F, 3.8F, light, overlay);
        MachineGeometry.box(p, buffers, 2, 7.2F, jaw + 1.36F, 1.7F, 8.8F, 13, 3.3F, light, overlay);
        ItemStack input = machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT);
        if (!input.isEmpty()) {
            item(p, buffers, input, 8, 6.5F, 2.4F, 2.6F, 180, 0, light, overlay);
            item(p, buffers, input, 1.5F, 7.5F, 1.1F, 2.0F, 180, 0, light, overlay);
        }
        if (active) for (int i = 0; i < 5; i++) {
            float x = 5.1F + (i * 1.37F) % 5.6F;
            float y = 5.4F + (float) Math.abs(Math.sin(phase * 9 + i)) * 1.3F;
            MachineGeometry.box(p, buffers, phase < .55F ? 1 : 3, x, y, 1.4F, x + .3F, y + .3F, 1.7F, light, overlay);
        }
        ItemStack output = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
        if (!output.isEmpty()) item(p, buffers, output, 8, 1.8F, .9F, 1.5F, 180, 0, light, overlay);
    }

    private void renderGardenMode(CondenserBlockEntity machine, float phase, boolean active, PoseStack p,
                                  MultiBufferSource buffers, int light, int overlay) {
        // The crusher carriage retracts and becomes a small grow-light/scanner assembly in Garden mode.
        MachineGeometry.box(p, buffers, 3, 4.4F, 10.6F, 1.15F, 11.6F, 11.45F, 3.6F, light, overlay);
        MachineGeometry.box(p, buffers, 5, 5.2F, 9.6F, 1.25F, 10.8F, 10.25F, 3.45F, light, overlay);

        ItemStack specimen = machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN);
        if (!specimen.isEmpty()) {
            float bob = active ? .18F * (float) Math.sin(phase * Math.PI * 4) : 0F;
            item(p, buffers, specimen, 8F, 7.25F + bob, 2.05F, 1.35F, 180F + phase * 22F, 0F, light, overlay);
        }

        // Botanical status markers on the front make the mode obvious even from outside the GUI.
        item(p, buffers, new ItemStack(Items.OAK_LEAVES), 3.1F, 7.1F, 1.0F, .72F, 180F, -8F, light, overlay);
        item(p, buffers, new ItemStack(Items.OAK_LEAVES), 12.9F, 7.1F, 1.0F, .72F, 180F, 8F, light, overlay);

        if (active) {
            float scanX = 4.7F + 6.2F * phase;
            MachineGeometry.box(p, buffers, 6, scanX, 10.35F, 1.0F, scanX + .5F, 11.75F, 3.8F, 15728880, overlay);
            float flutter = (float) Math.sin(phase * Math.PI * 6);
            item(p, buffers, new ItemStack(Items.OAK_SAPLING), 8F, 12.7F + .12F * flutter, 2.1F,
                    .58F, 180F + phase * 40F, 6F * flutter, light, overlay);
        }

        ItemStack primary = machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY);
        if (!primary.isEmpty()) item(p, buffers, primary, 6.3F, 1.75F, .9F, 1.15F, 180, 0, light, overlay);
        ItemStack secondary = machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY);
        if (!secondary.isEmpty()) item(p, buffers, secondary, 9.7F, 1.75F, .9F, 1.0F, 180, 0, light, overlay);
    }

    private void item(PoseStack p, MultiBufferSource buffers, ItemStack stack, float x, float y, float z,
                      float size, float yaw, float roll, int light, int overlay) {
        p.pushPose();
        p.translate(x, y, z);
        p.mulPose(Axis.YP.rotationDegrees(yaw));
        p.mulPose(Axis.ZP.rotationDegrees(roll));
        p.scale(size, size, size);
        items.renderStatic(stack, ItemDisplayContext.FIXED, light, overlay, p, buffers, null, 0);
        p.popPose();
    }

    @Override public int getViewDistance() { return 48; }
}

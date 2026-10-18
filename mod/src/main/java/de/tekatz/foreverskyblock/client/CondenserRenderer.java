package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.recipe.CondenserJob;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;
import net.minecraft.client.renderer.entity.ItemRenderer;
import net.minecraft.world.item.ItemDisplayContext;
import net.minecraft.world.item.ItemStack;

/** All moving parts remain inside the existing front recess, including at maximum travel. */
public final class CondenserRenderer implements BlockEntityRenderer<CondenserBlockEntity> {
    private final ItemRenderer items;
    public CondenserRenderer(BlockEntityRendererProvider.Context context) { items = context.getItemRenderer(); }
    @Override public void render(CondenserBlockEntity machine, float partialTick, PoseStack p,
                                 MultiBufferSource buffers, int light, int overlay) {
        p.pushPose(); p.translate(.5, 0, .5);
        p.mulPose(Axis.YP.rotationDegrees(-machine.getBlockState().getValue(CondenserBlock.FACING).toYRot() - 180));
        p.translate(-.5, 0, -.5); p.scale(1 / 16F, 1 / 16F, 1 / 16F);
        boolean running = machine.visualStatus() == CondenserBlockEntity.RUNNING;
        float phase = MachineAnimation.phase(machine.visualProgress(partialTick), machine.visualDuration());
        if (machine.hasGardenChamber()) garden(machine, phase, running, p, buffers, light, overlay);
        else process(machine, phase, running, p, buffers, light, overlay);
        status(machine, phase, running, p, buffers, light, overlay);
        p.popPose();
    }
    private void process(CondenserBlockEntity m, float phase, boolean running, PoseStack p,
                         MultiBufferSource b, int light, int overlay) {
        CondenserJob job = m.visualJob();
        boolean mixing = job == CondenserJob.MIXING || job == CondenserJob.CULTIVATING;
        boolean organic = job == CondenserJob.COMPOSTING;
        if (mixing || organic) {
            // A low mixing pan replaces the ram while combining ingredients or breaking down leaves.
            MachineGeometry.box(p, b, 3, 4.8F, 5.3F, 1.3F, 11.2F, 5.9F, 3.8F, light, overlay);
            float angle = phase * (organic ? 720F : 1080F);
            p.pushPose(); p.translate(8, 7.1F, 2.5F); p.scale(1, 1, .4F); p.mulPose(Axis.YP.rotationDegrees(angle));
            MachineGeometry.box(p, b, 2, -.22F, -1.1F, -.22F, .22F, 3.3F, .22F, light, overlay);
            MachineGeometry.box(p, b, 3, -2.3F, -.55F, -.28F, 2.3F, -.1F, .28F, light, overlay);
            p.popPose();
            float orbit = (float) Math.toRadians(angle);
            float dx = running ? (float) Math.cos(orbit) * 1.7F : 1.5F;
            item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.INPUT), 8 - dx, 7.4F, 1.5F, 1.4F, light, overlay);
            if (mixing) item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.SECOND_INPUT), 8 + dx, 7.4F, 1.5F, 1.4F, light, overlay);
            if (job == CondenserJob.CULTIVATING && phase > .45F)
                item(p, b, m.visualResult(), 8, 8.8F, 1.7F, 1F + MachineAnimation.growth(phase), light, overlay);
        } else if (m.machineTier() >= 2) {
            // Mark II uses geared, counter-rotating crushing/milling rollers instead of the Mark-I piston.
            float angle = job == CondenserJob.IDLE ? 0 : phase * (job == CondenserJob.MILLING ? 1440F : 720F);
            roller(p, b, 6.1F, angle, light, overlay); roller(p, b, 9.9F, -angle, light, overlay);
            float feed = running ? MachineAnimation.smooth(0, .48F, phase) : 0;
            item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.INPUT), 8, 10.6F - feed * 2.1F,
                    2.3F, 2F - feed * .8F, light, overlay);
        } else {
            float stroke = job == CondenserJob.IDLE ? 0 : MachineAnimation.press(phase);
            float jaw = 10.4F - 2.6F * stroke;
            MachineGeometry.box(p, b, 3, 4.6F, jaw, 1.25F, 11.4F, jaw + 1.1F, 3.8F, light, overlay);
            MachineGeometry.box(p, b, 2, 7.3F, jaw + 1.1F, 1.8F, 8.7F, 12.5F, 3.2F, light, overlay);
            item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.INPUT), 8, 6.6F,
                    2.3F, 2.4F - stroke * .75F, light, overlay);
        }
        if (running && phase > .45F && phase < .84F) {
            int material = organic || job == CondenserJob.CULTIVATING ? 5 : 1;
            for (int i = 0; i < 4; i++) {
                float fall = (phase * 5 + i * .23F) % 1;
                float x = 5.8F + i * 1.4F, y = 7.2F - 1.7F * fall;
                MachineGeometry.box(p, b, material, x, y, 1.4F, x + .18F, y + .18F, 1.65F, light, overlay);
            }
        }
        if (running && phase > .84F) {
            float fall = MachineAnimation.smooth(.84F, 1, phase);
            item(p, b, m.visualResult(), 8, 5.8F - fall * 3.6F, .9F, 1.3F, light, overlay);
        }
        item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT), 8, 1.85F, .8F, 1.35F, light, overlay);
    }
    private void roller(PoseStack p, MultiBufferSource b, float x, float angle, int light, int overlay) {
        p.pushPose(); p.translate(x, 8.2F, 2.5F); p.mulPose(Axis.ZP.rotationDegrees(angle));
        MachineGeometry.box(p, b, 3, -1.05F, -1.05F, -1.15F, 1.05F, 1.05F, 1.15F, light, overlay);
        for (int i = 0; i < 6; i++) {
            p.pushPose(); p.mulPose(Axis.ZP.rotationDegrees(i * 60));
            MachineGeometry.box(p, b, 2, -.3F, 1.05F, -1.1F, .3F, 1.5F, 1.1F, light, overlay); p.popPose();
        }
        p.popPose();
    }
    private void garden(CondenserBlockEntity m, float phase, boolean running, PoseStack p,
                        MultiBufferSource b, int light, int overlay) {
        MachineGeometry.box(p, b, 3, 4.7F, 11.3F, 1.3F, 11.3F, 12, 3.7F, light, overlay);
        MachineGeometry.box(p, b, 5, 5.2F, 5.3F, 1.3F, 10.8F, 5.8F, 3.7F, light, overlay);
        ItemStack specimen = m.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN);
        if (!specimen.isEmpty()) {
            p.pushPose(); p.translate(8, 5.9F, 2.5F); p.scale(.52F, .52F, .24F);
            GardenPlantRenderer.tree(p, b, phase, light, overlay); p.popPose();
        }
        if (running) {
            float scan = 5F + 5.6F * MachineAnimation.growth(phase);
            MachineGeometry.box(p, b, 7, scan, 10.7F, 1.35F, scan + .35F, 11.25F, 3.65F, light, overlay);
            float harvest = MachineAnimation.harvest(phase);
            if (harvest > 0) MachineGeometry.box(p, b, 7, 5.4F, 5.9F + harvest * 4.5F, 1.1F,
                    10.6F, 6.05F + harvest * 4.5F, 1.3F, light, overlay);
        }
        item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY), 6.6F, 1.85F, .85F, 1.1F, light, overlay);
        item(p, b, m.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY), 9.4F, 1.85F, .85F, 1.1F, light, overlay);
    }
    private void status(CondenserBlockEntity m, float phase, boolean running, PoseStack p,
                        MultiBufferSource b, int light, int overlay) {
        int material = m.visualStatus() == CondenserBlockEntity.BLOCKED ? 10 : running ? (m.hasGardenChamber() ? 7 : 6) : 4;
        for (int i = 0; i < 6; i++) {
            int segment = running && phase < (i + 1) / 6F ? 4 : material;
            MachineGeometry.box(p, b, segment, 4.7F + i * 1.1F, 12.2F, 1.2F, 5.4F + i * 1.1F, 12.5F, 1.45F, light, overlay);
        }
        if (m.hasFreezingChamber()) {
            float fill = m.visualFreezingProgress() / (float) CondenserBlockEntity.FREEZING_REQUIRED_ITEMS;
            int frost = m.visualFreezingStatus() == CondenserBlockEntity.BLOCKED ? 10 : 6;
            MachineGeometry.box(p, b, 4, .85F, 6, .9F, 1.25F, 10.3F, 1.2F, light, overlay);
            if (fill > 0) MachineGeometry.box(p, b, frost, .82F, 6, .85F, 1.28F, 6 + 4.3F * fill, 1.22F, light, overlay);
        }
    }
    private void item(PoseStack p, MultiBufferSource buffers, ItemStack stack, float x, float y, float z,
                      float size, int light, int overlay) {
        if (stack.isEmpty()) return;
        p.pushPose(); p.translate(x, y, z); p.mulPose(Axis.YP.rotationDegrees(180)); p.scale(size, size, size);
        items.renderStatic(stack, ItemDisplayContext.FIXED, light, overlay, p, buffers, null, 0); p.popPose();
    }
    @Override public int getViewDistance() { return 48; }
}

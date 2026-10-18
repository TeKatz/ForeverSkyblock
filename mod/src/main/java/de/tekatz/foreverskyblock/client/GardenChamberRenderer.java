package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.GardenChamberBlockEntity;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;
import net.minecraft.client.renderer.entity.ItemRenderer;
import net.minecraft.world.item.ItemDisplayContext;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

/** A rooted specimen grows beneath a travelling lamp; nothing moves without a running Garden job. */
public final class GardenChamberRenderer implements BlockEntityRenderer<GardenChamberBlockEntity> {
    private final ItemRenderer items;
    public GardenChamberRenderer(BlockEntityRendererProvider.Context context) { items = context.getItemRenderer(); }
    @Override public void render(GardenChamberBlockEntity chamber, float partialTick, PoseStack p,
                                 MultiBufferSource b, int light, int overlay) {
        CondenserBlockEntity m = chamber.linkedCondenser();
        if (m == null) return;
        ItemStack specimen = m.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN);
        boolean running = m.visualStatus() == CondenserBlockEntity.RUNNING && !specimen.isEmpty();
        float phase = MachineAnimation.phase(m.visualProgress(partialTick), m.visualDuration());
        p.pushPose(); p.scale(1 / 16F, 1 / 16F, 1 / 16F);
        if (!specimen.isEmpty()) {
            if (specimen.is(Items.OAK_SAPLING) || GardenPlantRenderer.cropProfile(specimen) != null) {
                p.pushPose(); p.translate(8, 3.25F, 8);
                GardenPlantRenderer.plant(p, b, specimen, phase, running, light, overlay); p.popPose();
            } else {
                // Datapack specimens remain identifiable rather than becoming an unrelated oak.
                p.pushPose(); p.translate(8, 5.6F, 8); p.scale(4, 4, 4);
                items.renderStatic(specimen, ItemDisplayContext.FIXED, light, overlay, p, b, null, 0); p.popPose();
            }
        }
        float z = 3.1F + 9.4F * MachineAnimation.growth(phase);
        MachineGeometry.box(p, b, 3, 2.1F, 13.4F, z, 13.9F, 13.95F, z + .4F, light, overlay);
        MachineGeometry.box(p, b, running ? 7 : 4, 3.1F, 13.15F, z + .05F, 12.9F, 13.4F, z + .35F, light, overlay);
        int status = m.visualStatus() == CondenserBlockEntity.BLOCKED ? 10 : running ? 7 : 4;
        for (int i = 0; i < 4; i++) {
            p.pushPose(); p.translate(8, 0, 8); p.mulPose(Axis.YP.rotationDegrees(i * 90));
            MachineGeometry.box(p, b, status, -2.2F, 1.05F, -7.97F, 2.2F, 1.4F, -7.87F, light, overlay);
            p.popPose();
        }
        if (running) {
            for (int i = 0; i < 4; i++) {
                float fall = (phase * 12 + i * .25F) % 1;
                float x = i % 2 == 0 ? 3.5F : 12.2F, dz = i < 2 ? 3.5F : 12.2F;
                MachineGeometry.box(p, b, 7, x, 12.7F - fall * 8.6F, dz,
                        x + .12F, 13F - fall * 8.6F, dz + .12F, light, overlay);
            }
            float harvest = MachineAnimation.harvest(phase);
            if (harvest > 0) {
                float sweep = 3.4F + harvest * 8.8F;
                MachineGeometry.box(p, b, 7, 2.7F, sweep, 2.7F, 13.3F, sweep + .12F, 2.85F, light, overlay);
                MachineGeometry.box(p, b, 7, 2.7F, sweep, 13.15F, 13.3F, sweep + .12F, 13.3F, light, overlay);
            }
        }
        p.popPose();
    }
    @Override public int getViewDistance() { return 48; }
}

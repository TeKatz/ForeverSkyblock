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

/** Small greenhouse vignette rendered behind the Garden Chamber glass. */
public final class GardenChamberRenderer implements BlockEntityRenderer<GardenChamberBlockEntity> {
    private final ItemRenderer items;

    public GardenChamberRenderer(BlockEntityRendererProvider.Context context) {
        items = context.getItemRenderer();
    }

    @Override public void render(GardenChamberBlockEntity chamber, float partialTick, PoseStack p,
                                 MultiBufferSource buffers, int light, int overlay) {
        CondenserBlockEntity condenser = chamber.linkedCondenser();
        if (condenser == null) return;

        ItemStack specimen = condenser.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN);
        boolean running = chamber.running() && !specimen.isEmpty();
        float time = chamber.getLevel() == null ? 0F : chamber.getLevel().getGameTime() + partialTick;
        float phase = running ? condenser.visualProgress(partialTick) / Math.max(1F, condenser.visualDuration()) : 0F;

        p.pushPose();
        p.scale(1 / 16F, 1 / 16F, 1 / 16F);

        // Warm little planter boxes give the greenhouse depth even while idle.
        MachineGeometry.box(p, buffers, 5, 2.4F, 2.9F, 3.0F, 6.6F, 3.7F, 7.2F, light, overlay);
        MachineGeometry.box(p, buffers, 5, 9.4F, 2.9F, 8.8F, 13.6F, 3.7F, 13.0F, light, overlay);

        if (!specimen.isEmpty()) {
            float pulse = running ? 1F + 0.06F * (float) Math.sin(time * 0.18F) : 1F;
            item(p, buffers, specimen, 8F, 5.2F + (running ? 0.18F * (float) Math.sin(time * 0.22F) : 0F), 8F,
                    0.62F * pulse, time * 0.7F, 0F, light, overlay);
        }

        if (running) {
            float orbit = time * 0.075F;
            float bobA = 0.28F * (float) Math.sin(time * 0.21F);
            float bobB = 0.24F * (float) Math.sin(time * 0.17F + 1.8F);
            item(p, buffers, new ItemStack(Items.OAK_LEAVES),
                    8F + 3.1F * (float) Math.cos(orbit), 8.4F + bobA, 8F + 2.1F * (float) Math.sin(orbit),
                    0.24F, time * 2.2F, 12F * (float) Math.sin(time * 0.12F), light, overlay);
            item(p, buffers, new ItemStack(Items.OAK_LEAVES),
                    8F + 2.5F * (float) Math.cos(orbit + 2.8F), 6.9F + bobB, 8F + 2.8F * (float) Math.sin(orbit + 2.8F),
                    0.19F, -time * 1.7F, -10F * (float) Math.sin(time * 0.15F), light, overlay);

            // Moving grow-lights read as a tiny scanner sweeping through the greenhouse.
            float sweep = 3.2F + 8.8F * phase;
            MachineGeometry.box(p, buffers, 6, sweep, 12.3F, 2.7F, sweep + 0.45F, 12.65F, 13.3F, 15728880, overlay);
            MachineGeometry.box(p, buffers, 6, 3.1F, 12.25F, 3.0F, 3.45F, 12.6F, 4.2F, 15728880, overlay);
            MachineGeometry.box(p, buffers, 6, 12.55F, 12.25F, 11.8F, 12.9F, 12.6F, 13.0F, 15728880, overlay);
        }

        p.popPose();
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

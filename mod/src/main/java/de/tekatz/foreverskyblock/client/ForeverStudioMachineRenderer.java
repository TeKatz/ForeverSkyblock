package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlockEntity;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineDefinition;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.block.BlockRenderDispatcher;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;
import net.minecraft.client.resources.model.ModelResourceLocation;

import java.util.Locale;

/**
 * Generic renderer for Studio-generated machine blocks. New runtime builds expose independently
 * baked component models, so authored transforms affect only their target component and use the
 * component's authored pivot. Older outputs retain the monolithic fallback for compatibility.
 */
public final class ForeverStudioMachineRenderer implements BlockEntityRenderer<ForeverStudioMachineBlockEntity> {
    private final BlockRenderDispatcher blocks;

    public ForeverStudioMachineRenderer(BlockEntityRendererProvider.Context context) {
        blocks = context.getBlockRenderDispatcher();
    }

    @Override
    public void render(ForeverStudioMachineBlockEntity machine, float partialTick, PoseStack poses,
                       MultiBufferSource buffers, int packedLight, int packedOverlay) {
        var components = ForeverStudioMachineComponentModels.modelsFor(machine.machineId());
        if (components.isEmpty()) {
            renderLegacy(machine, poses, buffers, packedLight, packedOverlay);
            return;
        }

        var synced = ForeverStudioMachineClientState.get(machine.getBlockPos()).orElse(null);
        for (var component : components) {
            poses.pushPose();
            boolean visible = true;
            for (int index = 0; index < machine.definition().animations().size(); index++) {
                ForeverStudioMachineDefinition.AnimationDefinition animation = machine.definition().animations().get(index);
                boolean rootTarget = animation.targetComponent().equalsIgnoreCase("root");
                if (!rootTarget && !animation.targetComponent().equalsIgnoreCase(component.id())) continue;
                double phase = synced == null ? machine.animationPhase(index) : synced.animationPhase(index);
                double pivotX = rootTarget ? 0.5 : component.pivotX();
                double pivotY = rootTarget ? 0.5 : component.pivotY();
                double pivotZ = rootTarget ? 0.5 : component.pivotZ();
                if (!apply(poses, animation, phase, pivotX, pivotY, pivotZ)) {
                    visible = false;
                    break;
                }
            }
            if (visible) renderComponent(machine, component, poses, buffers, packedLight, packedOverlay);
            poses.popPose();
        }
    }

    private void renderComponent(ForeverStudioMachineBlockEntity machine,
                                 ForeverStudioMachineComponentModels.ComponentModel component,
                                 PoseStack poses, MultiBufferSource buffers, int packedLight, int packedOverlay) {
        var model = Minecraft.getInstance().getModelManager()
                .getModel(ModelResourceLocation.standalone(component.model()));
        blocks.getModelRenderer().renderModel(
                poses.last(), buffers.getBuffer(RenderType.cutout()), machine.getBlockState(), model,
                1.0F, 1.0F, 1.0F, packedLight, packedOverlay);
    }

    private void renderLegacy(ForeverStudioMachineBlockEntity machine, PoseStack poses,
                              MultiBufferSource buffers, int packedLight, int packedOverlay) {
        var synced = ForeverStudioMachineClientState.get(machine.getBlockPos()).orElse(null);
        poses.pushPose();
        boolean visible = true;
        for (int index = 0; index < machine.definition().animations().size(); index++) {
            ForeverStudioMachineDefinition.AnimationDefinition animation = machine.definition().animations().get(index);
            double phase = synced == null ? machine.animationPhase(index) : synced.animationPhase(index);
            if (!apply(poses, animation, phase, 0.5, 0.5, 0.5)) {
                visible = false;
                break;
            }
        }
        if (visible)
            blocks.renderSingleBlock(machine.getBlockState(), poses, buffers, packedLight, packedOverlay);
        poses.popPose();
    }

    private static boolean apply(PoseStack poses, ForeverStudioMachineDefinition.AnimationDefinition animation,
                                 double phase, double pivotX, double pivotY, double pivotZ) {
        String preset = animation.preset().toLowerCase(Locale.ROOT);
        String axis = animation.axis().toLowerCase(Locale.ROOT);
        double clamped = Math.clamp(phase, 0.0, 1.0);

        if (preset.contains("visibility") || preset.contains("state-switch") || preset.contains("status-blink"))
            return clamped >= 0.5;

        if (preset.contains("rotate") || preset.contains("spin") || preset.contains("valve") ||
                preset.contains("mixer") || preset.contains("drill")) {
            aroundPivot(poses, pivotX, pivotY, pivotZ, () -> rotate(poses, axis, (float)(clamped * 360.0)));
            return true;
        }

        if (preset.contains("scale") || preset.contains("pulse") || preset.contains("heat-glow") ||
                preset.contains("energy-pulse")) {
            float scale = (float)(1.0 + clamped * 0.12);
            aroundPivot(poses, pivotX, pivotY, pivotZ, () -> poses.scale(scale, scale, scale));
            return true;
        }

        if (preset.contains("shake")) {
            translate(poses, axis, Math.sin(clamped * Math.PI * 8.0) * 0.04);
            return true;
        }

        if (preset.contains("translate") || preset.contains("follow-progress") || preset.contains("press") ||
                preset.contains("piston") || preset.contains("conveyor") || preset.contains("intake") ||
                preset.contains("output-eject") || preset.contains("pump") || preset.contains("fluid-flow") ||
                preset.contains("bob") || preset.contains("oscillate") || preset.contains("spring") ||
                preset.contains("ping-pong"))
            translate(poses, axis, clamped * 0.25);
        return true;
    }

    private static void aroundPivot(PoseStack poses, double x, double y, double z, Runnable transform) {
        poses.translate(x, y, z);
        transform.run();
        poses.translate(-x, -y, -z);
    }

    private static void rotate(PoseStack poses, String axis, float degrees) {
        switch (axis) {
            case "x" -> poses.mulPose(Axis.XP.rotationDegrees(degrees));
            case "z" -> poses.mulPose(Axis.ZP.rotationDegrees(degrees));
            default -> poses.mulPose(Axis.YP.rotationDegrees(degrees));
        }
    }

    private static void translate(PoseStack poses, String axis, double amount) {
        switch (axis) {
            case "x" -> poses.translate(amount, 0, 0);
            case "z" -> poses.translate(0, 0, amount);
            default -> poses.translate(0, amount, 0);
        }
    }
}

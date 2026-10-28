package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlockEntity;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineDefinition;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.block.BlockRenderDispatcher;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderer;
import net.minecraft.client.renderer.blockentity.BlockEntityRendererProvider;

import java.util.Locale;

/**
 * Generic renderer for Studio-generated machine blocks. Runtime machines use ENTITYBLOCK_ANIMATED,
 * so this renderer owns the real baked block model and authored transforms never double-render it.
 *
 * The current generic Minecraft model is a single baked model. Multiple authored animation bindings
 * therefore compose on that model in deterministic authoring order. The target component id remains
 * in the runtime contract so a later component-split model backend can narrow the exact geometry
 * without changing machine projects or the runtime data format.
 */
public final class ForeverStudioMachineRenderer implements BlockEntityRenderer<ForeverStudioMachineBlockEntity> {
    private final BlockRenderDispatcher blocks;

    public ForeverStudioMachineRenderer(BlockEntityRendererProvider.Context context) {
        blocks = context.getBlockRenderDispatcher();
    }

    @Override
    public void render(ForeverStudioMachineBlockEntity machine, float partialTick, PoseStack poses,
                       MultiBufferSource buffers, int packedLight, int packedOverlay) {
        poses.pushPose();
        boolean visible = true;
        for (int index = 0; index < machine.definition().animations().size(); index++) {
            ForeverStudioMachineDefinition.AnimationDefinition animation = machine.definition().animations().get(index);
            double phase = machine.animationPhase(index);
            if (!apply(poses, animation, phase)) {
                visible = false;
                break;
            }
        }
        if (visible)
            blocks.renderSingleBlock(machine.getBlockState(), poses, buffers, packedLight, packedOverlay);
        poses.popPose();
    }

    private static boolean apply(PoseStack poses, ForeverStudioMachineDefinition.AnimationDefinition animation, double phase) {
        String preset = animation.preset().toLowerCase(Locale.ROOT);
        String axis = animation.axis().toLowerCase(Locale.ROOT);
        double clamped = Math.clamp(phase, 0.0, 1.0);

        if (preset.contains("visibility") || preset.contains("state-switch") || preset.contains("status-blink"))
            return clamped >= 0.5;

        if (preset.contains("rotate") || preset.contains("spin") || preset.contains("valve") ||
                preset.contains("mixer") || preset.contains("drill")) {
            aroundCenter(poses, () -> rotate(poses, axis, (float)(clamped * 360.0)));
            return true;
        }

        if (preset.contains("scale") || preset.contains("pulse") || preset.contains("heat-glow") ||
                preset.contains("energy-pulse")) {
            float scale = (float)(1.0 + clamped * 0.12);
            aroundCenter(poses, () -> poses.scale(scale, scale, scale));
            return true;
        }

        if (preset.contains("shake")) {
            double amount = Math.sin(clamped * Math.PI * 8.0) * 0.04;
            translate(poses, axis, amount);
            return true;
        }

        if (preset.contains("translate") || preset.contains("follow-progress") || preset.contains("press") ||
                preset.contains("piston") || preset.contains("conveyor") || preset.contains("intake") ||
                preset.contains("output-eject") || preset.contains("pump") || preset.contains("fluid-flow") ||
                preset.contains("bob") || preset.contains("oscillate") || preset.contains("spring") ||
                preset.contains("ping-pong")) {
            translate(poses, axis, clamped * 0.25);
        }
        return true;
    }

    private static void aroundCenter(PoseStack poses, Runnable transform) {
        poses.translate(0.5, 0.5, 0.5);
        transform.run();
        poses.translate(-0.5, -0.5, -0.5);
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

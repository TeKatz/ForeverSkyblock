package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;

/** Applies Studio-authored transforms only to explicitly named source renderer components. */
public final class ForeverStudioRendererBinding {
    private ForeverStudioRendererBinding() { }

    public static boolean component(PoseStack poses, String assetId, String componentKey, double sourcePhase,
                                    double pivotX, double pivotY, double pivotZ, Runnable draw) {
        var frame = ForeverStudioClientAnimations.evaluateRuntime(assetId, componentKey, sourcePhase);
        if (!frame.visible()) return false;
        if (frame.isIdentity()) {
            draw.run();
            return true;
        }

        double[] position = frame.position();
        double[] scale = frame.scale();
        poses.pushPose();
        poses.translate(pivotX + position[0], pivotY + position[1], pivotZ + position[2]);
        float radians = (float) Math.toRadians(frame.rotationDegrees());
        switch (frame.rotationAxis().toLowerCase(java.util.Locale.ROOT)) {
            case "x" -> poses.mulPose(Axis.XP.rotation(radians));
            case "z" -> poses.mulPose(Axis.ZP.rotation(radians));
            default -> poses.mulPose(Axis.YP.rotation(radians));
        }
        poses.scale((float) scale[0], (float) scale[1], (float) scale[2]);
        poses.translate(-pivotX, -pivotY, -pivotZ);
        draw.run();
        poses.popPose();
        return true;
    }

    public static String status(String assetId, String axisId, double sourcePhase, String sourceOption) {
        return ForeverStudioClientAnimations.statusOption(assetId, axisId, sourcePhase, sourceOption);
    }

    public static boolean effect(String assetId, String effectId, double sourcePhase, boolean sourcePredicate) {
        return ForeverStudioClientAnimations.effectEnabled(assetId, effectId, sourcePhase, sourcePredicate);
    }

    public static double elapsedPhase(String assetId, double elapsedSeconds) {
        return ForeverStudioClientAnimations.elapsedTimelinePhase(assetId, elapsedSeconds);
    }
}

package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.recipe.CondenserJob;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.world.item.ItemStack;

/** Compact process cutaway. Coordinates stay inside the space between the real inventory slots. */
final class CondenserProcessScene {
    private CondenserProcessScene() { }
    static int color(CondenserJob job) {
        return switch (job) {
            case COMPOSTING, CULTIVATING -> 0xFF8BD57C;
            case MIXING -> 0xFFD0AD79;
            case MILLING -> 0xFFDDE5E9;
            default -> 0xFF70D9E8;
        };
    }
    static void draw(GuiGraphics g, int x, int y, int width, int tier, CondenserJob job,
                     float phase, boolean running, boolean blocked, ItemStack input, ItemStack second, ItemStack result) {
        int cx = x + width / 2, tint = blocked ? 0xFFE9AF68 : color(job);
        g.fill(x, y, x + width, y + 22, 0xFF17242D);
        g.fill(x + 1, y + 21, x + width - 1, y + 22, 0xFF576673);
        g.fill(x + 2, y + 2, x + 3, y + 19, 0xFF40515E);
        g.fill(x + width - 3, y + 2, x + width - 2, y + 19, 0xFF40515E);
        float motion = job == CondenserJob.IDLE ? 0 : phase;
        boolean combining = job == CondenserJob.MIXING || job == CondenserJob.COMPOSTING || job == CondenserJob.CULTIVATING;
        if (combining) {
            g.fill(cx - 11, y + 16, cx + 12, y + 19, 0xFF657580);
            g.fill(cx - 9, y + 13, cx + 10, y + 16, 0xFF394D49);
            int arm = Math.round((float) Math.cos(motion * Math.PI * 6) * 8);
            g.fill(cx, y + 3, cx + 2, y + 16, 0xFFBAC6C9);
            g.fill(Math.min(cx, cx + arm), y + 12, Math.max(cx + 2, cx + arm + 2), y + 14, tint);
        } else if (tier >= 2) {
            gear(g, cx - 8, y + 11, motion * (job == CondenserJob.MILLING ? 1440 : 720), tint);
            gear(g, cx + 8, y + 11, -motion * (job == CondenserJob.MILLING ? 1440 : 720), tint);
        } else {
            int stroke = Math.round((job == CondenserJob.IDLE ? 0 : MachineAnimation.press(phase)) * 6);
            g.fill(cx - 12, y + 18, cx + 13, y + 20, 0xFF778A98);
            g.fill(cx - 1, y + 1, cx + 2, y + 6 + stroke, 0xFF879DAA);
            g.fill(cx - 11, y + 5 + stroke, cx + 12, y + 8 + stroke, 0xFFC1CCD0);
            g.fill(cx - 10, y + 8 + stroke, cx + 11, y + 9 + stroke, tint);
        }
        // Each ingredient has one continuous path. Intake and outlet covers hide the route endpoints.
        g.enableScissor(x + 3, y + 2, x + width - 3, y + 21);
        if (job == CondenserJob.IDLE) {
            smallItem(g, input, cx - 4, y + 10, .44F);
        } else {
            float feed = MachineAnimation.intake(phase);
            float size = .46F * MachineAnimation.ingredient(phase);
            float orbit = combining ? (float) Math.sin(motion * Math.PI * 6) * 1.4F * MachineAnimation.smooth(.18F, .32F, phase) : 0;
            float target = cx - (combining ? 9 : 4) + orbit;
            smallItem(g, input, x - 9 + (target - x + 9) * feed, y + 9, size);
            if (job == CondenserJob.MIXING || job == CondenserJob.CULTIVATING) {
                float secondFeed = MachineAnimation.intake(phase - .035F);
                smallItem(g, second, x - 9 + (cx + 4 - orbit - x + 9) * secondFeed, y + 8,
                        .44F * MachineAnimation.ingredient(phase - .035F));
            }
            float product = job == CondenserJob.CULTIVATING ? MachineAnimation.smooth(.44F, .82F, phase) : MachineAnimation.product(phase);
            float exit = MachineAnimation.discharge(phase);
            float resultX = cx - 4 + (width + 8 - width / 2F) * exit;
            float resultY = y + (job == CondenserJob.CULTIVATING ? 3 + 6 * exit : 10);
            smallItem(g, result, resultX, resultY, .46F * product);
        }
        if (running) for (int i = 0; i < 3; i++) {
            float dust = MachineAnimation.dust(phase, i);
            int fall = Math.round(((phase * 5 + i * .23F) % 1) * 5);
            int alpha = Math.round(210 * dust);
            if (alpha > 0) g.fill(cx - 7 + i * 6, y + 14 + fall, cx - 6 + i * 6, y + 15 + fall, (tint & 0xFFFFFF) | alpha << 24);
        }
        g.disableScissor();
        // Item rendering uses a raised Z layer; the physical port lips must be in front of it too.
        g.pose().pushPose(); g.pose().translate(0, 0, 200);
        g.fill(x, y + 5, x + 5, y + 19, 0xFF536976);
        g.fill(x + width - 5, y + 5, x + width, y + 19, 0xFF536976);
        g.fill(x + 1, y + 8, x + 3, y + 16, 0xFF9DB2BC);
        g.fill(x + width - 3, y + 8, x + width - 1, y + 16, tint);
        g.pose().popPose();
        if (blocked) { g.fill(x + width - 8, y + 3, x + width - 6, y + 10, tint); g.fill(x + width - 8, y + 12, x + width - 6, y + 14, tint); }
    }
    private static void gear(GuiGraphics g, int x, int y, float angle, int tint) {
        g.fill(x - 4, y - 4, x + 5, y + 5, 0xFF8A9CA7);
        for (int i = 0; i < 6; i++) {
            double a = Math.toRadians(angle + i * 60);
            int dx = (int) Math.round(Math.cos(a) * 5), dy = (int) Math.round(Math.sin(a) * 5);
            g.fill(x + dx - 1, y + dy - 1, x + dx + 2, y + dy + 2, tint);
        }
        g.fill(x - 1, y - 1, x + 2, y + 2, 0xFF22343E);
    }
    static void smallItem(GuiGraphics g, ItemStack stack, float x, float y, float scale) {
        if (stack.isEmpty() || scale <= .001F) return;
        g.pose().pushPose(); g.pose().translate(x, y, 0); g.pose().scale(scale, scale, 1);
        g.renderItem(stack, 0, 0); g.pose().popPose();
    }
}

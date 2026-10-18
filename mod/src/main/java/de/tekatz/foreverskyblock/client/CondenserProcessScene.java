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
        if (job == CondenserJob.MIXING || job == CondenserJob.COMPOSTING || job == CondenserJob.CULTIVATING) {
            g.fill(cx - 11, y + 16, cx + 12, y + 19, 0xFF657580);
            g.fill(cx - 9, y + 13, cx + 10, y + 16, 0xFF394D49);
            int arm = Math.round((float) Math.cos(motion * Math.PI * 6) * 8);
            g.fill(cx, y + 3, cx + 2, y + 16, 0xFFBAC6C9);
            g.fill(Math.min(cx, cx + arm), y + 12, Math.max(cx + 2, cx + arm + 2), y + 14, tint);
            smallItem(g, input, cx - 9, y + 6, .44F);
            if (job != CondenserJob.COMPOSTING) smallItem(g, second, cx + 4, y + 6, .44F);
            if (job == CondenserJob.CULTIVATING && phase > .45F) smallItem(g, result, cx - 3, y + 1, .45F);
        } else if (tier >= 2) {
            gear(g, cx - 8, y + 11, motion * (job == CondenserJob.MILLING ? 1440 : 720), tint);
            gear(g, cx + 8, y + 11, -motion * (job == CondenserJob.MILLING ? 1440 : 720), tint);
        } else {
            int stroke = Math.round((job == CondenserJob.IDLE ? 0 : MachineAnimation.press(phase)) * 6);
            g.fill(cx - 12, y + 18, cx + 13, y + 20, 0xFF778A98);
            g.fill(cx - 1, y + 1, cx + 2, y + 6 + stroke, 0xFF879DAA);
            g.fill(cx - 11, y + 5 + stroke, cx + 12, y + 8 + stroke, 0xFFC1CCD0);
            g.fill(cx - 10, y + 8 + stroke, cx + 11, y + 9 + stroke, tint);
            smallItem(g, input, cx - 4, y + 11, .45F);
        }
        if (running) {
            float feed = MachineAnimation.smooth(0, .20F, phase);
            if (phase < .24F) smallItem(g, input, x + 5 + Math.round(feed * (width / 2F - 22)), y + 8, .46F);
            if (phase > .45F && phase < .84F) for (int i = 0; i < 3; i++) {
                int fall = Math.round(((phase * 6 + i * .29F) % 1) * 5);
                g.fill(cx - 7 + i * 6, y + 14 + fall, cx - 6 + i * 6, y + 15 + fall, tint);
            }
            if (phase > .84F) {
                float exit = MachineAnimation.smooth(.84F, 1F, phase);
                smallItem(g, result, cx + 15 + Math.round(exit * (width / 2F - 26)), y + 9, .46F);
            }
        }
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
    static void smallItem(GuiGraphics g, ItemStack stack, int x, int y, float scale) {
        if (stack.isEmpty()) return;
        g.pose().pushPose(); g.pose().translate(x, y, 0); g.pose().scale(scale, scale, 1);
        g.renderItem(stack, 0, 0); g.pose().popPose();
    }
}

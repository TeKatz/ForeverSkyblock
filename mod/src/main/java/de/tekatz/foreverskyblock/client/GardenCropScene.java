package de.tekatz.foreverskyblock.client;

import net.minecraft.client.gui.GuiGraphics;

/** Rooted crop rows reuse the block profile's stagger, leaf growth and green-to-gold ripening. */
final class GardenCropScene {
    private GardenCropScene() { }
    static void draw(GuiGraphics g, int cx, int ground, float phase, boolean running, GardenCropProfile profile) {
        g.fill(cx - 32, ground, cx + 34, ground + 2, 0xFF765236);
        for (var stalk : profile.stalks()) {
            float growth = profile.growth(phase, stalk), ear = profile.ear(phase, stalk);
            int base = ground - Math.round((2.5F - stalk.z()) * .6F);
            int root = cx + Math.round(stalk.x() * 7 + stalk.z() * 1.5F);
            int tip = root + Math.round(profile.sway(phase, stalk, running) * .6F);
            int height = Math.round(profile.height() * stalk.height() * (.12F + .88F * growth) * 4);
            int stem = 0xFF000000 | profile.stemColor(phase), grain = 0xFF000000 | profile.grainColor(phase);
            for (int i = 0; i < height; i++) {
                int xx = root + Math.round((tip - root) * i / (float) Math.max(1, height));
                g.fill(xx, base - i, xx + 1, base - i + 1, stem);
            }
            for (int side = -1; side <= 1; side += 2) {
                int leaf = Math.round(5 * growth), yy = base - Math.round(height * (side < 0 ? .38F : .55F));
                for (int i = 0; i < leaf; i++) {
                    int xx = root + side * (i + 1);
                    g.fill(xx, yy - i / 2, xx + 2, yy - i / 2 + 1, stem);
                }
            }
            int head = Math.round(7 * ear), top = base - height;
            for (int i = 0; i < head; i++) {
                int spread = i < head - 2 ? 2 : 1;
                g.fill(tip - spread, top - i, tip + spread + 1, top - i + 1, i % 2 == 0 ? grain : stem);
            }
            if (head > 1) g.fill(tip, top - head - 2, tip + 1, top - head + 1, grain);
        }
    }
}

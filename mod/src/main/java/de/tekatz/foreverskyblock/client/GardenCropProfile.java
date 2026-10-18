package de.tekatz.foreverskyblock.client;

import java.util.List;

/** Small crop template shared by the GUI and both block displays; no production state lives here. */
public record GardenCropProfile(float height, int young, int matureStem, int matureGrain, List<Stalk> stalks) {
    public record Stalk(float x, float z, float height, float delay) { }
    public GardenCropProfile { stalks = List.copyOf(stalks); }
    public static final GardenCropProfile WHEAT = new GardenCropProfile(6.3F, 0x79AC48, 0xB6A34D, 0xE4C76A,
            List.of(new Stalk(-2.7F, -2.1F, .88F, .02F), new Stalk(0, -2.5F, 1, 0),
                    new Stalk(2.6F, -1.7F, .92F, .045F), new Stalk(-1.3F, 0, .94F, .025F),
                    new Stalk(1.5F, .3F, .96F, .015F), new Stalk(-2.6F, 2.2F, .82F, .06F),
                    new Stalk(.1F, 2.5F, .87F, .04F), new Stalk(2.8F, 2.1F, .8F, .055F)));
    public float growth(float phase, Stalk stalk) { return MachineAnimation.smooth(.04F + stalk.delay(), .72F + stalk.delay(), phase); }
    public float ear(float phase, Stalk stalk) { return MachineAnimation.smooth(.40F + stalk.delay(), .79F + stalk.delay(), phase); }
    public float ripeness(float phase) { return MachineAnimation.smooth(.58F, .84F, phase); }
    public int stemColor(float phase) { return blend(young, matureStem, ripeness(phase)); }
    public int grainColor(float phase) { return blend(young, matureGrain, ripeness(phase)); }
    public float sway(float phase, Stalk stalk, boolean running) {
        // Fade at both ends: stopping/restarting a cycle must never snap a mature stalk sideways.
        float envelope = MachineAnimation.smooth(.08F, .30F, phase) * (1 - MachineAnimation.smooth(.86F, 1, phase));
        return running ? (float) Math.sin(phase * Math.PI * 6 + stalk.x() + stalk.z()) * 2.2F * envelope : 0;
    }
    private static int blend(int a, int b, float t) {
        int red = Math.round(((a >> 16) & 255) * (1 - t) + ((b >> 16) & 255) * t);
        int green = Math.round(((a >> 8) & 255) * (1 - t) + ((b >> 8) & 255) * t);
        int blue = Math.round((a & 255) * (1 - t) + (b & 255) * t);
        return red << 16 | green << 8 | blue;
    }
}

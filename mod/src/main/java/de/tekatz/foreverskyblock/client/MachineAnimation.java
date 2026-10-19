package de.tekatz.foreverskyblock.client;

/** Pure bounded motion functions shared by the renderer and geometry regression checks. */
public final class MachineAnimation {
    private MachineAnimation() { }
    public static float smooth(float from, float to, float phase) {
        float t = Math.clamp((phase - from) / (to - from), 0F, 1F);
        return t * t * (3F - 2F * t);
    }
    /** Feed, compression hold, release, discharge: one stroke per actual recipe cycle. */
    public static float press(float phase) {
        return smooth(.16F, .48F, phase) * (1F - smooth(.64F, .83F, phase));
    }
    public static float growth(float phase) { return smooth(.04F, .84F, phase); }
    public static float harvest(float phase) { return smooth(.86F, 1F, phase); }
    /** Workpieces enter the mechanism, are consumed under load, and become the emerging product. */
    public static float intake(float phase) { return smooth(.02F, .30F, phase); }
    public static float ingredient(float phase) { return smooth(0, .06F, phase) * (1F - smooth(.38F, .62F, phase)); }
    public static float product(float phase) { return smooth(.64F, .82F, phase); }
    public static float discharge(float phase) { return smooth(.84F, 1F, phase); }
    public static float dust(float phase, int index) {
        float fall = (phase * 5 + index * .23F) % 1;
        return smooth(.38F, .46F, phase) * (1F - smooth(.76F, .84F, phase))
                * (float) Math.sin(Math.PI * fall);
    }
    public static float phase(float progress, int duration) { return Math.clamp(progress / Math.max(1, duration), 0F, 1F); }
}

package de.tekatz.foreverskyblock.client;

/** Pure bounded motion functions shared by the renderer and geometry regression checks. */
public final class MachineAnimation {
    private MachineAnimation() { }
    public static float compression(float phase) { return (float)Math.pow(Math.sin(Math.PI*Math.clamp(phase,0,1)),2)*2.2F; }
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
    public static float phase(float progress, int duration) { return Math.clamp(progress / Math.max(1, duration), 0F, 1F); }
}

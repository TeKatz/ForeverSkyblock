package de.tekatz.foreverskyblock.client;

/** Pure bounded motion functions shared by the renderer and geometry regression checks. */
public final class MachineAnimation {
    private MachineAnimation() { }
    public static float compression(float phase) { return (float)Math.pow(Math.sin(Math.PI*Math.clamp(phase,0,1)),2)*2.2F; }
}

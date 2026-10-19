package de.tekatz.foreverskyblock.client;

/** Frame-rate independent, bounded menu motion. No world ticks or frame counters. */
public final class MenuMotion {
    private double value, target;
    private long previous;
    public double value() { return value; }
    public double target() { return target; }
    public void snap(double position) { value = target = Math.max(0, position); previous = 0; }
    public void scroll(double delta, double maximum) { target = Math.clamp(target + delta, 0, Math.max(0, maximum)); }
    public double advance(long now) {
        double seconds = previous == 0 ? 1D / 60 : Math.clamp((now - previous) / 1_000_000_000D, 0, 0.1);
        previous = now;
        value += (target - value) * (1 - Math.exp(-18 * seconds));
        if (Math.abs(target - value) < 0.05) value = target;
        return value;
    }
    public static double reveal(long start, long now) {
        return reveal(start,now,200);
    }
    public static double reveal(long start,long now,int milliseconds) {
        double t = milliseconds<=0?1:Math.clamp((now - start) / (milliseconds*1_000_000D), 0, 1);
        return 1 - Math.pow(1 - t, 3);
    }
    public static boolean insideNode(double mx, double my, double cx, double cy, double radius) {
        return (mx - cx) * (mx - cx) + (my - cy) * (my - cy) <= radius * radius;
    }
}

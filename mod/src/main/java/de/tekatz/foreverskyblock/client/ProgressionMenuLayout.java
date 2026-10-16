package de.tekatz.foreverskyblock.client;

/**
 * Logical coordinates for the Design-2 progression menu.
 * The 960x540 / GUI-scale-2 target keeps the full challenge sidebar and three-card grid;
 * smaller viewports collapse the sidebar while retaining native-size text.
 */
public record ProgressionMenuLayout(int width, int height, boolean compact) {
    public static ProgressionMenuLayout fit(int windowWidth, int windowHeight) {
        int w = Math.max(300, Math.min(944, windowWidth - 12));
        int h = Math.max(168, Math.min(522, windowHeight - 12));
        boolean compact = w < 760 || h < 430;
        return new ProgressionMenuLayout(w, h, compact);
    }

    public int padding() { return 8; }
    public boolean tiny() { return height < 240 || width < 360; }
    public int navTop() { return tiny() ? 30 : compact ? 50 : 66; }
    public int navHeight() { return tiny() ? 24 : compact ? 28 : 34; }
    public int contentTop() { return navTop() + navHeight() + (tiny() ? 6 : 8); }

    public int challengeSidebarWidth() {
        return compact ? 0 : 194;
    }

    public int challengeMainX() {
        return padding() + (challengeSidebarWidth() > 0 ? challengeSidebarWidth() + 8 : 0);
    }

    public int challengeMainWidth() {
        return width - challengeMainX() - padding();
    }

    public int challengeGridTop() {
        if (tiny()) return contentTop() + 34;
        if (challengeSidebarWidth() > 0) return contentTop() + 42;
        return contentTop() + (challengeMainWidth() < 520 ? 84 : 62);
    }

    public int challengeGridHeight() {
        return Math.max(24, height - challengeGridTop() - padding());
    }

    public int challengeColumns() {
        int available = challengeMainWidth();
        if (available >= 680) return 3;
        if (available >= 430) return 2;
        return 1;
    }
}

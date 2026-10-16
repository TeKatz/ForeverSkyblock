package de.tekatz.foreverskyblock.client;

/**
 * Layout tuned against the approved Design-2 mockup.
 * The primary target is a 640x360 logical GUI (1920x1080 at GUI scale 3), where the
 * challenge sidebar and a three-column grid must both remain visible.
 */
public record PremiumMenuLayout(int width, int height) {
    public static PremiumMenuLayout fit(int windowWidth, int windowHeight) {
        int w = Math.max(300, Math.min(944, windowWidth - 10));
        int h = Math.max(170, Math.min(522, windowHeight - 10));
        return new PremiumMenuLayout(w, h);
    }

    public int padding() { return height < 240 ? 5 : 7; }
    public boolean tiny() { return width < 470 || height < 280; }
    public boolean compact() { return width < 760 || height < 430; }

    public int headerHeight() {
        if (tiny()) return 44;
        return compact() ? 54 : 68;
    }

    public int navTop() { return padding() + headerHeight(); }
    public int navHeight() { return tiny() ? 22 : compact() ? 25 : 32; }
    public int contentTop() { return navTop() + navHeight() + (compact() ? 5 : 7); }
    public int contentBottom() { return height - padding(); }
    public int contentHeight() { return Math.max(32, contentBottom() - contentTop()); }

    public int challengeSidebarWidth() {
        if (tiny()) return 0;
        return Math.clamp(width / 5, 122, 190);
    }

    public int challengeGap() { return compact() ? 5 : 7; }

    public int challengeMainX() {
        return padding() + (challengeSidebarWidth() == 0 ? 0 : challengeSidebarWidth() + challengeGap());
    }

    public int challengeMainWidth() {
        return width - challengeMainX() - padding();
    }

    public int challengeHeaderHeight() { return tiny() ? 27 : compact() ? 32 : 39; }
    public int challengeGridTop() { return contentTop() + challengeHeaderHeight(); }
    public int challengeGridHeight() { return Math.max(28, contentBottom() - challengeGridTop()); }

    public int challengeColumns() {
        int available = challengeMainWidth();
        if (available >= 440) return 3;
        if (available >= 295) return 2;
        return 1;
    }

    public int challengeCardHeight() {
        if (height < 390) return 58;
        if (height < 470) return 68;
        return 78;
    }

    public int challengeCardGap() { return compact() ? 4 : 6; }

    public int sidebarRowHeight() {
        if (height < 390) return 20;
        return compact() ? 23 : 26;
    }
}

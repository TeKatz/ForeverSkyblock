package de.tekatz.foreverskyblock.client;

/** Logical GUI coordinates: narrow windows keep full-width cards and native-size text. */
public record ProgressionMenuLayout(int width, int height, boolean compact, int contentX, int contentWidth) {
    public static ProgressionMenuLayout fit(int windowWidth, int windowHeight) {
        int w = Math.min(760, windowWidth - 12), h = Math.min(440, windowHeight - 12);
        boolean compact = w < 520 || h < 260;
        int contentX = compact ? 10 : 136;
        return new ProgressionMenuLayout(w, h, compact, contentX, w - contentX - 10);
    }
    public int cardsTop(boolean search) { return search ? 90 : 66; }
    public int cardsHeight(boolean search) { return Math.max(24, height - 38 - cardsTop(search)); }
}

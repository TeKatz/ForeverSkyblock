package de.tekatz.foreverskyblock.progression;

import java.util.Locale;

/** Forever-owned progression wrapper around Apotheosis' native five World Tiers. */
public enum ApotheosisStage {
    DORMANT,
    HAVEN,
    FRONTIER,
    ASCENT,
    SUMMIT,
    PINNACLE;

    public String id() { return name().toLowerCase(Locale.ROOT); }
    public boolean awakened() { return this != DORMANT; }
    public boolean directlyFollows(ApotheosisStage previous) {
        return previous != null && ordinal() == previous.ordinal() + 1;
    }

    public static ApotheosisStage parse(String value) {
        if (value == null || value.isBlank()) return DORMANT;
        try { return valueOf(value.toUpperCase(Locale.ROOT)); }
        catch (IllegalArgumentException ignored) { return DORMANT; }
    }
}

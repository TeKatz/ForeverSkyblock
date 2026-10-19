package de.tekatz.foreverskyblock.progression;

import net.minecraft.server.level.ServerPlayer;

/** Explicit server-side test/admin operations; never exposed through client-authored payloads. */
public interface ProgressionAdminAccess {
    boolean adminHardReset(ServerPlayer player);
    boolean adminGrantPerks(ServerPlayer player,int amount);
    boolean adminTierUp(ServerPlayer player);
    boolean adminSetTier(ServerPlayer player,int tier);
}

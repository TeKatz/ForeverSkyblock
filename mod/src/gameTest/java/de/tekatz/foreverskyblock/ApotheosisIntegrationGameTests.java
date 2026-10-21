package de.tekatz.foreverskyblock;

import java.util.Arrays;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.neoforged.fml.ModList;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ApotheosisIntegrationGameTests {
    @GameTest(template = "empty")
    public static void pinnedApotheosisRuntimeMatchesForeverIntegration(GameTestHelper h) throws Exception {
        if (!Boolean.getBoolean("forever_skyblock.expectApotheosis")) { h.succeed(); return; }

        h.assertTrue(ModList.get().isLoaded("apotheosis"), "Pinned Apotheosis is loaded");
        h.assertTrue(ModList.get().isLoaded("placebo"), "Pinned Placebo is loaded");
        h.assertTrue(ModList.get().isLoaded("apothic_attributes"), "Pinned Apothic Attributes is loaded");

        Class<?> config = Class.forName("dev.shadowsoffire.apotheosis.AdventureConfig");
        h.assertTrue(!config.getField("enableManualWorldTierChanges").getBoolean(null),
                "Forever owns World Tier activation and disables Apotheosis manual tier changes");

        Class<?> tier = Class.forName("dev.shadowsoffire.apotheosis.tiers.WorldTier");
        String[] tiers = Arrays.stream(tier.getEnumConstants()).map(Object::toString).toArray(String[]::new);
        h.assertTrue(Arrays.equals(tiers, new String[]{"HAVEN", "FRONTIER", "ASCENT", "SUMMIT", "PINNACLE"}),
                "Pinned Apotheosis exposes the five native World Tiers Forever maps onto");
        h.assertTrue(Arrays.stream(tier.getMethods()).anyMatch(m -> m.getName().equals("setTier") && m.getParameterCount() == 2),
                "Pinned Apotheosis exposes WorldTier.setTier used by Forever synchronization");

        Class<?> mobs = Class.forName("dev.shadowsoffire.apotheosis.mobs.ApothMobEvents");
        h.assertTrue(Arrays.stream(mobs.getDeclaredMethods()).anyMatch(m -> m.getName().equals("finalizeMobSpawns")),
                "Pinned Apotheosis exposes the mob-processing boundary used by the Dormant gate");
        Class<?> loot = Class.forName("dev.shadowsoffire.apotheosis.loot.modifiers.ContextualLootModifier");
        h.assertTrue(Arrays.stream(loot.getDeclaredMethods()).anyMatch(m -> m.getName().equals("doApply")),
                "Pinned Apotheosis exposes the contextual loot boundary used by the Dormant gate");
        Class<?> events = Class.forName("dev.shadowsoffire.apotheosis.AdventureEvents");
        h.assertTrue(Arrays.stream(events.getDeclaredMethods()).anyMatch(m -> m.getName().equals("applyMissedTierAugments")),
                "Pinned Apotheosis exposes the tier-augment boundary used by the Dormant gate");

        h.succeed();
    }
}

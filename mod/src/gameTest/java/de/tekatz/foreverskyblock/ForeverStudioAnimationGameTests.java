package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationDocument;
import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationRegistry;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ForeverStudioAnimationGameTests {
    @GameTest(template = "empty")
    public static void studioAnimationDatapackLoadsIntoRuntimeRegistry(GameTestHelper helper) {
        var document = ForeverStudioAnimationRegistry.forAsset("runtime_loader_fixture").orElse(null);
        helper.assertTrue(document != null,
                "Forever Studio format-v1 fixture must be loaded by the server datapack reload listener");
        if (document == null) {
            helper.fail("Forever Studio animation fixture missing from runtime registry");
            return;
        }

        helper.assertTrue(document.formatVersion() == 1, "Runtime registry must preserve formatVersion 1");
        helper.assertTrue(document.animation().tracks().size() == 1, "Fixture must expose one target track");
        helper.assertTrue(ForeverStudioAnimationRegistry.targetTrack(
                        "runtime_loader_fixture", "runtime:runtime_loader_fixture:test_component").isPresent(),
                "Stable runtime TargetId lookup must resolve the fixture component");
        helper.assertTrue(ForeverStudioAnimationRegistry.statusTrack("runtime_loader_fixture", "status").isPresent(),
                "Status track lookup must resolve the fixture axis");
        helper.assertTrue(ForeverStudioAnimationRegistry.effectTrack("runtime_loader_fixture", "test-effect")
                        .map(track -> track.keyframes().getFirst().mode() == ForeverStudioAnimationDocument.EffectMode.SUPPRESSED)
                        .orElse(false),
                "Effect track lookup must preserve the suppressed mode");
        helper.succeed();
    }
}

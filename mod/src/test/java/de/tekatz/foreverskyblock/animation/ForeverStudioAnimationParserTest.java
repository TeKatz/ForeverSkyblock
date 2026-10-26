package de.tekatz.foreverskyblock.animation;

import com.google.gson.JsonParser;
import org.junit.jupiter.api.Test;

import java.util.Map;

import static org.junit.jupiter.api.Assertions.*;

final class ForeverStudioAnimationParserTest {
    private static final String VALID = """
            {
              "formatVersion": 1,
              "assetId": "condenser_mark_ii",
              "sourceModel": "mod/src/main/resources/assets/forever_skyblock/models/block/condenser_mark_ii.json",
              "futureField": { "ignored": true },
              "animation": {
                "id": "default",
                "displayName": "Condenser Test",
                "durationSeconds": 6.0,
                "loop": true,
                "tracks": [{
                  "targetId": "json:model#element:0123456789abcdefabcd",
                  "modelKey": "model|element:0",
                  "elementIndex": 0,
                  "keyframes": [
                    { "timeSeconds": 0, "position": [0,0,0], "rotationAxis": "y", "rotationDegrees": 0,
                      "scale": [1,1,1], "visible": true, "textureState": "", "lightStatus": "",
                      "specialEffect": "", "machineState": "Idle", "interpolation": "Smooth" },
                    { "timeSeconds": 2, "position": [1,2,3], "rotationAxis": "x", "rotationDegrees": 45,
                      "scale": [1,1.5,1], "visible": false, "textureState": "#active",
                      "interpolation": "Step" }
                  ]
                }],
                "runtimeStateBase": { "status": "idle", "job": "idle" },
                "statusTracks": [{ "axisId": "status", "keyframes": [
                  { "timeSeconds": 1, "optionId": "running" }
                ]}],
                "effectTracks": [{ "effectId": "process-dust", "keyframes": [
                  { "timeSeconds": 3, "mode": "suppressed" }
                ]}]
              }
            }
            """;

    @Test void parsesCompleteStudioFormatAndIgnoresUnknownExtensionFields() {
        var document = ForeverStudioAnimationParser.parse(JsonParser.parseString(VALID).getAsJsonObject());
        assertEquals(1, document.formatVersion());
        assertEquals("condenser_mark_ii", document.assetId());
        assertEquals(6.0D, document.animation().durationSeconds());
        assertTrue(document.animation().loop());
        assertEquals(1, document.animation().tracks().size());
        assertEquals(2, document.animation().tracks().getFirst().keyframes().size());
        assertFalse(document.animation().tracks().getFirst().keyframes().get(1).visible());
        assertEquals(ForeverStudioAnimationDocument.Interpolation.STEP,
                document.animation().tracks().getFirst().keyframes().get(1).interpolation());
        assertEquals("running", document.animation().statusTracks().getFirst().keyframes().getFirst().optionId());
        assertEquals(ForeverStudioAnimationDocument.EffectMode.SUPPRESSED,
                document.animation().effectTracks().getFirst().keyframes().getFirst().mode());
    }

    @Test void rejectsUnsupportedBreakingFormatVersion() {
        var root = JsonParser.parseString(VALID).getAsJsonObject();
        root.addProperty("formatVersion", 2);
        var error = assertThrows(IllegalArgumentException.class, () -> ForeverStudioAnimationParser.parse(root));
        assertTrue(error.getMessage().contains("Unsupported formatVersion 2"));
    }

    @Test void rejectsDuplicateStableTargetsInsteadOfSilentlyMergingThem() {
        var root = JsonParser.parseString(VALID).getAsJsonObject();
        var tracks = root.getAsJsonObject("animation").getAsJsonArray("tracks");
        tracks.add(tracks.get(0).deepCopy());
        var error = assertThrows(IllegalArgumentException.class, () -> ForeverStudioAnimationParser.parse(root));
        assertTrue(error.getMessage().contains("Duplicate stable targetId"));
    }

    @Test void rejectsInvalidEffectModesAndNegativeTimes() {
        var invalidEffect = JsonParser.parseString(VALID).getAsJsonObject();
        invalidEffect.getAsJsonObject("animation").getAsJsonArray("effectTracks").get(0).getAsJsonObject()
                .getAsJsonArray("keyframes").get(0).getAsJsonObject().addProperty("mode", "forced");
        assertThrows(IllegalArgumentException.class, () -> ForeverStudioAnimationParser.parse(invalidEffect));

        var invalidTime = JsonParser.parseString(VALID).getAsJsonObject();
        invalidTime.getAsJsonObject("animation").getAsJsonArray("tracks").get(0).getAsJsonObject()
                .getAsJsonArray("keyframes").get(1).getAsJsonObject().addProperty("timeSeconds", -0.25D);
        assertThrows(IllegalArgumentException.class, () -> ForeverStudioAnimationParser.parse(invalidTime));
    }

    @Test void preservesStudioWarningKeysBeyondDurationInsteadOfRejectingTheDocument() {
        var root = JsonParser.parseString(VALID).getAsJsonObject();
        root.getAsJsonObject("animation").getAsJsonArray("tracks").get(0).getAsJsonObject()
                .getAsJsonArray("keyframes").get(1).getAsJsonObject().addProperty("timeSeconds", 7.0D);
        var document = assertDoesNotThrow(() -> ForeverStudioAnimationParser.parse(root));
        assertEquals(7.0D, document.animation().tracks().getFirst().keyframes().get(1).timeSeconds());
    }

    @Test void registryReplacesSnapshotsAtomicallyAndExposesStableLookups() {
        var document = ForeverStudioAnimationParser.parse(JsonParser.parseString(VALID).getAsJsonObject());
        long before = ForeverStudioAnimationRegistry.generation();
        long after = ForeverStudioAnimationRegistry.replace(Map.of("condenser_mark_ii", document));
        assertEquals(before + 1, after);
        assertSame(document, ForeverStudioAnimationRegistry.forAsset("CONDENSER_MARK_II").orElseThrow());
        assertTrue(ForeverStudioAnimationRegistry.targetTrack("condenser_mark_ii", "json:model#element:0123456789abcdefabcd").isPresent());
        assertTrue(ForeverStudioAnimationRegistry.statusTrack("condenser_mark_ii", "STATUS").isPresent());
        assertTrue(ForeverStudioAnimationRegistry.effectTrack("condenser_mark_ii", "PROCESS-DUST").isPresent());
    }
}

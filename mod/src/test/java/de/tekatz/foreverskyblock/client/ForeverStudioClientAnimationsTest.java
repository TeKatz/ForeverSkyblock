package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationDocument;
import de.tekatz.foreverskyblock.animation.ForeverStudioAnimationWire;
import de.tekatz.foreverskyblock.network.ForeverStudioAnimationNetwork;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

import java.util.List;
import java.util.Map;

import static de.tekatz.foreverskyblock.animation.ForeverStudioAnimationDocument.*;
import static org.junit.jupiter.api.Assertions.*;

final class ForeverStudioClientAnimationsTest {
    @AfterEach void clearClientSnapshot() { ForeverStudioClientAnimations.clear(); }

    @Test void mirrorsStudioInterpolationAndAxisMismatchRules() {
        Track track = new Track("runtime:demo:arm", "", -1, List.of(
                key(2, new double[]{2,0,0}, "y", 90, new double[]{2,1,1}, false, Interpolation.SMOOTH),
                key(4, new double[]{4,0,0}, "x", 180, new double[]{1,1,1}, true, Interpolation.LINEAR)));

        var before = ForeverStudioClientAnimations.evaluate(track, 1);
        assertArrayEquals(new double[]{1,0,0}, before.position(), .000001);
        assertArrayEquals(new double[]{1.5,1,1}, before.scale(), .000001);
        assertEquals("y", before.rotationAxis());
        assertEquals(45, before.rotationDegrees(), .000001);
        assertTrue(before.visible(), "Visibility remains the neutral source value before the first exact key");

        var middle = ForeverStudioClientAnimations.evaluate(track, 3);
        assertArrayEquals(new double[]{3,0,0}, middle.position(), .000001);
        assertEquals("y", middle.rotationAxis());
        assertEquals(90, middle.rotationDegrees(), .000001, "Different rotation axes hold the left axis/angle");
        assertFalse(middle.visible(), "Discrete visibility holds the left key between keys");

        var exact = ForeverStudioClientAnimations.evaluate(track, 4);
        assertEquals("x", exact.rotationAxis());
        assertEquals(180, exact.rotationDegrees(), .000001);
        assertTrue(exact.visible());
        var held = ForeverStudioClientAnimations.evaluate(track, 9);
        assertArrayEquals(exact.position(), held.position(), .000001);
        assertArrayEquals(exact.scale(), held.scale(), .000001);
        assertEquals(exact.rotationAxis(), held.rotationAxis());
        assertEquals(exact.rotationDegrees(), held.rotationDegrees(), .000001);
        assertEquals(exact.visible(), held.visible());
    }

    @Test void stepInterpolationHoldsLeftTransform() {
        Track track = new Track("runtime:demo:arm", "", -1, List.of(
                key(1, new double[]{1,2,3}, "z", 30, new double[]{1,2,1}, true, Interpolation.STEP),
                key(3, new double[]{9,9,9}, "z", 90, new double[]{3,3,3}, true, Interpolation.LINEAR)));
        var frame = ForeverStudioClientAnimations.evaluate(track, 2.5);
        assertArrayEquals(new double[]{1,2,3}, frame.position(), .000001);
        assertEquals(30, frame.rotationDegrees(), .000001);
        assertArrayEquals(new double[]{1,2,1}, frame.scale(), .000001);
    }

    @Test void wireSnapshotInstallsAtomicallyAndUsesCanonicalRuntimeIds() {
        var document = document();
        String json = ForeverStudioAnimationWire.encodeSnapshot(List.of(document));
        ForeverStudioClientAnimations.accept(new ForeverStudioAnimationNetwork.SyncChunk(7,0,1,json));

        assertEquals(7, ForeverStudioClientAnimations.generation());
        assertEquals(1, ForeverStudioClientAnimations.size());
        assertEquals("runtime:condenser-mark-i:press-head",
                ForeverStudioClientAnimations.runtimeTargetId("Condenser Mark I", "Press / Head"));
        assertTrue(ForeverStudioClientAnimations.document("DEMO").isPresent());

        var frame = ForeverStudioClientAnimations.evaluateRuntime("demo", "arm", .5);
        assertArrayEquals(new double[]{2,0,0}, frame.position(), .000001);
    }

    @Test void incompleteChunksNeverReplaceTheInstalledSnapshot() {
        String json = ForeverStudioAnimationWire.encodeSnapshot(List.of(document()));
        int split = json.length()/2;
        ForeverStudioClientAnimations.accept(new ForeverStudioAnimationNetwork.SyncChunk(8,0,2,json.substring(0,split)));
        assertEquals(-1, ForeverStudioClientAnimations.generation());
        assertEquals(0, ForeverStudioClientAnimations.size());
        ForeverStudioClientAnimations.accept(new ForeverStudioAnimationNetwork.SyncChunk(8,1,2,json.substring(split)));
        assertEquals(8, ForeverStudioClientAnimations.generation());
        assertEquals(1, ForeverStudioClientAnimations.size());
    }

    @Test void statusIsSourceBackedBeforeFirstKeyAndEffectsCanOnlySuppress() {
        String json = ForeverStudioAnimationWire.encodeSnapshot(List.of(document()));
        ForeverStudioClientAnimations.accept(new ForeverStudioAnimationNetwork.SyncChunk(9,0,1,json));

        assertEquals("idle", ForeverStudioClientAnimations.statusOption("demo","status",.1,"idle"));
        assertEquals("running", ForeverStudioClientAnimations.statusOption("demo","status",.5,"idle"));
        assertTrue(ForeverStudioClientAnimations.effectEnabled("demo","sparks",.25,true));
        assertFalse(ForeverStudioClientAnimations.effectEnabled("demo","sparks",.75,true));
        assertFalse(ForeverStudioClientAnimations.effectEnabled("demo","sparks",.25,false),
                "Studio must never force an effect past a false source predicate");
    }

    @Test void elapsedSourceIndependentTimelineRespectsLoopFlag() {
        String json = ForeverStudioAnimationWire.encodeSnapshot(List.of(document()));
        ForeverStudioClientAnimations.accept(new ForeverStudioAnimationNetwork.SyncChunk(10,0,1,json));
        assertEquals(.25, ForeverStudioClientAnimations.elapsedTimelinePhase("demo",5), .000001);
    }

    private static ForeverStudioAnimationDocument document() {
        Track track = new Track("runtime:demo:arm", "", -1, List.of(
                key(0, new double[]{0,0,0}, "y", 0, new double[]{1,1,1}, true, Interpolation.LINEAR),
                key(4, new double[]{4,0,0}, "y", 90, new double[]{2,1,1}, true, Interpolation.SMOOTH)));
        StatusTrack status = new StatusTrack("status", List.of(new StatusKeyframe(1,"running")));
        EffectTrack effect = new EffectTrack("sparks", List.of(new EffectKeyframe(2,EffectMode.SUPPRESSED)));
        Animation animation = new Animation("default","Runtime test",4,true,List.of(track),Map.of(),List.of(status),List.of(effect));
        return new ForeverStudioAnimationDocument(1,"demo","models/demo.json",animation);
    }

    private static Keyframe key(double time,double[] position,String axis,double rotation,double[] scale,
                                boolean visible,Interpolation interpolation) {
        return new Keyframe(time,position,axis,rotation,scale,visible,"","","", "Idle",interpolation);
    }
}

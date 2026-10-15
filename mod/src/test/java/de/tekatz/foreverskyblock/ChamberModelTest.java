package de.tekatz.foreverskyblock;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class ChamberModelTest {
    private record Box(double[] lo, double[] hi) { }
    private record Face(int axis, double plane, double[] lo, double[] hi) { }
    private static double[] vector(JsonArray values) { return new double[]{values.get(0).getAsDouble(), values.get(1).getAsDouble(), values.get(2).getAsDouble()}; }
    @Test void exposedMeshHasNoOverlappingSolidsOrCoplanarFacesAndOnlyBoundaryCulling() throws Exception {
        try (var stream = getClass().getResourceAsStream("/assets/forever_skyblock/models/block/mixing_chamber_mark_i.json")) {
            assertNotNull(stream);
            var model = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8)).getAsJsonObject();
            List<Box> boxes = new ArrayList<>(); List<Face> faces = new ArrayList<>(); double volume = 0;
            for (var raw : model.getAsJsonArray("elements")) {
                JsonObject element = raw.getAsJsonObject(); double[] lo = vector(element.getAsJsonArray("from")), hi = vector(element.getAsJsonArray("to"));
                for (int axis = 0; axis < 3; axis++) assertTrue(lo[axis] >= 0 && hi[axis] <= 16 && lo[axis] < hi[axis]);
                volume += (hi[0] - lo[0]) * (hi[1] - lo[1]) * (hi[2] - lo[2]);
                boxes.add(new Box(lo, hi));
                for (var entry : element.getAsJsonObject("faces").entrySet()) {
                    String direction = entry.getKey(); int axis = switch (direction) { case "west", "east" -> 0; case "down", "up" -> 1; default -> 2; };
                    boolean positive = List.of("east", "up", "south").contains(direction);
                    double plane = positive ? hi[axis] : lo[axis];
                    var face = entry.getValue().getAsJsonObject();
                    if (face.has("cullface")) {
                        assertEquals(direction, face.get("cullface").getAsString());
                        assertEquals(positive ? 16 : 0, plane, "Inset surfaces must not be culled by unrelated neighbor faces");
                    }
                    faces.add(new Face(axis, plane, lo, hi));
                }
            }
            assertEquals(3536, volume, "Keep the original housing, posts and raised basin volume");
            for (int i = 0; i < boxes.size(); i++) for (int k = i + 1; k < boxes.size(); k++) {
                Box a = boxes.get(i), b = boxes.get(k); boolean intersects = true;
                for (int axis = 0; axis < 3; axis++) intersects &= Math.min(a.hi()[axis], b.hi()[axis]) > Math.max(a.lo()[axis], b.lo()[axis]);
                assertFalse(intersects, "Wood and stone volumes must not overlap");
            }
            for (int i = 0; i < faces.size(); i++) for (int k = i + 1; k < faces.size(); k++) {
                Face a = faces.get(i), b = faces.get(k);
                if (a.axis() != b.axis() || a.plane() != b.plane()) continue;
                boolean overlaps = true;
                for (int axis = 0; axis < 3; axis++) if (axis != a.axis()) overlaps &= Math.min(a.hi()[axis], b.hi()[axis]) > Math.max(a.lo()[axis], b.lo()[axis]);
                assertFalse(overlaps, "Coplanar overlapping faces cause z-fighting");
            }
        }
    }
}

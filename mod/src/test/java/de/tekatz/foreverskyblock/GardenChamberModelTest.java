package de.tekatz.foreverskyblock;

import com.google.gson.JsonObject;
import de.tekatz.foreverskyblock.client.GardenTreeShape;
import com.google.gson.JsonParser;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class GardenChamberModelTest {
    private record Face(int axis, double plane, double[] lo, double[] hi) { }
    private JsonObject model(String name) throws Exception {
        try (var stream = getClass().getResourceAsStream("/assets/forever_skyblock/models/block/" + name + ".json")) {
            assertNotNull(stream);
            return JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8)).getAsJsonObject();
        }
    }
    @Test void clearInsetGreenhouseMeshHasNoCoplanarOverlapsOrHiddenFaceCulling() throws Exception {
        var model = model("garden_chamber_mark_i");
        assertEquals("minecraft:cutout", model.get("render_type").getAsString());
        assertEquals("forever_skyblock:block/garden_glass", model.getAsJsonObject("textures").get("glass").getAsString());
        var faces = new ArrayList<Face>();
        int glassFaces = 0;
        for (var raw : model.getAsJsonArray("elements")) {
            var element = raw.getAsJsonObject();
            double[] lo = new double[3], hi = new double[3];
            for (int axis = 0; axis < 3; axis++) {
                lo[axis] = element.getAsJsonArray("from").get(axis).getAsDouble();
                hi[axis] = element.getAsJsonArray("to").get(axis).getAsDouble();
                assertTrue(lo[axis] >= 0 && hi[axis] <= 16 && lo[axis] <= hi[axis]);
            }
            for (var entry : element.getAsJsonObject("faces").entrySet()) {
                String direction = entry.getKey();
                int axis = switch (direction) { case "west", "east" -> 0; case "up", "down" -> 1; default -> 2; };
                boolean positive = direction.equals("east") || direction.equals("up") || direction.equals("south");
                var face = entry.getValue().getAsJsonObject();
                assertFalse(face.has("cullface"), "An adjacent opaque module must not erase inset greenhouse faces");
                if (face.get("texture").getAsString().equals("#glass")) glassFaces++;
                faces.add(new Face(axis, positive ? hi[axis] : lo[axis], lo, hi));
            }
        }
        assertTrue(glassFaces >= 10, "All four sides and roof need two-sided clear glazing");
        for (int i = 0; i < faces.size(); i++) for (int j = i + 1; j < faces.size(); j++) {
            Face a = faces.get(i), b = faces.get(j);
            if (a.axis != b.axis || Math.abs(a.plane - b.plane) > .00001) continue;
            boolean overlap = true;
            for (int axis = 0; axis < 3; axis++) if (axis != a.axis)
                overlap &= Math.min(a.hi[axis], b.hi[axis]) - Math.max(a.lo[axis], b.lo[axis]) > .00001;
            assertFalse(overlap, "Touching frame pieces must not leave overlapping coplanar faces");
        }
    }
    @Test void inventoryTreeDoesNotBecomeAFakePermanentCropInThePlacedBlock() throws Exception {
        var placed = model("garden_chamber_mark_i");
        var icon = model("garden_chamber_inventory");
        assertEquals(placed.getAsJsonArray("elements").size() + GardenTreeShape.PARTS.stream().filter(part -> part.faces() != 0).count(), icon.getAsJsonArray("elements").size());
        assertFalse(placed.getAsJsonObject("textures").has("leaves"));
    }
    @Test void everyExportedGardenElementHasAtLeastOneFaceAsRequiredByMinecraft() throws Exception {
        for (String name : new String[]{"garden_chamber_mark_i", "garden_chamber_inventory"}) {
            for (var raw : model(name).getAsJsonArray("elements")) {
                int faces = raw.getAsJsonObject().getAsJsonObject("faces").size();
                assertTrue(faces >= 1 && faces <= 6, "Minecraft rejects empty-face elements even inside a valid JSON model: " + name);
            }
        }
    }

}

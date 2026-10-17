package de.tekatz.foreverskyblock;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.HashSet;
import java.util.Set;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class FreezingChamberModelTest {
    @Test void designThreeModelKeepsOpenIcyBasinAndFourFacings() throws Exception {
        try (var stream = getClass().getResourceAsStream("/assets/forever_skyblock/models/block/freezing_chamber_mark_i.json")) {
            assertNotNull(stream);
            JsonObject model = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8)).getAsJsonObject();
            assertEquals("forever_skyblock:block/freezing_ice", model.getAsJsonObject("textures").get("ice").getAsString());

            boolean recessedIce = false;
            for (var raw : model.getAsJsonArray("elements")) {
                JsonObject element = raw.getAsJsonObject();
                var from = element.getAsJsonArray("from");
                var to = element.getAsJsonArray("to");
                for (int axis = 0; axis < 3; axis++) {
                    assertTrue(from.get(axis).getAsDouble() >= 0);
                    assertTrue(to.get(axis).getAsDouble() <= 16);
                    assertTrue(from.get(axis).getAsDouble() <= to.get(axis).getAsDouble());
                }
                for (var face : element.getAsJsonObject("faces").entrySet()) {
                    if (face.getValue().getAsJsonObject().get("texture").getAsString().equals("#ice")) {
                        recessedIce = from.get(1).getAsDouble() > 12 && to.get(1).getAsDouble() < 13;
                    }
                }
            }
            assertTrue(recessedIce, "Design 3 must retain its recessed icy top basin");
        }

        try (var stream = getClass().getResourceAsStream("/assets/forever_skyblock/blockstates/freezing_chamber_mark_i.json")) {
            assertNotNull(stream);
            JsonObject states = JsonParser.parseReader(new InputStreamReader(stream, StandardCharsets.UTF_8)).getAsJsonObject();
            Set<String> variants = new HashSet<>();
            states.getAsJsonObject("variants").entrySet().forEach(entry -> variants.add(entry.getKey()));
            assertEquals(Set.of("facing=north", "facing=east", "facing=south", "facing=west"), variants);
        }
    }
}

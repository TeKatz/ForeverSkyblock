package de.tekatz.foreverskyblock;

import java.nio.file.Files;
import java.nio.file.Path;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class BalancedRecipeResourcesTest {
    @Test void recipeEditorSnapshotCannotOverrideTheNewCostsWithStaleRecipes() throws Exception {
        String[] paths = {"forever_skyblock/recipe/tools/iron_pickaxe.json", "forever_skyblock/recipe/tools/iron_axe.json",
                "forever_skyblock/recipe/tools/iron_shovel.json", "forever_skyblock/recipe/tools/iron_hoe.json",
                "forever_skyblock/recipe/hopper.json", "minecraft/recipe/hopper.json",
                "forever_skyblock/recipe/condensing/logs.json", "forever_skyblock/recipe/condensing/packed_mud.json",
                "forever_skyblock/advancement/recipes/hopper.json"};
        for (String path : paths) try (var bundled = getClass().getResourceAsStream("/data/" + path)) {
            assertNotNull(bundled, path);
            assertArrayEquals(bundled.readAllBytes(), Files.readAllBytes(Path.of("../pack/config/forever_recipe_editor/generated_pack/data/" + path)),
                    "The distributed Recipe Editor pack must match the JAR: " + path);
        }
    }
}

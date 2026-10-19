package de.tekatz.foreverskyblock.client;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.awt.image.BufferedImage;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.HexFormat;
import javax.imageio.ImageIO;
import org.junit.jupiter.api.Test;

/** Locks the six approved K-menu header images against accidental substitution or regeneration. */
final class KMenuHeaderArtAssetsTest {
    private static final Path ROOT = Path.of("src/main/resources/assets/forever_skyblock/textures/gui/kmenu");

    @Test
    void approvedHeaderAssetsRemainByteExact() throws Exception {
        assertAsset("header_brand.png", 150, 63,
                "9860f553d4a6bf69ef850c1924a5274c5b78bc62dcd99faeb4b212244180cc5c");
        assertAsset("header_skill_points.png", 24, 24,
                "d559c36ea2d0537fd95811a17cbd8fce97be56483e89810de2e0023b9a7fdcaa");
        assertAsset("header_perk_points.png", 24, 24,
                "cbeb4ad06b39b18f1b6586259019cbaea00d24b8c24818b7013c020b927fd914");
        assertAsset("header_shop_tier.png", 24, 24,
                "c24e65b33d73342214377e107ca1944eae54e9d03ce4e38f3dc5e171193a4fb1");
        assertAsset("header_playtime.png", 24, 24,
                "b801422a2f2a4901be464db551b887e1eda82063582685fd04a5f18796f26ed6");
        assertAsset("header_island_level.png", 24, 24,
                "f0beb871a7eb9225069067b045e7186890e184813f35d3efb06a050ee3afec15");
    }

    private static void assertAsset(String file, int width, int height, String expectedSha256)
            throws IOException, NoSuchAlgorithmException {
        Path path = ROOT.resolve(file);
        byte[] bytes = Files.readAllBytes(path);
        BufferedImage image = ImageIO.read(path.toFile());
        assertEquals(width, image.getWidth(), file + " width");
        assertEquals(height, image.getHeight(), file + " height");
        assertEquals(expectedSha256, sha256(bytes), file + " SHA-256");
    }

    private static String sha256(byte[] bytes) throws NoSuchAlgorithmException {
        return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(bytes));
    }
}

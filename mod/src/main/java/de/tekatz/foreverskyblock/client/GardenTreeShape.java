package de.tekatz.foreverskyblock.client;

import java.util.ArrayList;
import java.util.List;

/** A compact oak crown made of volumetric foliage cells, rather than stretched leaf sheets. */
public final class GardenTreeShape {
    public record Part(boolean leaves, float x0, float y0, float z0, float x1, float y1, float z1, int faces) {
        private Part(boolean leaves, float x0, float y0, float z0, float x1, float y1, float z1) {
            this(leaves, x0, y0, z0, x1, y1, z1, 63);
        }
    }
    public static final List<Part> PARTS = create();
    private GardenTreeShape() { }

    private static List<Part> create() {
        var parts = new ArrayList<Part>();
        parts.add(new Part(false, -.6F, 0, -.6F, .6F, 3.6F, .6F));
        parts.add(new Part(false, -.4F, 3.6F, -.4F, .4F, 6.3F, .4F));
        // Branches terminate inside the crown; none are bare pegs below it.
        parts.add(new Part(false, -2.1F, 4.1F, -.25F, -.4F, 4.55F, .25F));
        parts.add(new Part(false, .4F, 5.25F, -.25F, 2.1F, 5.7F, .25F));
        for (int layer = 0; layer < 2; layer++) {
            for (int x = 0; x < 4; x++) for (int z = 0; z < 4; z++) {
                if ((x == 0 || x == 3) && (z == 0 || z == 3)) continue;
                // One recessed lower lobe avoids a perfectly cylindrical silhouette.
                if (layer == 0 && x == 3 && z == 1) continue;
                float y = 3.4F + layer * 2;
                parts.add(new Part(true, -4 + x * 2, y, -4 + z * 2, -2 + x * 2, y + 2, -2 + z * 2));
            }
        }
        for (int x = 0; x < 2; x++) for (int z = 0; z < 2; z++)
            parts.add(new Part(true, -2 + x * 2, 7.4F, -2 + z * 2, x * 2, 9, z * 2));
        var exposed = new ArrayList<Part>();
        for (var a : parts) {
            int faces = 63;
            if (a.leaves()) for (var b : parts) {
                if (a == b || !b.leaves()) continue;
                if (a.x0() >= b.x0() && a.x1() <= b.x1() && a.y0() >= b.y0() && a.y1() <= b.y1()) {
                    if (a.z0() == b.z1()) faces &= ~1;
                    if (a.z1() == b.z0()) faces &= ~2;
                }
                if (a.z0() >= b.z0() && a.z1() <= b.z1() && a.y0() >= b.y0() && a.y1() <= b.y1()) {
                    if (a.x0() == b.x1()) faces &= ~4;
                    if (a.x1() == b.x0()) faces &= ~8;
                }
                if (a.x0() >= b.x0() && a.x1() <= b.x1() && a.z0() >= b.z0() && a.z1() <= b.z1()) {
                    if (Math.abs(a.y1() - b.y0()) < .0001F) faces &= ~16;
                    if (Math.abs(a.y0() - b.y1()) < .0001F) faces &= ~32;
                }
            }
            exposed.add(new Part(a.leaves(), a.x0(), a.y0(), a.z0(), a.x1(), a.y1(), a.z1(), faces));
        }
        return List.copyOf(exposed);
    }
}

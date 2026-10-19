package de.tekatz.foreverskyblock.progression;

import java.util.List;

/** Server-owned world-shop definitions. Clients never supply prices, tiers or package contents. */
public final class ShopCatalog {
    public enum Kind { DEVELOPMENT, SUPPLY_PACKAGE, WORLD_UPGRADE }

    public static final String TEST_WORLD_ENTITLEMENT = "forever_skyblock:phase3_test_entitlement";
    public static final Product TEST = new Product(TEST_WORLD_ENTITLEMENT,
            "Phase 3 Test Entitlement", "Phase-3-Testberechtigung",
            "Development-only transaction proof with no item delivery.",
            "Nur für Entwicklung: Transaktionsnachweis ohne Item-Auslieferung.",
            1, 5, 0, Kind.DEVELOPMENT, List.of());

    public static final Product ADVANCED_AE2_STARTER = new Product(
            "forever_skyblock:shop/advanced_ae2_starter",
            "Advanced AE2 Starter Package", "Erweitertes AE2-Starterpaket",
            "Autocrafting infrastructure for the current world. One purchase per player and world.",
            "Autocrafting-Infrastruktur für die aktuelle Welt. Ein Kauf pro Spieler und Welt.",
            1, 12, 0, Kind.SUPPLY_PACKAGE, List.of(
                    new DeliveryEntry("ae2:drive", 1),
                    new DeliveryEntry("ae2:pattern_provider", 2),
                    new DeliveryEntry("ae2:molecular_assembler", 2),
                    new DeliveryEntry("ae2:blank_pattern", 16),
                    new DeliveryEntry("ae2:fluix_smart_cable", 16),
                    new DeliveryEntry("ae2:dense_energy_cell", 1)));

    public static final Product NO_PHANTOMS = new Product(
            "forever_skyblock:world_upgrade/no_phantoms",
            "No Phantoms", "Keine Phantome",
            "Permanently prevents Phantoms from spawning in this world.",
            "Verhindert dauerhaft das Spawnen von Phantomen in dieser Welt.",
            0, 1, 0, Kind.WORLD_UPGRADE, List.of());
    public static final Product NO_RAIN = new Product(
            "forever_skyblock:world_upgrade/no_rain",
            "No More Rain", "Kein Regen mehr",
            "Permanently disables rain and thunderstorms in this world.",
            "Deaktiviert Regen und Gewitter dauerhaft in dieser Welt.",
            0, 2, 0, Kind.WORLD_UPGRADE, List.of());
    public static final Product PERMANENT_DAY = new Product(
            "forever_skyblock:world_upgrade/permanent_day",
            "Permanent Day", "Dauerhafter Tag",
            "Permanently freezes this world at daytime.",
            "Friert diese Welt dauerhaft auf Tageszeit ein.",
            0, 3, 0, Kind.WORLD_UPGRADE, List.of());

    public static final List<Product> PRODUCTS = List.of(TEST, ADVANCED_AE2_STARTER, NO_PHANTOMS, NO_RAIN, PERMANENT_DAY);

    private ShopCatalog() { }

    public static List<Product> all() { return de.tekatz.foreverskyblock.editor.EditorRuntime.products(); }

    public static Product find(String id) {
        for (var product : all()) if (product.id().equals(id)) return product;
        return null;
    }

    public record DeliveryEntry(String itemId, int count) {
        public DeliveryEntry {
            ProgressionProfile.validateId(itemId);
            if (count < 1 || count > 64) throw new IllegalArgumentException("Invalid delivery stack size");
        }
    }

    public record Product(String id, String titleEn, String titleDe, String descriptionEn, String descriptionDe,
                          int requiredTier, int spCost, int ppCost, Kind kind, List<DeliveryEntry> contents) {
        public Product {
            ProgressionProfile.validateId(id);
            if (titleEn == null || titleEn.isBlank() || titleDe == null || titleDe.isBlank()
                    || descriptionEn == null || descriptionEn.isBlank() || descriptionDe == null || descriptionDe.isBlank()
                    || kind == null)
                throw new IllegalArgumentException("Missing shop product data");
            if (requiredTier < 0 || requiredTier > 4 || spCost < 1 || spCost > ProgressionProfile.MAX_POINTS
                    || ppCost < 0 || ppCost > ProgressionProfile.MAX_POINTS)
                throw new IllegalArgumentException("Invalid shop product");
            contents = List.copyOf(contents);
            if (kind == Kind.SUPPLY_PACKAGE && contents.isEmpty()) throw new IllegalArgumentException("Supply packages require delivery contents");
            if (kind == Kind.WORLD_UPGRADE && !contents.isEmpty()) throw new IllegalArgumentException("World upgrades cannot deliver item contents");
            if (contents.size() > 16) throw new IllegalArgumentException("Shop package too large");
        }
        public boolean developmentOnly() { return kind == Kind.DEVELOPMENT; }
        public boolean deliverable() { return kind == Kind.SUPPLY_PACKAGE; }
        public boolean worldUpgrade() { return kind == Kind.WORLD_UPGRADE; }
    }
}

package de.tekatz.foreverskyblock.progression;

import java.util.List;

/** Server-owned world-shop definitions. Clients never supply prices, tiers or package contents. */
public final class ShopCatalog {
    public static final String TEST_WORLD_ENTITLEMENT = "forever_skyblock:phase3_test_entitlement";
    public static final Product TEST = new Product(TEST_WORLD_ENTITLEMENT,
            "Phase 3 Test Entitlement", "Phase-3-Testberechtigung",
            "Development-only transaction proof with no item delivery.",
            "Nur fuer Entwicklung: Transaktionsnachweis ohne Item-Auslieferung.",
            1, 5, 0, true, List.of());

    /**
     * Phase 6 intentionally starts with one bounded real package. It accelerates AE2 automation
     * infrastructure without handing out the storage cells, 64k crafting storage or wireless
     * terminal used by the personal challenge catalog.
     */
    public static final Product ADVANCED_AE2_STARTER = new Product(
            "forever_skyblock:shop/advanced_ae2_starter",
            "Advanced AE2 Starter Package", "Erweitertes AE2-Starterpaket",
            "Autocrafting infrastructure for the current world. One purchase per player and world.",
            "Autocrafting-Infrastruktur fuer die aktuelle Welt. Ein Kauf pro Spieler und Welt.",
            1, 12, 0, false, List.of(
                    new DeliveryEntry("ae2:drive", 1),
                    new DeliveryEntry("ae2:pattern_provider", 2),
                    new DeliveryEntry("ae2:molecular_assembler", 2),
                    new DeliveryEntry("ae2:blank_pattern", 16),
                    new DeliveryEntry("ae2:fluix_smart_cable", 16),
                    new DeliveryEntry("ae2:dense_energy_cell", 1)));

    public static final List<Product> PRODUCTS = List.of(TEST, ADVANCED_AE2_STARTER);

    private ShopCatalog() { }

    public static Product find(String id) {
        for (var product : PRODUCTS) if (product.id().equals(id)) return product;
        return null;
    }

    public record DeliveryEntry(String itemId, int count) {
        public DeliveryEntry {
            ProgressionProfile.validateId(itemId);
            if (count < 1 || count > 64) throw new IllegalArgumentException("Invalid delivery stack size");
        }
    }

    public record Product(String id, String titleEn, String titleDe, String descriptionEn, String descriptionDe,
                          int requiredTier, int spCost, int ppCost, boolean developmentOnly,
                          List<DeliveryEntry> contents) {
        public Product {
            ProgressionProfile.validateId(id);
            if (titleEn == null || titleEn.isBlank() || titleDe == null || titleDe.isBlank()
                    || descriptionEn == null || descriptionEn.isBlank() || descriptionDe == null || descriptionDe.isBlank())
                throw new IllegalArgumentException("Missing shop product text");
            if (requiredTier < 0 || requiredTier > 4 || spCost < 1 || spCost > ProgressionProfile.MAX_POINTS
                    || ppCost < 0 || ppCost > ProgressionProfile.MAX_POINTS)
                throw new IllegalArgumentException("Invalid shop product");
            contents = List.copyOf(contents);
            if (!developmentOnly && contents.isEmpty()) throw new IllegalArgumentException("Real shop products require delivery contents");
            if (contents.size() > 16) throw new IllegalArgumentException("Shop package too large");
        }
        public boolean deliverable() { return !contents.isEmpty(); }
    }
}

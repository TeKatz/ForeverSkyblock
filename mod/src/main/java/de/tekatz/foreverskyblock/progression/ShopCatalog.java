package de.tekatz.foreverskyblock.progression;

import java.util.List;

/** Phase-3 infrastructure products only. Valuable package contents remain a separate design gate. */
public final class ShopCatalog {
    public static final String TEST_WORLD_ENTITLEMENT = "forever_skyblock:phase3_test_entitlement";
    public static final Product TEST = new Product(TEST_WORLD_ENTITLEMENT, 1, 5, true);
    public static final List<Product> PRODUCTS = List.of(TEST);

    private ShopCatalog() { }

    public static Product find(String id) {
        for (var product : PRODUCTS) if (product.id().equals(id)) return product;
        return null;
    }

    public record Product(String id, int requiredTier, int spCost, boolean developmentOnly) {
        public Product {
            ProgressionProfile.validateId(id);
            if (requiredTier < 0 || requiredTier > 4 || spCost < 1 || spCost > ProgressionProfile.MAX_POINTS)
                throw new IllegalArgumentException("Invalid shop product");
        }
    }
}

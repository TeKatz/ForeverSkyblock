package de.tekatz.foreverskyblock.item;

import net.minecraft.network.chat.Component;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;

/** Small MK II-only machine upgrades. Capacity cost and install limits are enforced by the Condenser. */
public final class CondenserUpgradeCardItem extends Item {
    public enum Kind {
        SPEED("Processing Speed Card", 1, 3),
        GARDEN("Garden Accelerator Card", 1, 2),
        BUFFER("Buffer Expansion Card", 1, 2),
        BATCH("Batch Processing Card", 2, 1);

        private final String displayName;
        private final int capacityCost;
        private final int maxInstalled;
        Kind(String displayName, int capacityCost, int maxInstalled) {
            this.displayName = displayName;
            this.capacityCost = capacityCost;
            this.maxInstalled = maxInstalled;
        }
        public String displayName() { return displayName; }
        public int capacityCost() { return capacityCost; }
        public int maxInstalled() { return maxInstalled; }
    }

    private final Kind kind;
    public CondenserUpgradeCardItem(Kind kind, Properties properties) {
        super(properties.stacksTo(1));
        this.kind = kind;
    }
    public Kind kind() { return kind; }
    @Override public Component getName(ItemStack stack) { return Component.literal(kind.displayName()); }
}

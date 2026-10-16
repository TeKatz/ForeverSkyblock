package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

/**
 * Client-only icon lookup for the Design-2 progression menu.
 * Optional integration items are resolved from the runtime registry so the base mod keeps no hard
 * class dependency on AE2, Mekanism, Powah or Mystical Agriculture.
 */
public final class PremiumIconResolver {
    private PremiumIconResolver() { }

    public static ItemStack challengeIcon(MilestoneCatalog.Milestone milestone) {
        ItemStack fallback = milestone.icon().getDefaultInstance();
        return switch (milestone.id()) {
            case "forever_skyblock:beyond_starter_cell" -> item("ae2:item_storage_cell_4k", fallback);
            case "forever_skyblock:autocrafting_online" -> item("ae2:crafting_unit", fallback);
            case "forever_skyblock:crafting_brain" -> item("ae2:64k_crafting_storage", fallback);
            case "forever_skyblock:wireless_operator" -> item("ae2:wireless_crafting_terminal", fallback);
            case "forever_skyblock:storage_architect" -> item("ae2:item_storage_cell_256k", fallback);
            case "forever_skyblock:mekanized" -> item("mekanism:metallurgic_infuser", fallback);
            case "forever_skyblock:digital_extraction" -> item("mekanism:digital_miner", fallback);
            case "forever_skyblock:energized" -> item("powah:energizing_orb", fallback);
            case "forever_skyblock:supremium" -> item("mysticalagriculture:supremium_essence", fallback);
            default -> fallback;
        };
    }

    public static ItemStack categoryIcon(String category) {
        return switch (category) {
            case "forever" -> ForeverSkyblock.STONE_FRAGMENT.get().getDefaultInstance();
            case "ae2" -> item("ae2:fluix_crystal", Items.AMETHYST_SHARD.getDefaultInstance());
            case "mekanism" -> item("mekanism:basic_control_circuit", Items.IRON_BLOCK.getDefaultInstance());
            case "powah" -> item("powah:energizing_orb", Items.GLOWSTONE_DUST.getDefaultInstance());
            case "mystical_agriculture" -> item("mysticalagriculture:inferium_essence", Items.WHEAT_SEEDS.getDefaultInstance());
            case "combat" -> Items.DIAMOND_SWORD.getDefaultInstance();
            case "contracts" -> Items.WRITABLE_BOOK.getDefaultInstance();
            default -> Items.NETHER_STAR.getDefaultInstance();
        };
    }

    private static ItemStack item(String id, ItemStack fallback) {
        ResourceLocation location = ResourceLocation.parse(id);
        return BuiltInRegistries.ITEM.getOptional(location)
                .map(item -> item.getDefaultInstance())
                .filter(stack -> !stack.isEmpty())
                .orElse(fallback);
    }
}

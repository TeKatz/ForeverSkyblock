package de.tekatz.foreverskyblock.progression;

import java.util.List;

/** Stable Phase-5 repeatable contract definitions. Exact resource quantities remain test values until playtested. */
public final class ContractCatalog {
    public enum Trigger { DRAGON, WITHER, RESOURCE }

    public record Contract(String id, Trigger trigger, int requiredTier, int target, int rewardSp,
                           String itemId, String iconId, String titleEn, String titleDe,
                           String descriptionEn, String descriptionDe) {
        public Contract {
            ProgressionProfile.validateId(id);
            if (requiredTier < 0 || requiredTier > 4 || target < 1 || target > 1_000_000
                    || rewardSp < 1 || rewardSp > 10) throw new IllegalArgumentException("Invalid contract definition");
            if (trigger == Trigger.RESOURCE) ProgressionProfile.validateId(itemId);
            else if (itemId != null) throw new IllegalArgumentException("Action contract cannot consume an item");
            ProgressionProfile.validateId(iconId);
        }
        public boolean resource() { return trigger == Trigger.RESOURCE; }
    }

    public static final Contract DRAGON_REBORN = action("dragon_reborn", Trigger.DRAGON, 1, 1, 1,
            "minecraft:dragon_head", "Dragon Reborn", "Drache Wiedergeboren",
            "Defeat another respawned Ender Dragon after Dragon Slayer.",
            "Besiege nach Dragon Slayer einen weiteren wiederbeschworenen Enderdrachen.");
    public static final Contract WITHER_HUNTER = action("wither_hunter", Trigger.WITHER, 1, 3, 1,
            "minecraft:wither_skeleton_skull", "Wither Hunter", "Wither-Jaeger",
            "Defeat three additional Withers. The counter resets after each payout.",
            "Besiege drei weitere Wither. Nach jeder Auszahlung beginnt der Zaehler erneut.");
    public static final Contract SINGULARITY = resource("singularity", 1, 4, 1, "ae2:singularity",
            "Singularity Contract", "Singularitaetsvertrag",
            "Submit and consume four AE2 Singularities.", "Gib vier AE2-Singularitaeten ab; sie werden verbraucht.");
    public static final Contract NITRO = resource("nitro", 1, 32, 1, "powah:crystal_nitro",
            "Nitro Contract", "Nitro-Vertrag",
            "Submit and consume 32 Nitro Crystals. Quantity is provisional for Phase 5 testing.",
            "Gib 32 Nitro-Kristalle ab. Die Menge ist fuer den Phase-5-Test vorlaeufig.");
    public static final Contract ANTIMATTER = resource("antimatter", 1, 4, 3, "mekanism:pellet_antimatter",
            "Antimatter Contract", "Antimaterie-Vertrag",
            "Submit and consume four Antimatter Pellets for an endgame payout.",
            "Gib vier Antimaterie-Pellets fuer eine Endgame-Auszahlung ab.");

    public static final List<Contract> CONTRACTS = List.of(DRAGON_REBORN, WITHER_HUNTER, SINGULARITY, NITRO, ANTIMATTER);

    private ContractCatalog() { }

    private static Contract action(String path, Trigger trigger, int tier, int target, int reward, String icon,
                                   String en, String de, String descriptionEn, String descriptionDe) {
        return new Contract("forever_skyblock:contract/" + path, trigger, tier, target, reward, null, icon,
                en, de, descriptionEn, descriptionDe);
    }
    private static Contract resource(String path, int tier, int target, int reward, String item,
                                     String en, String de, String descriptionEn, String descriptionDe) {
        return new Contract("forever_skyblock:contract/" + path, Trigger.RESOURCE, tier, target, reward, item, item,
                en, de, descriptionEn, descriptionDe);
    }
    public static Contract find(String id) {
        for (var contract : CONTRACTS) if (contract.id().equals(id)) return contract;
        return null;
    }
}

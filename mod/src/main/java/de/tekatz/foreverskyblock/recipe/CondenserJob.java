package de.tekatz.foreverskyblock.recipe;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.SaplingBlock;

/** Presentation only: classify the resolved recipe, never infer a job from an attached module. */
public enum CondenserJob {
    IDLE, CRUSHING, MILLING, COMPOSTING, MIXING, CULTIVATING;

    public static CondenserJob of(CondenserRecipe recipe) {
        if (recipe == null) return IDLE;
        var result = recipe.result();
        if (recipe.requiresModule()) {
            if (result.getItem() instanceof BlockItem block && block.getBlock() instanceof SaplingBlock)
                return CULTIVATING;
            return MIXING;
        }
        if (result.is(ForeverSkyblock.STONE_MEAL.get())) return MILLING;
        if (result.is(Items.BONE_MEAL)) return COMPOSTING;
        return CRUSHING;
    }

    public static CondenserJob fromId(int id) {
        return id >= 0 && id < values().length ? values()[id] : IDLE;
    }
}

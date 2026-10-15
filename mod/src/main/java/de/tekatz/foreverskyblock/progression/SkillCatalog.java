package de.tekatz.foreverskyblock.progression;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import java.util.List;
import net.minecraft.core.Holder;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.ai.attributes.Attribute;
import net.minecraft.world.entity.ai.attributes.AttributeModifier;
import net.minecraft.world.entity.ai.attributes.Attributes;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.Items;

/** Add definitions here: the menu, purchasing and effect application use the same catalog. */
public final class SkillCatalog {
    public record Skill(String id, int cost, int maxRank, Item icon, Holder<Attribute> attribute, double bonusPerRank) {
        public String titleKey() { return "skill." + id.replace(':', '.'); }
        public String descriptionKey() { return titleKey() + ".description"; }
        public void apply(ServerPlayer player, int rank) {
            var instance = player.getAttribute(attribute);
            if (instance == null) return;
            ResourceLocation modifier = ResourceLocation.parse(id);
            instance.removeModifier(modifier);
            if (rank > 0) instance.addTransientModifier(new AttributeModifier(modifier,
                    bonusPerRank * Math.min(rank, maxRank), AttributeModifier.Operation.ADD_VALUE));
        }
    }
    public static final String EXTENDED_REACH = ForeverSkyblock.MOD_ID + ":extended_reach";
    public static final List<Skill> SKILLS = List.of(new Skill(EXTENDED_REACH, 1, 1, Items.IRON_PICKAXE,
            Attributes.BLOCK_INTERACTION_RANGE, 1.0));
    private SkillCatalog() { }
    public static Skill find(String id) { return SKILLS.stream().filter(skill -> skill.id().equals(id)).findFirst().orElse(null); }
    public static ProgressionProfile reset(ProgressionProfile profile) {
        var costs = new java.util.LinkedHashMap<String, Integer>();
        for (var skill : SKILLS) costs.put(skill.id(), skill.cost());
        return profile.resetSkills(costs);
    }
    public static void apply(ServerPlayer player, ProgressionProfile profile) {
        for (Skill skill : SKILLS) skill.apply(player, profile.rank(skill.id()));
    }
}

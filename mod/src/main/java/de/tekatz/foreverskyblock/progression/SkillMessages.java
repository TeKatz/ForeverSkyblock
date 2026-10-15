package de.tekatz.foreverskyblock.progression;

import net.minecraft.ChatFormatting;
import net.minecraft.network.chat.ClickEvent;
import net.minecraft.network.chat.Component;
import net.minecraft.network.chat.HoverEvent;

/** Only the earned-point component is highlighted and linked to the read-only menu command. */
public final class SkillMessages {
    private SkillMessages() { }
    public static Component milestone(MilestoneCatalog.Milestone milestone) {
        Component points = Component.translatable("message.forever_skyblock.skill_points", milestone.reward())
                .withStyle(style -> style.withColor(ChatFormatting.GREEN).withBold(true).withUnderlined(true)
                        .withClickEvent(new ClickEvent(ClickEvent.Action.RUN_COMMAND, "/foreverskills"))
                        .withHoverEvent(new HoverEvent(HoverEvent.Action.SHOW_TEXT,
                                Component.translatable("message.forever_skyblock.open_skills"))));
        return Component.translatable("message.forever_skyblock.milestone", Component.translatable(milestone.titleKey()), points);
    }
}

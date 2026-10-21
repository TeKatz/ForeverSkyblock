package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.ApotheosisClientState;
import de.tekatz.foreverskyblock.client.PremiumMenuLayout;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import de.tekatz.foreverskyblock.progression.ApotheosisStage;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(PremiumSkillScreen.class)
public abstract class PremiumSkillScreenApotheosisMixin extends Screen {
    @Shadow private int x;
    @Shadow private int y;
    @Shadow private PremiumMenuLayout layout;

    protected PremiumSkillScreenApotheosisMixin(Component title) { super(title); }

    @Inject(method = "drawHeader", at = @At("TAIL"))
    private void forever$drawApotheosisStatus(GuiGraphics graphics, CallbackInfo ci) {
        ApotheosisStage stage = ApotheosisClientState.stage();
        Component tier = stage == ApotheosisStage.DORMANT
                ? Component.translatable("gui.forever_skyblock.meta.tier.-1")
                : Component.translatable("text.apotheosis.world_tier." + stage.id());
        Component line = Component.literal("Apotheosis · ").append(tier);
        if (stage == ApotheosisStage.PINNACLE) line = line.copy().append("  ✓");
        else line = line.copy().append("  →  ").append(forever$nextRequirement(stage));
        int left = x + layout.padding() + 2;
        int top = y + layout.headerHeight() - 9;
        int maxWidth = Math.max(40, layout.width() - layout.padding() * 2 - 4);
        String text = line.getString();
        if (font.width(text) > maxWidth) text = font.plainSubstrByWidth(text, maxWidth - font.width("…")) + "…";
        graphics.drawString(font, text, left, top, 0xFF9BC8D8, false);
    }

    private static Component forever$nextRequirement(ApotheosisStage stage) {
        return switch (stage) {
            case DORMANT -> Component.translatable("milestone.forever_skyblock.into_nether");
            case HAVEN -> Component.literal("🔥 ").append(Component.translatable("item.minecraft.diamond"));
            case FRONTIER -> Component.translatable("milestone.forever_skyblock.withered_away");
            case ASCENT -> Component.translatable("milestone.forever_skyblock.dragon_slayer");
            case SUMMIT -> Component.translatable("gui.forever_skyblock.machine.forever_core");
            case PINNACLE -> Component.literal("✓");
        };
    }
}

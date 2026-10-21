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
        Component tier = Component.translatable("apotheosis.forever_stage." + stage.id());
        Component next = Component.translatable("gui.forever_skyblock.apotheosis.next." + stage.id());
        Component line = Component.translatable("gui.forever_skyblock.apotheosis.status", tier, next);
        int left = x + layout.padding() + 2;
        int top = y + layout.headerHeight() - 9;
        int maxWidth = Math.max(40, layout.width() - layout.padding() * 2 - 4);
        String text = line.getString();
        if (font.width(text) > maxWidth) text = font.plainSubstrByWidth(text, maxWidth - font.width("…")) + "…";
        graphics.drawString(font, text, left, top, 0xFF9BC8D8, false);
    }
}
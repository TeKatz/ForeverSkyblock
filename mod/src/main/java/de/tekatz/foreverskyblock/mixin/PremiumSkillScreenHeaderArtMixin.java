package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.KMenuHeaderArt;
import de.tekatz.foreverskyblock.client.PremiumMenuLayout;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import net.minecraft.client.gui.GuiGraphics;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Replaces only the K-menu header painter; page content and input remain owned by PremiumSkillScreen. */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumSkillScreenHeaderArtMixin {
    @Shadow private int x;
    @Shadow private int y;
    @Shadow private PremiumMenuLayout layout;

    @Inject(method = "drawHeader", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$renderApprovedHeaderArt(GuiGraphics graphics, CallbackInfo ci) {
        KMenuHeaderArt.render(graphics, x, y, layout);
        ci.cancel();
    }
}

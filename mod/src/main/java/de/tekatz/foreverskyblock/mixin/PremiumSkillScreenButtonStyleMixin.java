package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.EditorStyleBridge;
import de.tekatz.foreverskyblock.editor.EditorDocument;
import net.minecraft.client.gui.GuiGraphics;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Adds Studio effect/shadow styling around native navigation and standard menu buttons without double-scaling text. */
@Mixin(targets="de.tekatz.foreverskyblock.client.PremiumSkillScreen$MenuButton")
public abstract class PremiumSkillScreenButtonStyleMixin {
    @Shadow private String elementId;
    @Unique private EditorDocument.Style foreverSkyblock$previousStyle;

    @Inject(method="renderWidget",at=@At("HEAD"))
    private void foreverSkyblock$beginEffects(GuiGraphics g,int mx,int my,float dt,CallbackInfo ci){
        if(elementId!=null&&!elementId.isEmpty())foreverSkyblock$previousStyle=EditorStyleBridge.pushEffects(elementId);
    }
    @Inject(method="renderWidget",at=@At("RETURN"))
    private void foreverSkyblock$endEffects(GuiGraphics g,int mx,int my,float dt,CallbackInfo ci){
        if(foreverSkyblock$previousStyle!=null){EditorStyleBridge.restore(foreverSkyblock$previousStyle);foreverSkyblock$previousStyle=null;}
    }
}

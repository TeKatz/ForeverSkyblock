package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.KMenuEditorScreen;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Shop roots/prerequisite geometry are not Product records; expose only their node geometry inspector. */
@Mixin(value=KMenuEditorScreen.class,priority=1100)
public abstract class KMenuEditorShopNodeMixin {
    @Shadow private String selected;
    @Inject(method="section",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$nodeOnly(String id,String english,String german,CallbackInfoReturnable<Boolean> cir){
        if("shop.node".equals(id)&&selected!=null&&selected.startsWith("shop:"))cir.setReturnValue(true);
    }
}

package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.EditorClient;
import de.tekatz.foreverskyblock.client.PremiumMenuLayout;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import java.util.List;
import net.minecraft.client.gui.components.AbstractWidget;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Overview duplicated Profile and is intentionally removed from the player-facing primary navigation. */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumSkillScreenOverviewMixin {
    @Shadow private int x;
    @Shadow private int y;
    @Shadow private PremiumMenuLayout layout;

    @Inject(method="init",at=@At("TAIL"))
    private void foreverSkyblock$removeOverview(CallbackInfo ci){
        var screen=(PremiumSkillScreen)(Object)this;
        var widgets=screen.children().stream().filter(c->c instanceof AbstractWidget).map(c->(AbstractWidget)c).toList();
        if(widgets.size()<6)return;
        // PremiumSkillScreen adds the six legacy page buttons first, before Close/editor controls.
        var overview=widgets.getFirst();overview.visible=false;overview.active=false;
        List<String> ids=List.of("challenges","shop","research","perks","profile");
        int left=x+layout.padding(),top=y+layout.navTop(),gap=1,bw=(layout.width()-2*layout.padding()-gap*(ids.size()-1))/ids.size();
        for(int i=0;i<ids.size();i++){
            var button=widgets.get(i+1);var r=EditorClient.rect("nav."+ids.get(i),left+i*(bw+gap),top,bw,layout.navHeight());
            button.setX(r.x());button.setY(r.y());button.setWidth(r.width());button.setHeight(r.height());
        }
    }
}

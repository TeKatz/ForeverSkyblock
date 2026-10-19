package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.*;
import java.io.IOException;
import net.minecraft.server.level.ServerPlayer;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;

/** Operator-only helpers used by registered server commands for deterministic pack testing. */
@Mixin(ProgressionService.class)
public abstract class ProgressionServiceAdminMixin implements ProgressionAdminAccess {
    @Shadow @Final private ProfileStore store;

    @Override public boolean adminHardReset(ServerPlayer player){
        try{
            var next=store.update(player.getUUID(),old->ProgressionProfile.empty());
            SkillCatalog.apply(player,next);((ProgressionService)(Object)this).open(player);return true;
        }catch(IOException|RuntimeException e){return false;}
    }

    @Override public boolean adminGrantPerks(ServerPlayer player,int amount){
        if(amount<1||amount>1000)return false;
        try{
            store.update(player.getUUID(),state->new ProgressionProfile(state.points(),state.milestones(),state.skills(),
                    Math.addExact(state.perkPoints(),amount),state.perkInvestments(),state.redeemedCores(),
                    state.meta().account(0,amount,0,0,0,0,0)));
            ((ProgressionService)(Object)this).open(player);return true;
        }catch(IOException|RuntimeException e){return false;}
    }

    @Override public boolean adminTierUp(ServerPlayer player){
        try{
            var current=store.load(player.getUUID()).meta().shopTier();return adminSetTier(player,Math.min(4,current+1));
        }catch(IOException|RuntimeException e){return false;}
    }

    @Override public boolean adminSetTier(ServerPlayer player,int tier){
        if(tier<0||tier>4)return false;
        try{
            store.update(player.getUUID(),state->{var m=state.meta();var next=new MetaProgress(tier,m.progress(),m.accounting(),m.worldClaims(),m.purchaseReceipts(),m.contracts(),m.pendingContracts(),m.deliveries());return state.withMeta(next);});
            ((ProgressionService)(Object)this).open(player);return true;
        }catch(IOException|RuntimeException e){return false;}
    }
}

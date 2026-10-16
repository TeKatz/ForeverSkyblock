package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.progression.ChallengeEvents;
import net.minecraft.advancements.CriteriaTriggers;
import net.minecraft.advancements.critereon.KilledTrigger;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.entity.Entity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(KilledTrigger.class)
public abstract class PersonalKillMixin {
    @Inject(method = "trigger", at = @At("TAIL"))
    private void foreverSkyblock$personalKill(ServerPlayer player, Entity victim, DamageSource source, CallbackInfo callback) {
        if ((Object) this == CriteriaTriggers.PLAYER_KILLED_ENTITY) ChallengeEvents.killed(player, victim);
    }
}

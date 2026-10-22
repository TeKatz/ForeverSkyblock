package de.tekatz.foreverskyblock.blockentity;

import net.minecraft.nbt.CompoundTag;
import net.minecraft.world.level.block.entity.BlockEntity;

/** Transient observer only: never persisted, never consulted by recipes, inventories or energy handlers. */
public final class MachineVisualState {
    private int status, progress, duration=1, amount;
    private long receivedAt, sentAt=Long.MIN_VALUE;
    private boolean initialized;
    public int status() { return status; }
    public int amount() { return amount; }
    public float progress(long now,float partial) {
        float elapsed=status==1?Math.clamp(now-receivedAt+partial,0F,20F):0F;
        return Math.clamp(progress+elapsed,0F,duration);
    }
    public float phase(long now,float partial) { return progress(now,partial)/duration; }
    public void observe(BlockEntity owner,int status,int progress,int duration,int amount) {
        var level=owner.getLevel();
        if(level==null||level.isClientSide)return;
        long now=level.getGameTime();
        boolean send=!initialized||this.status!=status||progress<this.progress
                ||this.amount!=amount||status==1&&now-sentAt>=20;
        this.status=status;this.progress=progress;this.duration=Math.max(1,duration);this.amount=amount;
        if(send) {
            initialized=true;sentAt=now;
            level.sendBlockUpdated(owner.getBlockPos(),owner.getBlockState(),owner.getBlockState(),2);
        }
    }
    public CompoundTag write() {
        CompoundTag tag=new CompoundTag();
        tag.putInt("Status",status);tag.putInt("Progress",progress);tag.putInt("Duration",duration);tag.putInt("Amount",amount);
        return tag;
    }
    public void read(CompoundTag tag,long now) {
        status=Math.clamp(tag.getInt("Status"),0,3);duration=Math.max(1,tag.getInt("Duration"));
        progress=Math.clamp(tag.getInt("Progress"),0,duration);amount=Math.clamp(tag.getInt("Amount"),0,20);receivedAt=now;
    }
}

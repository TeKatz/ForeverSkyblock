package de.tekatz.foreverskyblock.blockentity;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import org.jetbrains.annotations.Nullable;

/** Client-visible anchor used only for Garden Chamber presentation. The Condenser owns all real processing state. */
public final class GardenChamberBlockEntity extends BlockEntity {
    public GardenChamberBlockEntity(BlockPos pos, BlockState state) {
        super(ForeverSkyblock.GARDEN_CHAMBER_ENTITY.get(), pos, state);
    }

    public @Nullable CondenserBlockEntity linkedCondenser() {
        if (level == null) return null;
        CondenserBlockEntity fallback = null;
        for (Direction direction : Direction.values()) {
            if (!level.hasChunkAt(worldPosition.relative(direction))) continue;
            BlockEntity entity = level.getBlockEntity(worldPosition.relative(direction));
            if (entity instanceof CondenserBlockEntity condenser
                    && condenser.machineTier() >= 2
                    && condenser.hasGardenChamber()) {
                if (condenser.visualStatus() == CondenserBlockEntity.RUNNING) return condenser;
                if (fallback == null) fallback = condenser;
            }
        }
        return fallback;
    }

    public boolean running() {
        CondenserBlockEntity condenser = linkedCondenser();
        return condenser != null && condenser.visualStatus() == CondenserBlockEntity.RUNNING;
    }
}

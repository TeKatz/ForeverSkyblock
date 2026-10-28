package de.tekatz.foreverskyblock.studio.runtime;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.Registries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.EntityBlock;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.level.block.state.BlockBehaviour;
import net.minecraft.world.level.block.state.BlockState;
import net.neoforged.bus.api.IEventBus;
import net.neoforged.neoforge.registries.DeferredHolder;
import net.neoforged.neoforge.registries.DeferredRegister;
import org.jetbrains.annotations.Nullable;

/** Registered only for GameTests, so the runtime fixture has a valid block and block entity type. */
public final class ForeverStudioRuntimeGameTestFixture {
    public static final ResourceLocation MACHINE_ID =
            ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "studio_runtime_e2e");

    private static final DeferredRegister.Blocks BLOCKS = DeferredRegister.createBlocks(ForeverSkyblock.MOD_ID);
    private static final DeferredRegister<BlockEntityType<?>> BLOCK_ENTITIES =
            DeferredRegister.create(Registries.BLOCK_ENTITY_TYPE, ForeverSkyblock.MOD_ID);

    public static final DeferredHolder<Block, Block> MACHINE_BLOCK =
            BLOCKS.register("studio_runtime_e2e", () -> new FixtureBlock(BlockBehaviour.Properties.of()));
    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<ForeverStudioMachineBlockEntity>> MACHINE_BLOCK_ENTITY =
            BLOCK_ENTITIES.register("studio_runtime_e2e", () -> BlockEntityType.Builder.of(
                    ForeverStudioRuntimeGameTestFixture::createMachine, MACHINE_BLOCK.get()).build(null));

    private ForeverStudioRuntimeGameTestFixture() { }

    public static void register(IEventBus modBus) {
        if (!System.getProperty("neoforge.enabledGameTestNamespaces", "").contains(ForeverSkyblock.MOD_ID)) return;
        BLOCKS.register(modBus);
        BLOCK_ENTITIES.register(modBus);
    }

    private static ForeverStudioMachineBlockEntity createMachine(BlockPos pos, BlockState state) {
        return new ForeverStudioMachineBlockEntity(MACHINE_BLOCK_ENTITY.get(), pos, state, MACHINE_ID);
    }

    private static final class FixtureBlock extends Block implements EntityBlock {
        private FixtureBlock(Properties properties) { super(properties); }

        @Override public @Nullable BlockEntity newBlockEntity(BlockPos pos, BlockState state) {
            return createMachine(pos, state);
        }
    }
}

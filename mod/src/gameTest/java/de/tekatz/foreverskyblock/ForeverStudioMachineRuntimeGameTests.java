package de.tekatz.foreverskyblock.studio.runtime;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.BlockPos;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ForeverStudioMachineRuntimeGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static final ResourceLocation MACHINE_ID =
            ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "studio_runtime_e2e");

    // GameTest classes are discovered after vanilla registries have been frozen. Constructing a new
    // Block here would try to create a new registry-backed holder during static initialization and
    // crash discovery with "Registry is already frozen". The runtime test does not place its block
    // entity into the world, so an already-registered vanilla block is the correct inert carrier for
    // the isolated BlockEntityType and BlockState used by this test.
    private static final BlockEntityType<ForeverStudioMachineBlockEntity> TEST_BLOCK_ENTITY =
            BlockEntityType.Builder.<ForeverStudioMachineBlockEntity>of((pos, state) -> null, Blocks.STONE).build(null);

    private static void check(GameTestHelper helper, boolean condition, String message) {
        helper.assertTrue(condition, message);
    }

    private static ForeverStudioMachineBlockEntity machine(GameTestHelper helper) {
        var machine = new ForeverStudioMachineBlockEntity(
                TEST_BLOCK_ENTITY,
                helper.absolutePos(POS),
                Blocks.STONE.defaultBlockState(),
                MACHINE_ID);
        machine.setLevel(helper.getLevel());
        return machine;
    }

    private static void tick(GameTestHelper helper, ForeverStudioMachineBlockEntity machine, int count) {
        for (int i = 0; i < count; i++) {
            ForeverStudioMachineBlockEntity.tick(
                    helper.getLevel(),
                    machine.getBlockPos(),
                    machine.getBlockState(),
                    machine);
        }
    }

    @GameTest(template = "empty")
    public static void compilerContractProcessesAndPersists(GameTestHelper helper) {
        var machine = machine(helper);
        machine.inventory().insertItem(0, new ItemStack(Items.COBBLESTONE), false);

        tick(helper, machine, 45);
        check(helper, machine.menuData().get(0) == 45,
                "Studio runtime must make deterministic partial recipe progress before the save point");
        check(helper, machine.inventory().getStackInSlot(0).is(Items.COBBLESTONE),
                "Studio runtime must retain input until processing completes");
        check(helper, machine.inventory().getStackInSlot(1).isEmpty(),
                "Studio runtime must not create output before processing completes");

        var registries = helper.getLevel().registryAccess();
        var snapshot = new CompoundTag();
        machine.saveAdditional(snapshot, registries);
        int savedProgress = snapshot.getInt("Progress");
        check(helper, savedProgress == 45,
                "Studio runtime save data must contain the exact partial progress");

        var restored = machine(helper);
        restored.loadAdditional(snapshot, registries);
        check(helper, restored.menuData().get(0) == savedProgress,
                "Studio runtime must restore the exact saved progress");

        tick(helper, restored, 55);
        var input = restored.inventory().getStackInSlot(0);
        var output = restored.inventory().getStackInSlot(1);
        check(helper, input.isEmpty(),
                "Studio runtime must consume exactly one cobblestone after the remaining ticks");
        check(helper, output.is(Items.GRAVEL) && output.getCount() == 1,
                "Studio runtime must produce exactly one gravel from the compiled recipe contract");
        check(helper, restored.menuData().get(0) == 0,
                "Studio runtime must reset progress after recipe completion");
        helper.succeed();
    }
}

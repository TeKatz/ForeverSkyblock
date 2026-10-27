package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlock;
import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.state.BlockBehaviour;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ForeverStudioMachineRuntimeGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static final ResourceLocation MACHINE_ID =
            ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "studio_runtime_e2e");
    private static final ForeverStudioMachineBlock TEST_BLOCK = new ForeverStudioMachineBlock(
            MACHINE_ID,
            BlockBehaviour.Properties.of().strength(1.0F));

    private static void check(GameTestHelper helper, boolean condition, String message) {
        helper.assertTrue(condition, message);
    }

    private static ForeverStudioMachineBlockEntity machine(GameTestHelper helper) {
        helper.setBlock(POS, TEST_BLOCK);
        return (ForeverStudioMachineBlockEntity) helper.getBlockEntity(POS);
    }

    @GameTest(template = "empty", timeoutTicks = 180)
    public static void compilerContractProcessesAndPersists(GameTestHelper helper) {
        var machine = machine(helper);
        machine.inventory().insertItem(0, new ItemStack(Items.COBBLESTONE), false);

        helper.runAfterDelay(45, () -> {
            check(helper, machine.menuData().get(0) > 0 && machine.menuData().get(0) < 100,
                    "Studio runtime must make partial recipe progress before the save point");
            check(helper, machine.inventory().getStackInSlot(0).is(Items.COBBLESTONE),
                    "Studio runtime must retain input until processing completes");
            check(helper, machine.inventory().getStackInSlot(1).isEmpty(),
                    "Studio runtime must not create output before processing completes");

            var registries = helper.getLevel().registryAccess();
            var snapshot = machine.saveWithFullMetadata(registries);
            int savedProgress = snapshot.getInt("Progress");
            check(helper, savedProgress > 0 && savedProgress < 100,
                    "Studio runtime save data must contain partial progress");

            var restored = new ForeverStudioMachineBlockEntity(
                    helper.absolutePos(POS),
                    machine.getBlockState());
            restored.loadWithComponents(snapshot, registries);
            check(helper, restored.menuData().get(0) == savedProgress,
                    "Studio runtime must restore the exact saved progress");
            helper.getLevel().setBlockEntity(restored);
        });

        helper.runAfterDelay(115, () -> {
            var restored = (ForeverStudioMachineBlockEntity) helper.getBlockEntity(POS);
            var input = restored.inventory().getStackInSlot(0);
            var output = restored.inventory().getStackInSlot(1);
            check(helper, input.isEmpty(),
                    "Studio runtime must consume exactly one cobblestone after the remaining ticks");
            check(helper, output.is(Items.GRAVEL) && output.getCount() == 1,
                    "Studio runtime must produce exactly one gravel from the compiled recipe contract");
            check(helper, restored.menuData().get(0) == 0,
                    "Studio runtime must reset progress after recipe completion");
            helper.succeed();
        });
    }
}

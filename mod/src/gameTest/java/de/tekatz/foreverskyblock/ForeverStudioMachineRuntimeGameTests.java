package de.tekatz.foreverskyblock.studio.runtime;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.BlockPos;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.material.Fluids;
import net.neoforged.neoforge.fluids.FluidStack;
import net.neoforged.neoforge.fluids.capability.IFluidHandler;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ForeverStudioMachineRuntimeGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);

    private static void check(GameTestHelper helper, boolean condition, String message) {
        helper.assertTrue(condition, message);
    }

    private static ForeverStudioMachineBlockEntity machine(GameTestHelper helper) {
        helper.setBlock(POS, ForeverStudioRuntimeGameTestFixture.MACHINE_BLOCK.get());
        return helper.getBlockEntity(POS);
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

    private static void loadInputs(ForeverStudioMachineBlockEntity machine) {
        machine.inventory().insertItem(0, new ItemStack(Items.COBBLESTONE), false);
        machine.inventory().insertItem(1, new ItemStack(Items.FLINT), false);
    }

    private static void fillWater(GameTestHelper helper, ForeverStudioMachineBlockEntity machine, int amount) {
        IFluidHandler fluids = machine.fluidAutomation(null);
        check(helper, fluids != null, "Studio fluid processor must expose a fluid capability");
        int filled = fluids.fill(new FluidStack(Fluids.WATER, amount), IFluidHandler.FluidAction.EXECUTE);
        check(helper, filled == amount, "Studio fluid capability must accept the requested water amount");
    }

    @GameTest(template = "empty")
    public static void multiResourceContractProcessesPersistsAndSynchronizes(GameTestHelper helper) {
        var machine = machine(helper);
        loadInputs(machine);
        fillWater(helper, machine, 500);

        tick(helper, machine, 45);
        check(helper, machine.progress() == 45,
                "Studio runtime must make deterministic partial progress before the save point");
        check(helper, machine.inventory().getStackInSlot(0).is(Items.COBBLESTONE)
                        && machine.inventory().getStackInSlot(1).is(Items.FLINT),
                "Studio runtime must retain every item input until processing completes");
        check(helper, machine.inventory().getStackInSlot(2).isEmpty()
                        && machine.inventory().getStackInSlot(3).isEmpty(),
                "Studio runtime must not create any item output before processing completes");
        check(helper, machine.fluidAmount(0) == 500,
                "Studio runtime must retain fluid inputs until processing completes");
        check(helper, machine.menuData().get(ForeverStudioMachineBlockEntity.FLUID_DATA_START) == 500,
                "Menu synchronization must expose the authoritative tank amount");
        int animation = machine.menuData().get(ForeverStudioMachineBlockEntity.ANIMATION_DATA_START);
        check(helper, animation >= 4400 && animation <= 4600,
                "Authored progress animation must be evaluated and synchronized from runtime progress");

        var registries = helper.getLevel().registryAccess();
        var snapshot = new CompoundTag();
        machine.saveAdditional(snapshot, registries);
        check(helper, snapshot.getInt("Progress") == 45,
                "Studio runtime save data must contain exact partial progress");
        check(helper, snapshot.getInt("FluidAmount0") == 500,
                "Studio runtime save data must contain exact fluid contents");

        helper.setBlock(POS, Blocks.AIR);
        var restored = machine(helper);
        restored.loadAdditional(snapshot, registries);
        check(helper, restored.progress() == 45,
                "Studio runtime must restore exact saved progress");
        check(helper, restored.fluidAmount(0) == 500,
                "Studio runtime must restore exact saved fluid contents");

        tick(helper, restored, 55);
        check(helper, restored.inventory().getStackInSlot(0).isEmpty()
                        && restored.inventory().getStackInSlot(1).isEmpty(),
                "Studio runtime must consume all ordered item inputs atomically");
        var gravel = restored.inventory().getStackInSlot(2);
        var sand = restored.inventory().getStackInSlot(3);
        check(helper, gravel.is(Items.GRAVEL) && gravel.getCount() == 1,
                "Studio runtime must produce the first ordered output");
        check(helper, sand.is(Items.SAND) && sand.getCount() == 1,
                "Studio runtime must produce the second ordered output");
        check(helper, restored.fluidAmount(0) == 250,
                "Studio runtime must consume the authored fluid amount only on completion");
        check(helper, restored.progress() == 0,
                "Studio runtime must reset progress after recipe completion");
        helper.succeed();
    }

    @GameTest(template = "empty")
    public static void missingSecondInputNeverStarts(GameTestHelper helper) {
        var machine = machine(helper);
        machine.inventory().insertItem(0, new ItemStack(Items.COBBLESTONE), false);
        fillWater(helper, machine, 500);

        tick(helper, machine, 20);
        check(helper, machine.progress() == 0,
                "Studio multi-input runtime must not start with a missing ordered input");
        check(helper, machine.status() == ForeverStudioMachineBlockEntity.IDLE,
                "Studio multi-input runtime must remain idle while an input is missing");
        check(helper, machine.fluidAmount(0) == 500,
                "Missing item inputs must not consume fluid resources");
        helper.succeed();
    }

    @GameTest(template = "empty")
    public static void blockedOutputPreservesEveryInput(GameTestHelper helper) {
        var machine = machine(helper);
        loadInputs(machine);
        fillWater(helper, machine, 500);
        machine.inventory().setStackInSlot(2, new ItemStack(Items.DIAMOND));

        tick(helper, machine, 20);
        check(helper, machine.status() == ForeverStudioMachineBlockEntity.BLOCKED,
                "Studio runtime must report BLOCKED when any ordered output cannot fit");
        check(helper, machine.progress() == 0,
                "Blocked output must prevent processing progress from advancing");
        check(helper, machine.inventory().getStackInSlot(0).is(Items.COBBLESTONE)
                        && machine.inventory().getStackInSlot(1).is(Items.FLINT),
                "Blocked output must preserve all item inputs");
        check(helper, machine.fluidAmount(0) == 500,
                "Blocked output must preserve all fluid inputs");
        check(helper, machine.inventory().getStackInSlot(3).isEmpty(),
                "Blocked output must not partially write another output slot");
        helper.succeed();
    }
}

package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class CondenserUpgradeGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);

    private static CondenserBlockEntity mk2(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        return (CondenserBlockEntity) h.getBlockEntity(POS);
    }

    private static void tick(GameTestHelper h, CondenserBlockEntity machine, int ticks) {
        for (int i = 0; i < ticks; i++)
            CondenserBlockEntity.tick(h.getLevel(), h.absolutePos(POS), h.getBlockState(POS), machine);
    }

    private static void check(GameTestHelper h, boolean value, String message) { h.assertTrue(value, message); }

    @GameTest(template = "empty")
    public static void upgradeCapacityEnforcesCostsAndLimits(GameTestHelper h) {
        var machine = mk2(h);
        var batch = new ItemStack(ForeverSkyblock.BATCH_PROCESSING_CARD.get());
        var speed = new ItemStack(ForeverSkyblock.PROCESSING_SPEED_CARD.get());
        var buffer = new ItemStack(ForeverSkyblock.BUFFER_EXPANSION_CARD.get());

        check(h, machine.inventory().insertItem(CondenserBlockEntity.UPGRADE_1, batch, false).isEmpty(),
                "Batch card installs for two capacity");
        check(h, machine.inventory().insertItem(CondenserBlockEntity.UPGRADE_2, speed, false).isEmpty(),
                "Speed card fills the third capacity point");
        check(h, machine.upgradeCapacityUsed() == 3, "MK II reports all three capacity points in use");
        check(h, !machine.inventory().insertItem(CondenserBlockEntity.UPGRADE_3, buffer, false).isEmpty(),
                "A fourth capacity point is rejected");
        check(h, machine.batchUpgradeCount() == 1 && machine.speedUpgradeCount() == 1,
                "Installed upgrade counts remain exact");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void speedCardsUseDiminishingReturns(GameTestHelper h) {
        check(h, CondenserBlockEntity.applyProcessingSpeed(120, 0) == 120, "No card keeps base duration");
        check(h, CondenserBlockEntity.applyProcessingSpeed(120, 1) == 96, "One speed card is twenty percent faster");
        check(h, CondenserBlockEntity.applyProcessingSpeed(120, 2) == 78, "Two speed cards use the thirty-five percent reduction");
        check(h, CondenserBlockEntity.applyProcessingSpeed(120, 3) == 60, "Three speed cards halve processing time");
        check(h, CondenserBlockEntity.applyGardenSpeed(320, 0, 2) == 214,
                "Two Garden cards accelerate Garden cycles without changing yield");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void speedCardChangesLiveProcessingDuration(GameTestHelper h) {
        var machine = mk2(h);
        machine.inventory().setStackInSlot(CondenserBlockEntity.UPGRADE_1,
                new ItemStack(ForeverSkyblock.PROCESSING_SPEED_CARD.get()));
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG));
        tick(h, machine, 95);
        check(h, machine.menuData().get(1) == 96, "Live MK II duration is reduced to 96 ticks");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).isEmpty(), "No output before adjusted cycle completes");
        tick(h, machine, 1);
        ItemStack out = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
        check(h, out.is(ForeverSkyblock.STONE_FRAGMENT.get()) && out.getCount() == 4,
                "Speed card completes exactly at the adjusted duration");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void batchCardProcessesTwoRunsInOneCycle(GameTestHelper h) {
        var machine = mk2(h);
        machine.inventory().setStackInSlot(CondenserBlockEntity.UPGRADE_1,
                new ItemStack(ForeverSkyblock.BATCH_PROCESSING_CARD.get()));
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG, 2));
        tick(h, machine, 120);
        ItemStack out = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
        check(h, out.is(ForeverSkyblock.STONE_FRAGMENT.get()) && out.getCount() == 8,
                "Batch card doubles input and output per cycle");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).isEmpty(),
                "Batch cycle consumes both input runs");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void voidExcessKeepsWhatFitsAndNeverBlocks(GameTestHelper h) {
        var machine = mk2(h);
        machine.cycleOutputPolicy(CondenserBlockEntity.OUTPUT);
        check(h, machine.outputPolicy(CondenserBlockEntity.OUTPUT) == CondenserBlockEntity.POLICY_VOID_EXCESS,
                "First policy step selects Void Excess");
        machine.inventory().setStackInSlot(CondenserBlockEntity.OUTPUT,
                new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 63));
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG));
        tick(h, machine, 120);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).getCount() == 64,
                "Void Excess keeps the one fragment that still fits");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).isEmpty(),
                "Overflow is voided instead of stopping production");
        check(h, machine.menuData().get(2) != CondenserBlockEntity.BLOCKED, "Void Excess does not leave the machine blocked");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void voidAllConsumesInputWithoutCreatingOutput(GameTestHelper h) {
        var machine = mk2(h);
        machine.cycleOutputPolicy(CondenserBlockEntity.OUTPUT);
        machine.cycleOutputPolicy(CondenserBlockEntity.OUTPUT);
        check(h, machine.outputPolicy(CondenserBlockEntity.OUTPUT) == CondenserBlockEntity.POLICY_VOID_ALL,
                "Second policy step selects Void All");
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG));
        tick(h, machine, 120);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).isEmpty(), "Void All still consumes the recipe input");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).isEmpty(), "Void All discards all produced output");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void bufferCardAddsHiddenAutomationQueue(GameTestHelper h) {
        var machine = mk2(h);
        machine.inventory().setStackInSlot(CondenserBlockEntity.UPGRADE_1,
                new ItemStack(ForeverSkyblock.BUFFER_EXPANSION_CARD.get()));
        var side = machine.automation(Direction.EAST);
        check(h, side.insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG, 64), false).isEmpty(),
                "Automation fills the visible input stack");
        check(h, side.insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG, 20), false).isEmpty(),
                "Buffer card accepts a second queued input stack");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).getCount() == 64,
                "Visible input remains a normal Minecraft stack");
        check(h, machine.inputBuffers().getStackInSlot(0).getCount() == 20,
                "Overflow is stored in the hidden input buffer");
        tick(h, machine, 121);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).getCount() == 64,
                "Queued input refills the visible slot on the next machine tick");
        check(h, machine.inputBuffers().getStackInSlot(0).getCount() == 19,
                "Exactly one queued item moved back after the recipe consumed one");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void gardenPoliciesAreIndependent(GameTestHelper h) {
        var machine = mk2(h);
        h.setBlock(POS.east(), ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        tick(h, machine, 1);
        check(h, machine.hasGardenChamber(), "Garden Chamber attaches to MK II");
        machine.cycleOutputPolicy(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY);
        machine.cycleOutputPolicy(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY);
        machine.inventory().setStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN, new ItemStack(Items.WHEAT_SEEDS));
        machine.inventory().setStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY, new ItemStack(Items.WHEAT, 60));
        tick(h, machine, 320);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY).getCount() == 64,
                "Primary Garden harvest still follows its own Block policy");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY).isEmpty(),
                "Secondary Garden harvest can independently Void All");
        h.succeed();
    }
}

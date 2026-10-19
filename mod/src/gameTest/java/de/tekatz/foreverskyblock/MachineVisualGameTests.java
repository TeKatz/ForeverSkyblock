package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.GardenChamberBlockEntity;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import de.tekatz.foreverskyblock.recipe.CondenserJob;
import net.minecraft.core.BlockPos;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Blocks;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class MachineVisualGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static CondenserBlockEntity machine(GameTestHelper h, boolean markTwo) {
        h.setBlock(POS, markTwo ? ForeverSkyblock.CONDENSER_MARK_II.get() : ForeverSkyblock.CONDENSER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(POS);
    }
    @GameTest(template = "empty", timeoutTicks = 40)
    public static void freezingOnlyDoesNotRunTheMainPress(GameTestHelper h) {
        var m = machine(h, false);
        h.setBlock(POS.east(), ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get());
        m.inventory().insertItem(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.COBBLESTONE, 64), false);
        h.runAfterDelay(5, () -> {
            h.assertTrue(m.getBlockState().getValue(CondenserBlock.ACTIVE), "Freezing retains the shared active block state");
            var tag = m.getUpdateTag(h.getLevel().registryAccess()).getCompound("Visual");
            h.assertTrue(tag.getInt("Status") == CondenserBlockEntity.IDLE, "Main press must stay idle during freezing-only work");
            h.assertTrue(tag.getInt("FreezingStatus") == CondenserBlockEntity.RUNNING, "Independent freezing animation must remain running");
            h.assertTrue(tag.getInt("Job") == CondenserJob.IDLE.ordinal(), "No crushing job may be invented for the freezing input");
            h.succeed();
        });
    }
    @GameTest(template = "empty", timeoutTicks = 60)
    public static void snapshotsRoundTripTheRealJobAndStopOnBlockedOutput(GameTestHelper h) {
        var m = machine(h, true);
        h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        m.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 8), false);
        h.runAfterDelay(8, () -> {
            var tag = m.getUpdateTag(h.getLevel().registryAccess());
            var copy = new CondenserBlockEntity(h.absolutePos(POS), m.getBlockState());
            copy.setLevel(h.getLevel());
            copy.loadWithComponents(tag, h.getLevel().registryAccess());
            h.assertTrue(copy.visualJob() == CondenserJob.MILLING, "An attached Mixing Chamber must not misclassify a single-input milling job");
            h.assertTrue(copy.visualResult().is(ForeverSkyblock.STONE_MEAL.get()), "The display must show the resolved recipe output");
            h.assertTrue(copy.visualDuration() == m.menuData().get(1), "Real recipe duration must survive client snapshot round-trip");
            h.assertTrue(!m.saveWithoutMetadata(h.getLevel().registryAccess()).contains("Visual"), "Presentation snapshot must not change persistent machine data");
            int progress = m.menuData().get(0);
            m.inventory().setStackInSlot(CondenserBlockEntity.OUTPUT, new ItemStack(ForeverSkyblock.STONE_MEAL.get(), 64));
            h.runAfterDelay(4, () -> {
                h.assertTrue(m.menuData().get(0) == progress, "Blocked output must keep exact partial progress");
                h.assertTrue(m.getUpdateTag(h.getLevel().registryAccess()).getCompound("Visual").getInt("Status") == CondenserBlockEntity.BLOCKED,
                        "Blocked snapshot must stop the animation immediately");
                h.assertTrue(m.inventory().getStackInSlot(CondenserBlockEntity.INPUT).getCount() == 8, "Presentation must not consume extra ingredients");
                h.succeed();
            });
        });
    }
    @GameTest(template = "empty", timeoutTicks = 70)
    public static void gardenSnapshotUsesItsOwnDurationAndDetachesCleanly(GameTestHelper h) {
        var m = machine(h, true);
        h.setBlock(POS.north(), ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        var garden = (GardenChamberBlockEntity) h.getBlockEntity(POS.north());
        m.inventory().insertItem(CondenserBlockEntity.GARDEN_SPECIMEN, new ItemStack(Items.OAK_SAPLING), false);
        h.runAfterDelay(10, () -> {
            var tag = m.getUpdateTag(h.getLevel().registryAccess());
            h.assertTrue(tag.getCompound("Visual").getInt("GardenDuration") == 320, "Garden display must use 320 ticks rather than the old 160-tick condenser default");
            h.assertTrue(garden.running(), "Greenhouse animation must follow the real growing job");
            m.inventory().setStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.OAK_LEAVES, 64));
            h.runAfterDelay(3, () -> {
                h.assertTrue(!garden.running(), "Either full harvest slot must stop the greenhouse motion");
                h.setBlock(POS, Blocks.AIR);
                h.assertTrue(garden.linkedCondenser() == null && !garden.running(), "Removing the controller must leave no stale running animation");
                h.succeed();
            });
        });
    }
    @GameTest(template = "empty")
    public static void resolvedRecipesSelectDistinctPresentationJobs(GameTestHelper h) {
        var level = h.getLevel();
        ItemStack[][] inputs = {
                {new ItemStack(Items.OAK_LOG), ItemStack.EMPTY},
                {new ItemStack(Items.OAK_LEAVES, 8), ItemStack.EMPTY},
                {new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4), ItemStack.EMPTY},
                {new ItemStack(Items.DIRT, 4), new ItemStack(Items.ICE)},
                {new ItemStack(Items.WHEAT_SEEDS, 4), new ItemStack(Items.BONE_MEAL)}
        };
        CondenserJob[] expected = {CondenserJob.CRUSHING, CondenserJob.COMPOSTING, CondenserJob.MILLING, CondenserJob.MIXING, CondenserJob.CULTIVATING};
        for (int i = 0; i < inputs.length; i++) {
            var recipe = CondenserBlockEntity.findMatchingRecipe(level, new CondenserInput(inputs[i][0], inputs[i][1]), 2);
            h.assertTrue(recipe != null && CondenserJob.of(recipe.value()) == expected[i], "Incorrect visual job for recipe case " + i);
        }
        h.succeed();
    }
}

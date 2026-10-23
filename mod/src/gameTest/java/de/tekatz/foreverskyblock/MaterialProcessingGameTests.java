package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.BoneMealItem;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.SaplingBlock;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class MaterialProcessingGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);

    private static void check(GameTestHelper h, boolean condition, String message) { h.assertTrue(condition, message); }

    private static CondenserBlockEntity condenser(GameTestHelper h, BlockPos pos, boolean markTwo) {
        h.setBlock(pos, markTwo ? ForeverSkyblock.CONDENSER_MARK_II.get() : ForeverSkyblock.CONDENSER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(pos);
    }

    private static WashingBasinBlockEntity basin(GameTestHelper h, BlockPos pos) {
        h.setBlock(pos, ForeverSkyblock.WASHING_BASIN.get());
        return (WashingBasinBlockEntity) h.getBlockEntity(pos);
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void mudUsesFourDirtAndOneIce(GameTestHelper h) {
        var machine = condenser(h, POS, false);
        h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        machine.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.DIRT, 4), false);
        machine.inventory().insertItem(CondenserBlockEntity.SECOND_INPUT, new ItemStack(Items.ICE), false);
        h.runAfterDelay(180, () -> {
            ItemStack output = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
            check(h, output.is(Items.MUD) && output.getCount() == 4, "Four Dirt plus one Ice must create four Mud");
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).isEmpty(), "Mud recipe consumes all four Dirt");
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.SECOND_INPUT).isEmpty(), "Mud recipe consumes exactly one Ice");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 280)
    public static void washingRequiresSourceWaterAndProducesRawIron(GameTestHelper h) {
        var machine = basin(h, POS);
        machine.inventory().insertItem(WashingBasinBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_MEAL.get(), 4), false);
        h.runAfterDelay(60, () -> {
            check(h, machine.menuData().get(0) == 0, "Washing must not progress without adjacent source water");
            check(h, machine.menuData().get(2) == WashingBasinBlockEntity.NO_WATER, "Missing source water must be reported");
            h.setBlock(POS.east(), Blocks.WATER);
        });
        h.runAfterDelay(235, () -> {
            ItemStack output = machine.inventory().getStackInSlot(WashingBasinBlockEntity.OUTPUT);
            check(h, output.is(Items.RAW_IRON) && output.getCount() == 1, "Four Stone Meal must wash into one Raw Iron");
            check(h, machine.inventory().getStackInSlot(WashingBasinBlockEntity.INPUT).isEmpty(), "Washing consumes exactly four Stone Meal");
            check(h, h.getLevel().getFluidState(h.absolutePos(POS.east())).isSource(), "The adjacent water source must not be consumed");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 340)
    public static void washingPausesAndResumesWithWater(GameTestHelper h) {
        var machine = basin(h, POS);
        h.setBlock(POS.east(), Blocks.WATER);
        machine.inventory().insertItem(WashingBasinBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_MEAL.get(), 4), false);
        h.runAfterDelay(65, () -> {
            int progress = machine.menuData().get(0);
            check(h, progress > 0 && progress < 160, "Washing must accumulate partial progress with water");
            h.setBlock(POS.east(), Blocks.AIR);
            h.runAfterDelay(50, () -> {
                check(h, machine.menuData().get(0) == progress, "Removing water must pause without losing progress");
                check(h, machine.inventory().getStackInSlot(WashingBasinBlockEntity.INPUT).getCount() == 4, "Paused washing must not consume input");
                h.setBlock(POS.east(), Blocks.WATER);
            });
        });
        h.runAfterDelay(300, () -> {
            ItemStack output = machine.inventory().getStackInSlot(WashingBasinBlockEntity.OUTPUT);
            check(h, output.is(Items.RAW_IRON) && output.getCount() == 1, "Restoring water must resume and finish the same operation");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void washingAutomationWorksOnEveryFace(GameTestHelper h) {
        var machine = basin(h, POS);
        var absolute = h.absolutePos(POS);
        for (Direction face : Direction.values()) {
            var access = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, absolute, face);
            check(h, access != null && access.getSlots() == WashingBasinBlockEntity.MACHINE_SLOTS, "Washing capability exposes only public slots on " + face);
            var input = new ItemStack(ForeverSkyblock.STONE_MEAL.get(), 4);
            check(h, access.insertItem(0, input, true).isEmpty() && machine.inventory().getStackInSlot(0).isEmpty(), "Simulated washing input does not mutate inventory");
            check(h, access.insertItem(0, input, false).isEmpty(), "Every face accepts valid washing input");
            check(h, !access.insertItem(0, new ItemStack(Items.OAK_LOG), false).isEmpty()
                    && access.extractItem(0, 4, false).isEmpty(), "Invalid inputs and input extraction remain forbidden");
            machine.inventory().setStackInSlot(WashingBasinBlockEntity.OUTPUT, new ItemStack(Items.RAW_IRON));
            machine.inventory().setStackInSlot(WashingBasinBlockEntity.SECONDARY_OUTPUT, new ItemStack(Items.QUARTZ, 2));
            for (int slot : new int[] {WashingBasinBlockEntity.OUTPUT, WashingBasinBlockEntity.SECONDARY_OUTPUT}) {
                var expected = machine.inventory().getStackInSlot(slot).copy();
                check(h, !access.isItemValid(slot, expected) && !access.insertItem(slot, expected, false).isEmpty(), "Washing outputs reject insertion");
                check(h, ItemStack.matches(access.extractItem(slot, 64, true), expected)
                        && ItemStack.matches(machine.inventory().getStackInSlot(slot), expected), "Simulated washing output does not consume items");
                check(h, ItemStack.matches(access.extractItem(slot, 64, false), expected)
                        && machine.inventory().getStackInSlot(slot).isEmpty(), "Every face drains both washing outputs exactly on " + face);
            }
            machine.inventory().setStackInSlot(0, ItemStack.EMPTY);
        }
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 220)
    public static void markOneCannotAutomateStoneMeal(GameTestHelper h) {
        var machine = condenser(h, POS, false);
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4));
        h.runAfterDelay(190, () -> {
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).isEmpty(), "Condenser Mark I must not run Mark II recipes");
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).getCount() == 4, "Mark I must retain unsupported Stone Fragments");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void markTwoAutomatesStoneMeal(GameTestHelper h) {
        var machine = condenser(h, POS, true);
        check(h, machine.menuData().get(13) == 2, "Mark II must expose condenser tier 2 to the menu");
        machine.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4), false);
        h.runAfterDelay(180, () -> {
            ItemStack output = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
            check(h, output.is(ForeverSkyblock.STONE_MEAL.get()) && output.getCount() == 1, "Mark II must convert four Stone Fragments into one Stone Meal");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void markTwoMakesSaplingsFromSeedsAndBoneMeal(GameTestHelper h) {
        var machine = condenser(h, POS, true);
        h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        machine.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.WHEAT_SEEDS, 4), false);
        machine.inventory().insertItem(CondenserBlockEntity.SECOND_INPUT, new ItemStack(Items.BONE_MEAL), false);
        h.runAfterDelay(180, () -> {
            ItemStack output = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
            check(h, output.is(Items.OAK_SAPLING) && output.getCount() == 2, "Four Wheat Seeds plus Bone Meal must create two Oak Saplings");
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).isEmpty(), "Sapling recipe consumes four Wheat Seeds");
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.SECOND_INPUT).isEmpty(), "Sapling recipe consumes one Bone Meal");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 390)
    public static void gardenModeIsExclusiveAndKeepsItsSpecimen(GameTestHelper h) {
        var machine = condenser(h, POS, true);
        h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        h.setBlock(POS.west(), ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get());
        h.setBlock(POS.north(), ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        check(h, machine.menuData().get(14) == 1, "Garden Chamber must activate on Condenser Mark II");
        check(h, machine.menuData().get(3) == 0 && machine.menuData().get(4) == 0,
                "Garden mode must override Mixing and Freezing Chambers while connected");
        ItemStack remainder = machine.inventory().insertItem(CondenserBlockEntity.GARDEN_SPECIMEN, new ItemStack(Items.OAK_SAPLING), false);
        check(h, remainder.isEmpty(), "Oak Sapling must be accepted as a Garden specimen");
        check(h, !machine.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG), false).isEmpty(),
                "Normal Condenser inputs must be blocked in Garden mode");
        h.runAfterDelay(340, () -> {
            ItemStack specimen = machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN);
            ItemStack logs = machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY);
            ItemStack leaves = machine.inventory().getStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY);
            check(h, specimen.is(Items.OAK_SAPLING) && specimen.getCount() == 1, "Garden specimen must never be consumed");
            check(h, logs.is(Items.OAK_LOG) && logs.getCount() == 4, "One Garden cycle must produce four Oak Logs");
            check(h, leaves.is(Items.OAK_LEAVES) && leaves.getCount() == 8, "One Garden cycle must produce eight Oak Leaves");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void gardenAutomationUsesDedicatedSlots(GameTestHelper h) {
        var machine = condenser(h, POS, true);
        h.setBlock(POS.north(), ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        var absolute = h.absolutePos(POS);
        var top = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, absolute, Direction.UP);
        var bottom = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, absolute, Direction.DOWN);
        check(h, top != null && bottom != null, "Garden Condenser must expose item automation");
        check(h, top.insertItem(CondenserBlockEntity.GARDEN_SPECIMEN, new ItemStack(Items.OAK_SAPLING), false).isEmpty(),
                "Automation must be able to set the Garden specimen");
        machine.inventory().setStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY, new ItemStack(Items.OAK_LOG, 4));
        machine.inventory().setStackInSlot(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.OAK_LEAVES, 8));
        check(h, bottom.extractItem(CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY, 4, false).getCount() == 4,
                "Bottom automation extracts Garden primary harvest");
        check(h, bottom.extractItem(CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY, 8, false).isEmpty(),
                "Condenser bottom must never mix secondary harvest into the primary route");
        var secondary = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS.north()), Direction.DOWN);
        check(h, secondary != null && secondary.extractItem(0, 8, false).getCount() == 8,
                "Garden Chamber bottom extracts the controller's secondary harvest");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void markOneIgnoresGardenChamber(GameTestHelper h) {
        var machine = condenser(h, POS, false);
        h.setBlock(POS.north(), ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        check(h, machine.menuData().get(14) == 0, "Garden Chamber must require Condenser Mark II");
        check(h, !machine.inventory().insertItem(CondenserBlockEntity.GARDEN_SPECIMEN, new ItemStack(Items.OAK_SAPLING), false).isEmpty(),
                "Mark I must reject Garden specimens");
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 80)
    public static void oneBoneMealUseFullyPrimesOakSapling(GameTestHelper h) {
        BlockPos tree = new BlockPos(4, 2, 4);
        h.setBlock(tree.below(), Blocks.DIRT);
        h.setBlock(tree, Blocks.OAK_SAPLING);
        ItemStack boneMeal = new ItemStack(Items.BONE_MEAL);
        boolean applied = BoneMealItem.applyBonemeal(boneMeal, h.getLevel(), h.absolutePos(tree), null);
        check(h, applied, "Bone Meal must recognize a placed Sapling");
        h.runAfterDelay(2, () -> {
            var after = h.getLevel().getBlockState(h.absolutePos(tree));
            if (after.is(Blocks.OAK_SAPLING)) {
                check(h, after.getValue(SaplingBlock.STAGE) == 1,
                        "If the tiny GameTest structure blocks tree placement, one Bone Meal must still fully prime the Sapling");
            }
            check(h, boneMeal.isEmpty(), "Reliable Sapling growth must consume exactly one Bone Meal");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void materialProcessingRecipesAreLoaded(GameTestHelper h) {
        var manager = h.getLevel().getRecipeManager();
        String[] ids = {
                "forever_skyblock:condenser_mark_i",
                "forever_skyblock:condenser_mark_ii",
                "forever_skyblock:washing_basin",
                "forever_skyblock:garden_chamber_mark_i",
                "forever_skyblock:condensing/mud",
                "forever_skyblock:washing/raw_iron",
                "forever_skyblock:condensing/stone_meal_mk2",
                "forever_skyblock:condensing/oak_sapling_mk2",
                "forever_skyblock:garden/oak_tree"
        };
        for (String id : ids) check(h, manager.byKey(ResourceLocation.parse(id)).isPresent(), "Missing material-processing recipe: " + id);
        String[] deferredSpecies = {
                "spruce_sapling_mk2", "birch_sapling_mk2", "jungle_sapling_mk2", "acacia_sapling_mk2",
                "dark_oak_sapling_mk2", "cherry_sapling_mk2", "mangrove_propagule_mk2"
        };
        for (String id : deferredSpecies) check(h, manager.byKey(ResourceLocation.parse("forever_skyblock:condensing/" + id)).isEmpty(),
                "Unobtainable tree species must not advertise dead-end Mark II recipes: " + id);
        h.succeed();
    }
}

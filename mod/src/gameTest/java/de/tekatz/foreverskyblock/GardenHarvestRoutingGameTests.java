package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.GardenChamberBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.entity.ChestBlockEntity;
import net.neoforged.fml.ModList;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;
import static de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity.*;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class GardenHarvestRoutingGameTests {
    private static final BlockPos POS = new BlockPos(2, 3, 2), CHAMBER = POS.east();
    private static CondenserBlockEntity setup(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        h.setBlock(CHAMBER, ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(POS);
    }

    @GameTest(template = "empty")
    public static void portsExposeOnlyTheirHarvestAndSimulationDoesNotConsume(GameTestHelper h) {
        var m = setup(h);
        m.inventory().setStackInSlot(GARDEN_SPECIMEN, new ItemStack(Items.WHEAT_SEEDS));
        m.inventory().setStackInSlot(GARDEN_OUTPUT_PRIMARY, new ItemStack(Items.WHEAT, 4));
        m.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.WHEAT_SEEDS, 2));
        var level = h.getLevel();
        var primary = level.getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), Direction.DOWN);
        var secondary = level.getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(CHAMBER), Direction.DOWN);
        h.assertTrue(primary != null && secondary != null && secondary.getSlots() == 1, "Both physical ports expose the actual harvest");
        h.assertTrue(primary.getStackInSlot(GARDEN_OUTPUT_SECONDARY).isEmpty()
                && primary.extractItem(GARDEN_OUTPUT_SECONDARY, 64, false).isEmpty(), "Condenser route must hide and refuse secondary output");
        var unsided = level.getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), null);
        h.assertTrue(unsided.extractItem(GARDEN_OUTPUT_SECONDARY, 64, false).isEmpty(), "Unsided Condenser access must preserve harvest separation");
        h.assertTrue(primary.extractItem(GARDEN_OUTPUT_PRIMARY, 4, true).getCount() == 4
                && secondary.extractItem(0, 2, true).getCount() == 2, "Both routes support simulated extraction");
        h.assertTrue(m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).getCount() == 4
                && m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).getCount() == 2, "Simulation never changes either output");
        h.assertTrue(!secondary.insertItem(0, new ItemStack(Items.WHEAT_SEEDS), false).isEmpty()
                && !secondary.isItemValid(0, new ItemStack(Items.WHEAT_SEEDS)), "Harvest port must reject insertion");
        h.assertTrue(secondary.extractItem(1, 1, false).isEmpty() && secondary.extractItem(0, 0, false).isEmpty()
                && secondary.extractItem(0, -1, false).isEmpty(), "Invalid requests must not touch inventory");
        for (Direction side : Direction.values()) {
            var mainPort = level.getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), side);
            var chamberPort = level.getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(CHAMBER), side);
            h.assertTrue(mainPort != null && chamberPort != null && chamberPort.getSlots() == 1,
                    "Both harvest ports are accessible on " + side);
            m.inventory().setStackInSlot(GARDEN_SPECIMEN, ItemStack.EMPTY);
            h.assertTrue(mainPort.insertItem(GARDEN_SPECIMEN, new ItemStack(Items.WHEAT_SEEDS), true).isEmpty()
                    && m.inventory().getStackInSlot(GARDEN_SPECIMEN).isEmpty(), "Specimen adapter supports simulation on every face");
            h.assertTrue(mainPort.insertItem(GARDEN_SPECIMEN, new ItemStack(Items.WHEAT_SEEDS), false).isEmpty()
                    && m.inventory().getStackInSlot(GARDEN_SPECIMEN).getCount() == 1, "Every face can insert the retained specimen");
            h.assertTrue(mainPort.getStackInSlot(GARDEN_OUTPUT_SECONDARY).isEmpty()
                    && mainPort.extractItem(GARDEN_OUTPUT_SECONDARY, 64, false).isEmpty()
                    && mainPort.extractItem(GARDEN_SPECIMEN, 1, false).isEmpty(), "Every face preserves harvest separation and the specimen");
            h.assertTrue(chamberPort.getStackInSlot(0).is(Items.WHEAT_SEEDS)
                    && !chamberPort.isItemValid(0, new ItemStack(Items.WHEAT_SEEDS))
                    && !chamberPort.insertItem(0, new ItemStack(Items.WHEAT_SEEDS), false).isEmpty(), "Every chamber face is output-only");
            h.assertTrue(mainPort.extractItem(GARDEN_OUTPUT_PRIMARY, 4, true).getCount() == 4
                    && chamberPort.extractItem(0, 2, true).getCount() == 2
                    && m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).getCount() == 4
                    && m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).getCount() == 2, "Every face simulates without consuming harvest");
            h.assertTrue(mainPort.extractItem(GARDEN_OUTPUT_PRIMARY, 4, false).getCount() == 4
                    && chamberPort.extractItem(0, 2, false).getCount() == 2
                    && m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).isEmpty()
                    && m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).isEmpty(), "Every face drains its own harvest exactly once");
            m.inventory().setStackInSlot(GARDEN_OUTPUT_PRIMARY, new ItemStack(Items.WHEAT, 4));
            m.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.WHEAT_SEEDS, 2));
        }
        var unsidedChamber = level.getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(CHAMBER), null);
        h.assertTrue(unsidedChamber != null && unsidedChamber.extractItem(0, 2, true).getCount() == 2,
                "Unsided chamber access remains supported");
        var menu = new CondenserMenu(1, net.neoforged.neoforge.common.util.FakePlayerFactory.getMinecraft(h.getLevel()).getInventory(), m);
        h.assertTrue(menu.getSlot(GARDEN_OUTPUT_PRIMARY).getItem().is(Items.WHEAT)
                && menu.getSlot(GARDEN_OUTPUT_SECONDARY).getItem().is(Items.WHEAT_SEEDS), "Both real outputs remain accessible in the Condenser GUI");
        h.assertTrue(secondary.extractItem(0, 2, false).is(Items.WHEAT_SEEDS)
                && menu.getSlot(GARDEN_OUTPUT_SECONDARY).getItem().isEmpty(), "Chamber extraction updates the same output shown in the GUI");
        h.assertTrue(m.inventory().getStackInSlot(GARDEN_SPECIMEN).getCount() == 1
                && m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).getCount() == 4, "Secondary port must never steal specimen or main harvest");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void cachedPortFollowsLiveControllerAndStopsAfterRemoval(GameTestHelper h) {
        h.setBlock(CHAMBER, ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        var chamber = (GardenChamberBlockEntity) h.getBlockEntity(CHAMBER);
        var port = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(CHAMBER), Direction.DOWN);
        h.assertTrue(port != null && port.getStackInSlot(0).isEmpty(), "Unlinked chamber has an empty stable port");
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_I.get());
        var markOne = (CondenserBlockEntity) h.getBlockEntity(POS);
        markOne.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.OAK_LEAVES, 5));
        h.assertTrue(port.extractItem(0, 64, false).isEmpty(), "Mark I hidden inventory is never exposed");
        h.setBlock(POS, Blocks.AIR);
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        var m = (CondenserBlockEntity) h.getBlockEntity(POS);
        m.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.WHEAT_SEEDS, 2));
        h.assertTrue(port.extractItem(0, 1, false).is(Items.WHEAT_SEEDS), "Previously queried port discovers a newly attached Mark II");
        m.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, ItemStack.EMPTY);
        h.setBlock(POS, Blocks.AIR);
        h.assertTrue(port.extractItem(0, 64, false).isEmpty(), "Removed controller cannot be extracted through a stale reference");
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        var replacement = (CondenserBlockEntity) h.getBlockEntity(POS);
        replacement.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.OAK_LEAVES, 3));
        h.assertTrue(port.getStackInSlot(0).is(Items.OAK_LEAVES), "Cached port reads the replacement controller, not the old inventory");
        h.setBlock(CHAMBER, Blocks.AIR);
        h.assertTrue(chamber.isRemoved() && port.extractItem(0, 64, false).isEmpty(), "Removing the chamber invalidates its old port immediately");
        h.assertTrue(replacement.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).getCount() == 3, "Detaching must preserve stored secondary harvest");
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 560)
    public static void twoVanillaHoppersSeparateAnActualOakHarvest(GameTestHelper h) {
        realHarvest(h, Blocks.HOPPER, Items.OAK_SAPLING, Items.OAK_LOG, 4, Items.OAK_LEAVES, 8);
    }

    @GameTest(template = "empty", timeoutTicks = 560)
    public static void twoWoodenHoppersSeparateAnActualWheatHarvest(GameTestHelper h) {
        if (!ModList.get().isLoaded("woodenhopper")) { h.succeed(); return; }
        Block hopper = BuiltInRegistries.BLOCK.get(ResourceLocation.parse("woodenhopper:wooden_hopper"));
        h.assertTrue(hopper != Blocks.AIR, "Test the real Wooden Hopper block from the pack");
        realHarvest(h, hopper, Items.WHEAT_SEEDS, Items.WHEAT, 4, Items.WHEAT_SEEDS, 2);
    }

    private static void realHarvest(GameTestHelper h, Block hopper, Item specimen, Item main, int mainCount, Item second, int secondCount) {
        var m = setup(h);
        h.setBlock(POS.below(2), Blocks.CHEST);
        h.setBlock(CHAMBER.below(2), Blocks.CHEST.defaultBlockState().setValue(net.minecraft.world.level.block.ChestBlock.FACING, Direction.SOUTH));
        h.setBlock(POS.below(), hopper);
        h.setBlock(CHAMBER.below(), hopper);
        m.inventory().insertItem(GARDEN_SPECIMEN, new ItemStack(specimen), false);
        h.runAfterDelay(490, () -> {
            h.assertTrue(count(h, POS.below(2), main) == mainCount, "Main chest receives the exact primary harvest");
            h.assertTrue(count(h, CHAMBER.below(2), second) == secondCount, "Chamber chest receives the exact secondary harvest");
            h.assertTrue(count(h, POS.below(2), second) == 0 && count(h, CHAMBER.below(2), main) == 0,
                    "Unfiltered hoppers must never cross the two products");
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).isEmpty()
                    && m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).isEmpty(), "Both real outputs drain completely without item loss or duplication");
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_SPECIMEN).getCount() == 1, "Real hoppers retain the one-item specimen");
            h.succeed();
        });
    }

    private static int count(GameTestHelper h, BlockPos pos, Item item) {
        var chest = (ChestBlockEntity) h.getBlockEntity(pos);
        int count = 0;
        for (int i = 0; i < chest.getContainerSize(); i++) {
            ItemStack stack = chest.getItem(i);
            if (stack.is(item)) count += stack.getCount();
        }
        return count;
    }
}

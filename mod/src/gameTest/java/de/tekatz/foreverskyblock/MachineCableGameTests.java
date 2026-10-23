package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.RefiningChamberBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class MachineCableGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static final Direction[] SIDES = {null, Direction.DOWN, Direction.UP, Direction.NORTH,
            Direction.SOUTH, Direction.WEST, Direction.EAST};

    @GameTest(template = "empty")
    public static void markTwoCablesPreserveFiltersBuffersAndHiddenSlots(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        var machine = (CondenserBlockEntity) h.getBlockEntity(POS);
        machine.inventory().setStackInSlot(CondenserBlockEntity.UPGRADE_1, new ItemStack(ForeverSkyblock.BUFFER_EXPANSION_CARD.get()));
        h.assertTrue(machine.setFilter(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG)), "Install exact-item input filter");
        // Seed retained module inventory without attaching a module: cables must not expose it.
        machine.inventory().setStackInSlot(CondenserBlockEntity.SECOND_INPUT, new ItemStack(Items.DIRT));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT, new ItemStack(Items.ICE));
        for (Direction side : SIDES) {
            var access = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), side);
            h.assertTrue(access != null && access.getSlots() == CondenserBlockEntity.BASE_MACHINE_SLOTS,
                    "Mark II exposes only public automation slots on " + side);
            for (int slot : new int[] {CondenserBlockEntity.SECOND_INPUT, CondenserBlockEntity.FREEZING_INPUT,
                    CondenserBlockEntity.FREEZING_OUTPUT, CondenserBlockEntity.GARDEN_SPECIMEN,
                    CondenserBlockEntity.GARDEN_OUTPUT_PRIMARY, CondenserBlockEntity.GARDEN_OUTPUT_SECONDARY,
                    CondenserBlockEntity.UPGRADE_1, CondenserBlockEntity.UPGRADE_2, CondenserBlockEntity.UPGRADE_3}) {
                h.assertTrue(access.getStackInSlot(slot).isEmpty() && access.extractItem(slot, 64, false).isEmpty()
                        && !access.isItemValid(slot, new ItemStack(Items.OAK_LOG))
                        && !access.insertItem(slot, new ItemStack(Items.OAK_LOG), false).isEmpty(), "Hidden slots remain inaccessible on " + side);
            }
            h.assertTrue(!access.isItemValid(CondenserBlockEntity.INPUT, new ItemStack(Items.BIRCH_LOG))
                    && !access.insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.BIRCH_LOG), false).isEmpty(), "Every face respects exact-item filters");
            h.assertTrue(access.insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG, 64), false).isEmpty(), "Fill visible input through cable");
            h.assertTrue(access.insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG, 20), true).isEmpty()
                    && machine.inputBuffers().getStackInSlot(0).isEmpty(), "Buffer insertion simulation does not queue items");
            h.assertTrue(access.insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.OAK_LOG, 20), false).isEmpty()
                    && machine.inventory().getStackInSlot(CondenserBlockEntity.INPUT).getCount() == 64
                    && machine.inputBuffers().getStackInSlot(0).getCount() == 20, "Every face uses the bounded input buffer");
            h.assertTrue(access.extractItem(CondenserBlockEntity.INPUT, 64, false).isEmpty(), "Cables cannot extract buffered inputs");
            machine.inventory().setStackInSlot(CondenserBlockEntity.OUTPUT, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4));
            machine.outputBuffers().setStackInSlot(0, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 8));
            h.assertTrue(access.extractItem(CondenserBlockEntity.OUTPUT, 4, true).getCount() == 4
                    && machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).getCount() == 4
                    && machine.outputBuffers().getStackInSlot(0).getCount() == 8, "Extraction simulation leaves both output stores intact");
            h.assertTrue(access.extractItem(CondenserBlockEntity.OUTPUT, 4, false).getCount() == 4
                    && machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).getCount() == 8
                    && machine.outputBuffers().getStackInSlot(0).isEmpty(), "Real cable extraction refills output from its buffer");
            h.assertTrue(access.extractItem(CondenserBlockEntity.OUTPUT, 64, false).getCount() == 8
                    && access.extractItem(CondenserBlockEntity.OUTPUT, 64, false).isEmpty(), "Buffered output drains exactly once");
            machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, ItemStack.EMPTY);
            machine.inputBuffers().setStackInSlot(0, ItemStack.EMPTY);
        }
        h.assertTrue(machine.inventory().getStackInSlot(CondenserBlockEntity.UPGRADE_1).is(ForeverSkyblock.BUFFER_EXPANSION_CARD.get())
                && machine.inventory().getStackInSlot(CondenserBlockEntity.SECOND_INPUT).is(Items.DIRT)
                && machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT).is(Items.ICE), "Hidden retained inventory survives all cable queries");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void refiningCablesKeepInputOutputAndEnergyRules(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.REFINING_CHAMBER.get());
        var machine = (RefiningChamberBlockEntity) h.getBlockEntity(POS);
        for (Direction side : SIDES) {
            var access = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), side);
            h.assertTrue(access != null && access.getSlots() == RefiningChamberBlockEntity.MACHINE_SLOTS, "Refining exposes item automation on " + side);
            for (int slot : new int[] {RefiningChamberBlockEntity.PRIMARY, RefiningChamberBlockEntity.CATALYST}) {
                var input = slot == RefiningChamberBlockEntity.PRIMARY
                        ? new ItemStack(ForeverSkyblock.MINERAL_BLEND.get(), 2) : new ItemStack(Items.REDSTONE);
                h.assertTrue(access.isItemValid(slot, input) && access.insertItem(slot, input, true).isEmpty()
                        && machine.inventory().getStackInSlot(slot).isEmpty(), "Refining insertion simulation does not consume items");
                h.assertTrue(!access.isItemValid(slot, new ItemStack(Items.DIRT))
                        && !access.insertItem(slot, new ItemStack(Items.DIRT), false).isEmpty(), "Refining rejects invalid ingredients");
                h.assertTrue(access.insertItem(slot, input, false).isEmpty()
                        && access.extractItem(slot, 64, false).isEmpty(), "Every face inserts but cannot steal refining ingredients");
            }
            var expected = new ItemStack(Items.LAPIS_LAZULI, 6);
            machine.inventory().setStackInSlot(RefiningChamberBlockEntity.OUTPUT, expected.copy());
            h.assertTrue(!access.isItemValid(RefiningChamberBlockEntity.OUTPUT, expected)
                    && !access.insertItem(RefiningChamberBlockEntity.OUTPUT, expected, false).isEmpty(), "Refining output rejects insertion");
            h.assertTrue(ItemStack.matches(access.extractItem(RefiningChamberBlockEntity.OUTPUT, 64, true), expected)
                    && ItemStack.matches(machine.inventory().getStackInSlot(RefiningChamberBlockEntity.OUTPUT), expected), "Refining extraction simulation preserves output");
            h.assertTrue(ItemStack.matches(access.extractItem(RefiningChamberBlockEntity.OUTPUT, 64, false), expected)
                    && machine.inventory().getStackInSlot(RefiningChamberBlockEntity.OUTPUT).isEmpty(), "Every face extracts exact refining output");
            var energy = h.getLevel().getCapability(Capabilities.EnergyStorage.BLOCK, h.absolutePos(POS), side);
            h.assertTrue(energy != null && energy.canReceive() && !energy.canExtract()
                    && energy.receiveEnergy(5000, true) == RefiningChamberBlockEntity.MAX_RECEIVE
                    && energy.getEnergyStored() == 0 && energy.extractEnergy(1000, false) == 0, "Item cable changes preserve energy limits and simulation");
            machine.inventory().setStackInSlot(RefiningChamberBlockEntity.PRIMARY, ItemStack.EMPTY);
            machine.inventory().setStackInSlot(RefiningChamberBlockEntity.CATALYST, ItemStack.EMPTY);
        }
        h.succeed();
    }
}

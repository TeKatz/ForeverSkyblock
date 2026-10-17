package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.phys.AABB;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.entity.HopperBlockEntity;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;
import net.neoforged.neoforge.items.IItemHandler;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class CondenserGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static CondenserBlockEntity machine(GameTestHelper h, BlockPos pos) {
        h.setBlock(pos, ForeverSkyblock.CONDENSER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(pos);
    }
    private static void check(GameTestHelper h, boolean condition, String message) { h.assertTrue(condition, message); }
    private static int output(CondenserBlockEntity machine) { return machine.inventory().getStackInSlot(1).getCount(); }

    @GameTest(template = "empty", timeoutTicks = 220)
    public static void ratiosAndTiming(GameTestHelper h) {
        var logs = machine(h, POS);
        var planks = machine(h, POS.offset(2, 0, 0));
        logs.inventory().insertItem(0, new ItemStack(Items.OAK_LOG), false);
        planks.inventory().insertItem(0, new ItemStack(Items.OAK_PLANKS), false);
        h.runAfterDelay(100, () -> {
            check(h, output(logs) == 0 && output(planks) == 0, "No early output");
            check(h, logs.inventory().getStackInSlot(0).getCount() == 1, "Input retained until completion");
        });
        h.runAfterDelay(170, () -> {
            check(h, output(logs) == 4, "One log must produce four fragments");
            check(h, output(planks) == 1, "One plank must produce one fragment");
            check(h, logs.inventory().getStackInSlot(0).isEmpty() && planks.inventory().getStackInSlot(0).isEmpty(), "Consume exactly one input");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void leavesRequireEightAndProduceOneBoneMeal(GameTestHelper h) {
        var machine = machine(h, POS);
        machine.inventory().insertItem(0, new ItemStack(Items.OAK_LEAVES, 7), false);
        h.runAfterDelay(60, () -> {
            check(h, machine.menuData().get(0) == 0 && machine.inventory().getStackInSlot(1).isEmpty(),
                    "Seven leaves must not start the bone-meal recipe");
            machine.inventory().insertItem(0, new ItemStack(Items.OAK_LEAVES), false);
        });
        h.runAfterDelay(225, () -> {
            ItemStack result = machine.inventory().getStackInSlot(1);
            check(h, result.is(Items.BONE_MEAL) && result.getCount() == 1, "Eight leaves must produce exactly one bone meal");
            check(h, machine.inventory().getStackInSlot(0).isEmpty(), "Bone-meal recipe consumes all eight leaves");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 280)
    public static void blockedOutputResumesWithoutLoss(GameTestHelper h) {
        var machine = machine(h, POS);
        machine.inventory().insertItem(0, new ItemStack(Items.OAK_LOG), false);
        machine.inventory().setStackInSlot(1, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 62));
        h.runAfterDelay(60, () -> {
            check(h, machine.menuData().get(0) == 0, "Blocked machine must not progress");
            check(h, machine.inventory().getStackInSlot(0).getCount() == 1 && output(machine) == 62, "Blocked output must preserve both stacks");
            machine.automation(Direction.DOWN).extractItem(1, 64, false);
        });
        h.runAfterDelay(240, () -> {
            check(h, output(machine) == 4 && machine.inventory().getStackInSlot(0).isEmpty(), "Resume with exact output after extraction");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void sidedAutomationAndSimulation(GameTestHelper h) {
        var machine = machine(h, POS);
        var level = h.getLevel();
        var pos = h.absolutePos(POS);
        IItemHandler top = level.getCapability(Capabilities.ItemHandler.BLOCK, pos, Direction.UP);
        IItemHandler bottom = level.getCapability(Capabilities.ItemHandler.BLOCK, pos, Direction.DOWN);
        IItemHandler side = level.getCapability(Capabilities.ItemHandler.BLOCK, pos, Direction.EAST);
        check(h, top != null && bottom != null && side != null, "Expose item capabilities on every side");
        check(h, top.insertItem(0, new ItemStack(Items.OAK_LOG), true).isEmpty(), "Simulated valid insertion accepted");
        check(h, machine.inventory().getStackInSlot(0).isEmpty(), "Simulation must not mutate inventory");
        check(h, !top.insertItem(0, new ItemStack(Items.IRON_INGOT), false).isEmpty(), "Reject invalid inputs");
        check(h, !bottom.insertItem(0, new ItemStack(Items.OAK_LOG), false).isEmpty(), "Bottom must not insert");
        check(h, side.insertItem(0, new ItemStack(Items.BIRCH_PLANKS), false).isEmpty(), "Side accepts tagged planks");
        check(h, bottom.extractItem(0, 1, false).isEmpty(), "Automation must not steal input");
        machine.inventory().setStackInSlot(1, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4));
        check(h, top.extractItem(1, 4, false).isEmpty(), "Top must not extract output");
        check(h, bottom.extractItem(1, 4, true).getCount() == 4 && output(machine) == 4, "Simulated extraction leaves output intact");
        check(h, bottom.extractItem(1, 4, false).getCount() == 4 && output(machine) == 0, "Bottom extracts output");
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 320)
    public static void realHoppersFeedAndExtract(GameTestHelper h) {
        var machine = machine(h, POS);
        h.setBlock(POS.above(), Blocks.HOPPER);
        h.setBlock(POS.below(), Blocks.HOPPER);
        var top = (HopperBlockEntity) h.getBlockEntity(POS.above());
        var bottom = (HopperBlockEntity) h.getBlockEntity(POS.below());
        top.setItem(0, new ItemStack(Items.OAK_LOG));
        h.runAfterDelay(260, () -> {
            int fragments = 0;
            for (int i = 0; i < bottom.getContainerSize(); i++) {
                var stack = bottom.getItem(i);
                check(h, stack.isEmpty() || stack.is(ForeverSkyblock.STONE_FRAGMENT.get()), "Bottom hopper only receives fragments");
                fragments += stack.getCount();
            }
            check(h, top.isEmpty() && fragments == 4 && output(machine) == 0, "Actual hoppers complete the production chain");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void savedProgressSurvivesReload(GameTestHelper h) {
        var machine = machine(h, POS);
        machine.inventory().insertItem(0, new ItemStack(Items.OAK_LOG), false);
        h.runAfterDelay(70, () -> {
            var registry = h.getLevel().registryAccess();
            var tag = machine.saveWithFullMetadata(registry);
            check(h, tag.getInt("Progress") > 0 && tag.getInt("Progress") < 160, "Save partial progress");
            var restored = new CondenserBlockEntity(h.absolutePos(POS), machine.getBlockState());
            restored.loadWithComponents(tag, registry);
            check(h, restored.menuData().get(0) == machine.menuData().get(0), "Restore exact progress");
            h.getLevel().setBlockEntity(restored);
        });
        h.runAfterDelay(180, () -> {
            var restored = (CondenserBlockEntity) h.getBlockEntity(POS);
            check(h, output(restored) == 4 && restored.inventory().getStackInSlot(0).isEmpty(), "Reload preserves progress without lost or duplicated output");
            h.succeed();
        });
    }
    @GameTest(template = "empty")
    public static void shiftClickAndRemovalPreserveItems(GameTestHelper h) {
        var machine = machine(h, POS);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        player.getInventory().clearContent();
        var absolute = h.absolutePos(POS);
        player.setPos(absolute.getX() + 0.5, absolute.getY(), absolute.getZ() + 0.5);
        var menu = new CondenserMenu(1, player.getInventory(), machine);
        player.getInventory().setItem(9, new ItemStack(Items.OAK_LOG, 3));
        menu.quickMoveStack(player, 3);
        check(h, player.getInventory().getItem(9).isEmpty() && machine.inventory().getStackInSlot(0).getCount() == 3,
                "Shift-click moves wood into the input only");
        machine.inventory().setStackInSlot(1, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 4));
        menu.quickMoveStack(player, 1);
        int collected = 0;
        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            var stack = player.getInventory().getItem(i);
            if (stack.is(ForeverSkyblock.STONE_FRAGMENT.get())) collected += stack.getCount();
        }
        check(h, collected == 4 && output(machine) == 0, "Shift-click extracts exact output");
        check(h, menu.stillValid(player), "Nearby menu remains valid");
        player.setPos(absolute.getX() + 20, absolute.getY(), absolute.getZ());
        check(h, !menu.stillValid(player), "Menu closes out of range");
        machine.inventory().setStackInSlot(0, new ItemStack(Items.OAK_PLANKS, 2));
        machine.inventory().setStackInSlot(1, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 3));
        h.getLevel().destroyBlock(absolute, true);
        int wood = 0, fragments = 0;
        for (var entity : h.getLevel().getEntitiesOfClass(ItemEntity.class, new AABB(absolute).inflate(1))) {
            var stack = entity.getItem();
            if (stack.is(Items.OAK_PLANKS)) wood += stack.getCount();
            if (stack.is(ForeverSkyblock.STONE_FRAGMENT.get())) fragments += stack.getCount();
        }
        check(h, wood == 2 && fragments == 3, "Breaking drops each stored item exactly once");
        player.getInventory().clearContent();
        h.succeed();
    }
}

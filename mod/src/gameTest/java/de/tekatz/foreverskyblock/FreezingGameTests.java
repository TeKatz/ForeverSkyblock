package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.level.block.Blocks;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class FreezingGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);

    private static CondenserBlockEntity machine(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(POS);
    }

    private static void freezer(GameTestHelper h) {
        h.setBlock(POS.east(), ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get());
    }

    private static void mixing(GameTestHelper h) {
        h.setBlock(POS.west(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
    }

    private static void check(GameTestHelper h, boolean value, String message) {
        h.assertTrue(value, message);
    }

    private static void tick(GameTestHelper h, CondenserBlockEntity machine, int count) {
        for (int i = 0; i < count; i++) {
            CondenserBlockEntity.tick(h.getLevel(), h.absolutePos(POS), machine.getBlockState(), machine);
        }
    }

    @GameTest(template = "empty")
    public static void liveMenuHasFourModuleStates(GameTestHelper h) {
        var machine = machine(h);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        var menu = new CondenserMenu(1, player.getInventory(), machine);

        check(h, menu.moduleState() == 0, "Base condenser starts without modules");
        check(h, menu.getSlot(CondenserBlockEntity.INPUT).isActive()
                && !menu.getSlot(CondenserBlockEntity.SECOND_INPUT).isActive()
                && !menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).isActive()
                && !menu.getSlot(CondenserBlockEntity.FREEZING_OUTPUT).isActive(), "Base state exposes only condenser slots");

        mixing(h);
        check(h, menu.moduleState() == 1 && menu.getSlot(CondenserBlockEntity.SECOND_INPUT).isActive()
                && !menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).isActive(), "Mixing-only state updates the already-open menu");

        h.setBlock(POS.west(), Blocks.AIR);
        freezer(h);
        check(h, menu.moduleState() == 2 && !menu.getSlot(CondenserBlockEntity.SECOND_INPUT).isActive()
                && menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).isActive()
                && menu.getSlot(CondenserBlockEntity.FREEZING_OUTPUT).isActive(), "Freezing-only state updates the already-open menu");

        mixing(h);
        check(h, menu.moduleState() == 3 && menu.getSlot(CondenserBlockEntity.SECOND_INPUT).isActive()
                && menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).isActive(), "Full state exposes both modules together");

        h.setBlock(POS.east(), Blocks.AIR);
        check(h, menu.moduleState() == 1 && !menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).isActive(), "Removing freezer updates the same open menu");
        check(h, menu.slots.size() == CondenserBlockEntity.MACHINE_SLOTS + 36, "Menu keeps stable slot indices across every live state");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void arbitraryItemsBecomeIceAtExactlyThirtyTwo(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        ItemStack remainder = machine.inventory().insertItem(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.STICK, 32), false);
        check(h, remainder.isEmpty(), "Freezing input accepts an arbitrary item");

        tick(h, machine, 31);
        check(h, machine.menuData().get(5) == 31, "Thirty-one consumed items produce exactly 31/32 progress");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == 1, "Thirty-first step leaves the final item");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT).isEmpty(), "No Ice is produced early");

        tick(h, machine, 1);
        check(h, machine.menuData().get(5) == 0, "Ice completion resets material progress");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).isEmpty(), "Exactly 32 items are consumed");
        ItemStack output = machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT);
        check(h, output.is(Items.ICE) && output.getCount() == 1, "Exactly one Ice block is produced");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void freezingLockControlsGuiAndAutomation(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        var absolute = h.absolutePos(POS);
        player.setPos(absolute.getX() + 0.5, absolute.getY(), absolute.getZ() + 0.5);
        var menu = new CondenserMenu(2, player.getInventory(), machine);

        check(h, menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).mayPlace(new ItemStack(Items.DIAMOND)),
                "Unlocked Freezing input accepts any item");
        menu.clickMenuButton(player, 0);
        check(h, menu.setGhost(player, CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.COBBLESTONE)),
                "Freezing input supports the common lock editor");
        menu.clickMenuButton(player, 0);

        check(h, !menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).mayPlace(new ItemStack(Items.DIAMOND))
                && menu.getSlot(CondenserBlockEntity.FREEZING_INPUT).mayPlace(new ItemStack(Items.COBBLESTONE)),
                "GUI insertion obeys the Freezing lock");

        var automation = machine.automation(Direction.UP);
        ItemStack wrong = automation.insertItem(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.DIAMOND, 4), false);
        check(h, wrong.getCount() == 4 && machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).isEmpty(),
                "Pipes cannot bypass the Freezing lock");
        check(h, automation.insertItem(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.COBBLESTONE, 4), false).isEmpty(),
                "Pipes can insert the locked item");
        check(h, machine.filter(CondenserBlockEntity.FREEZING_INPUT).is(Items.COBBLESTONE), "Freezing lock is stored on the condenser");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void disconnectPreservesInputOutputProgressAndLock(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        machine.setFilter(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.COBBLESTONE));
        machine.inventory().insertItem(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.COBBLESTONE, 40), false);
        tick(h, machine, 10);
        check(h, machine.menuData().get(5) == 10, "Freezer accumulates partial progress before disconnect");

        h.setBlock(POS.east(), Blocks.AIR);
        var automation = machine.automation(Direction.UP);
        check(h, automation.getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).isEmpty()
                && !automation.insertItem(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.COBBLESTONE), false).isEmpty(),
                "Disconnected Freezing slots are hidden from automation");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == 30
                && machine.menuData().get(5) == 10
                && machine.filter(CondenserBlockEntity.FREEZING_INPUT).is(Items.COBBLESTONE),
                "Disconnect preserves real input, progress and lock");

        tick(h, machine, 10);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == 30
                && machine.menuData().get(5) == 10, "Disconnected freezer does not consume items");

        freezer(h);
        tick(h, machine, 22);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT).is(Items.ICE)
                && machine.menuData().get(5) == 0, "Reattached freezer resumes exactly where it stopped");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void blockedIceOutputNeverConsumesCompletionItem(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.DIRT, 31));
        tick(h, machine, 31);
        check(h, machine.menuData().get(5) == 31, "Prepare 31/32 progress");
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT, new ItemStack(Items.ICE, 64));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.DIRT));
        tick(h, machine, 1);
        check(h, machine.menuData().get(6) == CondenserBlockEntity.BLOCKED
                && machine.menuData().get(5) == 31
                && machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == 1,
                "Full Ice output blocks before consuming item 32");
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT, ItemStack.EMPTY);
        tick(h, machine, 1);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT).is(Items.ICE)
                && machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).isEmpty(),
                "Freezing resumes safely once output has room");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void mixingAndFreezingRunInParallel(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        mixing(h);
        machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_MEAL.get()));
        machine.inventory().setStackInSlot(CondenserBlockEntity.SECOND_INPUT, new ItemStack(Items.OAK_SAPLING));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.STICK, 32));

        tick(h, machine, 160);
        ItemStack normalOutput = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
        ItemStack freezeOutput = machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT);
        check(h, normalOutput.is(Items.DIRT) && normalOutput.getCount() == 1, "Mixing recipe completes while freezer is attached");
        check(h, freezeOutput.is(Items.ICE) && freezeOutput.getCount() == 1, "Freezing completes independently in the same condenser");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void freezerStateAndLockSurviveSaveReload(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        machine.setFilter(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.OAK_PLANKS));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.OAK_PLANKS, 7));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT, new ItemStack(Items.ICE, 2));
        tick(h, machine, 5);

        var registries = h.getLevel().registryAccess();
        var tag = machine.saveWithFullMetadata(registries);
        var restored = new CondenserBlockEntity(h.absolutePos(POS), machine.getBlockState());
        restored.loadWithComponents(tag, registries);
        check(h, restored.inventory().getSlots() == CondenserBlockEntity.MACHINE_SLOTS
                && restored.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == 2
                && restored.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT).getCount() == 2
                && restored.menuData().get(5) == 5
                && restored.filter(CondenserBlockEntity.FREEZING_INPUT).is(Items.OAK_PLANKS),
                "Freezing slots, partial progress and lock survive save/reload");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void condensingTransfersNeverTouchFreezingState(GameTestHelper h) {
        var machine = machine(h);
        freezer(h);
        mixing(h);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        player.getInventory().clearContent();
        var absolute = h.absolutePos(POS);
        player.setPos(absolute.getX() + 0.5, absolute.getY(), absolute.getZ() + 0.5);
        var menu = new CondenserMenu(4, player.getInventory(), machine);

        machine.setFilter(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.STICK));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_INPUT, new ItemStack(Items.STICK, 10));
        machine.inventory().setStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT, new ItemStack(Items.ICE, 2));
        tick(h, machine, 5);
        int freezeProgress = machine.menuData().get(5);
        int freezeInput = machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount();

        player.getInventory().setItem(0, new ItemStack(ForeverSkyblock.STONE_MEAL.get()));
        player.getInventory().setItem(1, new ItemStack(Items.OAK_SAPLING));
        var dirtId = ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "condensing/dirt");
        check(h, menu.transferRecipe(player, dirtId, false), "Normal JEI transfer succeeds with both modules attached");
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == freezeInput
                && machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_OUTPUT).getCount() == 2
                && machine.menuData().get(5) == freezeProgress
                && machine.filter(CondenserBlockEntity.FREEZING_INPUT).is(Items.STICK),
                "Normal recipe transfer preserves Freezing input, output, progress and lock");

        player.getInventory().setItem(9, new ItemStack(Items.DIAMOND));
        menu.quickMoveStack(player, CondenserMenu.MACHINE_SLOT_COUNT);
        check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.FREEZING_INPUT).getCount() == freezeInput,
                "Generic shift-click never treats the any-item Freezing input as a sink");
        player.getInventory().clearContent();
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void craftingRecipesGateWorkbenchAndBuildFreezer(GameTestHelper h) {
        var level = h.getLevel();
        var workbench = (net.minecraft.world.item.crafting.CraftingRecipe) level.getRecipeManager()
                .byKey(ResourceLocation.withDefaultNamespace("crafting_table")).orElseThrow().value();

        var cheap = new ArrayList<ItemStack>();
        for (int i = 0; i < 4; i++) cheap.add(new ItemStack(Items.OAK_PLANKS));
        var cheapInput = CraftingInput.of(2, 2, cheap);
        check(h, !workbench.matches(cheapInput, level), "Vanilla four-plank workbench pattern is replaced");

        List<ItemStack> gated = new ArrayList<>();
        for (int i = 0; i < 9; i++) gated.add(ItemStack.EMPTY);
        gated.set(1, new ItemStack(Items.OAK_PLANKS));
        gated.set(3, new ItemStack(Items.OAK_PLANKS));
        gated.set(4, new ItemStack(ForeverSkyblock.STONE_MEAL.get()));
        gated.set(5, new ItemStack(Items.OAK_PLANKS));
        gated.set(7, new ItemStack(Items.OAK_PLANKS));
        var gatedInput = CraftingInput.of(3, 3, gated);
        check(h, workbench.matches(gatedInput, level)
                && workbench.assemble(gatedInput, level.registryAccess()).is(Items.CRAFTING_TABLE),
                "Four planks plus Stone Meal craft the normal workbench");

        var freezerRecipe = (net.minecraft.world.item.crafting.CraftingRecipe) level.getRecipeManager()
                .byKey(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "freezing_chamber_mark_i"))
                .orElseThrow().value();
        List<ItemStack> freezerGrid = List.of(
                new ItemStack(Items.COBBLESTONE), new ItemStack(Items.BONE_MEAL), new ItemStack(Items.COBBLESTONE),
                new ItemStack(ForeverSkyblock.STONE_MEAL.get()), new ItemStack(ForeverSkyblock.MIXING_CHAMBER_MARK_I.get()), new ItemStack(ForeverSkyblock.STONE_MEAL.get()),
                new ItemStack(Items.COBBLESTONE), new ItemStack(Items.BONE_MEAL), new ItemStack(Items.COBBLESTONE));
        var freezerInput = CraftingInput.of(3, 3, freezerGrid);
        check(h, freezerRecipe.matches(freezerInput, level)
                && freezerRecipe.assemble(freezerInput, level.registryAccess()).is(ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get()),
                "Freezing Chamber recipe uses one Mixing Chamber plus current-progression materials");
        h.succeed();
    }
}

package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.menu.CondenserTransfer;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import java.util.List;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class TransferGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private record Setup(CondenserBlockEntity machine, CondenserMenu menu, net.neoforged.neoforge.common.util.FakePlayer player) { }
    private static Setup setup(GameTestHelper h, boolean module) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_I.get());
        if (module) h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        var machine = (CondenserBlockEntity) h.getBlockEntity(POS);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        player.getInventory().clearContent();
        var absolute = h.absolutePos(POS);
        player.setPos(absolute.getX() + 0.5, absolute.getY(), absolute.getZ() + 0.5);
        return new Setup(machine, new CondenserMenu(12, player.getInventory(), machine), player);
    }
    private static ResourceLocation id(String path) { return ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "condensing/" + path); }
    private static CondenserRecipe recipe(GameTestHelper h, String name) {
        return (CondenserRecipe) h.getLevel().getRecipeManager().byKey(id(name)).orElseThrow().value();
    }
    private static ItemStack meal(int count) { return new ItemStack(ForeverSkyblock.STONE_MEAL.get(), count); }
    private static void check(GameTestHelper h, boolean value, String message) { h.assertTrue(value, message); }
    private static int count(Setup s, net.minecraft.world.item.Item item) {
        int total = 0;
        for (int i = 0; i < 36; i++) { var stack = s.player().getInventory().getItem(i); if (stack.is(item)) total += stack.getCount(); }
        for (int i = 0; i < 3; i++) { var stack = s.machine().inventory().getStackInSlot(i); if (stack.is(item)) total += stack.getCount(); }
        return total;
    }
    @GameTest(template = "empty")
    public static void previewIsReadOnlyAndServerTransfersOneSet(GameTestHelper h) {
        var s = setup(h, true);
        s.player().getInventory().setItem(0, meal(4));
        s.player().getInventory().setItem(9, new ItemStack(Items.BIRCH_SAPLING, 3));
        var plan = CondenserTransfer.plan(s.menu(), recipe(h, "dirt"), false);
        check(h, plan.success() && s.machine().inventory().getStackInSlot(0).isEmpty()
                && s.player().getInventory().getItem(0).getCount() == 4, "JEI availability check cannot move items");
        check(h, s.menu().transferRecipe(s.player(), id("dirt"), false), "Authoritative transfer succeeds");
        check(h, s.machine().inventory().getStackInSlot(0).getCount() == 1 && s.machine().inventory().getStackInSlot(2).getCount() == 1,
                "Normal plus loads exactly one set");
        check(h, count(s, ForeverSkyblock.STONE_MEAL.get()) == 4 && count(s, Items.BIRCH_SAPLING) == 3, "Transfer conserves every ingredient");
        check(h, s.menu().transferRecipe(s.player(), id("dirt"), false)
                && s.machine().inventory().getStackInSlot(0).getCount() == 1, "Already loaded recipe is retained without repeatedly adding ingredients");
        s.player().getInventory().clearContent();
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void filtersSwapInputsAndShiftTransfersMaximum(GameTestHelper h) {
        var s = setup(h, true);
        s.machine().setFilter(0, new ItemStack(Items.OAK_SAPLING));
        s.machine().setFilter(2, meal(1));
        s.player().getInventory().setItem(0, meal(8));
        s.player().getInventory().setItem(1, new ItemStack(Items.OAK_SAPLING, 3));
        check(h, s.menu().transferRecipe(s.player(), id("dirt"), true), "Shift-transfer accepts reversed filters");
        check(h, s.machine().inventory().getStackInSlot(0).is(Items.OAK_SAPLING)
                && s.machine().inventory().getStackInSlot(0).getCount() == 3
                && s.machine().inventory().getStackInSlot(2).getCount() == 3, "Fill maximum complete sets in filter-selected order");
        check(h, count(s, ForeverSkyblock.STONE_MEAL.get()) == 8 && count(s, Items.OAK_SAPLING) == 3, "Maximum transfer conserves counts");
        check(h, s.machine().filter(0).is(Items.OAK_SAPLING) && s.machine().filter(2).is(ForeverSkyblock.STONE_MEAL.get()), "Transfer preserves filters");
        s.player().getInventory().clearContent();
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void missingBlockedAndStaleRequestsDoNotMutate(GameTestHelper h) {
        var s = setup(h, false);
        s.player().getInventory().setItem(0, meal(2));
        s.player().getInventory().setItem(1, new ItemStack(Items.OAK_SAPLING, 2));
        check(h, CondenserTransfer.plan(s.menu(), recipe(h, "dirt"), false).error() == CondenserTransfer.Error.MODULE, "Missing module disables plus");
        check(h, !s.menu().transferRecipe(s.player(), id("dirt"), false), "Server also rejects absent module");
        h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        s.player().getInventory().setItem(1, ItemStack.EMPTY);
        check(h, CondenserTransfer.plan(s.menu(), recipe(h, "dirt"), false).error() == CondenserTransfer.Error.MISSING, "Missing ingredient disables plus");
        check(h, !s.menu().transferRecipe(s.player(), id("dirt"), false) && s.player().getInventory().getItem(0).getCount() == 2, "Failure cannot partially transfer meal");
        s.player().getInventory().setItem(1, new ItemStack(Items.OAK_SAPLING, 2));
        s.machine().setFilter(0, new ItemStack(Items.OAK_LOG));
        check(h, CondenserTransfer.plan(s.menu(), recipe(h, "dirt"), false).error() == CondenserTransfer.Error.FILTERS, "Incompatible filters disable plus");
        s.machine().setFilter(0, ItemStack.EMPTY);
        s.menu().clickMenuButton(s.player(), 0);
        check(h, CondenserTransfer.plan(s.menu(), recipe(h, "dirt"), false).error() == CondenserTransfer.Error.EDITING, "Real transfer unavailable while editing ghosts");
        s.menu().clickMenuButton(s.player(), 0);
        s.menu().setCarried(new ItemStack(Items.STICK));
        check(h, CondenserTransfer.plan(s.menu(), recipe(h, "dirt"), false).error() == CondenserTransfer.Error.CURSOR, "Cursor contents are protected");
        s.menu().setCarried(ItemStack.EMPTY);
        h.setBlock(POS.east(), Blocks.AIR);
        check(h, !s.menu().transferRecipe(s.player(), id("dirt"), false), "Removing module after preview rejects stale request");
        check(h, !s.menu().transferRecipe(s.player(), id("nonexistent"), false)
                && !s.menu().transferRecipe(s.player(), ResourceLocation.withDefaultNamespace("oak_planks"), false), "Reject unknown and non-condenser recipe IDs");
        var pos = h.absolutePos(POS); s.player().setPos(pos.getX() + 20, pos.getY(), pos.getZ());
        check(h, !s.menu().transferRecipe(s.player(), id("logs"), false), "Reject out-of-range requests");
        check(h, count(s, ForeverSkyblock.STONE_MEAL.get()) == 2 && count(s, Items.OAK_SAPLING) == 2
                && s.machine().inventory().getStackInSlot(0).isEmpty(), "Rejected requests never mutate either inventory");
        s.player().getInventory().clearContent();
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void fullInventoryRollbackAndSafeReplacement(GameTestHelper h) {
        var s = setup(h, false);
        for (int i = 0; i < 36; i++) s.player().getInventory().setItem(i, new ItemStack(Items.STONE, 64));
        s.player().getInventory().setItem(0, new ItemStack(Items.OAK_LOG, 2));
        s.machine().inventory().setStackInSlot(0, new ItemStack(Items.OAK_PLANKS, 4));
        check(h, CondenserTransfer.plan(s.menu(), recipe(h, "logs"), false).error() == CondenserTransfer.Error.SPACE, "Cannot displace old input into full inventory");
        check(h, !s.menu().transferRecipe(s.player(), id("logs"), false)
                && s.player().getInventory().getItem(0).getCount() == 2
                && s.machine().inventory().getStackInSlot(0).getCount() == 4, "Failed replacement rolls back all slots");
        s.player().getInventory().setItem(0, new ItemStack(Items.OAK_LOG));
        check(h, s.menu().transferRecipe(s.player(), id("logs"), false), "Taking the last ingredient frees room for old inputs");
        check(h, s.machine().inventory().getStackInSlot(0).is(Items.OAK_LOG) && count(s, Items.OAK_PLANKS) == 4, "Replace input without dropping or losing old contents");
        s.player().getInventory().clearContent();
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void hiddenInputAndItemComponentsArePreserved(GameTestHelper h) {
        var s = setup(h, true);
        var named = new ItemStack(Items.OAK_LOG, 2);
        named.set(net.minecraft.core.component.DataComponents.CUSTOM_NAME, net.minecraft.network.chat.Component.literal("Tagged log"));
        s.player().getInventory().setItem(0, named.copy());
        s.machine().inventory().setStackInSlot(2, new ItemStack(Items.OAK_SAPLING, 6));
        s.machine().setFilter(2, new ItemStack(Items.OAK_SAPLING));
        h.setBlock(POS.east(), Blocks.AIR);
        check(h, s.menu().transferRecipe(s.player(), id("logs"), false), "Wood transfer still works without chamber");
        check(h, ItemStack.isSameItemSameComponents(s.machine().inventory().getStackInSlot(0), named), "Transferred stack retains its components");
        check(h, s.machine().inventory().getStackInSlot(2).getCount() == 6 && s.machine().filter(2).is(Items.OAK_SAPLING), "Hidden secondary items and filter are untouched by one-input transfer");
        s.player().getInventory().clearContent();
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void chamberDoesNotCullAdjacentLogFaces(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        var state = h.getBlockState(POS);
        check(h, !state.canOcclude(), "Open chamber must not advertise a full occluding cube");
        for (Direction face : Direction.values()) {
            var adjacent = POS.relative(face);
            h.setBlock(adjacent, Blocks.OAK_LOG);
            var absolute = h.absolutePos(adjacent);
            check(h, Block.shouldRenderFace(h.getBlockState(adjacent), h.getLevel(), absolute, face.getOpposite(), h.absolutePos(POS)),
                    "Neighboring log faces remain visible against the open chamber");
        }
        h.succeed();
    }
}

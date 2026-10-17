package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import java.util.ArrayList;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.inventory.ClickType;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.entity.HopperBlockEntity;
import net.minecraft.world.phys.AABB;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;
import net.neoforged.neoforge.items.ItemStackHandler;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class MixingGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static CondenserBlockEntity machine(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(POS);
    }
    private static void module(GameTestHelper h) { h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get()); }
    private static ItemStack meal(int count) { return new ItemStack(ForeverSkyblock.STONE_MEAL.get(), count); }
    private static void check(GameTestHelper h, boolean value, String message) { h.assertTrue(value, message); }

    @GameTest(template = "empty")
    public static void adjacencyAndOpenMenuUpdate(GameTestHelper h) {
        var machine = machine(h);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        var menu = new CondenserMenu(1, player.getInventory(), machine);
        check(h, !menu.getSlot(2).isActive(), "Only one input initially");
        h.setBlock(POS.offset(1, 1, 0), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        check(h, !machine.hasModule(), "Diagonal module must not count");
        h.setBlock(POS.offset(1, 1, 0), Blocks.AIR);
        for (Direction face : Direction.values()) {
            h.setBlock(POS.relative(face), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
            check(h, machine.hasModule() && menu.getSlot(2).isActive(), "Each shared face activates the same open menu");
            h.setBlock(POS.relative(face), Blocks.AIR);
            check(h, !menu.getSlot(2).isActive(), "Removal disables the existing slot immediately");
        }
        module(h);
        h.setBlock(POS.west(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        check(h, menu.slots.size() == CondenserBlockEntity.MACHINE_SLOTS + 36
                && machine.inventory().getSlots() == CondenserBlockEntity.MACHINE_SLOTS,
                "Multiple Mixing Chambers never add more slots");
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 450)
    public static void swappedInputsAndRemovalKeepContents(GameTestHelper h) {
        var machine = machine(h);
        module(h);
        machine.inventory().insertItem(0, new ItemStack(Items.BIRCH_SAPLING, 2), false);
        machine.inventory().insertItem(2, meal(2), false);
        machine.setFilter(2, meal(1));
        h.runAfterDelay(60, () -> {
            h.setBlock(POS.east(), Blocks.AIR);
            check(h, machine.inventory().getStackInSlot(2).getCount() == 2, "Removal retains second input");
            var access = machine.automation(Direction.UP);
            check(h, access.getStackInSlot(2).isEmpty() && !access.insertItem(2, meal(1), false).isEmpty(), "Automation cannot see or insert hidden input");
        });
        h.runAfterDelay(230, () -> {
            check(h, machine.inventory().getStackInSlot(1).isEmpty() && machine.inventory().getStackInSlot(0).getCount() == 2,
                    "No dirt or consumption without module");
            check(h, machine.filter(2).is(ForeverSkyblock.STONE_MEAL.get()), "Hidden filter survives");
            module(h);
        });
        h.runAfterDelay(405, () -> {
            check(h, machine.inventory().getStackInSlot(1).is(Items.DIRT) && machine.inventory().getStackInSlot(1).getCount() == 1,
                    "Swapped input recipe resumes after reattachment");
            check(h, machine.inventory().getStackInSlot(0).getCount() == 1 && machine.inventory().getStackInSlot(2).getCount() == 1,
                    "Consume exactly one of each real ingredient");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void ghostFiltersCannotMoveOrCreateItems(GameTestHelper h) {
        var machine = machine(h);
        module(h);
        var player = FakePlayerFactory.getMinecraft(h.getLevel());
        player.getInventory().clearContent();
        var pos = h.absolutePos(POS);
        player.setPos(pos.getX() + 0.5, pos.getY(), pos.getZ() + 0.5);
        var menu = new CondenserMenu(1, player.getInventory(), machine);
        menu.setCarried(meal(7));
        check(h, !menu.setGhost(player, 0, meal(1)), "Editing required on server");
        menu.clickMenuButton(player, 0);
        check(h, menu.setGhost(player, 0, menu.getCarried()), "Carried item sets ghost");
        check(h, menu.setGhost(player, 2, new ItemStack(Items.OAK_SAPLING)), "JEI-style item sets second ghost");
        check(h, menu.getCarried().getCount() == 7 && machine.inventory().getStackInSlot(0).isEmpty()
                && machine.inventory().getStackInSlot(2).isEmpty(), "Ghosts neither consume carried stack nor become real inputs");
        menu.clicked(0, 0, ClickType.PICKUP, player);
        check(h, menu.getCarried().getCount() == 7 && machine.inventory().getStackInSlot(0).isEmpty(), "Normal input click blocked during edit");
        menu.clickMenuButton(player, 0);
        check(h, !menu.getSlot(0).mayPlace(new ItemStack(Items.OAK_LOG)), "GUI rejects incorrect filtered item");
        var access = machine.automation(Direction.NORTH);
        check(h, !access.insertItem(0, new ItemStack(Items.OAK_LOG), false).isEmpty(), "Pipe rejects incorrect filtered item");
        check(h, access.insertItem(0, meal(2), true).isEmpty() && machine.inventory().getStackInSlot(0).isEmpty(), "Filtered simulation has no mutation");
        access.insertItem(0, meal(2), false);
        menu.clickMenuButton(player, 0);
        check(h, !menu.setGhost(player, 0, new ItemStack(Items.OAK_LOG)), "Cannot filter away a different real stack");
        check(h, machine.inventory().getStackInSlot(0).getCount() == 2, "Rejected filter preserves real contents");
        check(h, menu.setGhost(player, 0, ItemStack.EMPTY) && machine.filter(0).isEmpty(), "Clear removes filter only");
        h.setBlock(POS.east(), Blocks.AIR);
        check(h, !menu.setGhost(player, 2, ItemStack.EMPTY), "Hidden input cannot be edited");
        menu.setCarried(ItemStack.EMPTY);
        player.getInventory().clearContent();
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 320)
    public static void filteredHoppersMakeDirt(GameTestHelper h) {
        var machine = machine(h);
        module(h);
        machine.setFilter(0, meal(1));
        machine.setFilter(2, new ItemStack(Items.OAK_SAPLING));
        h.setBlock(POS.above(), Blocks.HOPPER);
        h.setBlock(POS.below(), Blocks.HOPPER);
        var top = (HopperBlockEntity) h.getBlockEntity(POS.above());
        var bottom = (HopperBlockEntity) h.getBlockEntity(POS.below());
        top.setItem(0, new ItemStack(Items.OAK_SAPLING));
        top.setItem(1, meal(1));
        h.runAfterDelay(270, () -> {
            int count = 0;
            for (int i = 0; i < bottom.getContainerSize(); i++) {
                var stack = bottom.getItem(i);
                check(h, stack.isEmpty() || stack.is(Items.DIRT), "Only actual dirt is extracted");
                count += stack.getCount();
            }
            check(h, top.isEmpty() && count == 1 && machine.inventory().getStackInSlot(1).isEmpty(), "Hoppers route different filtered inputs and extract one dirt");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void legacyInventoryFiltersAndHiddenDrops(GameTestHelper h) {
        var machine = machine(h);
        var registries = h.getLevel().registryAccess();
        var legacy = new ItemStackHandler(2);
        legacy.setStackInSlot(0, new ItemStack(Items.OAK_LOG, 3));
        legacy.setStackInSlot(1, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 5));
        var tag = machine.saveWithFullMetadata(registries);
        tag.put("Inventory", legacy.serializeNBT(registries));
        machine.loadWithComponents(tag, registries);
        check(h, machine.inventory().getSlots() == CondenserBlockEntity.MACHINE_SLOTS
                && machine.inventory().getStackInSlot(0).getCount() == 3
                && machine.inventory().getStackInSlot(1).getCount() == 5
                && machine.inventory().getStackInSlot(2).isEmpty()
                && machine.inventory().getStackInSlot(3).isEmpty()
                && machine.inventory().getStackInSlot(4).isEmpty(),
                "Legacy output remains output and all appended module slots start empty");
        module(h);
        machine.setFilter(0, new ItemStack(Items.OAK_LOG));
        machine.setFilter(2, new ItemStack(Items.OAK_SAPLING));
        machine.inventory().insertItem(2, new ItemStack(Items.OAK_SAPLING, 2), false);
        var saved = machine.saveWithFullMetadata(registries);
        var restored = new CondenserBlockEntity(h.absolutePos(POS), machine.getBlockState());
        restored.loadWithComponents(saved, registries);
        h.getLevel().setBlockEntity(restored);
        check(h, restored.filter(0).is(Items.OAK_LOG) && restored.filter(2).is(Items.OAK_SAPLING), "Both filters survive save/reload");
        h.setBlock(POS.east(), Blocks.AIR);
        h.getLevel().destroyBlock(h.absolutePos(POS), false);
        int saplings = 0, logs = 0, fragments = 0;
        for (var entity : h.getLevel().getEntitiesOfClass(ItemEntity.class, new AABB(h.absolutePos(POS)).inflate(1))) {
            var stack = entity.getItem();
            if (stack.is(Items.OAK_SAPLING)) saplings += stack.getCount();
            if (stack.is(Items.OAK_LOG)) logs += stack.getCount();
            if (stack.is(ForeverSkyblock.STONE_FRAGMENT.get())) fragments += stack.getCount();
        }
        check(h, saplings == 2 && logs == 3 && fragments == 5, "Breaking drops hidden contents and never drops ghosts");
        h.succeed();
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void blockedDirtAndSingleInputFallback(GameTestHelper h) {
        var machine = machine(h);
        module(h);
        machine.inventory().insertItem(0, meal(1), false);
        machine.inventory().insertItem(2, new ItemStack(Items.OAK_SAPLING), false);
        machine.inventory().setStackInSlot(1, new ItemStack(Items.DIRT, 64));
        h.runAfterDelay(50, () -> {
            check(h, machine.menuData().get(2) == CondenserBlockEntity.BLOCKED && machine.inventory().getStackInSlot(0).getCount() == 1
                    && machine.inventory().getStackInSlot(2).getCount() == 1, "Full output consumes neither ingredient");
            h.setBlock(POS.east(), Blocks.AIR);
            machine.inventory().setStackInSlot(0, new ItemStack(Items.OAK_LOG));
            machine.inventory().setStackInSlot(1, ItemStack.EMPTY);
        });
        h.runAfterDelay(220, () -> {
            check(h, machine.inventory().getStackInSlot(1).getCount() == 4 && machine.inventory().getStackInSlot(2).getCount() == 1,
                    "Single-input wood processing works without module and retains hidden input");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void mortarRemainderAndRecipeNetworkRoundTrip(GameTestHelper h) {
        var stacks = new ArrayList<ItemStack>();
        stacks.add(new ItemStack(ForeverSkyblock.MORTAR.get()));
        for (int i = 0; i < 4; i++) stacks.add(new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get()));
        for (int i = 0; i < 4; i++) stacks.add(ItemStack.EMPTY);
        var input = CraftingInput.of(3, 3, stacks);
        var recipe = h.getLevel().getRecipeManager().getRecipeFor(RecipeType.CRAFTING, input, h.getLevel()).orElseThrow().value();
        check(h, recipe.assemble(input, h.getLevel().registryAccess()).is(ForeverSkyblock.STONE_MEAL.get()), "Four fragments and mortar yield meal");
        var remains = recipe.getRemainingItems(input);
        int mortars = 0;
        for (var stack : remains) { if (stack.is(ForeverSkyblock.MORTAR.get())) mortars += stack.getCount(); else check(h, stack.isEmpty(), "No fragment remainders"); }
        check(h, mortars == 1, "Exactly one undamaged reusable mortar remains");
        var codec = ForeverSkyblock.CONDENSING_SERIALIZER.get().streamCodec();
        for (var holder : h.getLevel().getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get())) {
            var buffer = new net.minecraft.network.RegistryFriendlyByteBuf(io.netty.buffer.Unpooled.buffer(), h.getLevel().registryAccess());
            try {
                codec.encode(buffer, holder.value());
                var decoded = codec.decode(buffer);
                check(h, ItemStack.isSameItemSameComponents(decoded.result(), holder.value().result())
                        && decoded.result().getCount() == holder.value().result().getCount()
                        && decoded.requiresModule() == holder.value().requiresModule(), "Legacy and mixing recipe outputs survive network codec");
            } finally { buffer.release(); }
        }
        var dirt = h.getLevel().getRecipeManager().getRecipeFor(ForeverSkyblock.CONDENSING.get(), new CondenserInput(meal(1), new ItemStack(Items.OAK_SAPLING)), h.getLevel());
        check(h, dirt.isPresent() && dirt.get().value().requiresModule(), "Dirt recipe registered with two real inputs");
        h.succeed();
    }
}

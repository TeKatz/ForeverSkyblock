package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import de.tekatz.foreverskyblock.recipe.PackRecipes;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.BlockPos;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraft.world.item.crafting.RecipeType;
import static de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity.*;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class IronBalanceGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    private static CondenserBlockEntity machine(GameTestHelper h, BlockPos pos, boolean tierTwo) {
        h.setBlock(pos, tierTwo ? ForeverSkyblock.CONDENSER_MARK_II.get() : ForeverSkyblock.CONDENSER_MARK_I.get());
        return (CondenserBlockEntity) h.getBlockEntity(pos);
    }

    @GameTest(template = "empty", timeoutTicks = 150)
    public static void bothTiersCrushLogsInSixSeconds(GameTestHelper h) {
        var first = machine(h, POS, false);
        var second = machine(h, POS.south(2), true);
        for (var m : List.of(first, second)) m.inventory().insertItem(INPUT, new ItemStack(Items.OAK_LOG, 2), false);
        h.runAfterDelay(110, () -> {
            for (var m : List.of(first, second)) h.assertTrue(m.menuData().get(1) == 120
                    && m.inventory().getStackInSlot(OUTPUT).isEmpty(), "Six-second logs must not finish early");
        });
        h.runAfterDelay(130, () -> {
            for (var m : List.of(first, second)) {
                h.assertTrue(m.inventory().getStackInSlot(OUTPUT).is(ForeverSkyblock.STONE_FRAGMENT.get())
                        && m.inventory().getStackInSlot(OUTPUT).getCount() == 4, "Each tier produces exactly four fragments at six seconds");
                h.assertTrue(m.inventory().getStackInSlot(INPUT).getCount() == 1, "Each completed cycle consumes one log");
            }
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 240)
    public static void packedMudWorksOnBothTiersWithSwappedInputsAndRequiresMixing(GameTestHelper h) {
        var first = machine(h, POS, false);
        var second = machine(h, POS.south(2), true);
        first.inventory().setStackInSlot(INPUT, new ItemStack(Items.MUD, 2));
        first.inventory().setStackInSlot(SECOND_INPUT, new ItemStack(Items.WHEAT, 2));
        second.inventory().setStackInSlot(INPUT, new ItemStack(Items.WHEAT, 2));
        second.inventory().setStackInSlot(SECOND_INPUT, new ItemStack(Items.MUD, 2));
        h.runAfterDelay(20, () -> {
            for (var m : List.of(first, second)) h.assertTrue(m.menuData().get(0) == 0
                    && m.inventory().getStackInSlot(OUTPUT).isEmpty(), "Packed Mud must require the second input's Mixing Chamber");
            h.setBlock(POS.east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
            h.setBlock(POS.south(2).east(), ForeverSkyblock.MIXING_CHAMBER_MARK_I.get());
        });
        h.runAfterDelay(150, () -> {
            for (var m : List.of(first, second)) h.assertTrue(m.menuData().get(1) == 160
                    && m.inventory().getStackInSlot(OUTPUT).isEmpty(), "Packed Mud uses an eight-second cycle");
        });
        h.runAfterDelay(200, () -> {
            for (var m : List.of(first, second)) {
                h.assertTrue(m.inventory().getStackInSlot(OUTPUT).is(Items.PACKED_MUD)
                        && m.inventory().getStackInSlot(OUTPUT).getCount() == 1, "Mud and Wheat produce one Packed Mud on both tiers");
                h.assertTrue(m.inventory().getStackInSlot(INPUT).getCount() == 1
                        && m.inventory().getStackInSlot(SECOND_INPUT).getCount() == 1, "Exactly one of each ingredient is consumed, including reversed order");
            }
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void packedMudIsInTheJeiRecipeSourceAndNetworkRoundTrips(GameTestHelper h) {
        var holder = h.getLevel().getRecipeManager().getAllRecipesFor(ForeverSkyblock.CONDENSING.get()).stream()
                .filter(r -> r.id().equals(ResourceLocation.parse("forever_skyblock:condensing/packed_mud"))).findFirst().orElseThrow();
        var recipe = holder.value();
        h.assertTrue(recipe.requiresModule() && recipe.minimumTier() == 1 && recipe.availableToTier(2),
                "JEI's existing registered Condensing source must expose Packed Mud for both tiers and show Mixing as required");
        var bytes = new RegistryFriendlyByteBuf(io.netty.buffer.Unpooled.buffer(), h.getLevel().registryAccess());
        try {
            var codec = new CondenserRecipe.Serializer().streamCodec();
            codec.encode(bytes, recipe);
            var copy = codec.decode(bytes);
            h.assertTrue(copy.processingTime() == 160 && copy.result().is(Items.PACKED_MUD) && copy.result().getCount() == 1
                    && copy.matches(new CondenserInput(new ItemStack(Items.WHEAT), new ItemStack(Items.MUD)), h.getLevel()),
                    "Recipe synchronization preserves the visible duration, output and reversible inputs");
        } finally { bytes.release(); }
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void ironHopperRequiresBlocksAndCannotRegainCheapAlternatives(GameTestHelper h) {
        var manager = h.getLevel().getRecipeManager();
        var ingot = new ItemStack(Items.IRON_INGOT);
        var block = new ItemStack(Items.IRON_BLOCK);
        var chest = new ItemStack(Items.CHEST);
        var input = CraftingInput.of(3, 3, List.of(ingot, ItemStack.EMPTY, ingot, block, chest, block, ItemStack.EMPTY, ingot, ItemStack.EMPTY));
        var cheap = CraftingInput.of(3, 3, List.of(ingot, ItemStack.EMPTY, ingot, ingot, chest, ingot, ItemStack.EMPTY, ingot, ItemStack.EMPTY));
        var match = manager.getRecipeFor(RecipeType.CRAFTING, input, h.getLevel()).orElseThrow();
        h.assertTrue(match.id().equals(PackRecipes.IRON_HOPPER_RECIPE)
                && match.value().assemble(input, h.getLevel().registryAccess()).is(Items.HOPPER), "Two Blocks, three Ingots and a Chest craft the normal Hopper");
        h.assertTrue(match.value().getRemainingItems(input).stream().allMatch(ItemStack::isEmpty), "Hopper ingredients are consumed");
        h.assertTrue(manager.byKey(ResourceLocation.withDefaultNamespace("hopper")).isEmpty(), "Original Vanilla/NeoForge recipe is removed");
        h.assertTrue(manager.getRecipesFor(RecipeType.CRAFTING, cheap, h.getLevel()).stream().noneMatch(r ->
                r.value().getResultItem(h.getLevel().registryAccess()).is(Items.HOPPER)), "The previous five-ingot pattern cannot craft a Hopper");
        var all = new ArrayList<RecipeHolder<?>>(manager.getRecipes());
        all.add(new RecipeHolder<>(ResourceLocation.parse("test:cheap_hopper"), match.value()));
        var reloaded = new RecipeManager(h.getLevel().registryAccess());
        reloaded.replaceRecipes(all);
        h.assertTrue(reloaded.byKey(ResourceLocation.parse("test:cheap_hopper")).isEmpty()
                && reloaded.byKey(PackRecipes.IRON_HOPPER_RECIPE).isPresent(), "Reload/replacement cannot restore competing Hopper recipes");
        h.succeed();
    }
}

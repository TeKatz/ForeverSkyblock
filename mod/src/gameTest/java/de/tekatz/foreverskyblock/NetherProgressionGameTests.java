package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Blocks;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class NetherProgressionGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);

    private static void check(GameTestHelper h, boolean condition, String message) { h.assertTrue(condition, message); }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void markTwoCrushesNetherFortressBricks(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        var machine = (CondenserBlockEntity) h.getBlockEntity(POS);
        machine.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.NETHER_BRICKS), false);
        h.runAfterDelay(180, () -> {
            ItemStack output = machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT);
            check(h, output.is(ForeverSkyblock.NETHER_BRICK_DUST.get()) && output.getCount() == 1,
                    "Condenser Mark II must crush Nether Bricks into Nether Brick Dust");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void markOneCannotCrushNetherFortressBricks(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_I.get());
        var machine = (CondenserBlockEntity) h.getBlockEntity(POS);
        machine.inventory().insertItem(CondenserBlockEntity.INPUT, new ItemStack(Items.NETHER_BRICKS), false);
        h.runAfterDelay(180, () -> {
            check(h, machine.inventory().getStackInSlot(CondenserBlockEntity.OUTPUT).isEmpty(),
                    "Nether Brick Dust must remain a Mark II-only recipe");
            h.succeed();
        });
    }

    @GameTest(template = "empty", timeoutTicks = 230)
    public static void netherDustWashingProducesConcentrateAndQuartz(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.WASHING_BASIN.get());
        h.setBlock(POS.east(), Blocks.WATER);
        var basin = (WashingBasinBlockEntity) h.getBlockEntity(POS);
        basin.inventory().insertItem(WashingBasinBlockEntity.INPUT, new ItemStack(ForeverSkyblock.NETHER_BRICK_DUST.get()), false);
        h.runAfterDelay(180, () -> {
            ItemStack primary = basin.inventory().getStackInSlot(WashingBasinBlockEntity.OUTPUT);
            ItemStack secondary = basin.inventory().getStackInSlot(WashingBasinBlockEntity.SECONDARY_OUTPUT);
            check(h, primary.is(ForeverSkyblock.NETHER_CONCENTRATE.get()) && primary.getCount() == 1,
                    "Nether Brick Dust washing must produce one Nether Concentrate");
            check(h, secondary.is(Items.QUARTZ) && secondary.getCount() == 2,
                    "Nether Brick Dust washing must produce two Quartz");
            h.succeed();
        });
    }

    @GameTest(template = "empty")
    public static void netherDustRecipeKeepsTwentyPercentGoldBonus(GameTestHelper h) {
        var recipe = h.getLevel().getRecipeManager().getAllRecipesFor(ForeverSkyblock.WASHING.get()).stream()
                .filter(holder -> holder.id().equals(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "washing/nether_brick_dust")))
                .findFirst().orElse(null);
        check(h, recipe != null, "Nether Brick Dust washing recipe must be loaded");
        check(h, recipe.value().bonusChancePercent() == 20, "Nether Brick Dust must have a 20 percent bonus roll");
        check(h, recipe.value().bonusResult().is(Items.GOLD_NUGGET) && recipe.value().bonusResult().getCount() == 1,
                "Nether Brick Dust bonus must be one Gold Nugget");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void secondWashingOutputIsAutomatable(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.WASHING_BASIN.get());
        var basin = (WashingBasinBlockEntity) h.getBlockEntity(POS);
        basin.inventory().setStackInSlot(WashingBasinBlockEntity.OUTPUT, new ItemStack(ForeverSkyblock.NETHER_CONCENTRATE.get()));
        basin.inventory().setStackInSlot(WashingBasinBlockEntity.SECONDARY_OUTPUT, new ItemStack(Items.QUARTZ, 2));
        var bottom = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), Direction.DOWN);
        check(h, bottom != null, "Washing Basin bottom must expose item automation");
        check(h, bottom.extractItem(WashingBasinBlockEntity.OUTPUT, 1, false).is(ForeverSkyblock.NETHER_CONCENTRATE.get()),
                "Primary Washing output must extract from below");
        check(h, bottom.extractItem(WashingBasinBlockEntity.SECONDARY_OUTPUT, 2, false).getCount() == 2,
                "Secondary Washing output must extract from below");
        h.succeed();
    }
}

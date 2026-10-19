package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import static de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity.*;
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
public final class GardenWheatGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);
    @GameTest(template = "empty", timeoutTicks = 760)
    public static void wheatKeepsSeedAndStopsForEitherFullHarvest(GameTestHelper h) {
        h.setBlock(POS, ForeverSkyblock.CONDENSER_MARK_II.get());
        h.setBlock(POS.north(), ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get());
        var m = (CondenserBlockEntity) h.getBlockEntity(POS);
        h.assertTrue(m.inventory().insertItem(GARDEN_SPECIMEN, new ItemStack(Items.WHEAT_SEEDS, 2), false).getCount() == 1,
                "Seed specimen must be accepted with a one-item limit");
        h.runAfterDelay(340, () -> {
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_SPECIMEN).is(Items.WHEAT_SEEDS) && m.inventory().getStackInSlot(GARDEN_SPECIMEN).getCount() == 1,
                    "Wheat must retain its seed specimen");
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).is(Items.WHEAT) && m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).getCount() == 4,
                    "One cycle produces four Wheat");
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).is(Items.WHEAT_SEEDS) && m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).getCount() == 2,
                    "One cycle produces two harvest Seeds");
            m.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, new ItemStack(Items.WHEAT_SEEDS, 64));
            h.runAfterDelay(3, () -> {
                int progress = m.menuData().get(15);
                h.assertTrue(m.menuData().get(17) == CondenserBlockEntity.BLOCKED, "Full seed output blocks growth");
                h.runAfterDelay(10, () -> {
                    h.assertTrue(m.menuData().get(15) == progress, "Blocked crop retains its stage");
                    m.inventory().setStackInSlot(GARDEN_OUTPUT_SECONDARY, ItemStack.EMPTY);
                    m.inventory().setStackInSlot(GARDEN_OUTPUT_PRIMARY, new ItemStack(Items.WHEAT, 64));
                    h.runAfterDelay(3, () -> {
                        h.assertTrue(m.menuData().get(17) == CondenserBlockEntity.BLOCKED, "Full wheat output also blocks growth");
                        m.inventory().setStackInSlot(GARDEN_OUTPUT_PRIMARY, ItemStack.EMPTY);
                    });
                });
            });
        });
        h.runAfterDelay(710, () -> {
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_OUTPUT_PRIMARY).getCount() == 4 && m.inventory().getStackInSlot(GARDEN_OUTPUT_SECONDARY).getCount() == 2,
                    "Unblocking resumes a complete crop cycle without duplicate harvest");
            h.setBlock(POS.north(), Blocks.AIR);
            h.assertTrue(m.inventory().getStackInSlot(GARDEN_SPECIMEN).is(Items.WHEAT_SEEDS), "Detaching preserves the specimen");
            h.succeed();
        });
    }
}

package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.block.CrucibleBlock;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.neoforged.fml.ModList;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.fluids.capability.IFluidHandler;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class CrucibleGameTests {
    private static final BlockPos POS = new BlockPos(2, 2, 2);

    private static CrucibleBlockEntity setup(GameTestHelper h, Block heat) {
        h.setBlock(POS, ForeverSkyblock.CRUCIBLE.get());
        h.setBlock(POS.below(), heat);
        return (CrucibleBlockEntity) h.getBlockEntity(POS);
    }

    private static void tick(GameTestHelper h, CrucibleBlockEntity crucible, int ticks) {
        for (int i = 0; i < ticks; i++) CrucibleBlockEntity.tick(h.getLevel(), h.absolutePos(POS), h.getBlockState(POS), crucible);
    }

    private static void check(GameTestHelper h, boolean value, String message) { h.assertTrue(value, message); }

    @GameTest(template = "empty")
    public static void heatTiersKeepTorchIntentionallySlow(GameTestHelper h) {
        check(h, CrucibleBlockEntity.durationFor(CrucibleBlockEntity.HEAT_TORCH) == 6000, "Torch takes five minutes per bucket");
        check(h, CrucibleBlockEntity.durationFor(CrucibleBlockEntity.HEAT_CAMPFIRE) == 1600, "Campfire improves the primitive tier");
        check(h, CrucibleBlockEntity.durationFor(CrucibleBlockEntity.HEAT_LAVA) == 600, "Lava heat takes thirty seconds");
        check(h, CrucibleBlockEntity.durationFor(CrucibleBlockEntity.HEAT_SUPERHEATING) == 160, "Superheating element is the final eight-second tier");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void crucibleHasStablePlayerFacingName(GameTestHelper h) {
        check(h, "Crucible".equals(ForeverSkyblock.CRUCIBLE.get().getName().getString()), "Crucible uses the intended player-facing name");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void lavaHeatMeltsEightCobbleAndUpdatesVisibleFill(GameTestHelper h) {
        CrucibleBlockEntity crucible = setup(h, Blocks.LAVA);
        crucible.inventory().setStackInSlot(CrucibleBlockEntity.INPUT, new ItemStack(Items.COBBLESTONE, 9));
        tick(h, crucible, 600);
        check(h, crucible.lavaMb() == 1000, "Eight cobblestone create one bucket of lava");
        check(h, crucible.inventory().getStackInSlot(CrucibleBlockEntity.INPUT).getCount() == 1, "Exactly eight cobblestone are consumed");
        check(h, h.getBlockState(POS).getValue(CrucibleBlock.FILL) == 2, "One of four buckets advances the visible fill to two of eight stages");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void fortyFragmentsCreateOneHundredMb(GameTestHelper h) {
        CrucibleBlockEntity crucible = setup(h, Blocks.LAVA);
        crucible.inventory().setStackInSlot(CrucibleBlockEntity.INPUT, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), 41));
        tick(h, crucible, 600);
        check(h, crucible.lavaMb() == 100, "Forty fragments create 100 mB of lava");
        check(h, crucible.inventory().getStackInSlot(CrucibleBlockEntity.INPUT).getCount() == 1, "Exactly forty fragments are consumed");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void freezingChamberShowsProgressBeforeProducingObsidian(GameTestHelper h) {
        CrucibleBlockEntity crucible = setup(h, Blocks.LAVA);
        h.setBlock(POS.east(), ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get());
        crucible.inventory().setStackInSlot(CrucibleBlockEntity.INPUT, new ItemStack(Items.COBBLESTONE, 8));
        tick(h, crucible, 650);
        check(h, crucible.hasFreezer(), "Adjacent Freezing Chamber is detected");
        check(h, crucible.mode() == CrucibleBlockEntity.MODE_OBSIDIAN, "Freezing-only setup selects Obsidian route");
        check(h, crucible.lavaMb() == 1000, "Stored lava remains visible while freezing is in progress");
        check(h, crucible.freezeProgress() == 50, "Obsidian conversion exposes its real halfway progress");
        check(h, crucible.inventory().getStackInSlot(CrucibleBlockEntity.OUTPUT).isEmpty(), "Obsidian is not produced before freezing completes");
        tick(h, crucible, 50);
        check(h, crucible.lavaMb() == 0, "One bucket is consumed after freezing completes");
        check(h, crucible.freezeProgress() == 0, "Freezing progress resets after completion");
        check(h, crucible.inventory().getStackInSlot(CrucibleBlockEntity.OUTPUT).is(Items.OBSIDIAN), "Freezing produces one Obsidian");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void itemAndFluidCapabilitiesRespectAutomationDirections(GameTestHelper h) {
        CrucibleBlockEntity crucible = setup(h, Blocks.LAVA);
        var top = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), Direction.UP);
        var bottom = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, h.absolutePos(POS), Direction.DOWN);
        check(h, top != null && bottom != null, "Crucible keeps registered sided item capabilities");
        ItemStack remainder = top.insertItem(CrucibleBlockEntity.INPUT, new ItemStack(Items.COBBLESTONE, 8), false);
        check(h, remainder.isEmpty(), "Top/side automation accepts Crucible input");
        check(h, bottom.insertItem(CrucibleBlockEntity.INPUT, new ItemStack(Items.COBBLESTONE), false).getCount() == 1,
                "Bottom automation is output-only");
        tick(h, crucible, 600);
        crucible.inventory().setStackInSlot(CrucibleBlockEntity.OUTPUT, new ItemStack(Items.OBSIDIAN));
        check(h, top.extractItem(CrucibleBlockEntity.OUTPUT, 1, false).isEmpty()
                && bottom.extractItem(CrucibleBlockEntity.OUTPUT, 1, false).is(Items.OBSIDIAN), "Crucible output remains accessible only through its output view");
        var fluid = h.getLevel().getCapability(Capabilities.FluidHandler.BLOCK, h.absolutePos(POS), Direction.NORTH);
        check(h, fluid != null, "Crucible keeps its registered fluid capability");
        var drained = fluid.drain(250, IFluidHandler.FluidAction.EXECUTE);
        check(h, drained.getAmount() == 250 && drained.getFluid() == net.minecraft.world.level.material.Fluids.LAVA, "Fluid cables can drain stored lava");
        check(h, crucible.lavaMb() == 750, "Fluid extraction updates the internal buffer");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void bothTargetsExposeExplicitModeToggleWhenMekanismIsLoaded(GameTestHelper h) {
        if (!ModList.get().isLoaded("mekanism")) { h.succeed(); return; }
        Block tank = BuiltInRegistries.BLOCK.get(ResourceLocation.fromNamespaceAndPath("mekanism", "basic_fluid_tank"));
        check(h, tank != Blocks.AIR, "Mekanism Basic Fluid Tank is registered in the pack runtime");
        CrucibleBlockEntity crucible = setup(h, Blocks.LAVA);
        h.setBlock(POS.east(), ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get());
        h.setBlock(POS.west(), tank);
        tick(h, crucible, 1);
        check(h, crucible.hasTank() && crucible.hasFreezer(), "Crucible sees both output targets around itself");
        int before = crucible.mode();
        crucible.toggleMode();
        check(h, crucible.mode() != before, "Both targets allow explicit Lava/Obsidian switching");
        h.succeed();
    }
}

package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.blockentity.MachineVisualState;
import de.tekatz.foreverskyblock.blockentity.RefiningChamberBlockEntity;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import net.minecraft.core.BlockPos;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class MachinePresentationGameTests {
    @GameTest(template="empty")
    public static void observerInterpolatesOnlyRunningWorkAndNeverInventsAnotherCycle(GameTestHelper helper) {
        MachineVisualState visual=new MachineVisualState();CompoundTag tag=new CompoundTag();
        tag.putInt("Status",1);tag.putInt("Progress",30);tag.putInt("Duration",100);
        visual.read(tag,100);
        helper.assertTrue(visual.progress(110,.5F)==40.5F,"Running observer must interpolate partial ticks");
        helper.assertTrue(visual.progress(200,0)==50F,"Stale snapshots may extrapolate at most one heartbeat");
        tag.putInt("Status",2);visual.read(tag,100);
        helper.assertTrue(visual.progress(200,.9F)==30F,"Blocked work must freeze at its observed pose");
        tag.putInt("Status",3);visual.read(tag,100);
        helper.assertTrue(visual.progress(200,.9F)==30F,"Missing power or water must freeze the pose");
        tag.putInt("Status",1);tag.putInt("Progress",98);visual.read(tag,100);
        helper.assertTrue(visual.phase(200,0)==1F,"Completion must clamp instead of starting a fake new cycle");
        helper.succeed();
    }
    @GameTest(template="empty")
    public static void presentationPacketsCannotReplaceStoredInventoryOrEnergy(GameTestHelper helper) {
        var registries=helper.getLevel().registryAccess();
        var refinery=new RefiningChamberBlockEntity(BlockPos.ZERO,ForeverSkyblock.REFINING_CHAMBER.get().defaultBlockState());
        refinery.inventory().setStackInSlot(0,new ItemStack(Items.CHARCOAL,12));
        refinery.energyAutomation(null).receiveEnergy(4000,false);
        var saved=refinery.saveWithoutMetadata(registries);
        CompoundTag packet=new CompoundTag();packet.putInt("Status",1);packet.putInt("Duration",160);packet.putInt("Progress",50);
        refinery.handleUpdateTag(packet,registries);
        helper.assertTrue(saved.equals(refinery.saveWithoutMetadata(registries)),"Visual packet must not mutate persisted refinery state");
        helper.assertTrue(!refinery.getUpdateTag(registries).contains("Inventory"),"Viewer packet must not carry gameplay inventory");
        var basin=new WashingBasinBlockEntity(BlockPos.ZERO,ForeverSkyblock.WASHING_BASIN.get().defaultBlockState());
        basin.inventory().setStackInSlot(0,new ItemStack(Items.SAND,9));
        saved=basin.saveWithoutMetadata(registries);basin.handleUpdateTag(packet,registries);
        helper.assertTrue(saved.equals(basin.saveWithoutMetadata(registries)),"Visual packet must not mutate persisted basin state");
        helper.succeed();
    }
}

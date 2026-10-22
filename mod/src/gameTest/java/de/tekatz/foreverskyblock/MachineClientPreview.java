package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.*;
import de.tekatz.foreverskyblock.menu.*;
import de.tekatz.foreverskyblock.blockentity.*;
import java.lang.reflect.Field;
import net.minecraft.client.Minecraft;
import net.minecraft.client.Screenshot;
import net.minecraft.client.gui.screens.TitleScreen;
import net.minecraft.core.registries.Registries;
import net.minecraft.world.Difficulty;
import net.minecraft.world.inventory.AbstractContainerMenu;
import net.minecraft.world.inventory.ContainerData;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.*;
import net.minecraft.world.level.levelgen.WorldOptions;
import net.minecraft.world.level.levelgen.presets.WorldPresets;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.ClientTickEvent;

/** Opt-in rendered machine fixtures in a disposable integrated world; excluded from the shipped JAR. */
@EventBusSubscriber(modid=ForeverSkyblock.MOD_ID,value=Dist.CLIENT)
public final class MachineClientPreview {
    private static boolean started;
    private static int ticks,stage=-1;
    @SubscribeEvent public static void tick(ClientTickEvent.Post event) throws Exception {
        if(!Boolean.getBoolean("forever_skyblock.machineVisualTests"))return;
        var mc=Minecraft.getInstance();
        if(!started) {
            if(mc.screen!=null&&mc.screen.getClass().getSimpleName().equals("AccessibilityOnboardingScreen"))mc.setScreen(new TitleScreen());
            if(!(mc.screen instanceof TitleScreen)||mc.getOverlay()!=null)return;
            started=true;mc.options.guiScale().set(3);mc.options.pauseOnLostFocus=false;mc.resizeDisplay();
            mc.createWorldOpenFlows().createFreshLevel("machine-visual-qa-"+System.currentTimeMillis(),
                    new LevelSettings("Machine visual QA",GameType.CREATIVE,false,Difficulty.PEACEFUL,true,new GameRules(),WorldDataConfiguration.DEFAULT),
                    new WorldOptions(9425L,false,false),
                    registries->registries.registryOrThrow(Registries.WORLD_PRESET).getHolderOrThrow(WorldPresets.FLAT).value().createWorldDimensions(),new TitleScreen());
            return;
        }
        if(mc.level==null||mc.player==null||mc.getOverlay()!=null)return;
        if(++ticks % (stage>=16?100:16)!=0)return;
        if(stage>=0) {
            Screenshot.grab(mc.gameDirectory,"machine-"+stage+".png",mc.getMainRenderTarget(),c->{});
            System.out.println("MACHINE_VISUAL_STAGE "+stage+" "+(mc.screen==null?"world":mc.screen.getClass().getSimpleName()));
        }
        if(++stage>=18) {System.out.println("MACHINE_VISUAL_COMPLETE");mc.stop();return;}
        mc.options.guiScale().set(stage==14?2:stage==15?4:3);mc.resizeDisplay();
        if(stage>=16) {
            mc.setScreen(null);mc.options.hideGui=true;
            if(stage==16)gallery(mc);
            return;
        }
        var inventory=mc.player.getInventory();
        inventory.setItem(9,new ItemStack(Items.OAK_LOG,64));inventory.setItem(10,new ItemStack(Items.IRON_INGOT,32));
        inventory.setItem(0,new ItemStack(ForeverSkyblock.MORTAR.get()));
        if(stage<=5||stage>=14) {
            var menu=new CondenserMenu(0,inventory);
            data(menu,0,60);data(menu,1,120);data(menu,2,1);
            // Remaining module/status fields are explicitly named below rather than relying on undocumented indices.
            setCondenserData(menu,stage);
            menu.getSlot(CondenserBlockEntity.INPUT).set(new ItemStack(Items.OAK_LOG,32));
            if(stage==2) {menu.getSlot(CondenserBlockEntity.INPUT).set(new ItemStack(ForeverSkyblock.STONE_MEAL.get(),32)); menu.getSlot(CondenserBlockEntity.SECOND_INPUT).set(new ItemStack(Items.DIRT,16));}
            menu.getSlot(CondenserBlockEntity.OUTPUT).set(new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(),48));
            if(stage==2)menu.getSlot(CondenserBlockEntity.OUTPUT).set(new ItemStack(Items.MUD,16));
            menu.getSlot(CondenserBlockEntity.GARDEN_SPECIMEN).set(new ItemStack(stage==5?Items.WHEAT_SEEDS:Items.OAK_SAPLING));
            mc.player.containerMenu=menu;mc.setScreen(new CondenserScreen(menu,inventory,menuTitle("Condenser")));
        } else if(stage<=8) {
            var menu=new WashingMenu(0,inventory);data(menu,0,70);data(menu,1,160);data(menu,2,stage==7?3:stage==8?2:1);data(menu,3,stage==7?0:1);
            menu.getSlot(0).set(new ItemStack(ForeverSkyblock.STONE_MEAL.get(),32));menu.getSlot(1).set(new ItemStack(Items.RAW_IRON,8));
            mc.player.containerMenu=menu;mc.setScreen(new WashingScreen(menu,inventory,menuTitle("Washing Basin")));
        } else if(stage<=10) {
            var menu=new RefiningMenu(0,inventory);data(menu,0,80);data(menu,1,160);data(menu,2,stage==10?3:1);data(menu,3,stage==10?0:72000);data(menu,4,100000);
            menu.getSlot(0).set(new ItemStack(ForeverSkyblock.MINERAL_BLEND.get(),32));menu.getSlot(1).set(new ItemStack(Items.REDSTONE,16));
            menu.getSlot(2).set(new ItemStack(Items.LAPIS_LAZULI,12));
            mc.player.containerMenu=menu;mc.setScreen(new RefiningScreen(menu,inventory,menuTitle("Refining Condenser")));
        } else if(stage<=12) {
            var menu=new CrucibleMenu(0,inventory);
            data(menu,0,80);data(menu,1,160);data(menu,2,2000);data(menu,3,2);data(menu,4,1);data(menu,5,1);data(menu,6,stage==12?1:0);data(menu,7,stage==12?2:1);data(menu,8,30);
            mc.player.containerMenu=menu;mc.setScreen(new CrucibleScreen(menu,inventory,menuTitle("Crucible")));
        } else {
            var menu=new AscensionMenu(0,inventory);data(menu,0,80);data(menu,1,1);data(menu,2,1);menu.getSlot(0).set(new ItemStack(ForeverSkyblock.FOREVER_CORE.get()));
            mc.player.containerMenu=menu;mc.setScreen(new AscensionScreen(menu,inventory,menuTitle("Ascension")));
        }
    }
    private static net.minecraft.network.chat.Component menuTitle(String title) {return net.minecraft.network.chat.Component.literal(title);}
    private static void data(AbstractContainerMenu menu,int index,int value)throws Exception {
        Field field=menu.getClass().getDeclaredField("data");field.setAccessible(true);((ContainerData)field.get(menu)).set(index,value);
    }
    private static void setCondenserData(CondenserMenu menu,int stage)throws Exception {
        data(menu,3,stage==2?1:0);data(menu,4,stage==3?1:0);data(menu,5,32);data(menu,6,1);
        data(menu,13,stage==0?1:2);data(menu,14,stage==4||stage==5?1:0);
        data(menu,15,240);data(menu,16,320);data(menu,17,1);
    }
    private static void gallery(Minecraft mc) {
        var server=mc.getSingleplayerServer();
        server.execute(()-> {
            var level=server.overworld();
            var blocks=java.util.List.of(ForeverSkyblock.CONDENSER_MARK_I.get(),ForeverSkyblock.CONDENSER_MARK_II.get(),ForeverSkyblock.ASCENSION_CHAMBER.get(),
                    ForeverSkyblock.MIXING_CHAMBER_MARK_I.get(),ForeverSkyblock.FREEZING_CHAMBER_MARK_I.get(),ForeverSkyblock.WASHING_BASIN.get(),
                    ForeverSkyblock.GARDEN_CHAMBER_MARK_I.get(),ForeverSkyblock.CRUCIBLE.get(),ForeverSkyblock.REFINING_CHAMBER.get());
            for(int x=-3;x<=10;x++)for(int z=-14;z<=10;z++)level.setBlockAndUpdate(new net.minecraft.core.BlockPos(x,80,z),net.minecraft.world.level.block.Blocks.SMOOTH_STONE.defaultBlockState());
            for(int i=0;i<blocks.size();i++) {
                int x=(i%3)*3,y=81+(i/3)*2,z=(i/3)*3;
                for(int yy=81;yy<y;yy++)level.setBlockAndUpdate(new net.minecraft.core.BlockPos(x,yy,z),net.minecraft.world.level.block.Blocks.SMOOTH_STONE.defaultBlockState());
                var pos=new net.minecraft.core.BlockPos(x,y,z);level.setBlockAndUpdate(pos,blocks.get(i).defaultBlockState());
                if(level.getBlockEntity(pos) instanceof CondenserBlockEntity machine)machine.inventory().setStackInSlot(CondenserBlockEntity.INPUT,new ItemStack(Items.OAK_LOG,64));
                if(level.getBlockEntity(pos) instanceof RefiningChamberBlockEntity machine) {
                    machine.inventory().setStackInSlot(0,new ItemStack(ForeverSkyblock.MINERAL_BLEND.get(),64));machine.inventory().setStackInSlot(1,new ItemStack(Items.REDSTONE,64));
                    for(int j=0;j<25;j++)machine.energyAutomation(null).receiveEnergy(4000,false);
                }
            }
            var garden=new net.minecraft.core.BlockPos(1,85,6);
            level.setBlockAndUpdate(garden,ForeverSkyblock.CONDENSER_MARK_II.get().defaultBlockState());
            ((CondenserBlockEntity)level.getBlockEntity(garden)).inventory().setStackInSlot(CondenserBlockEntity.GARDEN_SPECIMEN,new ItemStack(Items.OAK_SAPLING));
            level.setDayTime(6000);
            var player=server.getPlayerList().getPlayer(mc.player.getUUID());
            player.connection.teleport(3.5,85,-12,0,18);
        });
    }
}

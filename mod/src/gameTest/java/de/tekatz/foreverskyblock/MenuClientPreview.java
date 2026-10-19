package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.*;
import java.lang.reflect.*;
import java.util.*;
import net.minecraft.client.Minecraft;
import net.minecraft.client.Screenshot;
import net.minecraft.client.gui.screens.TitleScreen;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.ClientTickEvent;

/** Opt-in real-render QA fixture. Compiled only into the GameTest source set, never the delivered JAR. */
@EventBusSubscriber(modid=ForeverSkyblock.MOD_ID,value=Dist.CLIENT)
public final class MenuClientPreview {
    private static int ticks, stage, startup;
    private static PremiumSkillScreen menu;
    @SubscribeEvent public static void tick(ClientTickEvent.Post event) throws Exception {
        if(!Boolean.getBoolean("forever_skyblock.menuVisualTests"))return;
        var mc=Minecraft.getInstance();
        if(menu==null) {
            if (++startup % 40 == 0) System.out.println("MENU_VISUAL_WAIT "+(mc.screen==null?"null":mc.screen.getClass().getName()));
            if (mc.screen != null && mc.screen.getClass().getSimpleName().equals("AccessibilityOnboardingScreen")) mc.setScreen(new TitleScreen());
            if(!(mc.screen instanceof TitleScreen) || mc.getOverlay()!=null)return;
            mc.options.guiScale().set(3);mc.resizeDisplay();
            var contracts=new LinkedHashMap<String,SkillNetwork.ContractState>();
            for(var c:ContractCatalog.CONTRACTS)contracts.put(c.id(),new SkillNetwork.ContractState(c.target(),3,true,c.resource(),false));
            var products=new LinkedHashMap<String,SkillNetwork.ProductState>();
            for(var p:ShopCatalog.PRODUCTS)products.put(p.id(),new SkillNetwork.ProductState(0,true));
            var snapshot=new SkillNetwork.Snapshot(63,2,Map.of(),Set.of(ChallengeRules.FOUNDATIONS.id(),ChallengeRules.DRAGON.id()),true,false,true,"",true,1,
                    Map.of(ChallengeRules.VETERAN.id(),418),new MetaProgress.Accounting(70,3,12,1,5,0,10,true),true,"The sky island",Set.of(),contracts,products,new SkillNetwork.MenuStatistics(725000,7,3,418));
            Field f=ClientSkills.class.getDeclaredField("snapshot");f.setAccessible(true);f.set(null,snapshot);
            menu=new PremiumSkillScreen();mc.setScreen(menu);return;
        }
        if(mc.getOverlay()!=null)return;
        if(++ticks%24!=0)return;
        Screenshot.grab(mc.gameDirectory,"menu-"+stage+".png",mc.getMainRenderTarget(),c->{});
        System.out.println("MENU_VISUAL_STAGE "+stage+" "+mc.getWindow().getGuiScaledWidth()+"x"+mc.getWindow().getGuiScaledHeight());
        switch(++stage) {
            case 1 -> clickCard(mc,"body",3,false,true);
            case 2 -> page(mc,"SHOP");
            case 3 -> clickCard(mc,"body",3,true,true);
            case 4 -> page(mc,"PROFILE");
            case 5 -> {page(mc,"CHALLENGES");Field f=PremiumSkillScreen.class.getDeclaredField("category");f.setAccessible(true);f.setInt(menu,7);mc.setScreen(null);mc.setScreen(menu);}
            case 6 -> clickCard(mc,"body",3,false,true);
            case 7 -> {mc.options.guiScale().set(4);mc.resizeDisplay();page(mc,"SHOP");}
            case 8 -> {mc.options.guiScale().set(6);mc.resizeDisplay();page(mc,"CHALLENGES");}
            case 9 -> {mc.options.guiScale().set(3);mc.resizeDisplay();mc.getLanguageManager().setSelected("de_de");mc.reloadResourcePacks();page(mc,"CHALLENGES");}
            case 10 -> page(mc,"SHOP");
            case 11 -> page(mc,"PROFILE");
            case 12 -> {page(mc,"CHALLENGES");Field f=PremiumSkillScreen.class.getDeclaredField("category");f.setAccessible(true);f.setInt(menu,7);mc.setScreen(null);mc.setScreen(menu);}
            case 13 -> {var m=PremiumSkillScreen.class.getDeclaredMethod("openContract",ContractCatalog.Contract.class);m.setAccessible(true);m.invoke(menu,ContractCatalog.NITRO);}
            case 14 -> {page(mc,"SHOP");menu.mouseScrolled(mc.getWindow().getGuiScaledWidth()/2D,mc.getWindow().getGuiScaledHeight()-40,0,-12);}
            case 15 -> {page(mc,"CHALLENGES");Field f=PremiumSkillScreen.class.getDeclaredField("category");f.setAccessible(true);f.setInt(menu,0);mc.setScreen(null);mc.setScreen(menu);menu.mouseScrolled(35,mc.getWindow().getGuiScaledHeight()-40,0,-12);}
            case 16 -> {
                clickCard(mc,"rail",7,false,false);
                Field f=PremiumSkillScreen.class.getDeclaredField("category");f.setAccessible(true);
                if(f.getInt(menu)!=7)throw new IllegalStateException("Scrolled category did not receive click");
            }
            default -> {System.out.println("MENU_VISUAL_COMPLETE");mc.stop();}
        }
    }
    private static void clickCard(Minecraft mc,String field,int index,boolean round,boolean opensDetail) throws Exception {
        Field f=PremiumSkillScreen.class.getDeclaredField(field);f.setAccessible(true);
        var panel=(net.neoforged.neoforge.client.gui.widget.ScrollPanel)f.get(menu);
        var button=(net.minecraft.client.gui.components.Button)panel.children().get(index);
        if(round) {
            menu.mouseClicked(button.getX()+1,button.getY()+1,0);
            if(mc.screen!=menu)throw new IllegalStateException("Round node accepted its square corner");
        }
        menu.mouseClicked(button.getX()+button.getWidth()/2D,button.getY()+(round?18:button.getHeight()/2D),0);
        if(opensDetail && mc.screen==menu)throw new IllegalStateException("Card or node did not open detail through screen input");
        System.out.println("MENU_INPUT_PASSED "+field+" "+index+" round="+round);
    }
    @SuppressWarnings({"rawtypes","unchecked"})
    private static void page(Minecraft mc,String name) throws Exception {
        mc.setScreen(menu);
        Class type=Class.forName(PremiumSkillScreen.class.getName()+"$Page");
        Method m=PremiumSkillScreen.class.getDeclaredMethod("navigate",type);m.setAccessible(true);m.invoke(menu,Enum.valueOf(type,name));
    }
}

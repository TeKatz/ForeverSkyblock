package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import de.tekatz.foreverskyblock.client.KMenuEditorScreen;
import de.tekatz.foreverskyblock.client.EditorClient;
import de.tekatz.foreverskyblock.editor.*;
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
    private static KMenuEditorScreen editor;
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
            case 17 -> {
                mc.getLanguageManager().setSelected("en_us");mc.reloadResourcePacks();mc.options.guiScale().set(2);mc.resizeDisplay();
                editor=new KMenuEditorScreen(menu,EditorDefaults.create());mc.setScreen(editor);
            }
            case 18 -> dragHeader();
            case 19 -> editorMode(mc,"QUESTS");
            case 20 -> {var add=KMenuEditorScreen.class.getDeclaredMethod("addEntry");add.setAccessible(true);add.invoke(editor);}
            case 21 -> editorMode(mc,"GRAPH");
            case 22 -> {editorMode(mc,"SHOP");}
            case 23 -> editorMode(mc,"CONTRACTS");
            case 24 -> {editorMode(mc,"LAYOUT");setEditor("previewState",1);editor.resize(mc,editor.width,editor.height);}
            case 25 -> {setEditor("viewportIndex",1);editor.resize(mc,editor.width,editor.height);}
            case 26 -> {mc.getLanguageManager().setSelected("de_de");mc.reloadResourcePacks();editorMode(mc,"LAYOUT");}
            case 27 -> {editorMode(mc,"SETTINGS");}
            default -> {System.out.println("MENU_EDITOR_VISUAL_COMPLETE");System.out.println("MENU_VISUAL_COMPLETE");mc.stop();}
        }
    }
    private static void setEditor(String name,Object value)throws Exception{Field f=KMenuEditorScreen.class.getDeclaredField(name);f.setAccessible(true);f.set(editor,value);}
    @SuppressWarnings({"rawtypes","unchecked"})
    private static void editorMode(Minecraft mc,String name)throws Exception{Class type=Class.forName(KMenuEditorScreen.class.getName()+"$Mode");setEditor("mode",Enum.valueOf(type,name));String selection=switch(name){case "LAYOUT"->"header.sp";case "QUESTS","GRAPH"->ChallengeRules.FOUNDATIONS.id();case "SHOP"->ShopCatalog.NO_RAIN.id();case "CONTRACTS"->ContractCatalog.SINGULARITY.id();case "CHAPTERS"->"forever";default->"settings";};setEditor("selected",selection);setEditor("propertyScroll",0);editor.resize(mc,editor.width,editor.height);}
    private static float editorFloat(String name)throws Exception{Field f=KMenuEditorScreen.class.getDeclaredField(name);f.setAccessible(true);return f.getFloat(editor);}
    private static void dragHeader()throws Exception{
        var rect=EditorClient.bounds().get("header.sp");if(rect==null)throw new IllegalStateException("Native header geometry missing");float scale=editorFloat("previewScale"),x=editorFloat("previewX"),y=editorFloat("previewY");double mx=x+(rect.x()+rect.width()/2F)*scale,my=y+(rect.y()+rect.height()/2F)*scale;
        editor.mouseClicked(mx,my,0);editor.mouseDragged(mx+12*scale,my+8*scale,0,12*scale,8*scale);editor.mouseReleased(mx+12*scale,my+8*scale,0);
        Field f=KMenuEditorScreen.class.getDeclaredField("session");f.setAccessible(true);var session=(EditorSession)f.get(editor);
        if(session.current().layout().get("header.sp").x()!=12||session.current().layout().get("header.sp").y()!=8)throw new IllegalStateException("Native drag did not update the selected layout element");
        session.undo();if(session.current().layout().get("header.sp").x()!=0)throw new IllegalStateException("Native drag undo failed");session.redo();
        System.out.println("MENU_EDITOR_INPUT_PASSED drag-header undo redo");
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

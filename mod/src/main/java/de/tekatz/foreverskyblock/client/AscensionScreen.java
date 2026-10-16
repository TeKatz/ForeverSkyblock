package de.tekatz.foreverskyblock.client;

import com.mojang.math.Axis;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity;
import de.tekatz.foreverskyblock.menu.*;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.PlayerFaceRenderer;
import net.minecraft.client.gui.components.Tooltip;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.item.ItemStack;

public final class AscensionScreen extends MachineScreen<AscensionMenu> {
    private MachineButton start;
    public AscensionScreen(AscensionMenu menu,Inventory inventory,Component title) { super(menu,inventory,title,MachineLayout.ASCENSION,true,1); }
    private static Component text(String key,Object... args) { return Component.translatable("gui.forever_skyblock.machine."+key,args); }
    @Override protected void init() {
        super.init();
        start=addRenderableWidget(new MachineButton(leftPos+20,topPos+141,208,20,text("consume"),skin,true,b->minecraft.gameMode.handleInventoryButtonClick(menu.containerId,0)));
        start.setTooltip(Tooltip.create(Component.translatable("gui.forever_skyblock.ascension.help")));
    }
    @Override protected void containerTick() {
        super.containerTick();start.active=!menu.busy()&&menu.getSlot(0).hasItem();start.setMessage(text(menu.busy()?"converting":"consume"));
    }
    @Override protected void renderBg(GuiGraphics g,float partialTick,int mouseX,int mouseY) {
        int x=leftPos,y=topPos;skin.frame(g,x,y,imageWidth,imageHeight);
        g.renderItem(new ItemStack(ForeverSkyblock.ASCENSION_CHAMBER.get()),x+18,y+10);
        MachineSkin.text(g,font,text("ascension_title"),x+41,y+12,142,MachineSkin.VIOLET,1.05F);
        MachineSkin.panel(g,x+190,y+9,40,15,0xFF615372);
        MachineSkin.centered(g,font,text(menu.busy()?"processing":"ready"),x+210,y+13,35,MachineSkin.LIGHT,.73F);
        MachineSkin.centered(g,font,text("forever_core"),x+62,y+32,96,MachineSkin.LIGHT,.85F);
        for(int i=0;i<2;i++) {
            g.pose().pushPose();g.pose().translate(x+62,y+66,0);
            if(menu.busy())g.pose().mulPose(Axis.ZP.rotationDegrees((float)(System.currentTimeMillis()%6000)/6000*360*(i==0?1:-1)));
            int r=24-i*5,color=i==0?0xFF6F7C8D:0xFF6E449A;
            g.fill(-r,-r,r,-r+2,color);g.fill(-r,r-2,r,r,color);g.fill(-r,-r+2,-r+2,r-2,color);g.fill(r-2,-r+2,r,r-2,color);
            g.pose().popPose();
        }
        skin.slot(g,x+54,y+58,true);if(menu.busy())skin.lock(g,x+70,y+74);
        MachineSkin.text(g,font,Component.literal("→"),x+110,y+59,25,MachineSkin.VIOLET,2.1F);
        MachineSkin.panel(g,x+149,y+46,70,41,0xFF8E4FD2);
        MachineSkin.centered(g,font,Component.literal("+1 PP"),x+184,y+53,60,MachineSkin.VIOLET,1.6F);
        MachineSkin.centered(g,font,text("perk_point"),x+184,y+72,61,MachineSkin.VIOLET,.73F);
        MachineSkin.centered(g,font,text("conversion"),x+124,y+94,210,MachineSkin.MUTED,.8F);
        MachineSkin.panel(g,x+23,y+106,146,8,0xFF697487);
        g.fill(x+25,y+108,x+25+Math.min(142,menu.progress()*142/AscensionChamberBlockEntity.DURATION),y+112,MachineSkin.VIOLET);
        MachineSkin.text(g,font,Component.literal("10 s"),x+176,y+103,50,MachineSkin.LIGHT,.85F);
        MachineSkin.text(g,font,text("no_power"),x+176,y+112,50,MachineSkin.MUTED,.65F);
        MachineSkin.panel(g,x+20,y+120,208,17,0xFF576073);
        Component recipient=minecraft.player.getDisplayName();var info=menu.busy()?minecraft.getConnection().getPlayerInfo(menu.owner()):null;
        if(menu.busy())recipient=info==null?Component.translatable("gui.forever_skyblock.ascension.offline_player"):Component.literal(info.getProfile().getName());
        PlayerFaceRenderer.draw(g,info==null?minecraft.player.getSkin():info.getSkin(),x+24,y+123,11);
        MachineSkin.text(g,font,text("recipient",recipient),x+40,y+125,117,MachineSkin.MUTED,.77F);
        var state=ClientSkills.snapshot();String balance=state==null||!state.available()?"—":Integer.toString(state.perkPoints());
        MachineSkin.text(g,font,text("balance",balance),x+160,y+125,64,MachineSkin.VIOLET,.72F);
        Component hint=text("consumed_hint");
        if(menu.status()>=2&&menu.status()<=5)hint=Component.translatable("gui.forever_skyblock.ascension."+switch(menu.status()){case 2->"offline";case 3->"storage_error";case 4->"invalid";default->"claimed";});
        MachineSkin.centered(g,font,hint,x+124,y+164,210,MachineSkin.MUTED,.68F);
        inventory(g);
    }
    @Override public boolean mouseClicked(double x,double y,int button) {
        int mx=logicalMouseX(x)-leftPos,my=logicalMouseY(y)-topPos;
        if(button==0&&mx>=23&&mx<=169&&my>=106&&my<=114&&ClientMachineRecipes.available()){ClientMachineRecipes.ascension();return true;}
        return super.mouseClicked(x,y,button);
    }
}

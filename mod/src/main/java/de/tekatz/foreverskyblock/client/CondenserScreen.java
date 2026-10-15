package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.*;
import de.tekatz.foreverskyblock.network.CondenserNetwork;
import de.tekatz.foreverskyblock.recipe.CondenserInput;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Tooltip;
import net.minecraft.client.renderer.Rect2i;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.inventory.ClickType;
import net.minecraft.world.inventory.Slot;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.network.PacketDistributor;

public final class CondenserScreen extends MachineScreen<CondenserMenu> {
    private MachineButton filters,recipes;
    public CondenserScreen(CondenserMenu menu,Inventory inventory,Component title) { super(menu,inventory,title,MachineLayout.CONDENSER,false,3); }
    private static Component text(String key,Object... args) { return Component.translatable("gui.forever_skyblock.machine."+key,args); }
    @Override protected void init() {
        super.init();
        filters=addRenderableWidget(new MachineButton(leftPos+20,topPos+119,111,19,text("edit_locks"),skin,true,b->minecraft.gameMode.handleInventoryButtonClick(menu.containerId,0)));
        filters.setTooltip(Tooltip.create(Component.translatable("gui.forever_skyblock.filter_help")));
        recipes=addRenderableWidget(new MachineButton(leftPos+174,topPos+119,54,19,text("recipes"),skin,false,b->ClientMachineRecipes.condenser()));
    }
    @Override protected void containerTick() { super.containerTick();filters.setMessage(text(menu.editing()?"finish_locks":"edit_locks"));recipes.active=ClientMachineRecipes.available(); }
    public Rect2i inputArea(int slot) { return screenArea((slot==0?CondenserMenu.FIRST_X:CondenserMenu.SECOND_X)-4,CondenserMenu.INPUT_Y-4,24,24); }
    public void setGhost(int slot,ItemStack stack) {
        if(menu.editing()&&menu.acceptsGhost(slot,stack))PacketDistributor.sendToServer(new CondenserNetwork.Filter(menu.containerId,slot,BuiltInRegistries.ITEM.getKey(stack.getItem())));
    }
    @Override protected void slotClicked(Slot slot, int slotId, int button, ClickType type) {
        if (menu.editing() && CondenserBlockEntity.isInput(slotId)) {
            if (button == 1) setGhost(slotId, ItemStack.EMPTY);
            else if (!menu.getCarried().isEmpty()) setGhost(slotId, menu.getCarried());
            return;
        }
        super.slotClicked(slot, slotId, button, type);
    }
    @Override public boolean mouseReleased(double x, double y, int button) {
        if (menu.editing() && button == 0 && !menu.getCarried().isEmpty()) {
            for (int slot : new int[]{0, 2}) if (inputArea(slot).contains((int) x, (int) y) && menu.getSlot(slot).isActive()) {
                setGhost(slot, menu.getCarried());
                // Let the base screen finish its drag state; the server blocks real input clicks in edit mode.
                super.mouseReleased(x, y, button);
                return true;
            }
        }
        return super.mouseReleased(x, y, button);
    }

    @Override protected void renderBg(GuiGraphics g,float partialTick,int mouseX,int mouseY) {
        int x=leftPos,y=topPos;skin.frame(g,x,y,imageWidth,imageHeight);
        g.renderItem(new ItemStack(ForeverSkyblock.CONDENSER_MARK_I.get()),x+18,y+10);
        MachineSkin.text(g,font,text("condenser_title"),x+42,y+12,126,MachineSkin.LIGHT,1.1F);
        int color=menu.status()==CondenserBlockEntity.BLOCKED?0xFFFFBF76:MachineSkin.CYAN;
        g.fill(x+177,y+12,x+181,y+16,color);
        MachineSkin.text(g,font,text(menu.status()==1?"processing":menu.status()==2?"blocked":"idle"),x+186,y+12,43,color,.73F);
        MachineSkin.panel(g,x+42,y+31,164,12,0xFF365A66);
        MachineSkin.centered(g,font,text(menu.hasModule()?"module_connected":"module_absent"),x+124,y+34,156,MachineSkin.CYAN,.72F);
        MachineSkin.panel(g,x+14,y+44,220,98,0xFF54616D);
        MachineSkin.text(g,font,text("input"),x+23,y+48,43,MachineSkin.MUTED,.73F);
        if(menu.hasModule())MachineSkin.text(g,font,text("module_input"),x+68,y+48,76,MachineSkin.MUTED,.73F);
        MachineSkin.text(g,font,text("output"),x+190,y+48,41,MachineSkin.MUTED,.73F);
        skin.slot(g,x+CondenserMenu.FIRST_X,y+CondenserMenu.INPUT_Y,true);
        if(menu.hasModule()){skin.slot(g,x+CondenserMenu.SECOND_X,y+CondenserMenu.INPUT_Y,true);MachineSkin.text(g,font,Component.literal("+"),x+59,y+67,13,MachineSkin.MUTED,1.25F);}
        skin.slot(g,x+CondenserMenu.OUTPUT_X,y+CondenserMenu.INPUT_Y,true);
        for(int slot:new int[]{0,2})if(menu.getSlot(slot).isActive()) {
            int sx=menu.getSlot(slot).x;var filter=menu.filter(slot);
            if(!filter.isEmpty()) {
                skin.lock(g,x+sx+12,y+CondenserMenu.INPUT_Y-7);
                if(menu.getSlot(slot).getItem().isEmpty()) {
                    g.renderItem(filter,x+sx,y+CondenserMenu.INPUT_Y);g.fill(x+sx,y+CondenserMenu.INPUT_Y,x+sx+16,y+CondenserMenu.INPUT_Y+16,0x88404C55);
                }
            }else if(menu.editing())g.fill(x+sx-3,y+CondenserMenu.INPUT_Y+21,x+sx+20,y+CondenserMenu.INPUT_Y+22,MachineSkin.CYAN);
        }
        int barX=x+(menu.hasModule()?111:83),barW=menu.hasModule()?70:98;
        MachineSkin.panel(g,barX,y+67,barW,8,0xFF637888);
        int filled=Math.min(barW-4,(barW-4)*menu.progress()/menu.duration());g.fill(barX+2,y+69,barX+2+filled,y+73,MachineSkin.CYAN);
        g.fill(barX+barW,y+66,barX+barW+3,y+76,MachineSkin.CYAN);
        MachineSkin.centered(g,font,Component.literal(String.format(java.util.Locale.ROOT,"%.1f / %.1f s",menu.progress()/20F,menu.duration()/20F)),barX+barW/2,y+80,barW+4,MachineSkin.MUTED,.7F);
        Component summary=text("wood_recipe");
        if(minecraft.level!=null) {
            var input=new CondenserInput(menu.getSlot(0).getItem(),menu.hasModule()?menu.getSlot(2).getItem():ItemStack.EMPTY);
            var recipe=minecraft.level.getRecipeManager().getRecipeFor(ForeverSkyblock.CONDENSING.get(),input,minecraft.level);
            if(recipe.isPresent())summary=Component.literal(menu.getSlot(0).getItem().getHoverName().getString()
                    +(recipe.get().value().requiresModule()?" + "+menu.getSlot(2).getItem().getHoverName().getString():"")+" → "+recipe.get().value().result().getHoverName().getString());
        }
        MachineSkin.text(g,font,summary,x+20,y+95,207,MachineSkin.LIGHT,1);
        MachineSkin.text(g,font,text("no_power"),x+20,y+107,207,MachineSkin.MUTED,.8F);
        MachineSkin.panel(g,x+18,y+145,212,11,0xFF5D6875);
        MachineSkin.centered(g,font,text("module_hint"),x+124,y+148,205,MachineSkin.MUTED,.67F);
        inventory(g);
    }
    @Override public boolean mouseClicked(double x,double y,int button) {
        int mx=logicalMouseX(x)-leftPos,my=logicalMouseY(y)-topPos;
        int start=menu.hasModule()?111:83;
        if(button==0&&mx>=start&&mx<=184&&my>=65&&my<=88&&ClientMachineRecipes.available()) { ClientMachineRecipes.condenser();return true; }
        return super.mouseClicked(x,y,button);
    }
    @Override protected void renderMachineTooltip(GuiGraphics g,int mouseX,int mouseY) {
        for(int slot:new int[]{0,2})if(menu.getSlot(slot).isActive()&&menu.editing()) {
            int sx=leftPos+menu.getSlot(slot).x;
            if(mouseX>=sx-4&&mouseX<sx+20&&mouseY>=topPos+58&&mouseY<topPos+82)
                g.renderComponentTooltip(font,java.util.List.of(Component.translatable("gui.forever_skyblock.filter_clear")),mouseX,mouseY);
        }
    }
}

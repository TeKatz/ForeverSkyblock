package de.tekatz.foreverskyblock.client;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.network.chat.Component;

final class MachineButton extends Button {
    private final MachineSkin skin;
    private final boolean primary;
    MachineButton(int x,int y,int width,int height,Component message,MachineSkin skin,boolean primary,OnPress action) {
        super(x,y,width,height,message,action,DEFAULT_NARRATION);this.skin=skin;this.primary=primary;
    }
    @Override protected void renderWidget(GuiGraphics g,int mouseX,int mouseY,float partialTick) {
        skin.button(g,getX(),getY(),width,height,primary,active,isHoveredOrFocused());
        MachineSkin.centered(g,Minecraft.getInstance().font,getMessage(),getX()+width/2,getY()+(height-8)/2,width-10,
                active?MachineSkin.LIGHT:MachineSkin.MUTED,.92F);
    }
}

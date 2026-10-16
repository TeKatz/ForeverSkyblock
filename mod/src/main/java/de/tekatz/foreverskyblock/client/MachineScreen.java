package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.menu.MachineLayout;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.inventory.AbstractContainerScreen;
import net.minecraft.client.renderer.Rect2i;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.inventory.AbstractContainerMenu;

/** One transform applies to every visual and pointer operation, including external JEI ghost targets. */
public abstract class MachineScreen<M extends AbstractContainerMenu> extends AbstractContainerScreen<M> {
    protected final MachineLayout layout;
    protected final MachineSkin skin;
    private final int machineSlots;
    protected MachineScreen(M menu,Inventory inventory,Component title,MachineLayout layout,boolean ascension,int machineSlots) {
        super(menu,inventory,title);this.layout=layout;this.skin=new MachineSkin(ascension);this.machineSlots=machineSlots;
        imageWidth=layout.width();imageHeight=layout.height();
    }
    protected MachineLayout.Viewport viewport() { return layout.viewport(width,height,leftPos,topPos); }
    protected int logicalMouseX(double x) { return (int)Math.floor(viewport().logicalX(x)); }
    protected int logicalMouseY(double y) { return (int)Math.floor(viewport().logicalY(y)); }
    public Rect2i screenArea(int relativeX,int relativeY,int w,int h) {
        var v=viewport();return new Rect2i((int)Math.floor(v.screenX(leftPos+relativeX)),(int)Math.floor(v.screenY(topPos+relativeY)),
                (int)Math.ceil(w*v.scale()),(int)Math.ceil(h*v.scale()));
    }
    @Override public int getGuiLeft() { return (int)Math.floor(viewport().x()); }
    @Override public int getGuiTop() { return (int)Math.floor(viewport().y()); }
    @Override public int getXSize() { return (int)Math.ceil(imageWidth*viewport().scale()); }
    @Override public int getYSize() { return (int)Math.ceil(imageHeight*viewport().scale()); }
    @Override public void renderBackground(GuiGraphics g,int mouseX,int mouseY,float partialTick) { }
    @Override public void render(GuiGraphics g,int mouseX,int mouseY,float partialTick) {
        // Draw the world backdrop once at native viewport size, before scaling the panel.
        super.renderBackground(g,mouseX,mouseY,partialTick);
        var v=viewport();int mx=logicalMouseX(mouseX),my=logicalMouseY(mouseY);
        g.pose().pushPose();g.pose().translate(v.x()-leftPos*v.scale(),v.y()-topPos*v.scale(),0);
        g.pose().scale((float)v.scale(),(float)v.scale(),1);
        super.render(g,mx,my,partialTick);renderTooltip(g,mx,my);renderMachineTooltip(g,mx,my);g.flush();g.pose().popPose();
    }
    protected void renderMachineTooltip(GuiGraphics g,int x,int y) { }
    @Override protected boolean isHovering(int x,int y,int w,int h,double mx,double my) {
        if(w==16&&h==16)for(int i=0;i<machineSlots;i++)if(menu.getSlot(i).x==x&&menu.getSlot(i).y==y)
            return super.isHovering(x-4,y-4,24,24,mx,my);
        return super.isHovering(x,y,w,h,mx,my);
    }
    @Override public boolean mouseClicked(double x,double y,int button) { return super.mouseClicked(viewport().logicalX(x),viewport().logicalY(y),button); }
    @Override public boolean mouseReleased(double x,double y,int button) { return super.mouseReleased(viewport().logicalX(x),viewport().logicalY(y),button); }
    @Override public boolean mouseDragged(double x,double y,int button,double dx,double dy) { return super.mouseDragged(viewport().logicalX(x),viewport().logicalY(y),button,dx/viewport().scale(),dy/viewport().scale()); }
    @Override public boolean mouseScrolled(double x,double y,double dx,double dy) { return super.mouseScrolled(viewport().logicalX(x),viewport().logicalY(y),dx,dy); }
    @Override public void mouseMoved(double x,double y) { super.mouseMoved(viewport().logicalX(x),viewport().logicalY(y)); }
    protected void inventory(GuiGraphics g) {
        for(int row=0;row<3;row++)for(int col=0;col<9;col++)skin.slot(g,leftPos+layout.inventoryX(col),topPos+layout.inventoryRowY(row),false);
        for(int col=0;col<9;col++)skin.slot(g,leftPos+layout.inventoryX(col),topPos+layout.hotbarY(),false);
    }
    @Override protected void renderLabels(GuiGraphics g,int mouseX,int mouseY) { }
}

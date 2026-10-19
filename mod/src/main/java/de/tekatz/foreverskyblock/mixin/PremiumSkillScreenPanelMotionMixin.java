package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.EditorStyleBridge;
import de.tekatz.foreverskyblock.editor.EditorDocument;
import java.lang.reflect.Field;
import java.util.List;
import java.util.Locale;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.neoforged.neoforge.client.gui.widget.ScrollPanel;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.*;
import org.spongepowered.asm.mixin.injection.callback.*;

/** Stable panel rebuilds, authored background effects and horizontal navigation for wide Shop trees. */
@Mixin(targets="de.tekatz.foreverskyblock.client.PremiumSkillScreen$MenuPanel")
public abstract class PremiumSkillScreenPanelMotionMixin {
    @Shadow @Final private List<Button> buttons;
    @Shadow @Final private List<int[]> offsets;
    @Shadow @Final private String key;
    @Shadow int contentHeight;
    @Unique private double foreverSkyblock$horizontal;
    @Unique private boolean foreverSkyblock$horizontalDragging;
    @Unique private EditorDocument.Style foreverSkyblock$previousStyle;

    @Inject(method="entryOffset",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$keepRebuiltPanelsStationary(CallbackInfoReturnable<Integer> cir){cir.setReturnValue(0);}

    @Unique private boolean foreverSkyblock$isShop(){return "SHOP".equals(key);}
    @Unique private String foreverSkyblock$styleId(){
        if("rail".equals(key))return "categories";
        if("detail".equals(key))return "detail.prose";
        String page=key==null?"":key.split(":",2)[0].toLowerCase(Locale.ROOT);
        return page.isEmpty()?"":"body."+page;
    }

    @Inject(method="drawBackground",at=@At("HEAD"))
    private void foreverSkyblock$beginBackgroundEffect(GuiGraphics g,net.minecraft.client.renderer.Tesselator tess,float tick,CallbackInfo ci){
        String id=foreverSkyblock$styleId();if(!id.isEmpty())foreverSkyblock$previousStyle=EditorStyleBridge.pushEffects(id);
    }
    @Inject(method="drawBackground",at=@At("RETURN"))
    private void foreverSkyblock$endBackgroundEffect(GuiGraphics g,net.minecraft.client.renderer.Tesselator tess,float tick,CallbackInfo ci){
        if(foreverSkyblock$previousStyle!=null){EditorStyleBridge.restore(foreverSkyblock$previousStyle);foreverSkyblock$previousStyle=null;}
    }

    @Unique private int foreverSkyblock$field(String name){
        try{Field field=ScrollPanel.class.getDeclaredField(name);field.setAccessible(true);return field.getInt(this);}catch(ReflectiveOperationException e){return 0;}
    }
    @Unique private int foreverSkyblock$contentWidth(){
        int width=foreverSkyblock$field("width"),max=width;
        for(int i=0;i<Math.min(offsets.size(),buttons.size());i++)max=Math.max(max,offsets.get(i)[0]+buttons.get(i).getWidth()+112);
        return max;
    }
    @Unique private double foreverSkyblock$maximum(){return Math.max(0,foreverSkyblock$contentWidth()-foreverSkyblock$field("width"));}
    @Unique private void foreverSkyblock$clamp(){foreverSkyblock$horizontal=Math.clamp(foreverSkyblock$horizontal,0,foreverSkyblock$maximum());}

    /** Button hit boxes move with the graph, while the panel/scissor itself stays fixed. */
    @Inject(method="position",at=@At("TAIL"))
    private void foreverSkyblock$shiftShopButtons(CallbackInfo ci){
        if(!foreverSkyblock$isShop())return;foreverSkyblock$clamp();int shift=(int)Math.round(foreverSkyblock$horizontal);
        if(shift==0)return;for(var button:buttons)button.setX(button.getX()-shift);
    }

    /** Shift the Shop edge painter by the same amount as its nodes. */
    @ModifyArg(method="drawPanel",at=@At(value="INVOKE",target="Lde/tekatz/foreverskyblock/client/PremiumSkillScreen$Painter;draw(Lnet/minecraft/client/gui/GuiGraphics;IIIII)V"),index=1)
    private int foreverSkyblock$shiftShopPainter(int left){return foreverSkyblock$isShop()?left-(int)Math.round(foreverSkyblock$horizontal):left;}

    /** Shift-wheel (or a native horizontal wheel) pans the live Shop sideways. */
    @Inject(method="mouseScrolled",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$horizontalWheel(double mx,double my,double horizontal,double vertical,CallbackInfoReturnable<Boolean> cir){
        var panel=(ScrollPanel)(Object)this;if(!foreverSkyblock$isShop()||!panel.isMouseOver(mx,my)||foreverSkyblock$maximum()<=0)return;
        if(horizontal!=0||Screen.hasShiftDown()){
            double delta=horizontal!=0?horizontal:vertical;foreverSkyblock$horizontal-=delta*54D;foreverSkyblock$clamp();cir.setReturnValue(true);
        }
    }

    @Inject(method="render",at=@At("TAIL"))
    private void foreverSkyblock$horizontalBar(GuiGraphics g,int mx,int my,float partialTick,CallbackInfo ci){
        if(!foreverSkyblock$isShop())return;double maximum=foreverSkyblock$maximum();if(maximum<=0)return;
        int left=foreverSkyblock$field("left"),right=foreverSkyblock$field("right"),bottom=foreverSkyblock$field("bottom"),view=Math.max(1,right-left),content=foreverSkyblock$contentWidth();
        int track=Math.max(1,view-5),thumb=Math.clamp((int)((long)track*view/Math.max(1,content)),24,track);int available=Math.max(1,track-thumb);
        int x=left+(int)Math.round(foreverSkyblock$horizontal*available/maximum);
        g.fill(left,bottom-3,left+track,bottom,0x99152E3C);g.fill(x,bottom-3,x+thumb,bottom,0xFF51D7ED);g.fill(x,bottom-2,x+thumb,bottom-1,0xFF91E9FF);
    }

    @Inject(method="mouseClicked",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$startHorizontalDrag(double mx,double my,int button,CallbackInfoReturnable<Boolean> cir){
        if(!foreverSkyblock$isShop()||button!=0||foreverSkyblock$maximum()<=0)return;
        int left=foreverSkyblock$field("left"),right=foreverSkyblock$field("right"),bottom=foreverSkyblock$field("bottom");
        if(mx>=left&&mx<right-4&&my>=bottom-5&&my<=bottom+2){foreverSkyblock$horizontalDragging=true;((ScrollPanel)(Object)this).setDragging(true);foreverSkyblock$setFromBar(mx);cir.setReturnValue(true);}
    }
    @Unique private void foreverSkyblock$setFromBar(double mx){
        double maximum=foreverSkyblock$maximum();int left=foreverSkyblock$field("left"),right=foreverSkyblock$field("right"),view=Math.max(1,right-left),content=foreverSkyblock$contentWidth();
        int track=Math.max(1,view-5),thumb=Math.clamp((int)((long)track*view/Math.max(1,content)),24,track),available=Math.max(1,track-thumb);
        foreverSkyblock$horizontal=Math.clamp((mx-left-thumb/2D)*maximum/available,0,maximum);
    }
    @Inject(method="mouseDragged",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$dragHorizontal(double mx,double my,int button,double dx,double dy,CallbackInfoReturnable<Boolean> cir){if(foreverSkyblock$horizontalDragging){foreverSkyblock$setFromBar(mx);cir.setReturnValue(true);}}
    @Inject(method="mouseReleased",at=@At("HEAD"),cancellable=true)
    private void foreverSkyblock$releaseHorizontal(double mx,double my,int button,CallbackInfoReturnable<Boolean> cir){if(foreverSkyblock$horizontalDragging){foreverSkyblock$horizontalDragging=false;((ScrollPanel)(Object)this).setDragging(false);cir.setReturnValue(true);}}
}

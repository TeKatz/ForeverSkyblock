package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.Tesselator;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.compat.IronJetpacksBridge;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.*;
import java.util.*;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.narration.*;
import net.minecraft.client.gui.screens.ConfirmScreen;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.client.gui.widget.ScrollPanel;
import org.lwjgl.glfw.GLFW;

/** Phase-one navigation over the existing server-authoritative progression operations. */
public final class SkillScreen extends Screen {
    private static final int LIGHT=0xFFE8EFF4, MUTED=0xFFABBBC7, CYAN=0xFF7DDDE2, GREEN=0xFF89DBA5;
    private enum Page { OVERVIEW, CHALLENGES, SHOP, RESEARCH, PERKS, PROFILE }
    private Page page=Page.OVERVIEW;
    private final EnumMap<Page,Integer> subsections=new EnumMap<>(Page.class);
    private final Map<String,Float> scrolls=new HashMap<>();
    private ProgressionMenuLayout layout;
    private int x,y;
    private String query="";
    private int filter;
    private EditBox search;
    private CatalogPanel catalog;
    private Card detail;
    private SkillNetwork.Snapshot lastSnapshot;
    private record Card(Component title, Component description, Component badge, ItemStack icon,
                        String skill, String milestone, Runnable action, Component actionText) { }
    public SkillScreen() { super(text("title")); }
    private static Component text(String key,Object... args) { return Component.translatable("gui.forever_skyblock.meta."+key,args); }
    private static Component old(String key,Object... args) { return Component.translatable("gui.forever_skyblock.skills."+key,args); }
    private static Component pageName(Page p) { return text("page."+p.name().toLowerCase(Locale.ROOT)); }
    private int sub() { return subsections.getOrDefault(page,0); }
    private String location() { return page.name()+":"+sub(); }
    private boolean searchable() { return page==Page.CHALLENGES && sub()!=2 && detail==null; }
    private List<String> sections() {
        return switch(page) {
            case CHALLENGES -> List.of("all","forever","contracts");
            case SHOP -> List.of("permanent","current_world","tier_progress");
            case RESEARCH -> List.of("jetpacks");
            default -> List.of();
        };
    }
    private void rememberScroll() { if(catalog!=null && detail==null)scrolls.put(location(),catalog.position()); }
    private void navigate(Page next,int subsection) {
        rememberScroll();page=next;subsections.put(next,subsection);detail=null;rebuildWidgets();
    }
    private Button button(Component title,int bx,int by,int bw,Runnable action) {
        return addRenderableWidget(Button.builder(title,b->action.run()).bounds(bx,by,bw,20).build());
    }
    @Override protected void init() {
        layout=ProgressionMenuLayout.fit(width,height);x=(width-layout.width())/2;y=(height-layout.height())/2;
        clearWidgets();search=null;catalog=null;
        if(layout.compact()) {
            int navWidth=(layout.contentWidth()-6)/2;
            button(pageName(page).copy().append(" >"),x+10,y+42,navWidth,()->navigate(Page.values()[(page.ordinal()+1)%Page.values().length],0));
            addSubnavigation(x+16+navWidth,navWidth);
        } else {
            for(Page p:Page.values()) {
                Button nav=button(pageName(p),x+10,y+42+p.ordinal()*24,116,()->navigate(p,subsections.getOrDefault(p,0)));
                nav.active=p!=page;
            }
            addSubnavigation(x+layout.contentX(),layout.contentWidth());
        }
        if(searchable()) {
            int searchWidth=layout.contentWidth()-110;
            search=new EditBox(font,x+layout.contentX(),y+66,searchWidth,18,text("search"));
            search.setMaxLength(80);search.setHint(text("search"));search.setValue(query);
            search.setResponder(value->{query=value;scrolls.put(location(),0F);replaceCatalog();});
            addRenderableWidget(search);
            button(text("filter."+filter).copy().append(" >"),x+layout.contentX()+searchWidth+6,y+65,104,()->{
                filter=(filter+1)%3;scrolls.put(location(),0F);rebuildWidgets();
            });
        }
        replaceCatalog();
        button(Component.translatable("gui.done"),x+layout.width()-80,y+layout.height()-26,70,this::onClose);
        lastSnapshot=ClientSkills.snapshot();refresh();
    }
    private void addSubnavigation(int bx,int bw) {
        if(detail!=null)button(text("back"),bx,y+42,bw,()->{detail=null;rebuildWidgets();});
        else if(!sections().isEmpty())button(text("sub."+sections().get(sub())).copy().append(sections().size()>1?" >":""),bx,y+42,bw,()->{
            if(sections().size()>1)navigate(page,(sub()+1)%sections().size());
        });
    }
    private void replaceCatalog() {
        if(layout==null)return;
        if(catalog!=null)removeWidget(catalog);
        catalog=addRenderableWidget(new CatalogPanel(cards()));
        if(detail==null)catalog.restore(scrolls.getOrDefault(location(),0F));
    }
    private Card info(String title,String description,ItemStack icon) {
        return new Card(text(title),text(description),Component.empty(),icon,"","",null,Component.empty());
    }
    private Card link(String title,String description,ItemStack icon,Page target,int subsection) {
        return new Card(text(title),text(description),Component.empty(),icon,"","",()->navigate(target,subsection),text("open"));
    }
    private List<Card> cards() {
        if(detail!=null)return List.of(new Card(detail.title(),detail.description(),detail.badge(),detail.icon(),detail.skill(),detail.milestone(),null,Component.empty()));
        var cards=new ArrayList<Card>();var state=ClientSkills.snapshot();boolean ready=state!=null&&state.available();
        switch(page) {
            case OVERVIEW -> {
                long done=ready?MilestoneCatalog.MILESTONES.stream().filter(m->state.milestones().contains(m.id())).count():0;
                cards.add(new Card(text("profile_summary"),ready?text("summary",state.points(),state.perkPoints(),done,MilestoneCatalog.MILESTONES.size(),ClientSkills.jetpackStage()):old("storage_error"),
                        text("live_data"),Items.NETHER_STAR.getDefaultInstance(),"","",null,Component.empty()));
                cards.add(link("page.challenges","overview_challenges",Items.BOOK.getDefaultInstance(),Page.CHALLENGES,0));
                cards.add(link("page.shop","overview_shop",Items.CHEST.getDefaultInstance(),Page.SHOP,0));
                cards.add(link("sub.contracts","contracts_pending",Items.WRITABLE_BOOK.getDefaultInstance(),Page.CHALLENGES,2));
                cards.add(link("page.research","overview_research",Items.FEATHER.getDefaultInstance(),Page.RESEARCH,0));
                cards.add(info("foundation_title","foundation_description",Items.COMPASS.getDefaultInstance()));
            }
            case CHALLENGES -> {
                if(sub()==2)cards.add(info("sub.contracts","contracts_pending",Items.WRITABLE_BOOK.getDefaultInstance()));
                else for(var entry:MilestoneCatalog.MILESTONES) {
                    boolean completed=ready&&state.milestones().contains(entry.id());
                    if(filter==1&&completed||filter==2&&!completed)continue;
                    var title=Component.translatable(entry.titleKey());var description=Component.translatable(entry.descriptionKey());
                    String haystack=(title.getString()+" "+description.getString()).toLowerCase(Locale.ROOT);
                    if(!haystack.contains(query.toLowerCase(Locale.ROOT)))continue;
                    cards.add(new Card(title,description,text("milestone_badge",entry.reward()),entry.icon().getDefaultInstance(),"",entry.id(),null,text("details")));
                }
                if(cards.isEmpty())cards.add(info("no_results","no_results_help",Items.BOOK.getDefaultInstance()));
            }
            case SHOP -> {
                if(sub()==0)for(var skill:SkillCatalog.SKILLS){if(!skill.jetpack())cards.add(skillCard(skill));}
                else if(sub()==1)cards.add(info("sub.current_world","world_pending",Items.CHEST.getDefaultInstance()));
                else {
                    cards.add(info("sub.tier_progress","tiers_pending",Items.COMPASS.getDefaultInstance()));
                    cards.add(info("tier_foundations","tier_foundations_description",Items.COBBLESTONE.getDefaultInstance()));
                    cards.add(info("tier_conqueror","tier_conqueror_description",Items.DRAGON_HEAD.getDefaultInstance()));
                    cards.add(info("tier_engineer","tier_engineer_description",Items.REDSTONE.getDefaultInstance()));
                    cards.add(info("tier_mastery","tier_mastery_description",Items.DIAMOND.getDefaultInstance()));
                    cards.add(info("tier_ascended","tier_ascended_description",ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance()));
                }
            }
            case RESEARCH -> { for(var skill:SkillCatalog.SKILLS)if(skill.jetpack())cards.add(skillCard(skill)); }
            case PERKS -> {
                cards.add(new Card(old("perk_info_title"),old("perk_info_description"),old("core_conversion"),ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance(),"","",null,Component.empty()));
                for(var skill:SkillCatalog.SKILLS)if(skill.perkCost()>0)cards.add(skillCard(skill));
            }
            case PROFILE -> {
                cards.add(new Card(text("profile_summary"),ready?text("profile_values",state.points(),state.perkPoints(),state.skills().size(),state.milestones().size()):old("storage_error"),
                        ready?old(state.local()?"local_scope":"server_scope"):Component.empty(),Items.PLAYER_HEAD.getDefaultInstance(),"","",null,Component.empty()));
                cards.add(info("history_title","history_description",Items.BOOK.getDefaultInstance()));
                cards.add(new Card(old("reset_title"),text("reset_scope"),text("permanent_scope"),Items.GRINDSTONE.getDefaultInstance(),"","",this::confirmReset,old("reset")));
                if(ready&&state.canTestPoints())cards.add(new Card(text("development"),old("test_points_hint"),text("admin_only"),Items.COMMAND_BLOCK.getDefaultInstance(),"","",()->{ClientSkills.testPoints();refresh();},old("test_points")));
            }
        }
        return cards;
    }
    private Card skillCard(SkillCatalog.Skill skill) {
        ItemStack icon=skill.icon().getDefaultInstance();var description=Component.translatable(skill.descriptionKey());
        if(skill.jetpack()) {
            if(JetpackGate.installed()) {
                var variants=IronJetpacksBridge.variants(JetpackResearch.IDS.indexOf(skill.id())+1);
                if(!variants.isEmpty())icon=variants.getFirst();
                description.append("\n").append(old("jetpack_variants",variants.stream().map(v->v.getHoverName().getString()).collect(java.util.stream.Collectors.joining(", "))));
            } else description.append("\n").append(old("missing_jetpacks"));
            if(!skill.prerequisite().isEmpty())description.append("\n").append(old("requires_skill",Component.translatable(SkillCatalog.find(skill.prerequisite()).titleKey())));
        }
        var price=skill.perkCost()>0?old("dual_cost",skill.cost(),skill.perkCost()):old("cost",skill.cost());
        return new Card(Component.translatable(skill.titleKey()),description,text("permanent_scope").copy().append(" · ").append(price),icon,skill.id(),"",null,Component.empty());
    }
    private Component status(Card card) {
        var state=ClientSkills.snapshot();
        if(!card.skill().isEmpty()) {
            var skill=SkillCatalog.find(card.skill());
            String key=state==null||!state.available()?"unavailable":state.rank(skill.id())>=skill.maxRank()?"unlocked"
                    :!skill.available()?"missing_jetpacks":!skill.prerequisite().isEmpty()&&state.rank(skill.prerequisite())==0?"requires_previous"
                    :ClientSkills.pending()?"waiting":state.perkPoints()<skill.perkCost()?"need_perks":state.points()<skill.cost()?"need_points":"unlock_short";
            return old(key,key.equals("need_perks")?skill.perkCost():skill.cost());
        }
        if(!card.milestone().isEmpty())return state==null||!state.available()?old("unavailable"):old(state.milestones().contains(card.milestone())?"completed":"task_open");
        return Component.empty();
    }
    private boolean canBuy(String id) {
        var state=ClientSkills.snapshot();var skill=SkillCatalog.find(id);
        return state!=null&&state.available()&&!ClientSkills.pending()&&skill.available()&&state.rank(id)<skill.maxRank()
                &&(skill.prerequisite().isEmpty()||state.rank(skill.prerequisite())>0)&&state.points()>=skill.cost()&&state.perkPoints()>=skill.perkCost();
    }
    private void confirmReset() {
        var state=ClientSkills.snapshot();if(state==null||!state.available()||ClientSkills.pending())return;
        rememberScroll();
        int sp=SkillCatalog.SKILLS.stream().mapToInt(s->state.rank(s.id())*s.cost()).sum();
        int pp=SkillCatalog.SKILLS.stream().mapToInt(s->state.rank(s.id())*s.perkCost()).sum();
        minecraft.setScreen(new ConfirmScreen(yes->{minecraft.setScreen(this);if(yes){ClientSkills.reset();refresh();}},old("reset_title"),old("reset_description",sp,pp),old("reset_confirm"),Component.translatable("gui.cancel")));
    }
    public void refresh() {
        if(catalog==null)return;
        if(lastSnapshot!=ClientSkills.snapshot()) {
            rememberScroll();lastSnapshot=ClientSkills.snapshot();replaceCatalog();
        }
        catalog.refreshActions();
    }
    @Override public void tick() { refresh(); }
    @Override public boolean isPauseScreen() { return false; }
    @Override public boolean keyPressed(int key,int scan,int modifiers) {
        if(search!=null&&search.isFocused()) {
            if(key==GLFW.GLFW_KEY_ESCAPE){search.setFocused(false);setFocused(null);return true;}
            if(super.keyPressed(key,scan,modifiers))return true;
            // Let subsequent charTyped events handle printable keys, including K.
            return false;
        }
        if(key==GLFW.GLFW_KEY_ESCAPE&&detail!=null){detail=null;rebuildWidgets();return true;}
        if(ClientSkills.OPEN.matches(key,scan)){onClose();return true;}
        return super.keyPressed(key,scan,modifiers);
    }
    @Override public boolean mouseClicked(double mx,double my,int button) {
        if(search!=null) {
            if(button==1&&search.isMouseOver(mx,my)){search.setValue("");return true;}
            if(!search.isMouseOver(mx,my))search.setFocused(false);
        }
        return super.mouseClicked(mx,my,button);
    }
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float tick) {
        super.renderBackground(g,mx,my,tick);
        g.fill(x,y,x+layout.width(),y+layout.height(),0xFF314653);
        g.fill(x+1,y+1,x+layout.width()-1,y+layout.height()-1,0xFF101D27);
        g.fill(x+1,y+1,x+layout.width()-1,y+3,CYAN);
        g.drawString(font,font.plainSubstrByWidth(title.getString(),layout.width()-152),x+10,y+10,LIGHT,false);
        g.drawString(font,font.plainSubstrByWidth(pageName(page).getString(),layout.width()-152),x+10,y+25,MUTED,false);
        var state=ClientSkills.snapshot();
        var balance=state==null||!state.available()?Component.literal("— SP · — PP"):old("balances",state.points(),state.perkPoints());
        g.drawString(font,font.plainSubstrByWidth(balance.getString(),132),x+layout.width()-142,y+17,0xFFFFDA82,false);
        String key=state!=null&&!state.available()?"storage_error":state!=null&&!state.message().isEmpty()?state.message():state!=null&&!state.local()?"server_scope":"local_scope";
        g.drawString(font,font.plainSubstrByWidth(old(key).getString(),layout.width()-102),x+10,y+layout.height()-19,MUTED,false);
    }
    private final class CatalogPanel extends ScrollPanel {
        private final List<Card> cards;
        private final List<Button> actions=new ArrayList<>();
        private final List<Integer> offsets=new ArrayList<>(),heights=new ArrayList<>();
        private int contentHeight;
        CatalogPanel(List<Card> cards) {
            super(SkillScreen.this.minecraft,layout.contentWidth(),layout.cardsHeight(searchable()),y+layout.cardsTop(searchable()),x+layout.contentX(),0,4,0xFF1B2B38,CYAN,0xFFA6EAF0);
            this.cards=List.copyOf(cards);
            for(Card card:cards) {
                offsets.add(contentHeight);
                int h=16+Math.max(18,lines(card.title(),cardWidth()-44)*10)+lines(card.description(),cardWidth()-20)*10
                        +lines(card.badge(),cardWidth()-20)*10+30;
                boolean actionable=!card.skill().isEmpty()||card.action()!=null||!card.milestone().isEmpty()&&detail==null;
                if(actionable)h+=26;
                heights.add(h);contentHeight+=h+7;
                Button b=Button.builder(card.actionText(),ignored->{
                    if(!card.skill().isEmpty()){ClientSkills.buy(card.skill());refreshActions();}
                    else if(card.action()!=null)card.action().run();
                    else if(!card.milestone().isEmpty()){rememberScroll();detail=card;rebuildWidgets();}
                }).bounds(left+10,top,cardWidth()-20,20).build();
                b.visible=actionable;actions.add(b);
            }
            contentHeight=Math.max(height,contentHeight);positionButtons();refreshActions();
        }
        private int lines(Component c,int w){return c.getString().isEmpty()?0:font.split(c,w).size();}
        private int cardWidth(){return width-8;}
        float position(){return scrollDistance;}
        void restore(float value){scrollDistance=Math.clamp(value,0,Math.max(0,contentHeight-height));positionButtons();}
        private void positionButtons(){
            for(int i=0;i<actions.size();i++) {
                var card=cards.get(i);var b=actions.get(i);b.setY(top-(int)scrollDistance+offsets.get(i)+heights.get(i)-28);
                boolean actionable=!card.skill().isEmpty()||card.action()!=null||!card.milestone().isEmpty()&&detail==null;
                b.visible=actionable&&b.getY()+20>top&&b.getY()<bottom;
            }
        }
        void refreshActions(){
            var state=ClientSkills.snapshot();
            for(int i=0;i<cards.size();i++) {
                var c=cards.get(i);var b=actions.get(i);
                if(!c.skill().isEmpty()){b.setMessage(status(c));b.active=canBuy(c.skill());}
                else {
                    b.active=true;
                    if(page==Page.PROFILE&&c.action()!=null)b.active=state!=null&&state.available()&&!ClientSkills.pending();
                }
            }
        }
        @Override protected int getContentHeight(){return contentHeight;}
        @Override protected void drawBackground(GuiGraphics g,Tesselator tess,float tick){g.fill(left,top,right,bottom,0xFF0B151D);}
        private int drawLines(GuiGraphics g,Component text,int dx,int dy,int w,int color){
            if(text.getString().isEmpty())return dy;
            for(var line:font.split(text,w)){g.drawString(font,line,dx,dy,color,false);dy+=10;}return dy;
        }
        @Override protected void drawPanel(GuiGraphics g,int entryRight,int baseY,Tesselator tess,int mx,int my){
            positionButtons();var state=ClientSkills.snapshot();
            for(int i=0;i<cards.size();i++) {
                var c=cards.get(i);int cy=baseY+offsets.get(i),h=heights.get(i),w=cardWidth();
                if(cy>=bottom||cy+h<=top)continue;
                boolean complete=state!=null&&state.available()&&(!c.milestone().isEmpty()?state.milestones().contains(c.milestone()):!c.skill().isEmpty()&&state.rank(c.skill())>0);
                g.fill(left,cy,left+w,cy+h,0xFF2C4352);g.fill(left+1,cy+1,left+w-1,cy+h-1,complete?0xFF152E2A:0xFF1A2A38);
                g.fill(left,cy,left+3,cy+h,complete?GREEN:CYAN);g.renderItem(c.icon(),left+10,cy+9);
                int dy=drawLines(g,c.title(),left+34,cy+8,w-44,LIGHT);
                dy=Math.max(dy,cy+26)+6;dy=drawLines(g,c.description(),left+10,dy,w-20,MUTED)+6;
                dy=drawLines(g,c.badge(),left+10,dy,w-20,CYAN)+4;
                if(c.skill().isEmpty())drawLines(g,status(c),left+10,dy,w-20,complete?GREEN:MUTED);
                actions.get(i).render(g,mx,my,0);
            }
            g.flush();
        }
        @Override public boolean mouseClicked(double mx,double my,int button){positionButtons();return isMouseOver(mx,my)&&super.mouseClicked(mx,my,button);}
        @Override public boolean mouseScrolled(double mx,double my,double horizontal,double vertical){
            if(!isMouseOver(mx,my))return false;boolean result=super.mouseScrolled(mx,my,horizontal,vertical);positionButtons();return result;
        }
        @Override public void setFocused(GuiEventListener listener){
            super.setFocused(listener);int i=actions.indexOf(listener);
            if(i>=0){int b=offsets.get(i)+heights.get(i)-8;if(b-scrollDistance>height)scrollDistance=b-height;if(b-20-scrollDistance<0)scrollDistance=Math.max(0,b-20);positionButtons();}
        }
        @Override public List<? extends GuiEventListener> children(){return actions;}
        @Override public NarratableEntry.NarrationPriority narrationPriority(){return getFocused()!=null?NarratableEntry.NarrationPriority.FOCUSED:NarratableEntry.NarrationPriority.NONE;}
        @Override public void updateNarration(NarrationElementOutput output){
            output.add(NarratedElementType.TITLE,pageName(page));
            for(int i=0;i<actions.size();i++)if(actions.get(i).isFocused())output.add(NarratedElementType.USAGE,cards.get(i).title().copy().append(". ").append(cards.get(i).description()).append(". ").append(actions.get(i).getMessage()));
        }
    }
}

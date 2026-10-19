package de.tekatz.foreverskyblock.client;

import static de.tekatz.foreverskyblock.client.MenuArt.*;
import com.mojang.blaze3d.vertex.Tesselator;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.editor.*;
import de.tekatz.foreverskyblock.editor.EditorDocument.*;
import de.tekatz.foreverskyblock.compat.IronJetpacksBridge;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.*;
import java.util.*;
import java.util.function.BooleanSupplier;
import java.util.function.Supplier;
import net.minecraft.ChatFormatting;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.components.PlayerFaceRenderer;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.narration.*;
import net.minecraft.client.gui.screens.ConfirmScreen;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.client.gui.widget.ScrollPanel;
import org.lwjgl.glfw.GLFW;

/** One presentation owner for geometry, animation and input; progression remains server-authoritative. */
public final class PremiumSkillScreen extends Screen {
    private enum Page { OVERVIEW, CHALLENGES, SHOP, RESEARCH, PERKS, PROFILE }
    private enum Sort { PROGRESS, REWARD, NAME }
    private static final List<String> CATEGORIES = List.of("all", "forever", "ae2", "mekanism", "powah", "mystical_agriculture", "combat", "contracts");
    private final boolean authoringPreview;
    private Screen previewDetail;
    private List<String> categories() {
        if(!EditorClient.customized())return CATEGORIES;
        var result=new ArrayList<String>();result.add("all");
        EditorClient.document().chapters().stream().sorted(Comparator.comparingInt(Chapter::order)).forEach(c->result.add(c.id()));
        if(!result.contains("contracts"))result.add("contracts");
        return result;
    }
    private Page page = Page.CHALLENGES;
    private Sort sort = Sort.PROGRESS;
    private int category, filter, x, y, ticks;
    private boolean grid = true;
    private String query = "";
    private EditBox search;
    private PremiumMenuLayout layout;
    private MenuPanel body, rail;
    private final Map<String, Double> positions = new HashMap<>();
    private long entered = System.nanoTime();
    private SkillNetwork.Snapshot lastSnapshot;
    private record Entry(Component title, Component description, ItemStack icon, String category,
                         Supplier<Component> badge, Supplier<Double> progress, Supplier<Component> status, Runnable open) { }
    private record Detail(Component title, ItemStack icon, int accent, Supplier<Component> summary,
                          Supplier<Component> description, Supplier<Component> progress, Supplier<Double> ratio,
                          Supplier<Component> actionLabel, BooleanSupplier enabled, Runnable action) { }
    private interface Painter { void draw(GuiGraphics g, int left, int top, int width, int mx, int my); }

    public PremiumSkillScreen() { this(false); }
    public PremiumSkillScreen(boolean preview) { super(text("title"));authoringPreview=preview; }
    public void editorReload(){remember();rebuildWidgets();}
    public void editorProgressRefresh(){if(page==Page.CHALLENGES){remember();rebuildBody();}}
    public void editorPage(String value){page=Page.valueOf(value);rebuildWidgets();}
    public Screen editorPreviewDetail(int kind){
        previewDetail=null;
        if(kind==1)openEditorQuest(EditorClient.document().quests().getFirst());
        else if(kind==2)openProduct(ShopCatalog.ADVANCED_AE2_STARTER);
        else if(kind==3)openContract(ContractCatalog.NITRO);
        return previewDetail==null?this:previewDetail;
    }
    public Screen editorActivePreview(){return previewDetail==null?this:previewDetail;}
    @Override public void onClose(){if(!authoringPreview)super.onClose();}
    @Override public void render(GuiGraphics g,int mx,int my,float dt){super.render(g,mx,my,dt);EditorClient.decorations(g,page.name(),x,y);}

    private static Component text(String key, Object... args) { return Component.translatable("gui.forever_skyblock.meta." + key, args); }
    private static Component old(String key, Object... args) { return Component.translatable("gui.forever_skyblock.skills." + key, args); }
    private static Component literal(Object value) { return Component.literal(String.valueOf(value)); }
    private static Component pageName(Page page) { return text("page." + page.name().toLowerCase(Locale.ROOT)); }
    private SkillNetwork.Snapshot state() { return ClientSkills.snapshot(); }
    private boolean ready() { return state() != null && state().available(); }
    private String location() { return page.name() + (page == Page.CHALLENGES ? ":" + category : ""); }
    private Component categoryName(String id) {
        if(EditorClient.customized())for(var chapter:EditorClient.document().chapters())if(chapter.id().equals(id))return literal(chapter.title().value(EditorClient.german()));
        String s = text("sub." + id).getString(); int split = s.indexOf(": ");
        return literal(split < 0 ? s : s.substring(split + 2));
    }
    private static int accent(String id) {
        return switch (id) { case "forever", "mystical_agriculture" -> GREEN; case "ae2" -> 0xFFBD9AF2;
            case "mekanism" -> 0xFF68DFD1; case "powah", "contracts" -> GOLD; case "combat" -> 0xFFFF929D; default -> CYAN; };
    }
    @Override protected void init() {
        layout = PremiumMenuLayout.fit(width, height); EditorClient.beginFrame(layout.width()); category=Math.min(category,categories().size()-1); x = (width-layout.width())/2; y = (height-layout.height())/2;
        search = null; body = rail = null;
        int left = x+layout.padding(), top = y+layout.navTop(), bw = (layout.width()-2*layout.padding()-5)/6;
        for (Page target : Page.values()) {
            final int bx = left+target.ordinal()*(bw+1);
            addRenderableWidget(new MenuButton(bx,top,bw,layout.navHeight(),()->pageName(target),()->true,
                    ()->navigate(target),target == page, target));
        }
        addRenderableWidget(new MenuButton(x+layout.width()-layout.padding()-18,y+layout.padding(),18,18,
                ()->literal("×"),()->true,this::onClose,false,null));
        if(!authoringPreview && (EditorClient.canEdit() || ready()&&state().canTestPoints()))
            addRenderableWidget(new MenuButton(x+layout.width()-layout.padding()-40,y+layout.padding(),18,18,()->literal("✎"),()->true,EditorClient::requestOpen,false,null));
        if (page == Page.CHALLENGES) initChallenges();
        else if (page == Page.SHOP) initShop();
        else if (page == Page.PROFILE || page == Page.OVERVIEW) initProfile();
        else initResearch();
        lastSnapshot = state();
    }
    private void remember() { if (body != null) positions.put(location(), body.motion.target()); if (rail != null) positions.put("rail",rail.motion.target()); }
    private void navigate(Page target) { remember(); page=target; entered=System.nanoTime(); rebuildWidgets(); }
    public void refresh() {
        // Stats refreshes do not reset search focus, animation, graph selection or scroll position.
        var next=state();
        if (next != lastSnapshot && page == Page.CHALLENGES && !"contracts".equals(categories().get(category))
                && (lastSnapshot == null || next == null || !next.milestones().equals(lastSnapshot.milestones()) || !next.progress().equals(lastSnapshot.progress()))) {
            remember(); rebuildBody();
        }
        if(next!=lastSnapshot && page==Page.CHALLENGES && categories().get(category).equals("contracts") && filter!=0
                && (lastSnapshot==null || next==null || !next.contracts().equals(lastSnapshot.contracts()))) {
            remember(); rebuildBody();
        }
        lastSnapshot=next;
    }
    @Override public void tick() { refresh(); if (++ticks % 100 == 0) ClientSkills.refreshMenu(); }
    @Override public boolean isPauseScreen() { return false; }
    @Override public boolean keyPressed(int key,int scan,int modifiers) {
        if (search != null && search.isFocused()) {
            if (key == GLFW.GLFW_KEY_ESCAPE) { search.setFocused(false); setFocused(null); return true; }
            return super.keyPressed(key,scan,modifiers);
        }
        if (ClientSkills.OPEN.matches(key,scan)) { onClose(); return true; }
        return super.keyPressed(key,scan,modifiers);
    }
    @Override public boolean mouseClicked(double mx,double my,int button) {
        if (search != null) {
            if (button == 1 && search.isMouseOver(mx,my)) { search.setValue(""); return true; }
            if (!search.isMouseOver(mx,my)) search.setFocused(false);
        }
        return super.mouseClicked(mx,my,button);
    }
    @Override public void renderBackground(GuiGraphics g,int mx,int my,float tick) {
        g.fill(0,0,width,height,0x94040C14);
        panel(g,x,y,layout.width(),layout.height(),0xEC08151F,BORDER);
        drawHeader(g);
        int l=x+layout.padding(), t=y+layout.contentTop(), w=layout.width()-2*layout.padding();
        panel(g,l,t,w,layout.contentBottom()-layout.contentTop(),0xBC091822,0xFF203D4D);
        int titleLeft=page==Page.CHALLENGES?x+layout.challengeMainX():l;
        int titleW=page==Page.CHALLENGES?layout.challengeMainWidth()-(layout.tiny()?0:112):w;
        centered(g,page==Page.CHALLENGES&&categories().get(category).equals("contracts")?categoryName("contracts"):pageName(page),titleLeft+5,t+3,titleW-10,20,WHITE,.86F);
        if(page==Page.CHALLENGES&&layout.challengeSidebarWidth()>0) {
            int sx=l+6, sw=layout.challengeSidebarWidth()-12;
            if(search!=null)panel(g,search.getX()-5,search.getY()-4,search.getWidth()+10,search.getHeight()+6,0xFF0D2331,BORDER);
        }
        int reveal=(int)((w-16)*MenuMotion.reveal(entered,System.nanoTime(),EditorClient.document().settings().animationMillis()));
        g.fill(l+8,t+24,l+8+reveal,t+25,0xFF326675);
    }
    private void drawHeader(GuiGraphics g) {
        int l=x+layout.padding(), top=y+layout.padding(), h=layout.headerHeight()-5;
        int available=layout.width()-2*layout.padding()-23;
        int brand=layout.tiny()? 0 : layout.compact()? 104:154;
        if (brand>0) {
            centered(g,literal("FOREVER").copy().withStyle(ChatFormatting.BOLD),l,top+5,brand-6,17,WHITE,.95F);
            centered(g,literal("SKYBLOCK"),l,top+22,brand-6,14,CYAN,.85F);
            if(h>55) centered(g,t("progression"),l,top+40,brand-6,10,MUTED,.65F);
        }
        int rem=available-brand, gap=4, sp=(int)(rem*.18), pp=sp, stats=(int)(rem*.25), tier=rem-sp-pp-stats-gap*3;
        int sx=l+brand;
        drawCurrency(g,sx,top,sp,h,0,t("sp"),ready()?literal(state().points()):literal("—"),CYAN); sx+=sp+gap;
        drawCurrency(g,sx,top,pp,h,1,t("pp"),ready()?literal(state().perkPoints()):literal("—"),GOLD); sx+=pp+gap;
        panel(g,sx,top,tier,h,0xB9102532,BORDER);
        if(tier>85) mark(g,sx+8,top+(h-16)/2,2,GOLD);
        int tx=sx+(tier>85?29:4), tw=tier-(tier>85?33:8);
        centered(g,t("tier"),tx,top+3,tw,12,MUTED,.62F);
        centered(g,ready()?text("tier."+state().shopTier()):literal("—"),tx,top+15,tw,15,WHITE,.78F);
        if(h>=44) {
            int count=ready()?completed():0; bar(g,tx,top+h-9,tw,count/(double)MilestoneCatalog.MILESTONES.size(),GOLD);
            if(h>55) centered(g,literal(count+" / "+MilestoneCatalog.MILESTONES.size()),tx,top+34,tw,10,MUTED,.6F);
        }
        sx+=tier+gap;
        panel(g,sx,top,stats,h,0xB9102532,BORDER);
        int rowH=h/2; var st=state()==null?SkillNetwork.MenuStatistics.UNKNOWN:state().statistics();
        statLine(g,sx,top,stats,rowH,3,t("playtime"),playtime(st.playTicks()),CYAN);
        statLine(g,sx,top+rowH,stats,h-rowH,4,t("island"),st.islandLevel()<0?literal("—"):literal(st.islandLevel()+" / "+ChallengeRules.ALL.size()),GREEN);
    }
    private void drawCurrency(GuiGraphics g,int x,int y,int w,int h,int kind,Component title,Component value,int color) {
        panel(g,x,y,w,h,0xB9102532,BORDER);
        boolean icon=w>=57; int lane=icon?22:2;
        if(icon) mark(g,x+5,y+(h-16)/2,kind,color);
        centered(g,title,x+lane,y+4,w-lane-3,12,MUTED,.62F);
        centered(g,value.copy().withStyle(ChatFormatting.BOLD),x+lane,y+18,w-lane-3,h-24,color,h<45?.95F:1.1F);
    }
    private void statLine(GuiGraphics g,int x,int y,int w,int h,int kind,Component title,Component value,int color) {
        boolean icon=w>68; int dx=icon?25:4; if(icon) mark(g,x+5,y+(h-16)/2,kind,color);
        centered(g,title,x+dx,y+1,w-dx-4,10,MUTED,.53F);
        centered(g,value,x+dx,y+11,w-dx-4,Math.max(9,h-12),WHITE,.7F);
    }
    private static Component playtime(long ticks) {
        if(ticks<0) return literal("—"); long minutes=ticks/1200;
        return literal(minutes/60+"h "+String.format(Locale.ROOT,"%02d",minutes%60)+"m");
    }
    private int completed() { if(EditorClient.customized())return EditorClient.completedCount();return (int)MilestoneCatalog.MILESTONES.stream().filter(m->state().milestones().contains(m.id())).count(); }
    private int bodyTop() { return y+layout.contentTop()+31; }
    private MenuPanel newBody(int left,int top,int w) {
        var r=EditorClient.rect("body."+page.name().toLowerCase(Locale.ROOT),left,top,w,Math.max(25,y+layout.contentBottom()-top-5));
        return addRenderableWidget(new MenuPanel(r.x(),r.y(),r.width(),r.height(),location()));
    }
    private void rebuildBody() {
        if(body!=null) removeWidget(body);
        if(page==Page.CHALLENGES) buildChallengeBody();
    }
    private void initChallenges() {
        int side=layout.challengeSidebarWidth();
        if(side>0) {
            int sx=x+layout.padding()+6, w=side-12, sy=bodyTop();
            var searchRect=EditorClient.rect("search",sx+5,sy+4,w-10,14);
            search=new EditBox(font,searchRect.x(),searchRect.y(),searchRect.width(),searchRect.height(),t("search")) {
                @Override public void renderWidget(GuiGraphics g,int mx,int my,float dt) {
                    if(getValue().isEmpty()&&!isFocused())centered(g,t("search"),getX(),getY(),getWidth(),14,MUTED,.75F);
                    else super.renderWidget(g,mx,my,dt);
                }
            };
            search.setBordered(false); search.setMaxLength(80); search.setValue(query); search.setHint(t("search"));
            search.setTextColor(WHITE);
            search.setResponder(v->{query=v; positions.put(location(),0D); rebuildBody();});
            addRenderableWidget(search);
            for(int f=0;f<3;f++) {
                final int chosen=f; int bw=(w-4)/3;
                addRenderableWidget(new MenuButton(sx+f*(bw+2),sy+23,bw,17,()->t("filter."+chosen),()->true,
                        ()->{remember(); filter=chosen; positions.put(location(),0D); rebuildWidgets();},f==filter,null));
            }
            var rr=EditorClient.rect("categories",sx,sy+48,w,Math.max(25,y+layout.contentBottom()-sy-53));
            rail=addRenderableWidget(new MenuPanel(rr.x(),rr.y(),rr.width(),rr.height(),"rail"));
            for(int i=0;i<categories().size();i++) {
                final int chosen=i; String id=categories().get(i); int count="contracts".equals(id)?EditorClient.contracts().size():EditorClient.customized()?(int)EditorClient.document().quests().stream().filter(q->q.enabled()&&(id.equals("all")||id.equals(q.chapter()))).count():"all".equals(id)?MilestoneCatalog.MILESTONES.size():(int)MilestoneCatalog.MILESTONES.stream().filter(m->id.equals(m.category())).count();
                rail.add(new MenuButton(0,i*(24+EditorClient.document().settings().categoryGap()),rail.widthValue()-5,24,()->categoryName(id),()->true,
                        ()->{remember();category=chosen;entered=System.nanoTime();rebuildWidgets();},i==category,null) {
                    @Override protected void renderWidget(GuiGraphics g,int mx,int my,float dt) {
                        panel(g,getX(),getY(),width,height,selected?0xFF173846:isHoveredOrFocused()?0xFF122D3D:0xAA10212C,selected?accent(id):0xFF203B4C);
                        ItemStack icon=PremiumIconResolver.categoryIcon(id);
                        if(!"contracts".equals(id)) g.renderItem(icon,getX()+3,getY()+4);
                        else mark(g,getX()+3,getY()+4,2,GOLD);
                        wrapped(g,getMessage(),getX()+24,getY()+4,width-45,selected?WHITE:MUTED,.62F,2);
                        centered(g,literal(count),getX()+width-20,getY(),17,height,accent(id),.66F);
                    }
                });
            }
            rail.contentHeight=categories().size()*(24+EditorClient.document().settings().categoryGap()); rail.restore();
        } else {
            addRenderableWidget(new MenuButton(x+layout.padding()+6,bodyTop(),110,18,()->categoryName(categories().get(category)),()->true,
                    ()->{remember();category=(category+1)%categories().size();rebuildWidgets();},true,null));
            addRenderableWidget(new MenuButton(x+layout.padding()+120,bodyTop(),65,18,()->t("filter."+filter),()->true,
                    ()->{filter=(filter+1)%3;positions.put(location(),0D);rebuildWidgets();},false,null));
        }
        if(!layout.tiny()) {
            int right=x+layout.width()-layout.padding()-7, top=y+layout.contentTop()+5;
            addRenderableWidget(new MenuButton(right-100,top,66,18,()->t("sort."+sort.name().toLowerCase(Locale.ROOT)),()->true,
                    ()->{sort=Sort.values()[(sort.ordinal()+1)%3];positions.put(location(),0D);rebuildBody();},false,null));
            addRenderableWidget(new MenuButton(right-30,top,30,18,()->t(grid?"list":"grid"),()->true,
                    ()->{remember();grid=!grid;positions.put(location(),0D);rebuildWidgets();},false,null));
        }
        buildChallengeBody();
    }
    private double ratio(MilestoneCatalog.Milestone m) { return !ready()?0:state().milestones().contains(m.id())?1:Math.clamp(state().progress().getOrDefault(m.id(),0)/(double)m.target(),0,1); }
    private Component challengeProgress(MilestoneCatalog.Milestone m) { return literal((ratio(m)==1?m.target():ready()?Math.max(0,state().progress().getOrDefault(m.id(),0)):0)+" / "+m.target()); }
    private void buildChallengeBody() {
        int top=bodyTop()+(layout.tiny()?22:0), left=x+layout.challengeMainX()+5, w=layout.challengeMainWidth()-10;
        body=newBody(left,top,w);
        if("contracts".equals(categories().get(category))) { buildContracts(); return; }
        if(EditorClient.customized()){buildEditorQuests();return;}
        var entries=new ArrayList<MilestoneCatalog.Milestone>();
        for(var m:MilestoneCatalog.MILESTONES) {
            boolean complete=ready()&&state().milestones().contains(m.id());
            if(category>0&&!categories().get(category).equals(m.category())) continue;
            if(filter==1&&complete||filter==2&&!complete)continue;
            if(!(Component.translatable(m.titleKey()).getString()+" "+Component.translatable(m.descriptionKey()).getString()).toLowerCase(Locale.ROOT).contains(query.toLowerCase(Locale.ROOT)))continue;
            entries.add(m);
        }
        Comparator<MilestoneCatalog.Milestone> order=switch(sort) {
            case NAME -> Comparator.comparing(m->Component.translatable(m.titleKey()).getString(),String.CASE_INSENSITIVE_ORDER);
            case REWARD -> Comparator.comparingInt(MilestoneCatalog.Milestone::reward).reversed();
            case PROGRESS -> Comparator.comparing((MilestoneCatalog.Milestone m)->ratio(m)==1).thenComparingDouble(m->-ratio(m));
        }; entries.sort(order);
        List<Entry> cards=new ArrayList<>();
        for(var m:entries) cards.add(new Entry(Component.translatable(m.titleKey()),Component.translatable(m.descriptionKey()),PremiumIconResolver.challengeIcon(m),m.category(),
                ()->literal("+"+m.reward()+" SP"),()->ratio(m),()->challengeProgress(m),()->openChallenge(m)));
        buildCards(cards,grid?layout.challengeColumns():1, false);
    }
    private boolean german() { return minecraft.getLanguageManager().getSelected().startsWith("de_"); }
    private Component contractTitle(ContractCatalog.Contract c) { return literal(german()?c.titleDe():c.titleEn()); }
    private Component contractDescription(ContractCatalog.Contract c) { return literal(german()?c.descriptionDe():c.descriptionEn()); }
    private SkillNetwork.ContractState contractState(ContractCatalog.Contract c) { return state()==null?null:state().contracts().get(c.id()); }
    private Component contractStatus(ContractCatalog.Contract c) {
        var s=contractState(c); if(!ready())return old("unavailable");
        if(s==null||!s.available()) return state().shopTier()<c.requiredTier()?text("world_requires_tier",text("tier."+c.requiredTier())):t("integration_missing");
        return t(c.resource()?"inventory_progress":"action_progress",s.progress(),c.target());
    }
    private void buildContracts() {
        var cards=new ArrayList<Entry>();
        for(var c:EditorClient.contracts()) {
            var configured=EditorClient.document().contract(c.id());if(configured!=null&&!configured.enabled())continue;
            if(!(contractTitle(c).getString()+" "+contractDescription(c).getString()).toLowerCase(Locale.ROOT).contains(query.toLowerCase(Locale.ROOT)))continue;
            var cs=contractState(c); if(filter==1&&cs!=null&&cs.completions()>0||filter==2&&(cs==null||cs.completions()==0))continue;
            cards.add(new Entry(contractTitle(c),contractDescription(c),item(c.iconId()),"contracts",()->literal("+"+c.rewardSp()+" SP"),
                    ()->contractState(c)==null?0:contractState(c).progress()/(double)c.target(),()->contractStatus(c),()->openContract(c)));
        }
        buildCards(cards,body.widthValue()>=300?2:1,true);
    }
    private void buildCards(List<Entry> entries,int columns,boolean contract) {
        var cardStyle=EditorClient.style("card");
        int gap=EditorClient.document().settings().cardGap(),cw=cardStyle.width()>0?cardStyle.width():(body.widthValue()-5-gap*(columns-1))/columns;
        if(cardStyle.width()>0)columns=Math.max(1,(body.widthValue()-5+gap)/(cw+gap));
        int ch=cardStyle.height()>0?cardStyle.height():contract?100:Math.max(84,layout.challengeCardHeight()+15);
        if(entries.isEmpty()) { body.paint=(g,l,t,w,mx,my)->centered(g,text("no_results"),l,t+12,w,30,MUTED,.8F);body.restore();return; }
        for(int i=0;i<entries.size();i++) {
            var entry=entries.get(i); int dx=(i%columns)*(cw+gap)+cardStyle.x(),dy=(i/columns)*(ch+gap)+cardStyle.y();
            body.add(new MenuButton(dx,dy,cw,ch,()->entry.title(),()->!cardStyle.hidden(),entry.open(),false,null) {
                @Override protected boolean clicked(double mx,double my){return !cardStyle.hidden()&&super.clicked(mx,my);}
                @Override protected void renderWidget(GuiGraphics g,int mx,int my,float dt) {
                    int c=entry.progress().get()>=1?GREEN:accent(entry.category());
                    if(cardStyle.hidden())return;
                    EditorClient.capture("card",getX(),getY(),width,height);
                    panel(g,getX(),getY(),width,height,EditorCodec.color(cardStyle.fill(),isHoveredOrFocused()?0xFF142E3E:0xF00D202D),EditorCodec.color(cardStyle.border(),isHoveredOrFocused()?c:BORDER));
                    g.fill(getX()+1,getY()+1,getX()+3,getY()+height-1,c);
                    EditorClient.item(g,entry.icon(),getX()+8,getY()+9,"card.icon");
                    EditorClient.text(g,entry.title().copy().withStyle(ChatFormatting.BOLD),getX()+30,getY()+8,width-38,20,EditorCodec.color(cardStyle.text(),WHITE),.72F*cardStyle.fontScale(),"card.title",true);
                    EditorClient.text(g,entry.description(),getX()+8,getY()+31,width-16,18,MUTED,.65F*cardStyle.fontScale(),"card.description",true);
                    int footer=getY()+height-26,badgeW=Math.min(width/2,Math.max(38,Math.round(font.width(entry.badge().get())*.68F)+12));
                    label(g,entry.status().get(),getX()+8,footer,width-badgeW-23,c,.62F);
                    bar(g,getX()+8,footer+12,width-badgeW-24,entry.progress().get(),c);
                    panel(g,getX()+width-badgeW-7,footer-1,badgeW,18,0xFF14384D,0xFF376578);
                    EditorClient.text(g,entry.badge().get(),getX()+width-badgeW-7,footer-1,badgeW,18,WHITE,.69F,"card.reward",false);
                }
            });
        }
        body.contentHeight=((entries.size()+columns-1)/columns)*(ch+gap);body.restore();
    }
    private boolean editorComplete(Quest q){if(EditorClient.simulation()>0)return EditorClient.simulation()==3;return q.legacy()?ready()&&state().milestones().contains(q.id()):EditorClient.state(q.id()).complete();}
    private boolean editorUnlocked(Quest q){
        if(EditorClient.simulation()>0)return EditorClient.simulation()!=1;
        if(!q.enabled())return false;
        if(!q.legacy())return EditorClient.state(q.id()).unlocked();
        if(!ready()||state().shopTier()<q.requiredTier())return false;
        var completed=new HashSet<>(state().milestones());for(var other:EditorClient.document().quests())if(EditorClient.state(other.id()).complete())completed.add(other.id());
        return QuestRules.dependencies(q.requires(),q.prerequisiteMatch(),completed);
    }
    private double editorRatio(Quest q){
        if(EditorClient.simulation()>0)return EditorClient.simulation()==3?1:EditorClient.simulation()==2?.35:0;
        if(editorComplete(q))return 1;
        if(q.legacy())return !ready()?0:Math.clamp(state().progress().getOrDefault(q.id(),0)/(double)q.tasks().getFirst().count(),0,1);
        var counts=EditorClient.state(q.id()).counts();
        var ratios=q.tasks().stream().mapToDouble(t->Math.clamp(counts.getOrDefault(t.id(),0)/(double)t.count(),0,1));
        return q.taskMatch()==Match.ANY?ratios.max().orElse(0):ratios.average().orElse(0);
    }
    private Component questTitle(Quest q){return literal(q.title().value(EditorClient.german()));}
    private Component questDescription(Quest q){return literal(q.description().value(EditorClient.german()));}
    private Component questReward(Quest q){return literal("+"+q.rewardSp()+" SP"+(q.rewardPp()>0?" · +"+q.rewardPp()+" PP":""));}
    private Component questStatus(Quest q){return !editorUnlocked(q)&&!editorComplete(q)?literal(EditorClient.german()?"Gesperrt":"Locked"):literal(Math.round(editorRatio(q)*100)+"%");}
    private void buildEditorQuests(){
        var quests=new ArrayList<Quest>();String chapter=categories().get(category);
        for(var q:EditorClient.document().quests()){
            if(!q.enabled()||!chapter.equals("all")&&!q.chapter().equals(chapter))continue;
            if(filter==1&&editorComplete(q)||filter==2&&!editorComplete(q))continue;
            if(!(questTitle(q).getString()+" "+questDescription(q).getString()).toLowerCase(Locale.ROOT).contains(query.toLowerCase(Locale.ROOT)))continue;
            quests.add(q);
        }
        quests.sort(switch(sort){case NAME->Comparator.comparing(q->questTitle(q).getString(),String.CASE_INSENSITIVE_ORDER);case REWARD->Comparator.comparingInt(Quest::rewardSp).reversed();case PROGRESS->Comparator.comparing((Quest q)->editorComplete(q)).thenComparingDouble(q->-editorRatio(q));});
        if(EditorClient.document().settings().questGraph()){
            int w=body.widthValue()-5,cols=Math.max(1,Math.min(3,w/145));
            body.paint=(g,l,t,pw,mx,my)->{
                for(int i=0;i<quests.size();i++){var q=quests.get(i);var b=questPoint(q.id(),i,cols,w);for(String dep:q.requires()){int j=-1;for(int k=0;k<quests.size();k++)if(quests.get(k).id().equals(dep)){j=k;break;}if(j>=0){var a=questPoint(dep,j,cols,w);arrow(g,l+a[0],t+a[1],l+b[0],t+b[1],18,q.prerequisiteMatch()==Match.ANY?GOLD:BORDER);}}}
            };
            int bottom=0;for(int i=0;i<quests.size();i++){var q=quests.get(i);var point=questPoint(q.id(),i,cols,w);addNode(q.id(),point[0],point[1],()->questTitle(q),()->questReward(q),item(q.icon()),()->editorComplete(q),editorUnlocked(q)?CYAN:DIM,()->openEditorQuest(q),Math.max(80,w/cols-12));bottom=Math.max(bottom,point[1]+95);}
            body.contentHeight=Math.max(body.getContentHeight(),bottom);body.restore();return;
        }
        var entries=new ArrayList<Entry>();for(var q:quests)entries.add(new Entry(questTitle(q),questDescription(q),item(q.icon()),q.chapter(),()->questReward(q),()->editorRatio(q),()->questStatus(q),()->openEditorQuest(q)));
        buildCards(entries,grid?layout.challengeColumns():1,false);
    }
    private int[] questPoint(String id,int i,int cols,int w){var node=GraphGeometry.point(EditorClient.document(),id,false);return new int[]{Math.round(node.x()*w/640F),Math.round(node.y())};}
    private void openEditorQuest(Quest q){
        open(new Detail(questTitle(q),item(q.icon()),accent(q.chapter()),()->questReward(q).copy().append(" · ").append(categoryName(q.chapter())),
                ()->{var desc=questDescription(q).copy().append("\n\n");var progress=EditorClient.state(q.id()).counts();
                    for(var task:q.tasks())desc.append("\n• "+task.type()+" · "+task.target()+" · "+(q.legacy()?ready()?state().progress().getOrDefault(q.id(),0):0:progress.getOrDefault(task.id(),0))+" / "+task.count());
                    if(!q.requires().isEmpty()){desc.append("\n\n"+(EditorClient.german()?"Voraussetzungen":"Prerequisites")+" ("+q.prerequisiteMatch()+"):");for(String id:q.requires()){var dependency=EditorClient.document().quest(id);desc.append("\n• "+(dependency==null?id:dependency.title().value(EditorClient.german())));}}
                    return desc;},()->questStatus(q),()->editorRatio(q),()->t("close"),()->true,()->minecraft.setScreen(this)));
    }
    private void openChallenge(MilestoneCatalog.Milestone m) {
        open(new Detail(Component.translatable(m.titleKey()),PremiumIconResolver.challengeIcon(m),accent(m.category()),
                ()->categoryName(m.category()).copy().append("  ·  +"+m.reward()+" SP"),
                ()->{
                    var description=Component.translatable(m.descriptionKey()).copy().append("\n\n").append(t("challenge_rules"));
                    var d=ChallengeRules.find(m.id());
                    if(d!=null&&d.target()>1&&!d.objectives().isEmpty()) {
                        description.append("\n\n").append(t("objectives"));
                        d.objectives().stream().sorted().forEach(id->description.append("\n• ").append(item(id).getHoverName()));
                    }
                    return description;
                },()->challengeProgress(m),()->ratio(m),()->t("close"),()->true,()->minecraft.setScreen(this)));
    }
    private void openContract(ContractCatalog.Contract c) {
        open(new Detail(contractTitle(c),item(c.iconId()),GOLD,()->literal("+"+c.rewardSp()+" SP  ·  ").copy().append(text("tier."+c.requiredTier())),
                ()->contractDescription(c).copy().append("\n\n").append(t(c.resource()?"contract_consumes":"contract_auto"))
                        .append("\n\n").append(t("completions",contractState(c)==null?0:contractState(c).completions())),
                ()->contractStatus(c),()->contractState(c)==null?0:contractState(c).progress()/(double)c.target(),
                ()->ClientSkills.pending()?old("waiting"):t(c.resource()?"submit":"automatic"),
                ()->c.resource()&&ready()&&!ClientSkills.pending()&&contractState(c)!=null&&contractState(c).canSubmit(),
                ()->ClientSkills.submitContract(c.id())));
    }
    private void initShop() {
        body=newBody(x+layout.padding()+7,bodyTop(),layout.width()-2*layout.padding()-14);
        int w=body.widthValue()-5, center=w*5/8;
        body.paint=(g,l,t,w0,mx,my)->{
            int a=ready()&&state().shopTier()>=0?0xFF4B8A8B:BORDER,b=ready()&&state().shopTier()>=1?0xFF4B8A8B:BORDER;
            shopEdge(g,l,t,"shop:permanent",w/8,22,"shop:reach",w/8,108,0xFF3B6576);
            shopEdge(g,l,t,"shop:tier0",center,22,"shop:tier1",center,245,b);
            String[] ids={ShopCatalog.NO_PHANTOMS.id(),ShopCatalog.NO_RAIN.id(),ShopCatalog.PERMANENT_DAY.id()};
            for(int i=0;i<ids.length;i++)shopEdge(g,l,t,"shop:tier0",center,22,ids[i],w*(2*(i+1)+1)/8,108,a);
            shopEdge(g,l,t,"shop:tier1",center,245,ShopCatalog.ADVANCED_AE2_STARTER.id(),center,326,b);
            for(var product:EditorClient.document().products())for(String prerequisite:product.requires()){
                var a0=GraphGeometry.point(EditorClient.document(),"shop:quest/"+prerequisite,true);var b0=GraphGeometry.point(EditorClient.document(),product.id(),true);
                shopEdge(g,l,t,"shop:quest/"+prerequisite,Math.round(a0.x()*w/640F),Math.round(a0.y()),product.id(),Math.round(b0.x()*w/640F),Math.round(b0.y()),GOLD);
            }
            for(var product:EditorClient.document().products())if(product.id().startsWith("forever_skyblock:shop/custom/")){
                var b0=GraphGeometry.point(EditorClient.document(),product.id(),true);shopEdge(g,l,t,product.requiredTier()==0?"shop:tier0":"shop:tier1",center,product.requiredTier()==0?22:245,product.id(),Math.round(b0.x()*w/640F),Math.round(b0.y()),BORDER);
            }
            centered(g,t("tree_hint"),l+5,t+383,w-10,15,MUTED,.66F);
        };
        addNode("shop:permanent",w/8,22,()->t("permanent_root"),()->t("sp_only"),Items.NETHER_STAR.getDefaultInstance(),
                ()->ready(),CYAN,()->navigate(Page.PROFILE),w/4-9);
        addNode("shop:tier0",center,22,()->text("tier.0"),()->Component.translatable("milestone.forever_skyblock.first_foundations"),Items.COBBLESTONE.getDefaultInstance(),
                ()->ready()&&state().shopTier()>=0,GOLD,()->openTier(0),Math.min(190,w-10));
        var reach=SkillCatalog.find(SkillCatalog.EXTENDED_REACH);
        addNode("shop:reach",w/8,108,()->Component.translatable(reach.titleKey()),()->old("cost",reach.cost()),reach.icon().getDefaultInstance(),
                ()->ready()&&state().rank(reach.id())>0,CYAN,()->openSkill(reach),w/4-9);
        var products=List.of(ShopCatalog.NO_PHANTOMS,ShopCatalog.NO_RAIN,ShopCatalog.PERMANENT_DAY).stream().map(p->EditorClient.product(p.id())).toList();
        for(int i=0;i<products.size();i++) {
            var p=products.get(i);if(!productVisible(p))continue;
            addNode(p.id(),w*(2*(i+1)+1)/8,108,()->productTitle(p),()->literal(p.spCost()+" SP"),productIcon(p),
                    ()->productStage(p)>=3,GREEN,()->openProduct(p),w/4-9);
        }
        addNode("shop:tier1",center,245,()->text("tier.1"),()->Component.translatable("milestone.forever_skyblock.dragon_slayer"),Items.DRAGON_HEAD.getDefaultInstance(),
                ()->ready()&&state().shopTier()>=1,GOLD,()->openTier(1),Math.min(190,w-10));
        var p=EditorClient.product(ShopCatalog.ADVANCED_AE2_STARTER.id());
        addNode(p.id(),center,326,()->productTitle(p),()->literal(p.spCost()+" SP"),productIcon(p),
                ()->productStage(p)>0,0xFFBD9AF2,()->openProduct(p),Math.min(205,w-10));
        // The operator transaction probe remains accessible without polluting the player-facing tree.
        if(ready()&&state().canTestPoints()) body.add(new MenuButton(5,409,Math.min(215,w-10),20,()->t("development_product"),()->true,()->openProduct(ShopCatalog.TEST),false,null));
        int extra=430;
        for(var custom:EditorClient.products())if(productVisible(custom)&&ShopCatalog.PRODUCTS.stream().noneMatch(base->base.id().equals(custom.id()))){
            int row=extra;addNode(custom.id(),center,row,()->productTitle(custom),()->literal(custom.spCost()+" SP"),productIcon(custom),()->productStage(custom)>0,0xFFBD9AF2,()->openProduct(custom),Math.min(205,w-10));extra+=105;
        }
        for(String id:GraphGeometry.shopQuests(EditorClient.document())){var quest=EditorClient.document().quest(id);var point=GraphGeometry.point(EditorClient.document(),"shop:quest/"+id,true);addNode("shop:quest/"+id,Math.round(point.x()*w/640F),Math.round(point.y()),()->questTitle(quest),()->questStatus(quest),item(quest.icon()),()->editorComplete(quest),GOLD,()->openEditorQuest(quest),Math.max(80,w/4-9));}
        body.contentHeight=Math.max(body.contentHeight,Math.max(ready()&&state().canTestPoints()?439:405,extra>430?extra:0));body.restore();
    }
    private void shopEdge(GuiGraphics g,int left,int top,String a,int ax,int ay,String b,int bx,int by,int color){
        var product=EditorClient.product(b);if(product!=null&&!productVisible(product))return;
        var na=EditorClient.document().nodes().get(a);var nb=EditorClient.document().nodes().get(b);int w=body.widthValue()-5;
        if(na!=null){ax=Math.round(na.x()*w/640F);ay=Math.round(na.y());}if(nb!=null){bx=Math.round(nb.x()*w/640F);by=Math.round(nb.y());}
        int middle=(ay+by)/2;line(g,left+ax,top+ay,left+ax,top+middle,color);line(g,left+ax,top+middle,left+bx,top+middle,color);line(g,left+bx,top+middle,left+bx,top+by,color);
    }
    private void addNode(String nodeId,int cx,int cy,Supplier<Component> title,Supplier<Component> subtitle,ItemStack icon,BooleanSupplier owned,int color,Runnable action,int labelWidth) {
        var position=EditorClient.document().nodes().get(nodeId);
        if(position!=null){cx=Math.round(position.x()*(body.widthValue()-5)/640F);cy=Math.round(position.y());}
        final int radius=position==null?18:position.radius();
        body.contentHeight=Math.max(body.contentHeight,cy+95);
        body.add(new MenuButton(cx-radius,cy-radius,radius*2+1,Math.max(88,radius*2+1),title,()->true,action,false,null) {
            @Override public boolean isMouseOver(double mx,double my) { return visible&&MenuMotion.insideNode(mx,my,getX()+radius,getY()+radius,radius); }
            @Override protected boolean clicked(double mx,double my) { return active&&isMouseOver(mx,my); }
            @Override protected void renderWidget(GuiGraphics g,int mx,int my,float dt) {
                int nx=getX()+radius,ny=getY()+radius,c=owned.getAsBoolean()?GREEN:color;
                boolean hover=isMouseOver(mx,my)||isFocused();
                if(hover)circle(g,nx,ny,radius+3,0x443EC4D0);
                circle(g,nx,ny,radius,hover||owned.getAsBoolean()?c:BORDER);
                circle(g,nx,ny,radius-2,owned.getAsBoolean()?0xFF143C35:0xFF122B39);
                g.renderItem(icon,nx-8,ny-8);
                if(owned.getAsBoolean()) { circle(g,nx+13,ny-12,4,GREEN); }
                // Up to three centered lines; the next node row is deliberately more than 80 px away.
                int yy=ny+25;
                for(var line:font.split(title.get(),Math.max(1,(int)(labelWidth/.72F)))) {
                    if(yy>ny+34)break;
                    int fw=Math.round(font.width(line)*.72F);
                    g.fill(nx-fw/2-2,yy-1,nx+fw/2+3,yy+8,0xFF091822);
                    g.pose().pushPose();g.pose().translate(nx-fw/2,yy,0);g.pose().scale(.72F,.72F,1);g.drawString(font,line,0,0,WHITE,false);g.pose().popPose();yy+=8;
                }
                int sw=Math.min(labelWidth,Math.round(font.width(subtitle.get().getString())*.62F));
                g.fill(nx-sw/2-2,ny+43,nx+sw/2+3,ny+54,0xFF091822);
                centered(g,subtitle.get(),nx-labelWidth/2,ny+43,labelWidth,10,owned.getAsBoolean()?GREEN:MUTED,.62F);
            }
        });
    }
    private void openTier(int tier) {
        var milestone=tier==0?MilestoneCatalog.MILESTONES.stream().filter(m->m.id().equals(ChallengeRules.FOUNDATIONS.id())).findFirst().orElseThrow():
                MilestoneCatalog.MILESTONES.stream().filter(m->m.id().equals(ChallengeRules.DRAGON.id())).findFirst().orElseThrow();
        open(new Detail(text("tier."+tier),tier==0?Items.COBBLESTONE.getDefaultInstance():Items.DRAGON_HEAD.getDefaultInstance(),GOLD,
                ()->text("permanent_scope"),()->text(tier==0?"tier_foundations_description":"tier_conqueror_description").copy().append("\n\n").append(t("tier_rules")),
                ()->t(ready()&&state().shopTier()>=tier?"tier_unlocked":"tier_requirement",Component.translatable(milestone.titleKey())),
                ()->ready()&&state().shopTier()>=tier?1D:ratio(milestone),()->text("page.challenges"),()->true,()->{minecraft.setScreen(this);navigate(Page.CHALLENGES);}));
    }
    private Component productTitle(ShopCatalog.Product p) {return literal(german()?p.titleDe():p.titleEn());}
    private int productStage(ShopCatalog.Product p) {if(EditorClient.simulation()>0)return EditorClient.simulation()==3?6:0;var s=state()==null?null:state().products().get(p.id());return s==null?0:s.stage();}
    private boolean productVisible(ShopCatalog.Product p){var configured=EditorClient.document().product(p.id());return configured==null||configured.enabled()||productStage(p)>0;}
    private ItemStack productIcon(ShopCatalog.Product p) {
        if(EditorClient.customized()){var custom=EditorClient.document().product(p.id());if(custom!=null)return item(custom.icon());}
        if(p.id().equals(ShopCatalog.NO_PHANTOMS.id()))return Items.PHANTOM_MEMBRANE.getDefaultInstance();
        if(p.id().equals(ShopCatalog.NO_RAIN.id()))return Items.WATER_BUCKET.getDefaultInstance();
        if(p.id().equals(ShopCatalog.PERMANENT_DAY.id()))return Items.SUNFLOWER.getDefaultInstance();
        return p.developmentOnly()?Items.PAPER.getDefaultInstance():ForeverSkyblock.WORLD_SUPPLY_CACHE.get().getDefaultInstance();
    }
    private boolean canBuyProduct(String id) {
        var p=EditorClient.product(id); if(!ready()||p==null||!state().worldAvailable()||ClientSkills.pending())return false;
        var ps=state().products().get(id);
        if(ps==null||!ps.available()||p.developmentOnly()&&!state().canTestPoints())return false;
        if(p.deliverable()&&ps.stage()==1)return true;
        return ps.stage()==0&&state().shopTier()>=p.requiredTier()&&state().points()>=p.spCost()&&state().perkPoints()>=p.ppCost();
    }
    private Component productStatus(String id) {
        var p=EditorClient.product(id);if(!ready()||p==null)return old("unavailable");
        if(ClientSkills.pending())return old("waiting");
        if(!state().worldAvailable())return text("world_unavailable");
        var ps=state().products().get(id);if(ps==null||!ps.available())return t("integration_missing");
        return switch(ps.stage()) {
            case 1 -> t("claim_package"); case 2,4 -> t("recovering"); case 3 -> t("package_delivered");case 5 -> t("package_opened");
            case 6 -> t(p.worldUpgrade()?"active":"claimed");
            default -> state().shopTier()<p.requiredTier()?text("world_requires_tier",text("tier."+p.requiredTier())):
                    state().perkPoints()<p.ppCost()?old("need_perks",p.ppCost()):state().points()<p.spCost()?old("need_points",p.spCost()):text("purchase");
        };
    }
    private void openProduct(ShopCatalog.Product p) {
        open(new Detail(productTitle(p),productIcon(p),p.worldUpgrade()?GREEN:0xFFBD9AF2,
                ()->literal(p.spCost()+" SP  ·  ").copy().append(text("tier."+p.requiredTier())).append("  ·  ").append(text("current_world_scope")),
                ()->{
                    var desc=literal(german()?p.descriptionDe():p.descriptionEn()).copy().append("\n\n").append(t(p.developmentOnly()?"development_rules":p.worldUpgrade()?"world_upgrade_rules":"world_package_rules"));
                    var customized=EditorClient.document().product(p.id());
                    if(customized!=null&&!customized.requires().isEmpty()){desc.append("\n\n"+(EditorClient.german()?"Voraussetzungen:":"Prerequisites:"));for(String id:customized.requires())desc.append("\n• "+EditorClient.document().quest(id).title().value(EditorClient.german()));}
                    if(!p.contents().isEmpty()) {desc.append("\n\n").append(t("contents"));for(var e:p.contents())desc.append("\n"+e.count()+" × ").append(item(e.itemId()).getHoverName());}
                    return desc;
                },()->productStatus(p.id()),()->productStage(p)>0?1D:0D,()->productStatus(p.id()),()->canBuyProduct(p.id()),()->confirmProduct(p.id())));
    }
    private void confirmProduct(String id) {
        if(authoringPreview)return;
        var p=EditorClient.product(id);if(p==null||!canBuyProduct(id))return;
        if(p.deliverable()&&productStage(p)==1) {ClientSkills.claimProduct(id);return;}
        Screen returnTo=minecraft.screen;
        minecraft.setScreen(new ConfirmScreen(yes->{minecraft.setScreen(returnTo);if(yes)ClientSkills.buyProduct(id);},
                t("purchase_title"),t(p.worldUpgrade()?"confirm_upgrade":"confirm_package",productTitle(p),p.spCost(),state().worldName()),text("purchase_confirm"),Component.translatable("gui.cancel")));
    }
    private boolean canBuySkill(String id) {
        var s=SkillCatalog.find(id);
        return ready()&&s!=null&&!ClientSkills.pending()&&s.available()&&state().rank(id)<s.maxRank()
                &&(s.prerequisite().isEmpty()||state().rank(s.prerequisite())>0)&&state().points()>=s.cost()&&state().perkPoints()>=s.perkCost();
    }
    private Component skillStatus(String id) {
        var s=SkillCatalog.find(id);if(!ready())return old("unavailable");
        if(state().rank(id)>=s.maxRank())return old("unlocked");if(!s.available())return old("missing_jetpacks");
        if(ClientSkills.pending())return old("waiting");if(!s.prerequisite().isEmpty()&&state().rank(s.prerequisite())==0)return old("requires_previous");
        if(state().perkPoints()<s.perkCost())return old("need_perks",s.perkCost());if(state().points()<s.cost())return old("need_points",s.cost());return old("unlock_short",s.cost());
    }
    private ItemStack skillIcon(SkillCatalog.Skill s) {
        if(s.jetpack()&&JetpackGate.installed()) {var variants=IronJetpacksBridge.variants(JetpackResearch.IDS.indexOf(s.id())+1);if(!variants.isEmpty())return variants.getFirst();}
        return s.icon().getDefaultInstance();
    }
    private void openSkill(SkillCatalog.Skill s) {
        open(new Detail(Component.translatable(s.titleKey()),skillIcon(s),CYAN,
                ()->text("permanent_scope").copy().append("  ·  ").append(s.perkCost()>0?old("dual_cost",s.cost(),s.perkCost()):old("cost",s.cost())),
                ()->{var d=Component.translatable(s.descriptionKey()).copy();
                    if(!s.prerequisite().isEmpty())d.append("\n\n").append(old("requires_skill",Component.translatable(SkillCatalog.find(s.prerequisite()).titleKey())));
                    if(s.jetpack()) {
                        if(!JetpackGate.installed())d.append("\n\n").append(old("missing_jetpacks"));
                        else d.append("\n\n").append(old("jetpack_variants",IronJetpacksBridge.variants(JetpackResearch.IDS.indexOf(s.id())+1).stream().map(v->v.getHoverName().getString()).collect(java.util.stream.Collectors.joining(", "))));
                    }return d;},()->skillStatus(s.id()),()->ready()&&state().rank(s.id())>0?1D:0D,
                ()->skillStatus(s.id()),()->canBuySkill(s.id()),()->ClientSkills.buy(s.id())));
    }
    private void initResearch() {
        body=newBody(x+layout.padding()+7,bodyTop(),layout.width()-2*layout.padding()-14);
        var entries=new ArrayList<Entry>();
        if(page==Page.PERKS) {
            entries.add(new Entry(old("perk_info_title"),old("perk_info_description"),ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance(),"all",
                    ()->literal("PP"),()->0D,()->old("core_conversion"),()->open(new Detail(old("perk_info_title"),ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance(),GOLD,
                        ()->old("core_conversion"),()->old("perk_info_description"),()->old("core_conversion"),()->0D,()->t("close"),()->true,()->minecraft.setScreen(this)))));
        }
        for(var s:SkillCatalog.SKILLS)if(page==Page.RESEARCH?s.jetpack():s.perkCost()>0)entries.add(new Entry(Component.translatable(s.titleKey()),Component.translatable(s.descriptionKey()),skillIcon(s),"all",
                ()->s.perkCost()>0?old("dual_cost",s.cost(),s.perkCost()):old("cost",s.cost()),()->ready()&&state().rank(s.id())>0?1D:0D,()->skillStatus(s.id()),()->openSkill(s)));
        buildCards(entries,body.widthValue()>440?2:1,false);
    }
    private void initProfile() {
        body=newBody(x+layout.padding()+7,bodyTop(),layout.width()-2*layout.padding()-14);
        int w=body.widthValue()-5;
        int cols=w>=460?3:2, gap=6, cw=(w-gap*(cols-1))/cols;
        body.paint=(g,l,t,w0,mx,my)->drawProfile(g,l,t,w);
        int metricsTop=56, cellH=38, rows=(9+cols-1)/cols, footer=metricsTop+rows*(cellH+gap)+119;
        body.add(new MenuButton(5,footer,w-10,22,()->old("reset"),()->ready()&&!ClientSkills.pending(),this::confirmReset,false,null));
        if(ready()&&state().canTestPoints())body.add(new MenuButton(5,footer+29,w-10,20,()->old("test_points"),()->!ClientSkills.pending(),ClientSkills::testPoints,false,null));
        body.contentHeight=footer+(ready()&&state().canTestPoints()?56:29);body.restore();
    }
    private void drawProfile(GuiGraphics g,int l,int top,int w) {
        EditorClient.group(g,"profile.identity",l,top,w,56,()->{
        face(g,l+10,top+9,32);
        label(g,minecraft.player==null?t("player"):minecraft.player.getDisplayName(),l+54,top+9,w-65,WHITE,1F);
        label(g,ready()?old(state().local()?"local_scope":"server_scope"):old("storage_error"),l+54,top+25,w-65,MUTED,.67F);
        label(g,ready()?t("world_name",state().worldName()):text("world_identity_error"),l+10,top+43,w-20,GREEN,.7F);
        });
        int cols=w>=460?3:2,gap=6,cw=(w-gap*(cols-1))/cols;
        var st=state()==null?SkillNetwork.MenuStatistics.UNKNOWN:state().statistics();
        List<Component> names=List.of(t("sp"),t("pp"),t("challenges"),t("playtime"),t("island"),t("unlocks"),t("deaths"),t("mob_kills"),text("current_world_claims"));
        List<Component> values=List.of(ready()?literal(state().points()):literal("—"),ready()?literal(state().perkPoints()):literal("—"),
                ready()?literal(completed()+" / "+(EditorClient.customized()?EditorClient.questCount():MilestoneCatalog.MILESTONES.size())):literal("—"),playtime(st.playTicks()),st.islandLevel()<0?literal("—"):literal(st.islandLevel()),
                ready()?literal(state().skills().size()):literal("—"),literal(st.deaths()<0?"—":st.deaths()),literal(st.mobKills()<0?"—":st.mobKills()),ready()?literal(state().worldClaims().size()):literal("—"));
        EditorClient.group(g,"profile.stats",l,top+56,w,((9+cols-1)/cols)*44,()->{
        for(int i=0;i<names.size();i++) {
            int dx=l+(i%cols)*(cw+gap),dy=top+56+(i/cols)*44;
            panel(g,dx,dy,cw,38,0xD1102734,BORDER);
            centered(g,names.get(i),dx+4,dy+5,cw-8,12,MUTED,.67F);
            centered(g,values.get(i),dx+4,dy+18,cw-8,17,i%3==1?GOLD:CYAN,.95F);
        }
        });
        int fy=top+56+((9+cols-1)/cols)*44+6;
        label(g,t("statistics_scope"),l+6,fy,w-12,MUTED,.65F);
        label(g,t("island_rules"),l+6,fy+12,w-12,GREEN,.65F);
        if(ready()) {
            var a=state().accounting();
            label(g,text("history_title"),l+6,fy+30,w-12,WHITE,.75F);
            label(g,t("earned",a.spEarned(),a.ppEarned()),l+6,fy+45,w-12,MUTED,.7F);
            label(g,t("spent",a.spSpent(),a.ppSpent()),l+6,fy+57,w-12,MUTED,.7F);
            label(g,t("refunded",a.spRefunded(),a.ppRefunded(),a.spAdmin()),l+6,fy+69,w-12,MUTED,.7F);
            label(g,text(a.legacyHistoryUnknown()?"history_legacy":"history_tracked"),l+6,fy+83,w-12,DIM,.63F);
        }
    }
    private void confirmReset() {
        if(authoringPreview)return;
        if(!ready()||ClientSkills.pending())return;
        int sp=SkillCatalog.SKILLS.stream().mapToInt(s->state().rank(s.id())*s.cost()).sum(),pp=SkillCatalog.SKILLS.stream().mapToInt(s->state().rank(s.id())*s.perkCost()).sum();
        minecraft.setScreen(new ConfirmScreen(yes->{minecraft.setScreen(this);if(yes)ClientSkills.reset();},old("reset_title"),old("reset_description",sp,pp),old("reset_confirm"),Component.translatable("gui.cancel")));
    }
    private static ItemStack item(String id) {return BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(id)).orElse(Items.BARRIER).getDefaultInstance();}
    private void face(GuiGraphics g,int x,int y,int size) {
        if(minecraft.player!=null)PlayerFaceRenderer.draw(g,minecraft.player.getSkin(),x,y,size);
        else {panel(g,x,y,size,size,0xFF1F3B49,CYAN);centered(g,literal("?"),x,y,size,size,CYAN,1F);}
    }
    private void pageIcon(GuiGraphics g,Page p,int x,int y) {
        if(p==Page.PROFILE){face(g,x,y,14);return;}
        ItemStack icon=switch(p){case OVERVIEW->Items.COMPASS.getDefaultInstance();case CHALLENGES->Items.IRON_SWORD.getDefaultInstance();
            case SHOP->Items.CHEST.getDefaultInstance();case RESEARCH->Items.BOOK.getDefaultInstance();default->ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance();};
        g.renderItem(icon,x,y);
    }
    private class MenuButton extends Button {
        final Supplier<Component> title; final BooleanSupplier enabled; final boolean selected; final Page tab;
        private String elementId="";
        MenuButton(int x,int y,int w,int h,Supplier<Component> title,BooleanSupplier enabled,Runnable action,boolean selected,Page tab) {
            super(x,y,w,h,title.get(),b->action.run(),DEFAULT_NARRATION);this.title=title;this.enabled=enabled;this.selected=selected;this.tab=tab;
            if(tab!=null){elementId="nav."+tab.name().toLowerCase(Locale.ROOT);var r=EditorClient.rect(elementId,x,y,w,h);setX(r.x());setY(r.y());setWidth(r.width());setHeight(r.height());}
        }
        @Override public void onPress() {if(authoringPreview && tab==null && (page==Page.PROFILE||page==Page.OVERVIEW))return;if(enabled.getAsBoolean())super.onPress();}
        @Override protected void renderWidget(GuiGraphics g,int mx,int my,float dt) {
            active=enabled.getAsBoolean();setMessage(title.get());
            var style=elementId.isEmpty()?Style.defaults():EditorClient.style(elementId);
            if(style.hidden())return;
            if(!elementId.isEmpty())EditorClient.capture(elementId,getX(),getY(),width,height);
            panel(g,getX(),getY(),width,height,EditorCodec.color(style.fill(),selected?0xFF163A4B:isHoveredOrFocused()?0xFF173141:0xD00D202B),EditorCodec.color(style.border(),selected?CYAN:BORDER));
            if(selected)g.fill(getX()+1,getY()+height-2,getX()+width-1,getY()+height-1,CYAN);
            boolean icon=tab!=null&&width>68; if(icon)pageIcon(g,tab,getX()+7,getY()+(height-16)/2);
            centered(g,title.get(),getX()+(icon?25:4),getY(),width-(icon?29:8),height,EditorCodec.color(style.text(),active?selected?WHITE:MUTED:DIM),.74F*style.fontScale());
        }
    }
    /** Children, narration, clipping and hit testing share exactly the same animated coordinates. */
    private final class MenuPanel extends ScrollPanel {
        private final List<MenuButton> buttons=new ArrayList<>();
        private final List<int[]> offsets=new ArrayList<>();
        final MenuMotion motion=new MenuMotion();
        private final String key;
        private final long born=System.nanoTime();
        private int entryOffset(){return (int)Math.round(5*(1-MenuMotion.reveal(born,System.nanoTime(),EditorClient.document().settings().animationMillis())));}
        int contentHeight; Painter paint;
        MenuPanel(int x,int y,int w,int h,String key) {
            super(PremiumSkillScreen.this.minecraft,w,h,y,x,0,3,0x00000000,CYAN,0xFF91E9FF);this.key=key;contentHeight=h;
        }
        int widthValue(){return width;}
        void add(MenuButton b){offsets.add(new int[]{b.getX(),b.getY()});buttons.add(b);}
        void restore(){motion.snap(Math.clamp(positions.getOrDefault(key,0D),0,Math.max(0,contentHeight-height)));position();}
        private void position(){
            for(int i=0;i<buttons.size();i++) {
                var b=buttons.get(i);b.setX(left+offsets.get(i)[0]);b.setY(top+offsets.get(i)[1]-(int)motion.value()+entryOffset());
                b.visible=b.getY()+b.getHeight()>top&&b.getY()<bottom;
                b.active=b.enabled.getAsBoolean();b.setMessage(b.title.get());
            }
        }
        @Override protected int getContentHeight(){return Math.max(height,contentHeight);}
        @Override protected void drawBackground(GuiGraphics g,Tesselator tess,float tick){var style=EditorClient.style(key.equals("rail")?"categories":key.equals("detail")?"detail.prose":"body."+page.name().toLowerCase(Locale.ROOT));if(!style.fill().isEmpty()||!style.border().isEmpty())panel(g,left,top,width,height,EditorCodec.color(style.fill(),0),EditorCodec.color(style.border(),0));}
        @Override protected void drawPanel(GuiGraphics g,int entryRight,int baseY,Tesselator tess,int mx,int my){
            scrollDistance=(float)motion.advance(System.nanoTime());position();
            if(paint!=null)paint.draw(g,left,top-(int)motion.value()+entryOffset(),width,mx,my);
            for(var b:buttons)if(b.visible)b.render(g,mx,my,0);
            g.flush();
        }
        @Override public boolean mouseScrolled(double mx,double my,double horizontal,double vertical){
            if(!isMouseOver(mx,my))return false;motion.scroll(-vertical*EditorClient.document().settings().scrollSpeed(),contentHeight-height);return true;
        }
        @Override public boolean mouseClicked(double mx,double my,int button){position();return isMouseOver(mx,my)&&super.mouseClicked(mx,my,button);}
        @Override public boolean mouseDragged(double mx,double my,int button,double dx,double dy){
            boolean result=super.mouseDragged(mx,my,button,dx,dy);if(result)motion.snap(Math.clamp(scrollDistance,0,Math.max(0,contentHeight-height)));return result;
        }
        @Override public List<? extends GuiEventListener> children(){return buttons;}
        @Override public NarratableEntry.NarrationPriority narrationPriority(){return getFocused()==null?NarratableEntry.NarrationPriority.NONE:NarratableEntry.NarrationPriority.FOCUSED;}
        @Override public void updateNarration(NarrationElementOutput o){o.add(NarratedElementType.TITLE,pageName(page));}
    }
    private void open(Detail detail){remember();if(authoringPreview){previewDetail=new DetailScreen(detail);previewDetail.init(minecraft,width,height);}else minecraft.setScreen(new DetailScreen(detail));}
    private final class DetailScreen extends Screen {
        final Detail detail; private long start=System.nanoTime();private int dx,dy,dw,dh;private MenuPanel prose;
        DetailScreen(Detail detail){super(detail.title());this.detail=detail;}
        @Override protected void init(){
            if(PremiumSkillScreen.this.width!=width || PremiumSkillScreen.this.height!=height)
                PremiumSkillScreen.this.resize(minecraft,width,height);
            dw=Math.min(width-22,430);dh=Math.min(height-22,300);dx=(width-dw)/2;dy=(height-dh)/2;
            var rect=EditorClient.rect("detail",dx,dy,dw,dh);dx=rect.x();dy=rect.y();dw=rect.width();dh=rect.height();
            var pr=EditorClient.rect("detail.prose",dx+13,dy+69,dw-26,Math.max(20,dh-112));
            prose=addRenderableWidget(new MenuPanel(pr.x(),pr.y(),pr.width(),pr.height(),"detail"));
            prose.paint=(g,l,t,w,mx,my)->wrapped(g,detail.description().get(),l,t,w-7,EditorCodec.color(EditorClient.style("detail.prose").text(),MUTED),.83F*EditorClient.style("detail.prose").fontScale(),Integer.MAX_VALUE);
            prose.contentHeight=wrappedHeight(detail.description().get(),pr.width()-7,.83F*EditorClient.style("detail.prose").fontScale())+8;
            addRenderableWidget(new MenuButton(dx+12,dy+dh-30,70,20,()->t("back"),()->true,this::onClose,false,null));
            addRenderableWidget(new MenuButton(dx+90,dy+dh-30,dw-102,20,detail.actionLabel(),detail.enabled(),()->{if(authoringPreview)previewDetail=null;else detail.action().run();},false,null));
        }
        @Override public void tick(){if(++ticks%100==0)ClientSkills.refreshMenu();}
        @Override public boolean isPauseScreen(){return false;}
        @Override public void onClose(){if(authoringPreview)previewDetail=null;else minecraft.setScreen(PremiumSkillScreen.this);}
        @Override public boolean keyPressed(int key,int scan,int modifiers){if(ClientSkills.OPEN.matches(key,scan)){minecraft.setScreen(null);return true;}return super.keyPressed(key,scan,modifiers);}
        @Override public void render(GuiGraphics g,int mx,int my,float tick){
            PremiumSkillScreen.this.render(g,-100,-100,tick);
            g.flush();
            g.pose().pushPose();g.pose().translate(0,0,400);
            super.render(g,mx,my,tick);
            g.flush();g.pose().popPose();
        }
        @Override public void renderBackground(GuiGraphics g,int mx,int my,float tick){
            double reveal=MenuMotion.reveal(start,System.nanoTime(),EditorClient.document().settings().animationMillis());
            g.fill(0,0,width,height,((int)(178*reveal)<<24)|0x020911);
            panel(g,dx-2,dy-2,dw+4,dh+4,EditorCodec.color(EditorClient.style("detail").fill(),0xFF081822),EditorCodec.color(EditorClient.style("detail").border(),detail.accent()));
            g.fill(dx,dy,dx+(int)(dw*reveal),dy+2,detail.accent());
            circle(g,dx+28,dy+29,19,0xFF183542);EditorClient.item(g,detail.icon(),dx+20,dy+21,"detail.icon");
            EditorClient.text(g,detail.title().copy().withStyle(ChatFormatting.BOLD),dx+55,dy+13,dw-69,22,WHITE,.95F,"detail.title",true);
            label(g,detail.summary().get(),dx+55,dy+39,dw-69,detail.accent(),.7F);
            bar(g,dx+13,dy+59,dw-26,detail.ratio().get(),detail.accent());
            label(g,detail.progress().get(),dx+14,dy+dh-42,dw-28,detail.accent(),.67F);
        }
    }
}

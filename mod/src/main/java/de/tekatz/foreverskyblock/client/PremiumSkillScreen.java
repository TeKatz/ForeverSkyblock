package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.Tesselator;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.compat.IronJetpacksBridge;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.ChallengeRules;
import de.tekatz.foreverskyblock.progression.JetpackGate;
import de.tekatz.foreverskyblock.progression.JetpackResearch;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.ShopCatalog;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.EnumMap;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import net.minecraft.ChatFormatting;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.narration.NarratedElementType;
import net.minecraft.client.gui.narration.NarratableEntry;
import net.minecraft.client.gui.narration.NarrationElementOutput;
import net.minecraft.client.gui.screens.ConfirmScreen;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.client.gui.widget.ScrollPanel;
import org.lwjgl.glfw.GLFW;

/** Design-2 visual pass tuned directly against the approved mockup. */
public final class PremiumSkillScreen extends Screen {
    private static final int WHITE = 0xFFF6FAFF;
    private static final int LIGHT = 0xFFDCE8F3;
    private static final int MUTED = 0xFF9BAFC0;
    private static final int DIM = 0xFF637B8F;
    private static final int CYAN = 0xFF35C9FF;
    private static final int CYAN_LIGHT = 0xFF7DE6FF;
    private static final int GREEN = 0xFF43E58A;
    private static final int GOLD = 0xFFFFC84E;
    private static final int RED = 0xFFFF626E;
    private static final int BORDER = 0xFF29465B;
    private static final int PANEL = 0xD20A1520;
    private static final int PANEL_LIGHT = 0xD8122331;
    private static final int CARD = 0xD5152938;

    private enum Page { OVERVIEW, CHALLENGES, SHOP, RESEARCH, PERKS, PROFILE }
    private enum SortMode { PROGRESS, REWARD, NAME }

    private record Card(Component title, Component description, Component badge, ItemStack icon,
                        String skill, String product, Runnable action, Component actionText) { }

    private Page page = Page.CHALLENGES;
    private SortMode sortMode = SortMode.PROGRESS;
    private final EnumMap<Page, Integer> subsections = new EnumMap<>(Page.class);
    private final Map<String, Float> scrolls = new HashMap<>();
    private PremiumMenuLayout layout;
    private int x, y;
    private int filter;
    private String query = "";
    private boolean gridView = true;
    private EditBox search;
    private ChallengePanel challengePanel;
    private GenericPanel genericPanel;
    private MilestoneCatalog.Milestone detail;
    private SkillNetwork.Snapshot lastSnapshot;

    public PremiumSkillScreen() { super(text("title")); }

    private static Component text(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.meta." + key, args);
    }

    private static Component old(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.skills." + key, args);
    }

    private static Component pageName(Page page) {
        return text("page." + page.name().toLowerCase(Locale.ROOT));
    }

    private int sub() { return subsections.getOrDefault(page, 0); }
    private String location() { return page.name() + ":" + sub(); }

    private List<String> sections() {
        return switch (page) {
            case CHALLENGES -> List.of("all", "forever", "ae2", "mekanism", "powah", "mystical_agriculture", "combat", "contracts");
            case SHOP -> List.of("permanent", "current_world", "tier_progress");
            case RESEARCH -> List.of("jetpacks");
            default -> List.of();
        };
    }

    @Override
    protected void init() {
        layout = PremiumMenuLayout.fit(width, height);
        x = (width - layout.width()) / 2;
        y = (height - layout.height()) / 2;
        clearWidgets();
        search = null;
        challengePanel = null;
        genericPanel = null;

        addNavigation();
        addRenderableWidget(new ChromeButton(x + layout.width() - layout.padding() - 19, y + layout.padding(), 18, 18,
                Component.literal("×"), false, b -> onClose()));

        if (page == Page.CHALLENGES && detail == null) initChallenges();
        else initGeneral();
        lastSnapshot = ClientSkills.snapshot();
        refresh();
    }

    private void addNavigation() {
        int left = x + layout.padding();
        int top = y + layout.navTop();
        int gap = 1;
        int available = layout.width() - layout.padding() * 2;
        int bw = (available - gap * 5) / 6;
        int i = 0;
        for (Page target : Page.values()) {
            var button = new NavButton(left + i * (bw + gap), top, bw, layout.navHeight(), target);
            button.active = target != page;
            addRenderableWidget(button);
            i++;
        }
    }

    private void initChallenges() {
        if (layout.challengeSidebarWidth() == 0) {
            initCollapsedChallengeFilters();
        } else {
            int sidebarX = x + layout.padding() + 6;
            int sidebarW = layout.challengeSidebarWidth() - 12;
            int sy = y + layout.contentTop() + 28;
            search = new EditBox(font, sidebarX + 7, sy + 3, sidebarW - 14, 14, text("search"));
            search.setBordered(false);
            search.setMaxLength(80);
            search.setHint(text("search"));
            search.setTextColor(LIGHT);
            search.setTextColorUneditable(MUTED);
            search.setValue(query);
            search.setResponder(value -> {
                query = value;
                scrolls.put(location(), 0F);
                replaceChallengePanel();
            });
            addRenderableWidget(search);

            int fy = sy + 25;
            int gap = 3;
            int fw = (sidebarW - gap * 2) / 3;
            for (int state = 0; state < 3; state++) {
                final int selected = state;
                addRenderableWidget(new ChromeButton(sidebarX + state * (fw + gap), fy, fw, 18,
                        filterLabel(state), filter == state, b -> {
                    filter = selected;
                    scrolls.put(location(), 0F);
                    rebuildWidgets();
                }));
            }

            int rowH = layout.sidebarRowHeight();
            int cy = fy + 25;
            for (int i = 0; i < sections().size(); i++) {
                final int chosen = i;
                String category = sections().get(i);
                addRenderableWidget(new CategoryButton(sidebarX, cy + i * rowH, sidebarW, rowH - 2,
                        category, categoryLabel(category), categoryIcon(category), categoryCount(category), i == sub(),
                        b -> navigate(Page.CHALLENGES, chosen)));
            }
        }

        addChallengeToolbar();
        replaceChallengePanel();
    }

    private void initCollapsedChallengeFilters() {
        int mainX = x + layout.challengeMainX();
        int top = y + layout.contentTop() + 27;
        int mainW = layout.challengeMainWidth();
        int half = (mainW - 4) / 2;
        addRenderableWidget(new ChromeButton(mainX, top, half, 18, categoryLabel(sections().get(sub())), true,
                b -> navigate(Page.CHALLENGES, (sub() + 1) % sections().size())));
        addRenderableWidget(new ChromeButton(mainX + half + 4, top, half, 18, filterLabel(filter), false, b -> {
            filter = (filter + 1) % 3;
            rebuildWidgets();
        }));
    }

    private void addChallengeToolbar() {
        int right = x + layout.challengeMainX() + layout.challengeMainWidth();
        int top = y + layout.contentTop() + 5;
        if (!layout.tiny()) {
            addRenderableWidget(new ChromeButton(right - 111, top, 72, 20,
                    Component.literal("Sort: ").append(sortLabel()), false, b -> {
                sortMode = SortMode.values()[(sortMode.ordinal() + 1) % SortMode.values().length];
                scrolls.put(location(), 0F);
                replaceChallengePanel();
            }));
            addRenderableWidget(new ViewButton(right - 36, top, 17, 20, true, gridView, b -> {
                if (!gridView) { gridView = true; scrolls.put(location(), 0F); rebuildWidgets(); }
            }));
            addRenderableWidget(new ViewButton(right - 18, top, 17, 20, false, !gridView, b -> {
                if (gridView) { gridView = false; scrolls.put(location(), 0F); rebuildWidgets(); }
            }));
        }
    }

    private void initGeneral() {
        int contentTop = y + layout.contentTop();
        if (detail != null) {
            addRenderableWidget(new ChromeButton(x + layout.width() - layout.padding() - 82, contentTop + 5, 75, 20,
                    text("back"), false, b -> { detail = null; rebuildWidgets(); }));
        } else if (!sections().isEmpty() && page != Page.CHALLENGES) {
            int left = x + layout.padding() + 8;
            int top = contentTop + 27;
            int gap = 4;
            int count = sections().size();
            int bw = Math.min(145, (layout.width() - layout.padding() * 2 - 16 - gap * (count - 1)) / count);
            for (int i = 0; i < count; i++) {
                final int chosen = i;
                addRenderableWidget(new ChromeButton(left + i * (bw + gap), top, bw, 20,
                        text("sub." + sections().get(i)), i == sub(), b -> navigate(page, chosen)));
            }
        }
        replaceGenericPanel();
    }

    private void navigate(Page target, int subsection) {
        rememberScroll();
        page = target;
        subsections.put(target, Math.max(0, subsection));
        detail = null;
        rebuildWidgets();
    }

    private void rememberScroll() {
        if (challengePanel != null) scrolls.put(location(), challengePanel.position());
        if (genericPanel != null && detail == null) scrolls.put(location(), genericPanel.position());
    }

    private Component filterLabel(int state) {
        return switch (state) {
            case 1 -> text("filter.1");
            case 2 -> text("filter.2");
            default -> Component.literal("All");
        };
    }

    private Component sortLabel() {
        return switch (sortMode) {
            case PROGRESS -> Component.literal("Progress");
            case REWARD -> Component.literal("SP");
            case NAME -> Component.literal("A–Z");
        };
    }

    private static String stripPrefix(String value) {
        int split = value.indexOf(": ");
        return split >= 0 ? value.substring(split + 2) : value;
    }

    private Component categoryLabel(String category) {
        return Component.literal(stripPrefix(text("sub." + category).getString()));
    }

    private ItemStack categoryIcon(String category) {
        return switch (category) {
            case "forever" -> ForeverSkyblock.STONE_FRAGMENT.get().getDefaultInstance();
            case "ae2" -> Items.AMETHYST_SHARD.getDefaultInstance();
            case "mekanism" -> Items.IRON_BLOCK.getDefaultInstance();
            case "powah" -> Items.GLOWSTONE_DUST.getDefaultInstance();
            case "mystical_agriculture" -> Items.WHEAT_SEEDS.getDefaultInstance();
            case "combat" -> Items.DIAMOND_SWORD.getDefaultInstance();
            case "contracts" -> Items.WRITABLE_BOOK.getDefaultInstance();
            default -> Items.NETHER_STAR.getDefaultInstance();
        };
    }

    private int categoryCount(String category) {
        if ("all".equals(category)) return MilestoneCatalog.MILESTONES.size();
        if ("contracts".equals(category)) return 0;
        return (int) MilestoneCatalog.MILESTONES.stream().filter(m -> category.equals(m.category())).count();
    }

    private int categoryColor(String category) {
        return switch (category) {
            case "forever" -> 0xFF43E786;
            case "ae2" -> 0xFFC86DFA;
            case "mekanism" -> 0xFF25D9D0;
            case "powah" -> 0xFFFFC84E;
            case "mystical_agriculture" -> 0xFF6EE56C;
            case "combat" -> RED;
            case "contracts" -> 0xFFFFC76A;
            default -> CYAN;
        };
    }

    private List<MilestoneCatalog.Milestone> challengeEntries() {
        String section = sections().get(sub());
        if ("contracts".equals(section)) return List.of();
        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        var result = new ArrayList<MilestoneCatalog.Milestone>();
        for (var milestone : MilestoneCatalog.MILESTONES) {
            if (!"all".equals(section) && !section.equals(milestone.category())) continue;
            boolean complete = ready && state.milestones().contains(milestone.id());
            if (filter == 1 && complete || filter == 2 && !complete) continue;
            String haystack = (Component.translatable(milestone.titleKey()).getString() + " "
                    + Component.translatable(milestone.descriptionKey()).getString()).toLowerCase(Locale.ROOT);
            if (!haystack.contains(query.toLowerCase(Locale.ROOT))) continue;
            result.add(milestone);
        }
        Comparator<MilestoneCatalog.Milestone> comparator = switch (sortMode) {
            case NAME -> Comparator.comparing(m -> Component.translatable(m.titleKey()).getString(), String.CASE_INSENSITIVE_ORDER);
            case REWARD -> Comparator.comparingInt(MilestoneCatalog.Milestone::reward).reversed()
                    .thenComparing(m -> Component.translatable(m.titleKey()).getString(), String.CASE_INSENSITIVE_ORDER);
            case PROGRESS -> Comparator
                    .comparing((MilestoneCatalog.Milestone m) -> ready && state.milestones().contains(m.id()))
                    .thenComparingDouble(m -> -progressRatio(state, m))
                    .thenComparing(m -> Component.translatable(m.titleKey()).getString(), String.CASE_INSENSITIVE_ORDER);
        };
        result.sort(comparator);
        return result;
    }

    private static double progressRatio(SkillNetwork.Snapshot state, MilestoneCatalog.Milestone milestone) {
        if (state == null || !state.available()) return 0;
        if (state.milestones().contains(milestone.id())) return 1;
        int current = Math.max(0, state.progress().getOrDefault(milestone.id(), 0));
        return milestone.target() <= 0 ? 0 : Math.min(1D, current / (double) milestone.target());
    }

    private void replaceChallengePanel() {
        if (challengePanel != null) removeWidget(challengePanel);
        if ("contracts".equals(sections().get(sub()))) {
            challengePanel = null;
            return;
        }
        challengePanel = addRenderableWidget(new ChallengePanel(challengeEntries()));
        challengePanel.restore(scrolls.getOrDefault(location(), 0F));
    }

    private void replaceGenericPanel() {
        if (genericPanel != null) removeWidget(genericPanel);
        int top = y + layout.contentTop() + (detail != null ? 31 : !sections().isEmpty() && page != Page.CHALLENGES ? 53 : 30);
        int left = x + layout.padding() + 7;
        int w = layout.width() - layout.padding() * 2 - 14;
        int h = Math.max(30, y + layout.contentBottom() - top);
        genericPanel = addRenderableWidget(new GenericPanel(generalCards(), left, top, w, h));
        if (detail == null) genericPanel.restore(scrolls.getOrDefault(location(), 0F));
    }

    private List<Card> generalCards() {
        var cards = new ArrayList<Card>();
        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();

        if (detail != null) {
            cards.add(new Card(Component.translatable(detail.titleKey()), Component.translatable(detail.descriptionKey()),
                    text("milestone_badge", detail.reward()), detail.icon().getDefaultInstance(), "", "", null, Component.empty()));
            return cards;
        }

        switch (page) {
            case OVERVIEW -> {
                long done = ready ? MilestoneCatalog.MILESTONES.stream().filter(m -> state.milestones().contains(m.id())).count() : 0;
                cards.add(new Card(text("profile_summary"),
                        ready ? text("summary", state.points(), state.perkPoints(), done, MilestoneCatalog.MILESTONES.size(), ClientSkills.jetpackStage()) : old("storage_error"),
                        ready ? text("current_tier", tierName(state.shopTier())) : text("live_data"), Items.NETHER_STAR.getDefaultInstance(), "", "", null, Component.empty()));
                cards.add(linkCard(text("page.challenges"), text("overview_challenges"), Items.IRON_SWORD.getDefaultInstance(), Page.CHALLENGES, 0));
                cards.add(linkCard(text("page.shop"), text("overview_shop"), Items.CHEST.getDefaultInstance(), Page.SHOP, 0));
                cards.add(linkCard(text("page.research"), text("overview_research"), Items.BOOK.getDefaultInstance(), Page.RESEARCH, 0));
            }
            case CHALLENGES -> { }
            case SHOP -> {
                if (sub() == 0) for (var skill : SkillCatalog.SKILLS) if (!skill.jetpack()) cards.add(skillCard(skill));
                else if (sub() == 1) {
                    cards.add(new Card(text("sub.current_world"), ready
                            ? state.worldAvailable() ? text("world_identity", state.worldName()) : text("world_identity_error")
                            : old("storage_error"), text("current_world_scope"), Items.COMPASS.getDefaultInstance(), "", "", null, Component.empty()));
                    if (ready && state.canTestPoints()) {
                        var product = ShopCatalog.TEST;
                        cards.add(new Card(text("test_product_title"), text("test_product_description"),
                                text("world_product_badge", product.spCost(), tierName(product.requiredTier())), Items.PAPER.getDefaultInstance(),
                                "", product.id(), null, text("purchase")));
                    }
                } else {
                    cards.add(new Card(text("sub.tier_progress"), text("tiers_pending"),
                            ready ? text("current_tier", tierName(state.shopTier())) : old("unavailable"), Items.COMPASS.getDefaultInstance(), "", "", null, Component.empty()));
                    cards.add(infoCard("tier_foundations", "tier_foundations_description", Items.COBBLESTONE.getDefaultInstance()));
                    cards.add(infoCard("tier_conqueror", "tier_conqueror_description", Items.DRAGON_HEAD.getDefaultInstance()));
                    cards.add(infoCard("tier_engineer", "tier_engineer_description", Items.REDSTONE.getDefaultInstance()));
                    cards.add(infoCard("tier_mastery", "tier_mastery_description", Items.DIAMOND.getDefaultInstance()));
                    cards.add(infoCard("tier_ascended", "tier_ascended_description", ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance()));
                }
            }
            case RESEARCH -> { for (var skill : SkillCatalog.SKILLS) if (skill.jetpack()) cards.add(skillCard(skill)); }
            case PERKS -> {
                cards.add(new Card(old("perk_info_title"), old("perk_info_description"), old("core_conversion"),
                        ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance(), "", "", null, Component.empty()));
                for (var skill : SkillCatalog.SKILLS) if (skill.perkCost() > 0) cards.add(skillCard(skill));
            }
            case PROFILE -> {
                cards.add(new Card(text("profile_summary"), ready
                        ? text("profile_values", state.points(), state.perkPoints(), state.skills().size(), state.milestones().size()) : old("storage_error"),
                        ready ? old(state.local() ? "local_scope" : "server_scope") : Component.empty(), Items.PLAYER_HEAD.getDefaultInstance(), "", "", null, Component.empty()));
                if (ready) {
                    var a = state.accounting();
                    cards.add(new Card(text("history_title"), text("accounting_values", a.spEarned(), a.ppEarned(), a.spSpent(), a.ppSpent(), a.spRefunded(), a.ppRefunded(), a.spAdmin()),
                            text(a.legacyHistoryUnknown() ? "history_legacy" : "history_tracked"), Items.BOOK.getDefaultInstance(), "", "", null, Component.empty()));
                    cards.add(new Card(text("current_world_claims"), state.worldAvailable()
                            ? text("world_profile_values", state.worldName(), state.worldClaims().size()) : text("world_identity_error"),
                            text("current_world_scope"), Items.COMPASS.getDefaultInstance(), "", "", null, Component.empty()));
                }
                cards.add(new Card(old("reset_title"), text("reset_scope"), text("permanent_scope"), Items.GRINDSTONE.getDefaultInstance(),
                        "", "", this::confirmReset, old("reset")));
                if (ready && state.canTestPoints()) cards.add(new Card(text("development"), old("test_points_hint"), text("admin_only"), Items.COMMAND_BLOCK.getDefaultInstance(),
                        "", "", () -> { ClientSkills.testPoints(); refresh(); }, old("test_points")));
            }
        }
        return cards;
    }

    private Card linkCard(Component title, Component description, ItemStack icon, Page target, int subsection) {
        return new Card(title, description, Component.empty(), icon, "", "", () -> navigate(target, subsection), text("open"));
    }

    private Card infoCard(String title, String description, ItemStack icon) {
        return new Card(text(title), text(description), Component.empty(), icon, "", "", null, Component.empty());
    }

    private Card skillCard(SkillCatalog.Skill skill) {
        ItemStack icon = skill.icon().getDefaultInstance();
        var description = Component.translatable(skill.descriptionKey());
        if (skill.jetpack()) {
            if (JetpackGate.installed()) {
                var variants = IronJetpacksBridge.variants(JetpackResearch.IDS.indexOf(skill.id()) + 1);
                if (!variants.isEmpty()) icon = variants.getFirst();
                description.append("\n").append(old("jetpack_variants",
                        variants.stream().map(v -> v.getHoverName().getString()).collect(java.util.stream.Collectors.joining(", "))));
            } else description.append("\n").append(old("missing_jetpacks"));
            if (!skill.prerequisite().isEmpty()) description.append("\n").append(old("requires_skill",
                    Component.translatable(SkillCatalog.find(skill.prerequisite()).titleKey())));
        }
        Component price = skill.perkCost() > 0 ? old("dual_cost", skill.cost(), skill.perkCost()) : old("cost", skill.cost());
        return new Card(Component.translatable(skill.titleKey()), description,
                text("permanent_scope").copy().append(" · ").append(price), icon, skill.id(), "", null, Component.empty());
    }

    private static Component tierName(int tier) { return text("tier." + tier); }

    private Component skillStatus(String id) {
        var state = ClientSkills.snapshot();
        var skill = SkillCatalog.find(id);
        if (state == null || !state.available()) return old("unavailable");
        if (state.rank(id) >= skill.maxRank()) return old("unlocked");
        if (!skill.available()) return old("missing_jetpacks");
        if (!skill.prerequisite().isEmpty() && state.rank(skill.prerequisite()) == 0) return old("requires_previous");
        if (ClientSkills.pending()) return old("waiting");
        if (state.perkPoints() < skill.perkCost()) return old("need_perks", skill.perkCost());
        if (state.points() < skill.cost()) return old("need_points", skill.cost());
        return old("unlock_short", skill.cost());
    }

    private boolean canBuySkill(String id) {
        var state = ClientSkills.snapshot();
        var skill = SkillCatalog.find(id);
        return state != null && state.available() && !ClientSkills.pending() && skill.available()
                && state.rank(id) < skill.maxRank()
                && (skill.prerequisite().isEmpty() || state.rank(skill.prerequisite()) > 0)
                && state.points() >= skill.cost() && state.perkPoints() >= skill.perkCost();
    }

    private boolean canBuyProduct(String id) {
        var state = ClientSkills.snapshot();
        var product = ShopCatalog.find(id);
        return state != null && state.available() && state.worldAvailable() && !ClientSkills.pending() && product != null
                && state.canTestPoints() && !state.worldClaims().contains(id)
                && state.shopTier() >= product.requiredTier() && state.points() >= product.spCost();
    }

    private Component productStatus(String id) {
        var state = ClientSkills.snapshot();
        var product = ShopCatalog.find(id);
        if (state == null || !state.available()) return old("unavailable");
        if (!state.worldAvailable()) return text("world_unavailable");
        if (state.worldClaims().contains(id)) return text("world_already_claimed");
        if (state.shopTier() < product.requiredTier()) return text("world_requires_tier", tierName(product.requiredTier()));
        if (state.points() < product.spCost()) return old("need_points", product.spCost());
        return text("world_purchase_available");
    }

    private void confirmReset() {
        var state = ClientSkills.snapshot();
        if (state == null || !state.available() || ClientSkills.pending()) return;
        int sp = SkillCatalog.SKILLS.stream().mapToInt(s -> state.rank(s.id()) * s.cost()).sum();
        int pp = SkillCatalog.SKILLS.stream().mapToInt(s -> state.rank(s.id()) * s.perkCost()).sum();
        minecraft.setScreen(new ConfirmScreen(yes -> {
            minecraft.setScreen(this);
            if (yes) ClientSkills.reset();
        }, old("reset_title"), old("reset_description", sp, pp), old("reset_confirm"), Component.translatable("gui.cancel")));
    }

    private void confirmProduct(String id) {
        var state = ClientSkills.snapshot();
        var product = ShopCatalog.find(id);
        if (product == null || !canBuyProduct(id)) return;
        minecraft.setScreen(new ConfirmScreen(yes -> {
            minecraft.setScreen(this);
            if (yes) ClientSkills.buyProduct(id);
        }, text("purchase_confirm_title"), text("purchase_confirm_description", product.spCost(), state.worldName()),
                text("purchase_confirm"), Component.translatable("gui.cancel")));
    }

    public void refresh() {
        if (lastSnapshot != ClientSkills.snapshot()) {
            rememberScroll();
            lastSnapshot = ClientSkills.snapshot();
            if (page == Page.CHALLENGES && detail == null) replaceChallengePanel();
            else replaceGenericPanel();
        }
        if (genericPanel != null) genericPanel.refreshActions();
    }

    @Override public void tick() { refresh(); }
    @Override public boolean isPauseScreen() { return false; }

    @Override
    public boolean keyPressed(int key, int scan, int modifiers) {
        if (search != null && search.isFocused()) {
            if (key == GLFW.GLFW_KEY_ESCAPE) {
                search.setFocused(false);
                setFocused(null);
                return true;
            }
            if (super.keyPressed(key, scan, modifiers)) return true;
            return false;
        }
        if (key == GLFW.GLFW_KEY_ESCAPE && detail != null) {
            detail = null;
            rebuildWidgets();
            return true;
        }
        if (ClientSkills.OPEN.matches(key, scan)) { onClose(); return true; }
        return super.keyPressed(key, scan, modifiers);
    }

    @Override
    public boolean mouseClicked(double mx, double my, int button) {
        if (search != null) {
            if (button == 1 && search.isMouseOver(mx, my)) { search.setValue(""); return true; }
            if (!search.isMouseOver(mx, my)) search.setFocused(false);
        }
        return super.mouseClicked(mx, my, button);
    }

    @Override
    public void renderBackground(GuiGraphics g, int mx, int my, float tick) {
        // Keep the world visible through the UI like the approved mockup instead of placing a nearly opaque slab over it.
        g.fill(0, 0, width, height, 0x70030A12);
        drawShell(g);
        if (page == Page.CHALLENGES && detail == null) drawChallengeChrome(g);
        else drawGeneralChrome(g);
    }

    private void drawShell(GuiGraphics g) {
        shadowPanel(g, x, y, layout.width(), layout.height(), 0xC8081420, 0xFF29465B);
        int left = x + layout.padding();
        int right = x + layout.width() - layout.padding();
        int top = y + layout.padding();
        int headerBottom = y + layout.navTop() - 5;

        // Brand area, deliberately much closer to the visual weight of the mockup.
        int brandW = layout.compact() ? Math.min(137, layout.width() / 4) : 205;
        g.fill(left, top, left + brandW, headerBottom, 0x69101E2B);
        g.renderItem(Items.GRASS_BLOCK.getDefaultInstance(), left + 8, top + (headerBottom - top - 16) / 2);
        drawScaled(g, Component.literal("FOREVER").withStyle(ChatFormatting.BOLD), left + 29, top + 7, WHITE, layout.compact() ? 0.82F : 1.0F, brandW - 35);
        drawScaled(g, Component.literal("SKYBLOCK").withStyle(ChatFormatting.BOLD), left + 29, top + (layout.compact() ? 21 : 25), CYAN, layout.compact() ? 0.82F : 1.0F, brandW - 35);
        if (!layout.compact()) drawScaled(g, Component.literal("SKILLS · CHALLENGES · PROGRESS"), left + 8, top + 43, DIM, 0.64F, brandW - 14);

        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        int closeSpace = 22;
        int statLeft = left + brandW + 5;
        int statRight = right - closeSpace;
        int gap = 4;
        int tierWeight = layout.compact() ? 2 : 3;
        int unit = Math.max(30, (statRight - statLeft - gap * 2) / (2 + tierWeight));
        int spW = unit;
        int ppW = unit;
        int tierW = statRight - statLeft - spW - ppW - gap * 2;
        drawStat(g, statLeft, top, spW, headerBottom - top,
                Items.DIAMOND.getDefaultInstance(), "SKILL POINTS", ready ? Integer.toString(state.points()) : "—", CYAN);
        drawStat(g, statLeft + spW + gap, top, ppW, headerBottom - top,
                Items.NETHER_STAR.getDefaultInstance(), "PERK POINTS", ready ? Integer.toString(state.perkPoints()) : "—", GOLD);
        drawTierStat(g, statLeft + spW + ppW + gap * 2, top, tierW, headerBottom - top, state);

        int navLeft = x + layout.padding();
        g.fill(navLeft, y + layout.navTop(), right, y + layout.navTop() + layout.navHeight(), 0xB80A1621);
        outline(g, navLeft, y + layout.navTop(), right - navLeft, layout.navHeight(), BORDER);
    }

    private void drawStat(GuiGraphics g, int sx, int sy, int sw, int sh, ItemStack icon, String label, String value, int color) {
        softPanel(g, sx, sy, sw, sh, 0xB9102030, 0xFF213D53);
        if (sw >= 58) g.renderItem(icon, sx + 6, sy + (sh - 16) / 2);
        int tx = sx + (sw >= 58 ? 27 : 6);
        drawScaled(g, Component.literal(label), tx, sy + 5, MUTED, sh < 48 ? 0.55F : 0.66F, sw - (tx - sx) - 5);
        drawScaled(g, Component.literal(value).withStyle(ChatFormatting.BOLD), tx, sy + (sh < 48 ? 17 : 20), color, sh < 48 ? 0.74F : 0.9F, sw - (tx - sx) - 5);
    }

    private void drawTierStat(GuiGraphics g, int sx, int sy, int sw, int sh, SkillNetwork.Snapshot state) {
        softPanel(g, sx, sy, sw, sh, 0xB9102030, 0xFF213D53);
        boolean ready = state != null && state.available();
        g.renderItem(Items.AMETHYST_SHARD.getDefaultInstance(), sx + 6, sy + (sh - 16) / 2);
        int tx = sx + 27;
        drawScaled(g, Component.literal("TIER STATUS"), tx, sy + 4, MUTED, sh < 48 ? 0.55F : 0.66F, sw - 32);
        String tier = ready ? stripPrefix(tierName(state.shopTier()).getString()) : "—";
        drawScaled(g, Component.literal(tier).withStyle(ChatFormatting.BOLD), tx, sy + (sh < 48 ? 15 : 18), WHITE, sh < 48 ? 0.72F : 0.83F, sw - 32);
        if (ready && sh >= 42) {
            int done = (int) MilestoneCatalog.MILESTONES.stream().filter(m -> state.milestones().contains(m.id())).count();
            int total = Math.max(1, MilestoneCatalog.MILESTONES.size());
            int barX = tx;
            int barY = sy + sh - 10;
            int barW = Math.max(20, sw - (tx - sx) - 8);
            progressBar(g, barX, barY, barW, 4, done / (double) total, GOLD);
            drawScaled(g, Component.literal(done + " / " + total), barX + barW - 31, barY - 7, MUTED, 0.53F, 31);
        }
    }

    private void drawChallengeChrome(GuiGraphics g) {
        int contentTop = y + layout.contentTop();
        int bottom = y + layout.contentBottom();
        int sidebarW = layout.challengeSidebarWidth();
        if (sidebarW > 0) {
            int sx = x + layout.padding();
            softPanel(g, sx, contentTop, sidebarW, bottom - contentTop, 0xC40B1925, 0xFF203D51);
            drawScaled(g, Component.literal("FILTERS").withStyle(ChatFormatting.BOLD), sx + 7, contentTop + 7, LIGHT, 0.72F, sidebarW - 14);
            int searchY = contentTop + 28;
            softPanel(g, sx + 6, searchY, sidebarW - 12, 20, 0xA9071520, 0xFF31536B);
        }

        int mx = x + layout.challengeMainX();
        int mw = layout.challengeMainWidth();
        softPanel(g, mx, contentTop, mw, bottom - contentTop, 0xB807141F, 0xFF203D51);
        if (!layout.tiny()) {
            g.renderItem(Items.WRITABLE_BOOK.getDefaultInstance(), mx + 8, contentTop + 7);
            drawScaled(g, pageName(Page.CHALLENGES).copy().withStyle(ChatFormatting.BOLD), mx + 28, contentTop + 6, WHITE, 0.88F, mw - 150);
            drawScaled(g, old("tasks_hint"), mx + 28, contentTop + 18, MUTED, 0.61F, mw - 155);
        }

        if ("contracts".equals(sections().get(sub()))) {
            int cy = contentTop + 48;
            softPanel(g, mx + 8, cy, mw - 16, 78, 0xB5112230, 0xFF5A4C2A);
            g.renderItem(Items.WRITABLE_BOOK.getDefaultInstance(), mx + 17, cy + 11);
            drawScaled(g, text("sub.contracts").copy().withStyle(ChatFormatting.BOLD), mx + 40, cy + 10, GOLD, 0.82F, mw - 55);
            drawWrappedScaled(g, text("contracts_pending"), mx + 16, cy + 31, mw - 32, MUTED, 0.62F, 5);
        }
    }

    private void drawGeneralChrome(GuiGraphics g) {
        int left = x + layout.padding();
        int top = y + layout.contentTop();
        int w = layout.width() - layout.padding() * 2;
        int h = layout.contentBottom() - layout.contentTop();
        softPanel(g, left, top, w, h, 0xB807141F, 0xFF203D51);
        drawScaled(g, pageName(page).copy().withStyle(ChatFormatting.BOLD), left + 9, top + 7, WHITE, 0.9F, w - 110);
    }

    private ItemStack pageIcon(Page page) {
        return switch (page) {
            case OVERVIEW -> Items.COMPASS.getDefaultInstance();
            case CHALLENGES -> Items.IRON_SWORD.getDefaultInstance();
            case SHOP -> Items.CHEST.getDefaultInstance();
            case RESEARCH -> Items.BOOK.getDefaultInstance();
            case PERKS -> Items.NETHER_STAR.getDefaultInstance();
            case PROFILE -> Items.PLAYER_HEAD.getDefaultInstance();
        };
    }

    private void drawScaled(GuiGraphics g, Component component, int dx, int dy, int color, float scale, int maxWidth) {
        String value = component.getString();
        int logicalWidth = Math.max(1, (int) (maxWidth / scale));
        value = font.plainSubstrByWidth(value, logicalWidth);
        g.pose().pushPose();
        g.pose().scale(scale, scale, 1F);
        g.drawString(font, value, Math.round(dx / scale), Math.round(dy / scale), color, false);
        g.pose().popPose();
    }

    private int drawWrappedScaled(GuiGraphics g, Component component, int dx, int dy, int maxWidth, int color, float scale, int maxLines) {
        int logicalWidth = Math.max(1, (int) (maxWidth / scale));
        int shown = 0;
        for (var line : font.split(component, logicalWidth)) {
            if (shown++ >= maxLines) break;
            g.pose().pushPose();
            g.pose().scale(scale, scale, 1F);
            g.drawString(font, line, Math.round(dx / scale), Math.round(dy / scale), color, false);
            g.pose().popPose();
            dy += Math.max(7, Math.round(10 * scale));
        }
        return dy;
    }

    private static void outline(GuiGraphics g, int ox, int oy, int ow, int oh, int color) {
        g.fill(ox, oy, ox + ow, oy + 1, color);
        g.fill(ox, oy + oh - 1, ox + ow, oy + oh, color);
        g.fill(ox, oy, ox + 1, oy + oh, color);
        g.fill(ox + ow - 1, oy, ox + ow, oy + oh, color);
    }

    private static void softPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border) {
        if (pw <= 1 || ph <= 1) return;
        g.fill(px + 1, py, px + pw - 1, py + ph, fill);
        g.fill(px, py + 1, px + pw, py + ph - 1, fill);
        outline(g, px + 1, py, pw - 2, ph, border);
        g.fill(px, py + 1, px + 1, py + ph - 1, border);
        g.fill(px + pw - 1, py + 1, px + pw, py + ph - 1, border);
    }

    private static void shadowPanel(GuiGraphics g, int px, int py, int pw, int ph, int fill, int border) {
        g.fill(px - 2, py + 2, px + pw + 2, py + ph + 3, 0x66000000);
        softPanel(g, px, py, pw, ph, fill, border);
    }

    private static void glowOutline(GuiGraphics g, int px, int py, int pw, int ph, int color) {
        g.fill(px - 2, py, px - 1, py + ph, (color & 0x00FFFFFF) | 0x44000000);
        g.fill(px + pw + 1, py, px + pw + 2, py + ph, (color & 0x00FFFFFF) | 0x44000000);
        g.fill(px, py - 2, px + pw, py - 1, (color & 0x00FFFFFF) | 0x44000000);
        g.fill(px, py + ph + 1, px + pw, py + ph + 2, (color & 0x00FFFFFF) | 0x44000000);
        outline(g, px, py, pw, ph, color);
    }

    private static void progressBar(GuiGraphics g, int px, int py, int pw, int ph, double ratio, int color) {
        g.fill(px, py, px + pw, py + ph, 0xD0030B12);
        outline(g, px, py, pw, ph, 0xFF42637B);
        int inner = Math.max(0, pw - 2);
        int fill = (int) Math.round(inner * Math.clamp(ratio, 0D, 1D));
        if (fill > 0) g.fill(px + 1, py + 1, px + 1 + fill, py + ph - 1, color);
    }

    private final class NavButton extends Button {
        private final Page target;
        NavButton(int bx, int by, int bw, int bh, Page target) {
            super(bx, by, bw, bh, pageName(target), b -> navigate(target, subsections.getOrDefault(target, 0)), DEFAULT_NARRATION);
            this.target = target;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            boolean selected = target == page;
            int bg = selected ? 0xD3193A52 : isHoveredOrFocused() ? 0xCE142A3A : 0xA90B1722;
            softPanel(g, getX(), getY(), width, height, bg, selected ? 0xFF39C9FF : 0xFF29465B);
            if (selected) {
                g.fill(getX() + 1, getY(), getX() + width - 1, getY() + 2, CYAN);
                glowOutline(g, getX(), getY(), width, height, 0xFF2EBFF4);
            }
            String label = getMessage().getString();
            float scale = layout.compact() ? 0.68F : 0.8F;
            int labelW = Math.round(font.width(label) * scale);
            boolean showIcon = width > labelW + 28;
            int total = labelW + (showIcon ? 20 : 0);
            int start = getX() + (width - total) / 2;
            if (showIcon) {
                g.renderItem(pageIcon(target), start, getY() + (height - 16) / 2);
                start += 20;
            }
            drawScaled(g, getMessage().copy().withStyle(selected ? ChatFormatting.BOLD : ChatFormatting.RESET), start,
                    getY() + (height - Math.round(8 * scale)) / 2, selected ? WHITE : LIGHT, scale, width - (start - getX()) - 4);
        }
    }

    private final class ChromeButton extends Button {
        private final boolean selected;
        ChromeButton(int bx, int by, int bw, int bh, Component message, boolean selected, OnPress action) {
            super(bx, by, bw, bh, message, action, DEFAULT_NARRATION);
            this.selected = selected;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            int border = selected ? CYAN : isHoveredOrFocused() ? 0xFF4C7C99 : 0xFF294B60;
            int bg = selected ? 0xD1195272 : isHoveredOrFocused() ? 0xCF153044 : 0xB70D1D2A;
            softPanel(g, getX(), getY(), width, height, bg, border);
            if (selected) glowOutline(g, getX(), getY(), width, height, 0xFF2BBEF2);
            drawScaled(g, getMessage(), getX() + 5, getY() + (height - 7) / 2,
                    active ? (selected ? WHITE : LIGHT) : DIM, layout.compact() ? 0.62F : 0.72F, width - 10);
        }
    }

    private final class CategoryButton extends Button {
        private final String category;
        private final ItemStack icon;
        private final int count;
        private final boolean selected;
        CategoryButton(int bx, int by, int bw, int bh, String category, Component message, ItemStack icon, int count,
                       boolean selected, OnPress action) {
            super(bx, by, bw, bh, message, action, DEFAULT_NARRATION);
            this.category = category;
            this.icon = icon;
            this.count = count;
            this.selected = selected;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            int accent = categoryColor(category);
            int bg = selected ? 0xCB143349 : isHoveredOrFocused() ? 0xBF122838 : 0x86101D28;
            softPanel(g, getX(), getY(), width, height, bg, selected ? accent : 0x442B4658);
            if (selected) {
                g.fill(getX(), getY() + 2, getX() + 2, getY() + height - 2, accent);
                glowOutline(g, getX(), getY(), width, height, accent);
            }
            if (height >= 18) g.renderItem(icon, getX() + 4, getY() + (height - 16) / 2);
            int tx = getX() + (height >= 18 ? 24 : 5);
            drawScaled(g, getMessage(), tx, getY() + (height - 7) / 2, selected ? WHITE : LIGHT,
                    layout.compact() ? 0.59F : 0.7F, width - (tx - getX()) - 22);
            String number = Integer.toString(count);
            drawScaled(g, Component.literal(number), getX() + width - 18, getY() + (height - 7) / 2, DIM,
                    layout.compact() ? 0.56F : 0.66F, 14);
        }
    }

    private final class ViewButton extends Button {
        private final boolean grid;
        private final boolean selected;
        ViewButton(int bx, int by, int bw, int bh, boolean grid, boolean selected, OnPress action) {
            super(bx, by, bw, bh, Component.empty(), action, DEFAULT_NARRATION);
            this.grid = grid;
            this.selected = selected;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            softPanel(g, getX(), getY(), width, height, selected ? 0xD1195272 : 0xB70D1D2A,
                    selected ? CYAN : 0xFF294B60);
            int cx = getX() + width / 2;
            int cy = getY() + height / 2;
            int c = selected ? WHITE : MUTED;
            if (grid) {
                for (int yy = -4; yy <= 2; yy += 6) for (int xx = -4; xx <= 2; xx += 6)
                    g.fill(cx + xx, cy + yy, cx + xx + 3, cy + yy + 3, c);
            } else {
                for (int yy = -4; yy <= 4; yy += 4) g.fill(cx - 5, cy + yy, cx + 5, cy + yy + 1, c);
            }
        }
    }

    private final class ChallengePanel extends ScrollPanel {
        private final List<MilestoneCatalog.Milestone> entries;
        private final List<Button> cards = new ArrayList<>();
        private final int columns;
        private final int cardWidth;
        private final int cardHeight;
        private final int gap;
        private int contentHeight;

        ChallengePanel(List<MilestoneCatalog.Milestone> entries) {
            super(PremiumSkillScreen.this.minecraft, layout.challengeMainWidth(), layout.challengeGridHeight(),
                    y + layout.challengeGridTop(), x + layout.challengeMainX(), 0, 3,
                    0x00000000, CYAN, 0xFF91E9FF);
            this.entries = List.copyOf(entries);
            this.columns = gridView ? layout.challengeColumns() : 1;
            this.gap = layout.challengeCardGap();
            this.cardWidth = Math.max(104, (width - 5 - gap * (columns - 1)) / columns);
            this.cardHeight = gridView ? layout.challengeCardHeight() : Math.max(54, layout.challengeCardHeight() - 4);
            for (int i = 0; i < entries.size(); i++) {
                int row = i / columns;
                int col = i % columns;
                int px = left + col * (cardWidth + gap);
                int py = top + row * (cardHeight + gap);
                var milestone = entries.get(i);
                cards.add(new ChallengeCardButton(px, py, cardWidth, cardHeight, milestone, b -> {
                    rememberScroll();
                    detail = milestone;
                    rebuildWidgets();
                }));
            }
            int rows = (entries.size() + columns - 1) / columns;
            contentHeight = Math.max(height, rows * (cardHeight + gap));
            positionCards();
        }

        float position() { return scrollDistance; }
        void restore(float value) {
            scrollDistance = Math.clamp(value, 0, Math.max(0, contentHeight - height));
            positionCards();
        }
        private void positionCards() {
            for (int i = 0; i < cards.size(); i++) {
                int row = i / columns;
                var b = cards.get(i);
                b.setY(top - (int) scrollDistance + row * (cardHeight + gap));
                b.visible = b.getY() + cardHeight > top && b.getY() < bottom;
            }
        }
        @Override protected int getContentHeight() { return contentHeight; }
        @Override protected void drawBackground(GuiGraphics g, Tesselator tess, float tick) { }
        @Override protected void drawPanel(GuiGraphics g, int entryRight, int baseY, Tesselator tess, int mx, int my) {
            positionCards();
            if (entries.isEmpty()) {
                drawScaled(g, text("no_results").copy().withStyle(ChatFormatting.BOLD), left + 9, top + 10, LIGHT, 0.72F, width - 25);
                drawWrappedScaled(g, text("no_results_help"), left + 9, top + 25, width - 25, MUTED, 0.6F, 4);
            } else for (Button button : cards) if (button.visible) button.render(g, mx, my, 0);
            g.flush();
        }
        @Override public boolean mouseClicked(double mx, double my, int button) {
            positionCards();
            return isMouseOver(mx, my) && super.mouseClicked(mx, my, button);
        }
        @Override public boolean mouseScrolled(double mx, double my, double horizontal, double vertical) {
            if (!isMouseOver(mx, my)) return false;
            boolean result = super.mouseScrolled(mx, my, horizontal, vertical);
            positionCards();
            return result;
        }
        @Override public List<? extends GuiEventListener> children() { return cards; }
        @Override public NarratableEntry.NarrationPriority narrationPriority() {
            return getFocused() == null ? NarratableEntry.NarrationPriority.NONE : NarratableEntry.NarrationPriority.FOCUSED;
        }
        @Override public void updateNarration(NarrationElementOutput output) {
            output.add(NarratedElementType.TITLE, pageName(Page.CHALLENGES));
        }
    }

    private final class ChallengeCardButton extends Button {
        private final MilestoneCatalog.Milestone milestone;
        ChallengeCardButton(int bx, int by, int bw, int bh, MilestoneCatalog.Milestone milestone, OnPress action) {
            super(bx, by, bw, bh, Component.translatable(milestone.titleKey()), action, DEFAULT_NARRATION);
            this.milestone = milestone;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            var state = ClientSkills.snapshot();
            boolean ready = state != null && state.available();
            boolean complete = ready && state.milestones().contains(milestone.id());
            int current = ready ? Math.max(0, state.progress().getOrDefault(milestone.id(), 0)) : 0;
            int target = Math.max(1, milestone.target());
            if (complete) current = target;

            int accent = complete ? GREEN : categoryColor(milestone.category());
            int bg = complete ? 0xC50A312A : isHoveredOrFocused() ? 0xDA182D3E : CARD;
            softPanel(g, getX(), getY(), width, height, bg, isHoveredOrFocused() || complete ? accent : 0xFF29495E);
            if (isHoveredOrFocused() || complete) glowOutline(g, getX(), getY(), width, height, accent);
            g.fill(getX(), getY() + 2, getX() + 2, getY() + height - 2, accent);

            int iconBox = Math.min(24, height - 24);
            int iconX = getX() + 6;
            int iconY = getY() + 6;
            softPanel(g, iconX, iconY, iconBox, iconBox, 0xB81A3141, 0x442E526A);
            g.renderItem(milestone.icon().getDefaultInstance(), iconX + (iconBox - 16) / 2, iconY + (iconBox - 16) / 2);

            float titleScale = layout.compact() ? 0.63F : 0.72F;
            float smallScale = layout.compact() ? 0.53F : 0.62F;
            int textX = iconX + iconBox + 6;
            int rightReserve = complete ? 15 : 3;
            drawScaled(g, getMessage().copy().withStyle(ChatFormatting.BOLD), textX, getY() + 6, WHITE, titleScale,
                    width - (textX - getX()) - rightReserve);
            drawScaled(g, categoryLabel(milestone.category()), textX, getY() + 17, categoryColor(milestone.category()), smallScale,
                    width - (textX - getX()) - 5);
            if (complete) drawScaled(g, Component.literal("✔"), getX() + width - 14, getY() + 5, GREEN, 0.74F, 12);

            int descY = getY() + (layout.compact() ? 30 : 33);
            int rewardW = Math.max(31, Math.round((font.width("+" + milestone.reward() + " SP") + 7) * smallScale));
            int descW = width - 12;
            if (height >= 64) drawWrappedScaled(g, Component.translatable(milestone.descriptionKey()), getX() + 6, descY,
                    descW, MUTED, smallScale, layout.compact() ? 1 : 2);

            int barY = getY() + height - 9;
            int barX = getX() + 6;
            int barW = Math.max(22, width - rewardW - 18);
            progressBar(g, barX, barY, barW, 4, current / (double) target, complete ? GREEN : CYAN);
            String progress = complete ? "1 / 1" : current + " / " + target;
            drawScaled(g, Component.literal(progress), barX + Math.max(0, barW - 28), barY - 8,
                    complete ? GREEN : MUTED, smallScale, 28);

            int rewardX = getX() + width - rewardW - 6;
            softPanel(g, rewardX, barY - 9, rewardW, 13, 0xD0143A64, 0xFF3679B7);
            drawScaled(g, Component.literal("+" + milestone.reward() + " SP").withStyle(ChatFormatting.BOLD), rewardX + 4,
                    barY - 6, WHITE, smallScale, rewardW - 7);
        }
    }

    private final class GenericPanel extends ScrollPanel {
        private final List<Card> cards;
        private final List<Button> actions = new ArrayList<>();
        private final List<Integer> offsets = new ArrayList<>();
        private final List<Integer> heights = new ArrayList<>();
        private int contentHeight;
        GenericPanel(List<Card> cards, int panelLeft, int panelTop, int panelWidth, int panelHeight) {
            super(PremiumSkillScreen.this.minecraft, panelWidth, panelHeight, panelTop, panelLeft, 0, 3,
                    0x00000000, CYAN, CYAN_LIGHT);
            this.cards = List.copyOf(cards);
            for (Card card : cards) {
                offsets.add(contentHeight);
                int h = layout.compact() ? 67 : 82;
                boolean actionable = !card.skill().isEmpty() || !card.product().isEmpty() || card.action() != null;
                heights.add(h);
                contentHeight += h + 6;
                Button action = new ChromeButton(left + 8, top, Math.max(70, panelWidth / 3), 18,
                        actionable ? card.actionText() : Component.empty(), false, b -> execute(card));
                action.visible = actionable;
                actions.add(action);
            }
            contentHeight = Math.max(height, contentHeight);
            positionButtons();
            refreshActions();
        }
        private void execute(Card card) {
            if (!card.skill().isEmpty()) { ClientSkills.buy(card.skill()); refreshActions(); }
            else if (!card.product().isEmpty()) confirmProduct(card.product());
            else if (card.action() != null) card.action().run();
        }
        float position() { return scrollDistance; }
        void restore(float value) { scrollDistance = Math.clamp(value, 0, Math.max(0, contentHeight - height)); positionButtons(); }
        private void positionButtons() {
            for (int i = 0; i < actions.size(); i++) {
                var action = actions.get(i);
                int cy = top - (int) scrollDistance + offsets.get(i);
                action.setX(left + width - action.getWidth() - 8);
                action.setY(cy + heights.get(i) - 23);
                action.visible = (!cards.get(i).skill().isEmpty() || !cards.get(i).product().isEmpty() || cards.get(i).action() != null)
                        && action.getY() + 18 > top && action.getY() < bottom;
            }
        }
        void refreshActions() {
            for (int i = 0; i < cards.size(); i++) {
                var card = cards.get(i);
                var action = actions.get(i);
                if (!card.skill().isEmpty()) {
                    action.setMessage(skillStatus(card.skill()));
                    action.active = canBuySkill(card.skill());
                } else if (!card.product().isEmpty()) {
                    action.setMessage(productStatus(card.product()));
                    action.active = canBuyProduct(card.product());
                } else action.active = true;
            }
        }
        @Override protected int getContentHeight() { return contentHeight; }
        @Override protected void drawBackground(GuiGraphics g, Tesselator tess, float tick) { }
        @Override protected void drawPanel(GuiGraphics g, int entryRight, int baseY, Tesselator tess, int mx, int my) {
            positionButtons();
            for (int i = 0; i < cards.size(); i++) {
                var card = cards.get(i);
                int cy = baseY + offsets.get(i);
                int h = heights.get(i);
                if (cy >= bottom || cy + h <= top) continue;
                softPanel(g, left, cy, width - 5, h, 0xCB122432, 0xFF29495E);
                g.fill(left, cy + 2, left + 2, cy + h - 2, CYAN);
                g.renderItem(card.icon(), left + 9, cy + 9);
                drawScaled(g, card.title().copy().withStyle(ChatFormatting.BOLD), left + 32, cy + 8, WHITE,
                        layout.compact() ? 0.66F : 0.76F, width - 44);
                drawWrappedScaled(g, card.description(), left + 9, cy + 27, width - 22, MUTED,
                        layout.compact() ? 0.56F : 0.64F, layout.compact() ? 2 : 3);
                drawScaled(g, card.badge(), left + 9, cy + h - 18, CYAN,
                        layout.compact() ? 0.52F : 0.61F, width / 2);
                actions.get(i).render(g, mx, my, 0);
            }
            g.flush();
        }
        @Override public boolean mouseClicked(double mx, double my, int button) {
            positionButtons(); return isMouseOver(mx, my) && super.mouseClicked(mx, my, button);
        }
        @Override public boolean mouseScrolled(double mx, double my, double horizontal, double vertical) {
            if (!isMouseOver(mx, my)) return false;
            boolean result = super.mouseScrolled(mx, my, horizontal, vertical); positionButtons(); return result;
        }
        @Override public List<? extends GuiEventListener> children() { return actions; }
        @Override public NarratableEntry.NarrationPriority narrationPriority() {
            return getFocused() == null ? NarratableEntry.NarrationPriority.NONE : NarratableEntry.NarrationPriority.FOCUSED;
        }
        @Override public void updateNarration(NarrationElementOutput output) { output.add(NarratedElementType.TITLE, pageName(page)); }
    }
}

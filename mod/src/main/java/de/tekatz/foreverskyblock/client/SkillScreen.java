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

/**
 * Design-2 progression menu: persistent top navigation, profile status header and
 * a dedicated challenge browser with filters on the left and a card grid on the right.
 */
public final class SkillScreen extends Screen {
    private static final int LIGHT = 0xFFF2F6FA;
    private static final int MUTED = 0xFF9DAFBE;
    private static final int DIM = 0xFF718493;
    private static final int CYAN = 0xFF33BDF2;
    private static final int GREEN = 0xFF43D477;
    private static final int GOLD = 0xFFFFC44A;
    private static final int PANEL = 0xF20B1621;
    private static final int PANEL_3 = 0xFF1A2D3C;
    private static final int BORDER = 0xFF314B5E;

    private enum Page { OVERVIEW, CHALLENGES, SHOP, RESEARCH, PERKS, PROFILE }
    private enum SortMode { PROGRESS, REWARD, NAME }

    private Page page = Page.OVERVIEW;
    private SortMode sortMode = SortMode.PROGRESS;
    private boolean gridView = true;
    private final EnumMap<Page, Integer> subsections = new EnumMap<>(Page.class);
    private final Map<String, Float> scrolls = new HashMap<>();

    private ProgressionMenuLayout layout;
    private int x, y;
    private String query = "";
    private int filter;
    private EditBox search;
    private CatalogPanel catalog;
    private ChallengePanel challenges;
    private Card detail;
    private SkillNetwork.Snapshot lastSnapshot;

    private record Card(Component title, Component description, Component badge, ItemStack icon,
                        String skill, String milestone, String product, Runnable action, Component actionText) {
        private Card(Component title, Component description, Component badge, ItemStack icon,
                     String skill, String milestone, Runnable action, Component actionText) {
            this(title, description, badge, icon, skill, milestone, "", action, actionText);
        }
    }

    public SkillScreen() { super(text("title")); }

    private static Component text(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.meta." + key, args);
    }

    private static Component old(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.skills." + key, args);
    }

    private static Component pageName(Page p) {
        return text("page." + p.name().toLowerCase(Locale.ROOT));
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

    private void rememberScroll() {
        if (catalog != null && detail == null) scrolls.put(location(), catalog.position());
        if (challenges != null && detail == null) scrolls.put(location(), challenges.position());
    }

    private void navigate(Page next, int subsection) {
        rememberScroll();
        page = next;
        subsections.put(next, Math.max(0, subsection));
        detail = null;
        rebuildWidgets();
    }

    private ItemStack pageIcon(Page p) {
        return switch (p) {
            case OVERVIEW -> Items.COMPASS.getDefaultInstance();
            case CHALLENGES -> Items.IRON_SWORD.getDefaultInstance();
            case SHOP -> Items.CHEST.getDefaultInstance();
            case RESEARCH -> Items.BOOK.getDefaultInstance();
            case PERKS -> Items.NETHER_STAR.getDefaultInstance();
            case PROFILE -> Items.PLAYER_HEAD.getDefaultInstance();
        };
    }

    private Button styledButton(Component message, int bx, int by, int bw, int bh, boolean selected, Runnable action) {
        return addRenderableWidget(new FlatButton(bx, by, bw, bh, message, selected, Items.AIR.getDefaultInstance(), b -> action.run()));
    }

    @Override
    protected void init() {
        layout = ProgressionMenuLayout.fit(width, height);
        x = (width - layout.width()) / 2;
        y = (height - layout.height()) / 2;
        clearWidgets();
        search = null;
        catalog = null;
        challenges = null;

        addTopNavigation();
        addRenderableWidget(new FlatButton(x + layout.width() - 30, y + 8, 22, 22,
                Component.literal("X"), false, Items.AIR.getDefaultInstance(), b -> onClose()));

        if (page == Page.CHALLENGES && detail == null) initChallenges();
        else initGeneralPage();

        lastSnapshot = ClientSkills.snapshot();
        refresh();
    }

    private void addTopNavigation() {
        int left = x + layout.padding();
        int top = y + layout.navTop();
        int gap = 2;
        int available = layout.width() - layout.padding() * 2;
        int w = (available - gap * (Page.values().length - 1)) / Page.values().length;
        int i = 0;
        for (Page p : Page.values()) {
            var button = new NavButton(left + i * (w + gap), top, w, layout.navHeight(), p);
            button.active = p != page;
            addRenderableWidget(button);
            i++;
        }
    }

    private void initChallenges() {
        String section = sections().get(sub());
        if (layout.tiny()) {
            int bx = x + layout.challengeMainX();
            int top = y + layout.contentTop() + 6;
            int bw = (layout.challengeMainWidth() - 4) / 2;
            styledButton(categoryLabel(section), bx, top, bw, 20, true,
                    () -> navigate(Page.CHALLENGES, (sub() + 1) % sections().size()));
            styledButton(filterLabel(filter), bx + bw + 4, top, bw, 20, false, () -> {
                filter = (filter + 1) % 3;
                scrolls.put(location(), 0F);
                rebuildWidgets();
            });
        } else if (layout.challengeSidebarWidth() > 0) {
            int sx = x + layout.padding() + 8;
            int sy = y + layout.contentTop() + 30;
            int sw = layout.challengeSidebarWidth() - 16;

            search = new EditBox(font, sx, sy, sw, 20, text("search"));
            search.setMaxLength(80);
            search.setHint(text("search"));
            search.setValue(query);
            search.setResponder(value -> {
                query = value;
                scrolls.put(location(), 0F);
                replaceChallengePanel();
            });
            addRenderableWidget(search);

            int fy = sy + 28;
            int fw = (sw - 6) / 3;
            for (int state = 0; state < 3; state++) {
                final int chosen = state;
                styledButton(filterLabel(state), sx + state * (fw + 3), fy, fw, 20, filter == state, () -> {
                    filter = chosen;
                    scrolls.put(location(), 0F);
                    rebuildWidgets();
                });
            }

            int cy = fy + 31;
            int index = 0;
            for (String category : sections()) {
                final int chosen = index;
                addRenderableWidget(new CategoryButton(sx, cy + index * 28, sw, 25, category,
                        categoryLabel(category), categoryIcon(category), categoryCount(category), chosen == sub(),
                        b -> navigate(Page.CHALLENGES, chosen)));
                index++;
            }
        } else {
            int bx = x + layout.challengeMainX();
            int top = y + layout.contentTop() + 34;
            int mainW = layout.challengeMainWidth();
            if (mainW < 520) {
                search = new EditBox(font, bx, top, mainW, 20, text("search"));
                search.setMaxLength(80);
                search.setHint(text("search"));
                search.setValue(query);
                search.setResponder(value -> {
                    query = value;
                    scrolls.put(location(), 0F);
                    replaceChallengePanel();
                });
                addRenderableWidget(search);
                int half = (mainW - 4) / 2;
                styledButton(categoryLabel(section), bx, top + 24, half, 20, true,
                        () -> navigate(Page.CHALLENGES, (sub() + 1) % sections().size()));
                styledButton(filterLabel(filter), bx + half + 4, top + 24, half, 20, false, () -> {
                    filter = (filter + 1) % 3;
                    scrolls.put(location(), 0F);
                    rebuildWidgets();
                });
            } else {
                int searchW = mainW - 270;
                search = new EditBox(font, bx, top, searchW, 20, text("search"));
                search.setMaxLength(80);
                search.setHint(text("search"));
                search.setValue(query);
                search.setResponder(value -> {
                    query = value;
                    scrolls.put(location(), 0F);
                    replaceChallengePanel();
                });
                addRenderableWidget(search);
                styledButton(categoryLabel(section), bx + searchW + 4, top, 138, 20, true,
                        () -> navigate(Page.CHALLENGES, (sub() + 1) % sections().size()));
                styledButton(filterLabel(filter), bx + searchW + 146, top, 120, 20, false, () -> {
                    filter = (filter + 1) % 3;
                    scrolls.put(location(), 0F);
                    rebuildWidgets();
                });
            }
        }

        addChallengeToolbar();
        replaceChallengePanel();
    }

    private void addChallengeToolbar() {
        if (layout.tiny()) return;
        int right = x + layout.challengeMainX() + layout.challengeMainWidth();
        int top = y + layout.contentTop() + 7;
        styledButton(sortLabel(), right - 150, top, 102, 22, false, () -> {
            sortMode = SortMode.values()[(sortMode.ordinal() + 1) % SortMode.values().length];
            scrolls.put(location(), 0F);
            replaceChallengePanel();
        });
        addRenderableWidget(new ViewButton(right - 44, top, 20, 22, true, gridView, b -> {
            if (!gridView) {
                gridView = true;
                scrolls.put(location(), 0F);
                rebuildWidgets();
            }
        }));
        addRenderableWidget(new ViewButton(right - 22, top, 20, 22, false, !gridView, b -> {
            if (gridView) {
                gridView = false;
                scrolls.put(location(), 0F);
                rebuildWidgets();
            }
        }));
    }

    private void initGeneralPage() {
        int mainX = x + layout.padding();
        int top = y + layout.contentTop();

        if (detail != null) {
            styledButton(text("back"), x + layout.width() - layout.padding() - 138, top + 6, 130, 22, false, () -> {
                detail = null;
                rebuildWidgets();
            });
        } else if (!sections().isEmpty() && page != Page.CHALLENGES) {
            int count = sections().size();
            int totalWidth = Math.min(layout.width() - layout.padding() * 2 - 16, count * 170);
            int bw = Math.max(110, totalWidth / count);
            for (int i = 0; i < count; i++) {
                final int chosen = i;
                styledButton(text("sub." + sections().get(i)), mainX + 8 + i * bw, top + 38, bw - 4, 24,
                        i == sub(), () -> navigate(page, chosen));
            }
        }

        replaceCatalog();
    }

    private Component filterLabel(int state) {
        if (state == 0) return Component.literal(stripPrefix(text("filter.0").getString()));
        return text("filter." + state);
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
            case "forever" -> 0xFF48D883;
            case "ae2" -> 0xFFC56BF3;
            case "mekanism" -> 0xFF35D8D1;
            case "powah" -> 0xFFFFC04B;
            case "mystical_agriculture" -> 0xFF75DC52;
            case "combat" -> 0xFFFF626B;
            case "contracts" -> 0xFFE8B65A;
            default -> CYAN;
        };
    }

    private void replaceChallengePanel() {
        if (layout == null) return;
        if (challenges != null) removeWidget(challenges);
        if ("contracts".equals(sections().get(sub()))) {
            challenges = null;
            return;
        }
        challenges = addRenderableWidget(new ChallengePanel(challengeEntries()));
        challenges.restore(scrolls.getOrDefault(location(), 0F));
    }

    private List<MilestoneCatalog.Milestone> challengeEntries() {
        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        String section = sections().get(sub());
        if ("contracts".equals(section)) return List.of();

        var entries = new ArrayList<MilestoneCatalog.Milestone>();
        for (var entry : MilestoneCatalog.MILESTONES) {
            if (!"all".equals(section) && !section.equals(entry.category())) continue;
            boolean complete = ready && state.milestones().contains(entry.id());
            if (filter == 1 && complete || filter == 2 && !complete) continue;
            var title = Component.translatable(entry.titleKey());
            var description = Component.translatable(entry.descriptionKey());
            String haystack = (title.getString() + " " + description.getString()).toLowerCase(Locale.ROOT);
            if (!haystack.contains(query.toLowerCase(Locale.ROOT))) continue;
            entries.add(entry);
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
        entries.sort(comparator);
        return entries;
    }

    private static double progressRatio(SkillNetwork.Snapshot state, MilestoneCatalog.Milestone m) {
        if (state == null || !state.available()) return 0;
        if (state.milestones().contains(m.id())) return 1;
        int current = Math.max(0, state.progress().getOrDefault(m.id(), 0));
        return m.target() <= 0 ? 0 : Math.min(1D, current / (double) m.target());
    }

    private void replaceCatalog() {
        if (layout == null) return;
        if (catalog != null) removeWidget(catalog);
        int panelTop = y + layout.contentTop() + (detail != null ? 40 : (!sections().isEmpty() && page != Page.CHALLENGES ? 70 : 42));
        int panelHeight = Math.max(24, y + layout.height() - layout.padding() - panelTop);
        catalog = addRenderableWidget(new CatalogPanel(cards(), x + layout.padding() + 8, panelTop,
                layout.width() - layout.padding() * 2 - 16, panelHeight));
        if (detail == null) catalog.restore(scrolls.getOrDefault(location(), 0F));
    }

    private Card info(String title, String description, ItemStack icon) {
        return new Card(text(title), text(description), Component.empty(), icon, "", "", null, Component.empty());
    }

    private Card link(String title, String description, ItemStack icon, Page target, int subsection) {
        return new Card(text(title), text(description), Component.empty(), icon, "", "", () -> navigate(target, subsection), text("open"));
    }

    private List<Card> cards() {
        if (detail != null) {
            return List.of(new Card(detail.title(), detail.description(), detail.badge(), detail.icon(),
                    detail.skill(), detail.milestone(), detail.product(), null, Component.empty()));
        }
        var cards = new ArrayList<Card>();
        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();

        switch (page) {
            case OVERVIEW -> {
                long done = ready ? MilestoneCatalog.MILESTONES.stream().filter(m -> state.milestones().contains(m.id())).count() : 0;
                cards.add(new Card(text("profile_summary"),
                        ready ? text("summary", state.points(), state.perkPoints(), done, MilestoneCatalog.MILESTONES.size(), ClientSkills.jetpackStage()) : old("storage_error"),
                        ready ? text("current_tier", tierName(state.shopTier())) : text("live_data"),
                        Items.NETHER_STAR.getDefaultInstance(), "", "", null, Component.empty()));
                cards.add(link("page.challenges", "overview_challenges", Items.IRON_SWORD.getDefaultInstance(), Page.CHALLENGES, 0));
                cards.add(link("page.shop", "overview_shop", Items.CHEST.getDefaultInstance(), Page.SHOP, 0));
                cards.add(link("sub.contracts", "contracts_pending", Items.WRITABLE_BOOK.getDefaultInstance(), Page.CHALLENGES, 7));
                cards.add(link("page.research", "overview_research", Items.BOOK.getDefaultInstance(), Page.RESEARCH, 0));
                cards.add(info("foundation_title", "foundation_description", Items.COMPASS.getDefaultInstance()));
            }
            case CHALLENGES -> { }
            case SHOP -> {
                if (sub() == 0) {
                    for (var skill : SkillCatalog.SKILLS) if (!skill.jetpack()) cards.add(skillCard(skill));
                } else if (sub() == 1) {
                    cards.add(new Card(text("sub.current_world"),
                            ready ? state.worldAvailable() ? text("world_identity", state.worldName()) : text("world_identity_error") : old("storage_error"),
                            text("current_world_scope"), Items.COMPASS.getDefaultInstance(), "", "", null, Component.empty()));
                    if (ready && state.canTestPoints()) {
                        var product = ShopCatalog.TEST;
                        cards.add(new Card(text("test_product_title"), text("test_product_description"),
                                text("world_product_badge", product.spCost(), tierName(product.requiredTier())),
                                Items.PAPER.getDefaultInstance(), "", "", product.id(), null, text("purchase")));
                    } else cards.add(info("world_products_title", "world_products_pending", Items.CHEST.getDefaultInstance()));
                } else {
                    cards.add(new Card(text("sub.tier_progress"), text("tiers_pending"),
                            ready ? text("current_tier", tierName(state.shopTier())) : old("unavailable"),
                            Items.COMPASS.getDefaultInstance(), "", "", null, Component.empty()));
                    cards.add(tierCard(0, "tier_foundations", "tier_foundations_description", Items.COBBLESTONE.getDefaultInstance()));
                    cards.add(tierCard(1, "tier_conqueror", "tier_conqueror_description", Items.DRAGON_HEAD.getDefaultInstance()));
                    cards.add(info("tier_engineer", "tier_engineer_description", Items.REDSTONE.getDefaultInstance()));
                    cards.add(info("tier_mastery", "tier_mastery_description", Items.DIAMOND.getDefaultInstance()));
                    cards.add(info("tier_ascended", "tier_ascended_description", ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance()));
                }
            }
            case RESEARCH -> { for (var skill : SkillCatalog.SKILLS) if (skill.jetpack()) cards.add(skillCard(skill)); }
            case PERKS -> {
                cards.add(new Card(old("perk_info_title"), old("perk_info_description"), old("core_conversion"),
                        ForeverSkyblock.FOREVER_CORE.get().getDefaultInstance(), "", "", null, Component.empty()));
                for (var skill : SkillCatalog.SKILLS) if (skill.perkCost() > 0) cards.add(skillCard(skill));
            }
            case PROFILE -> {
                cards.add(new Card(text("profile_summary"),
                        ready ? text("profile_values", state.points(), state.perkPoints(), state.skills().size(), state.milestones().size()) : old("storage_error"),
                        ready ? old(state.local() ? "local_scope" : "server_scope") : Component.empty(),
                        Items.PLAYER_HEAD.getDefaultInstance(), "", "", null, Component.empty()));
                if (ready) {
                    var a = state.accounting();
                    cards.add(new Card(text("history_title"),
                            text("accounting_values", a.spEarned(), a.ppEarned(), a.spSpent(), a.ppSpent(), a.spRefunded(), a.ppRefunded(), a.spAdmin()),
                            text(a.legacyHistoryUnknown() ? "history_legacy" : "history_tracked"),
                            Items.BOOK.getDefaultInstance(), "", "", null, Component.empty()));
                    cards.add(new Card(text("current_world_claims"),
                            state.worldAvailable() ? text("world_profile_values", state.worldName(), state.worldClaims().size()) : text("world_identity_error"),
                            text("current_world_scope"), Items.COMPASS.getDefaultInstance(), "", "", null, Component.empty()));
                }
                cards.add(new Card(old("reset_title"), text("reset_scope"), text("permanent_scope"),
                        Items.GRINDSTONE.getDefaultInstance(), "", "", this::confirmReset, old("reset")));
                if (ready && state.canTestPoints()) cards.add(new Card(text("development"), old("test_points_hint"), text("admin_only"),
                        Items.COMMAND_BLOCK.getDefaultInstance(), "", "", () -> { ClientSkills.testPoints(); refresh(); }, old("test_points")));
            }
        }
        return cards;
    }

    private static Component tierName(int tier) { return text("tier." + tier); }

    private Card tierCard(int tier, String title, String description, ItemStack icon) {
        var state = ClientSkills.snapshot();
        Component badge = state == null || !state.available() ? old("unavailable")
                : text(state.shopTier() >= tier ? "tier_unlocked" : "tier_locked");
        return new Card(text(title), text(description), badge, icon, "", "", null, Component.empty());
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
        var price = skill.perkCost() > 0 ? old("dual_cost", skill.cost(), skill.perkCost()) : old("cost", skill.cost());
        return new Card(Component.translatable(skill.titleKey()), description,
                text("permanent_scope").copy().append(" · ").append(price), icon, skill.id(), "", null, Component.empty());
    }

    private Component status(Card card) {
        var state = ClientSkills.snapshot();
        if (!card.skill().isEmpty()) {
            var skill = SkillCatalog.find(card.skill());
            String key = state == null || !state.available() ? "unavailable"
                    : state.rank(skill.id()) >= skill.maxRank() ? "unlocked"
                    : !skill.available() ? "missing_jetpacks"
                    : !skill.prerequisite().isEmpty() && state.rank(skill.prerequisite()) == 0 ? "requires_previous"
                    : ClientSkills.pending() ? "waiting"
                    : state.perkPoints() < skill.perkCost() ? "need_perks"
                    : state.points() < skill.cost() ? "need_points" : "unlock_short";
            return old(key, key.equals("need_perks") ? skill.perkCost() : skill.cost());
        }
        if (!card.milestone().isEmpty()) {
            if (state == null || !state.available()) return old("unavailable");
            if (state.milestones().contains(card.milestone())) return old("completed");
            int count = state.progress().getOrDefault(card.milestone(), 0);
            if (count < 0) return text("objective_revision");
            return text("objective_progress", count, ChallengeRules.find(card.milestone()).target());
        }
        if (!card.product().isEmpty()) {
            var product = ShopCatalog.find(card.product());
            if (state == null || !state.available()) return old("unavailable");
            if (!state.worldAvailable()) return text("world_unavailable");
            if (state.worldClaims().contains(product.id())) return text("world_already_claimed");
            if (ClientSkills.pending()) return old("waiting");
            if (state.shopTier() < product.requiredTier()) return text("world_requires_tier", tierName(product.requiredTier()));
            if (state.points() < product.spCost()) return old("need_points", product.spCost());
            return text("world_purchase_available");
        }
        return Component.empty();
    }

    private boolean canBuy(String id) {
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

    private void confirmReset() {
        var state = ClientSkills.snapshot();
        if (state == null || !state.available() || ClientSkills.pending()) return;
        rememberScroll();
        int sp = SkillCatalog.SKILLS.stream().mapToInt(s -> state.rank(s.id()) * s.cost()).sum();
        int pp = SkillCatalog.SKILLS.stream().mapToInt(s -> state.rank(s.id()) * s.perkCost()).sum();
        minecraft.setScreen(new ConfirmScreen(yes -> {
            minecraft.setScreen(this);
            if (yes) { ClientSkills.reset(); refresh(); }
        }, old("reset_title"), old("reset_description", sp, pp), old("reset_confirm"), Component.translatable("gui.cancel")));
    }

    private void confirmProduct(String id) {
        var state = ClientSkills.snapshot();
        var product = ShopCatalog.find(id);
        if (product == null || !canBuyProduct(id)) return;
        rememberScroll();
        minecraft.setScreen(new ConfirmScreen(yes -> {
            minecraft.setScreen(this);
            if (yes) { ClientSkills.buyProduct(id); refresh(); }
        }, text("purchase_confirm_title"), text("purchase_confirm_description", product.spCost(), state.worldName()),
                text("purchase_confirm"), Component.translatable("gui.cancel")));
    }

    public void refresh() {
        if (catalog == null && challenges == null) return;
        if (lastSnapshot != ClientSkills.snapshot()) {
            rememberScroll();
            lastSnapshot = ClientSkills.snapshot();
            if (page == Page.CHALLENGES && detail == null) replaceChallengePanel();
            else replaceCatalog();
        }
        if (catalog != null) catalog.refreshActions();
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
        super.renderBackground(g, mx, my, tick);
        g.fill(x, y, x + layout.width(), y + layout.height(), 0xFF294052);
        g.fill(x + 1, y + 1, x + layout.width() - 1, y + layout.height() - 1, PANEL);
        drawHeader(g);
        drawNavFrame(g);
        drawContentFrame(g);
        if (page == Page.CHALLENGES && detail == null) drawChallengesBackground(g);
        else drawGeneralBackground(g);
    }

    private void drawHeader(GuiGraphics g) {
        int left = x + layout.padding();
        int top = y + 6;
        int headerBottom = y + layout.navTop() - 6;
        g.fill(left, top, x + layout.width() - layout.padding(), headerBottom, 0xD9101D29);
        g.fill(left, headerBottom - 1, x + layout.width() - layout.padding(), headerBottom, BORDER);

        if (layout.tiny()) {
            g.drawString(font, "FOREVER SKYBLOCK", left + 8, top + 5, CYAN, false);
            return;
        }

        int brandW = layout.compact() ? 122 : 210;
        g.renderItem(Items.GRASS_BLOCK.getDefaultInstance(), left + 10, top + 13);
        g.drawString(font, "FOREVER", left + 32, top + 8, LIGHT, false);
        g.drawString(font, "SKYBLOCK", left + 32, top + 20, CYAN, false);
        if (!layout.compact()) g.drawString(font, "SKILLS · CHALLENGES · PROGRESS", left + 10, top + 37, DIM, false);

        var state = ClientSkills.snapshot();
        boolean ready = state != null && state.available();
        int statsLeft = left + brandW;
        int right = x + layout.width() - layout.padding() - 32;
        int gap = 4;
        int statCount = layout.compact() ? 3 : 4;
        int statW = Math.max(42, (right - statsLeft - gap * (statCount - 1)) / statCount);

        drawStat(g, statsLeft, top + 6, statW, headerBottom - top - 12,
                Items.DIAMOND.getDefaultInstance(), "SP", ready ? Integer.toString(state.points()) : "—", CYAN);
        drawStat(g, statsLeft + statW + gap, top + 6, statW, headerBottom - top - 12,
                Items.NETHER_STAR.getDefaultInstance(), "PP", ready ? Integer.toString(state.perkPoints()) : "—", GOLD);
        drawStat(g, statsLeft + (statW + gap) * 2, top + 6, statW, headerBottom - top - 12,
                Items.AMETHYST_SHARD.getDefaultInstance(), "TIER",
                ready ? stripPrefix(tierName(state.shopTier()).getString()) : "—", CYAN);
        if (!layout.compact()) {
            String world = ready && state.worldAvailable() && !state.worldName().isBlank() ? state.worldName()
                    : ready ? old(state.local() ? "local_scope" : "server_scope").getString() : "—";
            drawStat(g, statsLeft + (statW + gap) * 3, top + 6, statW, headerBottom - top - 12,
                    Items.COMPASS.getDefaultInstance(), "WORLD", world, LIGHT);
        }
    }

    private void drawStat(GuiGraphics g, int sx, int sy, int sw, int sh, ItemStack icon, String label, String value, int valueColor) {
        g.fill(sx, sy, sx + sw, sy + sh, 0xE1142431);
        outline(g, sx, sy, sw, sh, 0xFF263D4D);
        if (sw >= 86) g.renderItem(icon, sx + 7, sy + Math.max(3, (sh - 16) / 2));
        int tx = sx + (sw >= 86 ? 29 : 7);
        g.drawString(font, label, tx, sy + 5, MUTED, false);
        String fitted = font.plainSubstrByWidth(value, Math.max(20, sx + sw - tx - 6));
        g.drawString(font, fitted, tx, sy + 17, valueColor, false);
    }

    private void drawNavFrame(GuiGraphics g) {
        int left = x + layout.padding();
        int top = y + layout.navTop();
        g.fill(left, top, x + layout.width() - layout.padding(), top + layout.navHeight(), 0xF20E1A25);
        outline(g, left, top, layout.width() - layout.padding() * 2, layout.navHeight(), BORDER);
    }

    private void drawContentFrame(GuiGraphics g) {
        int left = x + layout.padding();
        int top = y + layout.contentTop();
        int w = layout.width() - layout.padding() * 2;
        int h = layout.height() - layout.contentTop() - layout.padding();
        g.fill(left, top, left + w, top + h, 0xF20A141E);
        outline(g, left, top, w, h, 0xFF243B4C);
    }

    private void drawChallengesBackground(GuiGraphics g) {
        if (layout.challengeSidebarWidth() > 0) {
            int left = x + layout.padding();
            int top = y + layout.contentTop();
            g.fill(left, top, left + layout.challengeSidebarWidth(), y + layout.height() - layout.padding(), 0xF20D1B27);
            g.fill(left + layout.challengeSidebarWidth(), top, left + layout.challengeSidebarWidth() + 1,
                    y + layout.height() - layout.padding(), 0xFF294356);
            g.drawString(font, "FILTERS", left + 8, top + 9, LIGHT, false);
        }
        int mainX = x + layout.challengeMainX();
        int top = y + layout.contentTop();
        if (!layout.tiny()) {
            g.drawString(font, pageName(Page.CHALLENGES), mainX + 8, top + 8, LIGHT, false);
            g.drawString(font, old("tasks_hint"), mainX + 8, top + 20, MUTED, false);
        }
        if ("contracts".equals(sections().get(sub()))) {
            int cy = top + 62;
            g.renderItem(Items.WRITABLE_BOOK.getDefaultInstance(), mainX + 14, cy + 6);
            g.drawString(font, text("sub.contracts"), mainX + 38, cy + 7, GOLD, false);
            drawWrapped(g, text("contracts_pending"), mainX + 14, cy + 28, layout.challengeMainWidth() - 28, MUTED, 7);
        }
    }

    private void drawGeneralBackground(GuiGraphics g) {
        int left = x + layout.padding() + 8;
        int top = y + layout.contentTop() + 8;
        g.drawString(font, pageName(page), left, top, LIGHT, false);
        Component subtitle = switch (page) {
            case OVERVIEW -> text("profile_summary");
            case SHOP -> text("overview_shop");
            case RESEARCH -> text("overview_research");
            case PERKS -> old("perks_hint");
            case PROFILE -> text("profile_summary");
            case CHALLENGES -> old("tasks_hint");
        };
        g.drawString(font, font.plainSubstrByWidth(subtitle.getString(), layout.width() - 60), left, top + 12, MUTED, false);
    }

    private static void outline(GuiGraphics g, int ox, int oy, int ow, int oh, int color) {
        g.fill(ox, oy, ox + ow, oy + 1, color);
        g.fill(ox, oy + oh - 1, ox + ow, oy + oh, color);
        g.fill(ox, oy, ox + 1, oy + oh, color);
        g.fill(ox + ow - 1, oy, ox + ow, oy + oh, color);
    }

    private int drawWrapped(GuiGraphics g, Component component, int dx, int dy, int maxWidth, int color, int maxLines) {
        int shown = 0;
        for (var line : font.split(component, maxWidth)) {
            if (shown++ >= maxLines) break;
            g.drawString(font, line, dx, dy, color, false);
            dy += 10;
        }
        return dy;
    }

    private final class NavButton extends Button {
        private final Page target;
        NavButton(int bx, int by, int bw, int bh, Page target) {
            super(bx, by, bw, bh, pageName(target), b -> navigate(target, subsections.getOrDefault(target, 0)), DEFAULT_NARRATION);
            this.target = target;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            boolean selected = target == page;
            int bg = selected ? 0xFF153C55 : isHoveredOrFocused() ? 0xFF142C3D : 0xFF0E1A25;
            g.fill(getX() + 1, getY() + 1, getX() + width - 1, getY() + height - 1, bg);
            if (selected) {
                g.fill(getX(), getY(), getX() + width, getY() + 2, CYAN);
                outline(g, getX(), getY(), width, height, 0xFF2CA8DE);
            } else outline(g, getX(), getY(), width, height, 0xFF243C4D);
            String label = font.plainSubstrByWidth(getMessage().getString(), Math.max(8, width - 8));
            int textW = font.width(label);
            boolean showIcon = width > textW + 32;
            int total = textW + (showIcon ? 21 : 0);
            int start = getX() + (width - total) / 2;
            if (showIcon) {
                g.renderItem(pageIcon(target), start, getY() + (height - 16) / 2);
                start += 21;
            }
            g.drawString(font, label, start, getY() + (height - 8) / 2, selected ? LIGHT : MUTED, false);
        }
    }

    private final class FlatButton extends Button {
        private final boolean selected;
        private final ItemStack icon;
        FlatButton(int bx, int by, int bw, int bh, Component message, boolean selected, ItemStack icon, OnPress action) {
            super(bx, by, bw, bh, message, action, DEFAULT_NARRATION);
            this.selected = selected;
            this.icon = icon;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            int bg = selected ? 0xFF174868 : isHoveredOrFocused() ? 0xFF173447 : 0xFF102330;
            g.fill(getX(), getY(), getX() + width, getY() + height, bg);
            outline(g, getX(), getY(), width, height, selected ? CYAN : 0xFF315065);
            int tx = getX() + 7;
            if (!icon.isEmpty() && width > 45) {
                g.renderItem(icon, tx, getY() + (height - 16) / 2);
                tx += 20;
            }
            String msg = font.plainSubstrByWidth(getMessage().getString(), width - (tx - getX()) - 7);
            g.drawString(font, msg, tx, getY() + (height - 8) / 2, active ? (selected ? LIGHT : MUTED) : DIM, false);
        }
    }

    private final class CategoryButton extends Button {
        private final ItemStack icon;
        private final int count;
        private final boolean selected;
        private final String category;
        CategoryButton(int bx, int by, int bw, int bh, String category, Component message, ItemStack icon, int count,
                       boolean selected, OnPress action) {
            super(bx, by, bw, bh, message, action, DEFAULT_NARRATION);
            this.icon = icon; this.count = count; this.selected = selected; this.category = category;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            int accent = categoryColor(category);
            int bg = selected ? 0xFF18384C : isHoveredOrFocused() ? 0xFF142C3B : 0xFF0D1D28;
            g.fill(getX(), getY(), getX() + width, getY() + height, bg);
            if (selected) {
                g.fill(getX(), getY(), getX() + 3, getY() + height, accent);
                outline(g, getX(), getY(), width, height, 0xFF305266);
            }
            g.renderItem(icon, getX() + 6, getY() + (height - 16) / 2);
            String label = font.plainSubstrByWidth(getMessage().getString(), width - 48);
            g.drawString(font, label, getX() + 28, getY() + (height - 8) / 2, selected ? LIGHT : MUTED, false);
            String c = Integer.toString(count);
            g.drawString(font, c, getX() + width - 7 - font.width(c), getY() + (height - 8) / 2, DIM, false);
        }
    }

    private final class ViewButton extends Button {
        private final boolean grid;
        private final boolean selected;
        ViewButton(int bx, int by, int bw, int bh, boolean grid, boolean selected, OnPress action) {
            super(bx, by, bw, bh, Component.empty(), action, DEFAULT_NARRATION);
            this.grid = grid; this.selected = selected;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            int bg = selected ? 0xFF174868 : isHoveredOrFocused() ? 0xFF173447 : 0xFF102330;
            g.fill(getX(), getY(), getX() + width, getY() + height, bg);
            outline(g, getX(), getY(), width, height, selected ? CYAN : 0xFF315065);
            int cx = getX() + width / 2;
            int cy = getY() + height / 2;
            if (grid) {
                for (int yy = -4; yy <= 2; yy += 6) for (int xx = -4; xx <= 2; xx += 6)
                    g.fill(cx + xx, cy + yy, cx + xx + 4, cy + yy + 4, selected ? LIGHT : MUTED);
            } else {
                for (int yy = -4; yy <= 4; yy += 4)
                    g.fill(cx - 5, cy + yy, cx + 5, cy + yy + 1, selected ? LIGHT : MUTED);
            }
        }
    }

    private final class ChallengePanel extends ScrollPanel {
        private final List<MilestoneCatalog.Milestone> entries;
        private final List<Button> cards = new ArrayList<>();
        private final int columns;
        private final int cardWidth;
        private final int cardHeight;
        private int contentHeight;

        ChallengePanel(List<MilestoneCatalog.Milestone> entries) {
            super(SkillScreen.this.minecraft, layout.challengeMainWidth(), layout.challengeGridHeight(),
                    y + layout.challengeGridTop(), x + layout.challengeMainX(), 0, 4,
                    0xFF0A141E, CYAN, 0xFFA6EAF0);
            this.entries = List.copyOf(entries);
            this.columns = gridView ? layout.challengeColumns() : 1;
            int gap = 7;
            this.cardWidth = Math.max(130, (width - 8 - gap * (columns - 1)) / columns);
            this.cardHeight = gridView ? 88 : 72;

            for (int i = 0; i < entries.size(); i++) {
                var entry = entries.get(i);
                int row = i / columns;
                int col = i % columns;
                int px = left + col * (cardWidth + gap);
                int py = top + row * (cardHeight + gap);
                cards.add(new ChallengeCardButton(px, py, cardWidth, cardHeight, entry, b -> {
                    rememberScroll();
                    detail = challengeCard(entry);
                    rebuildWidgets();
                }));
            }
            int rows = (entries.size() + columns - 1) / columns;
            contentHeight = Math.max(height, rows * (cardHeight + gap));
            positionCards();
        }

        private Card challengeCard(MilestoneCatalog.Milestone entry) {
            return new Card(Component.translatable(entry.titleKey()), Component.translatable(entry.descriptionKey()),
                    text("milestone_badge", entry.reward()), entry.icon().getDefaultInstance(), "", entry.id(), null, Component.empty());
        }
        float position() { return scrollDistance; }
        void restore(float value) {
            scrollDistance = Math.clamp(value, 0, Math.max(0, contentHeight - height));
            positionCards();
        }
        private void positionCards() {
            int gap = 7;
            for (int i = 0; i < cards.size(); i++) {
                int row = i / columns;
                var b = cards.get(i);
                b.setY(top - (int) scrollDistance + row * (cardHeight + gap));
                b.visible = b.getY() + cardHeight > top && b.getY() < bottom;
            }
        }
        @Override protected int getContentHeight() { return contentHeight; }
        @Override protected void drawBackground(GuiGraphics g, Tesselator tess, float tick) { g.fill(left, top, right, bottom, 0xFF08121A); }
        @Override protected void drawPanel(GuiGraphics g, int entryRight, int baseY, Tesselator tess, int mx, int my) {
            positionCards();
            if (entries.isEmpty()) {
                g.renderItem(Items.BOOK.getDefaultInstance(), left + 10, top + 10);
                g.drawString(font, text("no_results"), left + 34, top + 12, LIGHT, false);
                drawWrapped(g, text("no_results_help"), left + 10, top + 34, width - 30, MUTED, 5);
            } else for (Button b : cards) if (b.visible) b.render(g, mx, my, 0);
            g.flush();
        }
        @Override public boolean mouseClicked(double mx, double my, int button) {
            positionCards(); return isMouseOver(mx, my) && super.mouseClicked(mx, my, button);
        }
        @Override public boolean mouseScrolled(double mx, double my, double horizontal, double vertical) {
            if (!isMouseOver(mx, my)) return false;
            boolean result = super.mouseScrolled(mx, my, horizontal, vertical); positionCards(); return result;
        }
        @Override public List<? extends GuiEventListener> children() { return cards; }
        @Override public NarratableEntry.NarrationPriority narrationPriority() {
            return getFocused() != null ? NarratableEntry.NarrationPriority.FOCUSED : NarratableEntry.NarrationPriority.NONE;
        }
        @Override public void updateNarration(NarrationElementOutput output) { output.add(NarratedElementType.TITLE, pageName(Page.CHALLENGES)); }
    }

    private final class ChallengeCardButton extends Button {
        private final MilestoneCatalog.Milestone entry;
        ChallengeCardButton(int bx, int by, int bw, int bh, MilestoneCatalog.Milestone entry, OnPress action) {
            super(bx, by, bw, bh, Component.translatable(entry.titleKey()), action, DEFAULT_NARRATION);
            this.entry = entry;
        }
        @Override protected void renderWidget(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
            var state = ClientSkills.snapshot();
            boolean ready = state != null && state.available();
            boolean complete = ready && state.milestones().contains(entry.id());
            int current = ready ? Math.max(0, state.progress().getOrDefault(entry.id(), 0)) : 0;
            int target = Math.max(1, entry.target());
            if (complete) current = target;

            int accent = complete ? GREEN : categoryColor(entry.category());
            int bg = isHoveredOrFocused() ? 0xFF182D3B : 0xFF112330;
            g.fill(getX(), getY(), getX() + width, getY() + height, bg);
            g.fill(getX(), getY(), getX() + 3, getY() + height, accent);
            outline(g, getX(), getY(), width, height, isHoveredOrFocused() ? accent : 0xFF2A475A);
            g.fill(getX() + 9, getY() + 9, getX() + 35, getY() + 35, 0xFF182B38);
            g.renderItem(entry.icon().getDefaultInstance(), getX() + 14, getY() + 14);

            int rewardW = font.width("+" + entry.reward() + " SP") + 10;
            String title = font.plainSubstrByWidth(getMessage().getString(), width - 48 - rewardW);
            g.drawString(font, title, getX() + 42, getY() + 9, LIGHT, false);
            String category = categoryLabel(entry.category()).getString();
            g.drawString(font, font.plainSubstrByWidth(category, width - 52), getX() + 42, getY() + 22,
                    categoryColor(entry.category()), false);
            drawWrapped(g, Component.translatable(entry.descriptionKey()), getX() + 10, getY() + 38,
                    width - rewardW - 32, MUTED, 2);

            int barY = getY() + height - 18;
            int barX = getX() + 10;
            int barW = Math.max(20, width - rewardW - 34);
            g.fill(barX, barY, barX + barW, barY + 7, 0xFF08131C);
            outline(g, barX, barY, barW, 7, 0xFF426078);
            int fillW = (int) Math.round((barW - 2) * Math.min(1D, current / (double) target));
            if (fillW > 0) g.fill(barX + 1, barY + 1, barX + 1 + fillW, barY + 6, complete ? GREEN : CYAN);

            String progress = complete ? "✓" : current + " / " + target;
            g.drawString(font, progress, barX + Math.max(0, barW - font.width(progress)), barY - 10, complete ? GREEN : MUTED, false);
            int rwX = getX() + width - rewardW - 8;
            g.fill(rwX, barY - 10, rwX + rewardW, barY + 7, 0xFF153454);
            outline(g, rwX, barY - 10, rewardW, 17, 0xFF3E78B7);
            String reward = "+" + entry.reward() + " SP";
            g.drawString(font, reward, rwX + (rewardW - font.width(reward)) / 2, barY - 5, LIGHT, false);
        }
    }

    private final class CatalogPanel extends ScrollPanel {
        private final List<Card> cards;
        private final List<Button> actions = new ArrayList<>();
        private final List<Integer> offsets = new ArrayList<>(), heights = new ArrayList<>();
        private int contentHeight;
        CatalogPanel(List<Card> cards, int panelLeft, int panelTop, int panelWidth, int panelHeight) {
            super(SkillScreen.this.minecraft, panelWidth, panelHeight, panelTop, panelLeft, 0, 4,
                    0xFF0A141E, CYAN, 0xFFA6EAF0);
            this.cards = List.copyOf(cards);
            for (Card card : cards) {
                offsets.add(contentHeight);
                int h = 18 + Math.max(18, lines(card.title(), cardWidth() - 44) * 10)
                        + lines(card.description(), cardWidth() - 20) * 10 + lines(card.badge(), cardWidth() - 20) * 10 + 26;
                boolean actionable = !card.skill().isEmpty() || !card.product().isEmpty() || card.action() != null
                        || !card.milestone().isEmpty() && detail == null;
                if (actionable) h += 26;
                heights.add(h); contentHeight += h + 7;
                Button b = new FlatButton(left + 10, top, cardWidth() - 20, 20, card.actionText(), false,
                        Items.AIR.getDefaultInstance(), ignored -> {
                    if (!card.skill().isEmpty()) { ClientSkills.buy(card.skill()); refreshActions(); }
                    else if (!card.product().isEmpty()) confirmProduct(card.product());
                    else if (card.action() != null) card.action().run();
                    else if (!card.milestone().isEmpty()) { rememberScroll(); detail = card; rebuildWidgets(); }
                });
                b.visible = actionable; actions.add(b);
            }
            contentHeight = Math.max(height, contentHeight); positionButtons(); refreshActions();
        }
        private int lines(Component c, int w) { return c.getString().isEmpty() ? 0 : font.split(c, w).size(); }
        private int cardWidth() { return width - 8; }
        float position() { return scrollDistance; }
        void restore(float value) { scrollDistance = Math.clamp(value, 0, Math.max(0, contentHeight - height)); positionButtons(); }
        private void positionButtons() {
            for (int i = 0; i < actions.size(); i++) {
                var card = cards.get(i); var b = actions.get(i);
                b.setY(top - (int) scrollDistance + offsets.get(i) + heights.get(i) - 28);
                boolean actionable = !card.skill().isEmpty() || !card.product().isEmpty() || card.action() != null
                        || !card.milestone().isEmpty() && detail == null;
                b.visible = actionable && b.getY() + 20 > top && b.getY() < bottom;
            }
        }
        void refreshActions() {
            var state = ClientSkills.snapshot();
            for (int i = 0; i < cards.size(); i++) {
                var c = cards.get(i); var b = actions.get(i);
                if (!c.skill().isEmpty()) { b.setMessage(status(c)); b.active = canBuy(c.skill()); }
                else if (!c.product().isEmpty()) { b.setMessage(status(c)); b.active = canBuyProduct(c.product()); }
                else {
                    b.active = true;
                    if (page == Page.PROFILE && c.action() != null) b.active = state != null && state.available() && !ClientSkills.pending();
                }
            }
        }
        @Override protected int getContentHeight() { return contentHeight; }
        @Override protected void drawBackground(GuiGraphics g, Tesselator tess, float tick) { g.fill(left, top, right, bottom, 0xFF08121A); }
        private int drawLines(GuiGraphics g, Component value, int dx, int dy, int w, int color) {
            if (value.getString().isEmpty()) return dy;
            for (var line : font.split(value, w)) { g.drawString(font, line, dx, dy, color, false); dy += 10; }
            return dy;
        }
        @Override protected void drawPanel(GuiGraphics g, int entryRight, int baseY, Tesselator tess, int mx, int my) {
            positionButtons(); var state = ClientSkills.snapshot();
            for (int i = 0; i < cards.size(); i++) {
                var c = cards.get(i); int cy = baseY + offsets.get(i), h = heights.get(i), w = cardWidth();
                if (cy >= bottom || cy + h <= top) continue;
                boolean complete = state != null && state.available() && (!c.milestone().isEmpty() ? state.milestones().contains(c.milestone())
                        : !c.skill().isEmpty() ? state.rank(c.skill()) > 0 : !c.product().isEmpty() && state.worldClaims().contains(c.product()));
                g.fill(left, cy, left + w, cy + h, complete ? 0xFF173B32 : PANEL_3);
                outline(g, left, cy, w, h, complete ? GREEN : 0xFF315065);
                g.fill(left, cy, left + 3, cy + h, complete ? GREEN : CYAN);
                g.renderItem(c.icon(), left + 11, cy + 10);
                int dy = drawLines(g, c.title(), left + 36, cy + 9, w - 48, LIGHT);
                dy = Math.max(dy, cy + 27) + 5;
                dy = drawLines(g, c.description(), left + 11, dy, w - 22, MUTED) + 6;
                dy = drawLines(g, c.badge(), left + 11, dy, w - 22, CYAN) + 4;
                if (c.skill().isEmpty()) drawLines(g, status(c), left + 11, dy, w - 22, complete ? GREEN : MUTED);
                actions.get(i).render(g, mx, my, 0);
            }
            g.flush();
        }
        @Override public boolean mouseClicked(double mx, double my, int button) { positionButtons(); return isMouseOver(mx, my) && super.mouseClicked(mx, my, button); }
        @Override public boolean mouseScrolled(double mx, double my, double horizontal, double vertical) {
            if (!isMouseOver(mx, my)) return false;
            boolean result = super.mouseScrolled(mx, my, horizontal, vertical); positionButtons(); return result;
        }
        @Override public void setFocused(GuiEventListener listener) {
            super.setFocused(listener); int i = actions.indexOf(listener);
            if (i >= 0) {
                int b = offsets.get(i) + heights.get(i) - 8;
                if (b - scrollDistance > height) scrollDistance = b - height;
                if (b - 20 - scrollDistance < 0) scrollDistance = Math.max(0, b - 20);
                positionButtons();
            }
        }
        @Override public List<? extends GuiEventListener> children() { return actions; }
        @Override public NarratableEntry.NarrationPriority narrationPriority() {
            return getFocused() != null ? NarratableEntry.NarrationPriority.FOCUSED : NarratableEntry.NarrationPriority.NONE;
        }
        @Override public void updateNarration(NarrationElementOutput output) {
            output.add(NarratedElementType.TITLE, pageName(page));
            for (int i = 0; i < actions.size(); i++) if (actions.get(i).isFocused())
                output.add(NarratedElementType.USAGE, cards.get(i).title().copy().append(". ").append(cards.get(i).description()).append(". ").append(actions.get(i).getMessage()));
        }
    }
}

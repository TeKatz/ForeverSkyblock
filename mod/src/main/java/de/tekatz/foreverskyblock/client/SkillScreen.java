package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.vertex.Tesselator;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import de.tekatz.foreverskyblock.progression.JetpackGate;
import de.tekatz.foreverskyblock.progression.JetpackResearch;
import de.tekatz.foreverskyblock.compat.IronJetpacksBridge;
import net.minecraft.world.item.ItemStack;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.narration.NarratableEntry;
import net.minecraft.client.gui.narration.NarratedElementType;
import net.minecraft.client.gui.narration.NarrationElementOutput;
import net.minecraft.client.gui.screens.ConfirmScreen;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.Item;
import net.neoforged.neoforge.client.gui.widget.ScrollPanel;

/** Self-contained cards put each effect, price and action together in a scrollable catalog. */
public final class SkillScreen extends Screen {
    private static final int LIGHT = 0xFFE8EFF4, MUTED = 0xFFABBBC7, CYAN = 0xFF7DDDE2, GREEN = 0xFF89DBA5;
    private int x, y, panelWidth, panelHeight;
    private boolean tasks, jetpacks, perks;
    private Button skillsTab, tasksTab, jetpacksTab, perksTab, reset, testPoints;
    private CatalogPanel catalog;

    public SkillScreen() { super(label("title")); }
    private static Component label(String key, Object... arguments) {
        return Component.translatable("gui.forever_skyblock.skills." + key, arguments);
    }
    @Override protected void init() {
        panelWidth = Math.min(520, width - 16);
        panelHeight = Math.min(344, height - 16);
        x = (width - panelWidth) / 2;
        y = (height - panelHeight) / 2;
        clearWidgets();
        int tabWidth = (panelWidth - 30) / 4;
        skillsTab = addRenderableWidget(Button.builder(label("skills_short"), b -> changeTab(0))
                .bounds(x + 12, y + 44, tabWidth, 20).build());
        jetpacksTab = addRenderableWidget(Button.builder(label("jetpacks_tab"), b -> changeTab(1))
                .bounds(x + 14 + tabWidth, y + 44, tabWidth, 20).build());
        tasksTab = addRenderableWidget(Button.builder(label("tasks_short"), b -> changeTab(2))
                .bounds(x + 16 + tabWidth * 2, y + 44, tabWidth, 20).build());
        perksTab = addRenderableWidget(Button.builder(label("perks_tab"), b -> changeTab(3))
                .bounds(x + 18 + tabWidth * 3, y + 44, tabWidth, 20).build());
        catalog = addRenderableWidget(new CatalogPanel());
        reset = addRenderableWidget(Button.builder(label("reset"), b -> confirmReset())
                .bounds(x + 12, y + panelHeight - 26, (panelWidth - 108) / 2, 20).build());
        testPoints = addRenderableWidget(Button.builder(label("test_points"), b -> { ClientSkills.testPoints(); refresh(); })
                .bounds(x + 18 + (panelWidth - 108) / 2, y + panelHeight - 26, (panelWidth - 108) / 2, 20).build());
        testPoints.setTooltip(net.minecraft.client.gui.components.Tooltip.create(label("test_points_hint")));
        addRenderableWidget(Button.builder(Component.translatable("gui.done"), b -> onClose())
                .bounds(x + panelWidth - 84, y + panelHeight - 26, 72, 20).build());
        refresh();
    }
    private void changeTab(int value) {
        tasks = value == 2;
        jetpacks = value == 1;
        perks = value == 3;
        rebuildWidgets();
    }
    private void confirmReset() {
        var state = ClientSkills.snapshot();
        if (state == null || !state.available() || ClientSkills.pending()) return;
        int refund = 0;
        for (var skill : SkillCatalog.SKILLS) refund += state.rank(skill.id()) * skill.cost();
        minecraft.setScreen(new ConfirmScreen(confirmed -> {
            minecraft.setScreen(this);
            if (confirmed) { ClientSkills.reset(); refresh(); }
        }, label("reset_title"), label("reset_description", refund, SkillCatalog.SKILLS.stream().mapToInt(s -> state.rank(s.id()) * s.perkCost()).sum()), label("reset_confirm"), Component.translatable("gui.cancel")));
    }
    public void refresh() {
        if (catalog == null) return;
        var state = ClientSkills.snapshot();
        boolean available = state != null && state.available();
        reset.active = available && !ClientSkills.pending() && SkillCatalog.SKILLS.stream().anyMatch(s -> state.rank(s.id()) > 0);
        skillsTab.active = tasks || jetpacks || perks;
        perksTab.active = !perks;
        jetpacksTab.active = !jetpacks;
        testPoints.active = available && !ClientSkills.pending() && state.canTestPoints();
        tasksTab.active = !tasks;
        catalog.refresh();
    }
    @Override public void tick() { refresh(); }
    @Override public boolean isPauseScreen() { return false; }
    @Override public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (ClientSkills.OPEN.matches(keyCode, scanCode)) { onClose(); return true; }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }
    @Override public void renderBackground(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        // The inherited Screen.render draws this background once, then all widgets.
        super.renderBackground(g, mouseX, mouseY, partialTick);
        g.fill(x, y, x + panelWidth, y + panelHeight, 0xFF314653);
        g.fill(x + 1, y + 1, x + panelWidth - 1, y + panelHeight - 1, 0xFF101D27);
        g.fill(x + 1, y + 1, x + panelWidth - 1, y + 3, CYAN);
        g.drawString(font, font.substrByWidth(title, panelWidth - 164), x + 12, y + 12, LIGHT, false);
        g.drawString(font, font.substrByWidth(label("overview"), panelWidth - 164), x + 12, y + 27, MUTED, false);
        g.fill(x + panelWidth - 140, y + 8, x + panelWidth - 12, y + 38, 0xFF203444);
        g.drawCenteredString(font, label("available_points"), x + panelWidth - 76, y + 11, MUTED);
        var state = ClientSkills.snapshot();
        Component points = state == null || !state.available() ? Component.literal("—") : label("balances", state.points(), state.perkPoints());
        g.drawCenteredString(font, points, x + panelWidth - 76, y + 24, 0xFFFFDA82);
        int activeTabX = x + 12 + (perks ? 3 : tasks ? 2 : jetpacks ? 1 : 0) * (2 + (panelWidth - 30) / 4);
        g.fill(activeTabX, y + 64, activeTabX + (panelWidth - 30) / 4, y + 66, CYAN);
        g.drawString(font, font.substrByWidth(perks ? label("perks_hint") : jetpacks ? label("jetpacks_hint", ClientSkills.jetpackStage()) : label(tasks ? "tasks_hint" : "skills_hint"), panelWidth - 24), x + 12, y + 71, MUTED, false);
        g.fill(x + 12, y + 82, x + panelWidth - 12, y + panelHeight - 48, 0xFF0B151D);
        String footer = state != null && !state.available() ? "storage_error"
                : state != null && !state.message().isEmpty() ? state.message()
                : jetpacks ? "jetpack_details_hint" : state != null && !state.local() ? "server_scope" : "local_scope";
        var lines = font.split(label(footer), panelWidth - 24);
        if (!lines.isEmpty()) g.drawString(font, lines.getFirst(), x + 12, y + panelHeight - 40,
                state != null && !state.available() ? 0xFFFFAE94 : MUTED, false);
    }

    private record Card(String id, String titleKey, String descriptionKey, ItemStack icon, int cost, int perkCost, int maxRank, boolean task, String prerequisite, Component description) { }

    private final class CatalogPanel extends ScrollPanel {
        private final List<Card> cards = new ArrayList<>();
        private final List<Button> actions = new ArrayList<>();
        private final List<Integer> offsets = new ArrayList<>(), heights = new ArrayList<>();
        private int contentHeight;
        CatalogPanel() {
            super(SkillScreen.this.minecraft, panelWidth - 24, panelHeight - 132, y + 84, x + 12, 0, 4,
                    0xFF1B2B38, CYAN, 0xFFA6EAF0);
            if (perks) cards.add(new Card("ascension_info", "gui.forever_skyblock.skills.perk_info_title",
                    "gui.forever_skyblock.skills.perk_info_description", new ItemStack(de.tekatz.foreverskyblock.ForeverSkyblock.FOREVER_CORE.get()),
                    1, 0, 0, true, "", label("perk_info_description")));
            if (tasks) for (var entry : MilestoneCatalog.MILESTONES)
                if (entry.perkCost() > 0) description.append("\n").append(label("perk_requirement", entry.perkCost()));
                cards.add(new Card(entry.id(), entry.titleKey(), entry.descriptionKey(), entry.icon().getDefaultInstance(), entry.reward(), 0, 0, true, "", Component.translatable(entry.descriptionKey())));
            else for (var entry : SkillCatalog.SKILLS) {
                if (perks ? entry.perkCost() == 0 : entry.jetpack() != jetpacks) continue;
                ItemStack icon = entry.icon().getDefaultInstance();
                var description = Component.translatable(entry.descriptionKey());
                if (entry.jetpack()) {
                    if (JetpackGate.installed()) {
                        var variants = IronJetpacksBridge.variants(JetpackResearch.IDS.indexOf(entry.id()) + 1);
                        if (!variants.isEmpty()) icon = variants.getFirst();
                        String names = variants.stream().map(v -> v.getHoverName().getString()).collect(java.util.stream.Collectors.joining(", "));
                        description.append("\n").append(label("jetpack_variants", names));
                    } else description.append("\n").append(label("missing_jetpacks"));
                    if (!entry.prerequisite().isEmpty()) description.append("\n").append(label("requires_skill",
                            Component.translatable(SkillCatalog.find(entry.prerequisite()).titleKey())));
                }
                if (entry.perkCost() > 0) description.append("\n").append(label("perk_requirement", entry.perkCost()));
                cards.add(new Card(entry.id(), entry.titleKey(), entry.descriptionKey(), icon, entry.cost(), entry.perkCost(), entry.maxRank(), false, entry.prerequisite(), description));
            }
            for (Card card : cards) {
                offsets.add(contentHeight);
                int titleHeight = Math.max(16, font.split(Component.translatable(card.titleKey()), cardWidth() - 48).size() * 10);
                int cardHeight = jetpacks ? 64 : 52 + titleHeight + font.split(card.description(), cardWidth() - 20).size() * 10;
                heights.add(cardHeight);
                contentHeight += cardHeight + 6;
                Button action = Button.builder(Component.empty(), b -> { ClientSkills.buy(card.id()); SkillScreen.this.refresh(); })
                        .bounds(left + cardWidth() - 124, top, 114, 20).build();
                if (jetpacks) action.setTooltip(net.minecraft.client.gui.components.Tooltip.create(card.description()));
                actions.add(action);
            }
            contentHeight = Math.max(height, contentHeight);
            positionButtons();
        }
        private int cardWidth() { return width - 8; }
        private void positionButtons() {
            for (int i = 0; i < actions.size(); i++) {
                Button button = actions.get(i);
                button.setY(top - (int) scrollDistance + offsets.get(i) + heights.get(i) - 28);
                button.visible = button.getY() + 20 > top && button.getY() < bottom;
            }
        }
        void refresh() {
            var state = ClientSkills.snapshot();
            boolean available = state != null && state.available();
            for (int i = 0; i < cards.size(); i++) {
                Card card = cards.get(i);
                Button button = actions.get(i);
                boolean complete = available && (card.task() ? state.milestones().contains(card.id()) : state.rank(card.id()) >= card.maxRank());
                boolean prerequisite = card.prerequisite().isEmpty() || available && state.rank(card.prerequisite()) > 0;
                boolean installed = !JetpackResearch.IDS.contains(card.id()) || JetpackGate.installed();
                String key = card.task() ? complete ? "completed" : "task_open" : complete ? "unlocked"
                        : !available || !installed ? "unavailable" : !prerequisite ? "requires_previous" : ClientSkills.pending() ? "waiting"
                        : state.perkPoints() < card.perkCost() ? "need_perks" : state.points() < card.cost() ? "need_points" : card.perkCost() > 0 ? "unlock_short" : "unlock";
                button.setMessage(card.id().equals("ascension_info") ? label("machine_required") : label(key, key.equals("need_perks") ? card.perkCost() : card.cost()));
                button.active = !card.task() && available && !complete && prerequisite && installed && !ClientSkills.pending() && state.points() >= card.cost() && state.perkPoints() >= card.perkCost();
            }
        }
        @Override protected int getContentHeight() { return contentHeight; }
        @Override protected void drawBackground(GuiGraphics g, Tesselator tess, float partialTick) { }
        @Override protected void drawPanel(GuiGraphics g, int entryRight, int baseY, Tesselator tess, int mouseX, int mouseY) {
            positionButtons();
            var state = ClientSkills.snapshot();
            for (int i = 0; i < cards.size(); i++) {
                Card card = cards.get(i);
                int cardY = baseY + offsets.get(i), cardHeight = heights.get(i), cardWidth = cardWidth();
                if (cardY >= bottom || cardY + cardHeight <= top) continue;
                boolean complete = state != null && state.available() && (card.task() ? state.milestones().contains(card.id()) : state.rank(card.id()) >= card.maxRank());
                g.fill(left, cardY, left + cardWidth, cardY + cardHeight, 0xFF2C4352);
                g.fill(left + 1, cardY + 1, left + cardWidth - 1, cardY + cardHeight - 1, complete ? 0xFF152E2A : 0xFF1A2A38);
                g.fill(left, cardY, left + 3, cardY + cardHeight, complete ? GREEN : CYAN);
                if (jetpacks) {
                    g.renderItem(card.icon(), left + 10, cardY + 13);
                    g.drawString(font, Component.translatable(card.titleKey()), left + 34, cardY + 8, LIGHT, false);
                    int stage = JetpackResearch.IDS.indexOf(card.id()) + 1;
                    g.drawString(font, label("jetpack_summary_" + stage, card.cost(), card.perkCost()), left + 34, cardY + 24, complete ? GREEN : MUTED, false);
                    actions.get(i).render(g, mouseX, mouseY, 0);
                    continue;
                }
                g.renderItem(card.icon(), left + 10, cardY + 8);
                int titleY = cardY + 8;
                var titles = font.split(Component.translatable(card.titleKey()), cardWidth - 48);
                for (var line : titles) { g.drawString(font, line, left + 34, titleY, LIGHT, false); titleY += 10; }
                int descriptionY = cardY + 14 + Math.max(16, titles.size() * 10);
                for (var line : font.split(card.description(), cardWidth - 20)) {
                    g.drawString(font, line, left + 10, descriptionY, MUTED, false); descriptionY += 10;
                }
                Component status = card.id().equals("ascension_info") ? label("core_conversion") : card.perkCost() > 0 ? label("dual_cost", card.cost(), card.perkCost()) : card.task() ? label("reward", card.cost()) : (JetpackResearch.IDS.contains(card.id()) ? label("cost", card.cost()) : label("rank", state == null ? 0 : state.rank(card.id()), card.maxRank()));
                g.drawString(font, status, left + 10, cardY + cardHeight - 22, complete ? GREEN : CYAN, false);
                actions.get(i).render(g, mouseX, mouseY, 0);
            }
            // ScrollPanel uses a raw GL scissor. Flush buffered GUI text before it disables clipping.
            g.flush();
        }
        @Override public boolean mouseClicked(double mouseX, double mouseY, int button) {
            return isMouseOver(mouseX, mouseY) && super.mouseClicked(mouseX, mouseY, button);
        }
        @Override public boolean mouseScrolled(double mouseX, double mouseY, double horizontal, double vertical) {
            if (!isMouseOver(mouseX, mouseY)) return false;
            boolean handled = super.mouseScrolled(mouseX, mouseY, horizontal, vertical);
            positionButtons();
            return handled;
        }
        @Override public void setFocused(GuiEventListener listener) {
            super.setFocused(listener);
            int index = actions.indexOf(listener);
            if (index >= 0) {
                int buttonBottom = offsets.get(index) + heights.get(index) - 8;
                if (buttonBottom - scrollDistance > height) scrollDistance = buttonBottom - height;
                if (buttonBottom - 20 - scrollDistance < 0) scrollDistance = Math.max(0, buttonBottom - 20);
                positionButtons();
            }
        }
        @Override public List<? extends GuiEventListener> children() { return actions; }
        @Override public NarratableEntry.NarrationPriority narrationPriority() {
            return getFocused() != null ? NarratableEntry.NarrationPriority.FOCUSED : NarratableEntry.NarrationPriority.NONE;
        }
        @Override public void updateNarration(NarrationElementOutput output) {
            output.add(NarratedElementType.TITLE, label(perks ? "perks_tab" : tasks ? "tasks_short" : jetpacks ? "jetpacks_tab" : "skills_short"));
            for (int i = 0; i < actions.size(); i++) if (actions.get(i).isFocused()) {
                Card card = cards.get(i);
                output.add(NarratedElementType.USAGE, Component.translatable(card.titleKey()).append(". ")
                        .append(card.description()).append(". ").append(actions.get(i).getMessage()));
            }
        }
    }
}

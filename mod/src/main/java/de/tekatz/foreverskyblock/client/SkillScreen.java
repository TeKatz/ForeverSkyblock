package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.Tooltip;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.Item;

/** Catalog-backed rows and scrolling allow more skills/milestones without changing the layout. */
public final class SkillScreen extends Screen {
    private static final int CYAN = 0xFF74D5DD, LIGHT = 0xFFE0E9ED, MUTED = 0xFFA3B3BE;
    private int x, y, panelWidth, panelHeight, leftWidth, scroll, descriptionScroll;
    private boolean milestones;
    private String selected = SkillCatalog.SKILLS.getFirst().id();
    private final List<Button> rows = new ArrayList<>();
    private Button purchase;
    public SkillScreen() { super(Component.translatable("gui.forever_skyblock.skills.title")); }
    @Override protected void init() {
        panelWidth = Math.min(520, width - 16);
        panelHeight = Math.min(324, height - 16);
        x = (width - panelWidth) / 2;
        y = (height - panelHeight) / 2;
        leftWidth = Math.min(176, Math.max(118, panelWidth * 2 / 5));
        clearWidgets(); rows.clear();
        addRenderableWidget(Button.builder(Component.translatable("gui.forever_skyblock.skills.skills"), button -> selectTab(false))
                .bounds(x + 12, y + 49, (leftWidth - 16) / 2, 20).build());
        addRenderableWidget(Button.builder(Component.translatable("gui.forever_skyblock.skills.milestones"), button -> selectTab(true))
                .bounds(x + 14 + (leftWidth - 16) / 2, y + 49, (leftWidth - 16) / 2, 20).build());
        int count = milestones ? MilestoneCatalog.MILESTONES.size() : SkillCatalog.SKILLS.size();
        int visible = Math.max(1, (panelHeight - 125) / 32);
        scroll = Math.max(0, Math.min(scroll, Math.max(0, count - visible)));
        for (int row = 0; row < visible && row + scroll < count; row++) {
            String id = milestones ? MilestoneCatalog.MILESTONES.get(row + scroll).id() : SkillCatalog.SKILLS.get(row + scroll).id();
            String key = milestones ? MilestoneCatalog.MILESTONES.get(row + scroll).titleKey() : SkillCatalog.SKILLS.get(row + scroll).titleKey();
            Button button = Button.builder(Component.translatable(key), clicked -> { selected = id; descriptionScroll = 0; refresh(); })
                    .bounds(x + 12, y + 78 + row * 32, leftWidth - 4, 26).build();
            rows.add(addRenderableWidget(button));
        }
        purchase = addRenderableWidget(Button.builder(Component.empty(), button -> {
            if (!milestones) { ClientSkills.buy(selected); refresh(); }
        }).bounds(x + leftWidth + 24, y + panelHeight - 62, panelWidth - leftWidth - 36, 20).build());
        addRenderableWidget(Button.builder(Component.translatable("gui.done"), button -> onClose())
                .bounds(x + panelWidth - 74, y + panelHeight - 28, 62, 18).build());
        refresh();
    }
    private void selectTab(boolean value) {
        milestones = value;
        scroll = 0;
        descriptionScroll = 0;
        selected = value ? MilestoneCatalog.MILESTONES.getFirst().id() : SkillCatalog.SKILLS.getFirst().id();
        init();
    }
    public void refresh() {
        if (purchase == null) return;
        var state = ClientSkills.snapshot();
        boolean available = state != null && state.available();
        if (milestones) {
            boolean complete = available && state.milestones().contains(selected);
            purchase.setMessage(Component.translatable("gui.forever_skyblock.skills." + (complete ? "completed" : "not_reached")));
            purchase.active = false;
        } else {
            var skill = SkillCatalog.find(selected);
            boolean owned = available && state.rank(selected) >= skill.maxRank();
            purchase.setMessage(Component.translatable("gui.forever_skyblock.skills." + (owned ? "unlocked" : "unlock"), skill.cost()));
            purchase.active = available && !owned && !ClientSkills.pending() && state.points() >= skill.cost();
            purchase.setTooltip(Tooltip.create(Component.translatable(owned ? "gui.forever_skyblock.skills.already_owned"
                    : !available ? "gui.forever_skyblock.skills.storage_error"
                    : state.points() < skill.cost() ? "gui.forever_skyblock.skills.not_enough_points"
                    : "gui.forever_skyblock.skills.purchase_hint")));
        }
    }
    @Override public void tick() { refresh(); }
    @Override public boolean isPauseScreen() { return false; }
    @Override public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (ClientSkills.OPEN.matches(keyCode, scanCode)) { onClose(); return true; }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }
    @Override public boolean mouseScrolled(double mouseX, double mouseY, double horizontal, double vertical) {
        if (vertical == 0) return super.mouseScrolled(mouseX, mouseY, horizontal, vertical);
        if (mouseX >= x + leftWidth + 20 && mouseX < x + panelWidth - 12
                && mouseY >= y + 89 && mouseY < y + panelHeight - 96) {
            descriptionScroll += vertical < 0 ? 1 : -1;
            return true;
        }
        if (mouseX >= x + 8 && mouseX < x + leftWidth + 12 && mouseY >= y + 75 && mouseY < y + panelHeight - 40) {
            scroll += vertical < 0 ? 1 : -1;
            init();
            return true;
        }
        return super.mouseScrolled(mouseX, mouseY, horizontal, vertical);
    }
    @Override public void renderBackground(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        // Screen.render calls this before rendering widgets. Blur only the world, then draw the panel.
        super.renderBackground(g, mouseX, mouseY, partialTick);
        g.fill(x, y, x + panelWidth, y + panelHeight, 0xFF0D171F);
        g.fill(x + 1, y + 1, x + panelWidth - 1, y + panelHeight - 1, 0xFF344753);
        g.fill(x + 3, y + 3, x + panelWidth - 3, y + panelHeight - 3, 0xFF182630);
        g.fill(x + 12, y + 41, x + panelWidth - 12, y + 42, 0xFF3B5866);
        g.drawString(font, title, x + 12, y + 12, LIGHT, false);
        var state = ClientSkills.snapshot();
        Component points = Component.translatable("gui.forever_skyblock.skills.points", state == null ? 0 : state.points());
        g.drawString(font, points, x + panelWidth - 12 - font.width(points), y + 13, 0xFFF2CC77, false);
        g.drawString(font, Component.translatable("gui.forever_skyblock.skills.subtitle"), x + 12, y + 28, MUTED, false);
        int detailX = x + leftWidth + 20, detailWidth = panelWidth - leftWidth - 32;
        g.fill(detailX - 4, y + 49, x + panelWidth - 12, y + panelHeight - 69, 0xFF111D26);
        Item icon;
        String titleKey, descriptionKey;
        Component reward;
        if (milestones) {
            var milestone = MilestoneCatalog.MILESTONES.stream().filter(value -> value.id().equals(selected)).findFirst().orElseThrow();
            icon = milestone.icon(); titleKey = milestone.titleKey(); descriptionKey = milestone.descriptionKey();
            reward = Component.translatable("gui.forever_skyblock.skills.reward", milestone.reward());
        } else {
            var skill = SkillCatalog.find(selected);
            icon = skill.icon(); titleKey = skill.titleKey(); descriptionKey = skill.descriptionKey();
            reward = Component.translatable("gui.forever_skyblock.skills.rank", state == null ? 0 : state.rank(selected), skill.maxRank());
        }
        g.renderItem(icon.getDefaultInstance(), detailX + 2, y + 57);
        text(g, Component.translatable(titleKey), detailX + 24, y + 55, detailWidth - 28, LIGHT, 2);
        var description = font.split(Component.translatable(descriptionKey), Math.max(20, detailWidth - 14));
        int visibleLines = Math.max(1, (panelHeight - 185) / 10);
        int maxScroll = Math.max(0, description.size() - visibleLines);
        descriptionScroll = Math.max(0, Math.min(descriptionScroll, maxScroll));
        for (int i = 0; i < visibleLines && i + descriptionScroll < description.size(); i++) {
            g.drawString(font, description.get(i + descriptionScroll), detailX + 2, y + 89 + i * 10, MUTED, false);
        }
        if (maxScroll > 0) {
            int trackX = detailX + detailWidth - 5, trackY = y + 89, trackHeight = visibleLines * 10;
            int thumbHeight = Math.max(6, trackHeight * visibleLines / description.size());
            int thumbY = trackY + (trackHeight - thumbHeight) * descriptionScroll / maxScroll;
            g.fill(trackX, trackY, trackX + 2, trackY + trackHeight, 0xFF344753);
            g.fill(trackX, thumbY, trackX + 2, thumbY + thumbHeight, CYAN);
        }
        g.drawString(font, reward, detailX + 2, y + panelHeight - 85, CYAN, false);
        String scope = state != null && !state.local() ? "server_scope" : "local_scope";
        text(g, Component.translatable("gui.forever_skyblock.skills." + scope), x + 12, y + panelHeight - 28,
                panelWidth - 100, MUTED, 2);
        if (state != null && !state.message().isEmpty()) {
            text(g, Component.translatable("gui.forever_skyblock.skills." + state.message()), detailX + 2,
                    y + panelHeight - 39, detailWidth - 6, state.available() ? CYAN : 0xFFFFAC92, 1);
        }
        for (int i = 0; i < rows.size(); i++) {
            int index = scroll + i;
            String id = milestones ? MilestoneCatalog.MILESTONES.get(index).id() : SkillCatalog.SKILLS.get(index).id();
            boolean owned = state != null && (milestones ? state.milestones().contains(id) : state.rank(id) > 0);
            if (id.equals(selected)) g.fill(x + 8, rows.get(i).getY(), x + 10, rows.get(i).getY() + 26, CYAN);
            if (owned) g.fill(x + leftWidth + 9, rows.get(i).getY() + 10, x + leftWidth + 12, rows.get(i).getY() + 16, 0xFF85D992);
        }
    }
    private void text(GuiGraphics g, Component text, int x, int y, int width, int color, int maxLines) {
        var lines = font.split(text, Math.max(20, width));
        for (int i = 0; i < Math.min(lines.size(), maxLines); i++) g.drawString(font, lines.get(i), x, y + i * 10, color, false);
    }
}

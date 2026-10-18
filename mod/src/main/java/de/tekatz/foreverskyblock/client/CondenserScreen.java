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
    private MachineButton filters, recipes;

    public CondenserScreen(CondenserMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title, MachineLayout.CONDENSER, false, CondenserBlockEntity.MACHINE_SLOTS);
    }

    private static Component text(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.machine." + key, args);
    }

    @Override protected void init() {
        super.init();
        filters = addRenderableWidget(new MachineButton(leftPos + 20, topPos + 162, 120, 20, text("edit_locks"), skin, true,
                b -> minecraft.gameMode.handleInventoryButtonClick(menu.containerId, 0)));
        filters.setTooltip(Tooltip.create(Component.translatable("gui.forever_skyblock.filter_help")));
        recipes = addRenderableWidget(new MachineButton(leftPos + 168, topPos + 162, 60, 20, text("recipes"), skin, false,
                b -> { if (menu.hasGardenChamber()) ClientMachineRecipes.garden(); else ClientMachineRecipes.condenser(); }));
    }

    @Override protected void containerTick() {
        super.containerTick();
        boolean garden = menu.hasGardenChamber();
        filters.visible = !garden;
        filters.active = !garden;
        filters.setMessage(text(menu.editing() ? "finish_locks" : "edit_locks"));
        recipes.setX(leftPos + (garden ? 94 : 168));
        recipes.setY(topPos + (garden ? 151 : 162));
        recipes.active = ClientMachineRecipes.available();
    }

    public Rect2i inputArea(int slot) {
        Slot menuSlot = menu.getSlot(slot);
        return screenArea(menuSlot.x - 4, menuSlot.y - 4, 24, 24);
    }

    public void setGhost(int slot, ItemStack stack) {
        if (menu.editing() && !menu.hasGardenChamber() && menu.acceptsGhost(slot, stack))
            PacketDistributor.sendToServer(new CondenserNetwork.Filter(menu.containerId, slot, BuiltInRegistries.ITEM.getKey(stack.getItem())));
    }

    @Override protected void slotClicked(Slot slot, int slotId, int button, ClickType type) {
        if (!menu.hasGardenChamber() && menu.editing() && CondenserBlockEntity.isInput(slotId)) {
            if (button == 1) setGhost(slotId, ItemStack.EMPTY);
            else if (!menu.getCarried().isEmpty()) setGhost(slotId, menu.getCarried());
            return;
        }
        super.slotClicked(slot, slotId, button, type);
    }

    @Override public boolean mouseReleased(double x, double y, int button) {
        if (!menu.hasGardenChamber() && menu.editing() && button == 0 && !menu.getCarried().isEmpty()) {
            for (int slot : new int[]{CondenserBlockEntity.INPUT, CondenserBlockEntity.SECOND_INPUT, CondenserBlockEntity.FREEZING_INPUT})
                if (inputArea(slot).contains((int) x, (int) y) && menu.getSlot(slot).isActive()) {
                    setGhost(slot, menu.getCarried());
                    super.mouseReleased(x, y, button);
                    return true;
                }
        }
        return super.mouseReleased(x, y, button);
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        skin.frame(g, x, y, imageWidth, imageHeight);
        g.renderItem(new ItemStack(menu.machineTier() >= 2 ? ForeverSkyblock.CONDENSER_MARK_II.get() : ForeverSkyblock.CONDENSER_MARK_I.get()), x + 18, y + 10);
        MachineSkin.text(g, font, text(menu.machineTier() >= 2 ? "condenser_title_ii" : "condenser_title"), x + 42, y + 12, 126, MachineSkin.LIGHT, 1.1F);

        int visibleStatus = menu.hasGardenChamber() ? menu.gardenStatus() : menu.status();
        int color = visibleStatus == CondenserBlockEntity.BLOCKED ? 0xFFFFBF76
                : menu.hasGardenChamber() ? 0xFF76D982 : MachineSkin.CYAN;
        g.fill(x + 177, y + 12, x + 181, y + 16, color);
        String statusKey = visibleStatus == CondenserBlockEntity.RUNNING ? (menu.hasGardenChamber() ? "growing" : "processing")
                : visibleStatus == CondenserBlockEntity.BLOCKED ? (menu.hasGardenChamber() ? "harvest_full" : "blocked")
                : menu.hasGardenChamber() ? "garden_ready" : "idle";
        MachineSkin.text(g, font, text(statusKey), x + 186, y + 12, 43, color, .73F);

        if (menu.hasGardenChamber()) {
            MachineSkin.panel(g, x + 42, y + 31, 164, 12, 0xFF315B45);
            MachineSkin.centered(g, font, text("garden_connected"), x + 124, y + 34, 156, 0xFF8FE59B, .72F);
            renderGarden(g, x, y);
            MachineSkin.panel(g, x + 18, y + 183, 212, 16, 0xFF466252);
            MachineSkin.centered(g, font, text("hint_garden"), x + 124, y + 188, 204, 0xFFB4D1BC, .65F);
            inventory(g);
            return;
        }

        MachineSkin.panel(g, x + 42, y + 31, 164, 12, 0xFF365A66);
        String stateKey = switch (menu.moduleState()) {
            case 1 -> "state_mixing";
            case 2 -> "state_freezing";
            case 3 -> "state_full";
            default -> "state_base";
        };
        MachineSkin.centered(g, font, text(stateKey), x + 124, y + 34, 156, MachineSkin.CYAN, .72F);

        MachineSkin.panel(g, x + 14, y + 44, 220, 64, 0xFF54616D);
        renderCondensing(g, x, y);

        MachineSkin.panel(g, x + 14, y + 113, 220, 43, 0xFF465A68);
        if (menu.hasFreezingChamber()) renderFreezing(g, x, y);
        else MachineSkin.centered(g, font, text("freezing_hint"), x + 124, y + 129, 202, MachineSkin.MUTED, .67F);

        renderLocks(g, x, y);
        MachineSkin.panel(g, x + 18, y + 187, 212, 12, 0xFF5D6875);
        MachineSkin.centered(g, font, text(moduleHintKey()), x + 124, y + 190, 204, MachineSkin.MUTED, .67F);
        inventory(g);
    }

    private void renderGarden(GuiGraphics g, int x, int y) {
        MachineSkin.panel(g, x + 14, y + 44, 220, 134, 0xFF335544);

        int left = x + 20, right = x + 228, top = y + 50, ground = y + 112;
        // Layered sky and distant landscape; deliberately pixel-art rather than flat rectangles.
        g.fill(left, top, right, y + 68, 0xFF274B56);
        g.fill(left, y + 68, right, y + 88, 0xFF35636A);
        g.fill(left, y + 88, right, y + 100, 0xFF487263);
        g.fill(left, y + 96, x + 72, y + 105, 0xFF355F48);
        g.fill(x + 58, y + 91, x + 124, y + 105, 0xFF3B684C);
        g.fill(x + 112, y + 94, right, y + 105, 0xFF315B45);
        g.fill(left, y + 103, right, ground, 0xFF47763D);
        g.fill(left, ground, right, y + 117, 0xFF624830);
        g.fill(left, y + 103, right, y + 106, 0xFF6B9A4D);

        // Sun with a soft blocky halo.
        g.fill(x + 199, y + 56, x + 215, y + 72, 0x556F9B72);
        g.fill(x + 202, y + 59, x + 212, y + 69, 0xFFE8CF72);
        g.fill(x + 204, y + 61, x + 210, y + 67, 0xFFFFE29A);

        long now = System.currentTimeMillis();
        int cloud = (int) ((now / 95L) % 180L);
        drawCloud(g, x + 24 + cloud, y + 61, left, right);
        drawCloud(g, x + 172 - (cloud / 2), y + 76, left, right);

        ItemStack specimen = menu.getSlot(CondenserBlockEntity.GARDEN_SPECIMEN).getItem();
        if (!specimen.isEmpty()) {
            float ratio = Math.min(1F, menu.gardenProgress() / (float) menu.gardenDuration());
            renderGardenTree(g, x, y, ratio, now);
        } else {
            renderGardenTree(g, x, y, 0.05F, now);
            MachineSkin.centered(g, font, text("garden_insert"), x + 124, y + 75, 166, 0xFFE1F1E4, .72F);
        }

        int barX = x + 66, barW = 116;
        MachineSkin.panel(g, barX, y + 122, barW, 9, 0xFF456250);
        int filled = (barW - 4) * menu.gardenProgress() / menu.gardenDuration();
        g.fill(barX + 2, y + 124, barX + 2 + filled, y + 129, 0xFF62C46B);
        MachineSkin.centered(g, font, Component.literal(String.format(java.util.Locale.ROOT, "%.1f / %.1f s",
                menu.gardenProgress() / 20F, menu.gardenDuration() / 20F)), barX + barW / 2, y + 134, barW + 10, 0xFFCAE0CE, .68F);

        MachineSkin.centered(g, font, text("specimen"), x + CondenserMenu.GARDEN_SPECIMEN_X + 8, y + 143, 62, 0xFFBFD6C4, .69F);
        MachineSkin.centered(g, font, text("harvest"), x + 190, y + 143, 75, 0xFFBFD6C4, .69F);
        skin.slot(g, x + CondenserMenu.GARDEN_SPECIMEN_X, y + CondenserMenu.GARDEN_Y, true);
        skin.slot(g, x + CondenserMenu.GARDEN_PRIMARY_X, y + CondenserMenu.GARDEN_Y, true);
        skin.slot(g, x + CondenserMenu.GARDEN_SECONDARY_X, y + CondenserMenu.GARDEN_Y, true);
    }

    private void drawCloud(GuiGraphics g, int cx, int cy, int left, int right) {
        int width = 28;
        int x = cx;
        while (x > right) x -= 196;
        while (x + width < left) x += 196;
        g.fill(x + 4, cy, x + 17, cy + 3, 0x99DCE8E7);
        g.fill(x, cy + 3, x + 24, cy + 7, 0x99DCE8E7);
        g.fill(x + 8, cy - 2, x + 14, cy + 5, 0xB5EFF5F3);
    }

    private void renderGardenTree(GuiGraphics g, int x, int y, float ratio, long now) {
        float eased = ratio * ratio * (3F - 2F * ratio);
        int baseY = y + 104;
        int height = 5 + Math.round(31F * eased);
        int topY = baseY - height;
        int trunkWidth = ratio < .28F ? 2 : ratio < .58F ? 3 : 4;
        int trunkLeft = x + 124 - trunkWidth / 2;
        g.fill(trunkLeft, topY + 7, trunkLeft + trunkWidth, baseY, 0xFF68452B);
        if (ratio > .35F) {
            g.fill(x + 116, topY + 13, x + 124, topY + 15, 0xFF68452B);
            g.fill(x + 124, topY + 17, x + 133, topY + 19, 0xFF765033);
        }
        if (ratio < .12F) {
            g.fill(x + 122, baseY - 7, x + 125, baseY - 4, 0xFF5D9A4B);
            g.fill(x + 125, baseY - 9, x + 128, baseY - 6, 0xFF73B65A);
            return;
        }

        int r = 4 + Math.round(14F * eased);
        int cy = topY + 8;
        int shadow = 0xFF275E36, leaf = 0xFF347744, light = 0xFF4C9250, highlight = 0xFF65A95A;
        g.fill(x + 124 - r, cy - r / 2, x + 124 + r, cy + r / 2 + 6, shadow);
        g.fill(x + 116 - r / 2, cy + 1, x + 125, cy + r / 2 + 10, leaf);
        g.fill(x + 123, cy - 4, x + 132 + r / 2, cy + r / 2 + 8, leaf);
        g.fill(x + 121 - r / 2, cy - r / 2 - 3, x + 129 + r / 2, cy + 4, light);
        g.fill(x + 111 + r / 3, cy + 1, x + 119 + r / 2, cy + 7, light);
        g.fill(x + 128, cy, x + 135 + r / 3, cy + 6, highlight);
        if (ratio > .72F) {
            int shimmer = (int) ((now / 180L) % 3L);
            g.fill(x + 116 + shimmer * 5, cy - 5 + shimmer * 2, x + 118 + shimmer * 5, cy - 3 + shimmer * 2, 0xFFE4E89A);
        }
    }

    private void renderCondensing(GuiGraphics g, int x, int y) {
        MachineSkin.centered(g, font, text("input"), x + CondenserMenu.MAIN_INPUT_X + 8, y + 48, 43, MachineSkin.MUTED, .73F);
        if (menu.hasMixingChamber())
            MachineSkin.centered(g, font, text("module_input"), x + CondenserMenu.MIXING_INPUT_X + 8, y + 48, 49, MachineSkin.MUTED, .73F);
        MachineSkin.centered(g, font, text("output"), x + CondenserMenu.MAIN_OUTPUT_X + 8, y + 48, 41, MachineSkin.MUTED, .73F);
        skin.slot(g, x + CondenserMenu.MAIN_INPUT_X, y + CondenserMenu.MAIN_Y, true);
        if (menu.hasMixingChamber()) {
            skin.slot(g, x + CondenserMenu.MIXING_INPUT_X, y + CondenserMenu.MAIN_Y, true);
            MachineSkin.text(g, font, Component.literal("+"), x + 59, y + 66, 13, MachineSkin.MUTED, 1.25F);
        }
        skin.slot(g, x + CondenserMenu.MAIN_OUTPUT_X, y + CondenserMenu.MAIN_Y, true);

        int barX = x + (menu.hasMixingChamber() ? 111 : 83), barW = menu.hasMixingChamber() ? 70 : 98;
        MachineSkin.panel(g, barX, y + 66, barW, 8, 0xFF637888);
        int filled = Math.min(barW - 4, (barW - 4) * menu.progress() / menu.duration());
        g.fill(barX + 2, y + 68, barX + 2 + filled, y + 72, MachineSkin.CYAN);
        g.fill(barX + barW, y + 65, barX + barW + 3, y + 75, MachineSkin.CYAN);
        MachineSkin.centered(g, font, Component.literal(String.format(java.util.Locale.ROOT, "%.1f / %.1f s",
                menu.progress() / 20F, menu.duration() / 20F)), barX + barW / 2, y + 79, barW + 4, MachineSkin.MUTED, .7F);

        Component summary = text(menu.machineTier() >= 2 ? "mk2_recipe" : "wood_recipe");
        if (minecraft.level != null) {
            var input = new CondenserInput(menu.getSlot(CondenserBlockEntity.INPUT).getItem(),
                    menu.hasMixingChamber() ? menu.getSlot(CondenserBlockEntity.SECOND_INPUT).getItem() : ItemStack.EMPTY);
            var recipe = CondenserBlockEntity.findMatchingRecipe(minecraft.level, input, menu.machineTier());
            if (recipe != null) summary = Component.literal(menu.getSlot(CondenserBlockEntity.INPUT).getItem().getHoverName().getString()
                    + (recipe.value().requiresModule() ? " + " + menu.getSlot(CondenserBlockEntity.SECOND_INPUT).getItem().getHoverName().getString() : "")
                    + " → " + recipe.value().result().getHoverName().getString());
        }
        MachineSkin.text(g, font, summary, x + 20, y + 95, 207, MachineSkin.LIGHT, .84F);
    }

    private void renderFreezing(GuiGraphics g, int x, int y) {
        MachineSkin.centered(g, font, text("freezing"), x + 124, y + 116, 190, MachineSkin.CYAN, .72F);
        skin.slot(g, x + CondenserMenu.FREEZING_INPUT_X, y + CondenserMenu.FREEZING_Y, true);
        skin.slot(g, x + CondenserMenu.FREEZING_OUTPUT_X, y + CondenserMenu.FREEZING_Y, true);
        int barX = x + 62, barW = 124;
        MachineSkin.panel(g, barX, y + 132, barW, 8, 0xFF4E6879);
        int filled = (barW - 4) * menu.freezingProgress() / CondenserBlockEntity.FREEZING_REQUIRED_ITEMS;
        g.fill(barX + 2, y + 134, barX + 2 + filled, y + 138, MachineSkin.CYAN);
        int freezeColor = menu.freezingStatus() == CondenserBlockEntity.BLOCKED ? 0xFFFFBF76 : MachineSkin.LIGHT;
        MachineSkin.centered(g, font, Component.literal(menu.freezingProgress() + " / " + CondenserBlockEntity.FREEZING_REQUIRED_ITEMS),
                barX + barW / 2, y + 143, barW - 6, freezeColor, .7F);
    }

    private void renderLocks(GuiGraphics g, int x, int y) {
        for (int slot : new int[]{CondenserBlockEntity.INPUT, CondenserBlockEntity.SECOND_INPUT, CondenserBlockEntity.FREEZING_INPUT}) {
            if (!menu.getSlot(slot).isActive()) continue;
            int sx = menu.getSlot(slot).x, sy = menu.getSlot(slot).y;
            ItemStack filter = menu.filter(slot);
            if (!filter.isEmpty()) {
                skin.lock(g, x + sx + 12, y + sy - 9);
                if (menu.getSlot(slot).getItem().isEmpty()) {
                    g.renderItem(filter, x + sx, y + sy);
                    g.fill(x + sx, y + sy, x + sx + 16, y + sy + 16, 0x88404C55);
                }
            } else if (menu.editing()) {
                g.fill(x + sx - 3, y + sy + 21, x + sx + 20, y + sy + 22, MachineSkin.CYAN);
            }
        }
    }

    private String moduleHintKey() {
        return switch (menu.moduleState()) {
            case 1 -> "hint_mixing";
            case 2 -> "hint_freezing";
            case 3 -> "hint_full";
            default -> "hint_base";
        };
    }
}

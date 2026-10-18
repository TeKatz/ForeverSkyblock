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
            MachineSkin.panel(g, x + 18, y + 187, 212, 12, 0xFF466252);
            MachineSkin.centered(g, font, text("hint_garden"), x + 124, y + 190, 204, 0xFFB4D1BC, .67F);
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

        // Keep timed condensing and material freezing as visually separate process cards.
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
        MachineSkin.panel(g, x + 14, y + 44, 220, 112, 0xFF335544);
        // A compact artificial landscape makes Garden mode immediately distinct from normal condensing.
        g.fill(x + 20, y + 50, x + 228, y + 105, 0xFF496D6B);
        g.fill(x + 20, y + 86, x + 228, y + 105, 0xFF3F6A3E);
        g.fill(x + 20, y + 101, x + 228, y + 105, 0xFF5B4932);
        g.fill(x + 204, y + 57, x + 214, y + 67, 0xFFE8D56C);

        ItemStack specimen = menu.getSlot(CondenserBlockEntity.GARDEN_SPECIMEN).getItem();
        if (!specimen.isEmpty()) {
            float ratio = Math.min(1F, menu.gardenProgress() / (float) menu.gardenDuration());
            int trunkHeight = 6 + Math.round(23 * ratio);
            int trunkTop = y + 99 - trunkHeight;
            g.fill(x + 121, trunkTop, x + 127, y + 100, 0xFF735033);
            if (ratio > .16F) {
                int crown = 5 + Math.round(13 * ratio);
                g.fill(x + 124 - crown, trunkTop - crown / 2, x + 124 + crown, trunkTop + crown / 2 + 7, 0xFF3F7A43);
                g.fill(x + 118 - crown / 2, trunkTop + 1, x + 130 + crown / 2, trunkTop + 10, 0xFF4B8D4D);
            }
        } else {
            MachineSkin.centered(g, font, text("garden_insert"), x + 124, y + 72, 176, 0xFFC5DCC9, .72F);
        }

        int barX = x + 69, barW = 110;
        MachineSkin.panel(g, barX, y + 110, barW, 8, 0xFF456250);
        int filled = (barW - 4) * menu.gardenProgress() / menu.gardenDuration();
        g.fill(barX + 2, y + 112, barX + 2 + filled, y + 116, 0xFF62C46B);
        MachineSkin.centered(g, font, Component.literal(String.format(java.util.Locale.ROOT, "%.1f / %.1f s",
                menu.gardenProgress() / 20F, menu.gardenDuration() / 20F)), barX + barW / 2, y + 120, barW + 8, 0xFFBFD6C4, .68F);

        MachineSkin.centered(g, font, text("specimen"), x + CondenserMenu.GARDEN_SPECIMEN_X + 8, y + 128, 55, 0xFFBFD6C4, .69F);
        MachineSkin.centered(g, font, text("harvest"), x + 190, y + 128, 75, 0xFFBFD6C4, .69F);
        skin.slot(g, x + CondenserMenu.GARDEN_SPECIMEN_X, y + CondenserMenu.GARDEN_Y, true);
        skin.slot(g, x + CondenserMenu.GARDEN_PRIMARY_X, y + CondenserMenu.GARDEN_Y, true);
        skin.slot(g, x + CondenserMenu.GARDEN_SECONDARY_X, y + CondenserMenu.GARDEN_Y, true);
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

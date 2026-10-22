package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import de.tekatz.foreverskyblock.menu.MachineLayout;
import de.tekatz.foreverskyblock.menu.WashingMenu;
import de.tekatz.foreverskyblock.recipe.WashingInput;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.item.ItemStack;

public final class WashingScreen extends MachineScreen<WashingMenu> {
    private MachineButton recipes;

    public WashingScreen(WashingMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title, MachineLayout.WASHING, false, WashingBasinBlockEntity.MACHINE_SLOTS);
    }

    private static Component text(String key, Object... args) {
        return Component.translatable("gui.forever_skyblock.machine." + key, args);
    }

    @Override protected void init() {
        super.init();
        recipes = addRenderableWidget(new MachineButton(leftPos + 164, topPos + 145, 64, 22, text("recipes"), skin, false,
                b -> ClientMachineRecipes.washing()));
    }

    @Override protected void containerTick() {
        super.containerTick();
        recipes.active = ClientMachineRecipes.available();
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        skin.accent(MachineSkin.ICE);
        skin.frame(g, x, y, imageWidth, imageHeight);
        g.renderItem(new ItemStack(ForeverSkyblock.WASHING_BASIN.get()), x + 18, y + 10);
        MachineSkin.text(g, font, text("washing_title"), x + 42, y + 12, 130, MachineSkin.LIGHT, 1.05F);

        int statusColor = switch (menu.status()) {
            case WashingBasinBlockEntity.BLOCKED, WashingBasinBlockEntity.NO_WATER -> 0xFFFFBF76;
            case WashingBasinBlockEntity.RUNNING -> MachineSkin.CYAN;
            default -> MachineSkin.MUTED;
        };
        String statusKey = switch (menu.status()) {
            case WashingBasinBlockEntity.RUNNING -> "washing";
            case WashingBasinBlockEntity.BLOCKED -> "blocked";
            case WashingBasinBlockEntity.NO_WATER -> "no_water";
            default -> "idle";
        };
        g.fill(x + 177, y + 12, x + 181, y + 16, statusColor);
        MachineSkin.text(g, font, text(statusKey), x + 186, y + 12, 43, statusColor, .73F);

        MachineSkin.panel(g, x + 18, y + 35, 212, 18, menu.hasWater() ? 0xFF355B66 : 0xFF5C4939);
        MachineSkin.centered(g, font, text(menu.hasWater() ? "water_connected" : "water_required"), x + 124, y + 40, 204,
                menu.hasWater() ? MachineSkin.CYAN : 0xFFFFBF76, .74F);

        MachineSkin.panel(g, x + 18, y + 58, 212, 74, 0xFF54616D);
        MachineSkin.centered(g, font, text("input"), x + WashingMenu.INPUT_X + 8, y + 64, 54, MachineSkin.MUTED, .73F);
        MachineSkin.centered(g, font, text("output"), x + 195, y + 64, 70, MachineSkin.MUTED, .73F);
        skin.slot(g, x + WashingMenu.INPUT_X, y + WashingMenu.MACHINE_Y, true);
        skin.slot(g, x + WashingMenu.OUTPUT_X, y + WashingMenu.MACHINE_Y, true);
        skin.slot(g, x + WashingMenu.SECONDARY_OUTPUT_X, y + WashingMenu.MACHINE_Y, true);

        int barX = x + 83, barW = 82;
        boolean running = menu.status() == WashingBasinBlockEntity.RUNNING;
        float partial = minecraft.isPaused() ? 0 : partialTick;
        float phase = MachineAnimation.phase(menu.progress() + (running ? partial : 0), menu.duration());
        MachineProcessEffects.washing(g, barX, y + 74, barW, phase, running, menu.status() == WashingBasinBlockEntity.BLOCKED);
        MachineSkin.meter(g, barX, y + 109, barW, 3, phase, MachineSkin.ICE, phase * menu.duration(), running);
        MachineSkin.centered(g, font, Component.literal(String.format(java.util.Locale.ROOT, "%.1f / %.1f s",
                menu.progress() / 20F, menu.duration() / 20F)), barX + barW / 2, y + 97, barW + 8, MachineSkin.MUTED, .65F);

        Component summary = text("washing_recipe");
        if (minecraft.level != null) {
            ItemStack input = menu.getSlot(WashingBasinBlockEntity.INPUT).getItem();
            var recipe = minecraft.level.getRecipeManager().getRecipeFor(ForeverSkyblock.WASHING.get(), new WashingInput(input), minecraft.level);
            if (recipe.isPresent()) {
                var washing = recipe.get().value();
                StringBuilder result = new StringBuilder(washing.result().getHoverName().getString());
                if (washing.hasSecondaryResult()) result.append(" + ").append(washing.secondaryResult().getCount()).append("× ")
                        .append(washing.secondaryResult().getHoverName().getString());
                if (washing.hasBonusResult()) result.append(" + ").append(washing.bonusChancePercent()).append("% ")
                        .append(washing.bonusResult().getHoverName().getString());
                summary = Component.literal(washing.ingredientCount() + " × " + input.getHoverName().getString() + " → " + result);
            }
        }
        MachineSkin.centered(g, font, summary, x + 124, y + 118, 196, MachineSkin.LIGHT, .68F);

        MachineSkin.panel(g, x + 18, y + 138, 138, 34, 0xFF465A68);
        MachineSkin.text(g, font, text("washing_hint_source"), x + 25, y + 145, 124, MachineSkin.MUTED, .72F);
        MachineSkin.text(g, font, text("washing_hint_water"), x + 25, y + 157, 124, MachineSkin.MUTED, .72F);
        inventory(g);
    }
}

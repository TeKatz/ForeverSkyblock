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
        recipes = addRenderableWidget(new MachineButton(leftPos + 164, topPos + 126, 64, 20, text("recipes"), skin, false,
                b -> ClientMachineRecipes.washing()));
    }

    @Override protected void containerTick() {
        super.containerTick();
        recipes.active = ClientMachineRecipes.available();
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
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

        MachineSkin.panel(g, x + 18, y + 58, 212, 58, 0xFF54616D);
        MachineSkin.centered(g, font, text("input"), x + WashingMenu.INPUT_X + 8, y + 62, 54, MachineSkin.MUTED, .73F);
        MachineSkin.centered(g, font, text("output"), x + WashingMenu.OUTPUT_X + 8, y + 62, 54, MachineSkin.MUTED, .73F);
        skin.slot(g, x + WashingMenu.INPUT_X, y + WashingMenu.MACHINE_Y, true);
        skin.slot(g, x + WashingMenu.OUTPUT_X, y + WashingMenu.MACHINE_Y, true);

        int barX = x + 83, barW = 82;
        MachineSkin.panel(g, barX, y + 73, barW, 8, 0xFF637888);
        int filled = Math.min(barW - 4, (barW - 4) * menu.progress() / menu.duration());
        g.fill(barX + 2, y + 75, barX + 2 + filled, y + 79, MachineSkin.CYAN);
        g.fill(barX + barW, y + 72, barX + barW + 3, y + 82, MachineSkin.CYAN);
        MachineSkin.centered(g, font, Component.literal(String.format(java.util.Locale.ROOT, "%.1f / %.1f s",
                menu.progress() / 20F, menu.duration() / 20F)), barX + barW / 2, y + 87, barW + 8, MachineSkin.MUTED, .7F);

        Component summary = text("washing_recipe");
        if (minecraft.level != null) {
            ItemStack input = menu.getSlot(WashingBasinBlockEntity.INPUT).getItem();
            var recipe = minecraft.level.getRecipeManager().getRecipeFor(ForeverSkyblock.WASHING.get(), new WashingInput(input), minecraft.level);
            if (recipe.isPresent()) summary = Component.literal(input.getHoverName().getString() + " → "
                    + recipe.get().value().result().getHoverName().getString());
        }
        MachineSkin.centered(g, font, summary, x + 124, y + 101, 196, MachineSkin.LIGHT, .82F);

        MachineSkin.panel(g, x + 18, y + 122, 138, 28, 0xFF465A68);
        MachineSkin.text(g, font, text("washing_hint"), x + 25, y + 129, 124, MachineSkin.MUTED, .68F);
        inventory(g);
    }
}

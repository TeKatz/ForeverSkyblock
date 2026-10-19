package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
import de.tekatz.foreverskyblock.menu.CrucibleMenu;
import de.tekatz.foreverskyblock.menu.MachineLayout;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.item.ItemStack;

public final class CrucibleScreen extends MachineScreen<CrucibleMenu> {
    private MachineButton modeButton;

    public CrucibleScreen(CrucibleMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title, MachineLayout.WASHING, false, CrucibleBlockEntity.MACHINE_SLOTS);
    }

    @Override protected void init() {
        super.init();
        modeButton = addRenderableWidget(new MachineButton(leftPos + 80, topPos + 151, 88, 22,
                Component.literal("Output: Lava"), skin, true, b -> {
                    if (minecraft.gameMode != null) minecraft.gameMode.handleInventoryButtonClick(menu.containerId, CrucibleMenu.BUTTON_TOGGLE_MODE);
                }));
        refreshModeButton();
    }

    @Override protected void containerTick() {
        super.containerTick();
        refreshModeButton();
    }

    private void refreshModeButton() {
        if (modeButton == null) return;
        modeButton.visible = menu.canToggleMode();
        modeButton.active = menu.canToggleMode();
        modeButton.setMessage(Component.literal(menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN ? "Output: Obsidian" : "Output: Lava"));
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        skin.frame(g, x, y, imageWidth, imageHeight);
        g.renderItem(new ItemStack(ForeverSkyblock.CRUCIBLE.get()), x + 18, y + 10);
        MachineSkin.text(g, font, Component.literal("CRUCIBLE"), x + 42, y + 12, 128, MachineSkin.LIGHT, 1.05F);

        int statusColor = switch (menu.status()) {
            case CrucibleBlockEntity.HEATING, CrucibleBlockEntity.FREEZING -> MachineSkin.CYAN;
            case CrucibleBlockEntity.NO_HEAT, CrucibleBlockEntity.STORAGE_FULL, CrucibleBlockEntity.OUTPUT_BLOCKED -> 0xFFFFBF76;
            default -> MachineSkin.MUTED;
        };
        String status = switch (menu.status()) {
            case CrucibleBlockEntity.HEATING -> "MELTING";
            case CrucibleBlockEntity.FREEZING -> "FREEZING";
            case CrucibleBlockEntity.NO_HEAT -> "NO HEAT";
            case CrucibleBlockEntity.STORAGE_FULL -> "LAVA FULL";
            case CrucibleBlockEntity.OUTPUT_BLOCKED -> "OUTPUT FULL";
            default -> "IDLE";
        };
        g.fill(x + 177, y + 12, x + 181, y + 16, statusColor);
        MachineSkin.text(g, font, Component.literal(status), x + 186, y + 12, 44, statusColor, .68F);

        MachineSkin.panel(g, x + 18, y + 35, 212, 18, menu.heatTier() > 0 ? 0xFF5A4536 : 0xFF4B4F54);
        MachineSkin.centered(g, font, Component.literal("Heat: " + CrucibleBlockEntity.heatTierName(menu.heatTier())), x + 124, y + 40, 204,
                menu.heatTier() > 0 ? 0xFFFFBF76 : MachineSkin.MUTED, .76F);

        MachineSkin.panel(g, x + 18, y + 58, 103, 18, menu.hasTank() ? 0xFF355B66 : 0xFF454B50);
        MachineSkin.centered(g, font, Component.literal(menu.hasTank() ? "Fluid Tank detected" : "No Fluid Tank"), x + 69, y + 63, 95,
                menu.hasTank() ? MachineSkin.CYAN : MachineSkin.MUTED, .68F);
        MachineSkin.panel(g, x + 127, y + 58, 103, 18, menu.hasFreezer() ? 0xFF355B66 : 0xFF454B50);
        MachineSkin.centered(g, font, Component.literal(menu.hasFreezer() ? "Freezing detected" : "No Freezing Chamber"), x + 178, y + 63, 95,
                menu.hasFreezer() ? MachineSkin.CYAN : MachineSkin.MUTED, .66F);

        MachineSkin.panel(g, x + 18, y + 81, 212, 67, 0xFF54616D);
        MachineSkin.centered(g, font, Component.literal("INPUT"), x + CrucibleMenu.INPUT_X + 8, y + 82, 60, MachineSkin.MUTED, .70F);
        MachineSkin.centered(g, font, Component.literal("OUTPUT"), x + CrucibleMenu.OUTPUT_X + 8, y + 82, 60, MachineSkin.MUTED, .70F);
        skin.slot(g, x + CrucibleMenu.INPUT_X, y + CrucibleMenu.MACHINE_Y, true);
        skin.slot(g, x + CrucibleMenu.OUTPUT_X, y + CrucibleMenu.MACHINE_Y, true);

        int meterX = x + 111, meterY = y + 91, meterW = 26, meterH = 32;
        MachineSkin.panel(g, meterX, meterY, meterW, meterH, 0xFF3D454B);
        int lavaHeight = Math.min(meterH - 4, (meterH - 4) * menu.lavaMb() / CrucibleBlockEntity.CAPACITY);
        if (lavaHeight > 0) g.fill(meterX + 2, meterY + meterH - 2 - lavaHeight, meterX + meterW - 2, meterY + meterH - 2, 0xFFFF6A20);
        MachineSkin.centered(g, font, Component.literal(menu.lavaMb() + " mB"), x + 124, y + 124, 72, MachineSkin.LIGHT, .64F);

        int meltBarX = x + 46, freezeBarX = x + 138, barY = y + 138, barW = 64;
        MachineSkin.centered(g, font, Component.literal("MELT"), x + 78, y + 130, 48, MachineSkin.MUTED, .56F);
        MachineSkin.centered(g, font, Component.literal("FREEZE"), x + 170, y + 130, 52,
                menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN && menu.hasFreezer() ? MachineSkin.CYAN : MachineSkin.MUTED, .56F);
        g.fill(meltBarX, barY, meltBarX + barW, barY + 4, 0xFF637888);
        int meltFilled = Math.min(barW, barW * menu.progress() / menu.duration());
        if (meltFilled > 0) g.fill(meltBarX, barY, meltBarX + meltFilled, barY + 4, 0xFFFF6A20);
        g.fill(freezeBarX, barY, freezeBarX + barW, barY + 4, 0xFF637888);
        int freezeFilled = Math.min(barW, barW * menu.freezeProgress() / menu.freezeDuration());
        if (freezeFilled > 0) g.fill(freezeBarX, barY, freezeBarX + freezeFilled, barY + 4, MachineSkin.CYAN);

        if (!menu.canToggleMode()) {
            String output = menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN ? "Route: Obsidian" : "Route: Lava";
            MachineSkin.centered(g, font, Component.literal(output), x + 124, y + 157, 160, MachineSkin.MUTED, .74F);
        }
        MachineSkin.centered(g, font, Component.literal("8 Cobblestone or 40 Stone Fragments → 1000 mB"), x + 124, y + 178, 210, MachineSkin.MUTED, .60F);
        inventory(g);
    }
}

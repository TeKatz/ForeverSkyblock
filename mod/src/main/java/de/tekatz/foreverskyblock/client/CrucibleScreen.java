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
    private MachineButton modeButton, recipes;
    private float framePartial;

    public CrucibleScreen(CrucibleMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title, MachineLayout.WASHING, false, CrucibleBlockEntity.MACHINE_SLOTS);
    }

    @Override protected void init() {
        super.init();
        modeButton = addRenderableWidget(new MachineButton(leftPos + 80, topPos + 151, 88, 22,
                Component.literal("Output: Lava"), skin, true, b -> {
                    if (minecraft.gameMode != null) minecraft.gameMode.handleInventoryButtonClick(menu.containerId, CrucibleMenu.BUTTON_TOGGLE_MODE);
                }));
        recipes = addRenderableWidget(new MachineButton(leftPos + 174, topPos + 151, 56, 22,
                Component.literal("Recipes"), skin, false, b -> ClientMachineRecipes.crucible()));
        refreshModeButton();
    }

    @Override protected void containerTick() {
        super.containerTick();
        refreshModeButton();
        if (recipes != null) recipes.active = ClientMachineRecipes.available();
    }

    private void refreshModeButton() {
        if (modeButton == null) return;
        modeButton.visible = menu.canToggleMode();
        modeButton.active = menu.canToggleMode();
        modeButton.setMessage(Component.literal(menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN ? "Output: Obsidian" : "Output: Lava"));
    }

    private float animationTime() {
        return (minecraft.level == null ? 0 : minecraft.level.getGameTime()) + framePartial;
    }

    private static float progressRatio(int progress, int duration, float partial, boolean running) {
        if (duration <= 0) return 0F;
        return Math.min(1F, Math.max(0F, (progress + (running ? partial : 0F)) / duration));
    }

    private static void drawSoftSweep(GuiGraphics g, int x, int y, int width, float time, int outer, int middle, int core) {
        if (width <= 0) return;
        float phase = (time % 48F) / 48F;
        int center = x - 7 + Math.round(phase * (width + 14));
        int left = x;
        int right = x + width;
        int a0 = Math.max(left, center - 7), a1 = Math.min(right, center + 8);
        if (a1 > a0) g.fill(a0, y, a1, y + 4, outer);
        int b0 = Math.max(left, center - 4), b1 = Math.min(right, center + 5);
        if (b1 > b0) g.fill(b0, y, b1, y + 4, middle);
        int c0 = Math.max(left, center - 1), c1 = Math.min(right, center + 2);
        if (c1 > c0) g.fill(c0, y, c1, y + 4, core);
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        framePartial = minecraft.isPaused() ? 0 : partialTick;
        int x = leftPos, y = topPos;
        float time = animationTime();
        boolean melting = menu.status() == CrucibleBlockEntity.HEATING;
        boolean freezing = menu.status() == CrucibleBlockEntity.FREEZING;

        skin.accent(MachineSkin.COPPER);
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
        if (melting || freezing) {
            int pulse = 0x22 + Math.round((.5F + .5F * (float)Math.sin(time * .18F)) * 0x33);
            int glow = (pulse << 24) | 0x004DEBFF;
            g.fill(x + 175, y + 10, x + 183, y + 18, glow);
        }
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

        MachineSkin.panel(g, x + 18, y + 81, 212, 70, 0xFF54616D);
        MachineSkin.centered(g, font, Component.literal("INPUT"), x + CrucibleMenu.INPUT_X + 8, y + 89, 60, MachineSkin.MUTED, .70F);
        MachineSkin.centered(g, font, Component.literal("OUTPUT"), x + CrucibleMenu.OUTPUT_X + 8, y + 89, 60, MachineSkin.MUTED, .70F);
        skin.slot(g, x + CrucibleMenu.INPUT_X, y + CrucibleMenu.MACHINE_Y, true);
        skin.slot(g, x + CrucibleMenu.OUTPUT_X, y + CrucibleMenu.MACHINE_Y, true);

        int meterX = x + 111, meterY = y + 95, meterW = 26, meterH = 32;
        MachineSkin.panel(g, meterX, meterY, meterW, meterH, 0xFF3D454B);
        int lavaHeight = Math.min(meterH - 4, (meterH - 4) * menu.lavaMb() / CrucibleBlockEntity.CAPACITY);
        if (lavaHeight > 0) {
            int lavaTop = meterY + meterH - 2 - lavaHeight;
            g.fill(meterX + 2, lavaTop, meterX + meterW - 2, meterY + meterH - 2, 0xFFFF6A20);
            int drift = Math.floorMod((int)(time / 5F), 3);
            g.fill(meterX + 3 + drift, lavaTop, meterX + meterW - 4, lavaTop + 1, 0xFFFFB34D);
            if (melting) {
                for (int i = 0; i < 3; i++) {
                    int bubbleX = meterX + 5 + i * 6;
                    int travel = Math.floorMod((int)(time * 1.6F) + i * 9, Math.max(2, lavaHeight + 5));
                    int bubbleY = meterY + meterH - 4 - travel;
                    if (bubbleY >= lavaTop + 1 && bubbleY < meterY + meterH - 3)
                        g.fill(bubbleX, bubbleY, bubbleX + 1, bubbleY + 1, i == 1 ? 0xFFFFE1A8 : 0xFFFFC26A);
                }
            }
            if (freezing) {
                int frost = 0x99DFF9FF;
                g.fill(meterX + 2, lavaTop, meterX + 5, lavaTop + 1, frost);
                g.fill(meterX + meterW - 5, lavaTop + 1, meterX + meterW - 2, lavaTop + 2, frost);
            }
        }

        if (melting) {
            int heatBaseY = meterY - 2;
            for (int i = 0; i < 3; i++) {
                int hx = meterX + 5 + i * 7;
                int rise = Math.floorMod((int)(time * 1.2F) + i * 4, 6);
                int hy = heatBaseY - rise;
                g.fill(hx, hy, hx + 1, hy + 2, i == 1 ? 0x66FFB04A : 0x44FF7A2A);
            }
        }
        if (freezing) {
            for (int i = 0; i < 4; i++) {
                int px = meterX - 2 + Math.floorMod((int)(time * .8F) + i * 8, meterW + 4);
                int py = meterY + 3 + Math.floorMod((int)(time * .6F) + i * 5, meterH - 7);
                if (px < meterX + 2 || px >= meterX + meterW - 2)
                    g.fill(px, py, px + 1, py + 1, i % 2 == 0 ? 0xFFE1F7FF : MachineSkin.CYAN);
            }
        }
        MachineSkin.centered(g, font, Component.literal(menu.lavaMb() + " mB"), x + 124, y + 129, 72, MachineSkin.LIGHT, .64F);

        int meltBarX = x + 46, freezeBarX = x + 138, barY = y + 141, barW = 64;
        MachineSkin.centered(g, font, Component.literal("MELT"), x + 78, y + 133, 48, melting ? 0xFFFFBF76 : MachineSkin.MUTED, .56F);
        MachineSkin.centered(g, font, Component.literal("FREEZE"), x + 170, y + 133, 52,
                menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN && menu.hasFreezer() ? MachineSkin.CYAN : MachineSkin.MUTED, .56F);
        g.fill(meltBarX, barY, meltBarX + barW, barY + 4, 0xFF637888);
        float meltRatio = progressRatio(menu.progress(), menu.duration(), framePartial, melting);
        int meltFilled = Math.round(barW * meltRatio);
        if (meltFilled > 0) {
            g.fill(meltBarX, barY, meltBarX + meltFilled, barY + 4, 0xFFFF6A20);
            if (melting) drawSoftSweep(g, meltBarX, barY, meltFilled, time, 0x33FFD29A, 0x66FFD08A, 0xFFFFE0AE);
        }
        g.fill(freezeBarX, barY, freezeBarX + barW, barY + 4, 0xFF637888);
        float freezeRatio = progressRatio(menu.freezeProgress(), menu.freezeDuration(), framePartial, freezing);
        int freezeFilled = Math.round(barW * freezeRatio);
        if (freezeFilled > 0) {
            g.fill(freezeBarX, barY, freezeBarX + freezeFilled, barY + 4, MachineSkin.CYAN);
            if (freezing) drawSoftSweep(g, freezeBarX, barY, freezeFilled, time + 12F, 0x334DEBFF, 0x668DEEFF, 0xFFE1F7FF);
        }

        if (!menu.canToggleMode()) {
            String output = menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN ? "Route: Obsidian" : "Route: Lava";
            MachineSkin.centered(g, font, Component.literal(output), x + 124, y + 157, 160, MachineSkin.MUTED, .74F);
        }
        MachineSkin.centered(g, font, Component.literal("8 Cobble → 1000 mB · 40 Fragments → 100 mB"), x + 124, y + 178, 210, MachineSkin.MUTED, .60F);
        inventory(g);
    }
}

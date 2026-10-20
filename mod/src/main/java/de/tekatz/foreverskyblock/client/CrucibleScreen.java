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

    private float animationTime() {
        return (minecraft.level == null ? 0 : minecraft.level.getGameTime()) + framePartial;
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        framePartial = minecraft.isPaused() ? 0 : partialTick;
        int x = leftPos, y = topPos;
        float time = animationTime();
        boolean melting = menu.status() == CrucibleBlockEntity.HEATING;
        boolean freezing = menu.status() == CrucibleBlockEntity.FREEZING;

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
            int glow = ((int)(time * 4) & 1) == 0 ? 0x554DEBFF : 0x3356CBE4;
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
            int wave = ((int)(time * 3) & 1);
            g.fill(meterX + 3 + wave, lavaTop, meterX + meterW - 4, lavaTop + 1, 0xFFFFB34D);
            if (melting) {
                for (int i = 0; i < 3; i++) {
                    int bubbleX = meterX + 5 + i * 6;
                    int travel = Math.floorMod((int)(time * 5) + i * 7, Math.max(2, lavaHeight));
                    int bubbleY = meterY + meterH - 4 - travel;
                    if (bubbleY >= lavaTop + 1) g.fill(bubbleX, bubbleY, bubbleX + 1, bubbleY + 1, 0xFFFFD28A);
                }
            }
        }
        MachineSkin.centered(g, font, Component.literal(menu.lavaMb() + " mB"), x + 124, y + 129, 72, MachineSkin.LIGHT, .64F);

        int meltBarX = x + 46, freezeBarX = x + 138, barY = y + 141, barW = 64;
        MachineSkin.centered(g, font, Component.literal("MELT"), x + 78, y + 133, 48, melting ? 0xFFFFBF76 : MachineSkin.MUTED, .56F);
        MachineSkin.centered(g, font, Component.literal("FREEZE"), x + 170, y + 133, 52,
                menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN && menu.hasFreezer() ? MachineSkin.CYAN : MachineSkin.MUTED, .56F);
        g.fill(meltBarX, barY, meltBarX + barW, barY + 4, 0xFF637888);
        int meltFilled = Math.min(barW, barW * menu.progress() / menu.duration());
        if (meltFilled > 0) {
            g.fill(meltBarX, barY, meltBarX + meltFilled, barY + 4, 0xFFFF6A20);
            if (melting) {
                int shine = Math.min(meltFilled - 1, Math.floorMod((int)(time * 9), Math.max(1, meltFilled)));
                g.fill(meltBarX + shine, barY, meltBarX + Math.min(meltFilled, shine + 3), barY + 1, 0xFFFFD08A);
            }
        }
        g.fill(freezeBarX, barY, freezeBarX + barW, barY + 4, 0xFF637888);
        int freezeFilled = Math.min(barW, barW * menu.freezeProgress() / menu.freezeDuration());
        if (freezeFilled > 0) {
            g.fill(freezeBarX, barY, freezeBarX + freezeFilled, barY + 4, MachineSkin.CYAN);
            if (freezing) {
                int shine = Math.min(freezeFilled - 1, Math.floorMod((int)(time * 8), Math.max(1, freezeFilled)));
                g.fill(freezeBarX + shine, barY, freezeBarX + Math.min(freezeFilled, shine + 3), barY + 1, 0xFFE1F7FF);
            }
        }

        if (melting) {
            for (int i = 0; i < 3; i++) {
                int shimmerX = x + 105 + i * 10;
                int rise = Math.floorMod((int)(time * 4) + i * 5, 10);
                int shimmerY = y + 91 - rise;
                g.fill(shimmerX, shimmerY, shimmerX + 1, shimmerY + 3, i == 1 ? 0x66FFB04A : 0x44FF7A2A);
            }
        }
        if (freezing) {
            for (int i = 0; i < 4; i++) {
                int sparkX = x + 166 + Math.floorMod((int)(time * 3) + i * 11, 37);
                int sparkY = y + 94 + Math.floorMod((int)(time * 2) + i * 7, 25);
                g.fill(sparkX, sparkY, sparkX + 1, sparkY + 1, i % 2 == 0 ? 0xFFE1F7FF : MachineSkin.CYAN);
            }
        }

        if (!menu.canToggleMode()) {
            String output = menu.mode() == CrucibleBlockEntity.MODE_OBSIDIAN ? "Route: Obsidian" : "Route: Lava";
            MachineSkin.centered(g, font, Component.literal(output), x + 124, y + 157, 160, MachineSkin.MUTED, .74F);
        }
        MachineSkin.centered(g, font, Component.literal("8 Cobblestone or 40 Stone Fragments → 1000 mB"), x + 124, y + 178, 210, MachineSkin.MUTED, .60F);
        inventory(g);
    }
}

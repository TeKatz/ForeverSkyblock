package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.blockentity.RefiningChamberBlockEntity;
import de.tekatz.foreverskyblock.menu.MachineLayout;
import de.tekatz.foreverskyblock.menu.RefiningMenu;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.item.ItemStack;

public final class RefiningScreen extends MachineScreen<RefiningMenu> {
    private MachineButton recipes;

    public RefiningScreen(RefiningMenu menu, Inventory inventory, Component title) {
        super(menu, inventory, title, MachineLayout.WASHING, false, RefiningChamberBlockEntity.MACHINE_SLOTS);
    }
    private static Component text(String key, Object... args) { return Component.translatable("gui.forever_skyblock.machine." + key, args); }

    @Override protected void init() {
        super.init();
        recipes = addRenderableWidget(new MachineButton(leftPos + 164, topPos + 144, 64, 22, text("recipes"), skin, false,
                b -> ClientMachineRecipes.refining()));
    }

    @Override protected void containerTick() {
        super.containerTick();
        if (recipes != null) recipes.active = ClientMachineRecipes.available();
    }

    @Override protected void renderBg(GuiGraphics g, float partialTick, int mouseX, int mouseY) {
        int x = leftPos, y = topPos;
        skin.accent(MachineSkin.VIOLET);
        skin.frame(g, x, y, imageWidth, imageHeight);
        g.renderItem(new ItemStack(ForeverSkyblock.REFINING_CHAMBER.get()), x + 18, y + 10);
        MachineSkin.text(g, font, Component.literal("REFINING CONDENSER"), x + 42, y + 12, 130, MachineSkin.LIGHT, 1.05F);
        int statusColor = switch (menu.status()) {
            case RefiningChamberBlockEntity.RUNNING -> MachineSkin.CYAN;
            case RefiningChamberBlockEntity.BLOCKED, RefiningChamberBlockEntity.NO_POWER -> 0xFFFFBF76;
            default -> MachineSkin.MUTED;
        };
        Component statusText = switch (menu.status()) {
            case RefiningChamberBlockEntity.RUNNING -> Component.literal("REFINING");
            case RefiningChamberBlockEntity.BLOCKED -> text("blocked");
            case RefiningChamberBlockEntity.NO_POWER -> Component.literal("NO POWER");
            default -> text("idle");
        };
        g.fill(x + 177, y + 12, x + 181, y + 16, statusColor);
        MachineSkin.text(g, font, statusText, x + 186, y + 12, 43, statusColor, .73F);

        MachineSkin.panel(g, x + 18, y + 35, 212, 20, 0xFF465A68);
        MachineSkin.centered(g, font, Component.literal(menu.energy() + " / " + menu.energyCapacity() + " FE"), x + 124, y + 38, 204, MachineSkin.LIGHT, .70F);
        g.fill(x + 23, y + 49, x + 225, y + 52, 0xFF2D3843);
        MachineSkin.meter(g, x + 23, y + 49, 202, 3, menu.energy() / (float) menu.energyCapacity(), MachineSkin.VIOLET, 0, false);

        MachineSkin.panel(g, x + 18, y + 60, 212, 72, 0xFF54616D);
        MachineSkin.centered(g, font, Component.literal("MATERIAL"), x + RefiningMenu.PRIMARY_X + 8, y + 65, 54, MachineSkin.MUTED, .73F);
        MachineSkin.centered(g, font, Component.literal("CATALYST"), x + RefiningMenu.CATALYST_X + 8, y + 65, 54, MachineSkin.MUTED, .73F);
        MachineSkin.centered(g, font, text("output"), x + RefiningMenu.OUTPUT_X + 8, y + 65, 54, MachineSkin.MUTED, .73F);
        skin.slot(g, x + RefiningMenu.PRIMARY_X, y + RefiningMenu.MACHINE_Y, true);
        skin.slot(g, x + RefiningMenu.CATALYST_X, y + RefiningMenu.MACHINE_Y, true);
        skin.slot(g, x + RefiningMenu.OUTPUT_X, y + RefiningMenu.MACHINE_Y, true);
        int barX = x + 111, barW = 50;
        boolean running = menu.status() == RefiningChamberBlockEntity.RUNNING;
        float partial = minecraft.isPaused() ? 0 : partialTick;
        float phase = MachineAnimation.phase(menu.progress() + (running ? partial : 0), menu.duration());
        MachineProcessEffects.refining(g, barX, y + 75, barW, phase, running,
                menu.status() == RefiningChamberBlockEntity.BLOCKED || menu.status() == RefiningChamberBlockEntity.NO_POWER);
        MachineSkin.meter(g, barX, y + 115, barW, 3, phase, MachineSkin.VIOLET, phase * menu.duration(), running);
        MachineSkin.centered(g, font, Component.literal(String.format(java.util.Locale.ROOT, "%.1f / %.1f s", menu.progress()/20F, menu.duration()/20F)), barX + barW/2, y + 104, 60, MachineSkin.MUTED, .65F);

        MachineSkin.panel(g, x + 18, y + 138, 212, 34, 0xFF465A68);
        MachineSkin.text(g, font, Component.literal("Consumes FE continuously"), x + 25, y + 145, 132, MachineSkin.MUTED, .67F);
        MachineSkin.text(g, font, Component.literal("Materials & costs in JEI"), x + 25, y + 157, 132, MachineSkin.MUTED, .67F);
        inventory(g);
    }
}

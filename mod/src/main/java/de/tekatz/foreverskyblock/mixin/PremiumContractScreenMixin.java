package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.client.PremiumMenuLayout;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import de.tekatz.foreverskyblock.progression.ContractCatalog;
import java.lang.reflect.Field;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Phase-5 contract cards layered onto the existing Design-2 Challenges/Contracts page. */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumContractScreenMixin {
    @Shadow private PremiumMenuLayout layout;
    @Shadow private int x;
    @Shadow private int y;
    private static final int GOLD = 0xFFFFC447, WHITE = 0xFFF6FAFF, MUTED = 0xFF93A9BC, CYAN = 0xFF2CCBFF;
    private static final Field PAGE = field("page"), SUBSECTIONS = field("subsections");

    @Inject(method = "categoryCount", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$contractCount(String category, CallbackInfoReturnable<Integer> callback) {
        if ("contracts".equals(category)) callback.setReturnValue(ContractCatalog.CONTRACTS.size());
    }

    @Inject(method = "drawChallengeChrome", at = @At("TAIL"))
    private void foreverSkyblock$drawContracts(GuiGraphics g, CallbackInfo callback) {
        if (!contractsPage()) return;
        Font font = Minecraft.getInstance().font;
        int mx = x + layout.challengeMainX(), mw = layout.challengeMainWidth();
        int top = y + layout.contentTop() + 43, bottom = y + layout.contentBottom() - 5;
        g.fill(mx + 4, top, mx + mw - 4, bottom, 0xF208151F);
        String intro = german() ? "WIEDERHOLBARE VERTRAEGE · Ressourcen werden serverseitig verbraucht."
                : "REPEATABLE CONTRACTS · Resources are consumed server-side.";
        g.drawString(font, intro, mx + 10, top + 5, GOLD, false);

        int areaTop = top + 19, areaW = mw - 16, gap = 6, columns = areaW >= 300 ? 2 : 1;
        int rows = (ContractCatalog.CONTRACTS.size() + columns - 1) / columns;
        int cardW = (areaW - gap * (columns - 1)) / columns;
        int cardH = Math.max(38, Math.min(64, (bottom - areaTop - gap * Math.max(0, rows - 1)) / Math.max(1, rows)));
        for (int i = 0; i < ContractCatalog.CONTRACTS.size(); i++) {
            var contract = ContractCatalog.CONTRACTS.get(i);
            int cx = mx + 8 + (i % columns) * (cardW + gap), cy = areaTop + (i / columns) * (cardH + gap);
            drawCard(g, font, contract, cx, cy, cardW, cardH);
        }
    }

    @Inject(method = "mouseClicked", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$clickContract(double mouseX, double mouseY, int button, CallbackInfoReturnable<Boolean> callback) {
        if (button != 0 || !contractsPage() || ClientSkills.pending()) return;
        var snapshot = ClientSkills.snapshot(); if (snapshot == null || !snapshot.available()) return;
        int mx = x + layout.challengeMainX(), mw = layout.challengeMainWidth();
        int top = y + layout.contentTop() + 43, bottom = y + layout.contentBottom() - 5;
        int areaTop = top + 19, areaW = mw - 16, gap = 6, columns = areaW >= 300 ? 2 : 1;
        int rows = (ContractCatalog.CONTRACTS.size() + columns - 1) / columns;
        int cardW = (areaW - gap * (columns - 1)) / columns;
        int cardH = Math.max(38, Math.min(64, (bottom - areaTop - gap * Math.max(0, rows - 1)) / Math.max(1, rows)));
        for (int i = 0; i < ContractCatalog.CONTRACTS.size(); i++) {
            var contract = ContractCatalog.CONTRACTS.get(i); if (!contract.resource()) continue;
            var state = snapshot.contracts().get(contract.id()); if (state == null || !state.canSubmit()) continue;
            int cx = mx + 8 + (i % columns) * (cardW + gap), cy = areaTop + (i / columns) * (cardH + gap);
            int bx = cx + cardW - 61, by = cy + cardH - 17;
            if (mouseX >= bx && mouseX < bx + 55 && mouseY >= by && mouseY < by + 13) {
                ClientSkills.submitContract(contract.id()); callback.setReturnValue(true); return;
            }
        }
    }

    private void drawCard(GuiGraphics g, Font font, ContractCatalog.Contract contract, int cx, int cy, int cw, int ch) {
        var snapshot = ClientSkills.snapshot();
        var state = snapshot == null ? null : snapshot.contracts().get(contract.id());
        boolean available = state != null && state.available();
        int border = available ? 0xFF385D72 : 0xFF493F42;
        g.fill(cx, cy, cx + cw, cy + ch, available ? 0xE20C1C29 : 0xD9181A1D); contractOutline(g, cx, cy, cw, ch, border);
        ItemStack icon = icon(contract); g.renderItem(icon, cx + 6, cy + 7);
        String title = german() ? contract.titleDe() : contract.titleEn();
        String description = german() ? contract.descriptionDe() : contract.descriptionEn();
        g.drawString(font, trim(font, title, cw - 31), cx + 27, cy + 6, available ? WHITE : MUTED, false);
        if (ch >= 50) g.drawString(font, trim(font, description, cw - 34), cx + 27, cy + 18, MUTED, false);
        int progress = state == null ? 0 : state.progress(); long completions = state == null ? 0 : state.completions();
        String progressText = contract.resource()
                ? (german() ? "Im Inventar: " : "Inventory: ") + progress + " / " + contract.target()
                : (german() ? "Fortschritt: " : "Progress: ") + progress + " / " + contract.target();
        int py = cy + Math.max(29, ch - 26);
        g.drawString(font, trim(font, progressText, cw - 12), cx + 6, py, available ? CYAN : MUTED, false);
        String reward = "+" + contract.rewardSp() + " SP · " + (german() ? "Abschluesse: " : "Completed: ") + completions;
        g.drawString(font, trim(font, reward, cw - (contract.resource() ? 70 : 12)), cx + 6, cy + ch - 13, GOLD, false);
        if (!available) g.drawString(font, german() ? "Tier I erforderlich" : "Requires Tier I", cx + Math.max(6, cw - 92), cy + ch - 13, 0xFFFF7777, false);
        else if (contract.resource()) {
            int bx = cx + cw - 61, by = cy + ch - 17; boolean submit = state != null && state.canSubmit();
            g.fill(bx, by, bx + 55, by + 13, submit ? 0xFF174B5B : 0xFF23323A); contractOutline(g, bx, by, 55, 13, submit ? CYAN : 0xFF4A5D68);
            String button = state != null && state.pending() ? (german() ? "LAEUFT" : "PENDING") : (german() ? "ABGEBEN" : "TURN IN");
            int tw = font.width(button); g.drawString(font, button, bx + (55 - tw) / 2, by + 3, submit ? WHITE : MUTED, false);
        } else if (ch >= 50) {
            String automatic = german() ? "AUTOMATISCH" : "AUTOMATIC";
            g.drawString(font, automatic, cx + cw - font.width(automatic) - 6, cy + ch - 13, MUTED, false);
        }
    }

    private boolean contractsPage() {
        try {
            Object page = PAGE.get(this); if (page == null || !"CHALLENGES".equals(page.toString())) return false;
            @SuppressWarnings("unchecked") Map<Object, Integer> values = (Map<Object, Integer>) SUBSECTIONS.get(this);
            return values.getOrDefault(page, 0) == 7;
        } catch (IllegalAccessException e) { return false; }
    }
    private static Field field(String name) {
        try { Field field = PremiumSkillScreen.class.getDeclaredField(name); field.setAccessible(true); return field; }
        catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static boolean german() { return Minecraft.getInstance().getLanguageManager().getSelected().toLowerCase(java.util.Locale.ROOT).startsWith("de_"); }
    private static ItemStack icon(ContractCatalog.Contract contract) {
        try { return BuiltInRegistries.ITEM.getOptional(ResourceLocation.parse(contract.iconId())).orElse(Items.BARRIER).getDefaultInstance(); }
        catch (RuntimeException e) { return Items.BARRIER.getDefaultInstance(); }
    }
    private static String trim(Font font, String text, int width) { return font.plainSubstrByWidth(text, Math.max(1, width)); }
    private static void contractOutline(GuiGraphics g, int x, int y, int w, int h, int color) {
        g.fill(x, y, x + w, y + 1, color); g.fill(x, y + h - 1, x + w, y + h, color);
        g.fill(x, y, x + 1, y + h, color); g.fill(x + w - 1, y, x + w, y + h, color);
    }
}

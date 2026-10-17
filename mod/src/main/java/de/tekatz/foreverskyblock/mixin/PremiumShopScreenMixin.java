package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.client.ClientSkills;
import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.ShopCatalog;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.List;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.ConfirmScreen;
import net.minecraft.network.chat.Component;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Real current-world products integrated into the existing Design-2 generic card panel. */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumShopScreenMixin {
    private static final Field PAGE = field("page"), SUBSECTIONS = field("subsections");
    private static final Constructor<?> CARD = cardConstructor();
    private static final Method CARD_PRODUCT = cardProduct();

    @Inject(method = "cards", at = @At("RETURN"))
    private void foreverSkyblock$addProducts(CallbackInfoReturnable<List<?>> callback) {
        if (!currentWorldShop()) return;
        List<?> raw = callback.getReturnValue();
        if (raw == null) return;
        @SuppressWarnings("unchecked") List<Object> cards = (List<Object>) raw;
        cards.removeIf(card -> ShopCatalog.TEST_WORLD_ENTITLEMENT.equals(productId(card)));
        for (var product : ShopCatalog.PRODUCTS) {
            if (product.developmentOnly()) continue;
            try {
                String title = german() ? product.titleDe() : product.titleEn();
                String description = german() ? product.descriptionDe() : product.descriptionEn();
                String badge = product.spCost() + " SP · " + tier(product.requiredTier());
                Object card = CARD.newInstance(Component.literal(title), Component.literal(description), Component.literal(badge),
                        ForeverSkyblock.WORLD_SUPPLY_CACHE.get().getDefaultInstance(), "", product.id(), null,
                        Component.literal(german() ? "KAUFEN" : "PURCHASE"));
                cards.add(card);
            } catch (ReflectiveOperationException e) { throw new IllegalStateException("Could not build current-world shop card", e); }
        }
    }

    @Inject(method = "canBuyProduct", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$realProductEligibility(String id, CallbackInfoReturnable<Boolean> callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || product.developmentOnly()) return;
        var snapshot = ClientSkills.snapshot();
        SkillNetwork.ProductState state = snapshot == null ? null : snapshot.products().get(id);
        callback.setReturnValue(canPurchase(snapshot, state, product));
    }

    @Inject(method = "productStatus", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$productStatus(String id, CallbackInfoReturnable<Component> callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || product.developmentOnly()) return;
        var snapshot = ClientSkills.snapshot();
        if (snapshot == null || !snapshot.available()) { callback.setReturnValue(Component.literal(german() ? "NICHT VERFÜGBAR" : "UNAVAILABLE")); return; }
        if (!snapshot.worldAvailable()) { callback.setReturnValue(Component.literal(german() ? "WELT NICHT VERFÜGBAR" : "WORLD UNAVAILABLE")); return; }
        SkillNetwork.ProductState state = snapshot.products().get(id);
        if (state == null || !state.available()) { callback.setReturnValue(Component.literal(german() ? "INTEGRATION FEHLT" : "INTEGRATION UNAVAILABLE")); return; }
        callback.setReturnValue(switch (state.stage()) {
            case 1 -> Component.literal(german() ? "PAKET ABHOLEN" : "CLAIM PACKAGE");
            case 2, 4 -> Component.literal(german() ? "WIEDERHERSTELLUNG…" : "RECOVERING…");
            case 3 -> Component.literal(german() ? "PAKET AUSGELIEFERT" : "PACKAGE DELIVERED");
            case 5 -> Component.literal(german() ? "PAKET GEÖFFNET" : "PACKAGE OPENED");
            case 6 -> Component.literal(german() ? "BEANSPRUCHT" : "CLAIMED");
            default -> {
                if (snapshot.shopTier() < product.requiredTier()) yield Component.literal((german() ? "BENÖTIGT " : "REQUIRES ") + tier(product.requiredTier()));
                if (snapshot.perkPoints() < product.ppCost()) yield Component.literal(german() ? "ZU WENIG PP" : "NOT ENOUGH PP");
                if (snapshot.points() < product.spCost()) yield Component.literal(german() ? "ZU WENIG SP" : "NOT ENOUGH SP");
                yield Component.literal(german() ? "KAUFEN" : "PURCHASE");
            }
        });
    }

    /**
     * Phase 6 fell through to the old Phase-3 test-entitlement confirmation for new real products.
     * Intercept every real product here so the confirmation describes the package that is actually bought.
     */
    @Inject(method = "confirmProduct", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$realProductAction(String id, CallbackInfo callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || product.developmentOnly()) return;
        var snapshot = ClientSkills.snapshot();
        SkillNetwork.ProductState state = snapshot == null ? null : snapshot.products().get(id);
        if (state != null && state.stage() == 1 && !ClientSkills.pending()) {
            ClientSkills.claimProduct(id);
            callback.cancel();
            return;
        }
        if (canPurchase(snapshot, state, product)) {
            Minecraft minecraft = Minecraft.getInstance();
            PremiumSkillScreen parent = (PremiumSkillScreen) (Object) this;
            String name = german() ? product.titleDe() : product.titleEn();
            Component title = Component.literal(german() ? "Weltpaket kaufen?" : "Purchase current-world package?");
            Component description = Component.literal(german()
                    ? name + " für " + product.spCost() + " SP in „" + snapshot.worldName() + "“ kaufen? Der Kauf ist nicht erstattbar und gilt nur für diese Welt."
                    : "Buy " + name + " for " + product.spCost() + " SP in “" + snapshot.worldName() + "”? The purchase is non-refundable and applies only to this world.");
            Component confirm = Component.literal(german() ? "Kaufen" : "Purchase");
            minecraft.setScreen(new ConfirmScreen(yes -> {
                minecraft.setScreen(parent);
                if (yes) ClientSkills.buyProduct(id);
            }, title, description, confirm, Component.translatable("gui.cancel")));
        }
        callback.cancel();
    }

    private static boolean canPurchase(SkillNetwork.Snapshot snapshot, SkillNetwork.ProductState state, ShopCatalog.Product product) {
        return snapshot != null && snapshot.available() && snapshot.worldAvailable() && !ClientSkills.pending()
                && state != null && state.available() && state.stage() == 0
                && snapshot.shopTier() >= product.requiredTier() && snapshot.points() >= product.spCost()
                && snapshot.perkPoints() >= product.ppCost();
    }

    private boolean currentWorldShop() {
        try {
            Object page = PAGE.get(this); if (page == null || !"SHOP".equals(page.toString())) return false;
            @SuppressWarnings("unchecked") Map<Object, Integer> values = (Map<Object, Integer>) SUBSECTIONS.get(this);
            return values.getOrDefault(page, 0) == 1;
        } catch (IllegalAccessException e) { return false; }
    }
    private static String productId(Object card) {
        try { return (String) CARD_PRODUCT.invoke(card); }
        catch (ReflectiveOperationException e) { return ""; }
    }
    private static Field field(String name) {
        try { Field field = PremiumSkillScreen.class.getDeclaredField(name); field.setAccessible(true); return field; }
        catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static Constructor<?> cardConstructor() {
        try {
            Class<?> card = Class.forName("de.tekatz.foreverskyblock.client.PremiumSkillScreen$Card");
            Constructor<?> ctor = card.getDeclaredConstructors()[0]; ctor.setAccessible(true); return ctor;
        } catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static Method cardProduct() {
        try {
            Class<?> card = Class.forName("de.tekatz.foreverskyblock.client.PremiumSkillScreen$Card");
            Method method = card.getDeclaredMethod("product"); method.setAccessible(true); return method;
        } catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static boolean german() { return Minecraft.getInstance().getLanguageManager().getSelected().toLowerCase(java.util.Locale.ROOT).startsWith("de_"); }
    private static String tier(int tier) { return switch (tier) { case 0 -> "Tier 0"; case 1 -> "Tier I"; case 2 -> "Tier II"; case 3 -> "Tier III"; case 4 -> "Tier IV"; default -> "Tier ?"; }; }
}

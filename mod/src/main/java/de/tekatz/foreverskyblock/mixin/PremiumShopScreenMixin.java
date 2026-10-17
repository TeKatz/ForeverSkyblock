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
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Real current-world products and world upgrades integrated into the existing generic card panel. */
@Mixin(PremiumSkillScreen.class)
public abstract class PremiumShopScreenMixin {
    private static final Field PAGE = shopField("page"), SUBSECTIONS = shopField("subsections");
    private static final Constructor<?> CARD = shopCardConstructor();
    private static final Method CARD_PRODUCT = shopCardProduct();

    @Inject(method = "generalCards", at = @At("RETURN"))
    private void foreverSkyblock$addProducts(CallbackInfoReturnable<List<?>> callback) {
        if (!shopCurrentWorldShop()) return;
        List<?> raw = callback.getReturnValue();
        if (raw == null) return;
        @SuppressWarnings("unchecked") List<Object> cards = (List<Object>) raw;
        cards.removeIf(card -> ShopCatalog.TEST_WORLD_ENTITLEMENT.equals(shopProductId(card)));
        for (var product : ShopCatalog.PRODUCTS) {
            if (product.developmentOnly()) continue;
            try {
                String title = shopGerman() ? product.titleDe() : product.titleEn();
                String description = shopGerman() ? product.descriptionDe() : product.descriptionEn();
                String badge = product.worldUpgrade() ? product.spCost() + " SP · " + (shopGerman() ? "WELT-UPGRADE" : "WORLD UPGRADE")
                        : product.spCost() + " SP · " + shopTier(product.requiredTier());
                Object card = CARD.newInstance(Component.literal(title), Component.literal(description), Component.literal(badge),
                        shopIcon(product), "", product.id(), null, Component.literal(shopGerman() ? "KAUFEN" : "PURCHASE"));
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
        callback.setReturnValue(shopCanAct(snapshot, state, product));
    }

    @Inject(method = "productStatus", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$productStatus(String id, CallbackInfoReturnable<Component> callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || product.developmentOnly()) return;
        var snapshot = ClientSkills.snapshot();
        if (snapshot == null || !snapshot.available()) { callback.setReturnValue(Component.literal(shopGerman() ? "NICHT VERFÜGBAR" : "UNAVAILABLE")); return; }
        if (!snapshot.worldAvailable()) { callback.setReturnValue(Component.literal(shopGerman() ? "WELT NICHT VERFÜGBAR" : "WORLD UNAVAILABLE")); return; }
        SkillNetwork.ProductState state = snapshot.products().get(id);
        if (state == null || !state.available()) { callback.setReturnValue(Component.literal(shopGerman() ? "INTEGRATION FEHLT" : "INTEGRATION UNAVAILABLE")); return; }
        callback.setReturnValue(switch (state.stage()) {
            case 1 -> Component.literal(shopGerman() ? "PAKET ABHOLEN" : "CLAIM PACKAGE");
            case 2, 4 -> Component.literal(shopGerman() ? "WIEDERHERSTELLUNG…" : "RECOVERING…");
            case 3 -> Component.literal(shopGerman() ? "PAKET AUSGELIEFERT" : "PACKAGE DELIVERED");
            case 5 -> Component.literal(shopGerman() ? "PAKET GEÖFFNET" : "PACKAGE OPENED");
            case 6 -> Component.literal(product.worldUpgrade() ? (shopGerman() ? "AKTIV" : "ACTIVE") : (shopGerman() ? "BEANSPRUCHT" : "CLAIMED"));
            default -> {
                if (snapshot.shopTier() < product.requiredTier()) yield Component.literal((shopGerman() ? "BENÖTIGT " : "REQUIRES ") + shopTier(product.requiredTier()));
                if (snapshot.perkPoints() < product.ppCost()) yield Component.literal(shopGerman() ? "ZU WENIG PP" : "NOT ENOUGH PP");
                if (snapshot.points() < product.spCost()) yield Component.literal(shopGerman() ? "ZU WENIG SP" : "NOT ENOUGH SP");
                yield Component.literal(shopGerman() ? "KAUFEN" : "PURCHASE");
            }
        });
    }

    @Inject(method = "confirmProduct", at = @At("HEAD"), cancellable = true)
    private void foreverSkyblock$realProductAction(String id, CallbackInfo callback) {
        ShopCatalog.Product product = ShopCatalog.find(id);
        if (product == null || product.developmentOnly()) return;
        var snapshot = ClientSkills.snapshot();
        SkillNetwork.ProductState state = snapshot == null ? null : snapshot.products().get(id);
        if (!product.worldUpgrade() && state != null && state.stage() == 1 && !ClientSkills.pending()) {
            ClientSkills.claimProduct(id);
            callback.cancel();
            return;
        }
        if (shopCanPurchase(snapshot, state, product)) {
            Minecraft minecraft = Minecraft.getInstance();
            PremiumSkillScreen parent = (PremiumSkillScreen) (Object) this;
            String name = shopGerman() ? product.titleDe() : product.titleEn();
            Component title = Component.literal(product.worldUpgrade()
                    ? (shopGerman() ? "Welt-Upgrade aktivieren?" : "Activate world upgrade?")
                    : (shopGerman() ? "Weltpaket kaufen?" : "Purchase current-world package?"));
            Component description = Component.literal(product.worldUpgrade()
                    ? (shopGerman()
                        ? name + " für " + product.spCost() + " SP in „" + snapshot.worldName() + "“ aktivieren? Das Upgrade gilt dauerhaft für die gesamte Welt und ist nicht erstattbar."
                        : "Activate " + name + " for " + product.spCost() + " SP in “" + snapshot.worldName() + "”? The upgrade permanently affects the entire world and is non-refundable.")
                    : (shopGerman()
                        ? name + " für " + product.spCost() + " SP in „" + snapshot.worldName() + "“ kaufen? Der Kauf ist nicht erstattbar und gilt nur für diese Welt."
                        : "Buy " + name + " for " + product.spCost() + " SP in “" + snapshot.worldName() + "”? The purchase is non-refundable and applies only to this world."));
            Component confirm = Component.literal(product.worldUpgrade()
                    ? (shopGerman() ? "Aktivieren" : "Activate") : (shopGerman() ? "Kaufen" : "Purchase"));
            minecraft.setScreen(new ConfirmScreen(yes -> {
                minecraft.setScreen(parent);
                if (yes) ClientSkills.buyProduct(id);
            }, title, description, confirm, Component.translatable("gui.cancel")));
        }
        callback.cancel();
    }

    private static ItemStack shopIcon(ShopCatalog.Product product) {
        if (product.id().equals(ShopCatalog.NO_PHANTOMS.id())) return Items.PHANTOM_MEMBRANE.getDefaultInstance();
        if (product.id().equals(ShopCatalog.NO_RAIN.id())) return Items.WATER_BUCKET.getDefaultInstance();
        if (product.id().equals(ShopCatalog.PERMANENT_DAY.id())) return Items.SUNFLOWER.getDefaultInstance();
        return ForeverSkyblock.WORLD_SUPPLY_CACHE.get().getDefaultInstance();
    }

    private static boolean shopCanAct(SkillNetwork.Snapshot snapshot, SkillNetwork.ProductState state, ShopCatalog.Product product) {
        if (snapshot == null || !snapshot.available() || !snapshot.worldAvailable() || ClientSkills.pending()
                || state == null || !state.available()) return false;
        if (!product.worldUpgrade() && state.stage() == 1) return true;
        return shopCanPurchase(snapshot, state, product);
    }

    private static boolean shopCanPurchase(SkillNetwork.Snapshot snapshot, SkillNetwork.ProductState state, ShopCatalog.Product product) {
        return snapshot != null && snapshot.available() && snapshot.worldAvailable() && !ClientSkills.pending()
                && state != null && state.available() && state.stage() == 0
                && snapshot.shopTier() >= product.requiredTier() && snapshot.points() >= product.spCost()
                && snapshot.perkPoints() >= product.ppCost();
    }

    private boolean shopCurrentWorldShop() {
        try {
            Object page = PAGE.get(this); if (page == null || !"SHOP".equals(page.toString())) return false;
            @SuppressWarnings("unchecked") Map<Object, Integer> values = (Map<Object, Integer>) SUBSECTIONS.get(this);
            return values.getOrDefault(page, 0) == 1;
        } catch (IllegalAccessException e) { return false; }
    }
    private static String shopProductId(Object card) {
        try { return (String) CARD_PRODUCT.invoke(card); }
        catch (ReflectiveOperationException e) { return ""; }
    }
    private static Field shopField(String name) {
        try { Field field = PremiumSkillScreen.class.getDeclaredField(name); field.setAccessible(true); return field; }
        catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static Constructor<?> shopCardConstructor() {
        try {
            Class<?> card = Class.forName("de.tekatz.foreverskyblock.client.PremiumSkillScreen$Card");
            Constructor<?> ctor = card.getDeclaredConstructors()[0]; ctor.setAccessible(true); return ctor;
        } catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static Method shopCardProduct() {
        try {
            Class<?> card = Class.forName("de.tekatz.foreverskyblock.client.PremiumSkillScreen$Card");
            Method method = card.getDeclaredMethod("product"); method.setAccessible(true); return method;
        } catch (ReflectiveOperationException e) { throw new ExceptionInInitializerError(e); }
    }
    private static boolean shopGerman() { return Minecraft.getInstance().getLanguageManager().getSelected().toLowerCase(java.util.Locale.ROOT).startsWith("de_"); }
    private static String shopTier(int tier) { return switch (tier) { case 0 -> "Tier 0"; case 1 -> "Tier I"; case 2 -> "Tier II"; case 3 -> "Tier III"; case 4 -> "Tier IV"; default -> "Tier ?"; }; }
}

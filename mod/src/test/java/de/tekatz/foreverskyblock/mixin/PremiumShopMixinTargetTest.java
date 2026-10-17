package de.tekatz.foreverskyblock.mixin;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

import de.tekatz.foreverskyblock.client.PremiumSkillScreen;
import java.lang.reflect.Method;
import org.junit.jupiter.api.Test;
import org.spongepowered.asm.mixin.injection.Inject;

/** Guards client mixin target names that ordinary dedicated-server GameTests cannot validate. */
class PremiumShopMixinTargetTest {
    @Test
    void shopMixinTargetsMatchCurrentPremiumScreenMethods() throws Exception {
        assertNotNull(PremiumSkillScreen.class.getDeclaredMethod("generalCards"));
        assertNotNull(PremiumSkillScreen.class.getDeclaredMethod("canBuyProduct", String.class));
        assertNotNull(PremiumSkillScreen.class.getDeclaredMethod("productStatus", String.class));
        assertNotNull(PremiumSkillScreen.class.getDeclaredMethod("confirmProduct", String.class));

        Method addProducts = PremiumShopScreenMixin.class.getDeclaredMethod(
                "foreverSkyblock$addProducts",
                org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable.class);
        Inject inject = addProducts.getAnnotation(Inject.class);
        assertNotNull(inject, "Shop-card handler must remain an @Inject method");
        assertArrayEquals(new String[]{"generalCards"}, inject.method(),
                "Shop-card mixin must target the current card factory method");
    }
}

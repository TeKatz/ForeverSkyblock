package de.tekatz.foreverskyblock.mixin;

import static org.junit.jupiter.api.Assertions.assertTrue;

import java.nio.file.Files;
import java.nio.file.Path;
import org.junit.jupiter.api.Test;

/** Guards client mixin target names that ordinary dedicated-server GameTests cannot validate. */
class PremiumShopMixinTargetTest {
    @Test
    void shopMixinTargetsMatchCurrentPremiumScreenMethods() throws Exception {
        String screen = Files.readString(Path.of("src/main/java/de/tekatz/foreverskyblock/client/PremiumSkillScreen.java"));
        String mixin = Files.readString(Path.of("src/main/java/de/tekatz/foreverskyblock/mixin/PremiumShopScreenMixin.java"));

        assertTrue(screen.contains("private List<Card> generalCards()"), "PremiumSkillScreen must expose the current card factory");
        assertTrue(screen.contains("private boolean canBuyProduct(String id)"), "PremiumSkillScreen must expose product eligibility");
        assertTrue(screen.contains("private Component productStatus(String id)"), "PremiumSkillScreen must expose product status");
        assertTrue(screen.contains("private void confirmProduct(String id)"), "PremiumSkillScreen must expose product confirmation");

        assertTrue(mixin.contains("@Inject(method = \"generalCards\""),
                "PremiumShopScreenMixin must inject into the current card factory method");
        assertTrue(mixin.contains("@Inject(method = \"canBuyProduct\""),
                "PremiumShopScreenMixin must inject into product eligibility");
        assertTrue(mixin.contains("@Inject(method = \"productStatus\""),
                "PremiumShopScreenMixin must inject into product status");
        assertTrue(mixin.contains("@Inject(method = \"confirmProduct\""),
                "PremiumShopScreenMixin must inject into product confirmation");
    }
}

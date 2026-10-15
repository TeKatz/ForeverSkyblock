package de.tekatz.foreverskyblock;

import com.mojang.authlib.GameProfile;
import de.tekatz.foreverskyblock.progression.JetpackGate;
import de.tekatz.foreverskyblock.progression.JetpackResearch;
import de.tekatz.foreverskyblock.progression.ProgressionService;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import java.util.UUID;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.item.crafting.ShapedRecipe;
import net.neoforged.neoforge.common.CommonHooks;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class JetpackGameTests {
    @GameTest(template = "empty")
    public static void optionalModAndPrerequisitesAreEnforced(GameTestHelper h) {
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "ResearchTest"));
        var service = ProgressionService.get(player.server);
        service.grant(player, 100);
        service.buy(player, JetpackResearch.IDS.get(3));
        h.assertTrue(service.jetpackStage(player) == 0, "Cannot skip straight to Creative");
        service.buy(player, JetpackResearch.IDS.getFirst());
        h.assertTrue(service.jetpackStage(player) == (JetpackGate.installed() ? 1 : 0), "Missing optional mod cannot charge/unlock research");
        service.reset(player);
        h.assertTrue(service.jetpackStage(player) == 0, "Reset clears research");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void actualJetpacksComponentsRecipesAndFlightUnlockByStage(GameTestHelper h) {
        if (Boolean.getBoolean("forever_skyblock.expectJetpacks")) h.assertTrue(JetpackGate.installed(), "Integration run must actually load Iron Jetpacks");
        if (!JetpackGate.installed()) { h.succeed(); return; }
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "FlightTest"));
        var other = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "OtherTest"));
        var service = ProgressionService.get(player.server);
        service.grant(player, 100);
        var registry = com.blakebr0.ironjetpacks.registry.JetpackRegistry.getInstance();
        var wood = com.blakebr0.ironjetpacks.util.JetpackUtils.getItemForJetpack(registry.getJetpackById(ResourceLocation.parse("ironjetpacks:wood")));
        var creative = com.blakebr0.ironjetpacks.util.JetpackUtils.getItemForJetpack(registry.getJetpackById(ResourceLocation.parse("ironjetpacks:creative")));
        h.assertTrue(JetpackGate.requiredStage(wood) == 1 && JetpackGate.requiredStage(creative) == 4, "Component variants have independent requirements");
        player.setItemSlot(EquipmentSlot.CHEST, wood);
        h.assertTrue(com.blakebr0.ironjetpacks.util.JetpackUtils.getEquippedJetpack(player).isEmpty(), "Locked chest jetpack cannot power flight");
        CommonHooks.setCraftingPlayer(player);
        try {
            for (int stage = 0; stage <= 4; stage++) {
                int checked = 0;
                for (var holder : h.getLevel().getRecipeManager().getAllRecipesFor(RecipeType.CRAFTING)) {
                    var recipe = holder.value();
                    int needed = JetpackGate.requiredStage(recipe.getResultItem(h.getLevel().registryAccess()));
                    if (needed == 0 || !(recipe instanceof ShapedRecipe shaped)) continue;
                    var inputs = recipe.getIngredients().stream().map(i -> i.isEmpty() ? ItemStack.EMPTY : i.getItems().length == 0 ? ItemStack.EMPTY : i.getItems()[0].copy()).toList();
                    if (java.util.stream.IntStream.range(0, inputs.size()).anyMatch(i -> !recipe.getIngredients().get(i).isEmpty() && inputs.get(i).isEmpty())) continue;
                    var grid = CraftingInput.of(shaped.getWidth(), shaped.getHeight(), inputs);
                    h.assertTrue(recipe.matches(grid, h.getLevel()) == (stage >= needed), "Actual recipe gate at stage " + stage + ": " + holder.id());
                    checked++;
                }
                h.assertTrue(checked >= 20, "Check actual dynamic recipes and components, not only synthetic recipes");
                if (stage < 4) service.buy(player, JetpackResearch.IDS.get(stage));
            }
        } finally { CommonHooks.setCraftingPlayer(null); }
        h.assertTrue(service.jetpackStage(player) == 4, "All four research stages unlock");
        h.assertTrue(!com.blakebr0.ironjetpacks.util.JetpackUtils.getEquippedJetpack(player).isEmpty(), "Unlocked jetpack powers flight");
        other.setItemSlot(EquipmentSlot.CHEST, creative);
        h.assertTrue(com.blakebr0.ironjetpacks.util.JetpackUtils.getEquippedJetpack(other).isEmpty(), "Another player's research does not grant personal flight");
        h.assertTrue(h.getLevel().getRecipeManager().byKey(ResourceLocation.parse("forever_skyblock:creative_jetpack")).isPresent(), "Creative variant gets an actual crafting recipe");
        service.reset(player);
        h.assertTrue(com.blakebr0.ironjetpacks.util.JetpackUtils.getEquippedJetpack(player).isEmpty(), "Reset revokes already-equipped flight");
        h.assertTrue(JetpackGate.canUse(player, new ItemStack(Items.IRON_PICKAXE)), "Unrelated items remain usable");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void cachedAe2PatternsRelockAfterReset(GameTestHelper h) throws Exception {
        if (!Boolean.getBoolean("forever_skyblock.expectJetpacks")) { h.succeed(); return; }
        h.assertTrue(JetpackGate.installed() && net.neoforged.fml.ModList.get().isLoaded("ae2"), "Integration run requires both mods");
        // Force transformation of the manual terminal hook as well as the pattern implementation.
        Class.forName("appeng.menu.slot.CraftingTermSlot").getDeclaredMethods();
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "PatternTest"));
        var service = ProgressionService.get(player.server);
        service.grant(player, 100); service.buy(player, JetpackResearch.IDS.getFirst());
        CommonHooks.setCraftingPlayer(player);
        try {
            var holder = h.getLevel().getRecipeManager().getAllRecipesFor(RecipeType.CRAFTING).stream()
                    .filter(r -> r.id().equals(ResourceLocation.parse("ironjetpacks:strap"))).findFirst().orElseThrow();
            var inputs = holder.value().getIngredients().stream().map(i -> i.isEmpty() ? ItemStack.EMPTY : i.getItems()[0].copy()).toList();
            var grid = CraftingInput.of(3, 3, inputs);
            var output = holder.value().assemble(grid, h.getLevel().registryAccess());
            var encoded = appeng.api.crafting.PatternDetailsHelper.encodeCraftingPattern(holder, inputs.toArray(ItemStack[]::new), output, false, false);
            var pattern = new appeng.crafting.pattern.AECraftingPattern(appeng.api.stacks.AEItemKey.of(encoded), h.getLevel());
            h.assertTrue(!pattern.assemble(grid, h.getLevel()).isEmpty(), "Unlocked AE2 pattern crafts successfully");
            service.reset(player);
            h.assertTrue(pattern.assemble(grid, h.getLevel()).isEmpty(), "Previously cached AE2 pattern refuses locked output after reset");
            h.assertTrue(inputs.stream().filter(s -> !s.isEmpty()).allMatch(s -> s.getCount() == 1), "Gate does not consume input items");
        } finally { CommonHooks.setCraftingPlayer(null); }
        h.succeed();
    }

}

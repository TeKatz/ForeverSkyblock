package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.progression.WorldQoLEvents;
import de.tekatz.foreverskyblock.progression.WorldQoLService;
import de.tekatz.foreverskyblock.recipe.PackRecipes;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.item.crafting.RecipeType;
import net.neoforged.fml.ModList;
import net.neoforged.neoforge.event.entity.EntityJoinLevelEvent;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class GardenPackFixGameTests {
    @GameTest(template = "empty")
    public static void lockedTraderAlsoBlocksFreshTraderLlamas(GameTestHelper h) {
        var level = h.getLevel();
        h.assertTrue(!WorldQoLService.wanderingTraderAllowed(level.getServer()), "Fresh test world starts before Emerald unlock");
        var trader = new EntityJoinLevelEvent(EntityType.WANDERING_TRADER.create(level), level, false);
        WorldQoLEvents.entityJoins(trader);
        h.assertTrue(trader.isCanceled(), "Trader remains gated");
        var llama = EntityType.TRADER_LLAMA.create(level);
        var fresh = new EntityJoinLevelEvent(llama, level, false);
        WorldQoLEvents.entityJoins(fresh);
        h.assertTrue(fresh.isCanceled(), "Fresh trader llama must share the Emerald gate");
        var loaded = new EntityJoinLevelEvent(llama, level, true);
        WorldQoLEvents.entityJoins(loaded);
        h.assertTrue(!loaded.isCanceled(), "Existing saved trader llamas are preserved");
        llama.setTamed(true);
        var tamed = new EntityJoinLevelEvent(llama, level, false);
        WorldQoLEvents.entityJoins(tamed);
        h.assertTrue(!tamed.isCanceled(), "Owned trader llamas are preserved");
        var ordinary = new EntityJoinLevelEvent(EntityType.LLAMA.create(level), level, false);
        WorldQoLEvents.entityJoins(ordinary);
        h.assertTrue(!ordinary.isCanceled(), "Ordinary llamas must remain independent of the gate");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void woodenHopperUsesIronAndCannotRegainAlternativeRecipes(GameTestHelper h) {
        var manager = h.getLevel().getRecipeManager();
        boolean loaded = ModList.get().isLoaded("woodenhopper");
        h.assertTrue(loaded == Boolean.getBoolean("forever_skyblock.expectToolIntegrations"), "Runtime must exercise the expected optional Hopper dependency");
        if (!loaded) {
            h.assertTrue(manager.byKey(PackRecipes.HOPPER_RECIPE).isEmpty(), "Optional recipe must disappear without Wooden Hopper");
            h.succeed(); return;
        }
        var iron = new ItemStack(Items.IRON_INGOT);
        var wood = new ItemStack(Items.OAK_PLANKS);
        var chest = new ItemStack(Items.CHEST);
        var priced = CraftingInput.of(3, 3, List.of(iron, ItemStack.EMPTY, iron, wood, chest, wood, ItemStack.EMPTY, wood, ItemStack.EMPTY));
        var cheap = CraftingInput.of(3, 3, List.of(wood, ItemStack.EMPTY, wood, wood, chest, wood, ItemStack.EMPTY, wood, ItemStack.EMPTY));
        var match = manager.getRecipeFor(RecipeType.CRAFTING, priced, h.getLevel()).orElseThrow();
        h.assertTrue(match.id().equals(PackRecipes.HOPPER_RECIPE), "Iron corners must select the pack recipe");
        h.assertTrue(BuiltInRegistries.ITEM.getKey(match.value().assemble(priced, h.getLevel().registryAccess()).getItem()).equals(PackRecipes.HOPPER), "Priced recipe crafts the real Wooden Hopper");
        h.assertTrue(manager.getRecipesFor(RecipeType.CRAFTING, cheap, h.getLevel()).stream().noneMatch(r ->
                BuiltInRegistries.ITEM.getKey(r.value().getResultItem(h.getLevel().registryAccess()).getItem()).equals(PackRecipes.HOPPER)), "All-wood recipe must no longer craft a Hopper");
        var recipes = new ArrayList<RecipeHolder<?>>(manager.getRecipes());
        recipes.add(new RecipeHolder<>(ResourceLocation.parse("woodenhopper:test_alternative"), match.value()));
        manager.replaceRecipes(recipes);
        h.assertTrue(manager.byKey(ResourceLocation.parse("woodenhopper:test_alternative")).isEmpty(), "Recipe reload/replacement cannot reintroduce an alternative Hopper recipe");
        h.assertTrue(manager.byKey(PackRecipes.HOPPER_RECIPE).isPresent(), "The priced Hopper remains available after replacement");
        h.succeed();
    }
}

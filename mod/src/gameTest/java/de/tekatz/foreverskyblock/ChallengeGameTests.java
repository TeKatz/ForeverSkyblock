package de.tekatz.foreverskyblock;

import com.mojang.authlib.GameProfile;
import de.tekatz.foreverskyblock.progression.*;
import java.util.List;
import java.util.UUID;
import net.minecraft.advancements.CriteriaTriggers;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.inventory.ClickType;
import net.minecraft.world.inventory.CraftingMenu;
import net.minecraft.world.inventory.SmithingMenu;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.crafting.ShapedRecipe;
import net.minecraft.world.level.Level;
import net.neoforged.fml.loading.FMLPaths;
import net.neoforged.neoforge.common.NeoForge;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.event.entity.player.PlayerEvent;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ChallengeGameTests {
    private static ResourceLocation id(String path) { return ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, path); }
    private static ProgressionProfile profile(ServerPlayer player) throws Exception {
        return new ProfileStore(FMLPaths.GAMEDIR.get().resolve("forever_skyblock/progression")
                .resolve(player.server.isDedicatedServer() ? "server" : "local")).load(player.getUUID());
    }
    private static void craft(GameTestHelper h, ServerPlayer player, String path, boolean shift) {
        var recipe = h.getLevel().getRecipeManager().byKey(id(path)).orElseThrow().value();
        var menu = new CraftingMenu(1, player.getInventory()); player.containerMenu = menu;
        var ingredients = recipe.getIngredients();
        int recipeWidth = recipe instanceof ShapedRecipe shaped ? shaped.getWidth() : 3;
        for (int i=0;i<ingredients.size();i++) if (!ingredients.get(i).isEmpty()) {
            int slot = recipe instanceof ShapedRecipe ? (i / recipeWidth) * 3 + i % recipeWidth : i;
            menu.getSlot(slot+1).set(ingredients.get(i).getItems()[0].copy());
        }
        h.assertTrue(!menu.getSlot(0).getItem().isEmpty(), "Real crafting menu must resolve " + path);
        if (shift) menu.quickMoveStack(player, 0); else menu.clicked(0, 0, ClickType.PICKUP, player);
        h.assertTrue(menu.getSlot(0).getItem().isEmpty(), "Taking the output consumes the real recipe ingredients");
    }
    @GameTest(template="empty")
    public static void personalCraftsAndSmithingUseRealMenus(GameTestHelper h) throws Exception {
        var player=h.makeMockServerPlayerInLevel();
        craft(h,player,"cobblestone_from_fragments",false);
        h.assertTrue(profile(player).points()==1 && profile(player).meta().shopTier()==0,"Foundations keeps its one reward and unlocks Tier 0");
        craft(h,player,"stone_meal",true);
        h.assertTrue(profile(player).milestones().contains(ChallengeRules.STONEWORKER.id()),"Mortar craft counts on shift-click");
        for(String tool:List.of("pickaxe","axe","shovel","hoe")) craft(h,player,"tools/stone_"+tool,true);
        h.assertTrue(profile(player).milestones().contains(ChallengeRules.STONE_TOOLS.id()),"All four distinct Stone tools complete the set");
        for(String tool:List.of("pickaxe","axe","shovel","hoe")) craft(h,player,"tools/iron_"+tool,true);
        craft(h,player,"tools/diamond_pickaxe",true);
        var smith=new SmithingMenu(2,player.getInventory());player.containerMenu=smith;
        smith.getSlot(0).set(new ItemStack(Items.NETHERITE_UPGRADE_SMITHING_TEMPLATE));
        smith.getSlot(1).set(new ItemStack(Items.DIAMOND_PICKAXE));
        smith.getSlot(2).set(new ItemStack(ForeverSkyblock.REINFORCED_NETHERITE_INGOT.get()));
        h.assertTrue(smith.getSlot(3).getItem().is(Items.NETHERITE_PICKAXE),"Real smithing output resolves");
        h.assertTrue(!profile(player).milestones().contains(ChallengeRules.NETHERITE.id()),"Preview never counts");
        smith.quickMoveStack(player,3);
        h.assertTrue(profile(player).milestones().contains(ChallengeRules.NETHERITE.id()),"Smithing onTake produces personal evidence");
        h.assertTrue(profile(player).points()==15,"Only the six approved Forever challenges pay, including existing Foundations");
        h.succeed();
    }
    @GameTest(template="empty")
    public static void vanillaKillCreditAwardsOnlyActorAndDeduplicatesVictims(GameTestHelper h) throws Exception {
        var player=h.makeMockServerPlayerInLevel();var spectator=h.makeMockServerPlayerInLevel();
        var zombie=new net.minecraft.world.entity.monster.Zombie(EntityType.ZOMBIE,h.getLevel());
        zombie.hurt(player.damageSources().playerAttack(player),1000F);
        h.assertTrue(profile(player).meta().progress().get(ChallengeRules.VETERAN.id()).evidence().size()==1,"Actual lethal player damage counts a hostile kill");
        CriteriaTriggers.PLAYER_KILLED_ENTITY.trigger(player,zombie,player.damageSources().playerAttack(player));
        h.assertTrue(profile(player).meta().progress().get(ChallengeRules.VETERAN.id()).evidence().size()==1,"Duplicate victim evidence does not increment");
        var cow=EntityType.COW.create(h.getLevel()); cow.hurt(player.damageSources().playerAttack(player),1000F);
        h.assertTrue(profile(player).meta().progress().get(ChallengeRules.VETERAN.id()).evidence().size()==1,"Passive mobs do not count");
        var wither=EntityType.WITHER.create(h.getLevel());wither.hurt(player.damageSources().playerAttack(player),10000F);
        h.assertTrue(profile(player).milestones().contains(ChallengeRules.WITHER.id()),"Wither kill pays its unique reward");
        var dragon=EntityType.ENDER_DRAGON.create(h.getLevel());dragon.hurt(dragon.head,player.damageSources().playerAttack(player),10000F);
        h.assertTrue(profile(player).milestones().contains(ChallengeRules.DRAGON.id()),"Real Dragon lethal damage triggers personal credit despite its death animation");
        h.assertTrue(profile(player).meta().shopTier()==1 && profile(player).points()==11,"Dragon completion and Tier I commit together");
        h.assertTrue(profile(spectator).milestones().isEmpty() && profile(spectator).meta().progress().isEmpty(),"Nearby bystanders receive no credit");
        h.succeed();
    }
    @GameTest(template="empty")
    public static void dimensionsFakePlayersAndWrongRecipesAreGuarded(GameTestHelper h) throws Exception {
        var player=h.makeMockServerPlayerInLevel();
        NeoForge.EVENT_BUS.post(new PlayerEvent.PlayerChangedDimensionEvent(player,Level.OVERWORLD,Level.NETHER));
        NeoForge.EVENT_BUS.post(new PlayerEvent.PlayerChangedDimensionEvent(player,Level.NETHER,Level.END));
        NeoForge.EVENT_BUS.post(new PlayerEvent.PlayerChangedDimensionEvent(player,Level.OVERWORLD,Level.NETHER));
        h.assertTrue(profile(player).points()==5,"Dimension events pay once per personal claim");
        var fake=FakePlayerFactory.get(h.getLevel(),new GameProfile(UUID.randomUUID(),"NoChallenges"));
        var ingredients=List.of(new ItemStack(Items.NETHERITE_UPGRADE_SMITHING_TEMPLATE),new ItemStack(Items.DIAMOND_PICKAXE),new ItemStack(ForeverSkyblock.REINFORCED_NETHERITE_INGOT.get()));
        CriteriaTriggers.RECIPE_CRAFTED.trigger(fake,id("tools/netherite_pickaxe_smithing"),ingredients);
        CriteriaTriggers.PLAYER_KILLED_ENTITY.trigger(fake,EntityType.WITHER.create(h.getLevel()),fake.damageSources().playerAttack(fake));
        NeoForge.EVENT_BUS.post(new PlayerEvent.PlayerChangedDimensionEvent(fake,Level.OVERWORLD,Level.END));
        h.assertTrue(profile(fake).equals(ProgressionProfile.empty()),"Fake players cannot earn crafts, kills or dimensions");
        CriteriaTriggers.CRAFTER_RECIPE_CRAFTED.trigger(player,id("tools/netherite_pickaxe_smithing"),ingredients);
        ChallengeEvents.crafted(player,id("tools/netherite_pickaxe_smithing"),List.of(new ItemStack(Items.NETHERITE_INGOT)));
        ChallengeEvents.crafted(player,ResourceLocation.parse("minecraft:netherite_pickaxe_smithing"),ingredients);
        h.assertTrue(profile(player).points()==5,"Automated crafting, wrong ingredients and alternative IDs never award");
        h.succeed();
    }
    @GameTest(template="empty")
    public static void ae2PersonalCraftingUsesTheSameChallengeAdapter(GameTestHelper h) throws Exception {
        if(!net.neoforged.fml.ModList.get().isLoaded("ae2")){h.succeed();return;}
        var player=h.makeMockServerPlayerInLevel();
        // Instantiate the actual optional AE2 slot only in its presence, keeping baseline tests loadable.
        Class<?> invClass=Class.forName("appeng.util.inv.AppEngInternalInventory");
        Object inv=invClass.getConstructor(int.class).newInstance(9);
        var set=invClass.getMethod("setItemDirect",int.class,ItemStack.class);
        set.invoke(inv,0,new ItemStack(ForeverSkyblock.MORTAR.get()));
        for(int i=1;i<=4;i++)set.invoke(inv,i,new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get()));
        Class<?> slotClass=Class.forName("appeng.menu.slot.AppEngCraftingSlot");
        Object slot=slotClass.getConstructor(net.minecraft.world.entity.player.Player.class,Class.forName("appeng.api.inventories.InternalInventory")).newInstance(player,inv);
        slotClass.getMethod("setRecipeUsed",net.minecraft.world.item.crafting.RecipeHolder.class).invoke(slot,h.getLevel().getRecipeManager().byKey(id("stone_meal")).orElseThrow());
        slotClass.getMethod("onTake",net.minecraft.world.entity.player.Player.class,ItemStack.class).invoke(slot,player,new ItemStack(ForeverSkyblock.STONE_MEAL.get()));
        h.assertTrue(profile(player).milestones().contains(ChallengeRules.STONEWORKER.id()),"Actual AE2 onTake emits personal recipe trigger");
        h.succeed();
    }
}

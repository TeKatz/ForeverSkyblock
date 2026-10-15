package de.tekatz.foreverskyblock;

import com.mojang.authlib.GameProfile;
import de.tekatz.foreverskyblock.tools.ToolPolicy;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicInteger;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.component.DataComponents;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.core.registries.Registries;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.chat.Component;
import net.minecraft.network.protocol.game.ClientboundUpdateRecipesPacket;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.context.UseOnContext;
import net.minecraft.world.item.crafting.CraftingInput;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.item.crafting.ShapedRecipe;
import net.minecraft.world.item.crafting.ShapelessRecipe;
import net.minecraft.world.item.crafting.SmithingRecipeInput;
import net.minecraft.world.item.crafting.SmithingTransformRecipe;
import net.minecraft.world.item.crafting.CraftingBookCategory;
import net.minecraft.world.item.enchantment.Enchantments;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.Vec3;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class ToolGameTests {
    private static final String[] TOOLS = {"pickaxe", "axe", "shovel", "hoe"};
    private static final String[] TIERS = {"wooden", "stone", "iron", "golden", "diamond", "netherite"};
    private static final String[][] PATTERNS = {{"XXX", "XTX", " C "}, {"XX ", "XTX", " C "}, {" X ", "XTX", " C "}, {"XX ", " TX", " C "}};
    private static final int[] COUNTS = {5, 4, 3, 3};
    private static ResourceLocation id(String path) { return ResourceLocation.withDefaultNamespace(path); }
    private static Item item(String path) { return BuiltInRegistries.ITEM.get(id(path)); }
    private static CraftingInput grid(String[] pattern, Item material, Item core, Item reinforcement) {
        var stacks = new ArrayList<ItemStack>();
        for (String row : pattern) for (char c : row.toCharArray()) stacks.add(switch (c) {
            case 'X' -> new ItemStack(material); case 'T' -> new ItemStack(core); case 'C' -> new ItemStack(reinforcement); default -> ItemStack.EMPTY;
        });
        return CraftingInput.of(3, 3, stacks);
    }
    private static Item material(int tier) { return switch (tier) { case 0 -> Items.OAK_PLANKS; case 1 -> ForeverSkyblock.STONE_FRAGMENT.get(); case 2 -> Items.IRON_INGOT; case 3 -> Items.GOLD_INGOT; default -> Items.DIAMOND; }; }
    private static Item reinforcement(int tier) { return switch (tier) { case 0 -> ForeverSkyblock.STONE_FRAGMENT.get(); case 1 -> ForeverSkyblock.STONE_MEAL.get(); case 2 -> Items.CHAIN; case 3 -> Items.REDSTONE; default -> Items.OBSIDIAN; }; }
    private static Item core(int tier, String tool) { return tier == 0 ? Items.STICK : item((tier == 1 ? "wooden" : tier == 4 ? "iron" : "stone") + "_" + tool); }
    private static void check(GameTestHelper h, boolean condition, String message) { h.assertTrue(condition, message); }

    @GameTest(template = "empty")
    public static void everyShapedTierHasExactCostsAndNoVanillaAlternative(GameTestHelper h) {
        var manager = h.getLevel().getRecipeManager();
        for (int tier = 0; tier < 5; tier++) for (int tool = 0; tool < 4; tool++) {
            String name = TIERS[tier] + "_" + TOOLS[tool];
            var holder = manager.byKey(id(name)).orElseThrow();
            check(h, holder.value() instanceof ShapedRecipe, "Use standard editable shaped recipe: " + name);
            var recipe = (ShapedRecipe) holder.value();
            var input = grid(PATTERNS[tool], material(tier), core(tier, TOOLS[tool]), reinforcement(tier));
            check(h, recipe.matches(input, h.getLevel()), "Exact handoff pattern matches: " + name + " size=" + recipe.getWidth() + "x" + recipe.getHeight() + " expected=" + input.items() + " actual=" + recipe.getIngredients().stream().map(g -> java.util.Arrays.toString(g.getItems())).toList());
            var result = recipe.assemble(input, h.getLevel().registryAccess());
            check(h, result.is(item(name)) && result.getCount() == 1 && !result.isDamageableItem(), "One permanent tool per recipe");
            Item headMaterial = material(tier);
            long head = input.items().stream().filter(s -> s.is(headMaterial)).count();
            check(h, head == COUNTS[tool], "Exact primary material count");
            check(h, recipe.getRemainingItems(input).stream().allMatch(ItemStack::isEmpty), "Prior-tier core is consumed, never returned");
            String[] vanilla = switch (tool) { case 0 -> new String[]{"XXX", " T ", " T "}; case 1 -> new String[]{"XX ", "XT ", " T "}; case 2 -> new String[]{" X ", " T ", " T "}; default -> new String[]{"XX ", " T ", " T "}; };
            Item oldMaterial = tier == 1 ? Items.COBBLESTONE : material(tier);
            var cheap = grid(vanilla, oldMaterial, Items.STICK, Items.AIR);
            check(h, manager.getRecipesFor(RecipeType.CRAFTING, cheap, h.getLevel()).stream()
                    .noneMatch(r -> r.value().getResultItem(h.getLevel().registryAccess()).is(item(name))), "Old cheap recipe is gone: " + name);
        }
        // Plank tags must accept another Vanilla wood type rather than only oak.
        var birch = grid(PATTERNS[0], Items.BIRCH_PLANKS, Items.STICK, ForeverSkyblock.STONE_FRAGMENT.get());
        check(h, manager.getRecipeFor(RecipeType.CRAFTING, birch, h.getLevel()).orElseThrow().id().equals(id("wooden_pickaxe")), "Plank tag resolves across wood variants");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void sidegradeAndDiamondGateUseExactPriorTiers(GameTestHelper h) {
        var manager = h.getLevel().getRecipeManager();
        for (int i = 0; i < 4; i++) {
            var gold = (ShapedRecipe) manager.byKey(id("golden_" + TOOLS[i])).orElseThrow().value();
            check(h, gold.matches(grid(PATTERNS[i], Items.GOLD_INGOT, item("stone_" + TOOLS[i]), Items.REDSTONE), h.getLevel()), "Gold branches from Stone: " + TOOLS[i] + " actual=" + gold.getIngredients().stream().map(g -> java.util.Arrays.toString(g.getItems())).toList());
            check(h, !gold.matches(grid(PATTERNS[i], Items.GOLD_INGOT, item("iron_" + TOOLS[i]), Items.REDSTONE), h.getLevel()), "Gold does not consume Iron");
            var diamond = (ShapedRecipe) manager.byKey(id("diamond_" + TOOLS[i])).orElseThrow().value();
            check(h, diamond.matches(grid(PATTERNS[i], Items.DIAMOND, item("iron_" + TOOLS[i]), Items.OBSIDIAN), h.getLevel()), "Diamond requires Iron and Obsidian");
            check(h, !diamond.matches(grid(PATTERNS[i], Items.DIAMOND, item("golden_" + TOOLS[i]), Items.OBSIDIAN), h.getLevel()), "Gold is never required for Diamond");
            check(h, !diamond.matches(grid(PATTERNS[i], Items.DIAMOND, item("iron_" + TOOLS[i]), Items.AIR), h.getLevel()), "No Diamond without the Obsidian gate");
        }
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void reinforcedIngotAndSmithingPreserveState(GameTestHelper h) {
        var manager = h.getLevel().getRecipeManager();
        var input = grid(new String[]{"CXC", "TDT", "CXC"}, Items.NETHERITE_INGOT, Items.GOLD_INGOT, Items.OBSIDIAN);
        var stacks = new ArrayList<>(input.items()); stacks.set(4, new ItemStack(Items.DIAMOND));
        var reinforced = manager.getRecipeFor(RecipeType.CRAFTING, CraftingInput.of(3, 3, stacks), h.getLevel()).orElseThrow();
        check(h, reinforced.value().assemble(CraftingInput.of(3, 3, stacks), h.getLevel().registryAccess()).is(ForeverSkyblock.REINFORCED_NETHERITE_INGOT.get()), "Exact 2 netherite / 4 obsidian / 2 gold / 1 diamond recipe");
        for (String tool : TOOLS) {
            var base = new ItemStack(item("diamond_" + tool));
            base.set(DataComponents.CUSTOM_NAME, Component.literal("Permanent " + tool));
            base.set(DataComponents.REPAIR_COST, 7);
            var tag = new net.minecraft.nbt.CompoundTag(); tag.putString("test_marker", "preserve");
            base.set(DataComponents.CUSTOM_DATA, net.minecraft.world.item.component.CustomData.of(tag));
            base.enchant(h.getLevel().registryAccess().lookupOrThrow(Registries.ENCHANTMENT).getOrThrow(Enchantments.EFFICIENCY), 4);
            var upgrade = new SmithingRecipeInput(new ItemStack(Items.NETHERITE_UPGRADE_SMITHING_TEMPLATE), base, new ItemStack(ForeverSkyblock.REINFORCED_NETHERITE_INGOT.get()));
            var recipe = manager.getRecipeFor(RecipeType.SMITHING, upgrade, h.getLevel()).orElseThrow();
            check(h, recipe.id().equals(id("netherite_" + tool + "_smithing")), "Keep Vanilla smithing ID");
            var result = recipe.value().assemble(upgrade, h.getLevel().registryAccess());
            check(h, result.is(item("netherite_" + tool)) && !result.isDamageableItem(), "Permanent Netherite result");
            for (var component : List.of(DataComponents.CUSTOM_NAME, DataComponents.REPAIR_COST, DataComponents.CUSTOM_DATA, DataComponents.ENCHANTMENTS))
                check(h, java.util.Objects.equals(base.get(component), result.get(component)), "Smithing preserves " + component);
            var cheap = new SmithingRecipeInput(upgrade.template(), base, new ItemStack(Items.NETHERITE_INGOT));
            check(h, manager.getRecipeFor(RecipeType.SMITHING, cheap, h.getLevel()).isEmpty(), "Old one-ingot smithing is gone");
        }
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void allToolsIgnoreDamageAndExcludedEquipmentStillWears(GameTestHelper h) {
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "Durability"));
        var broken = new AtomicInteger();
        int checked = 0;
        for (String tier : TIERS) for (String tool : TOOLS) {
            var stack = new ItemStack(item(tier + "_" + tool));
            check(h, stack.has(DataComponents.UNBREAKABLE) && !stack.isDamageableItem(), "Default components identify permanent tools");
            stack.enchant(h.getLevel().registryAccess().lookupOrThrow(Registries.ENCHANTMENT).getOrThrow(Enchantments.UNBREAKING), 3);
            stack.enchant(h.getLevel().registryAccess().lookupOrThrow(Registries.ENCHANTMENT).getOrThrow(Enchantments.MENDING), 1);
            stack.hurtAndBreak(100000, h.getLevel(), player, ignored -> broken.incrementAndGet());
            stack.setDamageValue(100000);
            check(h, stack.getCount() == 1 && stack.getDamageValue() == 0 && !stack.isDamaged(), "Damage hooks cannot break or wear the tool");
            stack.remove(DataComponents.UNBREAKABLE);
            stack.set(DataComponents.DAMAGE, 100000);
            check(h, !stack.isDamageableItem() && stack.getDamageValue() == 0, "Older/component-patched tools remain protected");
            var saved = stack.save(h.getLevel().registryAccess());
            var restored = ItemStack.parse(h.getLevel().registryAccess(), saved).orElseThrow();
            check(h, restored.getCount() == 1 && restored.getDamageValue() == 0, "Protection survives NBT/component round trip");
            checked++;
        }
        check(h, checked == 24 && broken.get() == 0, "Exactly the approved tools, no break callback");
        for (Item excluded : List.of(Items.IRON_SWORD, Items.SHEARS, Items.FISHING_ROD, Items.BRUSH, Items.FLINT_AND_STEEL, Items.BOW, Items.SHIELD, Items.IRON_CHESTPLATE)) {
            var stack = new ItemStack(excluded); stack.setDamageValue(3);
            check(h, stack.isDamageableItem() && stack.getDamageValue() == 3, "Open follow-up equipment retains normal durability");
        }
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void actualMiningCombatAndToolActionsDoNotWearTools(GameTestHelper h) {
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "ToolActions"));
        var relative = new BlockPos(2, 2, 2); var pos = h.absolutePos(relative);
        player.setPos(pos.getX() + 0.5, pos.getY() + 1, pos.getZ() + 0.5);
        var pick = new ItemStack(Items.IRON_PICKAXE); player.setItemSlot(EquipmentSlot.MAINHAND, pick);
        h.setBlock(relative, Blocks.STONE); check(h, player.gameMode.destroyBlock(pos), "Real server mining succeeds");
        check(h, h.getBlockState(relative).isAir() && pick.getDamageValue() == 0 && pick.getCount() == 1, "Mining has no wear");
        var target = new net.minecraft.world.entity.monster.Zombie(net.minecraft.world.entity.EntityType.ZOMBIE, h.getLevel());
        pick.hurtEnemy(target, player);
        pick.postHurtEnemy(target, player);
        check(h, pick.getDamageValue() == 0 && pick.getCount() == 1, "Tool combat has no wear");
        for (Item tool : List.of(Items.IRON_HOE, Items.IRON_SHOVEL, Items.IRON_AXE)) {
            var stack = new ItemStack(tool); player.setItemSlot(EquipmentSlot.MAINHAND, stack);
            h.setBlock(relative, tool == Items.IRON_AXE ? Blocks.OAK_LOG : Blocks.DIRT);
            var hit = new BlockHitResult(Vec3.atCenterOf(pos), Direction.UP, pos, false);
            check(h, stack.useOn(new UseOnContext(player, InteractionHand.MAIN_HAND, hit)).consumesAction(), "Actual tool action succeeds");
            check(h, stack.getDamageValue() == 0 && stack.getCount() == 1, "Hoeing/path/stripping do not wear tools");
        }
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void alternativeOutputsAreRemovedAndRecipesSync(GameTestHelper h) {
        var manager = new RecipeManager(h.getLevel().registryAccess());
        var all = new ArrayList<RecipeHolder<?>>(h.getLevel().getRecipeManager().getRecipes());
        var inputs = net.minecraft.core.NonNullList.of(Ingredient.EMPTY, Ingredient.of(Items.STICK));
        all.add(new RecipeHolder<>(ResourceLocation.parse("test:cheap_pick"), new ShapelessRecipe("", CraftingBookCategory.EQUIPMENT, new ItemStack(Items.DIAMOND_PICKAXE), inputs)));
        all.add(new RecipeHolder<>(ResourceLocation.parse("test:cheap_netherite"), new SmithingTransformRecipe(Ingredient.of(Items.STICK), Ingredient.of(Items.STICK), Ingredient.of(Items.STICK), new ItemStack(Items.NETHERITE_AXE))));
        manager.replaceRecipes(all);
        check(h, manager.byKey(ResourceLocation.parse("test:cheap_pick")).isEmpty() && manager.byKey(ResourceLocation.parse("test:cheap_netherite")).isEmpty(), "Remove mod/datapack alternatives from the authoritative recipe manager");
        check(h, manager.getRecipes().stream().filter(r -> ToolPolicy.includes(r.value().getResultItem(h.getLevel().registryAccess()))).count() == 24, "Exactly 24 authoritative tool recipes remain");
        var bytes = new RegistryFriendlyByteBuf(io.netty.buffer.Unpooled.buffer(), h.getLevel().registryAccess());
        try {
            ClientboundUpdateRecipesPacket.STREAM_CODEC.encode(bytes, new ClientboundUpdateRecipesPacket(manager.getRecipes()));
            var decoded = ClientboundUpdateRecipesPacket.STREAM_CODEC.decode(bytes);
            check(h, decoded.getRecipes().stream().filter(r -> ToolPolicy.includes(r.value().getResultItem(h.getLevel().registryAccess()))).count() == 24, "Client recipe sync contains only intended tool recipes");
        } finally { bytes.release(); }
        var repair = h.getLevel().getRecipeManager().getRecipeFor(RecipeType.CRAFTING,
                CraftingInput.of(2, 1, List.of(new ItemStack(Items.IRON_PICKAXE), new ItemStack(Items.IRON_PICKAXE))), h.getLevel());
        check(h, repair.isEmpty(), "No special crafting repair path for permanent tools");
        h.succeed();
    }

    @GameTest(template = "empty")
    public static void actualUltimineDispatchKeepsToolAndHarvestEligibility(GameTestHelper h) throws Exception {
        boolean installed = net.neoforged.fml.ModList.get().isLoaded("ftbultimine");
        if (Boolean.getBoolean("forever_skyblock.expectToolIntegrations")) check(h, installed, "Integration run must load real Ultimine");
        if (!installed) { h.succeed(); return; }
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "UltimineTool"));
        var stack = new ItemStack(Items.DIAMOND_PICKAXE); player.setItemSlot(EquipmentSlot.MAINHAND, stack);
        var relative = new BlockPos(2, 2, 2); var pos = h.absolutePos(relative);
        player.setPos(pos.getX() + 0.5, pos.getY() + 1, pos.getZ() + 0.5);
        Class<?> ultimine = Class.forName("dev.ftb.mods.ftbultimine.FTBUltimine");
        var valid = ultimine.getMethod("isValidTool", net.minecraft.world.entity.player.Player.class, BlockPos.class, net.minecraft.world.level.block.state.BlockState.class);
        check(h, (boolean) valid.invoke(null, player, pos, Blocks.STONE.defaultBlockState()), "Infinite durability must not disable Ultimine's tool requirement");
        Class<?> shapeType = Class.forName("dev.ftb.mods.ftbultimine.api.shape.Shape");
        var shape = Class.forName("dev.ftb.mods.ftbultimine.shape.ShapelessShape").getConstructor().newInstance();
        var breaker = ultimine.getDeclaredMethod("tryBreakBlock", net.minecraft.server.level.ServerPlayer.class, BlockPos.class, net.minecraft.world.level.block.state.BlockState.class, shapeType, BlockHitResult.class);
        breaker.setAccessible(true);
        for (int i = 0; i < 64; i++) {
            h.setBlock(relative, Blocks.STONE);
            check(h, (boolean) breaker.invoke(null, player, pos, h.getBlockState(relative), shape, new BlockHitResult(Vec3.atCenterOf(pos), Direction.UP, pos, false)), "Actual Ultimine block dispatch breaks stone");
        }
        check(h, stack.getCount() == 1 && stack.getDamageValue() == 0, "Repeated Ultimine dispatch never damages or consumes the pickaxe");
        h.succeed();
    }
}

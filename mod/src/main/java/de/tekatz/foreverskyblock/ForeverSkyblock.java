package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.block.CrucibleBlock;
import de.tekatz.foreverskyblock.block.FreezingChamberBlock;
import de.tekatz.foreverskyblock.block.GardenChamberBlock;
import de.tekatz.foreverskyblock.block.WashingBasinBlock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
import de.tekatz.foreverskyblock.blockentity.GardenChamberBlockEntity;
import de.tekatz.foreverskyblock.blockentity.WashingBasinBlockEntity;
import de.tekatz.foreverskyblock.item.MortarItem;
import de.tekatz.foreverskyblock.item.CondenserUpgradeCardItem;
import de.tekatz.foreverskyblock.item.SupplyCacheItem;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.menu.CrucibleMenu;
import de.tekatz.foreverskyblock.menu.WashingMenu;
import de.tekatz.foreverskyblock.network.CondenserNetwork;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import de.tekatz.foreverskyblock.recipe.GardenRecipe;
import de.tekatz.foreverskyblock.recipe.WashingRecipe;
import net.minecraft.core.registries.Registries;
import net.minecraft.world.flag.FeatureFlags;
import net.minecraft.world.inventory.MenuType;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.CreativeModeTabs;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.Rarity;
import net.minecraft.world.item.crafting.RecipeSerializer;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.SoundType;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.level.block.state.BlockBehaviour;
import net.minecraft.world.level.material.MapColor;
import net.neoforged.bus.api.IEventBus;
import net.neoforged.fml.common.Mod;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.capabilities.RegisterCapabilitiesEvent;
import net.neoforged.neoforge.event.BuildCreativeModeTabContentsEvent;
import net.neoforged.neoforge.registries.DeferredBlock;
import net.neoforged.neoforge.registries.DeferredHolder;
import net.neoforged.neoforge.registries.DeferredItem;
import net.neoforged.neoforge.registries.DeferredRegister;

@Mod(ForeverSkyblock.MOD_ID)
public final class ForeverSkyblock {
    public static final String MOD_ID = "forever_skyblock";
    private static final DeferredRegister.Blocks BLOCKS = DeferredRegister.createBlocks(MOD_ID);
    private static final DeferredRegister.Items ITEMS = DeferredRegister.createItems(MOD_ID);
    private static final DeferredRegister<BlockEntityType<?>> BLOCK_ENTITIES = DeferredRegister.create(Registries.BLOCK_ENTITY_TYPE, MOD_ID);
    private static final DeferredRegister<MenuType<?>> MENUS = DeferredRegister.create(Registries.MENU, MOD_ID);
    private static final DeferredRegister<RecipeType<?>> RECIPE_TYPES = DeferredRegister.create(Registries.RECIPE_TYPE, MOD_ID);
    private static final DeferredRegister<RecipeSerializer<?>> RECIPE_SERIALIZERS = DeferredRegister.create(Registries.RECIPE_SERIALIZER, MOD_ID);

    public static final DeferredBlock<de.tekatz.foreverskyblock.block.AscensionChamberBlock> ASCENSION_CHAMBER = BLOCKS.register("ascension_chamber",
            () -> new de.tekatz.foreverskyblock.block.AscensionChamberBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_PURPLE)
                    .strength(4.0F, 10.0F).sound(SoundType.METAL).requiresCorrectToolForDrops().noOcclusion()
                    .lightLevel(state -> state.getValue(de.tekatz.foreverskyblock.block.AscensionChamberBlock.ACTIVE) ? 8 : 0)));
    public static final DeferredItem<BlockItem> ASCENSION_CHAMBER_ITEM = ITEMS.registerSimpleBlockItem(ASCENSION_CHAMBER);
    public static final DeferredItem<Item> FOREVER_CORE = ITEMS.register("forever_core", () -> new Item(new Item.Properties().stacksTo(1).fireResistant().rarity(Rarity.EPIC)));
    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity>> ASCENSION_ENTITY =
            BLOCK_ENTITIES.register("ascension_chamber", () -> BlockEntityType.Builder.of(de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity::new, ASCENSION_CHAMBER.get()).build(null));
    public static final DeferredHolder<MenuType<?>, MenuType<de.tekatz.foreverskyblock.menu.AscensionMenu>> ASCENSION_MENU =
            MENUS.register("ascension_chamber", () -> new MenuType<>(de.tekatz.foreverskyblock.menu.AscensionMenu::new, FeatureFlags.DEFAULT_FLAGS));

    public static final DeferredBlock<CondenserBlock> CONDENSER_MARK_I = BLOCKS.register("condenser_mark_i",
            () -> new CondenserBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_GRAY)
                    .strength(2.0F, 6.0F).sound(SoundType.METAL).requiresCorrectToolForDrops().noOcclusion()
                    .lightLevel(state -> state.getValue(CondenserBlock.ACTIVE) ? 5 : 0)));
    public static final DeferredItem<BlockItem> CONDENSER_MARK_I_ITEM = ITEMS.registerSimpleBlockItem(CONDENSER_MARK_I);
    public static final DeferredBlock<CondenserBlock> CONDENSER_MARK_II = BLOCKS.register("condenser_mark_ii",
            () -> new CondenserBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_GRAY)
                    .strength(3.0F, 8.0F).sound(SoundType.METAL).requiresCorrectToolForDrops().noOcclusion()
                    .lightLevel(state -> state.getValue(CondenserBlock.ACTIVE) ? 7 : 0)));
    public static final DeferredItem<BlockItem> CONDENSER_MARK_II_ITEM = ITEMS.registerSimpleBlockItem(CONDENSER_MARK_II);
    public static final DeferredBlock<Block> MIXING_CHAMBER_MARK_I = BLOCKS.registerSimpleBlock("mixing_chamber_mark_i",
            BlockBehaviour.Properties.of().mapColor(MapColor.STONE).strength(2.0F, 6.0F).sound(SoundType.STONE).requiresCorrectToolForDrops().noOcclusion());
    public static final DeferredItem<BlockItem> MIXING_CHAMBER_MARK_I_ITEM = ITEMS.registerSimpleBlockItem(MIXING_CHAMBER_MARK_I);
    public static final DeferredBlock<FreezingChamberBlock> FREEZING_CHAMBER_MARK_I = BLOCKS.register("freezing_chamber_mark_i",
            () -> new FreezingChamberBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_LIGHT_BLUE)
                    .strength(2.0F, 6.0F).sound(SoundType.METAL).requiresCorrectToolForDrops().noOcclusion()));
    public static final DeferredItem<BlockItem> FREEZING_CHAMBER_MARK_I_ITEM = ITEMS.registerSimpleBlockItem(FREEZING_CHAMBER_MARK_I);
    // Keep the existing registry id for save compatibility; the player-facing name is simply "Garden Chamber".
    public static final DeferredBlock<GardenChamberBlock> GARDEN_CHAMBER_MARK_I = BLOCKS.register("garden_chamber_mark_i",
            () -> new GardenChamberBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_GREEN).strength(3.0F, 8.0F)
                    .sound(SoundType.METAL).requiresCorrectToolForDrops().noOcclusion()));
    public static final DeferredItem<BlockItem> GARDEN_CHAMBER_MARK_I_ITEM = ITEMS.registerSimpleBlockItem(GARDEN_CHAMBER_MARK_I);
    public static final DeferredBlock<WashingBasinBlock> WASHING_BASIN = BLOCKS.register("washing_basin",
            () -> new WashingBasinBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_BROWN)
                    .strength(2.5F, 6.0F).sound(SoundType.STONE).requiresCorrectToolForDrops().noOcclusion()));
    public static final DeferredItem<BlockItem> WASHING_BASIN_ITEM = ITEMS.registerSimpleBlockItem(WASHING_BASIN);
    public static final DeferredBlock<CrucibleBlock> CRUCIBLE = BLOCKS.register("crucible",
            () -> new CrucibleBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_BLACK)
                    .strength(3.0F, 8.0F).sound(SoundType.DEEPSLATE).requiresCorrectToolForDrops().noOcclusion()));
    public static final DeferredItem<BlockItem> CRUCIBLE_ITEM = ITEMS.registerSimpleBlockItem(CRUCIBLE);

    public static final DeferredItem<Item> REINFORCED_NETHERITE_INGOT = ITEMS.registerSimpleItem("reinforced_netherite_ingot");
    public static final DeferredItem<Item> STONE_MEAL = ITEMS.registerSimpleItem("stone_meal");
    public static final DeferredItem<MortarItem> MORTAR = ITEMS.register("mortar", () -> new MortarItem(new Item.Properties().stacksTo(1)));
    public static final DeferredItem<Item> STONE_FRAGMENT = ITEMS.registerSimpleItem("stone_fragment");
    public static final DeferredItem<CondenserUpgradeCardItem> PROCESSING_SPEED_CARD = ITEMS.register("processing_speed_card",
            () -> new CondenserUpgradeCardItem(CondenserUpgradeCardItem.Kind.SPEED, new Item.Properties()));
    public static final DeferredItem<CondenserUpgradeCardItem> GARDEN_ACCELERATOR_CARD = ITEMS.register("garden_accelerator_card",
            () -> new CondenserUpgradeCardItem(CondenserUpgradeCardItem.Kind.GARDEN, new Item.Properties()));
    public static final DeferredItem<CondenserUpgradeCardItem> BUFFER_EXPANSION_CARD = ITEMS.register("buffer_expansion_card",
            () -> new CondenserUpgradeCardItem(CondenserUpgradeCardItem.Kind.BUFFER, new Item.Properties()));
    public static final DeferredItem<CondenserUpgradeCardItem> BATCH_PROCESSING_CARD = ITEMS.register("batch_processing_card",
            () -> new CondenserUpgradeCardItem(CondenserUpgradeCardItem.Kind.BATCH, new Item.Properties()));
    /** Delivery carrier is intentionally absent from creative tabs; only a committed shop receipt can issue one. */
    public static final DeferredItem<SupplyCacheItem> WORLD_SUPPLY_CACHE = ITEMS.register("world_supply_cache",
            () -> new SupplyCacheItem(new Item.Properties().stacksTo(1).rarity(Rarity.UNCOMMON)));

    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<CondenserBlockEntity>> CONDENSER_ENTITY =
            BLOCK_ENTITIES.register("condenser_mark_i", () -> BlockEntityType.Builder.of(CondenserBlockEntity::new,
                    CONDENSER_MARK_I.get(), CONDENSER_MARK_II.get()).build(null));
    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<GardenChamberBlockEntity>> GARDEN_CHAMBER_ENTITY =
            BLOCK_ENTITIES.register("garden_chamber", () -> BlockEntityType.Builder.of(GardenChamberBlockEntity::new, GARDEN_CHAMBER_MARK_I.get()).build(null));
    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<WashingBasinBlockEntity>> WASHING_BASIN_ENTITY =
            BLOCK_ENTITIES.register("washing_basin", () -> BlockEntityType.Builder.of(WashingBasinBlockEntity::new, WASHING_BASIN.get()).build(null));
    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<CrucibleBlockEntity>> CRUCIBLE_ENTITY =
            BLOCK_ENTITIES.register("crucible", () -> BlockEntityType.Builder.of(CrucibleBlockEntity::new, CRUCIBLE.get()).build(null));
    public static final DeferredHolder<MenuType<?>, MenuType<CondenserMenu>> CONDENSER_MENU =
            MENUS.register("condenser_mark_i", () -> new MenuType<>(CondenserMenu::new, FeatureFlags.DEFAULT_FLAGS));
    public static final DeferredHolder<MenuType<?>, MenuType<WashingMenu>> WASHING_BASIN_MENU =
            MENUS.register("washing_basin", () -> new MenuType<>(WashingMenu::new, FeatureFlags.DEFAULT_FLAGS));
    public static final DeferredHolder<MenuType<?>, MenuType<CrucibleMenu>> CRUCIBLE_MENU =
            MENUS.register("crucible", () -> new MenuType<>(CrucibleMenu::new, FeatureFlags.DEFAULT_FLAGS));

    public static final DeferredHolder<RecipeType<?>, RecipeType<CondenserRecipe>> CONDENSING =
            RECIPE_TYPES.register("condensing", () -> new RecipeType<>() {
                @Override public String toString() { return MOD_ID + ":condensing"; }
            });
    public static final DeferredHolder<RecipeSerializer<?>, RecipeSerializer<CondenserRecipe>> CONDENSING_SERIALIZER =
            RECIPE_SERIALIZERS.register("condensing", CondenserRecipe.Serializer::new);
    public static final DeferredHolder<RecipeType<?>, RecipeType<GardenRecipe>> GARDEN =
            RECIPE_TYPES.register("garden", () -> new RecipeType<>() {
                @Override public String toString() { return MOD_ID + ":garden"; }
            });
    public static final DeferredHolder<RecipeSerializer<?>, RecipeSerializer<GardenRecipe>> GARDEN_SERIALIZER =
            RECIPE_SERIALIZERS.register("garden", GardenRecipe.Serializer::new);
    public static final DeferredHolder<RecipeType<?>, RecipeType<WashingRecipe>> WASHING =
            RECIPE_TYPES.register("washing", () -> new RecipeType<>() {
                @Override public String toString() { return MOD_ID + ":washing"; }
            });
    public static final DeferredHolder<RecipeSerializer<?>, RecipeSerializer<WashingRecipe>> WASHING_SERIALIZER =
            RECIPE_SERIALIZERS.register("washing", WashingRecipe.Serializer::new);

    public ForeverSkyblock(IEventBus modBus) {
        BLOCKS.register(modBus);
        ITEMS.register(modBus);
        BLOCK_ENTITIES.register(modBus);
        MENUS.register(modBus);
        RECIPE_TYPES.register(modBus);
        RECIPE_SERIALIZERS.register(modBus);
        modBus.addListener(ForeverSkyblock::addCreativeContents);
        modBus.addListener(ForeverSkyblock::registerCapabilities);
        modBus.addListener(SkillNetwork::register);
        modBus.addListener(de.tekatz.foreverskyblock.network.EditorNetwork::register);
        modBus.addListener(CondenserNetwork::register);
        modBus.addListener(de.tekatz.foreverskyblock.tools.ToolPolicy::modifyComponents);
    }

    private static void registerCapabilities(RegisterCapabilitiesEvent event) {
        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, ASCENSION_ENTITY.get(), de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity::automation);
        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, CONDENSER_ENTITY.get(),
                (machine, side) -> de.tekatz.foreverskyblock.blockentity.CondenserAutomationHandler.create(machine, side));
        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, GARDEN_CHAMBER_ENTITY.get(), GardenChamberBlockEntity::automation);
        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, WASHING_BASIN_ENTITY.get(), WashingBasinBlockEntity::automation);
        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, CRUCIBLE_ENTITY.get(), CrucibleBlockEntity::automation);
        event.registerBlockEntity(Capabilities.FluidHandler.BLOCK, CRUCIBLE_ENTITY.get(), CrucibleBlockEntity::fluidAutomation);
    }

    private static void addCreativeContents(BuildCreativeModeTabContentsEvent event) {
        if (event.getTabKey() == CreativeModeTabs.FUNCTIONAL_BLOCKS) {
            event.accept(ASCENSION_CHAMBER_ITEM.get());
            event.accept(CONDENSER_MARK_I_ITEM.get());
            event.accept(CONDENSER_MARK_II_ITEM.get());
            event.accept(MIXING_CHAMBER_MARK_I_ITEM.get());
            event.accept(FREEZING_CHAMBER_MARK_I_ITEM.get());
            event.accept(GARDEN_CHAMBER_MARK_I_ITEM.get());
            event.accept(WASHING_BASIN_ITEM.get());
            event.accept(CRUCIBLE_ITEM.get());
        }
        if (event.getTabKey() == CreativeModeTabs.INGREDIENTS) {
            event.accept(de.tekatz.foreverskyblock.item.ForeverCore.create());
            event.accept(STONE_FRAGMENT.get());
            event.accept(STONE_MEAL.get());
            event.accept(MORTAR.get());
            event.accept(REINFORCED_NETHERITE_INGOT.get());
            event.accept(PROCESSING_SPEED_CARD.get());
            event.accept(GARDEN_ACCELERATOR_CARD.get());
            event.accept(BUFFER_EXPANSION_CARD.get());
            event.accept(BATCH_PROCESSING_CARD.get());
        }
    }
}

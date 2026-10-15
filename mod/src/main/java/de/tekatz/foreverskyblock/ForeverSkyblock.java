package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.block.CondenserBlock;
import de.tekatz.foreverskyblock.blockentity.CondenserBlockEntity;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import net.minecraft.core.registries.Registries;
import net.minecraft.world.flag.FeatureFlags;
import net.minecraft.world.inventory.MenuType;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.CreativeModeTabs;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.crafting.RecipeSerializer;
import net.minecraft.world.item.crafting.RecipeType;
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

    public static final DeferredBlock<CondenserBlock> CONDENSER_MARK_I = BLOCKS.register("condenser_mark_i",
            () -> new CondenserBlock(BlockBehaviour.Properties.of().mapColor(MapColor.COLOR_GRAY)
                    .strength(2.0F, 6.0F).sound(SoundType.METAL).requiresCorrectToolForDrops()
                    .lightLevel(state -> state.getValue(CondenserBlock.ACTIVE) ? 5 : 0)));
    public static final DeferredItem<BlockItem> CONDENSER_MARK_I_ITEM = ITEMS.registerSimpleBlockItem(CONDENSER_MARK_I);
    public static final DeferredItem<Item> STONE_FRAGMENT = ITEMS.registerSimpleItem("stone_fragment");
    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<CondenserBlockEntity>> CONDENSER_ENTITY =
            BLOCK_ENTITIES.register("condenser_mark_i", () -> BlockEntityType.Builder.of(CondenserBlockEntity::new, CONDENSER_MARK_I.get()).build(null));
    public static final DeferredHolder<MenuType<?>, MenuType<CondenserMenu>> CONDENSER_MENU =
            MENUS.register("condenser_mark_i", () -> new MenuType<>(CondenserMenu::new, FeatureFlags.DEFAULT_FLAGS));
    public static final DeferredHolder<RecipeType<?>, RecipeType<CondenserRecipe>> CONDENSING =
            RECIPE_TYPES.register("condensing", () -> new RecipeType<>() {
                @Override public String toString() { return MOD_ID + ":condensing"; }
            });
    public static final DeferredHolder<RecipeSerializer<?>, RecipeSerializer<CondenserRecipe>> CONDENSING_SERIALIZER =
            RECIPE_SERIALIZERS.register("condensing", CondenserRecipe.Serializer::new);

    public ForeverSkyblock(IEventBus modBus) {
        BLOCKS.register(modBus);
        ITEMS.register(modBus);
        BLOCK_ENTITIES.register(modBus);
        MENUS.register(modBus);
        RECIPE_TYPES.register(modBus);
        RECIPE_SERIALIZERS.register(modBus);
        modBus.addListener(ForeverSkyblock::addCreativeContents);
        modBus.addListener(ForeverSkyblock::registerCapabilities);
    }

    private static void registerCapabilities(RegisterCapabilitiesEvent event) {
        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, CONDENSER_ENTITY.get(), CondenserBlockEntity::automation);
    }

    private static void addCreativeContents(BuildCreativeModeTabContentsEvent event) {
        if (event.getTabKey() == CreativeModeTabs.FUNCTIONAL_BLOCKS) event.accept(CONDENSER_MARK_I_ITEM.get());
        if (event.getTabKey() == CreativeModeTabs.INGREDIENTS) event.accept(STONE_FRAGMENT.get());
    }
}

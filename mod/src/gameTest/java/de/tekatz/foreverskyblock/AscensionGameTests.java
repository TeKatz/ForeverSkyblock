package de.tekatz.foreverskyblock;

import com.google.gson.JsonParser;
import com.mojang.authlib.GameProfile;
import de.tekatz.foreverskyblock.block.AscensionChamberBlock;
import de.tekatz.foreverskyblock.blockentity.AscensionChamberBlockEntity;
import de.tekatz.foreverskyblock.item.ForeverCore;
import de.tekatz.foreverskyblock.menu.AscensionMenu;
import de.tekatz.foreverskyblock.progression.*;
import io.netty.buffer.Unpooled;
import java.util.*;
import net.minecraft.core.*;
import net.minecraft.gametest.framework.*;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.resources.RegistryOps;
import net.minecraft.world.item.*;
import net.minecraft.world.item.crafting.*;
import net.minecraft.world.level.block.Blocks;
import net.neoforged.neoforge.capabilities.Capabilities;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.*;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class AscensionGameTests {
    @GameTest(template = "empty")
    public static void failedStorageRetainsCoreAndRetryCompletesOnce(GameTestHelper h) throws Exception {
        var machine = place(h); var owner = player(h, "StorageOwner");
        var file = net.neoforged.fml.loading.FMLPaths.GAMEDIR.get().resolve("forever_skyblock/progression/server/" + owner.getUUID() + ".json");
        java.nio.file.Files.createDirectories(file);
        try {
            machine.inventory().setStackInSlot(0, ForeverCore.create()); machine.start(owner);
            for (int i = 0; i < 200; i++) machine.advance(owner);
            h.assertTrue(!machine.inventory().getStackInSlot(0).isEmpty() && !machine.busy()
                    && machine.menuData().get(1) == AscensionChamberBlockEntity.STORAGE_ERROR, "Failed persistence retains the Core and stops automatic retries");
        } finally { java.nio.file.Files.delete(file); }
        machine.start(owner);
        for (int i = 0; i < 200; i++) machine.advance(owner);
        h.assertTrue(machine.inventory().getStackInSlot(0).isEmpty() && ProgressionService.get(owner.server).perkPoints(owner) == 1,
                "Repairing storage and explicitly retrying finishes the existing claim once");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void breakingActiveChamberReturnsOneCoreAndNoPoint(GameTestHelper h) throws Exception {
        var machine = place(h); var owner = player(h, "BreakOwner");
        var core = ForeverCore.create(); var identity = ForeverCore.identity(core);
        machine.inventory().setStackInSlot(0, core); machine.start(owner); machine.advance(owner);
        var pos = machine.getBlockPos(); h.getLevel().destroyBlock(pos, false);
        var drops = h.getLevel().getEntitiesOfClass(net.minecraft.world.entity.item.ItemEntity.class,
                new net.minecraft.world.phys.AABB(pos).inflate(1), e -> e.getItem().is(ForeverSkyblock.FOREVER_CORE.get()));
        h.assertTrue(drops.size() == 1 && identity.equals(ForeverCore.identity(drops.getFirst().getItem()))
                && ProgressionService.get(owner.server).perkPoints(owner) == 0, "Breaking a running Chamber returns the same Core exactly once and awards no PP");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void actualCoreRecipeAutomatesWithCrafterAndCannotReuseAe2CachedIdentity(GameTestHelper h) throws Exception {
        if (!Boolean.getBoolean("forever_skyblock.expectAscension")) { h.succeed(); return; }
        var holder = h.getLevel().getRecipeManager().byKey(net.minecraft.resources.ResourceLocation.parse("forever_skyblock:forever_core")).orElseThrow();
        var recipe = (ShapedRecipe) holder.value();
        var pos = new BlockPos(4, 1, 4); h.setBlock(pos, Blocks.CRAFTER);
        var absolute = h.absolutePos(pos);
        var crafter = (net.minecraft.world.level.block.entity.CrafterBlockEntity) h.getBlockEntity(pos);
        var inputs = recipe.getIngredients().stream().map(i -> i.getItems()[0].copyWithCount(1)).toList();
        for (int i = 0; i < 9; i++) crafter.setItem(i, inputs.get(i).copyWithCount(2));
        var dispense = net.minecraft.world.level.block.CrafterBlock.class.getDeclaredMethod("dispenseFrom",
                net.minecraft.world.level.block.state.BlockState.class, net.minecraft.server.level.ServerLevel.class, BlockPos.class);
        dispense.setAccessible(true);
        dispense.invoke(Blocks.CRAFTER, h.getLevel().getBlockState(absolute), h.getLevel(), absolute);
        dispense.invoke(Blocks.CRAFTER, h.getLevel().getBlockState(absolute), h.getLevel(), absolute);
        var outputs = h.getLevel().getEntitiesOfClass(net.minecraft.world.entity.item.ItemEntity.class,
                new net.minecraft.world.phys.AABB(absolute).inflate(3), e -> e.getItem().is(ForeverSkyblock.FOREVER_CORE.get()));
        h.assertTrue(outputs.size() == 2 && !ForeverCore.identity(outputs.get(0).getItem()).equals(ForeverCore.identity(outputs.get(1).getItem())), "Two real Crafter operations produce two independent Cores");
        for (int i = 0; i < 9; i++) h.assertTrue(crafter.getItem(i).isEmpty(), "Crafter consumes both complete recipe sets");
        var typed = new RecipeHolder<CraftingRecipe>(holder.id(), recipe);
        var grid = CraftingInput.of(3, 3, inputs);
        var encoded = appeng.api.crafting.PatternDetailsHelper.encodeCraftingPattern(typed, inputs.toArray(ItemStack[]::new),
                recipe.assemble(grid, h.getLevel().registryAccess()), false, false);
        var pattern = new appeng.crafting.pattern.AECraftingPattern(appeng.api.stacks.AEItemKey.of(encoded), h.getLevel());
        h.assertTrue(pattern.assemble(grid, h.getLevel()).isEmpty(), "Cached Core patterns fail before the Molecular Assembler consumes inputs");
        h.succeed();
    }
    private static AscensionChamberBlockEntity place(GameTestHelper h) {
        h.setBlock(new BlockPos(1, 1, 1), ForeverSkyblock.ASCENSION_CHAMBER.get());
        return (AscensionChamberBlockEntity) h.getLevel().getBlockEntity(h.absolutePos(new BlockPos(1, 1, 1)));
    }
    private static net.minecraft.server.level.ServerPlayer player(GameTestHelper h, String name) {
        var p = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), name));
        var pos = h.absolutePos(new BlockPos(1, 1, 1)); p.setPos(pos.getX() + 0.5, pos.getY() + 0.5, pos.getZ() + 0.5); return p;
    }
    @GameTest(template = "empty")
    public static void coreRecipeIsShapedUniqueSerializableAndConditionallyPresent(GameTestHelper h) {
        var json = JsonParser.parseString("""
                {"category":"misc","pattern":["NNN","NSN","NNN"],
                 "key":{"N":{"item":"minecraft:netherite_ingot"},"S":{"item":"minecraft:nether_star"}},
                 "result":{"id":"forever_skyblock:forever_core","count":1}}
                """);
        var ops = RegistryOps.create(com.mojang.serialization.JsonOps.INSTANCE, h.getLevel().registryAccess());
        var serializer = new ShapedRecipe.Serializer();
        var recipe = serializer.codec().codec().parse(ops, json).getOrThrow();
        var items = new ArrayList<ItemStack>(); for (int i = 0; i < 9; i++) items.add(new ItemStack(i == 4 ? Items.NETHER_STAR : Items.NETHERITE_INGOT));
        var grid = CraftingInput.of(3, 3, items);
        h.assertTrue(recipe instanceof ShapedRecipe && recipe.matches(grid, h.getLevel()), "Core uses the standard shaped recipe contract for JEI and automation");
        var first = recipe.assemble(grid, h.getLevel().registryAccess()); var second = recipe.assemble(grid, h.getLevel().registryAccess());
        h.assertTrue(ForeverCore.identity(first) != null && !ForeverCore.identity(first).equals(ForeverCore.identity(second)), "Every assembly creates a unique receipt");
        h.assertTrue(first.getMaxStackSize() == 1 && ForeverCore.identity(recipe.getResultItem(h.getLevel().registryAccess())) == null, "Display-only previews have no redeemable identity");
        var buffer = new RegistryFriendlyByteBuf(Unpooled.buffer(), h.getLevel().registryAccess());
        try {
            serializer.streamCodec().encode(buffer, recipe); var decoded = serializer.streamCodec().decode(buffer);
            h.assertTrue(decoded.matches(grid, h.getLevel()) && ForeverCore.identity(decoded.assemble(grid, h.getLevel().registryAccess())) != null, "Recipe sync retains assembly behavior");
        } finally { buffer.release(); }
        h.assertTrue(serializer.codec().codec().encodeStart(ops, recipe).isSuccess(), "Recipe survives JSON encoding");
        boolean available = java.util.stream.Stream.of("mekanism:pellet_antimatter", "powah:crystal_nitro", "ae2:singularity")
                .allMatch(id -> net.minecraft.core.registries.BuiltInRegistries.ITEM.containsKey(net.minecraft.resources.ResourceLocation.parse(id)));
        if (Boolean.getBoolean("forever_skyblock.expectAscension")) h.assertTrue(available, "Pack integration must load the actual Mekanism, Powah and AE2 items");
        h.assertTrue(h.getLevel().getRecipeManager().byKey(net.minecraft.resources.ResourceLocation.parse("forever_skyblock:forever_core")).isPresent() == available, "Missing optional recipe ingredients disable only the Core recipe");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void machineBindsRecipientLocksAutomationAndCompletesOnlyOnce(GameTestHelper h) throws Exception {
        var machine = place(h); var owner = player(h, "CoreOwner"); var other = player(h, "CoreOther");
        var core = ForeverCore.create(); var identity = ForeverCore.identity(core);
        var cap = h.getLevel().getCapability(Capabilities.ItemHandler.BLOCK, machine.getBlockPos(), Direction.UP);
        h.assertTrue(cap != null && cap.insertItem(0, core, true).isEmpty() && cap.getStackInSlot(0).isEmpty(), "Hopper simulation does not consume Core");
        h.assertTrue(!cap.insertItem(0, new ItemStack(Items.DIRT), false).isEmpty(), "Reject unrelated items");
        cap.insertItem(0, core, false);
        var menu = new AscensionMenu(17, owner.getInventory(), machine);
        h.assertTrue(menu.clickMenuButton(owner, 0) && !menu.clickMenuButton(other, 0), "Only the initial Start binds the player");
        h.assertTrue(menu.owner().equals(owner.getUUID()) && menu.busy(), "Full owner UUID round trips through 16-bit menu data");
        h.assertTrue(cap.extractItem(0, 1, false).isEmpty() && !menu.getSlot(0).mayPickup(other) && menu.quickMoveStack(owner, 0).isEmpty(), "Busy input is locked for pipes and shift-click");
        machine.advance(other); h.assertTrue(menu.progress() == 0, "Other player cannot advance or receive the conversion");
        for (int i = 0; i < AscensionChamberBlockEntity.DURATION; i++) machine.advance(owner);
        var service = ProgressionService.get(owner.server);
        h.assertTrue(service.perkPoints(owner) == 1 && service.perkPoints(other) == 0, "Only the initiator receives one PP");
        h.assertTrue(machine.inventory().getStackInSlot(0).isEmpty() && !machine.busy() && menu.status() == AscensionChamberBlockEntity.COMPLETE, "Exactly one Core consumed and input unlocked");
        h.assertTrue(service.redeemCore(owner, identity) == CoreRedemptions.Result.ALREADY_CREDITED, "Repeated request never pays twice");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void savedOperationAndRolledBackCoreCannotPayTwice(GameTestHelper h) throws Exception {
        var machine = place(h); var owner = player(h, "ReplayOwner");
        machine.inventory().setStackInSlot(0, ForeverCore.create()); machine.start(owner);
        for (int i = 0; i < 60; i++) machine.advance(owner);
        var saved = machine.saveWithoutMetadata(h.getLevel().registryAccess());
        machine.loadWithComponents(saved, h.getLevel().registryAccess());
        h.assertTrue(machine.menuData().get(0) == 60 && machine.busy(), "Restart preserves progress and recipient");
        for (int i = 0; i < 140; i++) machine.advance(owner);
        h.assertTrue(ProgressionService.get(owner.server).perkPoints(owner) == 1, "Resumed conversion pays once");
        machine.loadWithComponents(saved, h.getLevel().registryAccess());
        for (int i = 0; i < 140; i++) machine.advance(owner);
        h.assertTrue(machine.inventory().getStackInSlot(0).isEmpty() && ProgressionService.get(owner.server).perkPoints(owner) == 1, "World rollback consumes the already-redeemed Core without another point");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void invalidCopiedAndClaimedCoresHaveSafeBehavior(GameTestHelper h) throws Exception {
        var machine = place(h); var owner = player(h, "ClaimOwner"); var other = player(h, "ClaimOther");
        machine.inventory().setStackInSlot(0, new ItemStack(ForeverSkyblock.FOREVER_CORE.get()));
        h.assertTrue(!machine.start(owner) && !machine.inventory().getStackInSlot(0).isEmpty(), "A cheated preview with no crafting identity cannot generate points");
        var core = ForeverCore.create(); var service = ProgressionService.get(owner.server);
        service.redeemCore(other, ForeverCore.identity(core)); machine.inventory().setStackInSlot(0, core); machine.start(owner);
        for (int i = 0; i < 200; i++) machine.advance(owner);
        h.assertTrue(service.perkPoints(owner) == 0 && service.perkPoints(other) == 1 && !machine.inventory().getStackInSlot(0).isEmpty(), "Another player's claimed Core cannot transfer a second point");
        h.assertTrue(!machine.busy() && machine.menuData().get(1) == AscensionChamberBlockEntity.CLAIMED, "Claim failure leaves a recoverable item");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void offlinePausesDistanceRejectsAndFullCubeNeighborsRemainSolid(GameTestHelper h) {
        var machine = place(h); var owner = player(h, "OfflineOwner");
        var menu = new AscensionMenu(18, owner.getInventory(), machine);
        machine.inventory().setStackInSlot(0, ForeverCore.create()); owner.setPos(100000, 100, 100000);
        h.assertTrue(!menu.clickMenuButton(owner, 0) && !machine.busy(), "Remote menu button cannot start a conversion");
        owner.setPos(machine.getBlockPos().getX(), machine.getBlockPos().getY(), machine.getBlockPos().getZ());
        h.assertTrue(menu.clickMenuButton(owner, 0), "Nearby initiator can start");
        AscensionChamberBlockEntity.tick(h.getLevel(), machine.getBlockPos(), machine.getBlockState(), machine);
        h.assertTrue(menu.status() == AscensionChamberBlockEntity.OFFLINE && menu.progress() == 0, "An initiator absent from the online list pauses processing");
        h.setBlock(new BlockPos(2, 1, 1), Blocks.OAK_LOG);
        h.assertTrue(machine.getBlockState().canOcclude() && machine.getBlockState().isCollisionShapeFullBlock(h.getLevel(), machine.getBlockPos()), "Closed cube can legitimately cull only its shared full faces");
        h.assertTrue(!machine.getBlockState().getValue(AscensionChamberBlock.ACTIVE), "Paused machine has no processing light");
        h.succeed();
    }
}

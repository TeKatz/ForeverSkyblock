package de.tekatz.foreverskyblock;

import de.tekatz.foreverskyblock.editor.*;
import de.tekatz.foreverskyblock.network.EditorNetwork;
import de.tekatz.foreverskyblock.progression.*;
import io.netty.buffer.Unpooled;
import java.util.*;
import net.minecraft.gametest.framework.*;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.neoforged.neoforge.gametest.*;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class EditorGameTests {
    @GameTest(template="empty") public static void editorPacketsFitTheirDirectionsAndRoundTrip(GameTestHelper h){
        var b=new RegistryFriendlyByteBuf(Unpooled.buffer(),h.getLevel().registryAccess());
        try{var r=new EditorNetwork.Request(1,UUID.randomUUID(),0,2,7,"\u754c".repeat(EditorNetwork.CHUNK));EditorNetwork.Request.CODEC.encode(b,r);h.assertTrue(b.readableBytes()<32767,"Largest UTF-8 upload chunk fits the C2S limit");h.assertTrue(r.equals(EditorNetwork.Request.CODEC.decode(b)),"Upload metadata round trips");b.clear();var reply=new EditorNetwork.Reply(0,7,EditorCodec.WIRE.toJson(EditorDefaults.create()),"",true,true);EditorNetwork.Reply.CODEC.encode(b,reply);h.assertTrue(reply.equals(EditorNetwork.Reply.CODEC.decode(b)),"Published catalog round trips");h.succeed();}finally{b.release();}
    }
    @GameTest(template="empty") public static void editorDoesNotAcceptUnknownOrOversizedRequests(GameTestHelper h){
        h.assertTrue(rejected(()->new EditorNetwork.Request(4,UUID.randomUUID(),0,1,0,"")),"Unknown actions rejected");h.assertTrue(rejected(()->new EditorNetwork.Request(1,UUID.randomUUID(),0,37,0,"")),"Bounded assembly");h.assertTrue(rejected(()->new EditorNetwork.Request(1,UUID.randomUUID(),0,1,0,"x".repeat(EditorNetwork.CHUNK+1))),"Oversized chunk rejected");h.assertTrue(rejected(()->new EditorNetwork.Reply(0,-1,"","",false,false)),"Negative revisions rejected");h.succeed();
    }
    @GameTest(template="empty") public static void emptyEditorConfigRetainsExistingGameplayDefinitions(GameTestHelper h){var d=EditorService.get(h.getLevel().getServer()).document();h.assertTrue(d.quest(ChallengeRules.FOUNDATIONS.id()).rewardSp()==1,"Foundation reward unchanged");h.assertTrue(ShopCatalog.find(ShopCatalog.ADVANCED_AE2_STARTER.id()).spCost()==12,"Existing package price unchanged");h.assertTrue(ContractCatalog.find(ContractCatalog.WITHER_HUNTER.id()).target()==3,"Existing contract count unchanged");h.succeed();}
    @GameTest(template="empty") public static void nonAdministratorCannotPublishAnEditorDocument(GameTestHelper h){
        var player=net.neoforged.neoforge.common.util.FakePlayerFactory.get(h.getLevel(),new com.mojang.authlib.GameProfile(UUID.randomUUID(),"EditorNoPermission"));
        h.assertTrue(!ProgressionService.canTestPoints(player),"Fixture has no administrator rights");var service=EditorService.get(player.server);var previous=service.document();
        service.request(player,new EditorNetwork.Request(1,UUID.randomUUID(),0,1,previous.revision(),"{}"));h.assertTrue(previous.equals(service.document()),"An unauthorized payload cannot mutate published definitions");h.succeed();
    }
    @GameTest(template="empty") public static void authoredPackageAndContractUseExistingDurableTransactions(GameTestHelper h)throws Exception{
        var old=EditorRuntime.active();var d=EditorDefaults.create();var json=EditorCodec.JSON.toJsonTree(d).getAsJsonObject();
        String quest="forever_skyblock:quest/integration",product="forever_skyblock:shop/custom/integration",contract="forever_skyblock:contract/custom/integration";
        var q=new EditorDocument.Quest(quest,true,new EditorDocument.Text("Integration","Integration"),new EditorDocument.Text("Craft bricks","Ziegel herstellen"),"minecraft:mud_bricks","forever",List.of(new EditorDocument.Task("craft",EditorDocument.TaskType.CRAFT,"minecraft:mud_bricks",1)),EditorDocument.Match.ALL,List.of(),EditorDocument.Match.ALL,-1,3,1);
        json.getAsJsonArray("quests").add(EditorCodec.JSON.toJsonTree(q));
        var p=new EditorDocument.Product(product,true,new EditorDocument.Text("Package","Paket"),new EditorDocument.Text("Eight bricks","Acht Ziegel"),"minecraft:chest",0,2,1,List.of(quest),List.of(new EditorDocument.Stack("minecraft:mud_bricks",8)));
        json.getAsJsonArray("products").add(EditorCodec.JSON.toJsonTree(p));
        json.getAsJsonArray("contracts").add(EditorCodec.JSON.toJsonTree(new EditorDocument.Contract(contract,true,new EditorDocument.Text("Contract","Vertrag"),new EditorDocument.Text("Submit cobblestone","Bruchstein abgeben"),"minecraft:cobblestone",EditorDocument.ContractType.RESOURCE,"minecraft:cobblestone",4,2,0)));
        var authored=EditorCodec.decode(json.toString());EditorCodec.validateUpdate(d,authored);
        try{
            EditorRuntime.publish(authored);
            var player=net.neoforged.neoforge.common.util.FakePlayerFactory.get(h.getLevel(),new com.mojang.authlib.GameProfile(UUID.randomUUID(),"EditorTransactions"));player.getInventory().clearContent();
            var root=java.nio.file.Files.createTempDirectory("editor-transactions-");var store=new ProfileStore(root.resolve("profiles"));
            var constructor=ProgressionService.class.getDeclaredConstructor(ProfileStore.class,boolean.class,java.nio.file.Path.class,String.class);constructor.setAccessible(true);var service=constructor.newInstance(store,false,root.resolve("world"),"Editor transactions");
            store.update(player.getUUID(),state->state.grant(10).award(ChallengeRules.FOUNDATIONS.id(),1));
            service.buyProduct(player,product);h.assertTrue(store.load(player.getUUID()).points()==11,"Unmet custom quest blocks the purchase without charging");
            store.update(player.getUUID(),state->QuestRules.record(state,authored,new QuestRules.Event(EditorDocument.TaskType.CRAFT,"minecraft:mud_bricks",1,"")));
            service.buyProduct(player,product);h.assertTrue(store.load(player.getUUID()).points()==12&&store.load(player.getUUID()).perkPoints()==0,"Authored SP/PP prices are charged once");
            var disabled=EditorCodec.JSON.toJsonTree(authored).getAsJsonObject();for(var e:disabled.getAsJsonArray("products"))if(e.getAsJsonObject().get("id").getAsString().equals(product))e.getAsJsonObject().addProperty("enabled",false);EditorRuntime.publish(EditorCodec.decode(disabled.toString()));
            service.claimProduct(player,product);net.minecraft.world.item.ItemStack cache=null;for(int i=0;i<36;i++)if(de.tekatz.foreverskyblock.item.SupplyCacheItem.marker(player.getInventory().getItem(i))!=null){cache=player.getInventory().getItem(i);break;}
            h.assertTrue(cache!=null,"Disabling an offer does not revoke a paid package");var duplicate=cache.copy();service.openSupplyCache(player,cache);h.assertTrue(ProgressionService.inventoryCount(player,"minecraft:mud_bricks")==8,"Authored package contents are delivered");service.openSupplyCache(player,duplicate);h.assertTrue(ProgressionService.inventoryCount(player,"minecraft:mud_bricks")==8,"Replayed cache does not deliver twice");
            player.getInventory().add(new net.minecraft.world.item.ItemStack(net.minecraft.world.item.Items.COBBLESTONE,4));var request=UUID.randomUUID();service.submitContract(player,contract,request);h.assertTrue(store.load(player.getUUID()).points()==14&&ProgressionService.inventoryCount(player,"minecraft:cobblestone")==0,"Authored contract consumes resources and pays once");
            player.getInventory().add(new net.minecraft.world.item.ItemStack(net.minecraft.world.item.Items.COBBLESTONE,4));service.submitContract(player,contract,request);h.assertTrue(store.load(player.getUUID()).points()==14&&ProgressionService.inventoryCount(player,"minecraft:cobblestone")==4,"Replayed turn-in preserves newly supplied resources");h.succeed();
        }finally{EditorRuntime.publish(old);}
    }
    private static boolean rejected(Runnable r){try{r.run();return false;}catch(IllegalArgumentException e){return true;}}
}

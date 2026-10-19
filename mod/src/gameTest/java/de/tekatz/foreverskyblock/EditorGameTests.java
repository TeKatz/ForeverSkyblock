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
    private static boolean rejected(Runnable r){try{r.run();return false;}catch(IllegalArgumentException e){return true;}}
}

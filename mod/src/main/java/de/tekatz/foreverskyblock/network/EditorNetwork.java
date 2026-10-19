package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.editor.EditorService;
import java.util.UUID;
import java.util.function.Consumer;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;

/** Chunked client uploads fit the 32 KB C2S packet limit; all work executes on the game thread. */
public final class EditorNetwork {
    public static final int CHUNK=7000, MAX_CHUNKS=36;
    private static Consumer<Reply> receiver=r->{};
    private EditorNetwork() { }
    public static void receiver(Consumer<Reply> value){receiver=value;}
    public static void register(RegisterPayloadHandlersEvent event) {
        var registrar=event.registrar("kmenu-editor-1");
        registrar.playToServer(Request.TYPE,Request.CODEC,(r,ctx)->{if(ctx.player() instanceof ServerPlayer p)EditorService.get(p.server).request(p,r);});
        registrar.playToClient(Reply.TYPE,Reply.CODEC,(r,ctx)->receiver.accept(r));
    }
    public record Request(int action,UUID transfer,int index,int total,long revision,String data) implements CustomPacketPayload {
        public Request {if(action<0||action>3||transfer==null||index<0||total<1||total>MAX_CHUNKS||index>=total||revision<0||data==null||data.length()>CHUNK||action!=1&&(index!=0||total!=1||!data.isEmpty()))throw new IllegalArgumentException("Invalid editor request");}
        public static final Type<Request> TYPE=new Type<>(ResourceLocation.fromNamespaceAndPath("forever_skyblock","editor_request"));
        public static final StreamCodec<RegistryFriendlyByteBuf,Request> CODEC=new StreamCodec<>() {
            public Request decode(RegistryFriendlyByteBuf b){return new Request(b.readUnsignedByte(),b.readUUID(),b.readVarInt(),b.readVarInt(),b.readVarLong(),b.readUtf(CHUNK));}
            public void encode(RegistryFriendlyByteBuf b,Request r){b.writeByte(r.action());b.writeUUID(r.transfer());b.writeVarInt(r.index());b.writeVarInt(r.total());b.writeVarLong(r.revision());b.writeUtf(r.data(),CHUNK);}
        };
        public Type<? extends CustomPacketPayload> type(){return TYPE;}
    }
    /** kind 0 catalog, 1 player progress, 2 operation result. */
    public record Reply(int kind,long revision,String data,String message,boolean canEdit,boolean open) implements CustomPacketPayload {
        public Reply {if(kind<0||kind>2||revision<0||data==null||data.length()>240000||message==null||message.length()>1000)throw new IllegalArgumentException("Invalid editor reply");}
        public static final Type<Reply> TYPE=new Type<>(ResourceLocation.fromNamespaceAndPath("forever_skyblock","editor_reply"));
        public static final StreamCodec<RegistryFriendlyByteBuf,Reply> CODEC=new StreamCodec<>() {
            public Reply decode(RegistryFriendlyByteBuf b){return new Reply(b.readUnsignedByte(),b.readVarLong(),b.readUtf(240000),b.readUtf(1000),b.readBoolean(),b.readBoolean());}
            public void encode(RegistryFriendlyByteBuf b,Reply r){b.writeByte(r.kind());b.writeVarLong(r.revision());b.writeUtf(r.data(),240000);b.writeUtf(r.message(),1000);b.writeBoolean(r.canEdit());b.writeBoolean(r.open());}
        };
        public Type<? extends CustomPacketPayload> type(){return TYPE;}
    }
}

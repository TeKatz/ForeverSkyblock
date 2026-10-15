package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;
import net.neoforged.neoforge.network.PacketDistributor;
import java.util.function.Consumer;

public final class CondenserNetwork {
    private static Consumer<TransferResult> transferReceiver = result -> { };
    private CondenserNetwork() { }
    public static void setTransferReceiver(Consumer<TransferResult> receiver) { transferReceiver = receiver; }
    public static void register(RegisterPayloadHandlersEvent event) {
        var registrar = event.registrar("condenser-2");
        registrar.playToServer(Filter.TYPE, Filter.CODEC, (request, context) -> {
            if (!(context.player().containerMenu instanceof CondenserMenu menu) || menu.containerId != request.menuId()) return;
            var item = BuiltInRegistries.ITEM.getOptional(request.item());
            if (item.isPresent()) menu.setGhost(context.player(), request.slot(), new ItemStack(item.get()));
        });
        registrar.playToServer(Transfer.TYPE, Transfer.CODEC, (request, context) -> {
            boolean success = context.player().containerMenu instanceof CondenserMenu menu && menu.containerId == request.menuId()
                    && menu.transferRecipe(context.player(), request.recipeId(), request.maximum());
            PacketDistributor.sendToPlayer((net.minecraft.server.level.ServerPlayer) context.player(),
                    new TransferResult(request.menuId(), request.requestId(), success));
        });
        registrar.playToClient(TransferResult.TYPE, TransferResult.CODEC, (result, context) -> transferReceiver.accept(result));
    }
    public record Transfer(int menuId, ResourceLocation recipeId, boolean maximum, int requestId) implements CustomPacketPayload {
        public static final Type<Transfer> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "condenser_transfer"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Transfer> CODEC = new StreamCodec<>() {
            @Override public Transfer decode(RegistryFriendlyByteBuf buffer) { return new Transfer(buffer.readVarInt(), buffer.readResourceLocation(), buffer.readBoolean(), buffer.readVarInt()); }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Transfer value) { buffer.writeVarInt(value.menuId()); buffer.writeResourceLocation(value.recipeId()); buffer.writeBoolean(value.maximum()); buffer.writeVarInt(value.requestId()); }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
    public record TransferResult(int menuId, int requestId, boolean success) implements CustomPacketPayload {
        public static final Type<TransferResult> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "condenser_transfer_result"));
        public static final StreamCodec<RegistryFriendlyByteBuf, TransferResult> CODEC = new StreamCodec<>() {
            @Override public TransferResult decode(RegistryFriendlyByteBuf buffer) { return new TransferResult(buffer.readVarInt(), buffer.readVarInt(), buffer.readBoolean()); }
            @Override public void encode(RegistryFriendlyByteBuf buffer, TransferResult value) { buffer.writeVarInt(value.menuId()); buffer.writeVarInt(value.requestId()); buffer.writeBoolean(value.success()); }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
    public record Filter(int menuId, int slot, ResourceLocation item) implements CustomPacketPayload {
        public static final Type<Filter> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "condenser_filter"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Filter> CODEC = new StreamCodec<>() {
            @Override public Filter decode(RegistryFriendlyByteBuf buffer) {
                return new Filter(buffer.readVarInt(), buffer.readUnsignedByte(), buffer.readResourceLocation());
            }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Filter value) {
                buffer.writeVarInt(value.menuId()); buffer.writeByte(value.slot()); buffer.writeResourceLocation(value.item());
            }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
}

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

public final class CondenserNetwork {
    private CondenserNetwork() { }
    public static void register(RegisterPayloadHandlersEvent event) {
        event.registrar("condenser-1").playToServer(Filter.TYPE, Filter.CODEC, (request, context) -> {
            if (!(context.player().containerMenu instanceof CondenserMenu menu) || menu.containerId != request.menuId()) return;
            var item = BuiltInRegistries.ITEM.getOptional(request.item());
            if (item.isPresent()) menu.setGhost(context.player(), request.slot(), new ItemStack(item.get()));
        });
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

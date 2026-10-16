package de.tekatz.foreverskyblock.item;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.ProgressionProfile;
import de.tekatz.foreverskyblock.progression.ProgressionService;
import de.tekatz.foreverskyblock.progression.ShopCatalog;
import java.util.UUID;
import net.minecraft.core.component.DataComponents;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.component.CustomData;
import net.minecraft.world.level.Level;

/** Unique owner/world-bound carrier for one durable world-shop delivery receipt. */
public final class SupplyCacheItem extends Item {
    private static final int FORMAT = 1;

    public record Marker(String productId, UUID receiptId, UUID ownerId, UUID worldId) { }

    public SupplyCacheItem(Properties properties) { super(properties); }

    public static ItemStack create(ShopCatalog.Product product, UUID receipt, UUID owner, UUID world) {
        if (product == null || receipt == null || owner == null || world == null || !product.deliverable())
            throw new IllegalArgumentException("Invalid supply-cache identity");
        ItemStack stack = new ItemStack(ForeverSkyblock.WORLD_SUPPLY_CACHE.get());
        CompoundTag tag = new CompoundTag();
        tag.putInt("format", FORMAT);
        tag.putString("product", product.id());
        tag.putUUID("receipt", receipt);
        tag.putUUID("owner", owner);
        tag.putUUID("world", world);
        stack.set(DataComponents.CUSTOM_DATA, CustomData.of(tag));
        return stack;
    }

    public static Marker marker(ItemStack stack) {
        if (stack == null || stack.isEmpty() || !(stack.getItem() instanceof SupplyCacheItem)) return null;
        try {
            CustomData data = stack.get(DataComponents.CUSTOM_DATA);
            if (data == null || data.isEmpty()) return null;
            CompoundTag tag = data.copyTag();
            if (tag.getInt("format") != FORMAT || !tag.hasUUID("receipt") || !tag.hasUUID("owner") || !tag.hasUUID("world")) return null;
            String product = tag.getString("product");
            ProgressionProfile.validateId(product);
            return new Marker(product, tag.getUUID("receipt"), tag.getUUID("owner"), tag.getUUID("world"));
        } catch (RuntimeException e) { return null; }
    }

    @Override public Component getName(ItemStack stack) {
        Marker marker = marker(stack);
        ShopCatalog.Product product = marker == null ? null : ShopCatalog.find(marker.productId());
        return Component.literal(product == null ? "Forever Supply Cache" : product.titleEn() + " — Supply Cache");
    }

    @Override public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);
        if (level.isClientSide) return InteractionResultHolder.success(stack);
        if (!(player instanceof ServerPlayer serverPlayer)) return InteractionResultHolder.pass(stack);
        return ProgressionService.get(serverPlayer.server).openSupplyCache(serverPlayer, stack)
                ? InteractionResultHolder.success(stack) : InteractionResultHolder.fail(stack);
    }
}

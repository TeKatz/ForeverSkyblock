package de.tekatz.foreverskyblock.item;

import java.util.UUID;
import net.minecraft.core.component.DataComponents;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.component.CustomData;
import de.tekatz.foreverskyblock.ForeverSkyblock;

public final class ForeverCore {
    private static final String KEY = "forever_core_id";
    private ForeverCore() { }
    public static ItemStack create() {
        var stack = new ItemStack(ForeverSkyblock.FOREVER_CORE.get());
        var tag = new CompoundTag(); tag.putUUID(KEY, UUID.randomUUID());
        stack.set(DataComponents.CUSTOM_DATA, CustomData.of(tag));
        return stack;
    }
    public static UUID identity(ItemStack stack) {
        if (!stack.is(ForeverSkyblock.FOREVER_CORE.get()) || stack.getCount() != 1) return null;
        var tag = stack.getOrDefault(DataComponents.CUSTOM_DATA, CustomData.EMPTY).copyTag();
        return tag.hasUUID(KEY) ? tag.getUUID(KEY) : null;
    }
}

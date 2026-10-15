package de.tekatz.foreverskyblock.tools;

import java.util.Set;
import net.minecraft.core.component.DataComponents;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.component.Unbreakable;
import net.neoforged.neoforge.event.ModifyDefaultComponentsEvent;

/** Scope is the 24 approved Vanilla mining/farming tools; utility tools and weapons are separate design work. */
public final class ToolPolicy {
    private static final Set<String> TOOLS = Set.of("pickaxe", "axe", "shovel", "hoe");
    private static final Set<String> TIERS = Set.of("wooden", "stone", "iron", "golden", "diamond", "netherite");
    private ToolPolicy() { }
    public static boolean includes(ResourceLocation id) {
        if (!id.getNamespace().equals("minecraft")) return false;
        int separator = id.getPath().indexOf('_');
        return separator > 0 && TIERS.contains(id.getPath().substring(0, separator)) && TOOLS.contains(id.getPath().substring(separator + 1));
    }
    public static boolean includes(ItemStack stack) { return !stack.isEmpty() && includes(BuiltInRegistries.ITEM.getKey(stack.getItem())); }
    public static void modifyComponents(ModifyDefaultComponentsEvent event) {
        event.modifyMatching(item -> includes(BuiltInRegistries.ITEM.getKey(item)),
                builder -> builder.set(DataComponents.UNBREAKABLE, new Unbreakable(true)));
    }
}

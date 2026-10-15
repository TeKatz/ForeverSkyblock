package de.tekatz.foreverskyblock.compat;

import com.blakebr0.ironjetpacks.registry.Jetpack;
import com.blakebr0.ironjetpacks.registry.JetpackRegistry;
import com.blakebr0.ironjetpacks.util.JetpackUtils;
import de.tekatz.foreverskyblock.progression.JetpackResearch;
import java.util.List;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.item.ItemStack;

/** Loaded only when Iron Jetpacks is installed. Unknown/invalid variants fail closed. */
public final class IronJetpacksBridge {
    private IronJetpacksBridge() { }
    public static int requiredStage(ItemStack stack) {
        String path = BuiltInRegistries.ITEM.getKey(stack.getItem()).getPath();
        return switch (path) {
            case "strap", "basic_coil" -> 1;
            case "advanced_coil" -> 2;
            case "elite_coil", "ultimate_coil" -> 3;
            default -> {
                Jetpack jetpack = JetpackUtils.getJetpack(stack);
                yield jetpack == Jetpack.UNDEFINED || jetpack.disabled ? 4
                        : JetpackResearch.requiredStage(jetpack.tier, jetpack.creative);
            }
        };
    }
    public static List<ItemStack> variants(int stage) {
        return JetpackRegistry.getInstance().getJetpacks().stream().filter(j -> !j.disabled)
                .filter(j -> JetpackResearch.requiredStage(j.tier, j.creative) == stage)
                .map(JetpackUtils::getItemForJetpack).toList();
    }
}

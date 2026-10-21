package de.tekatz.foreverskyblock.mixin.compat;

import de.tekatz.foreverskyblock.client.ApotheosisClientState;
import de.tekatz.foreverskyblock.progression.ApotheosisStage;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.ChatFormatting;
import net.minecraft.client.gui.components.AbstractWidget;
import net.minecraft.network.chat.CommonComponents;
import net.minecraft.network.chat.Component;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Coerce;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Makes Apotheosis' CTRL+T screen describe Forever's authoritative world progression. */
@Mixin(targets = "dev.shadowsoffire.apotheosis.client.WorldTierSelectScreen", remap = false)
public abstract class ApotheosisWorldTierScreenMixin {

    @Inject(method = "tierLocked", at = @At("HEAD"), cancellable = true, remap = false)
    private static void forever$replaceUnlockCriteria(@Coerce Object tier,
            CallbackInfoReturnable<List<Component>> cir) {
        String name = tier instanceof Enum<?> e ? e.name() : String.valueOf(tier);
        List<Component> lines = new ArrayList<>();
        lines.add(Component.literal("Forever progression").withStyle(ChatFormatting.GOLD));
        lines.add(CommonComponents.SPACE);
        lines.add(Component.literal("World unlock: ").withStyle(ChatFormatting.GRAY)
                .append(Component.literal(requirement(name)).withStyle(ChatFormatting.WHITE)));
        cir.setReturnValue(lines);
    }

    @Inject(method = "updateButtonStatus", at = @At("TAIL"), remap = false)
    private void forever$markTierAsManaged(CallbackInfo ci) {
        try {
            Field field = this.getClass().getDeclaredField("activateButton");
            field.setAccessible(true);
            Object button = field.get(this);
            if (!(button instanceof AbstractWidget widget)) return;

            ApotheosisStage stage = ApotheosisClientState.stage();
            widget.active = false;
            widget.setMessage(Component.literal("Managed by Forever").withStyle(ChatFormatting.GOLD));

            Method setText = button.getClass().getMethod("setButtonText", Component.class);
            setText.invoke(button, Component.literal("FOREVER").withStyle(ChatFormatting.GOLD));

            List<Component> tooltip = new ArrayList<>();
            tooltip.add(Component.literal("Current: ").withStyle(ChatFormatting.GRAY)
                    .append(Component.literal(displayName(stage)).withStyle(ChatFormatting.WHITE)));
            if (stage != ApotheosisStage.PINNACLE) {
                ApotheosisStage next = nextStage(stage);
                tooltip.add(Component.literal("Next: ").withStyle(ChatFormatting.GRAY)
                        .append(Component.literal(displayName(next)).withStyle(ChatFormatting.WHITE)));
                tooltip.add(Component.literal(requirement(next.name())).withStyle(ChatFormatting.DARK_GRAY));
            }
            else {
                tooltip.add(Component.literal("Full Apotheosis progression unlocked.")
                        .withStyle(ChatFormatting.GRAY));
            }
            tooltip.add(Component.literal("World Tiers advance automatically through Forever Skyblock progression.")
                    .withStyle(ChatFormatting.DARK_GRAY));

            Method setInactive = button.getClass().getMethod("setInactiveMessage", List.class);
            setInactive.invoke(button, tooltip);
        }
        catch (ReflectiveOperationException ignored) {
            // The pinned Apotheosis API is guarded by the integration GameTest. Fail soft on other versions.
        }
    }

    private static ApotheosisStage nextStage(ApotheosisStage stage) {
        return switch (stage) {
            case DORMANT -> ApotheosisStage.HAVEN;
            case HAVEN -> ApotheosisStage.FRONTIER;
            case FRONTIER -> ApotheosisStage.ASCENT;
            case ASCENT -> ApotheosisStage.SUMMIT;
            case SUMMIT, PINNACLE -> ApotheosisStage.PINNACLE;
        };
    }

    private static String displayName(ApotheosisStage stage) {
        if (stage == ApotheosisStage.DORMANT) return "Dormant";
        String id = stage.id();
        return Character.toUpperCase(id.charAt(0)) + id.substring(1);
    }

    private static String requirement(String tier) {
        return switch (tier) {
            case "HAVEN" -> "Enter the Nether";
            case "FRONTIER" -> "Sacrifice a player-thrown Diamond to fire";
            case "ASCENT" -> "Defeat the Wither";
            case "SUMMIT" -> "Defeat the Ender Dragon";
            case "PINNACLE" -> "Craft the Forever Core";
            default -> "Advance Forever Skyblock progression";
        };
    }
}

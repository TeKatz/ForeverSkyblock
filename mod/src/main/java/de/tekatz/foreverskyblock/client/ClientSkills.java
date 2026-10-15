package de.tekatz.foreverskyblock.client;

import com.mojang.blaze3d.platform.InputConstants;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import net.minecraft.client.KeyMapping;
import net.minecraft.client.Minecraft;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.ClientPlayerNetworkEvent;
import net.neoforged.neoforge.client.event.ClientTickEvent;
import net.neoforged.neoforge.client.settings.KeyConflictContext;
import net.neoforged.neoforge.network.PacketDistributor;
import org.lwjgl.glfw.GLFW;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, value = Dist.CLIENT)
public final class ClientSkills {
    public static final KeyMapping OPEN = new KeyMapping("key.forever_skyblock.skills", KeyConflictContext.IN_GAME,
            InputConstants.Type.KEYSYM, GLFW.GLFW_KEY_K, "key.categories.forever_skyblock");
    private static SkillNetwork.Snapshot snapshot;
    private static boolean pending;
    private ClientSkills() { }
    public static SkillNetwork.Snapshot snapshot() { return snapshot; }
    public static boolean pending() { return pending; }
    public static void accept(SkillNetwork.Snapshot value) {
        var minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.level == null) return;
        snapshot = value;
        pending = false;
        if (value.open() && !(minecraft.screen instanceof SkillScreen)) minecraft.setScreen(new SkillScreen());
        if (minecraft.screen instanceof SkillScreen screen) screen.refresh();
    }
    public static void buy(String skill) {
        if (pending) return;
        pending = true;
        PacketDistributor.sendToServer(new SkillNetwork.Request(1, skill));
    }
    public static void reset() {
        if (pending) return;
        pending = true;
        PacketDistributor.sendToServer(new SkillNetwork.Request(2, ""));
    }
    @SubscribeEvent public static void tick(ClientTickEvent.Post event) {
        var minecraft = Minecraft.getInstance();
        while (OPEN.consumeClick()) if (minecraft.player != null && minecraft.screen == null)
            PacketDistributor.sendToServer(new SkillNetwork.Request(0, ""));
    }
    @SubscribeEvent public static void logout(ClientPlayerNetworkEvent.LoggingOut event) { snapshot = null; pending = false; }
}

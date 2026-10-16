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
    private static Runnable visibilityChanged = () -> { };
    public static void onVisibilityChanged(Runnable listener) { visibilityChanged = listener; }
    public static int jetpackStage() { return snapshot == null || !snapshot.available() ? 0
            : de.tekatz.foreverskyblock.progression.JetpackResearch.stage(snapshot::rank); }
    private ClientSkills() { }
    public static SkillNetwork.Snapshot snapshot() { return snapshot; }
    public static boolean pending() { return pending; }
    public static void accept(SkillNetwork.Snapshot value) {
        var minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.level == null) return;
        boolean firstSnapshot = snapshot == null;
        int oldStage = jetpackStage();
        snapshot = value;
        pending = false;
        // Counter-only snapshots must not rebuild JEI's research visibility on every mob kill.
        if (firstSnapshot || oldStage != jetpackStage()) visibilityChanged.run();
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
    public static void testPoints() {
        if (pending || snapshot == null || !snapshot.canTestPoints()) return;
        pending = true;
        PacketDistributor.sendToServer(new SkillNetwork.Request(3, ""));
    }
    @SubscribeEvent public static void tick(ClientTickEvent.Post event) {
        var minecraft = Minecraft.getInstance();
        while (OPEN.consumeClick()) if (minecraft.player != null && minecraft.screen == null)
            PacketDistributor.sendToServer(new SkillNetwork.Request(0, ""));
    }
    @SubscribeEvent public static void logout(ClientPlayerNetworkEvent.LoggingOut event) { snapshot = null; pending = false; visibilityChanged.run(); }
}

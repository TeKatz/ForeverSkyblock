package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.jei.ApotheosisJeiVisibility;
import de.tekatz.foreverskyblock.network.ApotheosisNetwork;
import de.tekatz.foreverskyblock.progression.ApotheosisStage;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.EventBusSubscriber;
import net.neoforged.neoforge.client.event.ClientPlayerNetworkEvent;

@EventBusSubscriber(modid = ForeverSkyblock.MOD_ID, value = Dist.CLIENT)
public final class ApotheosisClientState {
    private static ApotheosisStage stage = ApotheosisStage.DORMANT;

    static { ApotheosisNetwork.setClientReceiver(ApotheosisClientState::accept); }

    private ApotheosisClientState() { }

    public static ApotheosisStage stage() { return stage; }

    private static void accept(ApotheosisStage value) {
        stage = value == null ? ApotheosisStage.DORMANT : value;
        ApotheosisJeiVisibility.refresh();
    }

    @SubscribeEvent
    public static void login(ClientPlayerNetworkEvent.LoggingIn event) {
        ApotheosisNetwork.setClientReceiver(ApotheosisClientState::accept);
    }

    @SubscribeEvent
    public static void logout(ClientPlayerNetworkEvent.LoggingOut event) {
        stage = ApotheosisStage.DORMANT;
        ApotheosisJeiVisibility.refresh();
    }
}
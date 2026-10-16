package de.tekatz.foreverskyblock;

import java.lang.reflect.Method;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.resources.ResourceLocation;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class TechChallengeGameTests {
    private static void present(GameTestHelper h, String item) {
        h.assertTrue(BuiltInRegistries.ITEM.containsKey(ResourceLocation.parse(item)), "Expected installed item " + item);
    }

    @GameTest(template="empty")
    public static void installedPhaseFourApisAndIdsMatchPinnedPackVersions(GameTestHelper h) throws Exception {
        if (!Boolean.getBoolean("forever_skyblock.expectTechChallenges")) { h.succeed(); return; }

        present(h, "ae2:item_storage_cell_4k");
        present(h, "ae2:item_storage_cell_256k");
        present(h, "ae2:64k_crafting_storage");
        Class<?> cpu = Class.forName("appeng.crafting.execution.CraftingCpuLogic");
        h.assertTrue(cpu.getDeclaredField("job") != null && cpu.getDeclaredField("cluster") != null,
                "AE2 CPU keeps the requester-bearing job and cluster fields used by the adapter");
        Method finish = cpu.getDeclaredMethod("finishJob", boolean.class);
        h.assertTrue(finish.getReturnType() == void.class, "AE2 finishJob signature matches the mixin");
        Class<?> playerRegistry = Class.forName("appeng.api.features.IPlayerRegistry");
        h.assertTrue(playerRegistry.getMethod("getConnected", net.minecraft.server.MinecraftServer.class, int.class) != null,
                "AE2 player registry can resolve the connected requester");

        present(h, "mekanism:metallurgic_infuser");
        Class<?> miner = Class.forName("mekanism.common.tile.machine.TileEntityDigitalMiner");
        boolean replace = java.util.Arrays.stream(miner.getDeclaredMethods())
                .anyMatch(m -> m.getName().equals("setReplace") && m.getReturnType() == boolean.class && m.getParameterCount() == 3);
        h.assertTrue(replace, "Pinned Mekanism Digital Miner exposes the successful replacement boundary");
        h.assertTrue(miner.getMethod("getOwnerUUID") != null, "Digital Miner exposes its security owner");

        present(h, "powah:energizing_orb");
        Class<?> orb = Class.forName("owmii.powah.block.energizing.EnergizingOrbTile");
        h.assertTrue(orb.getMethod("getInventory") != null, "Pinned Powah Orb exposes its inventory");

        present(h, "mysticalagriculture:supremium_essence");
        h.succeed();
    }
}

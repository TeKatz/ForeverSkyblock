package de.tekatz.foreverskyblock;

import com.mojang.authlib.GameProfile;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.ProgressionProfile;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import io.netty.buffer.Unpooled;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.world.SimpleContainer;
import net.minecraft.world.entity.ai.attributes.Attributes;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.neoforged.neoforge.common.util.FakePlayerFactory;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class SkillGameTests {
    @GameTest(template = "empty")
    public static void reachBonusDoesNotStackAndCanBeReapplied(GameTestHelper h) {
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "SkillTest"));
        var attribute = player.getAttribute(Attributes.BLOCK_INTERACTION_RANGE);
        double before = attribute.getValue();
        var profile = new ProgressionProfile(0, Set.of(), Map.of(SkillCatalog.EXTENDED_REACH, 1));
        SkillCatalog.apply(player, profile); SkillCatalog.apply(player, profile);
        h.assertTrue(Math.abs(attribute.getValue() - before - 1.0) < 0.00001, "Reach must increase exactly once");
        SkillCatalog.apply(player, ProgressionProfile.empty());
        h.assertTrue(attribute.getValue() == before, "Clearing a profile removes the modifier");
        SkillCatalog.apply(player, profile);
        h.assertTrue(Math.abs(attribute.getValue() - before - 1.0) < 0.00001, "Reapplying after lifecycle changes restores the bonus");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void milestoneRequiresActualFragmentCraft(GameTestHelper h) {
        var grid = new SimpleContainer(4);
        for (int i = 0; i < 4; i++) grid.setItem(i, new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get()));
        h.assertTrue(MilestoneCatalog.isFragmentCraft(new ItemStack(Items.COBBLESTONE), grid), "Fragment recipe qualifies");
        h.assertTrue(!MilestoneCatalog.isFragmentCraft(new ItemStack(Items.STONE), grid), "Different output does not qualify");
        grid.setItem(0, new ItemStack(Items.DIRT));
        h.assertTrue(!MilestoneCatalog.isFragmentCraft(new ItemStack(Items.COBBLESTONE), grid), "Different inputs do not qualify");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void skillPacketsRoundTrip(GameTestHelper h) {
        var buffer = new RegistryFriendlyByteBuf(Unpooled.buffer(), h.getLevel().registryAccess());
        try {
            var request = new SkillNetwork.Request(1, SkillCatalog.EXTENDED_REACH);
            SkillNetwork.Request.CODEC.encode(buffer, request);
            h.assertTrue(request.equals(SkillNetwork.Request.CODEC.decode(buffer)), "Purchase request round trips");
            buffer.clear();
            var state = new SkillNetwork.Snapshot(3, Map.of(SkillCatalog.EXTENDED_REACH, 1),
                    Set.of(MilestoneCatalog.FIRST_FOUNDATIONS.id()), true, true, true, "purchased");
            SkillNetwork.Snapshot.CODEC.encode(buffer, state);
            h.assertTrue(state.equals(SkillNetwork.Snapshot.CODEC.decode(buffer)), "Authoritative profile snapshot round trips");
            h.succeed();
        } finally { buffer.release(); }
    }
}

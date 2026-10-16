package de.tekatz.foreverskyblock;

import com.mojang.authlib.GameProfile;
import de.tekatz.foreverskyblock.network.SkillNetwork;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.ProgressionProfile;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import de.tekatz.foreverskyblock.progression.SkillMessages;
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
            var reset = new SkillNetwork.Request(2, "");
            SkillNetwork.Request.CODEC.encode(buffer, reset);
            h.assertTrue(reset.equals(SkillNetwork.Request.CODEC.decode(buffer)), "Reset request round trips");
            buffer.clear();
            var state = new SkillNetwork.Snapshot(3, 2, Map.of(SkillCatalog.EXTENDED_REACH, 1),
                    Set.of(MilestoneCatalog.FIRST_FOUNDATIONS.id()), true, true, true, "purchased", true, 1,
                    Map.of("forever_skyblock:veteran", 630, "forever_skyblock:permanent_tools_i", 3),
                    new de.tekatz.foreverskyblock.progression.MetaProgress.Accounting(12, 1, 5, 0, 1, 0, 10, true),
                    true, "Phase 3 Test", Set.of(de.tekatz.foreverskyblock.progression.ShopCatalog.TEST_WORLD_ENTITLEMENT));
            SkillNetwork.Snapshot.CODEC.encode(buffer, state);
            h.assertTrue(state.equals(SkillNetwork.Snapshot.CODEC.decode(buffer)), "Authoritative profile snapshot round trips");
            h.succeed();
        } finally { buffer.release(); }
    }
    @GameTest(template = "empty")
    public static void runningServerWorldIdentityIsStable(GameTestHelper h) throws Exception {
        var root=h.getLevel().getServer().getWorldPath(net.minecraft.world.level.storage.LevelResource.ROOT);
        var first=new de.tekatz.foreverskyblock.progression.WorldIdentityStore().loadOrCreate(root);
        var second=new de.tekatz.foreverskyblock.progression.WorldIdentityStore().loadOrCreate(root);
        h.assertTrue(first.equals(second),"The running save keeps one stable world identity");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void resetRefundsAndRemovesReachWithoutRearmingMilestone(GameTestHelper h) {
        var player = FakePlayerFactory.get(h.getLevel(), new GameProfile(UUID.randomUUID(), "ResetTest"));
        var attribute = player.getAttribute(Attributes.BLOCK_INTERACTION_RANGE);
        double before = attribute.getValue();
        var purchased = new ProgressionProfile(2, Set.of(MilestoneCatalog.FIRST_FOUNDATIONS.id()), Map.of(SkillCatalog.EXTENDED_REACH, 1));
        SkillCatalog.apply(player, purchased);
        var reset = SkillCatalog.reset(purchased);
        SkillCatalog.apply(player, reset);
        h.assertTrue(reset.points() == 3 && reset.rank(SkillCatalog.EXTENDED_REACH) == 0, "Reset refunds the purchase once");
        h.assertTrue(attribute.getValue() == before, "Reset immediately removes the reach bonus");
        h.assertTrue(reset.equals(SkillCatalog.reset(reset)), "Repeated reset does not duplicate points");
        h.assertTrue(reset.award(MilestoneCatalog.FIRST_FOUNDATIONS.id(), 1).equals(reset), "Milestone cannot pay again after reset");
        h.succeed();
    }
    @GameTest(template = "empty")
    public static void rewardLinkIsGreenAndOnlyOpensSkills(GameTestHelper h) {
        var message = SkillMessages.milestone(MilestoneCatalog.FIRST_FOUNDATIONS);
        var contents = (net.minecraft.network.chat.contents.TranslatableContents) message.getContents();
        var points = (net.minecraft.network.chat.Component) contents.getArgs()[1];
        var style = points.getStyle();
        h.assertTrue(style.getColor().equals(net.minecraft.network.chat.TextColor.fromLegacyFormat(net.minecraft.ChatFormatting.GREEN)), "Point reward is green");
        h.assertTrue(style.getClickEvent().getAction() == net.minecraft.network.chat.ClickEvent.Action.RUN_COMMAND
                && style.getClickEvent().getValue().equals("/foreverskills"), "Reward link only opens the menu");
        h.assertTrue(style.getHoverEvent() != null, "Reward link explains its action on hover");
        h.assertTrue(message.getStyle().getClickEvent() == null && message.getStyle().getColor() == null, "Link styling stays on points");
        h.succeed();
    }

}

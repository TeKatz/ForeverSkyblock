package de.tekatz.foreverskyblock;

import net.minecraft.gametest.framework.GameTest;
import net.minecraft.gametest.framework.GameTestHelper;
import net.minecraft.world.item.ItemStack;
import net.neoforged.neoforge.gametest.GameTestHolder;
import net.neoforged.neoforge.gametest.PrefixGameTestTemplate;

@GameTestHolder(ForeverSkyblock.MOD_ID)
@PrefixGameTestTemplate(false)
public final class CruciblePresentationGameTests {
    @GameTest(template = "empty")
    public static void blockItemUsesPlayerFacingCrucibleName(GameTestHelper helper) {
        ItemStack stack = new ItemStack(ForeverSkyblock.CRUCIBLE_ITEM.get());
        helper.assertTrue("Crucible".equals(stack.getHoverName().getString()),
                "Crucible inventory and JEI name must not expose the untranslated description id");
        helper.succeed();
    }
}

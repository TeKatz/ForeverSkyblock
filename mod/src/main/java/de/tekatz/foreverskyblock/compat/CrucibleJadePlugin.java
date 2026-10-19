package de.tekatz.foreverskyblock.compat;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.block.CrucibleBlock;
import de.tekatz.foreverskyblock.blockentity.CrucibleBlockEntity;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.tags.FluidTags;
import net.minecraft.world.level.block.Blocks;
import snownee.jade.api.BlockAccessor;
import snownee.jade.api.IBlockComponentProvider;
import snownee.jade.api.IWailaClientRegistration;
import snownee.jade.api.IWailaPlugin;
import snownee.jade.api.ITooltip;
import snownee.jade.api.WailaPlugin;
import snownee.jade.api.config.IPluginConfig;

@WailaPlugin
public final class CrucibleJadePlugin implements IWailaPlugin {
    private static final ResourceLocation UID = ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "crucible_heat");
    private static final ResourceLocation SUPERHEATING = ResourceLocation.fromNamespaceAndPath("mekanism", "superheating_element");
    private static final Provider PROVIDER = new Provider();

    @Override public void registerClient(IWailaClientRegistration registration) {
        registration.registerBlockComponent(PROVIDER, CrucibleBlock.class);
    }

    private static final class Provider implements IBlockComponentProvider {
        @Override public void appendTooltip(ITooltip tooltip, BlockAccessor accessor, IPluginConfig config) {
            var below = accessor.getLevel().getBlockState(accessor.getPosition().below());
            int tier;
            ResourceLocation id = BuiltInRegistries.BLOCK.getKey(below.getBlock());
            if (SUPERHEATING.equals(id)) tier = CrucibleBlockEntity.HEAT_SUPERHEATING;
            else if (below.getFluidState().is(FluidTags.LAVA) && below.getFluidState().isSource()) tier = CrucibleBlockEntity.HEAT_LAVA;
            else if (below.is(Blocks.CAMPFIRE) || below.is(Blocks.SOUL_CAMPFIRE)) tier = CrucibleBlockEntity.HEAT_CAMPFIRE;
            else if (below.is(Blocks.TORCH)) tier = CrucibleBlockEntity.HEAT_TORCH;
            else tier = CrucibleBlockEntity.HEAT_NONE;
            tooltip.add(Component.literal("Melting tier: " + CrucibleBlockEntity.heatTierName(tier)));
            if (tier > CrucibleBlockEntity.HEAT_NONE) {
                tooltip.add(Component.literal("1000 mB: " + (CrucibleBlockEntity.durationFor(tier) / 20) + " s"));
            }
        }

        @Override public ResourceLocation getUid() { return UID; }
    }
}

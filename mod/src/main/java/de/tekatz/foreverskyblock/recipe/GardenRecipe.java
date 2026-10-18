package de.tekatz.foreverskyblock.recipe;

import com.mojang.serialization.Codec;
import com.mojang.serialization.MapCodec;
import com.mojang.serialization.codecs.RecordCodecBuilder;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.HolderLookup;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.ByteBufCodecs;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.item.crafting.Recipe;
import net.minecraft.world.item.crafting.RecipeSerializer;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.level.Level;

public record GardenRecipe(Ingredient specimen, ItemStack primaryOutput, ItemStack secondaryOutput, int processingTime)
        implements Recipe<GardenInput> {
    public static final int DEFAULT_TIME = 320;

    public GardenRecipe {
        primaryOutput = primaryOutput.copy();
        secondaryOutput = secondaryOutput.copy();
        if (primaryOutput.isEmpty() || secondaryOutput.isEmpty()) throw new IllegalArgumentException("Garden outputs must not be empty");
        if (primaryOutput.getCount() > Math.min(64, primaryOutput.getMaxStackSize())
                || secondaryOutput.getCount() > Math.min(64, secondaryOutput.getMaxStackSize()))
            throw new IllegalArgumentException("Invalid garden output count");
        if (processingTime < 1 || processingTime > 32767) throw new IllegalArgumentException("Invalid processing time");
    }

    public boolean accepts(ItemStack stack) { return !stack.isEmpty() && specimen.test(stack); }
    public ItemStack primaryResult() { return primaryOutput.copy(); }
    public ItemStack secondaryResult() { return secondaryOutput.copy(); }

    @Override public boolean matches(GardenInput input, Level level) { return accepts(input.specimen()); }
    @Override public ItemStack assemble(GardenInput input, HolderLookup.Provider registries) { return primaryResult(); }
    @Override public ItemStack getResultItem(HolderLookup.Provider registries) { return primaryResult(); }
    @Override public boolean canCraftInDimensions(int width, int height) { return true; }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverSkyblock.GARDEN_SERIALIZER.get(); }
    @Override public RecipeType<?> getType() { return ForeverSkyblock.GARDEN.get(); }

    public static final class Serializer implements RecipeSerializer<GardenRecipe> {
        private static final MapCodec<GardenRecipe> CODEC = RecordCodecBuilder.mapCodec(instance -> instance.group(
                Ingredient.CODEC.fieldOf("specimen").forGetter(GardenRecipe::specimen),
                ItemStack.CODEC.fieldOf("primary_result").forGetter(GardenRecipe::primaryOutput),
                ItemStack.CODEC.fieldOf("secondary_result").forGetter(GardenRecipe::secondaryOutput),
                Codec.intRange(1, 32767).optionalFieldOf("processing_time", DEFAULT_TIME).forGetter(GardenRecipe::processingTime)
        ).apply(instance, GardenRecipe::new));

        private static final StreamCodec<RegistryFriendlyByteBuf, GardenRecipe> STREAM = new StreamCodec<>() {
            @Override public GardenRecipe decode(RegistryFriendlyByteBuf buf) {
                return new GardenRecipe(
                        Ingredient.CONTENTS_STREAM_CODEC.decode(buf),
                        ItemStack.STREAM_CODEC.decode(buf),
                        ItemStack.STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf));
            }

            @Override public void encode(RegistryFriendlyByteBuf buf, GardenRecipe recipe) {
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.specimen());
                ItemStack.STREAM_CODEC.encode(buf, recipe.primaryOutput());
                ItemStack.STREAM_CODEC.encode(buf, recipe.secondaryOutput());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.processingTime());
            }
        };

        @Override public MapCodec<GardenRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, GardenRecipe> streamCodec() { return STREAM; }
    }
}

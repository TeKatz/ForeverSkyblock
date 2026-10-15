package de.tekatz.foreverskyblock.recipe;

import com.mojang.serialization.Codec;
import com.mojang.serialization.MapCodec;
import com.mojang.serialization.codecs.RecordCodecBuilder;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.core.HolderLookup;
import net.minecraft.core.NonNullList;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.ByteBufCodecs;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.item.crafting.Recipe;
import net.minecraft.world.item.crafting.RecipeSerializer;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.item.crafting.SingleRecipeInput;
import net.minecraft.world.level.Level;

public record CondenserRecipe(Ingredient ingredient, int fragmentCount, int processingTime) implements Recipe<SingleRecipeInput> {
    public static final int DEFAULT_TIME = 160;

    @Override public boolean matches(SingleRecipeInput input, Level level) { return ingredient.test(input.item()); }
    @Override public ItemStack assemble(SingleRecipeInput input, HolderLookup.Provider registries) { return result(); }
    @Override public ItemStack getResultItem(HolderLookup.Provider registries) { return result(); }
    public ItemStack result() { return new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), fragmentCount); }
    @Override public boolean canCraftInDimensions(int width, int height) { return true; }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverSkyblock.CONDENSING_SERIALIZER.get(); }
    @Override public RecipeType<?> getType() { return ForeverSkyblock.CONDENSING.get(); }
    @Override public boolean isSpecial() { return true; }
    @Override public NonNullList<Ingredient> getIngredients() { return NonNullList.of(Ingredient.EMPTY, ingredient); }
    @Override public ItemStack getToastSymbol() { return new ItemStack(ForeverSkyblock.CONDENSER_MARK_I.get()); }

    public static final class Serializer implements RecipeSerializer<CondenserRecipe> {
        private static final MapCodec<CondenserRecipe> CODEC = RecordCodecBuilder.mapCodec(instance -> instance.group(
                Ingredient.CODEC_NONEMPTY.fieldOf("ingredient").forGetter(CondenserRecipe::ingredient),
                Codec.intRange(1, 64).fieldOf("fragment_count").forGetter(CondenserRecipe::fragmentCount),
                Codec.intRange(1, 32767).optionalFieldOf("processing_time", DEFAULT_TIME).forGetter(CondenserRecipe::processingTime)
        ).apply(instance, CondenserRecipe::new));
        private static final StreamCodec<RegistryFriendlyByteBuf, CondenserRecipe> STREAM_CODEC = StreamCodec.composite(
                Ingredient.CONTENTS_STREAM_CODEC, CondenserRecipe::ingredient,
                ByteBufCodecs.VAR_INT, CondenserRecipe::fragmentCount,
                ByteBufCodecs.VAR_INT, CondenserRecipe::processingTime,
                CondenserRecipe::new);
        @Override public MapCodec<CondenserRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, CondenserRecipe> streamCodec() { return STREAM_CODEC; }
    }
}

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

public record WashingRecipe(Ingredient ingredient, int ingredientCount, ItemStack output, int processingTime)
        implements Recipe<WashingInput> {
    public static final int DEFAULT_TIME = 160;

    public WashingRecipe {
        if (ingredientCount < 1 || ingredientCount > 64) throw new IllegalArgumentException("Invalid ingredient count");
        if (processingTime < 1 || processingTime > 32767) throw new IllegalArgumentException("Invalid processing time");
        output = output.copy();
        if (output.isEmpty() || output.getCount() > Math.min(64, output.getMaxStackSize()))
            throw new IllegalArgumentException("Invalid washing output");
    }

    public boolean accepts(ItemStack stack) { return ingredient.test(stack); }

    @Override public boolean matches(WashingInput input, Level level) {
        return !input.stack().isEmpty() && ingredient.test(input.stack()) && input.stack().getCount() >= ingredientCount;
    }

    public ItemStack result() { return output.copy(); }

    @Override public ItemStack assemble(WashingInput input, HolderLookup.Provider registries) { return result(); }
    @Override public ItemStack getResultItem(HolderLookup.Provider registries) { return result(); }
    @Override public boolean canCraftInDimensions(int width, int height) { return true; }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverSkyblock.WASHING_SERIALIZER.get(); }
    @Override public RecipeType<?> getType() { return ForeverSkyblock.WASHING.get(); }

    public static final class Serializer implements RecipeSerializer<WashingRecipe> {
        private static final MapCodec<WashingRecipe> CODEC = RecordCodecBuilder.mapCodec(instance -> instance.group(
                Ingredient.CODEC.fieldOf("ingredient").forGetter(WashingRecipe::ingredient),
                Codec.intRange(1, 64).optionalFieldOf("ingredient_count", 1).forGetter(WashingRecipe::ingredientCount),
                ItemStack.CODEC.fieldOf("result").forGetter(WashingRecipe::output),
                Codec.intRange(1, 32767).optionalFieldOf("processing_time", DEFAULT_TIME).forGetter(WashingRecipe::processingTime)
        ).apply(instance, WashingRecipe::new));
        private static final StreamCodec<RegistryFriendlyByteBuf, WashingRecipe> STREAM = new StreamCodec<>() {
            @Override public WashingRecipe decode(RegistryFriendlyByteBuf buf) {
                return new WashingRecipe(
                        Ingredient.CONTENTS_STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf),
                        ItemStack.STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf));
            }
            @Override public void encode(RegistryFriendlyByteBuf buf, WashingRecipe recipe) {
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.ingredient());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.ingredientCount());
                ItemStack.STREAM_CODEC.encode(buf, recipe.output());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.processingTime());
            }
        };
        @Override public MapCodec<WashingRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, WashingRecipe> streamCodec() { return STREAM; }
    }
}

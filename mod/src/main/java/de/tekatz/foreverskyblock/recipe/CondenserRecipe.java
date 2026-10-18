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

public record CondenserRecipe(Ingredient ingredient, int ingredientCount, Ingredient secondaryIngredient, int fragmentCount,
                              ItemStack output, int processingTime, int minimumTier) implements Recipe<CondenserInput> {
    public static final int DEFAULT_TIME = 160;
    public CondenserRecipe {
        if (ingredientCount < 1 || ingredientCount > 64) throw new IllegalArgumentException("Invalid primary ingredient count");
        if (fragmentCount < 1 || fragmentCount > 64) throw new IllegalArgumentException("Invalid output count");
        if (processingTime < 1 || processingTime > 32767) throw new IllegalArgumentException("Invalid processing time");
        if (minimumTier < 1 || minimumTier > 2) throw new IllegalArgumentException("Invalid condenser tier");
        output = output.copy();
        if (!output.isEmpty() && output.getCount() > Math.min(64, output.getMaxStackSize()))
            throw new IllegalArgumentException("Invalid output stack");
    }
    public boolean requiresModule() { return !secondaryIngredient.isEmpty(); }
    public boolean accepts(ItemStack stack) {
        return ingredient.test(stack) || requiresModule() && secondaryIngredient.test(stack);
    }
    public boolean availableToTier(int tier) { return tier >= minimumTier; }
    @Override public boolean matches(CondenserInput input, Level level) {
        if (input.first().isEmpty()) return false;
        if (!requiresModule()) return ingredient.test(input.first()) && input.first().getCount() >= ingredientCount;
        if (input.second().isEmpty()) return false;
        boolean direct = ingredient.test(input.first()) && input.first().getCount() >= ingredientCount
                && secondaryIngredient.test(input.second());
        boolean swapped = secondaryIngredient.test(input.first()) && ingredient.test(input.second())
                && input.second().getCount() >= ingredientCount;
        return direct || swapped;
    }
    public int primaryConsumption(CondenserInput input) {
        return ingredient.test(input.first()) ? ingredientCount : 1;
    }
    public int secondaryConsumption(CondenserInput input) {
        return requiresModule() && ingredient.test(input.second()) ? ingredientCount : 1;
    }
    public ItemStack result() {
        return output.isEmpty() ? new ItemStack(ForeverSkyblock.STONE_FRAGMENT.get(), fragmentCount) : output.copy();
    }
    @Override public ItemStack assemble(CondenserInput input, HolderLookup.Provider registries) { return result(); }
    @Override public ItemStack getResultItem(HolderLookup.Provider registries) { return result(); }
    @Override public boolean canCraftInDimensions(int width, int height) { return true; }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverSkyblock.CONDENSING_SERIALIZER.get(); }
    @Override public RecipeType<?> getType() { return ForeverSkyblock.CONDENSING.get(); }
    public static final class Serializer implements RecipeSerializer<CondenserRecipe> {
        private static final MapCodec<CondenserRecipe> CODEC = RecordCodecBuilder.mapCodec(instance -> instance.group(
                Ingredient.CODEC.fieldOf("ingredient").forGetter(CondenserRecipe::ingredient),
                Codec.intRange(1, 64).optionalFieldOf("ingredient_count", 1).forGetter(CondenserRecipe::ingredientCount),
                Ingredient.CODEC.optionalFieldOf("secondary_ingredient", Ingredient.EMPTY).forGetter(CondenserRecipe::secondaryIngredient),
                Codec.intRange(1, 64).optionalFieldOf("fragment_count", 1).forGetter(CondenserRecipe::fragmentCount),
                ItemStack.CODEC.optionalFieldOf("result", ItemStack.EMPTY).forGetter(CondenserRecipe::output),
                Codec.intRange(1, 32767).optionalFieldOf("processing_time", DEFAULT_TIME).forGetter(CondenserRecipe::processingTime),
                Codec.intRange(1, 2).optionalFieldOf("minimum_tier", 1).forGetter(CondenserRecipe::minimumTier)
        ).apply(instance, CondenserRecipe::new));
        private static final StreamCodec<RegistryFriendlyByteBuf, CondenserRecipe> STREAM = new StreamCodec<>() {
            @Override public CondenserRecipe decode(RegistryFriendlyByteBuf buf) {
                return new CondenserRecipe(
                        Ingredient.CONTENTS_STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf),
                        Ingredient.CONTENTS_STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf),
                        ItemStack.OPTIONAL_STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf));
            }
            @Override public void encode(RegistryFriendlyByteBuf buf, CondenserRecipe recipe) {
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.ingredient());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.ingredientCount());
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.secondaryIngredient());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.fragmentCount());
                ItemStack.OPTIONAL_STREAM_CODEC.encode(buf, recipe.output());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.processingTime());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.minimumTier());
            }
        };
        @Override public MapCodec<CondenserRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, CondenserRecipe> streamCodec() { return STREAM; }
    }
}

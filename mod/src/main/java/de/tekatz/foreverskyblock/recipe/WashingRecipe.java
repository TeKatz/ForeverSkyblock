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

public record WashingRecipe(Ingredient ingredient, int ingredientCount, ItemStack output, ItemStack secondaryOutput,
                            ItemStack bonusOutput, int bonusChancePercent, int processingTime)
        implements Recipe<WashingInput> {
    public static final int DEFAULT_TIME = 160;

    public WashingRecipe {
        if (ingredientCount < 1 || ingredientCount > 64) throw new IllegalArgumentException("Invalid ingredient count");
        if (processingTime < 1 || processingTime > 32767) throw new IllegalArgumentException("Invalid processing time");
        if (bonusChancePercent < 0 || bonusChancePercent > 100) throw new IllegalArgumentException("Invalid bonus chance");
        output = output.copy();
        secondaryOutput = secondaryOutput.copy();
        bonusOutput = bonusOutput.copy();
        validateOutput(output, false, "washing output");
        validateOutput(secondaryOutput, true, "secondary washing output");
        validateOutput(bonusOutput, true, "bonus washing output");
        if (bonusOutput.isEmpty() != (bonusChancePercent == 0))
            throw new IllegalArgumentException("Bonus output and bonus chance must be configured together");
    }

    private static void validateOutput(ItemStack stack, boolean mayBeEmpty, String name) {
        if (!mayBeEmpty && stack.isEmpty()) throw new IllegalArgumentException("Invalid " + name);
        if (!stack.isEmpty() && stack.getCount() > Math.min(64, stack.getMaxStackSize()))
            throw new IllegalArgumentException("Invalid " + name);
    }

    public boolean accepts(ItemStack stack) { return ingredient.test(stack); }

    @Override public boolean matches(WashingInput input, Level level) {
        return !input.stack().isEmpty() && ingredient.test(input.stack()) && input.stack().getCount() >= ingredientCount;
    }

    public ItemStack result() { return output.copy(); }
    public ItemStack secondaryResult() { return secondaryOutput.copy(); }
    public ItemStack bonusResult() { return bonusOutput.copy(); }
    public boolean hasSecondaryResult() { return !secondaryOutput.isEmpty(); }
    public boolean hasBonusResult() { return !bonusOutput.isEmpty() && bonusChancePercent > 0; }

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
                ItemStack.CODEC.optionalFieldOf("secondary_result", ItemStack.EMPTY).forGetter(WashingRecipe::secondaryOutput),
                ItemStack.CODEC.optionalFieldOf("bonus_result", ItemStack.EMPTY).forGetter(WashingRecipe::bonusOutput),
                Codec.intRange(0, 100).optionalFieldOf("bonus_chance", 0).forGetter(WashingRecipe::bonusChancePercent),
                Codec.intRange(1, 32767).optionalFieldOf("processing_time", DEFAULT_TIME).forGetter(WashingRecipe::processingTime)
        ).apply(instance, WashingRecipe::new));
        private static final StreamCodec<RegistryFriendlyByteBuf, WashingRecipe> STREAM = new StreamCodec<>() {
            @Override public WashingRecipe decode(RegistryFriendlyByteBuf buf) {
                return new WashingRecipe(
                        Ingredient.CONTENTS_STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf),
                        ItemStack.STREAM_CODEC.decode(buf),
                        ItemStack.OPTIONAL_STREAM_CODEC.decode(buf),
                        ItemStack.OPTIONAL_STREAM_CODEC.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf),
                        ByteBufCodecs.VAR_INT.decode(buf));
            }
            @Override public void encode(RegistryFriendlyByteBuf buf, WashingRecipe recipe) {
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.ingredient());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.ingredientCount());
                ItemStack.STREAM_CODEC.encode(buf, recipe.output());
                ItemStack.OPTIONAL_STREAM_CODEC.encode(buf, recipe.secondaryOutput());
                ItemStack.OPTIONAL_STREAM_CODEC.encode(buf, recipe.bonusOutput());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.bonusChancePercent());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.processingTime());
            }
        };
        @Override public MapCodec<WashingRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, WashingRecipe> streamCodec() { return STREAM; }
    }
}

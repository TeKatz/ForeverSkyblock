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

public record RefiningRecipe(Ingredient ingredient, int ingredientCount, Ingredient catalyst, int catalystCount,
                             ItemStack output, int processingTime, int energyPerTick) implements Recipe<RefiningInput> {
    public static final int DEFAULT_TIME = 200;
    public static final int DEFAULT_ENERGY = 80;

    public RefiningRecipe {
        if (ingredientCount < 1 || ingredientCount > 64) throw new IllegalArgumentException("Invalid refining ingredient count");
        if (catalystCount < 1 || catalystCount > 64) throw new IllegalArgumentException("Invalid refining catalyst count");
        if (processingTime < 1 || processingTime > 32767) throw new IllegalArgumentException("Invalid refining time");
        if (energyPerTick < 1 || energyPerTick > 10000) throw new IllegalArgumentException("Invalid refining energy cost");
        output = output.copy();
        if (output.isEmpty() || output.getCount() > Math.min(64, output.getMaxStackSize()))
            throw new IllegalArgumentException("Invalid refining output");
    }

    public boolean acceptsIngredient(ItemStack stack) { return !stack.isEmpty() && ingredient.test(stack); }
    public boolean acceptsCatalyst(ItemStack stack) { return !stack.isEmpty() && catalyst.test(stack); }

    @Override public boolean matches(RefiningInput input, Level level) {
        return acceptsIngredient(input.primary()) && input.primary().getCount() >= ingredientCount
                && acceptsCatalyst(input.catalyst()) && input.catalyst().getCount() >= catalystCount;
    }
    public ItemStack result() { return output.copy(); }
    @Override public ItemStack assemble(RefiningInput input, HolderLookup.Provider registries) { return result(); }
    @Override public ItemStack getResultItem(HolderLookup.Provider registries) { return result(); }
    @Override public boolean canCraftInDimensions(int width, int height) { return true; }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverSkyblock.REFINING_SERIALIZER.get(); }
    @Override public RecipeType<?> getType() { return ForeverSkyblock.REFINING.get(); }

    public static final class Serializer implements RecipeSerializer<RefiningRecipe> {
        private static final MapCodec<RefiningRecipe> CODEC = RecordCodecBuilder.mapCodec(instance -> instance.group(
                Ingredient.CODEC.fieldOf("ingredient").forGetter(RefiningRecipe::ingredient),
                Codec.intRange(1, 64).optionalFieldOf("ingredient_count", 1).forGetter(RefiningRecipe::ingredientCount),
                Ingredient.CODEC.fieldOf("catalyst").forGetter(RefiningRecipe::catalyst),
                Codec.intRange(1, 64).optionalFieldOf("catalyst_count", 1).forGetter(RefiningRecipe::catalystCount),
                ItemStack.CODEC.fieldOf("result").forGetter(RefiningRecipe::output),
                Codec.intRange(1, 32767).optionalFieldOf("processing_time", DEFAULT_TIME).forGetter(RefiningRecipe::processingTime),
                Codec.intRange(1, 10000).optionalFieldOf("energy_per_tick", DEFAULT_ENERGY).forGetter(RefiningRecipe::energyPerTick)
        ).apply(instance, RefiningRecipe::new));
        private static final StreamCodec<RegistryFriendlyByteBuf, RefiningRecipe> STREAM = new StreamCodec<>() {
            @Override public RefiningRecipe decode(RegistryFriendlyByteBuf buf) {
                return new RefiningRecipe(Ingredient.CONTENTS_STREAM_CODEC.decode(buf), ByteBufCodecs.VAR_INT.decode(buf),
                        Ingredient.CONTENTS_STREAM_CODEC.decode(buf), ByteBufCodecs.VAR_INT.decode(buf),
                        ItemStack.STREAM_CODEC.decode(buf), ByteBufCodecs.VAR_INT.decode(buf), ByteBufCodecs.VAR_INT.decode(buf));
            }
            @Override public void encode(RegistryFriendlyByteBuf buf, RefiningRecipe recipe) {
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.ingredient());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.ingredientCount());
                Ingredient.CONTENTS_STREAM_CODEC.encode(buf, recipe.catalyst());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.catalystCount());
                ItemStack.STREAM_CODEC.encode(buf, recipe.output());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.processingTime());
                ByteBufCodecs.VAR_INT.encode(buf, recipe.energyPerTick());
            }
        };
        @Override public MapCodec<RefiningRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, RefiningRecipe> streamCodec() { return STREAM; }
    }
}

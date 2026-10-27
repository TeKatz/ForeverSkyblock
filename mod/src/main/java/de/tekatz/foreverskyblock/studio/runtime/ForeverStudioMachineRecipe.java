package de.tekatz.foreverskyblock.studio.runtime;

import com.mojang.serialization.Codec;
import com.mojang.serialization.MapCodec;
import com.mojang.serialization.codecs.RecordCodecBuilder;
import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
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

import java.util.List;
import java.util.Optional;

/** One generated recipe book per Studio machine, containing all recipes authored in Phase 04. */
public record ForeverStudioMachineRecipe(String machine, List<Entry> recipes)
        implements Recipe<ForeverStudioMachineInput> {

    public record Entry(Ingredient input, int inputCount, ItemStack output, int processingTime, int energyPerTick) {
        private static final Codec<Entry> CODEC = RecordCodecBuilder.create(instance -> instance.group(
                Ingredient.CODEC.fieldOf("input").forGetter(Entry::input),
                Codec.intRange(1, 64).optionalFieldOf("input_count", 1).forGetter(Entry::inputCount),
                ItemStack.CODEC.fieldOf("result").forGetter(Entry::output),
                Codec.intRange(1, 1_000_000).optionalFieldOf("processing_time", 100).forGetter(Entry::processingTime),
                Codec.intRange(0, 1_000_000).optionalFieldOf("energy_per_tick", 0).forGetter(Entry::energyPerTick)
        ).apply(instance, Entry::new));

        public Entry {
            if (inputCount < 1 || inputCount > 64) throw new IllegalArgumentException("Invalid input count");
            if (processingTime < 1 || processingTime > 1_000_000) throw new IllegalArgumentException("Invalid processing time");
            if (energyPerTick < 0 || energyPerTick > 1_000_000) throw new IllegalArgumentException("Invalid FE/t");
            output = output.copy();
            if (output.isEmpty() || output.getCount() > Math.min(64, output.getMaxStackSize()))
                throw new IllegalArgumentException("Invalid machine output");
        }

        public boolean accepts(ItemStack stack) {
            return !stack.isEmpty() && stack.getCount() >= inputCount && input.test(stack);
        }

        public ItemStack result() { return output.copy(); }
    }

    public ForeverStudioMachineRecipe {
        machine = machine == null ? "" : machine.trim().toLowerCase(java.util.Locale.ROOT);
        recipes = List.copyOf(recipes);
        if (machine.isBlank()) throw new IllegalArgumentException("Machine id is required");
        if (recipes.isEmpty()) throw new IllegalArgumentException("A generated machine needs at least one recipe");
    }

    public Optional<Entry> find(ForeverStudioMachineInput input) {
        if (!machine.equals(input.machineId().toString())) return Optional.empty();
        return recipes.stream().filter(entry -> entry.accepts(input.input())).findFirst();
    }

    public boolean acceptsItem(ItemStack stack) {
        return recipes.stream().anyMatch(entry -> !stack.isEmpty() && entry.input().test(stack));
    }

    @Override public boolean matches(ForeverStudioMachineInput input, Level level) { return find(input).isPresent(); }
    @Override public ItemStack assemble(ForeverStudioMachineInput input, HolderLookup.Provider registries) {
        return find(input).map(Entry::result).orElse(ItemStack.EMPTY);
    }
    @Override public ItemStack getResultItem(HolderLookup.Provider registries) { return recipes.getFirst().result(); }
    @Override public boolean canCraftInDimensions(int width, int height) { return true; }
    @Override public RecipeSerializer<?> getSerializer() { return ForeverStudioGeneratedMachines.MACHINE_RECIPE_SERIALIZER.get(); }
    @Override public RecipeType<?> getType() { return ForeverStudioGeneratedMachines.MACHINE_RECIPE_TYPE.get(); }

    public static final class Serializer implements RecipeSerializer<ForeverStudioMachineRecipe> {
        private static final MapCodec<ForeverStudioMachineRecipe> CODEC = RecordCodecBuilder.mapCodec(instance -> instance.group(
                Codec.STRING.fieldOf("machine").forGetter(ForeverStudioMachineRecipe::machine),
                Entry.CODEC.listOf().fieldOf("recipes").forGetter(ForeverStudioMachineRecipe::recipes)
        ).apply(instance, ForeverStudioMachineRecipe::new));

        private static final StreamCodec<RegistryFriendlyByteBuf, ForeverStudioMachineRecipe> STREAM = new StreamCodec<>() {
            @Override public ForeverStudioMachineRecipe decode(RegistryFriendlyByteBuf buf) {
                String machine = buf.readUtf(256);
                int count = ByteBufCodecs.VAR_INT.decode(buf);
                if (count < 1 || count > 256) throw new IllegalArgumentException("Invalid generated recipe count");
                java.util.ArrayList<Entry> entries = new java.util.ArrayList<>(count);
                for (int i = 0; i < count; i++) {
                    Ingredient ingredient = Ingredient.CONTENTS_STREAM_CODEC.decode(buf);
                    int inputCount = ByteBufCodecs.VAR_INT.decode(buf);
                    ItemStack output = ItemStack.STREAM_CODEC.decode(buf);
                    int processingTime = ByteBufCodecs.VAR_INT.decode(buf);
                    int energyPerTick = ByteBufCodecs.VAR_INT.decode(buf);
                    entries.add(new Entry(ingredient, inputCount, output, processingTime, energyPerTick));
                }
                return new ForeverStudioMachineRecipe(machine, entries);
            }

            @Override public void encode(RegistryFriendlyByteBuf buf, ForeverStudioMachineRecipe recipe) {
                buf.writeUtf(recipe.machine(), 256);
                ByteBufCodecs.VAR_INT.encode(buf, recipe.recipes().size());
                for (Entry entry : recipe.recipes()) {
                    Ingredient.CONTENTS_STREAM_CODEC.encode(buf, entry.input());
                    ByteBufCodecs.VAR_INT.encode(buf, entry.inputCount());
                    ItemStack.STREAM_CODEC.encode(buf, entry.output());
                    ByteBufCodecs.VAR_INT.encode(buf, entry.processingTime());
                    ByteBufCodecs.VAR_INT.encode(buf, entry.energyPerTick());
                }
            }
        };

        @Override public MapCodec<ForeverStudioMachineRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, ForeverStudioMachineRecipe> streamCodec() { return STREAM; }
    }
}

package de.tekatz.foreverskyblock.studio.runtime;

import com.mojang.serialization.Codec;
import com.mojang.serialization.MapCodec;
import com.mojang.serialization.codecs.RecordCodecBuilder;
import de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines;
import net.minecraft.core.HolderLookup;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.ByteBufCodecs;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.Ingredient;
import net.minecraft.world.item.crafting.Recipe;
import net.minecraft.world.item.crafting.RecipeSerializer;
import net.minecraft.world.item.crafting.RecipeType;
import net.minecraft.world.level.Level;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/** One generated recipe book per Studio machine, supporting item, fluid and energy processing. */
public record ForeverStudioMachineRecipe(String machine, List<Entry> recipes) implements Recipe<ForeverStudioMachineInput> {
    public record Input(Ingredient ingredient, int count) {
        private static final Codec<Input> CODEC = RecordCodecBuilder.create(instance -> instance.group(
                Ingredient.CODEC.fieldOf("ingredient").forGetter(Input::ingredient),
                Codec.intRange(1, 64).optionalFieldOf("count", 1).forGetter(Input::count)
        ).apply(instance, Input::new));
        public boolean accepts(ItemStack stack) {
            return !stack.isEmpty() && stack.getCount() >= count && ingredient.test(stack);
        }
    }

    /** Fluid id is stored as a resource location string to keep datapack/network contracts deterministic. */
    public record FluidRequirement(int tank, String fluid, int amount) {
        private static final Codec<FluidRequirement> CODEC = RecordCodecBuilder.create(instance -> instance.group(
                Codec.intRange(0, 7).fieldOf("tank").forGetter(FluidRequirement::tank),
                Codec.STRING.fieldOf("fluid").forGetter(FluidRequirement::fluid),
                Codec.intRange(1, 2_000_000_000).fieldOf("amount").forGetter(FluidRequirement::amount)
        ).apply(instance, FluidRequirement::new));

        public FluidRequirement {
            fluid = fluid == null ? "" : fluid.trim().toLowerCase(java.util.Locale.ROOT);
            if (ResourceLocation.tryParse(fluid) == null) throw new IllegalArgumentException("Invalid machine fluid id");
            if (tank < 0 || tank > 7 || amount < 1) throw new IllegalArgumentException("Invalid machine fluid requirement");
        }

        public ResourceLocation fluidId() {
            ResourceLocation id = ResourceLocation.tryParse(fluid);
            if (id == null) throw new IllegalStateException("Validated machine fluid id became invalid");
            return id;
        }
    }

    public record Entry(List<Input> inputs,
                        List<ItemStack> outputs,
                        List<FluidRequirement> fluidInputs,
                        List<FluidRequirement> fluidOutputs,
                        int processingTime,
                        int energyPerTick,
                        int energyOutputPerTick) {
        private static final Codec<Entry> CODEC = RecordCodecBuilder.create(instance -> instance.group(
                Input.CODEC.listOf().optionalFieldOf("inputs", List.of()).forGetter(Entry::inputs),
                ItemStack.CODEC.listOf().optionalFieldOf("results", List.of()).forGetter(Entry::outputs),
                FluidRequirement.CODEC.listOf().optionalFieldOf("fluid_inputs", List.of()).forGetter(Entry::fluidInputs),
                FluidRequirement.CODEC.listOf().optionalFieldOf("fluid_results", List.of()).forGetter(Entry::fluidOutputs),
                Ingredient.CODEC.optionalFieldOf("input").forGetter(entry -> Optional.empty()),
                Codec.intRange(1, 64).optionalFieldOf("input_count", 1).forGetter(entry -> 1),
                ItemStack.CODEC.optionalFieldOf("result").forGetter(entry -> Optional.empty()),
                Codec.intRange(1, 1_000_000).optionalFieldOf("processing_time", 100).forGetter(Entry::processingTime),
                Codec.intRange(0, 1_000_000).optionalFieldOf("energy_per_tick", 0).forGetter(Entry::energyPerTick),
                Codec.intRange(0, 1_000_000).optionalFieldOf("energy_output_per_tick", 0).forGetter(Entry::energyOutputPerTick)
        ).apply(instance, (inputs, outputs, fluidInputs, fluidOutputs, legacyInput, legacyCount, legacyOutput,
                           time, energy, energyOutput) -> {
            List<Input> normalizedInputs = inputs.isEmpty() && legacyInput.isPresent()
                    ? List.of(new Input(legacyInput.get(), legacyCount)) : inputs;
            List<ItemStack> normalizedOutputs = outputs.isEmpty() && legacyOutput.isPresent()
                    ? List.of(legacyOutput.get()) : outputs;
            return new Entry(normalizedInputs, normalizedOutputs, fluidInputs, fluidOutputs, time, energy, energyOutput);
        }));

        public Entry {
            inputs = List.copyOf(inputs);
            outputs = outputs.stream().map(ItemStack::copy).toList();
            fluidInputs = List.copyOf(fluidInputs);
            fluidOutputs = List.copyOf(fluidOutputs);
            if (inputs.isEmpty() || inputs.size() > 32) throw new IllegalArgumentException("Invalid machine inputs");
            if (outputs.size() > 32) throw new IllegalArgumentException("Invalid machine outputs");
            if (fluidInputs.size() > 8 || fluidOutputs.size() > 8) throw new IllegalArgumentException("Invalid machine fluids");
            if (outputs.isEmpty() && fluidOutputs.isEmpty() && energyOutputPerTick <= 0)
                throw new IllegalArgumentException("Machine recipe must produce an item, fluid or energy");
            if (processingTime < 1 || processingTime > 1_000_000) throw new IllegalArgumentException("Invalid processing time");
            if (energyPerTick < 0 || energyPerTick > 1_000_000 || energyOutputPerTick < 0 || energyOutputPerTick > 1_000_000)
                throw new IllegalArgumentException("Invalid FE/t");
            for (ItemStack output : outputs)
                if (output.isEmpty() || output.getCount() > Math.min(64, output.getMaxStackSize()))
                    throw new IllegalArgumentException("Invalid machine output");
        }

        public boolean acceptsItems(ForeverStudioMachineInput input) {
            if (input.size() != inputs.size()) return false;
            for (int i = 0; i < inputs.size(); i++)
                if (!inputs.get(i).accepts(input.getItem(i))) return false;
            return true;
        }

        public boolean accepts(ForeverStudioMachineInput input) { return acceptsItems(input); }
        public List<ItemStack> results() { return outputs.stream().map(ItemStack::copy).toList(); }
        public ItemStack result() { return outputs.isEmpty() ? ItemStack.EMPTY : outputs.getFirst().copy(); }
        public int inputCount() { return inputs.getFirst().count(); }
        public Ingredient input() { return inputs.getFirst().ingredient(); }
        public ItemStack output() { return result(); }
    }

    public ForeverStudioMachineRecipe {
        machine = machine == null ? "" : machine.trim().toLowerCase(java.util.Locale.ROOT);
        recipes = List.copyOf(recipes);
        if (machine.isBlank()) throw new IllegalArgumentException("Machine id is required");
        if (recipes.isEmpty()) throw new IllegalArgumentException("A generated machine needs at least one recipe");
    }

    public Optional<Entry> find(ForeverStudioMachineInput input) {
        if (!machine.equals(input.machineId().toString())) return Optional.empty();
        return recipes.stream().filter(entry -> entry.acceptsItems(input)).findFirst();
    }

    public boolean acceptsItem(int inputIndex, ItemStack stack) {
        return recipes.stream().anyMatch(entry -> inputIndex >= 0 && inputIndex < entry.inputs().size()
                && entry.inputs().get(inputIndex).ingredient().test(stack));
    }
    public boolean acceptsItem(ItemStack stack) { return acceptsItem(0, stack); }

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
                ArrayList<Entry> entries = new ArrayList<>(count);
                for (int i = 0; i < count; i++) {
                    int inputCount = ByteBufCodecs.VAR_INT.decode(buf);
                    if (inputCount < 1 || inputCount > 32) throw new IllegalArgumentException("Invalid generated input count");
                    ArrayList<Input> inputs = new ArrayList<>(inputCount);
                    for (int j = 0; j < inputCount; j++)
                        inputs.add(new Input(Ingredient.CONTENTS_STREAM_CODEC.decode(buf), ByteBufCodecs.VAR_INT.decode(buf)));

                    int outputCount = ByteBufCodecs.VAR_INT.decode(buf);
                    if (outputCount < 0 || outputCount > 32) throw new IllegalArgumentException("Invalid generated output count");
                    ArrayList<ItemStack> outputs = new ArrayList<>(outputCount);
                    for (int j = 0; j < outputCount; j++) outputs.add(ItemStack.STREAM_CODEC.decode(buf));

                    int fluidInputCount = ByteBufCodecs.VAR_INT.decode(buf);
                    if (fluidInputCount < 0 || fluidInputCount > 8) throw new IllegalArgumentException("Invalid generated fluid input count");
                    ArrayList<FluidRequirement> fluidInputs = new ArrayList<>(fluidInputCount);
                    for (int j = 0; j < fluidInputCount; j++) fluidInputs.add(readFluid(buf));

                    int fluidOutputCount = ByteBufCodecs.VAR_INT.decode(buf);
                    if (fluidOutputCount < 0 || fluidOutputCount > 8) throw new IllegalArgumentException("Invalid generated fluid output count");
                    ArrayList<FluidRequirement> fluidOutputs = new ArrayList<>(fluidOutputCount);
                    for (int j = 0; j < fluidOutputCount; j++) fluidOutputs.add(readFluid(buf));

                    entries.add(new Entry(inputs, outputs, fluidInputs, fluidOutputs,
                            ByteBufCodecs.VAR_INT.decode(buf), ByteBufCodecs.VAR_INT.decode(buf), ByteBufCodecs.VAR_INT.decode(buf)));
                }
                return new ForeverStudioMachineRecipe(machine, entries);
            }

            @Override public void encode(RegistryFriendlyByteBuf buf, ForeverStudioMachineRecipe recipe) {
                buf.writeUtf(recipe.machine(), 256);
                ByteBufCodecs.VAR_INT.encode(buf, recipe.recipes().size());
                for (Entry entry : recipe.recipes()) {
                    ByteBufCodecs.VAR_INT.encode(buf, entry.inputs().size());
                    for (Input input : entry.inputs()) {
                        Ingredient.CONTENTS_STREAM_CODEC.encode(buf, input.ingredient());
                        ByteBufCodecs.VAR_INT.encode(buf, input.count());
                    }
                    ByteBufCodecs.VAR_INT.encode(buf, entry.outputs().size());
                    for (ItemStack output : entry.outputs()) ItemStack.STREAM_CODEC.encode(buf, output);
                    ByteBufCodecs.VAR_INT.encode(buf, entry.fluidInputs().size());
                    for (FluidRequirement fluid : entry.fluidInputs()) writeFluid(buf, fluid);
                    ByteBufCodecs.VAR_INT.encode(buf, entry.fluidOutputs().size());
                    for (FluidRequirement fluid : entry.fluidOutputs()) writeFluid(buf, fluid);
                    ByteBufCodecs.VAR_INT.encode(buf, entry.processingTime());
                    ByteBufCodecs.VAR_INT.encode(buf, entry.energyPerTick());
                    ByteBufCodecs.VAR_INT.encode(buf, entry.energyOutputPerTick());
                }
            }

            private FluidRequirement readFluid(RegistryFriendlyByteBuf buf) {
                return new FluidRequirement(ByteBufCodecs.VAR_INT.decode(buf), buf.readUtf(256), ByteBufCodecs.VAR_INT.decode(buf));
            }

            private void writeFluid(RegistryFriendlyByteBuf buf, FluidRequirement fluid) {
                ByteBufCodecs.VAR_INT.encode(buf, fluid.tank());
                buf.writeUtf(fluid.fluid(), 256);
                ByteBufCodecs.VAR_INT.encode(buf, fluid.amount());
            }
        };

        @Override public MapCodec<ForeverStudioMachineRecipe> codec() { return CODEC; }
        @Override public StreamCodec<RegistryFriendlyByteBuf, ForeverStudioMachineRecipe> streamCodec() { return STREAM; }
    }
}

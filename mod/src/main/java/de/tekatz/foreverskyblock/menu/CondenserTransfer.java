package de.tekatz.foreverskyblock.menu;

import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.crafting.Ingredient;

/** Shared, side-effect-free transfer planning for the JEI preview and authoritative server execution. */
public final class CondenserTransfer {
    public enum Error { NONE, MODULE, EDITING, CURSOR, FILTERS, MISSING, SPACE }
    public record Plan(Error error, List<ItemStack> inventory, List<ItemStack> inputs) {
        private static Plan failed(Error error) { return new Plan(error, List.of(), List.of()); }
        public boolean success() { return error == Error.NONE; }
    }
    private CondenserTransfer() { }
    public static Plan plan(CondenserMenu menu, CondenserRecipe recipe, boolean maxTransfer) {
        if (menu.editing()) return Plan.failed(Error.EDITING);
        if (!menu.getCarried().isEmpty()) return Plan.failed(Error.CURSOR);
        if (recipe.requiresModule() && !menu.hasModule()) return Plan.failed(Error.MODULE);
        ItemStack[] source = new ItemStack[38];
        for (int i = 0; i < 36; i++) source[i] = menu.getSlot(i + 3).getItem().copy();
        source[36] = menu.getSlot(0).getItem().copy();
        source[37] = recipe.requiresModule() ? menu.getSlot(2).getItem().copy() : ItemStack.EMPTY;
        Ingredient[][] orders = recipe.requiresModule()
                ? new Ingredient[][]{{recipe.ingredient(), recipe.secondaryIngredient()}, {recipe.secondaryIngredient(), recipe.ingredient()}}
                : new Ingredient[][]{{recipe.ingredient()}};
        boolean validFilters = false, missing = true;
        Plan best = null;
        int bestSets = 0, bestKept = -1;
        for (Ingredient[] order : orders) {
            ItemStack[] filters = { menu.filter(0), menu.filter(2) };
            boolean compatible = true;
            for (int i = 0; i < order.length; i++) if (!filters[i].isEmpty() && !order[i].test(filters[i])) compatible = false;
            if (!compatible) continue;
            validFilters = true;
            int maxSets = 1;
            if (maxTransfer) {
                maxSets = 64;
                for (int i = 0; i < order.length; i++) {
                    int total = 0;
                    for (ItemStack stack : source) if (matches(order[i], filters[i], stack)) total += stack.getCount();
                    maxSets = Math.min(maxSets, total);
                }
            }
            for (int sets = maxSets; sets >= 1; sets--) {
                // Prefer keeping matching real input stacks; retry with one set if they compete for ingredients.
                for (boolean keep : new boolean[]{true, false}) {
                    var search = new Search(source, order, filters, sets, keep);
                    Plan attempt = search.find(0, copy(source), new ItemStack[order.length]);
                    if (search.foundIngredients) missing = false;
                    if (attempt == null) continue;
                    int kept = 0;
                    for (int i = 0; i < order.length; i++) if (ItemStack.isSameItemSameComponents(source[36 + i], attempt.inputs().get(i))) kept++;
                    if (sets > bestSets || sets == bestSets && kept > bestKept) { best = attempt; bestSets = sets; bestKept = kept; }
                    break;
                }
                if (best != null && bestSets >= sets) break;
            }
        }
        return best != null ? best : Plan.failed(!validFilters ? Error.FILTERS : missing ? Error.MISSING : Error.SPACE);
    }
    private static boolean matches(Ingredient ingredient, ItemStack filter, ItemStack stack) {
        return !stack.isEmpty() && ingredient.test(stack) && (filter.isEmpty() || ItemStack.isSameItem(filter, stack));
    }
    private static ItemStack[] copy(ItemStack[] source) {
        ItemStack[] copy = new ItemStack[source.length];
        for (int i = 0; i < source.length; i++) copy[i] = source[i].copy();
        return copy;
    }
    private static final class Search {
        private final ItemStack[] original;
        private final Ingredient[] ingredients;
        private final ItemStack[] filters;
        private final int sets;
        private final boolean keep;
        private boolean foundIngredients;
        private Search(ItemStack[] original, Ingredient[] ingredients, ItemStack[] filters, int sets, boolean keep) {
            this.original = original; this.ingredients = ingredients; this.filters = filters; this.sets = sets; this.keep = keep;
        }
        private Plan find(int input, ItemStack[] pool, ItemStack[] targets) {
            if (input == ingredients.length) {
                foundIngredients = true;
                var inventory = new ArrayList<ItemStack>(36);
                for (int i = 0; i < 36; i++) inventory.add(pool[i].copy());
                for (int i = 36; i < pool.length; i++) if (!returnToInventory(inventory, pool[i])) return null;
                return new Plan(Error.NONE, List.copyOf(inventory), List.of(copy(targets)));
            }
            List<ItemStack> variants = new ArrayList<>();
            // Check the current target first to retain an already loaded variant and its components.
            for (int index = -1; index < pool.length; index++) {
                ItemStack candidate = pool[index < 0 ? 36 + input : index];
                if (!matches(ingredients[input], filters[input], candidate)
                        || variants.stream().anyMatch(previous -> ItemStack.isSameItemSameComponents(previous, candidate))) continue;
                variants.add(candidate);
                int needed = sets;
                if (keep && ItemStack.isSameItemSameComponents(original[36 + input], candidate)) needed = Math.max(needed, original[36 + input].getCount());
                if (needed > Math.min(64, candidate.getMaxStackSize())) continue;
                int available = 0;
                for (ItemStack stack : pool) if (ItemStack.isSameItemSameComponents(stack, candidate)) available += stack.getCount();
                if (available < needed) continue;
                ItemStack[] remaining = copy(pool);
                int take = needed;
                for (int pass = 0; pass < remaining.length + 1 && take > 0; pass++) {
                    int i = pass == 0 ? 36 + input : pass - 1;
                    if (!ItemStack.isSameItemSameComponents(remaining[i], candidate)) continue;
                    int count = Math.min(take, remaining[i].getCount());
                    remaining[i].shrink(count); take -= count;
                }
                targets[input] = candidate.copyWithCount(needed);
                Plan plan = find(input + 1, remaining, targets);
                if (plan != null) return plan;
            }
            return null;
        }
    }
    private static boolean returnToInventory(List<ItemStack> inventory, ItemStack remainder) {
        ItemStack stack = remainder.copy();
        for (int pass = 0; pass < 2 && !stack.isEmpty(); pass++) for (int i = 0; i < inventory.size() && !stack.isEmpty(); i++) {
            ItemStack current = inventory.get(i);
            if (pass == 0 && !current.isEmpty() && ItemStack.isSameItemSameComponents(current, stack)) {
                int count = Math.min(stack.getCount(), Math.max(0, Math.min(64, current.getMaxStackSize()) - current.getCount()));
                current.grow(count); stack.shrink(count);
            } else if (pass == 1 && current.isEmpty()) {
                int count = Math.min(stack.getCount(), Math.min(64, stack.getMaxStackSize()));
                inventory.set(i, stack.copyWithCount(count)); stack.shrink(count);
            }
        }
        return stack.isEmpty();
    }
}

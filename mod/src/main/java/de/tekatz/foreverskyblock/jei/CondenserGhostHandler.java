package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.client.CondenserScreen;
import java.util.ArrayList;
import java.util.List;
import mezz.jei.api.gui.handlers.IGhostIngredientHandler;
import mezz.jei.api.ingredients.ITypedIngredient;
import mezz.jei.api.constants.VanillaTypes;
import net.minecraft.client.renderer.Rect2i;

public final class CondenserGhostHandler implements IGhostIngredientHandler<CondenserScreen> {
    @Override public <I> List<Target<I>> getTargetsTyped(CondenserScreen gui, ITypedIngredient<I> ingredient, boolean doStart) {
        var stack = ingredient.getIngredient(VanillaTypes.ITEM_STACK);
        if (!gui.getMenu().editing() || stack.isEmpty()) return List.of();
        List<Target<I>> targets = new ArrayList<>();
        for (int slot : new int[]{0, 2}) if (gui.getMenu().acceptsGhost(slot, stack.get())) {
            final int input = slot;
            targets.add(new Target<>() {
                @Override public Rect2i getArea() { return gui.inputArea(input); }
                @Override public void accept(I ignored) { gui.setGhost(input, stack.get()); }
            });
        }
        return targets;
    }
    @Override public void onComplete() { }
}

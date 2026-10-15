package de.tekatz.foreverskyblock.jei;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.menu.CondenserMenu;
import de.tekatz.foreverskyblock.menu.CondenserTransfer;
import de.tekatz.foreverskyblock.network.CondenserNetwork;
import de.tekatz.foreverskyblock.recipe.CondenserRecipe;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;
import mezz.jei.api.gui.ingredient.IRecipeSlotsView;
import mezz.jei.api.recipe.RecipeType;
import mezz.jei.api.recipe.transfer.IRecipeTransferContext;
import mezz.jei.api.recipe.transfer.IRecipeTransferError;
import mezz.jei.api.recipe.transfer.IRecipeTransferHandler;
import mezz.jei.api.recipe.transfer.IRecipeTransferHandlerHelper;
import mezz.jei.api.recipe.transfer.RecipeTransferResult;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.MenuType;
import net.minecraft.world.item.crafting.RecipeHolder;
import net.neoforged.neoforge.network.PacketDistributor;
import org.jetbrains.annotations.Nullable;

public final class CondenserRecipeTransferHandler implements IRecipeTransferHandler<CondenserMenu, RecipeHolder<CondenserRecipe>> {
    private final IRecipeTransferHandlerHelper helper;
    private final Map<Integer, IRecipeTransferContext<RecipeHolder<CondenserRecipe>, CondenserMenu>> pending = new HashMap<>();
    public CondenserRecipeTransferHandler(IRecipeTransferHandlerHelper helper) {
        this.helper = helper;
        CondenserNetwork.setTransferReceiver(this::complete);
    }
    @Override public Class<? extends CondenserMenu> getContainerClass() { return CondenserMenu.class; }
    @Override public Optional<MenuType<CondenserMenu>> getMenuType() { return Optional.of(ForeverSkyblock.CONDENSER_MENU.get()); }
    @Override public RecipeType<RecipeHolder<CondenserRecipe>> getRecipeType() { return CondenserJeiPlugin.TYPE; }
    private @Nullable IRecipeTransferError check(CondenserMenu menu, CondenserRecipe recipe, boolean maximum) {
        var plan = CondenserTransfer.plan(menu, recipe, maximum);
        return plan.success() ? null : helper.createUserErrorWithTooltip(Component.translatable(
                "jei.forever_skyblock.transfer." + plan.error().name().toLowerCase(java.util.Locale.ROOT)));
    }
    @Override public @Nullable IRecipeTransferError transferRecipe(IRecipeTransferContext<RecipeHolder<CondenserRecipe>, CondenserMenu> context, boolean doTransfer) {
        var error = check(context.getContainer(), context.getRecipe().value(), context.isMaxTransfer());
        if (error != null) { if (doTransfer) context.completeRecipeTransfer(RecipeTransferResult.REJECTED); return error; }
        if (doTransfer) {
            // The server reply completes the JEI request after validation and inventory synchronization.
            clearPending();
            pending.put(context.getTransferId(), context);
            PacketDistributor.sendToServer(new CondenserNetwork.Transfer(context.getContainer().containerId, context.getRecipe().id(),
                    context.isMaxTransfer(), context.getTransferId()));
        }
        return null;
    }
    @Override @SuppressWarnings("removal")
    public @Nullable IRecipeTransferError transferRecipe(CondenserMenu menu, RecipeHolder<CondenserRecipe> recipe, IRecipeSlotsView slots,
                                                       Player player, boolean maximum, boolean doTransfer) {
        var error = check(menu, recipe.value(), maximum);
        if (error == null && doTransfer) PacketDistributor.sendToServer(new CondenserNetwork.Transfer(menu.containerId, recipe.id(), maximum, -1));
        return error;
    }
    private void complete(CondenserNetwork.TransferResult result) {
        var context = pending.remove(result.requestId());
        if (context != null) context.completeRecipeTransfer(result.success() && context.getContainer().containerId == result.menuId()
                && net.minecraft.client.Minecraft.getInstance().player != null
                && net.minecraft.client.Minecraft.getInstance().player.containerMenu == context.getContainer()
                ? RecipeTransferResult.SUCCESS : RecipeTransferResult.REJECTED);
    }
    public void clearPending() {
        var previous = java.util.List.copyOf(pending.values());
        pending.clear();
        previous.forEach(context -> context.completeRecipeTransfer(RecipeTransferResult.REJECTED));
    }
}

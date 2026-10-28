using System.Windows;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _machineBuilderAssetExpansionPhase3Initialized;

    public void EnableForeverMachineBuilderAssetExpansionPhase3()
    {
        if (_machineBuilderAssetExpansionPhase3Initialized || _machineBuilderVisualSurface is null)
            return;
        _machineBuilderAssetExpansionPhase3Initialized = true;

        ReloadMachineBuilderExpandedAssetCatalog();
        _machineBuilderVisualSurface.IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                ReloadMachineBuilderExpandedAssetCatalog();
        };
    }

    private void ReloadMachineBuilderExpandedAssetCatalog()
    {
        // Reload from the active repository first so project-owned packs always win naturally.
        // The bundled expansion only fills IDs that are absent locally, which keeps source checkouts
        // and self-updated single-file builds on the exact same library contracts.
        _machineBuilderLibraryLoad = ForeverStudioLibraryCatalog.Load(_repoRoot);
        ForeverStudioVisualAssetExpansion.EnsureRegistered(_machineBuilderLibraryLoad);
    }
}

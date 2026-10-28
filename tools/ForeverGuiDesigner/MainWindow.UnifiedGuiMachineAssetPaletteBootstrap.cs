namespace ForeverGuiDesigner;

public partial class MainWindow
{
    public void EnableForeverUnifiedGuiMachineAssetPalette()
    {
        EnsureUnifiedGuiMachineAssetPalette();
        if (_studioGuiWorkspace is not null)
            _studioGuiWorkspace.IsVisibleChanged += (_, _) => RefreshUnifiedGuiMachineAssetPalette();
        ElementsList.SelectionChanged += (_, _) => RefreshUnifiedGuiMachineAssetPalette();
        RefreshUnifiedGuiMachineAssetPalette();
    }
}

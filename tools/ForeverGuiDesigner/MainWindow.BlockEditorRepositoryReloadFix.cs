using System.Windows;

namespace ForeverGuiDesigner;

/// <summary>
/// Keeps the Block Editor catalog valid when the Studio repository is scanned again
/// during the same application session. The foundation scan temporarily reuses the
/// machine selector for Forever machine templates, while the Block Model Viewer owns
/// that selector as a Minecraft block-asset catalog. Re-entering the Block Editor must
/// therefore verify the binding instead of trusting only the repository-path cache.
/// </summary>
public partial class MainWindow
{
    private bool _blockEditorRepositoryReloadFixInitialized;

    public void EnableForeverBlockEditorRepositoryReloadFix()
    {
        if (_blockEditorRepositoryReloadFixInitialized)
            return;
        _blockEditorRepositoryReloadFixInitialized = true;

        if (_studioBlockWorkspace is FrameworkElement workspace)
            workspace.IsVisibleChanged += BlockEditorRepositoryReloadFix_IsVisibleChanged;
    }

    private void BlockEditorRepositoryReloadFix_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true ||
            !_studioBlockViewerInitialized ||
            string.IsNullOrWhiteSpace(_repoRoot) ||
            _studioBlockMachineCombo is null)
        {
            return;
        }

        var catalogRepoIsCurrent = string.Equals(
            _studioBlockCatalogRepo,
            _repoRoot,
            StringComparison.OrdinalIgnoreCase);
        var selectorStillUsesBlockCatalog = ReferenceEquals(
            _studioBlockMachineCombo.ItemsSource,
            _studioBlockAssets);

        if (catalogRepoIsCurrent && selectorStillUsesBlockCatalog)
            return;

        // A Studio rescan can replace the shared selector ItemsSource with machine
        // templates even when the repository path did not change. Force the viewer
        // catalog back into the selector so the same repository works repeatedly
        // without requiring an application restart.
        RefreshStudioBlockCatalog(force: true);
    }
}

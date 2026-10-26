using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Makes the Phase-2 production-library entry deterministic. Phase 1 already keeps a direct
/// reference to the library summary TextBlock, so its logical parent is the exact library-card
/// StackPanel. No visual-tree discovery is required.
/// </summary>
public partial class MainWindow
{
    private bool _machineBuilderLibraryPhase2EntryFixInitialized;

    public void EnableForeverMachineBuilderLibraryPhase2EntryFix()
    {
        if (_machineBuilderLibraryPhase2EntryFixInitialized || _studioMachineBuilderWorkspace is null)
            return;
        _machineBuilderLibraryPhase2EntryFixInitialized = true;

        InstallMachineBuilderLibraryEntryDirect();
        _studioMachineBuilderWorkspace.Loaded += (_, _) => InstallMachineBuilderLibraryEntryDirect();
        _ = Dispatcher.BeginInvoke(
            InstallMachineBuilderLibraryEntryDirect,
            DispatcherPriority.ContextIdle);
    }

    private void InstallMachineBuilderLibraryEntryDirect()
    {
        var libraryPanel = _machineBuilderLibrarySummaryText?.Parent as StackPanel;
        if (libraryPanel is null)
            return;

        var description = libraryPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(text =>
                text.Text.Contains("Phase 1 establishes", StringComparison.OrdinalIgnoreCase) ||
                text.Text.Contains("Phase 2 fills", StringComparison.OrdinalIgnoreCase) ||
                text.Text.Contains("production building-block catalog", StringComparison.OrdinalIgnoreCase));
        if (description is not null)
        {
            description.Text =
                "Phase 2 ships the production building-block catalog. Search assets, typed machine functions, animation presets and reusable templates without leaving the Machine Builder.";
        }

        if (libraryPanel.Children.OfType<Button>()
            .Any(button => string.Equals(
                button.Tag?.ToString(),
                "machine_builder_library_open",
                StringComparison.Ordinal)))
            return;

        var button = CreateStudioChromeButton("OPEN PRODUCTION LIBRARY", double.NaN, true);
        button.Tag = "machine_builder_library_open";
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.Height = 38;
        button.Margin = new Thickness(0, 16, 0, 0);
        button.Click += (_, _) => ShowMachineBuilderLibrary();
        libraryPanel.Children.Add(button);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Makes the Phase-2 production-library entry resilient to WPF startup timing. The Machine Builder
/// workspace is constructed before Window.Show(), so visual-tree enumeration may not yet expose the
/// nested library StackPanel. Resolve it through the heading's logical parent and retry once the
/// workspace has loaded.
/// </summary>
public partial class MainWindow
{
    private bool _machineBuilderLibraryPhase2EntryFixInitialized;

    public void EnableForeverMachineBuilderLibraryPhase2EntryFix()
    {
        if (_machineBuilderLibraryPhase2EntryFixInitialized || _studioMachineBuilderWorkspace is null)
            return;
        _machineBuilderLibraryPhase2EntryFixInitialized = true;

        InstallMachineBuilderLibraryEntryResilient();
        _studioMachineBuilderWorkspace.Loaded += (_, _) => InstallMachineBuilderLibraryEntryResilient();
        _ = Dispatcher.BeginInvoke(
            InstallMachineBuilderLibraryEntryResilient,
            DispatcherPriority.ContextIdle);
    }

    private void InstallMachineBuilderLibraryEntryResilient()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;

        var heading = FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace)
            .FirstOrDefault(text => string.Equals(
                text.Text,
                "LIBRARY CONTRACT",
                StringComparison.Ordinal));

        var libraryPanel = heading?.Parent as StackPanel;
        libraryPanel ??= FindStudioVisualChildren<StackPanel>(_studioMachineBuilderWorkspace)
            .FirstOrDefault(panel => panel.Children.OfType<TextBlock>()
                .Any(text => string.Equals(
                    text.Text,
                    "LIBRARY CONTRACT",
                    StringComparison.Ordinal)));

        if (libraryPanel is null)
            return;

        var description = libraryPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(text =>
                text.Text.Contains("Phase 1 establishes", StringComparison.OrdinalIgnoreCase) ||
                text.Text.Contains("Phase 2 fills", StringComparison.OrdinalIgnoreCase));
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
        button.Margin = new Thickness(0, 16, 0, 0);
        button.Click += (_, _) => ShowMachineBuilderLibrary();
        libraryPanel.Children.Add(button);
    }
}

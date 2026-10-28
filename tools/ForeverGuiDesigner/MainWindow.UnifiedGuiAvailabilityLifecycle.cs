using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _unifiedGuiAvailabilityLifecycleInitialized;

    /// <summary>
    /// Keeps the shared-editor launch state synchronized with Machine Builder transitions that do
    /// not raise the GUI-tab click event (notably Functions -> GUI via the workflow continue button).
    /// </summary>
    public void EnableForeverUnifiedGuiAvailabilityLifecycle()
    {
        if (_unifiedGuiAvailabilityLifecycleInitialized)
            return;
        _unifiedGuiAvailabilityLifecycleInitialized = true;

        void ScheduleRefresh() => _ = Dispatcher.BeginInvoke(
            new Action(RefreshUnifiedMachineGuiAvailability),
            DispatcherPriority.ContextIdle);

        if (_machineBuilderFunctionSurface is not null)
        {
            var continueButton = FindStudioVisualChildren<Button>(_machineBuilderFunctionSurface)
                .FirstOrDefault(button => string.Equals(
                    button.Tag?.ToString(),
                    "machine_builder_phase5_continue",
                    StringComparison.Ordinal));
            if (continueButton is not null)
                continueButton.Click += (_, _) => ScheduleRefresh();
        }

        if (_machineBuilderPhase5ModeHost is not null)
            _machineBuilderPhase5ModeHost.IsVisibleChanged += (_, _) => ScheduleRefresh();

        if (_machineBuilderSaveButton is not null)
            _machineBuilderSaveButton.Click += (_, _) => ScheduleRefresh();

        ScheduleRefresh();
    }
}

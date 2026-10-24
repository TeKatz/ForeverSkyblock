using System.Windows;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (await SelfUpdateService.TryRunApplyModeAsync(e.Args))
        {
            Shutdown();
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        Dispatcher.BeginInvoke(() =>
        {
            window.EnablePersonalWorkspaceQoL();
            window.EnableForeverSkyblockTools();
            window.EnableForeverPhase5Tools();
            window.EnableForeverPhase5StateNormalization();
            window.EnableForeverPhase5InteractionCorrections();
            window.EnableForeverPhase6Tools();
            window.EnableForeverPhase6StableAnimation();
            window.EnableForeverPhase7Tools();
            window.EnableForeverPhase8Tools();
            window.EnableForeverPhase8InteractionTools();
            window.EnableForeverPhase8PerformanceHotfix();
            window.EnableForeverPhase9Tools();
            window.EnablePhase9SimulatorSnapshotFix();
            window.EnableForeverVisualFidelityPatch();
            window.EnableMachinePresentationLifecycleFix();
            window.EnableWashingSourceRenderer();
            window.EnableWashingRenderPipelineFix();
            window.EnableExclusiveMachineRuntimeTools();
            window.EnableMachineRuntimeVisualPolish();
            window.EnableMachineRuntimePerformanceFix();
            window.EnableSelfUpdater();
            window.EnableCompactContextMenus();
        }, DispatcherPriority.ContextIdle);
    }
}

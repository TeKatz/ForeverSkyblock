using System.Windows;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Dispatcher.BeginInvoke(() =>
        {
            if (Current.MainWindow is MainWindow window)
            {
                window.EnableForeverSkyblockTools();
                window.EnableForeverPhase5Tools();
                window.EnableForeverPhase5StateNormalization();
                window.EnableForeverPhase5InteractionCorrections();
                // Phase 6 depends on the project and state controls above so it can
                // restyle every command ComboBox and add machine-specific scene controls.
                window.EnableForeverPhase6Tools();
            }
        }, DispatcherPriority.ContextIdle);
    }
}

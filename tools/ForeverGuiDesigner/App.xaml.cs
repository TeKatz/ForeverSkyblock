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
                window.EnableForeverSkyblockTools();
        }, DispatcherPriority.ContextIdle);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Installs Function-step toolbar commands only after the WPF surface is loaded. Several optional
/// Machine Builder extensions run before button content is materialized in the visual tree, so
/// startup-time text searches are not reliable for this toolbar.
/// </summary>
public partial class MainWindow
{
    private bool _machineBuilderFunctionToolbarRepairInitialized;

    public void EnableForeverMachineBuilderFunctionToolbarRepair()
    {
        if (_machineBuilderFunctionToolbarRepairInitialized || _machineBuilderFunctionSurface is null)
            return;
        _machineBuilderFunctionToolbarRepairInitialized = true;

        _machineBuilderFunctionSurface.Loaded += (_, _) => QueueFunctionToolbarRepair();
        Loaded += (_, _) => QueueFunctionToolbarRepair();

        if (IsLoaded || _machineBuilderFunctionSurface.IsLoaded)
            QueueFunctionToolbarRepair();
    }

    private void QueueFunctionToolbarRepair()
    {
        _ = Dispatcher.BeginInvoke(
            InstallFunctionToolbarCommands,
            DispatcherPriority.Loaded);
    }

    private void InstallFunctionToolbarCommands()
    {
        if (_machineBuilderFunctionSurface is null)
            return;

        var actions = FindStudioVisualChildren<StackPanel>(_machineBuilderFunctionSurface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                panel.Children.OfType<Button>().Any(button => FunctionToolbarButtonHasLabel(button, "AUTO LAYOUT")));
        if (actions is null)
            return;

        if (!actions.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "function_templates", StringComparison.Ordinal)))
        {
            var templates = CreateStudioChromeButton("TEMPLATES", 96, true);
            templates.Tag = "function_templates";
            templates.Height = 31;
            templates.Margin = new Thickness(0, 0, 7, 0);
            templates.ToolTip = "Open prebuilt editable machine logic templates";
            templates.Click += (_, _) => ShowFunctionTemplates();
            actions.Children.Insert(0, templates);
        }

        if (!actions.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "function_delete_selected", StringComparison.Ordinal)))
        {
            var delete = CreateStudioChromeButton("DELETE SELECTED", 126, false);
            delete.Tag = "function_delete_selected";
            delete.Height = 31;
            delete.Margin = new Thickness(7, 0, 0, 0);
            delete.Click += (_, _) => DeleteSelectedFunctionNodeFromGuiWorkflow();

            var cancel = actions.Children.OfType<Button>()
                .FirstOrDefault(button => FunctionToolbarButtonHasLabel(button, "CANCEL LINK"));
            var index = cancel is null ? actions.Children.Count : actions.Children.IndexOf(cancel);
            actions.Children.Insert(Math.Max(0, index), delete);
        }
    }

    private static bool FunctionToolbarButtonHasLabel(Button button, string label)
    {
        if (button.Content is Border { Child: TextBlock text })
            return text.Text.Equals(label, StringComparison.Ordinal);
        if (button.Content is TextBlock directText)
            return directText.Text.Equals(label, StringComparison.Ordinal);
        if (button.Content is string directString)
            return directString.Equals(label, StringComparison.Ordinal);
        return FindStudioVisualChildren<TextBlock>(button)
            .Any(candidate => candidate.Text.Equals(label, StringComparison.Ordinal));
    }
}

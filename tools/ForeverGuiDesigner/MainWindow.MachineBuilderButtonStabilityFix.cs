using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Stabilizes the Machine Builder action buttons that use auto/stretch layouts. These controls keep
/// the Studio chrome colors but opt out of the small lift animation and native WPF button chrome so
/// their geometry cannot change while hovering, pressing or focusing them.
/// </summary>
public partial class MainWindow
{
    private const string MachineBuilderStableButtonMarker = "ForeverStudio.MachineBuilder.StableActionButton";

    private bool _machineBuilderButtonStabilityFixInitialized;

    public void EnableForeverMachineBuilderButtonStabilityFix()
    {
        if (_machineBuilderButtonStabilityFixInitialized || _studioMachineBuilderWorkspace is null)
            return;
        _machineBuilderButtonStabilityFixInitialized = true;

        _studioMachineBuilderWorkspace.Loaded += (_, _) => ScheduleMachineBuilderButtonStabilityPass();
        _studioMachineBuilderWorkspace.IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                ScheduleMachineBuilderButtonStabilityPass();
        };

        HookMachineBuilderButtonStabilityRefresh(_machineBuilderVisualSurface);
        HookMachineBuilderButtonStabilityRefresh(_machineBuilderFunctionSurface);
        HookMachineBuilderButtonStabilityRefresh(_machineBuilderPhase5Surface);

        if (_machineBuilderPhase5GuiTab is not null)
            _machineBuilderPhase5GuiTab.Click += (_, _) => ScheduleMachineBuilderButtonStabilityPass();
        if (_machineBuilderPhase5AnimationTab is not null)
            _machineBuilderPhase5AnimationTab.Click += (_, _) => ScheduleMachineBuilderButtonStabilityPass();
        if (_machineBuilderGuiStep is not null)
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ScheduleMachineBuilderButtonStabilityPass();
        if (_machineBuilderAnimationStep is not null)
            _machineBuilderAnimationStep.MouseLeftButtonUp += (_, _) => ScheduleMachineBuilderButtonStabilityPass();

        ApplyMachineBuilderButtonStabilityPass();
        ScheduleMachineBuilderButtonStabilityPass();
    }

    private void HookMachineBuilderButtonStabilityRefresh(FrameworkElement? surface)
    {
        if (surface is null)
            return;

        surface.IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                ScheduleMachineBuilderButtonStabilityPass();
        };
    }

    private void ScheduleMachineBuilderButtonStabilityPass()
        => _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(ApplyMachineBuilderButtonStabilityPass));

    private void ApplyMachineBuilderButtonStabilityPass()
    {
        StabilizeMachineBuilderButton(_machineBuilderSaveButton);

        StabilizeMachineBuilderButtonsByText(
            _machineBuilderProjectsList?.Parent as DependencyObject,
            "OPEN SELECTED");

        StabilizeMachineBuilderButtonsByText(
            _machineBuilderLibrarySummaryText?.Parent as DependencyObject,
            "OPEN PRODUCTION LIBRARY");

        if (_studioMachineBuilderWorkspace is not null)
        {
            var continueButton = FindStudioVisualChildren<Button>(_studioMachineBuilderWorkspace)
                .FirstOrDefault(button => string.Equals(
                    button.Tag?.ToString(),
                    "machine_builder_visual_continue",
                    StringComparison.Ordinal));
            if (continueButton is not null)
            {
                continueButton.Width = double.NaN;
                continueButton.HorizontalAlignment = HorizontalAlignment.Center;
                StabilizeMachineBuilderButton(continueButton);
            }
        }

        StabilizeMachineBuilderButtonsByText(
            _machineBuilderVisualSurface,
            "RESET ASSET DEFAULTS");
        StabilizeMachineBuilderAssetFilterButtons();

        StabilizeMachineBuilderButtonsByText(
            _machineBuilderFunctionInspectorBody,
            "APPLY POSITION",
            "DUPLICATE",
            "DELETE");

        StabilizeMachineBuilderButtonsByText(
            _guiDesignerRecipeList?.Parent as DependencyObject,
            "ADD / APPLY",
            "REMOVE");

        StabilizeMachineBuilderButtonsByText(
            _machineBuilderPhase5ModeHost,
            "OPEN SHARED GUI EDITOR",
            "ADD ANIMATION BINDING",
            "REMOVE SELECTED");
    }

    private void StabilizeMachineBuilderAssetFilterButtons()
    {
        foreach (var button in _machineBuilderAssetBrowserFilterButtons.Values)
        {
            // Let the WrapPanel measure the actual label instead of clipping longer category names
            // into legacy fixed widths. Compact padding preserves the dense two/three-row browser.
            button.Width = double.NaN;
            if (button.Content is Border surface)
                surface.Padding = new Thickness(9, 5, 9, 5);
            StabilizeMachineBuilderButton(button);
        }
    }

    private static void StabilizeMachineBuilderButtonsByText(
        DependencyObject? root,
        params string[] labels)
    {
        if (root is null || labels.Length == 0)
            return;

        var wanted = labels.ToHashSet(StringComparer.Ordinal);
        foreach (var button in FindStudioVisualChildren<Button>(root))
        {
            var text = ReadMachineBuilderStableButtonText(button);
            if (text is not null && wanted.Contains(text))
                StabilizeMachineBuilderButton(button);
        }
    }

    private static string? ReadMachineBuilderStableButtonText(Button button)
        => button.Content is Border { Child: TextBlock label }
            ? label.Text
            : button.Content?.ToString();

    private static void StabilizeMachineBuilderButton(Button? button)
    {
        if (button is null)
            return;

        // Reapply deliberately on every scoped pass. Some Machine Builder surfaces are composed lazily,
        // and this guarantees that later layout/state refreshes cannot restore the native Button chrome.
        ApplyStudioContentOnlyButtonTemplate(button);
        button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        button.VerticalContentAlignment = VerticalAlignment.Stretch;
        button.FocusVisualStyle = null;
        button.SnapsToDevicePixels = true;
        button.UseLayoutRounding = true;

        if (button.Content is Border surface)
        {
            surface.SnapsToDevicePixels = true;
            surface.UseLayoutRounding = true;
        }

        if (button.Resources.Contains(MachineBuilderStableButtonMarker))
        {
            ResetMachineBuilderStableButtonTranslation(button);
            return;
        }

        button.Resources[MachineBuilderStableButtonMarker] = true;

        // CreateStudioChromeButton owns the color transition and a small TranslateTransform lift.
        // The marked auto/stretch actions must not move: cancelling the translation after the factory
        // handlers preserves the color feedback while making hover/press/focus layout-stable.
        button.MouseEnter += (_, _) => ResetMachineBuilderStableButtonTranslation(button);
        button.MouseLeave += (_, _) => ResetMachineBuilderStableButtonTranslation(button);
        button.GotKeyboardFocus += (_, _) => ResetMachineBuilderStableButtonTranslation(button);
        button.LostKeyboardFocus += (_, _) => ResetMachineBuilderStableButtonTranslation(button);
        button.PreviewMouseLeftButtonDown += (_, _) => ResetMachineBuilderStableButtonTranslation(button);
        button.PreviewMouseLeftButtonUp += (_, _) => ResetMachineBuilderStableButtonTranslation(button);
        ResetMachineBuilderStableButtonTranslation(button);
    }

    private static void ResetMachineBuilderStableButtonTranslation(Button button)
    {
        if (button.RenderTransform is not TranslateTransform translate)
            return;

        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.Y = 0;
    }
}

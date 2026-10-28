using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Transitional UI bridge that lets Machine Builder use the established full GUI editor instead of
/// growing a second canvas. The existing editor remains untouched as the interaction engine; this
/// class only swaps its document through a context provider and restores the previous standalone
/// editor session when the user returns to Machine Builder.
/// </summary>
public partial class MainWindow
{
    private bool _unifiedGuiEditorBridgeInitialized;
    private ForeverGuiEditorSession? _unifiedMachineGuiSession;
    private ForeverStudioMachineDocument? _unifiedMachineGuiOwner;
    private ForeverMachineGuiEditorAccess.Identity? _unifiedMachineGuiIdentity;
    private Border? _unifiedGuiContextBar;
    private TextBlock? _unifiedGuiContextText;
    private string? _unifiedPreviousLayoutJson;
    private string? _unifiedPreviousPath;
    private bool _unifiedPreviousDirty;
    private int _unifiedPreviousSnapIndex;
    private ForeverMachineTemplate? _unifiedPreviousTemplate;
    private StudioWorkspace _unifiedPreviousStudioWorkspace = StudioWorkspace.ProjectHome;
    private readonly List<Button> _unifiedDisabledNavigationButtons = [];

    public void EnableForeverUnifiedGuiEditorBridge()
    {
        if (_unifiedGuiEditorBridgeInitialized)
            return;
        _unifiedGuiEditorBridgeInitialized = true;

        EnsureUnifiedGuiContextBar();

        void ScheduleInstall() => _ = Dispatcher.BeginInvoke(
            new Action(() =>
            {
                InstallUnifiedMachineGuiLaunchCard();
                RefreshUnifiedMachineGuiAvailability();
            }),
            DispatcherPriority.ContextIdle);

        if (_machineBuilderGuiStep is not null)
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ScheduleInstall();
        if (_machineBuilderPhase5GuiTab is not null)
            _machineBuilderPhase5GuiTab.Click += (_, _) => ScheduleInstall();
        if (_machineBuilderPhase5ScreenIdBox is not null)
            _machineBuilderPhase5ScreenIdBox.LostKeyboardFocus += (_, _) => ScheduleInstall();
        ScheduleInstall();
    }

    private void InstallUnifiedMachineGuiLaunchCard()
    {
        if (_guiDesignerBody is not Grid body)
            return;
        var leftScroll = body.Children.OfType<ScrollViewer>()
            .FirstOrDefault(child => Grid.GetColumn(child) == 0);
        if (leftScroll?.Content is not StackPanel stack)
            return;
        if (stack.Children.OfType<FrameworkElement>().Any(child =>
                string.Equals(child.Tag?.ToString(), "unified_gui_editor_launch", StringComparison.Ordinal)))
        {
            RefreshUnifiedMachineGuiAvailability();
            return;
        }

        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        card.Tag = "unified_gui_editor_launch";
        card.Margin = new Thickness(0, 12, 0, 0);
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock
        {
            Text = "FULL GUI EDITOR",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Edit this Machine GUI in the established WYSIWYG canvas with its existing selection, alignment, hierarchy and resize tooling. Machine bindings stay attached through the shared GUI contract.",
            Foreground = Brush("#82969E"),
            FontSize = 9.4,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 14,
            Margin = new Thickness(0, 7, 0, 10)
        });
        var open = CreateStudioChromeButton("OPEN FULL GUI EDITOR", double.NaN, true);
        open.Height = 36;
        open.HorizontalAlignment = HorizontalAlignment.Stretch;
        open.Click += (_, _) => OpenMachineGuiInUnifiedEditor();
        panel.Children.Add(open);
        card.Child = panel;

        // Compatibility fallback for older layouts that do not yet contain the native launch card.
        stack.Children.Insert(Math.Min(1, stack.Children.Count), card);
        RefreshUnifiedMachineGuiAvailability();
    }

    private void RefreshUnifiedMachineGuiAvailability()
    {
        var identity = ForeverMachineGuiEditorAccess.GetCreatedGui(_machineBuilderDocument);
        var canOpen = identity is not null && _unifiedMachineGuiSession is null;

        if (_guiDesignerBody is not null)
        {
            foreach (var button in FindStudioVisualChildren<Button>(_guiDesignerBody))
            {
                var text = ReadUnifiedGuiButtonText(button);
                if (!text.Equals("OPEN SHARED GUI EDITOR", StringComparison.OrdinalIgnoreCase) &&
                    !text.Equals("OPEN FULL GUI EDITOR", StringComparison.OrdinalIgnoreCase))
                    continue;

                button.IsEnabled = canOpen;
                button.ToolTip = identity is null
                    ? "Create/assign this machine's GUI in the preceding Machine Builder flow before opening the advanced editor."
                    : $"Open only this machine GUI: {identity.ScreenId}";
            }
        }

        if (_guiDesignerStatus is not null)
        {
            _guiDesignerStatus.Text = identity is null
                ? "No authored GUI is attached to this Machine Project yet. Complete the preceding GUI creation/binding flow first."
                : $"Ready to edit this machine's GUI only: {identity.ScreenId}.";
            _guiDesignerStatus.Foreground = Brush(identity is null ? "#D8B26A" : "#74C99A");
        }
    }

    private static string ReadUnifiedGuiButtonText(Button button)
        => button.Content as string ??
           FindStudioVisualChildren<TextBlock>(button).FirstOrDefault()?.Text ?? string.Empty;

    private void EnsureUnifiedGuiContextBar()
    {
        if (_unifiedGuiContextBar is not null || _studioGuiWorkspace is not DockPanel dock)
            return;

        var bar = new Border
        {
            Height = 48,
            Background = Brush("#132529"),
            BorderBrush = Brush("#3B666B"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Visibility = Visibility.Collapsed
        };
        DockPanel.SetDock(bar, Dock.Top);
        var grid = new Grid { Margin = new Thickness(16, 0, 16, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _unifiedGuiContextText = new TextBlock
        {
            Text = "Machine GUI",
            Foreground = Brush("#DCEBED"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(_unifiedGuiContextText);

        var cancel = CreateStudioChromeButton("CANCEL / RETURN", 132, false);
        cancel.Height = 32;
        cancel.VerticalAlignment = VerticalAlignment.Center;
        cancel.Click += (_, _) => CancelUnifiedMachineGuiEditor();
        Grid.SetColumn(cancel, 1);
        grid.Children.Add(cancel);

        var apply = CreateStudioChromeButton("APPLY TO MACHINE", 154, true);
        apply.Height = 32;
        apply.VerticalAlignment = VerticalAlignment.Center;
        apply.Click += (_, _) => ApplyUnifiedMachineGuiEditor();
        Grid.SetColumn(apply, 3);
        grid.Children.Add(apply);

        bar.Child = grid;
        _unifiedGuiContextBar = bar;

        // Existing structure is [workspace navigation, editor]. Insert the machine-context bar
        // between them so the established editor remains the DockPanel's fill child.
        dock.Children.Insert(Math.Min(1, dock.Children.Count), bar);
    }

    private void OpenMachineGuiInUnifiedEditor()
    {
        if (_unifiedMachineGuiSession is not null)
            return;

        var owner = _machineBuilderDocument;
        var identity = ForeverMachineGuiEditorAccess.GetCreatedGui(owner);
        if (owner is null || identity is null)
        {
            RefreshUnifiedMachineGuiAvailability();
            MessageBox.Show(
                this,
                "This Machine Project does not have an authored GUI yet. Complete the preceding GUI creation/binding flow before opening the shared editor.",
                "Machine GUI unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            var session = new ForeverGuiEditorSession(new ForeverMachineGuiContextProvider(owner));
            var legacy = ForeverGuiLegacyEditorProjection.ToLegacyLayout(session.Document);

            _unifiedPreviousLayoutJson = SerializeLayout();
            _unifiedPreviousPath = _currentPath;
            _unifiedPreviousDirty = _dirty;
            _unifiedPreviousSnapIndex = SnapCombo.SelectedIndex;
            _unifiedPreviousTemplate = _activeForeverTemplate;
            _unifiedPreviousStudioWorkspace = _studioWorkspace;
            _unifiedMachineGuiSession = session;
            _unifiedMachineGuiOwner = owner;
            _unifiedMachineGuiIdentity = identity;

            _activeForeverTemplate = null;
            LoadLayoutIntoEditor(legacy, null, clearHistory: true);
            SetUnifiedSnap(session.Document.Canvas.Snap);
            _dirty = false;

            if (_unifiedGuiContextText is not null)
                _unifiedGuiContextText.Text = $"MACHINE GUI · {session.Context.DisplayName} · {identity.ScreenId}";
            if (_unifiedGuiContextBar is not null)
                _unifiedGuiContextBar.Visibility = Visibility.Visible;
            DisableUnifiedGuiContextSwitching();

            if (_studioMachineBuilderWorkspace is not null)
                _studioMachineBuilderWorkspace.Visibility = Visibility.Collapsed;
            ShowStudioWorkspace(StudioWorkspace.GuiEditor);
            StatusText.Text = $"Editing Machine GUI '{identity.ScreenId}' for '{session.Context.DisplayName}' in the shared GUI editor.";
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or JsonException)
        {
            _unifiedMachineGuiSession = null;
            _unifiedMachineGuiOwner = null;
            _unifiedMachineGuiIdentity = null;
            RefreshUnifiedMachineGuiAvailability();
            MessageBox.Show(this, ex.Message, "Machine GUI cannot be opened safely", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyUnifiedMachineGuiEditor()
    {
        if (_unifiedMachineGuiSession is null ||
            _unifiedMachineGuiOwner is null ||
            _unifiedMachineGuiIdentity is null)
            return;

        if (!ReferenceEquals(_machineBuilderDocument, _unifiedMachineGuiOwner) ||
            !ForeverMachineGuiEditorAccess.Matches(_unifiedMachineGuiIdentity, _machineBuilderDocument))
        {
            MessageBox.Show(
                this,
                "The Machine Project or GUI screen changed after this editor session was opened. The changes were not applied. Return to Machine Builder and reopen the intended GUI.",
                "Machine GUI context changed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var edited = ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);
            _unifiedMachineGuiSession.ReplaceDocument(edited);
            var errors = _unifiedMachineGuiSession.Validate()
                .Where(issue => issue.Severity == ForeverGuiContractSeverity.Error)
                .ToList();
            if (errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    string.Join("\n", errors.Take(10).Select(issue => issue.Message)),
                    "Machine GUI validation failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var displayName = _unifiedMachineGuiSession.Context.DisplayName;
            var screenId = _unifiedMachineGuiIdentity.ScreenId;
            _unifiedMachineGuiSession.Commit();
            MarkMachineBuilderPhase5Dirty();
            ReturnFromUnifiedMachineGuiEditor();
            RefreshMachineBuilderGuiDesigner();
            StatusText.Text = $"Applied shared GUI editor changes to '{screenId}' on Machine Project '{displayName}'. Save the Machine Project to persist them on disk.";
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or JsonException)
        {
            MessageBox.Show(this, ex.Message, "Machine GUI apply failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelUnifiedMachineGuiEditor()
    {
        if (_unifiedMachineGuiSession is null)
            return;
        if (_dirty && MessageBox.Show(
                this,
                "Discard the GUI changes made in the shared editor and return to Machine Builder?",
                "Discard Machine GUI changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        ReturnFromUnifiedMachineGuiEditor();
        StatusText.Text = "Returned to Machine Builder without applying shared GUI editor changes.";
    }

    private void ReturnFromUnifiedMachineGuiEditor()
    {
        var previousJson = _unifiedPreviousLayoutJson;
        var previousPath = _unifiedPreviousPath;
        var previousDirty = _unifiedPreviousDirty;
        var previousSnapIndex = _unifiedPreviousSnapIndex;
        var previousTemplate = _unifiedPreviousTemplate;
        var previousWorkspace = _unifiedPreviousStudioWorkspace;

        _unifiedMachineGuiSession = null;
        _unifiedMachineGuiOwner = null;
        _unifiedMachineGuiIdentity = null;
        _unifiedPreviousLayoutJson = null;
        _unifiedPreviousPath = null;
        _unifiedPreviousTemplate = null;

        if (!string.IsNullOrWhiteSpace(previousJson))
        {
            var restored = JsonSerializer.Deserialize<GuiLayoutModel>(previousJson, JsonOptions)
                           ?? new GuiLayoutModel();
            _activeForeverTemplate = previousTemplate;
            LoadLayoutIntoEditor(restored, previousPath, clearHistory: true);
            _dirty = previousDirty;
            if (previousSnapIndex >= 0 && previousSnapIndex < SnapCombo.Items.Count)
                SnapCombo.SelectedIndex = previousSnapIndex;
            UpdateStatus();
        }

        if (_unifiedGuiContextBar is not null)
            _unifiedGuiContextBar.Visibility = Visibility.Collapsed;
        RestoreUnifiedGuiContextSwitching();
        if (_studioGuiWorkspace is not null)
            _studioGuiWorkspace.Visibility = Visibility.Collapsed;
        _studioWorkspace = previousWorkspace;
        if (_studioMachineBuilderWorkspace is not null)
            _studioMachineBuilderWorkspace.Visibility = Visibility.Visible;
        Title = "Forever Studio — Machine Builder";
        RefreshUnifiedMachineGuiAvailability();
    }

    private void DisableUnifiedGuiContextSwitching()
    {
        _unifiedDisabledNavigationButtons.Clear();
        if (_studioGuiWorkspace is null)
            return;

        foreach (var button in FindStudioVisualChildren<Button>(_studioGuiWorkspace))
        {
            if (!button.IsEnabled || !IsUnsafeUnifiedGuiContextButton(button))
                continue;
            button.IsEnabled = false;
            _unifiedDisabledNavigationButtons.Add(button);
        }
    }

    private static bool IsUnsafeUnifiedGuiContextButton(Button button)
    {
        var text = button.Content as string ??
                   FindStudioVisualChildren<TextBlock>(button).FirstOrDefault()?.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return text.Equals("New", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("Save", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("Save As", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Repo", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Project Home", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Load machine", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Scan project", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Reload source", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Review Java", StringComparison.OrdinalIgnoreCase);
    }

    private void RestoreUnifiedGuiContextSwitching()
    {
        foreach (var button in _unifiedDisabledNavigationButtons)
            button.IsEnabled = true;
        _unifiedDisabledNavigationButtons.Clear();
    }

    private void SetUnifiedSnap(double requested)
    {
        var supported = new[] { 1, 2, 4, 8 };
        var nearest = supported.OrderBy(value => Math.Abs(value - requested)).First();
        for (var index = 0; index < SnapCombo.Items.Count; index++)
        {
            if (SnapCombo.Items[index] is ComboBoxItem { Tag: string tag } &&
                int.TryParse(tag, out var value) && value == nearest)
            {
                SnapCombo.SelectedIndex = index;
                return;
            }
        }
        _snap = nearest;
    }
}

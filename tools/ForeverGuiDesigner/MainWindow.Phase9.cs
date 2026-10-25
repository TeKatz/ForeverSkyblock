using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _phase9Initialized;
    private ComboBox? _phase9GuiScaleCombo;
    private ComboBox? _phase9ResolutionCombo;
    private CheckBox? _phase9LiveReloadToggle;
    private Button? _phase9ReviewButton;
    private TextBlock? _phase9ValidationSummary;
    private ListBox? _phase9ValidationList;
    private DispatcherTimer? _phase9MaintenanceTimer;
    private SourceLiveReloadService? _phase9LiveReload;
    private string? _phase9WatchedRepo;
    private string? _phase9SourceBaseline;
    private string? _phase9LastValidationSnapshot;
    private DateTime _phase9IgnoreSourceEventsUntil;
    private Window? _phase9SimulatorWindow;
    private Canvas? _phase9SimulatorScreen;
    private TextBlock? _phase9SimulatorInfo;
    private IReadOnlyList<GuiValidationIssue> _phase9ValidationIssues = [];

    public void EnableForeverPhase9Tools()
    {
        if (_phase9Initialized)
            return;
        _phase9Initialized = true;

        BuildPhase9Toolbar();
        BuildPhase9ValidationPanel();

        var loadButton = SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Load machine", StringComparison.Ordinal));
        if (loadButton is not null)
        {
            loadButton.Click += (_, _) => Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    CapturePhase9SourceBaseline();
                    RunPhase9Validation(force: true);
                    RefreshPhase9Simulator();
                }));
        }

        var scanButton = SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Scan project", StringComparison.Ordinal));
        if (scanButton is not null)
        {
            scanButton.Click += (_, _) => Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    ConfigurePhase9LiveReload();
                    RunPhase9Validation(force: true);
                }));
        }

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_phase9SimulatorWindow?.IsVisible == true)
                RefreshPhase9Simulator();
        };

        _phase9MaintenanceTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _phase9MaintenanceTimer.Tick += (_, _) =>
        {
            if (!string.Equals(_phase9WatchedRepo, _repoRoot, StringComparison.OrdinalIgnoreCase))
                ConfigurePhase9LiveReload();
            RunPhase9Validation(force: false);
        };
        _phase9MaintenanceTimer.Start();

        Closed += (_, _) =>
        {
            _phase9MaintenanceTimer?.Stop();
            _phase9LiveReload?.Dispose();
            _phase9LiveReload = null;
            _phase9SimulatorWindow?.Close();
        };

        ConfigurePhase9LiveReload();
        RunPhase9Validation(force: true);
    }

    private void BuildPhase9Toolbar()
    {
        SkyblockBarHost.Children.Add(new Border
        {
            Width = 1,
            Height = 24,
            Background = Brush("#3B454E"),
            Margin = new Thickness(7, 0, 7, 0)
        });
        SkyblockBarHost.Children.Add(new TextBlock
        {
            Text = "PHASE 9",
            Foreground = Brush("#80C7D6"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        SkyblockBarHost.Children.Add(new TextBlock
        {
            Text = "GUI scale",
            Foreground = Brush("#AEBBC4"),
            Margin = new Thickness(2, 0, 2, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        _phase9GuiScaleCombo = new ComboBox { Width = 68 };
        _phase9GuiScaleCombo.Items.Add(new ComboBoxItem { Content = "Auto", Tag = "auto" });
        for (var scale = 1; scale <= 4; scale++)
            _phase9GuiScaleCombo.Items.Add(new ComboBoxItem { Content = $"{scale}x", Tag = scale.ToString() });
        _phase9GuiScaleCombo.SelectedIndex = 2;
        _phase9GuiScaleCombo.SelectionChanged += (_, _) => RefreshPhase9Simulator();
        SkyblockBarHost.Children.Add(_phase9GuiScaleCombo);

        _phase9ResolutionCombo = new ComboBox { Width = 112 };
        foreach (var resolution in new[] { "854x480", "1280x720", "1920x1080", "2560x1440", "3840x2160" })
            _phase9ResolutionCombo.Items.Add(new ComboBoxItem { Content = resolution.Replace('x', '×'), Tag = resolution });
        _phase9ResolutionCombo.SelectedIndex = 2;
        _phase9ResolutionCombo.SelectionChanged += (_, _) => RefreshPhase9Simulator();
        SkyblockBarHost.Children.Add(_phase9ResolutionCombo);

        var simulatorButton = new Button
        {
            Content = "Simulator",
            ToolTip = "Preview this GUI centered inside a Minecraft-style logical viewport at the selected framebuffer resolution and GUI scale."
        };
        simulatorButton.Click += (_, _) => ShowPhase9Simulator();
        SkyblockBarHost.Children.Add(simulatorButton);

        _phase9LiveReloadToggle = new CheckBox
        {
            Content = "Live source",
            IsChecked = true,
            ToolTip = "Watch Java screens/menus and Forever assets. Clean imported layouts reload automatically; edited layouts are preserved."
        };
        _phase9LiveReloadToggle.Checked += (_, _) => ConfigurePhase9LiveReload();
        _phase9LiveReloadToggle.Unchecked += (_, _) => ConfigurePhase9LiveReload();
        SkyblockBarHost.Children.Add(_phase9LiveReloadToggle);

        var reloadButton = new Button
        {
            Content = "Reload source",
            ToolTip = "Rescan the repository and reload the active machine from Java."
        };
        reloadButton.Click += (_, _) => ManualPhase9SourceReload();
        SkyblockBarHost.Children.Add(reloadButton);

        var validateButton = new Button
        {
            Content = "Validate",
            ToolTip = "Check bounds, ids, slot geometry, texture references and interactive overlaps."
        };
        validateButton.Click += (_, _) => RunPhase9Validation(force: true);
        SkyblockBarHost.Children.Add(validateButton);

        _phase9ReviewButton = new Button
        {
            Content = "Review Java…",
            ToolTip = "Build a safe Java diff for source-backed machine slots and MachineButton geometry before applying it."
        };
        _phase9ReviewButton.Click += ReviewPhase9Java_Click;
        SkyblockBarHost.Children.Add(_phase9ReviewButton);

        _phase9ValidationSummary = new TextBlock
        {
            Text = "validation: —",
            Foreground = Brush("#8F9DA7"),
            Margin = new Thickness(6, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        SkyblockBarHost.Children.Add(_phase9ValidationSummary);
    }

    private void BuildPhase9ValidationPanel()
    {
        if (SelectionText.Parent is not StackPanel host)
            return;

        var expander = new Expander
        {
            Header = "Phase 9 · Validation",
            IsExpanded = false,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var panel = new StackPanel { Margin = new Thickness(0, 7, 0, 0) };
        panel.Children.Add(new TextBlock
        {
            Text = "Errors block Java apply-back. Warnings remain reviewable.",
            Foreground = Brush("#8F9DA7"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 5)
        });
        _phase9ValidationList = new ListBox
        {
            Height = 180,
            Background = Brush("#17191C"),
            BorderBrush = Brush("#40464D"),
            Foreground = Brush("#E8E8E8")
        };
        _phase9ValidationList.SelectionChanged += (_, _) =>
        {
            if (_phase9ValidationList.SelectedItem is not GuiValidationIssue { ElementId: { Length: > 0 } id })
                return;
            var element = _layout.Elements.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
            if (element is not null)
                SetSelection([element]);
        };
        panel.Children.Add(_phase9ValidationList);
        expander.Content = panel;
        host.Children.Add(expander);
    }

    private void RunPhase9Validation(bool force)
    {
        if (_phase9ValidationList is null || _phase9ValidationSummary is null)
            return;

        var snapshot = SerializeLayout() + "\n@repo=" + (_repoRoot ?? string.Empty);
        if (!force && string.Equals(snapshot, _phase9LastValidationSnapshot, StringComparison.Ordinal))
            return;
        _phase9LastValidationSnapshot = snapshot;

        _phase9ValidationIssues = GuiValidationService.Validate(_layout, _repoRoot);
        _phase9ValidationList.ItemsSource = _phase9ValidationIssues;
        var errors = _phase9ValidationIssues.Count(issue => issue.Severity == GuiValidationSeverity.Error);
        var warnings = _phase9ValidationIssues.Count(issue => issue.Severity == GuiValidationSeverity.Warning);
        _phase9ValidationSummary.Text = errors == 0 && warnings == 0
            ? "validation: clean"
            : $"validation: {errors}E / {warnings}W";
        _phase9ValidationSummary.Foreground = errors > 0
            ? Brush("#F08B86")
            : warnings > 0 ? Brush("#E4B47C") : Brush("#9BD4A6");

        if (_phase9ReviewButton is not null)
            _phase9ReviewButton.IsEnabled = _activeForeverTemplate is not null && !string.IsNullOrWhiteSpace(_repoRoot);
    }

    private void ShowPhase9Simulator()
    {
        if (_phase9SimulatorWindow is null)
        {
            _phase9SimulatorInfo = new TextBlock
            {
                Foreground = Brush("#DCE7EC"),
                Margin = new Thickness(12, 9, 12, 9),
                TextWrapping = TextWrapping.Wrap
            };
            _phase9SimulatorScreen = new Canvas
            {
                Background = Brush("#0B0E11"),
                ClipToBounds = true
            };
            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                Margin = new Thickness(14),
                Child = _phase9SimulatorScreen
            };
            var root = new DockPanel { Background = Brush("#15191D") };
            DockPanel.SetDock(_phase9SimulatorInfo, Dock.Top);
            root.Children.Add(_phase9SimulatorInfo);
            root.Children.Add(viewbox);

            _phase9SimulatorWindow = new Window
            {
                Owner = this,
                Title = "Forever GUI Scale Simulator",
                Width = 980,
                Height = 680,
                MinWidth = 620,
                MinHeight = 420,
                Background = Brush("#15191D"),
                Content = root,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            _phase9SimulatorWindow.Closed += (_, _) =>
            {
                _phase9SimulatorWindow = null;
                _phase9SimulatorScreen = null;
                _phase9SimulatorInfo = null;
            };
        }

        RefreshPhase9Simulator();
        _phase9SimulatorWindow.Show();
        _phase9SimulatorWindow.Activate();
    }

    private void RefreshPhase9Simulator()
    {
        if (_phase9SimulatorScreen is null || _phase9SimulatorInfo is null)
            return;

        var (framebufferWidth, framebufferHeight) = GetPhase9Resolution();
        var guiScale = GetPhase9GuiScale(framebufferWidth, framebufferHeight);
        var logicalWidth = (int)Math.Ceiling(framebufferWidth / (double)guiScale);
        var logicalHeight = (int)Math.Ceiling(framebufferHeight / (double)guiScale);

        _phase9SimulatorScreen.Width = logicalWidth;
        _phase9SimulatorScreen.Height = logicalHeight;
        _phase9SimulatorScreen.Children.Clear();

        var gui = new Border
        {
            Width = Math.Max(1, _layout.Width),
            Height = Math.Max(1, _layout.Height),
            Background = new VisualBrush(PreviewCanvas)
            {
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            },
            BorderBrush = Brush("#6D8192"),
            BorderThickness = new Thickness(0.5),
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(gui, Math.Floor((logicalWidth - _layout.Width) / 2.0));
        Canvas.SetTop(gui, Math.Floor((logicalHeight - _layout.Height) / 2.0));
        _phase9SimulatorScreen.Children.Add(gui);

        _phase9SimulatorInfo.Text =
            $"Framebuffer {framebufferWidth}×{framebufferHeight} · GUI scale {guiScale}x · logical viewport {logicalWidth}×{logicalHeight} · " +
            $"GUI {_layout.Width}×{_layout.Height} logical px / {_layout.Width * guiScale}×{_layout.Height * guiScale} framebuffer px";
    }

    private (int Width, int Height) GetPhase9Resolution()
    {
        var value = (_phase9ResolutionCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "1920x1080";
        var parts = value.Split('x');
        return parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height)
            ? (Math.Max(320, width), Math.Max(240, height))
            : (1920, 1080);
    }

    private int GetPhase9GuiScale(int framebufferWidth, int framebufferHeight)
    {
        var tag = (_phase9GuiScaleCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "2";
        if (int.TryParse(tag, out var fixedScale))
            return Math.Clamp(fixedScale, 1, 8);

        var scale = 1;
        while (scale < 8 &&
               framebufferWidth / (scale + 1) >= 320 &&
               framebufferHeight / (scale + 1) >= 240)
        {
            scale++;
        }
        return scale;
    }

    private void ConfigurePhase9LiveReload()
    {
        if (!_phase9Initialized)
            return;

        _phase9LiveReload?.Dispose();
        _phase9LiveReload = null;
        _phase9WatchedRepo = _repoRoot;
        if (_phase9LiveReloadToggle?.IsChecked != true ||
            string.IsNullOrWhiteSpace(_repoRoot) ||
            !Directory.Exists(_repoRoot))
            return;

        var watcher = new SourceLiveReloadService();
        watcher.Changed += Phase9SourceFilesChanged;
        watcher.Start(_repoRoot);
        _phase9LiveReload = watcher;
    }

    private void Phase9SourceFilesChanged(object? sender, SourceFilesChangedEventArgs e)
    {
        if (DateTime.UtcNow < _phase9IgnoreSourceEventsUntil)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (_phase9LiveReloadToggle?.IsChecked != true || DateTime.UtcNow < _phase9IgnoreSourceEventsUntil)
                return;
            ReloadPhase9Source(automatic: true, forceReimport: false, changedPaths: e.Paths);
        }));
    }

    private void ManualPhase9SourceReload()
    {
        if (HasPhase9LocalEdits())
        {
            var answer = MessageBox.Show(
                this,
                "The active imported machine has local editor changes. Reloading from Java will discard those editor changes. Continue?",
                "Reload source",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
                return;
        }
        ReloadPhase9Source(automatic: false, forceReimport: true, changedPaths: null);
    }

    private void ReloadPhase9Source(bool automatic, bool forceReimport, IReadOnlyList<string>? changedPaths)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;

        try
        {
            var activeClass = _activeForeverTemplate?.ScreenClass;
            var selectedClass = (_foreverTemplateCombo?.SelectedItem as ForeverMachineTemplate)?.ScreenClass ?? activeClass;
            var hadLocalEdits = HasPhase9LocalEdits();
            var result = ForeverProjectScanner.Scan(_repoRoot);

            _foreverTemplates.Clear();
            _foreverTemplates.AddRange(result.Templates);
            if (_foreverTemplateCombo is not null)
            {
                _foreverTemplateCombo.ItemsSource = null;
                _foreverTemplateCombo.ItemsSource = _foreverTemplates;
                var selected = _foreverTemplates.FirstOrDefault(template =>
                    string.Equals(template.ScreenClass, selectedClass, StringComparison.OrdinalIgnoreCase));
                _foreverTemplateCombo.SelectedItem = selected ?? _foreverTemplates.FirstOrDefault();
            }

            var replacement = _foreverTemplates.FirstOrDefault(template =>
                string.Equals(template.ScreenClass, activeClass, StringComparison.OrdinalIgnoreCase));
            if (replacement is not null)
            {
                _activeForeverTemplate = replacement;
                RemoveForeverPreviewVisual();
                if (forceReimport || !hadLocalEdits)
                {
                    if (_foreverTemplateCombo is not null)
                        _foreverTemplateCombo.SelectedItem = replacement;
                    LoadSelectedForeverTemplate();
                    CapturePhase9SourceBaseline();
                }
                else
                {
                    EnsureForeverVisualPreview();
                }
            }

            RefreshAssets();
            _phase9LastValidationSnapshot = null;
            RunPhase9Validation(force: true);
            RefreshPhase9Simulator();

            var changed = changedPaths is { Count: > 0 }
                ? $" · {changedPaths.Count} source change(s)"
                : string.Empty;
            var warnings = result.Warnings.Count > 0 ? $" · {result.Warnings.Count} scan warning(s)" : string.Empty;
            if (automatic && hadLocalEdits && replacement is not null)
            {
                StatusText.Text = $"Live source reload: Java refreshed, local editor changes preserved{changed}{warnings}.";
            }
            else
            {
                StatusText.Text = $"Source reloaded: {_foreverTemplates.Count} machine template(s){changed}{warnings}.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Source reload failed: {ex.Message}";
        }
    }

    private void CapturePhase9SourceBaseline()
    {
        _phase9SourceBaseline = _activeForeverTemplate is null ? null : SerializeLayout();
    }

    private bool HasPhase9LocalEdits()
        => _activeForeverTemplate is not null &&
           _phase9SourceBaseline is not null &&
           !string.Equals(_phase9SourceBaseline, SerializeLayout(), StringComparison.Ordinal);

    private void ReviewPhase9Java_Click(object sender, RoutedEventArgs e)
    {
        RunPhase9Validation(force: true);
        var errors = _phase9ValidationIssues.Where(issue => issue.Severity == GuiValidationSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                $"Fix the {errors.Count} validation error(s) before Java apply-back. Select an issue in the Validation panel to jump to its element.",
                "Java apply-back blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (_activeForeverTemplate is null || string.IsNullOrWhiteSpace(_repoRoot))
        {
            MessageBox.Show(this, "Load a scanned Forever machine first.", "Java review", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        JavaSourceSyncPlan plan;
        try
        {
            plan = JavaSourceSyncService.CreatePlan(_repoRoot, _activeForeverTemplate, _layout);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not build Java diff", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ShowPhase9JavaReview(plan);
    }

    private void ShowPhase9JavaReview(JavaSourceSyncPlan plan)
    {
        var diagnostics = plan.Diagnostics.Count == 0
            ? "No diagnostics."
            : string.Join(Environment.NewLine, plan.Diagnostics.Select(item => item.ToString()));
        var diff = string.IsNullOrWhiteSpace(plan.Diff) ? "No Java changes." : plan.Diff;
        var text = diagnostics + Environment.NewLine + Environment.NewLine + diff;

        var reviewText = new TextBox
        {
            Text = text,
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(10)
        };
        var apply = new Button
        {
            Content = "Apply reviewed changes",
            IsEnabled = plan.CanApply,
            MinWidth = 160,
            Padding = new Thickness(10, 6, 10, 6)
        };
        var close = new Button
        {
            Content = "Close",
            MinWidth = 90,
            Padding = new Thickness(10, 6, 10, 6)
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10, 0, 10, 10)
        };
        buttons.Children.Add(apply);
        buttons.Children.Add(close);
        var root = new DockPanel { Background = Brush("#17191C") };
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(reviewText);

        var window = new Window
        {
            Owner = this,
            Title = $"Java Diff Review — {_activeForeverTemplate?.DisplayName ?? _layout.Name}",
            Width = 1040,
            Height = 720,
            MinWidth = 720,
            MinHeight = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#17191C"),
            Content = root
        };
        close.Click += (_, _) => window.Close();
        apply.Click += (_, _) =>
        {
            try
            {
                _phase9IgnoreSourceEventsUntil = DateTime.UtcNow.AddSeconds(2);
                JavaSourceSyncService.Apply(plan);
                _phase9SourceBaseline = null;
                ReloadPhase9Source(automatic: false, forceReimport: true, changedPaths: null);
                StatusText.Text = $"Applied reviewed Java geometry to {plan.Changes.Count} file(s) and reloaded the machine.";
                window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, ex.Message, "Java apply-back failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        window.ShowDialog();
    }
}

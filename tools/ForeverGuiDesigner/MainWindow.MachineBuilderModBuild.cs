using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// One-click local mod build/test tooling for Machine Builder Phase 6. A production action first
/// saves the Machine Project, validates/applies pending generated output, then delegates to the
/// repository-owned Gradle wrapper without blocking the WPF UI thread.
/// </summary>
public partial class MainWindow
{
    private const int MachineBuilderGradleLogLineLimit = 500;

    private readonly Queue<string> _machineBuilderGradleLogLines = new();
    private Button? _machineBuilderBuildJarButton;
    private Button? _machineBuilderLaunchClientButton;
    private Button? _machineBuilderCancelGradleButton;
    private Button? _machineBuilderOpenJarFolderButton;
    private TextBlock? _machineBuilderGradleStatus;
    private TextBlock? _machineBuilderGradleOutputPath;
    private TextBox? _machineBuilderGradleLog;
    private CancellationTokenSource? _machineBuilderGradleCancellation;
    private string? _machineBuilderLastJarPath;
    private bool _machineBuilderGradleBusy;
    private bool _machineBuilderGradleCloseHookInstalled;

    public void EnableForeverMachineBuilderModBuild()
        => InstallMachineBuilderModBuildControls();

    private void InstallMachineBuilderModBuildControls()
    {
        if (_machineBuilderPhase6Surface?.Child is not Grid shell ||
            shell.Children.OfType<FrameworkElement>().Any(element =>
                string.Equals(element.Tag?.ToString(), "machine_builder_mod_build", StringComparison.Ordinal)))
            return;

        shell.RowDefinitions.Insert(5, new RowDefinition { Height = GridLength.Auto });
        foreach (var child in shell.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) >= 5).ToList())
            Grid.SetRow(child, Grid.GetRow(child) + 1);

        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        card.Tag = "machine_builder_mod_build";
        card.Margin = new Thickness(0, 12, 0, 0);
        var grid = new Grid { Margin = new Thickness(14, 12, 14, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(178) });

        var console = new StackPanel();
        console.Children.Add(new TextBlock
        {
            Text = "MOD BUILD & TEST",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        });
        console.Children.Add(new TextBlock
        {
            Text = "One click saves and applies the current Machine Project, then runs the repository Gradle wrapper. The test client stays attached so it can be stopped cleanly from Studio.",
            Foreground = Brush("#7F929A"),
            FontSize = 9.3,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 8)
        });
        _machineBuilderGradleStatus = new TextBlock
        {
            Text = "READY",
            Foreground = Brush("#8FA1A8"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        };
        console.Children.Add(_machineBuilderGradleStatus);
        _machineBuilderGradleOutputPath = new TextBlock
        {
            Foreground = Brush("#AFC0C6"),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 9,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 6)
        };
        console.Children.Add(_machineBuilderGradleOutputPath);
        _machineBuilderGradleLog = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brush("#081014"),
            Foreground = Brush("#B9C8CD"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 9,
            Height = 96,
            Padding = new Thickness(7, 5, 7, 5)
        };
        console.Children.Add(_machineBuilderGradleLog);
        grid.Children.Add(console);

        var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _machineBuilderBuildJarButton = CreateStudioChromeButton("BUILD MOD JAR", 178, true);
        _machineBuilderBuildJarButton.Height = 36;
        _machineBuilderBuildJarButton.ToolTip = "Save + apply the current machine, run Gradle build, and locate the distributable JAR.";
        _machineBuilderBuildJarButton.Click += async (_, _) =>
            await RunMachineBuilderGradleOperationAsync(ForeverStudioGradleOperation.BuildModJar);
        actions.Children.Add(_machineBuilderBuildJarButton);

        _machineBuilderLaunchClientButton = CreateStudioChromeButton("LAUNCH TEST CLIENT", 178, false);
        _machineBuilderLaunchClientButton.Height = 34;
        _machineBuilderLaunchClientButton.Margin = new Thickness(0, 7, 0, 0);
        _machineBuilderLaunchClientButton.ToolTip = "Save + apply the current machine, then run the NeoForge development client from this repository.";
        _machineBuilderLaunchClientButton.Click += async (_, _) =>
            await RunMachineBuilderGradleOperationAsync(ForeverStudioGradleOperation.LaunchTestClient);
        actions.Children.Add(_machineBuilderLaunchClientButton);

        _machineBuilderCancelGradleButton = CreateStudioChromeButton("STOP", 178, false);
        _machineBuilderCancelGradleButton.Height = 32;
        _machineBuilderCancelGradleButton.Margin = new Thickness(0, 7, 0, 0);
        _machineBuilderCancelGradleButton.IsEnabled = false;
        _machineBuilderCancelGradleButton.Click += (_, _) => _machineBuilderGradleCancellation?.Cancel();
        actions.Children.Add(_machineBuilderCancelGradleButton);

        _machineBuilderOpenJarFolderButton = CreateStudioChromeButton("OPEN JAR FOLDER", 178, false);
        _machineBuilderOpenJarFolderButton.Height = 32;
        _machineBuilderOpenJarFolderButton.Margin = new Thickness(0, 7, 0, 0);
        _machineBuilderOpenJarFolderButton.IsEnabled = false;
        _machineBuilderOpenJarFolderButton.Click += (_, _) => OpenMachineBuilderJarFolder();
        actions.Children.Add(_machineBuilderOpenJarFolderButton);

        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);
        card.Child = grid;
        Grid.SetRow(card, 5);
        shell.Children.Add(card);

        _machineBuilderPhase6Surface.IsVisibleChanged += (_, _) => RefreshMachineBuilderModBuildAvailability();
        if (!_machineBuilderGradleCloseHookInstalled)
        {
            _machineBuilderGradleCloseHookInstalled = true;
            Closed += (_, _) => _machineBuilderGradleCancellation?.Cancel();
        }
        RefreshMachineBuilderModBuildAvailability();
    }

    private async Task RunMachineBuilderGradleOperationAsync(ForeverStudioGradleOperation operation)
    {
        if (_machineBuilderGradleBusy)
            return;

        ClearMachineBuilderGradleLog();
        SetMachineBuilderGradleStatus(
            operation == ForeverStudioGradleOperation.BuildModJar ? "PREPARING MOD BUILD…" : "PREPARING TEST CLIENT…",
            "#D8B26A");
        SetMachineBuilderGradleBusy(true);

        try
        {
            if (!TryPrepareMachineBuilderForGradle(out var preparationError))
            {
                AppendMachineBuilderGradleLog("! " + preparationError);
                SetMachineBuilderGradleStatus("BLOCKED", "#E18B8B");
                return;
            }

            var repoError = ForeverStudioGradleService.ValidateRepository(_repoRoot!);
            if (repoError is not null)
            {
                AppendMachineBuilderGradleLog("! " + repoError);
                SetMachineBuilderGradleStatus("GRADLE NOT READY", "#E18B8B");
                return;
            }

            _machineBuilderGradleCancellation = new CancellationTokenSource();
            var task = ForeverStudioGradleService.TaskFor(operation);
            AppendMachineBuilderGradleLog($"> Starting Gradle task '{task}'…");
            SetMachineBuilderGradleStatus(
                operation == ForeverStudioGradleOperation.BuildModJar ? "BUILDING MOD JAR…" : "TEST CLIENT RUNNING…",
                "#D8B26A");

            var progress = new Progress<string>(AppendMachineBuilderGradleLog);
            var result = await ForeverStudioGradleService.RunAsync(
                _repoRoot!, operation, progress, _machineBuilderGradleCancellation.Token);

            if (result.Cancelled)
            {
                AppendMachineBuilderGradleLog("> Operation stopped by user.");
                SetMachineBuilderGradleStatus("STOPPED", "#D8B26A");
                return;
            }

            if (!result.Succeeded)
            {
                AppendMachineBuilderGradleLog($"! Gradle exited with code {result.ExitCode}.");
                SetMachineBuilderGradleStatus($"GRADLE FAILED · EXIT {result.ExitCode}", "#E18B8B");
                return;
            }

            if (operation == ForeverStudioGradleOperation.BuildModJar)
            {
                _machineBuilderLastJarPath = result.PrimaryJarPath;
                if (_machineBuilderGradleOutputPath is not null)
                    _machineBuilderGradleOutputPath.Text = result.PrimaryJarPath is null
                        ? "Gradle succeeded, but no distributable JAR was found under mod/build/libs."
                        : result.PrimaryJarPath;
                AppendMachineBuilderGradleLog(result.PrimaryJarPath is null
                    ? "! Build succeeded, but JAR discovery found no distributable artifact."
                    : $"> JAR: {result.PrimaryJarPath}");
                SetMachineBuilderGradleStatus(
                    result.PrimaryJarPath is null ? "BUILD SUCCEEDED · JAR NOT FOUND" : "MOD JAR READY",
                    result.PrimaryJarPath is null ? "#D8B26A" : "#74C99A");
            }
            else
            {
                AppendMachineBuilderGradleLog("> Test client closed cleanly.");
                SetMachineBuilderGradleStatus("TEST CLIENT CLOSED", "#74C99A");
            }
        }
        catch (Exception ex)
        {
            AppendMachineBuilderGradleLog("! " + ex.Message);
            SetMachineBuilderGradleStatus("OPERATION FAILED", "#E18B8B");
        }
        finally
        {
            _machineBuilderGradleCancellation?.Dispose();
            _machineBuilderGradleCancellation = null;
            SetMachineBuilderGradleBusy(false);
            RefreshMachineBuilderProductionReview();
        }
    }

    private bool TryPrepareMachineBuilderForGradle(out string error)
    {
        error = string.Empty;
        if (_machineBuilderDocument is null)
        {
            error = "No Machine Project is open.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
        {
            error = "Choose a valid local ForeverSkyblock repository first.";
            return false;
        }

        if (_machineBuilderDirty ||
            _machineBuilderSourceHash.Equals(ForeverStudioMachineProjectService.MissingHash, StringComparison.Ordinal))
        {
            AppendMachineBuilderGradleLog("> Saving Machine Project…");
            SaveMachineBuilderProject();
            if (_machineBuilderDirty ||
                _machineBuilderSourceHash.Equals(ForeverStudioMachineProjectService.MissingHash, StringComparison.Ordinal))
            {
                error = "The Machine Project could not be saved. Resolve its validation errors before building.";
                return false;
            }
        }

        RefreshMachineBuilderProductionReview();
        if (_machineBuilderProductionPlan is not { } plan)
        {
            error = "The production review could not be created.";
            return false;
        }
        if (plan.ErrorCount > 0)
        {
            var blockers = string.Join(" | ", plan.Issues
                .Where(issue => issue.Severity == StudioValidationSeverity.Error)
                .Take(3)
                .Select(issue => issue.Message));
            error = $"Production review is blocked by {plan.ErrorCount} error(s): {blockers}";
            return false;
        }

        if (plan.Changes.Count > 0)
        {
            AppendMachineBuilderGradleLog($"> Applying {plan.Changes.Count} generated file change(s)…");
            var result = ForeverStudioMachineCompilerService.Apply(plan);
            AppendMachineBuilderGradleLog($"> Machine '{result.AssetId}' applied successfully.");
            RefreshMachineBuilderAfterProductionApply();
            RefreshMachineBuilderProductionReview();
        }
        else
        {
            AppendMachineBuilderGradleLog("> Generated machine output is already up to date.");
        }

        return true;
    }

    private void SetMachineBuilderGradleBusy(bool busy)
    {
        _machineBuilderGradleBusy = busy;
        RefreshMachineBuilderModBuildAvailability();
        if (_machineBuilderPhase6ApplyButton is not null && busy)
            _machineBuilderPhase6ApplyButton.IsEnabled = false;
    }

    private void RefreshMachineBuilderModBuildAvailability()
    {
        var repoReady = !string.IsNullOrWhiteSpace(_repoRoot) && Directory.Exists(_repoRoot);
        if (_machineBuilderBuildJarButton is not null)
            _machineBuilderBuildJarButton.IsEnabled = repoReady && !_machineBuilderGradleBusy;
        if (_machineBuilderLaunchClientButton is not null)
            _machineBuilderLaunchClientButton.IsEnabled = repoReady && !_machineBuilderGradleBusy;
        if (_machineBuilderCancelGradleButton is not null)
            _machineBuilderCancelGradleButton.IsEnabled = _machineBuilderGradleBusy;
        if (_machineBuilderOpenJarFolderButton is not null)
            _machineBuilderOpenJarFolderButton.IsEnabled = !_machineBuilderGradleBusy &&
                (!string.IsNullOrWhiteSpace(_machineBuilderLastJarPath) ||
                 (!string.IsNullOrWhiteSpace(_repoRoot) && Directory.Exists(Path.Combine(_repoRoot, "mod", "build", "libs"))));
    }

    private void SetMachineBuilderGradleStatus(string text, string color)
    {
        if (_machineBuilderGradleStatus is null)
            return;
        _machineBuilderGradleStatus.Text = text;
        _machineBuilderGradleStatus.Foreground = Brush(color);
    }

    private void ClearMachineBuilderGradleLog()
    {
        _machineBuilderGradleLogLines.Clear();
        if (_machineBuilderGradleLog is not null)
            _machineBuilderGradleLog.Clear();
        if (_machineBuilderGradleOutputPath is not null)
            _machineBuilderGradleOutputPath.Text = string.Empty;
    }

    private void AppendMachineBuilderGradleLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;
        _machineBuilderGradleLogLines.Enqueue(line.TrimEnd());
        while (_machineBuilderGradleLogLines.Count > MachineBuilderGradleLogLineLimit)
            _machineBuilderGradleLogLines.Dequeue();
        if (_machineBuilderGradleLog is null)
            return;
        _machineBuilderGradleLog.Text = string.Join(Environment.NewLine, _machineBuilderGradleLogLines);
        _machineBuilderGradleLog.ScrollToEnd();
    }

    private void OpenMachineBuilderJarFolder()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;
        var folder = !string.IsNullOrWhiteSpace(_machineBuilderLastJarPath)
            ? Path.GetDirectoryName(_machineBuilderLastJarPath)
            : Path.Combine(_repoRoot, "mod", "build", "libs");
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppendMachineBuilderGradleLog("! Could not open JAR folder: " + ex.Message);
        }
    }
}

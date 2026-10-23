using System.Windows;
using System.Windows.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private readonly UpdatePreferences _updatePreferences = SelfUpdateService.LoadPreferences();
    private UpdateManifest? _availableUpdate;
    private ComboBox? _updateChannelCombo;
    private TextBlock? _updateStatusText;
    private Button? _updateButton;
    private Button? _updateLogButton;
    private CancellationTokenSource? _updateOperationCancellation;
    private bool _updateBusy;
    private bool _updaterInitialized;

    public void EnableSelfUpdater()
    {
        if (_updaterInitialized)
            return;
        _updaterInitialized = true;

        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("UPDATE"));

        _updateChannelCombo = new ComboBox
        {
            Width = 116,
            ToolTip = "Choose which Forever GUI Designer update channel to follow. Development tracks this editor branch; Stable tracks main."
        };
        _updateChannelCombo.Items.Add(ComboItem("Development", "development"));
        _updateChannelCombo.Items.Add(ComboItem("Stable", "stable"));
        _updateChannelCombo.SelectedIndex = _updatePreferences.Channel == UpdateChannel.Stable ? 1 : 0;
        _updateChannelCombo.SelectionChanged += UpdateChannelChanged;
        SkyblockBarHost.Children.Add(_updateChannelCombo);

        _updateStatusText = new TextBlock
        {
            Width = 170,
            Margin = new Thickness(5, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#93A5B0"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Text = $"Installed {SelfUpdateService.ShortCommit(SelfUpdateService.CurrentCommit)}"
        };
        SkyblockBarHost.Children.Add(_updateStatusText);

        _updateButton = new Button
        {
            Content = "Check",
            ToolTip = "Check GitHub for a newer Forever GUI Designer build. During a check this button cancels the operation."
        };
        _updateButton.Click += UpdateButton_Click;
        SkyblockBarHost.Children.Add(_updateButton);

        _updateLogButton = new Button
        {
            Content = "Log",
            ToolTip = $"Open updater diagnostics: {UpdaterDiagnostics.LogPath}"
        };
        _updateLogButton.Click += (_, _) => UpdaterDiagnostics.OpenLog();
        SkyblockBarHost.Children.Add(_updateLogButton);

        UpdaterDiagnostics.Write($"Updater initialized. Build={SelfUpdateService.CurrentCommit}, channel={SelectedUpdateChannel}, autoCheck={_updatePreferences.AutoCheck}.");

        if (_updatePreferences.AutoCheck)
            _ = CheckForUpdatesAsync(showErrors: false);
    }

    private UpdateChannel SelectedUpdateChannel
        => _updateChannelCombo?.SelectedItem is ComboBoxItem { Tag: string tag } &&
           string.Equals(tag, "stable", StringComparison.OrdinalIgnoreCase)
            ? UpdateChannel.Stable
            : UpdateChannel.Development;

    private async void UpdateChannelChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updaterInitialized)
            return;

        if (_updateBusy)
            _updateOperationCancellation?.Cancel();

        _updatePreferences.Channel = SelectedUpdateChannel;
        SelfUpdateService.SavePreferences(_updatePreferences);
        _availableUpdate = null;
        SetUpdateButton("Check", true, accent: false);
        await CheckForUpdatesAsync(showErrors: false);
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updateBusy)
        {
            UpdaterDiagnostics.Write("User requested cancellation of the active updater operation.");
            _updateOperationCancellation?.Cancel();
            SetUpdateStatus("Canceling update check…", "#C5A474");
            SetUpdateButton("Canceling…", false, accent: false);
            return;
        }

        if (_availableUpdate is null)
        {
            await CheckForUpdatesAsync(showErrors: true);
            return;
        }

        var manifest = _availableUpdate;
        var notes = string.IsNullOrWhiteSpace(manifest.Notes) ? string.Empty : $"\n\n{manifest.Notes}";
        var answer = MessageBox.Show(this,
            $"Install Forever GUI Designer update {SelfUpdateService.ShortCommit(manifest.Commit)} from the {SelectedUpdateChannel} channel?" +
            $"\n\nInstalled: {SelfUpdateService.ShortCommit(SelfUpdateService.CurrentCommit)}" +
            $"\nAvailable: {SelfUpdateService.ShortCommit(manifest.Commit)}" +
            notes +
            "\n\nThe editor will close, replace its executable and restart automatically.",
            "Update Forever GUI Designer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        await DownloadAndInstallAsync(manifest);
    }

    private async Task CheckForUpdatesAsync(bool showErrors)
    {
        if (_updateBusy)
            return;

        _updateBusy = true;
        _availableUpdate = null;
        var channel = SelectedUpdateChannel;
        var operationCancellation = new CancellationTokenSource();
        _updateOperationCancellation = operationCancellation;

        SetUpdateButton("Cancel", true, accent: false);
        SetUpdateStatus($"Checking {channel}…", "#93A5B0");
        UpdaterDiagnostics.Write($"Update check started for {channel}. UI thread={Environment.CurrentManagedThreadId}.");

        try
        {
            // The entire network resolver is started on a worker thread. This is
            // intentionally stronger than normal async HttpClient usage: if Windows
            // blocks synchronously in DNS/TLS/proxy setup before GetAsync yields a
            // Task, the WPF dispatcher still remains free to run the watchdog/cancel.
            var resolveTask = Task.Run(
                () => ResolveUpdateManifestAsync(channel, operationCancellation.Token),
                CancellationToken.None);

            UpdaterDiagnostics.Write("Network resolver dispatched to worker thread; 18-second UI watchdog armed.");

            var watchdogTask = Task.Delay(TimeSpan.FromSeconds(18));
            var completed = await Task.WhenAny(resolveTask, watchdogTask);

            if (completed != resolveTask)
            {
                UpdaterDiagnostics.Write("UI watchdog fired after 18 seconds; abandoning network resolver.");
                operationCancellation.Cancel();
                _ = resolveTask.ContinueWith(
                    task => UpdaterDiagnostics.Write($"Abandoned resolver later completed: status={task.Status}, error={task.Exception?.GetBaseException().Message}"),
                    TaskScheduler.Default);
                throw new TimeoutException("The complete update check exceeded the 18 second UI safety timeout. See updater.log for the last completed network step.");
            }

            var manifest = await resolveTask;
            UpdaterDiagnostics.Write(manifest is null
                ? "Update resolver completed without a release manifest."
                : $"Update resolver completed. Remote build={manifest.Commit}, size={manifest.Size}.");

            if (manifest is null)
            {
                SetUpdateStatus(channel == UpdateChannel.Stable
                    ? "No Stable build yet"
                    : "No Development build yet", "#93A5B0");
                SetUpdateButton("Check", true, accent: false);
                return;
            }

            if (!SelfUpdateService.IsUpdateAvailable(manifest))
            {
                SetUpdateStatus($"Up to date · {SelfUpdateService.ShortCommit(manifest.Commit)}", "#86C99B");
                SetUpdateButton("Check", true, accent: false);
                return;
            }

            _availableUpdate = manifest;
            SetUpdateStatus($"Update available · {SelfUpdateService.ShortCommit(manifest.Commit)}", "#8FD7E8");
            SetUpdateButton("Update & Restart", true, accent: true);
        }
        catch (OperationCanceledException)
        {
            UpdaterDiagnostics.Write("Update check canceled.");
            SetUpdateStatus("Update check canceled", "#C5A474");
            SetUpdateButton("Retry", true, accent: false);
        }
        catch (Exception ex)
        {
            UpdaterDiagnostics.Write($"Update check failed: {ex}");
            SetUpdateStatus("Update check unavailable", "#C5A474");
            SetUpdateButton("Retry", true, accent: false);
            if (showErrors)
            {
                MessageBox.Show(this,
                    $"Could not check for updates.\n\n{ex.Message}\n\nDiagnostics: {UpdaterDiagnostics.LogPath}",
                    "Update check failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        finally
        {
            if (ReferenceEquals(_updateOperationCancellation, operationCancellation))
                _updateOperationCancellation = null;
            _updateBusy = false;
            UpdaterDiagnostics.Write("Update check UI operation finished and busy state released.");
        }
    }

    private static async Task<UpdateManifest?> ResolveUpdateManifestAsync(
        UpdateChannel channel,
        CancellationToken cancellationToken)
    {
        UpdaterDiagnostics.Write($"Worker resolver entered on thread {Environment.CurrentManagedThreadId}; trying GitHub API.");
        Exception? primaryError = null;
        try
        {
            var result = await SelfUpdateService.CheckAsync(channel, cancellationToken);
            UpdaterDiagnostics.Write("GitHub API path completed successfully.");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UpdaterDiagnostics.Write("GitHub API path observed cancellation.");
            throw;
        }
        catch (Exception ex)
        {
            primaryError = ex;
            UpdaterDiagnostics.Write($"GitHub API path failed: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            UpdaterDiagnostics.Write("Trying release-manifest fallback path.");
            var result = await ReleaseManifestFallback.CheckAsync(channel, cancellationToken);
            UpdaterDiagnostics.Write("Release-manifest fallback completed successfully.");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UpdaterDiagnostics.Write("Release-manifest fallback observed cancellation.");
            throw;
        }
        catch (Exception fallbackError)
        {
            UpdaterDiagnostics.Write($"Release-manifest fallback failed: {fallbackError.GetType().Name}: {fallbackError.Message}");
            throw new InvalidOperationException(
                $"GitHub API: {primaryError?.Message ?? "unknown error"}\n" +
                $"Release manifest fallback: {fallbackError.Message}",
                fallbackError);
        }
    }

    private async Task DownloadAndInstallAsync(UpdateManifest manifest)
    {
        _updateBusy = true;
        var operationCancellation = new CancellationTokenSource();
        _updateOperationCancellation = operationCancellation;
        SetUpdateButton("Cancel", true, accent: true);
        SetUpdateStatus("Downloading update…", "#8FD7E8");
        UpdaterDiagnostics.Write($"Update download started. Build={manifest.Commit}, size={manifest.Size}.");

        try
        {
            var progress = new Progress<double>(value =>
            {
                var percent = (int)Math.Round(value * 100);
                SetUpdateStatus($"Downloading… {percent}%", "#8FD7E8");
                SetUpdateButton("Cancel", true, accent: true);
            });

            var downloaded = await SelfUpdateService.DownloadAsync(
                SelectedUpdateChannel,
                manifest,
                progress,
                operationCancellation.Token);
            UpdaterDiagnostics.Write($"Update download and verification completed: {downloaded}");
            SetUpdateStatus("Verified · restarting…", "#86C99B");
            SetUpdateButton("Restarting…", false, accent: true);

            SelfUpdateService.BeginApplyAndRestart(downloaded, manifest);
            UpdaterDiagnostics.Write("Update helper started; shutting down editor.");
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            UpdaterDiagnostics.Write("Update download canceled.");
            SetUpdateStatus("Update canceled", "#C5A474");
            SetUpdateButton("Retry", true, accent: false);
        }
        catch (Exception ex)
        {
            UpdaterDiagnostics.Write($"Update install failed: {ex}");
            SetUpdateStatus("Update failed", "#E3A28E");
            SetUpdateButton("Retry", true, accent: false);
            MessageBox.Show(this,
                $"The update could not be installed. The current editor was not replaced.\n\n{ex.Message}\n\nDiagnostics: {UpdaterDiagnostics.LogPath}",
                "Update failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (ReferenceEquals(_updateOperationCancellation, operationCancellation))
                _updateOperationCancellation = null;
            _updateBusy = false;
        }
    }

    private void SetUpdateStatus(string text, string color)
    {
        if (_updateStatusText is null)
            return;
        _updateStatusText.Text = text;
        _updateStatusText.Foreground = Brush(color);
        _updateStatusText.ToolTip = $"Current build: {SelfUpdateService.CurrentCommit}\nLog: {UpdaterDiagnostics.LogPath}";
    }

    private void SetUpdateButton(string text, bool enabled, bool accent)
    {
        if (_updateButton is null)
            return;
        _updateButton.Content = text;
        _updateButton.IsEnabled = enabled;
        _updateButton.Background = Brush(accent ? "#295A67" : "#30363D");
        _updateButton.BorderBrush = Brush(accent ? "#76D8D2" : "#4A535D");
    }
}

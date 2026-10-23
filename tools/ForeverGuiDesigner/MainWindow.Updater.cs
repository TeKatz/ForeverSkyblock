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
            ToolTip = "Check GitHub for a newer Forever GUI Designer build. Updates are only installed after you confirm."
        };
        _updateButton.Click += UpdateButton_Click;
        SkyblockBarHost.Children.Add(_updateButton);

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

        _updatePreferences.Channel = SelectedUpdateChannel;
        SelfUpdateService.SavePreferences(_updatePreferences);
        _availableUpdate = null;
        SetUpdateButton("Check", true, accent: false);
        await CheckForUpdatesAsync(showErrors: false);
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updateBusy)
            return;

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
        SetUpdateButton("Checking…", false, accent: false);
        SetUpdateStatus($"Checking {SelectedUpdateChannel}…", "#93A5B0");

        try
        {
            var manifest = await ResolveUpdateManifestAsync(SelectedUpdateChannel);
            if (manifest is null)
            {
                SetUpdateStatus(SelectedUpdateChannel == UpdateChannel.Stable
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
        catch (Exception ex)
        {
            SetUpdateStatus("Update check unavailable", "#C5A474");
            SetUpdateButton("Retry", true, accent: false);
            if (showErrors)
            {
                MessageBox.Show(this,
                    $"Could not check for updates through either GitHub check path.\n\n{ex.Message}",
                    "Update check failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        finally
        {
            _updateBusy = false;
        }
    }

    private static async Task<UpdateManifest?> ResolveUpdateManifestAsync(UpdateChannel channel)
    {
        Exception? primaryError = null;
        try
        {
            return await SelfUpdateService.CheckAsync(channel);
        }
        catch (Exception ex)
        {
            primaryError = ex;
        }

        try
        {
            return await ReleaseManifestFallback.CheckAsync(channel);
        }
        catch (Exception fallbackError)
        {
            throw new InvalidOperationException(
                $"GitHub API: {primaryError?.Message ?? "unknown error"}\n" +
                $"Release manifest fallback: {fallbackError.Message}",
                fallbackError);
        }
    }

    private async Task DownloadAndInstallAsync(UpdateManifest manifest)
    {
        _updateBusy = true;
        SetUpdateButton("Downloading…", false, accent: true);
        SetUpdateStatus("Downloading update…", "#8FD7E8");

        try
        {
            var progress = new Progress<double>(value =>
            {
                var percent = (int)Math.Round(value * 100);
                SetUpdateStatus($"Downloading… {percent}%", "#8FD7E8");
                SetUpdateButton($"{percent}%", false, accent: true);
            });

            var downloaded = await SelfUpdateService.DownloadAsync(SelectedUpdateChannel, manifest, progress);
            SetUpdateStatus("Verified · restarting…", "#86C99B");
            SetUpdateButton("Restarting…", false, accent: true);

            SelfUpdateService.BeginApplyAndRestart(downloaded, manifest);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            SetUpdateStatus("Update failed", "#E3A28E");
            SetUpdateButton("Retry", true, accent: false);
            MessageBox.Show(this,
                $"The update could not be installed. The current editor was not replaced.\n\n{ex.Message}",
                "Update failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _updateBusy = false;
        }
    }

    private void SetUpdateStatus(string text, string color)
    {
        if (_updateStatusText is null)
            return;
        _updateStatusText.Text = text;
        _updateStatusText.Foreground = Brush(color);
        _updateStatusText.ToolTip = $"Current build: {SelfUpdateService.CurrentCommit}";
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

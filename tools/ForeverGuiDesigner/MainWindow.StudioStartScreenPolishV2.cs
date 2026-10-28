using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Second presentation pass for the Studio entry screens. This intentionally augments
/// the first polish layer instead of replacing project scanning/navigation behavior.
/// It uses the otherwise empty lower regions for updater/status information and removes
/// the default WPF button chrome that could flash behind the custom Studio surfaces.
/// </summary>
public partial class MainWindow
{
    private bool _studioStartScreenPolishV2Initialized;
    private readonly List<StudioStartUpdateSurface> _studioStartUpdateSurfaces = [];
    private DispatcherTimer? _studioStartUpdateMirrorTimer;
    private bool _studioStartUpdateChannelSync;

    public void EnableForeverStudioStartScreenPolishV2()
    {
        if (_studioStartScreenPolishV2Initialized)
            return;
        _studioStartScreenPolishV2Initialized = true;

        ExpandStudioLauncherComposition();
        ExpandStudioProjectHomeComposition();

        if (_studioLauncher is not null)
            RemoveDefaultStudioButtonChrome(_studioLauncher);
        if (_studioProjectHome is not null)
            RemoveDefaultStudioButtonChrome(_studioProjectHome);

        StartStudioUpdateSurfaceMirror();
    }

    private void ExpandStudioLauncherComposition()
    {
        if (_studioLauncher is not Grid root)
            return;

        var shell = root.Children
            .OfType<Grid>()
            .FirstOrDefault(grid => grid.MaxWidth >= 1100 && grid.ColumnDefinitions.Count == 2);
        if (shell is null)
            return;

        shell.MaxWidth = 1240;
        shell.Margin = new Thickness(48, 34, 48, 38);
        if (shell.RowDefinitions.Count == 0)
        {
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        foreach (UIElement child in shell.Children)
        {
            if (Grid.GetRow(child) == 0)
                Grid.SetRow(child, 0);
        }

        var updater = CreateStudioStartUpdatePanel(compact: true);
        Grid.SetRow(updater, 2);
        Grid.SetColumn(updater, 0);
        Grid.SetColumnSpan(updater, 2);
        shell.Children.Add(updater);
    }

    private void ExpandStudioProjectHomeComposition()
    {
        if (_studioProjectHome is null)
            return;

        var heading = FindStudioVisualChildren<TextBlock>(_studioProjectHome)
            .FirstOrDefault(text => string.Equals(text.Text, "Choose what you want to shape", StringComparison.Ordinal));
        if (heading is not null && VisualTreeHelper.GetParent(heading) is StackPanel workspaceArea)
        {
            foreach (var cardButton in FindStudioVisualChildren<Button>(workspaceArea)
                         .Where(button => double.IsNaN(button.Height) && ContainsStudioWorkspaceTitle(button)))
            {
                cardButton.MinHeight = 322;
            }

            var updater = CreateStudioStartUpdatePanel(compact: false);
            updater.Margin = new Thickness(0, 22, 0, 0);
            workspaceArea.Children.Add(updater);
        }

        if (_studioProjectHome is Grid root)
        {
            var shell = root.Children
                .OfType<Grid>()
                .FirstOrDefault(grid => grid.MaxWidth >= 1500 && grid.RowDefinitions.Count >= 3);
            if (shell is not null)
            {
                shell.MaxWidth = 1600;
                shell.Margin = new Thickness(38, 30, 38, 34);
            }
        }
    }

    private static bool ContainsStudioWorkspaceTitle(Button button)
    {
        if (button.Content is not DependencyObject content)
            return false;

        return FindStudioVisualChildren<TextBlock>(content)
            .Any(text => text.Text is "GUI EDITOR" or "BLOCK EDITOR");
    }

    private Border CreateStudioStartUpdatePanel(bool compact)
    {
        var card = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        card.MinHeight = compact ? 102 : 122;

        var grid = new Grid { Margin = compact ? new Thickness(22, 17, 22, 17) : new Thickness(24, 20, 24, 20) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var eyebrow = new Grid();
        eyebrow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        eyebrow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        eyebrow.Children.Add(new TextBlock
        {
            Text = "STUDIO UPDATE",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var autoPill = CreateStudioStatusPill(
            _updatePreferences.AutoCheck ? "AUTO" : "MANUAL",
            _updatePreferences.AutoCheck ? "#82D5A2" : "#9AABB2",
            _updatePreferences.AutoCheck ? "#173427" : "#1D282D");
        autoPill.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(autoPill, 1);
        eyebrow.Children.Add(autoPill);
        copy.Children.Add(eyebrow);

        copy.Children.Add(new TextBlock
        {
            Text = compact ? "Keep Forever Studio current" : "Updates without leaving Project Home",
            Foreground = Brush("#EDF5F6"),
            FontSize = compact ? 16 : 17,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 0)
        });

        var status = new TextBlock
        {
            Text = _updateStatusText?.Text ?? $"Installed {SelfUpdateService.ShortCommit(SelfUpdateService.CurrentCommit)}",
            Foreground = _updateStatusText?.Foreground ?? Brush("#8FA2AA"),
            FontSize = 10.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 5, 0, 0),
            ToolTip = $"Current build: {SelfUpdateService.CurrentCommit}\nDiagnostics: {UpdaterDiagnostics.LogPath}"
        };
        copy.Children.Add(status);
        Grid.SetColumn(copy, 0);
        grid.Children.Add(copy);

        var channelBlock = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        channelBlock.Children.Add(new TextBlock
        {
            Text = "CHANNEL",
            Foreground = Brush("#687D86"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        var channel = new ComboBox
        {
            Width = compact ? 150 : 166,
            Height = 36,
            Padding = new Thickness(9, 4, 9, 4),
            Background = Brush("#111A1F"),
            Foreground = Brush("#E3ECEF"),
            BorderBrush = Brush("#354A53"),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        channel.Items.Add(ComboItem("Development", "development"));
        channel.Items.Add(ComboItem("Stable", "stable"));
        channel.SelectedIndex = _updateChannelCombo?.SelectedIndex
            ?? (_updatePreferences.Channel == UpdateChannel.Stable ? 1 : 0);
        channel.SelectionChanged += StudioStartUpdateChannelChanged;
        channelBlock.Children.Add(channel);
        Grid.SetColumn(channelBlock, 2);
        grid.Children.Add(channelBlock);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        };
        var diagnostics = CreateStudioChromeButton("Log", 70, false);
        diagnostics.Click += (_, _) => UpdaterDiagnostics.OpenLog();
        actions.Children.Add(diagnostics);

        var action = CreateStudioChromeButton("Check", compact ? 142 : 154, true);
        action.Margin = new Thickness(9, 0, 0, 0);
        action.Click += UpdateButton_Click;
        actions.Children.Add(action);
        Grid.SetColumn(actions, 3);
        grid.Children.Add(actions);

        card.Child = grid;
        _studioStartUpdateSurfaces.Add(new StudioStartUpdateSurface(status, action, channel));
        return card;
    }

    private void StudioStartUpdateChannelChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_studioStartUpdateChannelSync || sender is not ComboBox source || source.SelectedIndex < 0)
            return;

        if (_updateChannelCombo is null)
            return;

        _studioStartUpdateChannelSync = true;
        try
        {
            if (_updateChannelCombo.SelectedIndex != source.SelectedIndex)
                _updateChannelCombo.SelectedIndex = source.SelectedIndex;

            foreach (var surface in _studioStartUpdateSurfaces)
            {
                if (!ReferenceEquals(surface.Channel, source) && surface.Channel.SelectedIndex != source.SelectedIndex)
                    surface.Channel.SelectedIndex = source.SelectedIndex;
            }
        }
        finally
        {
            _studioStartUpdateChannelSync = false;
        }
    }

    private void StartStudioUpdateSurfaceMirror()
    {
        if (_studioStartUpdateMirrorTimer is not null)
            return;

        SyncStudioStartUpdateSurfaces();
        _studioStartUpdateMirrorTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _studioStartUpdateMirrorTimer.Tick += (_, _) => SyncStudioStartUpdateSurfaces();
        _studioStartUpdateMirrorTimer.Start();
        Closed += (_, _) =>
        {
            _studioStartUpdateMirrorTimer?.Stop();
            _studioStartUpdateMirrorTimer = null;
        };
    }

    private void SyncStudioStartUpdateSurfaces()
    {
        var statusText = _updateStatusText?.Text ?? $"Installed {SelfUpdateService.ShortCommit(SelfUpdateService.CurrentCommit)}";
        var statusBrush = _updateStatusText?.Foreground ?? Brush("#8FA2AA");
        var buttonText = _updateButton?.Content?.ToString() ?? "Check";
        var buttonEnabled = _updateButton?.IsEnabled ?? true;
        var channelIndex = _updateChannelCombo?.SelectedIndex
            ?? (_updatePreferences.Channel == UpdateChannel.Stable ? 1 : 0);

        _studioStartUpdateChannelSync = true;
        try
        {
            foreach (var surface in _studioStartUpdateSurfaces)
            {
                if (!string.Equals(surface.Status.Text, statusText, StringComparison.Ordinal))
                    surface.Status.Text = statusText;
                if (!Equals(surface.Status.Foreground, statusBrush))
                    surface.Status.Foreground = statusBrush;
                if (!string.Equals(GetStudioChromeButtonText(surface.Action), buttonText, StringComparison.Ordinal))
                    SetStudioChromeButtonText(surface.Action, buttonText);
                if (surface.Action.IsEnabled != buttonEnabled)
                    surface.Action.IsEnabled = buttonEnabled;

                var targetOpacity = buttonEnabled ? 1d : 0.62d;
                if (Math.Abs(surface.Action.Opacity - targetOpacity) > 0.001)
                    surface.Action.Opacity = targetOpacity;
                if (surface.Channel.SelectedIndex != channelIndex)
                    surface.Channel.SelectedIndex = channelIndex;
            }
        }
        finally
        {
            _studioStartUpdateChannelSync = false;
        }
    }

    private static string? GetStudioChromeButtonText(Button button)
        => button.Content is Border { Child: TextBlock label }
            ? label.Text
            : button.Content?.ToString();

    private static void SetStudioChromeButtonText(Button button, string text)
    {
        if (button.Content is Border { Child: TextBlock label })
            label.Text = text;
        else
            button.Content = text;
    }

    private static void RemoveDefaultStudioButtonChrome(DependencyObject root)
    {
        foreach (var button in FindStudioVisualChildren<Button>(root))
        {
            if (double.IsNaN(button.Height) || button.Height < 28 || button.Height > 48)
                continue;

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.ContentSourceProperty, "Content");
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

            button.Template = new ControlTemplate(typeof(Button))
            {
                VisualTree = presenter
            };
            button.ApplyTemplate();
        }
    }

    private sealed record StudioStartUpdateSurface(TextBlock Status, Button Action, ComboBox Channel);
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForeverGuiDesigner;

/// <summary>
/// Personal workspace helpers for Timo's 3x 1920x1080 desktop setup.
/// Kept isolated from the designer/runtime preview pipeline on purpose.
/// </summary>
public partial class MainWindow
{
    private const int PersonalWorkspaceMonitorCount = 3;

    private readonly List<Button> _personalWorkspaceButtons = [];
    private bool _personalWorkspaceQolInitialized;
    private int _personalWorkspaceMode;

    public void EnablePersonalWorkspaceQoL()
    {
        if (_personalWorkspaceQolInitialized)
            return;
        _personalWorkspaceQolInitialized = true;

        InstallPersonalRepoHighlightAndWorkspaceControls();
    }

    private void InstallPersonalRepoHighlightAndWorkspaceControls()
    {
        var repoButton = FindPersonalToolbarButtons(this)
            .FirstOrDefault(button => string.Equals(button.Content as string, "Repo…", StringComparison.Ordinal));
        if (repoButton?.Parent is not StackPanel toolbar)
            return;

        var repoIndex = toolbar.Children.IndexOf(repoButton);
        if (repoIndex >= 0)
        {
            toolbar.Children.RemoveAt(repoIndex);
            repoButton.Margin = new Thickness(0);

            var repoHighlight = new Border
            {
                BorderBrush = Brush("#E34B4B"),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(1),
                Margin = new Thickness(2),
                Child = repoButton,
                ToolTip = "Repository selector — highlighted for quick access"
            };
            toolbar.Children.Insert(repoIndex, repoHighlight);
        }

        toolbar.Children.Add(new Border
        {
            Width = 1,
            Height = 24,
            Background = Brush("#3B454E"),
            Margin = new Thickness(7, 0, 7, 0)
        });
        toolbar.Children.Add(new TextBlock
        {
            Text = "WORKSPACE",
            Foreground = Brush("#7F929F"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        AddPersonalWorkspaceButton(toolbar, 1, "1×1080p", "Fit the designer to the primary 1920×1080 workspace.");
        AddPersonalWorkspaceButton(toolbar, 2, "2×1080p", "Span the designer across the primary monitor and one adjacent 1920×1080 monitor.");
        AddPersonalWorkspaceButton(toolbar, 3, "3×1080p", "Span the designer across the complete 3×1920×1080 desktop.");
    }

    private void AddPersonalWorkspaceButton(StackPanel toolbar, int monitors, string text, string tooltip)
    {
        var button = new Button
        {
            Content = text,
            Tag = monitors,
            ToolTip = tooltip,
            Padding = new Thickness(7, 5, 7, 5),
            MinWidth = 68
        };
        button.Click += PersonalWorkspace_Click;
        _personalWorkspaceButtons.Add(button);
        toolbar.Children.Add(button);
    }

    private void PersonalWorkspace_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int monitors })
            return;
        ApplyPersonalWorkspace(monitors);
    }

    private void ApplyPersonalWorkspace(int requestedMonitors)
    {
        var monitors = Math.Clamp(requestedMonitors, 1, PersonalWorkspaceMonitorCount);
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;
        var virtualRight = virtualLeft + virtualWidth;
        var primaryWorkArea = SystemParameters.WorkArea;

        // SystemParameters is already expressed in WPF device-independent pixels,
        // so using the primary work-area width automatically respects Windows DPI
        // scaling while still mapping to Timo's physical 1920px-wide screens.
        var singleScreenWidth = primaryWorkArea.Width > 0
            ? primaryWorkArea.Width
            : virtualWidth / PersonalWorkspaceMonitorCount;
        var targetWidth = Math.Min(virtualWidth, singleScreenWidth * monitors);
        var targetHeight = primaryWorkArea.Height > 0
            ? Math.Min(primaryWorkArea.Height, virtualHeight)
            : virtualHeight;

        double targetLeft;
        if (monitors == PersonalWorkspaceMonitorCount)
        {
            targetLeft = virtualLeft;
        }
        else
        {
            // Anchor 1-screen mode to the primary display. For 2-screen mode prefer
            // primary + right neighbour; if the primary is already right-most, move
            // left far enough to use primary + left neighbour instead.
            targetLeft = primaryWorkArea.Left;
            if (targetLeft + targetWidth > virtualRight)
                targetLeft = primaryWorkArea.Right - targetWidth;

            targetLeft = Math.Clamp(targetLeft, virtualLeft, Math.Max(virtualLeft, virtualRight - targetWidth));
        }

        var targetTop = Math.Clamp(primaryWorkArea.Top, virtualTop,
            Math.Max(virtualTop, virtualTop + virtualHeight - targetHeight));

        WindowState = WindowState.Normal;
        SizeToContent = SizeToContent.Manual;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = targetLeft;
        Top = targetTop;
        Width = Math.Max(MinWidth, targetWidth);
        Height = Math.Max(MinHeight, targetHeight);

        _personalWorkspaceMode = monitors;
        UpdatePersonalWorkspaceButtons();
        StatusText.Text = monitors switch
        {
            1 => "Workspace: 1×1080p · primary monitor",
            2 => "Workspace: 2×1080p · primary + adjacent monitor",
            _ => "Workspace: 3×1080p · full triple-monitor desktop"
        };
    }

    private void UpdatePersonalWorkspaceButtons()
    {
        foreach (var button in _personalWorkspaceButtons)
        {
            var active = button.Tag is int mode && mode == _personalWorkspaceMode;
            button.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
            button.Background = active ? Brush("#365563") : Brush("#30363D");
        }
    }

    private static IEnumerable<Button> FindPersonalToolbarButtons(DependencyObject root)
    {
        if (root is null)
            yield break;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button button)
                yield return button;
            foreach (var nested in FindPersonalToolbarButtons(child))
                yield return nested;
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private const string EmptyStateKittenResource = "pack://application:,,,/Assets/empty-state-kitten.png";
    private const string ApplicationIconResource = "pack://application:,,,/Assets/ForeverGuiDesigner.ico";

    private Grid? _emptyStateOverlay;
    private GuiLayoutModel? _openLayoutBefore;
    private bool _startupPresentationHooksInitialized;

    public void PrepareStartupPresentation()
    {
        WindowState = WindowState.Maximized;
        ApplyApplicationIcon();

        // The constructor still has to initialize the editor controls first, but the
        // first window shown to the user is always a genuinely empty workspace.
        _activeForeverTemplate = null;
        LoadLayoutIntoEditor(new GuiLayoutModel(), null, clearHistory: true);

        EnsureEmptyStateOverlay();
        UpdateEmptyStatePresentation();
    }

    public void EnableStartupPresentationHooks()
    {
        if (_startupPresentationHooksInitialized)
            return;
        _startupPresentationHooksInitialized = true;

        EnsureEmptyStateOverlay();
        ElementsList.ItemContainerGenerator.ItemsChanged += (_, _) => UpdateEmptyStatePresentation();

        if (FindButtonByContent("New") is { } newButton)
        {
            newButton.Click += (_, _) =>
            {
                _activeForeverTemplate = null;
                UpdateEmptyStatePresentation();
            };
        }

        if (FindButtonByContent("Open") is { } openButton)
        {
            openButton.PreviewMouseLeftButtonDown += (_, _) => _openLayoutBefore = _layout;
            openButton.PreviewKeyDown += (_, e) =>
            {
                if (e.Key is Key.Enter or Key.Space)
                    _openLayoutBefore = _layout;
            };
            openButton.Click += (_, _) =>
            {
                if (_openLayoutBefore is not null && !ReferenceEquals(_openLayoutBefore, _layout))
                    _activeForeverTemplate = null;
                _openLayoutBefore = null;
                UpdateEmptyStatePresentation();
            };
        }

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.Click += (_, _) => UpdateEmptyStatePresentation();
        }

        UpdateEmptyStatePresentation();
    }

    private void EnsureEmptyStateOverlay()
    {
        if (_emptyStateOverlay is not null)
            return;

        if (LogicalTreeHelper.GetParent(PreviewCanvas) is not Border previewBorder ||
            LogicalTreeHelper.GetParent(previewBorder) is not ScrollViewer previewScroll ||
            LogicalTreeHelper.GetParent(previewScroll) is not Grid workspaceGrid)
            return;

        var kitten = new Image
        {
            Source = LoadPackBitmap(EmptyStateKittenResource),
            MaxWidth = 520,
            MaxHeight = 520,
            Margin = new Thickness(64),
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetBitmapScalingMode(kitten, BitmapScalingMode.HighQuality);

        _emptyStateOverlay = new Grid
        {
            Background = Brush("#111315"),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        _emptyStateOverlay.Children.Add(kitten);

        Grid.SetColumn(_emptyStateOverlay, Grid.GetColumn(previewScroll));
        Grid.SetColumnSpan(_emptyStateOverlay, Grid.GetColumnSpan(previewScroll));
        Grid.SetRow(_emptyStateOverlay, Grid.GetRow(previewScroll));
        Grid.SetRowSpan(_emptyStateOverlay, Grid.GetRowSpan(previewScroll));
        Panel.SetZIndex(_emptyStateOverlay, 10_000);
        workspaceGrid.Children.Add(_emptyStateOverlay);
    }

    private void UpdateEmptyStatePresentation()
    {
        if (_emptyStateOverlay is null)
            return;

        var showEmptyState = _layout.Elements.Count == 0 && !HasActiveForeverMachineLayout;
        _emptyStateOverlay.Visibility = showEmptyState ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyApplicationIcon()
    {
        try
        {
            Icon = BitmapFrame.Create(
                new Uri(ApplicationIconResource, UriKind.Absolute),
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
        }
        catch
        {
            // The executable still carries ApplicationIcon; keep startup functional
            // if a resource lookup is unavailable in an unusual hosting context.
        }
    }

    private static BitmapImage LoadPackBitmap(string uri)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(uri, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private Button? FindButtonByContent(string content)
        => FindStartupVisualChildren<Button>(this)
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), content, StringComparison.Ordinal));

    private static IEnumerable<T> FindStartupVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;

            foreach (var descendant in FindStartupVisualChildren<T>(child))
                yield return descendant;
        }
    }
}

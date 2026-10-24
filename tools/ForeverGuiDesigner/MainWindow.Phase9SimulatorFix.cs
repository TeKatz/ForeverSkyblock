using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _phase9SnapshotSimulatorInitialized;
    private Window? _phase9SnapshotSimulatorWindow;
    private Canvas? _phase9SnapshotSimulatorScreen;
    private TextBlock? _phase9SnapshotSimulatorInfo;
    private DispatcherTimer? _phase9SnapshotSimulatorTimer;
    private string? _phase9SnapshotSimulatorSignature;

    public void EnablePhase9SimulatorSnapshotFix()
    {
        if (_phase9SnapshotSimulatorInitialized)
            return;
        _phase9SnapshotSimulatorInitialized = true;

        var oldButton = SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Simulator", StringComparison.Ordinal));
        if (oldButton is not null)
        {
            var index = SkyblockBarHost.Children.IndexOf(oldButton);
            var replacement = new Button
            {
                Content = "Simulator",
                ToolTip = "Preview the current GUI centered inside a Minecraft logical viewport at the selected framebuffer resolution and GUI scale."
            };
            replacement.Click += (_, _) => ShowPhase9SnapshotSimulator();
            SkyblockBarHost.Children.RemoveAt(index);
            SkyblockBarHost.Children.Insert(index, replacement);
        }

        if (_phase9GuiScaleCombo is not null)
            _phase9GuiScaleCombo.SelectionChanged += (_, _) => RefreshPhase9SnapshotSimulator(force: true);
        if (_phase9ResolutionCombo is not null)
            _phase9ResolutionCombo.SelectionChanged += (_, _) => RefreshPhase9SnapshotSimulator(force: true);

        _phase9SnapshotSimulatorTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _phase9SnapshotSimulatorTimer.Tick += (_, _) => RefreshPhase9SnapshotSimulator(force: false);

        Closed += (_, _) =>
        {
            _phase9SnapshotSimulatorTimer?.Stop();
            _phase9SnapshotSimulatorWindow?.Close();
        };
    }

    private void ShowPhase9SnapshotSimulator()
    {
        if (_phase9SnapshotSimulatorWindow is null)
        {
            _phase9SnapshotSimulatorInfo = new TextBlock
            {
                Foreground = Brush("#DCE7EC"),
                Margin = new Thickness(12, 9, 12, 9),
                TextWrapping = TextWrapping.Wrap
            };

            _phase9SnapshotSimulatorScreen = new Canvas
            {
                Background = Brush("#080B0E"),
                ClipToBounds = true,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetEdgeMode(_phase9SnapshotSimulatorScreen, EdgeMode.Aliased);
            RenderOptions.SetBitmapScalingMode(_phase9SnapshotSimulatorScreen, BitmapScalingMode.NearestNeighbor);

            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                Margin = new Thickness(14),
                Child = _phase9SnapshotSimulatorScreen
            };

            var hint = new TextBlock
            {
                Text = "The dark area is the Minecraft logical viewport. The loaded GUI is centered inside it at the selected GUI scale.",
                Foreground = Brush("#87949D"),
                FontSize = 10,
                Margin = new Thickness(12, 0, 12, 9),
                TextWrapping = TextWrapping.Wrap
            };

            var root = new DockPanel { Background = Brush("#15191D") };
            DockPanel.SetDock(_phase9SnapshotSimulatorInfo, Dock.Top);
            DockPanel.SetDock(hint, Dock.Bottom);
            root.Children.Add(_phase9SnapshotSimulatorInfo);
            root.Children.Add(hint);
            root.Children.Add(viewbox);

            _phase9SnapshotSimulatorWindow = new Window
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
            _phase9SnapshotSimulatorWindow.Closed += (_, _) =>
            {
                _phase9SnapshotSimulatorTimer?.Stop();
                _phase9SnapshotSimulatorWindow = null;
                _phase9SnapshotSimulatorScreen = null;
                _phase9SnapshotSimulatorInfo = null;
                _phase9SnapshotSimulatorSignature = null;
            };
        }

        _phase9SnapshotSimulatorSignature = null;
        RefreshPhase9SnapshotSimulator(force: true);
        if (!_phase9SnapshotSimulatorWindow.IsVisible)
            _phase9SnapshotSimulatorWindow.Show();
        _phase9SnapshotSimulatorWindow.Activate();
        _phase9SnapshotSimulatorTimer?.Start();
    }

    private void RefreshPhase9SnapshotSimulator(bool force)
    {
        if (_phase9SnapshotSimulatorWindow?.IsVisible != true ||
            _phase9SnapshotSimulatorScreen is null ||
            _phase9SnapshotSimulatorInfo is null)
            return;

        var signature = BuildPhase9SnapshotSimulatorSignature();
        if (!force && string.Equals(signature, _phase9SnapshotSimulatorSignature, StringComparison.Ordinal))
            return;
        _phase9SnapshotSimulatorSignature = signature;

        var (framebufferWidth, framebufferHeight) = GetPhase9Resolution();
        var guiScale = GetPhase9GuiScale(framebufferWidth, framebufferHeight);
        var logicalWidth = (int)Math.Ceiling(framebufferWidth / (double)guiScale);
        var logicalHeight = (int)Math.Ceiling(framebufferHeight / (double)guiScale);

        _phase9SnapshotSimulatorScreen.Width = logicalWidth;
        _phase9SnapshotSimulatorScreen.Height = logicalHeight;
        _phase9SnapshotSimulatorScreen.Children.Clear();

        AddSimulatorGuide(
            _phase9SnapshotSimulatorScreen,
            logicalWidth / 2.0,
            0,
            logicalWidth / 2.0,
            logicalHeight);
        AddSimulatorGuide(
            _phase9SnapshotSimulatorScreen,
            0,
            logicalHeight / 2.0,
            logicalWidth,
            logicalHeight / 2.0);

        var snapshot = CapturePhase9PreviewSnapshot();
        var guiWidth = Math.Max(1, _layout.Width);
        var guiHeight = Math.Max(1, _layout.Height);
        var image = new Image
        {
            Width = guiWidth,
            Height = guiHeight,
            Source = snapshot,
            Stretch = Stretch.Fill,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        var gui = new Border
        {
            Width = guiWidth,
            Height = guiHeight,
            Background = snapshot is null ? Brush("#3A2020") : Brushes.Transparent,
            BorderBrush = snapshot is null ? Brush("#D96A63") : Brush("#6D8192"),
            BorderThickness = new Thickness(0.5),
            Child = image,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(gui, Math.Floor((logicalWidth - guiWidth) / 2.0));
        Canvas.SetTop(gui, Math.Floor((logicalHeight - guiHeight) / 2.0));
        _phase9SnapshotSimulatorScreen.Children.Add(gui);

        _phase9SnapshotSimulatorInfo.Text = snapshot is null
            ? $"Framebuffer {framebufferWidth}×{framebufferHeight} · GUI scale {guiScale}x · logical viewport {logicalWidth}×{logicalHeight} · preview snapshot unavailable"
            : $"Framebuffer {framebufferWidth}×{framebufferHeight} · GUI scale {guiScale}x · logical viewport {logicalWidth}×{logicalHeight} · " +
              $"GUI {guiWidth}×{guiHeight} logical px / {guiWidth * guiScale}×{guiHeight * guiScale} framebuffer px";
    }

    private BitmapSource? CapturePhase9PreviewSnapshot()
    {
        if (_layout.Width <= 0 || _layout.Height <= 0 || PreviewCanvas.ActualWidth <= 0 || PreviewCanvas.ActualHeight <= 0)
            return null;

        try
        {
            PreviewCanvas.UpdateLayout();

            var zoomX = PreviewScale?.ScaleX ?? 1.0;
            var zoomY = PreviewScale?.ScaleY ?? 1.0;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(_layout.Width * Math.Max(1.0, zoomX)));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(_layout.Height * Math.Max(1.0, zoomY)));

            // Capture the already composed editor canvas into a detached bitmap. This avoids
            // the cross-window VisualBrush path that can render as an empty/black surface.
            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(PreviewCanvas);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private string BuildPhase9SnapshotSimulatorSignature()
    {
        var parts = new List<string>(_layout.Elements.Count + 4)
        {
            $"{_layout.Width}x{_layout.Height}",
            ((_phase9ResolutionCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "1920x1080"),
            ((_phase9GuiScaleCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "2"),
            _phase8TimelineProgress.ToString("0.00", CultureInfo.InvariantCulture)
        };

        foreach (var element in _layout.Elements)
        {
            parts.Add(string.Join(':',
                element.Id,
                element.Type,
                element.X,
                element.Y,
                element.Width,
                element.Height,
                element.Visible,
                element.Content ?? string.Empty,
                element.TexturePath ?? string.Empty));
        }
        return string.Join('|', parts);
    }

    private static void AddSimulatorGuide(Canvas canvas, double x1, double y1, double x2, double y2)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Brush("#16212A"),
            StrokeThickness = 1,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        canvas.Children.Add(line);
    }
}

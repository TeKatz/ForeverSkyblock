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
    private BitmapSource? _phase9SnapshotSimulatorBitmap;
    private bool _phase9SnapshotCaptureQueued;
    private FrameworkElement? _phase9SnapshotVisualAnchor;
    private int _phase9SnapshotVisualCount = -1;

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
            _phase9GuiScaleCombo.SelectionChanged += (_, _) => RefreshPhase9SnapshotSimulator(capturePreview: false);
        if (_phase9ResolutionCombo is not null)
            _phase9ResolutionCombo.SelectionChanged += (_, _) => RefreshPhase9SnapshotSimulator(capturePreview: false);

        var loadButton = SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Load machine", StringComparison.Ordinal));
        if (loadButton is not null)
            loadButton.Click += (_, _) => QueuePhase9SnapshotCapture();

        ApplyButton.Click += (_, _) => QueuePhase9SnapshotCapture();

        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueuePhase9SnapshotCapture();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueuePhase9SnapshotCapture();
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueuePhase9SnapshotCapture();
        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) => QueuePhase9SnapshotCapture();

        CapturePhase9SnapshotVisualGeneration();
        PreviewCanvas.LayoutUpdated += Phase9SnapshotLayoutUpdated;

        Closed += (_, _) =>
        {
            _phase9SnapshotSimulatorWindow?.Close();
            _phase9SnapshotSimulatorBitmap = null;
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
                Text = "Dark area = Minecraft logical viewport. The GUI should appear fully visible and centered. Resolution/GUI-scale changes reuse the same native snapshot.",
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
                _phase9SnapshotSimulatorWindow = null;
                _phase9SnapshotSimulatorScreen = null;
                _phase9SnapshotSimulatorInfo = null;
                _phase9SnapshotSimulatorBitmap = null;
                _phase9SnapshotCaptureQueued = false;
            };
        }

        if (!_phase9SnapshotSimulatorWindow.IsVisible)
            _phase9SnapshotSimulatorWindow.Show();
        _phase9SnapshotSimulatorWindow.Activate();

        RefreshPhase9SnapshotSimulator(capturePreview: true);
    }

    private void Phase9SnapshotLayoutUpdated(object? sender, EventArgs e)
    {
        if (_phase9SnapshotSimulatorWindow?.IsVisible != true)
            return;
        if (!Phase9SnapshotVisualGenerationChanged())
            return;

        CapturePhase9SnapshotVisualGeneration();
        QueuePhase9SnapshotCapture();
    }

    private bool Phase9SnapshotVisualGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _phase9SnapshotVisualCount ||
               !ReferenceEquals(anchor, _phase9SnapshotVisualAnchor);
    }

    private void CapturePhase9SnapshotVisualGeneration()
    {
        _phase9SnapshotVisualCount = _visuals.Count;
        _phase9SnapshotVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private void QueuePhase9SnapshotCapture()
    {
        if (_phase9SnapshotSimulatorWindow?.IsVisible != true || _phase9SnapshotCaptureQueued)
            return;

        _phase9SnapshotCaptureQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            _phase9SnapshotCaptureQueued = false;
            if (_phase9SnapshotSimulatorWindow?.IsVisible != true)
                return;

            CapturePhase9SnapshotVisualGeneration();
            RefreshPhase9SnapshotSimulator(capturePreview: true);
        }));
    }

    private void RefreshPhase9SnapshotSimulator(bool capturePreview)
    {
        if (_phase9SnapshotSimulatorWindow?.IsVisible != true ||
            _phase9SnapshotSimulatorScreen is null ||
            _phase9SnapshotSimulatorInfo is null)
            return;

        if (capturePreview || _phase9SnapshotSimulatorBitmap is null)
            _phase9SnapshotSimulatorBitmap = CapturePhase9PreviewSnapshot();

        var (framebufferWidth, framebufferHeight) = GetPhase9Resolution();
        var guiScale = GetPhase9GuiScale(framebufferWidth, framebufferHeight);
        var logicalWidth = (int)Math.Ceiling(framebufferWidth / (double)guiScale);
        var logicalHeight = (int)Math.Ceiling(framebufferHeight / (double)guiScale);

        _phase9SnapshotSimulatorScreen.Width = logicalWidth;
        _phase9SnapshotSimulatorScreen.Height = logicalHeight;
        _phase9SnapshotSimulatorScreen.Children.Clear();

        AddSimulatorGuide(_phase9SnapshotSimulatorScreen, logicalWidth / 2.0, 0, logicalWidth / 2.0, logicalHeight);
        AddSimulatorGuide(_phase9SnapshotSimulatorScreen, 0, logicalHeight / 2.0, logicalWidth, logicalHeight / 2.0);

        var guiWidth = Math.Max(1, _layout.Width);
        var guiHeight = Math.Max(1, _layout.Height);
        var image = new Image
        {
            Width = guiWidth,
            Height = guiHeight,
            Source = _phase9SnapshotSimulatorBitmap,
            Stretch = Stretch.Fill,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        var gui = new Border
        {
            Width = guiWidth,
            Height = guiHeight,
            Background = _phase9SnapshotSimulatorBitmap is null ? Brush("#3A2020") : Brushes.Transparent,
            BorderBrush = _phase9SnapshotSimulatorBitmap is null ? Brush("#D96A63") : Brush("#6D8192"),
            BorderThickness = new Thickness(0.5),
            Child = image,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(gui, Math.Floor((logicalWidth - guiWidth) / 2.0));
        Canvas.SetTop(gui, Math.Floor((logicalHeight - guiHeight) / 2.0));
        _phase9SnapshotSimulatorScreen.Children.Add(gui);

        _phase9SnapshotSimulatorInfo.Text = _phase9SnapshotSimulatorBitmap is null
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
            var pixelWidth = Math.Max(1, _layout.Width);
            var pixelHeight = Math.Max(1, _layout.Height);

            // Do not render PreviewCanvas directly. Its editor zoom is a LayoutTransform,
            // and RenderTargetBitmap faithfully keeps that transformed visual state. A
            // native-sized target therefore captures only a zoomed fragment. Instead,
            // paint the complete visual through a VisualBrush into a fresh DrawingVisual
            // at (0,0). Stretch.Fill maps the transformed editor visual back onto exactly
            // the GUI's native logical bounds without mutating or re-laying out the editor.
            var sourceBrush = new VisualBrush(PreviewCanvas)
            {
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            var snapshotVisual = new DrawingVisual();
            RenderOptions.SetEdgeMode(snapshotVisual, EdgeMode.Aliased);
            RenderOptions.SetBitmapScalingMode(snapshotVisual, BitmapScalingMode.NearestNeighbor);
            using (var drawing = snapshotVisual.RenderOpen())
            {
                drawing.DrawRectangle(
                    sourceBrush,
                    null,
                    new Rect(0, 0, pixelWidth, pixelHeight));
            }

            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(snapshotVisual);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
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

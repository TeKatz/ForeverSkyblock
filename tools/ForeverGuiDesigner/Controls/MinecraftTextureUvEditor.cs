using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ForeverGuiDesigner.Controls;

public enum MinecraftTextureEditTool
{
    Uv,
    Pencil,
    Eraser,
    Fill,
    Eyedropper,
    Line,
    Rectangle,
    Selection
}

public sealed class MinecraftTextureUvEditor : Grid
{
    private readonly ScrollViewer _scroll;
    private readonly Canvas _surface;
    private readonly Image _image;
    private readonly Rectangle _pixelGrid;
    private readonly Rectangle _uvRect;
    private readonly Rectangle _selectionRect;
    private readonly Thumb[] _handles;
    private WriteableBitmap? _bitmap;
    private double[] _uv = [0, 0, 16, 16];
    private double _zoom = 1;
    private double _baseScale = 1;
    private bool _movingUv;
    private Point _uvMoveStart;
    private double[] _uvMoveOrigin = [0, 0, 16, 16];
    private bool _painting;
    private bool _selecting;
    private bool _movingSelection;
    private Point _lastPaintPixel = new(-1, -1);
    private Point _gestureStartPixel = new(-1, -1);
    private Int32Rect _selectionPixels = Int32Rect.Empty;
    private Int32Rect _selectionMoveOrigin = Int32Rect.Empty;
    private BitmapSource? _selectionClipboard;
    private Color _paintColor = Colors.White;
    private Color _lastPickedColor = Colors.White;

    public MinecraftTextureUvEditor()
    {
        Height = 340;
        Background = new SolidColorBrush(Color.FromRgb(9, 14, 17));
        ClipToBounds = true;

        _surface = new Canvas
        {
            Background = new SolidColorBrush(Color.FromRgb(11, 17, 21)),
            ClipToBounds = false
        };
        _surface.MouseLeftButtonDown += Surface_MouseLeftButtonDown;
        _surface.MouseMove += Surface_MouseMove;
        _surface.MouseLeftButtonUp += Surface_MouseLeftButtonUp;

        _image = new Image
        {
            Stretch = Stretch.Fill,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        _surface.Children.Add(_image);

        _pixelGrid = new Rectangle
        {
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        Panel.SetZIndex(_pixelGrid, 5);
        _surface.Children.Add(_pixelGrid);

        _uvRect = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(34, 83, 216, 222)),
            Stroke = new SolidColorBrush(Color.FromRgb(126, 227, 232)),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Cursor = Cursors.SizeAll
        };
        _uvRect.MouseLeftButtonDown += UvRect_MouseLeftButtonDown;
        _uvRect.MouseMove += UvRect_MouseMove;
        _uvRect.MouseLeftButtonUp += UvRect_MouseLeftButtonUp;
        Panel.SetZIndex(_uvRect, 10);
        _surface.Children.Add(_uvRect);

        _selectionRect = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(26, 255, 201, 91)),
            Stroke = new SolidColorBrush(Color.FromRgb(255, 201, 91)),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 3, 2 },
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        Panel.SetZIndex(_selectionRect, 12);
        _surface.Children.Add(_selectionRect);

        _handles =
        [
            CreateHandle("nw", Cursors.SizeNWSE),
            CreateHandle("ne", Cursors.SizeNESW),
            CreateHandle("se", Cursors.SizeNWSE),
            CreateHandle("sw", Cursors.SizeNESW)
        ];
        foreach (var handle in _handles)
            _surface.Children.Add(handle);

        _scroll = new ScrollViewer
        {
            Content = _surface,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Background,
            CanContentScroll = false
        };
        Children.Add(_scroll);

        Tool = MinecraftTextureEditTool.Uv;
        UpdateInteractionState();
    }

    public event Action<double[]>? UvChanged;
    public event Action<BitmapSource>? TextureChanged;
    public event Action<Color>? ColorPicked;
    public event Action<Int32Rect>? SelectionChanged;

    public MinecraftTextureEditTool Tool { get; private set; }
    public string? TexturePath { get; private set; }
    public bool HasTextureChanges { get; private set; }
    public double UvSnap { get; set; } = 0.25;
    public BitmapSource? CurrentBitmap => _bitmap;
    public Color LastPickedColor => _lastPickedColor;
    public Color PaintColor => _paintColor;
    public Int32Rect Selection => _selectionPixels;

    public void SetTool(MinecraftTextureEditTool tool)
    {
        Tool = tool;
        UpdateInteractionState();
    }

    public void SetPaintColor(Color color) => _paintColor = color;

    public void SetZoom(double zoom)
    {
        _zoom = Math.Clamp(zoom, 0.5, 16);
        UpdateSurfaceSize();
    }

    public void SetUv(double[] uv)
    {
        if (uv.Length < 4)
            return;
        _uv = [uv[0], uv[1], uv[2], uv[3]];
        UpdateUvVisuals();
    }

    public void LoadTexture(string? path, BitmapSource? inMemorySource, double[] uv)
    {
        TexturePath = path;
        HasTextureChanges = inMemorySource is not null;
        _bitmap = null;
        _image.Source = null;
        _selectionPixels = Int32Rect.Empty;

        try
        {
            BitmapSource? source = inMemorySource;
            if (source is null && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                source = bitmap;
            }

            if (source is not null)
            {
                if (source.Format != PixelFormats.Bgra32)
                    source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                _bitmap = new WriteableBitmap(source);
                _image.Source = _bitmap;
                var largest = Math.Max(_bitmap.PixelWidth, _bitmap.PixelHeight);
                _baseScale = largest <= 0 ? 1 : Math.Max(1, 256.0 / largest);
            }
            else
            {
                _baseScale = 1;
            }
        }
        catch (IOException)
        {
            _baseScale = 1;
        }
        catch (NotSupportedException)
        {
            _baseScale = 1;
        }

        SetUv(uv);
        UpdateSurfaceSize();
        UpdateSelectionVisuals();
    }

    public void ResetTextureFromDisk()
    {
        if (string.IsNullOrWhiteSpace(TexturePath))
            return;
        double[] uv = [.. _uv];
        LoadTexture(TexturePath, null, uv);
        HasTextureChanges = false;
    }

    public void SelectAll()
    {
        if (_bitmap is null)
            return;
        _selectionPixels = new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);
        UpdateSelectionVisuals();
        SelectionChanged?.Invoke(_selectionPixels);
    }

    public bool CopySelection()
    {
        if (_bitmap is null || _selectionPixels.IsEmpty)
            return false;
        _selectionClipboard = CropBitmap(_selectionPixels);
        return _selectionClipboard is not null;
    }

    public bool PasteSelection()
    {
        if (_bitmap is null || _selectionClipboard is null)
            return false;
        var x = _selectionPixels.IsEmpty ? 0 : _selectionPixels.X;
        var y = _selectionPixels.IsEmpty ? 0 : _selectionPixels.Y;
        var rect = BlitBitmap(_selectionClipboard, x, y);
        if (rect.IsEmpty)
            return false;
        _selectionPixels = rect;
        UpdateSelectionVisuals();
        NotifyTextureChanged();
        SelectionChanged?.Invoke(_selectionPixels);
        return true;
    }

    public bool FlipSelectionHorizontal()
        => TransformSelection((source, width, height) =>
        {
            var result = new byte[source.Length];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                CopyPixel(source, (y * width + x) * 4, result, (y * width + (width - 1 - x)) * 4);
            return (result, width, height);
        });

    public bool FlipSelectionVertical()
        => TransformSelection((source, width, height) =>
        {
            var result = new byte[source.Length];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                CopyPixel(source, (y * width + x) * 4, result, ((height - 1 - y) * width + x) * 4);
            return (result, width, height);
        });

    public bool RotateSelection90()
        => TransformSelection((source, width, height) =>
        {
            var result = new byte[source.Length];
            var targetWidth = height;
            var targetHeight = width;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var tx = height - 1 - y;
                var ty = x;
                CopyPixel(source, (y * width + x) * 4, result, (ty * targetWidth + tx) * 4);
            }
            return (result, targetWidth, targetHeight);
        });

    public int ReplaceColor(Color source, Color replacement, bool selectionOnly = true)
    {
        if (_bitmap is null)
            return 0;
        var rect = selectionOnly && !_selectionPixels.IsEmpty
            ? _selectionPixels
            : new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);
        var stride = rect.Width * 4;
        var pixels = new byte[stride * rect.Height];
        _bitmap.CopyPixels(rect, pixels, stride, 0);
        var changed = 0;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i] != source.B || pixels[i + 1] != source.G || pixels[i + 2] != source.R || pixels[i + 3] != source.A)
                continue;
            pixels[i] = replacement.B;
            pixels[i + 1] = replacement.G;
            pixels[i + 2] = replacement.R;
            pixels[i + 3] = replacement.A;
            changed++;
        }
        if (changed == 0)
            return 0;
        _bitmap.WritePixels(rect, pixels, stride, 0);
        NotifyTextureChanged();
        return changed;
    }

    public bool SetSelectionAlpha(byte alpha)
    {
        if (_bitmap is null)
            return false;
        var rect = _selectionPixels.IsEmpty
            ? new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight)
            : _selectionPixels;
        var stride = rect.Width * 4;
        var pixels = new byte[stride * rect.Height];
        _bitmap.CopyPixels(rect, pixels, stride, 0);
        for (var i = 3; i < pixels.Length; i += 4)
            pixels[i] = alpha;
        _bitmap.WritePixels(rect, pixels, stride, 0);
        NotifyTextureChanged();
        return true;
    }

    private Thumb CreateHandle(string tag, Cursor cursor)
    {
        var handle = new Thumb
        {
            Width = 10,
            Height = 10,
            Background = new SolidColorBrush(Color.FromRgb(126, 227, 232)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(10, 31, 34)),
            BorderThickness = new Thickness(1),
            Cursor = cursor,
            Tag = tag
        };
        handle.DragDelta += Handle_DragDelta;
        Panel.SetZIndex(handle, 11);
        return handle;
    }

    private void UpdateInteractionState()
    {
        var uvMode = Tool == MinecraftTextureEditTool.Uv;
        _uvRect.IsHitTestVisible = uvMode;
        _uvRect.Visibility = uvMode && _bitmap is not null ? Visibility.Visible : Visibility.Collapsed;
        foreach (var handle in _handles)
        {
            handle.IsHitTestVisible = uvMode;
            handle.Visibility = uvMode && _bitmap is not null ? Visibility.Visible : Visibility.Collapsed;
        }
        _selectionRect.Visibility = Tool == MinecraftTextureEditTool.Selection && !_selectionPixels.IsEmpty
            ? Visibility.Visible
            : Visibility.Collapsed;
        Cursor = Tool switch
        {
            MinecraftTextureEditTool.Pencil => Cursors.Pen,
            MinecraftTextureEditTool.Eraser => Cursors.Cross,
            MinecraftTextureEditTool.Fill => Cursors.Cross,
            MinecraftTextureEditTool.Eyedropper => Cursors.Cross,
            MinecraftTextureEditTool.Line => Cursors.Cross,
            MinecraftTextureEditTool.Rectangle => Cursors.Cross,
            MinecraftTextureEditTool.Selection => Cursors.Cross,
            _ => Cursors.Arrow
        };
    }

    private void UpdateSurfaceSize()
    {
        var width = _bitmap?.PixelWidth ?? 256;
        var height = _bitmap?.PixelHeight ?? 256;
        var scale = _baseScale * _zoom;
        _surface.Width = Math.Max(64, width * scale);
        _surface.Height = Math.Max(64, height * scale);
        _image.Width = _surface.Width;
        _image.Height = _surface.Height;
        Canvas.SetLeft(_image, 0);
        Canvas.SetTop(_image, 0);
        _pixelGrid.Width = _surface.Width;
        _pixelGrid.Height = _surface.Height;
        Canvas.SetLeft(_pixelGrid, 0);
        Canvas.SetTop(_pixelGrid, 0);
        UpdatePixelGrid();
        UpdateUvVisuals();
        UpdateSelectionVisuals();
        UpdateInteractionState();
    }

    private void UpdatePixelGrid()
    {
        if (_bitmap is null)
        {
            _pixelGrid.Visibility = Visibility.Collapsed;
            return;
        }
        var cellWidth = _surface.Width / _bitmap.PixelWidth;
        var cellHeight = _surface.Height / _bitmap.PixelHeight;
        if (Math.Min(cellWidth, cellHeight) < 8)
        {
            _pixelGrid.Visibility = Visibility.Collapsed;
            return;
        }
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(80, 115, 135, 145)), 0.6);
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(null, pen, new LineGeometry(new Point(0, 0), new Point(0, cellHeight))));
        drawing.Children.Add(new GeometryDrawing(null, pen, new LineGeometry(new Point(0, 0), new Point(cellWidth, 0))));
        _pixelGrid.Fill = new DrawingBrush(drawing)
        {
            Viewport = new Rect(0, 0, cellWidth, cellHeight),
            ViewportUnits = BrushMappingMode.Absolute,
            TileMode = TileMode.Tile,
            Stretch = Stretch.None
        };
        _pixelGrid.Visibility = Visibility.Visible;
    }

    private void UpdateUvVisuals()
    {
        if (_surface.Width <= 0 || _surface.Height <= 0)
            return;
        var left = Math.Min(_uv[0], _uv[2]) / 16.0 * _surface.Width;
        var right = Math.Max(_uv[0], _uv[2]) / 16.0 * _surface.Width;
        var top = Math.Min(_uv[1], _uv[3]) / 16.0 * _surface.Height;
        var bottom = Math.Max(_uv[1], _uv[3]) / 16.0 * _surface.Height;

        Canvas.SetLeft(_uvRect, left);
        Canvas.SetTop(_uvRect, top);
        _uvRect.Width = Math.Max(1, right - left);
        _uvRect.Height = Math.Max(1, bottom - top);
        PositionHandle(_handles[0], left, top);
        PositionHandle(_handles[1], right, top);
        PositionHandle(_handles[2], right, bottom);
        PositionHandle(_handles[3], left, bottom);
    }

    private void UpdateSelectionVisuals()
    {
        if (_bitmap is null || _selectionPixels.IsEmpty || _surface.Width <= 0 || _surface.Height <= 0)
        {
            _selectionRect.Visibility = Visibility.Collapsed;
            return;
        }
        var xScale = _surface.Width / _bitmap.PixelWidth;
        var yScale = _surface.Height / _bitmap.PixelHeight;
        Canvas.SetLeft(_selectionRect, _selectionPixels.X * xScale);
        Canvas.SetTop(_selectionRect, _selectionPixels.Y * yScale);
        _selectionRect.Width = Math.Max(1, _selectionPixels.Width * xScale);
        _selectionRect.Height = Math.Max(1, _selectionPixels.Height * yScale);
        _selectionRect.Visibility = Tool == MinecraftTextureEditTool.Selection ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void PositionHandle(FrameworkElement handle, double x, double y)
    {
        Canvas.SetLeft(handle, x - handle.Width / 2);
        Canvas.SetTop(handle, y - handle.Height / 2);
    }

    private void UvRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Tool != MinecraftTextureEditTool.Uv)
            return;
        _movingUv = true;
        _uvMoveStart = e.GetPosition(_surface);
        _uvMoveOrigin = [.. _uv];
        _uvRect.CaptureMouse();
        e.Handled = true;
    }

    private void UvRect_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_movingUv || e.LeftButton != MouseButtonState.Pressed)
            return;
        var current = e.GetPosition(_surface);
        var du = Snap((current.X - _uvMoveStart.X) / _surface.Width * 16.0);
        var dv = Snap((current.Y - _uvMoveStart.Y) / _surface.Height * 16.0);
        du = Math.Clamp(du, -Math.Min(_uvMoveOrigin[0], _uvMoveOrigin[2]), 16 - Math.Max(_uvMoveOrigin[0], _uvMoveOrigin[2]));
        dv = Math.Clamp(dv, -Math.Min(_uvMoveOrigin[1], _uvMoveOrigin[3]), 16 - Math.Max(_uvMoveOrigin[1], _uvMoveOrigin[3]));
        _uv = [_uvMoveOrigin[0] + du, _uvMoveOrigin[1] + dv, _uvMoveOrigin[2] + du, _uvMoveOrigin[3] + dv];
        UpdateUvVisuals();
        RaiseUvChanged();
    }

    private void UvRect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_movingUv)
            return;
        _movingUv = false;
        _uvRect.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void Handle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb handle || handle.Tag is not string tag || _surface.Width <= 0 || _surface.Height <= 0)
            return;
        var du = Snap(e.HorizontalChange / _surface.Width * 16.0);
        var dv = Snap(e.VerticalChange / _surface.Height * 16.0);
        var left = _uv[0] <= _uv[2] ? 0 : 2;
        var right = left == 0 ? 2 : 0;
        var top = _uv[1] <= _uv[3] ? 1 : 3;
        var bottom = top == 1 ? 3 : 1;
        if (tag.Contains('w')) _uv[left] = Math.Clamp(Snap(_uv[left] + du), 0, 16);
        if (tag.Contains('e')) _uv[right] = Math.Clamp(Snap(_uv[right] + du), 0, 16);
        if (tag.Contains('n')) _uv[top] = Math.Clamp(Snap(_uv[top] + dv), 0, 16);
        if (tag.Contains('s')) _uv[bottom] = Math.Clamp(Snap(_uv[bottom] + dv), 0, 16);
        UpdateUvVisuals();
        RaiseUvChanged();
    }

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Tool == MinecraftTextureEditTool.Uv || _bitmap is null)
            return;
        var pixel = SurfaceToPixel(e.GetPosition(_surface));
        _lastPaintPixel = new Point(-1, -1);
        _gestureStartPixel = pixel;

        if (Tool == MinecraftTextureEditTool.Selection)
        {
            var px = (int)pixel.X;
            var py = (int)pixel.Y;
            if (!_selectionPixels.IsEmpty && Contains(_selectionPixels, px, py))
            {
                _movingSelection = true;
                _selectionMoveOrigin = _selectionPixels;
            }
            else
            {
                _selecting = true;
                _selectionPixels = new Int32Rect(px, py, 1, 1);
                UpdateSelectionVisuals();
            }
            _surface.CaptureMouse();
            e.Handled = true;
            return;
        }

        _painting = Tool is MinecraftTextureEditTool.Pencil or MinecraftTextureEditTool.Eraser or MinecraftTextureEditTool.Line or MinecraftTextureEditTool.Rectangle;
        ApplyTextureTool(pixel, immediateOnly: Tool is MinecraftTextureEditTool.Pencil or MinecraftTextureEditTool.Eraser or MinecraftTextureEditTool.Fill or MinecraftTextureEditTool.Eyedropper);
        if (_painting)
            _surface.CaptureMouse();
        e.Handled = true;
    }

    private void Surface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_bitmap is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        var pixel = SurfaceToPixel(e.GetPosition(_surface));
        if (_selecting)
        {
            _selectionPixels = RectFromPoints(_gestureStartPixel, pixel, _bitmap.PixelWidth, _bitmap.PixelHeight);
            UpdateSelectionVisuals();
            return;
        }
        if (_movingSelection)
        {
            var dx = (int)pixel.X - (int)_gestureStartPixel.X;
            var dy = (int)pixel.Y - (int)_gestureStartPixel.Y;
            _selectionPixels = ClampRect(
                new Int32Rect(_selectionMoveOrigin.X + dx, _selectionMoveOrigin.Y + dy, _selectionMoveOrigin.Width, _selectionMoveOrigin.Height),
                _bitmap.PixelWidth,
                _bitmap.PixelHeight,
                preserveSize: true);
            UpdateSelectionVisuals();
            return;
        }
        if (!_painting || Tool is MinecraftTextureEditTool.Line or MinecraftTextureEditTool.Rectangle)
            return;
        ApplyTextureTool(pixel, immediateOnly: true);
        e.Handled = true;
    }

    private void Surface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_bitmap is null)
            return;
        var pixel = SurfaceToPixel(e.GetPosition(_surface));
        if (_selecting)
        {
            _selecting = false;
            _selectionPixels = RectFromPoints(_gestureStartPixel, pixel, _bitmap.PixelWidth, _bitmap.PixelHeight);
            _surface.ReleaseMouseCapture();
            UpdateSelectionVisuals();
            SelectionChanged?.Invoke(_selectionPixels);
            e.Handled = true;
            return;
        }
        if (_movingSelection)
        {
            _movingSelection = false;
            var destination = _selectionPixels;
            _selectionPixels = _selectionMoveOrigin;
            MoveSelectionTo(destination.X, destination.Y);
            _surface.ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
        if (!_painting)
            return;
        if (Tool == MinecraftTextureEditTool.Line)
        {
            DrawLine((int)_gestureStartPixel.X, (int)_gestureStartPixel.Y, (int)pixel.X, (int)pixel.Y, _paintColor);
            NotifyTextureChanged();
        }
        else if (Tool == MinecraftTextureEditTool.Rectangle)
        {
            DrawRectangle(RectFromPoints(_gestureStartPixel, pixel, _bitmap.PixelWidth, _bitmap.PixelHeight), _paintColor);
            NotifyTextureChanged();
        }
        _painting = false;
        _surface.ReleaseMouseCapture();
        e.Handled = true;
    }

    private Point SurfaceToPixel(Point position)
    {
        if (_bitmap is null || _surface.Width <= 0 || _surface.Height <= 0)
            return new Point(0, 0);
        var x = Math.Clamp((int)Math.Floor(position.X / _surface.Width * _bitmap.PixelWidth), 0, _bitmap.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Floor(position.Y / _surface.Height * _bitmap.PixelHeight), 0, _bitmap.PixelHeight - 1);
        return new Point(x, y);
    }

    private void ApplyTextureTool(Point pixel, bool immediateOnly)
    {
        if (_bitmap is null)
            return;
        var x = (int)pixel.X;
        var y = (int)pixel.Y;
        if (_lastPaintPixel.X == x && _lastPaintPixel.Y == y && _painting)
            return;
        _lastPaintPixel = pixel;

        switch (Tool)
        {
            case MinecraftTextureEditTool.Pencil when immediateOnly:
                WritePixel(x, y, _paintColor);
                NotifyTextureChanged();
                break;
            case MinecraftTextureEditTool.Eraser when immediateOnly:
                WritePixel(x, y, Color.FromArgb(0, 0, 0, 0));
                NotifyTextureChanged();
                break;
            case MinecraftTextureEditTool.Fill when immediateOnly:
                FloodFill(x, y, _paintColor);
                NotifyTextureChanged();
                break;
            case MinecraftTextureEditTool.Eyedropper when immediateOnly:
                _lastPickedColor = ReadPixel(x, y);
                ColorPicked?.Invoke(_lastPickedColor);
                break;
        }
    }

    private Color ReadPixel(int x, int y)
    {
        if (_bitmap is null)
            return Colors.Transparent;
        var pixel = new byte[4];
        _bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    private void WritePixel(int x, int y, Color color)
    {
        if (_bitmap is null)
            return;
        var pixel = new[] { color.B, color.G, color.R, color.A };
        _bitmap.WritePixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
    }

    private void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            WritePixel(x0, y0, color);
            if (x0 == x1 && y0 == y1)
                break;
            var twice = 2 * error;
            if (twice >= dy) { error += dy; x0 += sx; }
            if (twice <= dx) { error += dx; y0 += sy; }
        }
    }

    private void DrawRectangle(Int32Rect rect, Color color)
    {
        if (rect.IsEmpty)
            return;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;
        DrawLine(rect.X, rect.Y, right, rect.Y, color);
        DrawLine(rect.X, bottom, right, bottom, color);
        DrawLine(rect.X, rect.Y, rect.X, bottom, color);
        DrawLine(right, rect.Y, right, bottom, color);
    }

    private void FloodFill(int startX, int startY, Color replacement)
    {
        if (_bitmap is null)
            return;
        var width = _bitmap.PixelWidth;
        var height = _bitmap.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        _bitmap.CopyPixels(pixels, stride, 0);
        var start = startY * stride + startX * 4;
        var targetB = pixels[start];
        var targetG = pixels[start + 1];
        var targetR = pixels[start + 2];
        var targetA = pixels[start + 3];
        if (targetB == replacement.B && targetG == replacement.G && targetR == replacement.R && targetA == replacement.A)
            return;

        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (x < 0 || x >= width || y < 0 || y >= height)
                continue;
            var index = y * stride + x * 4;
            if (pixels[index] != targetB || pixels[index + 1] != targetG || pixels[index + 2] != targetR || pixels[index + 3] != targetA)
                continue;
            pixels[index] = replacement.B;
            pixels[index + 1] = replacement.G;
            pixels[index + 2] = replacement.R;
            pixels[index + 3] = replacement.A;
            queue.Enqueue((x - 1, y));
            queue.Enqueue((x + 1, y));
            queue.Enqueue((x, y - 1));
            queue.Enqueue((x, y + 1));
        }
        _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
    }

    private bool MoveSelectionTo(int x, int y)
    {
        if (_bitmap is null || _selectionPixels.IsEmpty)
            return false;
        var sourceRect = _selectionPixels;
        var snapshot = CropBitmap(sourceRect);
        if (snapshot is null)
            return false;
        ClearRegion(sourceRect);
        var destination = BlitBitmap(snapshot, x, y);
        if (destination.IsEmpty)
            return false;
        _selectionPixels = destination;
        UpdateSelectionVisuals();
        NotifyTextureChanged();
        SelectionChanged?.Invoke(_selectionPixels);
        return true;
    }

    private bool TransformSelection(Func<byte[], int, int, (byte[] Pixels, int Width, int Height)> transform)
    {
        if (_bitmap is null || _selectionPixels.IsEmpty)
            return false;
        var sourceRect = _selectionPixels;
        var stride = sourceRect.Width * 4;
        var pixels = new byte[stride * sourceRect.Height];
        _bitmap.CopyPixels(sourceRect, pixels, stride, 0);
        var transformed = transform(pixels, sourceRect.Width, sourceRect.Height);
        ClearRegion(sourceRect);
        var bitmap = BitmapSource.Create(
            transformed.Width,
            transformed.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            transformed.Pixels,
            transformed.Width * 4);
        var destination = BlitBitmap(bitmap, sourceRect.X, sourceRect.Y);
        _selectionPixels = destination;
        UpdateSelectionVisuals();
        NotifyTextureChanged();
        SelectionChanged?.Invoke(_selectionPixels);
        return true;
    }

    private BitmapSource? CropBitmap(Int32Rect rect)
    {
        if (_bitmap is null || rect.IsEmpty)
            return null;
        var clipped = ClampRect(rect, _bitmap.PixelWidth, _bitmap.PixelHeight, preserveSize: false);
        if (clipped.IsEmpty)
            return null;
        var crop = new CroppedBitmap(_bitmap, clipped);
        if (crop.CanFreeze)
            crop.Freeze();
        return crop;
    }

    private Int32Rect BlitBitmap(BitmapSource source, int x, int y)
    {
        if (_bitmap is null)
            return Int32Rect.Empty;
        BitmapSource normalized = source;
        if (normalized.Format != PixelFormats.Bgra32)
            normalized = new FormatConvertedBitmap(normalized, PixelFormats.Bgra32, null, 0);
        var destination = ClampRect(
            new Int32Rect(x, y, normalized.PixelWidth, normalized.PixelHeight),
            _bitmap.PixelWidth,
            _bitmap.PixelHeight,
            preserveSize: false);
        if (destination.IsEmpty)
            return Int32Rect.Empty;
        var sourceX = destination.X - x;
        var sourceY = destination.Y - y;
        var sourceRect = new Int32Rect(sourceX, sourceY, destination.Width, destination.Height);
        var stride = destination.Width * 4;
        var pixels = new byte[stride * destination.Height];
        normalized.CopyPixels(sourceRect, pixels, stride, 0);
        _bitmap.WritePixels(destination, pixels, stride, 0);
        return destination;
    }

    private void ClearRegion(Int32Rect rect)
    {
        if (_bitmap is null || rect.IsEmpty)
            return;
        var clipped = ClampRect(rect, _bitmap.PixelWidth, _bitmap.PixelHeight, preserveSize: false);
        if (clipped.IsEmpty)
            return;
        var stride = clipped.Width * 4;
        _bitmap.WritePixels(clipped, new byte[stride * clipped.Height], stride, 0);
    }

    private static void CopyPixel(byte[] source, int sourceIndex, byte[] target, int targetIndex)
    {
        target[targetIndex] = source[sourceIndex];
        target[targetIndex + 1] = source[sourceIndex + 1];
        target[targetIndex + 2] = source[sourceIndex + 2];
        target[targetIndex + 3] = source[sourceIndex + 3];
    }

    private static bool Contains(Int32Rect rect, int x, int y)
        => !rect.IsEmpty && x >= rect.X && x < rect.X + rect.Width && y >= rect.Y && y < rect.Y + rect.Height;

    private static Int32Rect RectFromPoints(Point a, Point b, int width, int height)
    {
        var x1 = Math.Clamp(Math.Min((int)a.X, (int)b.X), 0, width - 1);
        var y1 = Math.Clamp(Math.Min((int)a.Y, (int)b.Y), 0, height - 1);
        var x2 = Math.Clamp(Math.Max((int)a.X, (int)b.X), 0, width - 1);
        var y2 = Math.Clamp(Math.Max((int)a.Y, (int)b.Y), 0, height - 1);
        return new Int32Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
    }

    private static Int32Rect ClampRect(Int32Rect rect, int width, int height, bool preserveSize)
    {
        if (rect.IsEmpty || width <= 0 || height <= 0)
            return Int32Rect.Empty;
        if (preserveSize)
        {
            var w = Math.Min(rect.Width, width);
            var h = Math.Min(rect.Height, height);
            var x = Math.Clamp(rect.X, 0, width - w);
            var y = Math.Clamp(rect.Y, 0, height - h);
            return new Int32Rect(x, y, w, h);
        }
        var left = Math.Clamp(rect.X, 0, width);
        var top = Math.Clamp(rect.Y, 0, height);
        var right = Math.Clamp(rect.X + rect.Width, 0, width);
        var bottom = Math.Clamp(rect.Y + rect.Height, 0, height);
        return right <= left || bottom <= top
            ? Int32Rect.Empty
            : new Int32Rect(left, top, right - left, bottom - top);
    }

    private void NotifyTextureChanged()
    {
        if (_bitmap is null)
            return;
        HasTextureChanges = true;
        TextureChanged?.Invoke(_bitmap);
    }

    private void RaiseUvChanged() => UvChanged?.Invoke([.. _uv]);

    private double Snap(double value)
        => UvSnap <= 0 ? value : Math.Round(value / UvSnap, MidpointRounding.AwayFromZero) * UvSnap;
}

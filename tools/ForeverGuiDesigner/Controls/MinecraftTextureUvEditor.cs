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
    Eyedropper
}

public sealed class MinecraftTextureUvEditor : Grid
{
    private readonly ScrollViewer _scroll;
    private readonly Canvas _surface;
    private readonly Image _image;
    private readonly Rectangle _uvRect;
    private readonly Thumb[] _handles;
    private WriteableBitmap? _bitmap;
    private double[] _uv = [0, 0, 16, 16];
    private double _zoom = 1;
    private double _baseScale = 1;
    private bool _movingUv;
    private Point _uvMoveStart;
    private double[] _uvMoveOrigin = [0, 0, 16, 16];
    private bool _painting;
    private Point _lastPaintPixel = new(-1, -1);
    private Color _paintColor = Colors.White;

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

    public MinecraftTextureEditTool Tool { get; private set; }
    public string? TexturePath { get; private set; }
    public bool HasTextureChanges { get; private set; }
    public double UvSnap { get; set; } = 0.25;
    public BitmapSource? CurrentBitmap => _bitmap;

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
    }

    public void ResetTextureFromDisk()
    {
        if (string.IsNullOrWhiteSpace(TexturePath))
            return;
        double[] uv = [.. _uv];
        LoadTexture(TexturePath, null, uv);
        HasTextureChanges = false;
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
        foreach (var handle in _handles)
        {
            handle.IsHitTestVisible = uvMode;
            handle.Visibility = uvMode && _bitmap is not null ? Visibility.Visible : Visibility.Collapsed;
        }
        Cursor = Tool switch
        {
            MinecraftTextureEditTool.Pencil => Cursors.Pen,
            MinecraftTextureEditTool.Eraser => Cursors.Cross,
            MinecraftTextureEditTool.Fill => Cursors.Cross,
            MinecraftTextureEditTool.Eyedropper => Cursors.Cross,
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
        UpdateUvVisuals();
        UpdateInteractionState();
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
        _painting = Tool is MinecraftTextureEditTool.Pencil or MinecraftTextureEditTool.Eraser;
        _lastPaintPixel = new Point(-1, -1);
        ApplyTextureTool(e.GetPosition(_surface));
        if (_painting)
            _surface.CaptureMouse();
        e.Handled = true;
    }

    private void Surface_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_painting || e.LeftButton != MouseButtonState.Pressed || _bitmap is null)
            return;
        ApplyTextureTool(e.GetPosition(_surface));
        e.Handled = true;
    }

    private void Surface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_painting)
            return;
        _painting = false;
        _surface.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void ApplyTextureTool(Point position)
    {
        if (_bitmap is null || _surface.Width <= 0 || _surface.Height <= 0)
            return;
        var x = Math.Clamp((int)Math.Floor(position.X / _surface.Width * _bitmap.PixelWidth), 0, _bitmap.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Floor(position.Y / _surface.Height * _bitmap.PixelHeight), 0, _bitmap.PixelHeight - 1);
        if (_lastPaintPixel.X == x && _lastPaintPixel.Y == y && _painting)
            return;
        _lastPaintPixel = new Point(x, y);

        switch (Tool)
        {
            case MinecraftTextureEditTool.Pencil:
                WritePixel(x, y, _paintColor);
                NotifyTextureChanged();
                break;
            case MinecraftTextureEditTool.Eraser:
                WritePixel(x, y, Color.FromArgb(0, 0, 0, 0));
                NotifyTextureChanged();
                break;
            case MinecraftTextureEditTool.Fill:
                FloodFill(x, y, _paintColor);
                NotifyTextureChanged();
                break;
            case MinecraftTextureEditTool.Eyedropper:
                ColorPicked?.Invoke(ReadPixel(x, y));
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

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner.Controls;

public sealed class MinecraftBlockModelViewport : Grid
{
    private const int FaceTextureTargetPixels = 256;
    private const int FaceTextureMaxScale = 16;

    private readonly PerspectiveCamera _camera = new();
    private readonly Model3DGroup _scene = new();
    private readonly Model3DGroup _content = new();
    private readonly Dictionary<string, BitmapSource> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TextureSliceKey, BitmapSource> _faceTextureCache = new();
    private Point _lastMouse;
    private bool _orbiting;
    private double _yaw = -35;
    private double _pitch = 24;
    private double _distance = 34;

    public MinecraftBlockModelViewport()
    {
        Background = new SolidColorBrush(Color.FromRgb(12, 16, 19));
        ClipToBounds = true;
        Focusable = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);

        _camera.FieldOfView = 34;
        UpdateCamera();

        _scene.Children.Add(new AmbientLight(Color.FromRgb(145, 145, 145)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(230, 230, 230), new Vector3D(-1, -1.2, -1)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(110, 125, 135), new Vector3D(1, 0.4, 0.8)));
        _scene.Children.Add(_content);

        var viewport = new Viewport3D
        {
            Camera = _camera,
            ClipToBounds = true
        };
        RenderOptions.SetBitmapScalingMode(viewport, BitmapScalingMode.NearestNeighbor);
        viewport.Children.Add(new ModelVisual3D { Content = _scene });
        Children.Add(viewport);

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        LostMouseCapture += (_, _) => _orbiting = false;
    }

    public int RenderedFaceCount { get; private set; }
    public int MissingTextureCount { get; private set; }

    public void SetModel(
        string repoRoot,
        MinecraftResolvedBlockModel model,
        int blockstateXRotation,
        int blockstateYRotation)
    {
        _content.Children.Clear();
        RenderedFaceCount = 0;
        MissingTextureCount = 0;

        foreach (var element in model.Elements)
        {
            var elementGroup = new Model3DGroup();
            foreach (var (direction, face) in element.Faces)
            {
                var uv = face.Uv ?? GetDefaultUv(direction, element);
                var geometry = CreateFaceGeometry(direction, element, uv, face.Rotation);
                if (geometry is null)
                    continue;

                var material = CreateMaterial(repoRoot, model, face.Texture, uv);
                var geometryModel = new GeometryModel3D(geometry, material)
                {
                    BackMaterial = material
                };
                elementGroup.Children.Add(geometryModel);
                RenderedFaceCount++;
            }

            if (element.Rotation is not null && Math.Abs(element.Rotation.Angle) > 0.0001)
                elementGroup.Transform = CreateElementRotation(element.Rotation);

            _content.Children.Add(elementGroup);
        }

        _content.Transform = CreateBlockstateRotation(blockstateXRotation, blockstateYRotation);
    }

    public void ClearModel()
    {
        _content.Children.Clear();
        RenderedFaceCount = 0;
        MissingTextureCount = 0;
    }

    public void ResetView()
    {
        _yaw = -35;
        _pitch = 24;
        _distance = 34;
        UpdateCamera();
    }

    private Material CreateMaterial(
        string repoRoot,
        MinecraftResolvedBlockModel model,
        string textureReference,
        double[] uv)
    {
        var path = MinecraftBlockModelPreviewService.ResolveTexturePath(repoRoot, model, textureReference);
        if (path is null)
        {
            MissingTextureCount++;
            return new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(184, 74, 184)));
        }

        if (!_textureCache.TryGetValue(path, out var source))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                source = bitmap;
                _textureCache[path] = source;
            }
            catch (IOException)
            {
                MissingTextureCount++;
                return new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(184, 74, 184)));
            }
            catch (NotSupportedException)
            {
                MissingTextureCount++;
                return new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(184, 74, 184)));
            }
        }

        var faceTexture = GetPixelSharpFaceTexture(path, source, uv);
        var brush = new ImageBrush(faceTexture)
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
            Viewbox = new Rect(0, 0, 1, 1)
        };
        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);
        return new DiffuseMaterial(brush);
    }

    private BitmapSource GetPixelSharpFaceTexture(string path, BitmapSource source, double[] uv)
    {
        var u1 = uv.Length > 0 ? uv[0] : 0;
        var v1 = uv.Length > 1 ? uv[1] : 0;
        var u2 = uv.Length > 2 ? uv[2] : 16;
        var v2 = uv.Length > 3 ? uv[3] : 16;
        var uMin = Math.Clamp(Math.Min(u1, u2), 0, 16);
        var vMin = Math.Clamp(Math.Min(v1, v2), 0, 16);
        var uMax = Math.Clamp(Math.Max(u1, u2), 0, 16);
        var vMax = Math.Clamp(Math.Max(v1, v2), 0, 16);
        var key = new TextureSliceKey(path, uMin, vMin, uMax, vMax);
        if (_faceTextureCache.TryGetValue(key, out var cached))
            return cached;

        var left = Math.Clamp((int)Math.Round(uMin / 16.0 * source.PixelWidth), 0, Math.Max(0, source.PixelWidth - 1));
        var top = Math.Clamp((int)Math.Round(vMin / 16.0 * source.PixelHeight), 0, Math.Max(0, source.PixelHeight - 1));
        var right = Math.Clamp((int)Math.Round(uMax / 16.0 * source.PixelWidth), left + 1, source.PixelWidth);
        var bottom = Math.Clamp((int)Math.Round(vMax / 16.0 * source.PixelHeight), top + 1, source.PixelHeight);

        BitmapSource slice = new CroppedBitmap(source, new Int32Rect(left, top, right - left, bottom - top));
        if (slice.CanFreeze)
            slice.Freeze();

        var largestDimension = Math.Max(slice.PixelWidth, slice.PixelHeight);
        var scale = largestDimension <= 0
            ? 1
            : Math.Clamp((int)Math.Ceiling(FaceTextureTargetPixels / (double)largestDimension), 1, FaceTextureMaxScale);
        if (scale > 1)
            slice = UpscaleNearestNeighbor(slice, scale);

        _faceTextureCache[key] = slice;
        return slice;
    }

    private static BitmapSource UpscaleNearestNeighbor(BitmapSource source, int scale)
    {
        BitmapSource normalized = source;
        if (source.Format != PixelFormats.Bgra32)
        {
            normalized = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            if (normalized.CanFreeze)
                normalized.Freeze();
        }

        var sourceWidth = normalized.PixelWidth;
        var sourceHeight = normalized.PixelHeight;
        var sourceStride = sourceWidth * 4;
        var sourcePixels = new byte[sourceStride * sourceHeight];
        normalized.CopyPixels(sourcePixels, sourceStride, 0);

        var targetWidth = checked(sourceWidth * scale);
        var targetHeight = checked(sourceHeight * scale);
        var targetStride = checked(targetWidth * 4);
        var targetPixels = new byte[checked(targetStride * targetHeight)];
        var expandedRow = new byte[targetStride];

        for (var y = 0; y < sourceHeight; y++)
        {
            Array.Clear(expandedRow);
            var sourceRow = y * sourceStride;
            for (var x = 0; x < sourceWidth; x++)
            {
                var sourceIndex = sourceRow + x * 4;
                for (var repeat = 0; repeat < scale; repeat++)
                {
                    var targetIndex = (x * scale + repeat) * 4;
                    expandedRow[targetIndex] = sourcePixels[sourceIndex];
                    expandedRow[targetIndex + 1] = sourcePixels[sourceIndex + 1];
                    expandedRow[targetIndex + 2] = sourcePixels[sourceIndex + 2];
                    expandedRow[targetIndex + 3] = sourcePixels[sourceIndex + 3];
                }
            }

            for (var repeatY = 0; repeatY < scale; repeatY++)
                Buffer.BlockCopy(expandedRow, 0, targetPixels, (y * scale + repeatY) * targetStride, targetStride);
        }

        var result = BitmapSource.Create(
            targetWidth,
            targetHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            targetPixels,
            targetStride);
        result.Freeze();
        return result;
    }

    private static MeshGeometry3D? CreateFaceGeometry(
        string direction,
        MinecraftModelElement element,
        double[] uv,
        int textureRotation)
    {
        var x1 = Math.Min(element.From[0], element.To[0]);
        var y1 = Math.Min(element.From[1], element.To[1]);
        var z1 = Math.Min(element.From[2], element.To[2]);
        var x2 = Math.Max(element.From[0], element.To[0]);
        var y2 = Math.Max(element.From[1], element.To[1]);
        var z2 = Math.Max(element.From[2], element.To[2]);

        Point3D[] vertices = direction.ToLowerInvariant() switch
        {
            "north" =>
            [
                new Point3D(x1, y2, z1), new Point3D(x2, y2, z1),
                new Point3D(x2, y1, z1), new Point3D(x1, y1, z1)
            ],
            "south" =>
            [
                new Point3D(x2, y2, z2), new Point3D(x1, y2, z2),
                new Point3D(x1, y1, z2), new Point3D(x2, y1, z2)
            ],
            "west" =>
            [
                new Point3D(x1, y2, z2), new Point3D(x1, y2, z1),
                new Point3D(x1, y1, z1), new Point3D(x1, y1, z2)
            ],
            "east" =>
            [
                new Point3D(x2, y2, z1), new Point3D(x2, y2, z2),
                new Point3D(x2, y1, z2), new Point3D(x2, y1, z1)
            ],
            "up" =>
            [
                new Point3D(x1, y2, z1), new Point3D(x2, y2, z1),
                new Point3D(x2, y2, z2), new Point3D(x1, y2, z2)
            ],
            "down" =>
            [
                new Point3D(x1, y1, z2), new Point3D(x2, y1, z2),
                new Point3D(x2, y1, z1), new Point3D(x1, y1, z1)
            ],
            _ => []
        };

        if (vertices.Length != 4)
            return null;

        var textureCoordinates = BuildTextureCoordinates(uv, textureRotation, direction);

        var mesh = new MeshGeometry3D();
        foreach (var point in vertices)
            mesh.Positions.Add(point);
        foreach (var point in textureCoordinates)
            mesh.TextureCoordinates.Add(point);
        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(1);
        mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(3);
        return mesh;
    }

    private static Point[] BuildTextureCoordinates(double[] uv, int rotation, string direction)
    {
        var u1 = uv.Length > 0 ? uv[0] : 0;
        var v1 = uv.Length > 1 ? uv[1] : 0;
        var u2 = uv.Length > 2 ? uv[2] : 16;
        var v2 = uv.Length > 3 ? uv[3] : 16;
        var uLeft = u2 >= u1 ? 0.0 : 1.0;
        var uRight = u2 >= u1 ? 1.0 : 0.0;
        var vTop = v2 >= v1 ? 0.0 : 1.0;
        var vBottom = v2 >= v1 ? 1.0 : 0.0;

        // Semantic corners as seen from outside the face: top-left, top-right, bottom-right, bottom-left.
        var semantic = new[]
        {
            new Point(uLeft, vTop),
            new Point(uRight, vTop),
            new Point(uRight, vBottom),
            new Point(uLeft, vBottom)
        };

        var steps = ((rotation % 360) + 360) % 360 / 90;
        if (steps != 0)
        {
            var rotated = new Point[4];
            var downFace = direction.Equals("down", StringComparison.OrdinalIgnoreCase);
            for (var i = 0; i < 4; i++)
            {
                var sourceIndex = downFace
                    ? (i + steps) % 4
                    : (i - steps + 4) % 4;
                rotated[i] = semantic[sourceIndex];
            }
            semantic = rotated;
        }

        return direction.ToLowerInvariant() switch
        {
            // Side geometry is wound top-right -> top-left -> bottom-left -> bottom-right.
            "north" or "south" or "west" or "east" =>
            [semantic[1], semantic[0], semantic[3], semantic[2]],
            _ => semantic
        };
    }

    private static double[] GetDefaultUv(string direction, MinecraftModelElement element)
    {
        var from = element.From;
        var to = element.To;
        var x1 = Math.Min(from[0], to[0]);
        var y1 = Math.Min(from[1], to[1]);
        var z1 = Math.Min(from[2], to[2]);
        var x2 = Math.Max(from[0], to[0]);
        var y2 = Math.Max(from[1], to[1]);
        var z2 = Math.Max(from[2], to[2]);

        return direction.ToLowerInvariant() switch
        {
            "down" => [x1, 16 - z2, x2, 16 - z1],
            "up" => [x1, z1, x2, z2],
            "north" => [16 - x2, 16 - y2, 16 - x1, 16 - y1],
            "south" => [x1, 16 - y2, x2, 16 - y1],
            "west" => [z1, 16 - y2, z2, 16 - y1],
            "east" => [16 - z2, 16 - y2, 16 - z1, 16 - y1],
            _ => [0, 0, 16, 16]
        };
    }

    private static Transform3D CreateElementRotation(MinecraftModelElementRotation rotation)
    {
        var axis = rotation.Axis.ToLowerInvariant() switch
        {
            "x" => new Vector3D(1, 0, 0),
            "z" => new Vector3D(0, 0, 1),
            _ => new Vector3D(0, 1, 0)
        };
        var origin = new Point3D(rotation.Origin[0], rotation.Origin[1], rotation.Origin[2]);
        return new RotateTransform3D(new AxisAngleRotation3D(axis, rotation.Angle), origin);
    }

    private static Transform3D CreateBlockstateRotation(int xRotation, int yRotation)
    {
        if (xRotation == 0 && yRotation == 0)
            return Transform3D.Identity;

        var center = new Point3D(8, 8, 8);
        var group = new Transform3DGroup();
        if (xRotation != 0)
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), xRotation), center));
        if (yRotation != 0)
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -yRotation), center));
        return group;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            ResetView();
            e.Handled = true;
            return;
        }

        _lastMouse = e.GetPosition(this);
        _orbiting = true;
        CaptureMouse();
        Cursor = Cursors.Hand;
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_orbiting)
            return;
        _orbiting = false;
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_orbiting || e.LeftButton != MouseButtonState.Pressed)
            return;

        var current = e.GetPosition(this);
        var delta = current - _lastMouse;
        _lastMouse = current;
        _yaw += delta.X * 0.45;
        _pitch = Math.Clamp(_pitch - delta.Y * 0.45, -82, 82);
        UpdateCamera();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _distance *= e.Delta > 0 ? 0.88 : 1.14;
        _distance = Math.Clamp(_distance, 12, 90);
        UpdateCamera();
        e.Handled = true;
    }

    private void UpdateCamera()
    {
        var center = new Point3D(8, 8, 8);
        var yaw = _yaw * Math.PI / 180.0;
        var pitch = _pitch * Math.PI / 180.0;
        var horizontal = _distance * Math.Cos(pitch);
        var position = new Point3D(
            center.X + horizontal * Math.Sin(yaw),
            center.Y + _distance * Math.Sin(pitch),
            center.Z + horizontal * Math.Cos(yaw));

        _camera.Position = position;
        _camera.LookDirection = center - position;
        _camera.UpDirection = new Vector3D(0, 1, 0);
        _camera.NearPlaneDistance = 0.1;
        _camera.FarPlaneDistance = 500;
    }

    private readonly record struct TextureSliceKey(
        string Path,
        double UMin,
        double VMin,
        double UMax,
        double VMax);
}

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner.Controls;

public enum MinecraftViewportEditMode
{
    None,
    Move,
    Scale
}

public sealed class MinecraftBlockModelViewport : Grid
{
    private const int FaceTextureTargetPixels = 256;
    private const int FaceTextureMaxScale = 16;
    private const double OrbitDragThreshold = 3.0;

    private readonly PerspectiveCamera _camera = new();
    private readonly Viewport3D _viewport;
    private readonly Model3DGroup _scene = new();
    private readonly Model3DGroup _content = new();
    private readonly Dictionary<string, BitmapSource> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TextureSliceKey, BitmapSource> _faceTextureCache = new();
    private readonly Dictionary<Model3D, FaceHitInfo> _faceHitMap = new();
    private readonly Dictionary<string, GeometryModel3D> _faceModels = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, BitmapSource> _textureOverrides = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
    private MinecraftResolvedBlockModel? _currentModel;
    private Point _lastMouse;
    private Point _leftDownPoint;
    private bool _orbiting;
    private bool _leftDragging;
    private bool _panning;
    private double _yaw = -35;
    private double _pitch = 24;
    private double _distance = 34;
    private Point3D _target = new(8, 8, 8);
    private int _selectedElementIndex = -1;
    private string? _selectedFaceDirection;
    private int _blockstateXRotation;
    private int _blockstateYRotation;
    private MinecraftItemDisplayTransform? _displayTransform;

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
        _scene.Children.Add(BuildGroundGrid());
        _scene.Children.Add(_content);

        _viewport = new Viewport3D
        {
            Camera = _camera,
            ClipToBounds = true
        };
        RenderOptions.SetBitmapScalingMode(_viewport, BitmapScalingMode.NearestNeighbor);
        _viewport.Children.Add(new ModelVisual3D { Content = _scene });
        Children.Add(_viewport);

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseRightButtonUp += OnMouseRightButtonUp;
        MouseDown += OnAuxiliaryMouseDown;
        MouseUp += OnAuxiliaryMouseUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        LostMouseCapture += (_, _) =>
        {
            _orbiting = false;
            _leftDragging = false;
            _panning = false;
            Cursor = Cursors.Arrow;
        };
    }

    public event Action<int>? ElementSelected;
    public event Action<int, string>? FaceSelected;
    public event Action<int, string, double, MinecraftViewportEditMode>? ElementDragDelta;

    public int RenderedFaceCount { get; private set; }
    public int MissingTextureCount { get; private set; }
    public MinecraftViewportEditMode EditMode { get; set; }
    public string EditAxis { get; set; } = "x";
    public int SelectedElementIndex => _selectedElementIndex;
    public string? SelectedFaceDirection => _selectedFaceDirection;

    public void SetModel(
        string repoRoot,
        MinecraftResolvedBlockModel model,
        int blockstateXRotation,
        int blockstateYRotation)
    {
        _currentModel = model;
        _content.Children.Clear();
        _faceHitMap.Clear();
        _faceModels.Clear();
        RenderedFaceCount = 0;
        MissingTextureCount = 0;
        _blockstateXRotation = blockstateXRotation;
        _blockstateYRotation = blockstateYRotation;

        for (var elementIndex = 0; elementIndex < model.Elements.Count; elementIndex++)
        {
            var element = model.Elements[elementIndex];
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
                _faceHitMap[geometryModel] = new FaceHitInfo(elementIndex, direction);
                _faceModels[FaceModelKey(elementIndex, direction)] = geometryModel;
                elementGroup.Children.Add(geometryModel);
                RenderedFaceCount++;
            }

            if (element.Rotation is not null && Math.Abs(element.Rotation.Angle) > 0.0001)
                elementGroup.Transform = CreateElementRotation(element.Rotation);

            _content.Children.Add(elementGroup);
        }

        if (_selectedElementIndex >= model.Elements.Count)
        {
            _selectedElementIndex = -1;
            _selectedFaceDirection = null;
        }

        UpdateContentTransform();
    }

    /// <summary>
    /// High-frequency animation path. Reuses the existing WPF 3D scene, materials and texture slices
    /// and only replaces geometry/element transforms. A topology change returns false so the caller
    /// can perform one full SetModel rebuild, after which following frames stay incremental again.
    /// </summary>
    public bool TryUpdateAnimatedModel(
        string repoRoot,
        MinecraftResolvedBlockModel model,
        int blockstateXRotation,
        int blockstateYRotation)
    {
        if (_currentModel is null ||
            _currentModel.Elements.Count != model.Elements.Count ||
            _content.Children.Count != model.Elements.Count)
            return false;

        // Validate topology before touching the live scene. This prevents half-updated frames when a
        // conditional runtime branch changes its element/face composition.
        for (var elementIndex = 0; elementIndex < model.Elements.Count; elementIndex++)
        {
            if (_content.Children[elementIndex] is not Model3DGroup group ||
                group.Children.Count != model.Elements[elementIndex].Faces.Count)
                return false;

            foreach (var direction in model.Elements[elementIndex].Faces.Keys)
            {
                if (!_faceModels.ContainsKey(FaceModelKey(elementIndex, direction)))
                    return false;
            }
        }

        for (var elementIndex = 0; elementIndex < model.Elements.Count; elementIndex++)
        {
            var element = model.Elements[elementIndex];
            var previousElement = _currentModel.Elements[elementIndex];
            var elementGroup = (Model3DGroup)_content.Children[elementIndex];

            foreach (var (direction, face) in element.Faces)
            {
                var geometryModel = _faceModels[FaceModelKey(elementIndex, direction)];
                var uv = face.Uv ?? GetDefaultUv(direction, element);
                var geometry = CreateFaceGeometry(direction, element, uv, face.Rotation);
                if (geometry is null)
                    return false;

                geometryModel.Geometry = geometry;

                // Runtime box dimensions alter UV extents every frame. Re-cropping those atlas slices
                // would recreate bitmaps/materials continuously and was the main source of texture
                // jitter. Keep the existing material while the texture slot stays the same; refresh it
                // only when a real source texture/material changes (for example a status light).
                if (!previousElement.Faces.TryGetValue(direction, out var previousFace) ||
                    !string.Equals(previousFace.Texture, face.Texture, StringComparison.OrdinalIgnoreCase))
                {
                    var material = CreateMaterial(repoRoot, model, face.Texture, uv);
                    geometryModel.Material = material;
                    geometryModel.BackMaterial = material;
                }
            }

            elementGroup.Transform = element.Rotation is not null && Math.Abs(element.Rotation.Angle) > 0.0001
                ? CreateElementRotation(element.Rotation)
                : Transform3D.Identity;
        }

        var contentTransformChanged = _blockstateXRotation != blockstateXRotation ||
                                      _blockstateYRotation != blockstateYRotation;
        _blockstateXRotation = blockstateXRotation;
        _blockstateYRotation = blockstateYRotation;
        _currentModel = model;
        if (contentTransformChanged)
            UpdateContentTransform();
        return true;
    }

    public void SetTextureOverrides(IReadOnlyDictionary<string, BitmapSource>? overrides)
    {
        _textureOverrides = overrides ?? new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        _faceTextureCache.Clear();
    }

    public void SetDisplayTransform(MinecraftItemDisplayTransform? transform)
    {
        _displayTransform = transform is null
            ? null
            : new MinecraftItemDisplayTransform
            {
                Rotation = [.. transform.Rotation],
                Translation = [.. transform.Translation],
                Scale = [.. transform.Scale]
            };
        UpdateContentTransform();
    }

    public void SelectElement(int index)
    {
        _selectedElementIndex = index;
        if (index < 0)
            _selectedFaceDirection = null;
    }

    public void ClearModel()
    {
        _currentModel = null;
        _content.Children.Clear();
        _faceHitMap.Clear();
        _faceModels.Clear();
        _selectedElementIndex = -1;
        _selectedFaceDirection = null;
        RenderedFaceCount = 0;
        MissingTextureCount = 0;
    }

    public void ResetView()
    {
        _yaw = -35;
        _pitch = 24;
        _distance = 34;
        _target = new Point3D(8, 8, 8);
        UpdateCamera();
    }

    private static string FaceModelKey(int elementIndex, string direction)
        => $"{elementIndex}:{direction}";

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
            return MissingTextureMaterial();
        }

        BitmapSource? source = null;
        if (_textureOverrides.TryGetValue(path, out var inMemory))
        {
            source = inMemory;
        }
        else if (!_textureCache.TryGetValue(path, out source))
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
                return MissingTextureMaterial();
            }
            catch (NotSupportedException)
            {
                MissingTextureCount++;
                return MissingTextureMaterial();
            }
        }

        if (source is null)
        {
            MissingTextureCount++;
            return MissingTextureMaterial();
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

    private static Material MissingTextureMaterial()
        => new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(184, 74, 184)));

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
        var sourceStride = checked(sourceWidth * 4);
        var sourcePixels = new byte[checked(sourceStride * sourceHeight)];
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

    private void UpdateContentTransform()
    {
        var group = new Transform3DGroup();
        if (_blockstateXRotation != 0 || _blockstateYRotation != 0)
            group.Children.Add(CreateBlockstateRotation(_blockstateXRotation, _blockstateYRotation));

        if (_displayTransform is { } display)
        {
            var rotation = display.Rotation;
            var translation = display.Translation;
            var scale = display.Scale;
            var sx = SafeScale(scale.ElementAtOrDefault(0));
            var sy = SafeScale(scale.ElementAtOrDefault(1));
            var sz = SafeScale(scale.ElementAtOrDefault(2));

            group.Children.Add(new TranslateTransform3D(-8, -8, -8));
            group.Children.Add(new ScaleTransform3D(sx, sy, sz));
            if (Math.Abs(rotation.ElementAtOrDefault(0)) > 0.0001)
                group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), rotation[0])));
            if (Math.Abs(rotation.ElementAtOrDefault(1)) > 0.0001)
                group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -rotation[1])));
            if (Math.Abs(rotation.ElementAtOrDefault(2)) > 0.0001)
                group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), rotation[2])));
            group.Children.Add(new TranslateTransform3D(
                8 + translation.ElementAtOrDefault(0),
                8 + translation.ElementAtOrDefault(1),
                8 + translation.ElementAtOrDefault(2)));
        }

        _content.Transform = group.Children.Count == 0 ? Transform3D.Identity : group;
    }

    private static double SafeScale(double value)
    {
        var clamped = Math.Clamp(value, -8, 8);
        return Math.Abs(clamped) < 0.0001 ? 0.0001 : clamped;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        _leftDownPoint = e.GetPosition(this);
        _lastMouse = _leftDownPoint;
        _leftDragging = false;
        _orbiting = true;
        CaptureMouse();
        Cursor = Cursors.Hand;
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_orbiting)
            return;

        var point = e.GetPosition(this);
        var wasClick = !_leftDragging && (point - _leftDownPoint).Length <= OrbitDragThreshold;
        _orbiting = false;
        _leftDragging = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        Cursor = Cursors.Arrow;

        if (wasClick && TryPickFace(point, out var hit))
            SelectHitFace(hit);

        e.Handled = true;
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(this);
        FaceHitInfo? hit = TryPickFace(point, out var picked) ? picked : null;
        var menu = BuildViewportContextMenu(hit);
        menu.PlacementTarget = this;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void OnAuxiliaryMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;
        _lastMouse = e.GetPosition(this);
        _panning = true;
        CaptureMouse();
        Cursor = Cursors.ScrollAll;
        e.Handled = true;
    }

    private void OnAuxiliaryMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_panning)
            return;
        _panning = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var current = e.GetPosition(this);
        var delta = current - _lastMouse;

        if (_panning && e.MiddleButton == MouseButtonState.Pressed)
        {
            _lastMouse = current;
            PanCamera(delta);
            return;
        }

        if (!_orbiting || e.LeftButton != MouseButtonState.Pressed)
            return;

        if (!_leftDragging)
        {
            if ((current - _leftDownPoint).Length <= OrbitDragThreshold)
                return;
            _leftDragging = true;
        }

        _lastMouse = current;
        _yaw += delta.X * 0.45;
        _pitch = Math.Clamp(_pitch - delta.Y * 0.45, -82, 82);
        UpdateCamera();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _distance *= e.Delta > 0 ? 0.88 : 1.14;
        _distance = Math.Clamp(_distance, 8, 120);
        UpdateCamera();
        e.Handled = true;
    }

    private void PanCamera(Vector delta)
    {
        var look = _camera.LookDirection;
        if (look.LengthSquared < 0.0001)
            return;
        look.Normalize();
        var right = Vector3D.CrossProduct(look, _camera.UpDirection);
        if (right.LengthSquared < 0.0001)
            return;
        right.Normalize();
        var up = Vector3D.CrossProduct(right, look);
        up.Normalize();
        var factor = _distance * 0.0025;
        var offset = (-delta.X * factor * right) + (delta.Y * factor * up);
        _target += offset;
        UpdateCamera();
    }

    private bool TryPickFace(Point point, out FaceHitInfo hit)
    {
        hit = default;
        var viewportPoint = TranslatePoint(point, _viewport);
        var result = VisualTreeHelper.HitTest(_viewport, viewportPoint);
        if (result is not RayMeshGeometry3DHitTestResult mesh || mesh.ModelHit is null)
            return false;
        return _faceHitMap.TryGetValue(mesh.ModelHit, out hit);
    }

    private void SelectHitFace(FaceHitInfo hit)
    {
        _selectedElementIndex = hit.ElementIndex;
        _selectedFaceDirection = hit.Direction;
        ElementSelected?.Invoke(hit.ElementIndex);
        FaceSelected?.Invoke(hit.ElementIndex, hit.Direction);
    }

    private ContextMenu BuildViewportContextMenu(FaceHitInfo? hit)
    {
        var menu = new ContextMenu();
        if (hit is { } face)
        {
            menu.Items.Add(new MenuItem
            {
                Header = $"Element {face.ElementIndex + 1} · {face.Direction.ToUpperInvariant()} face",
                IsEnabled = false
            });
            menu.Items.Add(MenuItem("Select this face", () => SelectHitFace(face)));
        }
        else
        {
            menu.Items.Add(new MenuItem { Header = "Viewport", IsEnabled = false });
        }

        menu.Items.Add(MenuItem("Frame selected element", FrameSelectedElement, _selectedElementIndex >= 0));
        menu.Items.Add(new Separator());

        var transformIndex = hit?.ElementIndex ?? _selectedElementIndex;
        var move = new MenuItem { Header = "Move selected" };
        AddTransformNudges(move, transformIndex, MinecraftViewportEditMode.Move);
        move.IsEnabled = transformIndex >= 0;
        menu.Items.Add(move);

        var scale = new MenuItem { Header = "Scale selected" };
        AddTransformNudges(scale, transformIndex, MinecraftViewportEditMode.Scale);
        scale.IsEnabled = transformIndex >= 0;
        menu.Items.Add(scale);

        menu.Items.Add(new Separator());
        var view = new MenuItem { Header = "View" };
        view.Items.Add(MenuItem("Isometric", () => SetViewPreset(-35, 24)));
        view.Items.Add(MenuItem("Front", () => SetViewPreset(0, 0)));
        view.Items.Add(MenuItem("Back", () => SetViewPreset(180, 0)));
        view.Items.Add(MenuItem("Left", () => SetViewPreset(-90, 0)));
        view.Items.Add(MenuItem("Right", () => SetViewPreset(90, 0)));
        view.Items.Add(MenuItem("Top", () => SetViewPreset(0, 82)));
        view.Items.Add(MenuItem("Bottom", () => SetViewPreset(0, -82)));
        menu.Items.Add(view);
        menu.Items.Add(MenuItem("Reset view", ResetView));
        return menu;
    }

    private void AddTransformNudges(MenuItem parent, int elementIndex, MinecraftViewportEditMode mode)
    {
        foreach (var axis in new[] { "x", "y", "z" })
        {
            var axisMenu = new MenuItem { Header = axis.ToUpperInvariant() };
            axisMenu.Items.Add(MenuItem("− 0.25", () => EmitTransformNudge(elementIndex, axis, -0.25, mode)));
            axisMenu.Items.Add(MenuItem("+ 0.25", () => EmitTransformNudge(elementIndex, axis, 0.25, mode)));
            axisMenu.Items.Add(MenuItem("− 1", () => EmitTransformNudge(elementIndex, axis, -1, mode)));
            axisMenu.Items.Add(MenuItem("+ 1", () => EmitTransformNudge(elementIndex, axis, 1, mode)));
            parent.Items.Add(axisMenu);
        }
    }

    private void EmitTransformNudge(int elementIndex, string axis, double delta, MinecraftViewportEditMode mode)
    {
        if (elementIndex < 0)
            return;
        _selectedElementIndex = elementIndex;
        ElementSelected?.Invoke(elementIndex);
        ElementDragDelta?.Invoke(elementIndex, axis, delta, mode);
    }

    private void FrameSelectedElement()
    {
        if (_currentModel is null || _selectedElementIndex < 0 || _selectedElementIndex >= _currentModel.Elements.Count)
            return;
        var element = _currentModel.Elements[_selectedElementIndex];
        var minX = Math.Min(element.From[0], element.To[0]);
        var minY = Math.Min(element.From[1], element.To[1]);
        var minZ = Math.Min(element.From[2], element.To[2]);
        var maxX = Math.Max(element.From[0], element.To[0]);
        var maxY = Math.Max(element.From[1], element.To[1]);
        var maxZ = Math.Max(element.From[2], element.To[2]);
        _target = new Point3D((minX + maxX) / 2.0, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);
        var largest = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
        _distance = Math.Clamp(Math.Max(10, largest * 2.7), 8, 80);
        UpdateCamera();
    }

    private void SetViewPreset(double yaw, double pitch)
    {
        _yaw = yaw;
        _pitch = pitch;
        UpdateCamera();
    }

    private static MenuItem MenuItem(string header, Action action, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        item.Click += (_, _) => action();
        return item;
    }

    private void UpdateCamera()
    {
        var yaw = _yaw * Math.PI / 180.0;
        var pitch = _pitch * Math.PI / 180.0;
        var horizontal = _distance * Math.Cos(pitch);
        var position = new Point3D(
            _target.X + horizontal * Math.Sin(yaw),
            _target.Y + _distance * Math.Sin(pitch),
            _target.Z + horizontal * Math.Cos(yaw));

        _camera.Position = position;
        _camera.LookDirection = _target - position;
        _camera.UpDirection = new Vector3D(0, 1, 0);
        _camera.NearPlaneDistance = 0.1;
        _camera.FarPlaneDistance = 500;
    }

    private static Model3D BuildGroundGrid()
    {
        var group = new Model3DGroup();
        var materialGroup = new MaterialGroup();
        materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(32, 42, 47))));
        materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(20, 28, 32))));
        for (var i = -16; i <= 32; i++)
        {
            group.Children.Add(CreateGridStrip(i, -16, i, 32, materialGroup));
            group.Children.Add(CreateGridStrip(-16, i, 32, i, materialGroup));
        }
        return group;
    }

    private static GeometryModel3D CreateGridStrip(double x1, double z1, double x2, double z2, Material material)
    {
        const double half = 0.012;
        const double y = -0.035;
        var dx = x2 - x1;
        var dz = z2 - z1;
        var length = Math.Sqrt(dx * dx + dz * dz);
        var nx = length <= 0 ? 0 : -dz / length * half;
        var nz = length <= 0 ? 0 : dx / length * half;
        var mesh = new MeshGeometry3D();
        mesh.Positions.Add(new Point3D(x1 + nx, y, z1 + nz));
        mesh.Positions.Add(new Point3D(x2 + nx, y, z2 + nz));
        mesh.Positions.Add(new Point3D(x2 - nx, y, z2 - nz));
        mesh.Positions.Add(new Point3D(x1 - nx, y, z1 - nz));
        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(1);
        mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(3);
        return new GeometryModel3D(mesh, material) { BackMaterial = material };
    }

    private readonly record struct FaceHitInfo(int ElementIndex, string Direction);

    private readonly record struct TextureSliceKey(
        string Path,
        double UMin,
        double VMin,
        double UMax,
        double VMax);
}

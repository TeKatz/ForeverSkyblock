using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Runtime safety bridge for Phase 13/14 texture editing. The editable WriteableBitmap must never
/// be handed directly to the 3D renderer because WPF Freezable graphs may freeze the source while
/// materials/cropped bitmaps are prepared. The renderer therefore receives detached frozen snapshots.
/// </summary>
public partial class MainWindow
{
    private bool _studioTextureCanvasStabilityInitialized;
    private bool _studioItemTextureSnapshotHooked;

    public void EnableForeverTextureCanvasStability()
    {
        if (_studioTextureCanvasStabilityInitialized || _studioTextureUvEditor is null)
            return;

        _studioTextureCanvasStabilityInitialized = true;

        _studioTextureUvEditor.TextureChanged += bitmap =>
        {
            var path = _studioTextureUvEditor.TexturePath;
            if (string.IsNullOrWhiteSpace(path))
                return;

            _studioTextureOverrides[path] = CreateStudioTextureRenderSnapshot(bitmap);
        };

        _ = Dispatcher.BeginInvoke(() =>
        {
            HookStudioItemTextureSnapshotBridge();
            StabilizeStudioTextureButtons();
        }, DispatcherPriority.Loaded);
    }

    private void HookStudioItemTextureSnapshotBridge()
    {
        if (_studioItemTextureSnapshotHooked || _studioItemTextureEditor is null)
            return;

        _studioItemTextureSnapshotHooked = true;
        _studioItemTextureEditor.TextureChanged += bitmap =>
        {
            var path = _studioItemTextureEditor.TexturePath;
            if (string.IsNullOrWhiteSpace(path))
                return;

            _studioItemTextureOverrides[path] = CreateStudioTextureRenderSnapshot(bitmap);
        };
    }

    private void StabilizeStudioTextureButtons()
    {
        if (_studioTextureUvEditor?.Parent is not DependencyObject root)
            return;

        foreach (var button in FindStudioVisualChildren<Button>(root))
        {
            var label = button.Content?.ToString();
            var textureTool = label is "UV" or "Pencil" or "Eraser" or "Fill" or "Picker" or "Line" or "Rect" or "Select";
            var paletteButton = button.ToolTip?.ToString()?.Contains(" · #", StringComparison.Ordinal) == true;
            if (!textureTool && !paletteButton)
                continue;

            button.Focusable = false;
            button.IsTabStop = false;
        }
    }

    private static BitmapSource CreateStudioTextureRenderSnapshot(BitmapSource source)
    {
        BitmapSource normalized = source;
        if (normalized.Format != PixelFormats.Bgra32)
            normalized = new FormatConvertedBitmap(normalized, PixelFormats.Bgra32, null, 0);

        var width = Math.Max(1, normalized.PixelWidth);
        var height = Math.Max(1, normalized.PixelHeight);
        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        normalized.CopyPixels(pixels, stride, 0);

        var snapshot = BitmapSource.Create(
            width,
            height,
            normalized.DpiX > 0 ? normalized.DpiX : 96,
            normalized.DpiY > 0 ? normalized.DpiY : 96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        snapshot.Freeze();
        return snapshot;
    }
}

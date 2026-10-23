using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ForeverGuiDesigner.Models;

public sealed class AssetItemModel
{
    private ImageSource? _previewSource;
    private bool _previewLoaded;

    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }

    // Binding a raw Windows path directly to Image.Source is surprisingly theme/runtime dependent.
    // Decode the file ourselves and detach it from the file handle so thumbnails always render.
    public ImageSource? PreviewSource
    {
        get
        {
            if (_previewLoaded)
                return _previewSource;

            _previewLoaded = true;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(FullPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                _previewSource = bitmap;
            }
            catch
            {
                _previewSource = null;
            }
            return _previewSource;
        }
    }

    public override string ToString() => RelativePath;
}

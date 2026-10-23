using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ForeverGuiDesigner.Services;

internal static class ForeverMachinePreviewRenderer
{
    private sealed record PanelSpec(int X, int Y, int Width, int Height, Color Edge);

    public static string? CreatePreview(
        string repoRoot,
        string screenClass,
        string source,
        int width,
        int height,
        int inventoryY,
        int hotbarY)
    {
        try
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "ForeverGuiDesigner", "machine-previews");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, Sanitize(screenClass) + ".png");

            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);

            using (var dc = visual.RenderOpen())
            {
                var accent = ResolveAccent(screenClass, source);
                DrawFrame(dc, width, height, accent);

                foreach (var panel in ParsePanels(source))
                    DrawPanel(dc, panel.X, panel.Y, panel.Width, panel.Height, panel.Edge);

                // Shared MachineScreen inventory panel. This is rendered for every current Forever machine screen.
                DrawPanel(dc, 18, inventoryY - 6, 212, hotbarY - inventoryY + 28, FromArgb(0xFF405866));
                Fill(dc, 24, hotbarY - 6, width - 48, 1, FromArgb(0xFF344C5B));

                DrawMachineTextureHint(dc, repoRoot, screenClass);
            }

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(outputPath);
            encoder.Save(stream);
            return outputPath;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<PanelSpec> ParsePanels(string source)
    {
        var regex = new Regex(
            @"MachineSkin\.panel\s*\(\s*g\s*,\s*x\s*\+\s*(?<x>\d+)\s*,\s*y\s*\+\s*(?<y>\d+)\s*,\s*(?<w>\d+)\s*,\s*(?<h>\d+)\s*,\s*(?<color>0x[0-9A-Fa-f]{8})\s*\)",
            RegexOptions.Multiline);

        // A screen can contain alternate branches (for example Condenser garden vs normal mode).
        // Keeping the last panel for identical geometry favors the normal fall-through presentation.
        var byGeometry = new Dictionary<string, PanelSpec>(StringComparer.Ordinal);
        foreach (Match match in regex.Matches(source))
        {
            var panel = new PanelSpec(
                int.Parse(match.Groups["x"].Value),
                int.Parse(match.Groups["y"].Value),
                int.Parse(match.Groups["w"].Value),
                int.Parse(match.Groups["h"].Value),
                ParseArgb(match.Groups["color"].Value));
            byGeometry[$"{panel.X}:{panel.Y}:{panel.Width}:{panel.Height}"] = panel;
        }
        return byGeometry.Values;
    }

    private static Color ResolveAccent(string screenClass, string source)
    {
        if (screenClass.Contains("Ascension", StringComparison.OrdinalIgnoreCase))
            return FromArgb(0xFFBCA4F3);

        var match = Regex.Match(source, @"skin\.accent\s*\(\s*MachineSkin\.(?<name>[A-Z_]+)\s*\)");
        return match.Groups["name"].Value switch
        {
            "COPPER" => FromArgb(0xFFE4B47C),
            "GREEN" => FromArgb(0xFF9BD4A6),
            "ICE" => FromArgb(0xFF9DDDF0),
            "VIOLET" => FromArgb(0xFFBCA4F3),
            _ => FromArgb(0xFF76D8D2)
        };
    }

    private static void DrawFrame(DrawingContext dc, int width, int height, Color accent)
    {
        Fill(dc, 3, 4, width, height, FromArgb(0x66050910));
        Fill(dc, 3, 0, width - 6, height, FromArgb(0xFF0C151F));
        Fill(dc, 0, 3, width, height - 6, FromArgb(0xFF0C151F));
        Fill(dc, 1, 4, width - 2, height - 8, FromArgb(0xFF425764));
        Fill(dc, 4, 1, width - 8, height - 2, FromArgb(0xFF425764));

        var body = new LinearGradientBrush(FromArgb(0xFF253746), FromArgb(0xFF15232F), 90);
        body.Freeze();
        dc.DrawRectangle(body, null, new Rect(3, 4, Math.Max(1, width - 6), Math.Max(1, height - 8)));

        Fill(dc, 8, 7, width - 16, 20, FromArgb(0xFF101D29));
        Fill(dc, 14, 27, width - 28, 1, FromArgb(0xFF526979));
        Fill(dc, 14, 27, Math.Max(1, Math.Min(52, width - 28)), 1, accent);
        Fill(dc, 10, 33, 1, Math.Max(1, height - 45), FromArgb(0xFF344A58));
        Fill(dc, width - 11, 33, 1, Math.Max(1, height - 45), FromArgb(0xFF344A58));

        foreach (var dx in new[] { 5, width - 7 })
        foreach (var dy in new[] { 5, height - 7 })
        {
            Fill(dc, dx, dy, 2, 2, FromArgb(0xFF0C1923));
            Fill(dc, dx, dy, 1, 1, FromArgb(0xFF91A5AE));
        }

        Fill(dc, width / 2 - 14, height - 4, 28, 1, accent);
    }

    private static void DrawPanel(DrawingContext dc, int x, int y, int width, int height, Color edge)
    {
        if (width < 3 || height < 3)
            return;

        Fill(dc, x, y, width, height, FromArgb(0xFF0A151F));
        Fill(dc, x + 1, y + 1, width - 2, height - 2, edge);

        var inner = new LinearGradientBrush(FromArgb(0xFF1B2C39), FromArgb(0xFF12212D), 90);
        inner.Freeze();
        dc.DrawRectangle(inner, null, new Rect(x + 2, y + 2, Math.Max(1, width - 4), Math.Max(1, height - 4)));
        Fill(dc, x + 3, y + 1, Math.Max(1, width - 6), 1, FromArgb(0xFF637884));
    }

    private static void DrawMachineTextureHint(DrawingContext dc, string repoRoot, string screenClass)
    {
        var assetRoot = Path.Combine(repoRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock", "textures");
        if (!Directory.Exists(assetRoot))
            return;

        var path = FindBestMachineTexture(assetRoot, screenClass);
        if (path is null)
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            dc.DrawImage(bitmap, new Rect(18, 10, 16, 16));
        }
        catch
        {
            // The rest of the generated machine preview remains useful even if one texture cannot be decoded.
        }
    }

    private static string? FindBestMachineTexture(string textureRoot, string screenClass)
    {
        var baseName = Regex.Replace(screenClass.Replace("Screen", string.Empty, StringComparison.OrdinalIgnoreCase), "([a-z0-9])([A-Z])", "$1_$2")
            .ToLowerInvariant();
        baseName = baseName.Replace("_chamber", string.Empty, StringComparison.OrdinalIgnoreCase);
        var firstToken = baseName.Split('_', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? baseName;

        var candidates = new List<(string Path, int Score)>();
        foreach (var category in new[] { "item", "block" })
        {
            var root = Path.Combine(textureRoot, category);
            if (!Directory.Exists(root))
                continue;

            foreach (var path in Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories))
            {
                var stem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                var score = 0;
                if (stem.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)) score += 120;
                if (stem.Contains(baseName, StringComparison.OrdinalIgnoreCase)) score += 80;
                if (stem.StartsWith(firstToken, StringComparison.OrdinalIgnoreCase)) score += 55;
                if (stem.Contains(firstToken, StringComparison.OrdinalIgnoreCase)) score += 30;
                if (stem.EndsWith("_front", StringComparison.OrdinalIgnoreCase)) score += 18;
                else if (stem.EndsWith("_panel", StringComparison.OrdinalIgnoreCase)) score += 16;
                else if (stem.EndsWith("_top", StringComparison.OrdinalIgnoreCase)) score += 14;
                else if (stem.EndsWith("_body", StringComparison.OrdinalIgnoreCase)) score += 12;
                else if (stem.EndsWith("_base", StringComparison.OrdinalIgnoreCase)) score += 10;
                if (category == "item") score += 3;
                if (score > 0) candidates.Add((path, score));
            }
        }

        return candidates.OrderByDescending(item => item.Score)
            .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Path)
            .FirstOrDefault();
    }

    private static void Fill(DrawingContext dc, double x, double y, double width, double height, Color color)
    {
        if (width <= 0 || height <= 0)
            return;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        dc.DrawRectangle(brush, null, new Rect(x, y, width, height));
    }

    private static Color ParseArgb(string value)
        => FromArgb(Convert.ToUInt32(value[2..], 16));

    private static Color FromArgb(uint value)
        => Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);

    private static string Sanitize(string value)
        => new(value.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray());
}

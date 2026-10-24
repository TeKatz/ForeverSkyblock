using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ForeverGuiDesigner.Controls;

/// <summary>
/// Deterministic nearest-neighbour bitmap text used by the WYSIWYG canvas.
/// It mirrors Minecraft's one-pixel shadow/alignment/scale behaviour and uses
/// a compact 5x7 fallback glyph set when the vanilla font atlas is not present
/// in the project assets.
/// </summary>
internal sealed class MinecraftPixelText : FrameworkElement
{
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = BuildGlyphs();
    private string _text;
    private readonly Brush _foreground;
    private readonly bool _shadow;
    private readonly double _scale;
    private readonly string _alignment;

    public bool IsOverflowing { get; private set; }

    /// <summary>
    /// Mutable text content for source-owned machine previews. Updating this property
    /// invalidates only this bitmap glyph surface; callers do not need to replace the
    /// Border.Child and therefore do not disturb WPF layout or source geometry.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_text, next, StringComparison.Ordinal))
                return;
            _text = next;
            InvalidateVisual();
        }
    }

    public MinecraftPixelText(string text, Brush foreground, bool shadow, double scale, string alignment)
    {
        _text = text ?? string.Empty;
        _foreground = foreground;
        _shadow = shadow;
        _scale = Math.Clamp(scale, .25, 4);
        _alignment = alignment?.ToLowerInvariant() ?? "left";
        SnapsToDevicePixels = true;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var pixel = Math.Max(.5, _scale);
        var advance = 6 * pixel;
        var width = Math.Max(0, _text.Length * advance - pixel);
        IsOverflowing = width > ActualWidth + .01;
        var x = _alignment switch
        {
            "center" => Math.Max(0, (ActualWidth - width) / 2),
            "right" => Math.Max(0, ActualWidth - width),
            _ => 0
        };
        var y = Math.Max(0, (ActualHeight - 7 * pixel) / 2);

        if (_shadow)
            Draw(dc, x + pixel, y + pixel, pixel, new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)));
        Draw(dc, x, y, pixel, _foreground);
    }

    private void Draw(DrawingContext dc, double x, double y, double pixel, Brush brush)
    {
        var cursor = x;
        foreach (var raw in _text)
        {
            var c = char.ToUpperInvariant(raw);
            if (c == ' ')
            {
                cursor += 4 * pixel;
                continue;
            }

            if (!Glyphs.TryGetValue(c, out var rows))
                rows = Glyphs['?'];

            for (var row = 0; row < rows.Length; row++)
            for (var col = 0; col < rows[row].Length; col++)
                if (rows[row][col] == '1')
                    dc.DrawRectangle(brush, null, new Rect(cursor + col * pixel, y + row * pixel, pixel, pixel));
            cursor += 6 * pixel;
        }
    }

    private static IReadOnlyDictionary<char, string[]> BuildGlyphs()
    {
        static string[] G(params string[] rows) => rows;
        return new Dictionary<char, string[]>
        {
            ['A']=G("01110","10001","10001","11111","10001","10001","10001"),
            ['B']=G("11110","10001","10001","11110","10001","10001","11110"),
            ['C']=G("01111","10000","10000","10000","10000","10000","01111"),
            ['D']=G("11110","10001","10001","10001","10001","10001","11110"),
            ['E']=G("11111","10000","10000","11110","10000","10000","11111"),
            ['F']=G("11111","10000","10000","11110","10000","10000","10000"),
            ['G']=G("01111","10000","10000","10111","10001","10001","01111"),
            ['H']=G("10001","10001","10001","11111","10001","10001","10001"),
            ['I']=G("11111","00100","00100","00100","00100","00100","11111"),
            ['J']=G("00111","00010","00010","00010","10010","10010","01100"),
            ['K']=G("10001","10010","10100","11000","10100","10010","10001"),
            ['L']=G("10000","10000","10000","10000","10000","10000","11111"),
            ['M']=G("10001","11011","10101","10101","10001","10001","10001"),
            ['N']=G("10001","11001","10101","10011","10001","10001","10001"),
            ['O']=G("01110","10001","10001","10001","10001","10001","01110"),
            ['P']=G("11110","10001","10001","11110","10000","10000","10000"),
            ['Q']=G("01110","10001","10001","10001","10101","10010","01101"),
            ['R']=G("11110","10001","10001","11110","10100","10010","10001"),
            ['S']=G("01111","10000","10000","01110","00001","00001","11110"),
            ['T']=G("11111","00100","00100","00100","00100","00100","00100"),
            ['U']=G("10001","10001","10001","10001","10001","10001","01110"),
            ['V']=G("10001","10001","10001","10001","10001","01010","00100"),
            ['W']=G("10001","10001","10001","10101","10101","10101","01010"),
            ['X']=G("10001","10001","01010","00100","01010","10001","10001"),
            ['Y']=G("10001","10001","01010","00100","00100","00100","00100"),
            ['Z']=G("11111","00001","00010","00100","01000","10000","11111"),
            ['0']=G("01110","10001","10011","10101","11001","10001","01110"),
            ['1']=G("00100","01100","00100","00100","00100","00100","01110"),
            ['2']=G("01110","10001","00001","00010","00100","01000","11111"),
            ['3']=G("11110","00001","00001","01110","00001","00001","11110"),
            ['4']=G("00010","00110","01010","10010","11111","00010","00010"),
            ['5']=G("11111","10000","10000","11110","00001","00001","11110"),
            ['6']=G("01110","10000","10000","11110","10001","10001","01110"),
            ['7']=G("11111","00001","00010","00100","01000","01000","01000"),
            ['8']=G("01110","10001","10001","01110","10001","10001","01110"),
            ['9']=G("01110","10001","10001","01111","00001","00001","01110"),
            ['.']=G("0","0","0","0","0","1","1"), [',']=G("0","0","0","0","0","1","1"),
            [':']=G("0","1","1","0","1","1","0"), ['-']=G("00000","00000","00000","11111","00000","00000","00000"),
            ['+']=G("00000","00100","00100","11111","00100","00100","00000"), ['/']=G("00001","00010","00010","00100","01000","01000","10000"),
            ['%']=G("11001","11010","00100","01000","10110","10011","00000"), ['→']=G("00000","00100","00010","11111","00010","00100","00000"),
            ['?']=G("01110","10001","00001","00010","00100","00000","00100")
        };
    }
}

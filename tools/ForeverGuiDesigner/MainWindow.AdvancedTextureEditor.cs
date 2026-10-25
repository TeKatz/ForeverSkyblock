using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 13 advanced pixel workflow. This owns the additional texture tools and the live in-memory
/// texture bridge into the block-model viewport; persistence remains deferred to Phase 16.
/// </summary>
public partial class MainWindow
{
    private bool _studioAdvancedTextureEditorInitialized;
    private TextBox? _studioTextureReplaceSourceBox;
    private TextBox? _studioTextureAlphaBox;

    public void EnableForeverAdvancedTextureEditor()
    {
        if (_studioAdvancedTextureEditorInitialized || _studioTextureUvEditor is null || _studioTextureUvEditor.Parent is not Panel inspector)
            return;
        _studioAdvancedTextureEditorInitialized = true;

        if (_studioTextureZoomCombo is not null)
        {
            var current = _studioTextureZoomCombo.SelectedItem?.ToString() ?? "1×";
            _studioTextureZoomCombo.ItemsSource = new[] { "1×", "2×", "4×", "8×", "16×" };
            _studioTextureZoomCombo.SelectedItem = current;
        }

        _studioTextureUvEditor.TextureChanged += _ => RefreshStudioLiveTexturePreview();
        foreach (var reset in FindStudioVisualChildren<Button>(inspector)
                     .Where(button => string.Equals(button.Content?.ToString(), "Reset texture", StringComparison.OrdinalIgnoreCase)))
        {
            reset.Click += (_, _) => Dispatcher.BeginInvoke(RefreshStudioLiveTexturePreview, DispatcherPriority.Background);
        }

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("ADVANCED PIXEL TOOLS"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Line/rectangle drawing, selection movement, clipboard-style transforms, color replacement and alpha editing. Texture edits feed the 3D preview immediately.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 12)
        });

        var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        AddTextureToolButton(tools, _studioTextureUvEditor, "Line", MinecraftTextureEditTool.Line, 58);
        AddTextureToolButton(tools, _studioTextureUvEditor, "Rect", MinecraftTextureEditTool.Rectangle, 58);
        AddTextureToolButton(tools, _studioTextureUvEditor, "Select", MinecraftTextureEditTool.Selection, 64);
        inspector.Children.Add(tools);
        inspector.Children.Add(BuildStudioTextureSelectionCommands(_studioTextureUvEditor));

        _studioTextureReplaceSourceBox = BuildStudioColorBox("#FFFFFFFF");
        _studioTextureAlphaBox = StudioModelNumberBox();
        _studioTextureAlphaBox.Text = "255";
        inspector.Children.Add(BuildStudioReplaceAlphaPanel(
            _studioTextureUvEditor,
            _studioTextureReplaceSourceBox,
            _studioTextureAlphaBox,
            () => _studioTextureColorBox?.Text ?? "#FFFFFFFF",
            SetStudioEditorStatus));
        inspector.Children.Add(BuildStudioMachinePalette(_studioTextureUvEditor, _studioTextureColorBox));
    }

    private void RefreshStudioLiveTexturePreview()
    {
        if (_studioBlockViewport is null)
            return;
        _studioBlockViewport.SetTextureOverrides(_studioTextureOverrides);
        RenderStudioEditableBlockModel();
    }

    private static void AddTextureToolButton(Panel row, MinecraftTextureUvEditor editor, string label, MinecraftTextureEditTool tool, double width)
    {
        var button = StudioButton(label, width);
        button.Height = 30;
        button.Margin = new Thickness(0, 0, 6, 5);
        button.FontSize = 10;
        button.Click += (_, _) => editor.SetTool(tool);
        row.Children.Add(button);
    }

    private FrameworkElement BuildStudioTextureSelectionCommands(MinecraftTextureUvEditor editor)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        AddTextureCommand(row, "All", 46, editor.SelectAll);
        AddTextureCommand(row, "Copy", 54, () => editor.CopySelection());
        AddTextureCommand(row, "Paste", 54, () => editor.PasteSelection());
        AddTextureCommand(row, "Flip H", 58, () => editor.FlipSelectionHorizontal());
        AddTextureCommand(row, "Flip V", 58, () => editor.FlipSelectionVertical());
        AddTextureCommand(row, "Rotate 90°", 78, () => editor.RotateSelection90());
        return row;
    }

    private static void AddTextureCommand(Panel row, string label, double width, Action action)
    {
        var button = StudioButton(label, width);
        button.Height = 28;
        button.Margin = new Thickness(0, 0, 5, 4);
        button.FontSize = 10;
        button.Click += (_, _) => action();
        row.Children.Add(button);
    }

    private FrameworkElement BuildStudioReplaceAlphaPanel(
        MinecraftTextureUvEditor editor,
        TextBox sourceBox,
        TextBox alphaBox,
        Func<string> replacementText,
        Action<string, string> status)
    {
        var root = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        var replaceGrid = new Grid();
        replaceGrid.ColumnDefinitions.Add(new ColumnDefinition());
        replaceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var sourcePanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        sourcePanel.Children.Add(new TextBlock { Text = "Replace color", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        sourcePanel.Children.Add(sourceBox);
        replaceGrid.Children.Add(sourcePanel);
        var replace = StudioButton("Replace", 75);
        replace.VerticalAlignment = VerticalAlignment.Bottom;
        replace.Click += (_, _) =>
        {
            if (!TryParseStudioTextureColor(sourceBox.Text, out var source) ||
                !TryParseStudioTextureColor(replacementText(), out var replacement))
            {
                status("Invalid replace color. Use #RRGGBB or #AARRGGBB.", "#E18B8B");
                return;
            }
            var count = editor.ReplaceColor(source, replacement, selectionOnly: !editor.Selection.IsEmpty);
            status($"Replaced {count} matching pixel(s) in memory.", "#74C99A");
        };
        Grid.SetColumn(replace, 1);
        replaceGrid.Children.Add(replace);
        root.Children.Add(replaceGrid);

        var alphaGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        alphaGrid.ColumnDefinitions.Add(new ColumnDefinition());
        alphaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var alphaPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        alphaPanel.Children.Add(new TextBlock { Text = "Alpha (0-255)", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        alphaPanel.Children.Add(alphaBox);
        alphaGrid.Children.Add(alphaPanel);
        var applyAlpha = StudioButton("Apply alpha", 90);
        applyAlpha.VerticalAlignment = VerticalAlignment.Bottom;
        applyAlpha.Click += (_, _) =>
        {
            if (!byte.TryParse(alphaBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var alpha))
            {
                status("Alpha must be between 0 and 255.", "#E18B8B");
                return;
            }
            editor.SetSelectionAlpha(alpha);
            status("Alpha updated in memory.", "#74C99A");
        };
        Grid.SetColumn(applyAlpha, 1);
        alphaGrid.Children.Add(applyAlpha);
        root.Children.Add(alphaGrid);
        return root;
    }

    private static TextBox BuildStudioColorBox(string value)
    {
        var box = StudioModelNumberBox();
        box.Text = value;
        return box;
    }

    private FrameworkElement BuildStudioMachinePalette(MinecraftTextureUvEditor editor, TextBox? paintBox)
    {
        var root = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        root.Children.Add(new TextBlock
        {
            Text = "Forever Machine Palette",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 5)
        });
        var row = new WrapPanel();
        foreach (var (name, hex) in StudioMachinePalette)
        {
            if (!TryParseStudioTextureColor(hex, out var color))
                continue;
            var button = new Button
            {
                Width = 25,
                Height = 25,
                Margin = new Thickness(0, 0, 5, 5),
                Background = new SolidColorBrush(color),
                BorderBrush = Brush("#53636B"),
                BorderThickness = new Thickness(1),
                ToolTip = $"{name} · {hex}",
                Cursor = Cursors.Hand
            };
            button.Click += (_, _) =>
            {
                editor.SetPaintColor(color);
                if (paintBox is not null)
                    paintBox.Text = hex;
            };
            row.Children.Add(button);
        }
        root.Children.Add(row);
        return root;
    }

    private static readonly (string Name, string Hex)[] StudioMachinePalette =
    [
        ("Graphite", "#FF20282D"),
        ("Steel Dark", "#FF39464D"),
        ("Steel Mid", "#FF60717A"),
        ("Steel Highlight", "#FF9AAAB2"),
        ("Copper", "#FFA96845"),
        ("Copper Highlight", "#FFD28B62"),
        ("Status Green", "#FF54C47A"),
        ("Warning Orange", "#FFE8A44A"),
        ("Freezing Cyan", "#FF62D7E6"),
        ("Ascension Purple", "#FF9D76E8")
    ];
}

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// UX/QoL consolidation for the Phase 11-14 Studio tools. This intentionally stays in-memory and
/// does not cross the Phase 16 persistence boundary.
/// </summary>
public partial class MainWindow
{
    private const int StudioTextureHistoryLimit = 64;

    private bool _studioUxQolInitialized;
    private readonly Dictionary<MinecraftTextureUvEditor, TextureEditorUxState> _studioTextureUxStates = [];

    public void EnableForeverStudioUxQoL()
    {
        if (_studioUxQolInitialized)
            return;
        _studioUxQolInitialized = true;

        if (_studioTextureUvEditor?.Parent is Panel blockRoot)
            ConfigureStudioTextureEditorUx(_studioTextureUvEditor, blockRoot, isItemEditor: false);
        if (_studioItemTextureEditor?.Parent is Panel itemRoot)
            ConfigureStudioTextureEditorUx(_studioItemTextureEditor, itemRoot, isItemEditor: true);

        HookStudioViewportUx();
        PreviewKeyDown += StudioUxQol_PreviewKeyDown;
    }

    private void ConfigureStudioTextureEditorUx(MinecraftTextureUvEditor editor, Panel root, bool isItemEditor)
    {
        if (_studioTextureUxStates.ContainsKey(editor))
            return;

        var state = new TextureEditorUxState(editor, root, isItemEditor)
        {
            Scroll = FindStudioVisualChildren<ScrollViewer>(editor).FirstOrDefault(),
            Surface = FindStudioVisualChildren<Canvas>(editor).FirstOrDefault(),
            LastTexturePath = editor.TexturePath
        };
        _studioTextureUxStates[editor] = state;

        var toolButtons = FindStudioVisualChildren<Button>(root)
            .Where(button => TryMapTextureTool(button.Content?.ToString(), out _))
            .ToList();
        foreach (var button in toolButtons)
        {
            if (!TryMapTextureTool(button.Content?.ToString(), out var tool))
                continue;
            state.ToolButtons[tool] = button;
            button.Focusable = false;
            button.IsTabStop = false;
            button.ToolTip = $"{TextureToolDisplayName(tool)} ({TextureToolShortcut(tool)})";
            button.Click += (_, _) =>
            {
                editor.SetTool(tool);
                UpdateStudioTextureUxState(state);
            };
        }

        if (isItemEditor && !state.ToolButtons.ContainsKey(MinecraftTextureEditTool.Uv))
            editor.SetTool(MinecraftTextureEditTool.Pencil);

        state.PaletteButtons.AddRange(FindStudioVisualChildren<Button>(root)
            .Where(IsStudioPaletteButton));
        foreach (var palette in state.PaletteButtons)
        {
            palette.Focusable = false;
            palette.IsTabStop = false;
            palette.Click += (_, _) => Dispatcher.BeginInvoke(
                () => UpdateStudioTextureUxState(state),
                DispatcherPriority.Background);
        }

        if (!isItemEditor && _studioTextureColorBox is not null)
        {
            _studioTextureColorBox.LostFocus += (_, _) => Dispatcher.BeginInvoke(
                () => UpdateStudioTextureUxState(state),
                DispatcherPriority.Background);
            _studioTextureColorBox.PreviewKeyUp += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    Dispatcher.BeginInvoke(() => UpdateStudioTextureUxState(state), DispatcherPriority.Background);
            };
        }

        editor.ColorPicked += color =>
        {
            editor.SetPaintColor(color);
            UpdateStudioTextureUxState(state);
        };
        editor.SelectionChanged += _ => UpdateStudioTextureUxState(state);
        editor.TextureChanged += bitmap => StudioTextureUx_TextureChanged(state, bitmap);
        editor.IsVisibleChanged += (_, _) => Dispatcher.BeginInvoke(
            () => ResetStudioTextureHistoryIfAssetChanged(state),
            DispatcherPriority.Background);

        editor.PreviewMouseDown += (_, e) => StudioTextureUx_PreviewMouseDown(state, e);
        editor.PreviewMouseMove += (_, e) => StudioTextureUx_PreviewMouseMove(state, e);
        editor.PreviewMouseUp += (_, e) => StudioTextureUx_PreviewMouseUp(state, e);
        editor.PreviewMouseWheel += (_, e) => StudioTextureUx_PreviewMouseWheel(state, e);
        editor.LostMouseCapture += (_, _) => state.Panning = false;

        if (!isItemEditor && _studioTextureZoomCombo is not null)
        {
            var current = _studioTextureZoomCombo.SelectedItem?.ToString() ?? "1×";
            _studioTextureZoomCombo.ItemsSource = new[] { "Fit", "1×", "2×", "4×", "8×", "16×" };
            _studioTextureZoomCombo.SelectedItem = current;
            _studioTextureZoomCombo.SelectionChanged += (_, _) =>
            {
                var value = _studioTextureZoomCombo.SelectedItem?.ToString();
                if (string.Equals(value, "Fit", StringComparison.OrdinalIgnoreCase))
                {
                    FitStudioTextureEditor(state);
                    return;
                }
                var text = value?.TrimEnd('×');
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom))
                {
                    state.Zoom = Math.Clamp(zoom, 0.5, 16);
                    editor.SetZoom(state.Zoom);
                    UpdateStudioTextureUxState(state);
                }
            };
        }

        foreach (var button in FindStudioVisualChildren<Button>(root))
        {
            var label = button.Content?.ToString() ?? string.Empty;
            if (label is "Paste" or "Flip H" or "Flip V" or "Rotate 90°" or "Replace" or "Apply alpha")
            {
                button.PreviewMouseLeftButtonDown += (_, _) => ArmStudioTextureMutation(state);
            }
            else if (label is "Reset texture" or "Reset item texture")
            {
                button.PreviewMouseLeftButtonDown += (_, _) =>
                {
                    var snapshot = CaptureStudioTextureSnapshot(editor);
                    if (snapshot is not null)
                        PushStudioTextureUndo(state, snapshot, clearRedo: true);
                };
            }

            if (label == "Copy")
            {
                state.CopyButton = button;
                button.Click += (_, _) =>
                {
                    state.HasClipboard = !editor.Selection.IsEmpty;
                    UpdateStudioTextureUxState(state);
                };
            }
            else if (label == "Paste") state.PasteButton = button;
            else if (label == "Flip H") state.FlipHButton = button;
            else if (label == "Flip V") state.FlipVButton = button;
            else if (label == "Rotate 90°") state.RotateButton = button;
        }

        if (!isItemEditor && _studioFaceCombo is not null)
            _studioFaceCombo.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
                () => ResetStudioTextureHistoryIfAssetChanged(state),
                DispatcherPriority.Background);
        if (isItemEditor && _studioItemCombo is not null)
            _studioItemCombo.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
                () => ResetStudioTextureHistoryIfAssetChanged(state),
                DispatcherPriority.Background);

        BuildStudioTextureUxBar(state);
        UpdateStudioTextureUxState(state);
    }

    private void BuildStudioTextureUxBar(TextureEditorUxState state)
    {
        var root = new Border
        {
            Background = Brush("#0D1519"),
            BorderBrush = Brush("#30424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(9),
            Margin = new Thickness(0, 10, 0, 4)
        };
        var stack = new StackPanel();
        var row = new WrapPanel();

        state.UndoButton = StudioButton("Undo", 58);
        state.UndoButton.Height = 28;
        state.UndoButton.Margin = new Thickness(0, 0, 5, 5);
        state.UndoButton.ToolTip = "Undo texture edit (Ctrl+Z)";
        state.UndoButton.Click += (_, _) => UndoStudioTexture(state);
        row.Children.Add(state.UndoButton);

        state.RedoButton = StudioButton("Redo", 58);
        state.RedoButton.Height = 28;
        state.RedoButton.Margin = new Thickness(0, 0, 5, 5);
        state.RedoButton.ToolTip = "Redo texture edit (Ctrl+Y)";
        state.RedoButton.Click += (_, _) => RedoStudioTexture(state);
        row.Children.Add(state.RedoButton);

        state.DeleteButton = StudioButton("Delete", 62);
        state.DeleteButton.Height = 28;
        state.DeleteButton.Margin = new Thickness(0, 0, 5, 5);
        state.DeleteButton.ToolTip = "Clear selected pixels (Delete)";
        state.DeleteButton.Click += (_, _) => DeleteStudioTextureSelection(state);
        row.Children.Add(state.DeleteButton);

        var zoomOut = StudioButton("−", 34);
        zoomOut.Height = 28;
        zoomOut.Margin = new Thickness(6, 0, 5, 5);
        zoomOut.ToolTip = "Zoom out";
        zoomOut.Click += (_, _) => SetStudioTextureZoom(state, state.Zoom / 1.25);
        row.Children.Add(zoomOut);

        var fit = StudioButton("Fit", 44);
        fit.Height = 28;
        fit.Margin = new Thickness(0, 0, 5, 5);
        fit.ToolTip = "Fit texture canvas to the available area";
        fit.Click += (_, _) => FitStudioTextureEditor(state);
        row.Children.Add(fit);

        var zoomIn = StudioButton("+", 34);
        zoomIn.Height = 28;
        zoomIn.Margin = new Thickness(0, 0, 8, 5);
        zoomIn.ToolTip = "Zoom in";
        zoomIn.Click += (_, _) => SetStudioTextureZoom(state, state.Zoom * 1.25);
        row.Children.Add(zoomIn);

        state.ColorSwatch = new Border
        {
            Width = 30,
            Height = 28,
            Margin = new Thickness(2, 0, 7, 5),
            BorderBrush = Brush("#879AA3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            ToolTip = "Current paint color"
        };
        row.Children.Add(state.ColorSwatch);

        state.ColorText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#B8C6CC"),
            FontSize = 10,
            Margin = new Thickness(0, 0, 0, 5)
        };
        row.Children.Add(state.ColorText);
        stack.Children.Add(row);

        state.StatusText = new TextBlock
        {
            Foreground = Brush("#83969F"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(state.StatusText);
        root.Child = stack;
        state.Root.Children.Add(root);
    }

    private void StudioTextureUx_PreviewMouseDown(TextureEditorUxState state, MouseButtonEventArgs e)
    {
        if (state.Scroll is not null &&
            (e.ChangedButton == MouseButton.Middle ||
             (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space))))
        {
            state.Panning = true;
            state.PanStart = e.GetPosition(state.Editor);
            state.PanHorizontalOffset = state.Scroll.HorizontalOffset;
            state.PanVerticalOffset = state.Scroll.VerticalOffset;
            state.Editor.CaptureMouse();
            state.Editor.Cursor = Cursors.ScrollAll;
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left &&
            state.Editor.Tool is not MinecraftTextureEditTool.Uv and not MinecraftTextureEditTool.Eyedropper)
        {
            ArmStudioTextureMutation(state);
        }
    }

    private void StudioTextureUx_PreviewMouseMove(TextureEditorUxState state, MouseEventArgs e)
    {
        if (state.Panning && state.Scroll is not null)
        {
            var current = e.GetPosition(state.Editor);
            var delta = current - state.PanStart;
            state.Scroll.ScrollToHorizontalOffset(Math.Max(0, state.PanHorizontalOffset - delta.X));
            state.Scroll.ScrollToVerticalOffset(Math.Max(0, state.PanVerticalOffset - delta.Y));
            e.Handled = true;
            return;
        }

        UpdateStudioTexturePointer(state, e);
    }

    private void StudioTextureUx_PreviewMouseUp(TextureEditorUxState state, MouseButtonEventArgs e)
    {
        if (state.Panning && e.ChangedButton is MouseButton.Middle or MouseButton.Left)
        {
            state.Panning = false;
            if (state.Editor.IsMouseCaptured)
                state.Editor.ReleaseMouseCapture();
            UpdateStudioTextureUxState(state);
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            Dispatcher.BeginInvoke(() =>
            {
                state.PendingMutation = null;
                state.PendingMutationCommitted = false;
            }, DispatcherPriority.Background);
        }
    }

    private void StudioTextureUx_PreviewMouseWheel(TextureEditorUxState state, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        var oldZoom = state.Zoom;
        var nextZoom = Math.Clamp(oldZoom * (e.Delta > 0 ? 1.25 : 0.8), 0.5, 16);
        if (Math.Abs(nextZoom - oldZoom) < 0.0001)
            return;

        var anchor = state.Scroll is null ? new Point() : e.GetPosition(state.Scroll);
        var oldHorizontal = state.Scroll?.HorizontalOffset ?? 0;
        var oldVertical = state.Scroll?.VerticalOffset ?? 0;
        SetStudioTextureZoom(state, nextZoom);

        if (state.Scroll is not null)
        {
            var ratio = nextZoom / oldZoom;
            Dispatcher.BeginInvoke(() =>
            {
                state.Scroll.ScrollToHorizontalOffset(Math.Max(0, (oldHorizontal + anchor.X) * ratio - anchor.X));
                state.Scroll.ScrollToVerticalOffset(Math.Max(0, (oldVertical + anchor.Y) * ratio - anchor.Y));
            }, DispatcherPriority.Background);
        }
        e.Handled = true;
    }

    private void UpdateStudioTexturePointer(TextureEditorUxState state, MouseEventArgs e)
    {
        var bitmap = state.Editor.CurrentBitmap;
        var surface = state.Surface;
        if (bitmap is null || surface is null || surface.ActualWidth <= 0 || surface.ActualHeight <= 0)
        {
            state.PointerX = -1;
            state.PointerY = -1;
            UpdateStudioTextureUxState(state);
            return;
        }

        var point = e.GetPosition(surface);
        if (point.X < 0 || point.Y < 0 || point.X >= surface.ActualWidth || point.Y >= surface.ActualHeight)
        {
            state.PointerX = -1;
            state.PointerY = -1;
        }
        else
        {
            state.PointerX = Math.Clamp((int)Math.Floor(point.X / surface.ActualWidth * bitmap.PixelWidth), 0, bitmap.PixelWidth - 1);
            state.PointerY = Math.Clamp((int)Math.Floor(point.Y / surface.ActualHeight * bitmap.PixelHeight), 0, bitmap.PixelHeight - 1);
        }
        UpdateStudioTextureUxState(state);
    }

    private void ArmStudioTextureMutation(TextureEditorUxState state)
    {
        if (state.PendingMutation is not null)
            return;
        state.PendingMutation = CaptureStudioTextureSnapshot(state.Editor);
        state.PendingMutationCommitted = false;
    }

    private void StudioTextureUx_TextureChanged(TextureEditorUxState state, BitmapSource bitmap)
    {
        if (state.PendingMutation is not null && !state.PendingMutationCommitted)
        {
            PushStudioTextureUndo(state, state.PendingMutation, clearRedo: true);
            state.PendingMutationCommitted = true;
        }
        UpdateStudioTextureUxState(state);
    }

    private static TextureSnapshot? CaptureStudioTextureSnapshot(MinecraftTextureUvEditor editor)
    {
        if (editor.CurrentBitmap is not BitmapSource source || source.PixelWidth <= 0 || source.PixelHeight <= 0)
            return null;

        BitmapSource normalized = source;
        if (normalized.Format != PixelFormats.Bgra32)
            normalized = new FormatConvertedBitmap(normalized, PixelFormats.Bgra32, null, 0);
        var stride = checked(normalized.PixelWidth * 4);
        var pixels = new byte[checked(stride * normalized.PixelHeight)];
        normalized.CopyPixels(pixels, stride, 0);
        return new TextureSnapshot(normalized.PixelWidth, normalized.PixelHeight, pixels);
    }

    private void PushStudioTextureUndo(TextureEditorUxState state, TextureSnapshot snapshot, bool clearRedo)
    {
        if (state.Undo.Count > 0 && StudioTextureSnapshotsEqual(state.Undo[^1], snapshot))
            return;
        state.Undo.Add(snapshot);
        if (state.Undo.Count > StudioTextureHistoryLimit)
            state.Undo.RemoveAt(0);
        if (clearRedo)
            state.Redo.Clear();
        UpdateStudioTextureUxState(state);
    }

    private static bool StudioTextureSnapshotsEqual(TextureSnapshot left, TextureSnapshot right)
        => left.Width == right.Width && left.Height == right.Height && left.Pixels.AsSpan().SequenceEqual(right.Pixels);

    private void UndoStudioTexture(TextureEditorUxState state)
    {
        if (state.Undo.Count == 0)
            return;
        var current = CaptureStudioTextureSnapshot(state.Editor);
        if (current is null)
            return;
        var target = state.Undo[^1];
        state.Undo.RemoveAt(state.Undo.Count - 1);
        state.Redo.Add(current);
        RestoreStudioTextureSnapshot(state, target);
    }

    private void RedoStudioTexture(TextureEditorUxState state)
    {
        if (state.Redo.Count == 0)
            return;
        var current = CaptureStudioTextureSnapshot(state.Editor);
        if (current is null)
            return;
        var target = state.Redo[^1];
        state.Redo.RemoveAt(state.Redo.Count - 1);
        state.Undo.Add(current);
        RestoreStudioTextureSnapshot(state, target);
    }

    private void RestoreStudioTextureSnapshot(TextureEditorUxState state, TextureSnapshot snapshot)
    {
        if (state.Editor.CurrentBitmap is not WriteableBitmap target ||
            target.PixelWidth != snapshot.Width || target.PixelHeight != snapshot.Height)
            return;
        var stride = checked(snapshot.Width * 4);
        target.WritePixels(new Int32Rect(0, 0, snapshot.Width, snapshot.Height), snapshot.Pixels, stride, 0);
        PublishStudioTextureUxBitmap(state);
        UpdateStudioTextureUxState(state);
    }

    private void DeleteStudioTextureSelection(TextureEditorUxState state)
    {
        if (state.Editor.CurrentBitmap is not WriteableBitmap bitmap || state.Editor.Selection.IsEmpty)
            return;
        var before = CaptureStudioTextureSnapshot(state.Editor);
        if (before is null)
            return;
        PushStudioTextureUndo(state, before, clearRedo: true);
        var rect = state.Editor.Selection;
        var stride = checked(rect.Width * 4);
        bitmap.WritePixels(rect, new byte[checked(stride * rect.Height)], stride, 0);
        PublishStudioTextureUxBitmap(state);
        UpdateStudioTextureUxState(state);
    }

    private void PublishStudioTextureUxBitmap(TextureEditorUxState state)
    {
        if (state.Editor.CurrentBitmap is not BitmapSource bitmap || string.IsNullOrWhiteSpace(state.Editor.TexturePath))
            return;
        var path = state.Editor.TexturePath;
        var snapshot = CreateStudioTextureRenderSnapshot(bitmap);

        if (ReferenceEquals(state.Editor, _studioTextureUvEditor))
        {
            _studioTextureOverrides[path] = snapshot;
            _studioTextureModifiedPaths.Add(path);
            UpdateStudioTextureEditState(path);
            RefreshStudioLiveTexturePreview();
            SetStudioEditorStatus("Texture history applied in memory. Source PNG is unchanged.", "#D8B26A");
        }
        else if (ReferenceEquals(state.Editor, _studioItemTextureEditor))
        {
            _studioItemTextureOverrides[path] = snapshot;
            UpdateStudioItemTextureState(path);
            QueueEnhancedItemRefresh(reloadTextureEditor: false);
            SetStudioItemStatus("Item texture history applied in memory. Source PNG is unchanged.", "#D8B26A");
        }
    }

    private void SetStudioTextureZoom(TextureEditorUxState state, double zoom)
    {
        state.Zoom = Math.Clamp(zoom, 0.5, 16);
        state.Editor.SetZoom(state.Zoom);
        UpdateStudioTextureUxState(state);
    }

    private void FitStudioTextureEditor(TextureEditorUxState state)
    {
        if (state.Scroll is null || state.Surface is null || state.Surface.ActualWidth <= 0 || state.Surface.ActualHeight <= 0)
        {
            SetStudioTextureZoom(state, 1);
            return;
        }
        var availableWidth = Math.Max(64, state.Scroll.ViewportWidth - 18);
        var availableHeight = Math.Max(64, state.Scroll.ViewportHeight - 18);
        var factor = Math.Min(availableWidth / state.Surface.ActualWidth, availableHeight / state.Surface.ActualHeight);
        SetStudioTextureZoom(state, state.Zoom * factor);
        Dispatcher.BeginInvoke(() =>
        {
            state.Scroll.ScrollToHorizontalOffset(Math.Max(0, (state.Scroll.ExtentWidth - state.Scroll.ViewportWidth) / 2.0));
            state.Scroll.ScrollToVerticalOffset(Math.Max(0, (state.Scroll.ExtentHeight - state.Scroll.ViewportHeight) / 2.0));
        }, DispatcherPriority.Background);
    }

    private void UpdateStudioTextureUxState(TextureEditorUxState state)
    {
        ResetStudioTextureHistoryIfAssetChanged(state);
        UpdateStudioTextureToolButtons(state);
        UpdateStudioTexturePalette(state);

        var selection = state.Editor.Selection;
        var hasSelection = !selection.IsEmpty;
        if (state.CopyButton is not null) state.CopyButton.IsEnabled = hasSelection;
        if (state.PasteButton is not null) state.PasteButton.IsEnabled = state.HasClipboard;
        if (state.FlipHButton is not null) state.FlipHButton.IsEnabled = hasSelection;
        if (state.FlipVButton is not null) state.FlipVButton.IsEnabled = hasSelection;
        if (state.RotateButton is not null) state.RotateButton.IsEnabled = hasSelection;
        if (state.DeleteButton is not null) state.DeleteButton.IsEnabled = hasSelection;
        if (state.UndoButton is not null) state.UndoButton.IsEnabled = state.Undo.Count > 0;
        if (state.RedoButton is not null) state.RedoButton.IsEnabled = state.Redo.Count > 0;

        var color = state.Editor.PaintColor;
        if (state.ColorSwatch is not null)
            state.ColorSwatch.Background = new SolidColorBrush(color);
        if (state.ColorText is not null)
            state.ColorText.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        if (state.StatusText is not null)
        {
            var pointer = state.PointerX >= 0 && state.PointerY >= 0
                ? $"X {state.PointerX} · Y {state.PointerY}"
                : "X — · Y —";
            var selectionText = hasSelection
                ? $"Selection {selection.Width}×{selection.Height}px @ {selection.X},{selection.Y}"
                : "No selection";
            state.StatusText.Text = $"{TextureToolDisplayName(state.Editor.Tool)} · {state.Zoom:0.##}× · {pointer} · {selectionText} · Middle/Space drag pans · Ctrl+wheel zooms";
        }
    }

    private void ResetStudioTextureHistoryIfAssetChanged(TextureEditorUxState state)
    {
        var path = state.Editor.TexturePath;
        if (string.Equals(path, state.LastTexturePath, StringComparison.OrdinalIgnoreCase))
            return;
        state.LastTexturePath = path;
        state.Undo.Clear();
        state.Redo.Clear();
        state.PendingMutation = null;
        state.PendingMutationCommitted = false;
        state.HasClipboard = false;
    }

    private void UpdateStudioTextureToolButtons(TextureEditorUxState state)
    {
        foreach (var (tool, button) in state.ToolButtons)
        {
            var active = state.Editor.Tool == tool;
            button.Background = Brush(active ? "#244E54" : "#172127");
            button.BorderBrush = Brush(active ? "#70DCE2" : "#3A4B53");
            button.BorderThickness = new Thickness(active ? 2 : 1);
            button.Foreground = Brush(active ? "#E8FBFC" : "#B8C6CC");
            button.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private void UpdateStudioTexturePalette(TextureEditorUxState state)
    {
        var activeColor = state.Editor.PaintColor;
        foreach (var button in state.PaletteButtons)
        {
            var selected = TryGetStudioPaletteColor(button, out var color) && color == activeColor;
            button.BorderBrush = Brush(selected ? "#E7FAFB" : "#53636B");
            button.BorderThickness = new Thickness(selected ? 3 : 1);
            button.Opacity = selected ? 1 : 0.88;
        }
    }

    private void StudioUxQol_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var state = GetActiveStudioTextureUxState();
        if (state is null)
            return;

        if (Keyboard.FocusedElement is TextBox or ComboBox)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (control)
        {
            switch (e.Key)
            {
                case Key.Z:
                    UndoStudioTexture(state);
                    e.Handled = true;
                    return;
                case Key.Y:
                    RedoStudioTexture(state);
                    e.Handled = true;
                    return;
                case Key.C:
                    state.HasClipboard = state.Editor.CopySelection();
                    UpdateStudioTextureUxState(state);
                    e.Handled = true;
                    return;
                case Key.V:
                    ArmStudioTextureMutation(state);
                    state.Editor.PasteSelection();
                    state.PendingMutation = null;
                    state.PendingMutationCommitted = false;
                    UpdateStudioTextureUxState(state);
                    e.Handled = true;
                    return;
                case Key.A:
                    state.Editor.SelectAll();
                    UpdateStudioTextureUxState(state);
                    e.Handled = true;
                    return;
            }
        }

        MinecraftTextureEditTool? tool = e.Key switch
        {
            Key.P => MinecraftTextureEditTool.Pencil,
            Key.E => MinecraftTextureEditTool.Eraser,
            Key.F => MinecraftTextureEditTool.Fill,
            Key.I => MinecraftTextureEditTool.Eyedropper,
            Key.L => MinecraftTextureEditTool.Line,
            Key.R => MinecraftTextureEditTool.Rectangle,
            Key.S => MinecraftTextureEditTool.Selection,
            Key.U when !state.IsItemEditor => MinecraftTextureEditTool.Uv,
            _ => null
        };
        if (tool is not null)
        {
            state.Editor.SetTool(tool.Value);
            UpdateStudioTextureUxState(state);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            DeleteStudioTextureSelection(state);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && state.Editor.Tool == MinecraftTextureEditTool.Selection)
        {
            state.Editor.SetTool(MinecraftTextureEditTool.Pencil);
            UpdateStudioTextureUxState(state);
            e.Handled = true;
        }
    }

    private TextureEditorUxState? GetActiveStudioTextureUxState()
        => _studioTextureUxStates.Values.FirstOrDefault(state => state.Editor.IsVisible && state.Root.IsVisible);

    private void HookStudioViewportUx()
    {
        if (_studioBlockViewport is not null)
        {
            _studioBlockViewport.FaceSelected += StudioViewport_FaceSelected;
            if (_studioBlockViewport.Parent is DependencyObject viewportRoot)
            {
                foreach (var hint in FindStudioVisualChildren<TextBlock>(viewportRoot)
                             .Where(text => text.Text.Contains("drag", StringComparison.OrdinalIgnoreCase) ||
                                            text.Text.Contains("Double click", StringComparison.OrdinalIgnoreCase)))
                {
                    if (hint.Text.StartsWith("Left drag:", StringComparison.OrdinalIgnoreCase))
                        hint.Text = "Click face: select · left drag: orbit · middle drag: pan · right click: menu · wheel: zoom";
                }

                foreach (var border in FindStudioVisualChildren<Border>(viewportRoot))
                {
                    if (FindStudioVisualChildren<TextBlock>(border)
                        .Any(text => text.Text.Equals("DIRECT MODEL GIZMO", StringComparison.OrdinalIgnoreCase)))
                    {
                        border.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        if (_studioItemEditorSurface is not null)
        {
            foreach (var hint in FindStudioVisualChildren<TextBlock>(_studioItemEditorSurface)
                         .Where(text => text.Text.StartsWith("3D item preview", StringComparison.OrdinalIgnoreCase)))
            {
                hint.Text = "3D item preview · left drag orbit · middle drag pan · right click menu · wheel zoom";
            }
        }
    }

    private void StudioViewport_FaceSelected(int elementIndex, string direction)
    {
        if (_studioElementList is null || elementIndex < 0 || elementIndex >= _studioElementList.Items.Count)
            return;

        _studioElementList.SelectedIndex = elementIndex;
        _studioElementList.ScrollIntoView(_studioElementList.SelectedItem);
        RefreshStudioFaceEditor();

        if (_studioFaceCombo is not null)
        {
            var match = _studioFaceCombo.Items.Cast<object>()
                .Select(item => item?.ToString())
                .FirstOrDefault(value => string.Equals(value, direction, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                _studioFaceCombo.SelectedItem = match;
        }
        LoadStudioSelectedFace();
        SetStudioEditorStatus($"Selected {direction.ToUpperInvariant()} face on element {elementIndex + 1}.", "#74C99A");
    }

    private static bool TryMapTextureTool(string? label, out MinecraftTextureEditTool tool)
    {
        tool = label switch
        {
            "UV" => MinecraftTextureEditTool.Uv,
            "Pencil" => MinecraftTextureEditTool.Pencil,
            "Eraser" => MinecraftTextureEditTool.Eraser,
            "Fill" => MinecraftTextureEditTool.Fill,
            "Picker" => MinecraftTextureEditTool.Eyedropper,
            "Line" => MinecraftTextureEditTool.Line,
            "Rect" => MinecraftTextureEditTool.Rectangle,
            "Select" => MinecraftTextureEditTool.Selection,
            _ => (MinecraftTextureEditTool)(-1)
        };
        return (int)tool >= 0;
    }

    private static string TextureToolDisplayName(MinecraftTextureEditTool tool)
        => tool switch
        {
            MinecraftTextureEditTool.Uv => "UV",
            MinecraftTextureEditTool.Pencil => "Pencil",
            MinecraftTextureEditTool.Eraser => "Eraser",
            MinecraftTextureEditTool.Fill => "Fill",
            MinecraftTextureEditTool.Eyedropper => "Picker",
            MinecraftTextureEditTool.Line => "Line",
            MinecraftTextureEditTool.Rectangle => "Rectangle",
            MinecraftTextureEditTool.Selection => "Selection",
            _ => tool.ToString()
        };

    private static string TextureToolShortcut(MinecraftTextureEditTool tool)
        => tool switch
        {
            MinecraftTextureEditTool.Uv => "U",
            MinecraftTextureEditTool.Pencil => "P",
            MinecraftTextureEditTool.Eraser => "E",
            MinecraftTextureEditTool.Fill => "F",
            MinecraftTextureEditTool.Eyedropper => "I",
            MinecraftTextureEditTool.Line => "L",
            MinecraftTextureEditTool.Rectangle => "R",
            MinecraftTextureEditTool.Selection => "S",
            _ => string.Empty
        };

    private static bool IsStudioPaletteButton(Button button)
        => button.ToolTip?.ToString()?.Contains(" · #", StringComparison.Ordinal) == true;

    private static bool TryGetStudioPaletteColor(Button button, out Color color)
    {
        color = Colors.Transparent;
        var tooltip = button.ToolTip?.ToString();
        if (string.IsNullOrWhiteSpace(tooltip))
            return false;
        var marker = tooltip.LastIndexOf(" · #", StringComparison.Ordinal);
        if (marker < 0)
            return false;
        var hex = tooltip[(marker + 3)..].Trim();
        if (!hex.StartsWith('#'))
            hex = "#" + hex;
        return TryParseStudioTextureColor(hex, out color);
    }

    private sealed class TextureEditorUxState(
        MinecraftTextureUvEditor editor,
        Panel root,
        bool isItemEditor)
    {
        public MinecraftTextureUvEditor Editor { get; } = editor;
        public Panel Root { get; } = root;
        public bool IsItemEditor { get; } = isItemEditor;
        public Dictionary<MinecraftTextureEditTool, Button> ToolButtons { get; } = [];
        public List<Button> PaletteButtons { get; } = [];
        public List<TextureSnapshot> Undo { get; } = [];
        public List<TextureSnapshot> Redo { get; } = [];
        public ScrollViewer? Scroll { get; set; }
        public Canvas? Surface { get; set; }
        public Button? CopyButton { get; set; }
        public Button? PasteButton { get; set; }
        public Button? FlipHButton { get; set; }
        public Button? FlipVButton { get; set; }
        public Button? RotateButton { get; set; }
        public Button? UndoButton { get; set; }
        public Button? RedoButton { get; set; }
        public Button? DeleteButton { get; set; }
        public Border? ColorSwatch { get; set; }
        public TextBlock? ColorText { get; set; }
        public TextBlock? StatusText { get; set; }
        public TextureSnapshot? PendingMutation { get; set; }
        public bool PendingMutationCommitted { get; set; }
        public bool HasClipboard { get; set; }
        public bool Panning { get; set; }
        public Point PanStart { get; set; }
        public double PanHorizontalOffset { get; set; }
        public double PanVerticalOffset { get; set; }
        public double Zoom { get; set; } = 1;
        public int PointerX { get; set; } = -1;
        public int PointerY { get; set; } = -1;
        public string? LastTexturePath { get; set; }
    }

    private sealed record TextureSnapshot(int Width, int Height, byte[] Pixels);
}

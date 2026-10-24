using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private readonly List<Thumb> _phase8ResizeHandles = [];
    private readonly List<Line> _phase8Guides = [];
    private TreeView? _phase8Hierarchy;
    private TextBox? _phase8ItemIdBox;
    private TextBox? _phase8StackBox;
    private TextBox? _phase8TextScaleBox;
    private ComboBox? _phase8TextAlignBox;
    private CheckBox? _phase8TextShadowBox;
    private CheckBox? _phase8GhostBox;
    private CheckBox? _phase8GlintBox;
    private Slider? _phase8TimelineSlider;
    private TextBlock? _phase8TimelineText;
    private ComboBox? _phase8SpeedBox;
    private CheckBox? _phase8LoopBox;
    private DispatcherTimer? _phase8TimelineTimer;
    private DateTime _phase8LastTick;
    private double _phase8TimelineProgress;
    private bool _phase8Playing;
    private bool _phase8UpdatingSlider;
    private bool _phase8OverlayBusy;
    private bool _phase8Initialized;
    private string? _phase8ResizeUndo;
    private Point _phase8ResizeStartMouse;
    private Rect _phase8ResizeStartRect;

    public void EnableForeverPhase8Tools()
    {
        if (_phase8Initialized) return;
        _phase8Initialized = true;

        BuildPhase8Hierarchy();
        BuildPhase8Inspector();
        BuildPhase8Timeline();

        if (SkyblockBarHost.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Content, "Load machine")) is { } load)
            load.Click += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(EnsurePhase8MachineScene));
        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(RefreshPhase8Ui));

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_phase8OverlayBusy) return;
            _phase8OverlayBusy = true;
            try
            {
                EnsurePhase8OverlayVisuals();
                RefreshPhase8Hierarchy();
                DecoratePhase8Visuals();
            }
            finally { _phase8OverlayBusy = false; }
        };
        ElementsList.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshPhase8Ui));

        _phase8TimelineTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _phase8TimelineTimer.Tick += Phase8TimelineTick;
        _phase8TimelineTimer.Start();
        RefreshPhase8Ui();
    }

    private void BuildPhase8Hierarchy()
    {
        if (ElementsList.Parent is not DockPanel host) return;
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(panel, Dock.Top);
        panel.Children.Add(new TextBlock { Text = "Hierarchy", FontWeight = FontWeights.SemiBold, Foreground = Brush("#D9E2E8") });
        panel.Children.Add(new TextBlock { Text = "Groups · visibility · locks · layer order", FontSize = 10, Foreground = Brush("#8F9DA7"), Margin = new Thickness(0, 0, 0, 4) });
        _phase8Hierarchy = new TreeView { Height = 190, Background = Brush("#17191C"), BorderBrush = Brush("#40464D"), Foreground = Brush("#E8E8E8") };
        _phase8Hierarchy.SelectedItemChanged += (_, _) =>
        {
            if (_phase8Hierarchy.SelectedItem is TreeViewItem { Tag: GuiElementModel model }) SetSelection([model]);
        };
        panel.Children.Add(_phase8Hierarchy);
        var actions = new UniformGrid { Columns = 3, Margin = new Thickness(0, 4, 0, 0) };
        actions.Children.Add(Phase8Button("Hide/show", (_, _) => TogglePhase8Visibility()));
        actions.Children.Add(Phase8Button("Lock", (_, _) => TogglePhase8Lock()));
        actions.Children.Add(Phase8Button("Group", (_, _) => GroupPhase8Selection()));
        actions.Children.Add(Phase8Button("Ungroup", (_, _) => SetPhase8Group(null)));
        actions.Children.Add(Phase8Button("↑ Layer", (_, _) => MovePhase8Layer(-1)));
        actions.Children.Add(Phase8Button("↓ Layer", (_, _) => MovePhase8Layer(1)));
        panel.Children.Add(actions);
        host.Children.Insert(0, panel);
    }

    private void BuildPhase8Inspector()
    {
        if (SelectionText.Parent is not StackPanel host) return;
        var expander = new Expander { Header = "True WYSIWYG", IsExpanded = true, Margin = new Thickness(0, 10, 0, 0) };
        var panel = new StackPanel { Margin = new Thickness(0, 7, 0, 0) };
        panel.Children.Add(new TextBlock { Text = "Item / block preview", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Resource id", Foreground = Brush("#AEB4BA") });
        _phase8ItemIdBox = new TextBox(); panel.Children.Add(_phase8ItemIdBox);
        panel.Children.Add(new TextBlock { Text = "Stack count", Foreground = Brush("#AEB4BA") });
        _phase8StackBox = new TextBox(); panel.Children.Add(_phase8StackBox);
        var toggles = new StackPanel { Orientation = Orientation.Horizontal };
        _phase8GhostBox = new CheckBox { Content = "Ghost" };
        _phase8GlintBox = new CheckBox { Content = "Glint" };
        toggles.Children.Add(_phase8GhostBox); toggles.Children.Add(_phase8GlintBox); panel.Children.Add(toggles);
        panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        panel.Children.Add(new TextBlock { Text = "Minecraft text", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Scale", Foreground = Brush("#AEB4BA") });
        _phase8TextScaleBox = new TextBox(); panel.Children.Add(_phase8TextScaleBox);
        _phase8TextAlignBox = new ComboBox();
        foreach (var s in new[] { "left", "center", "right" }) _phase8TextAlignBox.Items.Add(s);
        _phase8TextAlignBox.SelectedIndex = 0; panel.Children.Add(_phase8TextAlignBox);
        _phase8TextShadowBox = new CheckBox { Content = "1 px shadow" }; panel.Children.Add(_phase8TextShadowBox);
        panel.Children.Add(Phase8Button("Apply WYSIWYG properties", ApplyPhase8Inspector));
        expander.Content = panel;
        host.Children.Add(expander);
    }

    private void BuildPhase8Timeline()
    {
        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("TIMELINE"));
        SkyblockBarHost.Children.Add(Phase8Button("▶", (_, _) => PlayPhase8Timeline()));
        SkyblockBarHost.Children.Add(Phase8Button("Ⅱ", (_, _) => _phase8Playing = false));
        SkyblockBarHost.Children.Add(Phase8Button("■", (_, _) => StopPhase8Timeline()));
        _phase8LoopBox = new CheckBox { Content = "Loop", IsChecked = true };
        SkyblockBarHost.Children.Add(_phase8LoopBox);
        _phase8TimelineSlider = new Slider { Width = 130, Minimum = 0, Maximum = 100, Value = 50, TickFrequency = 25, IsSnapToTickEnabled = false, Margin = new Thickness(4, 0, 4, 0) };
        _phase8TimelineSlider.ValueChanged += (_, _) =>
        {
            if (_phase8UpdatingSlider) return;
            _phase8Playing = false;
            SetPhase8TimelineProgress(_phase8TimelineSlider.Value / 100.0);
        };
        SkyblockBarHost.Children.Add(_phase8TimelineSlider);
        _phase8TimelineText = new TextBlock { Width = 74, Foreground = Brush("#AEBBC4"), VerticalAlignment = VerticalAlignment.Center };
        SkyblockBarHost.Children.Add(_phase8TimelineText);
        _phase8SpeedBox = new ComboBox { Width = 64 };
        foreach (var speed in new[] { .25, .5, 1.0, 2.0 }) _phase8SpeedBox.Items.Add(new ComboBoxItem { Content = $"{speed:0.##}x", Tag = speed });
        _phase8SpeedBox.SelectedIndex = 2;
        SkyblockBarHost.Children.Add(_phase8SpeedBox);
    }

    private static Button Phase8Button(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text };
        button.Click += click;
        return button;
    }

    private void RefreshPhase8Ui()
    {
        RefreshPhase8Hierarchy();
        RefreshPhase8Inspector();
        EnsurePhase8OverlayVisuals();
        DecoratePhase8Visuals();
        UpdatePhase8DynamicVisuals();
    }

    private void RefreshPhase8Hierarchy()
    {
        if (_phase8Hierarchy is null) return;
        var selectedIds = _selection.Select(e => e.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _phase8Hierarchy.Items.Clear();
        foreach (var group in _layout.Elements.GroupBy(e => string.IsNullOrWhiteSpace(e.Group) ? "Ungrouped" : e.Group!).OrderBy(g => g.Key))
        {
            var groupNode = new TreeViewItem { Header = group.Key, IsExpanded = true };
            foreach (var element in group)
            {
                var flags = $"{(element.Visible ? "◉" : "○")} {(element.Locked ? "🔒" : "  ")}";
                var child = new TreeViewItem { Header = $"{flags} {element.Id} [{element.Type}]", Tag = element };
                if (selectedIds.Contains(element.Id)) child.FontWeight = FontWeights.SemiBold;
                groupNode.Items.Add(child);
            }
            _phase8Hierarchy.Items.Add(groupNode);
        }
    }

    private void RefreshPhase8Inspector()
    {
        var item = _selection.Count == 1 ? _selection.First() : null;
        if (_phase8ItemIdBox is null || _phase8StackBox is null || _phase8TextScaleBox is null || _phase8TextAlignBox is null || _phase8TextShadowBox is null || _phase8GhostBox is null || _phase8GlintBox is null) return;
        var enabled = item is not null;
        foreach (var control in new Control[] { _phase8ItemIdBox, _phase8StackBox, _phase8TextScaleBox, _phase8TextAlignBox, _phase8TextShadowBox, _phase8GhostBox, _phase8GlintBox }) control.IsEnabled = enabled;
        if (item is null) return;
        _phase8ItemIdBox.Text = item.ItemId ?? string.Empty;
        _phase8StackBox.Text = item.StackCount.ToString(CultureInfo.InvariantCulture);
        _phase8TextScaleBox.Text = item.TextScale.ToString("0.##", CultureInfo.InvariantCulture);
        _phase8TextAlignBox.SelectedItem = item.TextAlignment;
        _phase8TextShadowBox.IsChecked = item.TextShadow;
        _phase8GhostBox.IsChecked = item.GhostPreview;
        _phase8GlintBox.IsChecked = item.GlintPreview;
    }

    private void ApplyPhase8Inspector(object sender, RoutedEventArgs e)
    {
        if (_selection.Count != 1) return;
        var model = _selection.First();
        PushUndo();
        model.ItemId = string.IsNullOrWhiteSpace(_phase8ItemIdBox?.Text) ? null : _phase8ItemIdBox.Text.Trim();
        if (int.TryParse(_phase8StackBox?.Text, out var count)) model.StackCount = Math.Clamp(count, 1, 99);
        if (double.TryParse(_phase8TextScaleBox?.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)) model.TextScale = Math.Clamp(scale, .25, 4);
        model.TextAlignment = _phase8TextAlignBox?.SelectedItem as string ?? "left";
        model.TextShadow = _phase8TextShadowBox?.IsChecked != false;
        model.GhostPreview = _phase8GhostBox?.IsChecked == true;
        model.GlintPreview = _phase8GlintBox?.IsChecked == true;
        RenderLayout(); MarkDirty(); RefreshPhase8Ui();
    }

    private void TogglePhase8Visibility()
    {
        if (_selection.Count == 0) return;
        PushUndo(); var visible = !_selection.All(e => e.Visible);
        foreach (var e in _selection) e.Visible = visible;
        RenderLayout(); MarkDirty();
    }

    private void TogglePhase8Lock()
    {
        if (_selection.Count == 0) return;
        PushUndo(); var locked = !_selection.All(e => e.Locked);
        foreach (var e in _selection) e.Locked = locked;
        RefreshElementList(); RenderLayout(); MarkDirty();
    }

    private void GroupPhase8Selection()
    {
        if (_selection.Count < 1) return;
        var n = 1; var used = _layout.Elements.Select(e => e.Group).Where(g => !string.IsNullOrWhiteSpace(g)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        while (used.Contains($"Group {n}")) n++;
        SetPhase8Group($"Group {n}");
    }

    private void SetPhase8Group(string? group)
    {
        if (_selection.Count == 0) return;
        PushUndo(); foreach (var e in _selection) e.Group = group;
        RefreshElementList(); RefreshPhase8Hierarchy(); MarkDirty();
    }

    private void MovePhase8Layer(int direction)
    {
        if (_selection.Count != 1) return;
        var item = _selection.First(); var index = _layout.Elements.IndexOf(item); var next = Math.Clamp(index + direction, 0, _layout.Elements.Count - 1);
        if (index == next) return;
        PushUndo(); _layout.Elements.RemoveAt(index); _layout.Elements.Insert(next, item); RefreshElementList(); RenderLayout(); MarkDirty();
    }

    private void DecoratePhase8Visuals()
    {
        foreach (var pair in _visuals) DecoratePhase8ElementVisual(pair.Key, pair.Value);
    }

    private void DecoratePhase8ElementVisual(GuiElementModel model, FrameworkElement visual)
    {
        if (visual is not Border border) return;
        border.Cursor = model.Locked ? Cursors.Arrow : Cursors.SizeAll;

        if ((model.Type == GuiElementType.Text || model.Type == GuiElementType.Button) && border.Child is not MinecraftPixelText)
        {
            var color = model.Type == GuiElementType.Button ? Brush("#FFFFFF") : Brush("#3F3F3F");
            var text = string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content;
            border.Child = new MinecraftPixelText(text, color, model.TextShadow, model.TextScale, model.TextAlignment);
            var needed = text.Sum(c => c == ' ' ? 4.0 : 6.0) * model.TextScale;
            if (needed > model.Width)
            {
                border.BorderBrush = Brush("#E06C75");
                border.ToolTip = $"Text overflow: ~{needed:0.#} px required, {model.Width} px available.";
            }
        }

        if (model.Type == GuiElementType.Slot && !string.IsNullOrWhiteSpace(model.ItemId))
            DecoratePhase8SlotItem(model, border);

        if (model.Id.Contains("phase8_panel", StringComparison.OrdinalIgnoreCase))
        {
            border.Background = Brush("#465A68"); border.BorderBrush = Brush("#71808D"); border.Child = null;
        }
        else if (model.Id.Contains("phase8_meter", StringComparison.OrdinalIgnoreCase))
            DecoratePhase8Meter(model, border);
        else if (model.Id.Contains("phase8_ring", StringComparison.OrdinalIgnoreCase))
        {
            border.Background = Brushes.Transparent; border.BorderBrush = Brush("#8E4FD2"); border.BorderThickness = new Thickness(2); border.Child = null;
        }
    }

    private void DecoratePhase8SlotItem(GuiElementModel model, Border border)
    {
        if (border.Child is Grid { Tag: string tag } && tag == "phase8-item") return;
        var grid = new Grid { Tag = "phase8-item" };
        grid.Children.Add(CreateSlotBody());
        if (!string.IsNullOrWhiteSpace(_repoRoot) && Directory.Exists(_repoRoot))
        {
            var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap(model.ItemId!);
            if (bitmap is not null)
            {
                var image = new Image { Source = bitmap, Stretch = Stretch.Uniform, Margin = new Thickness(2), Opacity = model.GhostPreview ? .45 : 1 };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor); grid.Children.Add(image);
            }
        }
        if (model.GlintPreview)
        {
            grid.Children.Add(new Border { Margin = new Thickness(3), BorderBrush = Brush("#9C78D8"), BorderThickness = new Thickness(1), Opacity = .75, IsHitTestVisible = false });
        }
        if (model.StackCount > 1)
        {
            grid.Children.Add(new TextBlock { Text = model.StackCount.ToString(), FontSize = 6, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0,0,1,0), Effect = null });
        }
        if (model.Durability >= 0)
        {
            var track = new Grid { Height = 2, Margin = new Thickness(2,0,2,1), VerticalAlignment = VerticalAlignment.Bottom };
            track.Children.Add(new Border { Background = Brush("#1A1A1A") });
            track.Children.Add(new Border { Background = Brush("#55FF55"), HorizontalAlignment = HorizontalAlignment.Left, Width = Math.Max(0, 12 * Math.Clamp(model.Durability,0,1)) });
            grid.Children.Add(track);
        }
        border.Child = grid;
    }

    private void DecoratePhase8Meter(GuiElementModel model, Border border)
    {
        if (border.Child is Grid { Tag: string tag } && tag == "phase8-meter") return;
        var grid = new Grid { Tag = "phase8-meter", Margin = new Thickness(1) };
        grid.Children.Add(new Border { Background = Brush("#2D3843") });
        grid.Children.Add(new Border { Background = model.Id.Contains("energy", StringComparison.OrdinalIgnoreCase) ? Brush("#8E4FD2") : Brush("#4DEBFF"), HorizontalAlignment = HorizontalAlignment.Left, Width = Math.Max(0, (model.Width - 2) * _phase8TimelineProgress) });
        border.Background = Brush("#637888"); border.BorderBrush = Brush("#2D3843"); border.Child = grid;
    }

    private void EnsurePhase8OverlayVisuals()
    {
        foreach (var h in _phase8ResizeHandles) if (PreviewCanvas.Children.Contains(h)) PreviewCanvas.Children.Remove(h);
        _phase8ResizeHandles.Clear();
        if (_selection.Count != 1) return;
        var model = _selection.First();
        if (model.Locked || !model.Visible) return;
        foreach (var dir in new[] { "nw","n","ne","e","se","s","sw","w" })
        {
            var thumb = new Thumb { Width = 4, Height = 4, Background = Brush("#E8F4FF"), BorderBrush = Brush("#2E9BFF"), BorderThickness = new Thickness(.5), Tag = dir, Cursor = Phase8Cursor(dir) };
            thumb.DragStarted += Phase8ResizeStarted; thumb.DragDelta += Phase8ResizeDelta; thumb.DragCompleted += Phase8ResizeCompleted;
            Panel.SetZIndex(thumb, 10002); PreviewCanvas.Children.Add(thumb); _phase8ResizeHandles.Add(thumb);
        }
        PositionPhase8ResizeHandles(model);
    }

    private static Cursor Phase8Cursor(string dir) => dir switch { "n" or "s" => Cursors.SizeNS, "e" or "w" => Cursors.SizeWE, "nw" or "se" => Cursors.SizeNWSE, _ => Cursors.SizeNESW };

    private void PositionPhase8ResizeHandles(GuiElementModel m)
    {
        if (_phase8ResizeHandles.Count != 8) return;
        var points = new[] { new Point(m.X-2,m.Y-2), new Point(m.X+m.Width/2.0-2,m.Y-2), new Point(m.X+m.Width-2,m.Y-2), new Point(m.X+m.Width-2,m.Y+m.Height/2.0-2), new Point(m.X+m.Width-2,m.Y+m.Height-2), new Point(m.X+m.Width/2.0-2,m.Y+m.Height-2), new Point(m.X-2,m.Y+m.Height-2), new Point(m.X-2,m.Y+m.Height/2.0-2) };
        for (var i=0;i<8;i++) { Canvas.SetLeft(_phase8ResizeHandles[i], points[i].X); Canvas.SetTop(_phase8ResizeHandles[i], points[i].Y); }
    }

    private void Phase8ResizeStarted(object sender, DragStartedEventArgs e)
    {
        if (_selection.Count != 1) return; var m = _selection.First();
        _phase8ResizeUndo = SerializeLayout(); _phase8ResizeStartMouse = Mouse.GetPosition(PreviewCanvas); _phase8ResizeStartRect = new Rect(m.X,m.Y,m.Width,m.Height);
    }

    private void Phase8ResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selection.Count != 1 || sender is not Thumb { Tag: string dir }) return;
        var m = _selection.First(); var mouse = Mouse.GetPosition(PreviewCanvas); var dx = Snap(mouse.X-_phase8ResizeStartMouse.X); var dy = Snap(mouse.Y-_phase8ResizeStartMouse.Y);
        var r = _phase8ResizeStartRect; var left=r.Left; var top=r.Top; var right=r.Right; var bottom=r.Bottom;
        if (dir.Contains('w')) left += dx; if (dir.Contains('e')) right += dx; if (dir.Contains('n')) top += dy; if (dir.Contains('s')) bottom += dy;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) { if (dir.Contains('w')||dir.Contains('e')) { left=r.Left-dx; right=r.Right+dx; } if (dir.Contains('n')||dir.Contains('s')) { top=r.Top-dy; bottom=r.Bottom+dy; } }
        var width=Math.Max(1,right-left); var height=Math.Max(1,bottom-top);
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) { var ratio=r.Width/Math.Max(1,r.Height); if (Math.Abs(dx)>=Math.Abs(dy)) height=width/ratio; else width=height*ratio; }
        m.X=Clamp((int)Math.Round(left),0,_layout.Width-1); m.Y=Clamp((int)Math.Round(top),0,_layout.Height-1); m.Width=Clamp((int)Math.Round(width),1,_layout.Width-m.X); m.Height=Clamp((int)Math.Round(height),1,_layout.Height-m.Y);
        if (_visuals.TryGetValue(m,out var visual)) { visual.Width=m.Width; visual.Height=m.Height; Canvas.SetLeft(visual,m.X); Canvas.SetTop(visual,m.Y); }
        PositionPhase8ResizeHandles(m); UpdateInspector(); MarkDirty();
    }

    private void Phase8ResizeCompleted(object sender, DragCompletedEventArgs e)
    {
        if (_phase8ResizeUndo is not null) PushUndoSnapshot(_phase8ResizeUndo); _phase8ResizeUndo=null; RenderLayout();
    }

    private void ApplyPhase8SmartGuides(ref int dx, ref int dy)
    {
        ClearPhase8Guides();
        if (_selection.Count == 0 || _dragOrigins.Count == 0 || (Keyboard.Modifiers & ModifierKeys.Alt) != 0) return;
        var left=_dragOrigins.Min(p=>p.Value.X)+dx; var top=_dragOrigins.Min(p=>p.Value.Y)+dy; var right=_dragOrigins.Max(p=>p.Value.X+p.Key.Width)+dx; var bottom=_dragOrigins.Max(p=>p.Value.Y+p.Key.Height)+dy;
        var xCandidates=new[]{left,(left+right)/2,right}; var yCandidates=new[]{top,(top+bottom)/2,bottom}; const double threshold=2.5;
        double? bestX=null,bestY=null,targetX=null,targetY=null;
        var others=_layout.Elements.Where(e=>e.Visible&&!_selection.Contains(e)).ToList();
        foreach(var other in others)
        {
            foreach(var tx in new[]{(double)other.X,other.X+other.Width/2.0,(double)(other.X+other.Width)}) foreach(var sx in xCandidates) { var d=tx-sx; if(Math.Abs(d)<=threshold && (bestX is null||Math.Abs(d)<Math.Abs(bestX.Value))) {bestX=d;targetX=tx;} }
            foreach(var ty in new[]{(double)other.Y,other.Y+other.Height/2.0,(double)(other.Y+other.Height)}) foreach(var sy in yCandidates) { var d=ty-sy; if(Math.Abs(d)<=threshold && (bestY is null||Math.Abs(d)<Math.Abs(bestY.Value))) {bestY=d;targetY=ty;} }
        }
        var cx=_layout.Width/2.0; var cy=_layout.Height/2.0;
        foreach(var sx in xCandidates){var d=cx-sx;if(Math.Abs(d)<=threshold&&(bestX is null||Math.Abs(d)<Math.Abs(bestX.Value))){bestX=d;targetX=cx;}}
        foreach(var sy in yCandidates){var d=cy-sy;if(Math.Abs(d)<=threshold&&(bestY is null||Math.Abs(d)<Math.Abs(bestY.Value))){bestY=d;targetY=cy;}}
        if(bestX is not null){dx+=(int)Math.Round(bestX.Value);AddPhase8Guide(targetX!.Value,true);} if(bestY is not null){dy+=(int)Math.Round(bestY.Value);AddPhase8Guide(targetY!.Value,false);}
    }

    private void AddPhase8Guide(double value,bool vertical)
    {
        var line=new Line{Stroke=Brush("#4DEBFF"),StrokeThickness=.6,IsHitTestVisible=false,X1=vertical?value:0,X2=vertical?value:_layout.Width,Y1=vertical?0:value,Y2=vertical?_layout.Height:value};
        Panel.SetZIndex(line,10001); PreviewCanvas.Children.Add(line); _phase8Guides.Add(line);
    }
    private void ClearPhase8Guides(){foreach(var l in _phase8Guides)PreviewCanvas.Children.Remove(l);_phase8Guides.Clear();}

    private void PlayPhase8Timeline(){_phase8Playing=true;_phase8LastTick=DateTime.UtcNow;_phase7ProcessingTimer?.Stop();}
    private void StopPhase8Timeline(){_phase8Playing=false;SetPhase8TimelineProgress(0);}
    private void Phase8TimelineTick(object? sender,EventArgs e)
    {
        if(!_phase8Playing)return;var now=DateTime.UtcNow;var dt=(now-_phase8LastTick).TotalSeconds;_phase8LastTick=now;
        var speed=_phase8SpeedBox?.SelectedItem is ComboBoxItem{Tag:double s}?s:1.0;var duration=Phase8DurationSeconds();var next=_phase8TimelineProgress+dt*speed/duration;
        if(next>=1){if(_phase8LoopBox?.IsChecked==true)next%=1;else{next=1;_phase8Playing=false;}}SetPhase8TimelineProgress(next);
    }
    private double Phase8DurationSeconds()=>_activeForeverTemplate?.ScreenClass switch{"AscensionScreen"=>10.0,"CrucibleScreen"=>5.0,"RefiningScreen"=>5.0,_=>5.0};
    private void SetPhase8TimelineProgress(double value)
    {
        _phase8TimelineProgress=Math.Clamp(value,0,1);if(_phase8TimelineSlider is not null){_phase8UpdatingSlider=true;_phase8TimelineSlider.Value=_phase8TimelineProgress*100;_phase8UpdatingSlider=false;}
        if(_phase8TimelineText is not null)_phase8TimelineText.Text=$"{_phase8TimelineProgress*Phase8DurationSeconds():0.0}/{Phase8DurationSeconds():0.#}s";
        if(_phase7ActiveProcessingItem is not null){if(!ReferenceEquals(_foreverProgressCombo?.SelectedItem,_phase7ActiveProcessingItem))_foreverProgressCombo!.SelectedItem=_phase7ActiveProcessingItem;_phase7ActiveProcessingItem.Tag=(_phase8TimelineProgress*100).ToString("0.###",CultureInfo.InvariantCulture);UpdatePhase7ProgressVisuals();}
        UpdatePhase8DynamicVisuals();
    }

    private void UpdatePhase8DynamicVisuals()
    {
        foreach(var pair in _visuals)
        {
            if(!pair.Key.Id.Contains("phase8_meter",StringComparison.OrdinalIgnoreCase)||pair.Value is not Border{Child:Grid grid}||grid.Children.Count<2||grid.Children[1] is not Border fill)continue;
            fill.Width=Math.Max(0,(pair.Key.Width-2)*_phase8TimelineProgress);
        }
        if(_activeForeverTemplate?.ScreenClass=="AscensionScreen")
        {
            foreach(var pair in _visuals.Where(p=>p.Key.Id.Contains("phase8_ring",StringComparison.OrdinalIgnoreCase))) pair.Value.RenderTransform=new RotateTransform(_phase8TimelineProgress*360*(pair.Key.Id.EndsWith("2")?-1:1),pair.Key.Width/2.0,pair.Key.Height/2.0);
        }
    }

    private void EnsurePhase8MachineScene()
    {
        if(_activeForeverTemplate is null||!string.Equals(_layout.Name,_activeForeverTemplate.DisplayName,StringComparison.Ordinal))return;
        var prefix="forever_phase8_";_layout.Elements.RemoveAll(e=>e.Id.StartsWith(prefix,StringComparison.OrdinalIgnoreCase));
        void Add(string id,GuiElementType type,int x,int y,int w,int h,string content="",string? item=null){_layout.Elements.Add(new GuiElementModel{Id=prefix+id,Type=type,X=x,Y=y,Width=w,Height=h,Content=content,ItemId=item,TextShadow=true});}
        switch(_activeForeverTemplate.ScreenClass)
        {
            case "CrucibleScreen":
                Add("panel_heat",GuiElementType.Texture,18,35,212,18);Add("panel_tank",GuiElementType.Texture,18,58,103,18);Add("panel_freezer",GuiElementType.Texture,127,58,103,18);Add("panel_process",GuiElementType.Texture,18,81,212,70);
                Add("text_title",GuiElementType.Text,42,9,130,16,"CRUCIBLE");Add("text_heat",GuiElementType.Text,50,39,148,10,"HEAT: NONE");Add("text_input",GuiElementType.Text,30,88,48,9,"INPUT");Add("text_output",GuiElementType.Text,176,88,48,9,"OUTPUT");
                Add("meter_melt",GuiElementType.Texture,46,141,64,4);Add("meter_freeze",GuiElementType.Texture,138,141,64,4);break;
            case "AscensionScreen":
                Add("text_title",GuiElementType.Text,41,9,142,16,"ASCENSION CHAMBER");Add("text_core",GuiElementType.Text,22,29,80,10,"FOREVER CORE");
                Add("ring_1",GuiElementType.Texture,44,48,36,36);Add("ring_2",GuiElementType.Texture,49,53,26,26);Add("panel_reward",GuiElementType.Texture,149,46,70,41);Add("text_reward",GuiElementType.Text,158,50,52,18,"+1 PP");
                Add("meter_conversion",GuiElementType.Texture,23,106,146,8);Add("panel_recipient",GuiElementType.Texture,20,120,208,17);
                var core=_layout.Elements.FirstOrDefault(e=>e.Type==GuiElementType.Slot&&e.X==54&&e.Y==58);if(core is not null){core.ItemId="forever_skyblock:forever_core";core.StackCount=1;}break;
            case "RefiningScreen":
                Add("text_title",GuiElementType.Text,42,9,132,16,"REFINING CONDENSER");Add("panel_energy",GuiElementType.Texture,18,35,212,20);Add("meter_energy",GuiElementType.Texture,23,49,202,3);Add("panel_process",GuiElementType.Texture,18,60,212,72);
                Add("text_material",GuiElementType.Text,24,63,54,10,"MATERIAL");Add("text_catalyst",GuiElementType.Text,65,63,54,10,"CATALYST");Add("text_output",GuiElementType.Text,157,63,54,10,"OUTPUT");Add("meter_refining",GuiElementType.Texture,111,115,50,3);Add("panel_info",GuiElementType.Texture,18,138,212,34);break;
            default:return;
        }
        PushUndo();RefreshElementList();RenderLayout();MarkDirty();StatusText.Text=$"Phase 8 scene loaded from {_activeForeverTemplate.ScreenClass}: persistent WYSIWYG overlays + timeline.";
    }
}

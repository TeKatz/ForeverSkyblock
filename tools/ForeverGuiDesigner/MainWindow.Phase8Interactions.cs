using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private GuiElementModel? _phase8LayerDragCandidate;
    private Point _phase8LayerDragStart;
    private bool _phase8InteractionInitialized;

    public void EnableForeverPhase8InteractionTools()
    {
        if (_phase8InteractionInitialized) return;
        _phase8InteractionInitialized = true;

        // Preview handlers run before/after the existing element handlers without
        // replacing the stable drag implementation in MainWindow.xaml.cs.
        PreviewCanvas.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(Phase8CanvasPreviewMouseDown), true);
        PreviewCanvas.AddHandler(UIElement.MouseMoveEvent,
            new MouseEventHandler(Phase8CanvasBubbleMouseMove), true);
        PreviewCanvas.AddHandler(UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler((_, _) => ClearPhase8Guides()), true);

        if (_phase8Hierarchy is not null)
        {
            _phase8Hierarchy.AllowDrop = true;
            _phase8Hierarchy.PreviewMouseLeftButtonDown += Phase8HierarchyMouseDown;
            _phase8Hierarchy.PreviewMouseMove += Phase8HierarchyMouseMove;
            _phase8Hierarchy.Drop += Phase8HierarchyDrop;
        }

        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("POSITION"));
        var h = new Button { Content = "Distribute H", ToolTip = "Distribute selected elements with equal horizontal gaps." };
        h.Click += (_, _) => DistributePhase8Selection(horizontal: true);
        SkyblockBarHost.Children.Add(h);
        var v = new Button { Content = "Distribute V", ToolTip = "Distribute selected elements with equal vertical gaps." };
        v.Click += (_, _) => DistributePhase8Selection(horizontal: false);
        SkyblockBarHost.Children.Add(v);
    }

    private void Phase8CanvasPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        for (DependencyObject? current = e.OriginalSource as DependencyObject;
             current is not null && !ReferenceEquals(current, PreviewCanvas);
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { Tag: GuiElementModel { Locked: true } })
            {
                e.Handled = true;
                StatusText.Text = "Layer is locked. Unlock it in Hierarchy before moving it.";
                return;
            }
        }
    }

    private void Phase8CanvasBubbleMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed || _selection.Count == 0)
            return;

        // The normal drag handler has already applied its pixel snap when this
        // bubbling handler is reached. Add only the small guide correction.
        var left = _selection.Min(item => (double)item.X);
        var top = _selection.Min(item => (double)item.Y);
        var right = _selection.Max(item => (double)(item.X + item.Width));
        var bottom = _selection.Max(item => (double)(item.Y + item.Height));
        var sourceX = new[] { left, (left + right) / 2.0, right };
        var sourceY = new[] { top, (top + bottom) / 2.0, bottom };
        const double threshold = 2.5;

        double? correctionX = null, correctionY = null, guideX = null, guideY = null;
        void ConsiderX(double target)
        {
            foreach (var source in sourceX)
            {
                var delta = target - source;
                if (Math.Abs(delta) <= threshold && (correctionX is null || Math.Abs(delta) < Math.Abs(correctionX.Value)))
                { correctionX = delta; guideX = target; }
            }
        }
        void ConsiderY(double target)
        {
            foreach (var source in sourceY)
            {
                var delta = target - source;
                if (Math.Abs(delta) <= threshold && (correctionY is null || Math.Abs(delta) < Math.Abs(correctionY.Value)))
                { correctionY = delta; guideY = target; }
            }
        }

        foreach (var other in _layout.Elements.Where(item => item.Visible && !_selection.Contains(item)))
        {
            ConsiderX(other.X); ConsiderX(other.X + other.Width / 2.0); ConsiderX(other.X + other.Width);
            ConsiderY(other.Y); ConsiderY(other.Y + other.Height / 2.0); ConsiderY(other.Y + other.Height);
        }
        ConsiderX(_layout.Width / 2.0); ConsiderY(_layout.Height / 2.0);

        var cx = correctionX is null ? 0 : (int)Math.Round(correctionX.Value);
        var cy = correctionY is null ? 0 : (int)Math.Round(correctionY.Value);
        if (cx == 0 && cy == 0) { ClearPhase8Guides(); return; }

        foreach (var item in _selection)
        {
            item.X = Clamp(item.X + cx, 0, _layout.Width - item.Width);
            item.Y = Clamp(item.Y + cy, 0, _layout.Height - item.Height);
            if (_visuals.TryGetValue(item, out var visual))
            {
                Canvas.SetLeft(visual, item.X);
                Canvas.SetTop(visual, item.Y);
            }
        }
        ClearPhase8Guides();
        if (guideX is not null) AddPhase8Guide(guideX.Value, true);
        if (guideY is not null) AddPhase8Guide(guideY.Value, false);
        UpdateInspector();
    }

    private void DistributePhase8Selection(bool horizontal)
    {
        if (_selection.Count < 3) return;
        PushUndo();
        var ordered = horizontal ? _selection.OrderBy(e => e.X).ToList() : _selection.OrderBy(e => e.Y).ToList();
        if (horizontal)
        {
            var first = ordered[0].X;
            var lastRight = ordered[^1].X + ordered[^1].Width;
            var occupied = ordered.Sum(e => e.Width);
            var gap = (lastRight - first - occupied) / (double)(ordered.Count - 1);
            var cursor = (double)first;
            foreach (var item in ordered) { item.X = (int)Math.Round(cursor); cursor += item.Width + gap; }
        }
        else
        {
            var first = ordered[0].Y;
            var lastBottom = ordered[^1].Y + ordered[^1].Height;
            var occupied = ordered.Sum(e => e.Height);
            var gap = (lastBottom - first - occupied) / (double)(ordered.Count - 1);
            var cursor = (double)first;
            foreach (var item in ordered) { item.Y = (int)Math.Round(cursor); cursor += item.Height + gap; }
        }
        RenderLayout(); UpdateInspector(); MarkDirty();
    }

    private void Phase8HierarchyMouseDown(object sender, MouseButtonEventArgs e)
    {
        _phase8LayerDragStart = e.GetPosition(_phase8Hierarchy);
        _phase8LayerDragCandidate = FindPhase8HierarchyElement(e.OriginalSource as DependencyObject);
    }

    private void Phase8HierarchyMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _phase8LayerDragCandidate is null || _phase8Hierarchy is null) return;
        var p = e.GetPosition(_phase8Hierarchy);
        if (Math.Abs(p.X - _phase8LayerDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(p.Y - _phase8LayerDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        DragDrop.DoDragDrop(_phase8Hierarchy, _phase8LayerDragCandidate, DragDropEffects.Move);
        _phase8LayerDragCandidate = null;
    }

    private void Phase8HierarchyDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(GuiElementModel)) || e.Data.GetData(typeof(GuiElementModel)) is not GuiElementModel source) return;
        var target = FindPhase8HierarchyElement(e.OriginalSource as DependencyObject);
        if (target is null || ReferenceEquals(source, target)) return;
        PushUndo();
        var targetIndex = _layout.Elements.IndexOf(target);
        _layout.Elements.Remove(source);
        targetIndex = Math.Clamp(targetIndex, 0, _layout.Elements.Count);
        _layout.Elements.Insert(targetIndex, source);
        source.Group = target.Group;
        RefreshElementList(); RenderLayout(); MarkDirty();
        e.Handled = true;
    }

    private static GuiElementModel? FindPhase8HierarchyElement(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is TreeViewItem { Tag: GuiElementModel model }) return model;
        return null;
    }
}

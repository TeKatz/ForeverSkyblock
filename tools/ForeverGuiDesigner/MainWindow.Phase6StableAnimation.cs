using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private const string StableWashingSceneTag = "__phase6_stable_washing_scene";
    private const string StableWashingMeterTag = "__phase6_stable_washing_meter";

    private DispatcherTimer? _phase6StableAnimationTimer;
    private bool _phase6StableAnimationInitialized;

    public void EnableForeverPhase6StableAnimation()
    {
        if (_phase6StableAnimationInitialized)
            return;

        _phase6StableAnimationInitialized = true;

        // Phase 6 originally rebuilt the whole Washing presentation every 50 ms.
        // Stop that timer completely: repeatedly replacing Border.Child and the
        // generated backdrop invalidates most of the scaled Canvas and visibly
        // flashes on some Windows/WPF render paths.
        _phase6Timer?.Stop();

        _phase6StableAnimationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _phase6StableAnimationTimer.Tick += (_, _) =>
        {
            if (!IsWashingPhase6Active)
                return;

            EnsureStableWashingAnimationVisuals();

            if (_phase6PlayAnimations?.IsChecked == true && WashingRunning)
                _phase6AnimationClock += .05;

            UpdateStableWashingAnimationVisuals();
        };
        _phase6StableAnimationTimer.Start();

        // Progress is shared with the Condenser controls. Updating it must refresh
        // Washing even when the scene is paused/idle, without rebuilding the GUI.
        if (_foreverProgressCombo is not null)
        {
            _foreverProgressCombo.SelectionChanged += (_, _) =>
            {
                if (!IsWashingPhase6Active)
                    return;
                EnsureStableWashingAnimationVisuals();
                UpdateStableWashingAnimationVisuals();
            };
        }

        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.Checked += (_, _) =>
            {
                EnsureStableWashingAnimationVisuals();
                UpdateStableWashingAnimationVisuals();
            };
            _phase6PlayAnimations.Unchecked += (_, _) =>
            {
                EnsureStableWashingAnimationVisuals();
                UpdateStableWashingAnimationVisuals();
            };
        }

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (!IsWashingPhase6Active)
                return;

            // Normal editor operations can rebuild element Borders. Recreate only
            // the two tiny persistent animation surfaces when that actually happened.
            EnsureStableWashingAnimationVisuals();
            UpdateStableWashingAnimationVisuals();
        };
    }

    private void EnsureStableWashingAnimationVisuals()
    {
        if (!IsWashingPhase6Active)
            return;

        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border)
                continue;

            if (string.Equals(pair.Key.Id, "forever_phase6_washing_process_scene", StringComparison.OrdinalIgnoreCase))
            {
                if (border.Child is not Canvas { Tag: StableWashingSceneTag })
                {
                    border.Background = Brushes.Transparent;
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                    border.Child = BuildStableWashingScene(pair.Key.Width, pair.Key.Height);
                }
            }
            else if (string.Equals(pair.Key.Id, "forever_phase6_washing_meter_progress", StringComparison.OrdinalIgnoreCase))
            {
                if (border.Child is not Canvas { Tag: StableWashingMeterTag })
                {
                    border.Background = Brushes.Transparent;
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                    border.Child = BuildStableWashingMeter(pair.Key.Width, pair.Key.Height);
                }
            }
        }
    }

    private Canvas BuildStableWashingScene(int width, int height)
    {
        var canvas = new Canvas
        {
            Tag = StableWashingSceneTag,
            Width = width,
            Height = height,
            ClipToBounds = true,
            IsHitTestVisible = false,
            Background = Brushes.Transparent,
            SnapsToDevicePixels = true
        };

        canvas.Children.Add(new Rectangle
        {
            Width = width,
            Height = Math.Max(1, height - 1),
            Fill = new LinearGradientBrush(Brush("#102431").Color, Brush("#224754").Color, 90),
            IsHitTestVisible = false
        });

        AddStableRect(canvas, 0, Math.Max(0, height - 2), width, 2, "#708E98");
        AddStableRect(canvas, 5, 16, Math.Max(1, width - 10), 2, "#839FA3");
        for (var x = 7; x < width - 6; x += 6)
            AddStableRect(canvas, x, 17, 3, 4, "#294652");

        for (var i = 0; i < 4; i++)
        {
            var ripple = new Rectangle
            {
                Tag = $"ripple:{i}",
                Width = 1,
                Height = 1,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
            canvas.Children.Add(ripple);
        }

        for (var i = 0; i < 5; i++)
        {
            var particle = new Rectangle
            {
                Tag = $"particle:{i}",
                Width = 2,
                Height = 2,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
            canvas.Children.Add(particle);
        }

        AddStableRect(canvas, 0, 0, 2, height, "#527787");
        AddStableRect(canvas, Math.Max(0, width - 2), 0, 2, height, "#527787");
        return canvas;
    }

    private Canvas BuildStableWashingMeter(int width, int height)
    {
        var actualHeight = Math.Max(3, height);
        var canvas = new Canvas
        {
            Tag = StableWashingMeterTag,
            Width = width,
            Height = actualHeight,
            ClipToBounds = true,
            IsHitTestVisible = false,
            Background = Brushes.Transparent,
            SnapsToDevicePixels = true
        };

        AddStableRect(canvas, 0, 0, width, actualHeight, "#07141E");

        var fill = new Rectangle
        {
            Tag = "meter:fill",
            Width = 0,
            Height = actualHeight,
            Fill = Brush("#9DDDF0"),
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        canvas.Children.Add(fill);

        for (var i = -3; i <= 3; i++)
        {
            var shine = new Rectangle
            {
                Tag = $"meter:shine:{i}",
                Width = 1,
                Height = actualHeight,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
            canvas.Children.Add(shine);
        }

        return canvas;
    }

    private void UpdateStableWashingAnimationVisuals()
    {
        if (!IsWashingPhase6Active)
            return;

        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border)
                continue;

            if (string.Equals(pair.Key.Id, "forever_phase6_washing_process_scene", StringComparison.OrdinalIgnoreCase) &&
                border.Child is Canvas { Tag: StableWashingSceneTag } scene)
            {
                UpdateStableWashingScene(scene, pair.Key.Width);
            }
            else if (string.Equals(pair.Key.Id, "forever_phase6_washing_meter_progress", StringComparison.OrdinalIgnoreCase) &&
                     border.Child is Canvas { Tag: StableWashingMeterTag } meter)
            {
                UpdateStableWashingMeter(meter, pair.Key.Width, pair.Key.Height);
            }
        }
    }

    private void UpdateStableWashingScene(Canvas canvas, int width)
    {
        var phase = CurrentProgress;
        if (WashingRunning && _phase6PlayAnimations?.IsChecked == true)
            phase = (phase + _phase6AnimationClock * .14) % 1.0;

        var effect = WashingBlocked ? Brush("#E4B47C").Color : Brush("#9DDDF0").Color;

        foreach (var ripple in canvas.Children.OfType<Rectangle>()
                     .Where(r => r.Tag is string tag && tag.StartsWith("ripple:", StringComparison.Ordinal)))
        {
            var index = int.Parse(((string)ripple.Tag).AsSpan("ripple:".Length));
            var t = (phase * 8 + index * .25) % 1.0;
            var alpha = (byte)Math.Clamp(Math.Round(Math.Sin(Math.PI * t) * 150), 0, 255);
            var radius = 3 + (int)Math.Round(t * (width / 2.0 - 6));

            ripple.Width = Math.Max(1, radius * 2);
            ripple.Fill = new SolidColorBrush(Color.FromArgb(alpha, effect.R, effect.G, effect.B));
            Canvas.SetLeft(ripple, width / 2.0 - radius);
            Canvas.SetTop(ripple, 6 + index * 4);
        }

        foreach (var particle in canvas.Children.OfType<Rectangle>()
                     .Where(r => r.Tag is string tag && tag.StartsWith("particle:", StringComparison.Ordinal)))
        {
            var index = int.Parse(((string)particle.Tag).AsSpan("particle:".Length));
            if (!WashingRunning)
            {
                particle.Visibility = Visibility.Collapsed;
                continue;
            }

            particle.Visibility = Visibility.Visible;
            var t = (phase * 7 + index * .2) % 1.0;
            var strength = Math.Sin(Math.PI * t);
            particle.Fill = new SolidColorBrush(Color.FromArgb(
                (byte)Math.Clamp(Math.Round(strength * 230), 0, 255), 0xE1, 0xD3, 0xAD));
            Canvas.SetLeft(particle, 9 + index * Math.Max(1, width - 18) / 5.0);
            Canvas.SetTop(particle, 2 + t * 21);
        }
    }

    private void UpdateStableWashingMeter(Canvas canvas, int width, int height)
    {
        var actualHeight = Math.Max(3, height);
        var filled = (int)Math.Round(Math.Clamp(CurrentProgress, 0, 1) * width);

        var fill = canvas.Children.OfType<Rectangle>()
            .FirstOrDefault(r => string.Equals(r.Tag as string, "meter:fill", StringComparison.Ordinal));
        if (fill is not null)
        {
            fill.Width = Math.Max(0, filled);
            fill.Height = actualHeight;
            fill.Fill = Brush(WashingBlocked ? "#E4B47C" : "#9DDDF0");
            fill.Visibility = filled > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        var showShine = filled > 0 && WashingRunning && _phase6PlayAnimations?.IsChecked == true;
        var center = (int)Math.Round((_phase6AnimationClock % 2.0) / 2.0 * (filled + 10)) - 5;

        foreach (var shine in canvas.Children.OfType<Rectangle>()
                     .Where(r => r.Tag is string tag && tag.StartsWith("meter:shine:", StringComparison.Ordinal)))
        {
            var offset = int.Parse(((string)shine.Tag).AsSpan("meter:shine:".Length));
            var x = center + offset;
            if (!showShine || x < 0 || x >= filled)
            {
                shine.Visibility = Visibility.Collapsed;
                continue;
            }

            var alpha = (byte)((4 - Math.Abs(offset)) * 24);
            shine.Fill = new SolidColorBrush(Color.FromArgb(alpha, 0xE8, 0xFF, 0xF3));
            shine.Height = actualHeight;
            shine.Visibility = Visibility.Visible;
            Canvas.SetLeft(shine, x);
            Canvas.SetTop(shine, 0);
        }
    }

    private static void AddStableRect(Canvas canvas, double x, double y, double width, double height, string color)
    {
        if (width <= 0 || height <= 0)
            return;

        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brush(color),
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }
}

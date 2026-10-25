using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17 block animation authoring surface. This is deliberately separate from the legacy GUI
/// timeline. Tracks are kept in memory; the existing block model remains the source geometry and
/// the Minecraft runtime MachineAnimation class remains the source of truth for machine curves.
/// </summary>
public partial class MainWindow
{
    private enum StudioAnimationInterpolation
    {
        Step,
        Linear,
        Smooth
    }

    private enum StudioAnimationMachineState
    {
        Idle,
        Active,
        Finished,
        Blocked
    }

    private sealed class StudioBlockAnimationKeyframe
    {
        public double TimeSeconds { get; set; }
        public double[] Position { get; set; } = [0d, 0d, 0d];
        public string RotationAxis { get; set; } = "y";
        public double RotationDegrees { get; set; }
        public double[] Scale { get; set; } = [1d, 1d, 1d];
        public bool Visible { get; set; } = true;
        public string TextureState { get; set; } = string.Empty;
        public string LightStatus { get; set; } = string.Empty;
        public string SpecialEffect { get; set; } = string.Empty;
        public StudioAnimationMachineState MachineState { get; set; } = StudioAnimationMachineState.Idle;
        public StudioAnimationInterpolation Interpolation { get; set; } = StudioAnimationInterpolation.Smooth;

        public StudioBlockAnimationKeyframe Clone()
            => new()
            {
                TimeSeconds = TimeSeconds,
                Position = [.. Position],
                RotationAxis = RotationAxis,
                RotationDegrees = RotationDegrees,
                Scale = [.. Scale],
                Visible = Visible,
                TextureState = TextureState,
                LightStatus = LightStatus,
                SpecialEffect = SpecialEffect,
                MachineState = MachineState,
                Interpolation = Interpolation
            };
    }

    private sealed class StudioBlockAnimationTrack
    {
        public required string ModelKey { get; init; }
        public required int ElementIndex { get; init; }
        public List<StudioBlockAnimationKeyframe> Keyframes { get; } = [];
    }

    private bool _studioAnimationStudioInitialized;
    private FrameworkElement? _studioAnimationSurface;
    private Button? _studioAnimationTab;
    private MinecraftBlockModelViewport? _studioAnimationViewport;
    private ComboBox? _studioAnimationElementCombo;
    private Slider? _studioAnimationTimeline;
    private TextBlock? _studioAnimationTimeText;
    private TextBox? _studioAnimationDurationBox;
    private ComboBox? _studioAnimationSpeedCombo;
    private CheckBox? _studioAnimationLoopCheck;
    private Button? _studioAnimationPlayButton;
    private ListBox? _studioAnimationKeyframeList;
    private TextBox? _studioAnimationTimeBox;
    private readonly TextBox?[] _studioAnimationPositionBoxes = new TextBox?[3];
    private ComboBox? _studioAnimationRotationAxisCombo;
    private TextBox? _studioAnimationRotationAngleBox;
    private readonly TextBox?[] _studioAnimationScaleBoxes = new TextBox?[3];
    private CheckBox? _studioAnimationVisibleCheck;
    private ComboBox? _studioAnimationMachineStateCombo;
    private ComboBox? _studioAnimationInterpolationCombo;
    private TextBox? _studioAnimationTextureStateBox;
    private TextBox? _studioAnimationLightStatusBox;
    private TextBox? _studioAnimationEffectBox;
    private ComboBox? _studioAnimationRuntimeCurveCombo;
    private TextBlock? _studioAnimationRuntimeValueText;
    private TextBlock? _studioAnimationStatusText;
    private DispatcherTimer? _studioAnimationTimer;
    private DateTime _studioAnimationLastTickUtc;
    private bool _studioAnimationPlaying;
    private bool _studioAnimationUiUpdating;
    private double _studioAnimationDurationSeconds = 6;
    private double _studioAnimationSpeed = 1;
    private readonly Dictionary<string, StudioBlockAnimationTrack> _studioAnimationTracks = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverAnimationStudio()
    {
        if (_studioAnimationStudioInitialized || _studioBlockWorkspace is not DockPanel blockWorkspace)
            return;

        var page = blockWorkspace.Children.OfType<Grid>().FirstOrDefault(grid => grid.RowDefinitions.Count == 3);
        var tabs = page?.Children.OfType<StackPanel>().FirstOrDefault(panel => Grid.GetRow(panel) == 1);
        if (page is null || tabs is null)
            return;

        _studioAnimationStudioInitialized = true;
        _studioAnimationTab = StudioButton("ANIMATION", 125);
        _studioAnimationTab.Margin = new Thickness(8, 0, 0, 0);
        _studioAnimationTab.ToolTip = "Phase 17 block animation timeline";
        _studioAnimationTab.Click += (_, _) => ShowStudioAnimationEditor();
        tabs.Children.Add(_studioAnimationTab);

        foreach (var tab in tabs.Children.OfType<Button>().Where(button => !ReferenceEquals(button, _studioAnimationTab)))
            tab.Click += (_, _) => HideStudioAnimationEditor();

        _studioAnimationSurface = BuildStudioAnimationSurface();
        _studioAnimationSurface.Visibility = Visibility.Collapsed;
        Grid.SetRow(_studioAnimationSurface, 2);
        page.Children.Add(_studioAnimationSurface);

        _studioAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _studioAnimationTimer.Tick += StudioAnimationTimer_Tick;

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioAnimationSourceRefresh();
        if (_studioBlockStateCombo is not null)
            _studioBlockStateCombo.SelectionChanged += (_, _) => QueueStudioAnimationSourceRefresh();
        if (_studioBlockFacingCombo is not null)
            _studioBlockFacingCombo.SelectionChanged += (_, _) => QueueStudioAnimationSourceRefresh();
        if (_studioElementList is not null)
            _studioElementList.SelectionChanged += (_, _) => QueueStudioAnimationSourceRefresh(syncSelectionOnly: true);
    }

    private FrameworkElement BuildStudioAnimationSurface()
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });

        var viewportCard = StudioCard();
        viewportCard.Margin = new Thickness(0, 0, 20, 0);
        var viewportGrid = new Grid();
        _studioAnimationViewport = new MinecraftBlockModelViewport();
        viewportGrid.Children.Add(_studioAnimationViewport);

        var badge = new Border
        {
            Background = Brush("#16262B"),
            BorderBrush = Brush("#3E6E73"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14),
            Padding = new Thickness(10, 5, 10, 5),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "BLOCK ANIMATION · PHASE 17",
                Foreground = Brush("#7ED9DB"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            }
        };
        Panel.SetZIndex(badge, 10);
        viewportGrid.Children.Add(badge);

        var resetView = StudioButton("Reset view", 100);
        resetView.HorizontalAlignment = HorizontalAlignment.Right;
        resetView.VerticalAlignment = VerticalAlignment.Top;
        resetView.Margin = new Thickness(14);
        resetView.Click += (_, _) => _studioAnimationViewport?.ResetView();
        Panel.SetZIndex(resetView, 10);
        viewportGrid.Children.Add(resetView);

        var hint = new Border
        {
            Background = Brush("#C0101417"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16),
            Padding = new Thickness(12, 7, 12, 7),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "Scrub timeline or press Play · preview never mutates source JSON",
                Foreground = Brush("#A8B6BD"),
                FontSize = 11
            }
        };
        Panel.SetZIndex(hint, 10);
        viewportGrid.Children.Add(hint);
        viewportCard.Child = viewportGrid;
        Grid.SetColumn(viewportCard, 0);
        content.Children.Add(viewportCard);

        var inspectorCard = StudioCard();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var inspector = new StackPanel { Margin = new Thickness(22) };
        inspector.Children.Add(StudioSectionLabel("BLOCK ANIMATION STUDIO"));
        inspector.Children.Add(new TextBlock
        {
            Text = "A dedicated block timeline. GUI animation remains a separate system. Tracks are currently authoring/preview data only and are not written to project files.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 18)
        });

        inspector.Children.Add(BlockViewerLabel("Animated element"));
        _studioAnimationElementCombo = BlockViewerCombo();
        _studioAnimationElementCombo.SelectionChanged += (_, _) =>
        {
            if (!_studioAnimationUiUpdating)
                RefreshStudioAnimationTrackUi();
        };
        inspector.Children.Add(_studioAnimationElementCombo);

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("TIMELINE"));
        var transport = new WrapPanel { Margin = new Thickness(0, 9, 0, 8) };
        _studioAnimationPlayButton = StudioButton("Play", 66);
        _studioAnimationPlayButton.Height = 30;
        _studioAnimationPlayButton.Margin = new Thickness(0, 0, 6, 5);
        _studioAnimationPlayButton.Click += (_, _) => ToggleStudioAnimationPlayback();
        transport.Children.Add(_studioAnimationPlayButton);
        var stop = StudioButton("Stop", 66);
        stop.Height = 30;
        stop.Margin = new Thickness(0, 0, 8, 5);
        stop.Click += (_, _) => StopStudioAnimation(resetTime: true);
        transport.Children.Add(stop);
        _studioAnimationLoopCheck = new CheckBox
        {
            Content = "Loop",
            IsChecked = true,
            Foreground = Brush("#C7D4D9"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 5)
        };
        transport.Children.Add(_studioAnimationLoopCheck);
        _studioAnimationSpeedCombo = BlockViewerCombo();
        _studioAnimationSpeedCombo.Width = 92;
        _studioAnimationSpeedCombo.ItemsSource = new[] { "0.25×", "0.5×", "1×", "2×", "4×" };
        _studioAnimationSpeedCombo.SelectedItem = "1×";
        _studioAnimationSpeedCombo.SelectionChanged += (_, _) => UpdateStudioAnimationSpeed();
        transport.Children.Add(_studioAnimationSpeedCombo);
        inspector.Children.Add(transport);

        _studioAnimationTimeline = new Slider
        {
            Minimum = 0,
            Maximum = _studioAnimationDurationSeconds,
            Value = 0,
            SmallChange = 0.05,
            LargeChange = 0.5,
            IsSnapToTickEnabled = false,
            Margin = new Thickness(0, 0, 0, 4)
        };
        _studioAnimationTimeline.ValueChanged += (_, _) =>
        {
            if (!_studioAnimationUiUpdating)
                StudioAnimationTimelineChanged();
        };
        inspector.Children.Add(_studioAnimationTimeline);
        _studioAnimationTimeText = new TextBlock
        {
            Text = "0.00s / 6.00s",
            Foreground = Brush("#AFC0C7"),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 10)
        };
        inspector.Children.Add(_studioAnimationTimeText);

        var durationGrid = new Grid();
        durationGrid.ColumnDefinitions.Add(new ColumnDefinition());
        durationGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var durationPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        durationPanel.Children.Add(new TextBlock { Text = "Duration (s)", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        _studioAnimationDurationBox = StudioModelNumberBox();
        _studioAnimationDurationBox.Text = "6";
        _studioAnimationDurationBox.LostFocus += (_, _) => CommitStudioAnimationDuration();
        durationPanel.Children.Add(_studioAnimationDurationBox);
        durationGrid.Children.Add(durationPanel);
        var runtimePanel = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        runtimePanel.Children.Add(new TextBlock { Text = "Runtime curve", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        _studioAnimationRuntimeCurveCombo = BlockViewerCombo();
        _studioAnimationRuntimeCurveCombo.ItemsSource = new[]
        {
            "None", "Press", "Growth", "Harvest", "Intake", "Ingredient", "Product", "Discharge", "Dust 0", "Dust 1", "Dust 2", "Dust 3"
        };
        _studioAnimationRuntimeCurveCombo.SelectedItem = "None";
        _studioAnimationRuntimeCurveCombo.SelectionChanged += (_, _) => UpdateStudioAnimationRuntimeCurveReadout();
        runtimePanel.Children.Add(_studioAnimationRuntimeCurveCombo);
        Grid.SetColumn(runtimePanel, 1);
        durationGrid.Children.Add(runtimePanel);
        inspector.Children.Add(durationGrid);

        _studioAnimationRuntimeValueText = new TextBlock
        {
            Text = "MachineAnimation.java · no runtime curve selected",
            Foreground = Brush("#71848D"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        };
        inspector.Children.Add(_studioAnimationRuntimeValueText);

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("KEYFRAMES"));
        _studioAnimationKeyframeList = new ListBox
        {
            Height = 150,
            Background = Brush("#0D1418"),
            Foreground = Brush("#D9E4E9"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 8, 0, 10)
        };
        _studioAnimationKeyframeList.SelectionChanged += (_, _) => LoadSelectedStudioAnimationKeyframe();
        inspector.Children.Add(_studioAnimationKeyframeList);

        inspector.Children.Add(BlockViewerLabel("Keyframe time (s)"));
        _studioAnimationTimeBox = StudioModelNumberBox();
        _studioAnimationTimeBox.Text = "0";
        inspector.Children.Add(_studioAnimationTimeBox);

        inspector.Children.Add(BlockViewerLabel("Position offset", 12));
        inspector.Children.Add(BuildStudioAnimationVectorEditor(_studioAnimationPositionBoxes, [0d, 0d, 0d]));

        inspector.Children.Add(BlockViewerLabel("Rotation", 12));
        var rotationGrid = new Grid();
        rotationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        rotationGrid.ColumnDefinitions.Add(new ColumnDefinition());
        _studioAnimationRotationAxisCombo = BlockViewerCombo();
        _studioAnimationRotationAxisCombo.ItemsSource = new[] { "x", "y", "z" };
        _studioAnimationRotationAxisCombo.SelectedItem = "y";
        rotationGrid.Children.Add(_studioAnimationRotationAxisCombo);
        _studioAnimationRotationAngleBox = StudioModelNumberBox();
        _studioAnimationRotationAngleBox.Text = "0";
        _studioAnimationRotationAngleBox.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(_studioAnimationRotationAngleBox, 1);
        rotationGrid.Children.Add(_studioAnimationRotationAngleBox);
        inspector.Children.Add(rotationGrid);

        inspector.Children.Add(BlockViewerLabel("Scale", 12));
        inspector.Children.Add(BuildStudioAnimationVectorEditor(_studioAnimationScaleBoxes, [1d, 1d, 1d]));

        var stateGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        stateGrid.ColumnDefinitions.Add(new ColumnDefinition());
        stateGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var visiblePanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        visiblePanel.Children.Add(new TextBlock { Text = "Visibility", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 5) });
        _studioAnimationVisibleCheck = new CheckBox
        {
            Content = "Visible",
            IsChecked = true,
            Foreground = Brush("#C7D4D9")
        };
        visiblePanel.Children.Add(_studioAnimationVisibleCheck);
        stateGrid.Children.Add(visiblePanel);
        var machineStatePanel = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        machineStatePanel.Children.Add(new TextBlock { Text = "Machine state", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        _studioAnimationMachineStateCombo = BlockViewerCombo();
        _studioAnimationMachineStateCombo.ItemsSource = Enum.GetNames<StudioAnimationMachineState>();
        _studioAnimationMachineStateCombo.SelectedItem = StudioAnimationMachineState.Idle.ToString();
        machineStatePanel.Children.Add(_studioAnimationMachineStateCombo);
        Grid.SetColumn(machineStatePanel, 1);
        stateGrid.Children.Add(machineStatePanel);
        inspector.Children.Add(stateGrid);

        inspector.Children.Add(BlockViewerLabel("Texture / state token", 12));
        _studioAnimationTextureStateBox = StudioAnimationTextBox();
        inspector.Children.Add(_studioAnimationTextureStateBox);
        inspector.Children.Add(BlockViewerLabel("Light / status token", 10));
        _studioAnimationLightStatusBox = StudioAnimationTextBox();
        inspector.Children.Add(_studioAnimationLightStatusBox);
        inspector.Children.Add(BlockViewerLabel("Special machine effect", 10));
        _studioAnimationEffectBox = StudioAnimationTextBox();
        inspector.Children.Add(_studioAnimationEffectBox);

        inspector.Children.Add(BlockViewerLabel("Interpolation", 12));
        _studioAnimationInterpolationCombo = BlockViewerCombo();
        _studioAnimationInterpolationCombo.ItemsSource = Enum.GetNames<StudioAnimationInterpolation>();
        _studioAnimationInterpolationCombo.SelectedItem = StudioAnimationInterpolation.Smooth.ToString();
        inspector.Children.Add(_studioAnimationInterpolationCombo);

        var keyframeCommands = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        var upsert = StudioPrimaryButton("ADD / UPDATE", 128);
        upsert.Height = 32;
        upsert.Margin = new Thickness(0, 0, 7, 7);
        upsert.Click += (_, _) => UpsertStudioAnimationKeyframe();
        keyframeCommands.Children.Add(upsert);
        var delete = StudioButton("Delete", 76);
        delete.Height = 32;
        delete.Margin = new Thickness(0, 0, 7, 7);
        delete.Click += (_, _) => DeleteStudioAnimationKeyframe();
        keyframeCommands.Children.Add(delete);
        var clear = StudioButton("Clear track", 100);
        clear.Height = 32;
        clear.Margin = new Thickness(0, 0, 0, 7);
        clear.Click += (_, _) => ClearStudioAnimationTrack();
        keyframeCommands.Children.Add(clear);
        inspector.Children.Add(keyframeCommands);

        inspector.Children.Add(BlockViewerSeparator());
        _studioAnimationStatusText = new TextBlock
        {
            Text = "Open the Animation tab with a block model loaded.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        inspector.Children.Add(_studioAnimationStatusText);

        scroll.Content = inspector;
        inspectorCard.Child = scroll;
        Grid.SetColumn(inspectorCard, 1);
        content.Children.Add(inspectorCard);
        return content;
    }

    private FrameworkElement BuildStudioAnimationVectorEditor(TextBox?[] targets, double[] defaults)
    {
        var grid = new Grid();
        for (var i = 0; i < 3; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        var labels = new[] { "X", "Y", "Z" };
        for (var i = 0; i < 3; i++)
        {
            var panel = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 5, 0, i == 2 ? 0 : 5, 0) };
            panel.Children.Add(new TextBlock { Text = labels[i], Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
            var box = StudioModelNumberBox();
            box.Text = FormatStudioModelNumber(defaults[i]);
            targets[i] = box;
            panel.Children.Add(box);
            Grid.SetColumn(panel, i);
            grid.Children.Add(panel);
        }
        return grid;
    }

    private static TextBox StudioAnimationTextBox()
        => new()
        {
            Height = 31,
            Padding = new Thickness(7, 5, 7, 5),
            Background = Brush("#0D1418"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };

    private void ShowStudioAnimationEditor()
    {
        if (_studioAnimationSurface is null)
            return;

        if (_studioBlockEditorSurface is not null)
            _studioBlockEditorSurface.Visibility = Visibility.Collapsed;
        if (_studioItemEditorSurface is not null)
            _studioItemEditorSurface.Visibility = Visibility.Collapsed;
        if (_studioBlockMachineRow is not null)
            _studioBlockMachineRow.Visibility = Visibility.Visible;
        _studioAnimationSurface.Visibility = Visibility.Visible;
        SetStudioAnimationTabVisual(active: true);
        RefreshStudioAnimationSource();
    }

    private void HideStudioAnimationEditor()
    {
        if (_studioAnimationSurface?.Visibility != Visibility.Visible)
            return;
        StopStudioAnimation(resetTime: false);
        _studioAnimationSurface.Visibility = Visibility.Collapsed;
        SetStudioAnimationTabVisual(active: false);
    }

    private void SetStudioAnimationTabVisual(bool active)
    {
        if (_studioAnimationTab is null)
            return;
        _studioAnimationTab.Background = Brush(active ? "#286F78" : "#1A252B");
        _studioAnimationTab.BorderBrush = Brush(active ? "#58B9C0" : "#35464F");
        _studioAnimationTab.Foreground = Brushes.White;
        if (!active)
            return;
        if (_studioBlockAssetTab is not null)
        {
            _studioBlockAssetTab.Background = Brush("#1A252B");
            _studioBlockAssetTab.BorderBrush = Brush("#35464F");
        }
        if (_studioItemAssetTab is not null)
        {
            _studioItemAssetTab.Background = Brush("#1A252B");
            _studioItemAssetTab.BorderBrush = Brush("#35464F");
        }
    }

    private void QueueStudioAnimationSourceRefresh(bool syncSelectionOnly = false)
    {
        if (!_studioAnimationStudioInitialized)
            return;
        Dispatcher.BeginInvoke(() =>
        {
            if (_studioAnimationSurface?.Visibility != Visibility.Visible)
                return;
            if (syncSelectionOnly && _studioSelectedElementIndex >= 0 && _studioAnimationElementCombo is not null)
            {
                _studioAnimationUiUpdating = true;
                try
                {
                    _studioAnimationElementCombo.SelectedIndex = Math.Min(_studioSelectedElementIndex, _studioAnimationElementCombo.Items.Count - 1);
                }
                finally
                {
                    _studioAnimationUiUpdating = false;
                }
                RefreshStudioAnimationTrackUi();
                return;
            }
            RefreshStudioAnimationSource();
        }, DispatcherPriority.Background);
    }

    private void RefreshStudioAnimationSource()
    {
        StopStudioAnimation(resetTime: false);
        if (_studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
        {
            _studioAnimationViewport?.ClearModel();
            if (_studioAnimationElementCombo is not null)
                _studioAnimationElementCombo.ItemsSource = null;
            SetStudioAnimationStatus("No editable block model is loaded. Select a block asset first.", "#D8B26A");
            return;
        }

        var selected = _studioAnimationElementCombo?.SelectedIndex ?? _studioSelectedElementIndex;
        var entries = _studioEditableBlockModel.Elements
            .Select((element, index) => DescribeStudioBlockElement(element, index))
            .ToList();
        _studioAnimationUiUpdating = true;
        try
        {
            if (_studioAnimationElementCombo is not null)
            {
                _studioAnimationElementCombo.ItemsSource = entries;
                _studioAnimationElementCombo.SelectedIndex = entries.Count == 0 ? -1 : Math.Clamp(selected, 0, entries.Count - 1);
            }
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }

        RenderStudioAnimationFrame(_studioAnimationTimeline?.Value ?? 0);
        RefreshStudioAnimationTrackUi();
        SetStudioAnimationStatus(
            entries.Count == 0
                ? "The selected model has no elements to animate."
                : "Animation preview ready. Keyframes stay in memory and do not touch source JSON.",
            entries.Count == 0 ? "#D8B26A" : "#74C99A");
    }

    private void RefreshStudioAnimationTrackUi()
    {
        var track = GetStudioAnimationTrack(create: false);
        var keyframes = track?.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList() ?? [];
        if (_studioAnimationKeyframeList is not null)
        {
            _studioAnimationUiUpdating = true;
            try
            {
                _studioAnimationKeyframeList.ItemsSource = null;
                _studioAnimationKeyframeList.ItemsSource = keyframes.Select(DescribeStudioAnimationKeyframe).ToList();
                _studioAnimationKeyframeList.SelectedIndex = -1;
            }
            finally
            {
                _studioAnimationUiUpdating = false;
            }
        }
        RenderStudioAnimationFrame(_studioAnimationTimeline?.Value ?? 0);
    }

    private StudioBlockAnimationTrack? GetStudioAnimationTrack(bool create)
    {
        if (_studioEditableBlockModel is null || _studioAnimationElementCombo is null || _studioAnimationElementCombo.SelectedIndex < 0)
            return null;
        var elementIndex = _studioAnimationElementCombo.SelectedIndex;
        var key = $"{_studioEditableBlockModel.SourcePath}|element:{elementIndex}";
        if (_studioAnimationTracks.TryGetValue(key, out var existing))
            return existing;
        if (!create)
            return null;
        var created = new StudioBlockAnimationTrack { ModelKey = key, ElementIndex = elementIndex };
        _studioAnimationTracks[key] = created;
        return created;
    }

    private void UpsertStudioAnimationKeyframe()
    {
        var track = GetStudioAnimationTrack(create: true);
        if (track is null)
        {
            SetStudioAnimationStatus("Select an element before adding a keyframe.", "#E18B8B");
            return;
        }
        if (!TryReadStudioAnimationFrameFields(out var keyframe))
            return;

        var existing = track.Keyframes.FirstOrDefault(candidate => Math.Abs(candidate.TimeSeconds - keyframe.TimeSeconds) < 0.0001);
        if (existing is null)
            track.Keyframes.Add(keyframe);
        else
        {
            var index = track.Keyframes.IndexOf(existing);
            track.Keyframes[index] = keyframe;
        }
        track.Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
        if (keyframe.TimeSeconds > _studioAnimationDurationSeconds)
            SetStudioAnimationDuration(keyframe.TimeSeconds);
        RefreshStudioAnimationTrackUi();
        SetStudioAnimationTimelineTime(keyframe.TimeSeconds);
        SetStudioAnimationStatus($"Keyframe stored at {keyframe.TimeSeconds:0.###}s for element {track.ElementIndex + 1}.", "#74C99A");
    }

    private bool TryReadStudioAnimationFrameFields(out StudioBlockAnimationKeyframe keyframe)
    {
        keyframe = new StudioBlockAnimationKeyframe();
        if (_studioAnimationTimeBox is null || !TryParseStudioModelNumber(_studioAnimationTimeBox.Text, out var time) ||
            !TryReadStudioVector(_studioAnimationPositionBoxes, out var position) ||
            _studioAnimationRotationAngleBox is null || !TryParseStudioModelNumber(_studioAnimationRotationAngleBox.Text, out var angle) ||
            !TryReadStudioVector(_studioAnimationScaleBoxes, out var scale))
        {
            SetStudioAnimationStatus("Invalid keyframe numeric value.", "#E18B8B");
            return false;
        }

        if (!double.IsFinite(time) || !double.IsFinite(angle) || position.Any(value => !double.IsFinite(value)) || scale.Any(value => !double.IsFinite(value)))
        {
            SetStudioAnimationStatus("Animation values must be finite numbers.", "#E18B8B");
            return false;
        }

        keyframe.TimeSeconds = Math.Max(0, time);
        keyframe.Position = position.Select(value => Math.Clamp(value, -64, 64)).ToArray();
        keyframe.RotationAxis = _studioAnimationRotationAxisCombo?.SelectedItem?.ToString()?.ToLowerInvariant() is "x" or "z" ?
            _studioAnimationRotationAxisCombo.SelectedItem!.ToString()!.ToLowerInvariant() : "y";
        keyframe.RotationDegrees = Math.Clamp(angle, -3600, 3600);
        keyframe.Scale = scale.Select(SafeStudioAnimationScale).ToArray();
        keyframe.Visible = _studioAnimationVisibleCheck?.IsChecked != false;
        keyframe.TextureState = _studioAnimationTextureStateBox?.Text?.Trim() ?? string.Empty;
        keyframe.LightStatus = _studioAnimationLightStatusBox?.Text?.Trim() ?? string.Empty;
        keyframe.SpecialEffect = _studioAnimationEffectBox?.Text?.Trim() ?? string.Empty;
        keyframe.MachineState = Enum.TryParse<StudioAnimationMachineState>(_studioAnimationMachineStateCombo?.SelectedItem?.ToString(), out var state)
            ? state
            : StudioAnimationMachineState.Idle;
        keyframe.Interpolation = Enum.TryParse<StudioAnimationInterpolation>(_studioAnimationInterpolationCombo?.SelectedItem?.ToString(), out var interpolation)
            ? interpolation
            : StudioAnimationInterpolation.Smooth;
        return true;
    }

    private static double SafeStudioAnimationScale(double value)
    {
        var clamped = Math.Clamp(value, -8, 8);
        return Math.Abs(clamped) < 0.0001 ? 0.0001 : clamped;
    }

    private void DeleteStudioAnimationKeyframe()
    {
        var track = GetStudioAnimationTrack(create: false);
        if (track is null || _studioAnimationKeyframeList is null || _studioAnimationKeyframeList.SelectedIndex < 0)
            return;
        var ordered = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var selected = _studioAnimationKeyframeList.SelectedIndex;
        if (selected >= ordered.Count)
            return;
        track.Keyframes.Remove(ordered[selected]);
        RefreshStudioAnimationTrackUi();
        SetStudioAnimationStatus("Keyframe removed from the in-memory track.", "#74C99A");
    }

    private void ClearStudioAnimationTrack()
    {
        var track = GetStudioAnimationTrack(create: false);
        if (track is null)
            return;
        track.Keyframes.Clear();
        RefreshStudioAnimationTrackUi();
        SetStudioAnimationStatus("Animation track cleared. Source files are unchanged.", "#74C99A");
    }

    private void LoadSelectedStudioAnimationKeyframe()
    {
        if (_studioAnimationUiUpdating || _studioAnimationKeyframeList is null || _studioAnimationKeyframeList.SelectedIndex < 0)
            return;
        var track = GetStudioAnimationTrack(create: false);
        if (track is null)
            return;
        var ordered = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var index = _studioAnimationKeyframeList.SelectedIndex;
        if (index >= ordered.Count)
            return;
        var keyframe = ordered[index];
        PopulateStudioAnimationFields(keyframe);
        SetStudioAnimationTimelineTime(keyframe.TimeSeconds);
    }

    private void PopulateStudioAnimationFields(StudioBlockAnimationKeyframe keyframe)
    {
        _studioAnimationUiUpdating = true;
        try
        {
            if (_studioAnimationTimeBox is not null)
                _studioAnimationTimeBox.Text = FormatStudioModelNumber(keyframe.TimeSeconds);
            SetStudioVectorBoxes(_studioAnimationPositionBoxes, keyframe.Position);
            SetStudioVectorBoxes(_studioAnimationScaleBoxes, keyframe.Scale);
            if (_studioAnimationRotationAxisCombo is not null)
                _studioAnimationRotationAxisCombo.SelectedItem = keyframe.RotationAxis;
            if (_studioAnimationRotationAngleBox is not null)
                _studioAnimationRotationAngleBox.Text = FormatStudioModelNumber(keyframe.RotationDegrees);
            if (_studioAnimationVisibleCheck is not null)
                _studioAnimationVisibleCheck.IsChecked = keyframe.Visible;
            if (_studioAnimationMachineStateCombo is not null)
                _studioAnimationMachineStateCombo.SelectedItem = keyframe.MachineState.ToString();
            if (_studioAnimationInterpolationCombo is not null)
                _studioAnimationInterpolationCombo.SelectedItem = keyframe.Interpolation.ToString();
            if (_studioAnimationTextureStateBox is not null)
                _studioAnimationTextureStateBox.Text = keyframe.TextureState;
            if (_studioAnimationLightStatusBox is not null)
                _studioAnimationLightStatusBox.Text = keyframe.LightStatus;
            if (_studioAnimationEffectBox is not null)
                _studioAnimationEffectBox.Text = keyframe.SpecialEffect;
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }
    }

    private void StudioAnimationTimelineChanged()
    {
        var time = _studioAnimationTimeline?.Value ?? 0;
        if (_studioAnimationTimeBox is not null && !_studioAnimationTimeBox.IsKeyboardFocusWithin)
            _studioAnimationTimeBox.Text = FormatStudioModelNumber(time);
        UpdateStudioAnimationTimeText();
        UpdateStudioAnimationRuntimeCurveReadout();
        RenderStudioAnimationFrame(time);
    }

    private void SetStudioAnimationTimelineTime(double time)
    {
        if (_studioAnimationTimeline is null)
            return;
        _studioAnimationUiUpdating = true;
        try
        {
            _studioAnimationTimeline.Value = Math.Clamp(time, 0, _studioAnimationDurationSeconds);
            if (_studioAnimationTimeBox is not null)
                _studioAnimationTimeBox.Text = FormatStudioModelNumber(_studioAnimationTimeline.Value);
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }
        UpdateStudioAnimationTimeText();
        UpdateStudioAnimationRuntimeCurveReadout();
        RenderStudioAnimationFrame(_studioAnimationTimeline.Value);
    }

    private void CommitStudioAnimationDuration()
    {
        if (_studioAnimationDurationBox is null || !TryParseStudioModelNumber(_studioAnimationDurationBox.Text, out var duration) || !double.IsFinite(duration))
        {
            if (_studioAnimationDurationBox is not null)
                _studioAnimationDurationBox.Text = FormatStudioModelNumber(_studioAnimationDurationSeconds);
            SetStudioAnimationStatus("Duration must be a finite number.", "#E18B8B");
            return;
        }
        var maxKey = _studioAnimationTracks.Values.SelectMany(track => track.Keyframes).Select(keyframe => keyframe.TimeSeconds).DefaultIfEmpty(0).Max();
        SetStudioAnimationDuration(Math.Max(duration, maxKey));
    }

    private void SetStudioAnimationDuration(double seconds)
    {
        _studioAnimationDurationSeconds = Math.Clamp(seconds, 0.1, 120);
        if (_studioAnimationTimeline is not null)
        {
            _studioAnimationTimeline.Maximum = _studioAnimationDurationSeconds;
            _studioAnimationTimeline.Value = Math.Min(_studioAnimationTimeline.Value, _studioAnimationDurationSeconds);
        }
        if (_studioAnimationDurationBox is not null)
            _studioAnimationDurationBox.Text = FormatStudioModelNumber(_studioAnimationDurationSeconds);
        UpdateStudioAnimationTimeText();
        UpdateStudioAnimationRuntimeCurveReadout();
    }

    private void UpdateStudioAnimationSpeed()
    {
        var text = _studioAnimationSpeedCombo?.SelectedItem?.ToString()?.TrimEnd('×') ?? "1";
        _studioAnimationSpeed = TryParseStudioModelNumber(text, out var speed) ? Math.Clamp(speed, 0.05, 8) : 1;
    }

    private void ToggleStudioAnimationPlayback()
    {
        if (_studioAnimationPlaying)
        {
            _studioAnimationPlaying = false;
            _studioAnimationTimer?.Stop();
            if (_studioAnimationPlayButton is not null)
                _studioAnimationPlayButton.Content = "Play";
            return;
        }
        if (_studioEditableBlockModel is null || _studioAnimationElementCombo is null || _studioAnimationElementCombo.SelectedIndex < 0)
        {
            SetStudioAnimationStatus("Load a block model and select an element before playback.", "#D8B26A");
            return;
        }
        if ((_studioAnimationTimeline?.Value ?? 0) >= _studioAnimationDurationSeconds)
            SetStudioAnimationTimelineTime(0);
        _studioAnimationPlaying = true;
        _studioAnimationLastTickUtc = DateTime.UtcNow;
        if (_studioAnimationPlayButton is not null)
            _studioAnimationPlayButton.Content = "Pause";
        _studioAnimationTimer?.Start();
    }

    private void StudioAnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (!_studioAnimationPlaying || _studioAnimationTimeline is null)
            return;
        var now = DateTime.UtcNow;
        var delta = Math.Clamp((now - _studioAnimationLastTickUtc).TotalSeconds, 0, 0.25) * _studioAnimationSpeed;
        _studioAnimationLastTickUtc = now;
        var next = _studioAnimationTimeline.Value + delta;
        if (next >= _studioAnimationDurationSeconds)
        {
            if (_studioAnimationLoopCheck?.IsChecked == true)
                next %= _studioAnimationDurationSeconds;
            else
            {
                next = _studioAnimationDurationSeconds;
                _studioAnimationPlaying = false;
                _studioAnimationTimer?.Stop();
                if (_studioAnimationPlayButton is not null)
                    _studioAnimationPlayButton.Content = "Play";
            }
        }
        SetStudioAnimationTimelineTime(next);
    }

    private void StopStudioAnimation(bool resetTime)
    {
        _studioAnimationPlaying = false;
        _studioAnimationTimer?.Stop();
        if (_studioAnimationPlayButton is not null)
            _studioAnimationPlayButton.Content = "Play";
        if (resetTime)
            SetStudioAnimationTimelineTime(0);
    }

    private void RenderStudioAnimationFrame(double time)
    {
        if (_studioAnimationViewport is null || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;
        var selectedIndex = _studioAnimationElementCombo?.SelectedIndex ?? -1;
        if (selectedIndex < 0 || selectedIndex >= _studioEditableBlockModel.Elements.Count)
        {
            _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
            _studioAnimationViewport.SetModel(_repoRoot, _studioEditableBlockModel, _studioEditableBlockstateXRotation, _studioEditableBlockstateYRotation);
            return;
        }

        var frame = EvaluateStudioAnimationFrame(GetStudioAnimationTrack(create: false), time);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);
        if (!frame.Visible)
        {
            preview.Elements.RemoveAt(selectedIndex);
        }
        else
        {
            var source = preview.Elements[selectedIndex];
            var center = new[]
            {
                (source.From[0] + source.To[0]) / 2.0,
                (source.From[1] + source.To[1]) / 2.0,
                (source.From[2] + source.To[2]) / 2.0
            };
            var from = new double[3];
            var to = new double[3];
            for (var i = 0; i < 3; i++)
            {
                from[i] = center[i] + (source.From[i] - center[i]) * frame.Scale[i] + frame.Position[i];
                to[i] = center[i] + (source.To[i] - center[i]) * frame.Scale[i] + frame.Position[i];
            }

            MinecraftModelElementRotation? rotation;
            if (Math.Abs(frame.RotationDegrees) < 0.0001)
            {
                rotation = source.Rotation is null ? null : CloneStudioBlockRotation(source.Rotation);
            }
            else if (source.Rotation is not null && source.Rotation.Axis.Equals(frame.RotationAxis, StringComparison.OrdinalIgnoreCase))
            {
                rotation = CloneStudioBlockRotation(source.Rotation);
                rotation.Angle += frame.RotationDegrees;
                for (var i = 0; i < 3; i++)
                    rotation.Origin[i] += frame.Position[i];
            }
            else
            {
                rotation = new MinecraftModelElementRotation
                {
                    Origin = [center[0] + frame.Position[0], center[1] + frame.Position[1], center[2] + frame.Position[2]],
                    Axis = frame.RotationAxis,
                    Angle = frame.RotationDegrees,
                    Rescale = false
                };
            }
            preview.Elements[selectedIndex] = CloneStudioBlockElement(source, from, to, rotation, explicitlyReplaceRotation: true);
        }

        _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
        _studioAnimationViewport.SetModel(_repoRoot, preview, _studioEditableBlockstateXRotation, _studioEditableBlockstateYRotation);
        if (frame.Visible)
            _studioAnimationViewport.SelectElement(selectedIndex);

        var tokens = new List<string> { frame.MachineState.ToString() };
        if (!string.IsNullOrWhiteSpace(frame.TextureState)) tokens.Add($"state={frame.TextureState}");
        if (!string.IsNullOrWhiteSpace(frame.LightStatus)) tokens.Add($"light={frame.LightStatus}");
        if (!string.IsNullOrWhiteSpace(frame.SpecialEffect)) tokens.Add($"fx={frame.SpecialEffect}");
        SetStudioAnimationStatus($"Preview {time:0.00}s · {string.Join(" · ", tokens)}", "#74C99A");
    }

    private static StudioBlockAnimationKeyframe EvaluateStudioAnimationFrame(StudioBlockAnimationTrack? track, double time)
    {
        if (track is null || track.Keyframes.Count == 0)
            return new StudioBlockAnimationKeyframe { TimeSeconds = time };
        var ordered = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        if (time <= ordered[0].TimeSeconds)
            return ordered[0].Clone();
        if (time >= ordered[^1].TimeSeconds)
            return ordered[^1].Clone();

        var rightIndex = ordered.FindIndex(keyframe => keyframe.TimeSeconds >= time);
        if (rightIndex <= 0)
            return ordered[0].Clone();
        var left = ordered[rightIndex - 1];
        var right = ordered[rightIndex];
        var span = Math.Max(0.000001, right.TimeSeconds - left.TimeSeconds);
        var raw = Math.Clamp((time - left.TimeSeconds) / span, 0, 1);
        var amount = left.Interpolation switch
        {
            StudioAnimationInterpolation.Step => 0,
            StudioAnimationInterpolation.Linear => raw,
            _ => raw * raw * (3 - 2 * raw)
        };
        var result = left.Clone();
        result.TimeSeconds = time;
        for (var i = 0; i < 3; i++)
        {
            result.Position[i] = Lerp(left.Position[i], right.Position[i], amount);
            result.Scale[i] = Lerp(left.Scale[i], right.Scale[i], amount);
        }
        result.RotationDegrees = Lerp(left.RotationDegrees, right.RotationDegrees, amount);
        if (raw >= 1)
        {
            result.RotationAxis = right.RotationAxis;
            result.Visible = right.Visible;
            result.TextureState = right.TextureState;
            result.LightStatus = right.LightStatus;
            result.SpecialEffect = right.SpecialEffect;
            result.MachineState = right.MachineState;
        }
        return result;
    }

    private static double Lerp(double from, double to, double amount) => from + (to - from) * amount;

    private static string DescribeStudioAnimationKeyframe(StudioBlockAnimationKeyframe keyframe)
    {
        static string V(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        var visibility = keyframe.Visible ? "visible" : "hidden";
        return $"{keyframe.TimeSeconds:0.###}s · P[{V(keyframe.Position[0])},{V(keyframe.Position[1])},{V(keyframe.Position[2])}] · R{keyframe.RotationAxis.ToUpperInvariant()} {V(keyframe.RotationDegrees)}° · S[{V(keyframe.Scale[0])},{V(keyframe.Scale[1])},{V(keyframe.Scale[2])}] · {visibility} · {keyframe.MachineState} · {keyframe.Interpolation}";
    }

    private void UpdateStudioAnimationTimeText()
    {
        if (_studioAnimationTimeText is not null)
            _studioAnimationTimeText.Text = $"{(_studioAnimationTimeline?.Value ?? 0):0.00}s / {_studioAnimationDurationSeconds:0.00}s";
    }

    private void UpdateStudioAnimationRuntimeCurveReadout()
    {
        if (_studioAnimationRuntimeValueText is null)
            return;
        var curve = _studioAnimationRuntimeCurveCombo?.SelectedItem?.ToString() ?? "None";
        if (curve == "None")
        {
            _studioAnimationRuntimeValueText.Text = "MachineAnimation.java · no runtime curve selected";
            return;
        }
        var phase = Math.Clamp((_studioAnimationTimeline?.Value ?? 0) / Math.Max(0.0001, _studioAnimationDurationSeconds), 0, 1);
        var value = EvaluateStudioMachineAnimationMirror(curve, phase);
        _studioAnimationRuntimeValueText.Text = $"MachineAnimation.java mirror · {curve} · phase {phase:0.000} = {value:0.000}";
    }

    // Read-only preview mirror. Runtime source of truth remains mod/.../client/MachineAnimation.java.
    private static double EvaluateStudioMachineAnimationMirror(string curve, double phase)
        => curve switch
        {
            "Press" => StudioMachineSmooth(.16, .48, phase) * (1 - StudioMachineSmooth(.64, .83, phase)),
            "Growth" => StudioMachineSmooth(.04, .84, phase),
            "Harvest" => StudioMachineSmooth(.86, 1, phase),
            "Intake" => StudioMachineSmooth(.02, .30, phase),
            "Ingredient" => StudioMachineSmooth(0, .06, phase) * (1 - StudioMachineSmooth(.38, .62, phase)),
            "Product" => StudioMachineSmooth(.64, .82, phase),
            "Discharge" => StudioMachineSmooth(.84, 1, phase),
            "Dust 0" => StudioMachineDust(phase, 0),
            "Dust 1" => StudioMachineDust(phase, 1),
            "Dust 2" => StudioMachineDust(phase, 2),
            "Dust 3" => StudioMachineDust(phase, 3),
            _ => 0
        };

    private static double StudioMachineSmooth(double from, double to, double phase)
    {
        var t = Math.Clamp((phase - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static double StudioMachineDust(double phase, int index)
    {
        var fall = (phase * 5 + index * .23) % 1;
        return StudioMachineSmooth(.38, .46, phase) * (1 - StudioMachineSmooth(.76, .84, phase)) * Math.Sin(Math.PI * fall);
    }

    private void SetStudioAnimationStatus(string message, string color)
    {
        if (_studioAnimationStatusText is null)
            return;
        _studioAnimationStatusText.Text = message;
        _studioAnimationStatusText.Foreground = Brush(color);
    }
}

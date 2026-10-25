using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17C.1 runtime-state inspector and preview composer. Runtime states are source-backed and
/// preview-only; they do not mutate Java, blockstate JSON or Studio animation keyframes.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeStateStudioInitialized;
    private bool _studioRuntimeStatePreviewQueued;
    private double _studioRuntimeStatePreviewTime;
    private StackPanel? _studioRuntimeStateControlsPanel;
    private TextBlock? _studioRuntimeStateSummaryText;
    private readonly Dictionary<string, ComboBox> _studioRuntimeStateCombos = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _studioRuntimeStateSelections = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverRuntimeStateStudio()
    {
        if (_studioRuntimeStateStudioInitialized ||
            _studioAnimationRuntimeValueText?.Parent is not StackPanel inspector)
            return;

        _studioRuntimeStateStudioInitialized = true;

        // RuntimeAnimationIntegration remains responsible for source catalog/readout/target registry,
        // but 17C owns frame composition. Turning off its queue prevents two competing runtime scenes
        // while preserving every already-registered profile/readout event handler.
        _studioRuntimeAnimationIntegrationInitialized = false;

        var section = BuildStudioRuntimeStateSection();
        var insertAt = inspector.Children.IndexOf(_studioAnimationRuntimeValueText) + 1;
        inspector.Children.Insert(Math.Clamp(insertAt, 0, inspector.Children.Count), section);

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioRuntimeStateInspectorRefresh();
        if (_studioBlockStateCombo is not null)
            _studioBlockStateCombo.SelectionChanged += (_, _) => QueueStudioRuntimeStatePreview();
        if (_studioBlockFacingCombo is not null)
            _studioBlockFacingCombo.SelectionChanged += (_, _) => QueueStudioRuntimeStatePreview();

        if (_studioAnimationTimeline is not null)
        {
            _studioAnimationTimeline.ValueChanged += (_, _) =>
            {
                // Manual scrubbing: the legacy 17B base render has already run because its handler was
                // registered first. Garden must be restored synchronously to keep its hidden glass from
                // becoming a presented intermediate frame; other machines use the coalesced queue.
                if (_studioAnimationUiUpdating)
                    return;

                if (ResolveCurrentStudioRuntimeAssetId() == "garden_chamber_mark_i")
                    RenderStudioRuntimeStatePreview(_studioAnimationTimeline.Value);
                else
                    QueueStudioRuntimeStatePreview();
            };
        }

        // Playback uses SetStudioAnimationTimelineTime(), which suppresses the slider's legacy handler
        // and then performs a base render manually. This handler is registered after the transport tick,
        // so the semantic runtime state always becomes the final frame for that tick.
        if (_studioAnimationTimer is not null)
            _studioAnimationTimer.Tick += (_, _) => QueueStudioRuntimeStatePreview();

        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                {
                    QueueStudioRuntimeStateInspectorRefresh();
                    QueueStudioRuntimeStatePreview();
                }
            };
        }

        RefreshStudioRuntimeStateInspector();
        QueueStudioRuntimeStatePreview();
    }

    private FrameworkElement BuildStudioRuntimeStateSection()
    {
        var root = new StackPanel();
        root.Children.Add(BlockViewerSeparator());
        root.Children.Add(StudioSectionLabel("RUNTIME STATE · PHASE 17C.1"));
        root.Children.Add(new TextBlock
        {
            Text = "Source-backed machine state. These controls select real renderer predicates; Runtime curve below/above remains read-only inspection and no longer chooses the renderer branch.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 10)
        });

        _studioRuntimeStateControlsPanel = new StackPanel();
        root.Children.Add(_studioRuntimeStateControlsPanel);

        _studioRuntimeStateSummaryText = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 2)
        };
        root.Children.Add(_studioRuntimeStateSummaryText);
        return root;
    }

    private void QueueStudioRuntimeStateInspectorRefresh()
    {
        if (!_studioRuntimeStateStudioInitialized)
            return;

        _ = Dispatcher.BeginInvoke(() =>
        {
            RefreshStudioRuntimeStateInspector();
            QueueStudioRuntimeStatePreview();
        }, DispatcherPriority.ContextIdle);
    }

    private void RefreshStudioRuntimeStateInspector()
    {
        if (_studioRuntimeStateControlsPanel is null)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        _studioRuntimeStateControlsPanel.Children.Clear();
        _studioRuntimeStateCombos.Clear();

        if (!profile.HasStateControls)
        {
            _studioRuntimeStateControlsPanel.Children.Add(new TextBlock
            {
                Text = "No standalone runtime-state controls for this asset.",
                Foreground = Brush("#71848D"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 4)
            });
            if (_studioRuntimeStateSummaryText is not null)
                _studioRuntimeStateSummaryText.Text = profile.SourceNote;
            return;
        }

        foreach (var axis in profile.Axes)
        {
            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var label = new TextBlock
            {
                Text = axis.DisplayName,
                Foreground = Brush("#71848D"),
                FontSize = 9,
                Margin = new Thickness(1, 0, 0, 3),
                ToolTip = axis.Description
            };
            row.Children.Add(label);

            var combo = BlockViewerCombo();
            combo.DisplayMemberPath = nameof(ForeverRuntimeStateOption.DisplayName);
            combo.ItemsSource = axis.Options;
            combo.Tag = axis;

            var key = RuntimeStateSelectionKey(profile.AssetId, axis.Id);
            if (!_studioRuntimeStateSelections.TryGetValue(key, out var selectedId) ||
                !axis.Options.Any(option => option.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase)))
            {
                selectedId = axis.DefaultOptionId;
                _studioRuntimeStateSelections[key] = selectedId;
            }

            combo.SelectedItem = axis.Options.First(option => option.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is not ForeverRuntimeStateOption selected ||
                    combo.Tag is not ForeverRuntimeStateAxis selectedAxis)
                    return;

                _studioRuntimeStateSelections[RuntimeStateSelectionKey(profile.AssetId, selectedAxis.Id)] = selected.Id;
                UpdateStudioRuntimeStateSummary();

                // State changes may legitimately change renderer topology. Render immediately once;
                // subsequent timeline frames return to the in-place mesh-update path.
                RenderStudioRuntimeStatePreview(_studioAnimationTimeline?.Value ?? 0);
            };
            row.Children.Add(combo);
            _studioRuntimeStateControlsPanel.Children.Add(row);
            _studioRuntimeStateCombos[axis.Id] = combo;
        }

        UpdateStudioRuntimeStateSummary();
    }

    private void UpdateStudioRuntimeStateSummary()
    {
        if (_studioRuntimeStateSummaryText is null)
            return;

        var profile = ForeverRuntimeStateCatalog.ForAsset(ResolveCurrentStudioRuntimeAssetId());
        if (!profile.HasStateControls)
        {
            _studioRuntimeStateSummaryText.Text = profile.SourceNote;
            return;
        }

        var values = profile.Axes.Select(axis =>
        {
            var selected = GetStudioRuntimeStateOption(profile, axis);
            return $"{axis.DisplayName}: {selected.DisplayName}";
        });
        _studioRuntimeStateSummaryText.Text = $"{string.Join(" · ", values)}\n{profile.SourceNote}";
    }

    private static string RuntimeStateSelectionKey(string assetId, string axisId)
        => $"{assetId.Trim().ToLowerInvariant()}|{axisId.Trim().ToLowerInvariant()}";

    private ForeverRuntimeStateOption GetStudioRuntimeStateOption(
        ForeverRuntimeStateProfile profile,
        ForeverRuntimeStateAxis axis)
    {
        var key = RuntimeStateSelectionKey(profile.AssetId, axis.Id);
        var id = _studioRuntimeStateSelections.TryGetValue(key, out var selected)
            ? selected
            : axis.DefaultOptionId;
        return axis.Options.FirstOrDefault(option => option.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
               ?? axis.DefaultOption;
    }

    private string GetStudioRuntimeState(string assetId, string axisId, string fallback)
    {
        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        var axis = profile.Axes.FirstOrDefault(candidate => candidate.Id.Equals(axisId, StringComparison.OrdinalIgnoreCase));
        if (axis is null)
            return fallback;
        return GetStudioRuntimeStateOption(profile, axis).Id;
    }

    private void QueueStudioRuntimeStatePreview()
    {
        if (!_studioRuntimeStateStudioInitialized)
            return;

        _studioRuntimeStatePreviewTime = _studioAnimationTimeline?.Value ?? 0;
        if (_studioRuntimeStatePreviewQueued)
            return;

        _studioRuntimeStatePreviewQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioRuntimeStatePreviewQueued = false;
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                RenderStudioRuntimeStatePreview(_studioRuntimeStatePreviewTime);
        }, DispatcherPriority.Render);
    }

    private void RenderStudioRuntimeStatePreview(double time)
    {
        if (_studioAnimationViewport is null || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var phase = Math.Clamp(time / Math.Max(.0001, _studioAnimationDurationSeconds), 0, 1);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);

        var selectedTarget = GetSelectedStudioAnimationTarget();
        if (selectedTarget?.Kind == AnimationTargetKind.JsonModelElement && selectedTarget.ElementIndex is int elementIndex)
        {
            var authoredTrack = FindStudioAnimationTrackForTarget(selectedTarget);
            var frame = EvaluateStudioAnimationFrame(authoredTrack, time);
            ApplyStudioAnimationInteractionFrameToElement(preview, elementIndex, frame);
        }

        AddRuntimeTextureSlots(preview);
        ComposeStudioRuntimeState(preview, assetId, phase);

        if (!_studioAnimationViewport.TryUpdateAnimatedModel(
                _repoRoot,
                preview,
                _studioEditableBlockstateXRotation,
                _studioEditableBlockstateYRotation))
        {
            _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
            _studioAnimationViewport.SetModel(
                _repoRoot,
                preview,
                _studioEditableBlockstateXRotation,
                _studioEditableBlockstateYRotation,
                forceRebuild: true);
        }

        if (selectedTarget?.Kind == AnimationTargetKind.JsonModelElement && selectedTarget.ElementIndex is int selectedIndex)
            _studioAnimationViewport.SelectElement(selectedIndex);
    }

    private void ComposeStudioRuntimeState(MinecraftResolvedBlockModel model, string assetId, double phase)
    {
        switch (assetId)
        {
            case "condenser_mark_i":
                AddStudioRuntimeCondenserState(model, phase, tier: 1);
                break;
            case "condenser_mark_ii":
                AddStudioRuntimeCondenserState(model, phase, tier: 2);
                break;
            case "ascension_chamber":
                AddStudioRuntimeAscensionState(model, phase);
                break;
            case "refining_chamber":
                AddStudioRuntimeRefining(model,
                    GetStudioRuntimeState(assetId, "activity", "running") == "running" ? phase : 0);
                break;
            case "garden_chamber_mark_i":
                AddStudioRuntimeGardenState(model, phase);
                break;
            case "washing_basin":
                AddStudioRuntimeWashingState(model, phase);
                break;
            case "crucible":
                AddStudioRuntimeCrucibleState(model, phase);
                break;
        }
    }

    private void AddStudioRuntimeCondenserState(MinecraftResolvedBlockModel model, double phase, int tier)
    {
        var assetId = tier >= 2 ? "condenser_mark_ii" : "condenser_mark_i";
        var status = GetStudioRuntimeState(assetId, "status", "running");
        var job = GetStudioRuntimeState(assetId, "job", "crushing");
        var attachment = GetStudioRuntimeState(assetId, "attachment", "none");
        var specimen = GetStudioRuntimeState(assetId, "specimen", "oak");
        var running = status == "running";
        var hasGarden = attachment is "garden" or "garden+freezing";
        var hasFreezing = attachment is "freezing" or "garden+freezing";

        if (hasGarden)
            AddStudioRuntimeCondenserGardenState(model, phase, running, specimen != "none");
        else
            AddStudioRuntimeCondenserProcessState(model, phase, running, job, tier);

        AddStudioRuntimeCondenserStatusState(model, phase, status, hasGarden, hasFreezing, assetId);
    }

    private static void AddStudioRuntimeCondenserProcessState(
        MinecraftResolvedBlockModel model,
        double phase,
        bool running,
        string job,
        int tier)
    {
        var hasJob = job != "idle";
        var mixing = job is "mixing" or "cultivating";
        var organic = job == "composting";

        if (mixing || organic)
        {
            AddStudioRuntimeCondenserMixing(model, phase, composting: organic);
        }
        else if (tier >= 2)
        {
            var angle = hasJob ? phase * (job == "milling" ? 1440 : 720) : 0;
            AddRuntimeBox(model, 3, 5.05, 7.15, 1.35, 7.15, 9.25, 3.65, angle, [6.1,8.2,2.5]);
            AddRuntimeBox(model, 3, 8.85, 7.15, 1.35, 10.95, 9.25, 3.65, -angle, [9.9,8.2,2.5]);
            var intake = RuntimeSmooth(.02, .30, phase);
            var ingredient = hasJob ? RuntimeSmooth(0, .06, phase) * (1 - RuntimeSmooth(.38, .62, phase)) : 1;
            var y = hasJob ? 11.8 - 3.1 * intake : 10.6;
            var s = Math.Max(.08, 1.0 * ingredient);
            AddRuntimeBox(model, 5, 8-s,y-s,2.3-s,8+s,y+s,2.3+s,0,[8,y,2.3]);
            if (hasJob)
                AddStudioRuntimeCondenserStateProduct(model, phase, cultivating: false);
        }
        else
        {
            var stroke = hasJob ? RuntimePress(phase) : 0;
            var jaw = 10.4 - 2.6 * stroke;
            AddRuntimeBox(model, 3, 4.6, jaw, 1.25, 11.4, jaw + 1.1, 3.8, 0, [8,8,2.5]);
            AddRuntimeBox(model, 2, 7.3, jaw + 1.1, 1.8, 8.7, 12.5, 3.2, 0, [8,8,2.5]);
            var intake = RuntimeSmooth(.02, .30, phase);
            var ingredient = hasJob ? RuntimeSmooth(0, .06, phase) * (1 - RuntimeSmooth(.38, .62, phase)) : 1;
            var x = hasJob ? 3.8 + 4.2 * intake : 8;
            var s = Math.Max(.08, (2.4 - stroke * .75) * ingredient * .5);
            AddRuntimeBox(model, 5, x-s,6.6-s,1.5-s,x+s,6.6+s,1.5+s,0,[x,6.6,1.5]);
            if (hasJob)
                AddStudioRuntimeCondenserStateProduct(model, phase, cultivating: false);
        }

        // Dust exists only while RUNNING in the Java renderer. Keep four elements stable during a
        // running cycle by collapsing inactive particles instead of changing topology at thresholds.
        if (running)
        {
            for (var i = 0; i < 4; i++)
            {
                var strength = RuntimeSmooth(.38, .46, phase) * (1 - RuntimeSmooth(.76, .84, phase));
                var fall = ((phase * 5 + i * .23) % 1 + 1) % 1;
                strength *= Math.Sin(Math.PI * fall);
                var size = Math.Max(0, .18 * strength);
                var x = 5.8 + i * 1.4;
                var y = 7.2 - 1.7 * fall;
                var material = organic || job == "cultivating" ? 5 : 1;
                AddRuntimeBox(model, material, x,y,1.4,x+size,y+size,1.4+size,0,[x,y,1.4]);
            }
        }
    }

    private static void AddStudioRuntimeCondenserStateProduct(
        MinecraftResolvedBlockModel model,
        double phase,
        bool cultivating)
    {
        var product = cultivating
            ? RuntimeSmooth(.44, .82, phase)
            : RuntimeSmooth(.64, .82, phase);
        var exit = RuntimeSmooth(.84, 1, phase);
        product *= 1 - RuntimeSmooth(.92, 1, phase);
        var startY = cultivating ? 7.4 + product * 1.2 : 5.8;
        var y = startY + (1.85 - startY) * exit;
        var z = 1.35 + 1.35 * exit;
        var s = .675 * product;
        AddRuntimeBox(model, 7, 8-s,y-s,z-s,8+s,y+s,z+s,0,[8,y,z]);
    }

    private static void AddStudioRuntimeCondenserGardenState(
        MinecraftResolvedBlockModel model,
        double phase,
        bool running,
        bool specimenPresent)
    {
        AddRuntimeBox(model,3,4.7,11.3,1.3,11.3,12,3.7,0,[8,11.6,2.5]);
        AddRuntimeBox(model,5,5.2,5.3,1.3,10.8,5.8,3.7,0,[8,5.5,2.5]);

        if (specimenPresent)
        {
            var growth = RuntimeSmooth(.04, .84, phase);
            var h = .25 + 2.6 * growth;
            AddRuntimeBox(model,5,7.82,5.9,2.35,8.18,5.9+h,2.65,0,[8,5.9,2.5]);
        }

        if (!running)
            return;

        var scan = 5 + 5.6 * RuntimeSmooth(.04,.84,phase);
        AddRuntimeBox(model,7,scan,10.7,1.35,scan+.35,11.25,3.65,0,[scan,11,2.5]);
        var harvest = RuntimeSmooth(.86,1,phase);
        var harvestY = 5.9 + harvest * 4.5;
        if (harvest > 0)
            AddRuntimeBox(model,7,5.4,harvestY,1.1,10.6,harvestY+.15,1.3,0,[8,harvestY+.075,1.2]);
        else
            AddRuntimeBox(model,7,8,harvestY,1.2,8,harvestY,1.2,0,[8,harvestY,1.2]);
    }

    private void AddStudioRuntimeCondenserStatusState(
        MinecraftResolvedBlockModel model,
        double phase,
        string status,
        bool hasGarden,
        bool hasFreezing,
        string assetId)
    {
        var running = status == "running";
        var material = status == "blocked" ? 10 : running ? hasGarden ? 7 : 6 : 4;
        for (var i = 0; i < 6; i++)
        {
            var segmentMaterial = running && phase < (i + 1) / 6d ? 4 : material;
            AddRuntimeBox(model,segmentMaterial,4.7+i*1.1,12.2,1.2,5.4+i*1.1,12.5,1.45,0,[8,12.35,1.3]);
        }

        if (!hasFreezing)
            return;

        var progressId = GetStudioRuntimeState(assetId, "freezing-progress", "50");
        var fill = double.TryParse(progressId, out var percent) ? Math.Clamp(percent / 100d, 0, 1) : .5;
        var frost = status == "blocked" ? 10 : 6;
        AddRuntimeBox(model,4,.85,6,.9,1.25,10.3,1.2,0,[1.05,8.15,1.05]);
        var top = 6 + 4.3 * fill;
        if (fill > 0)
            AddRuntimeBox(model,frost,.82,6,.85,1.28,top,1.22,0,[1.05,8.15,1.05]);
        else
            AddRuntimeBox(model,frost,1.05,6,1.035,1.05,6,1.035,0,[1.05,6,1.035]);
    }

    private void AddStudioRuntimeAscensionState(MinecraftResolvedBlockModel model, double phase)
    {
        var active = GetStudioRuntimeState("ascension_chamber", "activity", "active") == "active";
        var corePresent = GetStudioRuntimeState("ascension_chamber", "core", "present") == "present";
        var p = active ? phase : 0;
        var accent = active ? 7 : 4;
        AddRuntimeRing(model, 4.5, 2.8, p * 180, accent);
        AddRuntimeRing(model, 3.15, 1.6, -p * 360, accent);

        if (corePresent)
        {
            var bob = active ? Math.Sin(p * Math.PI * 4) * .18 : 0;
            var tilt = active ? Math.Sin(p * Math.PI * 2) * 9 : 0;
            AddRuntimeBox(model,32,6.6,6.6+bob,1.5,9.4,9.4+bob,3.4,tilt,[8,8+bob,2.5]);
        }

        if (active)
        {
            for (var i = 0; i < 6; i++)
            {
                var angle = p * Math.PI * 4 + i * Math.PI / 3;
                var x = 8 + Math.Cos(angle) * 2.3;
                var y = 8 + Math.Sin(angle) * 2.3;
                AddRuntimeBox(model,7,x,y,2.1,x+.13,y+.13,2.25,0,[x,y,2.1]);
            }
        }
    }

    private void AddStudioRuntimeGardenState(MinecraftResolvedBlockModel model, double phase)
    {
        var linked = GetStudioRuntimeState("garden_chamber_mark_i", "link", "linked") == "linked";
        if (!linked)
            return;

        var status = GetStudioRuntimeState("garden_chamber_mark_i", "status", "running");
        var specimen = GetStudioRuntimeState("garden_chamber_mark_i", "specimen", "oak");
        var specimenPresent = specimen != "none";
        var running = status == "running" && specimenPresent;
        var p = running ? phase : 0;

        PrepareStudioRuntimeGardenVisibility(model);
        var growth = RuntimeSmooth(.04, .84, p);
        var z = 3.1 + 9.4 * growth;
        AddRuntimeBox(model,3,2.1,13.4,z,13.9,13.95,z+.4,0,[8,13.6,z]);
        AddRuntimeBox(model,running ? 7 : 4,3.1,13.15,z+.05,12.9,13.4,z+.35,0,[8,13.3,z]);

        if (specimenPresent)
        {
            var height = .6 + 5.5 * growth;
            // The Garden parity layer expands this stable proxy signature into the exact 31-part
            // GardenTreeShape used by GardenPlantRenderer.
            AddRuntimeBox(model,5,7.75,3.25,7.75,8.25,3.25+height,8.25,0,[8,3.25,8]);
        }

        var statusMaterial = status == "blocked" ? 10 : running ? 7 : 4;
        AddStudioRuntimeGardenStatusStrips(model, statusMaterial);

        if (!running)
            return;

        for (var i = 0; i < 4; i++)
        {
            var fall = ((phase * 12 + i * .25) % 1 + 1) % 1;
            var x = i % 2 == 0 ? 3.5 : 12.2;
            var zz = i < 2 ? 3.5 : 12.2;
            var y = 12.7 - fall * 8.6;
            AddRuntimeBox(model,7,x,y,zz,x+.12,y+.3,zz+.12,0,[x,y,zz]);
        }

        var harvest = RuntimeSmooth(.86,1,phase);
        var sweep = 3.4 + harvest * 8.8;
        if (harvest > 0)
        {
            AddRuntimeBox(model,7,2.7,sweep,2.7,13.3,sweep+.12,2.85,0,[8,sweep,2.8]);
            AddRuntimeBox(model,7,2.7,sweep,13.15,13.3,sweep+.12,13.3,0,[8,sweep,13.2]);
        }
        else
        {
            AddRuntimeBox(model,7,8,sweep,2.775,8,sweep,2.775,0,[8,sweep,2.775]);
            AddRuntimeBox(model,7,8,sweep,13.225,8,sweep,13.225,0,[8,sweep,13.225]);
        }
    }

    private static void AddStudioRuntimeGardenStatusStrips(MinecraftResolvedBlockModel model, int material)
    {
        AddRuntimeBox(model,material,5.8,1.05,.03,10.2,1.40,.13,0,[8,1.2,.08]);
        AddRuntimeBox(model,material,5.8,1.05,15.87,10.2,1.40,15.97,0,[8,1.2,15.92]);
        AddRuntimeBox(model,material,.03,1.05,5.8,.13,1.40,10.2,0,[.08,1.2,8]);
        AddRuntimeBox(model,material,15.87,1.05,5.8,15.97,1.40,10.2,0,[15.92,1.2,8]);
    }

    private void AddStudioRuntimeWashingState(MinecraftResolvedBlockModel model, double phase)
    {
        var amount = GetStudioRuntimeState("washing_basin", "amount", "full");
        if (amount == "dry")
            return;

        var running = GetStudioRuntimeState("washing_basin", "status", "running") == "running";
        AddRuntimeBox(model,4,3.05,9.1,3.05,12.95,9.2,12.95,0,[8,9.15,8]);
        for (var i = 0; i < 3; i++)
        {
            var wave = running
                ? ((phase * 9 + i / 3d) % 1 + 1) % 1
                : (i + 1) / 4d;
            var r = 1 + 3.5 * wave;
            var t = running ? .04 + .09 * Math.Sin(wave * Math.PI) : .06;
            var material = running ? 7 : 4;
            AddRuntimeBox(model,material,8-r,9.22,8-r,8+r,9.25,8-r+t,0,[8,9.23,8]);
            AddRuntimeBox(model,material,8-r,9.22,8+r-t,8+r,9.25,8+r,0,[8,9.23,8]);
            AddRuntimeBox(model,material,8-r,9.22,8-r,8-r+t,9.25,8+r,0,[8,9.23,8]);
            AddRuntimeBox(model,material,8+r-t,9.22,8-r,8+r,9.25,8+r,0,[8,9.23,8]);
        }
    }

    private void AddStudioRuntimeCrucibleState(MinecraftResolvedBlockModel model, double phase)
    {
        var fillId = GetStudioRuntimeState("crucible", "fill", "2");
        if (!int.TryParse(fillId, out var fill) || fill <= 0)
            return;

        fill = Math.Clamp(fill, 1, 4);
        var top = 4.35 + fill * 1.05;
        AddRuntimeBox(model,6,3.1,4.15,3.1,12.9,top,12.9,0,[8,top,8]);
        AddRuntimeBox(model,7,2.95,top,2.95,13.05,top+.14,13.05,0,[8,top,8]);
        for (var i = 0; i < 4; i++)
        {
            var drift = ((phase + i * .25) % 1 + 1) % 1;
            var size = .12 + .28 * Math.Sin(Math.PI * drift);
            var x = 4.2 + i * 2.2;
            var z = 6 + Math.Sin(i * 2.4 + drift * Math.PI * 2) * 2;
            AddRuntimeBox(model,7,x-size,top+.15,z-size,x+size,top+.18,z+size,0,[x,top,z]);
        }
    }
}

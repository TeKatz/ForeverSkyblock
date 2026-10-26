using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17D.1 persistence foundation. Animation data becomes project-backed without generating or
/// mutating Java runtime code. Writes reuse the Phase-16 hash/temporary-file Apply service and only
/// happen after an explicit review confirmation.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPersistenceInitialized;
    private bool _studioAnimationPersistenceLoading;
    private TextBox? _studioAnimationPersistenceIdBox;
    private TextBox? _studioAnimationPersistenceNameBox;
    private TextBlock? _studioAnimationPersistencePathText;
    private TextBlock? _studioAnimationPersistenceStatusText;
    private readonly HashSet<string> _studioAnimationPersistenceDirtyAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _studioAnimationPersistenceLoadedAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ForeverStudioAnimationDocument> _studioAnimationPersistencePending =
        new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverAnimationPersistence()
    {
        if (_studioAnimationPersistenceInitialized || _studioAnimationStatusText?.Parent is not StackPanel inspector)
            return;

        _studioAnimationPersistenceInitialized = true;
        InstallStudioAnimationPersistenceUi(inspector);

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioAnimationPersistenceWorkspaceRefresh();
        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    QueueStudioAnimationPersistenceWorkspaceRefresh();
            };
        }
        if (_studioAnimationLoopCheck is not null)
        {
            _studioAnimationLoopCheck.Checked += (_, _) => MarkStudioAnimationPersistenceDirty();
            _studioAnimationLoopCheck.Unchecked += (_, _) => MarkStudioAnimationPersistenceDirty();
        }

        QueueStudioAnimationPersistenceWorkspaceRefresh();
    }

    private void InstallStudioAnimationPersistenceUi(StackPanel inspector)
    {
        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("PROJECT ANIMATION DATA · PHASE 17D.1"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Versioned project data for the Phase 17A–17C animation model. Save opens Review before writing. Java/runtime source remains untouched.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 10)
        });

        var metadata = new Grid();
        metadata.ColumnDefinitions.Add(new ColumnDefinition());
        metadata.ColumnDefinitions.Add(new ColumnDefinition());

        var idPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        idPanel.Children.Add(new TextBlock { Text = "Animation id", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        _studioAnimationPersistenceIdBox = StudioAnimationTextBox();
        _studioAnimationPersistenceIdBox.Text = "default";
        _studioAnimationPersistenceIdBox.TextChanged += (_, _) => MarkStudioAnimationPersistenceDirty();
        idPanel.Children.Add(_studioAnimationPersistenceIdBox);
        metadata.Children.Add(idPanel);

        var namePanel = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        namePanel.Children.Add(new TextBlock { Text = "Display name", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        _studioAnimationPersistenceNameBox = StudioAnimationTextBox();
        _studioAnimationPersistenceNameBox.Text = "Default Animation";
        _studioAnimationPersistenceNameBox.TextChanged += (_, _) => MarkStudioAnimationPersistenceDirty();
        namePanel.Children.Add(_studioAnimationPersistenceNameBox);
        Grid.SetColumn(namePanel, 1);
        metadata.Children.Add(namePanel);
        inspector.Children.Add(metadata);

        _studioAnimationPersistencePathText = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 9,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 8)
        };
        inspector.Children.Add(_studioAnimationPersistencePathText);

        var commands = new WrapPanel();
        var load = StudioButton("Load project", 106);
        load.Height = 30;
        load.Margin = new Thickness(0, 0, 6, 5);
        load.Click += (_, _) => LoadStudioAnimationPersistenceFromDisk(confirmDirty: true);
        commands.Children.Add(load);

        var validate = StudioButton("Validate", 86);
        validate.Height = 30;
        validate.Margin = new Thickness(0, 0, 6, 5);
        validate.Click += (_, _) => ValidateStudioAnimationPersistence(showDialog: true);
        commands.Children.Add(validate);

        var save = StudioPrimaryButton("SAVE / REVIEW", 126);
        save.Height = 30;
        save.Margin = new Thickness(0, 0, 0, 5);
        save.Click += (_, _) => ReviewAndApplyStudioAnimationPersistence();
        commands.Children.Add(save);
        inspector.Children.Add(commands);

        _studioAnimationPersistenceStatusText = new TextBlock
        {
            Foreground = Brush("#91A3AC"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        inspector.Children.Add(_studioAnimationPersistenceStatusText);
    }

    private string CurrentStudioAnimationPersistenceAssetId()
        => (_studioBlockMachineCombo?.SelectedItem as MinecraftBlockAssetDescriptor)?.Id?.Trim() ?? string.Empty;

    private string CurrentStudioAnimationPersistenceRelativePath()
        => ForeverStudioAnimationPersistence.RelativePathForAsset(CurrentStudioAnimationPersistenceAssetId());

    private string? CurrentStudioAnimationPersistenceFullPath()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || string.IsNullOrWhiteSpace(CurrentStudioAnimationPersistenceAssetId()))
            return null;
        return Path.GetFullPath(Path.Combine(
            _repoRoot,
            CurrentStudioAnimationPersistenceRelativePath().Replace('/', Path.DirectorySeparatorChar)));
    }

    private void QueueStudioAnimationPersistenceWorkspaceRefresh()
    {
        if (!_studioAnimationPersistenceInitialized)
            return;
        _ = Dispatcher.BeginInvoke(RefreshStudioAnimationPersistenceWorkspace, DispatcherPriority.ContextIdle);
    }

    private void RefreshStudioAnimationPersistenceWorkspace()
    {
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        if (_studioAnimationPersistencePathText is not null)
            _studioAnimationPersistencePathText.Text = string.IsNullOrWhiteSpace(assetId)
                ? "No machine selected."
                : $"formatVersion {ForeverStudioAnimationPersistence.CurrentFormatVersion} · {CurrentStudioAnimationPersistenceRelativePath()}";
        if (string.IsNullOrWhiteSpace(assetId) || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
        {
            UpdateStudioAnimationPersistenceStatus("No project animation workspace is available for the current selection.", "#D8B26A");
            return;
        }

        if (_studioAnimationPersistencePending.TryGetValue(assetId, out var pending))
        {
            ApplyStudioAnimationPersistenceDocument(pending, markClean: false);
            UpdateStudioAnimationPersistenceStatus("DIRTY · unsaved in-memory animation restored for this machine.", "#D8B26A");
            return;
        }

        if (_studioAnimationPersistenceLoadedAssets.Contains(assetId))
        {
            UpdateStudioAnimationPersistenceDirtyReadout();
            return;
        }

        var fullPath = CurrentStudioAnimationPersistenceFullPath();
        if (fullPath is not null && File.Exists(fullPath))
        {
            LoadStudioAnimationPersistenceFromDisk(confirmDirty: false);
            return;
        }

        _studioAnimationPersistenceLoading = true;
        try
        {
            _studioAnimationPersistenceIdBox!.Text = "default";
            _studioAnimationPersistenceNameBox!.Text = $"{(_studioBlockMachineCombo?.SelectedItem as MinecraftBlockAssetDescriptor)?.DisplayName ?? assetId} Animation";
        }
        finally
        {
            _studioAnimationPersistenceLoading = false;
        }
        _studioAnimationPersistenceLoadedAssets.Add(assetId);

        if (HasStudioAnimationPersistenceContentForCurrentAsset())
            MarkStudioAnimationPersistenceDirty();
        else
            UpdateStudioAnimationPersistenceStatus("NEW · no project animation file exists yet.", "#91A3AC");
    }

    private bool HasStudioAnimationPersistenceContentForCurrentAsset()
    {
        if (_studioEditableBlockModel is null)
            return false;
        var source = NormalizeStudioAnimationPersistenceModelKey(_studioEditableBlockModel.SourcePath);
        var hasTracks = _studioAnimationTracks.Values.Distinct().Any(track =>
            NormalizeStudioAnimationPersistenceModelKey(track.ModelKey).Equals(source, StringComparison.OrdinalIgnoreCase) &&
            track.Keyframes.Count > 0);
        if (hasTracks)
            return true;
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        var prefix = assetId.ToLowerInvariant() + "|";
        return _studioRuntimeStatusTracks.Any(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && pair.Value.Count > 0) ||
               _studioRuntimeEffectTracks.Any(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && pair.Value.Count > 0);
    }

    private void MarkStudioAnimationPersistenceDirty()
    {
        if (!_studioAnimationPersistenceInitialized || _studioAnimationPersistenceLoading || _studioEditableBlockModel is null)
            return;
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        if (string.IsNullOrWhiteSpace(assetId))
            return;
        _studioAnimationPersistenceDirtyAssets.Add(assetId);
        _studioAnimationPersistencePending[assetId] = BuildCurrentStudioAnimationPersistenceDocument();
        UpdateStudioAnimationPersistenceDirtyReadout();
    }

    private void UpdateStudioAnimationPersistenceDirtyReadout()
    {
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        if (string.IsNullOrWhiteSpace(assetId))
            return;
        if (_studioAnimationPersistenceDirtyAssets.Contains(assetId))
            UpdateStudioAnimationPersistenceStatus("DIRTY · Save / Review will validate the complete document before Apply.", "#D8B26A");
        else
            UpdateStudioAnimationPersistenceStatus("SAVED · in-memory animation matches the last loaded/applied project document.", "#74C99A");
    }

    private void UpdateStudioAnimationPersistenceStatus(string text, string color)
    {
        if (_studioAnimationPersistenceStatusText is null)
            return;
        _studioAnimationPersistenceStatusText.Text = text;
        _studioAnimationPersistenceStatusText.Foreground = Brush(color);
    }

    private ForeverStudioAnimationDocument BuildCurrentStudioAnimationPersistenceDocument()
    {
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        var sourceModel = _studioEditableBlockModel?.SourcePath ?? string.Empty;
        var normalizedSource = NormalizeStudioAnimationPersistenceModelKey(sourceModel);
        var document = new ForeverStudioAnimationDocument
        {
            AssetId = assetId,
            SourceModel = sourceModel,
            Animation = new ForeverStudioAnimationData
            {
                Id = string.IsNullOrWhiteSpace(_studioAnimationPersistenceIdBox?.Text) ? "default" : _studioAnimationPersistenceIdBox.Text.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(_studioAnimationPersistenceNameBox?.Text)
                    ? $"{assetId} Animation"
                    : _studioAnimationPersistenceNameBox.Text.Trim(),
                DurationSeconds = _studioAnimationDurationSeconds,
                Loop = _studioAnimationLoopCheck?.IsChecked == true
            }
        };

        foreach (var track in _studioAnimationTracks.Values.Distinct()
                     .Where(track => NormalizeStudioAnimationPersistenceModelKey(track.ModelKey)
                         .Equals(normalizedSource, StringComparison.OrdinalIgnoreCase)))
        {
            document.Animation.Tracks.Add(new ForeverStudioAnimationTrackData
            {
                TargetId = track.TargetId,
                ModelKey = track.ModelKey,
                ElementIndex = track.ElementIndex,
                Keyframes = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).Select(keyframe =>
                    new ForeverStudioAnimationKeyframeData
                    {
                        TimeSeconds = keyframe.TimeSeconds,
                        Position = [.. keyframe.Position],
                        RotationAxis = keyframe.RotationAxis,
                        RotationDegrees = keyframe.RotationDegrees,
                        Scale = [.. keyframe.Scale],
                        Visible = keyframe.Visible,
                        TextureState = keyframe.TextureState,
                        LightStatus = keyframe.LightStatus,
                        SpecialEffect = keyframe.SpecialEffect,
                        MachineState = keyframe.MachineState.ToString(),
                        Interpolation = keyframe.Interpolation.ToString()
                    }).ToList()
            });
        }

        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        foreach (var axis in profile.Axes)
            document.Animation.RuntimeStateBase[axis.Id] = GetStudioRuntimeStateOption(profile, axis).Id;

        var prefix = assetId.ToLowerInvariant() + "|";
        foreach (var (key, frames) in _studioRuntimeStatusTracks
                     .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && pair.Value.Count > 0)
                     .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            document.Animation.StatusTracks.Add(new ForeverStudioStatusTrackData
            {
                AxisId = key[prefix.Length..],
                Keyframes = frames.OrderBy(frame => frame.TimeSeconds)
                    .Select(frame => new ForeverStudioStatusKeyframeData
                    {
                        TimeSeconds = frame.TimeSeconds,
                        OptionId = frame.OptionId
                    }).ToList()
            });
        }
        foreach (var (key, frames) in _studioRuntimeEffectTracks
                     .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && pair.Value.Count > 0)
                     .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            document.Animation.EffectTracks.Add(new ForeverStudioEffectTrackData
            {
                EffectId = key[prefix.Length..],
                Keyframes = frames.OrderBy(frame => frame.TimeSeconds)
                    .Select(frame => new ForeverStudioEffectKeyframeData
                    {
                        TimeSeconds = frame.TimeSeconds,
                        Mode = frame.Suppressed ? "suppressed" : "source"
                    }).ToList()
            });
        }
        return document;
    }

    private static string NormalizeStudioAnimationPersistenceModelKey(string modelKey)
    {
        var normalized = (modelKey ?? string.Empty).Replace('\\', '/').Trim();
        var marker = normalized.LastIndexOf("|element:", StringComparison.OrdinalIgnoreCase);
        return marker >= 0 ? normalized[..marker] : normalized;
    }

    private void LoadStudioAnimationPersistenceFromDisk(bool confirmDirty)
    {
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        var fullPath = CurrentStudioAnimationPersistenceFullPath();
        if (string.IsNullOrWhiteSpace(assetId) || fullPath is null)
            return;
        if (!File.Exists(fullPath))
        {
            MessageBox.Show(this, $"No persisted animation exists yet:\n\n{CurrentStudioAnimationPersistenceRelativePath()}", "Forever Studio · Animation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (confirmDirty && _studioAnimationPersistenceDirtyAssets.Contains(assetId))
        {
            var replace = MessageBox.Show(
                this,
                "This machine has unsaved animation changes. Reload the project file and discard the current in-memory version?",
                "Forever Studio · Reload Animation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (replace != MessageBoxResult.Yes)
                return;
        }

        ForeverStudioAnimationPersistenceValidation validation;
        try
        {
            validation = ForeverStudioAnimationPersistence.ValidateRoundTrip(File.ReadAllText(fullPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Could not read the animation file:\n\n{ex.Message}", "Forever Studio · Animation", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (validation.Document is null || validation.HasErrors)
        {
            MessageBox.Show(
                this,
                string.Join(Environment.NewLine, validation.Issues.Select(issue => issue.ToString())),
                "Forever Studio · Animation file invalid",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
        if (!validation.Document.AssetId.Equals(assetId, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, $"The animation file belongs to '{validation.Document.AssetId}', not '{assetId}'.", "Forever Studio · Animation", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ApplyStudioAnimationPersistenceDocument(validation.Document, markClean: true);
        _studioAnimationPersistenceLoadedAssets.Add(assetId);
        _studioAnimationPersistenceDirtyAssets.Remove(assetId);
        _studioAnimationPersistencePending.Remove(assetId);
        var diagnostics = BuildStudioAnimationPersistenceTargetIssues(validation.Document);
        var warnings = validation.Issues.Concat(diagnostics).Count(issue => issue.Severity == StudioValidationSeverity.Warning);
        UpdateStudioAnimationPersistenceStatus(
            warnings == 0
                ? "LOADED · roundtrip valid · all persisted targets resolved."
                : $"LOADED · roundtrip valid · {warnings} diagnostic warning(s); data was preserved.",
            warnings == 0 ? "#74C99A" : "#D8B26A");
    }

    private void ApplyStudioAnimationPersistenceDocument(ForeverStudioAnimationDocument document, bool markClean)
    {
        _studioAnimationPersistenceLoading = true;
        try
        {
            var assetId = document.AssetId;
            var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NormalizeStudioAnimationPersistenceModelKey(document.SourceModel)
            };
            if (_studioEditableBlockModel is not null)
                sources.Add(NormalizeStudioAnimationPersistenceModelKey(_studioEditableBlockModel.SourcePath));

            foreach (var key in _studioAnimationTracks.Keys.ToList())
            {
                var track = _studioAnimationTracks[key];
                if (sources.Contains(NormalizeStudioAnimationPersistenceModelKey(track.ModelKey)))
                    _studioAnimationTracks.Remove(key);
            }

            var sequence = 0;
            foreach (var stored in document.Animation.Tracks)
            {
                var track = new StudioBlockAnimationTrack
                {
                    TargetId = string.IsNullOrWhiteSpace(stored.TargetId) ? null : stored.TargetId,
                    ModelKey = stored.ModelKey,
                    ElementIndex = stored.ElementIndex
                };
                foreach (var storedKeyframe in stored.Keyframes)
                {
                    track.Keyframes.Add(new StudioBlockAnimationKeyframe
                    {
                        TimeSeconds = storedKeyframe.TimeSeconds,
                        Position = storedKeyframe.Position is { Length: 3 } ? [.. storedKeyframe.Position] : [0d,0d,0d],
                        RotationAxis = storedKeyframe.RotationAxis is "x" or "z" ? storedKeyframe.RotationAxis : "y",
                        RotationDegrees = storedKeyframe.RotationDegrees,
                        Scale = storedKeyframe.Scale is { Length: 3 } ? [.. storedKeyframe.Scale] : [1d,1d,1d],
                        Visible = storedKeyframe.Visible,
                        TextureState = storedKeyframe.TextureState ?? string.Empty,
                        LightStatus = storedKeyframe.LightStatus ?? string.Empty,
                        SpecialEffect = storedKeyframe.SpecialEffect ?? string.Empty,
                        MachineState = Enum.TryParse<StudioAnimationMachineState>(storedKeyframe.MachineState, out var state) ? state : StudioAnimationMachineState.Idle,
                        Interpolation = Enum.TryParse<StudioAnimationInterpolation>(storedKeyframe.Interpolation, out var interpolation) ? interpolation : StudioAnimationInterpolation.Smooth
                    });
                }
                track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
                var dictionaryKey = !string.IsNullOrWhiteSpace(track.TargetId)
                    ? track.TargetId!
                    : $"legacy:{assetId}:{sequence++}:{track.ModelKey}:{track.ElementIndex}";
                _studioAnimationTracks[dictionaryKey] = track;
            }

            var prefix = assetId.ToLowerInvariant() + "|";
            foreach (var key in _studioRuntimeStateSelections.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                _studioRuntimeStateSelections.Remove(key);
            foreach (var pair in document.Animation.RuntimeStateBase)
                _studioRuntimeStateSelections[RuntimeStateSelectionKey(assetId, pair.Key)] = pair.Value;

            foreach (var key in _studioRuntimeStatusTracks.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                _studioRuntimeStatusTracks.Remove(key);
            foreach (var stored in document.Animation.StatusTracks)
            {
                _studioRuntimeStatusTracks[StudioRuntimeStatusTrackKey(assetId, stored.AxisId)] = stored.Keyframes
                    .OrderBy(frame => frame.TimeSeconds)
                    .Select(frame => new StudioRuntimeStatusKeyframe { TimeSeconds = frame.TimeSeconds, OptionId = frame.OptionId })
                    .ToList();
            }

            foreach (var key in _studioRuntimeEffectTracks.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                _studioRuntimeEffectTracks.Remove(key);
            foreach (var stored in document.Animation.EffectTracks)
            {
                _studioRuntimeEffectTracks[StudioRuntimeEffectTrackKey(assetId, stored.EffectId)] = stored.Keyframes
                    .OrderBy(frame => frame.TimeSeconds)
                    .Select(frame => new StudioRuntimeEffectKeyframe
                    {
                        TimeSeconds = frame.TimeSeconds,
                        Suppressed = frame.Mode.Equals("suppressed", StringComparison.OrdinalIgnoreCase)
                    }).ToList();
            }

            if (_studioAnimationPersistenceIdBox is not null)
                _studioAnimationPersistenceIdBox.Text = document.Animation.Id;
            if (_studioAnimationPersistenceNameBox is not null)
                _studioAnimationPersistenceNameBox.Text = document.Animation.DisplayName;
            SetStudioAnimationDuration(document.Animation.DurationSeconds);
            if (_studioAnimationLoopCheck is not null)
                _studioAnimationLoopCheck.IsChecked = document.Animation.Loop;

            RebuildStudioAnimationTargetRegistry();
            RefreshStudioAnimationTrackUi();
            RefreshStudioRuntimeStateInspector();
            RefreshStudioRuntimeStatusTimelineInspector();
            RefreshStudioRuntimeEffectInspector();
            RefreshStudioMaterialInspector();
            QueueStudioRuntimeStatePreview();
        }
        finally
        {
            _studioAnimationPersistenceLoading = false;
        }

        if (markClean)
        {
            _studioAnimationPersistenceDirtyAssets.Remove(document.AssetId);
            _studioAnimationPersistencePending.Remove(document.AssetId);
        }
    }

    private ForeverStudioAnimationPersistenceValidation ValidateStudioAnimationPersistence(bool showDialog)
    {
        var document = BuildCurrentStudioAnimationPersistenceDocument();
        var initialJson = ForeverStudioAnimationPersistence.Serialize(document);
        var validation = ForeverStudioAnimationPersistence.ValidateRoundTrip(initialJson);
        var targetIssues = BuildStudioAnimationPersistenceTargetIssues(document);
        var all = validation.Issues.Concat(targetIssues).ToList();
        if (showDialog)
        {
            var errors = all.Count(issue => issue.Severity == StudioValidationSeverity.Error);
            var warnings = all.Count(issue => issue.Severity == StudioValidationSeverity.Warning);
            MessageBox.Show(
                this,
                all.Count == 0
                    ? "READY · formatVersion 1 roundtrip is stable and all targets resolve."
                    : $"{errors} error(s) · {warnings} warning(s)\n\n{string.Join(Environment.NewLine, all.Select(issue => issue.ToString()))}",
                "Forever Studio · Animation Validation",
                MessageBoxButton.OK,
                errors > 0 ? MessageBoxImage.Error : warnings > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        return new ForeverStudioAnimationPersistenceValidation(validation.Document, validation.NormalizedJson, all);
    }

    private List<StudioValidationIssue> BuildStudioAnimationPersistenceTargetIssues(ForeverStudioAnimationDocument document)
    {
        var issues = new List<StudioValidationIssue>();
        RebuildStudioAnimationTargetRegistry();
        foreach (var track in document.Animation.Tracks)
        {
            var resolution = _studioAnimationTargetRegistry.Resolve(track.TargetId, track.ModelKey, track.ElementIndex);
            if (resolution.State == AnimationTargetResolutionState.Unresolved)
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Warning,
                    $"Target '{track.TargetId ?? $"{track.ModelKey} @ {track.ElementIndex}"}' is unresolved. Track/keyframes remain preserved and no fallback target will be chosen."));
            }
            else if (resolution.State == AnimationTargetResolutionState.LegacyMigratable)
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Warning,
                    $"Legacy target '{track.ModelKey} @ {track.ElementIndex}' resolves to '{resolution.Target?.TargetId.Value}', but is intentionally not auto-migrated."));
            }

            if (_studioEditableBlockModel is not null)
            {
                foreach (var material in track.Keyframes.Select(keyframe => keyframe.TextureState).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!IsValidStudioMaterialTextureReference(_studioEditableBlockModel, material))
                    {
                        issues.Add(new StudioValidationIssue(
                            StudioValidationSeverity.Warning,
                            $"Material state '{material}' is not a current texture slot and will remain preserved but inactive."));
                    }
                }
            }
        }

        var stateProfile = ForeverRuntimeStateCatalog.ForAsset(document.AssetId);
        foreach (var pair in document.Animation.RuntimeStateBase)
        {
            var axis = stateProfile.Axes.FirstOrDefault(candidate => candidate.Id.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
            if (axis is null)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"Runtime state axis '{pair.Key}' is no longer present in the source-backed catalog."));
            else if (!axis.Options.Any(option => option.Id.Equals(pair.Value, StringComparison.OrdinalIgnoreCase)))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"Runtime state '{pair.Key}={pair.Value}' is no longer a valid source option."));
        }
        foreach (var track in document.Animation.StatusTracks)
        {
            var axis = stateProfile.Axes.FirstOrDefault(candidate => candidate.Id.Equals(track.AxisId, StringComparison.OrdinalIgnoreCase));
            if (axis is null)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"Status axis '{track.AxisId}' is no longer present. Its keyframes remain preserved."));
                continue;
            }
            foreach (var optionId in track.Keyframes.Select(frame => frame.OptionId).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!axis.Options.Any(option => option.Id.Equals(optionId, StringComparison.OrdinalIgnoreCase)))
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"Status value '{track.AxisId}={optionId}' is no longer present in the source catalog."));
            }
        }

        var effectProfile = ForeverRuntimeEffectCatalog.ForAsset(document.AssetId);
        foreach (var track in document.Animation.EffectTracks)
        {
            if (!effectProfile.Effects.Any(effect => effect.Id.Equals(track.EffectId, StringComparison.OrdinalIgnoreCase)))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"Special effect '{track.EffectId}' is no longer present in the source renderer catalog. Its keyframes remain preserved."));
        }
        return issues;
    }

    private void ReviewAndApplyStudioAnimationPersistence()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || _studioEditableBlockModel is null)
            return;

        MarkStudioAnimationPersistenceDirty();
        var validation = ValidateStudioAnimationPersistence(showDialog: false);
        var errors = validation.Issues.Count(issue => issue.Severity == StudioValidationSeverity.Error);
        var warnings = validation.Issues.Count(issue => issue.Severity == StudioValidationSeverity.Warning);
        var fullPath = CurrentStudioAnimationPersistenceFullPath();
        if (fullPath is null)
            return;
        var before = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "(file does not exist yet)";
        var originalHash = File.Exists(fullPath) ? HashStudioAnimationPersistenceFile(fullPath) : "<missing>";
        var change = new StudioReviewChange
        {
            // ApplyChanges only uses this enum for presentation; this dedicated 17D.1 dialog labels
            // the payload explicitly as Animation Data while reusing the established safe writer.
            Kind = StudioChangeKind.Blockstate,
            RelativePath = CurrentStudioAnimationPersistenceRelativePath(),
            FullPath = fullPath,
            OriginalHash = originalHash,
            BeforeText = before,
            AfterText = validation.NormalizedJson,
            Summary = $"Forever Studio Animation · formatVersion {ForeverStudioAnimationPersistence.CurrentFormatVersion}"
        };
        change.Issues.AddRange(validation.Issues);

        var dialog = new Window
        {
            Owner = this,
            Title = "Forever Studio · Animation Save Review",
            Width = 1120,
            Height = 720,
            MinWidth = 860,
            MinHeight = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#0B1013"),
            Foreground = Brushes.White
        };
        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        dialog.Content = root;

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new TextBlock { Text = "ANIMATION DATA · REVIEW / VALIDATE / APPLY", Foreground = Brush("#7ED9DB"), FontWeight = FontWeights.Bold, FontSize = 13 });
        header.Children.Add(new TextBlock
        {
            Text = $"{change.RelativePath}\nformatVersion 1 · {errors} error(s) · {warnings} warning(s)",
            Foreground = Brush(errors > 0 ? "#E18B8B" : warnings > 0 ? "#D8B26A" : "#74C99A"),
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0)
        });
        root.Children.Add(header);

        var tabs = new TabControl { Background = Brush("#0E161A"), Foreground = Brushes.White, BorderBrush = Brush("#35464F") };
        var beforeBox = StudioAnimationPersistenceReviewBox(before);
        var afterBox = StudioAnimationPersistenceReviewBox(validation.NormalizedJson);
        var validationBox = StudioAnimationPersistenceReviewBox(validation.Issues.Count == 0
            ? "READY · roundtrip stable · all targets resolved."
            : string.Join(Environment.NewLine, validation.Issues.Select(issue => issue.ToString())));
        tabs.Items.Add(new TabItem { Header = "BEFORE", Content = beforeBox });
        tabs.Items.Add(new TabItem { Header = "AFTER", Content = afterBox });
        tabs.Items.Add(new TabItem { Header = "VALIDATION", Content = validationBox });
        Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);

        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var apply = StudioPrimaryButton(errors == 0 ? "APPLY SAVE" : "APPLY BLOCKED", 124);
        apply.IsEnabled = errors == 0;
        apply.Margin = new Thickness(0, 0, 8, 0);
        apply.Click += (_, _) =>
        {
            try
            {
                ForeverStudioReviewApplyService.ApplyChanges([change]);
                var assetId = CurrentStudioAnimationPersistenceAssetId();
                _studioAnimationPersistenceDirtyAssets.Remove(assetId);
                _studioAnimationPersistencePending.Remove(assetId);
                _studioAnimationPersistenceLoadedAssets.Add(assetId);
                UpdateStudioAnimationPersistenceStatus("SAVED · validated project animation applied successfully.", "#74C99A");
                dialog.DialogResult = true;
                dialog.Close();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show(dialog, $"Animation save was not applied.\n\n{ex.Message}", "Forever Studio · Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        footer.Children.Add(apply);
        var close = StudioButton("Close", 88);
        close.Click += (_, _) => dialog.Close();
        footer.Children.Add(close);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        dialog.ShowDialog();
    }

    private static TextBox StudioAnimationPersistenceReviewBox(string text)
        => new()
        {
            Text = text,
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brush("#0B1013"),
            Foreground = Brush("#DCE7EC"),
            BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11
        };

    private static string HashStudioAnimationPersistenceFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

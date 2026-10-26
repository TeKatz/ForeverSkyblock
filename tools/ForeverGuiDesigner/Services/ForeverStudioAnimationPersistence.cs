using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public sealed class ForeverStudioAnimationDocument
{
    public int FormatVersion { get; set; } = ForeverStudioAnimationPersistence.CurrentFormatVersion;
    public string AssetId { get; set; } = string.Empty;
    public string SourceModel { get; set; } = string.Empty;
    public ForeverStudioAnimationData Animation { get; set; } = new();
}

public sealed class ForeverStudioAnimationData
{
    public string Id { get; set; } = "default";
    public string DisplayName { get; set; } = "Default Animation";
    public double DurationSeconds { get; set; } = 6;
    public bool Loop { get; set; } = true;
    public List<ForeverStudioAnimationTrackData> Tracks { get; set; } = [];
    public SortedDictionary<string, string> RuntimeStateBase { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ForeverStudioStatusTrackData> StatusTracks { get; set; } = [];
    public List<ForeverStudioEffectTrackData> EffectTracks { get; set; } = [];
}

public sealed class ForeverStudioAnimationTrackData
{
    public string? TargetId { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public int ElementIndex { get; set; } = -1;
    public List<ForeverStudioAnimationKeyframeData> Keyframes { get; set; } = [];
}

public sealed class ForeverStudioAnimationKeyframeData
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
    public string MachineState { get; set; } = "Idle";
    public string Interpolation { get; set; } = "Smooth";
}

public sealed class ForeverStudioStatusTrackData
{
    public string AxisId { get; set; } = string.Empty;
    public List<ForeverStudioStatusKeyframeData> Keyframes { get; set; } = [];
}

public sealed class ForeverStudioStatusKeyframeData
{
    public double TimeSeconds { get; set; }
    public string OptionId { get; set; } = string.Empty;
}

public sealed class ForeverStudioEffectTrackData
{
    public string EffectId { get; set; } = string.Empty;
    public List<ForeverStudioEffectKeyframeData> Keyframes { get; set; } = [];
}

public sealed class ForeverStudioEffectKeyframeData
{
    public double TimeSeconds { get; set; }
    public string Mode { get; set; } = "source";
}

public sealed record ForeverStudioAnimationPersistenceValidation(
    ForeverStudioAnimationDocument? Document,
    string NormalizedJson,
    IReadOnlyList<StudioValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);
}

public static class ForeverStudioAnimationPersistence
{
    public const int CurrentFormatVersion = 1;
    public const string RelativeDirectory = "mod/src/main/resources/data/forever_skyblock/forever_studio/animations";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string RelativePathForAsset(string assetId)
    {
        var safe = string.Concat((assetId ?? string.Empty).Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_'));
        if (string.IsNullOrWhiteSpace(safe))
            safe = "unnamed_asset";
        return $"{RelativeDirectory}/{safe}.json";
    }

    public static string Serialize(ForeverStudioAnimationDocument document)
    {
        Normalize(document);
        return JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
    }

    public static ForeverStudioAnimationPersistenceValidation ValidateRoundTrip(string json)
    {
        var issues = new List<StudioValidationIssue>();
        ForeverStudioAnimationDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ForeverStudioAnimationDocument>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Animation JSON is invalid: {ex.Message}"));
            return new ForeverStudioAnimationPersistenceValidation(null, string.Empty, issues);
        }

        if (document is null)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Animation JSON did not contain a document."));
            return new ForeverStudioAnimationPersistenceValidation(null, string.Empty, issues);
        }

        ValidateDocument(document, issues);
        var normalized = Serialize(document);
        try
        {
            var second = JsonSerializer.Deserialize<ForeverStudioAnimationDocument>(normalized, JsonOptions);
            var secondJson = second is null ? string.Empty : Serialize(second);
            if (second is null || !string.Equals(normalized, secondJson, StringComparison.Ordinal))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    "Animation serialization is not stable across a serialize → deserialize → serialize roundtrip."));
            }
        }
        catch (JsonException ex)
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Animation roundtrip deserialization failed: {ex.Message}"));
        }

        return new ForeverStudioAnimationPersistenceValidation(document, normalized, issues);
    }

    private static void Normalize(ForeverStudioAnimationDocument document)
    {
        document.AssetId = document.AssetId.Trim();
        document.SourceModel = document.SourceModel.Trim().Replace('\\', '/');
        document.Animation.Id = document.Animation.Id.Trim();
        document.Animation.DisplayName = document.Animation.DisplayName.Trim();
        document.Animation.Tracks = document.Animation.Tracks
            .OrderBy(track => track.TargetId ?? track.ModelKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(track => track.ElementIndex)
            .ToList();
        foreach (var track in document.Animation.Tracks)
        {
            track.TargetId = string.IsNullOrWhiteSpace(track.TargetId) ? null : track.TargetId.Trim();
            track.ModelKey = track.ModelKey.Trim().Replace('\\', '/');
            track.Keyframes = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        }
        document.Animation.RuntimeStateBase = new SortedDictionary<string, string>(
            document.Animation.RuntimeStateBase
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value.Trim(), StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        document.Animation.StatusTracks = document.Animation.StatusTracks
            .OrderBy(track => track.AxisId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var track in document.Animation.StatusTracks)
            track.Keyframes = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        document.Animation.EffectTracks = document.Animation.EffectTracks
            .OrderBy(track => track.EffectId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var track in document.Animation.EffectTracks)
            track.Keyframes = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
    }

    private static void ValidateDocument(
        ForeverStudioAnimationDocument document,
        ICollection<StudioValidationIssue> issues)
    {
        if (document.FormatVersion != CurrentFormatVersion)
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Unsupported animation formatVersion {document.FormatVersion}. This Studio build supports version {CurrentFormatVersion}."));
        }
        if (string.IsNullOrWhiteSpace(document.AssetId))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "assetId is required."));
        if (string.IsNullOrWhiteSpace(document.SourceModel))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "sourceModel is required."));
        if (string.IsNullOrWhiteSpace(document.Animation.Id))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "animation.id is required."));
        if (!double.IsFinite(document.Animation.DurationSeconds) || document.Animation.DurationSeconds < .1 || document.Animation.DurationSeconds > 120)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "animation.durationSeconds must be finite and between 0.1 and 120."));

        var stableIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var track in document.Animation.Tracks)
        {
            if (string.IsNullOrWhiteSpace(track.TargetId) && string.IsNullOrWhiteSpace(track.ModelKey))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Every animation track requires targetId or legacy modelKey data."));
            if (!string.IsNullOrWhiteSpace(track.TargetId) && !stableIds.Add(track.TargetId))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Duplicate stable targetId '{track.TargetId}'."));
            if (track.ElementIndex < -1)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Legacy elementIndex cannot be below -1."));

            var times = new HashSet<double>();
            foreach (var keyframe in track.Keyframes)
            {
                if (!ValidateVector(keyframe.Position) || !ValidateVector(keyframe.Scale) ||
                    !double.IsFinite(keyframe.TimeSeconds) || !double.IsFinite(keyframe.RotationDegrees))
                {
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Track '{track.TargetId ?? track.ModelKey}' contains non-finite transform data."));
                    continue;
                }
                if (keyframe.TimeSeconds < 0)
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Track '{track.TargetId ?? track.ModelKey}' contains a negative keyframe time."));
                if (keyframe.TimeSeconds > document.Animation.DurationSeconds + .0001)
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"Track '{track.TargetId ?? track.ModelKey}' has a keyframe beyond animation duration."));
                if (!times.Add(Math.Round(keyframe.TimeSeconds, 6)))
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Track '{track.TargetId ?? track.ModelKey}' contains duplicate keyframe times."));
                if (keyframe.RotationAxis is not ("x" or "y" or "z"))
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Track '{track.TargetId ?? track.ModelKey}' contains invalid rotation axis '{keyframe.RotationAxis}'."));
                if (keyframe.Interpolation is not ("Step" or "Linear" or "Smooth"))
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Track '{track.TargetId ?? track.ModelKey}' contains invalid interpolation '{keyframe.Interpolation}'."));
            }
        }

        ValidateDiscreteTracks(
            document.Animation.StatusTracks.Select(track => (track.AxisId, track.Keyframes.Select(key => key.TimeSeconds))),
            document.Animation.DurationSeconds,
            "status",
            issues);
        ValidateDiscreteTracks(
            document.Animation.EffectTracks.Select(track => (track.EffectId, track.Keyframes.Select(key => key.TimeSeconds))),
            document.Animation.DurationSeconds,
            "effect",
            issues);

        foreach (var effect in document.Animation.EffectTracks.SelectMany(track => track.Keyframes))
        {
            if (effect.Mode is not ("source" or "suppressed"))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Unknown effect mode '{effect.Mode}'."));
        }
    }

    private static void ValidateDiscreteTracks(
        IEnumerable<(string Id, IEnumerable<double> Times)> tracks,
        double duration,
        string kind,
        ICollection<StudioValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, times) in tracks)
        {
            if (string.IsNullOrWhiteSpace(id))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"A {kind} track is missing its source id."));
            else if (!ids.Add(id))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Duplicate {kind} track '{id}'."));

            var seen = new HashSet<double>();
            foreach (var time in times)
            {
                if (!double.IsFinite(time) || time < 0)
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"{kind} track '{id}' contains an invalid keyframe time."));
                else if (time > duration + .0001)
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, $"{kind} track '{id}' has a keyframe beyond animation duration."));
                if (!seen.Add(Math.Round(time, 6)))
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"{kind} track '{id}' contains duplicate keyframe times."));
            }
        }
    }

    private static bool ValidateVector(double[]? values)
        => values is { Length: 3 } && values.All(double.IsFinite);
}

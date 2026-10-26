using System.IO;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Versioned, data-only description of Studio-specific runtime geometry/state for blocks whose
/// BlockEntityRenderer adds geometry that is not present in the normal Minecraft model JSON.
///
/// The contract deliberately contains no executable expressions. Runtime source curves are bounded
/// numeric samples, preview geometry may only reference local forever_skyblock block models, and a
/// malformed descriptor is ignored without taking the ordinary block/model editor down with it.
/// </summary>
internal sealed class ForeverStudioRuntimeBlockMetadata
{
    public int SchemaVersion { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RuntimeOwner { get; set; } = string.Empty;
    public string CoverageNote { get; set; } = string.Empty;
    public List<ForeverStudioRuntimeComponentMetadata> Components { get; set; } = [];
    public List<ForeverStudioRuntimeStateMetadata> States { get; set; } = [];
    public List<ForeverStudioRuntimeSourceTrackMetadata> SourceTracks { get; set; } = [];
}

internal sealed class ForeverStudioRuntimeComponentMetadata
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PreviewModel { get; set; } = string.Empty;
    public double[] Pivot { get; set; } = [8, 8, 8];
    public Dictionary<string, List<string>> VisibleWhen { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class ForeverStudioRuntimeStateMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DefaultOptionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ForeverStudioRuntimeStateOptionMetadata> Options { get; set; } = [];
}

internal sealed class ForeverStudioRuntimeStateOptionMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

internal sealed class ForeverStudioRuntimeSourceTrackMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string SourceSymbol { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Interpolation { get; set; } = "linear";
    public string Description { get; set; } = string.Empty;
    public List<ForeverStudioRuntimeCurvePoint> Keyframes { get; set; } = [];
}

internal sealed class ForeverStudioRuntimeCurvePoint
{
    public double Phase { get; set; }
    public double Value { get; set; }
}

internal static class ForeverStudioRuntimeMetadataCatalog
{
    public const int CurrentSchemaVersion = 1;
    public const string RelativeMetadataDirectory = "mod/src/main/resources/assets/forever_skyblock/forever_studio/blocks";

    private const long MaxMetadataBytes = 1024 * 1024;
    private const int MaxFiles = 512;
    private const int MaxComponents = 128;
    private const int MaxStates = 64;
    private const int MaxOptionsPerState = 128;
    private const int MaxTracks = 128;
    private const int MaxKeyframesPerTrack = 256;

    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32
    };

    private static readonly HashSet<string> SupportedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "translateX", "translateY", "translateZ",
        "rotateX", "rotateY", "rotateZ",
        "scaleX", "scaleY", "scaleZ", "uniformScale", "visibility"
    };

    private static string? _repositoryRoot;
    private static IReadOnlyDictionary<string, ForeverStudioRuntimeBlockMetadata> _blocks =
        new Dictionary<string, ForeverStudioRuntimeBlockMetadata>(StringComparer.OrdinalIgnoreCase);
    private static IReadOnlyList<string> _warnings = [];

    public static IReadOnlyList<string> Warnings
    {
        get
        {
            lock (Gate)
                return [.. _warnings];
        }
    }

    public static IEnumerable<ForeverStudioRuntimeBlockMetadata> AllBlocks
    {
        get
        {
            lock (Gate)
                return _blocks.Values.ToList();
        }
    }

    public static void Configure(string? repoRoot, bool forceReload = false)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            lock (Gate)
            {
                _repositoryRoot = null;
                _blocks = new Dictionary<string, ForeverStudioRuntimeBlockMetadata>(StringComparer.OrdinalIgnoreCase);
                _warnings = [];
            }
            return;
        }

        string normalizedRoot;
        try
        {
            normalizedRoot = Path.GetFullPath(repoRoot.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            lock (Gate)
            {
                _repositoryRoot = null;
                _blocks = new Dictionary<string, ForeverStudioRuntimeBlockMetadata>(StringComparer.OrdinalIgnoreCase);
                _warnings = [$"Runtime metadata repository path is invalid: {ex.Message}"];
            }
            return;
        }

        lock (Gate)
        {
            if (!forceReload && string.Equals(_repositoryRoot, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return;
        }

        var loaded = Load(normalizedRoot, out var warnings);
        lock (Gate)
        {
            _repositoryRoot = normalizedRoot;
            _blocks = loaded;
            _warnings = warnings;
        }
    }

    public static bool TryGetBlock(string? assetId, out ForeverStudioRuntimeBlockMetadata metadata)
    {
        var key = NormalizeId(assetId);
        lock (Gate)
            return _blocks.TryGetValue(key, out metadata!);
    }

    public static bool TryGetRuntimeAnimationProfile(string? assetId, out ForeverRuntimeAnimationProfile profile)
    {
        if (!TryGetBlock(assetId, out var metadata))
        {
            profile = null!;
            return false;
        }

        var tracks = metadata.SourceTracks.Select(track => new ForeverRuntimeAnimationTrack(
            track.Id,
            string.IsNullOrWhiteSpace(track.DisplayName) ? Humanize(track.Id) : track.DisplayName,
            track.Component,
            track.SourceSymbol,
            track.SourcePath,
            ParseKind(track),
            phase => EvaluateTrack(track, phase),
            track.Description)).ToList();

        var componentKeys = metadata.Components.Select(component => component.Key)
            .Concat(tracks.Select(track => track.ComponentKey))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        profile = new ForeverRuntimeAnimationProfile(
            metadata.AssetId,
            metadata.DisplayName,
            string.IsNullOrWhiteSpace(metadata.RuntimeOwner) ? "Project runtime metadata" : metadata.RuntimeOwner,
            componentKeys,
            tracks,
            string.IsNullOrWhiteSpace(metadata.CoverageNote)
                ? $"Project runtime metadata v{metadata.SchemaVersion}."
                : metadata.CoverageNote);
        return true;
    }

    public static bool TryGetRuntimeStateProfile(string? assetId, out ForeverRuntimeStateProfile profile)
    {
        if (!TryGetBlock(assetId, out var metadata))
        {
            profile = null!;
            return false;
        }

        var axes = metadata.States.Select(state => new ForeverRuntimeStateAxis(
            state.Id,
            state.DisplayName,
            state.DefaultOptionId,
            state.Options.Select(option => new ForeverRuntimeStateOption(
                option.Id,
                option.DisplayName,
                option.Description)).ToList(),
            state.Description)).ToList();

        profile = new ForeverRuntimeStateProfile(
            metadata.AssetId,
            metadata.DisplayName,
            axes,
            string.IsNullOrWhiteSpace(metadata.CoverageNote)
                ? $"Project runtime metadata v{metadata.SchemaVersion}."
                : metadata.CoverageNote);
        return true;
    }

    public static double EvaluateTrack(ForeverStudioRuntimeSourceTrackMetadata track, double phase)
    {
        if (track.Keyframes.Count == 0)
            return DefaultChannelValue(track.Channel);

        var points = track.Keyframes.OrderBy(point => point.Phase).ToList();
        var p = Math.Clamp(phase, 0d, 1d);
        if (p <= points[0].Phase)
            return points[0].Value;
        if (p >= points[^1].Phase)
            return points[^1].Value;

        for (var index = 1; index < points.Count; index++)
        {
            var right = points[index];
            if (p > right.Phase)
                continue;

            var left = points[index - 1];
            var width = Math.Max(0.0000001, right.Phase - left.Phase);
            var t = Math.Clamp((p - left.Phase) / width, 0d, 1d);
            if (track.Interpolation.Equals("step", StringComparison.OrdinalIgnoreCase))
                return left.Value;
            if (track.Interpolation.Equals("smooth", StringComparison.OrdinalIgnoreCase))
                t = t * t * (3d - 2d * t);
            return left.Value + (right.Value - left.Value) * t;
        }

        return points[^1].Value;
    }

    public static bool IsComponentVisible(
        ForeverStudioRuntimeComponentMetadata component,
        Func<string, string?> stateResolver)
    {
        foreach (var condition in component.VisibleWhen)
        {
            var selected = stateResolver(condition.Key) ?? string.Empty;
            if (condition.Value.Count == 0 ||
                !condition.Value.Any(option => option.Equals(selected, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }

    private static IReadOnlyDictionary<string, ForeverStudioRuntimeBlockMetadata> Load(
        string repoRoot,
        out IReadOnlyList<string> warnings)
    {
        var warningList = new List<string>();
        var result = new Dictionary<string, ForeverStudioRuntimeBlockMetadata>(StringComparer.OrdinalIgnoreCase);
        var root = Path.Combine(repoRoot, RelativeMetadataDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root))
        {
            warnings = warningList;
            return result;
        }

        IEnumerable<string> paths;
        try
        {
            paths = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(MaxFiles + 1)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warnings = [$"Runtime metadata directory cannot be read: {ex.Message}"];
            return result;
        }

        var pathList = paths.ToList();
        if (pathList.Count > MaxFiles)
        {
            warningList.Add($"Runtime metadata contains more than {MaxFiles} files. Only the first {MaxFiles} are read.");
            pathList = pathList.Take(MaxFiles).ToList();
        }

        foreach (var path in pathList)
        {
            var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            try
            {
                var info = new FileInfo(path);
                if (info.Length > MaxMetadataBytes)
                {
                    warningList.Add($"{relative}: descriptor exceeds the {MaxMetadataBytes / 1024} KiB safety limit and was ignored.");
                    continue;
                }

                var metadata = JsonSerializer.Deserialize<ForeverStudioRuntimeBlockMetadata>(File.ReadAllText(path), JsonOptions);
                if (metadata is null)
                {
                    warningList.Add($"{relative}: descriptor is empty and was ignored.");
                    continue;
                }

                if (!ValidateAndNormalize(metadata, relative, warningList))
                    continue;

                if (result.ContainsKey(metadata.AssetId))
                {
                    warningList.Add($"{relative}: duplicate assetId '{metadata.AssetId}' was ignored; one descriptor per block is allowed.");
                    continue;
                }
                result[metadata.AssetId] = metadata;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                warningList.Add($"{relative}: {ex.Message}");
            }
        }

        warnings = warningList;
        return result;
    }

    private static bool ValidateAndNormalize(
        ForeverStudioRuntimeBlockMetadata metadata,
        string relativePath,
        List<string> warnings)
    {
        if (metadata.SchemaVersion != CurrentSchemaVersion)
        {
            warnings.Add($"{relativePath}: unsupported schemaVersion {metadata.SchemaVersion}; supported version is {CurrentSchemaVersion}.");
            return false;
        }

        metadata.AssetId = NormalizeId(metadata.AssetId);
        if (!IsSafeId(metadata.AssetId))
        {
            warnings.Add($"{relativePath}: assetId is missing or contains unsupported characters.");
            return false;
        }

        metadata.DisplayName = string.IsNullOrWhiteSpace(metadata.DisplayName)
            ? Humanize(metadata.AssetId)
            : metadata.DisplayName.Trim();
        metadata.RuntimeOwner = metadata.RuntimeOwner?.Trim() ?? string.Empty;
        metadata.CoverageNote = metadata.CoverageNote?.Trim() ?? string.Empty;
        metadata.Components ??= [];
        metadata.States ??= [];
        metadata.SourceTracks ??= [];

        if (metadata.Components.Count > MaxComponents || metadata.States.Count > MaxStates || metadata.SourceTracks.Count > MaxTracks)
        {
            warnings.Add($"{relativePath}: descriptor exceeds a collection safety limit and was ignored.");
            return false;
        }

        var componentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in metadata.Components)
        {
            component.Key = NormalizeId(component.Key);
            if (!IsSafeId(component.Key) || !componentKeys.Add(component.Key))
            {
                warnings.Add($"{relativePath}: runtime component keys must be unique safe IDs.");
                return false;
            }
            component.DisplayName = string.IsNullOrWhiteSpace(component.DisplayName)
                ? Humanize(component.Key)
                : component.DisplayName.Trim();
            component.Pivot = NormalizeVector(component.Pivot, [8, 8, 8]);
            component.VisibleWhen = NormalizeConditions(component.VisibleWhen);

            if (!string.IsNullOrWhiteSpace(component.PreviewModel))
            {
                var model = NormalizePreviewModel(component.PreviewModel);
                if (model is null)
                {
                    warnings.Add($"{relativePath}: component '{component.Key}' has an unsafe/non-local previewModel; the target remains available without preview geometry.");
                    component.PreviewModel = string.Empty;
                }
                else
                {
                    component.PreviewModel = model;
                }
            }
        }

        var stateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in metadata.States)
        {
            state.Id = NormalizeId(state.Id);
            state.Options ??= [];
            if (!IsSafeId(state.Id) || !stateIds.Add(state.Id) || state.Options.Count == 0 || state.Options.Count > MaxOptionsPerState)
            {
                warnings.Add($"{relativePath}: state axes must have a unique safe ID and 1..{MaxOptionsPerState} options.");
                return false;
            }

            state.DisplayName = string.IsNullOrWhiteSpace(state.DisplayName) ? Humanize(state.Id) : state.DisplayName.Trim();
            state.Description = state.Description?.Trim() ?? string.Empty;
            var optionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in state.Options)
            {
                option.Id = NormalizeId(option.Id);
                if (!IsSafeId(option.Id) || !optionIds.Add(option.Id))
                {
                    warnings.Add($"{relativePath}: options in state '{state.Id}' must have unique safe IDs.");
                    return false;
                }
                option.DisplayName = string.IsNullOrWhiteSpace(option.DisplayName) ? Humanize(option.Id) : option.DisplayName.Trim();
                option.Description = option.Description?.Trim() ?? string.Empty;
            }

            state.DefaultOptionId = NormalizeId(state.DefaultOptionId);
            if (!optionIds.Contains(state.DefaultOptionId))
            {
                warnings.Add($"{relativePath}: defaultOptionId '{state.DefaultOptionId}' is not an option of state '{state.Id}'.");
                return false;
            }
        }

        foreach (var component in metadata.Components)
        {
            foreach (var condition in component.VisibleWhen)
            {
                var state = metadata.States.FirstOrDefault(candidate => candidate.Id.Equals(condition.Key, StringComparison.OrdinalIgnoreCase));
                if (state is null || condition.Value.Any(option => !state.Options.Any(candidate => candidate.Id.Equals(option, StringComparison.OrdinalIgnoreCase))))
                {
                    warnings.Add($"{relativePath}: component '{component.Key}' has a visibleWhen condition that references an unknown state/option.");
                    return false;
                }
            }
        }

        var trackIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var track in metadata.SourceTracks)
        {
            track.Id = NormalizeId(track.Id);
            track.Component = NormalizeId(track.Component);
            track.Channel = track.Channel?.Trim() ?? string.Empty;
            track.Interpolation = string.IsNullOrWhiteSpace(track.Interpolation) ? "linear" : track.Interpolation.Trim().ToLowerInvariant();
            track.Keyframes ??= [];

            if (!IsSafeId(track.Id) || !trackIds.Add(track.Id) || !IsSafeId(track.Component) ||
                !SupportedChannels.Contains(track.Channel) ||
                track.Keyframes.Count == 0 || track.Keyframes.Count > MaxKeyframesPerTrack ||
                track.Interpolation is not ("linear" or "smooth" or "step"))
            {
                warnings.Add($"{relativePath}: source track '{track.Id}' is invalid or exceeds safety limits.");
                return false;
            }

            if (!componentKeys.Contains(track.Component))
            {
                warnings.Add($"{relativePath}: source track '{track.Id}' references unknown component '{track.Component}'.");
                return false;
            }

            foreach (var point in track.Keyframes)
            {
                if (!double.IsFinite(point.Phase) || !double.IsFinite(point.Value) || point.Phase < 0 || point.Phase > 1)
                {
                    warnings.Add($"{relativePath}: source track '{track.Id}' contains a non-finite value or phase outside 0..1.");
                    return false;
                }
            }
            track.Keyframes = track.Keyframes.OrderBy(point => point.Phase).ToList();
            track.DisplayName = string.IsNullOrWhiteSpace(track.DisplayName) ? Humanize(track.Id) : track.DisplayName.Trim();
            track.SourceSymbol = track.SourceSymbol?.Trim() ?? string.Empty;
            track.SourcePath = track.SourcePath?.Trim().Replace('\\', '/') ?? string.Empty;
            track.Description = track.Description?.Trim() ?? string.Empty;
        }

        return true;
    }

    private static ForeverRuntimeAnimationKind ParseKind(ForeverStudioRuntimeSourceTrackMetadata track)
    {
        if (Enum.TryParse<ForeverRuntimeAnimationKind>(track.Kind, true, out var explicitKind))
            return explicitKind;

        if (track.Channel.StartsWith("rotate", StringComparison.OrdinalIgnoreCase))
            return ForeverRuntimeAnimationKind.Rotation;
        if (track.Channel.StartsWith("translate", StringComparison.OrdinalIgnoreCase))
            return ForeverRuntimeAnimationKind.Translation;
        if (track.Channel.StartsWith("scale", StringComparison.OrdinalIgnoreCase) ||
            track.Channel.Equals("uniformScale", StringComparison.OrdinalIgnoreCase))
            return ForeverRuntimeAnimationKind.Scale;
        if (track.Channel.Equals("visibility", StringComparison.OrdinalIgnoreCase))
            return ForeverRuntimeAnimationKind.Effect;
        return ForeverRuntimeAnimationKind.Procedural;
    }

    private static Dictionary<string, List<string>> NormalizeConditions(Dictionary<string, List<string>>? source)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (source is null)
            return result;
        foreach (var pair in source)
        {
            var key = NormalizeId(pair.Key);
            if (!IsSafeId(key))
                continue;
            result[key] = (pair.Value ?? []).Select(NormalizeId).Where(IsSafeId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        return result;
    }

    private static double[] NormalizeVector(double[]? value, double[] fallback)
    {
        if (value is not { Length: 3 } || value.Any(number => !double.IsFinite(number) || number < -4096 || number > 4096))
            return [.. fallback];
        return [.. value];
    }

    private static string? NormalizePreviewModel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Replace('\\', '/');
        if (!normalized.Contains(':'))
            normalized = $"forever_skyblock:{normalized}";
        if (!normalized.StartsWith("forever_skyblock:block/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("..", StringComparison.Ordinal) ||
            normalized.Any(char.IsControl))
            return null;
        return normalized;
    }

    private static string NormalizeId(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsSafeId(string value)
        => value.Length is > 0 and <= 96 &&
           value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');

    private static string Humanize(string value)
        => string.Join(' ', value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]));

    private static double DefaultChannelValue(string channel)
        => channel.StartsWith("scale", StringComparison.OrdinalIgnoreCase) ||
           channel.Equals("uniformScale", StringComparison.OrdinalIgnoreCase) ||
           channel.Equals("visibility", StringComparison.OrdinalIgnoreCase)
            ? 1d
            : 0d;
}

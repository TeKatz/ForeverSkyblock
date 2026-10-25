using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ForeverGuiDesigner.Services;

internal readonly record struct AnimationTargetId(string Value)
{
    public override string ToString() => Value;

    public static bool TryParse(string? value, out AnimationTargetId targetId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            targetId = default;
            return false;
        }

        targetId = new AnimationTargetId(value.Trim());
        return true;
    }
}

internal enum AnimationTargetKind
{
    JsonModelElement,
    RuntimeRendererComponent
}

internal enum AnimationTargetResolutionState
{
    Resolved,
    LegacyMigratable,
    Unresolved
}

internal sealed class AnimationTargetDescriptor
{
    public required AnimationTargetId TargetId { get; init; }
    public required AnimationTargetKind Kind { get; init; }
    public required string Scope { get; init; }
    public required string DisplayName { get; init; }
    public string? ModelKey { get; init; }
    public int? ElementIndex { get; init; }
    public string? ElementFingerprint { get; init; }
    public string? RuntimeComponentKey { get; init; }
    public object? RuntimeTarget { get; init; }

    public override string ToString() => DisplayName;
}

internal sealed record AnimationTargetResolution(
    AnimationTargetResolutionState State,
    AnimationTargetDescriptor? Target,
    string Message);

/// <summary>
/// Phase 17A canonical target registry. It is the only place that translates a stable animation
/// target identity into the current JSON element index or runtime preview component.
/// </summary>
internal sealed class AnimationTargetRegistry
{
    private readonly Dictionary<AnimationTargetId, AnimationTargetDescriptor> _targets = [];

    public IReadOnlyCollection<AnimationTargetDescriptor> Targets => _targets.Values;

    public IReadOnlyList<AnimationTargetDescriptor> JsonTargetsForModel(string sourcePath)
    {
        var normalized = NormalizeModelKey(sourcePath);
        return _targets.Values
            .Where(target => target.Kind == AnimationTargetKind.JsonModelElement &&
                             string.Equals(NormalizeModelKey(target.ModelKey), normalized, StringComparison.Ordinal))
            .OrderBy(target => target.ElementIndex ?? int.MaxValue)
            .ToList();
    }

    public IReadOnlyList<AnimationTargetDescriptor> RuntimeTargets()
        => _targets.Values
            .Where(target => target.Kind == AnimationTargetKind.RuntimeRendererComponent)
            .OrderBy(target => target.TargetId.Value, StringComparer.Ordinal)
            .ToList();

    public void RebuildJsonModelTargets(MinecraftResolvedBlockModel model)
    {
        RemoveWhere(target => target.Kind == AnimationTargetKind.JsonModelElement);

        var sourcePath = NormalizeModelKey(model.SourcePath);
        var duplicateCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < model.Elements.Count; index++)
        {
            var element = model.Elements[index];
            var fingerprint = CreateElementFingerprint(element);
            var baseId = $"json:{sourcePath}#element:{fingerprint}";
            duplicateCounts.TryGetValue(baseId, out var duplicateIndex);
            duplicateCounts[baseId] = duplicateIndex + 1;
            var idValue = duplicateIndex == 0 ? baseId : $"{baseId}:duplicate-{duplicateIndex + 1}";

            var descriptor = new AnimationTargetDescriptor
            {
                TargetId = new AnimationTargetId(idValue),
                Kind = AnimationTargetKind.JsonModelElement,
                Scope = sourcePath,
                DisplayName = DescribeElement(element, index),
                ModelKey = sourcePath,
                ElementIndex = index,
                ElementFingerprint = fingerprint
            };
            _targets[descriptor.TargetId] = descriptor;
        }
    }

    public void ClearRuntimeTargets()
        => RemoveWhere(target => target.Kind == AnimationTargetKind.RuntimeRendererComponent);

    public AnimationTargetDescriptor RegisterRuntimeComponent(
        string machineScope,
        string componentKey,
        object runtimeTarget,
        string? displayName = null)
    {
        var normalizedScope = NormalizeRuntimeToken(machineScope);
        var normalizedKey = NormalizeRuntimeToken(componentKey);
        var descriptor = new AnimationTargetDescriptor
        {
            TargetId = new AnimationTargetId($"runtime:{normalizedScope}:{normalizedKey}"),
            Kind = AnimationTargetKind.RuntimeRendererComponent,
            Scope = normalizedScope,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? $"Runtime · {componentKey}"
                : displayName,
            RuntimeComponentKey = componentKey,
            RuntimeTarget = runtimeTarget
        };
        _targets[descriptor.TargetId] = descriptor;
        return descriptor;
    }

    public AnimationTargetDescriptor? FindJsonTarget(string modelKey, int elementIndex)
    {
        var normalized = NormalizeModelKey(modelKey);
        return _targets.Values.FirstOrDefault(target =>
            target.Kind == AnimationTargetKind.JsonModelElement &&
            target.ElementIndex == elementIndex &&
            string.Equals(NormalizeModelKey(target.ModelKey), normalized, StringComparison.Ordinal));
    }

    public AnimationTargetDescriptor? FindById(string? targetId)
    {
        if (!AnimationTargetId.TryParse(targetId, out var id))
            return null;
        return _targets.TryGetValue(id, out var target) ? target : null;
    }

    public AnimationTargetResolution Resolve(string? targetId, string? legacyModelKey, int legacyElementIndex)
    {
        if (AnimationTargetId.TryParse(targetId, out var stableId))
        {
            if (_targets.TryGetValue(stableId, out var stableTarget))
                return new AnimationTargetResolution(AnimationTargetResolutionState.Resolved, stableTarget, "Stable TargetId resolved.");

            return new AnimationTargetResolution(
                AnimationTargetResolutionState.Unresolved,
                null,
                $"Stable target '{stableId.Value}' is not present in the current registry.");
        }

        var legacySource = ExtractLegacyModelSource(legacyModelKey, out var indexFromKey);
        var legacyIndex = legacyElementIndex >= 0 ? legacyElementIndex : indexFromKey;
        if (!string.IsNullOrWhiteSpace(legacySource) && legacyIndex >= 0)
        {
            var migrated = FindJsonTarget(legacySource, legacyIndex);
            if (migrated is not null)
            {
                return new AnimationTargetResolution(
                    AnimationTargetResolutionState.LegacyMigratable,
                    migrated,
                    $"Legacy modelKey + elementIndex resolves to '{migrated.TargetId.Value}'. Migration is deferred until an explicit write operation.");
            }
        }

        return new AnimationTargetResolution(
            AnimationTargetResolutionState.Unresolved,
            null,
            $"Legacy target '{legacyModelKey ?? "<none>"}' element {legacyElementIndex} could not be resolved. Data must be preserved.");
    }

    public static string CreateElementFingerprint(MinecraftModelElement element)
    {
        var canonical = new StringBuilder();
        AppendVector(canonical, element.From);
        canonical.Append('|');
        AppendVector(canonical, element.To);
        canonical.Append('|');
        if (element.Rotation is null)
        {
            canonical.Append("rotation:none");
        }
        else
        {
            canonical.Append("rotation:");
            AppendVector(canonical, element.Rotation.Origin);
            canonical.Append(':').Append(element.Rotation.Axis.ToLowerInvariant());
            canonical.Append(':').Append(Format(element.Rotation.Angle));
            canonical.Append(':').Append(element.Rotation.Rescale ? '1' : '0');
        }

        foreach (var face in element.Faces.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            canonical.Append("|face:").Append(face.Key.ToLowerInvariant());
            canonical.Append(':').Append(face.Value.Texture.Trim().ToLowerInvariant());
            canonical.Append(':').Append(face.Value.Rotation);
            canonical.Append(':').Append(face.Value.CullFace?.Trim().ToLowerInvariant() ?? string.Empty);
            canonical.Append(':').Append(face.Value.TintIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            canonical.Append(':');
            if (face.Value.Uv is not null)
                AppendVector(canonical, face.Value.Uv);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..20];
    }

    private void RemoveWhere(Func<AnimationTargetDescriptor, bool> predicate)
    {
        foreach (var key in _targets.Where(pair => predicate(pair.Value)).Select(pair => pair.Key).ToList())
            _targets.Remove(key);
    }

    private static string ExtractLegacyModelSource(string? modelKey, out int elementIndex)
    {
        elementIndex = -1;
        if (string.IsNullOrWhiteSpace(modelKey))
            return string.Empty;

        var value = modelKey.Trim();
        var marker = value.LastIndexOf("|element:", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
        {
            var suffix = value[(marker + "|element:".Length)..];
            _ = int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out elementIndex);
            value = value[..marker];
        }
        return NormalizeModelKey(value);
    }

    private static string NormalizeModelKey(string? value)
        => (value ?? string.Empty).Trim().Replace('\\', '/').ToLowerInvariant();

    private static string NormalizeRuntimeToken(string value)
    {
        var normalized = new string(value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' ? character : '-')
            .ToArray());
        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        return normalized.Trim('-');
    }

    private static string DescribeElement(MinecraftModelElement element, int index)
    {
        static string V(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        return $"Element {index + 1:000} · [{V(element.From[0])},{V(element.From[1])},{V(element.From[2])}] → [{V(element.To[0])},{V(element.To[1])},{V(element.To[2])}]";
    }

    private static void AppendVector(StringBuilder builder, IEnumerable<double> values)
        => builder.AppendJoin(',', values.Select(Format));

    private static string Format(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);
}

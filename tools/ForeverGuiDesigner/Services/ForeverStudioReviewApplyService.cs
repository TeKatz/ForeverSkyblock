using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Media.Imaging;

namespace ForeverGuiDesigner.Services;

public enum StudioChangeKind
{
    BlockModel,
    ItemModel,
    Blockstate,
    Texture
}

public enum StudioValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record StudioValidationIssue(StudioValidationSeverity Severity, string Message)
{
    public override string ToString()
        => $"{Severity.ToString().ToUpperInvariant()} · {Message}";
}

public sealed class StudioReviewChange
{
    public required StudioChangeKind Kind { get; init; }
    public required string RelativePath { get; init; }
    public required string FullPath { get; init; }
    public required string OriginalHash { get; init; }
    public required string BeforeText { get; init; }
    public required string AfterText { get; init; }
    public required string Summary { get; init; }
    public byte[]? BinaryPayload { get; init; }
    public List<StudioValidationIssue> Issues { get; } = [];

    public bool IsBinary => BinaryPayload is not null;
    public bool HasErrors => Issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);
    public int WarningCount => Issues.Count(issue => issue.Severity == StudioValidationSeverity.Warning);

    public override string ToString()
    {
        var state = HasErrors ? "ERROR" : WarningCount > 0 ? "WARN" : "READY";
        return $"{state} · {Kind} · {RelativePath}";
    }
}

public static class ForeverStudioReviewApplyService
{
    private const string Namespace = "forever_skyblock";
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };
    private static readonly HashSet<string> FaceDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "down", "up", "north", "south", "west", "east"
    };

    public static StudioReviewChange BuildBlockModelChange(
        string repoRoot,
        MinecraftResolvedBlockModel source,
        MinecraftResolvedBlockModel editable)
    {
        var fullPath = ResolveRepoPath(repoRoot, editable.SourcePath);
        var before = ReadRequiredText(fullPath);
        var issues = ValidateBlockModel(repoRoot, editable, fullPath);
        string after;
        try
        {
            after = PatchBlockModelJson(before, source, editable);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            after = before;
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Could not build a lossless block-model patch: {ex.Message}"));
        }

        var change = NewJsonChange(
            StudioChangeKind.BlockModel,
            repoRoot,
            fullPath,
            before,
            after,
            $"{editable.Elements.Count} model element(s) staged");
        change.Issues.AddRange(issues);
        return change;
    }

    public static StudioReviewChange BuildItemModelChange(
        string repoRoot,
        MinecraftResolvedItemModel item,
        IReadOnlyDictionary<string, MinecraftItemDisplayTransform> workingTransforms)
    {
        var fullPath = ResolveRepoPath(repoRoot, item.SourcePath);
        var before = ReadRequiredText(fullPath);
        var issues = ValidateItemModel(repoRoot, item, workingTransforms, fullPath);
        string after;
        try
        {
            after = PatchItemModelJson(before, item.DisplayTransforms, workingTransforms);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            after = before;
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Could not build a lossless item-model patch: {ex.Message}"));
        }

        var changedModes = workingTransforms.Keys
            .Union(item.DisplayTransforms.Keys, StringComparer.OrdinalIgnoreCase)
            .Count(key => !TransformsEquivalent(
                workingTransforms.TryGetValue(key, out var working) ? working : null,
                item.DisplayTransforms.TryGetValue(key, out var source) ? source : null));
        var change = NewJsonChange(
            StudioChangeKind.ItemModel,
            repoRoot,
            fullPath,
            before,
            after,
            $"{changedModes} display-transform context(s) changed");
        change.Issues.AddRange(issues);
        return change;
    }

    public static StudioReviewChange BuildBlockstateChange(
        string repoRoot,
        string blockstatePath,
        IReadOnlyList<MinecraftBlockVariantDescriptor> sourceVariants,
        IReadOnlyList<MinecraftBlockVariantDescriptor> editableVariants)
    {
        var fullPath = ResolveRepoPath(repoRoot, blockstatePath);
        var before = ReadRequiredText(fullPath);
        var issues = ValidateBlockstate(repoRoot, editableVariants, fullPath);
        string after;
        try
        {
            after = PatchBlockstateJson(before, sourceVariants, editableVariants);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            after = before;
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Could not build a lossless blockstate patch: {ex.Message}"));
        }

        var change = NewJsonChange(
            StudioChangeKind.Blockstate,
            repoRoot,
            fullPath,
            before,
            after,
            $"{editableVariants.Count} variant(s) in the staged blockstate");
        change.Issues.AddRange(issues);
        return change;
    }

    public static StudioReviewChange BuildTextureChange(string repoRoot, string fullPath, BitmapSource bitmap)
    {
        var normalizedPath = Path.GetFullPath(fullPath);
        var issues = new List<StudioValidationIssue>();
        ValidateExpectedPath(repoRoot, normalizedPath, Path.Combine("textures"), issues);
        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                "Texture dimensions must be positive."));
        }

        byte[] payload;
        try
        {
            var snapshot = new WriteableBitmap(bitmap);
            if (snapshot.CanFreeze)
                snapshot.Freeze();
            payload = EncodePng(snapshot);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            payload = [];
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Texture could not be encoded as PNG: {ex.Message}"));
        }

        var existed = File.Exists(normalizedPath);
        var originalHash = existed ? HashFile(normalizedPath) : MissingHash;
        var beforeLength = existed ? new FileInfo(normalizedPath).Length : 0;
        var afterHash = payload.Length == 0 ? "—" : HashBytes(payload);
        var relative = SafeRelativePath(repoRoot, normalizedPath);
        var change = new StudioReviewChange
        {
            Kind = StudioChangeKind.Texture,
            RelativePath = relative,
            FullPath = normalizedPath,
            OriginalHash = originalHash,
            BinaryPayload = payload,
            BeforeText = existed
                ? $"PNG on disk\nSize: {beforeLength:N0} bytes\nSHA-256: {originalHash}"
                : "PNG does not exist on disk.",
            AfterText = $"PNG staged by Forever Studio\nDimensions: {bitmap.PixelWidth} × {bitmap.PixelHeight}\nSize: {payload.Length:N0} bytes\nSHA-256: {afterHash}",
            Summary = $"{bitmap.PixelWidth}×{bitmap.PixelHeight} PNG texture"
        };
        change.Issues.AddRange(issues);
        return change;
    }

    public static void ApplyChanges(IReadOnlyList<StudioReviewChange> changes)
    {
        if (changes.Count == 0)
            return;
        var blocking = changes.SelectMany(change => change.Issues)
            .Where(issue => issue.Severity == StudioValidationSeverity.Error)
            .ToList();
        if (blocking.Count > 0)
            throw new InvalidOperationException("Apply is blocked because validation errors remain.");

        var duplicate = changes.GroupBy(change => Path.GetFullPath(change.FullPath), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"More than one staged change targets '{duplicate.Key}'.");

        // Preflight the entire set first. If any source changed after Review, nothing is written.
        foreach (var change in changes)
        {
            var currentHash = File.Exists(change.FullPath) ? HashFile(change.FullPath) : MissingHash;
            if (!string.Equals(currentHash, change.OriginalHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException(
                    $"'{change.RelativePath}' changed on disk after Review. Refresh the review before applying.");
            }
        }

        var staged = new List<StagedFile>();
        var applied = new List<StagedFile>();
        try
        {
            foreach (var change in changes)
            {
                var directory = Path.GetDirectoryName(change.FullPath)
                                ?? throw new IOException($"Target directory is unavailable for '{change.RelativePath}'.");
                Directory.CreateDirectory(directory);
                var token = Guid.NewGuid().ToString("N");
                var temp = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.tmp");
                var backup = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.bak");
                var existed = File.Exists(change.FullPath);
                if (existed)
                    File.Copy(change.FullPath, backup, overwrite: true);

                if (change.BinaryPayload is { } payload)
                    File.WriteAllBytes(temp, payload);
                else
                    File.WriteAllText(temp, change.AfterText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                staged.Add(new StagedFile(change, temp, backup, existed));
            }

            foreach (var file in staged)
            {
                File.Move(file.TempPath, file.Change.FullPath, overwrite: true);
                applied.Add(file);
            }
        }
        catch
        {
            foreach (var file in applied.AsEnumerable().Reverse())
            {
                try
                {
                    if (file.ExistedBefore && File.Exists(file.BackupPath))
                        File.Copy(file.BackupPath, file.Change.FullPath, overwrite: true);
                    else if (!file.ExistedBefore && File.Exists(file.Change.FullPath))
                        File.Delete(file.Change.FullPath);
                }
                catch
                {
                    // Preserve the original exception. Backups are left in place if rollback itself fails.
                }
            }
            throw;
        }
        finally
        {
            foreach (var file in staged)
            {
                TryDelete(file.TempPath);
                TryDelete(file.BackupPath);
            }
        }
    }

    private static StudioReviewChange NewJsonChange(
        StudioChangeKind kind,
        string repoRoot,
        string fullPath,
        string before,
        string after,
        string summary)
        => new()
        {
            Kind = kind,
            RelativePath = SafeRelativePath(repoRoot, fullPath),
            FullPath = fullPath,
            OriginalHash = File.Exists(fullPath) ? HashFile(fullPath) : MissingHash,
            BeforeText = before,
            AfterText = after,
            Summary = summary
        };

    private static string PatchBlockModelJson(
        string before,
        MinecraftResolvedBlockModel source,
        MinecraftResolvedBlockModel editable)
    {
        var root = ParseObject(before);
        var existing = root["elements"] as JsonArray;
        root["elements"] = BuildPatchedElements(existing, source.Elements, editable.Elements);
        return FormatJson(root);
    }

    private static JsonArray BuildPatchedElements(
        JsonArray? existing,
        IReadOnlyList<MinecraftModelElement> source,
        IReadOnlyList<MinecraftModelElement> editable)
    {
        var result = new JsonArray();
        if (existing is null || existing.Count != source.Count)
        {
            foreach (var element in editable)
                result.Add(PatchElement(new JsonObject(), element));
            return result;
        }

        var mapping = MatchElements(source, editable);
        for (var index = 0; index < editable.Count; index++)
        {
            var sourceIndex = mapping[index];
            var raw = sourceIndex >= 0 && sourceIndex < existing.Count
                ? existing[sourceIndex]?.DeepClone() as JsonObject
                : null;
            result.Add(PatchElement(raw ?? new JsonObject(), editable[index]));
        }
        return result;
    }

    private static int[] MatchElements(
        IReadOnlyList<MinecraftModelElement> source,
        IReadOnlyList<MinecraftModelElement> editable)
    {
        var map = Enumerable.Repeat(-1, editable.Count).ToArray();
        var used = new bool[source.Count];
        if (source.Count == editable.Count)
        {
            for (var i = 0; i < editable.Count; i++)
            {
                map[i] = i;
                used[i] = true;
            }
            return map;
        }

        // First preserve exact unchanged elements. This handles insert/delete operations without
        // moving unknown JSON metadata to a neighboring element.
        for (var e = 0; e < editable.Count; e++)
        {
            for (var s = 0; s < source.Count; s++)
            {
                if (used[s] || !KnownElementEquivalent(source[s], editable[e]))
                    continue;
                map[e] = s;
                used[s] = true;
                break;
            }
        }

        // Remaining elements are edited/new. Pair edited elements with the nearest unused source
        // by structural similarity; excess editable elements are treated as newly inserted.
        for (var e = 0; e < editable.Count; e++)
        {
            if (map[e] >= 0)
                continue;
            var best = -1;
            var bestScore = double.MaxValue;
            for (var s = 0; s < source.Count; s++)
            {
                if (used[s])
                    continue;
                var score = ElementDistance(source[s], editable[e]) + Math.Abs(s - e) * 0.01;
                if (score >= bestScore)
                    continue;
                best = s;
                bestScore = score;
            }
            if (best >= 0 && bestScore < 250)
            {
                map[e] = best;
                used[best] = true;
            }
        }
        return map;
    }

    private static JsonObject PatchElement(JsonObject target, MinecraftModelElement element)
    {
        target["from"] = NumberArray(element.From);
        target["to"] = NumberArray(element.To);

        if (element.Rotation is null)
        {
            target.Remove("rotation");
        }
        else
        {
            var rotation = target["rotation"] as JsonObject ?? new JsonObject();
            rotation["origin"] = NumberArray(element.Rotation.Origin);
            rotation["axis"] = element.Rotation.Axis;
            rotation["angle"] = element.Rotation.Angle;
            if (element.Rotation.Rescale)
                rotation["rescale"] = true;
            else
                rotation.Remove("rescale");
            target["rotation"] = rotation;
        }

        var faces = target["faces"] as JsonObject ?? new JsonObject();
        foreach (var property in faces.ToList())
        {
            if (FaceDirections.Contains(property.Key) && !element.Faces.ContainsKey(property.Key))
                faces.Remove(property.Key);
        }
        foreach (var (direction, face) in element.Faces)
        {
            var existingName = FindPropertyName(faces, direction) ?? direction;
            var faceObject = faces[existingName] as JsonObject ?? new JsonObject();
            faceObject["texture"] = face.Texture;
            if (face.Uv is { Length: >= 4 })
                faceObject["uv"] = NumberArray(face.Uv.Take(4));
            else
                faceObject.Remove("uv");
            if (face.Rotation == 0)
                faceObject.Remove("rotation");
            else
                faceObject["rotation"] = face.Rotation;
            if (string.IsNullOrWhiteSpace(face.CullFace))
                faceObject.Remove("cullface");
            else
                faceObject["cullface"] = face.CullFace;
            if (face.TintIndex is null)
                faceObject.Remove("tintindex");
            else
                faceObject["tintindex"] = face.TintIndex.Value;
            faces[existingName] = faceObject;
        }
        target["faces"] = faces;
        return target;
    }

    private static string PatchItemModelJson(
        string before,
        IReadOnlyDictionary<string, MinecraftItemDisplayTransform> source,
        IReadOnlyDictionary<string, MinecraftItemDisplayTransform> working)
    {
        var root = ParseObject(before);
        var display = root["display"] as JsonObject ?? new JsonObject();
        foreach (var key in working.Keys.Union(source.Keys, StringComparer.OrdinalIgnoreCase))
        {
            working.TryGetValue(key, out var edited);
            source.TryGetValue(key, out var original);
            if (TransformsEquivalent(edited, original))
                continue;
            if (edited is null)
            {
                var existingName = FindPropertyName(display, key);
                if (existingName is not null)
                    display.Remove(existingName);
                continue;
            }

            var propertyName = FindPropertyName(display, key) ?? key;
            var transform = display[propertyName] as JsonObject ?? new JsonObject();
            transform["rotation"] = NumberArray(edited.Rotation);
            transform["translation"] = NumberArray(edited.Translation);
            transform["scale"] = NumberArray(edited.Scale);
            display[propertyName] = transform;
        }
        if (display.Count == 0)
            root.Remove("display");
        else
            root["display"] = display;
        return FormatJson(root);
    }

    private static string PatchBlockstateJson(
        string before,
        IReadOnlyList<MinecraftBlockVariantDescriptor> source,
        IReadOnlyList<MinecraftBlockVariantDescriptor> editable)
    {
        var root = ParseObject(before);
        var variants = root["variants"] as JsonObject ?? new JsonObject();
        var sourceMap = source.GroupBy(VariantStorageKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var editedMap = editable.GroupBy(VariantStorageKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var (key, _) in sourceMap)
        {
            if (editedMap.ContainsKey(key))
                continue;
            var propertyName = FindPropertyName(variants, key);
            if (propertyName is not null)
                variants.Remove(propertyName);
        }

        foreach (var (key, edited) in editedMap)
        {
            sourceMap.TryGetValue(key, out var original);
            if (original is not null && VariantsEquivalent(original, edited))
                continue;

            var propertyName = FindPropertyName(variants, key) ?? key;
            var existing = variants[propertyName];
            if (existing is JsonArray array)
            {
                var clone = array.DeepClone() as JsonArray ?? new JsonArray();
                var firstObjectIndex = -1;
                for (var i = 0; i < clone.Count; i++)
                {
                    if (clone[i] is JsonObject)
                    {
                        firstObjectIndex = i;
                        break;
                    }
                }
                if (firstObjectIndex >= 0)
                    clone[firstObjectIndex] = PatchVariantObject((JsonObject)clone[firstObjectIndex]!, edited);
                else
                    clone.Add(PatchVariantObject(new JsonObject(), edited));
                variants[propertyName] = clone;
            }
            else
            {
                var variantObject = existing?.DeepClone() as JsonObject ?? new JsonObject();
                variants[propertyName] = PatchVariantObject(variantObject, edited);
            }
        }

        root["variants"] = variants;
        return FormatJson(root);
    }

    private static JsonObject PatchVariantObject(JsonObject target, MinecraftBlockVariantDescriptor variant)
    {
        target["model"] = variant.ModelReference;
        if (NormalizeRotation(variant.XRotation) == 0)
            target.Remove("x");
        else
            target["x"] = NormalizeRotation(variant.XRotation);
        if (NormalizeRotation(variant.YRotation) == 0)
            target.Remove("y");
        else
            target["y"] = NormalizeRotation(variant.YRotation);
        if (variant.UvLock)
            target["uvlock"] = true;
        else
            target.Remove("uvlock");
        return target;
    }

    private static List<StudioValidationIssue> ValidateBlockModel(
        string repoRoot,
        MinecraftResolvedBlockModel model,
        string fullPath)
    {
        var issues = new List<StudioValidationIssue>();
        ValidateExpectedPath(repoRoot, fullPath, Path.Combine("models", "block"), issues);
        if (model.Elements.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning, "Model contains no elements."));

        for (var elementIndex = 0; elementIndex < model.Elements.Count; elementIndex++)
        {
            var element = model.Elements[elementIndex];
            if (!ValidVector(element.From, 3) || !ValidVector(element.To, 3))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    $"Element {elementIndex + 1} has invalid coordinates."));
                continue;
            }
            for (var axis = 0; axis < 3; axis++)
            {
                if (element.From[axis] >= element.To[axis])
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} has From >= To on axis {AxisName(axis)}."));
                }
                if (element.From[axis] < -16 || element.To[axis] > 32)
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Warning,
                        $"Element {elementIndex + 1} extends beyond the common Minecraft model range -16..32 on axis {AxisName(axis)}."));
                }
            }

            if (element.Rotation is { } rotation)
            {
                if (!ValidVector(rotation.Origin, 3) || rotation.Axis is not ("x" or "y" or "z"))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} has an invalid rotation origin or axis."));
                }
                var legalAngles = new[] { -45d, -22.5d, 0d, 22.5d, 45d };
                if (!double.IsFinite(rotation.Angle) || !legalAngles.Any(value => Math.Abs(value - rotation.Angle) < 0.0001))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} rotation {rotation.Angle.ToString(CultureInfo.InvariantCulture)}° is not a Minecraft model rotation (-45, -22.5, 0, 22.5, 45)."));
                }
            }

            foreach (var (direction, face) in element.Faces)
            {
                if (!FaceDirections.Contains(direction))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Warning,
                        $"Element {elementIndex + 1} uses non-standard face direction '{direction}'."));
                }
                if (string.IsNullOrWhiteSpace(face.Texture))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} face {direction} has no texture reference."));
                }
                else
                {
                    ValidateFaceTexture(repoRoot, model, elementIndex, direction, face.Texture, issues);
                }
                if (NormalizeRotation(face.Rotation) is not (0 or 90 or 180 or 270))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} face {direction} has invalid rotation {face.Rotation}°."));
                }
                if (face.Uv is { } uv)
                {
                    if (!ValidVector(uv, 4))
                    {
                        issues.Add(new StudioValidationIssue(
                            StudioValidationSeverity.Error,
                            $"Element {elementIndex + 1} face {direction} has invalid UV coordinates."));
                    }
                    else if (uv.Any(value => value < 0 || value > 16))
                    {
                        issues.Add(new StudioValidationIssue(
                            StudioValidationSeverity.Warning,
                            $"Element {elementIndex + 1} face {direction} uses UV values outside 0..16."));
                    }
                }
            }
        }

        try
        {
            _ = MinecraftBlockModelPreviewService.LoadResolvedModel(repoRoot, model.ModelReference);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Model parent/reference chain is invalid: {ex.Message}"));
        }
        return issues;
    }

    private static List<StudioValidationIssue> ValidateItemModel(
        string repoRoot,
        MinecraftResolvedItemModel item,
        IReadOnlyDictionary<string, MinecraftItemDisplayTransform> transforms,
        string fullPath)
    {
        var issues = new List<StudioValidationIssue>();
        ValidateExpectedPath(repoRoot, fullPath, Path.Combine("models", "item"), issues);
        foreach (var (mode, transform) in transforms)
        {
            if (!ValidVector(transform.Rotation, 3) || !ValidVector(transform.Translation, 3) || !ValidVector(transform.Scale, 3))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    $"Display transform '{mode}' contains NaN, infinity, or an incomplete vector."));
                continue;
            }
            if (transform.Scale.Any(value => Math.Abs(value) > 8) || transform.Translation.Any(value => Math.Abs(value) > 80))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Warning,
                    $"Display transform '{mode}' contains unusually large scale/translation values."));
            }
        }

        if (item.IsGeneratedItem && string.IsNullOrWhiteSpace(item.Layer0TextureReference))
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                "Generated item has no resolved layer0 texture."));
        }

        try
        {
            _ = MinecraftItemModelService.LoadResolvedItem(repoRoot, item.Id);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Item parent/reference chain is invalid: {ex.Message}"));
        }
        return issues;
    }

    private static List<StudioValidationIssue> ValidateBlockstate(
        string repoRoot,
        IReadOnlyList<MinecraftBlockVariantDescriptor> variants,
        string fullPath)
    {
        var issues = new List<StudioValidationIssue>();
        ValidateExpectedPath(repoRoot, fullPath, "blockstates", issues);
        foreach (var duplicate in variants.GroupBy(VariantStorageKey, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Variant key '{DisplayVariantKey(duplicate.Key)}' exists {duplicate.Count()} times."));
        }
        foreach (var variant in variants)
        {
            if (string.IsNullOrWhiteSpace(variant.ModelReference))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Variant '{variant.Key}' has no model target."));
                continue;
            }
            if (NormalizeRotation(variant.XRotation) is not (0 or 90 or 180 or 270) ||
                NormalizeRotation(variant.YRotation) is not (0 or 90 or 180 or 270))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    $"Variant '{variant.Key}' uses a blockstate rotation that is not 0/90/180/270°."));
            }
            if (!BlockModelTargetExists(repoRoot, variant.ModelReference))
            {
                issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Error,
                    $"Variant '{variant.Key}' targets missing model '{variant.ModelReference}'."));
            }
        }
        return issues;
    }

    private static void ValidateFaceTexture(
        string repoRoot,
        MinecraftResolvedBlockModel model,
        int elementIndex,
        string direction,
        string textureReference,
        List<StudioValidationIssue> issues)
    {
        var resource = MinecraftBlockModelPreviewService.ResolveTextureResource(model.Textures, textureReference);
        if (string.IsNullOrWhiteSpace(resource))
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Element {elementIndex + 1} face {direction} has unresolved texture reference '{textureReference}'."));
            return;
        }
        var colon = resource.IndexOf(':');
        var resourceNamespace = colon >= 0 ? resource[..colon] : Namespace;
        if (!resourceNamespace.Equals(Namespace, StringComparison.OrdinalIgnoreCase))
            return;
        var resourcePath = colon >= 0 ? resource[(colon + 1)..] : resource;
        var path = Path.Combine(
            repoRoot,
            "mod", "src", "main", "resources", "assets", Namespace,
            "textures",
            resourcePath.Replace('/', Path.DirectorySeparatorChar) + ".png");
        if (!File.Exists(path))
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Element {elementIndex + 1} face {direction} targets missing texture '{resource}'."));
        }
    }

    private static void ValidateExpectedPath(
        string repoRoot,
        string fullPath,
        string assetSection,
        List<StudioValidationIssue> issues)
    {
        var expected = Path.Combine(
            repoRoot,
            "mod", "src", "main", "resources", "assets", Namespace,
            assetSection);
        if (!IsPathInside(fullPath, expected))
        {
            issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Asset is outside the expected project path '{SafeRelativePath(repoRoot, expected)}'."));
        }
    }

    private static bool BlockModelTargetExists(string repoRoot, string reference)
    {
        var normalized = reference.Contains(':') ? reference : $"{Namespace}:{reference}";
        var colon = normalized.IndexOf(':');
        var ns = colon >= 0 ? normalized[..colon] : Namespace;
        if (!ns.Equals(Namespace, StringComparison.OrdinalIgnoreCase))
            return true;
        var resource = colon >= 0 ? normalized[(colon + 1)..] : normalized;
        var path = Path.Combine(
            repoRoot,
            "mod", "src", "main", "resources", "assets", Namespace,
            "models",
            resource.Replace('/', Path.DirectorySeparatorChar) + ".json");
        return File.Exists(path);
    }

    private static bool VariantsEquivalent(MinecraftBlockVariantDescriptor left, MinecraftBlockVariantDescriptor right)
        => string.Equals(left.ModelReference, right.ModelReference, StringComparison.OrdinalIgnoreCase) &&
           NormalizeRotation(left.XRotation) == NormalizeRotation(right.XRotation) &&
           NormalizeRotation(left.YRotation) == NormalizeRotation(right.YRotation) &&
           left.UvLock == right.UvLock &&
           DictionaryEquivalent(left.Properties, right.Properties);

    private static bool DictionaryEquivalent(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count)
            return false;
        return left.All(pair => right.TryGetValue(pair.Key, out var value) &&
                                string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TransformsEquivalent(MinecraftItemDisplayTransform? left, MinecraftItemDisplayTransform? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return VectorEquivalent(left.Rotation, right.Rotation) &&
               VectorEquivalent(left.Translation, right.Translation) &&
               VectorEquivalent(left.Scale, right.Scale);
    }

    private static bool KnownElementEquivalent(MinecraftModelElement left, MinecraftModelElement right)
    {
        if (!VectorEquivalent(left.From, right.From) || !VectorEquivalent(left.To, right.To))
            return false;
        if (!RotationsEquivalent(left.Rotation, right.Rotation) || left.Faces.Count != right.Faces.Count)
            return false;
        foreach (var (direction, face) in left.Faces)
        {
            if (!right.Faces.TryGetValue(direction, out var other) ||
                !string.Equals(face.Texture, other.Texture, StringComparison.OrdinalIgnoreCase) ||
                face.Rotation != other.Rotation ||
                !NullableVectorEquivalent(face.Uv, other.Uv) ||
                !string.Equals(face.CullFace, other.CullFace, StringComparison.OrdinalIgnoreCase) ||
                face.TintIndex != other.TintIndex)
                return false;
        }
        return true;
    }

    private static double ElementDistance(MinecraftModelElement left, MinecraftModelElement right)
    {
        var distance = VectorDistance(left.From, right.From) + VectorDistance(left.To, right.To);
        var leftDirections = left.Faces.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightDirections = right.Faces.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        distance += leftDirections.SymmetricExceptCount(rightDirections) * 25;
        foreach (var direction in leftDirections.Intersect(rightDirections, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.Equals(left.Faces[direction].Texture, right.Faces[direction].Texture, StringComparison.OrdinalIgnoreCase))
                distance += 5;
        }
        return distance;
    }

    private static bool RotationsEquivalent(MinecraftModelElementRotation? left, MinecraftModelElementRotation? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return string.Equals(left.Axis, right.Axis, StringComparison.OrdinalIgnoreCase) &&
               Math.Abs(left.Angle - right.Angle) < 0.000001 &&
               left.Rescale == right.Rescale &&
               VectorEquivalent(left.Origin, right.Origin);
    }

    private static bool NullableVectorEquivalent(double[]? left, double[]? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        return VectorEquivalent(left, right);
    }

    private static bool VectorEquivalent(IReadOnlyList<double> left, IReadOnlyList<double> right)
        => left.Count == right.Count && left.Select((value, index) => Math.Abs(value - right[index]) < 0.000001).All(value => value);

    private static double VectorDistance(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var count = Math.Min(left.Count, right.Count);
        var result = 0d;
        for (var i = 0; i < count; i++)
            result += Math.Abs(left[i] - right[i]);
        return result + Math.Abs(left.Count - right.Count) * 100;
    }

    private static bool ValidVector(double[] values, int expected)
        => values.Length >= expected && values.Take(expected).All(double.IsFinite);

    private static string AxisName(int index) => index switch { 0 => "X", 1 => "Y", _ => "Z" };

    private static string VariantStorageKey(MinecraftBlockVariantDescriptor variant)
        => variant.Properties.Count == 0 && variant.Key.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : variant.Key;

    private static string DisplayVariantKey(string storageKey)
        => string.IsNullOrEmpty(storageKey) ? "default" : storageKey;

    private static int NormalizeRotation(int value) => ((value % 360) + 360) % 360;

    private static JsonObject ParseObject(string json)
        => JsonNode.Parse(json) as JsonObject
           ?? throw new InvalidOperationException("JSON root is not an object.");

    private static JsonArray NumberArray(IEnumerable<double> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(JsonValue.Create(value));
        return array;
    }

    private static string FormatJson(JsonObject root)
        => root.ToJsonString(IndentedJson) + Environment.NewLine;

    private static string? FindPropertyName(JsonObject owner, string requested)
        => owner.Select(pair => pair.Key)
            .FirstOrDefault(key => key.Equals(requested, StringComparison.OrdinalIgnoreCase));

    private static byte[] EncodePng(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static string ReadRequiredText(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Studio source file was not found.", path);
        return File.ReadAllText(path);
    }

    private static string ResolveRepoPath(string repoRoot, string relativePath)
        => Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string SafeRelativePath(string repoRoot, string path)
    {
        try
        {
            return Path.GetRelativePath(Path.GetFullPath(repoRoot), Path.GetFullPath(path)).Replace('\\', '/');
        }
        catch
        {
            return path;
        }
    }

    private static bool IsPathInside(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string HashBytes(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes));

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cleanup is best-effort; the actual apply result has already been decided.
        }
    }

    private const string MissingHash = "<missing>";

    private sealed record StagedFile(
        StudioReviewChange Change,
        string TempPath,
        string BackupPath,
        bool ExistedBefore);

    private static int SymmetricExceptCount<T>(this HashSet<T> left, HashSet<T> right)
    {
        var clone = new HashSet<T>(left, left.Comparer);
        clone.SymmetricExceptWith(right);
        return clone.Count;
    }
}

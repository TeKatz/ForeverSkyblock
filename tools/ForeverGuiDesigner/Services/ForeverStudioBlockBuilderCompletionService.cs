using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public enum StudioBlockstateAuthoringSupport
{
    SimpleVariants,
    WeightedVariantsReadOnly,
    MultipartReadOnly,
    UnsupportedReadOnly,
    InvalidReadOnly
}

public sealed record StudioBlockstateAuthoringInfo(
    StudioBlockstateAuthoringSupport Support,
    string Message,
    string? PreviewModelReference = null)
{
    public bool CanAuthor => Support == StudioBlockstateAuthoringSupport.SimpleVariants;
}

/// <summary>
/// Final Block Builder safety layer. It keeps unsupported blockstate formats read-only,
/// augments block-model validation, and applies reviewed files through per-file atomic
/// replacement with source hash rechecks and rollback.
/// </summary>
public static class ForeverStudioBlockBuilderCompletionService
{
    private const string Namespace = "forever_skyblock";
    private const string MissingHash = "<missing>";
    private static readonly HashSet<string> FaceDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "down", "up", "north", "south", "west", "east"
    };

    public static StudioBlockstateAuthoringInfo InspectBlockstate(string repoRoot, string blockstatePath)
    {
        try
        {
            var fullPath = ResolveRepoPath(repoRoot, blockstatePath);
            if (!File.Exists(fullPath))
            {
                return new StudioBlockstateAuthoringInfo(
                    StudioBlockstateAuthoringSupport.InvalidReadOnly,
                    "Blockstate source is missing. Authoring is disabled until the project is rescanned.");
            }

            using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
            var root = document.RootElement;
            var hasMultipart = root.TryGetProperty("multipart", out var multipart) && multipart.ValueKind == JsonValueKind.Array;
            var hasVariants = root.TryGetProperty("variants", out var variants) && variants.ValueKind == JsonValueKind.Object;

            if (hasMultipart)
            {
                return new StudioBlockstateAuthoringInfo(
                    StudioBlockstateAuthoringSupport.MultipartReadOnly,
                    "Multipart blockstate detected. The Block Builder shows a representative model preview, but multipart conditions/apply lists are read-only so they cannot be rewritten incorrectly.",
                    FindFirstMultipartModel(multipart));
            }

            if (hasVariants)
            {
                var weighted = variants.EnumerateObject().Any(property => property.Value.ValueKind == JsonValueKind.Array);
                if (weighted)
                {
                    return new StudioBlockstateAuthoringInfo(
                        StudioBlockstateAuthoringSupport.WeightedVariantsReadOnly,
                        "Weighted blockstate variant arrays are read-only. The preview uses the first candidate, while weights and alternate models remain untouched on disk.");
                }

                return new StudioBlockstateAuthoringInfo(
                    StudioBlockstateAuthoringSupport.SimpleVariants,
                    "Simple variants blockstate. Variant authoring is enabled.");
            }

            return new StudioBlockstateAuthoringInfo(
                StudioBlockstateAuthoringSupport.UnsupportedReadOnly,
                "This blockstate does not use the simple variants format supported by the visual authoring matrix. It remains read-only.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new StudioBlockstateAuthoringInfo(
                StudioBlockstateAuthoringSupport.InvalidReadOnly,
                $"Blockstate could not be parsed safely and is read-only: {ex.Message}");
        }
    }

    public static StudioReviewChange BuildBlockModelChange(
        string repoRoot,
        MinecraftResolvedBlockModel source,
        MinecraftResolvedBlockModel editable)
    {
        var change = ForeverStudioReviewApplyService.BuildBlockModelChange(repoRoot, source, editable);
        AugmentBlockModelValidation(change, editable);
        ValidateGeneratedJson(change);
        return change;
    }

    public static StudioReviewChange BuildBlockstateChange(
        string repoRoot,
        string blockstatePath,
        IReadOnlyList<MinecraftBlockVariantDescriptor> sourceVariants,
        IReadOnlyList<MinecraftBlockVariantDescriptor> editableVariants)
    {
        var change = ForeverStudioReviewApplyService.BuildBlockstateChange(
            repoRoot,
            blockstatePath,
            sourceVariants,
            editableVariants);
        var support = InspectBlockstate(repoRoot, blockstatePath);
        if (!support.CanAuthor)
        {
            change.Issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                support.Message));
        }
        ValidateGeneratedJson(change);
        return change;
    }

    public static void ValidateReviewChange(StudioReviewChange change)
    {
        if (!change.IsBinary)
            ValidateGeneratedJson(change);
    }

    public static void ApplyChangesAtomically(IReadOnlyList<StudioReviewChange> changes)
    {
        if (changes.Count == 0)
            return;

        if (changes.SelectMany(change => change.Issues)
            .Any(issue => issue.Severity == StudioValidationSeverity.Error))
        {
            throw new InvalidOperationException("Apply is blocked because validation errors remain.");
        }

        var duplicate = changes
            .GroupBy(change => Path.GetFullPath(change.FullPath), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"More than one staged change targets '{duplicate.Key}'.");

        VerifyReviewedSources(changes);

        var staged = new List<AtomicStagedFile>();
        var applied = new List<AtomicStagedFile>();
        var preserveBackups = false;
        try
        {
            foreach (var change in changes)
            {
                ValidateReviewChange(change);
                var directory = Path.GetDirectoryName(change.FullPath)
                                ?? throw new IOException($"Target directory is unavailable for '{change.RelativePath}'.");
                Directory.CreateDirectory(directory);

                var token = Guid.NewGuid().ToString("N");
                var temp = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.tmp");
                var backup = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.bak");
                var payload = GetPayload(change);
                File.WriteAllBytes(temp, payload);

                var stagedHash = HashFile(temp);
                var expectedHash = HashBytes(payload);
                if (!string.Equals(stagedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"Staging verification failed for '{change.RelativePath}'.");

                staged.Add(new AtomicStagedFile(
                    change,
                    temp,
                    backup,
                    File.Exists(change.FullPath),
                    expectedHash));
            }

            // Staging may take noticeable time for textures. Recheck all reviewed inputs again
            // after staging and before the first repository file is replaced.
            VerifyReviewedSources(changes);

            foreach (var file in staged)
            {
                var currentHash = File.Exists(file.Change.FullPath) ? HashFile(file.Change.FullPath) : MissingHash;
                if (!string.Equals(currentHash, file.Change.OriginalHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException(
                        $"'{file.Change.RelativePath}' changed on disk immediately before Apply. Refresh Review and try again.");
                }

                if (file.ExistedBefore)
                {
                    File.Replace(file.TempPath, file.Change.FullPath, file.BackupPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(file.TempPath, file.Change.FullPath);
                }
                applied.Add(file);

                if (!File.Exists(file.Change.FullPath) ||
                    !string.Equals(HashFile(file.Change.FullPath), file.ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException($"Post-write verification failed for '{file.Change.RelativePath}'.");
                }
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
                    preserveBackups = true;
                }
            }
            throw;
        }
        finally
        {
            foreach (var file in staged)
            {
                TryDelete(file.TempPath);
                if (!preserveBackups)
                    TryDelete(file.BackupPath);
            }
        }
    }

    public static string? FindFirstMultipartPreviewModel(string repoRoot, string blockstatePath)
        => InspectBlockstate(repoRoot, blockstatePath).PreviewModelReference;

    private static void AugmentBlockModelValidation(StudioReviewChange change, MinecraftResolvedBlockModel model)
    {
        for (var elementIndex = 0; elementIndex < model.Elements.Count; elementIndex++)
        {
            var element = model.Elements[elementIndex];
            if (element.Faces.Count == 0)
            {
                change.Issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Warning,
                    $"Element {elementIndex + 1} has no rendered faces."));
            }

            if (element.Rotation is { } rotation && rotation.Origin.Take(3).Any(value => value < -16 || value > 32))
            {
                change.Issues.Add(new StudioValidationIssue(
                    StudioValidationSeverity.Warning,
                    $"Element {elementIndex + 1} rotation origin extends beyond the common Minecraft model range -16..32."));
            }

            foreach (var (direction, face) in element.Faces)
            {
                if (!string.IsNullOrWhiteSpace(face.CullFace) && !FaceDirections.Contains(face.CullFace))
                {
                    change.Issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} face {direction} has invalid cullface '{face.CullFace}'."));
                }
                if (face.TintIndex is < 0)
                {
                    change.Issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"Element {elementIndex + 1} face {direction} has a negative tint index."));
                }
                if (face.Uv is { Length: >= 4 } uv &&
                    (Math.Abs(uv[0] - uv[2]) < 0.000001 || Math.Abs(uv[1] - uv[3]) < 0.000001))
                {
                    change.Issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Warning,
                        $"Element {elementIndex + 1} face {direction} has a zero-area UV rectangle."));
                }
            }
        }
    }

    private static void ValidateGeneratedJson(StudioReviewChange change)
    {
        if (change.IsBinary || string.Equals(change.BeforeText, change.AfterText, StringComparison.Ordinal))
            return;
        try
        {
            using var _ = JsonDocument.Parse(change.AfterText);
        }
        catch (JsonException ex)
        {
            change.Issues.Add(new StudioValidationIssue(
                StudioValidationSeverity.Error,
                $"Generated JSON failed roundtrip parsing: {ex.Message}"));
        }
    }

    private static void VerifyReviewedSources(IEnumerable<StudioReviewChange> changes)
    {
        foreach (var change in changes)
        {
            var currentHash = File.Exists(change.FullPath) ? HashFile(change.FullPath) : MissingHash;
            if (!string.Equals(currentHash, change.OriginalHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException(
                    $"'{change.RelativePath}' changed on disk after Review. Refresh Review before applying.");
            }
        }
    }

    private static byte[] GetPayload(StudioReviewChange change)
        => change.BinaryPayload ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(change.AfterText);

    private static string? FindFirstMultipartModel(JsonElement multipart)
    {
        foreach (var entry in multipart.EnumerateArray())
        {
            if (!entry.TryGetProperty("apply", out var apply))
                continue;
            if (TryReadModel(apply, out var model))
                return NormalizeModelReference(model);
            if (apply.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var candidate in apply.EnumerateArray())
            {
                if (TryReadModel(candidate, out model))
                    return NormalizeModelReference(model);
            }
        }
        return null;
    }

    private static bool TryReadModel(JsonElement value, out string model)
    {
        model = string.Empty;
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("model", out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            return false;
        }
        model = property.GetString()!;
        return true;
    }

    private static string NormalizeModelReference(string value)
        => value.Contains(':') ? value : $"{Namespace}:{value}";

    private static string ResolveRepoPath(string repoRoot, string relativePath)
        => Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

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
            // Preserve the actual Apply result; cleanup is best-effort only.
        }
    }

    private sealed record AtomicStagedFile(
        StudioReviewChange Change,
        string TempPath,
        string BackupPath,
        bool ExistedBefore,
        string ExpectedHash);
}

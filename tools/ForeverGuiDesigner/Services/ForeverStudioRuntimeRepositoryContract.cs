using System.Text.RegularExpressions;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Verifies that the selected repository checkout contains the Java runtime contract expected by
/// this Forever Studio build. Runtime-machine generation must fail before writing generated files
/// when Studio is newer than the selected local checkout.
/// </summary>
public static partial class ForeverStudioRuntimeRepositoryContract
{
    public const int ExpectedCompilerContractVersion = 2;
    public const string RelativeMarkerPath =
        "mod/src/main/java/de/tekatz/foreverskyblock/studio/runtime/ForeverStudioRuntimeContract.java";

    [GeneratedRegex(@"COMPILER_CONTRACT_VERSION\s*=\s*(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ContractVersionRegex();

    public static StudioValidationIssue? Validate(string repoRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
        var root = Path.GetFullPath(repoRoot.Trim());
        var markerPath = Path.Combine(root, RelativeMarkerPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(markerPath))
        {
            return Error(
                $"Selected repository checkout is missing Forever Studio runtime contract v{ExpectedCompilerContractVersion}. " +
                "This Studio build is newer than the Java runtime in that checkout. Update or switch the selected " +
                "ForeverSkyblock repository to a revision containing the matching Studio runtime before building a runtime machine. " +
                $"Expected marker: {RelativeMarkerPath}");
        }

        string source;
        try
        {
            source = File.ReadAllText(markerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Error($"Forever Studio runtime compatibility marker cannot be read: {ex.Message}");
        }

        var match = ContractVersionRegex().Match(source);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var actualVersion))
        {
            return Error(
                $"Forever Studio runtime compatibility marker is invalid. Expected contract v{ExpectedCompilerContractVersion} in " +
                $"{RelativeMarkerPath}. Update the selected repository checkout before building.");
        }

        if (actualVersion != ExpectedCompilerContractVersion)
        {
            return Error(
                $"Forever Studio runtime contract mismatch: Studio requires v{ExpectedCompilerContractVersion}, but the selected " +
                $"repository provides v{actualVersion}. Update the selected repository checkout before building a runtime machine.");
        }

        return null;
    }

    private static StudioValidationIssue Error(string message)
        => new(StudioValidationSeverity.Error, message);
}

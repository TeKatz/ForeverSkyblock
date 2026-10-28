using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class RuntimeRepositoryContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "forever-studio-runtime-contract-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);

            var missing = ForeverStudioRuntimeRepositoryContract.Validate(root);
            Check(missing is not null && missing.Severity == StudioValidationSeverity.Error,
                "Missing runtime marker must block runtime-machine generation.");
            Check(missing.Message.Contains("missing Forever Studio runtime contract v2", StringComparison.OrdinalIgnoreCase),
                "Missing-marker diagnostic must explain the repository/runtime mismatch.");

            var marker = Path.Combine(root,
                ForeverStudioRuntimeRepositoryContract.RelativeMarkerPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            File.WriteAllText(marker, "public static final int COMPILER_CONTRACT_VERSION = 1;");

            var stale = ForeverStudioRuntimeRepositoryContract.Validate(root);
            Check(stale is not null && stale.Message.Contains("provides v1", StringComparison.OrdinalIgnoreCase),
                "A stale runtime marker must report the actual incompatible contract version.");

            File.WriteAllText(marker,
                $"public static final int COMPILER_CONTRACT_VERSION = {ForeverStudioRuntimeRepositoryContract.ExpectedCompilerContractVersion};");
            Check(ForeverStudioRuntimeRepositoryContract.Validate(root) is null,
                "Matching runtime contract must allow runtime-machine generation.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

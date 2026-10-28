using System.Security.Cryptography;
using System.Text.Json;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Compiles the shared GUI presentation layer into a runtime sidecar without contaminating the
/// compact machine-definition contract. Unsupported/ambiguous presentation semantics fail closed.
/// </summary>
internal static class ForeverStudioGuiRuntimePlanFixup
{
    internal const int SchemaVersion = 1;
    private const string MissingHash = "<missing>";

    private static readonly HashSet<string> SupportedRuntimeEffects = new(StringComparer.OrdinalIgnoreCase)
    {
        ForeverGuiStyleEffectService.HoverKind,
        ForeverGuiStyleEffectService.GlowKind,
        ForeverGuiStyleEffectService.ShadowKind,
        ForeverGuiStyleEffectService.PulseKind,
        ForeverGuiStyleEffectService.PressedOffsetKind
    };

    internal static ForeverStudioMachineProductionPlan Normalize(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioMachineProductionPlan plan)
    {
        if (!plan.ProductionKind.Equals(ForeverStudioRuntimeCompiler.ProductionKindRuntimeMachine, StringComparison.Ordinal))
            return plan;

        ForeverGuiDocument gui;
        try
        {
            gui = ForeverMachineGuiDocumentBridge.FromMachineDocument(document);
        }
        catch (InvalidDataException ex)
        {
            return AddError(plan, "Shared GUI presentation cannot be compiled: " + ex.Message);
        }

        var issues = plan.Issues.ToList();
        var runtimeBindings = gui.Bindings
            .Where(binding => binding.Provider.Equals("machine", StringComparison.OrdinalIgnoreCase))
            .Select(binding => binding.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var elements = new SortedDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var element in gui.Elements.OrderBy(candidate => candidate.Order).ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase))
        {
            var bindingId = string.IsNullOrWhiteSpace(element.BindingId) ? element.Id : element.BindingId;
            if (!runtimeBindings.Contains(bindingId))
                continue;

            var style = string.IsNullOrWhiteSpace(element.StyleId)
                ? null
                : gui.Styles.FirstOrDefault(candidate => candidate.Id.Equals(element.StyleId, StringComparison.OrdinalIgnoreCase));
            var effects = gui.Effects
                .Where(effect => element.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var effect in effects)
            {
                if (effect.Kind.Equals(ForeverGuiStyleEffectService.DisabledAppearanceKind, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"GUI element '{element.Id}' uses Disabled Appearance, but no deterministic runtime disabled-state source is defined yet."));
                }
                else if (effect.Kind.Equals(ForeverGuiStyleEffectService.StateSwitchKind, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"GUI element '{element.Id}' uses State Switch, but GUI-state runtime switching is not defined yet."));
                }
                else if (!SupportedRuntimeEffects.Contains(effect.Kind))
                {
                    issues.Add(new StudioValidationIssue(
                        StudioValidationSeverity.Error,
                        $"GUI element '{element.Id}' uses unsupported runtime effect '{effect.Kind}'."));
                }
            }

            if (style is null && effects.Count == 0)
                continue;

            elements[bindingId] = new Dictionary<string, object?>
            {
                ["elementId"] = element.Id,
                ["style"] = style is null ? null : style.Properties,
                ["effects"] = effects.Select(effect => new Dictionary<string, object?>
                {
                    ["kind"] = effect.Kind,
                    ["properties"] = effect.Properties
                }).ToArray()
            };
        }

        if (issues.Any(issue => issue.Severity == StudioValidationSeverity.Error))
            return plan with { Issues = issues };

        var relativePath = $"mod/src/main/resources/data/forever_skyblock/forever_studio/gui/{document.AssetId}.json";
        var root = Path.GetFullPath(repoRoot.Trim());
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var text = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schemaVersion"] = SchemaVersion,
            ["machine"] = $"forever_skyblock:{document.AssetId}",
            ["elements"] = elements
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        var normalized = NormalizeNewlines(text);
        var existed = File.Exists(fullPath);
        var current = existed ? NormalizeNewlines(File.ReadAllText(fullPath)) : null;

        var changes = plan.Changes
            .Where(change => !change.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.Equals(current, normalized, StringComparison.Ordinal))
        {
            changes.Add(new ForeverStudioMachineProductionChange
            {
                Kind = ForeverStudioMachineProductionChangeKind.BuildManifest,
                RelativePath = relativePath,
                Description = "Compile shared GUI styles/effects into the Minecraft presentation sidecar.",
                NewText = normalized,
                PreviewLines = normalized.Split('\n').Take(120).ToList(),
                FullPath = fullPath,
                OriginalHash = existed ? HashFile(fullPath) : MissingHash,
                ExistedBefore = existed
            });
        }

        return plan with { Changes = changes, Issues = issues };
    }

    private static ForeverStudioMachineProductionPlan AddError(ForeverStudioMachineProductionPlan plan, string message)
        => plan with
        {
            Issues = plan.Issues.Concat([
                new StudioValidationIssue(StudioValidationSeverity.Error, message)
            ]).ToList()
        };

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}

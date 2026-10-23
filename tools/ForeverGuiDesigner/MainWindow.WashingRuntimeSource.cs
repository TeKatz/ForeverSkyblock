using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Reads the Washing preview metadata from the selected ForeverSkyblock checkout.
/// Geometry is discovered by ForeverProjectScanner; this companion keeps dynamic
/// text, recipe examples and processing duration tied to the same runtime sources
/// instead of duplicating them as Phase 6 demo constants.
/// </summary>
public partial class MainWindow
{
    private sealed record WashingRuntimeProfile(
        string Title,
        string Running,
        string Idle,
        string Blocked,
        string NoWater,
        string WaterConnected,
        string WaterRequired,
        string Input,
        string Output,
        string Recipes,
        string HintSource,
        string HintWater,
        string RecipeSummary,
        double DurationSeconds)
    {
        public static WashingRuntimeProfile Fallback { get; } = new(
            "WASHING BASIN",
            "WASHING",
            "IDLE",
            "OUTPUT FULL",
            "NO WATER",
            "WATER SOURCE CONNECTED",
            "WATER SOURCE REQUIRED",
            "INPUT",
            "OUTPUT",
            "Recipes",
            "Source water on any face.",
            "Water is not consumed.",
            "4 × Stone Meal → Raw Iron",
            8.0);
    }

    private WashingRuntimeProfile _washingRuntimeProfile = WashingRuntimeProfile.Fallback;
    private string? _washingRuntimeProfileRoot;
    private DateTime _washingRuntimeProfileStampUtc;

    private double WashingRuntimeDurationSeconds
        => Math.Max(.05, _washingRuntimeProfile.DurationSeconds);

    // Exactly one progress value owns every Washing runtime surface. The fixed
    // percentage combo remains useful outside Active Processing, while the exclusive
    // controller owns the live timeline whenever Washing has exclusive ownership.
    private double WashingVisualProgress
        => _washingExclusiveModeActive
            ? Math.Clamp(_phase8TimelineProgress, 0, 1)
            : Math.Clamp(CurrentProgress, 0, 1);

    private void RefreshWashingRuntimeProfile(bool force = false)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
        {
            _washingRuntimeProfile = WashingRuntimeProfile.Fallback;
            _washingRuntimeProfileRoot = null;
            _washingRuntimeProfileStampUtc = DateTime.MinValue;
            return;
        }

        var root = _repoRoot;
        var resourcesRoot = Path.Combine(root, "mod", "src", "main", "resources");
        var javaRoot = Path.Combine(root, "mod", "src", "main", "java");
        var langPath = Path.Combine(resourcesRoot, "assets", "forever_skyblock", "lang", "en_us.json");
        var recipeClassPath = Directory.Exists(javaRoot)
            ? Directory.EnumerateFiles(javaRoot, "WashingRecipe.java", SearchOption.AllDirectories).FirstOrDefault()
            : null;
        var washingRecipeRoot = Path.Combine(resourcesRoot, "data", "forever_skyblock", "recipe", "washing");
        var preferredRecipePath = Path.Combine(washingRecipeRoot, "raw_iron.json");
        var recipePath = File.Exists(preferredRecipePath)
            ? preferredRecipePath
            : Directory.Exists(washingRecipeRoot)
                ? Directory.EnumerateFiles(washingRecipeRoot, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault()
                : null;

        var stamp = LatestWriteUtc(langPath, recipeClassPath, recipePath);
        if (!force &&
            string.Equals(root, _washingRuntimeProfileRoot, StringComparison.OrdinalIgnoreCase) &&
            stamp == _washingRuntimeProfileStampUtc)
        {
            return;
        }

        try
        {
            var language = ReadLanguage(langPath);
            string T(string suffix, string fallback)
                => language.TryGetValue("gui.forever_skyblock.machine." + suffix, out var value) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : fallback;

            var defaultTicks = ReadDefaultWashingTicks(recipeClassPath) ?? 160;
            var recipe = ReadWashingRecipePreview(recipePath, language, defaultTicks);

            _washingRuntimeProfile = new WashingRuntimeProfile(
                T("washing_title", "WASHING BASIN"),
                T("washing", "WASHING"),
                T("idle", "IDLE"),
                T("blocked", "OUTPUT FULL"),
                T("no_water", "NO WATER"),
                T("water_connected", "WATER SOURCE CONNECTED"),
                T("water_required", "WATER SOURCE REQUIRED"),
                T("input", "INPUT"),
                T("output", "OUTPUT"),
                T("recipes", "Recipes"),
                T("washing_hint_source", "Source water on any face."),
                T("washing_hint_water", "Water is not consumed."),
                recipe.Summary ?? T("washing_recipe", "INPUT → OUTPUT + SECONDARY"),
                recipe.ProcessingTicks / 20.0);
        }
        catch
        {
            // A partially edited source tree must not break the editor. The fallback
            // reflects the current runtime defaults and is replaced on the next scan.
            _washingRuntimeProfile = WashingRuntimeProfile.Fallback;
        }

        _washingRuntimeProfileRoot = root;
        _washingRuntimeProfileStampUtc = stamp;
    }

    private bool SynchronizeWashingRuntimeModelText()
    {
        if (!IsWashingPhase6Active)
            return false;

        var changed = false;
        void Set(string id, string value)
        {
            var model = _layout.Elements.FirstOrDefault(element =>
                string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase));
            if (model is null || string.Equals(model.Content, value, StringComparison.Ordinal))
                return;
            model.Content = value;
            changed = true;
        }

        Set("screen_title", _washingRuntimeProfile.Title);
        Set("forever_phase6_washing_status_text", CurrentWashingRuntimeStatusText);
        Set("forever_phase6_washing_water_text",
            WashingHasWater ? _washingRuntimeProfile.WaterConnected : _washingRuntimeProfile.WaterRequired);
        Set("forever_phase6_washing_label_input", _washingRuntimeProfile.Input);
        Set("forever_phase6_washing_label_output", _washingRuntimeProfile.Output);
        Set("forever_phase6_washing_time", FormatWashingRuntimeTime(WashingVisualProgress));
        Set("forever_phase6_washing_recipe_summary", _washingRuntimeProfile.RecipeSummary);
        Set("forever_phase6_washing_hint_source", _washingRuntimeProfile.HintSource);
        Set("forever_phase6_washing_hint_water", _washingRuntimeProfile.HintWater);

        foreach (var button in _layout.Elements.Where(element =>
                     element.Type == GuiElementType.Button &&
                     string.Equals(element.Id, "recipes", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.Equals(button.Content, _washingRuntimeProfile.Recipes, StringComparison.Ordinal))
                continue;
            button.Content = _washingRuntimeProfile.Recipes;
            changed = true;
        }

        return changed;
    }

    private string CurrentWashingRuntimeStatusText
        => CurrentWashingStatus switch
        {
            "blocked" => _washingRuntimeProfile.Blocked,
            "no_water" => _washingRuntimeProfile.NoWater,
            "idle" => _washingRuntimeProfile.Idle,
            _ => _washingRuntimeProfile.Running
        };

    private string FormatWashingRuntimeTime(double progress)
    {
        var duration = WashingRuntimeDurationSeconds;
        return $"{Math.Clamp(progress, 0, 1) * duration:0.0} / {duration:0.0} s";
    }

    private static Dictionary<string, string> ReadLanguage(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                result[property.Name] = property.Value.GetString() ?? string.Empty;
        }
        return result;
    }

    private static int? ReadDefaultWashingTicks(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        var source = File.ReadAllText(path);
        var match = Regex.Match(source, @"DEFAULT_TIME\s*=\s*(?<ticks>\d+)");
        return match.Success && int.TryParse(match.Groups["ticks"].Value, out var ticks) ? ticks : null;
    }

    private static (string? Summary, int ProcessingTicks) ReadWashingRecipePreview(
        string? path,
        IReadOnlyDictionary<string, string> language,
        int defaultTicks)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return (null, defaultTicks);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ticks = root.TryGetProperty("processing_time", out var processing) && processing.TryGetInt32(out var parsedTicks)
            ? parsedTicks
            : defaultTicks;

        var ingredientCount = root.TryGetProperty("ingredient_count", out var count) && count.TryGetInt32(out var parsedCount)
            ? parsedCount
            : 1;
        var ingredientId = ReadResourceId(root, "ingredient");
        var resultId = ReadResourceId(root, "result");
        if (ingredientId is null || resultId is null)
            return (null, ticks);

        var summary = $"{ingredientCount} × {ResolveResourceName(ingredientId, language)} → {ResolveResourceName(resultId, language)}";

        if (root.TryGetProperty("secondary_result", out var secondary) && secondary.ValueKind == JsonValueKind.Object)
        {
            var secondaryId = ReadResourceId(secondary);
            if (secondaryId is not null)
            {
                var secondaryCount = secondary.TryGetProperty("count", out var secondaryCountElement) && secondaryCountElement.TryGetInt32(out var value)
                    ? value
                    : 1;
                summary += $" + {secondaryCount}× {ResolveResourceName(secondaryId, language)}";
            }
        }

        if (root.TryGetProperty("bonus_result", out var bonus) && bonus.ValueKind == JsonValueKind.Object)
        {
            var bonusId = ReadResourceId(bonus);
            if (bonusId is not null && root.TryGetProperty("bonus_chance", out var chance) && chance.TryGetInt32(out var chanceValue) && chanceValue > 0)
                summary += $" + {chanceValue}% {ResolveResourceName(bonusId, language)}";
        }

        return (summary, Math.Max(1, ticks));
    }

    private static string? ReadResourceId(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) ? ReadResourceId(value) : null;

    private static string? ReadResourceId(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString();
        if (value.ValueKind != JsonValueKind.Object)
            return null;
        if (value.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            return id.GetString();
        if (value.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.String)
            return item.GetString();
        return null;
    }

    private static string ResolveResourceName(string resourceId, IReadOnlyDictionary<string, string> language)
    {
        var parts = resourceId.Split(':', 2);
        var resourceNamespace = parts.Length == 2 ? parts[0] : "minecraft";
        var path = parts.Length == 2 ? parts[1] : parts[0];

        foreach (var prefix in new[] { "item", "block" })
        {
            var key = $"{prefix}.{resourceNamespace}.{path}";
            if (language.TryGetValue(key, out var translated) && !string.IsNullOrWhiteSpace(translated))
                return translated;
        }

        var words = path.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word));
        return string.Join(' ', words);
    }

    private static DateTime LatestWriteUtc(params string?[] paths)
    {
        var latest = DateTime.MinValue;
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;
            var write = File.GetLastWriteTimeUtc(path);
            if (write > latest)
                latest = write;
        }
        return latest;
    }
}

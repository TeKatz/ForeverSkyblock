using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private sealed record MachineRuntimeProfile(string ScreenClass, string Title, string Recipe, double DurationSeconds, double AlternateDurationSeconds = 0);

    private MachineRuntimeProfile? _machineRuntimeProfile;
    private string? _machineRuntimeProfileRoot;
    private DateTime _machineRuntimeProfileStampUtc;

    private void RefreshMachineRuntimeProfile(bool force = false)
    {
        var screen = _activeForeverTemplate?.ScreenClass ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
        {
            _machineRuntimeProfile = MachineRuntimeFallback(screen);
            return;
        }

        var root = _repoRoot;
        var resources = Path.Combine(root, "mod", "src", "main", "resources");
        var java = Path.Combine(root, "mod", "src", "main", "java");
        var lang = Path.Combine(resources, "assets", "forever_skyblock", "lang", "en_us.json");
        string? primary = null, alternate = null, blockEntity = null;

        switch (screen)
        {
            case "RefiningScreen": primary = FirstRuntimeJson(Path.Combine(resources, "data", "forever_skyblock", "recipe", "refining")); break;
            case "CondenserScreen":
                primary = FirstRuntimeJson(Path.Combine(resources, "data", "forever_skyblock", "recipe", "condensing"));
                alternate = FirstRuntimeJson(Path.Combine(resources, "data", "forever_skyblock", "recipe", "garden"));
                break;
            case "AscensionScreen": blockEntity = FindRuntimeJava(java, "AscensionChamberBlockEntity.java"); break;
            case "CrucibleScreen": blockEntity = FindRuntimeJava(java, "CrucibleBlockEntity.java"); break;
        }

        var stamp = LatestWriteUtc(lang, primary, alternate, blockEntity);
        if (!force && string.Equals(root, _machineRuntimeProfileRoot, StringComparison.OrdinalIgnoreCase) &&
            stamp == _machineRuntimeProfileStampUtc && string.Equals(_machineRuntimeProfile?.ScreenClass, screen, StringComparison.Ordinal)) return;

        var fallback = MachineRuntimeFallback(screen);
        try
        {
            var language = ReadLanguage(lang);
            var duration = fallback.DurationSeconds;
            var alternateDuration = fallback.AlternateDurationSeconds;
            var recipe = fallback.Recipe;

            if (primary is not null)
            {
                var parsed = ReadRuntimeRecipe(primary, language, duration);
                duration = parsed.Duration;
                if (!string.IsNullOrWhiteSpace(parsed.Summary)) recipe = parsed.Summary!;
            }
            if (alternate is not null)
                alternateDuration = ReadRuntimeRecipe(alternate, language, alternateDuration > 0 ? alternateDuration : duration).Duration;

            if (screen == "AscensionScreen" && blockEntity is not null)
                duration = ReadRuntimeConstant(blockEntity, "DURATION", 200) / 20.0;
            if (screen == "CrucibleScreen" && blockEntity is not null)
            {
                alternateDuration = ReadRuntimeConstant(blockEntity, "FREEZE_DURATION", 100) / 20.0;
                var cobble = ReadRuntimeConstant(blockEntity, "COBBLE_PER_BATCH", 8);
                var cobbleMb = ReadRuntimeConstant(blockEntity, "COBBLE_LAVA_MB", 1000);
                var fragments = ReadRuntimeConstant(blockEntity, "FRAGMENTS_PER_BATCH", 40);
                var fragmentsMb = ReadRuntimeConstant(blockEntity, "FRAGMENTS_LAVA_MB", 100);
                recipe = $"{cobble} Cobble → {cobbleMb} mB · {fragments} Fragments → {fragmentsMb} mB";
            }

            string T(string key, string value) => language.TryGetValue("gui.forever_skyblock.machine." + key, out var translated) && !string.IsNullOrWhiteSpace(translated) ? translated : value;
            var title = screen switch
            {
                "CondenserScreen" => T(CurrentTier == "mk2" ? "condenser_title_ii" : "condenser_title", CurrentTier == "mk2" ? "CONDENSER MK II" : "CONDENSER MK I"),
                "AscensionScreen" => T("ascension_title", "ASCENSION CHAMBER"),
                "RefiningScreen" => "REFINING CONDENSER",
                "CrucibleScreen" => "CRUCIBLE",
                _ => fallback.Title
            };
            _machineRuntimeProfile = new(screen, title, recipe, Math.Max(.05, duration), Math.Max(0, alternateDuration));
        }
        catch { _machineRuntimeProfile = fallback; }

        _machineRuntimeProfileRoot = root;
        _machineRuntimeProfileStampUtc = stamp;
    }

    private static MachineRuntimeProfile MachineRuntimeFallback(string screen) => screen switch
    {
        "CrucibleScreen" => new(screen, "CRUCIBLE", "8 Cobble → 1000 mB · 40 Fragments → 100 mB", 8, 5),
        "RefiningScreen" => new(screen, "REFINING CONDENSER", "Material + Catalyst → Refined output", 8),
        "AscensionScreen" => new(screen, "ASCENSION CHAMBER", "1 Forever Core → 1 Perk Point", 10),
        "CondenserScreen" => new(screen, "CONDENSER MK I", "Wood → Stone Fragments", 5, 5),
        _ => new(screen, screen, string.Empty, 5)
    };

    private static string? FindRuntimeJava(string root, string name) => Directory.Exists(root) ? Directory.EnumerateFiles(root, name, SearchOption.AllDirectories).FirstOrDefault() : null;
    private static string? FirstRuntimeJson(string directory) => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault() : null;

    private static int ReadRuntimeConstant(string path, string name, int fallback)
    {
        if (!File.Exists(path)) return fallback;
        var match = Regex.Match(File.ReadAllText(path), $@"\b{Regex.Escape(name)}\s*=\s*(?<value>\d+)");
        return match.Success && int.TryParse(match.Groups["value"].Value, out var value) ? value : fallback;
    }

    private static (string? Summary, double Duration) ReadRuntimeRecipe(string path, IReadOnlyDictionary<string, string> language, double fallbackSeconds)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ticks = root.TryGetProperty("processing_time", out var processing) && processing.TryGetInt32(out var value) ? Math.Max(1, value) : Math.Max(1, (int)Math.Round(fallbackSeconds * 20));
        string? input = null, output = null;
        foreach (var key in new[] { "ingredient", "input", "primary", "first" }) { input = ReadResourceId(root, key); if (input is not null) break; }
        foreach (var key in new[] { "result", "output" }) { output = ReadResourceId(root, key); if (output is not null) break; }
        var summary = input is not null && output is not null ? $"{ResolveResourceName(input, language)} → {ResolveResourceName(output, language)}" : null;
        return (summary, ticks / 20.0);
    }
}

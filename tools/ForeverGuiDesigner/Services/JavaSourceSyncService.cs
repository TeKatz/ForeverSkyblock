using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

public enum JavaSyncDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record JavaSyncDiagnostic(JavaSyncDiagnosticSeverity Severity, string Message)
{
    public override string ToString() => $"{Severity.ToString().ToUpperInvariant()} · {Message}";
}

public sealed record JavaSourceFileChange(
    string RelativePath,
    string FullPath,
    string OriginalText,
    string UpdatedText);

public sealed class JavaSourceSyncPlan
{
    public List<JavaSourceFileChange> Changes { get; } = [];
    public List<JavaSyncDiagnostic> Diagnostics { get; } = [];
    public bool CanApply => Changes.Count > 0 && Diagnostics.All(item => item.Severity != JavaSyncDiagnosticSeverity.Error);
    public string Diff => JavaSourceSyncService.BuildDiff(Changes);
}

public static class JavaSourceSyncService
{
    private sealed record TextReplacement(int Start, int Length, string Value);
    private sealed record MachineBinding(
        string? XConstant,
        string? YConstant,
        int? LiteralXStart,
        int? LiteralXLength,
        int? LiteralYStart,
        int? LiteralYLength,
        int CurrentX,
        int CurrentY);

    public static JavaSourceSyncPlan CreatePlan(
        string repoRoot,
        ForeverMachineTemplate template,
        GuiLayoutModel layout)
    {
        var plan = new JavaSourceSyncPlan();
        var root = Path.GetFullPath(repoRoot);
        var screenPath = ResolveInsideRepository(root, template.ScreenPath);
        if (screenPath is null || !File.Exists(screenPath))
        {
            plan.Diagnostics.Add(new JavaSyncDiagnostic(
                JavaSyncDiagnosticSeverity.Error,
                $"Screen source is missing: {template.ScreenPath}"));
            return plan;
        }

        var screenSource = File.ReadAllText(screenPath);
        var screenReplacements = new List<TextReplacement>();

        string? menuPath = null;
        string? menuSource = null;
        var menuReplacements = new List<TextReplacement>();
        if (!string.IsNullOrWhiteSpace(template.MenuPath))
        {
            menuPath = ResolveInsideRepository(root, template.MenuPath!);
            if (menuPath is not null && File.Exists(menuPath))
                menuSource = File.ReadAllText(menuPath);
        }

        PlanMachineSlots(plan, template, layout, screenSource, menuSource, screenReplacements, menuReplacements);
        PlanButtons(plan, layout, screenSource, screenReplacements);

        if (plan.Diagnostics.Any(item => item.Severity == JavaSyncDiagnosticSeverity.Error))
            return plan;

        var updatedScreen = ApplyReplacements(screenSource, screenReplacements);
        if (!string.Equals(screenSource, updatedScreen, StringComparison.Ordinal))
        {
            plan.Changes.Add(new JavaSourceFileChange(
                template.ScreenPath,
                screenPath,
                screenSource,
                updatedScreen));
        }

        if (menuPath is not null && menuSource is not null)
        {
            var updatedMenu = ApplyReplacements(menuSource, menuReplacements);
            if (!string.Equals(menuSource, updatedMenu, StringComparison.Ordinal))
            {
                plan.Changes.Add(new JavaSourceFileChange(
                    template.MenuPath!,
                    menuPath,
                    menuSource,
                    updatedMenu));
            }
        }

        if (plan.Changes.Count == 0)
        {
            plan.Diagnostics.Add(new JavaSyncDiagnostic(
                JavaSyncDiagnosticSeverity.Info,
                "No source-backed geometry differs from the current Java sources."));
        }
        else
        {
            plan.Diagnostics.Add(new JavaSyncDiagnostic(
                JavaSyncDiagnosticSeverity.Info,
                $"{plan.Changes.Count} Java file(s) contain reviewed geometry changes."));
        }

        return plan;
    }

    private static void PlanMachineSlots(
        JavaSourceSyncPlan plan,
        ForeverMachineTemplate template,
        GuiLayoutModel layout,
        string screenSource,
        string? menuSource,
        List<TextReplacement> screenReplacements,
        List<TextReplacement> menuReplacements)
    {
        if (string.IsNullOrWhiteSpace(menuSource))
        {
            if (layout.Elements.Any(element => element.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase)))
            {
                plan.Diagnostics.Add(new JavaSyncDiagnostic(
                    JavaSyncDiagnosticSeverity.Error,
                    "The active screen has machine slots, but its Menu source is unavailable for safe constant resolution."));
            }
            return;
        }

        var constants = ParseIntConstants(menuSource);
        var bindings = new List<MachineBinding>();
        var seenCoordinates = new HashSet<(int X, int Y)>();
        var menuClass = Regex.Escape(template.MenuClass);
        var constantPattern = new Regex(
            $@"skin\.slot\s*\(\s*g\s*,\s*x\s*\+\s*{menuClass}\.(?<x>[A-Z0-9_]+)\s*,\s*y\s*\+\s*{menuClass}\.(?<y>[A-Z0-9_]+)\s*,\s*true\s*\)",
            RegexOptions.Multiline);

        foreach (Match match in constantPattern.Matches(screenSource))
        {
            var xConstant = match.Groups["x"].Value;
            var yConstant = match.Groups["y"].Value;
            if (!constants.TryGetValue(xConstant, out var x) || !constants.TryGetValue(yConstant, out var y))
            {
                plan.Diagnostics.Add(new JavaSyncDiagnostic(
                    JavaSyncDiagnosticSeverity.Error,
                    $"Could not resolve slot constants {xConstant}/{yConstant} in {template.MenuClass}."));
                continue;
            }
            if (!seenCoordinates.Add((x, y)))
                continue;
            bindings.Add(new MachineBinding(xConstant, yConstant, null, null, null, null, x, y));
        }

        var literalPattern = new Regex(
            @"skin\.slot\s*\(\s*g\s*,\s*x\s*\+\s*(?<x>\d+)\s*,\s*y\s*\+\s*(?<y>\d+)\s*,\s*true\s*\)",
            RegexOptions.Multiline);
        foreach (Match match in literalPattern.Matches(screenSource))
        {
            var x = int.Parse(match.Groups["x"].Value);
            var y = int.Parse(match.Groups["y"].Value);
            if (!seenCoordinates.Add((x, y)))
                continue;
            bindings.Add(new MachineBinding(
                null,
                null,
                match.Groups["x"].Index,
                match.Groups["x"].Length,
                match.Groups["y"].Index,
                match.Groups["y"].Length,
                x,
                y));
        }

        var elements = layout.Elements
            .Select(element => (Element: element, Index: MachineSlotIndex(element.Id)))
            .Where(item => item.Index is not null)
            .OrderBy(item => item.Index)
            .Select(item => item.Element)
            .ToList();

        if (bindings.Count != elements.Count)
        {
            plan.Diagnostics.Add(new JavaSyncDiagnostic(
                JavaSyncDiagnosticSeverity.Error,
                $"Source binding mismatch: Java exposes {bindings.Count} machine slot(s), but the editable layout exposes {elements.Count}. Reload the machine before applying back."));
            return;
        }

        var constantRequirements = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < bindings.Count; index++)
        {
            var binding = bindings[index];
            var element = elements[index];
            if (element.Width != 24 || element.Height != 24)
            {
                plan.Diagnostics.Add(new JavaSyncDiagnostic(
                    JavaSyncDiagnosticSeverity.Error,
                    $"{element.Id} must remain 24×24 before it can be applied to MachineSkin.slot(..., true)."));
                continue;
            }

            var desiredX = element.X + 4;
            var desiredY = element.Y + 4;
            if (binding.XConstant is not null)
                AddConstantRequirement(plan, constantRequirements, binding.XConstant, desiredX);
            else if (binding.LiteralXStart is int xStart && binding.LiteralXLength is int xLength && desiredX != binding.CurrentX)
                screenReplacements.Add(new TextReplacement(xStart, xLength, desiredX.ToString()));

            if (binding.YConstant is not null)
                AddConstantRequirement(plan, constantRequirements, binding.YConstant, desiredY);
            else if (binding.LiteralYStart is int yStart && binding.LiteralYLength is int yLength && desiredY != binding.CurrentY)
                screenReplacements.Add(new TextReplacement(yStart, yLength, desiredY.ToString()));
        }

        if (plan.Diagnostics.Any(item => item.Severity == JavaSyncDiagnosticSeverity.Error))
            return;

        foreach (var requirement in constantRequirements)
        {
            var assignment = Regex.Match(
                menuSource,
                $@"\b{Regex.Escape(requirement.Key)}\s*=\s*(?<value>-?\d+)",
                RegexOptions.Multiline);
            if (!assignment.Success)
            {
                plan.Diagnostics.Add(new JavaSyncDiagnostic(
                    JavaSyncDiagnosticSeverity.Error,
                    $"Could not locate the declaration of {template.MenuClass}.{requirement.Key}."));
                continue;
            }

            var valueGroup = assignment.Groups["value"];
            if (!int.TryParse(valueGroup.Value, out var current) || current == requirement.Value)
                continue;
            menuReplacements.Add(new TextReplacement(valueGroup.Index, valueGroup.Length, requirement.Value.ToString()));
        }
    }

    private static void AddConstantRequirement(
        JavaSourceSyncPlan plan,
        Dictionary<string, int> requirements,
        string name,
        int value)
    {
        if (requirements.TryGetValue(name, out var existing) && existing != value)
        {
            plan.Diagnostics.Add(new JavaSyncDiagnostic(
                JavaSyncDiagnosticSeverity.Error,
                $"Shared Java constant {name} would need two values ({existing} and {value}). Move the linked slots together or refactor the Java source manually."));
            return;
        }
        requirements[name] = value;
    }

    private static void PlanButtons(
        JavaSourceSyncPlan plan,
        GuiLayoutModel layout,
        string screenSource,
        List<TextReplacement> replacements)
    {
        var pattern = new Regex(
            @"(?:(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*)?addRenderableWidget\s*\(\s*new\s+MachineButton\s*\(\s*leftPos\s*\+\s*(?<x>\d+)\s*,\s*topPos\s*\+\s*(?<y>\d+)\s*,\s*(?<w>\d+)\s*,\s*(?<h>\d+)",
            RegexOptions.Singleline);
        var matches = pattern.Matches(screenSource).Cast<Match>().ToList();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var boundElements = new HashSet<GuiElementModel>();
        var anonymousIndex = 1;

        foreach (var match in matches)
        {
            var rawName = match.Groups["name"].Value;
            var baseId = string.IsNullOrWhiteSpace(rawName) ? $"button_{anonymousIndex++}" : rawName;
            var id = baseId;
            var suffix = 2;
            while (!usedIds.Add(id))
                id = baseId + "_" + suffix++;

            var element = layout.Elements.FirstOrDefault(candidate =>
                candidate.Type == GuiElementType.Button &&
                string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
            if (element is null)
            {
                plan.Diagnostics.Add(new JavaSyncDiagnostic(
                    JavaSyncDiagnosticSeverity.Error,
                    $"Source-backed button '{id}' is no longer identifiable in the layout. Reload the machine or restore its imported id before applying back."));
                continue;
            }
            boundElements.Add(element);

            AddNumericReplacement(match.Groups["x"], element.X, replacements);
            AddNumericReplacement(match.Groups["y"], element.Y, replacements);
            AddNumericReplacement(match.Groups["w"], element.Width, replacements);
            AddNumericReplacement(match.Groups["h"], element.Height, replacements);
        }

        var editorOnlyButtons = layout.Elements
            .Where(element => element.Type == GuiElementType.Button && !boundElements.Contains(element))
            .Select(element => element.Id)
            .ToList();
        if (editorOnlyButtons.Count > 0)
        {
            plan.Diagnostics.Add(new JavaSyncDiagnostic(
                JavaSyncDiagnosticSeverity.Warning,
                $"Editor-only button(s) are not written automatically: {string.Join(", ", editorOnlyButtons)}."));
        }
    }

    private static void AddNumericReplacement(Group group, int desired, List<TextReplacement> replacements)
    {
        if (!int.TryParse(group.Value, out var current) || current == desired)
            return;
        replacements.Add(new TextReplacement(group.Index, group.Length, desired.ToString()));
    }

    private static int? MachineSlotIndex(string id)
    {
        var match = Regex.Match(id ?? string.Empty, @"^machine_slot_(?<index>\d+)$", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["index"].Value, out var index) ? index : null;
    }

    private static Dictionary<string, int> ParseIntConstants(string source)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Match declaration in Regex.Matches(source, @"public\s+static\s+final\s+int\s+(?<body>[^;]+);", RegexOptions.Multiline))
        {
            foreach (var part in declaration.Groups["body"].Value.Split(','))
            {
                var assignment = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (assignment.Length == 2 && int.TryParse(assignment[1], out var value))
                    result[assignment[0].Trim()] = value;
            }
        }
        return result;
    }

    private static string ApplyReplacements(string source, IEnumerable<TextReplacement> replacements)
    {
        var ordered = replacements
            .DistinctBy(item => (item.Start, item.Length, item.Value))
            .OrderByDescending(item => item.Start)
            .ToList();
        var builder = new StringBuilder(source);
        var lastStart = source.Length;
        foreach (var replacement in ordered)
        {
            if (replacement.Start < 0 || replacement.Length < 0 || replacement.Start + replacement.Length > source.Length)
                throw new InvalidOperationException("A source replacement was outside the source file.");
            if (replacement.Start + replacement.Length > lastStart)
                throw new InvalidOperationException("Overlapping Java source replacements were generated.");
            builder.Remove(replacement.Start, replacement.Length);
            builder.Insert(replacement.Start, replacement.Value);
            lastStart = replacement.Start;
        }
        return builder.ToString();
    }

    private static string? ResolveInsideRepository(string root, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    public static void Apply(JavaSourceSyncPlan plan)
    {
        if (!plan.CanApply)
            throw new InvalidOperationException("The Java sync plan contains blocking diagnostics or has no changes.");

        foreach (var change in plan.Changes)
        {
            if (!File.Exists(change.FullPath) ||
                !string.Equals(File.ReadAllText(change.FullPath), change.OriginalText, StringComparison.Ordinal))
            {
                throw new IOException($"Source changed after review: {change.RelativePath}. Rebuild the diff before applying.");
            }
        }

        var staged = new List<(JavaSourceFileChange Change, string Temp, string Backup)>();
        try
        {
            foreach (var change in plan.Changes)
            {
                var directory = Path.GetDirectoryName(change.FullPath)
                                ?? throw new IOException($"Could not resolve directory for {change.FullPath}.");
                var token = Guid.NewGuid().ToString("N");
                var temp = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.{token}.fgd.tmp");
                var backup = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.{token}.fgd.bak");
                File.WriteAllText(temp, change.UpdatedText, new UTF8Encoding(false));
                staged.Add((change, temp, backup));
            }

            foreach (var item in staged)
            {
                if (!string.Equals(File.ReadAllText(item.Change.FullPath), item.Change.OriginalText, StringComparison.Ordinal))
                    throw new IOException($"Source changed while applying: {item.Change.RelativePath}.");
            }

            foreach (var item in staged)
                File.Replace(item.Temp, item.Change.FullPath, item.Backup, ignoreMetadataErrors: true);

            foreach (var item in staged)
            {
                if (File.Exists(item.Backup))
                    File.Delete(item.Backup);
            }
        }
        catch
        {
            foreach (var item in staged.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(item.Backup))
                        File.Copy(item.Backup, item.Change.FullPath, overwrite: true);
                }
                catch
                {
                    // Preserve the original exception; a remaining .fgd.bak still provides recovery material.
                }
            }
            throw;
        }
        finally
        {
            foreach (var item in staged)
            {
                try
                {
                    if (File.Exists(item.Temp)) File.Delete(item.Temp);
                    if (File.Exists(item.Backup)) File.Delete(item.Backup);
                }
                catch
                {
                }
            }
        }
    }

    public static string BuildDiff(IEnumerable<JavaSourceFileChange> changes)
    {
        var builder = new StringBuilder();
        foreach (var change in changes)
        {
            builder.AppendLine($"--- a/{change.RelativePath}");
            builder.AppendLine($"+++ b/{change.RelativePath}");
            var before = change.OriginalText.Replace("\r\n", "\n").Split('\n');
            var after = change.UpdatedText.Replace("\r\n", "\n").Split('\n');
            var count = Math.Max(before.Length, after.Length);
            for (var index = 0; index < count; index++)
            {
                var oldLine = index < before.Length ? before[index] : string.Empty;
                var newLine = index < after.Length ? after[index] : string.Empty;
                if (string.Equals(oldLine, newLine, StringComparison.Ordinal))
                    continue;
                builder.AppendLine($"@@ line {index + 1} @@");
                builder.AppendLine("-" + oldLine);
                builder.AppendLine("+" + newLine);
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }
}

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;
using Microsoft.Win32;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private readonly List<ForeverMachineTemplate> _foreverTemplates = [];
    private ForeverMachineTemplate? _activeForeverTemplate;
    private ComboBox? _foreverTemplateCombo;
    private bool _foreverToolsInitialized;

    public void EnableForeverSkyblockTools()
    {
        if (_foreverToolsInitialized)
            return;

        if (Content is not DockPanel root)
            return;

        var toolbar = root.Children.OfType<ToolBar>().FirstOrDefault();
        if (toolbar is null)
            return;

        _foreverToolsInitialized = true;

        toolbar.Items.Add(new Separator());
        toolbar.Items.Add(new TextBlock
        {
            Text = "Skyblock",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(7, 0, 3, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        var scanButton = new Button
        {
            Content = "Scan project",
            ToolTip = "Scan the selected ForeverSkyblock repository for MachineScreen/Menu layouts."
        };
        scanButton.Click += ScanForeverProject_Click;
        toolbar.Items.Add(scanButton);

        _foreverTemplateCombo = new ComboBox
        {
            Width = 170,
            DisplayMemberPath = nameof(ForeverMachineTemplate.DisplayName),
            ToolTip = "Machine templates detected directly from the current ForeverSkyblock Java sources."
        };
        _foreverTemplateCombo.SelectionChanged += ForeverTemplateCombo_SelectionChanged;
        toolbar.Items.Add(_foreverTemplateCombo);

        var loadButton = new Button
        {
            Content = "Load machine",
            ToolTip = "Create an editable GUI layout from the selected Forever machine screen."
        };
        loadButton.Click += LoadForeverTemplate_Click;
        toolbar.Items.Add(loadButton);

        var exportButton = new Button
        {
            Content = "Export Java",
            ToolTip = "Export the edited layout as GUI-local Java constants for the mod."
        };
        exportButton.Click += JavaExport_Click;
        toolbar.Items.Add(exportButton);
    }

    private void ScanForeverProject_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
        {
            MessageBox.Show(this,
                "Choose your local ForeverSkyblock repository with Repo… first.",
                "Forever project scan",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            var result = ForeverProjectScanner.Scan(_repoRoot);
            _foreverTemplates.Clear();
            _foreverTemplates.AddRange(result.Templates);

            if (_foreverTemplateCombo is not null)
            {
                _foreverTemplateCombo.ItemsSource = null;
                _foreverTemplateCombo.ItemsSource = _foreverTemplates;
                if (_foreverTemplates.Count > 0)
                    _foreverTemplateCombo.SelectedIndex = 0;
            }

            var warningText = result.Warnings.Count == 0 ? string.Empty : $" · {result.Warnings.Count} warning(s)";
            StatusText.Text = $"Forever project scan: {_foreverTemplates.Count} machine template(s){warningText}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Forever project scan failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ForeverTemplateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_foreverTemplateCombo?.SelectedItem is ForeverMachineTemplate template)
        {
            StatusText.Text =
                $"{template.ScreenClass} → {template.MenuClass} · MachineLayout.{template.LayoutKey} · {template.Summary}";
        }
    }

    private void LoadForeverTemplate_Click(object sender, RoutedEventArgs e)
        => LoadSelectedForeverTemplate();

    private void LoadSelectedForeverTemplate()
    {
        if (_foreverTemplateCombo?.SelectedItem is not ForeverMachineTemplate template)
            return;

        var layout = new GuiLayoutModel
        {
            Name = template.DisplayName,
            Width = template.Layout.Width,
            Height = template.Layout.Height,
            Elements = template.Layout.Elements.Select(element => element.Clone()).ToList()
        };

        _activeForeverTemplate = template;
        LoadLayoutIntoEditor(layout, null, clearHistory: true);
        _dirty = true;
        UpdateStatus();
        StatusText.Text =
            $"{template.DisplayName} imported from {template.ScreenClass} · {template.MachineSlotCount} machine slots · {template.ButtonCount} buttons";
    }

    private void JavaExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Forever Java layout constants",
            Filter = "Java source snippet (*.java)|*.java|Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = SanitizeJavaIdentifier(_layout.Name, pascalCase: true) + "Layout.java"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, BuildJavaExport(), new UTF8Encoding(false));
            StatusText.Text = $"Java layout export: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Java export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string BuildJavaExport()
    {
        var className = SanitizeJavaIdentifier(_layout.Name, pascalCase: true) + "Layout";
        var builder = new StringBuilder();
        builder.AppendLine("// Generated by Forever GUI Designer.");
        if (_activeForeverTemplate is not null)
        {
            builder.AppendLine($"// Imported from: {_activeForeverTemplate.ScreenPath}");
            if (_activeForeverTemplate.MenuPath is not null)
                builder.AppendLine($"// Menu source: {_activeForeverTemplate.MenuPath}");
            builder.AppendLine($"// Original layout: MachineLayout.{_activeForeverTemplate.LayoutKey}");
        }

        builder.AppendLine("// Coordinates are GUI-local. Imported machine slot constants use vanilla Slot.x/Slot.y;");
        builder.AppendLine("// button and other visual constants use their rendered top-left coordinates.");
        builder.AppendLine();
        builder.AppendLine($"public final class {className} {{");
        builder.AppendLine($"    public static final int GUI_WIDTH = {_layout.Width};");
        builder.AppendLine($"    public static final int GUI_HEIGHT = {_layout.Height};");
        builder.AppendLine();

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in _layout.Elements)
        {
            if (!element.Visible)
                continue;

            // Player inventory coordinates are derived from MachineLayout in the mod and are intentionally not duplicated.
            if (element.Id.StartsWith("player_inventory_", StringComparison.OrdinalIgnoreCase) ||
                element.Id.StartsWith("player_hotbar_", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(element.Id, "screen_title", StringComparison.OrdinalIgnoreCase))
                continue;

            var name = UniqueJavaIdentifier(SanitizeJavaIdentifier(element.Id).ToUpperInvariant(), used);
            var x = element.X;
            var y = element.Y;

            // MachineSkin.slot(..., machine=true) expands four pixels around vanilla Slot.x/Slot.y.
            if (element.Type == GuiElementType.Slot &&
                element.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase) &&
                element.Width == 24 && element.Height == 24)
            {
                x += 4;
                y += 4;
            }

            builder.AppendLine($"    public static final int {name}_X = {x};");
            builder.AppendLine($"    public static final int {name}_Y = {y};");

            if (element.Type is GuiElementType.Button or GuiElementType.Texture ||
                (element.Type == GuiElementType.Slot && (element.Width != 24 || element.Height != 24)))
            {
                builder.AppendLine($"    public static final int {name}_WIDTH = {element.Width};");
                builder.AppendLine($"    public static final int {name}_HEIGHT = {element.Height};");
            }

            builder.AppendLine();
        }

        builder.AppendLine($"    private {className}() {{ }}");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string UniqueJavaIdentifier(string preferred, HashSet<string> used)
    {
        var baseName = string.IsNullOrWhiteSpace(preferred) ? "ELEMENT" : preferred;
        var candidate = baseName;
        var index = 2;
        while (!used.Add(candidate))
            candidate = baseName + "_" + index++;
        return candidate;
    }

    private static string SanitizeJavaIdentifier(string value, bool pascalCase = false)
    {
        var parts = Regex.Split(value ?? string.Empty, @"[^A-Za-z0-9]+")
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        if (parts.Length == 0)
            return pascalCase ? "Gui" : "element";

        var result = pascalCase
            ? string.Concat(parts.Select(part =>
                char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part[1..] : string.Empty)))
            : string.Join("_", parts);

        if (char.IsDigit(result[0]))
            result = "_" + result;
        return result;
    }
}

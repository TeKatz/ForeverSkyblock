using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Extends the Phase-04 recipe card without replacing the familiar single-stack quick editor.
/// Advanced recipes use compact deterministic lists so dual-input/output, fluids and generators can
/// be authored today while older projects remain editable through the legacy fields.
/// </summary>
public partial class MainWindow
{
    private bool _studioMultiResourceRecipeAuthoringEnabled;
    private TextBox? _studioRecipeInputsBox;
    private TextBox? _studioRecipeOutputsBox;
    private TextBox? _studioRecipeFluidInputsBox;
    private TextBox? _studioRecipeFluidOutputsBox;
    private TextBox? _studioRecipeEnergyOutputBox;

    public void EnableForeverStudioMultiResourceRecipeAuthoring()
    {
        if (_studioMultiResourceRecipeAuthoringEnabled)
            return;
        _studioMultiResourceRecipeAuthoringEnabled = true;

        void ScheduleInstall() => _ = Dispatcher.BeginInvoke(InstallStudioMultiResourceRecipeEditor, DispatcherPriority.ContextIdle);
        if (_machineBuilderGuiStep is not null)
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ScheduleInstall();
        if (_machineBuilderPhase5GuiTab is not null)
            _machineBuilderPhase5GuiTab.Click += (_, _) => ScheduleInstall();
        ScheduleInstall();
    }

    private void InstallStudioMultiResourceRecipeEditor()
    {
        if (_studioRecipeEnergyOutputBox is not null || _guiDesignerRecipeEnergyBox is null)
            return;

        FrameworkElement? cursor = _guiDesignerRecipeEnergyBox;
        StackPanel? host = null;
        while (cursor is not null)
        {
            if (cursor.Parent is StackPanel panel)
            {
                host = panel;
                break;
            }
            cursor = cursor.Parent as FrameworkElement;
        }
        if (host is null)
            return;

        var advanced = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        advanced.Children.Add(new Border
        {
            Background = Brush("#142429"),
            BorderBrush = Brush("#31525A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "MULTI-RESOURCE RECIPE\nUse semicolon-separated item stacks: minecraft:item*count. Fluids use tankNodeId|namespace:fluid|mB.",
                Foreground = Brush("#8EDDD9"),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            }
        });

        advanced.Children.Add(MachineBuilderVisualInspectorLabel("Inputs (ordered by Function slot)"));
        _studioRecipeInputsBox = MultiResourceBox("minecraft:cobblestone*1; minecraft:flint*1");
        advanced.Children.Add(_studioRecipeInputsBox);

        advanced.Children.Add(MachineBuilderVisualInspectorLabel("Outputs (ordered by Function slot)", 8));
        _studioRecipeOutputsBox = MultiResourceBox("minecraft:gravel*1; minecraft:sand*1");
        advanced.Children.Add(_studioRecipeOutputsBox);

        advanced.Children.Add(MachineBuilderVisualInspectorLabel("Fluid inputs", 8));
        _studioRecipeFluidInputsBox = MultiResourceBox("tank|minecraft:water|250", allowBlank: true);
        advanced.Children.Add(_studioRecipeFluidInputsBox);

        advanced.Children.Add(MachineBuilderVisualInspectorLabel("Fluid outputs", 8));
        _studioRecipeFluidOutputsBox = MultiResourceBox("tank|minecraft:water|250", allowBlank: true);
        advanced.Children.Add(_studioRecipeFluidOutputsBox);

        advanced.Children.Add(MachineBuilderVisualInspectorLabel("Generator FE/t output", 8));
        _studioRecipeEnergyOutputBox = MultiResourceBox("0", allowBlank: true);
        _studioRecipeEnergyOutputBox.Height = 30;
        advanced.Children.Add(_studioRecipeEnergyOutputBox);

        var save = CreateStudioChromeButton("SAVE MULTI-RESOURCE RECIPE", double.NaN, true);
        save.HorizontalAlignment = HorizontalAlignment.Stretch;
        save.Height = 36;
        save.Margin = new Thickness(0, 10, 0, 0);
        save.Click += (_, _) => CommitStudioMultiResourceRecipe();
        advanced.Children.Add(save);
        host.Children.Add(advanced);

        if (_guiDesignerRecipeList is not null)
            _guiDesignerRecipeList.SelectionChanged += (_, _) => LoadStudioMultiResourceRecipeSelection();
        LoadStudioMultiResourceRecipeSelection();
    }

    private TextBox MultiResourceBox(string example, bool allowBlank = false)
    {
        var box = MachineBuilderTextBox();
        box.Height = 42;
        box.AcceptsReturn = false;
        box.TextWrapping = TextWrapping.Wrap;
        box.ToolTip = allowBlank ? $"Optional. Example: {example}" : $"Example: {example}";
        return box;
    }

    private void LoadStudioMultiResourceRecipeSelection()
    {
        if ((_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe is not { } recipe)
            return;
        if (_studioRecipeInputsBox is not null)
            _studioRecipeInputsBox.Text = FormatRecipeStacks(recipe.Properties, "inputs", "input");
        if (_studioRecipeOutputsBox is not null)
            _studioRecipeOutputsBox.Text = FormatRecipeStacks(recipe.Properties, "outputs", "output");
        if (_studioRecipeFluidInputsBox is not null)
            _studioRecipeFluidInputsBox.Text = FormatRecipeFluids(recipe.Properties, "fluidInputs", "fluidInput");
        if (_studioRecipeFluidOutputsBox is not null)
            _studioRecipeFluidOutputsBox.Text = FormatRecipeFluids(recipe.Properties, "fluidOutputs", "fluidOutput");
        if (_studioRecipeEnergyOutputBox is not null)
        {
            var value = 0;
            if (recipe.Properties.TryGetValue("energy", out var energy) && energy.ValueKind == JsonValueKind.Object &&
                energy.TryGetProperty("feOutputPerTick", out var output) && output.TryGetInt32(out var parsed))
                value = parsed;
            _studioRecipeEnergyOutputBox.Text = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void CommitStudioMultiResourceRecipe()
    {
        if (_machineBuilderDocument is null)
            return;
        var id = SanitizeMachineBuilderGraphId(_guiDesignerRecipeIdBox?.Text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(id))
        {
            SetGuiDesignerStatus("Recipe ID is required.", "#E18B8B");
            return;
        }

        if (!TryParseRecipeStacks(_studioRecipeInputsBox?.Text, allowEmpty: false, out var inputs, out var inputError))
        {
            SetGuiDesignerStatus(inputError ?? "Invalid item input list.", "#E18B8B");
            return;
        }
        if (!TryParseRecipeStacks(_studioRecipeOutputsBox?.Text, allowEmpty: true, out var outputs, out var outputError))
        {
            SetGuiDesignerStatus(outputError ?? "Invalid item output list.", "#E18B8B");
            return;
        }
        if (!TryParseRecipeFluids(_studioRecipeFluidInputsBox?.Text, out var fluidInputs, out var fluidInputError))
        {
            SetGuiDesignerStatus(fluidInputError ?? "Invalid fluid input list.", "#E18B8B");
            return;
        }
        if (!TryParseRecipeFluids(_studioRecipeFluidOutputsBox?.Text, out var fluidOutputs, out var fluidOutputError))
        {
            SetGuiDesignerStatus(fluidOutputError ?? "Invalid fluid output list.", "#E18B8B");
            return;
        }
        if (!TryGuiDesignerInt(_guiDesignerRecipeDurationBox?.Text, 1, 1_000_000, out var duration) ||
            !TryGuiDesignerInt(_guiDesignerRecipeEnergyBox?.Text, 0, 1_000_000, out var fePerTick) ||
            !TryGuiDesignerInt(string.IsNullOrWhiteSpace(_studioRecipeEnergyOutputBox?.Text) ? "0" : _studioRecipeEnergyOutputBox!.Text,
                0, 1_000_000, out var feOutputPerTick))
        {
            SetGuiDesignerStatus("Duration, FE/t or generator FE/t output is outside the supported range.", "#E18B8B");
            return;
        }

        var process = _guiDesignerRecipeProcessCombo?.SelectedItem as GuiDesignerProcessNode;
        if (process is null)
        {
            SetGuiDesignerStatus("Select a processor Function node first.", "#E18B8B");
            return;
        }
        var isGenerator = process.LibraryItemId.Contains("generator", StringComparison.OrdinalIgnoreCase);
        if (!isGenerator && outputs.Count == 0)
        {
            SetGuiDesignerStatus("Processing recipes require at least one item output.", "#E18B8B");
            return;
        }
        if (isGenerator && feOutputPerTick <= 0)
        {
            SetGuiDesignerStatus("Generator recipes require a positive FE/t output.", "#E18B8B");
            return;
        }
        if (!process.LibraryItemId.Contains("fluid", StringComparison.OrdinalIgnoreCase) &&
            (fluidInputs.Count > 0 || fluidOutputs.Count > 0))
        {
            SetGuiDesignerStatus("Fluid stacks require a Fluid Processor.", "#E18B8B");
            return;
        }

        var selected = (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe;
        if (selected is null && _machineBuilderDocument.Recipes.Any(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
        {
            SetGuiDesignerStatus($"Recipe ID '{id}' already exists.", "#E18B8B");
            return;
        }
        var recipe = selected ?? new ForeverStudioMachineRecipeBinding();
        if (selected is null) _machineBuilderDocument.Recipes.Add(recipe);

        recipe.Id = id;
        recipe.RecipeTypeId = InferGuiDesignerRecipeType(process.LibraryItemId);
        recipe.Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["authoring"] = JsonSerializer.SerializeToElement("multi-resource-gui-designer"),
            ["processorNodeId"] = JsonSerializer.SerializeToElement(process.NodeId),
            ["inputs"] = JsonSerializer.SerializeToElement(inputs.Select(stack => new { item = stack.Item, count = stack.Count }).ToArray()),
            ["outputs"] = JsonSerializer.SerializeToElement(outputs.Select(stack => new { item = stack.Item, count = stack.Count }).ToArray()),
            ["processingTimeTicks"] = JsonSerializer.SerializeToElement(duration),
            ["energy"] = JsonSerializer.SerializeToElement(new
            {
                fePerTick,
                totalFe = (long)duration * fePerTick,
                feOutputPerTick
            })
        };
        if (fluidInputs.Count > 0)
            recipe.Properties["fluidInputs"] = JsonSerializer.SerializeToElement(fluidInputs.Select(fluid => new
            {
                tankNodeId = fluid.TankNodeId,
                fluid = fluid.Fluid,
                amount = fluid.Amount
            }).ToArray());
        if (fluidOutputs.Count > 0)
            recipe.Properties["fluidOutputs"] = JsonSerializer.SerializeToElement(fluidOutputs.Select(fluid => new
            {
                tankNodeId = fluid.TankNodeId,
                fluid = fluid.Fluid,
                amount = fluid.Amount
            }).ToArray());

        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerRecipes();
        ApplyStudioMultiResourcePreview(inputs, outputs);
        SetGuiDesignerStatus($"Recipe '{id}' saved: {inputs.Count} item input(s), {outputs.Count} item output(s), " +
                             $"{fluidInputs.Count} fluid input(s), {fluidOutputs.Count} fluid output(s).", "#74C99A");
    }

    private void ApplyStudioMultiResourcePreview(IReadOnlyList<StudioRecipeStack> inputs,
                                                  IReadOnlyList<StudioRecipeStack> outputs)
    {
        if (_machineBuilderDocument is null)
            return;
        var inputNodes = _machineBuilderDocument.Behavior.Nodes
            .Where(node => node.LibraryItemId is "function.item-input" or "function.fuel-slot")
            .OrderBy(node => node.Y).ThenBy(node => node.X).ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase).ToList();
        var outputNodes = _machineBuilderDocument.Behavior.Nodes
            .Where(node => node.LibraryItemId is "function.item-storage" or "function.item-output" or "function.secondary-output")
            .OrderBy(node => node.Y).ThenBy(node => node.X).ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var binding in _machineBuilderDocument.Gui.Bindings)
        {
            var inputIndex = inputNodes.FindIndex(node => node.Id.Equals(binding.SourceId, StringComparison.OrdinalIgnoreCase));
            if (inputIndex >= 0 && inputIndex < inputs.Count)
                binding.Properties["previewValue"] = JsonSerializer.SerializeToElement($"{inputs[inputIndex].Item}|{inputs[inputIndex].Count}");
            var outputIndex = outputNodes.FindIndex(node => node.Id.Equals(binding.SourceId, StringComparison.OrdinalIgnoreCase));
            if (outputIndex >= 0 && outputIndex < outputs.Count)
                binding.Properties["previewValue"] = JsonSerializer.SerializeToElement($"{outputs[outputIndex].Item}|{outputs[outputIndex].Count}");
        }
        RefreshGuiDesignerCanvas();
    }

    private sealed record StudioRecipeStack(string Item, int Count);
    private sealed record StudioRecipeFluid(string TankNodeId, string Fluid, int Amount);

    private static bool TryParseRecipeStacks(string? text, bool allowEmpty,
                                             out List<StudioRecipeStack> result, out string? error)
    {
        result = [];
        error = null;
        var entries = (text ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (entries.Length == 0)
        {
            if (allowEmpty) return true;
            return Fail("At least one item stack is required.", out error);
        }
        foreach (var entry in entries)
        {
            var parts = entry.Split('*', 2, StringSplitOptions.TrimEntries);
            var item = parts[0].Trim().ToLowerInvariant();
            if (!IsGuiDesignerResourceLocation(item)) return Fail($"'{item}' is not a valid item resource location.", out error);
            if (parts.Length != 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count is < 1 or > 64)
                return Fail($"Item '{item}' needs a count from 1 to 64 (use namespace:item*count).", out error);
            result.Add(new StudioRecipeStack(item, count));
        }
        return true;
    }

    private static bool TryParseRecipeFluids(string? text, out List<StudioRecipeFluid> result, out string? error)
    {
        result = [];
        error = null;
        foreach (var entry in (text ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split('|', 3, StringSplitOptions.TrimEntries);
            if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]) || !IsGuiDesignerResourceLocation(parts[1].ToLowerInvariant()) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) || amount < 1)
                return Fail($"Fluid '{entry}' must use tankNodeId|namespace:fluid|amount.", out error);
            result.Add(new StudioRecipeFluid(parts[0], parts[1].ToLowerInvariant(), amount));
        }
        return true;
    }

    private static bool Fail(string message, out string? error)
    {
        error = message;
        return false;
    }

    private static string FormatRecipeStacks(Dictionary<string, JsonElement> properties, string plural, string legacy)
    {
        var result = new List<string>();
        if (properties.TryGetValue(plural, out var stacks) && stacks.ValueKind == JsonValueKind.Array)
        {
            foreach (var stack in stacks.EnumerateArray())
                if (stack.TryGetProperty("item", out var item) && stack.TryGetProperty("count", out var count))
                    result.Add($"{item.GetString()}*{count.GetInt32()}");
        }
        else if (properties.TryGetValue(legacy, out var stack) && stack.ValueKind == JsonValueKind.Object &&
                 stack.TryGetProperty("item", out var item) && stack.TryGetProperty("count", out var count))
            result.Add($"{item.GetString()}*{count.GetInt32()}");
        return string.Join("; ", result);
    }

    private static string FormatRecipeFluids(Dictionary<string, JsonElement> properties, string plural, string legacy)
    {
        var values = new List<JsonElement>();
        if (properties.TryGetValue(plural, out var fluids) && fluids.ValueKind == JsonValueKind.Array)
            values.AddRange(fluids.EnumerateArray().Select(value => value.Clone()));
        else if (properties.TryGetValue(legacy, out var fluid) && fluid.ValueKind == JsonValueKind.Object)
            values.Add(fluid.Clone());
        return string.Join("; ", values.Select(value =>
        {
            var tank = value.TryGetProperty("tankNodeId", out var tankJson) ? tankJson.GetString() ?? "tank" : "tank";
            var id = value.TryGetProperty("fluid", out var fluidJson) ? fluidJson.GetString() ?? "minecraft:water" : "minecraft:water";
            var amount = value.TryGetProperty("amount", out var amountJson) && amountJson.TryGetInt32(out var parsed) ? parsed : 1000;
            return $"{tank}|{id}|{amount}";
        }));
    }
}

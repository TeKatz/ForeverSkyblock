using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder GUI phase after the unified-editor migration. Layout, widgets, bindings,
/// hierarchy, assets, styles and effects are edited only in the shared Forever GUI Editor.
/// Machine Builder keeps recipe authoring here because recipes are machine-domain data rather
/// than GUI-document data.
/// </summary>
public partial class MainWindow
{
    private sealed record GuiDesignerProcessNode(string NodeId, string DisplayName, string LibraryItemId)
    {
        public override string ToString() => DisplayName;
    }

    private sealed class GuiDesignerRecipeItem
    {
        public required ForeverStudioMachineRecipeBinding Recipe { get; init; }
        public override string ToString() => Recipe.Id;
    }

    private bool _guiDesignerPhase4Initialized;
    private FrameworkElement? _guiDesignerBody;
    private TextBlock? _guiDesignerStatus;
    private TextBlock? _guiDesignerSummary;

    private ListBox? _guiDesignerRecipeList;
    private TextBox? _guiDesignerRecipeIdBox;
    private ComboBox? _guiDesignerRecipeProcessCombo;
    private TextBox? _guiDesignerRecipeTypeText;
    private TextBox? _guiDesignerRecipeInputItemBox;
    private TextBox? _guiDesignerRecipeInputCountBox;
    private TextBox? _guiDesignerRecipeOutputItemBox;
    private TextBox? _guiDesignerRecipeOutputCountBox;
    private TextBox? _guiDesignerRecipeDurationBox;
    private TextBox? _guiDesignerRecipeEnergyBox;

    public void EnableForeverMachineBuilderGuiDesignerPhase4()
    {
        if (_guiDesignerPhase4Initialized)
            return;
        _guiDesignerPhase4Initialized = true;

        if (_machineBuilderGuiStep is not null)
        {
            if (_machineBuilderGuiStep.Child is Grid grid)
            {
                var copy = grid.Children.OfType<StackPanel>().FirstOrDefault();
                var description = copy?.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                if (description is not null)
                    description.Text = "Open the shared GUI editor and define the machine's actual recipes";
            }
            _machineBuilderGuiStep.MouseLeftButtonUp += (_, _) => ShowMachineBuilderGuiDesignerPhase4();
        }

        if (_machineBuilderPhase5GuiTab is not null)
        {
            var label = FindStudioVisualChildren<TextBlock>(_machineBuilderPhase5GuiTab).FirstOrDefault();
            if (label is not null)
                label.Text = "GUI & RECIPES";
            _machineBuilderPhase5GuiTab.Click += (_, _) => ShowMachineBuilderGuiDesignerPhase4();
        }

        if (_machineBuilderFunctionSurface is not null)
        {
            var continueButton = FindStudioVisualChildren<Button>(_machineBuilderFunctionSurface)
                .FirstOrDefault(button => string.Equals(
                    button.Tag?.ToString(),
                    "machine_builder_phase5_continue",
                    StringComparison.Ordinal));
            if (continueButton is not null)
            {
                var label = FindStudioVisualChildren<TextBlock>(continueButton).FirstOrDefault();
                if (label is not null)
                    label.Text = "GUI  →";
                continueButton.Click += (_, _) => ShowMachineBuilderGuiDesignerPhase4();
            }

            InstallFunctionDeleteShortcutForGuiWorkflow();
        }
    }

    private void InstallFunctionDeleteShortcutForGuiWorkflow()
    {
        if (_machineBuilderFunctionSurface is null)
            return;

        var actions = FindStudioVisualChildren<StackPanel>(_machineBuilderFunctionSurface)
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal &&
                panel.Children.OfType<Button>().Any(button =>
                    FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "AUTO LAYOUT")));
        if (actions is not null && !actions.Children.OfType<Button>().Any(button =>
                string.Equals(button.Tag?.ToString(), "function_delete_selected", StringComparison.Ordinal)))
        {
            var delete = CreateStudioChromeButton("DELETE SELECTED", 126, false);
            delete.Tag = "function_delete_selected";
            delete.Height = 31;
            delete.Margin = new Thickness(7, 0, 0, 0);
            delete.Click += (_, _) => DeleteSelectedFunctionNodeFromGuiWorkflow();
            actions.Children.Add(delete);
        }

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Delete || _machineBuilderFunctionSurface.Visibility != Visibility.Visible)
                return;
            DeleteSelectedFunctionNodeFromGuiWorkflow();
            e.Handled = true;
        };
    }

    private void DeleteSelectedFunctionNodeFromGuiWorkflow()
    {
        var node = GetSelectedMachineBuilderFunctionNode();
        if (node is null)
        {
            SetMachineBuilderFunctionStatus("Select a function node first.", "#D8B26A");
            return;
        }
        if (node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase))
        {
            SetMachineBuilderFunctionStatus("Machine Entry is required and cannot be deleted.", "#D8B26A");
            return;
        }
        DeleteMachineBuilderFunctionNode(node);
    }

    private void ShowMachineBuilderGuiDesignerPhase4()
    {
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);
        if (_machineBuilderDocument is null)
            return;

        ShowMachineBuilderPhase5(MachineBuilderPhase5Mode.Gui);
        if (_machineBuilderPhase5ModeHost is null)
            return;

        _guiDesignerBody ??= BuildMachineBuilderGuiDesignerBody();
        _machineBuilderPhase5ModeHost.Children.Clear();
        _machineBuilderPhase5ModeHost.Children.Add(_guiDesignerBody);
        RefreshMachineBuilderGuiDesigner();
    }

    private FrameworkElement BuildMachineBuilderGuiDesignerBody()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        root.ColumnDefinitions.Add(new ColumnDefinition());

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var stack = new StackPanel();
        stack.Children.Add(BuildSharedGuiEditorLaunchCard());
        var recipe = BuildGuiDesignerRecipeCard();
        recipe.Margin = new Thickness(0, 12, 0, 0);
        stack.Children.Add(recipe);
        scroll.Content = stack;
        Grid.SetColumn(scroll, 0);
        root.Children.Add(scroll);

        var info = BuildUnifiedGuiArchitectureCard();
        Grid.SetColumn(info, 2);
        root.Children.Add(info);
        return root;
    }

    private Border BuildSharedGuiEditorLaunchCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        card.Tag = "unified_gui_editor_launch";
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(GuiDesignerTitle(
            "SHARED GUI EDITOR",
            "There is now one GUI engine for standalone layouts and Machine Builder screens."));

        panel.Children.Add(new TextBlock
        {
            Text = "Layout, widgets, bindings, hierarchy, assets, styles and effects are edited in the established WYSIWYG editor. The retired Phase-04 mini canvas no longer owns GUI geometry.",
            Foreground = Brush("#82969E"),
            FontSize = 9.4,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 14,
            Margin = new Thickness(0, 2, 0, 10)
        });

        var open = CreateStudioChromeButton("OPEN SHARED GUI EDITOR", double.NaN, true);
        open.Height = 38;
        open.HorizontalAlignment = HorizontalAlignment.Stretch;
        open.Click += (_, _) => OpenMachineGuiInUnifiedEditor();
        panel.Children.Add(open);

        _guiDesignerStatus = new TextBlock
        {
            Text = "GUI authoring is routed through the shared document and editor context.",
            Foreground = Brush("#74C99A"),
            FontSize = 9.2,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        panel.Children.Add(_guiDesignerStatus);
        card.Child = panel;
        return card;
    }

    private Border BuildUnifiedGuiArchitectureCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock
        {
            Text = "ONE GUI ENGINE",
            Foreground = Brush("#72D5D3"),
            FontSize = 11,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Machine Builder now contributes machine-specific binding sources and recipe data to the shared editor instead of maintaining its own canvas, selection model or geometry inspector.\n\nRecipes stay here because they define machine behavior/runtime data, not GUI presentation.",
            Foreground = Brush("#82969E"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 10, 0, 16)
        });
        _guiDesignerSummary = new TextBlock
        {
            Foreground = Brush("#DCEBED"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(_guiDesignerSummary);
        card.Child = panel;
        return card;
    }

    private Border BuildGuiDesignerRecipeCard()
    {
        var card = CreateStudioGlassCard(new CornerRadius(8), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(GuiDesignerTitle(
            "ACTUAL RECIPES",
            "Define what the configured processor from Functions 03 really accepts and produces."));

        _guiDesignerRecipeList = Phase5ListBox(95);
        _guiDesignerRecipeList.SelectionChanged += (_, _) => LoadSelectedGuiDesignerRecipe();
        panel.Children.Add(_guiDesignerRecipeList);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Recipe ID", 10));
        _guiDesignerRecipeIdBox = MachineBuilderTextBox();
        _guiDesignerRecipeIdBox.Height = 30;
        _guiDesignerRecipeIdBox.Text = "recipe_1";
        panel.Children.Add(_guiDesignerRecipeIdBox);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Process from Functions 03", 8));
        _guiDesignerRecipeProcessCombo = Phase5ComboBox();
        _guiDesignerRecipeProcessCombo.SelectionChanged += (_, _) => RefreshGuiDesignerRecipeType();
        panel.Children.Add(_guiDesignerRecipeProcessCombo);

        _guiDesignerRecipeTypeText = MachineBuilderTextBox();
        _guiDesignerRecipeTypeText.Height = 28;
        _guiDesignerRecipeTypeText.IsReadOnly = true;
        _guiDesignerRecipeTypeText.Margin = new Thickness(0, 5, 0, 0);
        _guiDesignerRecipeTypeText.ToolTip = "Automatically derived from the selected process node.";
        panel.Children.Add(_guiDesignerRecipeTypeText);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Input item / count", 10));
        var input = new Grid();
        input.ColumnDefinitions.Add(new ColumnDefinition());
        input.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        input.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        _guiDesignerRecipeInputItemBox = MachineBuilderTextBox();
        _guiDesignerRecipeInputItemBox.Height = 30;
        _guiDesignerRecipeInputItemBox.Text = "minecraft:iron_ingot";
        input.Children.Add(_guiDesignerRecipeInputItemBox);
        _guiDesignerRecipeInputCountBox = MachineBuilderTextBox();
        _guiDesignerRecipeInputCountBox.Height = 30;
        _guiDesignerRecipeInputCountBox.Text = "1";
        Grid.SetColumn(_guiDesignerRecipeInputCountBox, 2);
        input.Children.Add(_guiDesignerRecipeInputCountBox);
        panel.Children.Add(input);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Output item / count", 8));
        var output = new Grid();
        output.ColumnDefinitions.Add(new ColumnDefinition());
        output.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        output.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        _guiDesignerRecipeOutputItemBox = MachineBuilderTextBox();
        _guiDesignerRecipeOutputItemBox.Height = 30;
        _guiDesignerRecipeOutputItemBox.Text = "minecraft:gold_ingot";
        output.Children.Add(_guiDesignerRecipeOutputItemBox);
        _guiDesignerRecipeOutputCountBox = MachineBuilderTextBox();
        _guiDesignerRecipeOutputCountBox.Height = 30;
        _guiDesignerRecipeOutputCountBox.Text = "1";
        Grid.SetColumn(_guiDesignerRecipeOutputCountBox, 2);
        output.Children.Add(_guiDesignerRecipeOutputCountBox);
        panel.Children.Add(output);

        panel.Children.Add(MachineBuilderVisualInspectorLabel("Duration ticks / FE per tick", 8));
        var process = new Grid();
        process.ColumnDefinitions.Add(new ColumnDefinition());
        process.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        process.ColumnDefinitions.Add(new ColumnDefinition());
        _guiDesignerRecipeDurationBox = MachineBuilderTextBox();
        _guiDesignerRecipeDurationBox.Height = 30;
        _guiDesignerRecipeDurationBox.Text = "100";
        process.Children.Add(_guiDesignerRecipeDurationBox);
        _guiDesignerRecipeEnergyBox = MachineBuilderTextBox();
        _guiDesignerRecipeEnergyBox.Height = 30;
        _guiDesignerRecipeEnergyBox.Text = "0";
        Grid.SetColumn(_guiDesignerRecipeEnergyBox, 2);
        process.Children.Add(_guiDesignerRecipeEnergyBox);
        panel.Children.Add(process);

        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        var apply = CreateStudioChromeButton("ADD / APPLY", double.NaN, true);
        apply.Height = 33;
        apply.Click += (_, _) => CommitGuiDesignerRecipe();
        actions.Children.Add(apply);
        var remove = CreateStudioChromeButton("REMOVE", double.NaN, false);
        remove.Height = 33;
        remove.Click += (_, _) => RemoveSelectedGuiDesignerRecipe();
        Grid.SetColumn(remove, 2);
        actions.Children.Add(remove);
        panel.Children.Add(actions);

        card.Child = panel;
        return card;
    }

    private void RefreshMachineBuilderGuiDesigner()
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        RefreshGuiDesignerRecipes();
        RefreshGuiDesignerRecipeProcessChoices();
        RefreshGuiDesignerRecipeType();

        if (_guiDesignerSummary is not null)
        {
            _guiDesignerSummary.Text =
                $"Shared GUI bindings: {_machineBuilderDocument.Gui.Bindings.Count}\n" +
                $"Recipes: {_machineBuilderDocument.Recipes.Count}\n" +
                $"Screen: {(_machineBuilderDocument.Gui.ScreenId.Length == 0 ? "not assigned yet" : _machineBuilderDocument.Gui.ScreenId)}";
        }
    }

    private void RefreshGuiDesignerRecipeProcessChoices()
    {
        if (_guiDesignerRecipeProcessCombo is null || _machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
            return;
        var choices = _machineBuilderDocument.Behavior.Nodes
            .Where(node => IsGuiDesignerProcessNode(node.LibraryItemId))
            .Select(node =>
            {
                var item = _machineBuilderLibraryLoad.Catalog.Find(node.LibraryItemId, node.LibraryItemVersion);
                return new GuiDesignerProcessNode(node.Id, $"{item?.DisplayName ?? node.LibraryItemId} · {node.Id}", node.LibraryItemId);
            })
            .ToList();
        var selectedId = (_guiDesignerRecipeProcessCombo.SelectedItem as GuiDesignerProcessNode)?.NodeId;
        _guiDesignerRecipeProcessCombo.ItemsSource = choices;
        _guiDesignerRecipeProcessCombo.SelectedItem = choices.FirstOrDefault(choice => choice.NodeId.Equals(selectedId, StringComparison.OrdinalIgnoreCase))
                                                      ?? choices.FirstOrDefault();
    }

    private static bool IsGuiDesignerProcessNode(string libraryItemId)
        => libraryItemId.Contains("processor", StringComparison.OrdinalIgnoreCase) ||
           libraryItemId.Contains("generator-core", StringComparison.OrdinalIgnoreCase) ||
           libraryItemId.Contains("recipe", StringComparison.OrdinalIgnoreCase);

    private void RefreshGuiDesignerRecipeType()
    {
        if (_guiDesignerRecipeTypeText is null)
            return;
        var process = _guiDesignerRecipeProcessCombo?.SelectedItem as GuiDesignerProcessNode;
        _guiDesignerRecipeTypeText.Text = InferGuiDesignerRecipeType(process?.LibraryItemId);
        if (_guiDesignerRecipeEnergyBox is not null && process is not null &&
            !process.LibraryItemId.Contains("powered", StringComparison.OrdinalIgnoreCase) &&
            !process.LibraryItemId.Contains("generator", StringComparison.OrdinalIgnoreCase))
        {
            _guiDesignerRecipeEnergyBox.Text = "0";
        }
    }

    private static string InferGuiDesignerRecipeType(string? libraryItemId)
    {
        if (libraryItemId?.Contains("fluid", StringComparison.OrdinalIgnoreCase) == true)
            return "forever_skyblock:fluid_processing";
        if (libraryItemId?.Contains("generator", StringComparison.OrdinalIgnoreCase) == true)
            return "forever_skyblock:generator";
        if (libraryItemId?.Contains("powered", StringComparison.OrdinalIgnoreCase) == true)
            return "forever_skyblock:powered_processing";
        return "forever_skyblock:machine_processing";
    }

    private void RefreshGuiDesignerRecipes()
    {
        if (_guiDesignerRecipeList is null || _machineBuilderDocument is null)
            return;
        var selectedId = (_guiDesignerRecipeList.SelectedItem as GuiDesignerRecipeItem)?.Recipe.Id;
        var items = _machineBuilderDocument.Recipes.Select(recipe => new GuiDesignerRecipeItem { Recipe = recipe }).ToList();
        _guiDesignerRecipeList.ItemsSource = items;
        _guiDesignerRecipeList.SelectedItem = items.FirstOrDefault(item => item.Recipe.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
        if (_guiDesignerSummary is not null)
            _guiDesignerSummary.Text = $"Shared GUI bindings: {_machineBuilderDocument.Gui.Bindings.Count}\nRecipes: {items.Count}";
    }

    private void LoadSelectedGuiDesignerRecipe()
    {
        var recipe = (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe;
        if (recipe is null)
            return;
        if (_guiDesignerRecipeIdBox is not null) _guiDesignerRecipeIdBox.Text = recipe.Id;
        if (_guiDesignerRecipeTypeText is not null) _guiDesignerRecipeTypeText.Text = recipe.RecipeTypeId;

        var processNodeId = ReadGuiDesignerString(recipe.Properties, "processorNodeId", string.Empty);
        if (_guiDesignerRecipeProcessCombo is not null)
            _guiDesignerRecipeProcessCombo.SelectedItem = _guiDesignerRecipeProcessCombo.Items
                .OfType<GuiDesignerProcessNode>()
                .FirstOrDefault(item => item.NodeId.Equals(processNodeId, StringComparison.OrdinalIgnoreCase));

        if (recipe.Properties.TryGetValue("input", out var input) && input.ValueKind == JsonValueKind.Object)
        {
            if (_guiDesignerRecipeInputItemBox is not null && input.TryGetProperty("item", out var item))
                _guiDesignerRecipeInputItemBox.Text = item.GetString() ?? string.Empty;
            if (_guiDesignerRecipeInputCountBox is not null && input.TryGetProperty("count", out var count) && count.TryGetInt32(out var amount))
                _guiDesignerRecipeInputCountBox.Text = amount.ToString(CultureInfo.InvariantCulture);
        }
        if (recipe.Properties.TryGetValue("output", out var output) && output.ValueKind == JsonValueKind.Object)
        {
            if (_guiDesignerRecipeOutputItemBox is not null && output.TryGetProperty("item", out var item))
                _guiDesignerRecipeOutputItemBox.Text = item.GetString() ?? string.Empty;
            if (_guiDesignerRecipeOutputCountBox is not null && output.TryGetProperty("count", out var count) && count.TryGetInt32(out var amount))
                _guiDesignerRecipeOutputCountBox.Text = amount.ToString(CultureInfo.InvariantCulture);
        }
        if (_guiDesignerRecipeDurationBox is not null && recipe.Properties.TryGetValue("processingTimeTicks", out var duration) && duration.TryGetInt32(out var ticks))
            _guiDesignerRecipeDurationBox.Text = ticks.ToString(CultureInfo.InvariantCulture);
        if (_guiDesignerRecipeEnergyBox is not null && recipe.Properties.TryGetValue("energy", out var energy) && energy.ValueKind == JsonValueKind.Object &&
            energy.TryGetProperty("fePerTick", out var fe) && fe.TryGetInt32(out var fePerTick))
            _guiDesignerRecipeEnergyBox.Text = fePerTick.ToString(CultureInfo.InvariantCulture);

        ApplySelectedRecipeToGuiPreview(recipe);
    }

    private void CommitGuiDesignerRecipe()
    {
        if (_machineBuilderDocument is null)
            return;
        var id = SanitizeMachineBuilderGraphId(_guiDesignerRecipeIdBox?.Text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(id))
        {
            SetGuiDesignerStatus("Recipe ID is required.", "#E18B8B");
            return;
        }
        var inputItem = (_guiDesignerRecipeInputItemBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        var outputItem = (_guiDesignerRecipeOutputItemBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsGuiDesignerResourceLocation(inputItem) || !IsGuiDesignerResourceLocation(outputItem))
        {
            SetGuiDesignerStatus("Input/output items must be resource locations such as minecraft:iron_ingot.", "#E18B8B");
            return;
        }
        if (!TryGuiDesignerInt(_guiDesignerRecipeInputCountBox?.Text, 1, 64, out var inputCount) ||
            !TryGuiDesignerInt(_guiDesignerRecipeOutputCountBox?.Text, 1, 64, out var outputCount) ||
            !TryGuiDesignerInt(_guiDesignerRecipeDurationBox?.Text, 1, 1_000_000, out var duration) ||
            !TryGuiDesignerInt(_guiDesignerRecipeEnergyBox?.Text, 0, 1_000_000, out var fePerTick))
        {
            SetGuiDesignerStatus("Counts, duration or FE/t contain unsupported values.", "#E18B8B");
            return;
        }
        if ((long)duration * fePerTick > int.MaxValue)
        {
            SetGuiDesignerStatus("Total recipe energy exceeds the supported range.", "#E18B8B");
            return;
        }

        var process = _guiDesignerRecipeProcessCombo?.SelectedItem as GuiDesignerProcessNode;
        var type = InferGuiDesignerRecipeType(process?.LibraryItemId);
        var selected = (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe;
        var recipe = selected;
        if (recipe is null || (!recipe.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                               _machineBuilderDocument.Recipes.Any(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase))))
        {
            if (_machineBuilderDocument.Recipes.Any(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                SetGuiDesignerStatus($"Recipe ID '{id}' already exists.", "#E18B8B");
                return;
            }
            recipe = new ForeverStudioMachineRecipeBinding();
            _machineBuilderDocument.Recipes.Add(recipe);
        }

        recipe.Id = id;
        recipe.RecipeTypeId = type;
        recipe.Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["authoring"] = JsonSerializer.SerializeToElement("gui-designer"),
            ["processorNodeId"] = JsonSerializer.SerializeToElement(process?.NodeId ?? string.Empty),
            ["input"] = JsonSerializer.SerializeToElement(new { item = inputItem, count = inputCount }),
            ["output"] = JsonSerializer.SerializeToElement(new { item = outputItem, count = outputCount }),
            ["processingTimeTicks"] = JsonSerializer.SerializeToElement(duration),
            ["energy"] = JsonSerializer.SerializeToElement(new
            {
                fePerTick,
                totalFe = duration * fePerTick
            })
        };

        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerRecipes();
        ApplySelectedRecipeToGuiPreview(recipe);
        SetGuiDesignerStatus($"Recipe '{id}' saved: {inputCount}× {inputItem} → {outputCount}× {outputItem}.", "#74C99A");
    }

    private void RemoveSelectedGuiDesignerRecipe()
    {
        if (_machineBuilderDocument is null ||
            (_guiDesignerRecipeList?.SelectedItem as GuiDesignerRecipeItem)?.Recipe is not { } recipe)
            return;
        _machineBuilderDocument.Recipes.Remove(recipe);
        MarkMachineBuilderPhase5Dirty();
        RefreshGuiDesignerRecipes();
        SetGuiDesignerStatus($"Removed recipe '{recipe.Id}'.", "#D8B26A");
    }

    private void ApplySelectedRecipeToGuiPreview(ForeverStudioMachineRecipeBinding recipe)
    {
        if (_machineBuilderDocument is null)
            return;
        var inputItem = "item";
        var inputCount = 1;
        var outputItem = "item";
        var outputCount = 1;
        if (recipe.Properties.TryGetValue("input", out var input) && input.ValueKind == JsonValueKind.Object)
        {
            if (input.TryGetProperty("item", out var item)) inputItem = item.GetString() ?? inputItem;
            if (input.TryGetProperty("count", out var count) && count.TryGetInt32(out var value)) inputCount = value;
        }
        if (recipe.Properties.TryGetValue("output", out var output) && output.ValueKind == JsonValueKind.Object)
        {
            if (output.TryGetProperty("item", out var item)) outputItem = item.GetString() ?? outputItem;
            if (output.TryGetProperty("count", out var count) && count.TryGetInt32(out var value)) outputCount = value;
        }

        foreach (var binding in _machineBuilderDocument.Gui.Bindings)
        {
            var widget = ReadGuiDesignerString(binding.Properties, "widget", string.Empty);
            if (widget is not ("item-slot" or "output-slot"))
                continue;
            var node = _machineBuilderDocument.Behavior.Nodes.FirstOrDefault(candidate =>
                candidate.Id.Equals(binding.SourceId, StringComparison.OrdinalIgnoreCase));
            if (node?.LibraryItemId.Contains("output", StringComparison.OrdinalIgnoreCase) == true || widget == "output-slot")
                binding.Properties["previewValue"] = JsonSerializer.SerializeToElement($"{outputItem}|{outputCount}");
            else
                binding.Properties["previewValue"] = JsonSerializer.SerializeToElement($"{inputItem}|{inputCount}");
        }
        QueueUnifiedMachinePreviewRefresh();
    }

    // Compatibility hook for the multi-resource recipe authoring partial. The retired compact
    // canvas no longer renders anything; shared-editor preview decoration is refreshed instead.
    private void RefreshGuiDesignerCanvas() => QueueUnifiedMachinePreviewRefresh();

    private static bool TryGuiDesignerInt(string? value, int min, int max, out int result)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= min && result <= max;

    private static string ReadGuiDesignerString(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static bool IsGuiDesignerResourceLocation(string value)
    {
        var split = value.Split(':', 2);
        if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
            return false;
        return split[0].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.') &&
               split[1].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.' or '/');
    }

    private static TextBlock GuiDesignerTitle(string title, string subtitle)
        => new()
        {
            Text = $"{title}\n{subtitle}",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 0, 0, 8)
        };

    private void SetGuiDesignerStatus(string text, string color)
    {
        if (_guiDesignerStatus is not null)
        {
            _guiDesignerStatus.Text = text;
            _guiDesignerStatus.Foreground = Brush(color);
        }
        SetMachineBuilderPhase5Status(text, color);
    }
}

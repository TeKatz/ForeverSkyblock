using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 15 Blockstate Editor. Blockstate edits are intentionally kept in memory;
/// Phase 16 owns validation/review/apply and all persistent writes.
/// </summary>
public partial class MainWindow
{
    private enum StudioBlockstateViewMode
    {
        Idle,
        Active,
        Compare
    }

    private sealed class StudioBlockstateDocument
    {
        public required string RepoKey { get; init; }
        public required string AssetId { get; init; }
        public required List<MinecraftBlockVariantDescriptor> SourceVariants { get; init; }
        public required List<MinecraftBlockVariantDescriptor> EditableVariants { get; set; }
        public bool Modified { get; set; }
    }

    private bool _studioBlockstateEditorInitialized;
    private readonly Dictionary<string, StudioBlockstateDocument> _studioBlockstateDocuments = new(StringComparer.OrdinalIgnoreCase);
    private StudioBlockstateDocument? _studioBlockstateDocument;
    private StudioBlockstateViewMode _studioBlockstateViewMode = StudioBlockstateViewMode.Idle;
    private MinecraftBlockVariantDescriptor? _studioBlockstateSelectedVariant;
    private bool _studioBlockstateUiUpdating;

    private StackPanel? _studioBlockstateRoot;
    private TextBlock? _studioBlockstateStateText;
    private TextBlock? _studioBlockstatePropertySummary;
    private Grid? _studioBlockstateMatrix;
    private ToggleButton? _studioBlockstateIdleButton;
    private ToggleButton? _studioBlockstateActiveButton;
    private ToggleButton? _studioBlockstateCompareButton;
    private TextBox? _studioBlockstateKeyBox;
    private ComboBox? _studioBlockstateModelCombo;
    private ComboBox? _studioBlockstateXRotationCombo;
    private ComboBox? _studioBlockstateYRotationCombo;
    private CheckBox? _studioBlockstateUvLockCheck;
    private TextBlock? _studioBlockstateSelectionStatus;
    private Button? _studioBlockstateApplyButton;
    private Button? _studioBlockstateRemoveButton;

    public void EnableForeverBlockstateEditor()
    {
        if (_studioBlockstateEditorInitialized || _studioBlockViewerStatusText?.Parent is not Panel inspector)
            return;
        _studioBlockstateEditorInitialized = true;

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("BLOCKSTATE EDITOR · PHASE 15"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Visual variant matrix. Changes update the block preview immediately but stay in memory until Phase 16 Review / Validation / Apply.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 14)
        });

        _studioBlockstateRoot = new StackPanel();
        inspector.Children.Add(_studioBlockstateRoot);
        BuildStudioBlockstateControls(_studioBlockstateRoot);

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => LoadStudioBlockstateDocument();
        if (_studioBlockWorkspace is FrameworkElement workspace)
            workspace.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    LoadStudioBlockstateDocument();
            };

        LoadStudioBlockstateDocument();
    }

    private void BuildStudioBlockstateControls(Panel root)
    {
        var modeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        _studioBlockstateIdleButton = StudioBlockstateModeButton("Idle");
        _studioBlockstateActiveButton = StudioBlockstateModeButton("Active");
        _studioBlockstateCompareButton = StudioBlockstateModeButton("Compare", 100);
        _studioBlockstateIdleButton.Click += (_, _) => SetStudioBlockstateViewMode(StudioBlockstateViewMode.Idle);
        _studioBlockstateActiveButton.Click += (_, _) => SetStudioBlockstateViewMode(StudioBlockstateViewMode.Active);
        _studioBlockstateCompareButton.Click += (_, _) => SetStudioBlockstateViewMode(StudioBlockstateViewMode.Compare);
        modeRow.Children.Add(_studioBlockstateIdleButton);
        modeRow.Children.Add(_studioBlockstateActiveButton);
        modeRow.Children.Add(_studioBlockstateCompareButton);
        root.Children.Add(modeRow);

        _studioBlockstateStateText = new TextBlock
        {
            Text = "SOURCE STATE",
            Foreground = Brush("#74C99A"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 7)
        };
        root.Children.Add(_studioBlockstateStateText);

        _studioBlockstatePropertySummary = new TextBlock
        {
            Foreground = Brush("#AFC0C7"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        root.Children.Add(_studioBlockstatePropertySummary);

        var matrixScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 330,
            Background = Brush("#0B1115")
        };
        _studioBlockstateMatrix = new Grid { Margin = new Thickness(8) };
        matrixScroll.Content = _studioBlockstateMatrix;
        root.Children.Add(matrixScroll);

        var legend = new WrapPanel { Margin = new Thickness(0, 8, 0, 14) };
        legend.Children.Add(StudioBlockstateLegend("IDENTICAL", "#244A3B"));
        legend.Children.Add(StudioBlockstateLegend("CHANGED", "#685624"));
        legend.Children.Add(StudioBlockstateLegend("ACTIVE ONLY", "#4C365E"));
        legend.Children.Add(StudioBlockstateLegend("IDLE ONLY", "#304C61"));
        legend.Children.Add(StudioBlockstateLegend("MISSING / INVALID", "#6A3035"));
        root.Children.Add(legend);

        root.Children.Add(BlockViewerLabel("Variant key / properties"));
        _studioBlockstateKeyBox = StudioBlockstateTextBox();
        _studioBlockstateKeyBox.TextChanged += (_, _) => UpdateStudioBlockstateActionButtons();
        root.Children.Add(_studioBlockstateKeyBox);

        root.Children.Add(BlockViewerLabel("Model", 12));
        _studioBlockstateModelCombo = BlockViewerCombo();
        _studioBlockstateModelCombo.IsEditable = true;
        root.Children.Add(_studioBlockstateModelCombo);

        var rotations = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        rotations.ColumnDefinitions.Add(new ColumnDefinition());
        rotations.ColumnDefinitions.Add(new ColumnDefinition());
        var xPanel = new StackPanel { Margin = new Thickness(0, 0, 5, 0) };
        xPanel.Children.Add(BlockViewerLabel("X rotation"));
        _studioBlockstateXRotationCombo = StudioBlockstateRotationCombo();
        xPanel.Children.Add(_studioBlockstateXRotationCombo);
        Grid.SetColumn(xPanel, 0);
        rotations.Children.Add(xPanel);
        var yPanel = new StackPanel { Margin = new Thickness(5, 0, 0, 0) };
        yPanel.Children.Add(BlockViewerLabel("Y rotation"));
        _studioBlockstateYRotationCombo = StudioBlockstateRotationCombo();
        yPanel.Children.Add(_studioBlockstateYRotationCombo);
        Grid.SetColumn(yPanel, 1);
        rotations.Children.Add(yPanel);
        root.Children.Add(rotations);

        _studioBlockstateUvLockCheck = new CheckBox
        {
            Content = "UV lock",
            Foreground = Brush("#C7D4D9"),
            Margin = new Thickness(0, 10, 0, 0)
        };
        root.Children.Add(_studioBlockstateUvLockCheck);

        _studioBlockstateSelectionStatus = new TextBlock
        {
            Foreground = Brush("#8FA1AA"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 10)
        };
        root.Children.Add(_studioBlockstateSelectionStatus);

        var actionRow = new WrapPanel();
        _studioBlockstateApplyButton = StudioButton("Apply variant", 120);
        _studioBlockstateApplyButton.Margin = new Thickness(0, 0, 7, 7);
        _studioBlockstateApplyButton.Click += (_, _) => ApplyStudioBlockstateVariant(addAsNew: false);
        actionRow.Children.Add(_studioBlockstateApplyButton);

        var add = StudioButton("Add variant", 110);
        add.Margin = new Thickness(0, 0, 7, 7);
        add.Click += (_, _) => ApplyStudioBlockstateVariant(addAsNew: true);
        actionRow.Children.Add(add);

        _studioBlockstateRemoveButton = StudioButton("Remove", 90);
        _studioBlockstateRemoveButton.Margin = new Thickness(0, 0, 7, 7);
        _studioBlockstateRemoveButton.Click += (_, _) => RemoveStudioBlockstateVariant();
        actionRow.Children.Add(_studioBlockstateRemoveButton);

        var reset = StudioButton("Reset blockstate", 135);
        reset.Margin = new Thickness(0, 0, 0, 7);
        reset.Click += (_, _) => ResetStudioBlockstateDocument();
        actionRow.Children.Add(reset);
        root.Children.Add(actionRow);

        SetStudioBlockstateViewMode(StudioBlockstateViewMode.Idle, refresh: false);
    }

    private static ToggleButton StudioBlockstateModeButton(string text, double width = 82)
        => new()
        {
            Content = text,
            Width = width,
            Height = 30,
            Margin = new Thickness(0, 0, 6, 0),
            Background = Brush("#172228"),
            Foreground = Brush("#D7E2E7"),
            BorderBrush = Brush("#40515A"),
            BorderThickness = new Thickness(1)
        };

    private static TextBox StudioBlockstateTextBox()
        => new()
        {
            Height = 32,
            Padding = new Thickness(7, 5, 7, 5),
            Background = Brush("#0D1418"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };

    private static ComboBox StudioBlockstateRotationCombo()
    {
        var combo = BlockViewerCombo();
        combo.ItemsSource = new[] { "0", "90", "180", "270" };
        combo.SelectedItem = "0";
        return combo;
    }

    private static Border StudioBlockstateLegend(string text, string background)
        => new()
        {
            Background = Brush(background),
            CornerRadius = new CornerRadius(3),
            Margin = new Thickness(0, 0, 5, 5),
            Padding = new Thickness(6, 3, 6, 3),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush("#DDE6EA"),
                FontSize = 9,
                FontWeight = FontWeights.SemiBold
            }
        };

    private void LoadStudioBlockstateDocument()
    {
        if (!_studioBlockstateEditorInitialized || string.IsNullOrWhiteSpace(_repoRoot) ||
            _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset)
            return;

        var repoKey = $"{Path.GetFullPath(_repoRoot)}|{asset.Id}";
        if (!_studioBlockstateDocuments.TryGetValue(repoKey, out var document))
        {
            document = new StudioBlockstateDocument
            {
                RepoKey = repoKey,
                AssetId = asset.Id,
                SourceVariants = asset.Variants.Select(CloneStudioBlockstateVariant).ToList(),
                EditableVariants = asset.Variants.Select(CloneStudioBlockstateVariant).ToList()
            };
            _studioBlockstateDocuments[repoKey] = document;
        }

        _studioBlockstateDocument = document;
        _studioBlockstateSelectedVariant = null;
        PopulateStudioBlockstateModelChoices();
        RefreshStudioBlockstateEditor();
    }

    private void PopulateStudioBlockstateModelChoices()
    {
        if (_studioBlockstateModelCombo is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        var modelRoot = Path.Combine(_repoRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock", "models", "block");
        var models = Directory.Exists(modelRoot)
            ? Directory.EnumerateFiles(modelRoot, "*.json", SearchOption.AllDirectories)
                .Select(path => "forever_skyblock:block/" + Path.GetRelativePath(modelRoot, path)
                    .Replace('\\', '/')
                    .Replace(".json", string.Empty, StringComparison.OrdinalIgnoreCase))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : [];
        _studioBlockstateModelCombo.ItemsSource = models;
    }

    private void SetStudioBlockstateViewMode(StudioBlockstateViewMode mode, bool refresh = true)
    {
        _studioBlockstateViewMode = mode;
        if (_studioBlockstateIdleButton is not null)
            _studioBlockstateIdleButton.IsChecked = mode == StudioBlockstateViewMode.Idle;
        if (_studioBlockstateActiveButton is not null)
            _studioBlockstateActiveButton.IsChecked = mode == StudioBlockstateViewMode.Active;
        if (_studioBlockstateCompareButton is not null)
            _studioBlockstateCompareButton.IsChecked = mode == StudioBlockstateViewMode.Compare;
        if (refresh)
            RefreshStudioBlockstateEditor();
    }

    private void RefreshStudioBlockstateEditor()
    {
        if (_studioBlockstateDocument is null)
            return;
        UpdateStudioBlockstateModifiedState();
        UpdateStudioBlockstatePropertySummary();
        RebuildStudioBlockstateMatrix();
        PopulateStudioBlockstateVariantFields();
    }

    private void UpdateStudioBlockstateModifiedState()
    {
        if (_studioBlockstateStateText is null || _studioBlockstateDocument is null)
            return;
        _studioBlockstateStateText.Text = _studioBlockstateDocument.Modified
            ? "PREVIEW MODIFIED · NOT SAVED"
            : "SOURCE STATE";
        _studioBlockstateStateText.Foreground = Brush(_studioBlockstateDocument.Modified ? "#D8B26A" : "#74C99A");
    }

    private void UpdateStudioBlockstatePropertySummary()
    {
        if (_studioBlockstatePropertySummary is null || _studioBlockstateDocument is null)
            return;

        var groups = _studioBlockstateDocument.EditableVariants
            .SelectMany(variant => variant.Properties)
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => $"{group.Key}: {string.Join(" / ", group.Select(pair => pair.Value).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}")
            .ToList();
        _studioBlockstatePropertySummary.Text = groups.Count == 0
            ? "Properties: default variant only"
            : "Properties · " + string.Join("   ·   ", groups);
    }

    private void RebuildStudioBlockstateMatrix()
    {
        if (_studioBlockstateMatrix is null || _studioBlockstateDocument is null)
            return;

        var matrix = _studioBlockstateMatrix;
        matrix.Children.Clear();
        matrix.RowDefinitions.Clear();
        matrix.ColumnDefinitions.Clear();

        var variants = _studioBlockstateDocument.EditableVariants;
        var facings = variants.Select(GetStudioVariantFacing)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(FacingSortKey)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (facings.Count == 0)
            facings.Add("default");

        var rowKeys = GetStudioBlockstateMatrixRows(variants);
        if (rowKeys.Count == 0)
            rowKeys.Add("default");

        matrix.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        foreach (var _ in facings)
            matrix.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(128) });
        matrix.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        foreach (var _ in rowKeys)
            matrix.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddStudioBlockstateMatrixLabel(matrix, _studioBlockstateViewMode == StudioBlockstateViewMode.Compare ? "GROUP" : "STATE", 0, 0, true);
        for (var column = 0; column < facings.Count; column++)
            AddStudioBlockstateMatrixLabel(matrix, facings[column].ToUpperInvariant(), 0, column + 1, true);

        for (var row = 0; row < rowKeys.Count; row++)
        {
            var rowKey = rowKeys[row];
            AddStudioBlockstateMatrixLabel(matrix, HumanizeStudioBlockstateRow(rowKey), row + 1, 0, false);
            for (var column = 0; column < facings.Count; column++)
            {
                var facing = facings[column];
                var cell = _studioBlockstateViewMode == StudioBlockstateViewMode.Compare
                    ? BuildStudioBlockstateCompareCell(rowKey, facing)
                    : BuildStudioBlockstateVariantCell(rowKey, facing);
                Grid.SetRow(cell, row + 1);
                Grid.SetColumn(cell, column + 1);
                matrix.Children.Add(cell);
            }
        }
    }

    private List<string> GetStudioBlockstateMatrixRows(IReadOnlyList<MinecraftBlockVariantDescriptor> variants)
    {
        IEnumerable<MinecraftBlockVariantDescriptor> filtered = variants;
        if (_studioBlockstateViewMode == StudioBlockstateViewMode.Idle && variants.Any(HasStudioActiveProperty))
            filtered = variants.Where(variant => IsStudioActiveValue(variant, false));
        else if (_studioBlockstateViewMode == StudioBlockstateViewMode.Active && variants.Any(HasStudioActiveProperty))
            filtered = variants.Where(variant => IsStudioActiveValue(variant, true));

        return filtered.Select(variant => _studioBlockstateViewMode == StudioBlockstateViewMode.Compare
                ? BuildStudioPropertyKey(variant, excludeActive: true, excludeFacing: true)
                : BuildStudioPropertyKey(variant, excludeActive: false, excludeFacing: true))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private FrameworkElement BuildStudioBlockstateVariantCell(string rowKey, string facing)
    {
        if (_studioBlockstateDocument is null)
            return new Border();

        var variant = _studioBlockstateDocument.EditableVariants.FirstOrDefault(candidate =>
            BuildStudioPropertyKey(candidate, excludeActive: false, excludeFacing: true).Equals(rowKey, StringComparison.OrdinalIgnoreCase) &&
            GetStudioVariantFacing(candidate).Equals(facing, StringComparison.OrdinalIgnoreCase) &&
            StudioVariantMatchesViewMode(candidate));
        if (variant is null)
            return StudioBlockstateMissingCell(rowKey, facing);

        var valid = IsStudioBlockstateVariantValid(variant, out var reason);
        var button = StudioBlockstateCellButton(
            valid ? StudioBlockstateCellText(variant) : $"INVALID\n{ShortStudioModelName(variant.ModelReference)}",
            valid ? "#173039" : "#6A3035");
        button.ToolTip = valid ? DescribeStudioBlockstateVariant(variant) : reason;
        button.Click += (_, _) => SelectStudioBlockstateVariant(variant);
        return button;
    }

    private FrameworkElement BuildStudioBlockstateCompareCell(string rowKey, string facing)
    {
        if (_studioBlockstateDocument is null)
            return new Border();

        var variants = _studioBlockstateDocument.EditableVariants;
        var idle = variants.FirstOrDefault(candidate =>
            IsStudioActiveValue(candidate, false) &&
            BuildStudioPropertyKey(candidate, excludeActive: true, excludeFacing: true).Equals(rowKey, StringComparison.OrdinalIgnoreCase) &&
            GetStudioVariantFacing(candidate).Equals(facing, StringComparison.OrdinalIgnoreCase));
        var active = variants.FirstOrDefault(candidate =>
            IsStudioActiveValue(candidate, true) &&
            BuildStudioPropertyKey(candidate, excludeActive: true, excludeFacing: true).Equals(rowKey, StringComparison.OrdinalIgnoreCase) &&
            GetStudioVariantFacing(candidate).Equals(facing, StringComparison.OrdinalIgnoreCase));

        string label;
        string background;
        if (idle is null && active is null)
        {
            label = "MISSING";
            background = "#6A3035";
        }
        else if (idle is null)
        {
            label = "ACTIVE ONLY";
            background = "#4C365E";
        }
        else if (active is null)
        {
            label = "IDLE ONLY";
            background = "#304C61";
        }
        else if (StudioBlockstateVariantsEquivalent(idle, active))
        {
            label = "IDENTICAL";
            background = "#244A3B";
        }
        else
        {
            label = "CHANGED";
            background = "#685624";
        }

        var button = StudioBlockstateCellButton(label, background);
        button.ToolTip = BuildStudioBlockstateCompareDescription(idle, active);
        button.Click += (_, _) =>
        {
            var target = active ?? idle;
            if (target is not null)
                SelectStudioBlockstateVariant(target);
            else
                PrimeStudioBlockstateNewVariant(rowKey, facing, preferActive: true);
        };
        return button;
    }

    private FrameworkElement StudioBlockstateMissingCell(string rowKey, string facing)
    {
        var button = StudioBlockstateCellButton("MISSING", "#6A3035");
        button.ToolTip = "No variant exists for this state/facing combination. Click to prepare a new variant.";
        button.Click += (_, _) => PrimeStudioBlockstateNewVariant(rowKey, facing, _studioBlockstateViewMode == StudioBlockstateViewMode.Active);
        return button;
    }

    private static Button StudioBlockstateCellButton(string text, string background)
        => new()
        {
            Content = text,
            MinWidth = 118,
            MinHeight = 52,
            Margin = new Thickness(3),
            Padding = new Thickness(6),
            Background = Brush(background),
            Foreground = Brushes.White,
            BorderBrush = Brush("#41515A"),
            BorderThickness = new Thickness(1),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold
        };

    private static void AddStudioBlockstateMatrixLabel(Grid grid, string text, int row, int column, bool header)
    {
        var label = new TextBlock
        {
            Text = text,
            Foreground = Brush(header ? "#8FA1AA" : "#C7D4D9"),
            FontSize = header ? 9 : 10,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 6, 6, 6)
        };
        Grid.SetRow(label, row);
        Grid.SetColumn(label, column);
        grid.Children.Add(label);
    }

    private void SelectStudioBlockstateVariant(MinecraftBlockVariantDescriptor variant)
    {
        _studioBlockstateSelectedVariant = variant;
        PopulateStudioBlockstateVariantFields();
        SelectStudioVariantInMainViewer(variant);
    }

    private void SelectStudioVariantInMainViewer(MinecraftBlockVariantDescriptor variant)
    {
        if (_studioBlockStateCombo is null || _studioBlockFacingCombo is null)
            return;
        _studioBlockSelectorUpdate = true;
        try
        {
            _studioBlockStateCombo.SelectedItem = variant.StateLabel;
            if (_studioBlockStateCombo.SelectedItem is null && _studioBlockStateCombo.Items.Count > 0)
                _studioBlockStateCombo.SelectedIndex = 0;
            if (_studioBlockMachineCombo?.SelectedItem is MinecraftBlockAssetDescriptor asset)
                PopulateStudioBlockFacings(asset);
            _studioBlockFacingCombo.SelectedItem = variant.Facing;
        }
        finally
        {
            _studioBlockSelectorUpdate = false;
        }
        LoadSelectedStudioBlockVariant();
    }

    private void PopulateStudioBlockstateVariantFields()
    {
        _studioBlockstateUiUpdating = true;
        try
        {
            var variant = _studioBlockstateSelectedVariant;
            if (_studioBlockstateKeyBox is not null)
                _studioBlockstateKeyBox.Text = variant?.Key ?? string.Empty;
            if (_studioBlockstateModelCombo is not null)
                _studioBlockstateModelCombo.Text = variant?.ModelReference ?? SuggestedStudioBlockstateModelReference();
            if (_studioBlockstateXRotationCombo is not null)
                _studioBlockstateXRotationCombo.SelectedItem = NormalizeStudioRotation(variant?.XRotation ?? 0).ToString();
            if (_studioBlockstateYRotationCombo is not null)
                _studioBlockstateYRotationCombo.SelectedItem = NormalizeStudioRotation(variant?.YRotation ?? 0).ToString();
            if (_studioBlockstateUvLockCheck is not null)
                _studioBlockstateUvLockCheck.IsChecked = variant?.UvLock ?? false;
            if (_studioBlockstateSelectionStatus is not null)
            {
                _studioBlockstateSelectionStatus.Text = variant is null
                    ? "Select a matrix cell, or enter a new property key and add a variant."
                    : DescribeStudioBlockstateVariant(variant);
                _studioBlockstateSelectionStatus.Foreground = Brush("#8FA1AA");
            }
        }
        finally
        {
            _studioBlockstateUiUpdating = false;
        }
        UpdateStudioBlockstateActionButtons();
    }

    private void PrimeStudioBlockstateNewVariant(string rowKey, string facing, bool preferActive)
    {
        _studioBlockstateSelectedVariant = null;
        var properties = ParseStudioVariantKey(rowKey);
        if (_studioBlockstateDocument?.EditableVariants.Any(HasStudioActiveProperty) == true && !properties.ContainsKey("active"))
            properties["active"] = preferActive ? "true" : "false";
        if (!facing.Equals("default", StringComparison.OrdinalIgnoreCase))
            properties["facing"] = facing;

        _studioBlockstateUiUpdating = true;
        try
        {
            if (_studioBlockstateKeyBox is not null)
                _studioBlockstateKeyBox.Text = FormatStudioVariantKey(properties);
            if (_studioBlockstateModelCombo is not null)
                _studioBlockstateModelCombo.Text = SuggestedStudioBlockstateModelReference();
            if (_studioBlockstateXRotationCombo is not null)
                _studioBlockstateXRotationCombo.SelectedItem = "0";
            if (_studioBlockstateYRotationCombo is not null)
                _studioBlockstateYRotationCombo.SelectedItem = FacingToStudioYRotation(facing).ToString();
            if (_studioBlockstateUvLockCheck is not null)
                _studioBlockstateUvLockCheck.IsChecked = false;
            if (_studioBlockstateSelectionStatus is not null)
            {
                _studioBlockstateSelectionStatus.Text = "Prepared missing combination. Choose a model and click Add variant.";
                _studioBlockstateSelectionStatus.Foreground = Brush("#D8B26A");
            }
        }
        finally
        {
            _studioBlockstateUiUpdating = false;
        }
        UpdateStudioBlockstateActionButtons();
    }

    private void ApplyStudioBlockstateVariant(bool addAsNew)
    {
        if (_studioBlockstateDocument is null || _studioBlockstateKeyBox is null || _studioBlockstateModelCombo is null)
            return;

        var keyText = _studioBlockstateKeyBox.Text.Trim();
        var key = string.IsNullOrWhiteSpace(keyText) ? "default" : keyText;
        if (!TryParseStudioVariantKey(key, out var properties, out var propertyError))
        {
            SetStudioBlockstateSelectionError(propertyError);
            return;
        }

        var model = NormalizeStudioModelReference(_studioBlockstateModelCombo.Text);
        if (string.IsNullOrWhiteSpace(model))
        {
            SetStudioBlockstateSelectionError("A model reference is required.");
            return;
        }

        var duplicate = _studioBlockstateDocument.EditableVariants.FirstOrDefault(candidate =>
            candidate.Key.Equals(key, StringComparison.OrdinalIgnoreCase) &&
            (addAsNew || !ReferenceEquals(candidate, _studioBlockstateSelectedVariant)));
        if (duplicate is not null)
        {
            SetStudioBlockstateSelectionError($"Variant '{key}' already exists.");
            return;
        }

        var replacement = new MinecraftBlockVariantDescriptor
        {
            Key = key,
            Properties = properties,
            ModelReference = model,
            XRotation = ReadStudioRotation(_studioBlockstateXRotationCombo),
            YRotation = ReadStudioRotation(_studioBlockstateYRotationCombo),
            UvLock = _studioBlockstateUvLockCheck?.IsChecked == true
        };

        if (!IsStudioBlockstateVariantValid(replacement, out var reason))
        {
            SetStudioBlockstateSelectionError(reason);
            return;
        }

        if (addAsNew || _studioBlockstateSelectedVariant is null)
        {
            _studioBlockstateDocument.EditableVariants.Add(replacement);
        }
        else
        {
            var index = _studioBlockstateDocument.EditableVariants.IndexOf(_studioBlockstateSelectedVariant);
            if (index < 0)
                _studioBlockstateDocument.EditableVariants.Add(replacement);
            else
                _studioBlockstateDocument.EditableVariants[index] = replacement;
        }

        _studioBlockstateSelectedVariant = replacement;
        _studioBlockstateDocument.Modified = !StudioBlockstateVariantListsEquivalent(
            _studioBlockstateDocument.SourceVariants,
            _studioBlockstateDocument.EditableVariants);
        PublishStudioBlockstateDocument();
        RefreshStudioBlockstateEditor();
        SelectStudioVariantInMainViewer(replacement);
        SetStudioBlockstateSelectionMessage("Variant updated in memory. Project JSON is unchanged.", "#74C99A");
    }

    private void RemoveStudioBlockstateVariant()
    {
        if (_studioBlockstateDocument is null || _studioBlockstateSelectedVariant is null)
            return;
        _studioBlockstateDocument.EditableVariants.Remove(_studioBlockstateSelectedVariant);
        _studioBlockstateSelectedVariant = null;
        _studioBlockstateDocument.Modified = !StudioBlockstateVariantListsEquivalent(
            _studioBlockstateDocument.SourceVariants,
            _studioBlockstateDocument.EditableVariants);
        PublishStudioBlockstateDocument();
        RefreshStudioBlockstateEditor();
        SetStudioBlockstateSelectionMessage("Variant removed from the in-memory blockstate preview.", "#D8B26A");
    }

    private void ResetStudioBlockstateDocument()
    {
        if (_studioBlockstateDocument is null)
            return;
        _studioBlockstateDocument.EditableVariants = _studioBlockstateDocument.SourceVariants
            .Select(CloneStudioBlockstateVariant)
            .ToList();
        _studioBlockstateDocument.Modified = false;
        _studioBlockstateSelectedVariant = null;
        PublishStudioBlockstateDocument();
        RefreshStudioBlockstateEditor();
        SetStudioBlockstateSelectionMessage("Blockstate reset to the repository source.", "#74C99A");
    }

    private void PublishStudioBlockstateDocument()
    {
        if (_studioBlockstateDocument is null || _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor selected)
            return;

        var index = _studioBlockAssets.ToList().FindIndex(asset =>
            asset.Id.Equals(_studioBlockstateDocument.AssetId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return;

        var replacement = new MinecraftBlockAssetDescriptor
        {
            Id = selected.Id,
            DisplayName = selected.DisplayName,
            BlockstatePath = selected.BlockstatePath,
            Variants = _studioBlockstateDocument.EditableVariants.Select(CloneStudioBlockstateVariant).ToList()
        };
        var assets = _studioBlockAssets.ToList();
        assets[index] = replacement;
        _studioBlockAssets = assets;

        _studioBlockSelectorUpdate = true;
        try
        {
            _studioBlockMachineCombo.ItemsSource = null;
            _studioBlockMachineCombo.DisplayMemberPath = nameof(MinecraftBlockAssetDescriptor.DisplayName);
            _studioBlockMachineCombo.ItemsSource = _studioBlockAssets;
            _studioBlockMachineCombo.SelectedItem = replacement;
        }
        finally
        {
            _studioBlockSelectorUpdate = false;
        }
        PopulateStudioBlockSelectors();
    }

    private bool IsStudioBlockstateVariantValid(MinecraftBlockVariantDescriptor variant, out string reason)
    {
        if (string.IsNullOrWhiteSpace(variant.ModelReference))
        {
            reason = "Missing model reference.";
            return false;
        }
        if (!IsStudioRotationValid(variant.XRotation) || !IsStudioRotationValid(variant.YRotation))
        {
            reason = "Blockstate rotations must be 0°, 90°, 180° or 270°.";
            return false;
        }
        if (!TryParseStudioVariantKey(variant.Key, out _, out reason))
            return false;
        if (string.IsNullOrWhiteSpace(_repoRoot))
        {
            reason = "Repository root is unavailable.";
            return false;
        }

        var reference = NormalizeStudioModelReference(variant.ModelReference);
        var colon = reference.IndexOf(':');
        var resourceNamespace = colon >= 0 ? reference[..colon] : "forever_skyblock";
        if (!resourceNamespace.Equals("forever_skyblock", StringComparison.OrdinalIgnoreCase))
        {
            reason = string.Empty;
            return true;
        }
        var resource = colon >= 0 ? reference[(colon + 1)..] : reference;
        var path = Path.Combine(
            _repoRoot,
            "mod", "src", "main", "resources", "assets", "forever_skyblock", "models",
            resource.Replace('/', Path.DirectorySeparatorChar) + ".json");
        if (!File.Exists(path))
        {
            reason = $"Model target does not exist: {reference}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool StudioVariantMatchesViewMode(MinecraftBlockVariantDescriptor variant)
    {
        if (!HasStudioActiveProperty(variant))
            return true;
        return _studioBlockstateViewMode switch
        {
            StudioBlockstateViewMode.Idle => IsStudioActiveValue(variant, false),
            StudioBlockstateViewMode.Active => IsStudioActiveValue(variant, true),
            _ => true
        };
    }

    private static bool HasStudioActiveProperty(MinecraftBlockVariantDescriptor variant)
        => variant.Properties.ContainsKey("active");

    private static bool IsStudioActiveValue(MinecraftBlockVariantDescriptor variant, bool active)
        => variant.Properties.TryGetValue("active", out var value) &&
           bool.TryParse(value, out var parsed) && parsed == active;

    private static string GetStudioVariantFacing(MinecraftBlockVariantDescriptor variant)
        => variant.Properties.TryGetValue("facing", out var facing) && !string.IsNullOrWhiteSpace(facing)
            ? facing
            : "default";

    private static string BuildStudioPropertyKey(
        MinecraftBlockVariantDescriptor variant,
        bool excludeActive,
        bool excludeFacing)
    {
        var properties = variant.Properties
            .Where(pair => !(excludeActive && pair.Key.Equals("active", StringComparison.OrdinalIgnoreCase)))
            .Where(pair => !(excludeFacing && pair.Key.Equals("facing", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        return FormatStudioVariantKey(properties);
    }

    private static string HumanizeStudioBlockstateRow(string key)
    {
        if (key.Equals("default", StringComparison.OrdinalIgnoreCase))
            return "Default";
        return string.Join("\n", key.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string StudioBlockstateCellText(MinecraftBlockVariantDescriptor variant)
        => $"✓  {NormalizeStudioRotation(variant.YRotation)}°\n{ShortStudioModelName(variant.ModelReference)}";

    private static string ShortStudioModelName(string modelReference)
    {
        var slash = modelReference.LastIndexOf('/');
        return slash >= 0 && slash < modelReference.Length - 1 ? modelReference[(slash + 1)..] : modelReference;
    }

    private static string DescribeStudioBlockstateVariant(MinecraftBlockVariantDescriptor variant)
        => $"{variant.Key} → {variant.ModelReference} · X {NormalizeStudioRotation(variant.XRotation)}° · Y {NormalizeStudioRotation(variant.YRotation)}°" +
           (variant.UvLock ? " · UV lock" : string.Empty);

    private static bool StudioBlockstateVariantsEquivalent(MinecraftBlockVariantDescriptor left, MinecraftBlockVariantDescriptor right)
        => left.ModelReference.Equals(right.ModelReference, StringComparison.OrdinalIgnoreCase) &&
           NormalizeStudioRotation(left.XRotation) == NormalizeStudioRotation(right.XRotation) &&
           NormalizeStudioRotation(left.YRotation) == NormalizeStudioRotation(right.YRotation) &&
           left.UvLock == right.UvLock;

    private static bool StudioBlockstateVariantListsEquivalent(
        IReadOnlyList<MinecraftBlockVariantDescriptor> left,
        IReadOnlyList<MinecraftBlockVariantDescriptor> right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var source in left)
        {
            var candidate = right.FirstOrDefault(value => value.Key.Equals(source.Key, StringComparison.OrdinalIgnoreCase));
            if (candidate is null || !StudioBlockstateVariantsEquivalent(source, candidate))
                return false;
        }
        return true;
    }

    private static string BuildStudioBlockstateCompareDescription(
        MinecraftBlockVariantDescriptor? idle,
        MinecraftBlockVariantDescriptor? active)
    {
        var idleText = idle is null ? "Idle: missing" : "Idle: " + DescribeStudioBlockstateVariant(idle);
        var activeText = active is null ? "Active: missing" : "Active: " + DescribeStudioBlockstateVariant(active);
        return idleText + "\n" + activeText;
    }

    private static MinecraftBlockVariantDescriptor CloneStudioBlockstateVariant(MinecraftBlockVariantDescriptor source)
        => new()
        {
            Key = source.Key,
            Properties = new Dictionary<string, string>(source.Properties, StringComparer.OrdinalIgnoreCase),
            ModelReference = source.ModelReference,
            XRotation = source.XRotation,
            YRotation = source.YRotation,
            UvLock = source.UvLock
        };

    private static bool TryParseStudioVariantKey(
        string key,
        out Dictionary<string, string> properties,
        out string error)
    {
        properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var value = key.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pieces.Length != 2 || string.IsNullOrWhiteSpace(pieces[0]) || string.IsNullOrWhiteSpace(pieces[1]))
            {
                error = $"Invalid property expression '{part}'. Expected name=value.";
                return false;
            }
            if (!properties.TryAdd(pieces[0], pieces[1]))
            {
                error = $"Property '{pieces[0]}' is specified more than once.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static Dictionary<string, string> ParseStudioVariantKey(string key)
        => TryParseStudioVariantKey(key, out var properties, out _) ? properties : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static string FormatStudioVariantKey(IReadOnlyDictionary<string, string> properties)
        => properties.Count == 0
            ? "default"
            : string.Join(",", properties.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));

    private static string NormalizeStudioModelReference(string value)
    {
        var result = value.Trim();
        if (string.IsNullOrWhiteSpace(result))
            return string.Empty;
        return result.Contains(':') ? result : $"forever_skyblock:{result}";
    }

    private string SuggestedStudioBlockstateModelReference()
    {
        if (_studioBlockstateSelectedVariant is not null)
            return _studioBlockstateSelectedVariant.ModelReference;
        var active = _studioBlockstateViewMode == StudioBlockstateViewMode.Active;
        var variants = _studioBlockstateDocument?.EditableVariants ?? [];
        return variants.FirstOrDefault(variant => !HasStudioActiveProperty(variant) || IsStudioActiveValue(variant, active))?.ModelReference
               ?? variants.FirstOrDefault()?.ModelReference
               ?? string.Empty;
    }

    private static int FacingToStudioYRotation(string facing)
        => facing.ToLowerInvariant() switch
        {
            "east" => 90,
            "south" => 180,
            "west" => 270,
            _ => 0
        };

    private static int ReadStudioRotation(ComboBox? combo)
        => combo?.SelectedItem?.ToString() is { } text && int.TryParse(text, out var value)
            ? NormalizeStudioRotation(value)
            : 0;

    private static int NormalizeStudioRotation(int value)
        => ((value % 360) + 360) % 360;

    private static bool IsStudioRotationValid(int value)
        => NormalizeStudioRotation(value) is 0 or 90 or 180 or 270;

    private void UpdateStudioBlockstateActionButtons()
    {
        if (_studioBlockstateUiUpdating)
            return;
        if (_studioBlockstateApplyButton is not null)
            _studioBlockstateApplyButton.IsEnabled = _studioBlockstateSelectedVariant is not null;
        if (_studioBlockstateRemoveButton is not null)
            _studioBlockstateRemoveButton.IsEnabled = _studioBlockstateSelectedVariant is not null;
    }

    private void SetStudioBlockstateSelectionError(string message)
        => SetStudioBlockstateSelectionMessage(message, "#E18B8B");

    private void SetStudioBlockstateSelectionMessage(string message, string color)
    {
        if (_studioBlockstateSelectionStatus is null)
            return;
        _studioBlockstateSelectionStatus.Text = message;
        _studioBlockstateSelectionStatus.Foreground = Brush(color);
    }
}

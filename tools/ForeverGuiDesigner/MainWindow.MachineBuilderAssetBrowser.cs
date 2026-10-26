using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 3.1 asset-browser layer. Replaces the compact flat Phase-3 palette with a scalable,
/// category-first browser while continuing to consume the canonical ForeverStudioLibraryCatalog.
/// </summary>
public partial class MainWindow
{
    private bool _machineBuilderAssetBrowserPhase3Initialized;
    private Border? _machineBuilderAssetBrowserHost;
    private TextBox? _machineBuilderAssetBrowserSearch;
    private StackPanel? _machineBuilderAssetBrowserGroups;
    private TextBlock? _machineBuilderAssetBrowserSummary;
    private string _machineBuilderAssetBrowserFilter = "all";
    private readonly Dictionary<string, Button> _machineBuilderAssetBrowserFilterButtons =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _machineBuilderAssetBrowserExpandedCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Structure / Housing",
            "Mechanical / Rotary"
        };

    public void EnableForeverMachineBuilderAssetBrowserPhase3()
    {
        if (_machineBuilderAssetBrowserPhase3Initialized || _machineBuilderVisualSurface is null)
            return;
        _machineBuilderAssetBrowserPhase3Initialized = true;

        var paletteTitle = FindStudioVisualChildren<TextBlock>(_machineBuilderVisualSurface)
            .FirstOrDefault(text => string.Equals(text.Text, "ASSET PALETTE", StringComparison.Ordinal));
        if (paletteTitle?.Parent is not Grid paletteGrid)
            return;

        // Give the library enough horizontal room for readable names while preserving the central viewport.
        if (paletteGrid.Parent is Border paletteCard && paletteCard.Parent is Grid body && body.ColumnDefinitions.Count > 0)
            body.ColumnDefinitions[0].Width = new GridLength(350);

        paletteTitle.Visibility = Visibility.Collapsed;
        if (_machineBuilderVisualAssetSearch is not null)
            _machineBuilderVisualAssetSearch.Visibility = Visibility.Collapsed;
        if (_machineBuilderVisualAssetList is not null)
            _machineBuilderVisualAssetList.Visibility = Visibility.Collapsed;

        var oldAddButton = FindStudioVisualChildren<Button>(paletteGrid)
            .FirstOrDefault(button => FindStudioVisualChildren<TextBlock>(button)
                .Any(text => text.Text.Contains("ADD SELECTED ASSET", StringComparison.OrdinalIgnoreCase)));
        if (oldAddButton is not null)
            oldAddButton.Visibility = Visibility.Collapsed;

        // The old flat list occupied only 210 px. Preserve the lower Assembly Components section,
        // but spend a little more vertical space on discovery now that the catalog is much larger.
        if (paletteGrid.RowDefinitions.Count > 3)
            paletteGrid.RowDefinitions[3].Height = new GridLength(365);

        _machineBuilderAssetBrowserHost = BuildMachineBuilderAssetBrowser();
        Grid.SetRow(_machineBuilderAssetBrowserHost, 0);
        Grid.SetRowSpan(_machineBuilderAssetBrowserHost, Math.Min(5, paletteGrid.RowDefinitions.Count));
        Panel.SetZIndex(_machineBuilderAssetBrowserHost, 20);
        paletteGrid.Children.Add(_machineBuilderAssetBrowserHost);

        _machineBuilderVisualSurface.IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                RefreshMachineBuilderAssetBrowser();
        };

        UpdateMachineBuilderAssetBrowserFilterButtons();
        RefreshMachineBuilderAssetBrowser();
    }

    private Border BuildMachineBuilderAssetBrowser()
    {
        var host = new Border
        {
            Background = Brush("#11191D"),
            CornerRadius = new CornerRadius(5)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());

        var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock
        {
            Text = "ASSET BROWSER",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        _machineBuilderAssetBrowserSummary = new TextBlock
        {
            Foreground = Brush("#6F858E"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_machineBuilderAssetBrowserSummary, 1);
        header.Children.Add(_machineBuilderAssetBrowserSummary);
        grid.Children.Add(header);

        _machineBuilderAssetBrowserSearch = MachineBuilderTextBox();
        _machineBuilderAssetBrowserSearch.Height = 34;
        _machineBuilderAssetBrowserSearch.ToolTip = "Search asset names, categories, descriptions and tags";
        _machineBuilderAssetBrowserSearch.TextChanged += (_, _) => RefreshMachineBuilderAssetBrowser();
        Grid.SetRow(_machineBuilderAssetBrowserSearch, 1);
        grid.Children.Add(_machineBuilderAssetBrowserSearch);

        var filters = new WrapPanel { Margin = new Thickness(0, 8, 0, 7) };
        AddMachineBuilderAssetBrowserFilter(filters, "all", "ALL", 46);
        AddMachineBuilderAssetBrowserFilter(filters, "structure", "STRUCTURE", 78);
        AddMachineBuilderAssetBrowserFilter(filters, "mechanical", "MECHANICAL", 88);
        AddMachineBuilderAssetBrowserFilter(filters, "transport", "TRANSPORT", 80);
        AddMachineBuilderAssetBrowserFilter(filters, "fluid", "FLUID", 52);
        AddMachineBuilderAssetBrowserFilter(filters, "electrical", "ELECTRICAL", 82);
        AddMachineBuilderAssetBrowserFilter(filters, "control", "CONTROL", 64);
        AddMachineBuilderAssetBrowserFilter(filters, "thermal", "THERMAL", 66);
        AddMachineBuilderAssetBrowserFilter(filters, "detail", "DETAIL", 56);
        Grid.SetRow(filters, 2);
        grid.Children.Add(filters);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brush("#091014"),
            BorderBrush = Brush("#2C4149"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            CanContentScroll = false
        };
        _machineBuilderAssetBrowserGroups = new StackPanel();
        scroll.Content = _machineBuilderAssetBrowserGroups;
        Grid.SetRow(scroll, 3);
        grid.Children.Add(scroll);

        host.Child = grid;
        return host;
    }

    private void AddMachineBuilderAssetBrowserFilter(Panel parent, string key, string label, double width)
    {
        var button = CreateStudioChromeButton(label, width, false);
        button.Height = 29;
        button.Margin = new Thickness(0, 0, 5, 5);
        button.Tag = key;
        button.Click += (_, _) =>
        {
            _machineBuilderAssetBrowserFilter = key;
            UpdateMachineBuilderAssetBrowserFilterButtons();
            RefreshMachineBuilderAssetBrowser();
        };
        _machineBuilderAssetBrowserFilterButtons[key] = button;
        parent.Children.Add(button);
    }

    private void UpdateMachineBuilderAssetBrowserFilterButtons()
    {
        foreach (var (key, button) in _machineBuilderAssetBrowserFilterButtons)
        {
            if (button.Content is not Border surface)
                continue;
            var selected = key.Equals(_machineBuilderAssetBrowserFilter, StringComparison.OrdinalIgnoreCase);
            surface.Background = Brush(selected ? "#173B3E" : "#1B282E");
            surface.BorderBrush = Brush(selected ? "#63C8C8" : "#344B54");
            if (surface.Child is TextBlock label)
                label.Foreground = Brush(selected ? "#E9FFFF" : "#BDCCD1");
        }
    }

    private void RefreshMachineBuilderAssetBrowser()
    {
        if (_machineBuilderAssetBrowserGroups is null)
            return;

        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        var catalog = _machineBuilderLibraryLoad.Catalog;
        var query = _machineBuilderAssetBrowserSearch?.Text?.Trim() ?? string.Empty;
        var allVisualAssets = catalog.Search(string.Empty, ForeverStudioLibraryItemKind.Asset)
            .Where(IsMachineBuilderVisualAsset)
            .ToList();
        var matching = catalog.Search(query, ForeverStudioLibraryItemKind.Asset)
            .Where(IsMachineBuilderVisualAsset)
            .Where(item => MachineBuilderAssetMatchesFilter(item, _machineBuilderAssetBrowserFilter))
            .OrderBy(item => MachineBuilderAssetCategoryRank(item.Category))
            .ThenBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_machineBuilderAssetBrowserSummary is not null)
        {
            _machineBuilderAssetBrowserSummary.Text = query.Length == 0 && _machineBuilderAssetBrowserFilter == "all"
                ? $"{allVisualAssets.Count} ASSETS"
                : $"{matching.Count} / {allVisualAssets.Count}";
        }

        _machineBuilderAssetBrowserGroups.Children.Clear();
        if (matching.Count == 0)
        {
            _machineBuilderAssetBrowserGroups.Children.Add(new TextBlock
            {
                Text = "No visual assets match this search/filter.",
                Foreground = Brush("#78909A"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10, 12, 10, 12)
            });
            return;
        }

        foreach (var group in matching.GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase))
            _machineBuilderAssetBrowserGroups.Children.Add(BuildMachineBuilderAssetCategory(group.Key, group.ToList(), query));
    }

    private FrameworkElement BuildMachineBuilderAssetCategory(
        string category,
        IReadOnlyList<ForeverStudioLibraryItem> items,
        string query)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 3) };
        var entries = new StackPanel();
        var forceExpanded = query.Length > 0 || !_machineBuilderAssetBrowserFilter.Equals("all", StringComparison.OrdinalIgnoreCase);
        var expanded = forceExpanded || _machineBuilderAssetBrowserExpandedCategories.Contains(category);
        entries.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;

        var header = new Border
        {
            Background = Brush("#101B20"),
            BorderBrush = Brush("#273B43"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 7, 7, 7),
            Cursor = Cursors.Hand
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var caret = new TextBlock
        {
            Text = expanded ? "▾" : "›",
            Foreground = Brush("#6CCBC9"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        headerGrid.Children.Add(caret);
        var title = new TextBlock
        {
            Text = category.ToUpperInvariant(),
            Foreground = Brush("#B9CBCFEF"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 1);
        headerGrid.Children.Add(title);
        var count = new TextBlock
        {
            Text = items.Count.ToString(),
            Foreground = Brush("#607780"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(count, 2);
        headerGrid.Children.Add(count);
        header.Child = headerGrid;
        header.MouseLeftButtonUp += (_, _) =>
        {
            var willExpand = entries.Visibility != Visibility.Visible;
            entries.Visibility = willExpand ? Visibility.Visible : Visibility.Collapsed;
            caret.Text = willExpand ? "▾" : "›";
            if (willExpand)
                _machineBuilderAssetBrowserExpandedCategories.Add(category);
            else
                _machineBuilderAssetBrowserExpandedCategories.Remove(category);
        };
        section.Children.Add(header);

        foreach (var item in items)
            entries.Children.Add(BuildMachineBuilderAssetRow(item));
        section.Children.Add(entries);
        return section;
    }

    private FrameworkElement BuildMachineBuilderAssetRow(ForeverStudioLibraryItem item)
    {
        var row = new Border
        {
            Background = Brush("#0B1317"),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 5, 6),
            Margin = new Thickness(0, 1, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = $"{item.Description}\n\n{item.Id} v{item.Version}\nSource: {item.Source}"
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = item.DisplayName,
            Foreground = Brush("#DDE8EB"),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        copy.Children.Add(new TextBlock
        {
            Text = item.Description,
            Foreground = Brush("#6F858E"),
            FontSize = 8.7,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        grid.Children.Add(copy);

        var add = CreateStudioChromeButton("+", 30, false);
        add.Height = 28;
        add.VerticalAlignment = VerticalAlignment.Center;
        add.ToolTip = $"Add {item.DisplayName}";
        add.Click += (_, e) =>
        {
            AddMachineBuilderAssetFromBrowser(item);
            e.Handled = true;
        };
        Grid.SetColumn(add, 1);
        grid.Children.Add(add);
        row.Child = grid;

        row.MouseEnter += (_, _) =>
        {
            row.Background = Brush("#132328");
            row.BorderBrush = Brush("#365D64");
        };
        row.MouseLeave += (_, _) =>
        {
            row.Background = Brush("#0B1317");
            row.BorderBrush = Brushes.Transparent;
        };
        row.MouseLeftButtonUp += (_, e) =>
        {
            if (e.ClickCount >= 2)
            {
                AddMachineBuilderAssetFromBrowser(item);
                e.Handled = true;
            }
        };
        return row;
    }

    private void AddMachineBuilderAssetFromBrowser(ForeverStudioLibraryItem item)
    {
        if (_machineBuilderDocument is null)
            return;

        var component = ForeverStudioVisualAssemblyService.CreateComponent(item, _machineBuilderDocument.Components);
        _machineBuilderDocument.Components.Add(component);
        _machineBuilderVisualSelectedComponentId = component.Id;
        MarkMachineBuilderVisualDirty();
        RefreshMachineBuilderVisualComponentList(component.Id);
        RefreshMachineBuilderVisualPreview();
        SetMachineBuilderVisualStatus($"Added {item.DisplayName} as '{component.Id}'.", "#74C99A");
    }

    private static bool IsMachineBuilderVisualAsset(ForeverStudioLibraryItem item)
        => item.Capabilities.Contains("visual-component", StringComparer.OrdinalIgnoreCase);

    private static bool MachineBuilderAssetMatchesFilter(ForeverStudioLibraryItem item, string filter)
    {
        if (filter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;

        var category = item.Category;
        bool HasTag(string tag) => item.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        return filter.ToLowerInvariant() switch
        {
            "structure" => category.StartsWith("Structure", StringComparison.OrdinalIgnoreCase) || HasTag("structure"),
            "mechanical" => category.StartsWith("Mechanical", StringComparison.OrdinalIgnoreCase) || HasTag("mechanical"),
            "transport" => category.StartsWith("Transport", StringComparison.OrdinalIgnoreCase) ||
                           category.Contains("Transport", StringComparison.OrdinalIgnoreCase) || HasTag("transport"),
            "fluid" => category.Contains("Fluid", StringComparison.OrdinalIgnoreCase) ||
                       HasTag("fluid") || HasTag("tank") || HasTag("pipe"),
            "electrical" => category.StartsWith("Electrical", StringComparison.OrdinalIgnoreCase) ||
                            HasTag("electrical") || HasTag("energy"),
            "control" => category.StartsWith("Control", StringComparison.OrdinalIgnoreCase) ||
                         category.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                         HasTag("control") || HasTag("interface"),
            "thermal" => category.StartsWith("Thermal", StringComparison.OrdinalIgnoreCase) ||
                         HasTag("thermal") || HasTag("cooling"),
            "detail" => category.StartsWith("Decorative", StringComparison.OrdinalIgnoreCase) ||
                        category.Contains("Detail", StringComparison.OrdinalIgnoreCase) || HasTag("detail"),
            _ => true
        };
    }

    private static int MachineBuilderAssetCategoryRank(string category)
    {
        if (category.StartsWith("Structure / Housing", StringComparison.OrdinalIgnoreCase)) return 0;
        if (category.StartsWith("Structure / Base", StringComparison.OrdinalIgnoreCase)) return 10;
        if (category.StartsWith("Structure / Chamber", StringComparison.OrdinalIgnoreCase)) return 20;
        if (category.StartsWith("Structure / Transport", StringComparison.OrdinalIgnoreCase)) return 30;
        if (category.StartsWith("Structure / Detail", StringComparison.OrdinalIgnoreCase)) return 40;
        if (category.StartsWith("Transport / Item", StringComparison.OrdinalIgnoreCase)) return 50;
        if (category.StartsWith("Transport / Fluid", StringComparison.OrdinalIgnoreCase)) return 60;
        if (category.StartsWith("Mechanical / Rotary", StringComparison.OrdinalIgnoreCase)) return 70;
        if (category.StartsWith("Mechanical / Linear", StringComparison.OrdinalIgnoreCase)) return 80;
        if (category.StartsWith("Mechanical / Tool", StringComparison.OrdinalIgnoreCase)) return 90;
        if (category.StartsWith("Mechanical / Control", StringComparison.OrdinalIgnoreCase)) return 100;
        if (category.StartsWith("Electrical / Power", StringComparison.OrdinalIgnoreCase)) return 110;
        if (category.StartsWith("Electrical / Connector", StringComparison.OrdinalIgnoreCase)) return 120;
        if (category.StartsWith("Electrical / Indicator", StringComparison.OrdinalIgnoreCase)) return 130;
        if (category.StartsWith("Control", StringComparison.OrdinalIgnoreCase)) return 140;
        if (category.StartsWith("Thermal", StringComparison.OrdinalIgnoreCase)) return 150;
        if (category.StartsWith("Decorative", StringComparison.OrdinalIgnoreCase)) return 160;
        return 1000;
    }
}

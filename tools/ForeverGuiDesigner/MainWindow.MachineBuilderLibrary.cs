using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 2 production-library surface. Keeps authoring read-only at this stage: Phase 3/4/5 consume
/// the exact same selected asset/function/animation contracts for placement and graph authoring.
/// </summary>
public partial class MainWindow
{
    private sealed class MachineBuilderLibraryListItem
    {
        public required ForeverStudioLibraryItem Item { get; init; }
        public override string ToString()
            => $"{Item.DisplayName}   ·   {Item.Category}";
    }

    private bool _machineBuilderLibraryPhase2Initialized;
    private Border? _machineBuilderLibraryOverlay;
    private TextBox? _machineBuilderLibrarySearchBox;
    private ListBox? _machineBuilderLibraryItemsList;
    private TextBlock? _machineBuilderLibraryBrowserSummary;
    private TextBlock? _machineBuilderLibraryDetailKind;
    private TextBlock? _machineBuilderLibraryDetailTitle;
    private TextBlock? _machineBuilderLibraryDetailDescription;
    private TextBlock? _machineBuilderLibraryDetailMeta;
    private TextBlock? _machineBuilderLibraryDetailPorts;
    private TextBlock? _machineBuilderLibraryDetailTags;
    private TextBlock? _machineBuilderLibraryBrowserWarnings;
    private ForeverStudioLibraryItemKind? _machineBuilderLibraryKindFilter;
    private readonly Dictionary<string, Button> _machineBuilderLibraryFilterButtons = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverMachineBuilderLibraryPhase2()
    {
        if (_machineBuilderLibraryPhase2Initialized || _studioMachineBuilderWorkspace is null)
            return;
        _machineBuilderLibraryPhase2Initialized = true;

        if (_studioMachineBuilderWorkspace is not DockPanel root)
            return;

        var page = root.Children.OfType<Grid>()
            .FirstOrDefault(grid => grid.ColumnDefinitions.Count >= 5);
        if (page is null)
            return;

        _machineBuilderLibraryOverlay = BuildMachineBuilderLibraryOverlay();
        Grid.SetColumn(_machineBuilderLibraryOverlay, 0);
        Grid.SetColumnSpan(_machineBuilderLibraryOverlay, Math.Max(1, page.ColumnDefinitions.Count));
        Panel.SetZIndex(_machineBuilderLibraryOverlay, 80);
        page.Children.Add(_machineBuilderLibraryOverlay);

        InstallMachineBuilderLibraryOpenButton();
        UpdateMachineBuilderPhase2Labels();

        _studioMachineBuilderWorkspace.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _machineBuilderLibraryOverlay?.Visibility == Visibility.Visible)
            {
                HideMachineBuilderLibrary();
                e.Handled = true;
            }
        };
    }

    private void InstallMachineBuilderLibraryOpenButton()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;

        var libraryPanel = FindStudioVisualChildren<StackPanel>(_studioMachineBuilderWorkspace)
            .FirstOrDefault(panel => panel.Children.OfType<TextBlock>()
                .Any(text => text.Text == "LIBRARY CONTRACT"));
        if (libraryPanel is null)
            return;

        var description = libraryPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(text => text.Text.Contains("Phase 1 establishes", StringComparison.OrdinalIgnoreCase));
        if (description is not null)
        {
            description.Text =
                "Phase 2 ships the production building-block catalog. Search assets, typed machine functions, animation presets and reusable templates without leaving the Machine Builder.";
        }

        if (libraryPanel.Children.OfType<Button>()
            .Any(button => button.Tag?.ToString() == "machine_builder_library_open"))
            return;

        var button = CreateStudioChromeButton("OPEN PRODUCTION LIBRARY", double.NaN, true);
        button.Tag = "machine_builder_library_open";
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.Margin = new Thickness(0, 16, 0, 0);
        button.Click += (_, _) => ShowMachineBuilderLibrary();
        libraryPanel.Children.Add(button);
    }

    private void UpdateMachineBuilderPhase2Labels()
    {
        if (_studioMachineBuilderWorkspace is null)
            return;

        foreach (var text in FindStudioVisualChildren<TextBlock>(_studioMachineBuilderWorkspace))
        {
            if (text.Text.StartsWith("PHASE 1 ·", StringComparison.OrdinalIgnoreCase))
                text.Text = "PHASE 2 · PRODUCTION LIBRARY";
            else if (text.Text.StartsWith("Phase 1 writes only", StringComparison.OrdinalIgnoreCase))
            {
                text.Text =
                    "Phase 2 establishes the reusable production catalog. The guided machine workflow remains on Identity until Phase 3 unlocks visual assembly.";
            }
        }
    }

    private Border BuildMachineBuilderLibraryOverlay()
    {
        var overlay = new Border
        {
            Visibility = Visibility.Collapsed,
            Background = Brush("#0C1216"),
            BorderBrush = Brush("#38515B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Margin = new Thickness(0),
            Padding = new Thickness(22),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var shell = new Grid();
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        shell.RowDefinitions.Add(new RowDefinition());

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heading = new StackPanel();
        heading.Children.Add(new TextBlock
        {
            Text = "PHASE 2 · PRODUCTION LIBRARY",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Building blocks for every later authoring surface",
            Foreground = Brush("#EFF5F6"),
            FontSize = 25,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        });
        _machineBuilderLibraryBrowserSummary = new TextBlock
        {
            Foreground = Brush("#879AA2"),
            FontSize = 11,
            Margin = new Thickness(0, 6, 0, 0)
        };
        heading.Children.Add(_machineBuilderLibraryBrowserSummary);
        header.Children.Add(heading);

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var reload = CreateStudioChromeButton("RELOAD", 90, false);
        reload.Height = 36;
        reload.Click += (_, _) =>
        {
            RefreshMachineBuilderRepositoryContext();
            RefreshMachineBuilderLibraryBrowser();
        };
        headerActions.Children.Add(reload);
        var close = CreateStudioChromeButton("CLOSE", 82, false);
        close.Height = 36;
        close.Margin = new Thickness(10, 0, 0, 0);
        close.Click += (_, _) => HideMachineBuilderLibrary();
        headerActions.Children.Add(close);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);
        Grid.SetRow(header, 0);
        shell.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        body.ColumnDefinitions.Add(new ColumnDefinition());

        var browserCard = CreateStudioGlassCard(new CornerRadius(9), strongShadow: false);
        var browser = new Grid { Margin = new Thickness(18) };
        browser.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browser.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browser.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        browser.RowDefinitions.Add(new RowDefinition());
        browser.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _machineBuilderLibrarySearchBox = MachineBuilderTextBox();
        _machineBuilderLibrarySearchBox.ToolTip = "Search name, ID, category, description, tags and capabilities";
        _machineBuilderLibrarySearchBox.TextChanged += (_, _) => RefreshMachineBuilderLibraryBrowser();
        Grid.SetRow(_machineBuilderLibrarySearchBox, 0);
        browser.Children.Add(_machineBuilderLibrarySearchBox);

        var filters = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        AddMachineBuilderLibraryFilter(filters, "all", "ALL", null, 58);
        AddMachineBuilderLibraryFilter(filters, "asset", "ASSETS", ForeverStudioLibraryItemKind.Asset, 72);
        AddMachineBuilderLibraryFilter(filters, "function", "FUNCTIONS", ForeverStudioLibraryItemKind.Function, 92);
        AddMachineBuilderLibraryFilter(filters, "animation", "ANIMATIONS", ForeverStudioLibraryItemKind.Animation, 102);
        AddMachineBuilderLibraryFilter(filters, "template", "TEMPLATES", ForeverStudioLibraryItemKind.Template, 94);
        Grid.SetRow(filters, 1);
        browser.Children.Add(filters);

        _machineBuilderLibraryItemsList = new ListBox
        {
            Background = Brush("#091014"),
            Foreground = Brush("#D6E1E4"),
            BorderBrush = Brush("#293C44"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5),
            FontSize = 10.5,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        _machineBuilderLibraryItemsList.SelectionChanged += (_, _) => RefreshMachineBuilderLibraryDetail();
        _machineBuilderLibraryItemsList.MouseDoubleClick += (_, _) => RefreshMachineBuilderLibraryDetail();
        Grid.SetRow(_machineBuilderLibraryItemsList, 3);
        browser.Children.Add(_machineBuilderLibraryItemsList);

        _machineBuilderLibraryBrowserWarnings = new TextBlock
        {
            Foreground = Brush("#D7B36B"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(_machineBuilderLibraryBrowserWarnings, 4);
        browser.Children.Add(_machineBuilderLibraryBrowserWarnings);
        browserCard.Child = browser;
        Grid.SetColumn(browserCard, 0);
        body.Children.Add(browserCard);

        var detailCard = CreateStudioGlassCard(new CornerRadius(9), strongShadow: false);
        var detailScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var detail = new StackPanel { Margin = new Thickness(26, 23, 26, 26) };
        _machineBuilderLibraryDetailKind = new TextBlock
        {
            Text = "SELECT A BUILDING BLOCK",
            Foreground = Brush("#72D5D3"),
            FontSize = 10,
            FontWeight = FontWeights.Bold
        };
        detail.Children.Add(_machineBuilderLibraryDetailKind);
        _machineBuilderLibraryDetailTitle = new TextBlock
        {
            Text = "Production Library",
            Foreground = Brush("#EFF5F6"),
            FontSize = 27,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 7, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        detail.Children.Add(_machineBuilderLibraryDetailTitle);
        _machineBuilderLibraryDetailDescription = new TextBlock
        {
            Text = "Choose an asset, function, animation or template to inspect its stable authoring contract.",
            Foreground = Brush("#91A3AA"),
            FontSize = 12,
            LineHeight = 19,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 9, 0, 0)
        };
        detail.Children.Add(_machineBuilderLibraryDetailDescription);

        detail.Children.Add(MachineBuilderLibrarySectionLabel("CONTRACT", 24));
        _machineBuilderLibraryDetailMeta = MachineBuilderLibraryDetailText();
        detail.Children.Add(_machineBuilderLibraryDetailMeta);
        detail.Children.Add(MachineBuilderLibrarySectionLabel("TYPED PORTS", 20));
        _machineBuilderLibraryDetailPorts = MachineBuilderLibraryDetailText();
        detail.Children.Add(_machineBuilderLibraryDetailPorts);
        detail.Children.Add(MachineBuilderLibrarySectionLabel("DISCOVERY", 20));
        _machineBuilderLibraryDetailTags = MachineBuilderLibraryDetailText();
        detail.Children.Add(_machineBuilderLibraryDetailTags);

        var phaseNote = new Border
        {
            Background = Brush("#101A1E"),
            BorderBrush = Brush("#2D424A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(13, 11, 13, 11),
            Margin = new Thickness(0, 24, 0, 0),
            Child = new TextBlock
            {
                Text = "Phase 2 defines and validates the palette. Phase 3 places Asset blocks in the visual assembly; Phase 4 consumes Function blocks in the typed behavior graph; Phase 5 binds Animation presets to semantic components.",
                Foreground = Brush("#82969E"),
                FontSize = 10.5,
                LineHeight = 16,
                TextWrapping = TextWrapping.Wrap
            }
        };
        detail.Children.Add(phaseNote);

        detailScroll.Content = detail;
        detailCard.Child = detailScroll;
        Grid.SetColumn(detailCard, 2);
        body.Children.Add(detailCard);

        Grid.SetRow(body, 2);
        shell.Children.Add(body);
        overlay.Child = shell;
        return overlay;
    }

    private void AddMachineBuilderLibraryFilter(
        Panel parent,
        string key,
        string label,
        ForeverStudioLibraryItemKind? kind,
        double width)
    {
        var button = CreateStudioChromeButton(label, width, false);
        button.Height = 32;
        button.Margin = new Thickness(0, 0, 7, 7);
        button.Tag = key;
        button.Click += (_, _) =>
        {
            _machineBuilderLibraryKindFilter = kind;
            UpdateMachineBuilderLibraryFilterButtons();
            RefreshMachineBuilderLibraryBrowser();
        };
        _machineBuilderLibraryFilterButtons[key] = button;
        parent.Children.Add(button);
    }

    private static TextBlock MachineBuilderLibrarySectionLabel(string text, double topMargin)
        => new()
        {
            Text = text,
            Foreground = Brush("#6BCAC9"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, topMargin, 0, 7)
        };

    private static TextBlock MachineBuilderLibraryDetailText()
        => new()
        {
            Foreground = Brush("#B5C4C9"),
            FontSize = 11,
            LineHeight = 18,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Segoe UI")
        };

    private void ShowMachineBuilderLibrary()
    {
        RefreshMachineBuilderRepositoryContext();
        RefreshMachineBuilderLibraryBrowser();
        if (_machineBuilderLibraryOverlay is not null)
        {
            _machineBuilderLibraryOverlay.Visibility = Visibility.Visible;
            _machineBuilderLibrarySearchBox?.Focus();
        }
    }

    private void HideMachineBuilderLibrary()
    {
        if (_machineBuilderLibraryOverlay is not null)
            _machineBuilderLibraryOverlay.Visibility = Visibility.Collapsed;
    }

    private void RefreshMachineBuilderLibraryBrowser()
    {
        if (_machineBuilderLibraryLoad is null)
            _machineBuilderLibraryLoad = ForeverStudioLibraryCatalog.Load(_repoRoot);

        var catalog = _machineBuilderLibraryLoad.Catalog;
        var counts = catalog.LatestItemCounts();
        if (_machineBuilderLibraryBrowserSummary is not null)
        {
            var total = counts.Values.Sum();
            _machineBuilderLibraryBrowserSummary.Text =
                $"{total} current building blocks  ·  " +
                $"{counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Asset)} assets  ·  " +
                $"{counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Function)} functions  ·  " +
                $"{counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Animation)} animations  ·  " +
                $"{counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Template)} templates";
        }

        var query = _machineBuilderLibrarySearchBox?.Text ?? string.Empty;
        var result = catalog.Search(query, _machineBuilderLibraryKindFilter);
        if (_machineBuilderLibraryItemsList is not null)
        {
            var selectedId = (_machineBuilderLibraryItemsList.SelectedItem as MachineBuilderLibraryListItem)?.Item.Id;
            var list = result.Select(item => new MachineBuilderLibraryListItem { Item = item }).ToList();
            _machineBuilderLibraryItemsList.ItemsSource = list;
            _machineBuilderLibraryItemsList.SelectedItem = list.FirstOrDefault(item =>
                string.Equals(item.Item.Id, selectedId, StringComparison.OrdinalIgnoreCase));
            if (_machineBuilderLibraryItemsList.SelectedItem is null && list.Count > 0)
                _machineBuilderLibraryItemsList.SelectedIndex = 0;
        }

        if (_machineBuilderLibraryBrowserWarnings is not null)
        {
            _machineBuilderLibraryBrowserWarnings.Text = _machineBuilderLibraryLoad.Warnings.Count == 0
                ? $"{result.Count} matching item(s) · library validation clean"
                : $"{result.Count} matching item(s) · {_machineBuilderLibraryLoad.Warnings.Count} library warning(s)";
            _machineBuilderLibraryBrowserWarnings.Foreground = Brush(
                _machineBuilderLibraryLoad.Warnings.Count == 0 ? "#72C894" : "#D7B36B");
        }
        RefreshMachineBuilderLibraryDetail();
    }

    private void UpdateMachineBuilderLibraryFilterButtons()
    {
        var active = _machineBuilderLibraryKindFilter?.ToString().ToLowerInvariant() ?? "all";
        foreach (var (key, button) in _machineBuilderLibraryFilterButtons)
        {
            if (button.Content is not Border surface)
                continue;
            var selected = key.Equals(active, StringComparison.OrdinalIgnoreCase);
            surface.Background = Brush(selected ? "#1B3B3E" : "#202D34");
            surface.BorderBrush = Brush(selected ? "#65C8C8" : "#41555E");
            if (surface.Child is TextBlock label)
                label.Foreground = Brush(selected ? "#E9FFFF" : "#E3ECEF");
        }
    }

    private void RefreshMachineBuilderLibraryDetail()
    {
        var item = (_machineBuilderLibraryItemsList?.SelectedItem as MachineBuilderLibraryListItem)?.Item;
        if (item is null)
        {
            if (_machineBuilderLibraryDetailKind is not null)
                _machineBuilderLibraryDetailKind.Text = "SELECT A BUILDING BLOCK";
            return;
        }

        if (_machineBuilderLibraryDetailKind is not null)
            _machineBuilderLibraryDetailKind.Text = $"{item.Kind.ToString().ToUpperInvariant()} · {item.Category.ToUpperInvariant()}";
        if (_machineBuilderLibraryDetailTitle is not null)
            _machineBuilderLibraryDetailTitle.Text = item.DisplayName;
        if (_machineBuilderLibraryDetailDescription is not null)
            _machineBuilderLibraryDetailDescription.Text = item.Description;
        if (_machineBuilderLibraryDetailMeta is not null)
        {
            _machineBuilderLibraryDetailMeta.Text =
                $"ID             {item.Id}\n" +
                $"Version        {item.Version}\n" +
                $"Generator      {(string.IsNullOrWhiteSpace(item.GeneratorKey) ? "—" : item.GeneratorKey)}\n" +
                $"Source         {item.Source}\n" +
                $"Composite      {(item.Composite ? "yes · expandable preset" : "no · primitive block")}";
        }
        if (_machineBuilderLibraryDetailPorts is not null)
        {
            _machineBuilderLibraryDetailPorts.Text = item.Ports.Count == 0
                ? "No typed ports · this item is structural/preset metadata."
                : string.Join("\n", item.Ports.Select(port =>
                    $"{(port.Direction == ForeverStudioLibraryPortDirection.Input ? "IN " : "OUT")}  " +
                    $"{port.DataType,-10}  {port.DisplayName}" +
                    $"{(port.Required ? "  · required" : string.Empty)}" +
                    $"{(port.Multiple ? "  · multiple" : string.Empty)}"));
        }
        if (_machineBuilderLibraryDetailTags is not null)
        {
            var capabilities = item.Capabilities.Count == 0 ? "—" : string.Join(" · ", item.Capabilities);
            var tags = item.Tags.Count == 0 ? "—" : string.Join(" · ", item.Tags);
            var recommended = item.RecommendedFor.Count == 0 ? "—" : string.Join(" · ", item.RecommendedFor);
            _machineBuilderLibraryDetailTags.Text =
                $"Capabilities   {capabilities}\n" +
                $"Tags           {tags}\n" +
                $"Recommended    {recommended}";
        }
    }
}

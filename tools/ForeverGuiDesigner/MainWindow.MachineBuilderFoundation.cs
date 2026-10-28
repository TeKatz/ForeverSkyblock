using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine Builder Phase 1. Adds the Create New Machine entry point and a deliberately small
/// foundation workspace around the canonical machine/library contracts. Later phases replace the
/// locked workflow steps with visual assembly, function, GUI and animation authoring surfaces.
/// </summary>
public partial class MainWindow
{
    private sealed class MachineBuilderProjectListItem
    {
        public required ForeverStudioMachineProjectLoadResult LoadResult { get; init; }
        public string Label => LoadResult.Document is { } document
            ? $"{document.DisplayName}  ·  {document.AssetId}"
            : $"INVALID  ·  {Path.GetFileName(LoadResult.RelativePath)}";
        public override string ToString() => Label;
    }

    private bool _machineBuilderFoundationInitialized;
    private FrameworkElement? _studioMachineBuilderWorkspace;
    private TextBox? _machineBuilderDisplayNameBox;
    private TextBox? _machineBuilderAssetIdBox;
    private TextBox? _machineBuilderDescriptionBox;
    private TextBlock? _machineBuilderPathText;
    private TextBlock? _machineBuilderStateText;
    private TextBlock? _machineBuilderLibrarySummaryText;
    private TextBlock? _machineBuilderLibraryWarningsText;
    private ListBox? _machineBuilderValidationList;
    private ListBox? _machineBuilderProjectsList;
    private Button? _machineBuilderSaveButton;
    private Button? _machineBuilderReloadButton;
    private ForeverStudioMachineDocument? _machineBuilderDocument;
    private ForeverStudioLibraryLoadResult? _machineBuilderLibraryLoad;
    private string _machineBuilderSourceHash = ForeverStudioMachineProjectService.MissingHash;
    private string? _machineBuilderPersistedAssetId;
    private bool _machineBuilderUiUpdating;
    private bool _machineBuilderDirty;

    public void EnableForeverMachineBuilderFoundation()
    {
        if (_machineBuilderFoundationInitialized || _studioRoot is null)
            return;
        _machineBuilderFoundationInitialized = true;

        _studioMachineBuilderWorkspace = BuildMachineBuilderWorkspace();
        _studioMachineBuilderWorkspace.Visibility = Visibility.Collapsed;
        _studioRoot.Children.Add(_studioMachineBuilderWorkspace);
        InstallMachineBuilderProjectHomeEntry();
        RefreshMachineBuilderRepositoryContext();
    }

    private void InstallMachineBuilderProjectHomeEntry()
    {
        if (_studioProjectHome is null)
            return;

        var workspaceArea = FindStudioVisualChildren<StackPanel>(_studioProjectHome)
            .FirstOrDefault(panel => panel.Children.OfType<TextBlock>()
                .Any(text => text.Text == "Choose what you want to shape"));
        if (workspaceArea is null)
            return;

        var cards = workspaceArea.Children.OfType<Grid>()
            .FirstOrDefault(grid => grid.Children.OfType<Button>().Count() >= 2);
        if (cards is null || cards.Children.OfType<Button>()
            .Any(button => FindStudioVisualChildren<TextBlock>(button).Any(text => text.Text == "CREATE NEW MACHINE")))
            return;

        cards.ColumnDefinitions.Add(new ColumnDefinition());
        var existingButtons = cards.Children.OfType<Button>().ToList();
        if (existingButtons.Count >= 1)
            existingButtons[0].Margin = new Thickness(0, 0, 10, 0);
        if (existingButtons.Count >= 2)
            existingButtons[1].Margin = new Thickness(10, 0, 10, 0);

        var create = CreatePolishedWorkspaceCard(
            "NEW",
            "CREATE NEW MACHINE",
            "Structure · functions · GUI · animation",
            "Start a versioned machine project and follow one guided workflow from identity to final integration.",
            "#E2B765");
        create.Margin = new Thickness(10, 0, 0, 0);
        create.Click += (_, _) => OpenMachineBuilderWorkspace();
        ApplyStudioContentOnlyButtonTemplate(create);
        Grid.SetColumn(create, 2);
        cards.Children.Add(create);
    }

    private FrameworkElement BuildMachineBuilderWorkspace()
    {
        var root = new DockPanel { Background = Brush("#0D1215") };
        var nav = BuildMachineBuilderNavigation();
        DockPanel.SetDock(nav, Dock.Top);
        root.Children.Add(nav);

        var page = new Grid
        {
            Margin = new Thickness(28, 24, 28, 28),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 470 });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });

        var workflow = BuildMachineBuilderWorkflowPanel();
        Grid.SetColumn(workflow, 0);
        page.Children.Add(workflow);

        var editorScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        editorScroll.Content = BuildMachineBuilderIdentityPanel();
        Grid.SetColumn(editorScroll, 2);
        page.Children.Add(editorScroll);

        var contextScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        contextScroll.Content = BuildMachineBuilderContextPanel();
        Grid.SetColumn(contextScroll, 4);
        page.Children.Add(contextScroll);

        root.Children.Add(page);
        return root;
    }

    private Border BuildMachineBuilderNavigation()
    {
        var border = new Border
        {
            Height = 54,
            Background = Brush("#121A1F"),
            BorderBrush = Brush("#293A42"),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var grid = new Grid { Margin = new Thickness(18, 0, 20, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var back = CreateStudioChromeButton("← Project Home", 132, false);
        back.Height = 36;
        back.VerticalAlignment = VerticalAlignment.Center;
        back.Click += (_, _) => CloseMachineBuilderWorkspace();
        Grid.SetColumn(back, 0);
        grid.Children.Add(back);

        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        };
        title.Children.Add(new TextBlock
        {
            Text = "Forever Studio  /  Machine Builder",
            Foreground = Brush("#E7F0F2"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        title.Children.Add(CreateMachineBuilderPhasePill());
        Grid.SetColumn(title, 1);
        grid.Children.Add(title);

        _machineBuilderStateText = new TextBlock
        {
            Text = "NO PROJECT",
            Foreground = Brush("#81969F"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_machineBuilderStateText, 2);
        grid.Children.Add(_machineBuilderStateText);
        border.Child = grid;
        return border;
    }

    private static Border CreateMachineBuilderPhasePill()
        => new()
        {
            Background = Brush("#302815"),
            BorderBrush = Brush("#9B7A3C"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(9, 3, 9, 3),
            Margin = new Thickness(12, 0, 0, 0),
            Child = new TextBlock
            {
                Text = "PHASE 1 · FOUNDATION",
                Foreground = Brush("#E1BA70"),
                FontSize = 9,
                FontWeight = FontWeights.Bold
            }
        };

    private Border BuildMachineBuilderWorkflowPanel()
    {
        var card = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(18, 20, 18, 20) };
        panel.Children.Add(new TextBlock
        {
            Text = "GUIDED WORKFLOW",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "One machine, one source of truth.",
            Foreground = Brush("#E9F1F3"),
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 18)
        });

        AddMachineBuilderStep(panel, "01", "Identity", "Project contract and stable IDs", active: true);
        AddMachineBuilderStep(panel, "02", "Visual Design", "Shape, assets and textures", active: false);
        AddMachineBuilderStep(panel, "03", "Function", "Visual machine logic", active: false);
        AddMachineBuilderStep(panel, "04", "Recipes & GUI", "Data and interface bindings", active: false);
        AddMachineBuilderStep(panel, "05", "Animation", "Runtime and authored motion", active: false);
        AddMachineBuilderStep(panel, "06", "Review & Build", "Validate and integrate", active: false);

        var note = new Border
        {
            Background = Brush("#10181C"),
            BorderBrush = Brush("#293D45"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11),
            Margin = new Thickness(0, 20, 0, 0),
            Child = new TextBlock
            {
                Text = "Phase 1 writes only the versioned Machine Builder source document. It does not generate Java or game assets yet.",
                Foreground = Brush("#83969E"),
                FontSize = 10.5,
                LineHeight = 16,
                TextWrapping = TextWrapping.Wrap
            }
        };
        panel.Children.Add(note);
        card.Child = panel;
        return card;
    }

    private static void AddMachineBuilderStep(Panel parent, string number, string title, string description, bool active)
    {
        var border = new Border
        {
            Background = Brush(active ? "#173035" : "#11191D"),
            BorderBrush = Brush(active ? "#4B9B9E" : "#27363D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 10, 11, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Opacity = active ? 1 : 0.62
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(new TextBlock
        {
            Text = number,
            Foreground = Brush(active ? "#74D2D0" : "#61727A"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Top
        });
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brush(active ? "#E8F2F3" : "#A2B0B5"),
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold
        });
        copy.Children.Add(new TextBlock
        {
            Text = active ? description : $"{description} · locked",
            Foreground = Brush("#71838B"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        border.Child = grid;
        parent.Children.Add(border);
    }

    private FrameworkElement BuildMachineBuilderIdentityPanel()
    {
        var card = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        var panel = new StackPanel { Margin = new Thickness(28, 25, 28, 28) };

        var heading = new Grid();
        heading.ColumnDefinitions.Add(new ColumnDefinition());
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = "MACHINE PROJECT · FORMAT V1",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Define the machine identity",
            Foreground = Brush("#EFF5F6"),
            FontSize = 27,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 7, 0, 0)
        });
        copy.Children.Add(new TextBlock
        {
            Text = "This document becomes the stable source of truth consumed by every later Machine Builder phase.",
            Foreground = Brush("#8FA1A9"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 0)
        });
        heading.Children.Add(copy);

        var newButton = CreateStudioChromeButton("NEW PROJECT", 116, false);
        newButton.VerticalAlignment = VerticalAlignment.Top;
        newButton.Click += (_, _) => StartNewMachineBuilderProject();
        Grid.SetColumn(newButton, 1);
        heading.Children.Add(newButton);
        panel.Children.Add(heading);

        panel.Children.Add(MachineBuilderLabel("Display name", 28));
        _machineBuilderDisplayNameBox = MachineBuilderTextBox();
        _machineBuilderDisplayNameBox.TextChanged += (_, _) => MachineBuilderIdentityChanged();
        panel.Children.Add(_machineBuilderDisplayNameBox);

        panel.Children.Add(MachineBuilderLabel("Asset ID", 16));
        _machineBuilderAssetIdBox = MachineBuilderTextBox();
        _machineBuilderAssetIdBox.TextChanged += (_, _) => MachineBuilderIdentityChanged();
        panel.Children.Add(_machineBuilderAssetIdBox);
        panel.Children.Add(new TextBlock
        {
            Text = "Stable Minecraft / Machine Builder identity. It is locked after the first save to avoid accidental orphaned project files.",
            Foreground = Brush("#687D86"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 5, 0, 0)
        });

        panel.Children.Add(MachineBuilderLabel("Description", 16));
        _machineBuilderDescriptionBox = MachineBuilderTextBox(multiline: true);
        _machineBuilderDescriptionBox.TextChanged += (_, _) => MachineBuilderIdentityChanged();
        panel.Children.Add(_machineBuilderDescriptionBox);

        var pathCard = new Border
        {
            Background = Brush("#0B1216"),
            BorderBrush = Brush("#293B43"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 18, 0, 0)
        };
        var pathPanel = new StackPanel();
        pathPanel.Children.Add(new TextBlock
        {
            Text = "PROJECT SOURCE",
            Foreground = Brush("#667B84"),
            FontSize = 9,
            FontWeight = FontWeights.Bold
        });
        _machineBuilderPathText = new TextBlock
        {
            Foreground = Brush("#AFC0C6"),
            FontSize = 10.5,
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };
        pathPanel.Children.Add(_machineBuilderPathText);
        pathCard.Child = pathPanel;
        panel.Children.Add(pathCard);

        panel.Children.Add(new TextBlock
        {
            Text = "FOUNDATION VALIDATION",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 23, 0, 8)
        });
        _machineBuilderValidationList = new ListBox
        {
            MinHeight = 116,
            MaxHeight = 220,
            Background = Brush("#0B1216"),
            Foreground = Brush("#B9C7CC"),
            BorderBrush = Brush("#293B43"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(7),
            FontSize = 10.5
        };
        panel.Children.Add(_machineBuilderValidationList);

        var actions = new Grid { Margin = new Thickness(0, 22, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _machineBuilderReloadButton = CreateStudioChromeButton("RELOAD", 92, false);
        _machineBuilderReloadButton.HorizontalAlignment = HorizontalAlignment.Left;
        _machineBuilderReloadButton.Click += (_, _) => ReloadMachineBuilderProject();
        actions.Children.Add(_machineBuilderReloadButton);

        _machineBuilderSaveButton = CreateStudioChromeButton("SAVE PROJECT DEFINITION", 210, true);
        _machineBuilderSaveButton.Click += (_, _) => SaveMachineBuilderProject();
        Grid.SetColumn(_machineBuilderSaveButton, 1);
        actions.Children.Add(_machineBuilderSaveButton);
        panel.Children.Add(actions);

        card.Child = panel;
        return card;
    }

    private FrameworkElement BuildMachineBuilderContextPanel()
    {
        var panel = new StackPanel();

        var libraryCard = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        var library = new StackPanel { Margin = new Thickness(20) };
        library.Children.Add(new TextBlock
        {
            Text = "LIBRARY CONTRACT",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        library.Children.Add(new TextBlock
        {
            Text = "Reusable building blocks",
            Foreground = Brush("#EDF4F5"),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        });
        library.Children.Add(new TextBlock
        {
            Text = "Phase 1 establishes versioned assets, functions, animations and templates. Phase 2 fills this catalog with the large production library.",
            Foreground = Brush("#82959D"),
            FontSize = 10.5,
            LineHeight = 16,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 14)
        });
        _machineBuilderLibrarySummaryText = new TextBlock
        {
            Foreground = Brush("#B9C8CD"),
            FontSize = 11,
            LineHeight = 19,
            TextWrapping = TextWrapping.Wrap
        };
        library.Children.Add(_machineBuilderLibrarySummaryText);
        _machineBuilderLibraryWarningsText = new TextBlock
        {
            Foreground = Brush("#D8B26A"),
            FontSize = 9.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        library.Children.Add(_machineBuilderLibraryWarningsText);
        libraryCard.Child = library;
        panel.Children.Add(libraryCard);

        var projectsCard = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        projectsCard.Margin = new Thickness(0, 18, 0, 0);
        var projects = new StackPanel { Margin = new Thickness(20) };
        projects.Children.Add(new TextBlock
        {
            Text = "MACHINE PROJECTS",
            Foreground = Brush("#72D5D3"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        });
        projects.Children.Add(new TextBlock
        {
            Text = "Existing authoring definitions",
            Foreground = Brush("#EDF4F5"),
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 12)
        });
        _machineBuilderProjectsList = new ListBox
        {
            MinHeight = 150,
            MaxHeight = 280,
            Background = Brush("#0B1216"),
            Foreground = Brush("#B8C6CB"),
            BorderBrush = Brush("#293B43"),
            BorderThickness = new Thickness(1),
            FontSize = 10.5,
            Padding = new Thickness(5)
        };
        _machineBuilderProjectsList.MouseDoubleClick += (_, _) => OpenSelectedMachineBuilderProject();
        projects.Children.Add(_machineBuilderProjectsList);

        var open = CreateStudioChromeButton("OPEN SELECTED", 132, false);
        open.Margin = new Thickness(0, 12, 0, 0);
        open.HorizontalAlignment = HorizontalAlignment.Left;
        open.Click += (_, _) => OpenSelectedMachineBuilderProject();
        projects.Children.Add(open);
        projectsCard.Child = projects;
        panel.Children.Add(projectsCard);
        return panel;
    }

    private static TextBlock MachineBuilderLabel(string text, double topMargin)
        => new()
        {
            Text = text,
            Foreground = Brush("#C9D5D9"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(1, topMargin, 0, 7)
        };

    private static TextBox MachineBuilderTextBox(bool multiline = false)
        => new()
        {
            Height = multiline ? 92 : 38,
            MinHeight = multiline ? 92 : 38,
            Padding = new Thickness(10, multiline ? 8 : 0, 10, multiline ? 8 : 0),
            Background = Brush("#0B1216"),
            Foreground = Brush("#EDF4F5"),
            CaretBrush = Brush("#77D4D2"),
            BorderBrush = Brush("#354A53"),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled
        };

    private void OpenMachineBuilderWorkspace()
    {
        RefreshMachineBuilderRepositoryContext();
        if (_machineBuilderDocument is null)
            StartNewMachineBuilderProject(confirmDirty: false);

        if (_studioLauncher is not null)
            _studioLauncher.Visibility = Visibility.Collapsed;
        if (_studioProjectHome is not null)
            _studioProjectHome.Visibility = Visibility.Collapsed;
        if (_studioGuiWorkspace is not null)
            _studioGuiWorkspace.Visibility = Visibility.Collapsed;
        if (_studioBlockWorkspace is not null)
            _studioBlockWorkspace.Visibility = Visibility.Collapsed;
        if (_studioMachineBuilderWorkspace is not null)
            _studioMachineBuilderWorkspace.Visibility = Visibility.Visible;
        Title = "Forever Studio — Machine Builder";
    }

    private void CloseMachineBuilderWorkspace()
    {
        if (_studioMachineBuilderWorkspace is not null)
            _studioMachineBuilderWorkspace.Visibility = Visibility.Collapsed;
        ShowStudioWorkspace(StudioWorkspace.ProjectHome);
    }

    private void RefreshMachineBuilderRepositoryContext()
    {
        _machineBuilderLibraryLoad = ForeverStudioLibraryCatalog.Load(_repoRoot);
        var counts = _machineBuilderLibraryLoad.Catalog.LatestItemCounts();
        if (_machineBuilderLibrarySummaryText is not null)
        {
            _machineBuilderLibrarySummaryText.Text =
                $"Contract v{ForeverStudioLibraryCatalog.CurrentContractVersion}\n" +
                $"Assets       {counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Asset)}\n" +
                $"Functions    {counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Function)}\n" +
                $"Animations   {counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Animation)}\n" +
                $"Templates    {counts.GetValueOrDefault(ForeverStudioLibraryItemKind.Template)}";
        }
        if (_machineBuilderLibraryWarningsText is not null)
        {
            _machineBuilderLibraryWarningsText.Text = _machineBuilderLibraryLoad.Warnings.Count == 0
                ? "Library contract loaded without warnings."
                : string.Join("\n", _machineBuilderLibraryLoad.Warnings.Take(4));
            _machineBuilderLibraryWarningsText.Foreground = Brush(
                _machineBuilderLibraryLoad.Warnings.Count == 0 ? "#74C99A" : "#D8B26A");
        }

        if (_machineBuilderProjectsList is not null)
        {
            _machineBuilderProjectsList.ItemsSource = null;
            if (!string.IsNullOrWhiteSpace(_repoRoot))
            {
                var scan = ForeverStudioMachineProjectService.Scan(_repoRoot, _machineBuilderLibraryLoad.Catalog);
                _machineBuilderProjectsList.ItemsSource = scan.Projects
                    .Select(project => new MachineBuilderProjectListItem { LoadResult = project })
                    .ToList();
                if (scan.Warnings.Count > 0 && _machineBuilderLibraryWarningsText is not null)
                {
                    var existing = _machineBuilderLibraryWarningsText.Text;
                    _machineBuilderLibraryWarningsText.Text = string.Join(
                        "\n",
                        new[] { existing }.Where(value => !string.IsNullOrWhiteSpace(value)).Concat(scan.Warnings.Take(3)));
                }
            }
        }

        RefreshMachineBuilderValidation();
    }

    private void StartNewMachineBuilderProject(bool confirmDirty = true)
    {
        if (confirmDirty && !ConfirmDiscardMachineBuilderChanges())
            return;

        _machineBuilderDocument = ForeverStudioMachineProjectService.CreateBlank("new_machine", "New Machine");
        _machineBuilderSourceHash = ForeverStudioMachineProjectService.MissingHash;
        _machineBuilderPersistedAssetId = null;
        _machineBuilderDirty = true;
        PopulateMachineBuilderIdentityFields();
        RefreshMachineBuilderValidation();
    }

    private bool ConfirmDiscardMachineBuilderChanges()
    {
        if (!_machineBuilderDirty)
            return true;
        return MessageBox.Show(
                   this,
                   "The current Machine Builder project contains unsaved changes. Discard them?",
                   "Forever Studio — Unsaved machine project",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void PopulateMachineBuilderIdentityFields()
    {
        if (_machineBuilderDocument is null)
            return;
        _machineBuilderUiUpdating = true;
        try
        {
            if (_machineBuilderDisplayNameBox is not null)
                _machineBuilderDisplayNameBox.Text = _machineBuilderDocument.DisplayName;
            if (_machineBuilderAssetIdBox is not null)
            {
                _machineBuilderAssetIdBox.Text = _machineBuilderDocument.AssetId;
                _machineBuilderAssetIdBox.IsReadOnly = _machineBuilderPersistedAssetId is not null;
                _machineBuilderAssetIdBox.Opacity = _machineBuilderAssetIdBox.IsReadOnly ? 0.72 : 1;
            }
            if (_machineBuilderDescriptionBox is not null)
                _machineBuilderDescriptionBox.Text = _machineBuilderDocument.Description;
        }
        finally
        {
            _machineBuilderUiUpdating = false;
        }
    }

    private void MachineBuilderIdentityChanged()
    {
        if (_machineBuilderUiUpdating || _machineBuilderDocument is null)
            return;

        _machineBuilderDocument.DisplayName = _machineBuilderDisplayNameBox?.Text ?? string.Empty;
        if (_machineBuilderPersistedAssetId is null)
            _machineBuilderDocument.AssetId = (_machineBuilderAssetIdBox?.Text ?? string.Empty).Trim().ToLowerInvariant();
        _machineBuilderDocument.Description = _machineBuilderDescriptionBox?.Text ?? string.Empty;
        _machineBuilderDirty = true;
        RefreshMachineBuilderValidation();
    }

    private void RefreshMachineBuilderValidation()
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null)
        {
            if (_machineBuilderStateText is not null)
            {
                _machineBuilderStateText.Text = "NO PROJECT";
                _machineBuilderStateText.Foreground = Brush("#81969F");
            }
            if (_machineBuilderSaveButton is not null)
                _machineBuilderSaveButton.IsEnabled = false;
            return;
        }

        var issues = ForeverStudioMachineProjectService.Validate(
            _machineBuilderDocument,
            _machineBuilderLibraryLoad.Catalog);
        var hasErrors = issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);
        var hasWarnings = issues.Any(issue => issue.Severity == StudioValidationSeverity.Warning);

        if (_machineBuilderValidationList is not null)
            _machineBuilderValidationList.ItemsSource = issues.Select(issue => issue.ToString()).ToList();
        if (_machineBuilderPathText is not null)
            _machineBuilderPathText.Text = ForeverStudioMachineProjectService.RelativePathForAsset(
                string.IsNullOrWhiteSpace(_machineBuilderDocument.AssetId) ? "new_machine" : _machineBuilderDocument.AssetId);
        if (_machineBuilderStateText is not null)
        {
            _machineBuilderStateText.Text = hasErrors
                ? "VALIDATION ERROR"
                : _machineBuilderDirty
                    ? "UNSAVED"
                    : hasWarnings ? "SAVED · WARNINGS" : "SAVED";
            _machineBuilderStateText.Foreground = Brush(hasErrors
                ? "#E17B82"
                : _machineBuilderDirty || hasWarnings ? "#D8B26A" : "#74C99A");
        }
        if (_machineBuilderSaveButton is not null)
            _machineBuilderSaveButton.IsEnabled = !hasErrors && !string.IsNullOrWhiteSpace(_repoRoot);
        if (_machineBuilderReloadButton is not null)
            _machineBuilderReloadButton.IsEnabled = _machineBuilderPersistedAssetId is not null;
    }

    private void SaveMachineBuilderProject()
    {
        if (_machineBuilderDocument is null || _machineBuilderLibraryLoad is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        MachineBuilderIdentityChanged();
        var issues = ForeverStudioMachineProjectService.Validate(_machineBuilderDocument, _machineBuilderLibraryLoad.Catalog);
        var errors = issues.Where(issue => issue.Severity == StudioValidationSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                string.Join("\n", errors.Take(8)),
                "Machine project validation failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var relative = ForeverStudioMachineProjectService.RelativePathForAsset(_machineBuilderDocument.AssetId);
        var review =
            $"Machine Builder source definition\n\n" +
            $"Asset: {_machineBuilderDocument.DisplayName} ({_machineBuilderDocument.AssetId})\n" +
            $"Format: v{_machineBuilderDocument.FormatVersion}\n" +
            $"Components: {_machineBuilderDocument.Components.Count}\n" +
            $"States: {_machineBuilderDocument.States.Count}\n" +
            $"Behavior nodes: {_machineBuilderDocument.Behavior.Nodes.Count}\n" +
            $"Library references: {_machineBuilderDocument.Library.References.Count}\n\n" +
            $"Write:\n{relative}\n\n" +
            "Phase 1 saves only the authoring definition; no Java, model, texture or runtime file is generated yet.\n\nSave this project definition?";
        if (MessageBox.Show(
                this,
                review,
                "Review Machine Project",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var result = ForeverStudioMachineProjectService.Save(
                _repoRoot,
                _machineBuilderDocument,
                _machineBuilderLibraryLoad.Catalog,
                _machineBuilderSourceHash);
            _machineBuilderSourceHash = result.SourceHash;
            _machineBuilderPersistedAssetId = _machineBuilderDocument.AssetId;
            _machineBuilderDirty = false;
            PopulateMachineBuilderIdentityFields();
            RefreshMachineBuilderRepositoryContext();
            StatusText.Text = $"Machine Builder saved '{_machineBuilderDocument.AssetId}' · {result.RelativePath}";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Machine Builder save failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenSelectedMachineBuilderProject()
    {
        if (_machineBuilderProjectsList?.SelectedItem is not MachineBuilderProjectListItem item)
            return;
        if (!ConfirmDiscardMachineBuilderChanges())
            return;
        if (item.LoadResult.Document is null)
        {
            MessageBox.Show(
                this,
                string.Join("\n", item.LoadResult.Issues.Take(8)),
                "Machine project cannot be opened",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _machineBuilderDocument = item.LoadResult.Document;
        _machineBuilderSourceHash = item.LoadResult.SourceHash;
        _machineBuilderPersistedAssetId = item.LoadResult.Document.AssetId;
        _machineBuilderDirty = false;
        PopulateMachineBuilderIdentityFields();
        RefreshMachineBuilderValidation();
    }

    private void ReloadMachineBuilderProject()
    {
        if (_machineBuilderPersistedAssetId is null || _machineBuilderLibraryLoad is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;
        if (!ConfirmDiscardMachineBuilderChanges())
            return;

        var load = ForeverStudioMachineProjectService.Load(
            _repoRoot,
            _machineBuilderPersistedAssetId,
            _machineBuilderLibraryLoad.Catalog);
        if (load.Document is null)
        {
            MessageBox.Show(
                this,
                string.Join("\n", load.Issues.Take(8)),
                "Machine project reload failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _machineBuilderDocument = load.Document;
        _machineBuilderSourceHash = load.SourceHash;
        _machineBuilderPersistedAssetId = load.Document.AssetId;
        _machineBuilderDirty = false;
        PopulateMachineBuilderIdentityFields();
        RefreshMachineBuilderValidation();
    }
}

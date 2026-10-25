using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ForeverGuiDesigner.Services;
using Microsoft.Win32;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 10 foundation shell. The existing GUI editor stays alive as one unchanged
/// workspace while Forever Studio owns project selection and top-level navigation.
/// </summary>
public partial class MainWindow
{
    private enum StudioWorkspace
    {
        Launcher,
        ProjectHome,
        GuiEditor,
        BlockEditor
    }

    private bool _studioFoundationInitialized;
    private Grid? _studioRoot;
    private FrameworkElement? _studioLauncher;
    private FrameworkElement? _studioProjectHome;
    private FrameworkElement? _studioGuiWorkspace;
    private FrameworkElement? _studioBlockWorkspace;
    private TextBox? _studioRepoBox;
    private TextBlock? _studioProjectNameText;
    private TextBlock? _studioProjectPathText;
    private TextBlock? _studioMachineCountText;
    private TextBlock? _studioBlockModelCountText;
    private TextBlock? _studioItemModelCountText;
    private TextBlock? _studioBlockstateCountText;
    private TextBlock? _studioBlockTextureCountText;
    private TextBlock? _studioItemTextureCountText;
    private TextBlock? _studioWarningText;
    private ComboBox? _studioBlockMachineCombo;
    private ForeverStudioProjectIndex? _studioProjectIndex;
    private StudioWorkspace _studioWorkspace = StudioWorkspace.Launcher;

    public void EnableForeverStudioFoundation()
    {
        if (_studioFoundationInitialized)
            return;
        _studioFoundationInitialized = true;

        Title = "Forever Studio";

        if (Content is not UIElement existingEditor)
            return;

        Content = null;
        _studioRoot = new Grid
        {
            Background = Brush("#101417")
        };

        _studioLauncher = BuildStudioLauncher();
        _studioProjectHome = BuildStudioProjectHome();
        _studioGuiWorkspace = BuildStudioGuiWorkspace(existingEditor);
        _studioBlockWorkspace = BuildStudioBlockWorkspace();

        _studioRoot.Children.Add(_studioLauncher);
        _studioRoot.Children.Add(_studioProjectHome);
        _studioRoot.Children.Add(_studioGuiWorkspace);
        _studioRoot.Children.Add(_studioBlockWorkspace);
        Content = _studioRoot;

        if (_studioRepoBox is not null)
            _studioRepoBox.Text = _repoRoot ?? string.Empty;

        ShowStudioWorkspace(StudioWorkspace.Launcher);
    }

    private FrameworkElement BuildStudioLauncher()
    {
        var root = StudioPageRoot();
        var panel = new StackPanel
        {
            Width = 760,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(StudioTitle("FOREVER STUDIO", 38));
        panel.Children.Add(new TextBlock
        {
            Text = "Forever Skyblock Development Tools",
            Foreground = Brush("#93A4AD"),
            FontSize = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 44)
        });

        var card = StudioCard();
        var cardPanel = new StackPanel { Margin = new Thickness(28) };
        cardPanel.Children.Add(StudioSectionLabel("PROJECT"));
        cardPanel.Children.Add(new TextBlock
        {
            Text = "Select the ForeverSkyblock repository, then scan it before opening an editor.",
            Foreground = Brush("#AEBBC1"),
            FontSize = 13,
            Margin = new Thickness(0, 8, 0, 22)
        });
        cardPanel.Children.Add(new TextBlock
        {
            Text = "Repository",
            Foreground = Brush("#DDE6EA"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 7)
        });

        var repoRow = new Grid();
        repoRow.ColumnDefinitions.Add(new ColumnDefinition());
        repoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _studioRepoBox = new TextBox
        {
            Height = 36,
            Padding = new Thickness(10, 7, 10, 7),
            Background = Brush("#0B1115"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#3B4B54"),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_studioRepoBox, 0);
        repoRow.Children.Add(_studioRepoBox);

        var browse = StudioButton("Browse…", 100);
        browse.Margin = new Thickness(10, 0, 0, 0);
        browse.Click += StudioBrowseRepository_Click;
        Grid.SetColumn(browse, 1);
        repoRow.Children.Add(browse);
        cardPanel.Children.Add(repoRow);

        var scan = StudioPrimaryButton("SCAN PROJECT", 180);
        scan.HorizontalAlignment = HorizontalAlignment.Center;
        scan.Margin = new Thickness(0, 28, 0, 4);
        scan.Click += StudioScanProject_Click;
        cardPanel.Children.Add(scan);

        cardPanel.Children.Add(new TextBlock
        {
            Text = "The selected path is kept in the launcher while the app is open. Auto-detected repositories are prefilled.",
            Foreground = Brush("#70818A"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 18, 0, 0)
        });

        card.Child = cardPanel;
        panel.Children.Add(card);
        root.Children.Add(panel);
        return root;
    }

    private FrameworkElement BuildStudioProjectHome()
    {
        var root = StudioPageRoot();
        var dock = new DockPanel { Margin = new Thickness(46, 34, 46, 36) };

        var header = new Grid { Margin = new Thickness(0, 0, 0, 30) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(StudioTitle("FOREVER STUDIO", 27, HorizontalAlignment.Left));
        _studioProjectNameText = new TextBlock
        {
            Foreground = Brush("#9FB0B8"),
            FontSize = 14,
            Margin = new Thickness(1, 5, 0, 0)
        };
        heading.Children.Add(_studioProjectNameText);
        Grid.SetColumn(heading, 0);
        header.Children.Add(heading);

        var changeRepo = StudioButton("Change repository", 150);
        changeRepo.Click += (_, _) => ShowStudioWorkspace(StudioWorkspace.Launcher);
        Grid.SetColumn(changeRepo, 1);
        header.Children.Add(changeRepo);
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var editorArea = new StackPanel { Margin = new Thickness(0, 0, 28, 0) };
        editorArea.Children.Add(StudioSectionLabel("WHAT DO YOU WANT TO EDIT?"));
        editorArea.Children.Add(new TextBlock
        {
            Text = "Choose a workspace. The GUI Editor is the existing editor; Block Editor is the new foundation for model, item, texture and blockstate tools.",
            Foreground = Brush("#92A2AA"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 22)
        });

        var cards = new Grid();
        cards.ColumnDefinitions.Add(new ColumnDefinition());
        cards.ColumnDefinitions.Add(new ColumnDefinition());
        var guiCard = StudioEditorCard(
            "GUI EDITOR",
            "Screens · slots · states · animations · source workflow",
            "Open the established Forever GUI editing workspace.");
        guiCard.Margin = new Thickness(0, 0, 10, 0);
        guiCard.Click += (_, _) => ShowStudioWorkspace(StudioWorkspace.GuiEditor);
        Grid.SetColumn(guiCard, 0);
        cards.Children.Add(guiCard);

        var blockCard = StudioEditorCard(
            "BLOCK EDITOR",
            "Block models · items · textures · blockstates",
            "Open the new asset workspace foundation.");
        blockCard.Margin = new Thickness(10, 0, 0, 0);
        blockCard.Click += (_, _) => ShowStudioWorkspace(StudioWorkspace.BlockEditor);
        Grid.SetColumn(blockCard, 1);
        cards.Children.Add(blockCard);
        editorArea.Children.Add(cards);

        Grid.SetColumn(editorArea, 0);
        content.Children.Add(editorArea);

        var scanCard = StudioCard();
        var scanPanel = new StackPanel { Margin = new Thickness(22) };
        scanPanel.Children.Add(StudioSectionLabel("PROJECT SCAN"));
        _studioProjectPathText = StudioInfoText();
        _studioProjectPathText.TextWrapping = TextWrapping.Wrap;
        _studioProjectPathText.Margin = new Thickness(0, 9, 0, 18);
        scanPanel.Children.Add(_studioProjectPathText);

        _studioMachineCountText = StudioScanRow(scanPanel, "Machine screens");
        _studioBlockModelCountText = StudioScanRow(scanPanel, "Block models");
        _studioItemModelCountText = StudioScanRow(scanPanel, "Item models");
        _studioBlockstateCountText = StudioScanRow(scanPanel, "Blockstates");
        _studioBlockTextureCountText = StudioScanRow(scanPanel, "Block textures");
        _studioItemTextureCountText = StudioScanRow(scanPanel, "Item textures");
        _studioWarningText = new TextBlock
        {
            Foreground = Brush("#D8B26A"),
            FontSize = 12,
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        scanPanel.Children.Add(_studioWarningText);

        var rescan = StudioButton("Rescan project", 130);
        rescan.Margin = new Thickness(0, 22, 0, 0);
        rescan.HorizontalAlignment = HorizontalAlignment.Left;
        rescan.Click += StudioScanProject_Click;
        scanPanel.Children.Add(rescan);
        scanCard.Child = scanPanel;
        Grid.SetColumn(scanCard, 1);
        content.Children.Add(scanCard);

        dock.Children.Add(content);
        root.Children.Add(dock);
        return root;
    }

    private FrameworkElement BuildStudioGuiWorkspace(UIElement existingEditor)
    {
        var root = new DockPanel { Background = Brush("#111315") };
        var nav = BuildWorkspaceNavigation("GUI Editor", StudioWorkspace.ProjectHome);
        DockPanel.SetDock(nav, Dock.Top);
        root.Children.Add(nav);
        root.Children.Add(existingEditor);
        return root;
    }

    private FrameworkElement BuildStudioBlockWorkspace()
    {
        var root = new DockPanel { Background = Brush("#101417") };
        var nav = BuildWorkspaceNavigation("Block Editor", StudioWorkspace.ProjectHome);
        DockPanel.SetDock(nav, Dock.Top);
        root.Children.Add(nav);

        var page = new Grid { Margin = new Thickness(36, 28, 36, 36) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition());

        var machineRow = new Grid { Margin = new Thickness(0, 0, 0, 24) };
        machineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        machineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        machineRow.ColumnDefinitions.Add(new ColumnDefinition());
        var machineLabel = new TextBlock
        {
            Text = "Machine",
            Foreground = Brush("#DCE7EC"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        machineRow.Children.Add(machineLabel);
        _studioBlockMachineCombo = new ComboBox
        {
            Height = 34,
            DisplayMemberPath = nameof(ForeverMachineTemplate.DisplayName),
            Background = Brush("#182229"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#43545D")
        };
        Grid.SetColumn(_studioBlockMachineCombo, 1);
        machineRow.Children.Add(_studioBlockMachineCombo);
        Grid.SetRow(machineRow, 0);
        page.Children.Add(machineRow);

        var tabs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 14)
        };
        var blockTab = StudioPrimaryButton("BLOCK", 105);
        blockTab.IsEnabled = false;
        tabs.Children.Add(blockTab);
        tabs.Children.Add(StudioPlannedTab("ITEM"));
        tabs.Children.Add(StudioPlannedTab("TEXTURES"));
        tabs.Children.Add(StudioPlannedTab("BLOCKSTATES"));
        Grid.SetRow(tabs, 1);
        page.Children.Add(tabs);

        var placeholder = StudioCard();
        var placeholderPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 680,
            Margin = new Thickness(40)
        };
        placeholderPanel.Children.Add(StudioTitle("BLOCK EDITOR FOUNDATION", 26));
        placeholderPanel.Children.Add(new TextBlock
        {
            Text = "The Studio workspace and project index are active. The next phase adds the read-only Minecraft block model viewer before editing is enabled.",
            Foreground = Brush("#9DADB5"),
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 14, 0, 18)
        });
        placeholderPanel.Children.Add(new TextBlock
        {
            Text = "Next: model JSON · elements · faces · UVs · textures · states · facing",
            Foreground = Brush("#6FCED0"),
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        placeholder.Child = placeholderPanel;
        Grid.SetRow(placeholder, 2);
        page.Children.Add(placeholder);

        root.Children.Add(page);
        return root;
    }

    private Border BuildWorkspaceNavigation(string workspaceName, StudioWorkspace backTarget)
    {
        var border = new Border
        {
            Height = 48,
            Background = Brush("#151C21"),
            BorderBrush = Brush("#2B3940"),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var grid = new Grid { Margin = new Thickness(16, 0, 18, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var home = StudioButton("← Project Home", 125);
        home.Height = 30;
        home.Click += (_, _) => ShowStudioWorkspace(backTarget);
        Grid.SetColumn(home, 0);
        grid.Children.Add(home);

        var breadcrumb = new TextBlock
        {
            Text = $"Forever Studio  /  {workspaceName}",
            Foreground = Brush("#DCE7EC"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        };
        Grid.SetColumn(breadcrumb, 1);
        grid.Children.Add(breadcrumb);

        var project = new TextBlock
        {
            Text = _studioProjectIndex?.ProjectName ?? (_repoRoot is null ? string.Empty : Path.GetFileName(_repoRoot)),
            Foreground = Brush("#81939C"),
            VerticalAlignment = VerticalAlignment.Center
        };
        project.Tag = "studio_project_breadcrumb";
        Grid.SetColumn(project, 2);
        grid.Children.Add(project);
        border.Child = grid;
        return border;
    }

    private void StudioBrowseRepository_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose the ForeverSkyblock repository",
            InitialDirectory = _repoRoot ?? Environment.CurrentDirectory
        };
        if (dialog.ShowDialog(this) != true)
            return;

        if (TrySetRepository(dialog.FolderName, showError: true) && _studioRepoBox is not null)
            _studioRepoBox.Text = _repoRoot ?? dialog.FolderName;
    }

    private void StudioScanProject_Click(object sender, RoutedEventArgs e)
    {
        var requestedRoot = _studioRepoBox?.Text?.Trim();
        if (!TrySetRepository(requestedRoot, showError: true) || string.IsNullOrWhiteSpace(_repoRoot))
            return;

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

            if (_studioBlockMachineCombo is not null)
            {
                _studioBlockMachineCombo.ItemsSource = null;
                _studioBlockMachineCombo.ItemsSource = _foreverTemplates;
                if (_foreverTemplates.Count > 0)
                    _studioBlockMachineCombo.SelectedIndex = 0;
            }

            _studioProjectIndex = ForeverStudioProjectIndexer.Build(_repoRoot, result);
            RefreshStudioProjectHome();
            StatusText.Text = $"Forever Studio project scan: {_studioProjectIndex.MachineScreenCount} machine screen(s), {_studioProjectIndex.TotalModelCount} model(s), {_studioProjectIndex.TotalTextureCount} texture(s).";
            ShowStudioWorkspace(StudioWorkspace.ProjectHome);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Forever Studio project scan failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStudioProjectHome()
    {
        if (_studioProjectIndex is null)
            return;

        if (_studioProjectNameText is not null)
            _studioProjectNameText.Text = $"Project: {_studioProjectIndex.ProjectName}";
        if (_studioProjectPathText is not null)
            _studioProjectPathText.Text = _studioProjectIndex.RepositoryRoot;
        SetStudioCount(_studioMachineCountText, _studioProjectIndex.MachineScreenCount);
        SetStudioCount(_studioBlockModelCountText, _studioProjectIndex.BlockModelCount);
        SetStudioCount(_studioItemModelCountText, _studioProjectIndex.ItemModelCount);
        SetStudioCount(_studioBlockstateCountText, _studioProjectIndex.BlockstateCount);
        SetStudioCount(_studioBlockTextureCountText, _studioProjectIndex.BlockTextureCount);
        SetStudioCount(_studioItemTextureCountText, _studioProjectIndex.ItemTextureCount);

        if (_studioWarningText is not null)
        {
            _studioWarningText.Text = _studioProjectIndex.WarningCount == 0
                ? "Scan completed without scanner warnings."
                : $"{_studioProjectIndex.WarningCount} scanner warning(s) — GUI editing remains available.";
            _studioWarningText.Foreground = _studioProjectIndex.WarningCount == 0
                ? Brush("#74C99A")
                : Brush("#D8B26A");
        }

        if (_studioRoot is not null)
        {
            foreach (var text in FindStudioVisualChildren<TextBlock>(_studioRoot)
                         .Where(text => Equals(text.Tag, "studio_project_breadcrumb")))
            {
                text.Text = _studioProjectIndex.ProjectName;
            }
        }
    }

    private void ShowStudioWorkspace(StudioWorkspace workspace)
    {
        _studioWorkspace = workspace;
        if (_studioLauncher is not null)
            _studioLauncher.Visibility = workspace == StudioWorkspace.Launcher ? Visibility.Visible : Visibility.Collapsed;
        if (_studioProjectHome is not null)
            _studioProjectHome.Visibility = workspace == StudioWorkspace.ProjectHome ? Visibility.Visible : Visibility.Collapsed;
        if (_studioGuiWorkspace is not null)
            _studioGuiWorkspace.Visibility = workspace == StudioWorkspace.GuiEditor ? Visibility.Visible : Visibility.Collapsed;
        if (_studioBlockWorkspace is not null)
            _studioBlockWorkspace.Visibility = workspace == StudioWorkspace.BlockEditor ? Visibility.Visible : Visibility.Collapsed;

        Title = workspace switch
        {
            StudioWorkspace.GuiEditor => "Forever Studio — GUI Editor",
            StudioWorkspace.BlockEditor => "Forever Studio — Block Editor",
            StudioWorkspace.ProjectHome => $"Forever Studio — {_studioProjectIndex?.ProjectName ?? "Project"}",
            _ => "Forever Studio"
        };
    }

    private static Grid StudioPageRoot()
        => new()
        {
            Background = Brush("#101417")
        };

    private static Border StudioCard()
        => new()
        {
            Background = Brush("#151D22"),
            BorderBrush = Brush("#314149"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

    private static TextBlock StudioTitle(string text, double size, HorizontalAlignment alignment = HorizontalAlignment.Center)
        => new()
        {
            Text = text,
            Foreground = Brush("#E5EFF2"),
            FontSize = size,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = alignment
        };

    private static TextBlock StudioSectionLabel(string text)
        => new()
        {
            Text = text,
            Foreground = Brush("#70CED1"),
            FontSize = 11,
            FontWeight = FontWeights.Bold
        };

    private static TextBlock StudioInfoText()
        => new()
        {
            Foreground = Brush("#91A2AA"),
            FontSize = 12
        };

    private static Button StudioButton(string text, double width)
        => new()
        {
            Content = text,
            Width = width,
            Height = 36,
            Padding = new Thickness(10, 4, 10, 4),
            Background = Brush("#243139"),
            Foreground = Brush("#E2EAED"),
            BorderBrush = Brush("#465760"),
            BorderThickness = new Thickness(1)
        };

    private static Button StudioPrimaryButton(string text, double width)
        => new()
        {
            Content = text,
            Width = width,
            Height = 38,
            Padding = new Thickness(12, 5, 12, 5),
            Background = Brush("#286F78"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#58B9C0"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.Bold
        };

    private static Button StudioPlannedTab(string text)
    {
        var button = StudioButton(text, text == "BLOCKSTATES" ? 125 : 105);
        button.Margin = new Thickness(8, 0, 0, 0);
        button.IsEnabled = false;
        return button;
    }

    private static Button StudioEditorCard(string title, string subtitle, string description)
    {
        var panel = new StackPanel { Margin = new Thickness(26) };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontSize = 21,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Brush("#71CDD0"),
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 14),
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = Brush("#A4B2B8"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "OPEN  →",
            Foreground = Brush("#E5EFF2"),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 28, 0, 0)
        });

        return new Button
        {
            Content = panel,
            MinHeight = 230,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Background = Brush("#151D22"),
            BorderBrush = Brush("#33464F"),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private static TextBlock StudioScanRow(Panel parent, string label)
    {
        var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var left = new TextBlock
        {
            Text = label,
            Foreground = Brush("#A7B4BA"),
            FontSize = 12
        };
        var value = new TextBlock
        {
            Text = "—",
            Foreground = Brush("#E5EFF2"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(value, 1);
        grid.Children.Add(left);
        grid.Children.Add(value);
        parent.Children.Add(grid);
        return value;
    }

    private static void SetStudioCount(TextBlock? target, int count)
    {
        if (target is not null)
            target.Text = $"✓  {count}";
    }

    private static IEnumerable<T> FindStudioVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;
            foreach (var descendant in FindStudioVisualChildren<T>(child))
                yield return descendant;
        }
    }
}

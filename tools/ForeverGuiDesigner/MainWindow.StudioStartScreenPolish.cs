using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ForeverGuiDesigner;

/// <summary>
/// Visual-only presentation pass for the Forever Studio launcher and project home.
/// Keeps the existing navigation/scanning behavior while giving the entry screens
/// a stronger identity, richer depth and restrained motion.
/// </summary>
public partial class MainWindow
{
    private bool _studioStartScreenPolishInitialized;

    public void EnableForeverStudioStartScreenPolish()
    {
        if (_studioStartScreenPolishInitialized)
            return;
        _studioStartScreenPolishInitialized = true;

        if (_studioRoot is null)
            return;

        if (_studioLauncher is not null)
            _studioRoot.Children.Remove(_studioLauncher);
        if (_studioProjectHome is not null)
            _studioRoot.Children.Remove(_studioProjectHome);

        _studioLauncher = BuildPolishedStudioLauncher();
        _studioProjectHome = BuildPolishedStudioProjectHome();

        _studioRoot.Children.Insert(0, _studioLauncher);
        _studioRoot.Children.Insert(1, _studioProjectHome);

        if (_studioRepoBox is not null)
            _studioRepoBox.Text = _repoRoot ?? string.Empty;
        if (_studioProjectIndex is not null)
            RefreshStudioProjectHome();

        ShowStudioWorkspace(_studioWorkspace);
    }

    private FrameworkElement BuildPolishedStudioLauncher()
    {
        var root = CreateStudioAmbientPage();
        var shell = new Grid
        {
            MaxWidth = 1180,
            Margin = new Thickness(56, 42, 56, 48),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.08, GridUnitType.Star) });
        shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.92, GridUnitType.Star) });

        var hero = new StackPanel
        {
            Margin = new Thickness(14, 10, 74, 10),
            VerticalAlignment = VerticalAlignment.Center
        };
        hero.Children.Add(CreateStudioBrandMark(74));
        hero.Children.Add(new TextBlock
        {
            Text = "FOREVER SKYBLOCK TOOLCHAIN",
            Foreground = Brush("#72D8D6"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(2, 26, 0, 10)
        });
        hero.Children.Add(new TextBlock
        {
            Text = "FOREVER\nSTUDIO",
            Foreground = Brush("#F1F7F8"),
            FontSize = 54,
            FontWeight = FontWeights.Bold,
            LineHeight = 55
        });

        var accentLine = new Border
        {
            Width = 86,
            Height = 3,
            Margin = new Thickness(2, 19, 0, 19),
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(2),
            Background = new LinearGradientBrush(
                ColorFrom("#6ED7D4"),
                ColorFrom("#2E7F87"),
                0)
        };
        hero.Children.Add(accentLine);
        hero.Children.Add(new TextBlock
        {
            Text = "Build the Skyblock experience in one place — from interface and assets to motion and runtime presentation.",
            Foreground = Brush("#A3B3BA"),
            FontSize = 15,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 500
        });

        var chips = new WrapPanel { Margin = new Thickness(0, 27, 0, 0) };
        chips.Children.Add(CreateStudioChip("GUI AUTHORING"));
        chips.Children.Add(CreateStudioChip("BLOCK ASSETS"));
        chips.Children.Add(CreateStudioChip("ANIMATION"));
        hero.Children.Add(chips);

        hero.Children.Add(new TextBlock
        {
            Text = "DESIGN  •  PREVIEW  •  REFINE",
            Foreground = Brush("#5F747D"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(2, 34, 0, 0)
        });

        Grid.SetColumn(hero, 0);
        shell.Children.Add(hero);

        var projectCard = CreateStudioGlassCard(new CornerRadius(12), strongShadow: true);
        projectCard.Margin = new Thickness(0, 8, 8, 8);
        var cardPanel = new StackPanel { Margin = new Thickness(34, 32, 34, 30) };

        var cardHeader = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        cardHeader.ColumnDefinitions.Add(new ColumnDefinition());
        cardHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var cardHeading = new StackPanel();
        cardHeading.Children.Add(new TextBlock
        {
            Text = "PROJECT WORKSPACE",
            Foreground = Brush("#73D4D3"),
            FontSize = 11,
            FontWeight = FontWeights.Bold
        });
        cardHeading.Children.Add(new TextBlock
        {
            Text = "Open your Forever workspace",
            Foreground = Brush("#EEF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 0)
        });
        cardHeader.Children.Add(cardHeading);
        var readyPill = CreateStudioStatusPill("READY", "#76D39B", "#173426");
        readyPill.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(readyPill, 1);
        cardHeader.Children.Add(readyPill);
        cardPanel.Children.Add(cardHeader);

        cardPanel.Children.Add(new TextBlock
        {
            Text = "Choose the ForeverSkyblock repository. Studio scans the project before opening its workspaces.",
            Foreground = Brush("#9DAEB5"),
            FontSize = 13,
            LineHeight = 20,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 25)
        });
        cardPanel.Children.Add(new TextBlock
        {
            Text = "Repository",
            Foreground = Brush("#DDE7EA"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(1, 0, 0, 8)
        });

        var repoRow = new Grid();
        repoRow.ColumnDefinitions.Add(new ColumnDefinition());
        repoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var repoFrame = new Border
        {
            Height = 42,
            CornerRadius = new CornerRadius(6),
            Background = Brush("#0A1013"),
            BorderBrush = Brush("#354A53"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2)
        };
        _studioRepoBox = new TextBox
        {
            Padding = new Thickness(10, 7, 10, 7),
            Background = Brushes.Transparent,
            Foreground = Brush("#EDF4F6"),
            BorderThickness = new Thickness(0),
            CaretBrush = Brush("#78D7D5"),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _studioRepoBox.GotKeyboardFocus += (_, _) => repoFrame.BorderBrush = Brush("#62BEC1");
        _studioRepoBox.LostKeyboardFocus += (_, _) => repoFrame.BorderBrush = Brush("#354A53");
        repoFrame.Child = _studioRepoBox;
        Grid.SetColumn(repoFrame, 0);
        repoRow.Children.Add(repoFrame);

        var browse = CreateStudioChromeButton("Browse…", 106, false);
        browse.Margin = new Thickness(10, 0, 0, 0);
        browse.Click += StudioBrowseRepository_Click;
        Grid.SetColumn(browse, 1);
        repoRow.Children.Add(browse);
        cardPanel.Children.Add(repoRow);

        var scan = CreateStudioChromeButton("SCAN PROJECT   →", double.NaN, true);
        scan.HorizontalAlignment = HorizontalAlignment.Stretch;
        scan.Margin = new Thickness(0, 24, 0, 0);
        scan.Click += StudioScanProject_Click;
        cardPanel.Children.Add(scan);

        var divider = new Border
        {
            Height = 1,
            Background = Brush("#27373E"),
            Margin = new Thickness(0, 26, 0, 18)
        };
        cardPanel.Children.Add(divider);

        var flow = new Grid();
        flow.ColumnDefinitions.Add(new ColumnDefinition());
        flow.ColumnDefinitions.Add(new ColumnDefinition());
        flow.ColumnDefinitions.Add(new ColumnDefinition());
        AddStudioMiniStep(flow, 0, "01", "SELECT");
        AddStudioMiniStep(flow, 1, "02", "SCAN");
        AddStudioMiniStep(flow, 2, "03", "CREATE");
        cardPanel.Children.Add(flow);

        cardPanel.Children.Add(new TextBlock
        {
            Text = "Auto-detected repositories are prefilled and kept for this Studio session.",
            Foreground = Brush("#687C85"),
            FontSize = 10.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 20, 0, 0)
        });

        projectCard.Child = cardPanel;
        Grid.SetColumn(projectCard, 1);
        shell.Children.Add(projectCard);

        AnimateStudioEntrance(hero, 40, 16);
        AnimateStudioEntrance(projectCard, 125, 20);
        AnimateStudioAccent(accentLine, 220);

        root.Children.Add(shell);
        return root;
    }

    private FrameworkElement BuildPolishedStudioProjectHome()
    {
        var root = CreateStudioAmbientPage();
        var shell = new Grid
        {
            MaxWidth = 1560,
            Margin = new Thickness(44, 34, 44, 40),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
        shell.RowDefinitions.Add(new RowDefinition());

        var header = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        var headerGrid = new Grid { Margin = new Thickness(24, 18, 20, 18) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var mark = CreateStudioBrandMark(48);
        mark.Margin = new Thickness(0, 0, 16, 0);
        Grid.SetColumn(mark, 0);
        headerGrid.Children.Add(mark);

        var heading = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        heading.Children.Add(new TextBlock
        {
            Text = "FOREVER STUDIO",
            Foreground = Brush("#EDF5F6"),
            FontSize = 24,
            FontWeight = FontWeights.Bold
        });
        _studioProjectNameText = new TextBlock
        {
            Text = "Project: —",
            Foreground = Brush("#8EA2AA"),
            FontSize = 12.5,
            Margin = new Thickness(1, 4, 0, 0)
        };
        heading.Children.Add(_studioProjectNameText);
        Grid.SetColumn(heading, 1);
        headerGrid.Children.Add(heading);

        var changeRepo = CreateStudioChromeButton("Change repository", 154, false);
        changeRepo.VerticalAlignment = VerticalAlignment.Center;
        changeRepo.Click += (_, _) => ShowStudioWorkspace(StudioWorkspace.Launcher);
        Grid.SetColumn(changeRepo, 2);
        headerGrid.Children.Add(changeRepo);
        header.Child = headerGrid;
        Grid.SetRow(header, 0);
        shell.Children.Add(header);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });

        var workspaceArea = new StackPanel { Margin = new Thickness(0, 0, 26, 0) };
        workspaceArea.Children.Add(new TextBlock
        {
            Text = "WORKSPACES",
            Foreground = Brush("#70D3D2"),
            FontSize = 11,
            FontWeight = FontWeights.Bold
        });
        workspaceArea.Children.Add(new TextBlock
        {
            Text = "Choose what you want to shape",
            Foreground = Brush("#EDF5F6"),
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 7, 0, 0)
        });
        workspaceArea.Children.Add(new TextBlock
        {
            Text = "Each workspace shares the same scanned project, assets and Forever Studio context.",
            Foreground = Brush("#899CA4"),
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 22)
        });

        var cards = new Grid();
        cards.ColumnDefinitions.Add(new ColumnDefinition());
        cards.ColumnDefinitions.Add(new ColumnDefinition());

        var guiCard = CreatePolishedWorkspaceCard(
            "GUI",
            "GUI EDITOR",
            "Screens · slots · states · animations",
            "Compose and review the in-game interfaces, source-driven machine presentation and animation workflow.",
            "#6ED7D4");
        guiCard.Margin = new Thickness(0, 0, 10, 0);
        guiCard.Click += (_, _) => ShowStudioWorkspace(StudioWorkspace.GuiEditor);
        Grid.SetColumn(guiCard, 0);
        cards.Children.Add(guiCard);

        var blockCard = CreatePolishedWorkspaceCard(
            "3D",
            "BLOCK EDITOR",
            "Models · items · textures · blockstates",
            "Shape the asset side of Forever Skyblock with model, texture, item and state-aware tooling.",
            "#8CAFE8");
        blockCard.Margin = new Thickness(10, 0, 0, 0);
        blockCard.Click += (_, _) => ShowStudioWorkspace(StudioWorkspace.BlockEditor);
        Grid.SetColumn(blockCard, 1);
        cards.Children.Add(blockCard);
        workspaceArea.Children.Add(cards);
        workspaceArea.Children.Add(CreateStudioWorkflowStrip());

        Grid.SetColumn(workspaceArea, 0);
        content.Children.Add(workspaceArea);

        var scanCard = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        scanCard.Margin = new Thickness(2, 0, 0, 0);
        var scanPanel = new StackPanel { Margin = new Thickness(24, 23, 24, 23) };

        var scanHeader = new Grid();
        scanHeader.ColumnDefinitions.Add(new ColumnDefinition());
        scanHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        scanHeader.Children.Add(new TextBlock
        {
            Text = "PROJECT SCAN",
            Foreground = Brush("#72D5D3"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var indexed = CreateStudioStatusPill("INDEX", "#82D5A2", "#173427");
        Grid.SetColumn(indexed, 1);
        scanHeader.Children.Add(indexed);
        scanPanel.Children.Add(scanHeader);

        scanPanel.Children.Add(new TextBlock
        {
            Text = "Repository",
            Foreground = Brush("#687D86"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 19, 0, 6)
        });
        var pathFrame = new Border
        {
            Background = Brush("#0A1013"),
            BorderBrush = Brush("#283940"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10, 9, 10, 9)
        };
        _studioProjectPathText = new TextBlock
        {
            Foreground = Brush("#A8B7BD"),
            FontSize = 10.5,
            TextWrapping = TextWrapping.Wrap
        };
        pathFrame.Child = _studioProjectPathText;
        scanPanel.Children.Add(pathFrame);

        var scanDivider = new Border
        {
            Height = 1,
            Background = Brush("#26373E"),
            Margin = new Thickness(0, 18, 0, 12)
        };
        scanPanel.Children.Add(scanDivider);

        _studioMachineCountText = CreatePolishedScanRow(scanPanel, "Machine screens");
        _studioBlockModelCountText = CreatePolishedScanRow(scanPanel, "Block models");
        _studioItemModelCountText = CreatePolishedScanRow(scanPanel, "Item models");
        _studioBlockstateCountText = CreatePolishedScanRow(scanPanel, "Blockstates");
        _studioBlockTextureCountText = CreatePolishedScanRow(scanPanel, "Block textures");
        _studioItemTextureCountText = CreatePolishedScanRow(scanPanel, "Item textures");

        _studioWarningText = new TextBlock
        {
            Foreground = Brush("#D8B26A"),
            FontSize = 11,
            LineHeight = 17,
            Margin = new Thickness(0, 17, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        scanPanel.Children.Add(_studioWarningText);

        var rescan = CreateStudioChromeButton("Rescan project", double.NaN, false);
        rescan.HorizontalAlignment = HorizontalAlignment.Stretch;
        rescan.Margin = new Thickness(0, 18, 0, 0);
        rescan.Click += StudioScanProject_Click;
        scanPanel.Children.Add(rescan);

        scanCard.Child = scanPanel;
        Grid.SetColumn(scanCard, 1);
        content.Children.Add(scanCard);

        Grid.SetRow(content, 2);
        shell.Children.Add(content);

        AnimateStudioEntrance(header, 25, 12);
        AnimateStudioEntrance(workspaceArea, 80, 16);
        AnimateStudioEntrance(scanCard, 145, 18);

        root.Children.Add(shell);
        return root;
    }

    private static Grid CreateStudioAmbientPage()
    {
        var root = new Grid
        {
            ClipToBounds = true,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.08, 0),
                EndPoint = new Point(0.92, 1),
                GradientStops = new GradientStopCollection
                {
                    new(ColorFrom("#0B0F12"), 0),
                    new(ColorFrom("#0E1519"), 0.48),
                    new(ColorFrom("#111A1E"), 1)
                }
            }
        };

        var atmosphere = new Canvas { IsHitTestVisible = false };
        var glowA = CreateStudioGlow(580, "#4D2A9DA2", "#002A9DA2");
        Canvas.SetRight(glowA, -170);
        Canvas.SetTop(glowA, -180);
        atmosphere.Children.Add(glowA);

        var glowB = CreateStudioGlow(430, "#35234D70", "#00234D70");
        Canvas.SetLeft(glowB, -150);
        Canvas.SetBottom(glowB, -145);
        atmosphere.Children.Add(glowB);

        var square = new Border
        {
            Width = 230,
            Height = 230,
            BorderBrush = Brush("#173B575F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new RotateTransform(13),
                    new TranslateTransform()
                }
            }
        };
        Canvas.SetRight(square, 110);
        Canvas.SetBottom(square, 50);
        atmosphere.Children.Add(square);
        AnimateStudioAmbientElement(square, 8.5, 0.16, 0.29, 12);

        var line = new Rectangle
        {
            Width = 300,
            Height = 1,
            Fill = Brush("#245B858D"),
            Opacity = 0.22,
            RenderTransform = new TranslateTransform()
        };
        Canvas.SetLeft(line, 90);
        Canvas.SetTop(line, 130);
        atmosphere.Children.Add(line);
        AnimateStudioAmbientElement(line, 10.5, 0.12, 0.25, -10);

        root.Children.Add(atmosphere);
        return root;
    }

    private static Ellipse CreateStudioGlow(double size, string center, string edge)
        => new()
        {
            Width = size,
            Height = size,
            IsHitTestVisible = false,
            Fill = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops = new GradientStopCollection
                {
                    new(ColorFrom(center), 0),
                    new(ColorFrom(edge), 1)
                }
            }
        };

    private static Border CreateStudioBrandMark(double size)
    {
        var surface = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.24),
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new(ColorFrom("#22373D"), 0),
                    new(ColorFrom("#142127"), 1)
                }
            },
            BorderBrush = Brush("#3B6970"),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 24,
                ShadowDepth = 8,
                Opacity = 0.32
            }
        };

        try
        {
            var image = new Image
            {
                Source = LoadPackBitmap(ApplicationIconResource),
                Width = size * 0.62,
                Height = size * 0.62,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            surface.Child = image;
        }
        catch
        {
            surface.Child = new TextBlock
            {
                Text = "FS",
                Foreground = Brush("#EAF5F5"),
                FontSize = size * 0.27,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return surface;
    }

    private static Border CreateStudioGlassCard(CornerRadius radius, bool strongShadow)
        => new()
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new(ColorFrom("#182228"), 0),
                    new(ColorFrom("#131B20"), 1)
                }
            },
            BorderBrush = Brush("#31474F"),
            BorderThickness = new Thickness(1),
            CornerRadius = radius,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = strongShadow ? 32 : 22,
                ShadowDepth = strongShadow ? 11 : 7,
                Opacity = strongShadow ? 0.36 : 0.25
            }
        };

    private static Border CreateStudioChip(string text)
        => new()
        {
            Background = Brush("#152329"),
            BorderBrush = Brush("#2F4B52"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(11, 6, 11, 6),
            Margin = new Thickness(0, 0, 8, 8),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush("#91AAB2"),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold
            }
        };

    private static Border CreateStudioStatusPill(string text, string foreground, string background)
        => new()
        {
            Background = Brush(background),
            BorderBrush = Brush(foreground),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(9, 4, 9, 4),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush(foreground),
                FontSize = 9,
                FontWeight = FontWeights.Bold
            }
        };

    private static Button CreateStudioChromeButton(string text, double width, bool primary)
    {
        var background = primary ? "#28747C" : "#202D34";
        var hover = primary ? "#32858D" : "#2A3A42";
        var border = primary ? "#55BDC1" : "#41555E";

        var surface = new Border
        {
            Background = Brush(background),
            BorderBrush = Brush(border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(13, 8, 13, 8),
            Child = new TextBlock
            {
                Text = text,
                Foreground = primary ? Brushes.White : Brush("#E3ECEF"),
                FontSize = 11,
                FontWeight = primary ? FontWeights.Bold : FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var button = new Button
        {
            Content = surface,
            Width = width,
            Height = 42,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            RenderTransform = new TranslateTransform()
        };

        void Enter()
        {
            surface.Background = Brush(hover);
            surface.BorderBrush = Brush(primary ? "#72D7D7" : "#5B717A");
            AnimateStudioTranslate((TranslateTransform)button.RenderTransform, -1.5, 130);
        }

        void Leave()
        {
            surface.Background = Brush(background);
            surface.BorderBrush = Brush(border);
            AnimateStudioTranslate((TranslateTransform)button.RenderTransform, 0, 150);
        }

        button.MouseEnter += (_, _) => Enter();
        button.MouseLeave += (_, _) => Leave();
        button.GotKeyboardFocus += (_, _) => Enter();
        button.LostKeyboardFocus += (_, _) => Leave();
        button.PreviewMouseLeftButtonDown += (_, _) => AnimateStudioTranslate((TranslateTransform)button.RenderTransform, 0.5, 70);
        button.PreviewMouseLeftButtonUp += (_, _) => AnimateStudioTranslate((TranslateTransform)button.RenderTransform, -1.5, 90);
        return button;
    }

    private static Button CreatePolishedWorkspaceCard(
        string badge,
        string title,
        string subtitle,
        string description,
        string accent)
    {
        var surface = CreateStudioGlassCard(new CornerRadius(10), strongShadow: false);
        surface.MinHeight = 282;
        surface.RenderTransform = new TranslateTransform();

        var panel = new Grid { Margin = new Thickness(25, 24, 25, 22) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition());
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var badgeBorder = new Border
        {
            Width = 50,
            Height = 50,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brush("#17272D"),
            BorderBrush = Brush(accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new TextBlock
            {
                Text = badge,
                Foreground = Brush(accent),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetRow(badgeBorder, 0);
        panel.Children.Add(badgeBorder);

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = Brush("#F0F6F7"),
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 18, 0, 0)
        };
        Grid.SetRow(titleText, 1);
        panel.Children.Add(titleText);

        var subtitleText = new TextBlock
        {
            Text = subtitle,
            Foreground = Brush(accent),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 7, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(subtitleText, 2);
        panel.Children.Add(subtitleText);

        var descriptionText = new TextBlock
        {
            Text = description,
            Foreground = Brush("#96A8AF"),
            FontSize = 12.5,
            LineHeight = 19,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 16, 0, 12)
        };
        Grid.SetRow(descriptionText, 3);
        panel.Children.Add(descriptionText);

        var footer = new Grid { Margin = new Thickness(0, 11, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.Children.Add(new TextBlock
        {
            Text = "OPEN WORKSPACE",
            Foreground = Brush("#DDE8EB"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var arrow = new TextBlock
        {
            Text = "→",
            Foreground = Brush(accent),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arrow, 1);
        footer.Children.Add(arrow);
        Grid.SetRow(footer, 4);
        panel.Children.Add(footer);

        surface.Child = panel;
        var button = new Button
        {
            Content = surface,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null
        };

        var shadow = surface.Effect as DropShadowEffect;
        void Enter()
        {
            surface.BorderBrush = Brush(accent);
            if (shadow is not null)
            {
                AnimateStudioShadow(shadow, 30, 0.38, 170);
            }
            AnimateStudioTranslate((TranslateTransform)surface.RenderTransform, -4, 170);
        }

        void Leave()
        {
            surface.BorderBrush = Brush("#31474F");
            if (shadow is not null)
            {
                AnimateStudioShadow(shadow, 22, 0.25, 190);
            }
            AnimateStudioTranslate((TranslateTransform)surface.RenderTransform, 0, 190);
        }

        button.MouseEnter += (_, _) => Enter();
        button.MouseLeave += (_, _) => Leave();
        button.GotKeyboardFocus += (_, _) => Enter();
        button.LostKeyboardFocus += (_, _) => Leave();
        button.PreviewMouseLeftButtonDown += (_, _) => AnimateStudioTranslate((TranslateTransform)surface.RenderTransform, -1.5, 80);
        button.PreviewMouseLeftButtonUp += (_, _) => AnimateStudioTranslate((TranslateTransform)surface.RenderTransform, -4, 90);
        return button;
    }

    private static Border CreateStudioWorkflowStrip()
    {
        var border = new Border
        {
            Margin = new Thickness(0, 22, 0, 0),
            Background = Brush("#10181C"),
            BorderBrush = Brush("#263A42"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 15, 20, 15)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        AddStudioFlowColumn(grid, 0, "01", "SELECT", "Choose the surface you want to work on.");
        AddStudioFlowColumn(grid, 1, "02", "SHAPE", "Edit with the shared scanned project context.");
        AddStudioFlowColumn(grid, 2, "03", "REVIEW", "Preview and validate before applying changes.");
        border.Child = grid;
        return border;
    }

    private static void AddStudioFlowColumn(Grid grid, int column, string number, string title, string body)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(column == 0 ? 0 : 16, 0, column == 2 ? 0 : 16, 0)
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"{number}  {title}",
            Foreground = Brush("#6ECED0"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = body,
            Foreground = Brush("#778A92"),
            FontSize = 10.5,
            LineHeight = 15,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private static TextBlock CreatePolishedScanRow(Panel parent, string label)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dot = new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = Brush("#4B7880"),
            Margin = new Thickness(0, 0, 9, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(dot);

        var left = new TextBlock
        {
            Text = label,
            Foreground = Brush("#96A8AF"),
            FontSize = 11.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(left, 1);
        grid.Children.Add(left);

        var value = new TextBlock
        {
            Text = "—",
            Foreground = Brush("#E6F0F2"),
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(value, 2);
        grid.Children.Add(value);
        parent.Children.Add(grid);
        return value;
    }

    private static void AddStudioMiniStep(Grid grid, int column, string number, string label)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(column == 0 ? 0 : 10, 0, column == 2 ? 0 : 10, 0)
        };
        panel.Children.Add(new TextBlock
        {
            Text = number,
            Foreground = Brush("#4D6C75"),
            FontSize = 9,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brush("#A2B2B8"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private static void AnimateStudioEntrance(FrameworkElement element, int delayMs, double fromY)
    {
        var translate = new TranslateTransform(0, fromY);
        element.RenderTransform = translate;
        element.Opacity = 0;
        element.Loaded += (_, _) =>
        {
            if (!SystemParameters.ClientAreaAnimation)
            {
                element.Opacity = 1;
                translate.Y = 0;
                return;
            }

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(430))
            {
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            });
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(fromY, 0, TimeSpan.FromMilliseconds(520))
            {
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            });
            element.Opacity = 1;
            translate.Y = 0;
        };
    }

    private static void AnimateStudioAccent(FrameworkElement element, int delayMs)
    {
        if (!SystemParameters.ClientAreaAnimation)
            return;

        var scale = new ScaleTransform(0.35, 1);
        element.RenderTransformOrigin = new Point(0, 0.5);
        element.RenderTransform = scale;
        element.Loaded += (_, _) =>
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(520))
            {
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            });
            scale.ScaleX = 1;
        };
    }

    private static void AnimateStudioAmbientElement(FrameworkElement element, double seconds, double fromOpacity, double toOpacity, double yOffset)
    {
        element.Opacity = fromOpacity;
        if (!SystemParameters.ClientAreaAnimation)
            return;

        element.Loaded += (_, _) =>
        {
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(fromOpacity, toOpacity, TimeSpan.FromSeconds(seconds * 0.62))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            TranslateTransform? translate = element.RenderTransform as TranslateTransform;
            if (element.RenderTransform is TransformGroup group)
                translate = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (translate is not null)
            {
                translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-yOffset * 0.35, yOffset, TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
            }
        };
    }

    private static void AnimateStudioTranslate(TranslateTransform translate, double target, int milliseconds)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            translate.Y = target;
            return;
        }

        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private static void AnimateStudioShadow(DropShadowEffect shadow, double blur, double opacity, int milliseconds)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            shadow.BlurRadius = blur;
            shadow.Opacity = opacity;
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(blur, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = easing
        });
        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = easing
        });
    }

    private static Color ColorFrom(string value)
        => (Color)ColorConverter.ConvertFromString(value);
}

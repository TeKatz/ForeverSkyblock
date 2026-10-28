using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 16 Review / Validation / Apply. This is the only Studio path that persists the
/// Phase 11-15 in-memory model, blockstate, item and texture edits to project assets.
/// </summary>
public partial class MainWindow
{
    private bool _studioReviewApplyInitialized;

    public void EnableForeverReviewValidationApply()
    {
        if (_studioReviewApplyInitialized)
            return;
        _studioReviewApplyInitialized = true;

        if (_studioBlockViewerStatusText?.Parent is Panel blockInspector)
            AddStudioReviewLauncher(blockInspector, "Review staged block/model/texture/blockstate changes before writing project files.");
        if (_studioItemStatusText?.Parent is Panel itemInspector)
            AddStudioReviewLauncher(itemInspector, "Review staged item transforms and item textures before writing project files.");
    }

    private void AddStudioReviewLauncher(Panel inspector, string description)
    {
        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("REVIEW · VALIDATE · APPLY · PHASE 16"));
        inspector.Children.Add(new TextBlock
        {
            Text = description + " Apply is disabled when validation errors exist and aborts if a reviewed file changes externally before the write.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 12)
        });

        var review = StudioButton("Review changes", 140);
        review.HorizontalAlignment = HorizontalAlignment.Left;
        review.Click += (_, _) => OpenStudioReviewDialog();
        inspector.Children.Add(review);

        inspector.Children.Add(new TextBlock
        {
            Text = "Nothing is written until Review → Apply is confirmed.",
            Foreground = Brush("#71848D"),
            FontSize = 10,
            Margin = new Thickness(0, 7, 0, 0)
        });
    }

    private void OpenStudioReviewDialog()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
        {
            MessageBox.Show(this, "Open a valid Forever Skyblock repository first.", "Forever Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        List<StudioReviewChange> changes;
        try
        {
            changes = CollectStudioCompletedReviewChanges();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"Could not build the review set:\n\n{ex.Message}", "Forever Studio · Review", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (changes.Count == 0)
        {
            MessageBox.Show(this, "There are no staged Studio changes to review.", "Forever Studio · Review", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var errors = changes.Sum(change => change.Issues.Count(issue => issue.Severity == StudioValidationSeverity.Error));
        var warnings = changes.Sum(change => change.Issues.Count(issue => issue.Severity == StudioValidationSeverity.Warning));

        var dialog = new Window
        {
            Owner = this,
            Title = "Forever Studio · Review / Validate / Apply",
            Width = 1220,
            Height = 780,
            MinWidth = 940,
            MinHeight = 620,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#0B1013"),
            Foreground = Brushes.White
        };

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        dialog.Content = root;

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        header.Children.Add(new TextBlock
        {
            Text = "REVIEW CHANGES",
            Foreground = Brush("#7ED9DB"),
            FontSize = 13,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = $"{changes.Count} file(s) staged · {errors} error(s) · {warnings} warning(s)",
            Foreground = Brush(errors > 0 ? "#E18B8B" : warnings > 0 ? "#D8B26A" : "#74C99A"),
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 0)
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(355) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        var changeList = new ListBox
        {
            ItemsSource = changes,
            Background = Brush("#0E161A"),
            Foreground = Brush("#DCE7EC"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };
        Grid.SetColumn(changeList, 0);
        content.Children.Add(changeList);

        var detail = new Grid();
        detail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        detail.RowDefinitions.Add(new RowDefinition());
        Grid.SetColumn(detail, 2);
        content.Children.Add(detail);

        var detailTitle = new TextBlock
        {
            Foreground = Brush("#DCE7EC"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(detailTitle, 0);
        detail.Children.Add(detailTitle);

        var tabs = new TabControl
        {
            Background = Brush("#0E161A"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#35464F")
        };
        var beforeBox = StudioReviewTextBox();
        var afterBox = StudioReviewTextBox();
        var validationBox = StudioReviewTextBox();
        tabs.Items.Add(StudioReviewTab("BEFORE", beforeBox));
        tabs.Items.Add(StudioReviewTab("AFTER", afterBox));
        tabs.Items.Add(StudioReviewTab("VALIDATION", validationBox));
        Grid.SetRow(tabs, 1);
        detail.Children.Add(tabs);

        void RefreshSelection()
        {
            if (changeList.SelectedItem is not StudioReviewChange selected)
                return;
            detailTitle.Text = $"{selected.Kind} · {selected.RelativePath}\n{selected.Summary}";
            beforeBox.Text = selected.BeforeText;
            afterBox.Text = selected.AfterText;
            validationBox.Text = selected.Issues.Count == 0
                ? "READY · No validation issues."
                : string.Join(Environment.NewLine, selected.Issues.Select(issue => issue.ToString()));
            validationBox.Foreground = Brush(selected.HasErrors ? "#E18B8B" : selected.WarningCount > 0 ? "#D8B26A" : "#74C99A");
        }

        changeList.SelectionChanged += (_, _) => RefreshSelection();
        changeList.SelectedIndex = 0;
        RefreshSelection();

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        var safetyText = new TextBlock
        {
            Text = errors == 0
                ? "Apply stages every payload, re-checks reviewed source hashes, replaces existing files atomically, verifies the written hashes, and rolls back the batch on failure."
                : "Apply is blocked until all validation errors are resolved.",
            Foreground = Brush(errors == 0 ? "#71848D" : "#E18B8B"),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(safetyText, 0);
        footer.Children.Add(safetyText);

        var apply = StudioButton(errors == 0 ? $"Apply {changes.Count} file(s)" : "Apply blocked", 150);
        apply.IsEnabled = errors == 0;
        apply.Margin = new Thickness(0, 0, 8, 0);
        apply.Click += (_, _) =>
        {
            var confirmation = MessageBox.Show(
                dialog,
                $"Apply {changes.Count} validated Studio change(s) to the repository?\n\nThe reviewed files will be written now. Temporary backups are used for rollback if the operation fails.",
                "Forever Studio · Apply",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                ForeverStudioBlockBuilderCompletionService.ApplyChangesAtomically(changes);
                AcceptStudioAppliedChanges(changes);
                MessageBox.Show(
                    dialog,
                    $"Applied {changes.Count} file(s) successfully. The Studio source state has been reloaded from disk.",
                    "Forever Studio · Apply",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                dialog.DialogResult = true;
                dialog.Close();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show(
                    dialog,
                    $"Apply was not completed.\n\n{ex.Message}\n\nIf a write failed after starting, Forever Studio attempted to restore the reviewed files from its temporary backups.",
                    "Forever Studio · Apply failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };
        Grid.SetColumn(apply, 1);
        footer.Children.Add(apply);

        var close = StudioButton("Close", 90);
        close.Click += (_, _) => dialog.Close();
        Grid.SetColumn(close, 2);
        footer.Children.Add(close);

        dialog.ShowDialog();
    }

    private List<StudioReviewChange> CollectStudioReviewChanges()
    {
        var changes = new List<StudioReviewChange>();
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return changes;

        if (_studioBlockModelModified && _studioSourceBlockModel is not null && _studioEditableBlockModel is not null)
        {
            changes.Add(ForeverStudioReviewApplyService.BuildBlockModelChange(
                _repoRoot,
                _studioSourceBlockModel,
                _studioEditableBlockModel));
        }

        var repoPrefix = Path.GetFullPath(_repoRoot) + "|";
        foreach (var document in _studioBlockstateDocuments.Values
                     .Where(document => document.Modified && document.RepoKey.StartsWith(repoPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var asset = _studioBlockAssets.FirstOrDefault(candidate =>
                candidate.Id.Equals(document.AssetId, StringComparison.OrdinalIgnoreCase));
            if (asset is null)
                continue;
            changes.Add(ForeverStudioReviewApplyService.BuildBlockstateChange(
                _repoRoot,
                asset.BlockstatePath,
                document.SourceVariants,
                document.EditableVariants));
        }

        var textureChanges = new Dictionary<string, StudioReviewChange>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in _studioTextureModifiedPaths)
        {
            if (!_studioTextureOverrides.TryGetValue(path, out var bitmap))
                continue;
            AddStudioTextureReviewChange(textureChanges, path, bitmap, "block texture editor");
        }
        foreach (var (path, bitmap) in _studioItemTextureOverrides)
            AddStudioTextureReviewChange(textureChanges, path, bitmap, "item texture editor");
        changes.AddRange(textureChanges.Values);

        if (_studioItemModelModified && _studioLoadedItem is not null)
        {
            changes.Add(ForeverStudioReviewApplyService.BuildItemModelChange(
                _repoRoot,
                _studioLoadedItem,
                _studioWorkingItemTransforms));
        }

        return changes
            .OrderBy(change => change.Kind)
            .ThenBy(change => change.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void AddStudioTextureReviewChange(
        IDictionary<string, StudioReviewChange> changes,
        string path,
        BitmapSource bitmap,
        string editorName)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;
        var fullPath = Path.GetFullPath(path);
        var candidate = ForeverStudioReviewApplyService.BuildTextureChange(_repoRoot, fullPath, bitmap);
        if (!changes.TryGetValue(fullPath, out var existing))
        {
            changes[fullPath] = candidate;
            return;
        }

        var payloadMatches = existing.BinaryPayload is not null && candidate.BinaryPayload is not null &&
                             existing.BinaryPayload.AsSpan().SequenceEqual(candidate.BinaryPayload);
        if (payloadMatches)
            return;
        existing.Issues.Add(new StudioValidationIssue(
            StudioValidationSeverity.Error,
            $"The same PNG has different staged edits in more than one editor ({editorName}). Reset one version before Apply."));
    }

    private void AcceptStudioAppliedChanges(IReadOnlyList<StudioReviewChange> changes)
    {
        AcceptStudioBlockBuilderAppliedChanges(changes);
        var appliedPaths = changes.Select(change => Path.GetFullPath(change.FullPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in _studioTextureModifiedPaths.ToList())
        {
            if (!appliedPaths.Contains(Path.GetFullPath(path)))
                continue;
            _studioTextureModifiedPaths.Remove(path);
            _studioTextureOverrides.Remove(path);
        }
        foreach (var path in _studioItemTextureOverrides.Keys.ToList())
        {
            if (appliedPaths.Contains(Path.GetFullPath(path)))
                _studioItemTextureOverrides.Remove(path);
        }

        _studioBlockstateDocuments.Clear();
        _studioBlockstateDocument = null;
        _studioBlockstateSelectedVariant = null;
        _studioBlockModelModified = false;
        _studioItemModelModified = false;

        // Re-scan after Apply rather than trusting the pre-write typed snapshots. This makes the
        // files on disk the source of truth again and also exercises the roundtrip immediately.
        RefreshStudioBlockCatalog(force: true);
        AugmentStudioBlockCatalogWithReadOnlyAssets();
        LoadSelectedStudioBlockVariant();
        LoadStudioBlockstateDocument();
        UpdateStudioBlockstateAuthoringSafety();
        RefreshStudioFaceEditor();
        UpdateStudioModelEditState();

        if (_studioItemStudioInitialized)
        {
            RefreshStudioItemCatalog(force: true);
            QueueEnhancedItemRefresh();
            UpdateStudioItemEditState();
        }
    }

    private static TextBox StudioReviewTextBox()
        => new()
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brush("#091014"),
            Foreground = Brush("#C9D5DA"),
            BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Padding = new Thickness(10)
        };

    private static TabItem StudioReviewTab(string header, UIElement content)
        => new()
        {
            Header = header,
            Content = content,
            Foreground = Brushes.White,
            Background = Brush("#152027")
        };
}

using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Final Block Builder authoring glue: keeps multiple edited block models staged at once,
/// exposes unsupported blockstate formats as read-only previews, and builds the complete
/// reviewed change set consumed by the Phase 16 apply pipeline.
/// </summary>
public partial class MainWindow
{
    private sealed class StudioBlockModelDocument
    {
        public required string RepoKey { get; init; }
        public required MinecraftResolvedBlockModel Source { get; init; }
        public required MinecraftResolvedBlockModel Editable { get; set; }
        public bool Modified { get; set; }
    }

    private bool _studioBlockBuilderCompletionInitialized;
    private readonly Dictionary<string, StudioBlockModelDocument> _studioBlockModelDocuments =
        new(StringComparer.OrdinalIgnoreCase);
    private StudioBlockModelDocument? _studioBlockModelDocument;
    private TextBlock? _studioBlockstateAuthoringSafetyText;

    public void EnableForeverBlockBuilderCompletion()
    {
        if (_studioBlockBuilderCompletionInitialized)
            return;
        _studioBlockBuilderCompletionInitialized = true;

        if (_studioBlockMachineCombo is not null)
        {
            _studioBlockMachineCombo.DropDownOpened += (_, _) => SyncStudioBlockModelDocument();
            _studioBlockMachineCombo.PreviewKeyDown += StudioBlockSelectionInput_PreviewKeyDown;
            _studioBlockMachineCombo.PreviewMouseDown += (_, _) => SyncStudioBlockModelDocument();
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioBlockBuilderCompletionRefresh();
        }
        if (_studioBlockStateCombo is not null)
        {
            _studioBlockStateCombo.DropDownOpened += (_, _) => SyncStudioBlockModelDocument();
            _studioBlockStateCombo.PreviewKeyDown += StudioBlockSelectionInput_PreviewKeyDown;
        }
        if (_studioBlockFacingCombo is not null)
        {
            _studioBlockFacingCombo.DropDownOpened += (_, _) => SyncStudioBlockModelDocument();
            _studioBlockFacingCombo.PreviewKeyDown += StudioBlockSelectionInput_PreviewKeyDown;
        }
        if (_studioBlockWorkspace is FrameworkElement workspace)
            workspace.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    QueueStudioBlockBuilderCompletionRefresh();
            };

        EnsureStudioBlockstateSafetyBanner();
        AugmentStudioBlockCatalogWithReadOnlyAssets();
        UpdateStudioBlockstateAuthoringSafety();
    }

    private void StudioBlockSelectionInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End or Key.Enter)
            SyncStudioBlockModelDocument();
    }

    private void QueueStudioBlockBuilderCompletionRefresh()
        => _ = Dispatcher.BeginInvoke(() =>
        {
            AugmentStudioBlockCatalogWithReadOnlyAssets();
            UpdateStudioBlockstateAuthoringSafety();
        });

    private void LoadStudioBlockEditorDocumentModel(
        MinecraftResolvedBlockModel model,
        int blockstateXRotation,
        int blockstateYRotation)
    {
        SyncStudioBlockModelDocument();
        if (string.IsNullOrWhiteSpace(_repoRoot))
        {
            LoadStudioBlockEditorModel(model, blockstateXRotation, blockstateYRotation);
            return;
        }

        var repoKey = StudioBlockModelRepoKey(_repoRoot, model.SourcePath);
        if (!_studioBlockModelDocuments.TryGetValue(repoKey, out var document))
        {
            LoadStudioBlockEditorModel(model, blockstateXRotation, blockstateYRotation);
            if (_studioSourceBlockModel is null || _studioEditableBlockModel is null)
                return;

            document = new StudioBlockModelDocument
            {
                RepoKey = repoKey,
                Source = _studioSourceBlockModel,
                Editable = _studioEditableBlockModel,
                Modified = _studioBlockModelModified
            };
            _studioBlockModelDocuments[repoKey] = document;
            _studioBlockModelDocument = document;
            return;
        }

        _studioBlockModelDocument = document;
        _studioSourceBlockModel = document.Source;
        _studioEditableBlockModel = document.Editable;
        _studioEditableBlockstateXRotation = blockstateXRotation;
        _studioEditableBlockstateYRotation = blockstateYRotation;
        _studioBlockModelModified = document.Modified;
        _studioSelectedElementIndex = _studioEditableBlockModel.Elements.Count > 0 ? 0 : -1;
        RefreshStudioElementList();
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
        QueueStudioFaceEditorRefresh();
    }

    private void SyncStudioBlockModelDocument()
    {
        if (_studioBlockModelDocument is null || _studioEditableBlockModel is null)
            return;
        _studioBlockModelDocument.Editable = _studioEditableBlockModel;
        _studioBlockModelDocument.Modified = _studioBlockModelModified;
    }

    private List<StudioReviewChange> CollectStudioCompletedReviewChanges()
    {
        SyncStudioBlockModelDocument();
        var changes = CollectStudioReviewChanges();
        changes.RemoveAll(change => change.Kind is StudioChangeKind.BlockModel or StudioChangeKind.Blockstate);

        if (!string.IsNullOrWhiteSpace(_repoRoot))
        {
            var prefix = Path.GetFullPath(_repoRoot) + "|";
            foreach (var document in _studioBlockModelDocuments.Values
                         .Where(document => document.Modified &&
                                            document.RepoKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                changes.Add(ForeverStudioBlockBuilderCompletionService.BuildBlockModelChange(
                    _repoRoot,
                    document.Source,
                    document.Editable));
            }

            foreach (var document in _studioBlockstateDocuments.Values
                         .Where(document => document.Modified &&
                                            document.RepoKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                var asset = _studioBlockAssets.FirstOrDefault(candidate =>
                    candidate.Id.Equals(document.AssetId, StringComparison.OrdinalIgnoreCase));
                if (asset is null)
                    continue;
                changes.Add(ForeverStudioBlockBuilderCompletionService.BuildBlockstateChange(
                    _repoRoot,
                    asset.BlockstatePath,
                    document.SourceVariants,
                    document.EditableVariants));
            }
        }

        foreach (var change in changes)
            ForeverStudioBlockBuilderCompletionService.ValidateReviewChange(change);

        return changes
            .OrderBy(change => change.Kind)
            .ThenBy(change => change.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void AcceptStudioBlockBuilderAppliedChanges(IReadOnlyList<StudioReviewChange> changes)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;

        var prefix = Path.GetFullPath(_repoRoot) + "|";
        foreach (var key in _studioBlockModelDocuments.Keys
                     .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            _studioBlockModelDocuments.Remove(key);
        }
        _studioBlockModelDocument = null;
    }

    private void EnsureStudioBlockstateSafetyBanner()
    {
        if (_studioBlockstateAuthoringSafetyText is not null || _studioBlockstateRoot is null)
            return;
        _studioBlockstateAuthoringSafetyText = new TextBlock
        {
            Foreground = Brush("#D8B26A"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            Visibility = Visibility.Collapsed
        };
        _studioBlockstateRoot.Children.Insert(0, _studioBlockstateAuthoringSafetyText);
    }

    private void UpdateStudioBlockstateAuthoringSafety()
    {
        EnsureStudioBlockstateSafetyBanner();
        if (string.IsNullOrWhiteSpace(_repoRoot) ||
            _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset)
        {
            return;
        }

        var support = ForeverStudioBlockBuilderCompletionService.InspectBlockstate(_repoRoot, asset.BlockstatePath);
        var canAuthor = support.CanAuthor;
        if (_studioBlockstateAuthoringSafetyText is not null)
        {
            _studioBlockstateAuthoringSafetyText.Text = support.Message;
            _studioBlockstateAuthoringSafetyText.Visibility = canAuthor ? Visibility.Collapsed : Visibility.Visible;
            _studioBlockstateAuthoringSafetyText.Foreground = Brush(
                support.Support == StudioBlockstateAuthoringSupport.InvalidReadOnly ? "#E18B8B" : "#D8B26A");
        }

        if (_studioBlockstateKeyBox is not null)
            _studioBlockstateKeyBox.IsReadOnly = !canAuthor;
        if (_studioBlockstateModelCombo is not null)
            _studioBlockstateModelCombo.IsEnabled = canAuthor;
        if (_studioBlockstateXRotationCombo is not null)
            _studioBlockstateXRotationCombo.IsEnabled = canAuthor;
        if (_studioBlockstateYRotationCombo is not null)
            _studioBlockstateYRotationCombo.IsEnabled = canAuthor;
        if (_studioBlockstateUvLockCheck is not null)
            _studioBlockstateUvLockCheck.IsEnabled = canAuthor;

        if (_studioBlockstateRoot is null)
            return;
        var authoringCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Apply variant", "Add variant", "Remove", "Reset blockstate"
        };
        foreach (var button in FindStudioVisualChildren<Button>(_studioBlockstateRoot))
        {
            if (button.Content?.ToString() is { } label && authoringCommands.Contains(label))
                button.IsEnabled = canAuthor;
        }
    }

    private void AugmentStudioBlockCatalogWithReadOnlyAssets()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot) || _studioBlockMachineCombo is null)
            return;

        var root = Path.Combine(
            _repoRoot,
            "mod", "src", "main", "resources", "assets", "forever_skyblock", "blockstates");
        if (!Directory.Exists(root))
            return;

        var assets = _studioBlockAssets.ToList();
        var knownPaths = assets.Select(asset => Path.GetFullPath(Path.Combine(_repoRoot, asset.BlockstatePath)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = false;
        foreach (var path in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            var fullPath = Path.GetFullPath(path);
            if (knownPaths.Contains(fullPath))
                continue;

            var relative = Path.GetRelativePath(_repoRoot, fullPath).Replace('\\', '/');
            var support = ForeverStudioBlockBuilderCompletionService.InspectBlockstate(_repoRoot, relative);
            if (support.Support != StudioBlockstateAuthoringSupport.MultipartReadOnly ||
                string.IsNullOrWhiteSpace(support.PreviewModelReference))
            {
                continue;
            }

            try
            {
                _ = MinecraftBlockModelPreviewService.LoadResolvedModel(_repoRoot, support.PreviewModelReference);
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
            {
                continue;
            }

            var id = Path.GetFileNameWithoutExtension(path);
            assets.Add(new MinecraftBlockAssetDescriptor
            {
                Id = id,
                DisplayName = HumanizeStudioBlockId(id),
                BlockstatePath = relative,
                Variants =
                [
                    new MinecraftBlockVariantDescriptor
                    {
                        Key = "multipart_preview",
                        ModelReference = support.PreviewModelReference
                    }
                ]
            });
            knownPaths.Add(fullPath);
            added = true;
        }

        if (!added)
            return;

        var previousId = (_studioBlockMachineCombo.SelectedItem as MinecraftBlockAssetDescriptor)?.Id;
        _studioBlockAssets = assets.OrderBy(asset => asset.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        _studioBlockSelectorUpdate = true;
        try
        {
            _studioBlockMachineCombo.ItemsSource = null;
            _studioBlockMachineCombo.DisplayMemberPath = nameof(MinecraftBlockAssetDescriptor.DisplayName);
            _studioBlockMachineCombo.ItemsSource = _studioBlockAssets;
            _studioBlockMachineCombo.SelectedItem = previousId is null
                ? _studioBlockAssets.FirstOrDefault()
                : _studioBlockAssets.FirstOrDefault(asset => asset.Id.Equals(previousId, StringComparison.OrdinalIgnoreCase))
                  ?? _studioBlockAssets.FirstOrDefault();
        }
        finally
        {
            _studioBlockSelectorUpdate = false;
        }
        PopulateStudioBlockSelectors();
    }

    private static string StudioBlockModelRepoKey(string repoRoot, string sourcePath)
        => Path.GetFullPath(repoRoot) + "|" + sourcePath.Replace('\\', '/');

    private static string HumanizeStudioBlockId(string value)
        => string.Join(' ', value
            .Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]));
}

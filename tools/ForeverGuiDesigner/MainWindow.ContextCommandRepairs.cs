using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;
using Microsoft.Win32;

namespace ForeverGuiDesigner;

/// <summary>
/// Explicit user-command overrides layered on top of the source-faithful machine
/// presentation owners. The normal runtime renderers remain authoritative until the
/// user deliberately cuts, reorders, edits or replaces one concrete editor element.
/// </summary>
public partial class MainWindow
{
    private const int EditorSendToBackZ = -900;
    private const int EditorBringToFrontZ = 900;
    private const string EditorTextOverrideTagPrefix = "__forever_editor_text_override:";
    private const string EditorTextureOverrideTagPrefix = "__forever_editor_texture_override:";

    private bool _contextCommandRepairsInitialized;
    private bool _contextCommandRepairQueued;
    private object? _contextCommandElementSource;
    private string _contextCommandSuppressionSignature = string.Empty;

    public void EnableContextCommandRepairs()
    {
        if (_contextCommandRepairsInitialized)
            return;
        _contextCommandRepairsInitialized = true;

        // Replace the compact menu keyboard path so Ctrl+X/Delete use the same
        // source-aware semantics as the repaired context-menu commands.
        PreviewKeyDown -= CompactContextMenuPreviewKeyDown;
        PreviewKeyDown += ContextCommandRepairPreviewKeyDown;

        if (_workspaceContextMenu is not null)
            _workspaceContextMenu.Opened += (_, _) => RepairWorkspaceContextMenuCommands();

        // The two persistent layer buttons are wired in XAML to the legacy model-only
        // handlers. Replace those handlers with commands that also persist a visual
        // Z override across the final machine presentation pass.
        foreach (var button in FindVisualChildren<Button>(this))
        {
            switch (button.Content?.ToString())
            {
                case "Bring front":
                    button.Click -= BringFront_Click;
                    button.Click += ContextRepairBringToFront_Click;
                    break;
                case "Send back":
                    button.Click -= SendBack_Click;
                    button.Click += ContextRepairSendToBack_Click;
                    break;
            }
        }

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (HasContextCommandOverrides())
                QueueContextCommandRepairPass();
        };

        QueueContextCommandRepairPass();
    }

    private void RepairWorkspaceContextMenuCommands()
    {
        if (_workspaceContextMenu is null)
            return;

        MarkPresentationOwnedElements();
        var hasSelection = _selection.Count > 0;
        var singleText = _selection.Count == 1 && _selection.First().Type == GuiElementType.Text;
        var singleTexture = _selection.Count == 1 && _selection.First().Type == GuiElementType.Texture;

        ReplaceContextMenuItem(_workspaceContextMenu, "Cut",
            ContextItem("Cut", (_, _) => ContextRepairCutSelection(), hasSelection, "Ctrl+X"));
        ReplaceContextMenuItem(_workspaceContextMenu, "Paste",
            ContextItem("Paste", (_, _) => ContextRepairPasteSelection(), _clipboardElements.Count > 0, "Ctrl+V"));
        ReplaceContextMenuItem(_workspaceContextMenu, "Delete",
            ContextItem("Delete", (_, _) => ContextRepairDeleteSelection(), hasSelection, "Del"));
        ReplaceContextMenuItem(_workspaceContextMenu, "Bring to front",
            ContextItem("Bring to front", ContextRepairBringToFront_Click, hasSelection));
        ReplaceContextMenuItem(_workspaceContextMenu, "Send to back",
            ContextItem("Send to back", ContextRepairSendToBack_Click, hasSelection));
        ReplaceContextMenuItem(_workspaceContextMenu, "Select all",
            ContextItem("Select all", (_, _) => SelectAllContextCommandElements(),
                _layout.Elements.Any(element => !element.EditorSuppressed), "Ctrl+A"));

        if (singleText)
        {
            ReplaceContextMenuItem(_workspaceContextMenu, "Edit Text…",
                ContextItem("Edit Text…", (_, _) => EditSelectedTextWithOverride(), true, "F2"));
        }

        if (singleTexture)
        {
            var cutIndex = FindTopLevelContextMenuItemIndex("Cut");
            if (cutIndex >= 0)
            {
                _workspaceContextMenu.Items.Insert(cutIndex,
                    ContextItem("Choose custom texture…", (_, _) => ChooseCustomTextureForSelection()));
                _workspaceContextMenu.Items.Insert(cutIndex + 1, new Separator());
            }
        }

        // The visual-polish handler runs before this repair handler. Re-style the
        // replacement items so they retain the same custom menu chrome.
        ApplyDesignerContextMenuStyles();
    }

    private static bool ReplaceContextMenuItem(ItemsControl owner, string header, MenuItem replacement)
    {
        for (var i = 0; i < owner.Items.Count; i++)
        {
            if (owner.Items[i] is not MenuItem item)
                continue;

            if (string.Equals(item.Header?.ToString(), header, StringComparison.Ordinal))
            {
                owner.Items[i] = replacement;
                return true;
            }

            if (ReplaceContextMenuItem(item, header, replacement))
                return true;
        }
        return false;
    }

    private int FindTopLevelContextMenuItemIndex(string header)
    {
        if (_workspaceContextMenu is null)
            return -1;
        for (var i = 0; i < _workspaceContextMenu.Items.Count; i++)
        {
            if (_workspaceContextMenu.Items[i] is MenuItem item &&
                string.Equals(item.Header?.ToString(), header, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    private void ContextCommandRepairPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (control && e.Key == Key.X)
        {
            ContextRepairCutSelection();
            e.Handled = true;
            return;
        }

        if (control && e.Key == Key.V)
        {
            ContextRepairPasteSelection();
            e.Handled = true;
            return;
        }

        if (control && e.Key == Key.A)
        {
            SelectAllContextCommandElements();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            ContextRepairDeleteSelection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2 && _selection.Count == 1 && _selection.First().Type == GuiElementType.Text)
        {
            EditSelectedTextWithOverride();
            e.Handled = true;
        }
    }

    private void SelectAllContextCommandElements()
        => SetSelection(_layout.Elements.Where(element => !element.EditorSuppressed));

    private void ContextRepairCutSelection()
    {
        if (_selection.Count == 0)
            return;

        CopySelection();
        ContextRepairDeleteSelectionCore();
    }

    private void ContextRepairDeleteSelection()
    {
        if (_selection.Count == 0)
            return;
        ContextRepairDeleteSelectionCore();
    }

    private void ContextRepairDeleteSelectionCore()
    {
        MarkPresentationOwnedElements();
        PushUndo();

        var selected = _selection.ToList();
        foreach (var element in selected)
        {
            if (element.EditorSourceOwned)
            {
                // Source/runtime owners treat a missing element as damaged structure
                // and reconstruct it. Keep a hidden tombstone in the model instead so
                // the source invariant is satisfied while the editor behaves as cut/deleted.
                element.EditorSuppressed = true;
                element.Visible = false;
                element.EditorZIndexOverride = null;
            }
            else
            {
                _layout.Elements.Remove(element);
            }
        }

        _selection.Clear();
        RefreshElementList();
        ApplySuppressedElementFilter();
        RenderLayout();
        UpdateInspector();
        MarkDirty();
        QueueContextCommandRepairPass();
    }

    private void ContextRepairPasteSelection()
    {
        if (_clipboardElements.Count == 0)
            return;

        PushUndo();
        var pasted = new List<GuiElementModel>();
        foreach (var source in _clipboardElements)
        {
            var copy = source.Clone();
            copy.Id = CreateUniqueId(source.Id + "_copy");
            copy.X = Clamp(source.X + 4, 0, Math.Max(0, _layout.Width - copy.Width));
            copy.Y = Clamp(source.Y + 4, 0, Math.Max(0, _layout.Height - copy.Height));
            copy.Visible = true;
            copy.EditorSuppressed = false;
            copy.EditorSourceOwned = false;
            copy.EditorZIndexOverride = null;
            _layout.Elements.Add(copy);
            pasted.Add(copy);
        }

        SetSelection(pasted);
        RefreshElementList();
        RenderLayout();
        MarkDirty();
        QueueContextCommandRepairPass();
    }

    private void ContextRepairBringToFront_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count == 0)
            return;

        PushUndo();
        var selectedInOrder = _layout.Elements.Where(_selection.Contains).ToList();
        _layout.Elements.RemoveAll(_selection.Contains);
        _layout.Elements.AddRange(selectedInOrder);
        foreach (var element in selectedInOrder)
            element.EditorZIndexOverride = EditorBringToFrontZ;

        RefreshElementList();
        RenderLayout();
        ApplyExplicitLayerOverrides();
        MarkDirty();
        QueueContextCommandRepairPass();
    }

    private void ContextRepairSendToBack_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count == 0)
            return;

        PushUndo();
        var selectedInOrder = _layout.Elements.Where(_selection.Contains).ToList();
        _layout.Elements.RemoveAll(_selection.Contains);
        _layout.Elements.InsertRange(0, selectedInOrder);
        foreach (var element in selectedInOrder)
            element.EditorZIndexOverride = EditorSendToBackZ;

        RefreshElementList();
        RenderLayout();
        ApplyExplicitLayerOverrides();
        MarkDirty();
        QueueContextCommandRepairPass();
    }

    private void EditSelectedTextWithOverride()
    {
        if (_selection.Count != 1 || _selection.First() is not { Type: GuiElementType.Text } element)
            return;

        var currentText = element.EditorTextOverride ?? element.Content ?? string.Empty;
        var editor = new TextBox
        {
            Text = currentText,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MinHeight = 90,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var apply = new Button { Content = "Apply", Width = 90, IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 90, IsCancel = true };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);

        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock
        {
            Text = element.Id,
            Foreground = Brush("#8DB9E8"),
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(editor);
        panel.Children.Add(buttons);

        var dialog = new Window
        {
            Owner = this,
            Title = "Edit Text",
            Width = 460,
            Height = 230,
            MinWidth = 360,
            MinHeight = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#202327"),
            Foreground = Brush("#E8E8E8"),
            Content = panel,
            ShowInTaskbar = false
        };

        apply.Click += (_, _) => dialog.DialogResult = true;
        dialog.Loaded += (_, _) =>
        {
            editor.Focus();
            editor.SelectAll();
        };

        if (dialog.ShowDialog() != true || string.Equals(currentText, editor.Text, StringComparison.Ordinal))
            return;

        PushUndo();
        element.EditorTextOverride = editor.Text;
        element.Content = editor.Text;

        // Runtime text updates are allowed to continue for every other element. Only
        // this explicitly edited text is restored by the scoped final override pass.
        RenderLayout();
        ApplyTextOverride(element);
        UpdateInspector();
        MarkDirty();
        QueueContextCommandRepairPass();
    }

    private void ChooseCustomTextureForSelection()
    {
        if (_selection.Count != 1 || _selection.First() is not { Type: GuiElementType.Texture } element)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Choose custom texture",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        var existing = ResolveTexturePath(element.TexturePath);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            var directory = Path.GetDirectoryName(existing);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;
        }
        else if (!string.IsNullOrWhiteSpace(_repoRoot) && Directory.Exists(_repoRoot))
        {
            dialog.InitialDirectory = _repoRoot;
        }

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            _ = LoadBitmap(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"The selected image could not be decoded by the GUI Designer.\n\n{ex.Message}",
                "Unsupported texture",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        PushUndo();
        element.TexturePath = Path.GetFullPath(dialog.FileName);
        element.EditorCustomTextureOverride = true;
        RenderLayout();
        ApplyCustomTextureOverride(element);
        UpdateInspector();
        MarkDirty();
        QueueContextCommandRepairPass();
    }

    private void MarkPresentationOwnedElements()
    {
        if (!HasActiveForeverMachineLayout || _activeForeverTemplate is null)
        {
            foreach (var element in _layout.Elements)
                element.EditorSourceOwned = false;
            return;
        }

        foreach (var element in _layout.Elements)
        {
            var copied = element.Id.Contains("_copy", StringComparison.OrdinalIgnoreCase);
            var templateOwned = !copied && _activeForeverTemplate.Layout.Elements.Any(source =>
                source.Type == element.Type &&
                (string.Equals(source.Id, element.Id, StringComparison.OrdinalIgnoreCase) ||
                 (element.Type == GuiElementType.Slot &&
                  string.Equals(source.Content, element.Content, StringComparison.OrdinalIgnoreCase) &&
                  source.X == element.X && source.Y == element.Y)));

            var generatedOwned = !copied &&
                (element.Id.StartsWith("forever_", StringComparison.OrdinalIgnoreCase) ||
                 element.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase));

            element.EditorSourceOwned = templateOwned || generatedOwned;
        }
    }

    private bool HasContextCommandOverrides()
        => _layout.Elements.Any(element =>
            element.EditorSuppressed ||
            element.EditorTextOverride is not null ||
            element.EditorZIndexOverride.HasValue ||
            element.EditorCustomTextureOverride);

    private void QueueContextCommandRepairPass()
    {
        if (!_contextCommandRepairsInitialized || _contextCommandRepairQueued || !HasContextCommandOverrides())
            return;

        _contextCommandRepairQueued = true;

        // LayoutUpdated first lets all existing owners queue/finish their normal work.
        // ContextIdle then queues one SystemIdle callback behind Washing's final source
        // compositor, so only explicit user overrides are restored last.
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
            {
                _contextCommandRepairQueued = false;
                ApplyContextCommandRepairPass();
            }));
        }));
    }

    private void ApplyContextCommandRepairPass()
    {
        if (!_contextCommandRepairsInitialized)
            return;

        MarkPresentationOwnedElements();
        ApplySuppressedElementFilter();
        PruneSuppressedHierarchyItems();

        foreach (var element in _layout.Elements.Where(element => element.Visible && !element.EditorSuppressed))
        {
            if (element.EditorTextOverride is not null)
                ApplyTextOverride(element);
            if (element.EditorCustomTextureOverride)
                ApplyCustomTextureOverride(element);
        }

        ApplyExplicitLayerOverrides();
    }

    private void ApplySuppressedElementFilter()
    {
        if (ElementsList.ItemsSource is null)
            return;

        var signature = string.Join("\u001F", _layout.Elements
            .Where(element => element.EditorSuppressed)
            .Select(element => element.Id));

        if (ReferenceEquals(_contextCommandElementSource, ElementsList.ItemsSource) &&
            string.Equals(signature, _contextCommandSuppressionSignature, StringComparison.Ordinal))
            return;

        _contextCommandElementSource = ElementsList.ItemsSource;
        _contextCommandSuppressionSignature = signature;
        var view = CollectionViewSource.GetDefaultView(ElementsList.ItemsSource);
        view.Filter = item => item is not GuiElementModel element || !element.EditorSuppressed;
        view.Refresh();
    }

    private void PruneSuppressedHierarchyItems()
    {
        if (_phase8Hierarchy is null)
            return;

        foreach (var group in _phase8Hierarchy.Items.OfType<TreeViewItem>().ToList())
        {
            foreach (var child in group.Items.OfType<TreeViewItem>()
                         .Where(item => item.Tag is GuiElementModel { EditorSuppressed: true })
                         .ToList())
            {
                group.Items.Remove(child);
            }

            if (group.Items.Count == 0)
                _phase8Hierarchy.Items.Remove(group);
        }
    }

    private void ApplyExplicitLayerOverrides()
    {
        foreach (var element in _layout.Elements.Where(element => element.EditorZIndexOverride.HasValue))
        {
            if (!_visuals.TryGetValue(element, out var visual))
                continue;
            var z = element.EditorZIndexOverride!.Value;
            if (Panel.GetZIndex(visual) != z)
                Panel.SetZIndex(visual, z);
        }
    }

    private void ApplyTextOverride(GuiElementModel element)
    {
        if (element.EditorTextOverride is null || !_visuals.TryGetValue(element, out var visual) || visual is not Border border)
            return;

        var overrideText = element.EditorTextOverride;
        element.Content = overrideText;
        var signature = $"{EditorTextOverrideTagPrefix}{overrideText}|{element.TextScale}|{element.TextAlignment}|{element.TextShadow}|washing={IsWashingPhase6Active}|blocked={WashingBlocked}|water={WashingHasWater}|running={WashingRunning}";
        if (border.Child is FrameworkElement { Tag: string tag } && string.Equals(tag, signature, StringComparison.Ordinal))
            return;

        if (IsNonWashingRuntimeMachineActive && _machineRuntimeExclusiveActive)
        {
            ApplyMachineRuntimeText(element, border);
            if (border.Child is FrameworkElement runtimeText)
                runtimeText.Tag = signature;
            return;
        }

        var color = "#E6EEE9";
        var scale = element.TextScale;
        var alignment = element.TextAlignment;
        var shadow = element.TextShadow;

        if (IsWashingPhase6Active)
        {
            switch (element.Id)
            {
                case "screen_title":
                    scale = 1.05;
                    alignment = "left";
                    shadow = false;
                    break;
                case "forever_phase6_washing_status_text":
                    color = WashingBlocked || !WashingHasWater ? "#FFBF76" : WashingRunning ? "#76D8D2" : "#A0B4BE";
                    scale = .73;
                    alignment = "left";
                    shadow = false;
                    break;
                case "forever_phase6_washing_water_text":
                    color = WashingHasWater ? "#9DDDF0" : "#FFBF76";
                    scale = .74;
                    alignment = "center";
                    shadow = false;
                    break;
                case "forever_phase6_washing_label_input":
                case "forever_phase6_washing_label_output":
                    color = "#A0B4BE";
                    scale = .73;
                    alignment = "center";
                    shadow = false;
                    break;
                case "forever_phase6_washing_time":
                    color = "#A0B4BE";
                    scale = .65;
                    alignment = "center";
                    shadow = false;
                    break;
                case "forever_phase6_washing_recipe_summary":
                    scale = .68;
                    alignment = "center";
                    shadow = false;
                    break;
                case "forever_phase6_washing_hint_source":
                case "forever_phase6_washing_hint_water":
                    color = "#A0B4BE";
                    scale = .72;
                    alignment = "left";
                    shadow = false;
                    break;
            }
        }
        else if (element.Id.Contains("label_", StringComparison.OrdinalIgnoreCase))
        {
            color = "#A0B4BE";
        }

        var required = EstimatePixelTextWidth(overrideText, scale);
        border.ToolTip = required > element.Width + .01
            ? $"Text overflow: ~{required:0.#} px required, {element.Width} px available."
            : null;
        border.Child = new MinecraftPixelText(overrideText, Brush(color), shadow, scale, alignment)
        {
            Tag = signature,
            Width = Math.Max(1, element.Width),
            Height = Math.Max(1, element.Height)
        };
        border.Background = Brushes.Transparent;

        if (IsWashingPhase6Active)
        {
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0);
        }
        else
        {
            ApplyFidelityOuterBorder(element, border);
        }
    }

    private void ApplyCustomTextureOverride(GuiElementModel element)
    {
        if (!element.EditorCustomTextureOverride || !_visuals.TryGetValue(element, out var visual) || visual is not Border border)
            return;

        var path = ResolveTexturePath(element.TexturePath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        var signature = EditorTextureOverrideTagPrefix + path;
        if (border.Child is FrameworkElement { Tag: string tag } && string.Equals(tag, signature, StringComparison.Ordinal))
            return;

        try
        {
            var image = new Image
            {
                Tag = signature,
                Source = LoadBitmap(path),
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            border.Child = image;
            border.Background = Brushes.Transparent;

            if (IsWashingPhase6Active || IsNonWashingRuntimeMachineActive)
            {
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
            }
            else
            {
                ApplyFidelityOuterBorder(element, border);
            }
        }
        catch
        {
            // Validation already reports missing/unreadable custom paths. Keep the
            // normal presentation intact if the external file changes after selection.
        }
    }
}

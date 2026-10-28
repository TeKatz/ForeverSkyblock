using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Machine-specific binding controls injected into the established GUI inspector. The inspector
/// remains the same shared editor surface; this panel is visible only while a Machine context is
/// active and translates friendly selections back to raw node/port ids through the binding context.
/// </summary>
public partial class MainWindow
{
    private Border? _unifiedMachineBindingInspector;
    private TextBlock? _unifiedMachineBindingCurrent;
    private ComboBox? _unifiedMachineBindingSourceCombo;
    private ComboBox? _unifiedMachineBindingWidgetCombo;
    private ForeverMachineGuiBindingContext? _unifiedMachineBindingContext;
    private bool _unifiedMachineBindingInspectorRefreshing;

    private void EnsureUnifiedMachineBindingInspector()
    {
        if (_unifiedMachineBindingInspector is not null || SelectionText.Parent is not StackPanel inspector)
            return;

        var card = new Border
        {
            Background = Brush("#152126"),
            BorderBrush = Brush("#35525A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 12),
            Visibility = Visibility.Collapsed
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "MACHINE BINDING",
            Foreground = Brush("#72D5D3"),
            FontWeight = FontWeights.Bold,
            FontSize = 10
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Bind this visual element to a typed Machine value without editing node or port IDs.",
            Foreground = Brush("#82969E"),
            FontSize = 9,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 8)
        });

        _unifiedMachineBindingCurrent = new TextBlock
        {
            Foreground = Brush("#C8D4D8"),
            FontSize = 9.3,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(_unifiedMachineBindingCurrent);

        panel.Children.Add(new TextBlock { Text = "Bind To", Foreground = Brush("#AEB4BA"), FontSize = 9.5 });
        _unifiedMachineBindingSourceCombo = new ComboBox
        {
            Height = 32,
            Margin = new Thickness(0, 3, 0, 8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        _unifiedMachineBindingSourceCombo.SelectionChanged += (_, _) => RefreshUnifiedMachineWidgetChoices();
        panel.Children.Add(_unifiedMachineBindingSourceCombo);

        panel.Children.Add(new TextBlock { Text = "Widget Role", Foreground = Brush("#AEB4BA"), FontSize = 9.5 });
        _unifiedMachineBindingWidgetCombo = new ComboBox
        {
            Height = 32,
            Margin = new Thickness(0, 3, 0, 8)
        };
        panel.Children.Add(_unifiedMachineBindingWidgetCombo);

        var apply = CreateStudioChromeButton("APPLY MACHINE BINDING", double.NaN, true);
        apply.Height = 34;
        apply.HorizontalAlignment = HorizontalAlignment.Stretch;
        apply.Click += (_, _) => ApplyUnifiedMachineBinding();
        panel.Children.Add(apply);
        card.Child = panel;
        _unifiedMachineBindingInspector = card;

        var applyIndex = inspector.Children.IndexOf(ApplyButton);
        inspector.Children.Insert(applyIndex >= 0 ? applyIndex + 1 : Math.Min(8, inspector.Children.Count), card);
        ElementsList.SelectionChanged += (_, _) => RefreshUnifiedMachineBindingInspector();
    }

    private void RefreshUnifiedMachineBindingInspector()
    {
        EnsureUnifiedMachineBindingInspector();
        if (_unifiedMachineBindingInspector is null)
            return;

        var active = _unifiedMachineGuiSession is not null && _machineBuilderDocument is not null;
        _unifiedMachineBindingInspector.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        if (!active)
            return;

        _machineBuilderLibraryLoad ??= ForeverStudioLibraryCatalog.Load(_repoRoot);
        _unifiedMachineBindingContext = ForeverMachineGuiBindingContext.Build(_machineBuilderDocument!, _machineBuilderLibraryLoad.Catalog);

        _unifiedMachineBindingInspectorRefreshing = true;
        try
        {
            _unifiedMachineBindingSourceCombo!.ItemsSource = _unifiedMachineBindingContext.Sources;
            if (_selection.Count != 1)
            {
                _unifiedMachineBindingCurrent!.Text = "Select exactly one GUI element to edit its Machine binding.";
                _unifiedMachineBindingSourceCombo.SelectedItem = null;
                _unifiedMachineBindingSourceCombo.IsEnabled = false;
                _unifiedMachineBindingWidgetCombo!.ItemsSource = null;
                _unifiedMachineBindingWidgetCombo.IsEnabled = false;
                return;
            }

            _unifiedMachineBindingSourceCombo.IsEnabled = true;
            _unifiedMachineBindingWidgetCombo!.IsEnabled = true;
            var shared = ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);
            var selectedId = _selection.Single().Id;
            var element = shared.Elements.FirstOrDefault(candidate => candidate.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
            if (element is null)
            {
                _unifiedMachineBindingCurrent!.Text = "The selected element is not present in the shared GUI document.";
                return;
            }

            var binding = FindSharedBinding(shared, element);
            var currentSource = binding is null ? null : _unifiedMachineBindingContext.Find(binding.SourceId, binding.SourcePortId);
            _unifiedMachineBindingSourceCombo.SelectedItem = currentSource ?? _unifiedMachineBindingContext.Sources.FirstOrDefault();
            RefreshUnifiedMachineWidgetChoices(element.Kind);
            _unifiedMachineBindingCurrent!.Text = currentSource is null
                ? "Not bound yet. Choose a typed Machine value below."
                : $"Current: {currentSource.Category} · {currentSource.DisplayName}";
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            _unifiedMachineBindingCurrent!.Text = "Binding metadata cannot be read safely: " + ex.Message;
            _unifiedMachineBindingSourceCombo!.IsEnabled = false;
            _unifiedMachineBindingWidgetCombo!.IsEnabled = false;
        }
        finally
        {
            _unifiedMachineBindingInspectorRefreshing = false;
        }
    }

    private void RefreshUnifiedMachineWidgetChoices(string? preferredKind = null)
    {
        if (_unifiedMachineBindingInspectorRefreshing || _unifiedMachineBindingWidgetCombo is null)
            return;
        if (_unifiedMachineBindingSourceCombo?.SelectedItem is not ForeverMachineGuiBindingSource source)
        {
            _unifiedMachineBindingWidgetCombo.ItemsSource = null;
            return;
        }

        var current = preferredKind;
        if (string.IsNullOrWhiteSpace(current) && _selection.Count == 1)
        {
            try
            {
                var shared = ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);
                current = shared.Elements.FirstOrDefault(element =>
                    element.Id.Equals(_selection.Single().Id, StringComparison.OrdinalIgnoreCase))?.Kind;
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                current = null;
            }
        }

        var choices = source.SuggestedWidgetKinds.ToList();
        if (!string.IsNullOrWhiteSpace(current) && !choices.Contains(current, StringComparer.OrdinalIgnoreCase))
            choices.Add(current);
        _unifiedMachineBindingWidgetCombo.ItemsSource = choices;
        _unifiedMachineBindingWidgetCombo.SelectedItem = choices.FirstOrDefault(choice =>
            choice.Equals(current, StringComparison.OrdinalIgnoreCase)) ?? choices.FirstOrDefault();
    }

    private void ApplyUnifiedMachineBinding()
    {
        if (_unifiedMachineGuiSession is null || _machineBuilderDocument is null || _selection.Count != 1 ||
            _unifiedMachineBindingSourceCombo?.SelectedItem is not ForeverMachineGuiBindingSource source ||
            _unifiedMachineBindingWidgetCombo?.SelectedItem is not string widgetKind)
            return;

        try
        {
            var selectedId = _selection.Single().Id;
            var shared = ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);
            var element = shared.Elements.FirstOrDefault(candidate => candidate.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase))
                          ?? throw new InvalidDataException($"Selected GUI element '{selectedId}' is missing from the shared document.");

            var binding = FindSharedBinding(shared, element);
            if (binding is null)
            {
                var bindingId = CreateSharedBindingId(shared, element.Id);
                binding = new ForeverGuiBinding { Id = bindingId };
                shared.Bindings.Add(binding);
                element.BindingId = bindingId;
            }

            binding.Provider = "machine";
            binding.SourceId = source.NodeId;
            binding.SourcePortId = source.PortId;
            binding.SourceType = source.DataType;
            binding.LibraryItemVersion = 1;
            element.Kind = widgetKind;

            var defaultAsset = DefaultMachineWidgetAsset(widgetKind);
            if (string.IsNullOrWhiteSpace(element.AssetId) || IsDefaultMachineWidgetAsset(element.AssetId))
                element.AssetId = defaultAsset;
            binding.LibraryItemId = element.AssetId ?? defaultAsset;
            EnsureSharedAsset(shared, binding.LibraryItemId);

            binding.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            binding.Properties["widget"] = JsonSerializer.SerializeToElement(widgetKind);
            binding.Properties["sourcePortId"] = JsonSerializer.SerializeToElement(source.PortId);
            binding.Properties["sourceType"] = JsonSerializer.SerializeToElement(source.DataType);
            if (!binding.Properties.ContainsKey("label"))
                binding.Properties["label"] = JsonSerializer.SerializeToElement(element.Text);

            PushUndo();
            var projected = ForeverGuiLegacyEditorProjection.ToLegacyLayout(shared);
            LoadLayoutIntoEditor(projected, null, clearHistory: false);
            var reselection = _layout.Elements.FirstOrDefault(candidate => candidate.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
            if (reselection is not null)
                SetSelection([reselection]);
            MarkDirty();
            RefreshUnifiedMachineBindingInspector();
            StatusText.Text = $"Bound '{selectedId}' to {source.Category} · {source.DisplayName} as {widgetKind}.";
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or JsonException)
        {
            MessageBox.Show(this, ex.Message, "Machine binding could not be applied", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static ForeverGuiBinding? FindSharedBinding(ForeverGuiDocument document, ForeverGuiElement element)
    {
        var id = string.IsNullOrWhiteSpace(element.BindingId) ? element.Id : element.BindingId;
        return document.Bindings.FirstOrDefault(binding => binding.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateSharedBindingId(ForeverGuiDocument document, string elementId)
    {
        var baseId = string.IsNullOrWhiteSpace(elementId) ? "machine_binding" : $"binding_{elementId}";
        var used = document.Bindings.Select(binding => binding.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = $"{baseId}_{suffix}";
        return id;
    }

    private static string DefaultMachineWidgetAsset(string widgetKind) => widgetKind switch
    {
        "item-slot" => "asset.input-tray",
        "output-slot" => "asset.output-tray",
        "fluid-gauge" => "asset.fluid-window",
        "status" => "asset.status-lamp",
        _ => "asset.screen"
    };

    private static bool IsDefaultMachineWidgetAsset(string assetId)
        => assetId is "asset.input-tray" or "asset.output-tray" or "asset.fluid-window" or "asset.status-lamp" or "asset.screen";

    private static void EnsureSharedAsset(ForeverGuiDocument document, string assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId) || document.Assets.Any(asset => asset.Id.Equals(assetId, StringComparison.OrdinalIgnoreCase)))
            return;
        document.Assets.Add(new ForeverGuiAssetReference
        {
            Id = assetId,
            Kind = "studio-library",
            Resource = assetId
        });
    }
}

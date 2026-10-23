using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _phase5NormalizationQueued;
    private bool _phase5Normalizing;

    public void EnableForeverPhase5StateNormalization()
    {
        PreviewCanvas.LayoutUpdated += (_, _) => QueuePhase5Normalization();
    }

    private void QueuePhase5Normalization()
    {
        if (_phase5NormalizationQueued || _phase5Normalizing)
            return;
        _phase5NormalizationQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(NormalizeCondenserStateElements));
    }

    private void NormalizeCondenserStateElements()
    {
        _phase5NormalizationQueued = false;
        if (_phase5Normalizing || !IsCondenserPhase5Active)
            return;

        var legacy = _layout.Elements
            .Where(element => element.Id.Contains("_non_garden_", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (legacy.Count == 0)
            return;

        _phase5Normalizing = true;
        try
        {
            foreach (var element in legacy)
                _layout.Elements.Remove(element);

            AddStateCopies("panel_condensing", GuiElementType.Texture, 14, 44, 220, 64, "Condensing");
            AddStateCopies("panel_secondary_area", GuiElementType.Texture, 14, 113, 220, 43, "Freezing chamber");
            AddStateCopies("panel_module_hint", GuiElementType.Texture, 18, 187, 212, 12, "Module hint");
            AddStateCopies("label_input", GuiElementType.Text, 18, 48, 43, 9, "Input");
            AddStateCopies("label_output", GuiElementType.Text, 189, 48, 41, 9, "Output");
            AddStateCopies("lock_main", GuiElementType.Texture, 42, 52, 8, 10, "Input lock");

            AddIfMissing("forever_phase5_mk2_base_policy_main", GuiElementType.Button, 126, 224, 49, 18, "Block");
            AddIfMissing("forever_phase5_mk2_mixing_policy_main", GuiElementType.Button, 126, 224, 49, 18, "Block");
            AddIfMissing("forever_phase5_mk2_freezing_policy_main", GuiElementType.Button, 126, 224, 49, 18, "Block");

            RefreshElementList();
            RenderLayout();
            QueuePhase5Refresh();
        }
        finally
        {
            _phase5Normalizing = false;
        }
    }

    private void AddStateCopies(string suffix, GuiElementType type, int x, int y, int width, int height, string content)
    {
        AddIfMissing($"forever_phase5_base_{suffix}", type, x, y, width, height, content);
        AddIfMissing($"forever_phase5_mixing_{suffix}", type, x, y, width, height, content);
        AddIfMissing($"forever_phase5_freezing_{suffix}", type, x, y, width, height, content);
    }

    private void AddIfMissing(string id, GuiElementType type, int x, int y, int width, int height, string content)
    {
        if (_layout.Elements.Any(element => string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase)))
            return;

        _layout.Elements.Add(new GuiElementModel
        {
            Id = id,
            Type = type,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Content = content
        });
    }
}

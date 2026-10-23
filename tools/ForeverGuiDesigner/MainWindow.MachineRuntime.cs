using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Exclusive runtime owner for Crucible, Refining, Ascension and Condenser.
/// Washing keeps its already-stable dedicated owner. This controller follows the
/// same invariants: one presentation writer, one progress clock, source-derived
/// geometry/data, non-destructive selection adorners and no per-frame RenderLayout.
/// </summary>
public partial class MainWindow
{
    private const string MachineRuntimePrefix = "forever_runtime_";
    private const string MachineRuntimeBackdropTag = "__forever_machine_runtime_backdrop_v1";
    private const string MachineRuntimeSelectionTag = "__forever_machine_runtime_selection_v1";

    private bool _machineRuntimeInitialized;
    private bool _machineRuntimeApplying;
    private bool _machineRuntimeExclusiveActive;
    private bool _machineRuntimePlaybackRequested;
    private FrameworkElement? _machineRuntimeVisualAnchor;
    private int _machineRuntimeVisualCount = -1;
    private DateTime _machineRuntimeLastTick = DateTime.UtcNow;
    private ComboBox? _machineRuntimeStateCombo;
    private TextBlock? _machineRuntimeSourceText;
    private string? _machineRuntimeConfiguredScreen;
    private readonly HashSet<Border> _machineRuntimeButtonHooks = [];

    public bool IsExclusiveNonWashingMachineRuntimeActive => IsNonWashingRuntimeMachineActive && _machineRuntimeExclusiveActive;
    private string ActiveRuntimeScreen => _activeForeverTemplate?.ScreenClass ?? string.Empty;
    private bool IsNonWashingRuntimeMachineActive => _activeForeverTemplate is { ScreenClass: "CrucibleScreen" or "RefiningScreen" or "AscensionScreen" or "CondenserScreen" } template &&
        string.Equals(_layout.Name, template.DisplayName, StringComparison.Ordinal) && _layout.Width == template.Layout.Width && _layout.Height == template.Layout.Height;

    public void EnableExclusiveMachineRuntimeTools()
    {
        if (_machineRuntimeInitialized) return;
        _machineRuntimeInitialized = true;

        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("RUNTIME"));
        _machineRuntimeStateCombo = new ComboBox { Width = 132, ToolTip = "Runtime state for the active machine preview." };
        _machineRuntimeStateCombo.SelectionChanged += (_, _) =>
        {
            if (!IsNonWashingRuntimeMachineActive || _machineRuntimeApplying) return;
            _machineRuntimePlaybackRequested = false;
            _phase8Playing = false;
            SetMachineRuntimeProgress(CurrentProgress, false);
            ApplyExclusiveMachineRuntimePresentation(false, true);
        };
        SkyblockBarHost.Children.Add(_machineRuntimeStateCombo);
        _machineRuntimeSourceText = new TextBlock { Width = 94, Foreground = Brush("#83A7B8"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        SkyblockBarHost.Children.Add(_machineRuntimeSourceText);

        if (SkyblockBarHost.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Content, "Load machine")) is { } load)
            load.Click += (_, _) => SynchronizeExclusiveMachineRuntime(true, true);
        ApplyButton.Click += (_, _) => SynchronizeExclusiveMachineRuntime(true, true);
        PreviewCanvas.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler((_, _) => SynchronizeMachineRuntimeSelection()), true);
        ElementsList.SelectionChanged += (_, _) => SynchronizeMachineRuntimeSelection();
        if (_phase8Hierarchy is not null) _phase8Hierarchy.SelectedItemChanged += (_, _) => SynchronizeMachineRuntimeSelection();
        if (_foreverTierCombo is not null) _foreverTierCombo.SelectionChanged += (_, _) => HandleMachineRuntimeVariantChanged();
        if (_foreverModeCombo is not null) _foreverModeCombo.SelectionChanged += (_, _) => HandleMachineRuntimeVariantChanged();
        if (_foreverProgressCombo is not null) _foreverProgressCombo.SelectionChanged += (_, _) => HandleMachineRuntimePresetChanged();

        foreach (var button in SkyblockBarHost.Children.OfType<Button>())
        {
            if (Equals(button.Content, "▶")) button.Click += (_, _) => StartMachineRuntimePlayback();
            else if (Equals(button.Content, "Ⅱ")) button.Click += (_, _) => PauseMachineRuntimePlayback();
            else if (Equals(button.Content, "■")) button.Click += (_, _) => StopMachineRuntimePlayback();
        }
        if (_phase8TimelineSlider is not null) _phase8TimelineSlider.ValueChanged += (_, _) => HandleMachineRuntimeScrub();
        if (_phase8TimelineTimer is not null) _phase8TimelineTimer.Tick += (_, _) => TickMachineRuntime();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_machineRuntimeApplying) return;
            if (IsNonWashingRuntimeMachineActive)
            {
                EnterExclusiveMachineRuntime();
                if (MachineRuntimeGenerationChanged() || !HasMachineRuntimeBackdrop()) ApplyExclusiveMachineRuntimePresentation(true, true);
            }
            else if (_machineRuntimeExclusiveActive) ExitExclusiveMachineRuntime();
        };

        CaptureMachineRuntimeGeneration();
        UpdateMachineRuntimeControls();
    }

    private void SynchronizeExclusiveMachineRuntime(bool ensureStructure, bool rebuildBackdrop)
    {
        if (IsNonWashingRuntimeMachineActive) { EnterExclusiveMachineRuntime(); ApplyExclusiveMachineRuntimePresentation(ensureStructure, rebuildBackdrop); }
        else if (_machineRuntimeExclusiveActive) ExitExclusiveMachineRuntime();
        UpdateMachineRuntimeControls();
    }

    private void EnterExclusiveMachineRuntime()
    {
        if (!IsNonWashingRuntimeMachineActive) return;
        var first = !_machineRuntimeExclusiveActive;
        _machineRuntimeExclusiveActive = true;

        // Hold every older writer exactly as the stable Washing owner does.
        _phase5Refreshing = true;
        _phase6Refreshing = true;
        _visualFidelityApplying = true;
        _machinePresentationApplying = true;
        _washingSourceRendererApplying = true;
        _phase6Timer?.Stop();
        _phase6StableAnimationTimer?.Stop();
        _phase7ProcessingTimer?.Stop();
        _phase8Playing = false;

        if (!first) return;
        RefreshMachineRuntimeProfile(true);
        ConfigureMachineRuntimeStates(true);
        _machineRuntimePlaybackRequested = false;
        SetMachineRuntimeProgress(CurrentProgress, false);
        _machineRuntimeLastTick = DateTime.UtcNow;
    }

    private void ExitExclusiveMachineRuntime()
    {
        if (!_machineRuntimeExclusiveActive) return;
        _machineRuntimeExclusiveActive = false;
        _machineRuntimePlaybackRequested = false;
        _phase8Playing = false;
        RemoveMachineRuntimeBackdrop();
        RemoveMachineRuntimeSelectionAdorners();
        _machineRuntimeButtonHooks.Clear();

        if (!_washingExclusiveModeActive)
        {
            _phase5Refreshing = false;
            _phase6Refreshing = false;
            _visualFidelityApplying = false;
            _machinePresentationApplying = false;
            _washingSourceRendererApplying = false;
            QueuePhase5Refresh(); QueuePhase6Refresh(); QueueVisualFidelityPass(); QueueMachinePresentation(); QueueWashingSourceRender();
        }
        UpdateMachineRuntimeControls();
    }

    private void ApplyExclusiveMachineRuntimePresentation(bool ensureStructure, bool rebuildBackdrop)
    {
        if (!IsNonWashingRuntimeMachineActive || _machineRuntimeApplying) return;
        _machineRuntimeApplying = true;
        try
        {
            EnterExclusiveMachineRuntime();
            RefreshMachineRuntimeProfile();
            ConfigureMachineRuntimeStates();
            var changed = false;
            if (ensureStructure)
            {
                changed |= RemoveLegacyRuntimeModels();
                changed |= EnsureMachineRuntimeSourceSlots();
                changed |= EnsureMachineRuntimeElements();
            }
            SynchronizeMachineRuntimeText();
            if (changed) RefreshElementList();
            if (changed || RuntimeMachineSlotMissingVisual()) { RenderLayout(); rebuildBackdrop = true; }

            PreviewCanvas.Background = Brush("#111315");
            RemoveForeverPreviewVisual();
            RemovePhase5Overlay();
            RemoveWashingOverlay();
            if (rebuildBackdrop || !HasMachineRuntimeBackdrop()) RebuildMachineRuntimeBackdrop();

            RestoreMachineRuntimeChrome();
            ApplyMachineRuntimeVisibility();
            UpdateMachineRuntimeDynamicVisuals();
            HideGeneratedPanelMetadata();
            UpdateMachineRuntimeSelectionAdorners();
            if (_phase8Initialized) EnsurePhase8OverlayVisuals();

            CaptureFidelityVisualGeneration();
            CaptureMachinePresentationGeneration();
            CaptureWashingSourceGeneration();
            CaptureMachineRuntimeGeneration();
        }
        finally { _machineRuntimeApplying = false; }
    }

    private bool RemoveLegacyRuntimeModels()
        => _layout.Elements.RemoveAll(element => element.Id.StartsWith("forever_phase8_", StringComparison.OrdinalIgnoreCase)) > 0;

    private bool EnsureMachineRuntimeSourceSlots()
    {
        if (_activeForeverTemplate is null) return false;
        var changed = false;
        foreach (var source in _activeForeverTemplate.Layout.Elements.Where(element => element.Type == GuiElementType.Slot && element.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase)))
        {
            if (_layout.Elements.Any(element => element.Type == GuiElementType.Slot && string.Equals(element.Content, source.Content, StringComparison.OrdinalIgnoreCase))) continue;
            var clone = source.Clone(); clone.Id = CreateUniqueId(source.Id); _layout.Elements.Add(clone); changed = true;
        }
        return changed;
    }

    private bool EnsureMachineRuntimeElements()
    {
        if (_layout.Elements.Any(element => element.Id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase))) return false;
        void Add(string id, GuiElementType type, int x, int y, int w, int h, string text) => _layout.Elements.Add(new GuiElementModel { Id = MachineRuntimePrefix + id, Type = type, X = x, Y = y, Width = w, Height = h, Content = text, TextShadow = true, TextAlignment = "center", TextScale = .72 });

        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen":
                Add("status_dot", GuiElementType.Texture,177,12,4,4,"Status"); Add("status_text",GuiElementType.Text,184,10,48,9,"IDLE");
                Add("heat_text",GuiElementType.Text,22,40,204,9,"Heat: Torch"); Add("tank_text",GuiElementType.Text,20,63,99,9,"Fluid Tank detected"); Add("freezer_text",GuiElementType.Text,129,63,99,9,"Freezing detected");
                Add("input_label",GuiElementType.Text,22,89,57,9,"INPUT"); Add("output_label",GuiElementType.Text,169,89,57,9,"OUTPUT"); Add("lava_meter",GuiElementType.Texture,111,95,26,32,"Lava"); Add("lava_text",GuiElementType.Text,88,129,72,9,"2000 mB");
                Add("melt_label",GuiElementType.Text,54,133,48,7,"MELT"); Add("freeze_label",GuiElementType.Text,144,133,52,7,"FREEZE"); Add("melt_meter",GuiElementType.Texture,46,141,64,4,"Melt"); Add("freeze_meter",GuiElementType.Texture,138,141,64,4,"Freeze"); Add("time_text",GuiElementType.Text,88,147,72,8,""); Add("recipe_summary",GuiElementType.Text,19,177,210,9,"");
                break;
            case "RefiningScreen":
                Add("status_dot",GuiElementType.Texture,177,12,4,4,"Status"); Add("status_text",GuiElementType.Text,184,10,48,9,"IDLE"); Add("energy_text",GuiElementType.Text,22,38,204,9,"5000 / 10000 FE"); Add("energy_meter",GuiElementType.Texture,23,49,202,3,"Energy");
                Add("material_label",GuiElementType.Text,18,65,64,9,"MATERIAL"); Add("catalyst_label",GuiElementType.Text,66,65,64,9,"CATALYST"); Add("output_label",GuiElementType.Text,158,65,64,9,"OUTPUT"); Add("process_scene",GuiElementType.Texture,111,75,50,26,"Refining"); Add("time_text",GuiElementType.Text,106,104,60,8,""); Add("process_meter",GuiElementType.Texture,111,115,50,3,"Progress"); Add("recipe_summary",GuiElementType.Text,19,126,210,8,""); Add("info_primary",GuiElementType.Text,25,145,132,8,"Consumes FE continuously"); Add("info_secondary",GuiElementType.Text,25,157,132,8,"Materials & costs in JEI");
                break;
            case "AscensionScreen":
                Add("status_text",GuiElementType.Text,192,12,36,8,"READY"); Add("core_label",GuiElementType.Text,14,32,96,9,"FOREVER CORE"); Add("ring_1",GuiElementType.Texture,44,48,36,36,"Ring"); Add("ring_2",GuiElementType.Texture,49,53,26,26,"Ring"); Add("arrow",GuiElementType.Text,108,56,30,20,"→"); Add("reward",GuiElementType.Text,154,52,60,18,"+1 PP"); Add("perk_label",GuiElementType.Text,154,72,60,8,"PERK POINT"); Add("conversion",GuiElementType.Text,19,94,210,9,"1 Forever Core → 1 Perk Point"); Add("process_meter",GuiElementType.Texture,23,106,146,8,"Conversion"); Add("time_text",GuiElementType.Text,176,103,50,9,"10 s"); Add("power_text",GuiElementType.Text,176,112,50,8,"No power required"); Add("recipient",GuiElementType.Text,40,125,117,8,"Recipient: Player"); Add("balance",GuiElementType.Text,160,125,64,8,"BALANCE: — PP"); Add("hint",GuiElementType.Text,19,164,210,8,"The Core is consumed. The point stays with your profile.");
                break;
            case "CondenserScreen":
                Add("status_dot",GuiElementType.Texture,177,12,4,4,"Status"); Add("status_text",GuiElementType.Text,184,10,48,9,"IDLE"); Add("module_state",GuiElementType.Text,46,34,156,8,"BASE CONDENSER"); Add("input_label",GuiElementType.Text,13,48,50,9,"INPUT"); Add("module_input_label",GuiElementType.Text,57,48,62,9,"MODULE INPUT"); Add("output_label",GuiElementType.Text,184,48,48,9,"OUTPUT"); Add("process_scene",GuiElementType.Texture,83,56,98,28,"Condensing"); Add("time_text",GuiElementType.Text,84,80,96,8,""); Add("process_meter",GuiElementType.Texture,84,88,96,3,"Progress"); Add("recipe_summary",GuiElementType.Text,20,95,207,9,""); Add("freezing_label",GuiElementType.Text,29,116,190,8,"FREEZING"); Add("freezing_meter",GuiElementType.Texture,62,132,124,8,"Freezing"); Add("freezing_text",GuiElementType.Text,62,143,124,8,"20 / 40"); Add("module_hint",GuiElementType.Text,20,190,208,8,"Base condenser"); Add("upgrade_label",GuiElementType.Text,22,207,96,8,"UPGRADES 0 / 3"); Add("policies_label",GuiElementType.Text,128,207,98,8,"OUTPUT POLICIES"); Add("policy_main",GuiElementType.Button,126,224,49,18,"Block"); Add("policy_secondary",GuiElementType.Button,179,224,49,18,"Block");
                Add("garden_connected",GuiElementType.Text,46,34,156,8,"GARDEN CONNECTED"); Add("garden_scene",GuiElementType.Texture,20,50,208,67,"Garden"); Add("garden_meter",GuiElementType.Texture,66,122,116,9,"Growth"); Add("garden_time",GuiElementType.Text,66,134,116,8,""); Add("garden_specimen",GuiElementType.Text,7,139,62,8,"SPECIMEN"); Add("garden_harvest",GuiElementType.Text,158,139,75,8,"HARVEST"); Add("garden_routes",GuiElementType.Text,20,185,208,12,"Garden output routes");
                break;
        }
        return true;
    }

    private void ConfigureMachineRuntimeStates(bool force = false)
    {
        if (_machineRuntimeStateCombo is null) return;
        var screen = IsNonWashingRuntimeMachineActive ? ActiveRuntimeScreen : string.Empty;
        if (!force && string.Equals(screen, _machineRuntimeConfiguredScreen, StringComparison.Ordinal)) return;
        _machineRuntimeConfiguredScreen = screen; _machineRuntimeStateCombo.Items.Clear();
        void State(string text, string tag) => _machineRuntimeStateCombo.Items.Add(new ComboBoxItem { Content = text, Tag = tag });
        switch (screen)
        {
            case "CrucibleScreen": State("Melting · Torch","melting_torch"); State("Melting · Campfire","melting_campfire"); State("Melting · Lava","melting_lava"); State("Melting · Superheat","melting_superheat"); State("Freezing","freezing"); State("No heat","no_heat"); State("Lava full","lava_full"); State("Output full","output_full"); State("Idle","idle"); break;
            case "RefiningScreen": State("Refining","running"); State("Output full","blocked"); State("No power","no_power"); State("Idle","idle"); break;
            case "AscensionScreen": State("Processing","processing"); State("Ready","ready"); State("Recipient offline","offline"); State("Storage error","storage_error"); State("Invalid Core","invalid"); State("Already claimed","claimed"); break;
            case "CondenserScreen": State("Running","running"); State("Output full","blocked"); State("Idle","idle"); break;
        }
        if (_machineRuntimeStateCombo.Items.Count > 0) _machineRuntimeStateCombo.SelectedIndex = screen == "AscensionScreen" ? 1 : 0;
        UpdateMachineRuntimeControls();
    }

    private string CurrentMachineRuntimeState => _machineRuntimeStateCombo?.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : "idle";
    private bool RuntimeStateRuns => CurrentMachineRuntimeState is "running" or "processing" or "freezing" || CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal);
    private double MachineRuntimeDurationSeconds => ActiveRuntimeScreen switch
    {
        "CrucibleScreen" => CurrentMachineRuntimeState switch { "melting_torch" => 300, "melting_campfire" => 80, "melting_lava" => 30, "melting_superheat" => 8, "freezing" => Math.Max(.05,_machineRuntimeProfile?.AlternateDurationSeconds ?? 5), _ => Math.Max(.05,_machineRuntimeProfile?.DurationSeconds ?? 8) },
        "CondenserScreen" when CurrentMode == "garden" && (_machineRuntimeProfile?.AlternateDurationSeconds ?? 0) > 0 => _machineRuntimeProfile!.AlternateDurationSeconds,
        _ => Math.Max(.05, _machineRuntimeProfile?.DurationSeconds ?? 5)
    };

    private void UpdateMachineRuntimeControls()
    {
        var active = IsNonWashingRuntimeMachineActive;
        if (_machineRuntimeStateCombo is not null) _machineRuntimeStateCombo.IsEnabled = active;
        if (_machineRuntimeSourceText is not null) _machineRuntimeSourceText.Text = active ? "source: repo" : "source: —";
    }

    private void SynchronizeMachineRuntimeText()
    {
        var profile = _machineRuntimeProfile ?? MachineRuntimeFallback(ActiveRuntimeScreen);
        SetMachineRuntimeText("screen_title", ActiveRuntimeScreen == "CondenserScreen" ? (CurrentTier == "mk2" ? "CONDENSER MK II" : "CONDENSER MK I") : profile.Title);
        SetMachineRuntimeText(MachineRuntimePrefix+"status_text", RuntimeStatusText());
        SetMachineRuntimeText(MachineRuntimePrefix+"recipe_summary", profile.Recipe);
        SetMachineRuntimeText(MachineRuntimePrefix+"time_text", RuntimeTimeText());
        if (ActiveRuntimeScreen == "CrucibleScreen") SetMachineRuntimeText(MachineRuntimePrefix+"heat_text", CurrentMachineRuntimeState switch { "melting_campfire"=>"Heat: Campfire", "melting_lava"=>"Heat: Lava", "melting_superheat"=>"Heat: Superheating", "no_heat"=>"Heat: None", _=>"Heat: Torch" });
        if (ActiveRuntimeScreen == "CondenserScreen")
        {
            SetMachineRuntimeText(MachineRuntimePrefix+"module_state", CurrentMode switch { "mixing"=>"MIXING CHAMBER CONNECTED", "freezing"=>"FREEZING CHAMBER CONNECTED", "garden"=>"GARDEN CONNECTED", _=>"BASE CONDENSER" });
            SetMachineRuntimeText(MachineRuntimePrefix+"module_hint", CurrentMode switch { "mixing"=>"Second input enabled by Mixing Chamber.", "freezing"=>"Freezing route enabled.", "garden"=>"Garden specimen and harvest routes active.", _=>"Second input appears only with a Mixing Chamber." });
            SetMachineRuntimeText(MachineRuntimePrefix+"garden_time", RuntimeTimeText());
        }
        if (ActiveRuntimeScreen == "AscensionScreen") SetMachineRuntimeText(MachineRuntimePrefix+"hint", CurrentMachineRuntimeState switch { "offline"=>"Paused: recipient offline", "storage_error"=>"Save failed. Core retained.", "invalid"=>"Invalid Core. Craft a new one.", "claimed"=>"Core already claimed", _=>"The Core is consumed. The point stays with your profile." });
    }

    private void SetMachineRuntimeText(string id, string value) { var model = _layout.Elements.FirstOrDefault(element => string.Equals(element.Id,id,StringComparison.OrdinalIgnoreCase)); if (model is not null) model.Content = value; }
    private string RuntimeTimeText() => $"{_phase8TimelineProgress*MachineRuntimeDurationSeconds:0.0} / {MachineRuntimeDurationSeconds:0.0} s";
    private string RuntimeStatusText() => ActiveRuntimeScreen switch
    {
        "CrucibleScreen" => CurrentMachineRuntimeState switch { "freezing"=>"FREEZING", "no_heat"=>"NO HEAT", "lava_full"=>"LAVA FULL", "output_full"=>"OUTPUT FULL", "idle"=>"IDLE", _=>"MELTING" },
        "RefiningScreen" => CurrentMachineRuntimeState switch { "running"=>"REFINING", "blocked"=>"OUTPUT FULL", "no_power"=>"NO POWER", _=>"IDLE" },
        "AscensionScreen" => CurrentMachineRuntimeState=="processing" ? "PROCESSING" : "READY",
        "CondenserScreen" => CurrentMachineRuntimeState switch { "running"=>CurrentMode=="garden"?"GROWING":"PROCESSING", "blocked"=>CurrentMode=="garden"?"HARVEST FULL":"OUTPUT FULL", _=>CurrentMode=="garden"?"GARDEN READY":"IDLE" },
        _ => "IDLE"
    };

    private void RestoreMachineRuntimeChrome()
    {
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (model.Id.StartsWith("forever_phase5_",StringComparison.OrdinalIgnoreCase) || model.Id.StartsWith("forever_phase8_",StringComparison.OrdinalIgnoreCase)) { pair.Value.Visibility=Visibility.Collapsed; continue; }
            if (pair.Value is not Border border) continue;
            if (model.Type==GuiElementType.Slot) { ApplyMachineSlotFidelity(model,border,false); border.BorderBrush=Brushes.Transparent; border.BorderThickness=new Thickness(0); }
            else if (model.Type==GuiElementType.Button) ApplyMachineRuntimeButton(model,border,border.IsMouseOver);
            else if (model.Type==GuiElementType.Text) ApplyMachineRuntimeText(model,border);
            else if (model.Id.StartsWith(MachineRuntimePrefix,StringComparison.OrdinalIgnoreCase)) ApplyMachineRuntimeTexture(model,border);
        }
        foreach (var pair in _visuals) Panel.SetZIndex(pair.Value, pair.Key.Id.StartsWith("machine_slot_",StringComparison.OrdinalIgnoreCase)?700:pair.Key.Type==GuiElementType.Button?650:pair.Key.Id.StartsWith(MachineRuntimePrefix,StringComparison.OrdinalIgnoreCase)?600:0);
    }

    private void ApplyMachineRuntimeText(GuiElementModel model, Border border)
    {
        var color = model.Id.Contains("status",StringComparison.OrdinalIgnoreCase) ? Brush(RuntimeAccent()) : model.Id.Contains("garden",StringComparison.OrdinalIgnoreCase)?Brush("#BFD6C4"):model.Id.Contains("reward",StringComparison.OrdinalIgnoreCase)||model.Id.Contains("perk",StringComparison.OrdinalIgnoreCase)||model.Id.Contains("balance",StringComparison.OrdinalIgnoreCase)?Brush("#BCA4F3"):Brush("#D8E3E8");
        border.Background=Brushes.Transparent; border.BorderBrush=Brushes.Transparent; border.BorderThickness=new Thickness(0);
        border.Child=new MinecraftPixelText(string.IsNullOrWhiteSpace(model.Content)?model.Id:model.Content,color,model.TextShadow,model.TextScale<=0?.72:model.TextScale,model.TextAlignment){Width=Math.Max(1,model.Width),Height=Math.Max(1,model.Height),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
    }

    private void ApplyMachineRuntimeButton(GuiElementModel model, Border border, bool hover)
    {
        border.Background=new LinearGradientBrush(Brush(hover?"#37515E":"#263B4B").Color,Brush("#172936").Color,90); border.BorderBrush=hover?Brush(RuntimeAccent()):Brush("#6A818C"); border.BorderThickness=new Thickness(1);
        border.Child=new MinecraftPixelText(string.IsNullOrWhiteSpace(model.Content)?model.Id:model.Content,Brush("#E6EEE9"),false,.82,"center"){Width=Math.Max(1,model.Width-4),Height=8,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
        if (_machineRuntimeButtonHooks.Add(border)) { border.MouseEnter+=(_,_)=>{if(IsNonWashingRuntimeMachineActive&&border.Tag is GuiElementModel m)ApplyMachineRuntimeButton(m,border,true);}; border.MouseLeave+=(_,_)=>{if(IsNonWashingRuntimeMachineActive&&border.Tag is GuiElementModel m)ApplyMachineRuntimeButton(m,border,false);}; }
    }

    private void ApplyMachineRuntimeTexture(GuiElementModel model, Border border)
    {
        var id=model.Id;
        if (id.Contains("_meter",StringComparison.OrdinalIgnoreCase))
        {
            var p=id.Contains("energy",StringComparison.OrdinalIgnoreCase)?.5:_phase8TimelineProgress; var canvas=new Canvas{Width=model.Width,Height=model.Height,IsHitTestVisible=false}; AddMachineRuntimeRect(canvas,0,0,model.Width,model.Height,"#263844"); AddMachineRuntimeRect(canvas,0,0,model.Width*Math.Clamp(p,0,1),model.Height,RuntimeAccent()); border.Background=Brushes.Transparent;border.BorderBrush=Brushes.Transparent;border.BorderThickness=new Thickness(0);border.Child=canvas; return;
        }
        if (id.Contains("ring_",StringComparison.OrdinalIgnoreCase)){border.Background=Brushes.Transparent;border.BorderBrush=Brush("#8E4FD2");border.BorderThickness=new Thickness(2);border.Child=null;return;}
        if (id.EndsWith("status_dot",StringComparison.OrdinalIgnoreCase)){border.Background=Brush(RuntimeAccent());border.BorderBrush=Brushes.Transparent;border.BorderThickness=new Thickness(0);border.Child=null;return;}
        if (id.Contains("lava_meter",StringComparison.OrdinalIgnoreCase)){var canvas=new Canvas{Width=model.Width,Height=model.Height,IsHitTestVisible=false};AddMachineRuntimeRect(canvas,0,0,model.Width,model.Height,"#3D454B");AddMachineRuntimeRect(canvas,2,11,model.Width-4,model.Height-13,"#FF6A20");AddMachineRuntimeRect(canvas,3,11,model.Width-6,1,"#FFB34D");border.Background=Brushes.Transparent;border.BorderBrush=Brushes.Transparent;border.BorderThickness=new Thickness(0);border.Child=canvas;return;}
        if(id.Contains("_scene",StringComparison.OrdinalIgnoreCase)){border.Background=Brush(id.Contains("garden",StringComparison.OrdinalIgnoreCase)?"#234B39":"#172A37");border.BorderBrush=Brush(id.Contains("garden",StringComparison.OrdinalIgnoreCase)?"#5A8A63":"#566D7A");border.BorderThickness=new Thickness(1);border.Child=null;}
    }

    private string RuntimeAccent()=>ActiveRuntimeScreen switch {"AscensionScreen" or "RefiningScreen"=>"#BCA4F3","CrucibleScreen"=>CurrentMachineRuntimeState=="freezing"?"#9DDDF0":"#FFBF76","CondenserScreen"=>CurrentMode=="garden"?"#76D982":CurrentTier=="mk2"?"#76D8D2":"#E4B47C",_=>"#76D8D2"};

    private void ApplyMachineRuntimeVisibility()
    {
        foreach(var pair in _visuals)
        {
            var model=pair.Key; var visible=model.Visible;
            if(model.Id.StartsWith("forever_phase5_",StringComparison.OrdinalIgnoreCase)||model.Id.StartsWith("forever_phase8_",StringComparison.OrdinalIgnoreCase)){pair.Value.Visibility=Visibility.Collapsed;continue;}
            if(ActiveRuntimeScreen=="CondenserScreen")
            {
                if(model.Type==GuiElementType.Slot&&model.Id.StartsWith("machine_slot_",StringComparison.OrdinalIgnoreCase))visible&=CondenserRuntimeSlotVisible(model.Content);
                else if(model.Id.StartsWith(MachineRuntimePrefix,StringComparison.OrdinalIgnoreCase))visible&=CondenserRuntimeElementVisible(model.Id);
                else if(model.Id=="filters")visible&=CurrentMode!="garden";
                if(model.Id=="recipes"&&visible){Canvas.SetLeft(pair.Value,CurrentMode=="garden"?94:168);Canvas.SetTop(pair.Value,CurrentMode=="garden"?151:162);}
            }
            pair.Value.Visibility=visible?Visibility.Visible:Visibility.Collapsed;
        }
    }

    private bool CondenserRuntimeSlotVisible(string content)
    {
        var p=content.Split(',',StringSplitOptions.TrimEntries);if(p.Length!=2||!int.TryParse(p[0],out var x)||!int.TryParse(p[1],out var y))return true;
        return (x,y) switch{(30,61)or(202,61)=>CurrentMode!="garden",(80,61)=>CurrentMode=="mixing",(30,128)or(202,128)=>CurrentMode=="freezing",(30,151)or(174,151)or(202,151)=>CurrentMode=="garden",(30,224)or(58,224)or(86,224)=>CurrentTier=="mk2",_=>true};
    }
    private bool CondenserRuntimeElementVisible(string id)
    {
        var garden=id.Contains("garden",StringComparison.OrdinalIgnoreCase); if(garden)return CurrentMode=="garden";
        if(CurrentMode=="garden")return id.EndsWith("status_dot")||id.EndsWith("status_text")||id.EndsWith("upgrade_label")||id.EndsWith("policies_label")||id.EndsWith("policy_main")||id.EndsWith("policy_secondary");
        if(id.Contains("module_input"))return CurrentMode=="mixing";if(id.Contains("freezing_"))return CurrentMode=="freezing";
        if(id.Contains("policy_"))return CurrentTier=="mk2"&&(id.EndsWith("policy_main")||(id.EndsWith("policy_secondary")&&CurrentMode=="freezing"));
        if(id.Contains("upgrade_label")||id.Contains("policies_label"))return CurrentTier=="mk2";return true;
    }

    private void RebuildMachineRuntimeBackdrop()
    {
        RemoveMachineRuntimeBackdrop(); if(!IsNonWashingRuntimeMachineActive)return;
        var c=new Canvas{Tag=MachineRuntimeBackdropTag,Width=_layout.Width,Height=_layout.Height,IsHitTestVisible=false,Background=Brush("#111315")};Panel.SetZIndex(c,-950);DrawMachineRuntimeFrame(c,RuntimeAccent());
        switch(ActiveRuntimeScreen)
        {
            case"CrucibleScreen":Panel(18,35,212,18,"#5A4536");Panel(18,58,103,18,"#355B66");Panel(127,58,103,18,"#355B66");Panel(18,81,212,70,"#54616D");break;
            case"RefiningScreen":Panel(18,35,212,20,"#465A68");Panel(18,60,212,72,"#54616D");Panel(18,138,212,34,"#465A68");break;
            case"AscensionScreen":Panel(190,9,40,15,"#615372");Panel(149,46,70,41,"#8E4FD2");Panel(23,106,146,8,"#697487");Panel(20,120,208,17,"#576073");break;
            case"CondenserScreen":if(CurrentMode=="garden"){Panel(42,31,164,12,"#315B45");Panel(14,44,220,138,"#335544");Panel(18,183,212,16,"#466252");}else{Panel(42,31,164,12,"#365A66");Panel(14,44,220,64,"#54616D");Panel(14,113,220,43,"#465A68");Panel(18,187,212,12,"#5D6875");}Panel(18,203,212,51,CurrentTier=="mk2"?"#4B5E6E":"#3E454C");break;
        }
        var iy=ActiveRuntimeScreen=="CondenserScreen"?260:ActiveRuntimeScreen=="AscensionScreen"?176:190;Panel(18,iy-6,212,92,"#405866");PreviewCanvas.Children.Insert(0,c);
        void Panel(double x,double y,double w,double h,string edge){AddMachineRuntimeRect(c,x,y,w,h,"#0A151F");AddMachineRuntimeRect(c,x+1,y+1,w-2,h-2,edge);AddMachineRuntimeRect(c,x+2,y+2,w-4,h-4,"#182A37");AddMachineRuntimeRect(c,x+3,y+1,w-6,1,"#637884");}
    }
    private static void DrawMachineRuntimeFrame(Canvas c,string accent){AddMachineRuntimeRect(c,0,0,c.Width,c.Height,"#0C151F");AddMachineRuntimeRect(c,3,4,c.Width-6,c.Height-8,"#253746");AddMachineRuntimeRect(c,8,7,c.Width-16,20,"#101D29");AddMachineRuntimeRect(c,14,27,c.Width-28,1,"#526979");AddMachineRuntimeRect(c,14,27,52,1,accent);AddMachineRuntimeRect(c,10,33,1,c.Height-45,"#344A58");AddMachineRuntimeRect(c,c.Width-11,33,1,c.Height-45,"#344A58");AddMachineRuntimeRect(c,c.Width/2-14,c.Height-4,28,1,accent);}
    private static void AddMachineRuntimeRect(Canvas c,double x,double y,double w,double h,string color){if(w<=0||h<=0)return;var r=new Rectangle{Width=w,Height=h,Fill=Brush(color),SnapsToDevicePixels=true,IsHitTestVisible=false};Canvas.SetLeft(r,x);Canvas.SetTop(r,y);c.Children.Add(r);}
    private bool HasMachineRuntimeBackdrop()=>PreviewCanvas.Children.OfType<FrameworkElement>().Any(child=>Equals(child.Tag,MachineRuntimeBackdropTag));
    private void RemoveMachineRuntimeBackdrop(){foreach(var child in PreviewCanvas.Children.OfType<FrameworkElement>().Where(child=>Equals(child.Tag,MachineRuntimeBackdropTag)).ToList())PreviewCanvas.Children.Remove(child);}

    private void SynchronizeMachineRuntimeSelection(){if(!IsNonWashingRuntimeMachineActive||_machineRuntimeApplying)return;EnterExclusiveMachineRuntime();RestoreMachineRuntimeChrome();UpdateMachineRuntimeSelectionAdorners();if(_phase8Initialized)EnsurePhase8OverlayVisuals();}
    private void UpdateMachineRuntimeSelectionAdorners(){RemoveMachineRuntimeSelectionAdorners();if(!IsNonWashingRuntimeMachineActive)return;foreach(var m in _selection.Where(m=>m.Visible&&_visuals.ContainsKey(m))){var b=new Border{Tag=MachineRuntimeSelectionTag,Width=Math.Max(1,m.Width),Height=Math.Max(1,m.Height),Background=Brushes.Transparent,BorderBrush=Brush("#2E9BFF"),BorderThickness=new Thickness(1),IsHitTestVisible=false};Canvas.SetLeft(b,m.X);Canvas.SetTop(b,m.Y);Panel.SetZIndex(b,10000);PreviewCanvas.Children.Add(b);}}
    private void RemoveMachineRuntimeSelectionAdorners(){foreach(var child in PreviewCanvas.Children.OfType<FrameworkElement>().Where(child=>Equals(child.Tag,MachineRuntimeSelectionTag)).ToList())PreviewCanvas.Children.Remove(child);}

    private void HandleMachineRuntimeVariantChanged(){if(!IsNonWashingRuntimeMachineActive||_machineRuntimeApplying)return;RefreshMachineRuntimeProfile(true);ApplyExclusiveMachineRuntimePresentation(false,true);}
    private void HandleMachineRuntimePresetChanged(){if(!IsNonWashingRuntimeMachineActive||_machineRuntimeApplying)return;EnterExclusiveMachineRuntime();_machineRuntimePlaybackRequested=false;_phase8Playing=false;SetMachineRuntimeProgress(CurrentProgress,true);}
    private void StartMachineRuntimePlayback(){if(!IsNonWashingRuntimeMachineActive)return;EnterExclusiveMachineRuntime();if(_phase8TimelineProgress>=.999999)SetMachineRuntimeProgress(0,true);_machineRuntimePlaybackRequested=true;_phase8Playing=false;_machineRuntimeLastTick=DateTime.UtcNow;UpdateMachineRuntimeDynamicVisuals();}
    private void PauseMachineRuntimePlayback(){if(!IsNonWashingRuntimeMachineActive)return;EnterExclusiveMachineRuntime();_machineRuntimePlaybackRequested=false;_phase8Playing=false;UpdateMachineRuntimeDynamicVisuals();}
    private void StopMachineRuntimePlayback(){if(!IsNonWashingRuntimeMachineActive)return;EnterExclusiveMachineRuntime();_machineRuntimePlaybackRequested=false;_phase8Playing=false;SetMachineRuntimeProgress(0,true);_machineRuntimeLastTick=DateTime.UtcNow;}
    private void HandleMachineRuntimeScrub(){if(!IsNonWashingRuntimeMachineActive||_phase8UpdatingSlider)return;EnterExclusiveMachineRuntime();_machineRuntimePlaybackRequested=false;_phase8Playing=false;SetMachineRuntimeProgress((_phase8TimelineSlider?.Value??0)/100.0,true);_machineRuntimeLastTick=DateTime.UtcNow;}
    private void TickMachineRuntime(){var now=DateTime.UtcNow;var dt=Math.Clamp((now-_machineRuntimeLastTick).TotalSeconds,0,.1);_machineRuntimeLastTick=now;if(!IsNonWashingRuntimeMachineActive){if(_machineRuntimeExclusiveActive)ExitExclusiveMachineRuntime();return;}if(!_machineRuntimeExclusiveActive)EnterExclusiveMachineRuntime();_phase8Playing=false;_phase6Timer?.Stop();_phase6StableAnimationTimer?.Stop();_phase7ProcessingTimer?.Stop();if(!_machineRuntimePlaybackRequested||!RuntimeStateRuns)return;var speed=_phase8SpeedBox?.SelectedItem is ComboBoxItem{Tag:double s}?s:1;var next=_phase8TimelineProgress+dt*speed/MachineRuntimeDurationSeconds;if(next>=1){if(_phase8LoopBox?.IsChecked==true)next%=1;else{next=1;_machineRuntimePlaybackRequested=false;}}SetMachineRuntimeProgress(next,true);}
    private void SetMachineRuntimeProgress(double p,bool update){p=Math.Clamp(p,0,1);_phase8TimelineProgress=p;_phase8Playing=false;if(_phase8TimelineSlider is not null){_phase8UpdatingSlider=true;_phase8TimelineSlider.Value=p*100;_phase8UpdatingSlider=false;}if(_phase8TimelineText is not null)_phase8TimelineText.Text=$"{p*MachineRuntimeDurationSeconds:0.0}/{MachineRuntimeDurationSeconds:0.0}s";if(_phase7ActiveProcessingItem is not null&&IsActiveProcessingSelected)_phase7ActiveProcessingItem.Tag=(p*100).ToString("0.###",CultureInfo.InvariantCulture);SynchronizeMachineRuntimeText();if(update)UpdateMachineRuntimeDynamicVisuals();}
    private void UpdateMachineRuntimeDynamicVisuals(){if(!IsNonWashingRuntimeMachineActive)return;SynchronizeMachineRuntimeText();foreach(var pair in _visuals){if(pair.Value is not Border b||!pair.Key.Id.StartsWith(MachineRuntimePrefix,StringComparison.OrdinalIgnoreCase))continue;if(pair.Key.Type==GuiElementType.Text)ApplyMachineRuntimeText(pair.Key,b);else if(pair.Key.Type==GuiElementType.Texture)ApplyMachineRuntimeTexture(pair.Key,b);if(pair.Key.Id.Contains("ring_1"))pair.Value.RenderTransform=new RotateTransform(_phase8TimelineProgress*360,pair.Key.Width/2.0,pair.Key.Height/2.0);else if(pair.Key.Id.Contains("ring_2"))pair.Value.RenderTransform=new RotateTransform(-_phase8TimelineProgress*360,pair.Key.Width/2.0,pair.Key.Height/2.0);}ApplyMachineRuntimeVisibility();UpdateMachineRuntimeSelectionAdorners();}
    private bool RuntimeMachineSlotMissingVisual()=>_layout.Elements.Any(m=>m.Visible&&m.Type==GuiElementType.Slot&&m.Id.StartsWith("machine_slot_",StringComparison.OrdinalIgnoreCase)&&!_visuals.ContainsKey(m));
    private bool MachineRuntimeGenerationChanged(){var anchor=_visuals.Count==0?null:_visuals.Values.First();return _visuals.Count!=_machineRuntimeVisualCount||!ReferenceEquals(anchor,_machineRuntimeVisualAnchor);}
    private void CaptureMachineRuntimeGeneration(){_machineRuntimeVisualCount=_visuals.Count;_machineRuntimeVisualAnchor=_visuals.Count==0?null:_visuals.Values.First();}
}

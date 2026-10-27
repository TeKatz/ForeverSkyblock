using System.Windows;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (await SelfUpdateService.TryRunApplyModeAsync(e.Args))
        {
            Shutdown();
            return;
        }

        StudioCrashDiagnostics.Install(this);

        var window = new MainWindow();
        window.PrepareStartupPresentation();
        window.EnableForeverStudioFoundation();
        window.EnableForeverStudioStartScreenPolish();
        window.EnableSelfUpdater();
        window.EnableForeverStudioStartScreenPolishV2();
        window.EnableForeverStudioStartScreenPolishV3();
        window.EnableForeverStudioStartScreenPolishV4();
        window.EnableForeverStudioStartScreenPolishV5();
        window.EnableForeverMachineBuilderFoundation();
        window.EnableForeverMachineBuilderLibraryPhase2();
        window.EnableForeverMachineBuilderLibraryPhase2EntryFix();
        window.EnableForeverMachineBuilderVisualDesignPhase3();
        window.EnableForeverMachineBuilderVisualDirectManipulationPhase3();
        window.EnableForeverMachineBuilderAssetExpansionPhase3();
        window.EnableForeverMachineBuilderAssetBrowserPhase3();
        window.EnableForeverMachineBuilderFunctionGraphPhase4();
        window.EnableForeverMachineBuilderBindingsPhase5();
        window.EnableForeverMachineBuilderGuiDesignerPhase4();
        window.EnableForeverMachineBuilderFunctionTemplatesPhase3();
        window.EnableForeverMachineBuilderFunctionToolbarRepair();
        window.EnableForeverMachineBuilderProductionPhase6();
        window.EnableForeverMachineBuilderUxPolish();
        window.EnableForeverBlockModelViewer();
        window.EnableForeverBlockEditorRepositoryReloadFix();
        window.EnableForeverUvTextureEditor();
        window.EnableForeverItemStudio();
        window.EnableForeverBlockDirectEditing();
        window.EnableForeverTextureCanvasStability();
        window.EnableForeverAdvancedTextureEditor();
        window.EnableForeverItemTextureStudio();
        window.EnableForeverStudioUxQoL();
        window.EnableForeverMachineBlockEntityPreviewParity();
        window.EnableForeverBlockstateEditor();
        window.EnableForeverReviewValidationApply();
        window.EnableForeverAnimationStudio();
        window.EnableForeverAnimationStudioInteraction();
        window.EnableForeverAnimationTimelineStability();
        window.EnableForeverRuntimeAnimationIntegration();
        window.EnableForeverRuntimeAnimationVariants();
        window.EnableStudioRuntimeAnimationProfileBindingFix();
        window.EnableForeverRuntimeStateStudio();
        window.EnableForeverRuntimeStateGardenFinalizer();
        window.EnableForeverRuntimeStateEditorFinalizer();
        window.EnableForeverAnimationDiscreteChannels();
        window.EnableForeverAnimationMaterialEffects();
        window.EnableForeverAnimationMaterialResolver();
        window.EnableForeverAnimationPhase17CLabels();
        window.EnableForeverAnimationPersistence();
        window.EnableForeverAnimationPersistenceDirtyTracking();
        window.EnableForeverAnimationPersistenceDirtySignals();
        window.EnableForeverAnimationRuntimeTargetAuthoring();
        window.EnableForeverAnimationRuntimeTargetPreview();
        window.EnableForeverAnimationRuntimeTargetRefresh();
        window.EnableForeverAnimationPhase17DLabels();
        window.EnableForeverAnimationPhase17Q();
        window.EnableForeverAnimationPhase17QWorkflowPolish();
        window.EnableForeverAnimationPhase17QLayoutTuning();
        window.EnableForeverAnimationPhase17QDensityFix();
        window.EnableForeverAnimationPhase17R();
        window.EnableForeverFutureProofRuntimeMetadata();
        MainWindow = window;
        window.Show();

        _ = Dispatcher.BeginInvoke(() =>
        {
            window.EnablePersonalWorkspaceQoL();
            window.EnableForeverSkyblockTools();
            window.EnableForeverPhase5Tools();
            window.EnableForeverPhase5StateNormalization();
            window.EnableForeverPhase5InteractionCorrections();
            window.EnableForeverPhase6Tools();
            window.EnableForeverPhase6StableAnimation();
            window.EnableForeverPhase7Tools();
            window.EnableForeverPhase8Tools();
            window.EnableForeverPhase8InteractionTools();
            window.EnableForeverPhase8PerformanceHotfix();
            window.EnableForeverPhase9Tools();
            window.EnablePhase9SimulatorSnapshotFix();
            window.EnableForeverVisualFidelityPatch();
            window.EnableMachinePresentationLifecycleFix();
            window.EnableWashingSourceRenderer();
            window.EnableWashingRenderPipelineFix();
            window.EnableExclusiveMachineRuntimeTools();
            window.EnableMachineRuntimeVisualPolish();
            window.EnableMachineRuntimePerformanceFix();
            window.EnableNonWashingStableAnimation();
            window.EnableRuntimeSelectionStabilityFix();
            window.EnableNonWashingButtonWashingParity();
            window.EnableCompactContextMenus();
            window.EnableContextMenuVisualPolish();
            window.EnableRuntimeContextSelectionFix();
            window.EnableContextCommandRepairs();
            window.EnableContextCommandFinalizationFix();
            window.EnableStartupPresentationHooks();
            window.EnableAttachmentAndResizeFix();
            window.EnableCondenserCombinedAttachmentPolish();
        }, DispatcherPriority.ContextIdle);
    }
}

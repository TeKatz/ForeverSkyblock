using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    /// <summary>
    /// Reads the document currently displayed by the established GUI editor. Machine context uses
    /// the lossless semantic projection; standalone context uses the same shared contract directly.
    /// </summary>
    private ForeverGuiDocument ReadUnifiedGuiDocumentFromEditor()
        => _unifiedMachineGuiSession is null
            ? ForeverGuiDocumentService.FromLegacyLayout(_layout)
            : ForeverGuiLegacyEditorProjection.FromLegacyLayout(_layout);

    private GuiLayoutModel ProjectUnifiedGuiDocumentToEditor(ForeverGuiDocument document)
        => _unifiedMachineGuiSession is null
            ? ForeverGuiDocumentService.ToLegacyLayout(document)
            : ForeverGuiLegacyEditorProjection.ToLegacyLayout(document);
}

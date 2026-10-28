using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Context boundary for the shared GUI editor engine. The editor owns one ForeverGuiDocument;
/// providers decide where that document comes from and how a committed document is projected back
/// into its host domain.
/// </summary>
public interface IForeverGuiContextProvider
{
    string ContextId { get; }
    string DisplayName { get; }
    ForeverGuiDocument LoadDocument();
    void ApplyDocument(ForeverGuiDocument document);
    IReadOnlyList<ForeverGuiContractIssue> ValidateDocument(ForeverGuiDocument document);
}

public sealed class ForeverLegacyGuiContextProvider : IForeverGuiContextProvider
{
    private readonly Func<GuiLayoutModel> _load;
    private readonly Action<GuiLayoutModel> _apply;

    public ForeverLegacyGuiContextProvider(
        string contextId,
        string displayName,
        Func<GuiLayoutModel> load,
        Action<GuiLayoutModel> apply)
    {
        ContextId = RequireIdentity(contextId, nameof(contextId));
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "GUI Layout" : displayName.Trim();
        _load = load ?? throw new ArgumentNullException(nameof(load));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }

    public string ContextId { get; }
    public string DisplayName { get; }

    public ForeverGuiDocument LoadDocument()
        => ForeverGuiDocumentService.FromLegacyLayout(_load());

    public void ApplyDocument(ForeverGuiDocument document)
        => _apply(ForeverGuiDocumentService.ToLegacyLayout(document));

    public IReadOnlyList<ForeverGuiContractIssue> ValidateDocument(ForeverGuiDocument document)
        => ForeverGuiDocumentService.Validate(document);

    private static string RequireIdentity(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Context id is required.", parameter);
        return value.Trim();
    }
}

public sealed class ForeverMachineGuiContextProvider : IForeverGuiContextProvider
{
    private readonly ForeverStudioMachineDocument _machine;

    public ForeverMachineGuiContextProvider(ForeverStudioMachineDocument machine)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
    }

    public string ContextId => $"machine:{_machine.AssetId}";
    public string DisplayName => string.IsNullOrWhiteSpace(_machine.DisplayName) ? _machine.AssetId : _machine.DisplayName;

    public ForeverGuiDocument LoadDocument()
        => ForeverMachineGuiDocumentBridge.FromMachineDocument(_machine);

    public void ApplyDocument(ForeverGuiDocument document)
        => ForeverMachineGuiDocumentBridge.ApplyToMachineDocument(_machine, document);

    public IReadOnlyList<ForeverGuiContractIssue> ValidateDocument(ForeverGuiDocument document)
    {
        var issues = ForeverGuiDocumentService.Validate(document).ToList();
        var machineBindingIds = document.Bindings
            .Where(binding => binding.Provider.Equals("machine", StringComparison.OrdinalIgnoreCase))
            .Select(binding => binding.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var element in document.Elements)
        {
            if (string.IsNullOrWhiteSpace(element.BindingId))
                continue; // Shared/static presentation is allowed and preserved outside the runtime projection.
            if (!machineBindingIds.Contains(element.BindingId))
            {
                issues.Add(new ForeverGuiContractIssue(
                    ForeverGuiContractSeverity.Error,
                    $"Machine GUI element '{element.Id}' references non-machine binding '{element.BindingId}'."));
            }
        }
        return issues;
    }
}

/// <summary>
/// Mutable editor session independent from WPF. A future shared Canvas/Inspector host can bind to
/// this object while standalone and Machine Builder entry points only supply different providers.
/// </summary>
public sealed class ForeverGuiEditorSession
{
    public ForeverGuiEditorSession(IForeverGuiContextProvider context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Document = context.LoadDocument();
    }

    public IForeverGuiContextProvider Context { get; }
    public ForeverGuiDocument Document { get; private set; }
    public bool IsDirty { get; private set; }

    public void MarkDirty() => IsDirty = true;

    public IReadOnlyList<ForeverGuiContractIssue> Validate()
        => Context.ValidateDocument(Document);

    public void Reload()
    {
        Document = Context.LoadDocument();
        IsDirty = false;
    }

    public void Commit()
    {
        var errors = Validate().Where(issue => issue.Severity == ForeverGuiContractSeverity.Error).ToList();
        if (errors.Count > 0)
            throw new InvalidDataException("GUI editor session cannot commit: " + string.Join(" | ", errors.Select(issue => issue.Message)));
        Context.ApplyDocument(Document);
        IsDirty = false;
    }
}

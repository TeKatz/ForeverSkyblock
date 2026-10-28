namespace ForeverGuiDesigner.Services;

/// <summary>
/// Fail-closed access contract for opening a Machine Project in the shared GUI editor.
/// A blank Machine Project does not implicitly own a GUI: an explicit screen id must have
/// been created by the earlier authoring flow first.
/// </summary>
public static class ForeverMachineGuiEditorAccess
{
    public sealed record Identity(string Namespace, string AssetId, string ScreenId)
    {
        public string ContextId => $"machine:{Namespace}:{AssetId}:screen:{ScreenId}";
    }

    public static Identity? GetCreatedGui(ForeverStudioMachineDocument? document)
    {
        if (document is null || string.IsNullOrWhiteSpace(document.Gui.ScreenId))
            return null;

        var ns = document.Namespace?.Trim().ToLowerInvariant() ?? string.Empty;
        var assetId = document.AssetId?.Trim().ToLowerInvariant() ?? string.Empty;
        var screenId = document.Gui.ScreenId.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(screenId))
            return null;

        return new Identity(ns, assetId, screenId);
    }

    public static bool HasCreatedGui(ForeverStudioMachineDocument? document)
        => GetCreatedGui(document) is not null;

    public static bool Matches(Identity? expected, ForeverStudioMachineDocument? document)
    {
        if (expected is null || GetCreatedGui(document) is not { } current)
            return false;

        return expected.Namespace.Equals(current.Namespace, StringComparison.OrdinalIgnoreCase) &&
               expected.AssetId.Equals(current.AssetId, StringComparison.OrdinalIgnoreCase) &&
               expected.ScreenId.Equals(current.ScreenId, StringComparison.OrdinalIgnoreCase);
    }
}

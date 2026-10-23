namespace ForeverGuiDesigner.Models;

public sealed class AssetItemModel
{
    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }

    public override string ToString() => RelativePath;
}

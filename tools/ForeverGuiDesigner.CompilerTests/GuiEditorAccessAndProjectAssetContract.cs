using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class GuiEditorAccessAndProjectAssetContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        MachineGuiAccessIsFailClosed();
        LocalBlockCanBackGuiItemPreview();
    }

    private static void MachineGuiAccessIsFailClosed()
    {
        var machine = ForeverStudioMachineProjectService.CreateBlank("scope_machine", "Scope Machine");
        Check(!ForeverMachineGuiEditorAccess.HasCreatedGui(machine),
            "A blank Machine Project must not implicitly expose the shared GUI editor.");

        machine.Gui.ScreenId = "scope_machine_screen";
        var identity = ForeverMachineGuiEditorAccess.GetCreatedGui(machine);
        Check(identity is not null && identity.ScreenId == "scope_machine_screen",
            "An explicitly authored screen id must enable shared GUI editing for that machine.");
        Check(ForeverMachineGuiEditorAccess.Matches(identity, machine),
            "The captured GUI identity must match its originating machine/screen.");

        machine.Gui.ScreenId = "different_screen";
        Check(!ForeverMachineGuiEditorAccess.Matches(identity, machine),
            "A shared-editor session must stop matching when the machine screen changes.");

        var other = ForeverStudioMachineProjectService.CreateBlank("other_machine", "Other Machine");
        other.Gui.ScreenId = "scope_machine_screen";
        Check(!ForeverMachineGuiEditorAccess.Matches(identity, other),
            "A GUI identity must not match a different Machine Project even when the screen id is the same.");
    }

    private static void LocalBlockCanBackGuiItemPreview()
    {
        var root = Path.Combine(Path.GetTempPath(), "forever-gui-local-block-" + Guid.NewGuid().ToString("N"));
        try
        {
            var assets = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock");
            var blockstates = Path.Combine(assets, "blockstates");
            var models = Path.Combine(assets, "models", "block");
            var textures = Path.Combine(assets, "textures", "block");
            Directory.CreateDirectory(blockstates);
            Directory.CreateDirectory(models);
            Directory.CreateDirectory(textures);

            File.WriteAllText(Path.Combine(blockstates, "test_block.json"),
                "{\"variants\":{\"\":{\"model\":\"forever_skyblock:block/test_block\"}}}");
            File.WriteAllText(Path.Combine(models, "test_block.json"),
                "{\"parent\":\"minecraft:block/cube_all\",\"textures\":{\"all\":\"forever_skyblock:block/test_block\"}}");
            var expectedTexture = Path.Combine(textures, "test_block.png");
            File.WriteAllBytes(expectedTexture, [0]);

            var resolver = new MinecraftAssetResolver(root);
            Check(Path.GetFullPath(resolver.ResolveItemTexture("forever_skyblock:test_block") ?? string.Empty) == Path.GetFullPath(expectedTexture),
                "A project block without an item-model wrapper must still resolve in GUI slot previews via blockstate/model data.");
            Check(Path.GetFullPath(resolver.ResolveItemTexture("test_block") ?? string.Empty) == Path.GetFullPath(expectedTexture),
                "An unqualified locally authored block id must prefer the discovered project namespace instead of being treated as vanilla-only.");

            var customAssets = Path.Combine(root, "mod", "src", "main", "resources", "assets", "examplemod");
            Directory.CreateDirectory(Path.Combine(customAssets, "models", "item"));
            Directory.CreateDirectory(Path.Combine(customAssets, "textures", "item"));
            File.WriteAllText(Path.Combine(customAssets, "models", "item", "custom_part.json"),
                "{\"parent\":\"minecraft:item/generated\",\"textures\":{\"layer0\":\"examplemod:item/custom_part\"}}");
            var customTexture = Path.Combine(customAssets, "textures", "item", "custom_part.png");
            File.WriteAllBytes(customTexture, [0]);
            Check(Path.GetFullPath(resolver.ResolveItemTexture("examplemod:custom_part") ?? string.Empty) == Path.GetFullPath(customTexture),
                "GUI preview resource resolution must remain namespace-generic for project-owned assets.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Tests frontend-permission-unknown against permissions declared as entries of a permissions.yaml file.</summary>
public sealed class FrontendPermissionCheckTests
{
    /// <summary>A frontend naming a permission its module's permissions.yaml declares reports nothing, and an undeclared one is reported.</summary>
    /// <proves>litespec/core/access#state-every-permission-with-its-meaning</proves>
    [Fact(DisplayName = "frontend permissions resolve against permissions.yaml entries")]
    public void FrontendPermissionsResolveAgainstPermissionEntries()
    {
        using var root = CopySmallWithFrontend();
        File.WriteAllText(
            Path.Combine(root.Path, "apps", "web", "src", "lib", "permissions.ts"),
            """
            export const BorrowCopy = 'permission/borrow-copy';
            export const Unknown = 'permission/print-receipts';
            """);

        var result = TestCli.Run("model", "check", "--root", root.Path);

        Assert.DoesNotContain("names permission 'permission/borrow-copy'", result.Output);
        Assert.Contains("names permission 'permission/print-receipts'", result.Output);
    }

    /// <summary>Copies the small model fixture and gives it a Svelte frontend at apps/web.</summary>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopySmallWithFrontend()
    {
        var source = TestCli.Fixture("Model/small");
        var temp = TestCli.Temp("frontend-permissions");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        var project = Path.Combine(temp.Path, ".litespec", "project.yaml");
        File.WriteAllText(project, File.ReadAllText(project).Replace(
            "frontends: []",
            """
            frontends:
            - name: web
              path: apps/web
              platform: svelte
            """,
            StringComparison.Ordinal));
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "web", "src", "lib"));

        return temp;
    }
}

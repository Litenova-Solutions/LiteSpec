using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Tests for <c>litespec rename</c>.</summary>
public sealed class RenameCommandTests
{
    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename rewrites the identifier in source and test files as well as pages")]
    public void Rename_Rewrites_Code_Citations()
    {
        using var fixture = ModelFixture.Prepare("small");
        var source = Path.Combine(fixture.Path, "src", "App", "Lending", "Loans", "BorrowCopy", "BorrowCopyOnRequestReaction.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllText(source, "/// <implements>use-case/lending.borrow-copy</implements>\ninternal sealed class BorrowCopyOnRequestReaction;\n");

        var result = Cli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/lending.borrow-item", File.ReadAllText(source));
        Assert.DoesNotContain("use-case/lending.borrow-copy", File.ReadAllText(source));
    }

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename points links to the moved page at its new path")]
    public void Rename_Rewrites_Links_To_The_Moved_Page()
    {
        using var fixture = ModelFixture.Prepare("small");
        var loans = Path.Combine(fixture.Path, "docs", "behavior", "modules", "lending", "loans");
        var linking = Path.Combine(fixture.Path, "docs", "behavior", "modules", "lending", "notes.md");
        File.WriteAllText(linking, "# Notes\n\nSee [borrowing](loans/borrow-copy.md#goal).\n");

        var result = Cli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(loans, "borrow-item.md")));
        Assert.Contains("[borrowing](loans/borrow-item.md#goal)", File.ReadAllText(linking));
    }
}

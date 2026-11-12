using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec rename</c>.</summary>
public sealed class RenameCommandTests
{
    /// <summary>Rename rewrites the identifier in source and test files as well as pages.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename rewrites the identifier in source and test files as well as pages")]
    public void RenameRewritesCodeCitations()
    {
        using var fixture = ModelFixture.Prepare("small");
        var source = Path.Combine(fixture.Path, "src", "App", "Lending", "Loans", "BorrowCopy", "BorrowCopyOnRequestReaction.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllText(source, "/// <implements>use-case/lending.borrow-copy</implements>\ninternal sealed class BorrowCopyOnRequestReaction;\n");

        var result = TestCli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/lending.borrow-item", File.ReadAllText(source));
        Assert.DoesNotContain("use-case/lending.borrow-copy", File.ReadAllText(source));
    }

    /// <summary>Rename points citations to the moved page at its new identifier.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename points citations to the moved page at its new identifier")]
    public void RenameRewritesLinksToTheMovedPage()
    {
        using var fixture = ModelFixture.Prepare("small");
        var loans = Path.Combine(fixture.Path, ".litespec", "behavior", "modules", "lending", "loans");
        var linking = Path.Combine(fixture.Path, ".litespec", "behavior", "modules", "lending", "notes.md");
        File.WriteAllText(linking, "---\ntype: reference\nid: notes\ntitle: Notes\n---\n\n# Notes\n\nSee [borrowing](use-case/lending.borrow-copy).\n");

        var result = TestCli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(loans, "borrow-item.yaml")));
        Assert.False(File.Exists(Path.Combine(loans, "borrow-copy.yaml")));
        Assert.True(File.Exists(Path.Combine(loans, "borrow-item.md")));
        Assert.Contains("[borrowing](use-case/lending.borrow-item)", File.ReadAllText(linking));
    }
}
